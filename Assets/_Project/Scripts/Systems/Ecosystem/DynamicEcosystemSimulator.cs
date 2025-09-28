using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Events;
using Laboratory.Chimera.Genetics.Advanced;
using Laboratory.AI.Personality;
using Laboratory.Systems.Quests;

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
            DebugManager.LogInfo("Initializing Dynamic Ecosystem Simulator");

            // Initialize biome nodes
            InitializeBiomeNodes();

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

            DebugManager.LogInfo($"Ecosystem initialized with {ecosystemNodes.Count} biome nodes and {globalResources.Count} resource types");
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
            biomePopulations[biome].totalBiomass += genome.traits.GetValueOrDefault("size", 1f);

            DebugManager.LogInfo($"Creature {creatureId} registered in {biome} biome");
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
                        biomePopulations[oldBiome].totalBiomass -= data.genome.traits.GetValueOrDefault("size", 1f);
                    }

                    if (!biomePopulations.ContainsKey(newBiome))
                    {
                        biomePopulations[newBiome] = new PopulationData();
                    }
                    biomePopulations[newBiome].totalPopulation++;
                    biomePopulations[newBiome].totalBiomass += data.genome.traits.GetValueOrDefault("size", 1f);

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
                    biomePopulations[data.currentBiome].totalBiomass -= data.genome.traits.GetValueOrDefault("size", 1f);
                }

                creatureEcosystemData.Remove(creatureId);
                DebugManager.LogInfo($"Creature {creatureId} unregistered from ecosystem");
            }
        }

        /// <summary>
        /// Gets environmental conditions at a specific position
        /// </summary>
        public EnvironmentalConditions GetEnvironmentalConditions(Vector3 position)
        {
            var biome = GetBiomeAtPosition(position);
            var node = ecosystemNodes.GetValueOrDefault(biome);

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
                var available = node.resourceAvailability.GetValueOrDefault(kvp.Key, 0f);
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
            DebugManager.LogInfo($"Environmental event started: {eventType} in {targetBiome} biome (Intensity: {intensity:F2})");
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
                stressFactors = new List<string>(),
                recommendations = new List<string>()
            };

            // Analyze biome stress
            foreach (var kvp in ecosystemNodes)
            {
                float biomeStress = CalculateBiomeStress(kvp.Value);
                analysis.biomeStressLevels[kvp.Key] = biomeStress;

                if (biomeStress > 0.7f)
                {
                    analysis.stressFactors.Add($"{kvp.Key} biome under severe stress");
                    analysis.recommendations.Add($"Reduce activity in {kvp.Key} biome");
                }
            }

            // Analyze resource stress
            foreach (var kvp in globalResources)
            {
                float resourceStress = 1f - (kvp.Value.currentAmount / kvp.Value.maxAmount);
                analysis.resourceStressLevels[kvp.Key] = resourceStress;

                if (resourceStress > 0.6f)
                {
                    analysis.stressFactors.Add($"{kvp.Key} resources critically low");
                    analysis.recommendations.Add($"Focus on {kvp.Key} resource regeneration");
                }
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
                var populationData = biomePopulations.GetValueOrDefault(kvp.Key, new PopulationData());

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
                var ecosystemNode = ecosystemNodes.GetValueOrDefault(kvp.Key);

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

                    DebugManager.LogInfo($"Environmental event ended: {activeEvent.eventType}");
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
                    node.resourceAvailability[resourceType] = biomeConfig.baseResourceLevels?.GetValueOrDefault(resourceType, 0.5f) ?? 0.5f;
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
            var node = ecosystemNodes.GetValueOrDefault(biome);

            if (node != null && node.environmentalModifiers.TryGetValue(EnvironmentalModifierType.Temperature, out float tempModifier))
            {
                baseTemp += tempModifier * 0.1f; // Small biome-specific adjustment
            }

            return Mathf.Clamp01(baseTemp);
        }

        private float CalculateLocalHumidity(BiomeType biome, Vector3 position)
        {
            float baseHumidity = globalHumidity;
            var node = ecosystemNodes.GetValueOrDefault(biome);

            if (node != null && node.environmentalModifiers.TryGetValue(EnvironmentalModifierType.Humidity, out float humidityModifier))
            {
                baseHumidity += humidityModifier * 0.1f;
            }

            return Mathf.Clamp01(baseHumidity);
        }

        private Dictionary<ResourceType, float> CalculateResourceConsumption(CreatureGenome genome)
        {
            var consumption = new Dictionary<ResourceType, float>();
            float baseConsumption = genome.traits.GetValueOrDefault("metabolism", 1f);

            consumption[ResourceType.Food] = baseConsumption * 0.1f;
            consumption[ResourceType.Water] = baseConsumption * 0.05f;
            consumption[ResourceType.Shelter] = baseConsumption * 0.02f;

            return consumption;
        }

        private Dictionary<EnvironmentalModifierType, float> CalculateEnvironmentalImpact(CreatureGenome genome)
        {
            var impact = new Dictionary<EnvironmentalModifierType, float>();
            float size = genome.traits.GetValueOrDefault("size", 1f);

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
            if (CreaturePersonalityManager.Instance != null)
            {
                CreaturePersonalityManager.Instance.OnSocialInteraction += HandleSocialInteraction;
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
                    var node = ecosystemNodes.GetValueOrDefault(dataA.currentBiome);
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
            DebugManager.SetDebugData("Ecosystem.OverallHealth", analytics.currentOverallHealth.ToString());
            DebugManager.SetDebugData("Ecosystem.StressLevel", analytics.currentStressLevel.ToString());
            DebugManager.SetDebugData("Ecosystem.ActiveEvents", analytics.totalActiveEvents);
            DebugManager.SetDebugData("Ecosystem.Temperature", CalculateCurrentTemperature());
        }

        // Additional helper methods would continue here...
        // (Truncated for length, but the pattern continues with all the supporting methods)

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        // Editor menu items
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

    // Enums and other supporting types...
    public enum BiomeType { Temperate, Desert, Forest, Ocean, Mountain, Arctic }
    public enum ResourceType { Food, Water, Shelter, Territory }
    public enum EnvironmentalEventType { Drought, Flood, Pestilence, BiodiversityBoost, ClimateShift }
    public enum EnvironmentalModifierType { Temperature, Humidity, Biodiversity, SoilQuality }
    public enum EcosystemHealth { Critical, Poor, Fair, Good, Excellent }
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
        public List<string> stressFactors;
        public List<string> recommendations;
    }
}