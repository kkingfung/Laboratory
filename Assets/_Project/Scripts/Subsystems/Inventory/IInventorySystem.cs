using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// Unified inventory system interface that combines functionality from both
    /// previous implementations. Provides comprehensive item management with
    /// proper event handling and validation.
    /// </summary>
    public interface IInventorySystem
    {
        /// <summary>
        /// Maximum number of slots in the inventory
        /// </summary>
        int MaxSlots { get; }


        /// <summary>
        /// Number of currently used slots
        /// </summary>
        int UsedSlots { get; }

        /// <summary>
        /// Number of available empty slots
        /// </summary>
        int AvailableSlots { get; }

        /// <summary>
        /// Whether the inventory is full
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// Initializes the inventory system
        /// </summary>
        void Initialize();

        /// <summary>
        /// Attempts to add an item to the inventory
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="quantity">Quantity to add</param>
        /// <returns>True if the item was successfully added</returns>
        bool TryAddItem(ItemData item, int quantity = 1);

        /// <summary>
        /// Attempts to remove an item from the inventory
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <param name="quantity">Quantity to remove</param>
        /// <returns>True if the item was successfully removed</returns>
        bool TryRemoveItem(ItemData item, int quantity = 1);

        /// <summary>
        /// Attempts to remove an item from the inventory by ID
        /// </summary>
        /// <param name="itemId">ID of the item to remove</param>
        /// <param name="quantity">Quantity to remove</param>
        /// <returns>True if the item was successfully removed</returns>
        bool TryRemoveItem(string itemId, int quantity = 1);

        /// <summary>
        /// Gets the quantity of a specific item in the inventory
        /// </summary>
        /// <param name="itemId">ID of the item</param>
        /// <returns>Quantity of the item</returns>
        int GetItemCount(string itemId);

        /// <summary>
        /// Checks if the inventory contains the specified quantity of an item
        /// </summary>
        /// <param name="itemId">ID of the item</param>
        /// <param name="quantity">Required quantity</param>
        /// <returns>True if the inventory contains enough of the item</returns>
        bool HasItem(string itemId, int quantity = 1);

        /// <summary>
        /// Checks if the inventory contains the specified quantity of an item
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <param name="quantity">Required quantity</param>
        /// <returns>True if the inventory contains enough of the item</returns>
        bool HasItem(ItemData item, int quantity = 1);

        /// <summary>
        /// Gets a specific inventory slot by index
        /// </summary>
        /// <param name="index">Slot index</param>
        /// <returns>The inventory slot or null if empty</returns>
        InventorySlot GetSlot(int index);

        /// <summary>
        /// Gets all inventory slots
        /// </summary>
        /// <returns>List of all inventory slots</returns>
        List<InventorySlot> GetAllSlots();

        /// <summary>
        /// Gets all items in the inventory (non-empty slots only)
        /// </summary>
        /// <returns>List of items in the inventory</returns>
        List<InventorySlot> GetAllItems();
        
        /// <summary>
        /// Adds an item to the inventory (simplified version)
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="quantity">Quantity to add</param>
        /// <returns>True if successful</returns>
        bool AddItem(ItemData item, int quantity = 1);
        
        /// <summary>
        /// Removes an item from the inventory (simplified version)
        /// </summary>
        /// <param name="item">Item to remove</param>
        /// <param name="quantity">Quantity to remove</param>
        /// <returns>True if successful</returns>
        bool RemoveItem(ItemData item, int quantity = 1);
        
        /// <summary>
        /// Clears all items from the inventory
        /// </summary>
        void ClearInventory();

        /// <summary>
        /// Event fired when an item is added to the inventory
        /// </summary>
        event Action<InventorySlot> OnItemAdded;

        /// <summary>
        /// Event fired when an item is removed from the inventory
        /// </summary>
        event Action<InventorySlot> OnItemRemoved;

        /// <summary>
        /// Event fired when an item in the inventory changes
        /// </summary>
        event Action<InventorySlot> OnItemChanged;

        /// <summary>
        /// Event fired when the inventory state changes
        /// </summary>
        event Action OnInventoryChanged;
    }

    /// <summary>
    /// Unified item data structure that combines features from both previous implementations
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(fileName = "New Item", menuName = "Laboratory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string id;
        [SerializeField] private string itemName;
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemType type;
        
        [Header("Properties")]
        [SerializeField] private int maxStackSize = 1;
        [SerializeField] private bool isConsumable;
        [SerializeField] private float value = 0f;
        [SerializeField] private int rarity = 0;
        [SerializeField] private bool isStackable = true;
        [SerializeField] private bool isUsable = false;
        [SerializeField] private ItemStats stats;

        // Primary accessors
        public string Id => id;
        public string Name => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemType Type => type;
        public int MaxStackSize => maxStackSize;
        public bool IsConsumable => isConsumable;
        public bool IsStackable => isStackable;
        public bool IsUsable => isUsable;
        public float Value => value;
        public int Rarity => rarity;
        public ItemStats Stats => stats;
        
        // Compatibility aliases
        public string ItemID => id;
        public string ItemName => itemName;
        
        // Validation
        public bool IsValid => !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(itemName);

        public ItemData()
        {
            // Default constructor for ScriptableObject
        }

        public ItemData(string itemId, string itemName)
        {
            this.id = itemId;
            this.itemName = itemName;
        }
    }

    /// <summary>
    /// Represents the different types of items
    /// </summary>
    public enum ItemType
    {
        Consumable,
        Equipment,
        Resource,
        Quest,
        Misc
    }

    /// <summary>
    /// Item statistics structure
    /// </summary>
    [System.Serializable]
    public struct ItemStats : System.Collections.Generic.IEnumerable<StatEntry>
    {
        public int damage;
        public int defense;
        public int healing;
        // Add more stats as needed
        // Force recompilation

        public System.Collections.Generic.IEnumerator<StatEntry> GetEnumerator()
        {
            if (damage != 0) yield return new StatEntry("Damage", damage.ToString(), null);
            if (defense != 0) yield return new StatEntry("Defense", defense.ToString(), null);
            if (healing != 0) yield return new StatEntry("Healing", healing.ToString(), null);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Represents a single stat entry for display
    /// </summary>
    [System.Serializable]
    public struct StatEntry
    {
        public string StatName { get; }
        public string StatValue { get; }
        public Sprite StatIcon { get; }

        public StatEntry(string name, string value, Sprite icon)
        {
            StatName = name;
            StatValue = value;
            StatIcon = icon;
        }
    }

    /// <summary>
    /// Represents a slot in the inventory
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        [SerializeField] private ItemData item;
        [SerializeField] private int quantity;
        [SerializeField] private int slotIndex;

        public ItemData Item => item;
        public int Quantity { get => quantity; set => quantity = value; }
        public int SlotIndex => slotIndex;
        public bool IsEmpty => item == null || quantity <= 0;
        public bool HasItem => !IsEmpty;

        public InventorySlot(int index)
        {
            slotIndex = index;
            item = null;
            quantity = 0;
        }

        public InventorySlot(ItemData itemData, int amount, int index)
        {
            item = itemData;
            quantity = amount;
            slotIndex = index;
        }

        public void SetItem(ItemData itemData, int amount)
        {
            item = itemData;
            quantity = amount;
        }

        public void AddQuantity(int amount)
        {
            quantity += amount;
        }

        public bool RemoveQuantity(int amount)
        {
            if (quantity >= amount)
            {
                quantity -= amount;
                if (quantity <= 0)
                {
                    Clear();
                }
                return true;
            }
            return false;
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }

        public bool CanStackWith(ItemData otherItem)
        {
            return HasItem && item == otherItem && item.IsStackable && quantity < item.MaxStackSize;
        }

        public int GetAvailableStackSpace()
        {
            if (!HasItem || !item.IsStackable)
                return 0;
            return item.MaxStackSize - quantity;
        }
    }
}