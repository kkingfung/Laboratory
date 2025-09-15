using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Gameplay.Inventory
{
    /// <summary>
    /// Basic inventory system for managing items
    /// </summary>
    public interface IInventorySystem
    {
        bool AddItem(Item item, int quantity = 1);
        bool RemoveItem(Item item, int quantity = 1);
        bool HasItem(Item item, int quantity = 1);
        int GetItemQuantity(Item item);
        List<InventorySlot> GetAllItems();
        void ClearInventory();
    }

    /// <summary>
    /// Represents an item in the game
    /// </summary>
    [System.Serializable]
    public class Item
    {
        [SerializeField] private string id;
        [SerializeField] private string name;
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemType type;
        [SerializeField] private int maxStackSize = 1;
        [SerializeField] private bool isConsumable;

        public string Id => id;
        public string Name => name;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemType Type => type;
        public int MaxStackSize => maxStackSize;
        public bool IsConsumable => isConsumable;

        public Item(string itemId, string itemName, ItemType itemType)
        {
            id = itemId;
            name = itemName;
            type = itemType;
        }
    }

    /// <summary>
    /// Represents a slot in an inventory
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        [SerializeField] private Item item;
        [SerializeField] private int quantity;

        public Item Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;

        public InventorySlot(Item slotItem, int slotQuantity)
        {
            item = slotItem;
            quantity = slotQuantity;
        }

        public void SetItem(Item newItem, int newQuantity)
        {
            item = newItem;
            quantity = newQuantity;
        }

        public void Clear()
        {
            item = null;
            quantity = 0;
        }
    }

    /// <summary>
    /// Types of items in the game
    /// </summary>
    public enum ItemType
    {
        Consumable,
        Weapon,
        Armor,
        Tool,
        Material,
        QuestItem,
        Currency
    }
}
