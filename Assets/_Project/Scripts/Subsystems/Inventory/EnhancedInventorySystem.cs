using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.DI;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// Enhanced inventory system for managing items
    /// </summary>
    public class EnhancedInventorySystem : MonoBehaviour, IInventorySystem
    {
        #region Fields

        [Header("Inventory Settings")]
        [SerializeField] private int maxSlots = 50;
        [SerializeField] private int maxStackSize = 99;
        [SerializeField] private bool allowDuplicates = true;

        private Dictionary<int, InventorySlot> _slots = new();
        private Dictionary<string, int> _itemCounts = new();
        private IEventBus _eventBus;

        #endregion

        #region Properties

        public int MaxSlots => maxSlots;
        public int UsedSlots => _slots.Count;
        public int AvailableSlots => maxSlots - UsedSlots;
        public bool IsFull => UsedSlots >= maxSlots;

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
            _eventBus = GlobalServiceProvider.Instance?.Resolve<IEventBus>();
        }

        #endregion

        #region Public Methods

        public void Initialize()
        {
            _slots.Clear();
            _itemCounts.Clear();
        }

        public bool AddItem(ItemData item, int quantity = 1)
        {
            try
            {
                if (item == null)
                {
                    Debug.LogWarning("[InventorySystem] Cannot add null item");
                    return false;
                }
                
                if (string.IsNullOrEmpty(item.ItemID))
                {
                    Debug.LogError($"[InventorySystem] Item {item.name} has null or empty ItemID");
                    return false;
                }
                
                if (quantity <= 0)
                {
                    Debug.LogWarning($"[InventorySystem] Invalid quantity {quantity} for item {item.ItemID}");
                    return false;
                }

                // Try to stack with existing items first
                if (allowDuplicates && TryStackItem(item, quantity))
                {
                    return true;
                }

                // Find empty slot
                int emptySlot = FindEmptySlot();
                if (emptySlot < 0)
                {
                    Debug.LogWarning($"[InventorySystem] No empty slots available for item {item.ItemID}");
                    return false;
                }

                var slot = new InventorySlot(item, quantity, emptySlot);

                _slots[emptySlot] = slot;
                UpdateItemCount(item.ItemID, quantity);

                OnItemAdded?.Invoke(slot);
                OnInventoryChanged?.Invoke();

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySystem] Error adding item: {ex.Message}");
                return false;
            }
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning("[InventorySystem] Cannot remove item with null or empty ID");
                return false;
            }
            
            if (quantity <= 0)
            {
                Debug.LogWarning($"[InventorySystem] Invalid quantity {quantity} for item {itemId}");
                return false;
            }
            
            var slots = GetSlotsWithItem(itemId);
            int remaining = quantity;

            foreach (var slot in slots)
            {
                if (remaining <= 0) break;

                int toRemove = Mathf.Min(remaining, slot.Quantity);
                slot.Quantity -= toRemove;
                remaining -= toRemove;

                if (slot.Quantity <= 0)
                {
                    _slots.Remove(slot.SlotIndex);
                    OnItemRemoved?.Invoke(slot);
                }
                else
                {
                    OnItemChanged?.Invoke(slot);
                }
            }

            UpdateItemCount(itemId, -quantity);
            OnInventoryChanged?.Invoke();

            return remaining == 0;
        }

        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning("[InventorySystem] Cannot get count for null or empty item ID");
                return 0;
            }
            
            return _itemCounts.GetValueOrDefault(itemId, 0);
        }

        public bool HasItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning("[InventorySystem] Cannot check for item with null or empty ID");
                return false;
            }
            
            return GetItemCount(itemId) >= quantity;
        }
        
        public bool HasItem(ItemData item, int quantity = 1)
        {
            if (item == null || string.IsNullOrEmpty(item.ItemID))
            {
                Debug.LogWarning("[InventorySystem] Cannot check for null item or item with empty ID");
                return false;
            }
            
            return GetItemCount(item.ItemID) >= quantity;
        }

        public InventorySlot GetSlot(int index)
        {
            return _slots.GetValueOrDefault(index);
        }

        public List<InventorySlot> GetAllSlots()
        {
            return new List<InventorySlot>(_slots.Values);
        }

        // Add missing methods required by InventorySaveSystem
        public bool TryAddItem(ItemData item, int quantity = 1)
        {
            return AddItem(item, quantity);
        }

        public bool TryRemoveItem(ItemData item, int quantity = 1)
        {
            return RemoveItem(item.ItemID, quantity);
        }

        public bool TryRemoveItem(string itemId, int quantity = 1)
        {
            return RemoveItem(itemId, quantity);
        }

        public List<InventorySlot> GetAllItems()
        {
            return GetAllSlots().Where(slot => slot.Item != null).ToList();
        }
        
        /// <summary>
        /// Removes an item from the inventory (interface implementation)
        /// </summary>
        public bool RemoveItem(ItemData item, int quantity = 1)
        {
            if (item == null || string.IsNullOrEmpty(item.ItemID))
            {
                Debug.LogWarning("[InventorySystem] Cannot remove null item or item with empty ID");
                return false;
            }
            
            return RemoveItem(item.ItemID, quantity);
        }
        
        /// <summary>
        /// Clears all items from the inventory (interface implementation)
        /// </summary>
        public void ClearInventory()
        {
            var allSlots = GetAllSlots().ToList();
            
            _slots.Clear();
            _itemCounts.Clear();
            
            foreach (var slot in allSlots)
            {
                OnItemRemoved?.Invoke(slot);
            }
            
            OnInventoryChanged?.Invoke();
            Debug.Log("[InventorySystem] Inventory cleared");
        }

        #endregion

        #region Private Methods

        private bool TryStackItem(ItemData item, int quantity)
        {
            if (item == null || string.IsNullOrEmpty(item.ItemID))
            {
                Debug.LogWarning("[InventorySystem] Cannot stack item with null data or ID");
                return false;
            }
            
            var existingSlots = GetSlotsWithItem(item.ItemID);
            int remaining = quantity;

            foreach (var slot in existingSlots)
            {
                if (remaining <= 0) break;

                int canAdd = Mathf.Min(remaining, maxStackSize - slot.Quantity);
                if (canAdd > 0)
                {
                    slot.Quantity += canAdd;
                    remaining -= canAdd;
                    OnItemChanged?.Invoke(slot);
                }
            }

            if (remaining > 0)
            {
                UpdateItemCount(item.ItemID, quantity - remaining);
            }

            return remaining == 0;
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (!_slots.ContainsKey(i))
                {
                    return i;
                }
            }
            return -1;
        }

        private List<InventorySlot> GetSlotsWithItem(string itemId)
        {
            var result = new List<InventorySlot>();
            
            if (string.IsNullOrEmpty(itemId))
            {
                return result;
            }
            
            foreach (var slot in _slots.Values)
            {
                if (slot.Item?.ItemID == itemId)
                {
                    result.Add(slot);
                }
            }
            return result;
        }

        private void UpdateItemCount(string itemId, int change)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning("[InventorySystem] Cannot update count for null or empty item ID");
                return;
            }
            
            if (!_itemCounts.ContainsKey(itemId))
            {
                _itemCounts[itemId] = 0;
            }

            _itemCounts[itemId] += change;

            if (_itemCounts[itemId] <= 0)
            {
                _itemCounts.Remove(itemId);
            }
        }

        #endregion
    }
}
