using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Enums;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.Shared.Types;

namespace Laboratory.Subsystems.Ecosystem
{
    /// <summary>
    /// Manages biome creation, environmental factors, and biome health for Project Chimera.
    /// Handles dynamic biome changes, trait modifiers, and environmental adaptation effects.
    /// </summary>
    public class BiomeManager : MonoBehaviour, IBiomeService
    {
        [Header("Configuration")]
        [SerializeField] private bool enableDynamicBiomes = true;
        [SerializeField] private bool enableBiomeHealth = true;
        [SerializeField] private bool enableTraitModifiers = true;

        private EcosystemSubsystemConfig _config;
        private readonly Dictionary<string, BiomeData> _activeBiomes = new();
        private readonly Dictionary<string, EnvironmentalFactors> _cachedEnvironmentalFactors = new();
        private float _lastUpdateTime = 0f;

        // Events
        public event Action<BiomeChangedEvent> OnBiomeChanged;

        // Properties
        public bool IsInitialized { get; private set; }
        public int ActiveBiomeCount => _activeBiomes.Count;

        #region Initialization

        public async Task InitializeAsync(EcosystemSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            enableDynamicBiomes = true; // Always enable for ecosystem simulation
            enableBiomeHealth = true;
            enableTraitModifiers = _config.EnableDebugLogging; // Enable for detailed simulation

            IsInitialized = true;
            await Task.CompletedTask;

            Debug.Log("[BiomeManager] Initialized successfully");
        }

        #endregion

        #region Core Biome Operations

        /// <summary>
        /// Creates a new biome from configuration
        /// </summary>
        public async Task<BiomeData> CreateBiomeAsync(BiomeConfig config)
        {
            if (!IsInitialized || config == null)
                return null;

            try
            {
                var biomeData = new BiomeData
                {
                    biomeId = config.biomeId,
                    biomeName = config.biomeName,
                    biomeType = config.biomeType,
                    currentTemperature = config.baseTemperature,
                    currentHumidity = config.baseHumidity,
                    healthIndex = 1.0f, // Perfect health initially
                    carryingCapacity = config.carryingCapacity,
                    currentCapacityUsed = 0f,
                    presentSpecies = new List<string>(config.nativeSpecies ?? new string[0]),
                    traits = config.traits ?? new BiomeTraits(),
                    lastUpdated = DateTime.UtcNow
                };

                // Initialize trait modifiers based on biome type
                InitializeBiomeTraitModifiers(biomeData);

                // Cache the biome
                _activeBiomes[biomeData.biomeId] = biomeData;

                // Generate initial environmental factors
                var environmentalFactors = GenerateEnvironmentalFactors(biomeData, null);
                _cachedEnvironmentalFactors[biomeData.biomeId] = environmentalFactors;

                await Task.CompletedTask;

                Debug.Log($"[BiomeManager] Created biome: {biomeData.biomeName} " +
                         $"(Type: {biomeData.biomeType}, Capacity: {biomeData.carryingCapacity})");

                // Fire biome creation event
                FireBiomeChangedEvent(biomeData, BiomeChangeType.SpeciesIntroduction, $"Biome {biomeData.biomeName} created");

                return biomeData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BiomeManager] Failed to create biome {config?.biomeId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets environmental factors for a biome
        /// </summary>
        public EnvironmentalFactors GetEnvironmentalFactors(BiomeData biome, WeatherData weather)
        {
            if (biome == null)
                return EnvironmentalFactors.CreateDefault();

            // Check cache first
            var cacheKey = $"{biome.biomeId}_{weather?.weatherType}_{weather?.intensity:F1}";
            if (_cachedEnvironmentalFactors.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            // Generate new environmental factors
            var factors = GenerateEnvironmentalFactors(biome, weather);
            _cachedEnvironmentalFactors[cacheKey] = factors;

            return factors;
        }

        /// <summary>
        /// Updates biome state based on populations and weather
        /// </summary>
        public void UpdateBiomes(float deltaTime, WeatherData weather, List<PopulationData> populations)
        {
            if (!IsInitialized || !enableDynamicBiomes)
                return;

            _lastUpdateTime += deltaTime;

            // Update each biome
            foreach (var biome in _activeBiomes.Values.ToArray())
            {
                UpdateBiome(biome, deltaTime, weather, populations);
            }

            // Clear cache periodically
            if (_lastUpdateTime >= _config.PerformanceConfig.CacheCleanupInterval)
            {
                ClearEnvironmentalFactorsCache();
                _lastUpdateTime = 0f;
            }
        }

        #endregion

        #region Biome Updates

        private void UpdateBiome(BiomeData biome, float deltaTime, WeatherData weather, List<PopulationData> populations)
        {
            var biomeBefore = CloneBiome(biome);
            bool biomeChanged = false;

            // Update temperature based on weather
            if (weather != null)
            {
                var targetTemperature = CalculateTargetTemperature(biome, weather);
                var temperatureDelta = (targetTemperature - biome.currentTemperature) * deltaTime * 0.1f;

                if (Mathf.Abs(temperatureDelta) > 0.1f)
                {
                    biome.currentTemperature += temperatureDelta;
                    biomeChanged = true;
                }

                // Update humidity
                var targetHumidity = CalculateTargetHumidity(biome, weather);
                var humidityDelta = (targetHumidity - biome.currentHumidity) * deltaTime * 0.1f;

                if (Mathf.Abs(humidityDelta) > 1f)
                {
                    biome.currentHumidity = Mathf.Clamp(biome.currentHumidity + humidityDelta, 0f, 100f);
                    biomeChanged = true;
                }
            }

            // Update carrying capacity usage
            var biomePopulations = populations?.Where(p => p.biomeId == biome.biomeId).ToList() ?? new List<PopulationData>();
            var newCapacityUsed = CalculateCapacityUsage(biome, biomePopulations);

            if (Mathf.Abs(newCapacityUsed - biome.currentCapacityUsed) > 0.01f)
            {
                biome.currentCapacityUsed = newCapacityUsed;
                biomeChanged = true;
            }

            // Update biome health
            if (enableBiomeHealth)
            {
                var newHealthIndex = CalculateBiomeHealth(biome, biomePopulations, weather);
                if (Mathf.Abs(newHealthIndex - biome.healthIndex) > 0.01f)
                {
                    biome.healthIndex = newHealthIndex;
                    biomeChanged = true;
                }
            }

            // Update present species list
            var currentSpecies = biomePopulations.Select(p => p.speciesId).Distinct().ToList();
            if (!biome.presentSpecies.SequenceEqual(currentSpecies))
            {
                var addedSpecies = currentSpecies.Except(biome.presentSpecies).ToList();
                var removedSpecies = biome.presentSpecies.Except(currentSpecies).ToList();

                biome.presentSpecies = currentSpecies;
                biomeChanged = true;

                // Fire specific events for species changes
                foreach (var species in addedSpecies)
                {
                    FireBiomeChangedEvent(biome, BiomeChangeType.SpeciesIntroduction, $"Species {species} introduced to {biome.biomeName}");
                }

                foreach (var species in removedSpecies)
                {
                    FireBiomeChangedEvent(biome, BiomeChangeType.SpeciesExtinction, $"Species {species} extinct in {biome.biomeName}");
                }
            }

            // Update timestamp and fire general change event if needed
            if (biomeChanged)
            {
                biome.lastUpdated = DateTime.UtcNow;

                // Determine the most significant change type
                var changeType = DeterminePrimaryChangeType(biomeBefore, biome);
                FireBiomeChangedEvent(biome, changeType, $"Biome {biome.biomeName} updated");

                // Invalidate cached environmental factors for this biome
                InvalidateBiomeCache(biome.biomeId);
            }
        }

        private float CalculateTargetTemperature(BiomeData biome, WeatherData weather)
        {
            var baseTemp = GetBiomeBaseTemperature(biome.biomeType);
            var seasonalAdjustment = GetSeasonalTemperatureAdjustment(weather?.currentSeason ?? Season.Spring);
            var weatherAdjustment = GetWeatherTemperatureAdjustment(weather?.weatherType ?? WeatherType.Sunny, weather?.intensity ?? 1f);

            return baseTemp + seasonalAdjustment + weatherAdjustment;
        }

        private float CalculateTargetHumidity(BiomeData biome, WeatherData weather)
        {
            var baseHumidity = GetBiomeBaseHumidity(biome.biomeType);
            var weatherHumidity = GetWeatherHumidityAdjustment(weather?.weatherType ?? WeatherType.Sunny, weather?.intensity ?? 1f);

            return Mathf.Clamp(baseHumidity + weatherHumidity, 0f, 100f);
        }

        private float CalculateCapacityUsage(BiomeData biome, List<PopulationData> populations)
        {
            if (biome.carryingCapacity <= 0)
                return 0f;

            var totalPopulation = populations.Sum(p => p.currentPopulation);
            return (float)totalPopulation / biome.carryingCapacity;
        }

        private float CalculateBiomeHealth(BiomeData biome, List<PopulationData> populations, WeatherData weather)
        {
            var health = 1.0f;

            // Carrying capacity stress
            if (biome.currentCapacityUsed > 1.0f)
            {
                health -= (biome.currentCapacityUsed - 1.0f) * 0.5f; // Overuse penalty
            }
            else if (biome.currentCapacityUsed < 0.3f)
            {
                health -= (0.3f - biome.currentCapacityUsed) * 0.2f; // Underuse penalty (ecosystem needs life)
            }

            // Species diversity bonus
            var speciesCount = biome.presentSpecies.Count;
            if (speciesCount >= 5)
            {
                health += 0.1f; // Diversity bonus
            }
            else if (speciesCount <= 2)
            {
                health -= 0.2f; // Low diversity penalty
            }

            // Weather stress
            if (weather != null)
            {
                var weatherStress = CalculateWeatherStress(biome, weather);
                health -= weatherStress * 0.3f;
            }

            // Population health impact
            if (populations.Any())
            {
                var avgPopulationHealth = populations.Average(p => p.healthIndex);
                health = (health + avgPopulationHealth) * 0.5f; // Blend biome and population health
            }

            return Mathf.Clamp01(health);
        }

        private float CalculateWeatherStress(BiomeData biome, WeatherData weather)
        {
            var stress = 0f;

            // Temperature stress
            var optimalTemp = GetBiomeBaseTemperature(biome.biomeType);
            var tempDeviation = Mathf.Abs(weather.temperature - optimalTemp) / 50f; // Normalize to 0-1
            stress += tempDeviation * 0.5f;

            // Weather type stress
            var weatherStress = GetBiomeWeatherStress(biome.biomeType, weather.weatherType, weather.intensity);
            stress += weatherStress;

            return Mathf.Clamp01(stress);
        }

        #endregion

        #region Environmental Factors Generation

        private EnvironmentalFactors GenerateEnvironmentalFactors(BiomeData biome, WeatherData weather)
        {
            var factors = EnvironmentalFactors.FromBiome(biome.biomeType);

            // Override with current biome data
            factors.temperature = biome.currentTemperature;
            factors.humidity = biome.currentHumidity;

            // Apply trait modifiers
            factors.foodAvailability = biome.traits.foodAbundance;
            factors.predatorPressure = biome.traits.predatorPressure;

            // Apply weather effects
            if (weather != null)
            {
                ApplyWeatherToEnvironmentalFactors(factors, weather, biome);
            }

            // Apply biome health effects
            if (enableBiomeHealth)
            {
                ApplyBiomeHealthToEnvironmentalFactors(factors, biome);
            }

            // Apply trait-specific modifiers
            if (enableTraitModifiers && biome.traits.traitModifiers != null)
            {
                foreach (var modifier in biome.traits.traitModifiers)
                {
                    factors.SetTraitBias(modifier.Key, modifier.Value);
                }
            }

            return factors;
        }

        private void ApplyWeatherToEnvironmentalFactors(EnvironmentalFactors factors, WeatherData weather, BiomeData biome)
        {
            switch (weather.weatherType)
            {
                case WeatherType.Rainy:
                    factors.foodAvailability *= 1.0f + (weather.intensity * 0.2f);
                    factors.humidity = Mathf.Min(100f, factors.humidity + (weather.intensity * 20f));
                    break;

                case WeatherType.Sunny:
                    factors.temperature += weather.intensity * 5f;
                    factors.foodAvailability *= 1.0f + (weather.intensity * 0.1f);
                    break;

                case WeatherType.Stormy:
                    factors.predatorPressure *= 0.7f; // Predators take shelter
                    factors.foodAvailability *= 0.8f; // Harder to forage
                    break;

                case WeatherType.Snowy:
                    factors.temperature -= weather.intensity * 10f;
                    factors.foodAvailability *= 0.6f; // Food scarce in snow
                    factors.predatorPressure *= 0.5f; // Predators less active
                    break;

                case WeatherType.Foggy:
                    factors.predatorPressure *= 0.8f; // Reduced visibility helps prey
                    factors.SetTraitBias("Stealth", 0.2f);
                    break;

                case WeatherType.Windy:
                    factors.SetTraitBias("Flying", weather.intensity > 0.7f ? -0.3f : 0.1f);
                    break;
            }
        }

        private void ApplyBiomeHealthToEnvironmentalFactors(EnvironmentalFactors factors, BiomeData biome)
        {
            var healthMultiplier = biome.healthIndex;

            // Healthy biomes have better conditions
            factors.foodAvailability *= 0.5f + (healthMultiplier * 0.5f); // 0.5x to 1.0x based on health

            // Unhealthy biomes have more stress
            if (healthMultiplier < 0.7f)
            {
                factors.predatorPressure *= 1.0f + ((0.7f - healthMultiplier) * 0.5f);
                factors.SetTraitBias("Disease Resistance", (0.7f - healthMultiplier) * 0.3f);
            }
        }

        #endregion

        #region Biome Type Helpers

        private float GetBiomeBaseTemperature(BiomeType biomeType)
        {
            return biomeType switch
            {
                BiomeType.Desert => 40f,
                BiomeType.Arctic => -5f,
                BiomeType.Ocean => 15f,
                BiomeType.Forest => 20f,
                BiomeType.Mountain => 8f,
                BiomeType.Grassland => 25f,
                BiomeType.Swamp => 28f,
                BiomeType.Cave => 12f,
                _ => 22f
            };
        }

        private float GetBiomeBaseHumidity(BiomeType biomeType)
        {
            return biomeType switch
            {
                BiomeType.Desert => 15f,
                BiomeType.Arctic => 40f,
                BiomeType.Ocean => 95f,
                BiomeType.Forest => 70f,
                BiomeType.Mountain => 50f,
                BiomeType.Grassland => 60f,
                BiomeType.Swamp => 90f,
                BiomeType.Cave => 80f,
                _ => 60f
            };
        }

        private float GetSeasonalTemperatureAdjustment(Season season)
        {
            return season switch
            {
                Season.Spring => 0f,
                Season.Summer => 8f,
                Season.Autumn => -3f,
                Season.Winter => -12f,
                _ => 0f
            };
        }

        private float GetWeatherTemperatureAdjustment(WeatherType weatherType, float intensity)
        {
            return weatherType switch
            {
                WeatherType.Sunny => intensity * 5f,
                WeatherType.Cloudy => -intensity * 2f,
                WeatherType.Rainy => -intensity * 3f,
                WeatherType.Stormy => -intensity * 4f,
                WeatherType.Snowy => -intensity * 8f,
                WeatherType.Foggy => -intensity * 1f,
                _ => 0f
            };
        }

        private float GetWeatherHumidityAdjustment(WeatherType weatherType, float intensity)
        {
            return weatherType switch
            {
                WeatherType.Sunny => -intensity * 10f,
                WeatherType.Rainy => intensity * 30f,
                WeatherType.Stormy => intensity * 25f,
                WeatherType.Snowy => intensity * 15f,
                WeatherType.Foggy => intensity * 40f,
                _ => 0f
            };
        }

        private float GetBiomeWeatherStress(BiomeType biomeType, WeatherType weatherType, float intensity)
        {
            // Define which weather types are stressful for each biome
            var stressMap = new Dictionary<(BiomeType, WeatherType), float>
            {
                { (BiomeType.Desert, WeatherType.Rainy), 0.3f },
                { (BiomeType.Desert, WeatherType.Snowy), 0.8f },
                { (BiomeType.Arctic, WeatherType.Sunny), 0.4f },
                { (BiomeType.Ocean, WeatherType.Stormy), 0.6f },
                { (BiomeType.Forest, WeatherType.Stormy), 0.4f },
                { (BiomeType.Grassland, WeatherType.Stormy), 0.5f }
            };

            if (stressMap.TryGetValue((biomeType, weatherType), out var stress))
            {
                return stress * intensity;
            }

            return 0f;
        }

        #endregion

        #region Biome Trait Initialization

        private void InitializeBiomeTraitModifiers(BiomeData biome)
        {
            if (biome.traits.traitModifiers == null)
            {
                biome.traits.traitModifiers = new Dictionary<string, float>();
            }

            // Set biome-specific trait modifiers
            switch (biome.biomeType)
            {
                case BiomeType.Desert:
                    biome.traits.traitModifiers["Heat Resistance"] = 0.3f;
                    biome.traits.traitModifiers["Water Conservation"] = 0.4f;
                    biome.traits.traitModifiers["Cold Resistance"] = -0.2f;
                    break;

                case BiomeType.Arctic:
                    biome.traits.traitModifiers["Cold Resistance"] = 0.4f;
                    biome.traits.traitModifiers["Thick Fur"] = 0.3f;
                    biome.traits.traitModifiers["Heat Resistance"] = -0.3f;
                    break;

                case BiomeType.Ocean:
                    biome.traits.traitModifiers["Swimming"] = 0.5f;
                    biome.traits.traitModifiers["Pressure Resistance"] = 0.3f;
                    biome.traits.traitModifiers["Aquatic Breathing"] = 0.4f;
                    break;

                case BiomeType.Forest:
                    biome.traits.traitModifiers["Climbing"] = 0.3f;
                    biome.traits.traitModifiers["Camouflage"] = 0.2f;
                    biome.traits.traitModifiers["Night Vision"] = 0.1f;
                    break;

                case BiomeType.Mountain:
                    biome.traits.traitModifiers["Climbing"] = 0.4f;
                    biome.traits.traitModifiers["Lung Capacity"] = 0.3f;
                    biome.traits.traitModifiers["Sure Footing"] = 0.2f;
                    break;

                case BiomeType.Grassland:
                    biome.traits.traitModifiers["Speed"] = 0.2f;
                    biome.traits.traitModifiers["Endurance"] = 0.2f;
                    biome.traits.traitModifiers["Social"] = 0.1f;
                    break;

                case BiomeType.Swamp:
                    biome.traits.traitModifiers["Disease Resistance"] = 0.3f;
                    biome.traits.traitModifiers["Poison Resistance"] = 0.2f;
                    biome.traits.traitModifiers["Swimming"] = 0.2f;
                    break;

                case BiomeType.Cave:
                    biome.traits.traitModifiers["Night Vision"] = 0.4f;
                    biome.traits.traitModifiers["Echolocation"] = 0.3f;
                    biome.traits.traitModifiers["Light Sensitivity"] = -0.2f;
                    break;
            }
        }

        #endregion

        #region Utility Methods


        private BiomeData CloneBiome(BiomeData original)
        {
            return new BiomeData
            {
                biomeId = original.biomeId,
                biomeName = original.biomeName,
                biomeType = original.biomeType,
                currentTemperature = original.currentTemperature,
                currentHumidity = original.currentHumidity,
                healthIndex = original.healthIndex,
                carryingCapacity = original.carryingCapacity,
                currentCapacityUsed = original.currentCapacityUsed,
                presentSpecies = new List<string>(original.presentSpecies),
                traits = original.traits,
                lastUpdated = original.lastUpdated
            };
        }

        private BiomeChangeType DeterminePrimaryChangeType(BiomeData before, BiomeData after)
        {
            if (Mathf.Abs(after.healthIndex - before.healthIndex) > 0.1f)
                return BiomeChangeType.HealthChange;

            if (Mathf.Abs(after.currentCapacityUsed - before.currentCapacityUsed) > 0.1f)
                return BiomeChangeType.CapacityChange;

            if (Mathf.Abs(after.currentTemperature - before.currentTemperature) > 2f)
                return BiomeChangeType.TemperatureChange;

            if (Mathf.Abs(after.currentHumidity - before.currentHumidity) > 5f)
                return BiomeChangeType.HumidityChange;

            return BiomeChangeType.TraitModification;
        }

        private void FireBiomeChangedEvent(BiomeData biome, BiomeChangeType changeType, string description)
        {
            var biomeEvent = new BiomeChangedEvent
            {
                biomeId = biome.biomeId,
                newBiomeData = biome,
                changeType = changeType,
                description = description,
                timestamp = DateTime.UtcNow
            };

            OnBiomeChanged?.Invoke(biomeEvent);
        }

        private void ClearEnvironmentalFactorsCache()
        {
            var toRemove = _cachedEnvironmentalFactors
                .Where(kvp => !kvp.Key.Contains("_")) // Keep biome-only keys
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _cachedEnvironmentalFactors.Remove(key);
            }

            Debug.Log($"[BiomeManager] Cleared {toRemove.Count} cached environmental factors");
        }

        private void InvalidateBiomeCache(string biomeId)
        {
            var keysToRemove = _cachedEnvironmentalFactors.Keys
                .Where(key => key.StartsWith(biomeId))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cachedEnvironmentalFactors.Remove(key);
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Print Biome Status")]
        private void PrintBiomeStatus()
        {
            foreach (var biome in _activeBiomes.Values)
            {
                Debug.Log($"Biome: {biome.biomeName} - Health: {biome.healthIndex:F2}, " +
                         $"Temp: {biome.currentTemperature:F1}°C, Humidity: {biome.currentHumidity:F1}%, " +
                         $"Capacity Used: {biome.currentCapacityUsed:P1}, Species: {biome.presentSpecies.Count}");
            }
        }

        #endregion
    }
}