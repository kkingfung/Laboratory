using System;
using System.Collections.Generic;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// Core interface for inventory system implementations.
    /// Provides standard inventory operations for item management.
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
}
