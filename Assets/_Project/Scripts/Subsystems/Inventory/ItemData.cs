using UnityEngine;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// ScriptableObject representing item data for inventory.
    /// </summary>
    [CreateAssetMenu(menuName = "Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        #region Fields

        [Header("Basic Info")]
        [SerializeField] private string itemID;
        [SerializeField] private string itemName;
        [TextArea, SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private int rarity; // 0=Common, 1=Uncommon, etc.
        [SerializeField] private int value;  // Gold or currency value

        [Header("Properties")]
        [SerializeField] private bool isStackable = true;
        [SerializeField] private bool isUsable = false;
        [SerializeField] private bool isConsumable = false;
        [SerializeField] private int maxStackSize = 99;
        [SerializeField] private float cooldownTime = 0f;
        [SerializeField] private ItemType itemType = ItemType.Miscellaneous;

        [Header("Stats")]
        [SerializeField] private StatEntry[] stats;

        #endregion

        #region Properties

        /// <summary>
        /// Unique item ID.
        /// </summary>
        public string ItemID => itemID;

        /// <summary>
        /// Item name.
        /// </summary>
        public string ItemName => itemName;

        /// <summary>
        /// Item description.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Item icon.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Item rarity.
        /// </summary>
        public int Rarity => rarity;

        /// <summary>
        /// Item value.
        /// </summary>
        public int Value => value;

        /// <summary>
        /// Whether this item can be stacked.
        /// </summary>
        public bool IsStackable => isStackable;

        /// <summary>
        /// Whether this item can be used/consumed.
        /// </summary>
        public bool IsUsable => isUsable;

        /// <summary>
        /// Whether this item is consumable (gets destroyed on use).
        /// </summary>
        public bool IsConsumable => isConsumable;

        /// <summary>
        /// Maximum stack size for this item.
        /// </summary>
        public int MaxStackSize => maxStackSize;

        /// <summary>
        /// Cooldown time after using this item.
        /// </summary>
        public float CooldownTime => cooldownTime;

        /// <summary>
        /// Type category of this item.
        /// </summary>
        public ItemType ItemType => itemType;

        /// <summary>
        /// Item stats.
        /// </summary>
        public StatEntry[] Stats => stats;

        #endregion

        #region Methods

        /// <summary>
        /// Uses the item. Override in subclasses for custom behavior.
        /// </summary>
        /// <returns>True if the item was successfully used</returns>
        public virtual bool Use()
        {
            return IsUsable;
        }

        #endregion

        #region Inner Classes, Enums

        [System.Serializable]
        public struct StatEntry
        {
            public string StatName;    // e.g. "Damage", "Armor"
            public string StatValue;   // e.g. "15-20", "10"
            public Sprite StatIcon;    // optional icon per stat
        }

        #endregion
    }

    /// <summary>
    /// Enumeration of different item types for categorization.
    /// </summary>
    public enum ItemType
    {
        Miscellaneous = 0,
        Weapon = 1,
        Armor = 2,
        Consumable = 3,
        Material = 4,
        Quest = 5,
        Tool = 6,
        Accessory = 7
    }
}
