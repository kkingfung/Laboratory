using UnityEngine;
using Laboratory.Chimera.Activities;

namespace Laboratory.Chimera.Equipment
{
    /// <summary>
    /// ScriptableObject configuration for equipment items
    /// Designer-friendly equipment creation and balancing
    /// </summary>
    [CreateAssetMenu(fileName = "EquipmentItem", menuName = "Chimera/Equipment/Equipment Item")]
    public class EquipmentConfig : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Unique item ID (must be unique across all equipment)")]
        public int itemId;

        [Tooltip("Display name shown to players")]
        public string itemName = "New Equipment";

        [TextArea(2, 4)]
        [Tooltip("Item description and effects")]
        public string description = "";

        [Tooltip("Equipment icon sprite")]
        public Sprite icon;

        [Header("Equipment Properties")]
        [Tooltip("Equipment category")]
        public EquipmentCategory category = EquipmentCategory.Accessory;

        [Tooltip("Which slot this equipment occupies")]
        public EquipmentSlot slot = EquipmentSlot.Accessory1;

        [Tooltip("Item rarity tier")]
        public EquipmentRarity rarity = EquipmentRarity.Common;

        [Header("Stat Bonuses")]
        [Tooltip("Which stats this equipment boosts")]
        public StatBonusType statBonusType = StatBonusType.None;

        [Tooltip("Bonus percentage (0.0 to 1.0 = 0% to 100%)")]
        [Range(0f, 1f)]
        public float statBonusValue = 0.1f;

        [Header("Activity Bonuses")]
        [Tooltip("Activity this equipment helps with (None = all activities)")]
        public ActivityType activityBonus = ActivityType.None;

        [Tooltip("Activity performance bonus (0.0 to 0.5 = 0% to 50%)")]
        [Range(0f, 0.5f)]
        public float activityBonusValue = 0.0f;

        [Header("Requirements")]
        [Tooltip("Minimum monster level to equip")]
        public int requiredLevel = 1;

        [Tooltip("Minimum activity level to equip (0 = no requirement)")]
        public int requiredActivityLevel = 0;

        [Header("Durability")]
        [Tooltip("Maximum durability (0 = infinite)")]
        public int maxDurability = 0;

        [Tooltip("Durability loss per use")]
        public int durabilityLossPerUse = 1;

        [Header("Economy")]
        [Tooltip("Purchase price in coins")]
        public int purchasePrice = 100;

        [Tooltip("Sell price in coins")]
        public int sellPrice = 50;

        [Header("Special Effects")]
        [Tooltip("Additional effects this equipment provides")]
        public EquipmentEffectConfig[] specialEffects;

        [Header("Set Bonus")]
        [Tooltip("Equipment set this belongs to (optional)")]
        public string equipmentSetName = "";

        [Tooltip("Number of pieces needed for set bonus")]
        public int setPiecesRequired = 0;

        [Tooltip("Set bonus description")]
        [TextArea(2, 3)]
        public string setBonusDescription = "";

        /// <summary>
        /// Converts config to equipment item struct
        /// </summary>
        public EquipmentItem ToEquipmentItem()
        {
            return new EquipmentItem
            {
                itemId = itemId,
                itemName = itemName,
                category = category,
                slot = slot,
                rarity = rarity,
                bonusType = statBonusType,
                bonusValue = statBonusValue,
                activityBonus = activityBonus,
                activityBonusValue = activityBonusValue,
                requiredLevel = requiredLevel,
                requiredActivityLevel = requiredActivityLevel,
                maxDurability = maxDurability,
                currentDurability = maxDurability,
                purchasePrice = purchasePrice,
                sellPrice = sellPrice,
                isEquipped = false
            };
        }

        /// <summary>
        /// Gets rarity color for UI display
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                EquipmentRarity.Common => new Color(0.7f, 0.7f, 0.7f), // Gray
                EquipmentRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f), // Green
                EquipmentRarity.Rare => new Color(0.3f, 0.5f, 1.0f), // Blue
                EquipmentRarity.Epic => new Color(0.8f, 0.3f, 1.0f), // Purple
                EquipmentRarity.Legendary => new Color(1.0f, 0.6f, 0.0f), // Orange
                _ => Color.white
            };
        }

        /// <summary>
        /// Gets formatted stat bonus text for UI
        /// </summary>
        public string GetBonusText()
        {
            string text = "";

            if (statBonusType != StatBonusType.None)
            {
                text += $"+{statBonusValue * 100f:F0}% {statBonusType}\n";
            }

            if (activityBonus != ActivityType.None && activityBonusValue > 0)
            {
                text += $"+{activityBonusValue * 100f:F0}% {activityBonus} Performance\n";
            }

            return text.TrimEnd('\n');
        }

        private void OnValidate()
        {
            // Ensure valid values
            itemId = Mathf.Max(0, itemId);
            requiredLevel = Mathf.Max(1, requiredLevel);
            requiredActivityLevel = Mathf.Max(0, requiredActivityLevel);
            maxDurability = Mathf.Max(0, maxDurability);
            purchasePrice = Mathf.Max(0, purchasePrice);
            sellPrice = Mathf.Max(0, sellPrice);

            // Sell price should be less than purchase price
            if (sellPrice > purchasePrice)
            {
                sellPrice = Mathf.RoundToInt(purchasePrice * 0.5f);
            }
        }
    }

    /// <summary>
    /// Special effect configuration for equipment
    /// </summary>
    [System.Serializable]
    public class EquipmentEffectConfig
    {
        public EquipmentEffectType effectType = EquipmentEffectType.StatBonus;

        [Tooltip("Target stat for stat bonuses")]
        public StatBonusType targetStat = StatBonusType.None;

        [Tooltip("Target activity for activity bonuses")]
        public ActivityType targetActivity = ActivityType.None;

        [Tooltip("Effect value (context-dependent)")]
        [Range(0f, 2f)]
        public float effectValue = 0.1f;

        [Tooltip("Effect duration in seconds (0 = permanent)")]
        public float duration = 0f;

        [TextArea(2, 3)]
        [Tooltip("Effect description for UI")]
        public string description = "";
    }
}
