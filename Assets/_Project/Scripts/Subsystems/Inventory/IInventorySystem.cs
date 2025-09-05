using System.Collections.Generic;
using Laboratory.Gameplay.Inventory;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// Interface for inventory system implementations
    /// </summary>
    public interface IInventorySystem
    {
        /// <summary>
        /// Maximum number of slots available
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
        /// Add an item to the inventory
        /// </summary>
        bool AddItem(ItemData item, int quantity = 1);
        
        /// <summary>
        /// Remove an item from the inventory
        /// </summary>
        bool RemoveItem(string itemId, int quantity = 1);
        
        /// <summary>
        /// Get the count of a specific item
        /// </summary>
        int GetItemCount(string itemId);
        
        /// <summary>
        /// Check if inventory has a specific item
        /// </summary>
        bool HasItem(string itemId, int quantity = 1);
        
        /// <summary>
        /// Get a specific inventory slot
        /// </summary>
        InventorySlot GetSlot(int index);
        
        /// <summary>
        /// Get all inventory slots
        /// </summary>
        List<InventorySlot> GetAllSlots();
        
        /// <summary>
        /// Try to add an item to the inventory
        /// </summary>
        bool TryAddItem(ItemData item, int quantity = 1);
        
        /// <summary>
        /// Try to remove an item from the inventory
        /// </summary>
        bool TryRemoveItem(ItemData item, int quantity = 1);
        
        /// <summary>
        /// Try to remove an item from the inventory by ID
        /// </summary>
        bool TryRemoveItem(string itemId, int quantity = 1);
        
        /// <summary>
        /// Get all items (non-empty slots)
        /// </summary>
        List<InventorySlot> GetAllItems();
    }
}
