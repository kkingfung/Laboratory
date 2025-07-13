// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    [System.Serializable]
    public class InventoryItem
    {
        public string itemName;
        public int quantity;
        public Sprite icon;

        public InventoryItem(string name, int qty, Sprite itemIcon)
        {
            itemName = name;
            quantity = qty;
            icon = itemIcon;
        }
    }

    [Header("Inventory Settings")]
    public int maxSlots = 20;

    private List<InventoryItem> items = new List<InventoryItem>();

    public delegate void OnInventoryChanged();
    public event OnInventoryChanged InventoryChanged;

    public bool AddItem(string itemName, Sprite icon, int quantity = 1)
    {
        // Check if the item already exists in the inventory
        InventoryItem existingItem = items.Find(item => item.itemName == itemName);

        if (existingItem != null)
        {
            existingItem.quantity += quantity;
        }
        else
        {
            if (items.Count >= maxSlots)
            {
                Debug.LogWarning("Inventory is full!");
                return false;
            }

            InventoryItem newItem = new InventoryItem(itemName, quantity, icon);
            items.Add(newItem);
        }

        InventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(string itemName, int quantity = 1)
    {
        InventoryItem existingItem = items.Find(item => item.itemName == itemName);

        if (existingItem != null)
        {
            existingItem.quantity -= quantity;

            if (existingItem.quantity <= 0)
            {
                items.Remove(existingItem);
            }

            InventoryChanged?.Invoke();
            return true;
        }

        Debug.LogWarning("Item not found in inventory!");
        return false;
    }

    public InventoryItem GetItem(string itemName)
    {
        return items.Find(item => item.itemName == itemName);
    }

    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(items);
    }

    public void ClearInventory()
    {
        items.Clear();
        InventoryChanged?.Invoke();
    }

    public int GetItemCount()
    {
        return items.Count;
    }
}