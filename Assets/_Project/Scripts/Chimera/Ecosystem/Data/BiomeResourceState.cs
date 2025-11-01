using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Enums;

namespace Laboratory.Chimera.Ecosystem.Data
{
    /// <summary>
    /// Resource management and distribution state for individual biomes.
    /// Handles resource levels, carrying capacity, and biome health.
    /// Used for: Resource distribution, population limits, biome sustainability.
    ///
    /// ARCHITECTURAL PURPOSE:
    /// This state type is specifically designed for biome-level resource management,
    /// separate from pure environmental conditions (EnvironmentalState) and
    /// ECS ecosystem simulation (EcosystemState component).
    ///
    /// Key responsibilities:
    /// - Track resource availability (food, water, shelter, etc.)
    /// - Manage carrying capacity and population limits
    /// - Monitor biome health and sustainability
    /// - Handle resource consumption and regeneration
    /// </summary>
    [Serializable]
    public struct BiomeResourceState
    {
        /// <summary>The type of biome this state represents</summary>
        public BiomeType biomeType;

        /// <summary>Current resource levels for each resource type</summary>
        public Dictionary<ResourceType, float> resourceLevels;

        /// <summary>Maximum population this biome can sustain</summary>
        public float carryingCapacity;

        /// <summary>Overall health of the biome (0.0 to 1.0)</summary>
        public float healthLevel;

        /// <summary>Stability factor affecting resource regeneration</summary>
        public float stability;

        /// <summary>Rate at which resources regenerate</summary>
        public float regenerationRate;

        /// <summary>Environmental stress affecting the biome</summary>
        public float environmentalStress;

        /// <summary>Last time this biome state was updated</summary>
        public float lastUpdateTime;

        /// <summary>
        /// Creates a new BiomeResourceState with default values
        /// </summary>
        public static BiomeResourceState CreateDefault(BiomeType biome)
        {
            return new BiomeResourceState
            {
                biomeType = biome,
                resourceLevels = new Dictionary<ResourceType, float>(),
                carryingCapacity = 100f,
                healthLevel = 1.0f,
                stability = 0.8f,
                regenerationRate = 0.1f,
                environmentalStress = 0.0f,
                lastUpdateTime = 0f
            };
        }

        /// <summary>
        /// Gets the current resource level for a specific resource type
        /// </summary>
        public float GetResourceLevel(ResourceType resourceType)
        {
            return resourceLevels.ContainsKey(resourceType) ? resourceLevels[resourceType] : 0f;
        }

        /// <summary>
        /// Sets the resource level for a specific resource type
        /// </summary>
        public void SetResourceLevel(ResourceType resourceType, float level)
        {
            if (resourceLevels == null)
                resourceLevels = new Dictionary<ResourceType, float>();

            resourceLevels[resourceType] = Mathf.Clamp01(level);
        }

        /// <summary>
        /// Applies resource consumption or generation
        /// </summary>
        public void ModifyResource(ResourceType resourceType, float amount)
        {
            if (resourceLevels == null)
                resourceLevels = new Dictionary<ResourceType, float>();

            float currentLevel = GetResourceLevel(resourceType);
            SetResourceLevel(resourceType, currentLevel + amount);
        }
    }

    /// <summary>
    /// Resource types that can be tracked in biomes
    /// </summary>
    public enum ResourceType
    {
        Food,
        Water,
        Shelter,
        Territory,
        Medicine,
        Energy,
        Minerals,
        Vegetation,
        MatingPartners,
        Sunlight,
        Nutrients,
        Oxygen
    }
}