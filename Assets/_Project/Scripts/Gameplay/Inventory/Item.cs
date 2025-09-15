using System;
using UnityEngine;

namespace Laboratory.Gameplay.Items
{
    [System.Serializable]
    public class GameItem
    {
        [SerializeField] private string itemID;
        [SerializeField] private string itemName;
        [SerializeField] private string description;
        [SerializeField] private GameItemType itemType;
        [SerializeField] private GameItemRarity rarity;
        [SerializeField] private int maxStackSize = 1;
        [SerializeField] private bool isConsumable;
        [SerializeField] private float weight;
        [SerializeField] private int value;
        [SerializeField] private Sprite icon;
        
        public string ItemID => itemID;
        public string ItemName => itemName;
        public string Description => description;
        public GameItemType ItemType => itemType;
        public GameItemRarity Rarity => rarity;
        public int MaxStackSize => maxStackSize;
        public bool IsConsumable => isConsumable;
        public float Weight => weight;
        public int Value => value;
        public Sprite Icon => icon;
        
        public GameItem()
        {
            itemID = Guid.NewGuid().ToString();
            itemName = "Unknown Item";
            description = "An unknown item.";
            itemType = GameItemType.Miscellaneous;
            rarity = GameItemRarity.Common;
            maxStackSize = 1;
            isConsumable = false;
            weight = 1.0f;
            value = 0;
        }

        public GameItem(string id, string name, string desc = "", GameItemType type = GameItemType.Miscellaneous)
        {
            itemID = id;
            itemName = name;
            description = desc;
            itemType = type;
            rarity = GameItemRarity.Common;
            maxStackSize = 1;
            isConsumable = false;
            weight = 1.0f;
            value = 0;
        }
        
        public virtual bool Use()
        {
            Debug.Log($"Using item: {itemName}");
            return true;
        }

        public virtual bool Equip()
        {
            Debug.Log($"Equipping item: {itemName}");
            return true;
        }

        public virtual bool Unequip()
        {
            Debug.Log($"Unequipping item: {itemName}");
            return true;
        }

        public virtual string GetDisplayName()
        {
            return itemName;
        }

        public virtual string GetTooltip()
        {
            return $"{itemName}\n{description}\nType: {itemType}\nRarity: {rarity}\nValue: {value}";
        }
        
        public override bool Equals(object obj)
        {
            if (obj is GameItem other)
            {
                return itemID == other.itemID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return itemID?.GetHashCode() ?? 0;
        }
    }

    public enum GameItemType
    {
        Weapon,
        Armor,
        Consumable,
        Tool,
        Material,
        Quest,
        Key,
        Miscellaneous
    }

    public enum GameItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
