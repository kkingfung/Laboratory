using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Activities;

namespace Laboratory.Chimera.Equipment
{
    /// <summary>
    /// Component indicating entity has equipment inventory
    /// </summary>
    public struct EquipmentInventoryTag : IComponentData
    {
    }

    /// <summary>
    /// Buffer storing all equipment items owned by entity
    /// </summary>
    public struct EquipmentInventoryElement : IBufferElementData
    {
        public EquipmentItem item;
    }

    /// <summary>
    /// Component tracking currently equipped items
    /// </summary>
    public struct EquippedItemsComponent : IComponentData
    {
        public int headSlotItemId;
        public int bodySlotItemId;
        public int handsSlotItemId;
        public int feetSlotItemId;
        public int accessory1SlotItemId;
        public int accessory2SlotItemId;
        public int toolSlotItemId;

        // Cached total bonuses (recalculated when equipment changes)
        public float totalStatBonus;
        public float totalActivityBonus;
    }

    /// <summary>
    /// Component storing active equipment effects
    /// </summary>
    public struct ActiveEquipmentEffect : IBufferElementData
    {
        public EquipmentEffect effect;
        public float startTime;
        public float endTime; // 0 = permanent
        public bool isActive;
    }

    /// <summary>
    /// Request to equip an item
    /// </summary>
    public struct EquipItemRequest : IComponentData
    {
        public Entity targetEntity;
        public int itemId;
        public EquipmentSlot targetSlot;
        public float requestTime;
    }

    /// <summary>
    /// Request to unequip an item
    /// </summary>
    public struct UnequipItemRequest : IComponentData
    {
        public Entity targetEntity;
        public EquipmentSlot targetSlot;
        public float requestTime;
    }

    /// <summary>
    /// Request to add item to inventory
    /// </summary>
    public struct AddItemRequest : IComponentData
    {
        public Entity targetEntity;
        public int itemId;
        public int quantity;
        public float requestTime;
    }

    /// <summary>
    /// Request to remove item from inventory
    /// </summary>
    public struct RemoveItemRequest : IComponentData
    {
        public Entity targetEntity;
        public int itemId;
        public int quantity;
        public float requestTime;
    }

    /// <summary>
    /// Equipment stat bonus cache component
    /// Stores calculated bonuses for quick access during activities
    /// </summary>
    public struct EquipmentBonusCache : IComponentData
    {
        // Stat bonuses (0.0 to 1.0 = 0% to 100%)
        public float strengthBonus;
        public float agilityBonus;
        public float intelligenceBonus;
        public float vitalityBonus;
        public float socialBonus;
        public float adaptabilityBonus;

        // Activity-specific bonuses
        public float racingBonus;
        public float combatBonus;
        public float puzzleBonus;
        public float strategyBonus;
        public float rhythmBonus;
        public float adventureBonus;
        public float platformingBonus;
        public float craftingBonus;

        // Multiplier bonuses
        public float experienceMultiplier; // 1.0 = normal, 1.5 = +50%
        public float currencyMultiplier;

        public float lastUpdateTime;
    }

    /// <summary>
    /// Equipment durability tracking
    /// </summary>
    public struct EquipmentDurabilityComponent : IComponentData
    {
        public int headDurability;
        public int bodyDurability;
        public int handsDurability;
        public int feetDurability;
        public int accessory1Durability;
        public int accessory2Durability;
        public int toolDurability;
    }

    /// <summary>
    /// Equipment set bonus tracking
    /// </summary>
    public struct EquipmentSetBonusElement : IBufferElementData
    {
        public FixedString64Bytes setName;
        public int equippedPieces;
        public int requiredPieces;
        public bool isBonusActive;
        public float bonusValue;
    }

    /// <summary>
    /// Crafting queue element
    /// </summary>
    public struct CraftingQueueElement : IBufferElementData
    {
        public int resultItemId;
        public int quantity;
        public float startTime;
        public float completionTime;
        public bool isComplete;
    }

    /// <summary>
    /// Component indicating entity can craft equipment
    /// </summary>
    public struct CraftingCapabilityComponent : IComponentData
    {
        public int craftingLevel;
        public int craftingExperience;
        public int unlockedRecipesCount;
    }

    /// <summary>
    /// Singleton component holding equipment database
    /// </summary>
    public struct EquipmentSystemData : IComponentData
    {
        public bool isInitialized;
        public int totalEquipmentTypes;
        public int totalItemsInCirculation;
        public float currentTime;
    }
}
