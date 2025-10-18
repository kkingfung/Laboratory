using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Ecosystem.Data;

namespace Laboratory.Chimera.Ecosystem.Systems
{
    /// <summary>
    /// Manages resource availability, consumption, and regeneration across the ecosystem
    /// </summary>
    public class ResourceFlowSystem : MonoBehaviour
    {
        [Header("Resource Configuration")]
        [SerializeField] private float resourceUpdateInterval = 5.0f;
        [SerializeField] private float globalRegenerationRate = 1.0f;
        [SerializeField] private float resourceCarryingCapacity = 1000f;
        [SerializeField] private bool enableResourceDepletion = true;

        [Header("Resource Types")]
        [SerializeField] private ResourceConfiguration[] resourceConfigurations;

        [Header("Seasonal Effects")]
        [SerializeField] private bool enableSeasonalModifiers = true;
        [SerializeField] private float seasonalVariation = 0.3f;

        private Dictionary<Vector2, Dictionary<ResourceType, ResourceFlow>> regionalResources = new();
        private Dictionary<ResourceType, float> globalResourceLevels = new();
        private Dictionary<ResourceType, ResourceConfiguration> resourceConfigs = new();

        // Dependencies
        private ClimateEvolutionSystem climateSystem;
        private BiomeTransitionSystem biomeSystem;

        // Events
        public System.Action<Vector2, ResourceType, float> OnResourceLevelChanged;
        public System.Action<Vector2, ResourceType> OnResourceDepleted;
        public System.Action<Vector2, ResourceType> OnResourceRestored;
        public System.Action<ResourceType, float> OnGlobalResourceChanged;

        private void Awake()
        {
            climateSystem = FindObjectOfType<ClimateEvolutionSystem>();
            biomeSystem = FindObjectOfType<BiomeTransitionSystem>();
            InitializeResourceConfigurations();
        }

        private void Start()
        {
            InitializeRegionalResources();
            StartCoroutine(ResourceUpdateLoop());
        }

        private void InitializeResourceConfigurations()
        {
            // Initialize default resource configurations
            var defaultConfigs = new ResourceConfiguration[]
            {
                new ResourceConfiguration
                {
                    Type = ResourceType.Water,
                    BaseAvailability = 500f,
                    RegenerationRate = 0.1f,
                    SeasonalModifier = 1.0f,
                    IsRenewable = true,
                    CarryingCapacity = 1000f,
                    DepletionThreshold = 0.1f
                },
                new ResourceConfiguration
                {
                    Type = ResourceType.Food,
                    BaseAvailability = 300f,
                    RegenerationRate = 0.15f,
                    SeasonalModifier = 1.2f,
                    IsRenewable = true,
                    CarryingCapacity = 800f,
                    DepletionThreshold = 0.05f
                },
                new ResourceConfiguration
                {
                    Type = ResourceType.Shelter,
                    BaseAvailability = 100f,
                    RegenerationRate = 0.02f,
                    SeasonalModifier = 0.8f,
                    IsRenewable = false,
                    CarryingCapacity = 200f,
                    DepletionThreshold = 0.1f
                },
                new ResourceConfiguration
                {
                    Type = ResourceType.Minerals,
                    BaseAvailability = 200f,
                    RegenerationRate = 0.01f,
                    SeasonalModifier = 1.0f,
                    IsRenewable = false,
                    CarryingCapacity = 500f,
                    DepletionThreshold = 0.2f
                },
                new ResourceConfiguration
                {
                    Type = ResourceType.Energy,
                    BaseAvailability = 400f,
                    RegenerationRate = 0.2f,
                    SeasonalModifier = 1.5f,
                    IsRenewable = true,
                    CarryingCapacity = 1000f,
                    DepletionThreshold = 0.1f
                },
                new ResourceConfiguration
                {
                    Type = ResourceType.Territory,
                    BaseAvailability = 150f,
                    RegenerationRate = 0.0f,
                    SeasonalModifier = 1.0f,
                    IsRenewable = false,
                    CarryingCapacity = 300f,
                    DepletionThreshold = 0.0f
                }
            };

            // Use custom configurations if provided, otherwise use defaults
            var configsToUse = resourceConfigurations?.Length > 0 ? resourceConfigurations : defaultConfigs;

            foreach (var config in configsToUse)
            {
                resourceConfigs[config.Type] = config;
                globalResourceLevels[config.Type] = config.BaseAvailability;
            }

            Debug.Log($"💧 Initialized {resourceConfigs.Count} resource types");
        }

        private void InitializeRegionalResources()
        {
            // Initialize resources for each biome region
            for (int x = -50; x <= 50; x += 10)
            {
                for (int y = -50; y <= 50; y += 10)
                {
                    var location = new Vector2(x, y);
                    var biome = biomeSystem?.GetBiomeAtLocation(location) ?? BiomeType.Grassland;
                    InitializeResourcesForLocation(location, biome);
                }
            }

            Debug.Log($"🗺️ Initialized resources for {regionalResources.Count} regions");
        }

        private void InitializeResourcesForLocation(Vector2 location, BiomeType biome)
        {
            var locationResources = new Dictionary<ResourceType, ResourceFlow>();

            foreach (var kvp in resourceConfigs)
            {
                var resourceType = kvp.Key;
                var config = kvp.Value;

                var biomeModifier = GetBiomeResourceModifier(biome, resourceType);
                var locationModifier = GetLocationResourceModifier(location, resourceType);

                var resourceFlow = new ResourceFlow
                {
                    Type = resourceType,
                    Availability = config.BaseAvailability * biomeModifier * locationModifier,
                    ConsumptionRate = 0f,
                    RegenerationRate = config.RegenerationRate * biomeModifier,
                    SeasonalModifier = config.SeasonalModifier,
                    Location = location,
                    Quality = Random.Range(0.6f, 1.0f),
                    IsRenewable = config.IsRenewable
                };

                locationResources[resourceType] = resourceFlow;
            }

            regionalResources[location] = locationResources;
        }

        private float GetBiomeResourceModifier(BiomeType biome, ResourceType resourceType)
        {
            var modifiers = new Dictionary<(BiomeType, ResourceType), float>
            {
                // Water availability by biome
                [(BiomeType.Ocean, ResourceType.Water)] = 3.0f,
                [(BiomeType.Swamp, ResourceType.Water)] = 2.5f,
                [(BiomeType.Rainforest, ResourceType.Water)] = 2.0f,
                [(BiomeType.Forest, ResourceType.Water)] = 1.5f,
                [(BiomeType.Grassland, ResourceType.Water)] = 1.0f,
                [(BiomeType.Desert, ResourceType.Water)] = 0.2f,
                [(BiomeType.Tundra, ResourceType.Water)] = 0.8f,

                // Food availability by biome
                [(BiomeType.Rainforest, ResourceType.Food)] = 2.5f,
                [(BiomeType.Forest, ResourceType.Food)] = 2.0f,
                [(BiomeType.Grassland, ResourceType.Food)] = 1.8f,
                [(BiomeType.Savanna, ResourceType.Food)] = 1.5f,
                [(BiomeType.Ocean, ResourceType.Food)] = 1.2f,
                [(BiomeType.Desert, ResourceType.Food)] = 0.3f,
                [(BiomeType.Tundra, ResourceType.Food)] = 0.5f,
                [(BiomeType.Mountain, ResourceType.Food)] = 0.7f,

                // Shelter availability by biome
                [(BiomeType.Forest, ResourceType.Shelter)] = 2.0f,
                [(BiomeType.Mountain, ResourceType.Shelter)] = 1.8f,
                [(BiomeType.Cave, ResourceType.Shelter)] = 3.0f,
                [(BiomeType.Grassland, ResourceType.Shelter)] = 0.8f,
                [(BiomeType.Desert, ResourceType.Shelter)] = 0.5f,
                [(BiomeType.Ocean, ResourceType.Shelter)] = 0.2f,

                // Mineral availability by biome
                [(BiomeType.Mountain, ResourceType.Minerals)] = 3.0f,
                [(BiomeType.Cave, ResourceType.Minerals)] = 2.5f,
                [(BiomeType.Volcanic, ResourceType.Minerals)] = 2.0f,
                [(BiomeType.Desert, ResourceType.Minerals)] = 1.5f,
                [(BiomeType.Ocean, ResourceType.Minerals)] = 0.3f,
                [(BiomeType.Swamp, ResourceType.Minerals)] = 0.4f,

                // Energy availability by biome
                [(BiomeType.Volcanic, ResourceType.Energy)] = 2.5f,
                [(BiomeType.Desert, ResourceType.Energy)] = 2.0f,
                [(BiomeType.Grassland, ResourceType.Energy)] = 1.5f,
                [(BiomeType.Forest, ResourceType.Energy)] = 1.2f,
                [(BiomeType.Cave, ResourceType.Energy)] = 0.3f,
                [(BiomeType.Tundra, ResourceType.Energy)] = 0.8f,

                // Territory availability by biome
                [(BiomeType.Grassland, ResourceType.Territory)] = 2.0f,
                [(BiomeType.Forest, ResourceType.Territory)] = 1.5f,
                [(BiomeType.Savanna, ResourceType.Territory)] = 1.8f,
                [(BiomeType.Mountain, ResourceType.Territory)] = 1.2f,
                [(BiomeType.Desert, ResourceType.Territory)] = 1.0f,
                [(BiomeType.Ocean, ResourceType.Territory)] = 0.5f
            };

            return modifiers.GetValueOrDefault((biome, resourceType), 1.0f);
        }

        private float GetLocationResourceModifier(Vector2 location, ResourceType resourceType)
        {
            float distance = location.magnitude;
            float modifier = 1.0f;

            switch (resourceType)
            {
                case ResourceType.Water:
                    // Water more available near center and coasts
                    modifier = distance < 20f ? 1.2f : (distance > 40f ? 1.5f : 1.0f);
                    break;
                case ResourceType.Food:
                    // Food more available in temperate zones
                    modifier = distance < 30f ? 1.3f : 0.8f;
                    break;
                case ResourceType.Minerals:
                    // Minerals more available in mountainous regions
                    modifier = distance > 30f && Mathf.Abs(location.y) > 20f ? 1.5f : 1.0f;
                    break;
                case ResourceType.Energy:
                    // Energy varies with elevation and climate
                    modifier = Mathf.Abs(location.y) < 25f ? 1.2f : 0.9f;
                    break;
            }

            return modifier;
        }

        private IEnumerator ResourceUpdateLoop()
        {
            while (true)
            {
                UpdateResourceRegeneration();
                ProcessResourceConsumption();
                ApplySeasonalEffects();
                UpdateGlobalResourceLevels();
                CheckResourceThresholds();

                yield return new WaitForSeconds(resourceUpdateInterval);
            }
        }

        private void UpdateResourceRegeneration()
        {
            foreach (var locationKvp in regionalResources.ToList())
            {
                var location = locationKvp.Key;
                var resources = locationKvp.Value;

                foreach (var resourceKvp in resources.ToList())
                {
                    var resourceType = resourceKvp.Key;
                    var resource = resourceKvp.Value;

                    if (resource.IsRenewable && resourceConfigs.TryGetValue(resourceType, out var config))
                    {
                        float regeneration = resource.RegenerationRate * globalRegenerationRate * resourceUpdateInterval;
                        regeneration *= resource.SeasonalModifier;

                        float newAvailability = Mathf.Min(
                            resource.Availability + regeneration,
                            config.CarryingCapacity
                        );

                        if (Mathf.Abs(newAvailability - resource.Availability) > 0.01f)
                        {
                            resource.Availability = newAvailability;
                            resources[resourceType] = resource;
                            OnResourceLevelChanged?.Invoke(location, resourceType, newAvailability);
                        }
                    }
                }
            }
        }

        private void ProcessResourceConsumption()
        {
            foreach (var locationKvp in regionalResources.ToList())
            {
                var location = locationKvp.Key;
                var resources = locationKvp.Value;

                foreach (var resourceKvp in resources.ToList())
                {
                    var resourceType = resourceKvp.Key;
                    var resource = resourceKvp.Value;

                    if (resource.ConsumptionRate > 0f)
                    {
                        float consumption = resource.ConsumptionRate * resourceUpdateInterval;
                        float newAvailability = Mathf.Max(0f, resource.Availability - consumption);

                        if (newAvailability != resource.Availability)
                        {
                            resource.Availability = newAvailability;
                            resources[resourceType] = resource;
                            OnResourceLevelChanged?.Invoke(location, resourceType, newAvailability);

                            if (newAvailability <= 0f)
                            {
                                OnResourceDepleted?.Invoke(location, resourceType);
                            }
                        }
                    }
                }
            }
        }

        private void ApplySeasonalEffects()
        {
            if (!enableSeasonalModifiers || climateSystem == null) return;

            var season = climateSystem.GetCurrentSeason();
            var seasonProgress = climateSystem.GetSeasonProgress();

            foreach (var locationKvp in regionalResources.ToList())
            {
                var location = locationKvp.Key;
                var resources = locationKvp.Value;

                foreach (var resourceKvp in resources.ToList())
                {
                    var resourceType = resourceKvp.Key;
                    var resource = resourceKvp.Value;

                    float seasonalModifier = CalculateSeasonalModifier(resourceType, season, seasonProgress);
                    resource.SeasonalModifier = seasonalModifier;
                    resources[resourceType] = resource;
                }
            }
        }

        private float CalculateSeasonalModifier(ResourceType resourceType, SeasonType season, float progress)
        {
            var baseModifier = 1.0f;

            switch (resourceType)
            {
                case ResourceType.Food:
                    baseModifier = season switch
                    {
                        SeasonType.Spring => 1.2f + progress * 0.3f,
                        SeasonType.Summer => 1.4f,
                        SeasonType.Autumn => 1.1f - progress * 0.4f,
                        SeasonType.Winter => 0.6f,
                        _ => 1.0f
                    };
                    break;

                case ResourceType.Water:
                    baseModifier = season switch
                    {
                        SeasonType.Spring => 1.3f,
                        SeasonType.Summer => 0.8f - progress * 0.2f,
                        SeasonType.Autumn => 1.1f,
                        SeasonType.Winter => 1.0f + progress * 0.3f,
                        _ => 1.0f
                    };
                    break;

                case ResourceType.Energy:
                    baseModifier = season switch
                    {
                        SeasonType.Spring => 1.1f,
                        SeasonType.Summer => 1.4f,
                        SeasonType.Autumn => 1.0f,
                        SeasonType.Winter => 0.7f,
                        _ => 1.0f
                    };
                    break;
            }

            return Mathf.Lerp(1.0f, baseModifier, seasonalVariation);
        }

        private void UpdateGlobalResourceLevels()
        {
            foreach (var resourceType in globalResourceLevels.Keys.ToList())
            {
                float totalAvailability = 0f;
                int regionCount = 0;

                foreach (var locationResources in regionalResources.Values)
                {
                    if (locationResources.TryGetValue(resourceType, out var resource))
                    {
                        totalAvailability += resource.Availability;
                        regionCount++;
                    }
                }

                float averageAvailability = regionCount > 0 ? totalAvailability / regionCount : 0f;
                globalResourceLevels[resourceType] = averageAvailability;
                OnGlobalResourceChanged?.Invoke(resourceType, averageAvailability);
            }
        }

        private void CheckResourceThresholds()
        {
            foreach (var locationKvp in regionalResources)
            {
                var location = locationKvp.Key;
                var resources = locationKvp.Value;

                foreach (var resourceKvp in resources)
                {
                    var resourceType = resourceKvp.Key;
                    var resource = resourceKvp.Value;

                    if (resourceConfigs.TryGetValue(resourceType, out var config))
                    {
                        float threshold = config.CarryingCapacity * config.DepletionThreshold;

                        if (resource.Availability <= threshold && resource.Availability > 0f)
                        {
                            // Resource is critically low but not depleted
                            Debug.LogWarning($"⚠️ Resource {resourceType} critically low at {location}: {resource.Availability:F1}");
                        }
                        else if (resource.Availability > threshold * 2f && resource.Availability < config.CarryingCapacity * 0.9f)
                        {
                            // Resource has recovered
                            OnResourceRestored?.Invoke(location, resourceType);
                        }
                    }
                }
            }
        }

        public float ConsumeResource(Vector2 location, ResourceType resourceType, float amount)
        {
            if (regionalResources.TryGetValue(location, out var resources) &&
                resources.TryGetValue(resourceType, out var resource))
            {
                float actualConsumption = Mathf.Min(amount, resource.Availability);
                resource.Availability -= actualConsumption;
                resource.ConsumptionRate += actualConsumption / resourceUpdateInterval;

                resources[resourceType] = resource;
                OnResourceLevelChanged?.Invoke(location, resourceType, resource.Availability);

                return actualConsumption;
            }

            return 0f;
        }

        public float GetResourceAvailability(Vector2 location, ResourceType resourceType)
        {
            if (regionalResources.TryGetValue(location, out var resources) &&
                resources.TryGetValue(resourceType, out var resource))
            {
                return resource.Availability;
            }

            return 0f;
        }

        public Dictionary<ResourceType, float> GetAllResourcesAtLocation(Vector2 location)
        {
            var result = new Dictionary<ResourceType, float>();

            if (regionalResources.TryGetValue(location, out var resources))
            {
                foreach (var kvp in resources)
                {
                    result[kvp.Key] = kvp.Value.Availability;
                }
            }

            return result;
        }

        public Dictionary<ResourceType, float> GetGlobalResourceLevels()
        {
            return new Dictionary<ResourceType, float>(globalResourceLevels);
        }

        public void AddResourceSource(Vector2 location, ResourceType resourceType, float amount, float quality = 1.0f)
        {
            if (regionalResources.TryGetValue(location, out var resources) &&
                resources.TryGetValue(resourceType, out var resource))
            {
                resource.Availability += amount;
                resource.Quality = Mathf.Max(resource.Quality, quality);
                resources[resourceType] = resource;

                OnResourceLevelChanged?.Invoke(location, resourceType, resource.Availability);
                Debug.Log($"💎 Added {amount} {resourceType} to {location}");
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }

    [System.Serializable]
    public class ResourceConfiguration
    {
        public ResourceType Type;
        public float BaseAvailability = 100f;
        public float RegenerationRate = 0.1f;
        public float SeasonalModifier = 1.0f;
        public bool IsRenewable = true;
        public float CarryingCapacity = 1000f;
        public float DepletionThreshold = 0.1f;
    }
}