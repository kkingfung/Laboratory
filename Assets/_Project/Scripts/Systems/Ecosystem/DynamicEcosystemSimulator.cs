using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core;
using Laboratory.Shared.Types;
using Laboratory.Core.Enums;
using Laboratory.Chimera.Genetics.Advanced;
using Laboratory.Core.Debug;
using Laboratory.Chimera.Ecosystem.Core;
using Laboratory.Chimera.Ecosystem.Data;

namespace Laboratory.Systems.Ecosystem
{
    /// <summary>
    /// Dynamic ecosystem simulator that creates a living world with interconnected
    /// biological, environmental, and resource systems that respond to creature populations
    /// and player actions, creating emergent ecosystem behaviors.
    /// </summary>
    public class DynamicEcosystemSimulator : MonoBehaviour
    {
        [Header("Ecosystem Configuration")]
        [SerializeField] private bool enableEcosystemSimulation = true;
        [SerializeField] private float simulationTimeScale = 1f;
        [SerializeField] private int maxEcosystemNodes = 50;
        [SerializeField] private float ecosystemUpdateInterval = 5f;

        [Header("Environmental Parameters")]
        [SerializeField, Range(0f, 1f)] private float globalTemperature = 0.5f;
        [SerializeField, Range(0f, 1f)] private float globalHumidity = 0.6f;
        [SerializeField, Range(0f, 1f)] private float seasonalVariation = 0.3f;
        [SerializeField] private AnimationCurve seasonalCycle;

        [Header("Resource Dynamics")]
        [SerializeField] private ResourceConfig[] resourceConfigs;
        [SerializeField, Range(1f, 10f)] private float resourceRegenerationRate = 2f;
        [SerializeField, Range(0f, 1f)] private float resourceScarcityThreshold = 0.3f;

        [Header("Population Dynamics")]
        [SerializeField, Range(1f, 100f)] private float carryingCapacityBase = 50f;
        [SerializeField, Range(0f, 1f)] private float predationPressure = 0.1f;
        [SerializeField, Range(0f, 1f)] private float competitionFactor = 0.2f;

        [Header("Environmental Events")]
        [SerializeField] private EnvironmentalEvent[] possibleEvents;
        [SerializeField, Range(0f, 1f)] private float eventProbability = 0.05f;
        [SerializeField] private float eventDuration = 30f;

        [Header("Biome System")]
        [SerializeField] private BiomeConfig[] availableBiomes;
        [SerializeField] private Transform[] biomeRegions;
        [SerializeField] private float biomeTransitionRange = 20f;

        // Core ecosystem data
        private Dictionary<BiomeType, EcosystemNode> ecosystemNodes = new Dictionary<BiomeType, EcosystemNode>();
        private Dictionary<BiomeType, BiomeResourceState> activeBiomes = new Dictionary<BiomeType, BiomeResourceState>();
        private Dictionary<ResourceType, GlobalResourceState> globalResources = new Dictionary<ResourceType, GlobalResourceState>();
        private List<ActiveEnvironmentalEvent> activeEvents = new List<ActiveEnvironmentalEvent>();

        // Simulation state
        private float lastEcosystemUpdate;
        private float seasonalTime;
        private EcosystemAnalytics analytics = new EcosystemAnalytics();

        // Population tracking
        private Dictionary<BiomeType, PopulationData> biomePopulations = new Dictionary<BiomeType, PopulationData>();
        private Dictionary<uint, CreatureEcosystemData> creatureEcosystemData = new Dictionary<uint, CreatureEcosystemData>();

        // Events
        public System.Action<EnvironmentalEvent> OnEnvironmentalEventStarted;
        public System.Action<EnvironmentalEvent> OnEnvironmentalEventEnded;
        public System.Action<ResourceType, float> OnResourceLevelChanged;
        public System.Action<BiomeType, float> OnBiomeHealthChanged;
        public System.Action<EcosystemStressLevel> OnEcosystemStressChanged;

        // Singleton access
        private static DynamicEcosystemSimulator instance;
        public static DynamicEcosystemSimulator Instance => instance;

        public float GlobalTemperature => CalculateCurrentTemperature();
        public float GlobalHumidity => globalHumidity;
        public EcosystemHealth OverallHealth => CalculateOverallHealth();
        public EcosystemAnalytics Analytics => analytics;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeEcosystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeEcosystem()
        {
            Debug.Log("Initializing Dynamic Ecosystem Simulator");

            // Initialize biome nodes
            InitializeBiomeNodes();

            // Initialize active biomes with resource states
            InitializeActiveBiomes();

            // Initialize global resources
            InitializeGlobalResources();

            // Initialize population tracking
            InitializePopulationTracking();

            // Create default seasonal cycle if not set
            if (seasonalCycle == null || seasonalCycle.keys.Length == 0)
            {
                CreateDefaultSeasonalCycle();
            }

            // Subscribe to other system events
            SubscribeToSystemEvents();

            Debug.Log($"Ecosystem initialized with {ecosystemNodes.Count} biome nodes and {globalResources.Count} resource types");
        }

        private void Update()
        {
            if (!enableEcosystemSimulation) return;

            // Update seasonal time
            seasonalTime += Time.deltaTime * simulationTimeScale / 3600f; // Hours to seasonal cycle

            // Update ecosystem periodically
            if (Time.time - lastEcosystemUpdate >= ecosystemUpdateInterval)
            {
                UpdateEcosystemSimulation();
                lastEcosystemUpdate = Time.time;
            }

            // Update active environmental events
            UpdateEnvironmentalEvents();
        }

        /// <summary>
        /// Registers a creature with the ecosystem for tracking
        /// </summary>
        public void RegisterCreature(uint creatureId, Vector3 position, CreatureGenome genome)
        {
            var biome = GetBiomeAtPosition(position);
            var ecosystemData = new CreatureEcosystemData
            {
                creatureId = creatureId,
                currentBiome = biome,
                position = position,
                genome = genome,
                resourceConsumption = CalculateResourceConsumption(genome),
                environmentalImpact = CalculateEnvironmentalImpact(genome),
                registrationTime = Time.time
            };

            creatureEcosystemData[creatureId] = ecosystemData;

            // Update biome population
            if (!biomePopulations.ContainsKey(biome))
            {
                biomePopulations[biome] = new PopulationData();
            }
            biomePopulations[biome].totalPopulation++;
            biomePopulations[biome].totalBiomass += genome.traits.ContainsKey(TraitType.Size) ? genome.traits[TraitType.Size].value : 1f;

            Debug.Log($"Creature {creatureId} registered in {biome} biome");
        }

        /// <summary>
        /// Updates a creature's position in the ecosystem
        /// </summary>
        public void UpdateCreaturePosition(uint creatureId, Vector3 newPosition)
        {
            if (creatureEcosystemData.TryGetValue(creatureId, out var data))
            {
                var oldBiome = data.currentBiome;
                var newBiome = GetBiomeAtPosition(newPosition);

                if (oldBiome != newBiome)
                {
                    // Transfer between biomes
                    if (biomePopulations.ContainsKey(oldBiome))
                    {
                        biomePopulations[oldBiome].totalPopulation--;
                        biomePopulations[oldBiome].totalBiomass -= data.genome.traits.ContainsKey(TraitType.Size) ? data.genome.traits[TraitType.Size].value : 1f;
                    }

                    if (!biomePopulations.ContainsKey(newBiome))
                    {
                        biomePopulations[newBiome] = new PopulationData();
                    }
                    biomePopulations[newBiome].totalPopulation++;
                    biomePopulations[newBiome].totalBiomass += data.genome.traits.ContainsKey(TraitType.Size) ? data.genome.traits[TraitType.Size].value : 1f;

                    data.currentBiome = newBiome;
                }

                data.position = newPosition;
            }
        }

        /// <summary>
        /// Unregisters a creature from the ecosystem
        /// </summary>
        public void UnregisterCreature(uint creatureId)
        {
            if (creatureEcosystemData.TryGetValue(creatureId, out var data))
            {
                // Remove from biome population
                if (biomePopulations.ContainsKey(data.currentBiome))
                {
                    biomePopulations[data.currentBiome].totalPopulation--;
                    biomePopulations[data.currentBiome].totalBiomass -= data.genome.traits.ContainsKey(TraitType.Size) ? data.genome.traits[TraitType.Size].value : 1f;
                }

                creatureEcosystemData.Remove(creatureId);
                Debug.Log($"Creature {creatureId} unregistered from ecosystem");
            }
        }

        /// <summary>
        /// Gets environmental conditions at a specific position
        /// </summary>
        public EnvironmentalConditions GetEnvironmentalConditions(Vector3 position)
        {
            var biome = GetBiomeAtPosition(position);
            var node = ecosystemNodes.ContainsKey(biome) ? ecosystemNodes[biome] : null;

            var conditions = new EnvironmentalConditions
            {
                temperature = CalculateLocalTemperature(biome, position),
                humidity = CalculateLocalHumidity(biome, position),
                resourceAvailability = node?.resourceAvailability ?? new Dictionary<ResourceType, float>(),
                predationRisk = CalculatePredationRisk(biome, position),
                competitionLevel = CalculateCompetitionLevel(biome, position),
                environmentalStress = CalculateEnvironmentalStress(biome)
            };

            return conditions;
        }

        /// <summary>
        /// Consumes resources from the ecosystem
        /// </summary>
        public bool ConsumeResources(Vector3 position, Dictionary<ResourceType, float> consumption)
        {
            var biome = GetBiomeAtPosition(position);
            if (!ecosystemNodes.TryGetValue(biome, out var node))
                return false;

            bool canConsume = true;

            // Check availability
            foreach (var kvp in consumption)
            {
                var available = node.resourceAvailability.ContainsKey(kvp.Key) ? node.resourceAvailability[kvp.Key] : 0f;
                if (available < kvp.Value)
                {
                    canConsume = false;
                    break;
                }
            }

            if (canConsume)
            {
                // Consume resources
                foreach (var kvp in consumption)
                {
                    node.resourceAvailability[kvp.Key] -= kvp.Value;
                    globalResources[kvp.Key].totalConsumed += kvp.Value;

                    // Trigger resource level change event
                    OnResourceLevelChanged?.Invoke(kvp.Key, node.resourceAvailability[kvp.Key]);
                }

                analytics.totalResourcesConsumed += consumption.Values.Sum();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Triggers an environmental event
        /// </summary>
        public void TriggerEnvironmentalEvent(EnvironmentalEventType eventType, BiomeType targetBiome, float intensity)
        {
            var eventConfig = possibleEvents?.FirstOrDefault(e => e.eventType == eventType);
            if (eventConfig == null) return;

            var activeEvent = new ActiveEnvironmentalEvent
            {
                eventType = eventType,
                targetBiome = targetBiome,
                intensity = intensity,
                startTime = Time.time,
                duration = eventDuration * Random.Range(0.8f, 1.2f),
                effects = eventConfig.effects
            };

            activeEvents.Add(activeEvent);
            OnEnvironmentalEventStarted?.Invoke(eventConfig);

            ApplyEventEffects(activeEvent, true);

            analytics.totalEnvironmentalEvents++;
            Debug.Log($"Environmental event started: {eventType} in {targetBiome} biome (Intensity: {intensity:F2})");
        }

        /// <summary>
        /// Gets ecosystem stress analysis
        /// </summary>
        public EcosystemStressAnalysis AnalyzeEcosystemStress()
        {
            var analysis = new EcosystemStressAnalysis
            {
                timestamp = Time.time,
                overallStressLevel = CalculateOverallStressLevel(),
                biomeStressLevels = new Dictionary<BiomeType, float>(),
                resourceStressLevels = new Dictionary<ResourceType, float>(),
                stressFactors = new List<EcosystemStressFactor>(),
                recommendations = new List<EcosystemRecommendation>()
            };

            // Analyze biome stress
            foreach (var kvp in ecosystemNodes)
            {
                float biomeStress = CalculateBiomeStress(kvp.Value);
                analysis.biomeStressLevels[kvp.Key] = biomeStress;

                if (biomeStress > 0.7f)
                {
                    analysis.stressFactors.Add(EcosystemStressFactor.BiomeDegradation);
                    analysis.recommendations.Add(EcosystemRecommendation.RestoreBiomeHealth);
                }
            }

            // Analyze resource stress
            foreach (var kvp in globalResources)
            {
                float resourceStress = 1f - (kvp.Value.currentAmount / kvp.Value.maxAmount);
                analysis.resourceStressLevels[kvp.Key] = resourceStress;

                if (resourceStress > 0.6f)
                {
                    analysis.stressFactors.Add(EcosystemStressFactor.ResourceDepletion);
                    analysis.recommendations.Add(EcosystemRecommendation.IncreaseResourceGeneration);
                    analysis.recommendations.Add(EcosystemRecommendation.MonitorResourceLevels);
                }
            }

            // Check for overpopulation stress
            foreach (var kvp in biomePopulations)
            {
                var populationData = kvp.Value;
                var ecosystemNode = ecosystemNodes.ContainsKey(kvp.Key) ? ecosystemNodes[kvp.Key] : null;

                if (ecosystemNode != null)
                {
                    float overpopulationPressure = populationData.totalPopulation / (carryingCapacityBase * ecosystemNode.carryingCapacityModifier);
                    if (overpopulationPressure > 1.2f)
                    {
                        analysis.stressFactors.Add(EcosystemStressFactor.OverpopulationStress);
                        analysis.recommendations.Add(EcosystemRecommendation.ReducePopulationDensity);
                    }
                }
            }

            // Check for temperature extremes
            float currentTemp = CalculateCurrentTemperature();
            if (currentTemp > 0.9f || currentTemp < 0.1f)
            {
                analysis.stressFactors.Add(EcosystemStressFactor.TemperatureExtreme);
                analysis.recommendations.Add(EcosystemRecommendation.RegulateTemperature);
                analysis.recommendations.Add(EcosystemRecommendation.StabilizeClimate);
            }

            return analysis;
        }

        private void UpdateEcosystemSimulation()
        {
            // Update resource regeneration
            UpdateResourceRegeneration();

            // Update biome health
            UpdateBiomeHealth();

            // Update population dynamics
            UpdatePopulationDynamics();

            // Check for random environmental events
            CheckForRandomEvents();

            // Update analytics
            UpdateAnalytics();
        }

        private void UpdateResourceRegeneration()
        {
            foreach (var kvp in globalResources)
            {
                var resource = kvp.Value;
                float regenerationAmount = resource.regenerationRate * resourceRegenerationRate * Time.deltaTime;

                // Apply environmental modifiers
                float environmentalModifier = CalculateEnvironmentalModifier(kvp.Key);
                regenerationAmount *= environmentalModifier;

                resource.currentAmount = Mathf.Min(resource.maxAmount, resource.currentAmount + regenerationAmount);

                // Distribute to biomes
                DistributeResourceToBiomes(kvp.Key, regenerationAmount);
            }
        }

        private void UpdateBiomeHealth()
        {
            foreach (var kvp in ecosystemNodes)
            {
                var node = kvp.Value;
                var populationData = biomePopulations.ContainsKey(kvp.Key) ? biomePopulations[kvp.Key] : new PopulationData();

                // Calculate carrying capacity pressure
                float carryingCapacityPressure = populationData.totalPopulation / (carryingCapacityBase * node.carryingCapacityModifier);

                // Calculate resource pressure
                float resourcePressure = CalculateResourcePressure(node);

                // Update biome health
                float healthChange = -carryingCapacityPressure * 0.1f - resourcePressure * 0.05f + 0.02f; // Small natural recovery
                node.health = Mathf.Clamp01(node.health + healthChange * Time.deltaTime);

                if (Mathf.Abs(healthChange) > 0.01f)
                {
                    OnBiomeHealthChanged?.Invoke(kvp.Key, node.health);
                }
            }
        }

        private void UpdatePopulationDynamics()
        {
            foreach (var kvp in biomePopulations)
            {
                var populationData = kvp.Value;
                var ecosystemNode = ecosystemNodes.ContainsKey(kvp.Key) ? ecosystemNodes[kvp.Key] : null;

                if (ecosystemNode != null)
                {
                    // Calculate population pressure effects
                    float overpopulationPressure = populationData.totalPopulation / (carryingCapacityBase * ecosystemNode.carryingCapacityModifier);

                    if (overpopulationPressure > 1f)
                    {
                        // Apply stress to creatures in this biome
                        ApplyPopulationStressToCreatures(kvp.Key, overpopulationPressure - 1f);
                    }
                }
            }
        }

        private void UpdateEnvironmentalEvents()
        {
            var eventsToRemove = new List<ActiveEnvironmentalEvent>();

            foreach (var activeEvent in activeEvents)
            {
                if (Time.time - activeEvent.startTime >= activeEvent.duration)
                {
                    ApplyEventEffects(activeEvent, false); // Remove effects
                    eventsToRemove.Add(activeEvent);

                    var eventConfig = possibleEvents?.FirstOrDefault(e => e.eventType == activeEvent.eventType);
                    if (eventConfig != null)
                    {
                        OnEnvironmentalEventEnded?.Invoke(eventConfig);
                    }

                    Debug.Log($"Environmental event ended: {activeEvent.eventType}");
                }
            }

            foreach (var eventToRemove in eventsToRemove)
            {
                activeEvents.Remove(eventToRemove);
            }
        }

        private void CheckForRandomEvents()
        {
            if (Random.Range(0f, 1f) < eventProbability * Time.deltaTime)
            {
                var possibleEventTypes = System.Enum.GetValues(typeof(EnvironmentalEventType)).Cast<EnvironmentalEventType>();
                var randomEventType = possibleEventTypes.ElementAt(Random.Range(0, possibleEventTypes.Count()));
                var availableBiomes = ecosystemNodes.Keys.ToArray();
                var randomBiome = availableBiomes[Random.Range(0, availableBiomes.Length)];
                var randomIntensity = Random.Range(0.3f, 0.8f);

                TriggerEnvironmentalEvent(randomEventType, randomBiome, randomIntensity);
            }
        }

        private void InitializeBiomeNodes()
        {
            if (availableBiomes == null || availableBiomes.Length == 0)
            {
                // Create default biomes
                CreateDefaultBiomes();
            }

            foreach (var biomeConfig in availableBiomes)
            {
                var node = new EcosystemNode
                {
                    biomeType = biomeConfig.biomeType,
                    health = 1f,
                    carryingCapacityModifier = biomeConfig.carryingCapacityModifier,
                    resourceAvailability = new Dictionary<ResourceType, float>(),
                    environmentalModifiers = biomeConfig.environmentalModifiers?.ToDictionary(
                        em => em.modifierType,
                        em => em.value
                    ) ?? new Dictionary<EnvironmentalModifierType, float>()
                };

                // Initialize resource availability
                foreach (ResourceType resourceType in System.Enum.GetValues(typeof(ResourceType)))
                {
                    node.resourceAvailability[resourceType] = biomeConfig.baseResourceLevels?.ContainsKey(resourceType) == true ? biomeConfig.baseResourceLevels[resourceType] : 0.5f;
                }

                ecosystemNodes[biomeConfig.biomeType] = node;
            }
        }

        private void InitializeGlobalResources()
        {
            if (resourceConfigs == null || resourceConfigs.Length == 0)
            {
                CreateDefaultResourceConfigs();
            }

            foreach (var config in resourceConfigs)
            {
                globalResources[config.resourceType] = new GlobalResourceState
                {
                    maxAmount = config.maxAmount,
                    currentAmount = config.initialAmount,
                    regenerationRate = config.regenerationRate,
                    totalConsumed = 0f,
                    totalRegenerated = 0f
                };
            }
        }

        private void InitializePopulationTracking()
        {
            foreach (BiomeType biomeType in System.Enum.GetValues(typeof(BiomeType)))
            {
                biomePopulations[biomeType] = new PopulationData();
            }
        }

        private void InitializeActiveBiomes()
        {
            foreach (var biomeConfig in availableBiomes)
            {
                var biomeResourceState = BiomeResourceState.CreateDefault(biomeConfig.biomeType);

                // Set initial resource levels from config
                if (biomeConfig.baseResourceLevels != null)
                {
                    foreach (var kvp in biomeConfig.baseResourceLevels)
                    {
                        biomeResourceState.SetResourceLevel((Laboratory.Chimera.Ecosystem.Data.ResourceType)kvp.Key, kvp.Value);
                    }
                }

                activeBiomes[biomeConfig.biomeType] = biomeResourceState;
            }
        }

        private BiomeType GetBiomeAtPosition(Vector3 position)
        {
            if (biomeRegions == null || biomeRegions.Length == 0)
                return BiomeType.Temperate; // Default

            // Find closest biome region
            float closestDistance = float.MaxValue;
            BiomeType closestBiome = BiomeType.Temperate;

            for (int i = 0; i < biomeRegions.Length && i < availableBiomes.Length; i++)
            {
                float distance = Vector3.Distance(position, biomeRegions[i].position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestBiome = availableBiomes[i].biomeType;
                }
            }

            return closestBiome;
        }

        private float CalculateCurrentTemperature()
        {
            float seasonalModifier = seasonalCycle.Evaluate(seasonalTime % 1f) * seasonalVariation;
            return Mathf.Clamp01(globalTemperature + seasonalModifier - 0.5f);
        }

        private float CalculateLocalTemperature(BiomeType biome, Vector3 position)
        {
            float baseTemp = CalculateCurrentTemperature();
            var node = ecosystemNodes.ContainsKey(biome) ? ecosystemNodes[biome] : null;

            if (node != null && node.environmentalModifiers.TryGetValue(EnvironmentalModifierType.Temperature, out float tempModifier))
            {
                baseTemp += tempModifier * 0.1f; // Small biome-specific adjustment
            }

            return Mathf.Clamp01(baseTemp);
        }

        private float CalculateLocalHumidity(BiomeType biome, Vector3 position)
        {
            float baseHumidity = globalHumidity;
            var node = ecosystemNodes.ContainsKey(biome) ? ecosystemNodes[biome] : null;

            if (node != null && node.environmentalModifiers.TryGetValue(EnvironmentalModifierType.Humidity, out float humidityModifier))
            {
                baseHumidity += humidityModifier * 0.1f;
            }

            return Mathf.Clamp01(baseHumidity);
        }

        private Dictionary<ResourceType, float> CalculateResourceConsumption(CreatureGenome genome)
        {
            var consumption = new Dictionary<ResourceType, float>();
            float baseConsumption = genome.traits.ContainsKey(TraitType.Metabolism) ? genome.traits[TraitType.Metabolism].value : 1f;

            consumption[ResourceType.Food] = baseConsumption * 0.1f;
            consumption[ResourceType.Water] = baseConsumption * 0.05f;
            consumption[ResourceType.Shelter] = baseConsumption * 0.02f;

            return consumption;
        }

        private Dictionary<EnvironmentalModifierType, float> CalculateEnvironmentalImpact(CreatureGenome genome)
        {
            var impact = new Dictionary<EnvironmentalModifierType, float>();
            float size = genome.traits.ContainsKey(TraitType.Size) ? genome.traits[TraitType.Size].value : 1f;

            impact[EnvironmentalModifierType.Biodiversity] = -size * 0.01f; // Larger creatures have more impact
            impact[EnvironmentalModifierType.SoilQuality] = size * 0.005f; // But also contribute to soil health

            return impact;
        }

        private void CreateDefaultBiomes()
        {
            availableBiomes = new BiomeConfig[]
            {
                new BiomeConfig
                {
                    biomeType = BiomeType.Temperate,
                    carryingCapacityModifier = 1f,
                    baseResourceLevels = new Dictionary<ResourceType, float>
                    {
                        [ResourceType.Food] = 0.7f,
                        [ResourceType.Water] = 0.6f,
                        [ResourceType.Shelter] = 0.5f
                    }
                },
                new BiomeConfig
                {
                    biomeType = BiomeType.Desert,
                    carryingCapacityModifier = 0.5f,
                    baseResourceLevels = new Dictionary<ResourceType, float>
                    {
                        [ResourceType.Food] = 0.3f,
                        [ResourceType.Water] = 0.2f,
                        [ResourceType.Shelter] = 0.8f
                    }
                }
            };
        }

        private void CreateDefaultResourceConfigs()
        {
            resourceConfigs = new ResourceConfig[]
            {
                new ResourceConfig { resourceType = ResourceType.Food, maxAmount = 1000f, initialAmount = 800f, regenerationRate = 10f },
                new ResourceConfig { resourceType = ResourceType.Water, maxAmount = 800f, initialAmount = 700f, regenerationRate = 15f },
                new ResourceConfig { resourceType = ResourceType.Shelter, maxAmount = 500f, initialAmount = 400f, regenerationRate = 2f }
            };
        }

        private void CreateDefaultSeasonalCycle()
        {
            seasonalCycle = new AnimationCurve(
                new Keyframe(0f, 0f),      // Winter
                new Keyframe(0.25f, 0.5f), // Spring
                new Keyframe(0.5f, 1f),    // Summer
                new Keyframe(0.75f, 0.5f), // Fall
                new Keyframe(1f, 0f)       // Winter
            );
        }

        private void SubscribeToSystemEvents()
        {
            // Subscribe to genetic system events
            if (GeneticEvolutionManager.Instance != null)
            {
                GeneticEvolutionManager.Instance.OnEliteCreatureEmerged += HandleEliteCreatureEmerged;
            }

            // Subscribe to personality system events
            var personalityManager = PersonalityManager.GetInstance();
            if (personalityManager != null)
            {
                personalityManager.OnSocialInteraction += HandleSocialInteraction;
            }
        }

        private void HandleEliteCreatureEmerged(CreatureGenome eliteCreature)
        {
            if (eliteCreature != null)
            {
                // Elite creatures have positive environmental impact
                TriggerEnvironmentalEvent(EnvironmentalEventType.BiodiversityBoost, BiomeType.Temperate, 0.5f);
            }
        }

        private void HandleSocialInteraction(uint creatureA, uint creatureB, SocialInteractionType interactionType)
        {
            // Social interactions can affect local ecosystem health
            if (interactionType == SocialInteractionType.Cooperation)
            {
                // Positive interaction improves local environment slightly
                if (creatureEcosystemData.TryGetValue(creatureA, out var dataA))
                {
                    var node = ecosystemNodes.ContainsKey(dataA.currentBiome) ? ecosystemNodes[dataA.currentBiome] : null;
                    if (node != null)
                    {
                        node.health += 0.001f; // Very small improvement
                    }
                }
            }
        }

        private EcosystemHealth CalculateOverallHealth()
        {
            if (ecosystemNodes.Count == 0) return EcosystemHealth.Excellent;

            float averageHealth = ecosystemNodes.Values.Average(n => n.health);
            float resourceHealth = globalResources.Values.Average(r => r.currentAmount / r.maxAmount);
            float combinedHealth = (averageHealth + resourceHealth) / 2f;

            if (combinedHealth >= 0.8f) return EcosystemHealth.Excellent;
            if (combinedHealth >= 0.6f) return EcosystemHealth.Good;
            if (combinedHealth >= 0.4f) return EcosystemHealth.Fair;
            if (combinedHealth >= 0.2f) return EcosystemHealth.Poor;
            return EcosystemHealth.Critical;
        }

        private EcosystemStressLevel CalculateOverallStressLevel()
        {
            float stressValue = CalculateOverallStressValue();

            if (stressValue <= 0.2f) return EcosystemStressLevel.Low;
            if (stressValue <= 0.4f) return EcosystemStressLevel.Moderate;
            if (stressValue <= 0.7f) return EcosystemStressLevel.High;
            return EcosystemStressLevel.Critical;
        }

        private float CalculateOverallStressValue()
        {
            float biomeStress = ecosystemNodes.Values.Average(n => CalculateBiomeStress(n));
            float resourceStress = globalResources.Values.Average(r => 1f - (r.currentAmount / r.maxAmount));
            float eventStress = activeEvents.Count * 0.1f; // Each active event adds stress

            return (biomeStress + resourceStress + eventStress) / 3f;
        }

        private float CalculateBiomeStress(EcosystemNode node)
        {
            float healthStress = 1f - node.health;
            float resourceStress = node.resourceAvailability.Values.Average(r => r < resourceScarcityThreshold ? 1f : 0f);

            return (healthStress + resourceStress) / 2f;
        }

        private void UpdateAnalytics()
        {
            analytics.currentOverallHealth = CalculateOverallHealth();
            analytics.currentStressLevel = CalculateOverallStressLevel();
            analytics.totalActiveEvents = activeEvents.Count;
            analytics.totalRegisteredCreatures = creatureEcosystemData.Count;

            // Update debug data
            DebugManager.SetDebugData("Ecosystem.OverallHealth", analytics.currentOverallHealth);
            DebugManager.SetDebugData("Ecosystem.StressLevel", analytics.currentStressLevel);
            DebugManager.SetDebugData("Ecosystem.ActiveEvents", analytics.totalActiveEvents);
            DebugManager.SetDebugData("Ecosystem.Temperature", CalculateCurrentTemperature());
        }

        private float CalculateEnvironmentalModifier(ResourceType resourceType)
        {
            float modifier = 1f;

            // Calculate modifier based on environmental conditions
            foreach (var biome in activeBiomes.Values)
            {
                // Check biome health impact on resources
                switch (resourceType)
                {
                    case ResourceType.Water:
                        if (biome.biomeType == BiomeType.Wetland || biome.biomeType == BiomeType.River)
                            modifier += (biome.healthLevel - 0.5f) * 0.3f;
                        break;
                    case ResourceType.Food:
                        if (biome.biomeType == BiomeType.Forest || biome.biomeType == BiomeType.Grassland)
                            modifier += (biome.healthLevel - 0.5f) * 0.4f;
                        break;
                    case ResourceType.Shelter:
                        if (biome.biomeType == BiomeType.Cave || biome.biomeType == BiomeType.Forest)
                            modifier += (biome.healthLevel - 0.5f) * 0.2f;
                        break;
                    case ResourceType.Territory:
                        modifier += (biome.healthLevel - 0.5f) * 0.1f;
                        break;
                }
            }

            // Apply weather effects based on seasonal and temperature conditions
            float currentTemp = CalculateCurrentTemperature();
            float humidity = globalHumidity;
            float seasonalModifier = seasonalCycle.Evaluate(seasonalTime % 1f);

            switch (resourceType)
            {
                case ResourceType.Water:
                    // Rain and humidity affect water availability
                    if (humidity > 0.7f || seasonalModifier > 0.6f) // High humidity or wet season
                        modifier *= 1.2f;
                    else if (humidity < 0.3f && seasonalModifier < 0.3f) // Dry conditions
                        modifier *= 0.7f;
                    break;
                case ResourceType.Food:
                    // Extreme temperatures hurt food production
                    if (currentTemp > 0.8f || currentTemp < 0.2f)
                        modifier *= 0.8f;
                    // Growing season (spring/summer) boosts food
                    else if (seasonalModifier > 0.5f)
                        modifier *= 1.1f;
                    break;
                case ResourceType.Vegetation:
                    // Vegetation is highly seasonal
                    modifier *= 0.7f + (seasonalModifier * 0.6f); // Range 0.7-1.3
                    if (humidity > 0.6f) modifier *= 1.1f;
                    break;
                case ResourceType.Energy:
                    // Energy availability varies by season (sunlight)
                    modifier *= 0.8f + (seasonalModifier * 0.4f); // Range 0.8-1.2
                    break;
            }

            return Mathf.Clamp(modifier, 0.1f, 2f);
        }

        private void DistributeResourceToBiomes(ResourceType resourceType, float amount)
        {
            if (activeBiomes.Count == 0) return;

            float amountPerBiome = amount / activeBiomes.Count;

            foreach (var kvp in activeBiomes)
            {
                var biome = kvp.Value;

                // Each biome gets a share based on its capacity and health
                float distributedAmount = amountPerBiome * biome.healthLevel * biome.carryingCapacity;

                // Apply the resource to the biome
                var updatedBiome = biome;
                updatedBiome.ModifyResource((Laboratory.Chimera.Ecosystem.Data.ResourceType)resourceType, distributedAmount);
                activeBiomes[kvp.Key] = updatedBiome;

                // Update biome health based on resource availability
                UpdateBiomeHealthFromResource(updatedBiome, resourceType, distributedAmount);
            }
        }

        private void ApplyResourceToBiome(BiomeResourceState biome, ResourceType resourceType, float amount)
        {
            // Apply resource using the new BiomeResourceState methods
            var updatedBiome = biome;
            updatedBiome.ModifyResource((Laboratory.Chimera.Ecosystem.Data.ResourceType)resourceType, amount);
            activeBiomes[biome.biomeType] = updatedBiome;

            // Log significant resource changes
            if (amount > 0.1f)
            {
                Debug.Log($"Applied {amount:F2} {resourceType} to {biome.biomeType} biome");
            }
        }

        private void UpdateBiomeHealthFromResource(BiomeResourceState biome, ResourceType resourceType, float amount)
        {
            // Resource abundance affects biome health
            float healthImpact = 0f;

            float resourceLevel = biome.GetResourceLevel((Laboratory.Chimera.Ecosystem.Data.ResourceType)resourceType);
            float carryingCapacity = biome.carryingCapacity;

            if (carryingCapacity > 0)
            {
                float resourceRatio = resourceLevel / carryingCapacity;

                if (resourceRatio > 0.8f)
                    healthImpact = 0.01f; // Abundant resources boost health
                else if (resourceRatio < 0.2f)
                    healthImpact = -0.02f; // Scarce resources hurt health
            }

            float newHealth = Mathf.Clamp01(biome.healthLevel + healthImpact);
            var updatedBiome = biome;
            updatedBiome.healthLevel = newHealth;
            activeBiomes[biome.biomeType] = updatedBiome;

            // Trigger health change event if significant
            if (Mathf.Abs(healthImpact) > 0.005f)
            {
                OnBiomeHealthChanged?.Invoke(biome.biomeType, newHealth);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        // Editor menu items
#if UNITY_EDITOR
        [UnityEditor.MenuItem("🧪 Laboratory/Ecosystem/Trigger Random Environmental Event", false, 300)]
        private static void MenuTriggerRandomEvent()
        {
            if (Application.isPlaying && Instance != null)
            {
                var eventTypes = System.Enum.GetValues(typeof(EnvironmentalEventType)).Cast<EnvironmentalEventType>();
                var randomEventType = eventTypes.ElementAt(Random.Range(0, eventTypes.Count()));
                var biomes = System.Enum.GetValues(typeof(BiomeType)).Cast<BiomeType>();
                var randomBiome = biomes.ElementAt(Random.Range(0, biomes.Count()));

                Instance.TriggerEnvironmentalEvent(randomEventType, randomBiome, Random.Range(0.3f, 0.8f));
                Debug.Log($"Triggered {randomEventType} event in {randomBiome} biome");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Ecosystem/Analyze Ecosystem Stress", false, 301)]
        private static void MenuAnalyzeStress()
        {
            if (Application.isPlaying && Instance != null)
            {
                var analysis = Instance.AnalyzeEcosystemStress();
                Debug.Log($"Ecosystem Stress Analysis:\n" +
                         $"Overall Stress: {analysis.overallStressLevel}\n" +
                         $"Active Stress Factors: {analysis.stressFactors.Count}\n" +
                         $"Recommendations: {analysis.recommendations.Count}");
            }
        }
#endif

        /// <summary>
        /// Applies population stress effects to creatures in a specific biome
        /// </summary>
        private void ApplyPopulationStressToCreatures(BiomeType biomeType, float stressLevel)
        {
            // Find all creatures in this biome and apply stress effects
            var creaturesInBiome = creatureEcosystemData.Values.Where(data => data.currentBiome == biomeType);

            foreach (var creatureData in creaturesInBiome)
            {
                // Apply environmental pressure for migration consideration
                // Use a simplified stress calculation based on population density
                float migrationChance = stressLevel * 0.3f;

                if (Random.Range(0f, 1f) < migrationChance)
                {
                    // Trigger migration attempt
                    AttemptCreatureMigration(creatureData);
                }
            }

            DebugManager.LogInfo($"Applied population stress {stressLevel:F2} to {creaturesInBiome.Count()} creatures in {biomeType}");
        }

        /// <summary>
        /// Applies or removes ecosystem event effects
        /// </summary>
        private void ApplyEventEffects(ActiveEnvironmentalEvent ecosystemEvent, bool applying)
        {
            float effectMultiplier = applying ? 1f : -1f;
            float eventIntensity = ecosystemEvent.intensity;

            // Apply effects based on event type
            switch (ecosystemEvent.eventType)
            {
                case EnvironmentalEventType.Drought:
                    // Reduce water resources globally
                    foreach (var biomeKvp in activeBiomes.ToList())
                    {
                        var biome = biomeKvp.Value;
                        biome.SetResourceLevel(Laboratory.Chimera.Ecosystem.Data.ResourceType.Water,
                            biome.GetResourceLevel(Laboratory.Chimera.Ecosystem.Data.ResourceType.Water) * (1f + effectMultiplier * -0.3f));
                        activeBiomes[biomeKvp.Key] = biome;
                    }
                    globalHumidity = Mathf.Clamp01(globalHumidity + effectMultiplier * -0.2f);
                    break;

                case EnvironmentalEventType.Flood:
                    // Increase water but reduce food and shelter
                    foreach (var biomeKvp in activeBiomes.ToList())
                    {
                        var biome = biomeKvp.Value;
                        biome.SetResourceLevel(Laboratory.Chimera.Ecosystem.Data.ResourceType.Water,
                            biome.GetResourceLevel(Laboratory.Chimera.Ecosystem.Data.ResourceType.Water) * (1f + effectMultiplier * 0.4f));
                        biome.SetResourceLevel(Laboratory.Chimera.Ecosystem.Data.ResourceType.Food,
                            biome.GetResourceLevel(Laboratory.Chimera.Ecosystem.Data.ResourceType.Food) * (1f + effectMultiplier * -0.2f));
                        biome.SetResourceLevel(Laboratory.Chimera.Ecosystem.Data.ResourceType.Shelter,
                            biome.GetResourceLevel(Laboratory.Chimera.Ecosystem.Data.ResourceType.Shelter) * (1f + effectMultiplier * -0.3f));
                        activeBiomes[biomeKvp.Key] = biome;
                    }
                    break;

                case EnvironmentalEventType.Pestilence:
                    // Simulate disease effects through environmental impact (replacing Disease)
                    // Reduce biome health to represent disease spreading
                    foreach (var biomeKvp in activeBiomes.ToList())
                    {
                        var biome = biomeKvp.Value;
                        biome.healthLevel = Mathf.Max(0.1f, biome.healthLevel + effectMultiplier * -0.2f);
                        activeBiomes[biomeKvp.Key] = biome;
                    }
                    break;

                case EnvironmentalEventType.BiodiversityBoost:
                    // Temporarily increase carrying capacity and resource flow (replacing Migration)
                    foreach (var node in ecosystemNodes.Values)
                    {
                        node.carryingCapacityModifier += effectMultiplier * 0.2f;
                    }
                    // Increase all resources (replacing ResourceBonus)
                    foreach (var biomeKvp in activeBiomes.ToList())
                    {
                        var biome = biomeKvp.Value;
                        foreach (Laboratory.Chimera.Ecosystem.Data.ResourceType resourceType in System.Enum.GetValues(typeof(Laboratory.Chimera.Ecosystem.Data.ResourceType)))
                        {
                            float currentLevel = biome.GetResourceLevel(resourceType);
                            biome.SetResourceLevel(resourceType, currentLevel * (1f + effectMultiplier * 0.3f));
                        }
                        activeBiomes[biomeKvp.Key] = biome;
                    }
                    break;

                case EnvironmentalEventType.ClimateShift:
                    // Gradually shift temperature and affect biome health (replacing ClimateChange)
                    globalTemperature += effectMultiplier * eventIntensity * 0.1f;
                    foreach (var biomeKvp in activeBiomes.ToList())
                    {
                        var biome = biomeKvp.Value;
                        biome.healthLevel = Mathf.Clamp01(biome.healthLevel + effectMultiplier * -0.1f);
                        activeBiomes[biomeKvp.Key] = biome;
                    }
                    break;
            }

            DebugManager.LogInfo($"{(applying ? "Applied" : "Removed")} {ecosystemEvent.eventType} event effects");
        }

        /// <summary>
        /// Attempts to migrate a creature to a less crowded biome
        /// </summary>
        private void AttemptCreatureMigration(CreatureEcosystemData creatureData)
        {
            // Find biomes with lower population pressure
            var targetBiomes = activeBiomes.Where(kvp =>
                kvp.Key != creatureData.currentBiome &&
                biomePopulations.ContainsKey(kvp.Key) &&
                biomePopulations[kvp.Key].totalPopulation < ecosystemNodes[creatureData.currentBiome].carryingCapacityModifier * carryingCapacityBase * 0.8f
            ).ToList();

            if (targetBiomes.Any())
            {
                // Choose random target biome
                var targetBiome = targetBiomes[Random.Range(0, targetBiomes.Count)];

                var oldBiome = creatureData.currentBiome;

                // Update creature's biome
                creatureData.currentBiome = targetBiome.Key;

                // Update population tracking
                if (biomePopulations.ContainsKey(oldBiome))
                {
                    biomePopulations[oldBiome].totalPopulation--;
                    biomePopulations[oldBiome].totalBiomass -= creatureData.genome.traits.ContainsKey(TraitType.Size) ? creatureData.genome.traits[TraitType.Size].value : 1f;
                }

                if (!biomePopulations.ContainsKey(targetBiome.Key))
                {
                    biomePopulations[targetBiome.Key] = new PopulationData();
                }
                biomePopulations[targetBiome.Key].totalPopulation++;
                biomePopulations[targetBiome.Key].totalBiomass += creatureData.genome.traits.ContainsKey(TraitType.Size) ? creatureData.genome.traits[TraitType.Size].value : 1f;

                DebugManager.LogInfo($"Creature {creatureData.creatureId} migrated from {oldBiome} to {targetBiome.Key}");
            }
        }

        /// <summary>
        /// Calculates predation risk for a creature at a specific position
        /// </summary>
        private float CalculatePredationRisk(BiomeType biome, Vector3 position)
        {
            float basePredationRisk = 0.1f;

            // Adjust based on biome type
            switch (biome)
            {
                case BiomeType.Forest:
                    basePredationRisk = 0.3f; // Higher predation in forests
                    break;
                case BiomeType.Grassland:
                    basePredationRisk = 0.2f; // Moderate predation in open areas
                    break;
                case BiomeType.Desert:
                    basePredationRisk = 0.15f; // Lower due to fewer predators
                    break;
                case BiomeType.Cave:
                    basePredationRisk = 0.05f; // Safe from most predators
                    break;
                case BiomeType.Wetland:
                    basePredationRisk = 0.25f; // Aquatic predators
                    break;
                default:
                    basePredationRisk = 0.1f;
                    break;
            }

            // Adjust based on population density (more creatures = more predator attraction)
            if (biomePopulations.ContainsKey(biome))
            {
                float populationDensity = biomePopulations[biome].totalPopulation / (carryingCapacityBase * 1.5f);
                basePredationRisk *= (1f + populationDensity * 0.3f);
            }

            return Mathf.Clamp01(basePredationRisk);
        }

        /// <summary>
        /// Calculates competition level for resources at a specific position
        /// </summary>
        private float CalculateCompetitionLevel(BiomeType biome, Vector3 position)
        {
            float competitionLevel = 0f;

            if (biomePopulations.ContainsKey(biome) && ecosystemNodes.ContainsKey(biome))
            {
                var population = biomePopulations[biome];
                var node = ecosystemNodes[biome];

                // Calculate competition based on population vs carrying capacity
                float populationRatio = population.totalPopulation / (carryingCapacityBase * node.carryingCapacityModifier);
                competitionLevel = Mathf.Max(0f, populationRatio - 0.5f); // Competition starts when population exceeds 50% of capacity

                // Adjust based on resource scarcity
                float resourceScarcity = 0f;
                int resourceCount = 0;

                foreach (var resource in node.resourceAvailability)
                {
                    if (resource.Value < 0.3f) // Scarce resource threshold
                    {
                        resourceScarcity += (0.3f - resource.Value);
                        resourceCount++;
                    }
                }

                if (resourceCount > 0)
                {
                    competitionLevel += resourceScarcity / resourceCount;
                }
            }

            return Mathf.Clamp01(competitionLevel);
        }

        /// <summary>
        /// Calculates environmental stress for a biome
        /// </summary>
        private float CalculateEnvironmentalStress(BiomeType biome)
        {
            float environmentalStress = 0f;

            if (ecosystemNodes.ContainsKey(biome))
            {
                var node = ecosystemNodes[biome];

                // Base stress from biome health
                environmentalStress += (1f - node.health) * 0.4f;

                // Stress from resource depletion
                float totalResourceLevel = 0f;
                int resourceCount = 0;

                foreach (var resource in node.resourceAvailability)
                {
                    totalResourceLevel += resource.Value;
                    resourceCount++;
                }

                if (resourceCount > 0)
                {
                    float averageResourceLevel = totalResourceLevel / resourceCount;
                    environmentalStress += (1f - averageResourceLevel) * 0.3f;
                }

                // Stress from active environmental events
                int eventsAffectingBiome = activeEvents.Count(e => e.targetBiome == biome);
                environmentalStress += eventsAffectingBiome * 0.1f;

                // Temperature stress
                float currentTemp = CalculateCurrentTemperature();
                if (currentTemp < 0.2f || currentTemp > 0.8f) // Extreme temperatures
                {
                    environmentalStress += 0.2f;
                }
            }

            return Mathf.Clamp01(environmentalStress);
        }

        /// <summary>
        /// Calculates resource pressure on a biome node
        /// </summary>
        private float CalculateResourcePressure(EcosystemNode node)
        {
            float totalPressure = 0f;
            int resourceCount = 0;

            foreach (var resource in node.resourceAvailability)
            {
                // Pressure increases as resources become scarce
                float resourcePressure = 1f - resource.Value;
                totalPressure += resourcePressure;
                resourceCount++;
            }

            float averagePressure = resourceCount > 0 ? totalPressure / resourceCount : 0f;

            // Adjust based on environmental modifiers
            if (node.environmentalModifiers.ContainsKey(EnvironmentalModifierType.Temperature))
            {
                float tempModifier = Mathf.Abs(node.environmentalModifiers[EnvironmentalModifierType.Temperature] - 0.5f) * 2f;
                averagePressure += tempModifier * 0.1f;
            }

            if (node.environmentalModifiers.ContainsKey(EnvironmentalModifierType.Humidity))
            {
                float humidityModifier = Mathf.Abs(node.environmentalModifiers[EnvironmentalModifierType.Humidity] - 0.5f) * 2f;
                averagePressure += humidityModifier * 0.1f;
            }

            return Mathf.Clamp01(averagePressure);
        }
    }

    // Supporting data structures (simplified for length)
    [System.Serializable]
    public class EcosystemNode
    {
        public BiomeType biomeType;
        public float health = 1f;
        public float carryingCapacityModifier = 1f;
        public Dictionary<ResourceType, float> resourceAvailability = new Dictionary<ResourceType, float>();
        public Dictionary<EnvironmentalModifierType, float> environmentalModifiers = new Dictionary<EnvironmentalModifierType, float>();
    }

    [System.Serializable]
    public class GlobalResourceState
    {
        public float maxAmount;
        public float currentAmount;
        public float regenerationRate;
        public float totalConsumed;
        public float totalRegenerated;
    }

    [System.Serializable]
    public class PopulationData
    {
        public int totalPopulation;
        public float totalBiomass;
        public float averageFitness;
    }

    [System.Serializable]
    public class CreatureEcosystemData
    {
        public uint creatureId;
        public BiomeType currentBiome;
        public Vector3 position;
        public CreatureGenome genome;
        public Dictionary<ResourceType, float> resourceConsumption;
        public Dictionary<EnvironmentalModifierType, float> environmentalImpact;
        public float registrationTime;
    }

    [System.Serializable]
    public class EnvironmentalConditions
    {
        public float temperature;
        public float humidity;
        public Dictionary<ResourceType, float> resourceAvailability;
        public float predationRisk;
        public float competitionLevel;
        public float environmentalStress;
    }

    [System.Serializable]
    public class EcosystemAnalytics
    {
        public EcosystemHealth currentOverallHealth;
        public EcosystemStressLevel currentStressLevel;
        public int totalEnvironmentalEvents;
        public int totalActiveEvents;
        public int totalRegisteredCreatures;
        public float totalResourcesConsumed;
    }

    public enum ResourceType { Food, Water, Shelter, Territory, Medicine, Energy, Minerals, Vegetation }
    public enum EnvironmentalEventType { Drought, Flood, Pestilence, BiodiversityBoost, ClimateShift }
    public enum EnvironmentalModifierType { Temperature, Humidity, Biodiversity, SoilQuality }
    public enum EcosystemStressLevel { Low, Moderate, High, Critical }

    [System.Serializable]
    public class BiomeConfig
    {
        public BiomeType biomeType;
        public float carryingCapacityModifier = 1f;
        public Dictionary<ResourceType, float> baseResourceLevels;
        public EnvironmentalModifier[] environmentalModifiers;
    }

    [System.Serializable]
    public class ResourceConfig
    {
        public ResourceType resourceType;
        public float maxAmount = 1000f;
        public float initialAmount = 800f;
        public float regenerationRate = 10f;
    }

    [System.Serializable]
    public class EnvironmentalEvent
    {
        public EnvironmentalEventType eventType;
        public string eventName;
        public string description;
        public EnvironmentalEffect[] effects;
    }

    [System.Serializable]
    public class EnvironmentalEffect
    {
        public EnvironmentalModifierType modifierType;
        public float effectStrength;
        public float duration;
    }

    [System.Serializable]
    public class EnvironmentalModifier
    {
        public EnvironmentalModifierType modifierType;
        public float value;
    }

    [System.Serializable]
    public class ActiveEnvironmentalEvent
    {
        public EnvironmentalEventType eventType;
        public BiomeType targetBiome;
        public float intensity;
        public float startTime;
        public float duration;
        public EnvironmentalEffect[] effects;
    }

    [System.Serializable]
    public class EcosystemStressAnalysis
    {
        public float timestamp;
        public EcosystemStressLevel overallStressLevel;
        public Dictionary<BiomeType, float> biomeStressLevels;
        public Dictionary<ResourceType, float> resourceStressLevels;
        public List<EcosystemStressFactor> stressFactors;
        public List<EcosystemRecommendation> recommendations;
    }

    /// <summary>
    /// Overall health status of the ecosystem
    /// </summary>
    public enum EcosystemHealth
    {
        Critical = 0,
        Poor = 1,
        Fair = 2,
        Average = 3,
        Good = 4,
        Excellent = 5
    }
}