using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.Activities;

namespace Laboratory.Core.Equipment
{
    /// <summary>
    /// Equipment System - Gear that enhances monster performance in activities
    /// FEATURES: Activity-specific gear, stat bonuses, visual customization, crafting integration
    /// PERFORMANCE: Optimized for 1000+ equipped creatures with burst compilation
    /// INTEGRATION: Works with Activity Centers to provide performance bonuses
    /// </summary>

    #region Equipment Components

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
        public uint CraftingSeed; // For procedural generation
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

    #endregion

    #region Enums

    public enum EquipmentType : byte
    {
        // Racing Gear
        SpeedBoots,
        AerodynamicHelmet,
        LightweightHarness,
        TurboBooster,

        // Combat Gear
        WeaponMelee,
        WeaponRanged,
        ArmorHeavy,
        ArmorLight,
        Shield,
        CombatStimulant,

        // Puzzle Gear
        ThinkingCap,
        ConcentrationAid,
        MemoryEnhancer,
        LogicProcessor,

        // Strategy Gear
        TacticalVisor,
        CommandBadge,
        StrategicAnalyzer,
        LeadershipSymbol,

        // Music Gear
        InstrumentWind,
        InstrumentString,
        InstrumentPercussion,
        RhythmAccessory,

        // Adventure Gear
        ExplorationPack,
        SurvivalGear,
        ClimbingEquipment,
        AdventureBoots,

        // Platforming Gear
        JumpEnhancer,
        GripGloves,
        BalanceAid,
        MobilityBooster,

        // Crafting Gear
        CraftingTools,
        PrecisionInstruments,
        QualityEnhancer,
        EfficiencyBooster,

        // Universal Gear
        EnergyCore,
        HealthBooster,
        ExperienceMultiplier,
        StatusProtection
    }

    public enum EquipmentRarity : byte
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythic = 5
    }

    public enum EquipmentSlot : byte
    {
        Head,
        Body,
        Weapon,
        Accessory,
        Special
    }

    #endregion

    #region Equipment Systems

    /// <summary>
    /// Core equipment management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EquipmentSystem : SystemBase
    {
        private EntityQuery equippedCreatureQuery;
        private EntityQuery equipmentQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            equippedCreatureQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<CreatureEquipmentComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>(),
                ComponentType.ReadOnly<ActivityParticipantComponent>()
            });

            equipmentQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<EquipmentComponent>(),
                ComponentType.ReadOnly<EquipmentStatsComponent>()
            });

            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Update equipment effects on creatures
            var equipmentEffectsJob = new EquipmentEffectsJob
            {
                DeltaTime = deltaTime
            };
            Dependency = equipmentEffectsJob.ScheduleParallel(equippedCreatureQuery, Dependency);

            // Update equipment durability
            var durabilityJob = new EquipmentDurabilityJob
            {
                DeltaTime = deltaTime
            };
            Dependency = durabilityJob.ScheduleParallel(equipmentQuery, Dependency);
        }
    }


    [BurstCompile]
    public partial struct EquipmentEffectsJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref CreatureEquipmentComponent equipment,
            RefRO<GeneticDataComponent> genetics,
            RefRO<ActivityParticipantComponent> activity)
        {
            // Calculate total equipment bonuses
            float totalStatBonus = 0f;
            int equippedCount = 0;

            // Count equipped items and calculate bonuses
            if (equipment.HeadGear != Entity.Null) equippedCount++;
            if (equipment.BodyArmor != Entity.Null) equippedCount++;
            if (equipment.Weapon != Entity.Null) equippedCount++;
            if (equipment.Accessory1 != Entity.Null) equippedCount++;
            if (equipment.Accessory2 != Entity.Null) equippedCount++;
            if (equipment.SpecialGear != Entity.Null) equippedCount++;

            equipment.EquippedItemCount = equippedCount;

            // Set bonus for complete equipment sets
            equipment.HasCompleteSet = equippedCount >= 4;
            if (equipment.HasCompleteSet)
            {
                totalStatBonus += 0.2f; // 20% set bonus
            }

            equipment.TotalStatBonus = totalStatBonus;
        }
    }


    [BurstCompile]
    public partial struct EquipmentDurabilityJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref EquipmentComponent equipment, in EquipmentStatsComponent stats)
        {
            if (!equipment.IsEquipped || equipment.Durability <= 0f)
                return;

            // Equipment loses durability over time when equipped
            float durabilityLoss = stats.DurabilityLoss * DeltaTime;
            equipment.Durability = math.max(0f, equipment.Durability - durabilityLoss);

            // Broken equipment provides no benefits (handled by other systems)
        }
    }

    /// <summary>
    /// Equipment crafting and creation system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EquipmentCraftingSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            // Equipment crafting would be triggered by crafting activities
            // This is a placeholder for the crafting integration
        }

        /// <summary>
        /// Create a new equipment entity
        /// </summary>
        public Entity CreateEquipment(EquipmentType equipmentType, EquipmentRarity rarity, uint craftingSeed = 0)
        {
            var ecb = ecbSystem.CreateCommandBuffer();
            var equipmentEntity = ecb.CreateEntity();

            if (craftingSeed == 0)
                craftingSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);

            // Generate equipment stats based on type and rarity
            var stats = GenerateEquipmentStats(equipmentType, rarity, craftingSeed);
            var slot = GetEquipmentSlot(equipmentType);

            ecb.AddComponent(equipmentEntity, new EquipmentComponent
            {
                Type = equipmentType,
                Rarity = rarity,
                Slot = slot,
                ItemID = equipmentType.GetHashCode() + (int)rarity,
                OwnerCreature = Entity.Null,
                IsEquipped = false,
                Durability = CalculateMaxDurability(rarity),
                MaxDurability = CalculateMaxDurability(rarity),
                CraftingSeed = craftingSeed
            });

            ecb.AddComponent(equipmentEntity, stats);

            ecb.AddComponent(equipmentEntity, new EquipmentVisualComponent
            {
                VisualPrefabID = equipmentType.GetHashCode(),
                ColorTint = GenerateColor(rarity, craftingSeed),
                GlowIntensity = GetRarityGlow(rarity),
                HasCustomTexture = rarity >= EquipmentRarity.Epic,
                VisualSeed = craftingSeed
            });

            return equipmentEntity;
        }

        private EquipmentStatsComponent GenerateEquipmentStats(EquipmentType type, EquipmentRarity rarity, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed);
            float rarityMultiplier = GetRarityMultiplier(rarity);

            var stats = new EquipmentStatsComponent();

            // Base stats for equipment type
            switch (type)
            {
                case EquipmentType.SpeedBoots:
                    stats.SpeedBonus = random.NextFloat(0.1f, 0.3f) * rarityMultiplier;
                    stats.AgilityBonus = random.NextFloat(0.05f, 0.15f) * rarityMultiplier;
                    stats.RacingBonus = random.NextFloat(0.15f, 0.35f) * rarityMultiplier;
                    break;

                case EquipmentType.ArmorHeavy:
                    stats.VitalityBonus = random.NextFloat(0.2f, 0.4f) * rarityMultiplier;
                    stats.StrengthBonus = random.NextFloat(0.1f, 0.2f) * rarityMultiplier;
                    stats.CombatBonus = random.NextFloat(0.15f, 0.25f) * rarityMultiplier;
                    break;

                case EquipmentType.ThinkingCap:
                    stats.IntelligenceBonus = random.NextFloat(0.2f, 0.4f) * rarityMultiplier;
                    stats.PuzzleBonus = random.NextFloat(0.25f, 0.45f) * rarityMultiplier;
                    break;

                case EquipmentType.ExperienceMultiplier:
                    stats.ExperienceMultiplier = random.NextFloat(1.1f, 1.5f) * rarityMultiplier;
                    break;

                default:
                    // Generic equipment gets small bonuses across the board
                    stats.StrengthBonus = random.NextFloat(0.05f, 0.1f) * rarityMultiplier;
                    stats.AgilityBonus = random.NextFloat(0.05f, 0.1f) * rarityMultiplier;
                    stats.IntelligenceBonus = random.NextFloat(0.05f, 0.1f) * rarityMultiplier;
                    break;
            }

            // Set durability loss rate
            stats.DurabilityLoss = random.NextFloat(0.1f, 0.3f) / (rarityMultiplier + 0.5f);

            return stats;
        }

        private float GetRarityMultiplier(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => 1.0f,
                EquipmentRarity.Uncommon => 1.2f,
                EquipmentRarity.Rare => 1.5f,
                EquipmentRarity.Epic => 2.0f,
                EquipmentRarity.Legendary => 2.5f,
                EquipmentRarity.Mythic => 3.0f,
                _ => 1.0f
            };
        }

        private EquipmentSlot GetEquipmentSlot(EquipmentType type)
        {
            return type switch
            {
                EquipmentType.AerodynamicHelmet or EquipmentType.ThinkingCap or EquipmentType.TacticalVisor => EquipmentSlot.Head,
                EquipmentType.ArmorHeavy or EquipmentType.ArmorLight or EquipmentType.LightweightHarness => EquipmentSlot.Body,
                EquipmentType.WeaponMelee or EquipmentType.WeaponRanged or EquipmentType.CraftingTools => EquipmentSlot.Weapon,
                EquipmentType.EnergyCore or EquipmentType.HealthBooster or EquipmentType.ExperienceMultiplier => EquipmentSlot.Special,
                _ => EquipmentSlot.Accessory
            };
        }

        private float CalculateMaxDurability(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => 100f,
                EquipmentRarity.Uncommon => 150f,
                EquipmentRarity.Rare => 200f,
                EquipmentRarity.Epic => 300f,
                EquipmentRarity.Legendary => 450f,
                EquipmentRarity.Mythic => 600f,
                _ => 100f
            };
        }

        private float3 GenerateColor(EquipmentRarity rarity, uint seed)
        {
            var random = new Unity.Mathematics.Random(seed);

            return rarity switch
            {
                EquipmentRarity.Common => new float3(0.8f, 0.8f, 0.8f), // Gray
                EquipmentRarity.Uncommon => new float3(0.2f, 1f, 0.2f), // Green
                EquipmentRarity.Rare => new float3(0.2f, 0.5f, 1f), // Blue
                EquipmentRarity.Epic => new float3(0.8f, 0.2f, 1f), // Purple
                EquipmentRarity.Legendary => new float3(1f, 0.6f, 0.1f), // Orange
                EquipmentRarity.Mythic => new float3(1f, 0.8f, 0.2f), // Gold
                _ => new float3(1f, 1f, 1f)
            };
        }

        private float GetRarityGlow(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => 0f,
                EquipmentRarity.Uncommon => 0.1f,
                EquipmentRarity.Rare => 0.3f,
                EquipmentRarity.Epic => 0.5f,
                EquipmentRarity.Legendary => 0.8f,
                EquipmentRarity.Mythic => 1.0f,
                _ => 0f
            };
        }
    }

    /// <summary>
    /// Equipment management for activity performance bonuses
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class ActivityEquipmentBonusSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Apply equipment bonuses to activity performance
            foreach (var (participant, equipment, entity) in
                SystemAPI.Query<RefRW<ActivityParticipantComponent>, RefRO<CreatureEquipmentComponent>>().WithEntityAccess())
            {
                if (participant.ValueRO.Status != ActivityStatus.Active)
                    continue;

                // Calculate activity-specific equipment bonuses
                float equipmentBonus = CalculateActivityBonus(participant.ValueRO.CurrentActivity, equipment.ValueRO);

                // Apply equipment bonus to performance
                float basePerformance = participant.ValueRO.PerformanceScore;
                float enhancedPerformance = basePerformance * (1f + equipmentBonus + equipment.ValueRO.TotalStatBonus);

                participant.ValueRW.PerformanceScore = math.clamp(enhancedPerformance, 0.1f, 3.0f);
            }
        }

        private float CalculateActivityBonus(ActivityType activity, CreatureEquipmentComponent equipment)
        {
            // This would query the actual equipment entities and sum their bonuses
            // For now, simplified calculation based on equipped item count
            float baseBonus = equipment.EquippedItemCount * 0.05f; // 5% per equipped item
            float setBonus = equipment.HasCompleteSet ? 0.2f : 0f; // 20% set bonus

            return baseBonus + setBonus;
        }
    }

    #endregion

    #region Authoring Components

    /// <summary>
    /// MonoBehaviour authoring for equipment shops and vendors
    /// </summary>
    public class EquipmentShopAuthoring : MonoBehaviour
    {
        [Header("Shop Configuration")]
        public EquipmentType[] availableEquipment;
        [Range(1, 10)] public int shopLevel = 1;
        public bool sellsAllRarities = false;
        public float restockInterval = 300f; // 5 minutes

        [Header("Pricing")]
        public int baseCost = 100;
        public float rarityPriceMultiplier = 2.0f;

        [ContextMenu("Stock Shop")]
        public void StockShop()
        {
            var craftingSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<EquipmentCraftingSystem>();
            if (craftingSystem == null) return;

            foreach (var equipType in availableEquipment)
            {
                var maxRarity = sellsAllRarities ? EquipmentRarity.Mythic : (EquipmentRarity)math.min((int)EquipmentRarity.Epic, shopLevel);
                var rarity = (EquipmentRarity)UnityEngine.Random.Range(0, (int)maxRarity + 1);

                var equipment = craftingSystem.CreateEquipment(equipType, rarity);
                Debug.Log($"Stocked {rarity} {equipType} in shop");
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 3f);
            Gizmos.DrawIcon(transform.position + Vector3.up * 2f, "Equipment_Shop_Icon");
        }
    }

    #endregion
}