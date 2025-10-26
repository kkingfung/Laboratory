using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Ecosystem.Data;
using EcoBiomeType = Laboratory.Chimera.Core.BiomeType;

namespace Laboratory.Chimera.Ecosystem.Systems
{
    /// <summary>
    /// Manages biome evolution and transitions based on environmental conditions
    /// </summary>
    public class BiomeTransitionSystem : MonoBehaviour
    {
        [Header("Transition Configuration")]
        [SerializeField] private float transitionCheckInterval = 10.0f;
        [SerializeField] private float baseTransitionRate = 0.01f;
        [SerializeField] private bool enableBiomeEvolution = true;
        [SerializeField] private float environmentalSensitivity = 1.0f;

        [Header("Biome Stability")]
        [SerializeField] private float stabilityThreshold = 0.7f;
        [SerializeField] private float transitionResistance = 0.3f;
        [SerializeField] private bool enableSuccession = true;

        private Dictionary<Vector2, EcoBiomeType> currentBiomes = new();
        private Dictionary<Vector2, BiomeTransition> activeTransitions = new();
        private Dictionary<EcoBiomeType, Dictionary<EcoBiomeType, float>> transitionProbabilities = new();

        // Dependencies
        private ClimateEvolutionSystem climateSystem;

        // Events
        public System.Action<Vector2, EcoBiomeType, EcoBiomeType> OnBiomeTransitionStarted;
        public System.Action<Vector2, EcoBiomeType> OnBiomeTransitionCompleted;
        public System.Action<Vector2, float> OnBiomeStabilityChanged;

        private void Awake()
        {
            climateSystem = FindObjectOfType<ClimateEvolutionSystem>();
            InitializeTransitionProbabilities();
        }

        private void Start()
        {
            InitializeBiomeMap();
            StartCoroutine(BiomeTransitionLoop());
        }

        private void InitializeTransitionProbabilities()
        {
            transitionProbabilities = new Dictionary<EcoBiomeType, Dictionary<EcoBiomeType, float>>
            {
                [EcoBiomeType.Forest] = new Dictionary<EcoBiomeType, float>
                {
                    [EcoBiomeType.Grassland] = 0.1f,
                    [EcoBiomeType.Desert] = 0.05f,
                    [EcoBiomeType.Swamp] = 0.08f,
                    [EcoBiomeType.Tropical] = 0.15f
                },
                [EcoBiomeType.Grassland] = new Dictionary<EcoBiomeType, float>
                {
                    [EcoBiomeType.Forest] = 0.12f,
                    [EcoBiomeType.Desert] = 0.08f,
                    [EcoBiomeType.Grassland] = 0.1f,
                    [EcoBiomeType.Swamp] = 0.05f
                },
                [EcoBiomeType.Desert] = new Dictionary<EcoBiomeType, float>
                {
                    [EcoBiomeType.Grassland] = 0.06f,
                    [EcoBiomeType.Grassland] = 0.08f,
                    [EcoBiomeType.Desert] = 0.15f
                },
                [EcoBiomeType.Tundra] = new Dictionary<EcoBiomeType, float>
                {
                    [EcoBiomeType.Temperate] = 0.1f,
                    [EcoBiomeType.Grassland] = 0.05f,
                    [EcoBiomeType.Mountain] = 0.03f
                },
                [EcoBiomeType.Ocean] = new Dictionary<EcoBiomeType, float>
                {
                    [EcoBiomeType.Swamp] = 0.02f,
                    [EcoBiomeType.Ocean] = 0.1f
                },
                [EcoBiomeType.Mountain] = new Dictionary<EcoBiomeType, float>
                {
                    [EcoBiomeType.Tundra] = 0.05f,
                    [EcoBiomeType.Forest] = 0.08f,
                    [EcoBiomeType.Mountain] = 0.1f
                },
                [EcoBiomeType.Swamp] = new Dictionary<EcoBiomeType, float>
                {
                    [EcoBiomeType.Forest] = 0.1f,
                    [EcoBiomeType.Grassland] = 0.08f,
                    [EcoBiomeType.Ocean] = 0.05f
                },
                [EcoBiomeType.Volcanic] = new Dictionary<EcoBiomeType, float>
                {
                    [EcoBiomeType.Mountain] = 0.2f,
                    [EcoBiomeType.Desert] = 0.1f,
                    [EcoBiomeType.Void] = 0.3f
                },
                [EcoBiomeType.Tropical] = new Dictionary<EcoBiomeType, float>
                {
                    [EcoBiomeType.Forest] = 0.08f,
                    [EcoBiomeType.Swamp] = 0.1f,
                    [EcoBiomeType.Tropical] = 0.15f
                },
                [EcoBiomeType.Grassland] = new Dictionary<EcoBiomeType, float>
                {
                    [EcoBiomeType.Grassland] = 0.1f,
                    [EcoBiomeType.Desert] = 0.08f,
                    [EcoBiomeType.Forest] = 0.06f
                },
                [EcoBiomeType.Temperate] = new Dictionary<EcoBiomeType, float>
                {
                    [EcoBiomeType.Forest] = 0.1f,
                    [EcoBiomeType.Tundra] = 0.08f,
                    [EcoBiomeType.Mountain] = 0.05f
                }
            };

            UnityEngine.Debug.Log("🌿 Biome transition probabilities initialized");
        }

        private void InitializeBiomeMap()
        {
            // Initialize with a basic biome distribution
            // In a real implementation, this would load from world data
            for (int x = -50; x <= 50; x += 10)
            {
                for (int y = -50; y <= 50; y += 10)
                {
                    var location = new Vector2(x, y);
                    var biome = DetermineInitialBiome(location);
                    currentBiomes[location] = biome;
                }
            }

            UnityEngine.Debug.Log($"🗺️ Initialized biome map with {currentBiomes.Count} regions");
        }

        private EcoBiomeType DetermineInitialBiome(Vector2 location)
        {
            // Simple biome determination based on location
            float distance = location.magnitude;
            float angle = Mathf.Atan2(location.y, location.x) * Mathf.Rad2Deg;

            if (distance < 20f)
            {
                return Random.value < 0.6f ? EcoBiomeType.Forest : EcoBiomeType.Grassland;
            }
            else if (distance < 40f)
            {
                if (angle > 45f && angle < 135f) return EcoBiomeType.Mountain;
                if (angle > -135f && angle < -45f) return EcoBiomeType.Desert;
                return EcoBiomeType.Grassland;
            }
            else
            {
                if (location.y > 30f) return EcoBiomeType.Tundra;
                if (location.y < -30f) return EcoBiomeType.Ocean;
                return Random.value < 0.5f ? EcoBiomeType.Desert : EcoBiomeType.Grassland;
            }
        }

        private IEnumerator BiomeTransitionLoop()
        {
            while (enableBiomeEvolution)
            {
                CheckForTransitions();
                UpdateActiveTransitions();
                yield return new WaitForSeconds(transitionCheckInterval);
            }
        }

        private void CheckForTransitions()
        {
            foreach (var kvp in currentBiomes.ToList())
            {
                var location = kvp.Key;
                var currentBiome = kvp.Value;

                if (activeTransitions.ContainsKey(location)) continue;

                var environmentalConditions = GetEnvironmentalConditions(location);
                var stability = CalculateBiomeStability(currentBiome, environmentalConditions);

                OnBiomeStabilityChanged?.Invoke(location, stability);

                if (stability < stabilityThreshold)
                {
                    var potentialTransition = DeterminePotentialTransition(currentBiome, environmentalConditions);
                    if (potentialTransition.HasValue)
                    {
                        StartBiomeTransition(location, currentBiome, potentialTransition.Value);
                    }
                }
            }
        }

        private EcosystemState GetEnvironmentalConditions(Vector2 location)
        {
            // Get current climate data
            var climate = climateSystem?.GetCurrentClimate() ?? new ClimateData();
            var season = climateSystem?.GetCurrentSeason() ?? SeasonType.Spring;

            // Calculate local conditions based on location and climate
            float temperatureModifier = CalculateLocationTemperatureModifier(location);
            float humidityModifier = CalculateLocationHumidityModifier(location);

            return new EcosystemState
            {
                Temperature = climate.GlobalTemperature + temperatureModifier,
                Humidity = 0.5f + humidityModifier,
                Rainfall = GetSeasonalRainfall(season),
                SoilQuality = Random.Range(0.3f, 0.9f),
                Biodiversity = Random.Range(0.4f, 0.8f),
                Stability = Random.Range(0.5f, 1.0f),
                CurrentSeason = season,
                SeasonProgress = climateSystem?.GetSeasonProgress() ?? 0f,
                ClimateZone = DetermineClimateZone(location),
                LastUpdate = System.DateTime.Now
            };
        }

        private float CalculateLocationTemperatureModifier(Vector2 location)
        {
            float latitudeEffect = -Mathf.Abs(location.y) * 0.1f;
            float altitudeEffect = location.magnitude > 30f ? -2f : 0f;
            return latitudeEffect + altitudeEffect;
        }

        private float CalculateLocationHumidityModifier(Vector2 location)
        {
            float oceanEffect = location.magnitude > 40f ? 0.2f : 0f;
            float mountainEffect = location.magnitude > 30f && Mathf.Abs(location.y) < 20f ? -0.3f : 0f;
            return oceanEffect + mountainEffect;
        }

        private float GetSeasonalRainfall(SeasonType season)
        {
            return season switch
            {
                SeasonType.Spring => 0.7f,
                SeasonType.Summer => 0.4f,
                SeasonType.Autumn => 0.6f,
                SeasonType.Winter => 0.5f,
                _ => 0.5f
            };
        }

        private ClimateType DetermineClimateZone(Vector2 location)
        {
            float distance = location.magnitude;
            if (distance < 20f) return ClimateType.Temperate;
            if (location.y > 30f) return ClimateType.Polar;
            if (location.y < -30f && distance > 40f) return ClimateType.Oceanic;
            if (distance > 35f && Mathf.Abs(location.y) < 15f) return ClimateType.Arid;
            return ClimateType.Continental;
        }

        private float CalculateBiomeStability(EcoBiomeType biome, EcosystemState conditions)
        {
            float temperatureStability = GetTemperatureStability(biome, conditions.Temperature);
            float humidityStability = GetHumidityStability(biome, conditions.Humidity);
            float rainfallStability = GetRainfallStability(biome, conditions.Rainfall);

            float baseStability = (temperatureStability + humidityStability + rainfallStability) / 3f;
            float biodiversityModifier = conditions.Biodiversity * 0.2f;
            float soilQualityModifier = conditions.SoilQuality * 0.1f;

            return Mathf.Clamp01(baseStability + biodiversityModifier + soilQualityModifier);
        }

        private float GetTemperatureStability(EcoBiomeType biome, float temperature)
        {
            var optimalRanges = new Dictionary<EcoBiomeType, Vector2>
            {
                [EcoBiomeType.Tundra] = new Vector2(-10f, 5f),
                [EcoBiomeType.Temperate] = new Vector2(-5f, 10f),
                [EcoBiomeType.Forest] = new Vector2(5f, 25f),
                [EcoBiomeType.Grassland] = new Vector2(10f, 30f),
                [EcoBiomeType.Desert] = new Vector2(20f, 45f),
                [EcoBiomeType.Tropical] = new Vector2(20f, 35f),
                [EcoBiomeType.Mountain] = new Vector2(-5f, 15f),
                [EcoBiomeType.Ocean] = new Vector2(0f, 25f),
                [EcoBiomeType.Swamp] = new Vector2(15f, 30f)
            };

            if (optimalRanges.TryGetValue(biome, out var range))
            {
                if (temperature >= range.x && temperature <= range.y)
                    return 1.0f;

                float distance = Mathf.Min(Mathf.Abs(temperature - range.x), Mathf.Abs(temperature - range.y));
                return Mathf.Max(0f, 1f - distance * 0.1f);
            }

            return 0.5f;
        }

        private float GetHumidityStability(EcoBiomeType biome, float humidity)
        {
            var optimalHumidity = new Dictionary<EcoBiomeType, float>
            {
                [EcoBiomeType.Desert] = 0.2f,
                [EcoBiomeType.Tundra] = 0.4f,
                [EcoBiomeType.Grassland] = 0.5f,
                [EcoBiomeType.Forest] = 0.7f,
                [EcoBiomeType.Tropical] = 0.9f,
                [EcoBiomeType.Swamp] = 0.95f,
                [EcoBiomeType.Ocean] = 0.8f
            };

            if (optimalHumidity.TryGetValue(biome, out var optimal))
            {
                float difference = Mathf.Abs(humidity - optimal);
                return Mathf.Max(0f, 1f - difference * 2f);
            }

            return 0.5f;
        }

        private float GetRainfallStability(EcoBiomeType biome, float rainfall)
        {
            var optimalRainfall = new Dictionary<EcoBiomeType, float>
            {
                [EcoBiomeType.Desert] = 0.1f,
                [EcoBiomeType.Tundra] = 0.3f,
                [EcoBiomeType.Grassland] = 0.5f,
                [EcoBiomeType.Forest] = 0.7f,
                [EcoBiomeType.Tropical] = 0.9f,
                [EcoBiomeType.Swamp] = 0.8f
            };

            if (optimalRainfall.TryGetValue(biome, out var optimal))
            {
                float difference = Mathf.Abs(rainfall - optimal);
                return Mathf.Max(0f, 1f - difference * 1.5f);
            }

            return 0.5f;
        }

        private EcoBiomeType? DeterminePotentialTransition(EcoBiomeType currentBiome, EcosystemState conditions)
        {
            if (!transitionProbabilities.TryGetValue(currentBiome, out var possibleTransitions))
                return null;

            EcoBiomeType? bestTransition = null;
            float bestStability = 0f;

            foreach (var transition in possibleTransitions)
            {
                var targetBiome = transition.Key;
                var baseProbability = transition.Value;

                float targetStability = CalculateBiomeStability(targetBiome, conditions);
                float adjustedProbability = baseProbability * targetStability * environmentalSensitivity;

                if (adjustedProbability > bestStability && Random.value < adjustedProbability)
                {
                    bestStability = adjustedProbability;
                    bestTransition = targetBiome;
                }
            }

            return bestTransition;
        }

        private void StartBiomeTransition(Vector2 location, EcoBiomeType fromBiome, EcoBiomeType toBiome)
        {
            var transition = new BiomeTransition
            {
                FromBiome = fromBiome,
                ToBiome = toBiome,
                TransitionRate = baseTransitionRate,
                RequiredTime = Random.Range(300f, 1800f), // 5-30 minutes
                Progress = 0f,
                RequiredConditions = GetTransitionRequirements(fromBiome, toBiome),
                IsActive = true
            };

            activeTransitions[location] = transition;
            OnBiomeTransitionStarted?.Invoke(location, fromBiome, toBiome);

            UnityEngine.Debug.Log($"🌱 Biome transition started at {location}: {fromBiome} → {toBiome}");
        }

        private List<string> GetTransitionRequirements(EcoBiomeType from, EcoBiomeType to)
        {
            var requirements = new List<string>();

            // Example transition requirements
            if (to == EcoBiomeType.Forest)
                requirements.AddRange(new[] { "adequate_rainfall", "moderate_temperature", "soil_quality" });
            if (to == EcoBiomeType.Desert)
                requirements.AddRange(new[] { "low_rainfall", "high_temperature", "low_humidity" });
            if (to == EcoBiomeType.Tropical)
                requirements.AddRange(new[] { "high_rainfall", "high_humidity", "warm_temperature" });

            return requirements;
        }

        private void UpdateActiveTransitions()
        {
            foreach (var kvp in activeTransitions.ToList())
            {
                var location = kvp.Key;
                var transition = kvp.Value;

                transition.Progress += transition.TransitionRate * Time.deltaTime * transitionCheckInterval;

                if (transition.Progress >= 1.0f)
                {
                    CompleteBiomeTransition(location, transition);
                }
                else
                {
                    activeTransitions[location] = transition;
                }
            }
        }

        private void CompleteBiomeTransition(Vector2 location, BiomeTransition transition)
        {
            currentBiomes[location] = transition.ToBiome;
            activeTransitions.Remove(location);

            OnBiomeTransitionCompleted?.Invoke(location, transition.ToBiome);
            UnityEngine.Debug.Log($"🌿 Biome transition completed at {location}: {transition.FromBiome} → {transition.ToBiome}");
        }

        public EcoBiomeType GetBiomeAtLocation(Vector2 location)
        {
            return currentBiomes.GetValueOrDefault(location, EcoBiomeType.Grassland);
        }

        public Dictionary<EcoBiomeType, int> GetBiomeDistribution()
        {
            var distribution = new Dictionary<EcoBiomeType, int>();
            foreach (var biome in currentBiomes.Values)
            {
                distribution[biome] = distribution.GetValueOrDefault(biome, 0) + 1;
            }
            return distribution;
        }

        public List<BiomeTransition> GetActiveTransitions()
        {
            return activeTransitions.Values.ToList();
        }

        public void ForceTransition(Vector2 location, EcoBiomeType toBiome)
        {
            if (currentBiomes.TryGetValue(location, out var fromBiome))
            {
                StartBiomeTransition(location, fromBiome, toBiome);
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}