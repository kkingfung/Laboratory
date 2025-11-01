using System;
using UnityEngine;

namespace Laboratory.Chimera.Genetics.Advanced
{
    /// <summary>
    /// Configuration ScriptableObject for the Temporal Genetics System.
    /// Allows designers to tweak genetic memory, evolutionary pressure, and archaeological settings.
    /// </summary>
    [CreateAssetMenu(fileName = "TemporalGeneticsConfig", menuName = "Chimera/Genetics/Temporal Genetics Config")]
    public class TemporalGeneticsConfig : ScriptableObject
    {
        [Header("Genetic Memory Configuration")]
        [Tooltip("Maximum number of generations to remember in genetic memory")]
        [Range(5, 20)]
        public int maxGenerationMemory = 10;

        [Tooltip("Rate at which genetic memories decay over time (0 = never decay, 1 = decay immediately)")]
        [Range(0f, 1f)]
        public float memoryDecayRate = 0.1f;

        [Tooltip("Stress level required to trigger ancestral trait activation")]
        [Range(0.1f, 1f)]
        public float stressActivationThreshold = 0.7f;

        [Tooltip("Base probability of ancestral trait activation under stress")]
        [Range(0.01f, 0.5f)]
        public float ancestralActivationBaseProbability = 0.1f;

        [Header("Evolutionary Pressure Configuration")]
        [Tooltip("Daily probability of a new evolutionary pressure event starting")]
        [Range(0.001f, 0.1f)]
        public float pressureEventFrequency = 0.02f;

        [Tooltip("Base intensity of evolutionary pressure effects")]
        [Range(0.1f, 1f)]
        public float basePressureIntensity = 0.3f;

        [Tooltip("Variation in pressure intensity (±this amount)")]
        [Range(0f, 0.5f)]
        public float pressureIntensityVariation = 0.1f;

        [Tooltip("Base duration of pressure events in game days")]
        [Range(7, 90)]
        public int basePressureDuration = 30;

        [Tooltip("Variation in pressure duration (±this amount in days)")]
        [Range(0, 30)]
        public int pressureDurationVariation = 10;

        [Header("Specific Pressure Configurations")]
        public EvolutionaryPressureConfig[] pressureConfigs = new EvolutionaryPressureConfig[]
        {
            new EvolutionaryPressureConfig
            {
                type = EvolutionaryPressureType.ClimateChange,
                weight = 1f,
                favoredTraits = new[] { "Heat Resistance", "Cold Resistance", "Adaptability" },
                description = "Climate instability favors adaptable creatures"
            },
            new EvolutionaryPressureConfig
            {
                type = EvolutionaryPressureType.ResourceScarcity,
                weight = 1.2f,
                favoredTraits = new[] { "Efficiency", "Foraging", "Conservation" },
                description = "Food scarcity rewards efficient resource usage"
            },
            new EvolutionaryPressureConfig
            {
                type = EvolutionaryPressureType.PredatorIncrease,
                weight = 0.8f,
                favoredTraits = new[] { "Speed", "Stealth", "Defensive", "Pack Behavior" },
                description = "Increased predation pressure favors survival traits"
            }
        };

        [Header("Genetic Archaeology Configuration")]
        [Tooltip("Probability of discovering ancient DNA during exploration")]
        [Range(0.0001f, 0.01f)]
        public float ancientDNADiscoveryRate = 0.001f;

        [Tooltip("Maximum age of discoverable ancient traits in generations")]
        [Range(10, 100)]
        public int maxAncientTraitAge = 50;

        [Tooltip("Minimum purity required for ancient DNA integration")]
        [Range(0.1f, 0.9f)]
        public float minIntegrationPurity = 0.5f;

        [Tooltip("Base success rate for ancient DNA integration")]
        [Range(0.1f, 1f)]
        public float baseIntegrationSuccessRate = 0.8f;

        [Header("Ancient DNA Fragment Rarity")]
        public AncientTraitRarityConfig[] ancientTraitRarities = new AncientTraitRarityConfig[]
        {
            new AncientTraitRarityConfig
            {
                rarity = AncientTraitRarity.Common,
                discoveryWeight = 0.6f,
                purityRange = new Vector2(0.3f, 0.7f),
                powerRange = new Vector2(0.5f, 0.8f)
            },
            new AncientTraitRarityConfig
            {
                rarity = AncientTraitRarity.Rare,
                discoveryWeight = 0.3f,
                purityRange = new Vector2(0.5f, 0.8f),
                powerRange = new Vector2(0.7f, 0.9f)
            },
            new AncientTraitRarityConfig
            {
                rarity = AncientTraitRarity.Legendary,
                discoveryWeight = 0.1f,
                purityRange = new Vector2(0.7f, 0.95f),
                powerRange = new Vector2(0.8f, 1.0f)
            }
        };

        [Header("Biome-Specific Ancient Traits")]
        public BiomeAncientTraitConfig[] biomeAncientTraits = new BiomeAncientTraitConfig[]
        {
            new BiomeAncientTraitConfig
            {
                biome = Laboratory.Core.Enums.BiomeType.Forest,
                ancientTraits = new[]
                {
                    "Photosynthetic Patches", "Bark Skin", "Root Network Communication",
                    "Seasonal Hibernation", "Pollen Immunity", "Tree Camouflage"
                }
            },
            new BiomeAncientTraitConfig
            {
                biome = Laboratory.Core.Enums.BiomeType.Desert,
                ancientTraits = new[]
                {
                    "Water Storage Organs", "Sand Burrowing", "Heat Absorption",
                    "Nocturnal Sight", "Sandstorm Resistance", "Mirage Creation"
                }
            },
            new BiomeAncientTraitConfig
            {
                biome = Laboratory.Core.Enums.BiomeType.Ocean,
                ancientTraits = new[]
                {
                    "Pressure Immunity", "Echolocation", "Bioluminescence",
                    "Electrical Generation", "Current Reading", "Deep Sea Vision"
                }
            },
            new BiomeAncientTraitConfig
            {
                biome = Laboratory.Core.Enums.BiomeType.Mountain,
                ancientTraits = new[]
                {
                    "Altitude Adaptation", "Rock Climbing Claws", "Thin Air Breathing",
                    "Avalanche Sense", "Stone Camouflage", "Wind Resistance"
                }
            },
            new BiomeAncientTraitConfig
            {
                biome = Laboratory.Core.Enums.BiomeType.Arctic,
                ancientTraits = new[]
                {
                    "Antifreeze Blood", "Blubber Insulation", "Ice Walking",
                    "Snow Vision", "Hibernation Mastery", "Cold Healing"
                }
            }
        };

        [Header("Advanced Settings")]
        [Tooltip("Global modifier for all temporal genetic effects")]
        [Range(0.1f, 3f)]
        public float globalTemporalModifier = 1f;

        [Tooltip("Enable temporal genetic events to affect wild populations")]
        public bool affectWildPopulations = true;

        [Tooltip("Enable cross-lineage genetic memory sharing")]
        public bool enableCrossLineageMemory = true;

        [Tooltip("Maximum number of active pressure events simultaneously")]
        [Range(1, 5)]
        public int maxSimultaneousPressures = 3;

        /// <summary>
        /// Gets the configuration for a specific evolutionary pressure type
        /// </summary>
        public EvolutionaryPressureConfig GetPressureConfig(EvolutionaryPressureType type)
        {
            foreach (var config in pressureConfigs)
            {
                if (config.type == type)
                    return config;
            }

            // Return default config if not found
            return new EvolutionaryPressureConfig
            {
                type = type,
                weight = 1f,
                favoredTraits = new[] { "Adaptability" },
                description = "Unknown evolutionary pressure"
            };
        }

        /// <summary>
        /// Gets ancient traits for a specific biome
        /// </summary>
        public string[] GetAncientTraitsForBiome(Laboratory.Core.Enums.BiomeType biome)
        {
            foreach (var config in biomeAncientTraits)
            {
                if (config.biome == biome)
                    return config.ancientTraits;
            }

            return new[] { "Ancient Wisdom", "Primal Instincts" }; // Default traits
        }

        /// <summary>
        /// Gets rarity configuration for ancient trait discovery
        /// </summary>
        public AncientTraitRarityConfig GetRarityConfig(AncientTraitRarity rarity)
        {
            foreach (var config in ancientTraitRarities)
            {
                if (config.rarity == rarity)
                    return config;
            }

            return ancientTraitRarities[0]; // Return common as default
        }

        /// <summary>
        /// Validates the configuration and fixes any invalid values
        /// </summary>
        void OnValidate()
        {
            // Ensure frequency doesn't get too high
            pressureEventFrequency = Mathf.Clamp(pressureEventFrequency, 0.001f, 0.1f);

            // Ensure discovery rate is reasonable
            ancientDNADiscoveryRate = Mathf.Clamp(ancientDNADiscoveryRate, 0.0001f, 0.01f);

            // Validate pressure configs have favored traits
            for (int i = 0; i < pressureConfigs.Length; i++)
            {
                if (pressureConfigs[i].favoredTraits == null || pressureConfigs[i].favoredTraits.Length == 0)
                {
                    pressureConfigs[i].favoredTraits = new[] { "Adaptability" };
                }
            }

            // Validate biome configs have ancient traits
            for (int i = 0; i < biomeAncientTraits.Length; i++)
            {
                if (biomeAncientTraits[i].ancientTraits == null || biomeAncientTraits[i].ancientTraits.Length == 0)
                {
                    biomeAncientTraits[i].ancientTraits = new[] { "Ancient Wisdom" };
                }
            }
        }
    }

    #region Configuration Data Structures

    /// <summary>
    /// Configuration for a specific evolutionary pressure type
    /// </summary>
    [Serializable]
    public struct EvolutionaryPressureConfig
    {
        public EvolutionaryPressureType type;
        [Range(0.1f, 3f)]
        public float weight; // How likely this pressure is to occur
        public string[] favoredTraits; // Traits that benefit from this pressure
        [TextArea(2, 3)]
        public string description;
    }

    /// <summary>
    /// Configuration for ancient trait rarity levels
    /// </summary>
    [Serializable]
    public struct AncientTraitRarityConfig
    {
        public AncientTraitRarity rarity;
        [Range(0f, 1f)]
        public float discoveryWeight; // Relative probability of discovery
        public Vector2 purityRange; // Min/max purity for this rarity
        public Vector2 powerRange; // Min/max trait power for this rarity
    }

    /// <summary>
    /// Configuration for biome-specific ancient traits
    /// </summary>
    [Serializable]
    public struct BiomeAncientTraitConfig
    {
        public Laboratory.Core.Enums.BiomeType biome;
        public string[] ancientTraits;
    }

    /// <summary>
    /// Rarity levels for ancient DNA fragments
    /// </summary>
    public enum AncientTraitRarity
    {
        Common,    // 60% of discoveries
        Rare,      // 30% of discoveries
        Legendary  // 10% of discoveries
    }

    #endregion
}