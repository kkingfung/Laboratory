using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.DI;

namespace Laboratory.Subsystems.Inventory
{
    #region Events

    /// <summary>
    /// Event data for inventory changes
    /// </summary>
    public class InventoryChangedEvent
    {
        public IInventorySystem Inventory { get; }
        public InventorySlot ChangedSlot { get; }
        public InventoryChangeType ChangeType { get; }
        public int OldQuantity { get; }
        public int NewQuantity { get; }
        public float Timestamp { get; }

        public InventoryChangedEvent(IInventorySystem inventory, InventorySlot slot, 
            InventoryChangeType changeType, int oldQuantity = 0, int newQuantity = 0)
        {
            Inventory = inventory;
            ChangedSlot = slot;
            ChangeType = changeType;
            OldQuantity = oldQuantity;
            NewQuantity = newQuantity;
            Timestamp = Time.unscaledTime;
        }
    }

    public enum InventoryChangeType
    {
        ItemAdded,
        ItemRemoved,
        QuantityChanged,
        SlotCleared,
        InventoryCleared
    }

    #endregion

    #region Implementation

    /// <summary>
    /// Unified implementation of the inventory system
    /// Provides comprehensive inventory management with events and validation
    /// Uses existing ItemData and InventorySlot classes for compatibility
    /// </summary>
    public class UnifiedInventorySystem : MonoBehaviour, IInventorySystem
    {
        #region Fields

        [Header("Inventory Configuration")]
        [SerializeField] private int inventoryCapacity = 30;
        [SerializeField] private bool enableEvents = true;
        [SerializeField] private bool enableLogging = false;
        [SerializeField] private bool autoRegisterWithDI = true;

        private List<InventorySlot> inventorySlots;
        private Dictionary<string, ItemData> itemDatabase;
        private IEventBus _eventBus;

        #endregion

        #region Properties

        public int MaxSlots => inventoryCapacity;
        public int UsedSlots => GetUsedSlotCount();
        public int AvailableSlots => MaxSlots - UsedSlots;
        public bool IsFull => UsedSlots >= MaxSlots;

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
            // Try to get event bus from DI
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.TryResolve<IEventBus>(out _eventBus);
            }

            // Register this system with DI if enabled
            if (autoRegisterWithDI && GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.Instance.RegisterInstance<IInventorySystem>(this);
                if (enableLogging)
                    Debug.Log("[UnifiedInventorySystem] Registered with DI container");
            }

            LoadItemDatabase();
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            inventorySlots = new List<InventorySlot>(inventoryCapacity);
            itemDatabase = new Dictionary<string, ItemData>();

            // Initialize empty slots
            for (int i = 0; i < inventoryCapacity; i++)
            {
                inventorySlots.Add(new InventorySlot(i));
            }

            if (enableLogging)
                Debug.Log($"[UnifiedInventorySystem] Initialized with capacity: {inventoryCapacity}");
        }

        private void LoadItemDatabase()
        {
            // Load all ItemData assets from Resources
            var items = Resources.LoadAll<ItemData>("Items");
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.ItemID))
                {
                    itemDatabase[item.ItemID] = item;
                }
            }

            // Also try ItemData folder
            var itemsAlt = Resources.LoadAll<ItemData>("ItemData");
            foreach (var item in itemsAlt)
            {
                if (!string.IsNullOrEmpty(item.ItemID))
                {
                    itemDatabase[item.ItemID] = item;
                }
            }
            
            if (enableLogging)
                Debug.Log($"[UnifiedInventorySystem] Loaded {itemDatabase.Count} items into database");
        }

        #endregion

        #region Item Operations (IInventorySystem Implementation)

        public bool TryAddItem(ItemData item, int quantity = 1)
        {
            return AddItem(item, quantity);
        }

        public bool TryRemoveItem(ItemData item, int quantity = 1)
        {
            return RemoveItem(item, quantity);
        }

        public bool TryRemoveItem(string itemId, int quantity = 1)
        {
            return RemoveItem(itemId, quantity);
        }

        public bool AddItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
            {
                if (enableLogging)
                    Debug.LogWarning("[UnifiedInventorySystem] Cannot add null item or invalid quantity");
                return false;
            }

            int remainingQuantity = quantity;

            // First, try to add to existing stacks
            if (item.IsStackable)
            {
                for (int i = 0; i < inventorySlots.Count && remainingQuantity > 0; i++)
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

            // Then, add to empty slots
            while (remainingQuantity > 0)
            {
                int emptySlotIndex = FindEmptySlot();
                if (emptySlotIndex == -1)
                {
                    if (enableLogging)
                        Debug.LogWarning("[UnifiedInventorySystem] Inventory is full");
                    break;
                }

                var slot = inventorySlots[emptySlotIndex];
                int addAmount = Mathf.Min(remainingQuantity, item.MaxStackSize);
                slot.SetItem(item, addAmount);
                remainingQuantity -= addAmount;

                FireItemChangedEvents(slot, InventoryChangeType.ItemAdded);
            }

            bool success = remainingQuantity == 0;
            if (success)
            {
                FireInventoryChangedEvent();
                if (enableLogging)
                    Debug.Log($"[UnifiedInventorySystem] Added {quantity} x {item.ItemName}");
            }

            return success;
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return false;

            int remainingQuantity = quantity;

            for (int i = 0; i < inventorySlots.Count && remainingQuantity > 0; i++)
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
                FireInventoryChangedEvent();
                if (enableLogging)
                    Debug.Log($"[UnifiedInventorySystem] Removed {quantity} x {itemId}");
            }

            return success;
        }

        public bool RemoveItem(ItemData item, int quantity = 1)
        {
            if (item == null)
                return false;

            return RemoveItem(item.ItemID, quantity);
        }

        public void ClearInventory()
        {
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                if (!inventorySlots[i].IsEmpty)
                {
                    inventorySlots[i].Clear();
                }
            }

            FireInventoryChangedEvent();
            if (enableLogging)
                Debug.Log("[UnifiedInventorySystem] Inventory cleared");
        }

        #endregion

        #region Query Operations

        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;

            int count = 0;
            foreach (var slot in inventorySlots)
            {
                if (!slot.IsEmpty && slot.Item.ItemID == itemId)
                {
                    count += slot.Quantity;
                }
            }

            return count;
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
            if (index >= 0 && index < inventorySlots.Count)
                return inventorySlots[index];

            return null;
        }

        public List<InventorySlot> GetAllSlots()
        {
            return new List<InventorySlot>(inventorySlots);
        }

        public List<InventorySlot> GetAllItems()
        {
            var items = new List<InventorySlot>();
            foreach (var slot in inventorySlots)
            {
                if (!slot.IsEmpty)
                {
                    items.Add(slot);
                }
            }
            return items;
        }

        #endregion

        #region Utility Methods

        private int FindEmptySlot()
        {
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                if (inventorySlots[i].IsEmpty)
                    return i;
            }
            return -1;
        }

        private int GetUsedSlotCount()
        {
            int count = 0;
            foreach (var slot in inventorySlots)
            {
                if (!slot.IsEmpty)
                    count++;
            }
            return count;
        }

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

                // Publish global event using proper event system
                if (_eventBus != null)
                {
                    var eventData = new InventoryChangedEvent(this, slot, changeType);
                    _eventBus.Publish(eventData);
                }
            }
            catch (Exception ex)
            {
                if (enableLogging)
                    Debug.LogError($"[UnifiedInventorySystem] Error firing events: {ex.Message}");
            }
        }

        private void FireInventoryChangedEvent()
        {
            if (!enableEvents) return;

            try
            {
                OnInventoryChanged?.Invoke();
                
                // Also publish a general inventory changed event
                if (_eventBus != null)
                {
                    var eventData = new InventoryChangedEvent(this, null, InventoryChangeType.InventoryCleared);
                    _eventBus.Publish(eventData);
                }
            }
            catch (Exception ex)
            {
                if (enableLogging)
                    Debug.LogError($"[UnifiedInventorySystem] Error firing inventory changed event: {ex.Message}");
            }
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Adds an item to the database for later use
        /// </summary>
        public void AddItemToDatabase(ItemData item)
        {
            if (item != null && !string.IsNullOrEmpty(item.ItemID))
            {
                itemDatabase[item.ItemID] = item;
                if (enableLogging)
                    Debug.Log($"[UnifiedInventorySystem] Added item to database: {item.ItemName}");
            }
        }

        /// <summary>
        /// Gets an item from the database
        /// </summary>
        public ItemData GetItemFromDatabase(string itemId)
        {
            itemDatabase.TryGetValue(itemId, out ItemData item);
            return item;
        }

        /// <summary>
        /// Adds an item by ID (looks up in database first)
        /// </summary>
        public bool AddItemById(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return false;

            // Try to get item from database
            if (itemDatabase.TryGetValue(itemId, out ItemData itemData))
            {
                return AddItem(itemData, quantity);
            }

            if (enableLogging)
                Debug.LogWarning($"[UnifiedInventorySystem] Item not found in database: {itemId}");
            
            return false;
        }

        /// <summary>
        /// Sorts the inventory by item name
        /// </summary>
        public void SortInventory()
        {
            var nonEmptySlots = GetAllItems().ToList();
            ClearInventory();

            // Sort by item name
            nonEmptySlots.Sort((a, b) => string.Compare(a.Item.ItemName, b.Item.ItemName, StringComparison.OrdinalIgnoreCase));

            // Re-add items
            foreach (var slot in nonEmptySlots)
            {
                AddItem(slot.Item, slot.Quantity);
            }

            if (enableLogging)
                Debug.Log("[UnifiedInventorySystem] Inventory sorted");
        }

        /// <summary>
        /// Gets inventory statistics
        /// </summary>
        public InventoryStats GetInventoryStats()
        {
            var allItems = GetAllItems();
            var stats = new InventoryStats
            {
                TotalSlots = MaxSlots,
                UsedSlots = UsedSlots,
                AvailableSlots = AvailableSlots,
                TotalItems = allItems.Sum(slot => slot.Quantity),
                UniqueItems = allItems.Count,
                TotalValue = (int)allItems.Sum(slot => slot.Item.Value * slot.Quantity)
            };

            return stats;
        }

        /// <summary>
        /// Transfers an item to another inventory system
        /// </summary>
        public bool TransferItem(IInventorySystem targetInventory, string itemId, int quantity = 1)
        {
            if (targetInventory == null || string.IsNullOrEmpty(itemId) || quantity <= 0)
                return false;

            // Check if we have enough of the item
            if (!HasItem(itemId, quantity))
                return false;

            // Find the item in our inventory
            ItemData itemToTransfer = null;
            foreach (var slot in inventorySlots)
            {
                if (!slot.IsEmpty && slot.Item.ItemID == itemId)
                {
                    itemToTransfer = slot.Item;
                    break;
                }
            }

            if (itemToTransfer == null)
                return false;

            // Try to add to target inventory
            if (targetInventory.AddItem(itemToTransfer, quantity))
            {
                // Remove from our inventory
                return RemoveItem(itemId, quantity);
            }

            return false;
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            if (inventoryCapacity <= 0)
                inventoryCapacity = 1;

            if (inventorySlots != null && inventorySlots.Count != inventoryCapacity)
            {
                ResizeInventory(inventoryCapacity);
            }
        }

        private void ResizeInventory(int newCapacity)
        {
            if (inventorySlots == null)
            {
                Initialize();
                return;
            }

            // If shrinking, move items from slots that will be removed
            if (newCapacity < inventorySlots.Count)
            {
                for (int i = newCapacity; i < inventorySlots.Count; i++)
                {
                    var slot = inventorySlots[i];
                    if (!slot.IsEmpty)
                    {
                        // Try to move item to available slot
                        if (!AddItem(slot.Item, slot.Quantity))
                        {
                            if (enableLogging)
                                Debug.LogWarning($"[UnifiedInventorySystem] Lost item during resize: {slot.Item.ItemName} x{slot.Quantity}");
                        }
                    }
                }
            }

            // Resize the list
            while (inventorySlots.Count < newCapacity)
            {
                inventorySlots.Add(new InventorySlot(inventorySlots.Count));
            }
            while (inventorySlots.Count > newCapacity)
            {
                inventorySlots.RemoveAt(inventorySlots.Count - 1);
            }

            inventoryCapacity = newCapacity;
        }

        #endregion
    }

    #endregion

    #region Supporting Classes

    /// <summary>
    /// Statistics about the inventory state
    /// </summary>
    [Serializable]
    public class InventoryStats
    {
        public int TotalSlots;
        public int UsedSlots;
        public int AvailableSlots;
        public int TotalItems;
        public int UniqueItems;
        public int TotalValue;

        public float UsagePercentage => TotalSlots > 0 ? (float)UsedSlots / TotalSlots * 100f : 0.0f;
        
        public override string ToString()
        {
            return $"Inventory Stats: {UsedSlots}/{TotalSlots} slots used ({UsagePercentage:F1}%), {TotalItems} total items, {UniqueItems} unique items, {TotalValue} total value";
        }
    }

    #endregion
}
