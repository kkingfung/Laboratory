using UnityEngine;
using System.Collections.Generic;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Gameplay;
using Laboratory.Gameplay.Inventory;
using Laboratory.UI.Helper;
using System;
using System.Linq;

namespace Laboratory.Inventory
{
    /// <summary>
    /// Comprehensive Enhanced Inventory System
    /// Version 3.1 - Complete item management with reactive updates and networking support
    /// </summary>
    public class EnhancedInventorySystem : MonoBehaviour
    {
        [Header("Inventory Configuration")]
        [SerializeField] private int maxSlots = 50;
        [SerializeField] private int quickSlots = 9;
        [SerializeField] private bool enableAutoSort = true;
        [SerializeField] private bool enableDebugLogs = false;
        
        [Header("UI References")]
        [SerializeField] private InventoryUI inventoryUI;
        
        private Dictionary<int, InventoryItem> items = new();
        private Dictionary<string, int> itemCounts = new();
        private HashSet<int> quickSlotIndices = new();
        private IEventBus eventBus;
        private List<IDisposable> eventSubscriptions = new();
        
        public event Action<InventoryItem> ItemAdded;
        public event Action<InventoryItem> ItemRemoved;
        public event Action<InventoryItem> ItemUsed;
        public event Action<InventoryItem> ItemMoved;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeInventorySystem();
        }

        private void Start()
        {
            SetupQuickSlots();
            
            if (inventoryUI != null)
            {
                inventoryUI.Initialize(this);
            }
        }


        
        #endregion

        #region Initialization
        
        private void InitializeInventorySystem()
        {
            try
            {
                // Get event bus for reactive updates
                eventBus = GlobalServiceProvider.Resolve<IEventBus>();
                
                // Register this system as a service (commented out due to missing method)
                // GlobalServiceProvider.GetContainer().Register<IInventorySystem>(this);
                
                // Subscribe to relevant events
                SubscribeToEvents();
                
                if (enableDebugLogs)
                    Debug.Log("[InventorySystem] Successfully initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySystem] Failed to initialize: {ex.Message}");
            }
        }

        private void SubscribeToEvents()
        {
            eventSubscriptions.Add(eventBus.Subscribe<ItemDroppedEvent>(HandleItemDropped));
            eventSubscriptions.Add(eventBus.Subscribe<ItemPickupRequestEvent>(HandleItemPickupRequest));
            eventSubscriptions.Add(eventBus.Subscribe<InventorySortRequestEvent>(HandleSortRequest));
        }

        private void SetupQuickSlots()
        {
            // Initialize quick slots (0-8 for hotbar)
            quickSlotIndices.Clear();
            for (int i = 0; i < quickSlots; i++)
            {
                quickSlotIndices.Add(i);
            }
        }
        
        #endregion

        #region Public API

        public bool TryAddItem(ItemData itemData, int quantity = 1)
        {
            if (itemData == null)
            {
                Debug.LogWarning("[InventorySystem] Attempted to add null item data");
                return false;
            }

            if (quantity <= 0)
            {
                Debug.LogWarning("[InventorySystem] Attempted to add item with invalid quantity");
                return false;
            }

            try
            {
                // Check if we can add the item
                if (!CanAddItem(itemData, quantity))
                {
                    eventBus.Publish(new InventoryFullEvent());
                    return false;
                }
                
                // Try to stack first if possible
                if (itemData.IsStackable && TryStackExistingItem(itemData, quantity))
                {
                    if (enableDebugLogs)
                        Debug.Log($"[InventorySystem] Stacked {quantity}x {itemData.ItemName}");
                    return true;
                }
                
                // Add to new slot
                if (TryAddToNewSlot(itemData, quantity))
                {
                    if (enableDebugLogs)
                        Debug.Log($"[InventorySystem] Added {quantity}x {itemData.ItemName} to new slot");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySystem] Error adding item: {ex.Message}");
                return false;
            }
        }

        public bool TryRemoveItem(string itemID, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemID) || quantity <= 0)
                return false;

            try
            {
                if (!CanRemoveItem(itemID, quantity))
                    return false;
                    
                var item = GetItemByID(itemID);
                if (item == null) return false;
                
                // Remove the specified quantity
                item.Quantity -= quantity;
                itemCounts[itemID] -= quantity;
                
                // Remove completely if quantity reaches 0
                if (item.Quantity <= 0)
                {
                    items.Remove(item.SlotIndex);
                    if (itemCounts[itemID] <= 0)
                        itemCounts.Remove(itemID);
                }
                
                // Fire events
                ItemRemoved?.Invoke(item);
                eventBus.Publish(new ItemRemovedEvent(item));
                
                if (enableDebugLogs)
                    Debug.Log($"[InventorySystem] Removed {quantity}x {item.ItemData.ItemName}");
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySystem] Error removing item: {ex.Message}");
                return false;
            }
        }

        public bool TryUseItem(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
                return false;

            try
            {
                var item = GetItemByID(itemID);
                if (item == null || !item.ItemData.IsUsable)
                    return false;

                // Check if item is on cooldown
                if (item.IsOnCooldown())
                {
                    if (enableDebugLogs)
                        Debug.Log($"[InventorySystem] Item {item.ItemData.ItemName} is on cooldown");
                    return false;
                }
                    
                // Execute item use logic
                bool useSuccessful = item.ItemData.Use();
                
                if (useSuccessful)
                {
                    // Start cooldown if applicable
                    if (item.ItemData.CooldownTime > 0)
                    {
                        item.StartCooldown();
                    }
                    
                    // Fire events
                    ItemUsed?.Invoke(item);
                    eventBus.Publish(new ItemUsedEvent(item));
                    
                    // Remove consumable items
                    if (item.ItemData.IsConsumable)
                    {
                        TryRemoveItem(itemID, 1);
                    }
                    
                    if (enableDebugLogs)
                        Debug.Log($"[InventorySystem] Used item: {item.ItemData.ItemName}");
                }
                
                return useSuccessful;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySystem] Error using item: {ex.Message}");
                return false;
            }
        }

        public bool TryMoveItem(int fromSlot, int toSlot)
        {
            if (fromSlot == toSlot || !IsValidSlot(fromSlot) || !IsValidSlot(toSlot))
                return false;

            try
            {
                if (!items.ContainsKey(fromSlot))
                    return false;

                var itemToMove = items[fromSlot];
                
                // Check if destination slot is occupied
                if (items.ContainsKey(toSlot))
                {
                    var destinationItem = items[toSlot];
                    
                    // Try to stack if same item type
                    if (itemToMove.ItemData.ItemID == destinationItem.ItemData.ItemID && 
                        itemToMove.ItemData.IsStackable)
                    {
                        return TryStackItems(itemToMove, destinationItem);
                    }
                    else
                    {
                        // Swap items
                        return TrySwapItems(fromSlot, toSlot);
                    }
                }
                else
                {
                    // Move to empty slot
                    items.Remove(fromSlot);
                    itemToMove.SlotIndex = toSlot;
                    items[toSlot] = itemToMove;
                    
                    ItemMoved?.Invoke(itemToMove);
                    eventBus.Publish(new ItemMovedEvent(itemToMove, fromSlot, toSlot));
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySystem] Error moving item: {ex.Message}");
                return false;
            }
        }

        public void SortInventory()
        {
            try
            {
                var sortedItems = items.Values
                    .OrderBy(item => item.ItemData.ItemType)
                    .ThenBy(item => item.ItemData.ItemName)
                    .ToList();

                // Clear current items
                items.Clear();
                
                // Re-add sorted items to new positions
                int currentSlot = quickSlots; // Start after quick slots
                foreach (var item in sortedItems)
                {
                    // Skip quick slot items during sort
                    if (quickSlotIndices.Contains(item.SlotIndex))
                        continue;
                        
                    item.SlotIndex = currentSlot;
                    items[currentSlot] = item;
                    currentSlot++;
                }
                
                eventBus.Publish(new InventorySortedEvent());
                
                if (enableDebugLogs)
                    Debug.Log("[InventorySystem] Inventory sorted");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySystem] Error sorting inventory: {ex.Message}");
            }
        }
        
        public InventoryItem GetItem(int slotIndex)
        {
            return items.TryGetValue(slotIndex, out var item) ? item : null;
        }

        public InventoryItem[] GetAllItems()
        {
            return items.Values.ToArray();
        }

        public InventoryItem[] GetQuickSlotItems()
        {
            return quickSlotIndices.Select(GetItem).Where(item => item != null).ToArray();
        }

        public int GetItemCount(string itemID)
        {
            return itemCounts.TryGetValue(itemID, out var count) ? count : 0;
        }

        public bool HasItem(string itemID, int requiredQuantity = 1)
        {
            return GetItemCount(itemID) >= requiredQuantity;
        }

        public int GetEmptySlotCount()
        {
            return maxSlots - items.Count;
        }

        public bool IsSlotEmpty(int slotIndex)
        {
            return !items.ContainsKey(slotIndex);
        }

        public bool IsQuickSlot(int slotIndex)
        {
            return quickSlotIndices.Contains(slotIndex);
        }
        
        #endregion

        #region Private Helper Methods

        private bool CanAddItem(ItemData itemData, int quantity)
        {
            if (itemData.IsStackable && itemCounts.ContainsKey(itemData.ItemID))
            {
                var existingItem = GetItemByID(itemData.ItemID);
                return existingItem != null && existingItem.CanAddToStack(quantity);
            }
            
            return GetEmptySlotCount() > 0;
        }

        private bool CanRemoveItem(string itemID, int quantity)
        {
            return itemCounts.ContainsKey(itemID) && itemCounts[itemID] >= quantity;
        }

        private bool TryStackExistingItem(ItemData itemData, int quantity)
        {
            if (!itemCounts.ContainsKey(itemData.ItemID))
                return false;

            var existingItem = GetItemByID(itemData.ItemID);
            if (existingItem == null || !existingItem.CanAddToStack(quantity))
                return false;

            existingItem.Quantity += quantity;
            itemCounts[itemData.ItemID] += quantity;

            ItemAdded?.Invoke(existingItem);
            eventBus.Publish(new ItemAddedEvent(existingItem));
            
            return true;
        }

        private bool TryAddToNewSlot(ItemData itemData, int quantity)
        {
            int emptySlot = GetFirstEmptySlot();
            if (emptySlot < 0)
                return false;

            var newItem = new InventoryItem(itemData, quantity, emptySlot);
            items[emptySlot] = newItem;

            if (!itemCounts.ContainsKey(itemData.ItemID))
                itemCounts[itemData.ItemID] = 0;
            itemCounts[itemData.ItemID] += quantity;

            ItemAdded?.Invoke(newItem);
            eventBus.Publish(new ItemAddedEvent(newItem));
            
            return true;
        }

        private bool TryStackItems(InventoryItem source, InventoryItem destination)
        {
            int transferAmount = Mathf.Min(source.Quantity, 
                                         destination.ItemData.MaxStackSize - destination.Quantity);
            
            if (transferAmount <= 0)
                return false;

            source.Quantity -= transferAmount;
            destination.Quantity += transferAmount;

            if (source.Quantity <= 0)
            {
                items.Remove(source.SlotIndex);
            }

            eventBus.Publish(new ItemStackedEvent(source, destination, transferAmount));
            return true;
        }

        private bool TrySwapItems(int fromSlot, int toSlot)
        {
            var item1 = items[fromSlot];
            var item2 = items[toSlot];

            item1.SlotIndex = toSlot;
            item2.SlotIndex = fromSlot;

            items[fromSlot] = item2;
            items[toSlot] = item1;

            eventBus.Publish(new ItemsSwappedEvent(item1, item2));
            return true;
        }

        private InventoryItem GetItemByID(string itemID)
        {
            return items.Values.FirstOrDefault(item => item.ItemData.ItemID == itemID);
        }

        private int GetFirstEmptySlot()
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (!items.ContainsKey(i))
                    return i;
            }
            return -1;
        }

        private bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < maxSlots;
        }

        #endregion

        #region Event Handlers

        private void HandleItemDropped(ItemDroppedEvent dropEvent)
        {
            // Remove item from inventory when dropped
            TryRemoveItem(dropEvent.ItemID, dropEvent.Quantity);
        }

        private void HandleItemPickupRequest(ItemPickupRequestEvent pickupEvent)
        {
            // Try to add item when pickup is requested
            bool success = TryAddItem(pickupEvent.ItemData, pickupEvent.Quantity);
            eventBus.Publish(new ItemPickupResultEvent(pickupEvent.ItemData, success));
        }

        private void HandleSortRequest(InventorySortRequestEvent sortEvent)
        {
            if (enableAutoSort)
            {
                SortInventory();
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Dispose of all event subscriptions
            foreach (var subscription in eventSubscriptions)
            {
                subscription?.Dispose();
            }
            eventSubscriptions.Clear();
        }
    }

    #region Enhanced Inventory Item

    /// <summary>
    /// Enhanced Inventory Item with cooldown support and advanced features
    /// </summary>
    [System.Serializable]
    public class InventoryItem
    {
        public ItemData ItemData { get; private set; }
        public int Quantity { get; set; }
        public int SlotIndex { get; set; }
        public DateTime AcquiredTime { get; private set; }
        public DateTime LastUsedTime { get; private set; }
        
        public InventoryItem(ItemData itemData, int quantity, int slotIndex)
        {
            ItemData = itemData;
            Quantity = quantity;
            SlotIndex = slotIndex;
            AcquiredTime = DateTime.Now;
            LastUsedTime = DateTime.MinValue;
        }

        public bool CanAddToStack(int additionalQuantity)
        {
            return ItemData.IsStackable && 
                   (Quantity + additionalQuantity) <= ItemData.MaxStackSize;
        }

        public bool IsOnCooldown()
        {
            if (ItemData.CooldownTime <= 0)
                return false;
                
            var timeSinceLastUse = DateTime.Now - LastUsedTime;
            return timeSinceLastUse.TotalSeconds < ItemData.CooldownTime;
        }

        public float GetCooldownRemaining()
        {
            if (!IsOnCooldown())
                return 0f;
                
            var timeSinceLastUse = DateTime.Now - LastUsedTime;
            return ItemData.CooldownTime - (float)timeSinceLastUse.TotalSeconds;
        }

        public void StartCooldown()
        {
            LastUsedTime = DateTime.Now;
        }
    }

    #endregion

    #region Interfaces

    /// <summary>
    /// Interface for inventory system
    /// </summary>
    public interface IInventorySystem
    {
        bool TryAddItem(ItemData itemData, int quantity = 1);
        bool TryRemoveItem(string itemID, int quantity = 1);
        bool TryUseItem(string itemID);
        bool TryMoveItem(int fromSlot, int toSlot);
        void SortInventory();
        
        InventoryItem GetItem(int slotIndex);
        InventoryItem[] GetAllItems();
        int GetItemCount(string itemID);
        bool HasItem(string itemID, int requiredQuantity = 1);
        int GetEmptySlotCount();
    }

    #endregion

    #region Events

    public class ItemAddedEvent
    {
        public InventoryItem Item { get; }
        public ItemAddedEvent(InventoryItem item) => Item = item;
    }

    public class ItemRemovedEvent
    {
        public InventoryItem Item { get; }
        public ItemRemovedEvent(InventoryItem item) => Item = item;
    }

    public class ItemUsedEvent
    {
        public InventoryItem Item { get; }
        public ItemUsedEvent(InventoryItem item) => Item = item;
    }

    public class ItemMovedEvent
    {
        public InventoryItem Item { get; }
        public int FromSlot { get; }
        public int ToSlot { get; }
        public ItemMovedEvent(InventoryItem item, int fromSlot, int toSlot)
        {
            Item = item;
            FromSlot = fromSlot;
            ToSlot = toSlot;
        }
    }

    public class InventoryFullEvent
    {
        public DateTime Timestamp { get; } = DateTime.Now;
    }

    public class ItemDroppedEvent
    {
        public string ItemID { get; }
        public int Quantity { get; }
        public Vector3 DropPosition { get; }
        
        public ItemDroppedEvent(string itemID, int quantity, Vector3 dropPosition)
        {
            ItemID = itemID;
            Quantity = quantity;
            DropPosition = dropPosition;
        }
    }

    public class ItemPickupRequestEvent
    {
        public ItemData ItemData { get; }
        public int Quantity { get; }
        
        public ItemPickupRequestEvent(ItemData itemData, int quantity)
        {
            ItemData = itemData;
            Quantity = quantity;
        }
    }

    public class ItemPickupResultEvent
    {
        public ItemData ItemData { get; }
        public bool Success { get; }
        
        public ItemPickupResultEvent(ItemData itemData, bool success)
        {
            ItemData = itemData;
            Success = success;
        }
    }

    public class ItemStackedEvent
    {
        public InventoryItem SourceItem { get; }
        public InventoryItem DestinationItem { get; }
        public int TransferredQuantity { get; }
        
        public ItemStackedEvent(InventoryItem sourceItem, InventoryItem destinationItem, int transferredQuantity)
        {
            SourceItem = sourceItem;
            DestinationItem = destinationItem;
            TransferredQuantity = transferredQuantity;
        }
    }

    public class ItemsSwappedEvent
    {
        public InventoryItem Item1 { get; }
        public InventoryItem Item2 { get; }
        
        public ItemsSwappedEvent(InventoryItem item1, InventoryItem item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }

    public class InventorySortedEvent
    {
        public DateTime SortTime { get; } = DateTime.Now;
    }

    public class InventorySortRequestEvent
    {
        public bool SortByType { get; }
        public bool SortByName { get; }
        
        public InventorySortRequestEvent(bool sortByType = true, bool sortByName = true)
        {
            SortByType = sortByType;
            SortByName = sortByName;
        }
    }

    #endregion
}