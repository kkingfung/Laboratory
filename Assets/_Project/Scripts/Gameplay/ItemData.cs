using UnityEngine;

namespace Game.Inventory
{
    [CreateAssetMenu(menuName = "Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string ItemName;
        [TextArea] public string Description;
        public Sprite Icon;
        public int Rarity; // 0=Common, 1=Uncommon, etc.
        public int Value;  // Gold or currency value

        [SerializeField]
        private StatEntry[] stats;

        [System.Serializable]
        public struct StatEntry
        {
            public string StatName;    // e.g. "Damage", "Armor"
            public string StatValue;   // e.g. "15-20", "10"
            public Sprite StatIcon;    // optional icon per stat
        }

        public StatEntry[] Stats => stats;
    }
}
