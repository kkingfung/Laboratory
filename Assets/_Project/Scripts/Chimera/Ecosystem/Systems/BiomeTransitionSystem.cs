using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Ecosystem.Data;
using Laboratory.Chimera.Ecosystem.Core;
using Laboratory.Shared.Types;

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

        private Dictionary<Vector2, BiomeType> currentBiomes = new();
        private Dictionary<Vector2, BiomeTransition> activeTransitions = new();
        private Dictionary<BiomeType, Dictionary<BiomeType, float>> transitionProbabilities = new();

        // Dependencies
        private ClimateEvolutionSystem climateSystem;

        // Events
        public System.Action<Vector2, BiomeType, BiomeType> OnBiomeTransitionStarted;
        public System.Action<Vector2, BiomeType> OnBiomeTransitionCompleted;
        public System.Action<Vector2, float> OnBiomeStabilityChanged;

        private void Awake()
        {
            EcosystemServiceLocator.RegisterBiome(this);
            InitializeTransitionProbabilities();
        }

        private void Start()
        {
            climateSystem = EcosystemServiceLocator.Climate;
            InitializeBiomeMap();
            StartCoroutine(BiomeTransitionLoop());
        }

        private void InitializeTransitionProbabilities()
        {
            transitionProbabilities = new Dictionary<BiomeType, Dictionary<BiomeType, float>>
            {
                [BiomeType.Forest] = new Dictionary<BiomeType, float>
                {
                    [BiomeType.Grassland] = 0.1f,
                    [BiomeType.Desert] = 0.05f,
                    [BiomeType.Swamp] = 0.08f,
                    [BiomeType.Tropical] = 0.15f
                },
                [BiomeType.Grassland] = new Dictionary<BiomeType, float>
                {
                    [BiomeType.Forest] = 0.12f,
                    [BiomeType.Desert] = 0.08f,
                    [BiomeType.Grassland] = 0.1f,
                    [BiomeType.Swamp] = 0.05f
                },
                [BiomeType.Desert] = new Dictionary<BiomeType, float>
                {
                    [BiomeType.Grassland] = 0.06f,
                    [BiomeType.Grassland] = 0.08f,
                    [BiomeType.Desert] = 0.15f
                },
                [BiomeType.Tundra] = new Dictionary<BiomeType, float>
                {
                    [BiomeType.Temperate] = 0.1f,
                    [BiomeType.Grassland] = 0.05f,
                    [BiomeType.Mountain] = 0.03f
                },
                [BiomeType.Ocean] = new Dictionary<BiomeType, float>
                {
                    [BiomeType.Swamp] = 0.02f,
                    [BiomeType.Ocean] = 0.1f
                },
                [BiomeType.Mountain] = new Dictionary<BiomeType, float>
                {
                    [BiomeType.Tundra] = 0.05f,
                    [BiomeType.Forest] = 0.08f,
                    [BiomeType.Mountain] = 0.1f
                },
                [BiomeType.Swamp] = new Dictionary<BiomeType, float>
                {
                    [BiomeType.Forest] = 0.1f,
                    [BiomeType.Grassland] = 0.08f,
                    [BiomeType.Ocean] = 0.05f
                },
                [BiomeType.Volcanic] = new Dictionary<BiomeType, float>
                {
                    [BiomeType.Mountain] = 0.2f,
                    [BiomeType.Desert] = 0.1f,
                    [BiomeType.Void] = 0.3f
                },
                [BiomeType.Tropical] = new Dictionary<BiomeType, float>
                {
                    [BiomeType.Forest] = 0.08f,
                    [BiomeType.Swamp] = 0.1f,
                    [BiomeType.Tropical] = 0.15f
                },
                [BiomeType.Grassland] = new Dictionary<BiomeType, float>
                {
                    [BiomeType.Grassland] = 0.1f,
                    [BiomeType.Desert] = 0.08f,
                    [BiomeType.Forest] = 0.06f
                },
                [BiomeType.Temperate] = new Dictionary<BiomeType, float>
                {
                    [BiomeType.Forest] = 0.1f,
                    [BiomeType.Tundra] = 0.08f,
                    [BiomeType.Mountain] = 0.05f
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

        private BiomeType DetermineInitialBiome(Vector2 location)
        {
            // Simple biome determination based on location
            float distance = location.magnitude;
            float angle = Mathf.Atan2(location.y, location.x) * Mathf.Rad2Deg;

            if (distance < 20f)
            {
                return Random.value < 0.6f ? BiomeType.Forest : BiomeType.Grassland;
            }
            else if (distance < 40f)
            {
                if (angle > 45f && angle < 135f) return BiomeType.Mountain;
                if (angle > -135f && angle < -45f) return BiomeType.Desert;
                return BiomeType.Grassland;
            }
            else
            {
                if (location.y > 30f) return BiomeType.Tundra;
                if (location.y < -30f) return BiomeType.Ocean;
                return Random.value < 0.5f ? BiomeType.Desert : BiomeType.Grassland;
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

        private EnvironmentalState GetEnvironmentalConditions(Vector2 location)
        {
            // Get current climate data
            var climate = climateSystem?.GetCurrentClimate() ?? new ClimateData();
            var season = climateSystem?.GetCurrentSeason() ?? SeasonType.Spring;

            // Calculate local conditions based on location and climate
            float temperatureModifier = CalculateLocationTemperatureModifier(location);
            float humidityModifier = CalculateLocationHumidityModifier(location);

            return new EnvironmentalState
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

        private float CalculateBiomeStability(BiomeType biome, EnvironmentalState conditions)
        {
            float temperatureStability = GetTemperatureStability(biome, conditions.Temperature);
            float humidityStability = GetHumidityStability(biome, conditions.Humidity);
            float rainfallStability = GetRainfallStability(biome, conditions.Rainfall);

            float baseStability = (temperatureStability + humidityStability + rainfallStability) / 3f;
            float biodiversityModifier = conditions.Biodiversity * 0.2f;
            float soilQualityModifier = conditions.SoilQuality * 0.1f;

            return Mathf.Clamp01(baseStability + biodiversityModifier + soilQualityModifier);
        }

        private float GetTemperatureStability(BiomeType biome, float temperature)
        {
            var optimalRanges = new Dictionary<BiomeType, Vector2>
            {
                [BiomeType.Tundra] = new Vector2(-10f, 5f),
                [BiomeType.Temperate] = new Vector2(-5f, 10f),
                [BiomeType.Forest] = new Vector2(5f, 25f),
                [BiomeType.Grassland] = new Vector2(10f, 30f),
                [BiomeType.Desert] = new Vector2(20f, 45f),
                [BiomeType.Tropical] = new Vector2(20f, 35f),
                [BiomeType.Mountain] = new Vector2(-5f, 15f),
                [BiomeType.Ocean] = new Vector2(0f, 25f),
                [BiomeType.Swamp] = new Vector2(15f, 30f)
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

        private float GetHumidityStability(BiomeType biome, float humidity)
        {
            var optimalHumidity = new Dictionary<BiomeType, float>
            {
                [BiomeType.Desert] = 0.2f,
                [BiomeType.Tundra] = 0.4f,
                [BiomeType.Grassland] = 0.5f,
                [BiomeType.Forest] = 0.7f,
                [BiomeType.Tropical] = 0.9f,
                [BiomeType.Swamp] = 0.95f,
                [BiomeType.Ocean] = 0.8f
            };

            if (optimalHumidity.TryGetValue(biome, out var optimal))
            {
                float difference = Mathf.Abs(humidity - optimal);
                return Mathf.Max(0f, 1f - difference * 2f);
            }

            return 0.5f;
        }

        private float GetRainfallStability(BiomeType biome, float rainfall)
        {
            var optimalRainfall = new Dictionary<BiomeType, float>
            {
                [BiomeType.Desert] = 0.1f,
                [BiomeType.Tundra] = 0.3f,
                [BiomeType.Grassland] = 0.5f,
                [BiomeType.Forest] = 0.7f,
                [BiomeType.Tropical] = 0.9f,
                [BiomeType.Swamp] = 0.8f
            };

            if (optimalRainfall.TryGetValue(biome, out var optimal))
            {
                float difference = Mathf.Abs(rainfall - optimal);
                return Mathf.Max(0f, 1f - difference * 1.5f);
            }

            return 0.5f;
        }

        private BiomeType? DeterminePotentialTransition(BiomeType currentBiome, EnvironmentalState conditions)
        {
            if (!transitionProbabilities.TryGetValue(currentBiome, out var possibleTransitions))
                return null;

            BiomeType? bestTransition = null;
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

        private void StartBiomeTransition(Vector2 location, BiomeType fromBiome, BiomeType toBiome)
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

        private List<string> GetTransitionRequirements(BiomeType from, BiomeType to)
        {
            var requirements = new List<string>();

            // Example transition requirements
            if (to == BiomeType.Forest)
                requirements.AddRange(new[] { "adequate_rainfall", "moderate_temperature", "soil_quality" });
            if (to == BiomeType.Desert)
                requirements.AddRange(new[] { "low_rainfall", "high_temperature", "low_humidity" });
            if (to == BiomeType.Tropical)
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

        public BiomeType GetBiomeAtLocation(Vector2 location)
        {
            return currentBiomes.GetValueOrDefault(location, BiomeType.Grassland);
        }

        public Dictionary<BiomeType, int> GetBiomeDistribution()
        {
            var distribution = new Dictionary<BiomeType, int>();
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

        public void ForceTransition(Vector2 location, BiomeType toBiome)
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