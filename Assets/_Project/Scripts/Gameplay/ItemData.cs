using UnityEngine;

namespace Laboratory.Gameplay.Inventory
{
    /// <summary>
    /// ScriptableObject representing item data for inventory.
    /// </summary>
    [CreateAssetMenu(menuName = "Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        #region Fields

        [Header("Basic Info")]
        [SerializeField] private string itemName;
        [TextArea, SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private int rarity; // 0=Common, 1=Uncommon, etc.
        [SerializeField] private int value;  // Gold or currency value

        [Header("Stats")]
        [SerializeField] private StatEntry[] stats;

        #endregion

        #region Properties

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
        /// Item stats.
        /// </summary>
        public StatEntry[] Stats => stats;

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
}
