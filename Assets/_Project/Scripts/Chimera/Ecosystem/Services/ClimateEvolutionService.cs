using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using Laboratory.Chimera.Ecosystem.Core;
using Laboratory.Chimera.Ecosystem.Data;
using Laboratory.Shared.Types;
using Laboratory.Core.Enums;

namespace Laboratory.Chimera.Ecosystem
{
    /// <summary>
    /// Service for managing climate evolution, seasonal cycles, and climate conditions.
    /// Handles global climate updates, seasonal modifiers, and climate-based biome conditions.
    /// Extracted from EcosystemEvolutionEngine for single responsibility.
    /// </summary>
    public class ClimateEvolutionService
    {
        private readonly float globalTemperatureVariance;
        private readonly float precipitationVariance;
        private readonly float seasonalIntensity;
        private readonly bool enableSeasonalCycles;
        private readonly bool enableClimateChange;

        private ClimateSystem globalClimate;
        private SeasonalCycleManager seasonalManager;
        private ClimateEvolutionEngine climateEngine;

        public event Action<float> OnClimateShift;

        public ClimateEvolutionService(
            float globalTemperatureVariance,
            float precipitationVariance,
            float seasonalIntensity,
            float climateStabilityFactor,
            bool enableSeasonalCycles,
            bool enableClimateChange)
        {
            this.globalTemperatureVariance = globalTemperatureVariance;
            this.precipitationVariance = precipitationVariance;
            this.seasonalIntensity = seasonalIntensity;
            this.enableSeasonalCycles = enableSeasonalCycles;
            this.enableClimateChange = enableClimateChange;

            InitializeClimateSystem(climateStabilityFactor);
        }

        private void InitializeClimateSystem(float climateStabilityFactor)
        {
            globalClimate = new ClimateSystem
            {
                globalTemperature = 15f, // Celsius
                globalPrecipitation = 1000f, // mm/year
                atmosphericCO2 = 400f, // ppm
                seasonalVariation = seasonalIntensity,
                climateStability = climateStabilityFactor
            };

            seasonalManager = new SeasonalCycleManager(enableSeasonalCycles);
            climateEngine = new ClimateEvolutionEngine(globalTemperatureVariance, precipitationVariance);
        }

        /// <summary>
        /// Updates global climate evolution
        /// </summary>
        public void UpdateGlobalClimate(float deltaTime)
        {
            if (!enableClimateChange) return;

            var previousTemp = globalClimate.globalTemperature;
            climateEngine.UpdateClimate(globalClimate, deltaTime);

            // Trigger climate shift event if significant change
            if (math.abs(globalClimate.globalTemperature - previousTemp) > 0.5f)
            {
                OnClimateShift?.Invoke(globalClimate.globalTemperature);
            }
        }

        /// <summary>
        /// Updates seasonal cycles for all biomes
        /// </summary>
        public void UpdateSeasonalCycles(Dictionary<uint, Biome> biomes, float deltaTime)
        {
            if (!enableSeasonalCycles) return;

            seasonalManager.UpdateSeason(deltaTime);
            var currentSeason = seasonalManager.GetCurrentSeason();

            foreach (var biome in biomes.Values)
            {
                if (biome.seasonalModifiers.TryGetValue(currentSeason, out var modifier))
                {
                    ApplySeasonalModifiers(biome, modifier);
                }
            }
        }

        /// <summary>
        /// Applies seasonal modifiers to a biome
        /// </summary>
        private void ApplySeasonalModifiers(Biome biome, SeasonalModifier modifier)
        {
            // Apply temporary climate adjustments
            biome.climateConditions.temperature += modifier.temperatureModifier;
            biome.climateConditions.precipitation *= modifier.precipitationModifier;

            // Apply resource and breeding modifiers
            foreach (var resourceKey in biome.resources.Keys.ToArray())
            {
                var resource = biome.resources[resourceKey];
                resource.currentAmount *= modifier.resourceModifier;
            }
        }

        /// <summary>
        /// Generates climate conditions for a new biome
        /// </summary>
        public ClimateCondition GenerateClimateConditions(BiomeType biomeType, Vector3 location)
        {
            var baseConditions = GetBaseClimateConditions(biomeType);

            // Apply global climate influence
            baseConditions.temperature += globalClimate.globalTemperature - 15f;
            baseConditions.precipitation *= globalClimate.globalPrecipitation / 1000f;

            // Apply location-based modifiers
            ApplyLocationModifiers(baseConditions, location);

            return baseConditions;
        }

        /// <summary>
        /// Gets base climate conditions for a biome type
        /// </summary>
        private ClimateCondition GetBaseClimateConditions(BiomeType biomeType)
        {
            return biomeType switch
            {
                BiomeType.Tropical => new ClimateCondition { temperature = 27f, precipitation = 2500f, humidity = 0.9f },
                BiomeType.Forest => new ClimateCondition { temperature = 15f, precipitation = 1200f, humidity = 0.7f },
                BiomeType.Grassland => new ClimateCondition { temperature = 18f, precipitation = 800f, humidity = 0.5f },
                BiomeType.Desert => new ClimateCondition { temperature = 25f, precipitation = 200f, humidity = 0.2f },
                BiomeType.Tundra => new ClimateCondition { temperature = -10f, precipitation = 300f, humidity = 0.6f },
                BiomeType.Temperate => new ClimateCondition { temperature = 2f, precipitation = 500f, humidity = 0.6f },
                BiomeType.Swamp => new ClimateCondition { temperature = 12f, precipitation = 1500f, humidity = 0.95f },
                BiomeType.Mountain => new ClimateCondition { temperature = 8f, precipitation = 1000f, humidity = 0.6f },
                BiomeType.Ocean => new ClimateCondition { temperature = 16f, precipitation = 1100f, humidity = 0.8f },
                _ => new ClimateCondition { temperature = 15f, precipitation = 1000f, humidity = 0.6f }
            };
        }

        /// <summary>
        /// Applies location-based climate modifiers
        /// </summary>
        private void ApplyLocationModifiers(ClimateCondition conditions, Vector3 location)
        {
            // Latitude effect
            float latitudeEffect = math.abs(location.z) / 100f;
            conditions.temperature -= latitudeEffect * 0.6f;

            // Altitude effect
            float altitudeEffect = location.y / 1000f;
            conditions.temperature -= altitudeEffect * 6.5f; // 6.5°C per 1000m

            // Ensure reasonable bounds
            conditions.temperature = math.clamp(conditions.temperature, -30f, 40f);
            conditions.precipitation = math.clamp(conditions.precipitation, 50f, 4000f);
        }

        /// <summary>
        /// Calculates climate stress for a biome
        /// </summary>
        public float CalculateClimateStress(Biome biome)
        {
            var optimalConditions = GetBaseClimateConditions(biome.biomeType);

            float tempStress = math.abs(biome.climateConditions.temperature - optimalConditions.temperature) / 20f;
            float precipStress = math.abs(biome.climateConditions.precipitation - optimalConditions.precipitation) / optimalConditions.precipitation;

            return math.clamp((tempStress + precipStress) / 2f, 0f, 1f);
        }

        /// <summary>
        /// Initializes seasonal modifiers for a biome
        /// </summary>
        public void InitializeSeasonalModifiers(Biome biome)
        {
            if (!enableSeasonalCycles) return;

            biome.seasonalModifiers[Season.Spring] = new SeasonalModifier
            {
                temperatureModifier = 0f,
                precipitationModifier = 1.2f,
                resourceModifier = 1.3f,
                breedingModifier = 1.5f
            };

            biome.seasonalModifiers[Season.Summer] = new SeasonalModifier
            {
                temperatureModifier = 5f,
                precipitationModifier = 0.8f,
                resourceModifier = 1.1f,
                breedingModifier = 1.2f
            };

            biome.seasonalModifiers[Season.Autumn] = new SeasonalModifier
            {
                temperatureModifier = -2f,
                precipitationModifier = 1.1f,
                resourceModifier = 0.9f,
                breedingModifier = 0.8f
            };

            biome.seasonalModifiers[Season.Winter] = new SeasonalModifier
            {
                temperatureModifier = -8f,
                precipitationModifier = 0.9f,
                resourceModifier = 0.6f,
                breedingModifier = 0.3f
            };
        }

        public ClimateSystem GetGlobalClimate() => globalClimate;
        public Season GetCurrentSeason() => seasonalManager.GetCurrentSeason();
    }
}
