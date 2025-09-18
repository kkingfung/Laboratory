using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// Quick fix to add missing HasItem method to inventory systems
    /// This should be added to any inventory implementation missing this method
    /// </summary>
    public static class InventorySystemExtensions
    {
        /// <summary>
        /// Extension method to add HasItem(ItemData, int) functionality
        /// to inventory systems that only have HasItem(string, int)
        /// </summary>
        public static bool HasItemExtension(this IInventorySystem inventory, ItemData item, int quantity = 1)
        {
            return item != null && !string.IsNullOrEmpty(item.Id) && inventory.HasItem(item.Id, quantity);
        }
    }
}
