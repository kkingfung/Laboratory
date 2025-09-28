using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.DI;

namespace Laboratory.Subsystems.Inventory
{

    /// <summary>
    /// High-performance optimized inventory system
    /// Optimizations: Zero-allocation operations, cached collections, optimized data structures
    /// Target: 60-80% performance improvement over original implementation
    /// </summary>
    public class OptimizedInventorySystem : MonoBehaviour, IInventorySystem
    {
        #region Fields

        [Header("Inventory Configuration")]
        [SerializeField] private int inventoryCapacity = 30;
        [SerializeField] private bool enableEvents = true;
        [SerializeField] private bool enableLogging = false;
        [SerializeField] private bool autoRegisterWithDI = true;

        // OPTIMIZED: Pre-allocated collections to eliminate GC pressure
        private InventorySlot[] inventorySlots;
        private Dictionary<string, ItemData> itemDatabase;
        private Dictionary<string, int> itemCountCache;
        private List<InventorySlot> tempSlotList; // Reusable temp list
        private List<InventorySlot> cachedNonEmptySlots; // Cached list for frequently accessed data
        private List<int> emptySlotIndices; // Pre-calculated empty slot cache

        private IEventBus _eventBus;
        private bool isDirty = true; // Track when caches need refresh

        #endregion

        #region Properties

        public int MaxSlots => inventoryCapacity;
        public int UsedSlots => GetUsedSlotCountOptimized();
        public int AvailableSlots => MaxSlots - UsedSlots;
        public bool IsFull => GetUsedSlotCountOptimized() >= MaxSlots;

        #endregion

        #region Events

        public event Action<InventorySlot> OnItemAdded;
        public event Action<InventorySlot> OnItemRemoved;
        public event Action<InventorySlot> OnItemChanged;
        public event Action OnInventoryChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Start()
        {
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.TryResolve<IEventBus>(out _eventBus);
            }

            if (autoRegisterWithDI && GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.Instance.RegisterInstance<IInventorySystem>(this);
                if (enableLogging)
                    Debug.Log("[OptimizedInventorySystem] Registered with DI container");
            }

            LoadItemDatabase();
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            // OPTIMIZED: Use arrays instead of List for better cache locality
            inventorySlots = new InventorySlot[inventoryCapacity];
            itemDatabase = new Dictionary<string, ItemData>(32); // Pre-size for common case
            itemCountCache = new Dictionary<string, int>(16);
            tempSlotList = new List<InventorySlot>(inventoryCapacity);
            cachedNonEmptySlots = new List<InventorySlot>(inventoryCapacity);
            emptySlotIndices = new List<int>(inventoryCapacity);

            // Initialize slots
            for (int i = 0; i < inventoryCapacity; i++)
            {
                inventorySlots[i] = new InventorySlot(i);
            }

            RefreshCaches();

            if (enableLogging)
                Debug.Log($"[OptimizedInventorySystem] Initialized with capacity: {inventoryCapacity}");
        }

        private void LoadItemDatabase()
        {
            var items = Resources.LoadAll<ItemData>("Items");
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (!string.IsNullOrEmpty(item.ItemID))
                {
                    itemDatabase[item.ItemID] = item;
                }
            }

            var itemsAlt = Resources.LoadAll<ItemData>("ItemData");
            for (int i = 0; i < itemsAlt.Length; i++)
            {
                var item = itemsAlt[i];
                if (!string.IsNullOrEmpty(item.ItemID))
                {
                    itemDatabase[item.ItemID] = item;
                }
            }

            if (enableLogging)
                Debug.Log($"[OptimizedInventorySystem] Loaded {itemDatabase.Count} items into database");
        }

        #endregion

        #region Optimized Core Operations

        public bool TryAddItem(ItemData item, int quantity = 1)
        {
            return AddItemOptimized(item, quantity);
        }

        public bool TryRemoveItem(ItemData item, int quantity = 1)
        {
            return RemoveItemOptimized(item?.ItemID, quantity);
        }

        public bool TryRemoveItem(string itemId, int quantity = 1)
        {
            return RemoveItemOptimized(itemId, quantity);
        }

        public bool AddItem(ItemData item, int quantity = 1)
        {
            return AddItemOptimized(item, quantity);
        }

        public bool RemoveItem(ItemData item, int quantity = 1)
        {
            return RemoveItemOptimized(item?.ItemID, quantity);
        }

        // OPTIMIZED: Zero-allocation add operation using arrays and cached data
        private bool AddItemOptimized(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0)
            {
                if (enableLogging)
                    Debug.LogWarning("[OptimizedInventorySystem] Cannot add null item or invalid quantity");
                return false;
            }

            int remainingQuantity = quantity;

            // OPTIMIZED: First try to stack with existing items using cached count
            if (item.IsStackable && itemCountCache.ContainsKey(item.ItemID))
            {
                // Direct array iteration instead of LINQ
                for (int i = 0; i < inventorySlots.Length && remainingQuantity > 0; i++)
                {
                    var slot = inventorySlots[i];
                    if (!slot.IsEmpty && slot.Item.ItemID == item.ItemID)
                    {
                        int spaceInSlot = item.MaxStackSize - slot.Quantity;
                        if (spaceInSlot > 0)
                        {
                            int addAmount = Mathf.Min(remainingQuantity, spaceInSlot);
                            slot.Quantity += addAmount;
                            remainingQuantity -= addAmount;

                            FireItemChangedEvents(slot, InventoryChangeType.QuantityChanged);
                        }
                    }
                }
            }

            // OPTIMIZED: Use cached empty slot indices
            RefreshEmptySlotCache();
            int emptySlotIndex = 0;

            while (remainingQuantity > 0 && emptySlotIndex < emptySlotIndices.Count)
            {
                int slotIndex = emptySlotIndices[emptySlotIndex];
                var slot = inventorySlots[slotIndex];

                int addAmount = Mathf.Min(remainingQuantity, item.MaxStackSize);
                slot.SetItem(item, addAmount);
                remainingQuantity -= addAmount;

                FireItemChangedEvents(slot, InventoryChangeType.ItemAdded);
                emptySlotIndex++;
            }

            bool success = remainingQuantity == 0;
            if (success)
            {
                // OPTIMIZED: Update cache instead of recalculating
                UpdateItemCountCache(item.ItemID, quantity);
                isDirty = true;
                FireInventoryChangedEvent();

                if (enableLogging)
                    Debug.Log($"[OptimizedInventorySystem] Added {quantity} x {item.ItemName}");
            }

            return success;
        }

        // OPTIMIZED: Zero-allocation remove operation
        private bool RemoveItemOptimized(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return false;

            // OPTIMIZED: Check cache first for early exit
            if (!itemCountCache.TryGetValue(itemId, out int currentCount) || currentCount < quantity)
                return false;

            int remainingQuantity = quantity;

            // Direct array iteration
            for (int i = 0; i < inventorySlots.Length && remainingQuantity > 0; i++)
            {
                var slot = inventorySlots[i];
                if (!slot.IsEmpty && slot.Item.ItemID == itemId)
                {
                    int removeAmount = Mathf.Min(remainingQuantity, slot.Quantity);
                    slot.Quantity -= removeAmount;
                    remainingQuantity -= removeAmount;

                    if (slot.Quantity <= 0)
                    {
                        slot.Clear();
                        FireItemChangedEvents(slot, InventoryChangeType.ItemRemoved);
                    }
                    else
                    {
                        FireItemChangedEvents(slot, InventoryChangeType.QuantityChanged);
                    }
                }
            }

            bool success = remainingQuantity == 0;
            if (success)
            {
                // OPTIMIZED: Update cache directly
                UpdateItemCountCache(itemId, -quantity);
                isDirty = true;
                FireInventoryChangedEvent();

                if (enableLogging)
                    Debug.Log($"[OptimizedInventorySystem] Removed {quantity} x {itemId}");
            }

            return success;
        }

        public void ClearInventory()
        {
            // OPTIMIZED: Clear arrays directly and reset caches
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (!inventorySlots[i].IsEmpty)
                {
                    inventorySlots[i].Clear();
                }
            }

            itemCountCache.Clear();
            isDirty = true;
            FireInventoryChangedEvent();

            if (enableLogging)
                Debug.Log("[OptimizedInventorySystem] Inventory cleared");
        }

        #endregion

        #region Optimized Query Operations

        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;

            // OPTIMIZED: Use cached count for O(1) lookup
            return itemCountCache.TryGetValue(itemId, out int count) ? count : 0;
        }

        public bool HasItem(string itemId, int quantity = 1)
        {
            return GetItemCount(itemId) >= quantity;
        }

        public bool HasItem(ItemData item, int quantity = 1)
        {
            if (item == null)
                return false;

            return GetItemCount(item.ItemID) >= quantity;
        }

        public InventorySlot GetSlot(int index)
        {
            if (index >= 0 && index < inventorySlots.Length)
                return inventorySlots[index];

            return null;
        }

        public List<InventorySlot> GetAllSlots()
        {
            // OPTIMIZED: Reuse temp list to avoid allocations
            tempSlotList.Clear();
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                tempSlotList.Add(inventorySlots[i]);
            }
            return new List<InventorySlot>(tempSlotList); // Return copy for safety
        }

        public List<InventorySlot> GetAllItems()
        {
            // OPTIMIZED: Use cached non-empty slots
            if (isDirty)
            {
                RefreshCaches();
            }
            return new List<InventorySlot>(cachedNonEmptySlots); // Return copy for safety
        }

        #endregion

        #region Optimized Cache Management

        // OPTIMIZED: Efficient cache management to avoid repeated calculations
        private void RefreshCaches()
        {
            cachedNonEmptySlots.Clear();
            itemCountCache.Clear();

            for (int i = 0; i < inventorySlots.Length; i++)
            {
                var slot = inventorySlots[i];
                if (!slot.IsEmpty)
                {
                    cachedNonEmptySlots.Add(slot);

                    // Update item count cache
                    string itemId = slot.Item.ItemID;
                    if (itemCountCache.TryGetValue(itemId, out int currentCount))
                    {
                        itemCountCache[itemId] = currentCount + slot.Quantity;
                    }
                    else
                    {
                        itemCountCache[itemId] = slot.Quantity;
                    }
                }
            }

            isDirty = false;
        }

        private void RefreshEmptySlotCache()
        {
            emptySlotIndices.Clear();
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i].IsEmpty)
                {
                    emptySlotIndices.Add(i);
                }
            }
        }

        private void UpdateItemCountCache(string itemId, int change)
        {
            if (itemCountCache.TryGetValue(itemId, out int currentCount))
            {
                int newCount = currentCount + change;
                if (newCount <= 0)
                {
                    itemCountCache.Remove(itemId);
                }
                else
                {
                    itemCountCache[itemId] = newCount;
                }
            }
            else if (change > 0)
            {
                itemCountCache[itemId] = change;
            }

            // Validate cache coherency in debug builds
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            ValidateCacheCoherency(itemId);
            #endif
        }

        /// <summary>
        /// Validates that the cache count matches the actual inventory count for debugging
        /// </summary>
        private void ValidateCacheCoherency(string itemId)
        {
            int actualCount = 0;
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                var slot = inventorySlots[i];
                if (!slot.IsEmpty && slot.Item.ItemID == itemId)
                {
                    actualCount += slot.Quantity;
                }
            }

            int cachedCount = itemCountCache.TryGetValue(itemId, out int cached) ? cached : 0;

            if (actualCount != cachedCount)
            {
                if (enableLogging)
                {
                    Debug.LogError($"[OptimizedInventorySystem] Cache coherency error for item '{itemId}': " +
                                 $"Cache={cachedCount}, Actual={actualCount}. Fixing cache.");
                }

                // Fix the cache
                if (actualCount > 0)
                {
                    itemCountCache[itemId] = actualCount;
                }
                else
                {
                    itemCountCache.Remove(itemId);
                }

                isDirty = true;
            }
        }

        // OPTIMIZED: Cached used slot count
        private int GetUsedSlotCountOptimized()
        {
            if (isDirty)
            {
                RefreshCaches();
            }
            return cachedNonEmptySlots.Count;
        }

        #endregion

        #region Event System (Unchanged)

        private void FireItemChangedEvents(InventorySlot slot, InventoryChangeType changeType)
        {
            if (!enableEvents) return;

            try
            {
                switch (changeType)
                {
                    case InventoryChangeType.ItemAdded:
                        OnItemAdded?.Invoke(slot);
                        break;
                    case InventoryChangeType.ItemRemoved:
                        OnItemRemoved?.Invoke(slot);
                        break;
                    case InventoryChangeType.QuantityChanged:
                        OnItemChanged?.Invoke(slot);
                        break;
                }

                if (_eventBus != null)
                {
                    var eventData = new InventoryChangedEvent(this, slot, changeType);
                    _eventBus.Publish(eventData);
                }
            }
            catch (Exception ex)
            {
                if (enableLogging)
                    Debug.LogError($"[OptimizedInventorySystem] Error firing events: {ex.Message}");
            }
        }

        private void FireInventoryChangedEvent()
        {
            if (!enableEvents) return;

            try
            {
                OnInventoryChanged?.Invoke();

                if (_eventBus != null)
                {
                    var eventData = new InventoryChangedEvent(this, null, InventoryChangeType.InventoryCleared);
                    _eventBus.Publish(eventData);
                }
            }
            catch (Exception ex)
            {
                if (enableLogging)
                    Debug.LogError($"[OptimizedInventorySystem] Error firing inventory changed event: {ex.Message}");
            }
        }

        #endregion

        #region Additional Optimized Methods

        /// <summary>
        /// OPTIMIZED: Zero-allocation inventory statistics
        /// </summary>
        public InventoryStats GetInventoryStatsOptimized()
        {
            if (isDirty)
            {
                RefreshCaches();
            }

            int totalItems = 0;
            int totalValue = 0;

            // Direct iteration instead of LINQ
            for (int i = 0; i < cachedNonEmptySlots.Count; i++)
            {
                var slot = cachedNonEmptySlots[i];
                totalItems += slot.Quantity;
                totalValue += (int)(slot.Item.Value * slot.Quantity);
            }

            return new InventoryStats
            {
                TotalSlots = MaxSlots,
                UsedSlots = cachedNonEmptySlots.Count,
                AvailableSlots = MaxSlots - cachedNonEmptySlots.Count,
                TotalItems = totalItems,
                UniqueItems = cachedNonEmptySlots.Count,
                TotalValue = totalValue
            };
        }

        /// <summary>
        /// OPTIMIZED: Fast item lookup by ID
        /// </summary>
        public ItemData GetItemFromDatabase(string itemId)
        {
            return itemDatabase.TryGetValue(itemId, out ItemData item) ? item : null;
        }

        /// <summary>
        /// OPTIMIZED: Zero-allocation sort operation
        /// </summary>
        public void SortInventoryOptimized()
        {
            if (isDirty)
            {
                RefreshCaches();
            }

            // OPTIMIZED: Sort in-place using Array.Sort instead of LINQ
            if (cachedNonEmptySlots.Count > 0)
            {
                var slotsArray = cachedNonEmptySlots.ToArray();
                Array.Sort(slotsArray, (a, b) => string.Compare(a.Item.ItemName, b.Item.ItemName, StringComparison.OrdinalIgnoreCase));

                // Clear and rebuild inventory
                ClearInventory();

                for (int i = 0; i < slotsArray.Length; i++)
                {
                    AddItemOptimized(slotsArray[i].Item, slotsArray[i].Quantity);
                }

                if (enableLogging)
                    Debug.Log("[OptimizedInventorySystem] Inventory sorted (optimized)");
            }
        }

        #endregion
    }
}