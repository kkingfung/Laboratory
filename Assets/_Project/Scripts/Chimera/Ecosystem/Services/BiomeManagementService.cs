using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Chimera.Ecosystem.Core;
using Laboratory.Shared.Types;

namespace Laboratory.Chimera.Ecosystem
{
    /// <summary>
    /// Service for managing biome lifecycle, transitions, and succession stages.
    /// Handles biome creation, evolution, stability, and transitions between biome types.
    /// Extracted from EcosystemEvolutionEngine for single responsibility.
    /// </summary>
    public class BiomeManagementService
    {
        private readonly int maxBiomes;
        private readonly float biomeTransitionRate;
        private readonly float carryingCapacityFlexibility;
        private readonly BiomeTransitionMatrix transitionMatrix;
        private readonly SuccessionManager successionManager;
        private readonly ClimateEvolutionService climateService;

        public event Action<uint, BiomeType, BiomeType> OnBiomeTransition;

        public BiomeManagementService(
            int maxBiomes,
            float biomeTransitionRate,
            float carryingCapacityFlexibility,
            ClimateEvolutionService climateService)
        {
            this.maxBiomes = maxBiomes;
            this.biomeTransitionRate = biomeTransitionRate;
            this.carryingCapacityFlexibility = carryingCapacityFlexibility;
            this.climateService = climateService;

            transitionMatrix = new BiomeTransitionMatrix();
            successionManager = new SuccessionManager();
            InitializeBiomeTransitionMatrix();
        }

        private void InitializeBiomeTransitionMatrix()
        {
            transitionMatrix.SetTransition(BiomeType.Grassland, BiomeType.Forest, 0.3f,
                new ClimateCondition { temperature = 15f, precipitation = 1200f });
            transitionMatrix.SetTransition(BiomeType.Grassland, BiomeType.Desert, 0.2f,
                new ClimateCondition { temperature = 25f, precipitation = 300f });
            transitionMatrix.SetTransition(BiomeType.Forest, BiomeType.Grassland, 0.2f,
                new ClimateCondition { temperature = 20f, precipitation = 800f });
            transitionMatrix.SetTransition(BiomeType.Desert, BiomeType.Grassland, 0.1f,
                new ClimateCondition { temperature = 18f, precipitation = 600f });
            transitionMatrix.SetTransition(BiomeType.Tundra, BiomeType.Temperate, 0.4f,
                new ClimateCondition { temperature = 0f, precipitation = 400f });
            transitionMatrix.SetTransition(BiomeType.Swamp, BiomeType.Forest, 0.3f,
                new ClimateCondition { temperature = 15f, precipitation = 1500f });
        }

        /// <summary>
        /// Creates a new biome with specified characteristics
        /// </summary>
        public Biome CreateBiome(
            Dictionary<uint, Biome> activeBiomes,
            BiomeType biomeType,
            Vector3 location,
            float area,
            ResourceNetwork resourceNetwork)
        {
            if (activeBiomes.Count >= maxBiomes)
            {
                Debug.LogWarning("Maximum biome limit reached");
                return null;
            }

            var biomeId = GenerateBiomeId();

            var biome = new Biome
            {
                biomeId = biomeId,
                biomeType = biomeType,
                location = location,
                area = area,
                creationTime = Time.time,
                climateConditions = climateService.GenerateClimateConditions(biomeType, location),
                resources = resourceNetwork.InitializeBiomeResources(biomeType),
                species = new List<EcosystemSpeciesData>(),
                carryingCapacity = CalculateCarryingCapacity(biomeType, area),
                biodiversityIndex = 0f,
                stabilityIndex = 0.8f,
                connectivityIndex = 0f,
                successionStage = SuccessionStage.Pioneer,
                disturbanceHistory = new List<DisturbanceEvent>(),
                seasonalModifiers = new Dictionary<Season, SeasonalModifier>()
            };

            // Initialize seasonal modifiers
            climateService.InitializeSeasonalModifiers(biome);

            Debug.Log($"Biome {biomeId} ({biomeType}) created at {location} with area {area:F1}");
            return biome;
        }

        /// <summary>
        /// Updates biome evolution and succession
        /// </summary>
        public void UpdateBiomeEvolution(Dictionary<uint, Biome> activeBiomes, float deltaTime)
        {
            foreach (var biome in activeBiomes.Values)
            {
                UpdateBiomeStability(biome, deltaTime);
                UpdateSuccessionStage(biome, deltaTime);

                // Check for biome transitions
                var potentialTransition = transitionMatrix.GetPotentialTransition(
                    biome.biomeType,
                    biome.climateConditions);

                if (potentialTransition.HasValue && UnityEngine.Random.value < biomeTransitionRate * deltaTime)
                {
                    TransitionBiome(biome, potentialTransition.Value);
                }
            }
        }

        /// <summary>
        /// Updates biome stability based on climate stress and disturbances
        /// </summary>
        private void UpdateBiomeStability(Biome biome, float deltaTime)
        {
            float climateStress = climateService.CalculateClimateStress(biome);
            float disturbanceImpact = CalculateDisturbanceImpact(biome);

            biome.stabilityIndex -= (climateStress + disturbanceImpact) * deltaTime * 0.1f;
            biome.stabilityIndex = Mathf.Clamp01(biome.stabilityIndex);

            // Stability recovery over time if conditions are favorable
            if (climateStress < 0.3f && disturbanceImpact < 0.2f)
            {
                biome.stabilityIndex += deltaTime * 0.05f;
                biome.stabilityIndex = Mathf.Clamp01(biome.stabilityIndex);
            }
        }

        /// <summary>
        /// Calculates impact from disturbance history
        /// </summary>
        private float CalculateDisturbanceImpact(Biome biome)
        {
            if (biome.disturbanceHistory.Count == 0) return 0f;

            float recentDisturbance = 0f;
            float currentTime = Time.time;

            foreach (var disturbance in biome.disturbanceHistory)
            {
                float timeSince = currentTime - disturbance.timestamp;
                float decay = Mathf.Exp(-timeSince / 100f); // Decay over time
                recentDisturbance += disturbance.severity * decay;
            }

            return Mathf.Clamp01(recentDisturbance);
        }

        /// <summary>
        /// Updates ecological succession stage
        /// </summary>
        private void UpdateSuccessionStage(Biome biome, float deltaTime)
        {
            successionManager.UpdateSuccession(biome, deltaTime);
        }

        /// <summary>
        /// Transitions biome to a new type
        /// </summary>
        private void TransitionBiome(Biome biome, BiomeType newType)
        {
            var previousType = biome.biomeType;
            biome.biomeType = newType;

            // Update climate conditions for new biome type
            biome.climateConditions = climateService.GenerateClimateConditions(newType, biome.location);

            // Recalculate carrying capacity
            biome.carryingCapacity = CalculateCarryingCapacity(newType, biome.area);

            OnBiomeTransition?.Invoke(biome.biomeId, previousType, newType);

            Debug.Log($"Biome {biome.biomeId} transitioned from {previousType} to {newType}");
        }

        /// <summary>
        /// Calculates carrying capacity based on biome type and area
        /// </summary>
        private float CalculateCarryingCapacity(BiomeType biomeType, float area)
        {
            float baseCapacity = biomeType switch
            {
                BiomeType.Tropical => 100f,
                BiomeType.Forest => 80f,
                BiomeType.Grassland => 60f,
                BiomeType.Desert => 20f,
                BiomeType.Tundra => 30f,
                BiomeType.Temperate => 50f,
                BiomeType.Swamp => 90f,
                BiomeType.Mountain => 40f,
                BiomeType.Ocean => 70f,
                _ => 50f
            };

            return baseCapacity * area * carryingCapacityFlexibility;
        }

        private uint GenerateBiomeId() => (uint)UnityEngine.Random.Range(1000, int.MaxValue);
    }
}
