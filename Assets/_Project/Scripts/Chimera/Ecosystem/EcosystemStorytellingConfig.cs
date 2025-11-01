using System;
using UnityEngine;
using Laboratory.Chimera.Core;
using Laboratory.Core.Enums;

namespace Laboratory.Chimera.Ecosystem
{
    /// <summary>
    /// Configuration ScriptableObject for the Dynamic Ecosystem Storytelling System.
    /// Allows designers to fine-tune ecosystem events, population dynamics, and storytelling elements.
    /// </summary>
    [CreateAssetMenu(fileName = "EcosystemStorytellingConfig", menuName = "Chimera/Ecosystem/Storytelling Config")]
    public class EcosystemStorytellingConfig : ScriptableObject
    {
        [Header("General Settings")]
        [Tooltip("Global multiplier for all ecosystem event frequencies")]
        [Range(0.1f, 5f)]
        public float globalEventFrequencyMultiplier = 1f;

        [Tooltip("Maximum number of major events that can be active simultaneously")]
        [Range(1, 10)]
        public int maxSimultaneousMajorEvents = 3;

        [Tooltip("Enable ecosystem events to create persistent world changes")]
        public bool enablePersistentWorldChanges = true;

        [Header("Apex Predator Events")]
        [Tooltip("Base probability per day of apex predator events")]
        [Range(0.001f, 0.1f)]
        public float apexPredatorBaseRate = 0.01f;

        [Tooltip("Multiplier based on ecosystem health (lower health = higher predator chance)")]
        [Range(0.5f, 3f)]
        public float healthBasedPredatorMultiplier = 2f;

        [Tooltip("Minimum days between apex predator events in same biome")]
        [Range(1, 90)]
        public int apexPredatorCooldownDays = 30;

        public ApexPredatorConfig[] apexPredatorConfigs = new ApexPredatorConfig[]
        {
            new ApexPredatorConfig
            {
                type = ApexPredatorType.ShadowStalker,
                preferredBiomes = new[] { BiomeType.Forest },
                threatLevel = new Vector2(0.6f, 0.9f),
                duration = new Vector2Int(7, 21),
                populationImpact = new Vector2(0.2f, 0.4f),
                specialAbilities = new[] { "Stealth", "Phase Through Trees", "Silent Movement" }
            },
            new ApexPredatorConfig
            {
                type = ApexPredatorType.AbyssalLeviathan,
                preferredBiomes = new[] { BiomeType.Ocean },
                threatLevel = new Vector2(0.7f, 1f),
                duration = new Vector2Int(14, 35),
                populationImpact = new Vector2(0.3f, 0.6f),
                specialAbilities = new[] { "Crushing Tentacles", "Bioluminescent Lures", "Deep Sea Pressure" }
            },
            new ApexPredatorConfig
            {
                type = ApexPredatorType.FrostTitan,
                preferredBiomes = new[] { BiomeType.Arctic },
                threatLevel = new Vector2(0.8f, 1f),
                duration = new Vector2Int(10, 28),
                populationImpact = new Vector2(0.4f, 0.7f),
                specialAbilities = new[] { "Freeze Breath", "Ice Armor", "Avalanche Creation" }
            }
        };

        [Header("Ecological Disasters")]
        [Tooltip("Base probability per day of ecological disasters")]
        [Range(0.001f, 0.05f)]
        public float ecologicalDisasterBaseRate = 0.005f;

        [Tooltip("Multiplier based on global environmental stress")]
        [Range(1f, 5f)]
        public float stressBasedDisasterMultiplier = 3f;

        public EcologicalDisasterConfig[] disasterConfigs = new EcologicalDisasterConfig[]
        {
            new EcologicalDisasterConfig
            {
                type = EcologicalDisasterType.VolcanicEruption,
                severity = new Vector2(0.5f, 0.9f),
                duration = new Vector2Int(14, 45),
                biomeImpact = new[]
                {
                    new BiomeImpact { biome = BiomeType.Mountain, healthImpact = -0.6f, stabilityImpact = -0.7f },
                    new BiomeImpact { biome = BiomeType.Forest, healthImpact = -0.3f, stabilityImpact = -0.4f }
                },
                recoveryTime = 60,
                warningEvents = new[] { "Increased seismic activity", "Strange underground sounds", "Animals fleeing the area" }
            },
            new EcologicalDisasterConfig
            {
                type = EcologicalDisasterType.ExtremeDrought,
                severity = new Vector2(0.4f, 0.8f),
                duration = new Vector2Int(30, 90),
                biomeImpact = new[]
                {
                    new BiomeImpact { biome = BiomeType.Desert, healthImpact = -0.2f, stabilityImpact = -0.3f },
                    new BiomeImpact { biome = BiomeType.Forest, healthImpact = -0.7f, stabilityImpact = -0.6f }
                },
                recoveryTime = 45,
                warningEvents = new[] { "Water levels dropping", "Vegetation showing stress", "Increased animal competition for water" }
            },
            new EcologicalDisasterConfig
            {
                type = EcologicalDisasterType.IceAge,
                severity = new Vector2(0.7f, 1f),
                duration = new Vector2Int(60, 180),
                biomeImpact = new[]
                {
                    new BiomeImpact { biome = BiomeType.Arctic, healthImpact = 0.1f, stabilityImpact = 0.2f }, // Arctic actually benefits
                    new BiomeImpact { biome = BiomeType.Forest, healthImpact = -0.8f, stabilityImpact = -0.6f },
                    new BiomeImpact { biome = BiomeType.Desert, healthImpact = -0.5f, stabilityImpact = -0.4f }
                },
                recoveryTime = 120,
                warningEvents = new[] { "Unusual cold fronts", "Ice forming in unexpected places", "Migration patterns changing" }
            }
        };

        [Header("Population Dynamics")]
        [Tooltip("Base probability per day of population events")]
        [Range(0.01f, 0.2f)]
        public float populationEventBaseRate = 0.05f;

        [Tooltip("Ideal species count per biome for ecosystem health")]
        [Range(3, 15)]
        public int idealSpeciesPerBiome = 8;

        [Tooltip("Population growth rate modifier for healthy ecosystems")]
        [Range(0.5f, 3f)]
        public float healthyEcosystemGrowthMultiplier = 1.5f;

        public PopulationEventConfig[] populationConfigs = new PopulationEventConfig[]
        {
            new PopulationEventConfig
            {
                type = PopulationEventType.PopulationBoom,
                triggerConditions = new[] { "High food availability", "Low predator pressure", "Favorable weather" },
                magnitude = new Vector2(0.4f, 0.8f),
                duration = new Vector2Int(21, 60),
                affectedSpeciesPercentage = 0.3f
            },
            new PopulationEventConfig
            {
                type = PopulationEventType.MassiveMigration,
                triggerConditions = new[] { "Seasonal changes", "Resource depletion", "Habitat destruction" },
                magnitude = new Vector2(0.3f, 0.7f),
                duration = new Vector2Int(7, 21),
                affectedSpeciesPercentage = 0.4f
            },
            new PopulationEventConfig
            {
                type = PopulationEventType.InvasiveSpecies,
                triggerConditions = new[] { "Ecosystem disruption", "Trade routes", "Climate change" },
                magnitude = new Vector2(0.5f, 0.9f),
                duration = new Vector2Int(30, 90),
                affectedSpeciesPercentage = 0.1f
            }
        };

        [Header("Weather Events")]
        [Tooltip("Base probability per 6 hours of weather events")]
        [Range(0.1f, 1f)]
        public float weatherEventBaseRate = 0.3f;

        [Tooltip("Enable seasonal weather pattern changes")]
        public bool enableSeasonalWeatherPatterns = true;

        public WeatherEventConfig[] weatherConfigs = new WeatherEventConfig[]
        {
            new WeatherEventConfig
            {
                type = WeatherEventType.StormSeason,
                seasonalPreference = new[] { "Spring", "Autumn" },
                intensity = new Vector2(0.4f, 0.8f),
                duration = new Vector2Int(14, 28),
                biomeModifiers = new[]
                {
                    new BiomeWeatherModifier { biome = BiomeType.Ocean, activityModifier = 0.7f, populationImpact = -0.1f },
                    new BiomeWeatherModifier { biome = BiomeType.Forest, activityModifier = 0.8f, populationImpact = -0.05f }
                }
            },
            new WeatherEventConfig
            {
                type = WeatherEventType.AuroraDisplay,
                seasonalPreference = new[] { "Winter" },
                intensity = new Vector2(0.2f, 0.6f),
                duration = new Vector2Int(1, 3),
                biomeModifiers = new[]
                {
                    new BiomeWeatherModifier { biome = BiomeType.Arctic, activityModifier = 1.1f, populationImpact = 0.05f }
                },
                specialEffects = new[] { "Increased mutation chance", "Enhanced nocturnal behavior", "Improved breeding success" }
            },
            new WeatherEventConfig
            {
                type = WeatherEventType.MeteorShower,
                seasonalPreference = new[] { "Summer", "Autumn" },
                intensity = new Vector2(0.5f, 0.9f),
                duration = new Vector2Int(1, 2),
                biomeModifiers = new[]
                {
                    new BiomeWeatherModifier { biome = BiomeType.Desert, activityModifier = 1.2f, populationImpact = 0.1f },
                    new BiomeWeatherModifier { biome = BiomeType.Mountain, activityModifier = 1.1f, populationImpact = 0.05f }
                },
                specialEffects = new[] { "Beneficial mutations", "Rare mineral deposits", "Enhanced night vision" }
            }
        };

        [Header("Storytelling Elements")]
        [Tooltip("Maximum number of story events to keep in history")]
        [Range(50, 500)]
        public int maxStoryHistoryEvents = 200;

        [Tooltip("Enable dynamic narrative generation based on events")]
        public bool enableDynamicNarratives = true;

        [Tooltip("Enable player action impact tracking")]
        public bool enablePlayerImpactTracking = true;

        [Header("Recovery and Regeneration")]
        [Tooltip("Base ecosystem recovery rate per day")]
        [Range(0.001f, 0.1f)]
        public float baseEcosystemRecoveryRate = 0.02f;

        [Tooltip("Population recovery multiplier after disasters")]
        [Range(0.5f, 3f)]
        public float postDisasterRecoveryMultiplier = 1.5f;

        [Tooltip("Time in days for ecosystems to show recovery signs")]
        [Range(7, 60)]
        public int recoveryVisibilityDays = 21;

        [Header("Advanced Features")]
        [Tooltip("Enable cross-biome event cascades")]
        public bool enableEventCascades = true;

        [Tooltip("Enable adaptive event difficulty based on player progression")]
        public bool enableAdaptiveDifficulty = true;

        [Tooltip("Global environmental stress threshold for disaster acceleration")]
        [Range(0.3f, 0.9f)]
        public float disasterAccelerationThreshold = 0.7f;

        /// <summary>
        /// Gets configuration for a specific apex predator type
        /// </summary>
        public ApexPredatorConfig GetApexPredatorConfig(ApexPredatorType type)
        {
            foreach (var config in apexPredatorConfigs)
            {
                if (config.type == type)
                    return config;
            }

            // Return default if not found
            return new ApexPredatorConfig
            {
                type = type,
                preferredBiomes = new[] { BiomeType.Forest },
                threatLevel = new Vector2(0.5f, 0.8f),
                duration = new Vector2Int(7, 14),
                populationImpact = new Vector2(0.2f, 0.4f),
                specialAbilities = new[] { "Unknown Ability" }
            };
        }

        /// <summary>
        /// Gets configuration for a specific disaster type
        /// </summary>
        public EcologicalDisasterConfig GetDisasterConfig(EcologicalDisasterType type)
        {
            foreach (var config in disasterConfigs)
            {
                if (config.type == type)
                    return config;
            }

            // Return default if not found
            return new EcologicalDisasterConfig
            {
                type = type,
                severity = new Vector2(0.3f, 0.7f),
                duration = new Vector2Int(7, 21),
                biomeImpact = new BiomeImpact[0],
                recoveryTime = 30,
                warningEvents = new[] { "Environmental disturbance detected" }
            };
        }

        /// <summary>
        /// Gets configuration for a specific weather event type
        /// </summary>
        public WeatherEventConfig GetWeatherConfig(WeatherEventType type)
        {
            foreach (var config in weatherConfigs)
            {
                if (config.type == type)
                    return config;
            }

            // Return default if not found
            return new WeatherEventConfig
            {
                type = type,
                seasonalPreference = new[] { "Any" },
                intensity = new Vector2(0.3f, 0.7f),
                duration = new Vector2Int(1, 7),
                biomeModifiers = new BiomeWeatherModifier[0],
                specialEffects = new string[0]
            };
        }

        /// <summary>
        /// Calculates event probability based on current ecosystem state
        /// </summary>
        public float CalculateEventProbability(EcosystemEventType eventType, float ecosystemHealth, float environmentalStress)
        {
            float baseProbability = eventType switch
            {
                EcosystemEventType.ApexPredatorArrival => apexPredatorBaseRate,
                EcosystemEventType.EcologicalDisaster => ecologicalDisasterBaseRate,
                EcosystemEventType.PopulationMigration => populationEventBaseRate,
                EcosystemEventType.WeatherPhenomena => weatherEventBaseRate,
                _ => 0.01f
            };

            // Apply ecosystem health modifiers
            float healthModifier = eventType switch
            {
                EcosystemEventType.ApexPredatorArrival => Mathf.Lerp(healthBasedPredatorMultiplier, 1f, ecosystemHealth),
                EcosystemEventType.EcologicalDisaster => Mathf.Lerp(stressBasedDisasterMultiplier, 1f, 1f - environmentalStress),
                _ => 1f
            };

            return baseProbability * healthModifier * globalEventFrequencyMultiplier;
        }

        void OnValidate()
        {
            // Ensure reasonable values
            globalEventFrequencyMultiplier = Mathf.Clamp(globalEventFrequencyMultiplier, 0.1f, 5f);
            maxSimultaneousMajorEvents = Mathf.Clamp(maxSimultaneousMajorEvents, 1, 10);

            // Validate event rates
            apexPredatorBaseRate = Mathf.Clamp(apexPredatorBaseRate, 0.001f, 0.1f);
            ecologicalDisasterBaseRate = Mathf.Clamp(ecologicalDisasterBaseRate, 0.001f, 0.05f);
            populationEventBaseRate = Mathf.Clamp(populationEventBaseRate, 0.01f, 0.2f);
            weatherEventBaseRate = Mathf.Clamp(weatherEventBaseRate, 0.1f, 1f);

            // Ensure apex predator configs have valid data
            for (int i = 0; i < apexPredatorConfigs.Length; i++)
            {
                if (apexPredatorConfigs[i].preferredBiomes == null || apexPredatorConfigs[i].preferredBiomes.Length == 0)
                {
                    apexPredatorConfigs[i].preferredBiomes = new[] { BiomeType.Forest };
                }
                if (apexPredatorConfigs[i].specialAbilities == null || apexPredatorConfigs[i].specialAbilities.Length == 0)
                {
                    apexPredatorConfigs[i].specialAbilities = new[] { "Unknown Ability" };
                }
            }

            // Ensure disaster configs have valid data
            for (int i = 0; i < disasterConfigs.Length; i++)
            {
                if (disasterConfigs[i].warningEvents == null || disasterConfigs[i].warningEvents.Length == 0)
                {
                    disasterConfigs[i].warningEvents = new[] { "Environmental disturbance detected" };
                }
            }
        }
    }

    #region Configuration Data Structures

    /// <summary>
    /// Configuration for apex predator events
    /// </summary>
    [Serializable]
    public struct ApexPredatorConfig
    {
        public ApexPredatorType type;
        public BiomeType[] preferredBiomes;
        public Vector2 threatLevel; // Min/max threat level
        public Vector2Int duration; // Min/max duration in days
        public Vector2 populationImpact; // Min/max population impact
        public string[] specialAbilities;
        [TextArea(2, 3)]
        public string flavorText;
    }

    /// <summary>
    /// Configuration for ecological disasters
    /// </summary>
    [Serializable]
    public struct EcologicalDisasterConfig
    {
        public EcologicalDisasterType type;
        public Vector2 severity; // Min/max severity
        public Vector2Int duration; // Min/max duration in days
        public BiomeImpact[] biomeImpact; // How this disaster affects different biomes
        public int recoveryTime; // Days to full recovery
        public string[] warningEvents; // Signs that this disaster is approaching
        [TextArea(2, 3)]
        public string description;
    }

    /// <summary>
    /// How a disaster affects a specific biome
    /// </summary>
    [Serializable]
    public struct BiomeImpact
    {
        public BiomeType biome;
        [Range(-1f, 1f)]
        public float healthImpact; // Negative = damage, positive = benefit
        [Range(-1f, 1f)]
        public float stabilityImpact;
        [Range(-1f, 1f)]
        public float biodiversityImpact;
    }

    /// <summary>
    /// Configuration for population events
    /// </summary>
    [Serializable]
    public struct PopulationEventConfig
    {
        public PopulationEventType type;
        public string[] triggerConditions; // What causes this event
        public Vector2 magnitude; // Min/max effect strength
        public Vector2Int duration; // Min/max duration in days
        [Range(0f, 1f)]
        public float affectedSpeciesPercentage; // % of species affected
        [TextArea(2, 3)]
        public string description;
    }

    /// <summary>
    /// Configuration for weather events
    /// </summary>
    [Serializable]
    public struct WeatherEventConfig
    {
        public WeatherEventType type;
        public string[] seasonalPreference; // Which seasons this weather prefers
        public Vector2 intensity; // Min/max intensity
        public Vector2Int duration; // Min/max duration in days
        public BiomeWeatherModifier[] biomeModifiers; // How this weather affects different biomes
        public string[] specialEffects; // Special effects this weather can cause
        [TextArea(2, 3)]
        public string description;
    }

    /// <summary>
    /// How weather affects a specific biome
    /// </summary>
    [Serializable]
    public struct BiomeWeatherModifier
    {
        public BiomeType biome;
        [Range(0.1f, 2f)]
        public float activityModifier; // How weather affects creature activity
        [Range(-0.5f, 0.5f)]
        public float populationImpact; // Direct population effect
        [Range(-0.3f, 0.3f)]
        public float healthModifier; // Effect on ecosystem health
    }

    #endregion
}