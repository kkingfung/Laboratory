using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Laboratory.Core.Equipment.Types;

namespace Laboratory.Core.Equipment.Components
{
    /// <summary>
    /// Equipment instance component - represents a single piece of gear
    /// </summary>
    public struct EquipmentComponent : IComponentData
    {
        public EquipmentType Type;
        public EquipmentRarity Rarity;
        public EquipmentSlot Slot;
        public int ItemID;
        public Entity OwnerCreature;
        public bool IsEquipped;
        public float Durability;
        public float MaxDurability;
        public uint CraftingSeed;
    }

    /// <summary>
    /// Equipment stats and bonuses
    /// </summary>
    public struct EquipmentStatsComponent : IComponentData
    {
        // Core stat bonuses
        public float StrengthBonus;
        public float AgilityBonus;
        public float IntelligenceBonus;
        public float VitalityBonus;
        public float SpeedBonus;
        public float EnduranceBonus;

        // Activity-specific bonuses
        public float RacingBonus;
        public float CombatBonus;
        public float PuzzleBonus;
        public float StrategyBonus;
        public float MusicBonus;
        public float AdventureBonus;
        public float PlatformingBonus;
        public float CraftingBonus;

        // Special properties
        public float ExperienceMultiplier;
        public float DurabilityLoss;
        public bool HasSetBonus;
    }

    /// <summary>
    /// Creature equipment inventory
    /// </summary>
    public struct CreatureEquipmentComponent : IComponentData
    {
        public Entity HeadGear;
        public Entity BodyArmor;
        public Entity Weapon;
        public Entity Accessory1;
        public Entity Accessory2;
        public Entity SpecialGear;
        public int EquippedItemCount;
        public float TotalStatBonus;
        public bool HasCompleteSet;
    }

    /// <summary>
    /// Equipment visual representation
    /// </summary>
    public struct EquipmentVisualComponent : IComponentData
    {
        public int VisualPrefabID;
        public float3 ColorTint;
        public float GlowIntensity;
        public bool HasCustomTexture;
        public uint VisualSeed;
    }
}