using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Ecosystem.Core;

namespace Laboratory.Chimera.Ecosystem
{
    /// <summary>
    /// Service for managing resource dynamics, flows, and consumption.
    /// Handles resource network updates, consumption by species, and resource flows between biomes.
    /// Extracted from EcosystemEvolutionEngine for single responsibility.
    /// </summary>
    public class ResourceDynamicsService
    {
        private readonly ResourceNetwork resourceNetwork;

        public ResourceDynamicsService(int resourceTypes)
        {
            resourceNetwork = new ResourceNetwork(resourceTypes);
        }

        /// <summary>
        /// Updates resource dynamics for all biomes
        /// </summary>
        public void UpdateResourceDynamics(
            Dictionary<uint, Biome> activeBiomes,
            Dictionary<uint, EcosystemNode> ecosystemNodes,
            float deltaTime)
        {
            // Update resource consumption
            foreach (var biome in activeBiomes.Values)
            {
                ApplyResourceConsumption(biome, deltaTime);
            }

            // Update resource flows between connected biomes
            UpdateResourceFlows(ecosystemNodes, deltaTime);

            // Regenerate resources
            RegenerateResources(activeBiomes, deltaTime);
        }

        /// <summary>
        /// Applies resource consumption by species in a biome
        /// </summary>
        private void ApplyResourceConsumption(Biome biome, float deltaTime)
        {
            foreach (var species in biome.species)
            {
                float consumptionRate = CalculateSpeciesConsumption(species);

                // Consume resources proportionally
                foreach (var resourceType in biome.resources.Keys.ToArray())
                {
                    float consumption = consumptionRate * species.populationSize * deltaTime;
                    biome.resources[resourceType] = Mathf.Max(0f, biome.resources[resourceType] - consumption);
                }
            }
        }

        /// <summary>
        /// Calculates resource consumption rate for a species
        /// </summary>
        private float CalculateSpeciesConsumption(EcosystemSpeciesData species)
        {
            // Base consumption depends on metabolic rate and body size
            float baseConsumption = 0.1f; // Base rate per individual per time unit

            // Adjust for trophic level (carnivores consume less but require more energy)
            float trophicModifier = species.trophicLevel switch
            {
                TrophicLevel.Producer => 0.0f, // Producers don't consume, they produce
                TrophicLevel.PrimaryConsumer => 1.0f,
                TrophicLevel.SecondaryConsumer => 0.8f, // More efficient
                TrophicLevel.TertiaryConsumer => 0.6f, // Most efficient
                _ => 0.5f
            };

            return baseConsumption * trophicModifier;
        }

        /// <summary>
        /// Updates resource flows between connected biomes
        /// </summary>
        private void UpdateResourceFlows(Dictionary<uint, EcosystemNode> ecosystemNodes, float deltaTime)
        {
            resourceNetwork.UpdateFlows(ecosystemNodes, deltaTime);
        }

        /// <summary>
        /// Regenerates resources in biomes based on biome type and conditions
        /// </summary>
        private void RegenerateResources(Dictionary<uint, Biome> activeBiomes, float deltaTime)
        {
            foreach (var biome in activeBiomes.Values)
            {
                float regenerationRate = CalculateRegenerationRate(biome);

                foreach (var resourceType in biome.resources.Keys.ToArray())
                {
                    float maxResource = GetMaxResourceCapacity(biome.biomeType, resourceType);
                    float currentResource = biome.resources[resourceType];

                    if (currentResource < maxResource)
                    {
                        float regeneration = regenerationRate * deltaTime;
                        biome.resources[resourceType] = Mathf.Min(maxResource, currentResource + regeneration);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates regeneration rate based on biome conditions
        /// </summary>
        private float CalculateRegenerationRate(Biome biome)
        {
            // Base regeneration depends on biome type
            float baseRate = biome.biomeType switch
            {
                Laboratory.Core.Enums.BiomeType.Tropical => 2.0f,
                Laboratory.Core.Enums.BiomeType.Forest => 1.5f,
                Laboratory.Core.Enums.BiomeType.Grassland => 1.2f,
                Laboratory.Core.Enums.BiomeType.Desert => 0.3f,
                Laboratory.Core.Enums.BiomeType.Tundra => 0.5f,
                Laboratory.Core.Enums.BiomeType.Swamp => 1.8f,
                _ => 1.0f
            };

            // Modify by climate conditions
            float climateModifier = Mathf.Clamp01(biome.climateConditions.precipitation / 1000f);

            return baseRate * climateModifier * biome.stabilityIndex;
        }

        /// <summary>
        /// Gets maximum resource capacity for a biome type and resource
        /// </summary>
        private float GetMaxResourceCapacity(Laboratory.Core.Enums.BiomeType biomeType, string resourceType)
        {
            // Base capacity varies by biome type
            return 1000f; // Simplified - would be more complex in full implementation
        }

        /// <summary>
        /// Gets available resources for a species in a biome
        /// </summary>
        public float GetAvailableResources(Biome biome, string resourceType)
        {
            return biome.resources.GetValueOrDefault(resourceType, 0f);
        }

        public ResourceNetwork GetResourceNetwork() => resourceNetwork;
    }
}
