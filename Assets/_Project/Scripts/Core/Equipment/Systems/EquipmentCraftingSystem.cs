using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Core.Equipment.Components;
using Laboratory.Core.Equipment.Types;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Core.Equipment.Systems
{
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
}