using System;
using UnityEngine;
using Laboratory.Core.Events;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Resource Management System for Monster Town.
    /// Handles all economic aspects: coins, gems, tokens, materials, energy, etc.
    ///
    /// Integrates with existing event system and provides thread-safe resource operations.
    /// </summary>
    public class ResourceManager : IResourceManager
    {
        private readonly IEventBus eventBus;
        private TownResources currentResources;
        private readonly object resourceLock = new object();

        // Events
        public event Action<TownResources> OnResourcesChanged;

        // Resource generation tracking
        private float lastGenerationTime;
        private readonly ResourceGenerationSettings generationSettings;

        public ResourceManager(IEventBus eventBus, ResourceGenerationSettings settings = null)
        {
            this.eventBus = eventBus;
            this.generationSettings = settings ?? ResourceGenerationSettings.GetDefault();
            this.lastGenerationTime = Time.time;

            Debug.Log("💰 Resource Manager initialized");
        }

        #region IResourceManager Implementation

        public void InitializeResources(TownResources startingResources)
        {
            lock (resourceLock)
            {
                currentResources = startingResources;
                lastGenerationTime = Time.time;
            }

            OnResourcesChanged?.Invoke(currentResources);
            eventBus?.Publish(new ResourcesChangedEvent(TownResources.Zero, currentResources));

            Debug.Log($"💰 Resources initialized: {currentResources}");
        }

        public void UpdateResources(TownResources newResources)
        {
            TownResources oldResources;

            lock (resourceLock)
            {
                oldResources = currentResources;
                currentResources = newResources;
            }

            OnResourcesChanged?.Invoke(currentResources);
            eventBus?.Publish(new ResourcesChangedEvent(oldResources, currentResources));
        }

        public bool CanAfford(TownResources cost)
        {
            lock (resourceLock)
            {
                return currentResources.CanAfford(cost);
            }
        }

        public void AddResources(TownResources resources)
        {
            if (!resources.HasAnyResource()) return;

            TownResources oldResources;
            TownResources newResources;

            lock (resourceLock)
            {
                oldResources = currentResources;
                currentResources += resources;
                newResources = currentResources;
            }

            OnResourcesChanged?.Invoke(newResources);
            eventBus?.Publish(new ResourcesChangedEvent(oldResources, newResources));

            Debug.Log($"💰 Added resources: {resources}");
        }

        public void DeductResources(TownResources cost)
        {
            if (!cost.HasAnyResource()) return;

            TownResources oldResources;
            TownResources newResources;

            lock (resourceLock)
            {
                if (!currentResources.CanAfford(cost))
                {
                    Debug.LogWarning($"💰 Cannot afford cost: {cost}. Current: {currentResources}");
                    return;
                }

                oldResources = currentResources;
                currentResources -= cost;
                newResources = currentResources;
            }

            OnResourcesChanged?.Invoke(newResources);
            eventBus?.Publish(new ResourcesChangedEvent(oldResources, newResources));

            Debug.Log($"💰 Deducted resources: {cost}");
        }

        public TownResources GetCurrentResources()
        {
            lock (resourceLock)
            {
                return currentResources;
            }
        }

        #endregion

        #region Resource Generation

        /// <summary>
        /// Update resource generation - call this regularly from town manager
        /// </summary>
        public void UpdateGeneration(float deltaTime)
        {
            if (!generationSettings.enableGeneration) return;

            var currentTime = Time.time;
            if (currentTime - lastGenerationTime >= generationSettings.generationInterval)
            {
                var generatedResources = CalculateGeneration();
                if (generatedResources.HasAnyResource())
                {
                    AddResources(generatedResources);
                }

                lastGenerationTime = currentTime;
            }
        }

        /// <summary>
        /// Calculate resource generation based on current settings
        /// </summary>
        private TownResources CalculateGeneration()
        {
            var generated = new TownResources
            {
                coins = generationSettings.baseCoinsPerInterval,
                energy = generationSettings.baseEnergyPerInterval
            };

            // Apply any multipliers
            generated *= generationSettings.globalMultiplier;

            return generated;
        }

        /// <summary>
        /// Add bonus generation from buildings or other sources
        /// </summary>
        public void AddGenerationBonus(TownResources bonus, float duration = -1f)
        {
            // Could implement temporary bonuses here
            Debug.Log($"💰 Added generation bonus: {bonus}");
        }

        #endregion

        #region Resource Exchange

        /// <summary>
        /// Exchange one resource type for another (e.g., coins for gems)
        /// </summary>
        public bool ExchangeResources(ResourceExchangeRate rate, int amount)
        {
            var cost = new TownResources();
            var reward = new TownResources();

            // Set cost and reward based on exchange rate
            SetResourceValue(ref cost, rate.fromResource, amount * rate.exchangeRate);
            SetResourceValue(ref reward, rate.toResource, amount);

            if (!CanAfford(cost))
            {
                Debug.LogWarning($"💰 Cannot afford exchange: {cost}");
                return false;
            }

            DeductResources(cost);
            AddResources(reward);

            Debug.Log($"💰 Exchanged {cost} for {reward}");
            return true;
        }

        private void SetResourceValue(ref TownResources resources, ResourceType resourceType, int value)
        {
            switch (resourceType)
            {
                case ResourceType.Coins:
                    resources.coins = value;
                    break;
                case ResourceType.Gems:
                    resources.gems = value;
                    break;
                case ResourceType.ActivityTokens:
                    resources.activityTokens = value;
                    break;
                case ResourceType.GeneticSamples:
                    resources.geneticSamples = value;
                    break;
                case ResourceType.Materials:
                    resources.materials = value;
                    break;
                case ResourceType.Energy:
                    resources.energy = value;
                    break;
            }
        }

        #endregion

        #region Resource Validation

        /// <summary>
        /// Validate resources and fix any invalid values
        /// </summary>
        public void ValidateResources()
        {
            lock (resourceLock)
            {
                currentResources.coins = Mathf.Max(0, currentResources.coins);
                currentResources.gems = Mathf.Max(0, currentResources.gems);
                currentResources.activityTokens = Mathf.Max(0, currentResources.activityTokens);
                currentResources.geneticSamples = Mathf.Max(0, currentResources.geneticSamples);
                currentResources.materials = Mathf.Max(0, currentResources.materials);
                currentResources.energy = Mathf.Max(0, currentResources.energy);

                // Apply maximum limits if needed
                if (generationSettings.hasMaximumLimits)
                {
                    currentResources.coins = Mathf.Min(currentResources.coins, generationSettings.maxCoins);
                    currentResources.gems = Mathf.Min(currentResources.gems, generationSettings.maxGems);
                    currentResources.activityTokens = Mathf.Min(currentResources.activityTokens, generationSettings.maxTokens);
                    currentResources.geneticSamples = Mathf.Min(currentResources.geneticSamples, generationSettings.maxSamples);
                    currentResources.materials = Mathf.Min(currentResources.materials, generationSettings.maxMaterials);
                    currentResources.energy = Mathf.Min(currentResources.energy, generationSettings.maxEnergy);
                }
            }
        }

        #endregion

        #region Resource Statistics

        /// <summary>
        /// Get resource statistics for UI display
        /// </summary>
        public ResourceStatistics GetResourceStatistics()
        {
            lock (resourceLock)
            {
                return new ResourceStatistics
                {
                    currentResources = currentResources,
                    generationRate = CalculateGeneration(),
                    totalResourceValue = CalculateTotalValue(currentResources),
                    generationMultiplier = generationSettings.globalMultiplier
                };
            }
        }

        private float CalculateTotalValue(TownResources resources)
        {
            // Calculate total "worth" of resources using exchange rates
            return resources.coins +
                   resources.gems * 10f + // Gems are worth 10 coins
                   resources.activityTokens * 2f + // Tokens worth 2 coins
                   resources.geneticSamples * 50f + // Samples are valuable
                   resources.materials * 0.5f + // Materials are common
                   resources.energy * 1f; // Energy worth 1 coin
        }

        #endregion

        #region Persistence Support

        /// <summary>
        /// Get resource data for saving
        /// </summary>
        public ResourceSaveData GetSaveData()
        {
            lock (resourceLock)
            {
                return new ResourceSaveData
                {
                    resources = currentResources,
                    lastGenerationTime = this.lastGenerationTime,
                    generationSettings = this.generationSettings
                };
            }
        }

        /// <summary>
        /// Load resource data from save
        /// </summary>
        public void LoadFromSaveData(ResourceSaveData saveData)
        {
            if (saveData == null) return;

            lock (resourceLock)
            {
                currentResources = saveData.resources;
                lastGenerationTime = saveData.lastGenerationTime;
            }

            OnResourcesChanged?.Invoke(currentResources);
            Debug.Log($"💰 Loaded resources from save: {currentResources}");
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            OnResourcesChanged = null;
            Debug.Log("💰 Resource Manager disposed");
        }

        #endregion
    }

    #region Supporting Data Structures

    /// <summary>
    /// Resource generation settings
    /// </summary>
    [Serializable]
    public class ResourceGenerationSettings
    {
        [Header("Generation Settings")]
        public bool enableGeneration = true;
        public float generationInterval = 60f; // seconds
        public float globalMultiplier = 1f;

        [Header("Base Generation Rates")]
        public int baseCoinsPerInterval = 10;
        public int baseEnergyPerInterval = 5;

        [Header("Maximum Limits")]
        public bool hasMaximumLimits = false;
        public int maxCoins = 999999;
        public int maxGems = 99999;
        public int maxTokens = 99999;
        public int maxSamples = 9999;
        public int maxMaterials = 99999;
        public int maxEnergy = 1000;

        public static ResourceGenerationSettings GetDefault()
        {
            return new ResourceGenerationSettings
            {
                enableGeneration = true,
                generationInterval = 60f,
                globalMultiplier = 1f,
                baseCoinsPerInterval = 10,
                baseEnergyPerInterval = 5
            };
        }
    }

    /// <summary>
    /// Resource exchange rate definition
    /// </summary>
    [Serializable]
    public struct ResourceExchangeRate
    {
        public ResourceType fromResource;
        public ResourceType toResource;
        public int exchangeRate; // How many 'from' resources needed for 1 'to' resource
        public string description;
    }

    /// <summary>
    /// Resource statistics for UI
    /// </summary>
    [Serializable]
    public struct ResourceStatistics
    {
        public TownResources currentResources;
        public TownResources generationRate;
        public float totalResourceValue;
        public float generationMultiplier;
    }

    /// <summary>
    /// Save data for resource persistence
    /// </summary>
    [Serializable]
    public struct ResourceSaveData
    {
        public TownResources resources;
        public float lastGenerationTime;
        public ResourceGenerationSettings generationSettings;
    }

    /// <summary>
    /// Resource types for exchange system
    /// </summary>
    public enum ResourceType
    {
        Coins,
        Gems,
        ActivityTokens,
        GeneticSamples,
        Materials,
        Energy
    }

    #endregion
}