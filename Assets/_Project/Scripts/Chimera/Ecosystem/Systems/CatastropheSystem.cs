using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Ecosystem.Data;

namespace Laboratory.Chimera.Ecosystem.Systems
{
    /// <summary>
    /// Manages catastrophic events and their effects on the ecosystem
    /// </summary>
    public class CatastropheSystem : MonoBehaviour
    {
        [Header("Catastrophe Configuration")]
        [SerializeField] private float catastropheCheckInterval = 60.0f;
        [SerializeField] private float baseCatastropheProbability = 0.05f;
        [SerializeField] private bool enableCatastrophes = true;
        [SerializeField] private bool enableRecovery = true;

        [Header("Event Types")]
        [SerializeField] private CatastropheConfiguration[] catastropheConfigs;
        [SerializeField] private float climateInfluenceFactor = 1.0f;
        [SerializeField] private float seasonalVariationFactor = 0.5f;

        [Header("Recovery Settings")]
        [SerializeField] private float baseRecoveryRate = 0.1f;
        [SerializeField] private float resilienceBonus = 0.2f;
        [SerializeField] private bool enableAdaptiveRecovery = true;

        private List<CatastrophicEvent> activeEvents = new();
        private Dictionary<CatastropheType, CatastropheConfiguration> eventConfigs = new();
        private Dictionary<Vector2, List<CatastrophicEvent>> regionalEvents = new();
        private Dictionary<CatastropheType, float> eventHistory = new();

        // Dependencies
        private ClimateEvolutionSystem climateSystem;
        private BiomeTransitionSystem biomeSystem;
        private ResourceFlowSystem resourceSystem;
        private SpeciesInteractionSystem speciesSystem;
        private EcosystemHealthMonitor healthMonitor;

        // Events
        public System.Action<CatastrophicEvent> OnCatastropheTriggered;
        public System.Action<CatastrophicEvent> OnCatastropheEnded;
        public System.Action<Vector2, CatastropheType, float> OnRecoveryProgress;
        public System.Action<CatastropheType, float> OnEventProbabilityChanged;

        private void Awake()
        {
            FindDependencies();
            InitializeCatastropheConfigurations();
        }

        private void Start()
        {
            StartCoroutine(CatastropheMonitoringLoop());
        }

        private void FindDependencies()
        {
            climateSystem = FindObjectOfType<ClimateEvolutionSystem>();
            biomeSystem = FindObjectOfType<BiomeTransitionSystem>();
            resourceSystem = FindObjectOfType<ResourceFlowSystem>();
            speciesSystem = FindObjectOfType<SpeciesInteractionSystem>();
            healthMonitor = FindObjectOfType<EcosystemHealthMonitor>();
        }

        private void InitializeCatastropheConfigurations()
        {
            var defaultConfigs = new CatastropheConfiguration[]
            {
                new CatastropheConfiguration
                {
                    Type = CatastropheType.Wildfire,
                    BaseProbability = 0.03f,
                    MinIntensity = 0.3f,
                    MaxIntensity = 1.0f,
                    MinDuration = 300f,
                    MaxDuration = 1800f,
                    MinRadius = 10f,
                    MaxRadius = 30f,
                    SeasonalModifier = new Dictionary<SeasonType, float>
                    {
                        [SeasonType.Summer] = 2.0f,
                        [SeasonType.Autumn] = 1.5f,
                        [SeasonType.Spring] = 0.8f,
                        [SeasonType.Winter] = 0.3f
                    },
                    BiomeModifier = new Dictionary<BiomeType, float>
                    {
                        [BiomeType.Forest] = 2.0f,
                        [BiomeType.Grassland] = 1.5f,
                        [BiomeType.Desert] = 0.8f,
                        [BiomeType.Swamp] = 0.3f
                    },
                    Effects = new Dictionary<string, float>
                    {
                        ["VegetationDestruction"] = -0.8f,
                        ["SoilDamage"] = -0.4f,
                        ["AirQuality"] = -0.6f,
                        ["WaterQuality"] = -0.3f
                    }
                },
                new CatastropheConfiguration
                {
                    Type = CatastropheType.Flood,
                    BaseProbability = 0.025f,
                    MinIntensity = 0.2f,
                    MaxIntensity = 1.0f,
                    MinDuration = 600f,
                    MaxDuration = 2400f,
                    MinRadius = 15f,
                    MaxRadius = 50f,
                    SeasonalModifier = new Dictionary<SeasonType, float>
                    {
                        [SeasonType.Spring] = 2.0f,
                        [SeasonType.Autumn] = 1.3f,
                        [SeasonType.Summer] = 0.7f,
                        [SeasonType.Winter] = 1.2f
                    },
                    BiomeModifier = new Dictionary<BiomeType, float>
                    {
                        [BiomeType.Grassland] = 1.8f,
                        [BiomeType.Forest] = 1.2f,
                        [BiomeType.Swamp] = 0.5f,
                        [BiomeType.Desert] = 0.2f,
                        [BiomeType.Mountain] = 0.3f
                    },
                    Effects = new Dictionary<string, float>
                    {
                        ["SoilErosion"] = -0.6f,
                        ["WaterContamination"] = -0.7f,
                        ["HabitatDestruction"] = -0.5f,
                        ["Displacement"] = -0.8f
                    }
                },
                new CatastropheConfiguration
                {
                    Type = CatastropheType.Drought,
                    BaseProbability = 0.04f,
                    MinIntensity = 0.4f,
                    MaxIntensity = 1.0f,
                    MinDuration = 1800f,
                    MaxDuration = 7200f,
                    MinRadius = 25f,
                    MaxRadius = 100f,
                    SeasonalModifier = new Dictionary<SeasonType, float>
                    {
                        [SeasonType.Summer] = 2.5f,
                        [SeasonType.Autumn] = 1.5f,
                        [SeasonType.Spring] = 0.5f,
                        [SeasonType.Winter] = 0.2f
                    },
                    BiomeModifier = new Dictionary<BiomeType, float>
                    {
                        [BiomeType.Desert] = 0.8f,
                        [BiomeType.Grassland] = 2.0f,
                        [BiomeType.Savanna] = 1.8f,
                        [BiomeType.Forest] = 1.2f,
                        [BiomeType.Swamp] = 0.3f
                    },
                    Effects = new Dictionary<string, float>
                    {
                        ["WaterAvailability"] = -0.9f,
                        ["VegetationHealth"] = -0.7f,
                        ["SoilQuality"] = -0.5f,
                        ["FoodAvailability"] = -0.8f
                    }
                },
                new CatastropheConfiguration
                {
                    Type = CatastropheType.VolcanicEruption,
                    BaseProbability = 0.005f,
                    MinIntensity = 0.6f,
                    MaxIntensity = 1.0f,
                    MinDuration = 1200f,
                    MaxDuration = 3600f,
                    MinRadius = 20f,
                    MaxRadius = 80f,
                    SeasonalModifier = new Dictionary<SeasonType, float>
                    {
                        [SeasonType.Spring] = 1.2f,
                        [SeasonType.Summer] = 1.0f,
                        [SeasonType.Autumn] = 1.1f,
                        [SeasonType.Winter] = 0.9f
                    },
                    BiomeModifier = new Dictionary<BiomeType, float>
                    {
                        [BiomeType.Volcanic] = 3.0f,
                        [BiomeType.Mountain] = 1.5f,
                        [BiomeType.Desert] = 0.8f,
                        [BiomeType.Ocean] = 0.3f
                    },
                    Effects = new Dictionary<string, float>
                    {
                        ["AirQuality"] = -0.9f,
                        ["SoilEnrichment"] = 0.3f, // Long-term positive effect
                        ["VegetationDestruction"] = -0.95f,
                        ["ClimateImpact"] = -0.7f
                    }
                },
                new CatastropheConfiguration
                {
                    Type = CatastropheType.Plague,
                    BaseProbability = 0.02f,
                    MinIntensity = 0.3f,
                    MaxIntensity = 0.8f,
                    MinDuration = 900f,
                    MaxDuration = 3600f,
                    MinRadius = 30f,
                    MaxRadius = 60f,
                    SeasonalModifier = new Dictionary<SeasonType, float>
                    {
                        [SeasonType.Spring] = 1.5f,
                        [SeasonType.Summer] = 1.8f,
                        [SeasonType.Autumn] = 1.2f,
                        [SeasonType.Winter] = 0.6f
                    },
                    BiomeModifier = new Dictionary<BiomeType, float>
                    {
                        [BiomeType.Swamp] = 2.0f,
                        [BiomeType.Forest] = 1.5f,
                        [BiomeType.Grassland] = 1.3f,
                        [BiomeType.Desert] = 0.7f,
                        [BiomeType.Tundra] = 0.5f
                    },
                    Effects = new Dictionary<string, float>
                    {
                        ["PopulationHealth"] = -0.7f,
                        ["ReproductionRate"] = -0.5f,
                        ["SocialStructure"] = -0.4f,
                        ["Immunity"] = 0.2f // Survivors gain resistance
                    }
                }
            };

            var configsToUse = catastropheConfigs?.Length > 0 ? catastropheConfigs : defaultConfigs;

            foreach (var config in configsToUse)
            {
                eventConfigs[config.Type] = config;
                eventHistory[config.Type] = 0f;
            }

            Debug.Log($"💥 Initialized {eventConfigs.Count} catastrophe types");
        }

        private IEnumerator CatastropheMonitoringLoop()
        {
            while (enableCatastrophes)
            {
                CheckForNewCatastrophes();
                UpdateActiveCatastrophes();
                ProcessRecovery();

                yield return new WaitForSeconds(catastropheCheckInterval);
            }
        }

        private void CheckForNewCatastrophes()
        {
            foreach (var config in eventConfigs.Values)
            {
                float probability = CalculateCatastropheProbability(config);

                if (Random.value < probability)
                {
                    TriggerCatastrophe(config.Type);
                }
            }
        }

        private float CalculateCatastropheProbability(CatastropheConfiguration config)
        {
            float baseProbability = config.BaseProbability * baseCatastropheProbability;

            // Apply seasonal modifier
            float seasonalModifier = 1.0f;
            if (climateSystem != null)
            {
                var currentSeason = climateSystem.GetCurrentSeason();
                seasonalModifier = config.SeasonalModifier.GetValueOrDefault(currentSeason, 1.0f);
            }

            // Apply climate influence
            float climateModifier = 1.0f;
            if (climateSystem != null)
            {
                var climate = climateSystem.GetCurrentClimate();
                climateModifier = 1.0f + (1.0f - climate.ClimateStability) * climateInfluenceFactor;
            }

            // Apply ecosystem health influence
            float healthModifier = 1.0f;
            if (healthMonitor != null)
            {
                var health = healthMonitor.GetCurrentHealth();
                healthModifier = 1.0f + (1.0f - health.OverallHealthScore) * 0.5f;
            }

            // Apply frequency dampening (reduce probability if event happened recently)
            float frequencyModifier = 1.0f - eventHistory.GetValueOrDefault(config.Type, 0f) * 0.5f;

            float finalProbability = baseProbability * seasonalModifier * climateModifier *
                                   healthModifier * frequencyModifier * seasonalVariationFactor;

            OnEventProbabilityChanged?.Invoke(config.Type, finalProbability);
            return finalProbability;
        }

        private void TriggerCatastrophe(CatastropheType type)
        {
            if (!eventConfigs.TryGetValue(type, out var config)) return;

            var location = SelectCatastropheLocation(type);
            var intensity = Random.Range(config.MinIntensity, config.MaxIntensity);
            var duration = Random.Range(config.MinDuration, config.MaxDuration);
            var radius = Random.Range(config.MinRadius, config.MaxRadius);

            var catastrophe = new CatastrophicEvent
            {
                Type = type,
                EpicenterLocation = location,
                Intensity = intensity,
                AffectedRadius = radius,
                Duration = duration,
                TimeRemaining = duration,
                Effects = new Dictionary<string, float>(config.Effects),
                IsActive = true,
                RecoveryTime = duration * 2f // Recovery takes twice as long as the event
            };

            // Scale effects by intensity
            foreach (var effectKey in catastrophe.Effects.Keys.ToList())
            {
                catastrophe.Effects[effectKey] *= intensity;
            }

            activeEvents.Add(catastrophe);

            if (!regionalEvents.ContainsKey(location))
                regionalEvents[location] = new List<CatastrophicEvent>();
            regionalEvents[location].Add(catastrophe);

            eventHistory[type] = 1.0f; // Mark as recently occurred

            ApplyCatastropheEffects(catastrophe);
            OnCatastropheTriggered?.Invoke(catastrophe);

            Debug.Log($"💥 {type} triggered at {location} (intensity: {intensity:F2}, radius: {radius:F1})");
        }

        private Vector2 SelectCatastropheLocation(CatastropheType type)
        {
            var config = eventConfigs[type];

            // Find suitable biomes for this catastrophe type
            var suitableLocations = new List<Vector2>();

            if (biomeSystem != null)
            {
                for (int x = -50; x <= 50; x += 10)
                {
                    for (int y = -50; y <= 50; y += 10)
                    {
                        var location = new Vector2(x, y);
                        var biome = biomeSystem.GetBiomeAtLocation(location);

                        float biomeModifier = config.BiomeModifier.GetValueOrDefault(biome, 1.0f);
                        if (biomeModifier > 0.5f) // Only consider locations where this event is reasonably likely
                        {
                            // Add multiple entries for higher probability locations
                            int weight = Mathf.RoundToInt(biomeModifier * 10);
                            for (int i = 0; i < weight; i++)
                            {
                                suitableLocations.Add(location);
                            }
                        }
                    }
                }
            }

            if (suitableLocations.Count > 0)
            {
                return suitableLocations[Random.Range(0, suitableLocations.Count)];
            }

            // Fallback to random location
            return new Vector2(Random.Range(-50f, 50f), Random.Range(-50f, 50f));
        }

        private void UpdateActiveCatastrophes()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                var catastrophe = activeEvents[i];
                catastrophe.TimeRemaining -= catastropheCheckInterval;

                if (catastrophe.TimeRemaining <= 0f)
                {
                    EndCatastrophe(catastrophe);
                    activeEvents.RemoveAt(i);
                }
                else
                {
                    // Continue applying effects
                    ApplyCatastropheEffects(catastrophe);
                    activeEvents[i] = catastrophe;
                }
            }
        }

        private void ApplyCatastropheEffects(CatastrophicEvent catastrophe)
        {
            ApplyResourceEffects(catastrophe);
            ApplySpeciesEffects(catastrophe);
            ApplyBiomeEffects(catastrophe);
            ApplyClimateEffects(catastrophe);
        }

        private void ApplyResourceEffects(CatastrophicEvent catastrophe)
        {
            if (resourceSystem == null) return;

            var affectedLocations = GetAffectedLocations(catastrophe);

            foreach (var location in affectedLocations)
            {
                float distanceModifier = CalculateDistanceModifier(location, catastrophe);

                if (catastrophe.Effects.ContainsKey("WaterAvailability"))
                {
                    float effect = catastrophe.Effects["WaterAvailability"] * distanceModifier;
                    // Would apply to water resources at this location
                }

                if (catastrophe.Effects.ContainsKey("FoodAvailability"))
                {
                    float effect = catastrophe.Effects["FoodAvailability"] * distanceModifier;
                    // Would apply to food resources at this location
                }

                if (catastrophe.Effects.ContainsKey("SoilEnrichment"))
                {
                    float effect = catastrophe.Effects["SoilEnrichment"] * distanceModifier;
                    // Positive effect - volcanic ash enriches soil
                }
            }
        }

        private void ApplySpeciesEffects(CatastrophicEvent catastrophe)
        {
            if (speciesSystem == null) return;

            var affectedLocations = GetAffectedLocations(catastrophe);

            foreach (var location in affectedLocations)
            {
                float distanceModifier = CalculateDistanceModifier(location, catastrophe);

                if (catastrophe.Effects.ContainsKey("PopulationHealth"))
                {
                    float effect = catastrophe.Effects["PopulationHealth"] * distanceModifier;
                    // Would affect all species in this location
                }

                if (catastrophe.Effects.ContainsKey("Displacement"))
                {
                    float effect = catastrophe.Effects["Displacement"] * distanceModifier;
                    // Would trigger migration away from affected area
                }
            }
        }

        private void ApplyBiomeEffects(CatastrophicEvent catastrophe)
        {
            if (biomeSystem == null) return;

            var affectedLocations = GetAffectedLocations(catastrophe);

            foreach (var location in affectedLocations)
            {
                float distanceModifier = CalculateDistanceModifier(location, catastrophe);

                if (catastrophe.Effects.ContainsKey("VegetationDestruction"))
                {
                    float effect = catastrophe.Effects["VegetationDestruction"] * distanceModifier;
                    // Would accelerate biome transitions or create wasteland
                }

                if (catastrophe.Effects.ContainsKey("HabitatDestruction"))
                {
                    float effect = catastrophe.Effects["HabitatDestruction"] * distanceModifier;
                    // Would reduce habitat quality
                }
            }
        }

        private void ApplyClimateEffects(CatastrophicEvent catastrophe)
        {
            if (climateSystem == null) return;

            if (catastrophe.Effects.ContainsKey("ClimateImpact"))
            {
                float effect = catastrophe.Effects["ClimateImpact"];
                // Would affect global climate temporarily
            }

            if (catastrophe.Effects.ContainsKey("AirQuality"))
            {
                float effect = catastrophe.Effects["AirQuality"];
                // Would create air pollution or ash clouds
            }
        }

        private List<Vector2> GetAffectedLocations(CatastrophicEvent catastrophe)
        {
            var locations = new List<Vector2>();
            float radius = catastrophe.AffectedRadius;

            for (int x = -50; x <= 50; x += 10)
            {
                for (int y = -50; y <= 50; y += 10)
                {
                    var location = new Vector2(x, y);
                    float distance = Vector2.Distance(location, catastrophe.EpicenterLocation);

                    if (distance <= radius)
                    {
                        locations.Add(location);
                    }
                }
            }

            return locations;
        }

        private float CalculateDistanceModifier(Vector2 location, CatastrophicEvent catastrophe)
        {
            float distance = Vector2.Distance(location, catastrophe.EpicenterLocation);
            float normalizedDistance = distance / catastrophe.AffectedRadius;
            return Mathf.Max(0f, 1f - normalizedDistance);
        }

        private void EndCatastrophe(CatastrophicEvent catastrophe)
        {
            catastrophe.IsActive = false;

            // Remove from regional events
            if (regionalEvents.TryGetValue(catastrophe.EpicenterLocation, out var events))
            {
                events.Remove(catastrophe);
                if (events.Count == 0)
                {
                    regionalEvents.Remove(catastrophe.EpicenterLocation);
                }
            }

            OnCatastropheEnded?.Invoke(catastrophe);
            Debug.Log($"💥 {catastrophe.Type} ended at {catastrophe.EpicenterLocation}");

            // Start recovery process
            if (enableRecovery)
            {
                StartCoroutine(ProcessCatastropheRecovery(catastrophe));
            }
        }

        private void ProcessRecovery()
        {
            // Gradually reduce event history values
            foreach (var eventType in eventHistory.Keys.ToList())
            {
                eventHistory[eventType] = Mathf.Max(0f, eventHistory[eventType] - Time.deltaTime * 0.001f);
            }
        }

        private IEnumerator ProcessCatastropheRecovery(CatastrophicEvent catastrophe)
        {
            float recoveryProgress = 0f;
            float recoveryRate = baseRecoveryRate;

            // Apply ecosystem resilience bonus
            if (healthMonitor != null)
            {
                var health = healthMonitor.GetCurrentHealth();
                recoveryRate += health.OverallHealthScore * resilienceBonus;
            }

            while (recoveryProgress < 1f)
            {
                recoveryProgress += recoveryRate * Time.deltaTime / catastrophe.RecoveryTime;
                recoveryProgress = Mathf.Clamp01(recoveryProgress);

                ApplyRecoveryEffects(catastrophe, recoveryProgress);
                OnRecoveryProgress?.Invoke(catastrophe.EpicenterLocation, catastrophe.Type, recoveryProgress);

                yield return null;
            }

            Debug.Log($"🌱 Recovery from {catastrophe.Type} completed at {catastrophe.EpicenterLocation}");
        }

        private void ApplyRecoveryEffects(CatastrophicEvent catastrophe, float progress)
        {
            // Gradually reverse catastrophe effects
            var affectedLocations = GetAffectedLocations(catastrophe);

            foreach (var location in affectedLocations)
            {
                float distanceModifier = CalculateDistanceModifier(location, catastrophe);
                float recoveryModifier = progress * distanceModifier;

                // Apply positive recovery effects
                if (enableAdaptiveRecovery)
                {
                    ApplyAdaptiveRecovery(location, catastrophe, recoveryModifier);
                }
            }
        }

        private void ApplyAdaptiveRecovery(Vector2 location, CatastrophicEvent catastrophe, float recoveryModifier)
        {
            // Some catastrophes can lead to positive long-term changes
            switch (catastrophe.Type)
            {
                case CatastropheType.Wildfire:
                    // Fire can clear undergrowth and promote new growth
                    // Would enhance soil nutrients and biodiversity in the long term
                    break;

                case CatastropheType.VolcanicEruption:
                    // Volcanic ash enriches soil
                    // Would improve soil quality over time
                    break;

                case CatastropheType.Flood:
                    // Floods can deposit fertile sediments
                    // Would improve soil quality in some areas
                    break;
            }
        }

        public List<CatastrophicEvent> GetActiveEvents() => new List<CatastrophicEvent>(activeEvents);

        public Dictionary<Vector2, List<CatastrophicEvent>> GetRegionalEvents() =>
            new Dictionary<Vector2, List<CatastrophicEvent>>(regionalEvents);

        public Dictionary<CatastropheType, float> GetEventHistory() =>
            new Dictionary<CatastropheType, float>(eventHistory);

        public void TriggerCatastropheAt(CatastropheType type, Vector2 location, float intensity = 1.0f)
        {
            if (!eventConfigs.TryGetValue(type, out var config)) return;

            var catastrophe = new CatastrophicEvent
            {
                Type = type,
                EpicenterLocation = location,
                Intensity = Mathf.Clamp01(intensity),
                AffectedRadius = Random.Range(config.MinRadius, config.MaxRadius),
                Duration = Random.Range(config.MinDuration, config.MaxDuration),
                TimeRemaining = Random.Range(config.MinDuration, config.MaxDuration),
                Effects = new Dictionary<string, float>(config.Effects),
                IsActive = true,
                RecoveryTime = Random.Range(config.MinDuration, config.MaxDuration) * 2f
            };

            activeEvents.Add(catastrophe);
            ApplyCatastropheEffects(catastrophe);
            OnCatastropheTriggered?.Invoke(catastrophe);

            Debug.Log($"💥 Manually triggered {type} at {location}");
        }

        public void SetCatastropheProbability(CatastropheType type, float probability)
        {
            if (eventConfigs.TryGetValue(type, out var config))
            {
                config.BaseProbability = Mathf.Clamp01(probability);
                eventConfigs[type] = config;
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }

    [System.Serializable]
    public class CatastropheConfiguration
    {
        public CatastropheType Type;
        public float BaseProbability = 0.01f;
        public float MinIntensity = 0.1f;
        public float MaxIntensity = 1.0f;
        public float MinDuration = 300f;
        public float MaxDuration = 1800f;
        public float MinRadius = 10f;
        public float MaxRadius = 50f;
        public Dictionary<SeasonType, float> SeasonalModifier = new();
        public Dictionary<BiomeType, float> BiomeModifier = new();
        public Dictionary<string, float> Effects = new();
    }
}