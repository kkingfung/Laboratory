using System;
using Unity.Collections;
using Laboratory.Chimera.Activities;
using Laboratory.Core.Enums;

namespace Laboratory.Chimera.Equipment
{
    /// <summary>
    /// Equipment categories for different purposes
    /// </summary>
    public enum EquipmentCategory : byte
    {
        Weapon = 0,
        Armor = 1,
        Accessory = 2,
        Tool = 3,
        Consumable = 4
    }

    /// <summary>
    /// Stat bonus types that equipment can provide
    /// </summary>
    [Flags]
    public enum StatBonusType : byte
    {
        None = 0,
        Strength = 1 << 0,
        Agility = 1 << 1,
        Intelligence = 1 << 2,
        Vitality = 1 << 3,
        Social = 1 << 4,
        Adaptability = 1 << 5,
        AllStats = Strength | Agility | Intelligence | Vitality | Social | Adaptability
    }

    /// <summary>
    /// Equipment effect types
    /// </summary>
    public enum EquipmentEffectType : byte
    {
        StatBonus = 0,              // Increases base stats
        ActivityBonus = 1,          // Boosts specific activity performance
        ExperienceMultiplier = 2,   // Increases XP gain
        CurrencyMultiplier = 3,     // Increases coin/token gain
        SpecialAbility = 4          // Grants unique ability
    }

    /// <summary>
    /// Equipment data structure for inventory storage
    /// </summary>
    [Serializable]
    public struct EquipmentItem
    {
        public int itemId;
        public FixedString64Bytes itemName;
        public EquipmentCategory category;
        public EquipmentSlot slot;
        public EquipmentRarity rarity;

        // Stat modifications
        public StatBonusType bonusType;
        public float bonusValue; // 0.0 to 1.0 (represents percentage)

        // Activity-specific bonus
        public ActivityType activityBonus;
        public float activityBonusValue;

        // Level requirements
        public int requiredLevel;
        public int requiredActivityLevel; // For activity-specific equipment

        // Durability (for consumables)
        public int maxDurability;
        public int currentDurability;

        // Market value
        public int purchasePrice;
        public int sellPrice;

        public bool isEquipped;
    }

    /// <summary>
    /// Equipment effect data
    /// </summary>
    [Serializable]
    public struct EquipmentEffect
    {
        public EquipmentEffectType effectType;
        public StatBonusType targetStat;
        public ActivityType targetActivity;
        public float effectValue;
        public float duration; // For temporary effects (0 = permanent)
    }

    /// <summary>
    /// Equipment set bonus (when wearing multiple pieces)
    /// </summary>
    [Serializable]
    public struct EquipmentSetBonus
    {
        public FixedString64Bytes setName;
        public int requiredPieces;
        public StatBonusType bonusType;
        public float bonusValue;
        public FixedString128Bytes description;
    }

    /// <summary>
    /// Crafting recipe for equipment
    /// </summary>
    [Serializable]
    public struct CraftingRecipe
    {
        public int resultItemId;
        public int resultQuantity;

        // Material requirements (simplified - up to 5 materials)
        public FixedList64Bytes<CraftingMaterial> requiredMaterials;

        public int craftingCost; // Coin cost
        public int requiredCraftingLevel;
        public float craftingTime; // In seconds
    }

    /// <summary>
    /// Crafting material requirement
    /// </summary>
    [Serializable]
    public struct CraftingMaterial
    {
        public int materialId;
        public int quantity;
    }

    /// <summary>
    /// Equipment upgrade data
    /// </summary>
    [Serializable]
    public struct EquipmentUpgrade
    {
        public int currentLevel;
        public int maxLevel;
        public float bonusMultiplier; // Increases with level
        public int upgradeCost;
        public int requiredMaterialId;
        public int requiredMaterialCount;
    }

    /// <summary>
    /// Equipment drop chance data (for activities/enemies)
    /// </summary>
    [Serializable]
    public struct EquipmentDrop
    {
        public int itemId;
        public float dropChance; // 0.0 to 1.0
        public ActivityType sourceActivity;
        public ActivityDifficulty minimumDifficulty;
        public EquipmentRarity guaranteedRarity; // Minimum rarity
    }

    /// <summary>
    /// Equipment loadout (preset equipment configurations)
    /// </summary>
    [Serializable]
    public struct EquipmentLoadout
    {
        public int loadoutId;
        public FixedString64Bytes loadoutName;
        public ActivityType optimizedFor;

        // Equipment slots
        public int headSlotItemId;
        public int bodySlotItemId;
        public int handsSlotItemId;
        public int feetSlotItemId;
        public int accessory1SlotItemId;
        public int accessory2SlotItemId;
        public int toolSlotItemId;
    }
}
