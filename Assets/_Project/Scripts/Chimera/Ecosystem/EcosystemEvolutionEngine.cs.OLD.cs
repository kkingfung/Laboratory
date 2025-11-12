using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Laboratory.Chimera.Ecosystem.Core;
using Laboratory.Chimera.Core;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;

namespace Laboratory.Chimera.Ecosystem
{
    /// <summary>
    /// Advanced ecosystem evolution and biome dynamics engine that simulates complex
    /// environmental interactions, climate systems, resource flows, and emergent ecological
    /// phenomena with temporal and spatial evolution patterns.
    /// </summary>
    [CreateAssetMenu(fileName = "EcosystemEvolutionEngine", menuName = "Chimera/Ecosystem/Evolution Engine")]
    public class EcosystemEvolutionEngine : ScriptableObject
    {
        [Header("Ecosystem Configuration")]
        [SerializeField] private int maxBiomes = 20;
        [SerializeField] private float evolutionRate = 0.01f;
        [SerializeField] private bool enableClimateChange = true;
        [SerializeField] private bool enableSeasonalCycles = true;

        [Header("Biome Dynamics")]
        [SerializeField] private float biomeTransitionRate = 0.005f;
        [SerializeField] private float carryingCapacityFlexibility = 0.3f;
        [SerializeField] private float biomeMigrationThreshold = 0.7f;

        [Header("Climate System")]
        [SerializeField] private float globalTemperatureVariance = 2f;
        [SerializeField] private float precipitationVariance = 0.3f;
        [SerializeField] private float seasonalIntensity = 0.5f;
        [SerializeField] private float climateStabilityFactor = 0.8f;

        [Header("Resource Management")]
        [SerializeField] private int resourceTypes = 8;

        [Header("Ecological Interactions")]
        [SerializeField] private float predatorPreyBalance = 0.6f;
        [SerializeField] private float competitionIntensity = 0.4f;
        [SerializeField] private float symbiosisRate = 0.2f;
        [SerializeField] private float extinctionThreshold = 0.05f;

        [Header("Temporal Dynamics")]
        [SerializeField] private float timeScale = 1f;
        [SerializeField] private bool enableGeologicalTime = false;
        [SerializeField] private float catastropheFrequency = 0.001f;

        // Core ecosystem data structures
        private Dictionary<uint, Biome> activeBiomes = new Dictionary<uint, Biome>();
        private Dictionary<uint, EcosystemNode> ecosystemNodes = new Dictionary<uint, EcosystemNode>();
        private ClimateSystem globalClimate = new ClimateSystem();
        private List<EcologicalRelationship> foodWeb = new List<EcologicalRelationship>();

        // Environmental systems
        private ResourceNetwork resourceNetwork;
        private BiomeTransitionMatrix transitionMatrix;
        private SeasonalCycleManager seasonalManager;
        private ClimateEvolutionEngine climateEngine;

        // Ecological processes
        private SuccessionManager successionManager;
        private MigrationManager migrationManager;
        private ExtinctionPreventionSystem extinctionPrevention;
        private BiodiversityTracker biodiversityTracker;

        // Temporal and spatial dynamics
        private TemporalEvolutionEngine temporalEngine;
        private SpatialConnectivityManager spatialManager;
        private CatastropheManager catastropheManager;

        // Monitoring and analytics
        private EcosystemMetrics globalMetrics = new EcosystemMetrics();
        private List<EcologicalEvent> eventHistory = new List<EcologicalEvent>();
        private Dictionary<uint, BiomeHealth> biomeHealthMetrics = new Dictionary<uint, BiomeHealth>();

        public event Action<uint, BiomeType, BiomeType> OnBiomeTransition;
        public event Action<EcologicalCatastrophe> OnCatastrophicEvent;
        public event Action<uint, string> OnSpeciesExtinction;
        public event Action<float> OnClimateShift;

        private void OnEnable()
        {
            InitializeEcosystemEngine();
            UnityEngine.Debug.Log("Ecosystem Evolution Engine initialized");
        }

        private void InitializeEcosystemEngine()
        {
            globalClimate = new ClimateSystem
            {
                globalTemperature = 15f, // Celsius
                globalPrecipitation = 1000f, // mm/year
                atmosphericCO2 = 400f, // ppm
                seasonalVariation = seasonalIntensity,
                climateStability = climateStabilityFactor
            };

            resourceNetwork = new ResourceNetwork(resourceTypes);
            transitionMatrix = new BiomeTransitionMatrix();
            seasonalManager = new SeasonalCycleManager(enableSeasonalCycles);
            climateEngine = new ClimateEvolutionEngine(globalTemperatureVariance, precipitationVariance);

            successionManager = new SuccessionManager();
            migrationManager = new MigrationManager(biomeMigrationThreshold);
            extinctionPrevention = new ExtinctionPreventionSystem(extinctionThreshold);
            biodiversityTracker = new BiodiversityTracker();

            temporalEngine = new TemporalEvolutionEngine(timeScale, enableGeologicalTime);
            spatialManager = new SpatialConnectivityManager();
            catastropheManager = new CatastropheManager(catastropheFrequency);

            globalMetrics = new EcosystemMetrics();
            InitializeBiomeTransitionMatrix();
            UnityEngine.Debug.Log("Ecosystem subsystems initialized");
        }

        private void InitializeBiomeTransitionMatrix()
        {
            // Define biome transition probabilities based on climate conditions
            transitionMatrix.SetTransition(BiomeType.Grassland, BiomeType.Forest, 0.3f, new ClimateCondition { temperature = 15f, precipitation = 1200f });
            transitionMatrix.SetTransition(BiomeType.Grassland, BiomeType.Desert, 0.2f, new ClimateCondition { temperature = 25f, precipitation = 300f });
            transitionMatrix.SetTransition(BiomeType.Forest, BiomeType.Grassland, 0.2f, new ClimateCondition { temperature = 20f, precipitation = 800f });
            transitionMatrix.SetTransition(BiomeType.Desert, BiomeType.Grassland, 0.1f, new ClimateCondition { temperature = 18f, precipitation = 600f });
            transitionMatrix.SetTransition(BiomeType.Tundra, BiomeType.Temperate, 0.4f, new ClimateCondition { temperature = 0f, precipitation = 400f });
            transitionMatrix.SetTransition(BiomeType.Swamp, BiomeType.Forest, 0.3f, new ClimateCondition { temperature = 15f, precipitation = 1500f });
        }

        /// <summary>
        /// Creates a new biome with specified characteristics and initial conditions
        /// </summary>
        public Biome CreateBiome(BiomeType biomeType, Vector3 location, float area, Dictionary<string, float> initialConditions)
        {
            if (activeBiomes.Count >= maxBiomes)
            {
                UnityEngine.Debug.LogWarning("Maximum biome limit reached");
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
                climateConditions = GenerateClimateConditions(biomeType, location),
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
            InitializeSeasonalModifiers(biome);

            // Add to ecosystem
            activeBiomes[biomeId] = biome;
            ecosystemNodes[biomeId] = new EcosystemNode
            {
                nodeId = biomeId,
                biome = biome,
                connections = new List<uint>(),
                migrationRoutes = new List<MigrationRoute>(),
                resourceFlows = new Dictionary<string, float>()
            };

            // Initialize biome health metrics
            biomeHealthMetrics[biomeId] = new BiomeHealth
            {
                overallHealth = 0.8f,
                speciesDiversity = 0f,
                resourceAvailability = 1f,
                climateStress = 0.2f,
                humanImpact = 0f,
                resilienceScore = 0.7f
            };

            UnityEngine.Debug.Log($"Biome {biomeId} ({biomeType}) created at {location} with area {area:F1}");
            return biome;
        }

        private ClimateCondition GenerateClimateConditions(BiomeType biomeType, Vector3 location)
        {
            // Base climate conditions modified by global climate and location
            var baseConditions = GetBaseClimateConditions(biomeType);

            // Apply global climate influence
            baseConditions.temperature += globalClimate.globalTemperature - 15f; // Adjust from baseline
            baseConditions.precipitation *= globalClimate.globalPrecipitation / 1000f; // Adjust from baseline

            // Apply location-based modifiers (latitude, altitude, etc.)
            ApplyLocationModifiers(baseConditions, location);

            return baseConditions;
        }

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

        private void ApplyLocationModifiers(ClimateCondition conditions, Vector3 location)
        {
            // Latitude effect (approximation)
            float latitudeEffect = math.abs(location.z) / 100f; // Assuming z represents latitude proxy
            conditions.temperature -= latitudeEffect * 0.6f; // Cooler at higher latitudes

            // Altitude effect
            float altitudeEffect = location.y / 1000f; // Assuming y represents altitude in meters
            conditions.temperature -= altitudeEffect * 6.5f; // 6.5°C per 1000m altitude

            // Ensure reasonable bounds
            conditions.temperature = math.clamp(conditions.temperature, -30f, 40f);
            conditions.precipitation = math.clamp(conditions.precipitation, 50f, 4000f);
        }

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

        private void InitializeSeasonalModifiers(Biome biome)
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

        /// <summary>
        /// Processes ecosystem evolution for a single time step
        /// </summary>
        public void UpdateEcosystemEvolution(float deltaTime)
        {
            float scaledDeltaTime = deltaTime * timeScale;

            // Update global systems
            UpdateGlobalClimate(scaledDeltaTime);
            UpdateSeasonalCycles(scaledDeltaTime);

            // Update biome-level processes
            UpdateBiomeEvolution(scaledDeltaTime);
            UpdateResourceDynamics(scaledDeltaTime);

            // Update ecological interactions
            UpdateSpeciesInteractions(scaledDeltaTime);
            UpdateMigrationPatterns(scaledDeltaTime);

            // Update spatial connectivity
            UpdateSpatialConnectivity(scaledDeltaTime);

            // Process ecological succession
            ProcessEcologicalSuccession(scaledDeltaTime);

            // Handle catastrophic events
            ProcessCatastrophicEvents(scaledDeltaTime);

            // Update biodiversity and ecosystem health
            UpdateBiodiversityMetrics();
            UpdateEcosystemHealth();

            // Update global metrics
            UpdateGlobalMetrics();
        }

        private void UpdateGlobalClimate(float deltaTime)
        {
            if (!enableClimateChange) return;

            climateEngine.UpdateClimate(globalClimate, deltaTime);

            // Check for significant climate changes (using climate engine's change rate for now)
            float changeRate = climateEngine.GetClimateChangeRate();
            if (changeRate > 0.1f)
            {
                OnClimateShift?.Invoke(changeRate);
                UnityEngine.Debug.Log($"Climate shift detected: Change rate={changeRate:F2}");
            }

            // Update all biome climate conditions
            foreach (var biome in activeBiomes.Values)
            {
                var climateChange = new ClimateChange
                {
                    temperatureChange = changeRate * UnityEngine.Random.Range(-1f, 1f),
                    precipitationChange = changeRate * UnityEngine.Random.Range(-0.5f, 0.5f),
                    humidityChange = changeRate * UnityEngine.Random.Range(-0.3f, 0.3f)
                };
                ApplyClimateChangeToBiome(biome, climateChange);
            }
        }

        private void UpdateSeasonalCycles(float deltaTime)
        {
            if (!enableSeasonalCycles) return;

            seasonalManager.UpdateSeason(deltaTime);
            var currentSeason = seasonalManager.GetCurrentSeason();

            // Apply seasonal effects to all biomes
            foreach (var biome in activeBiomes.Values)
            {
                if (biome.seasonalModifiers.TryGetValue(currentSeason, out var modifier))
                {
                    ApplySeasonalModifiers(biome, modifier);
                }
            }
        }

        private void ApplySeasonalModifiers(Biome biome, SeasonalModifier modifier)
        {
            // Temporary climate modifications for the season
            biome.climateConditions.temperature += modifier.temperatureModifier;
            biome.climateConditions.precipitation *= modifier.precipitationModifier;

            // Resource availability changes
            foreach (var resource in biome.resources.Values)
            {
                resource.currentAmount *= modifier.resourceModifier;
                resource.currentAmount = math.clamp(resource.currentAmount, 0f, resource.maxAmount);
            }
        }

        private void UpdateBiomeEvolution(float deltaTime)
        {
            var biomesToTransition = new List<(uint biomeId, BiomeType newType)>();

            foreach (var biome in activeBiomes.Values)
            {
                // Check for biome transitions based on climate conditions
                var possibleTransition = transitionMatrix.EvaluateTransition(biome.biomeType, biome.climateConditions);

                if (possibleTransition.HasValue && UnityEngine.Random.value < biomeTransitionRate * deltaTime)
                {
                    biomesToTransition.Add((biome.biomeId, possibleTransition.Value));
                }

                // Update biome stability based on disturbances and climate stress
                UpdateBiomeStability(biome, deltaTime);

                // Progress ecological succession
                UpdateSuccessionStage(biome, deltaTime);
            }

            // Execute biome transitions
            foreach (var (biomeId, newType) in biomesToTransition)
            {
                TransitionBiome(biomeId, newType);
            }
        }

        private void UpdateBiomeStability(Biome biome, float deltaTime)
        {
            float stabilityChange = 0f;

            // Climate stress affects stability
            float climateStress = CalculateClimateStress(biome);
            stabilityChange -= climateStress * 0.1f * deltaTime;

            // Species diversity supports stability
            float diversityBonus = biome.biodiversityIndex * 0.05f * deltaTime;
            stabilityChange += diversityBonus;

            // Recent disturbances reduce stability
            float recentDisturbances = biome.disturbanceHistory
                .Count(d => Time.time - d.timestamp < 100f); // Events in last 100 time units
            stabilityChange -= recentDisturbances * 0.02f * deltaTime;

            biome.stabilityIndex = math.clamp(biome.stabilityIndex + stabilityChange, 0f, 1f);
        }

        private float CalculateClimateStress(Biome biome)
        {
            var optimalConditions = GetBaseClimateConditions(biome.biomeType);

            float temperatureStress = math.abs(biome.climateConditions.temperature - optimalConditions.temperature) / 10f;
            float precipitationStress = math.abs(biome.climateConditions.precipitation - optimalConditions.precipitation) / 500f;

            return math.clamp((temperatureStress + precipitationStress) / 2f, 0f, 1f);
        }

        private void UpdateSuccessionStage(Biome biome, float deltaTime)
        {
            // Succession progresses based on time, stability, and species establishment
            float successionRate = evolutionRate * biome.stabilityIndex * deltaTime;

            if (UnityEngine.Random.value < successionRate)
            {
                biome.successionStage = successionManager.AdvanceSuccession(biome.successionStage, biome);
            }
        }

        private void TransitionBiome(uint biomeId, BiomeType newType)
        {
            if (!activeBiomes.TryGetValue(biomeId, out var biome))
                return;

            var oldType = biome.biomeType;
            biome.biomeType = newType;

            // Update climate conditions for new biome type
            var newClimateBase = GetBaseClimateConditions(newType);
            biome.climateConditions.temperature = newClimateBase.temperature;
            biome.climateConditions.precipitation = newClimateBase.precipitation;
            biome.climateConditions.humidity = newClimateBase.humidity;

            // Update carrying capacity
            biome.carryingCapacity = CalculateCarryingCapacity(newType, biome.area);

            // Reset succession stage
            biome.successionStage = SuccessionStage.Pioneer;

            // Add transition event to history
            var transitionEvent = new EcologicalEvent
            {
                eventType = (EventType)EcologicalEventType.BiomeTransition,
                timestamp = Time.time,
                affectedBiomes = new List<uint> { biomeId },
                description = $"Biome transition from {oldType} to {newType}",
                severity = (float)EventSeverity.Moderate
            };

            eventHistory.Add(transitionEvent);
            OnBiomeTransition?.Invoke(biomeId, oldType, newType);

            UnityEngine.Debug.Log($"Biome {biomeId} transitioned from {oldType} to {newType}");
        }

        private void UpdateResourceDynamics(float deltaTime)
        {
            foreach (var biome in activeBiomes.Values)
            {
                resourceNetwork.UpdateBiomeResources(biome, deltaTime);

                // Resource regeneration
                foreach (var resource in biome.resources.Values)
                {
                    float regenerationAmount = resource.regenerationRate * deltaTime;
                    resource.currentAmount = math.min(resource.maxAmount, resource.currentAmount + regenerationAmount);
                }

                // Resource depletion from species consumption
                ApplyResourceConsumption(biome, deltaTime);
            }

            // Update resource flows between connected biomes
            UpdateResourceFlows(deltaTime);
        }

        private void ApplyResourceConsumption(Biome biome, float deltaTime)
        {
            foreach (var species in biome.species)
            {
                var consumptionRate = CalculateSpeciesConsumption(species);

                foreach (var resourceType in species.resourceRequirements.Keys)
                {
                    if (biome.resources.TryGetValue(resourceType, out var resource))
                    {
                        float consumption = consumptionRate * species.population * deltaTime;
                        resource.currentAmount = math.max(0f, resource.currentAmount - consumption);
                    }
                }
            }
        }

        private float CalculateSpeciesConsumption(EcosystemSpeciesData species)
        {
            // Base consumption modified by species traits and environmental factors
            return species.metabolicRate * species.bodySize * species.activityLevel;
        }

        private void UpdateResourceFlows(float deltaTime)
        {
            foreach (var node in ecosystemNodes.Values)
            {
                foreach (var flowPair in node.resourceFlows)
                {
                    var resourceFlow = new ResourceFlow
                    {
                        resourceType = flowPair.Key,
                        flowRate = flowPair.Value,
                        sourceNode = node.nodeId,
                        destinationNode = 0, // Default destination
                        efficiency = 1.0f
                    };
                    resourceNetwork.ProcessResourceFlow(resourceFlow, deltaTime);
                }
            }
        }

        private void UpdateSpeciesInteractions(float deltaTime)
        {
            foreach (var biome in activeBiomes.Values)
            {
                // Process intra-biome species interactions
                ProcessSpeciesCompetition(biome, deltaTime);
                ProcessPredatorPreyRelationships(biome, deltaTime);
                ProcessSymbioticRelationships(biome, deltaTime);

                // Update species populations based on interactions
                UpdateEcosystemSpeciesDatas(biome, deltaTime);
            }
        }

        private void ProcessSpeciesCompetition(Biome biome, float deltaTime)
        {
            for (int i = 0; i < biome.species.Count; i++)
            {
                for (int j = i + 1; j < biome.species.Count; j++)
                {
                    var speciesA = biome.species[i];
                    var speciesB = biome.species[j];

                    float competitionStrength = CalculateCompetitionStrength(speciesA, speciesB);

                    if (competitionStrength > 0.5f)
                    {
                        ApplyCompetitionPressure(speciesA, speciesB, competitionStrength, deltaTime);
                    }
                }
            }
        }

        private float CalculateCompetitionStrength(EcosystemSpeciesData speciesA, EcosystemSpeciesData speciesB)
        {
            // Competition based on resource overlap and niche similarity
            float resourceOverlap = CalculateResourceOverlap(speciesA.resourceRequirements, speciesB.resourceRequirements);
            float nicheOverlap = CalculateNicheOverlap(speciesA.ecologicalNiche, speciesB.ecologicalNiche);

            return (resourceOverlap + nicheOverlap) / 2f * competitionIntensity;
        }

        private float CalculateResourceOverlap(Dictionary<string, float> requirementsA, Dictionary<string, float> requirementsB)
        {
            float overlap = 0f;
            int commonResources = 0;

            foreach (var resourceA in requirementsA)
            {
                if (requirementsB.TryGetValue(resourceA.Key, out var requirementB))
                {
                    overlap += math.min(resourceA.Value, requirementB) / math.max(resourceA.Value, requirementB);
                    commonResources++;
                }
            }

            return commonResources > 0 ? overlap / commonResources : 0f;
        }

        private float CalculateNicheOverlap(EcologicalNiche nicheA, EcologicalNiche nicheB)
        {
            float overlap = 0f;

            // Compare habitat preferences
            overlap += 1f - math.abs(nicheA.habitatSpecialization - nicheB.habitatSpecialization);

            // Compare feeding strategies
            if (nicheA.feedingStrategy == nicheB.feedingStrategy)
                overlap += 0.5f;

            // Compare activity patterns
            if (nicheA.activityPattern == nicheB.activityPattern)
                overlap += 0.3f;

            return overlap / 1.8f; // Normalize to 0-1
        }

        private void ApplyCompetitionPressure(EcosystemSpeciesData speciesA, EcosystemSpeciesData speciesB, float competitionStrength, float deltaTime)
        {
            float pressureA = competitionStrength * (speciesB.population / speciesA.population) * deltaTime * 0.1f;
            float pressureB = competitionStrength * (speciesA.population / speciesB.population) * deltaTime * 0.1f;

            speciesA.populationGrowthRate -= pressureA;
            speciesB.populationGrowthRate -= pressureB;

            // Record competitive pressure in species stress
            speciesA.environmentalStress += pressureA;
            speciesB.environmentalStress += pressureB;
        }

        private void ProcessPredatorPreyRelationships(Biome biome, float deltaTime)
        {
            var predatorPreyPairs = foodWeb.Where(r => r.relationshipType == RelationshipType.Predation).ToList();

            foreach (var relationship in predatorPreyPairs)
            {
                var predator = biome.species.FirstOrDefault(s => s.speciesId == relationship.speciesA);
                var prey = biome.species.FirstOrDefault(s => s.speciesId == relationship.speciesB);

                if (predator != null && prey != null)
                {
                    ProcessPredationInteraction(predator, prey, relationship.strength, deltaTime);
                }
            }
        }

        private void ProcessPredationInteraction(EcosystemSpeciesData predator, EcosystemSpeciesData prey, float relationshipStrength, float deltaTime)
        {
            // Lotka-Volterra inspired dynamics
            float predationRate = relationshipStrength * predatorPreyBalance * deltaTime;
            float preyConsumed = predationRate * predator.population * prey.population / (prey.population + 1000f);

            // Reduce prey population
            prey.population = math.max(0f, prey.population - preyConsumed);

            // Benefit predator population growth
            predator.populationGrowthRate += preyConsumed * 0.001f; // Conversion efficiency

            // Update predator hunting success
            predator.huntingSuccess = math.clamp(preyConsumed / (predator.population + 1f), 0f, 1f);
        }

        private void ProcessSymbioticRelationships(Biome biome, float deltaTime)
        {
            var symbioticPairs = foodWeb.Where(r => r.relationshipType == RelationshipType.Mutualism).ToList();

            foreach (var relationship in symbioticPairs)
            {
                var speciesA = biome.species.FirstOrDefault(s => s.speciesId == relationship.speciesA);
                var speciesB = biome.species.FirstOrDefault(s => s.speciesId == relationship.speciesB);

                if (speciesA != null && speciesB != null)
                {
                    ProcessSymbioticInteraction(speciesA, speciesB, relationship.strength, deltaTime);
                }
            }
        }

        private void ProcessSymbioticInteraction(EcosystemSpeciesData speciesA, EcosystemSpeciesData speciesB, float relationshipStrength, float deltaTime)
        {
            float symbiosisBonus = relationshipStrength * symbiosisRate * deltaTime;

            // Both species benefit from the relationship
            speciesA.populationGrowthRate += symbiosisBonus;
            speciesB.populationGrowthRate += symbiosisBonus;

            // Reduce environmental stress through cooperation
            speciesA.environmentalStress = math.max(0f, speciesA.environmentalStress - symbiosisBonus);
            speciesB.environmentalStress = math.max(0f, speciesB.environmentalStress - symbiosisBonus);
        }

        private void UpdateEcosystemSpeciesDatas(Biome biome, float deltaTime)
        {
            var speciesToRemove = new List<EcosystemSpeciesData>();

            foreach (var species in biome.species)
            {
                // Apply population growth/decline
                float growthChange = species.populationGrowthRate * deltaTime;
                species.population = math.max(0f, species.population + growthChange);

                // Reset growth rate for next cycle
                species.populationGrowthRate = CalculateIntrinsicGrowthRate(species, biome);

                // Check for extinction
                if (species.population < extinctionThreshold * species.maxPopulation)
                {
                    if (extinctionPrevention.ShouldPreventExtinction(species, biome))
                    {
                        species.population = extinctionThreshold * species.maxPopulation * 2f; // Small recovery
                        UnityEngine.Debug.Log($"Extinction prevented for species {species.speciesName} in biome {biome.biomeId}");
                    }
                    else
                    {
                        speciesToRemove.Add(species);
                        OnSpeciesExtinction?.Invoke(biome.biomeId, species.speciesName);
                    }
                }

                // Apply environmental stress effects
                ApplyEnvironmentalStress(species, deltaTime);
            }

            // Remove extinct species
            foreach (var extinctSpecies in speciesToRemove)
            {
                biome.species.Remove(extinctSpecies);
                UnityEngine.Debug.Log($"Species {extinctSpecies.speciesName} went extinct in biome {biome.biomeId}");
            }
        }

        private float CalculateIntrinsicGrowthRate(EcosystemSpeciesData species, Biome biome)
        {
            float baseGrowthRate = species.intrinsicGrowthRate;

            // Carrying capacity limitation
            float carryingCapacityEffect = 1f - (species.population / (biome.carryingCapacity + 1f));
            baseGrowthRate *= carryingCapacityEffect;

            // Resource availability
            float resourceAvailability = CalculateResourceAvailability(species, biome);
            baseGrowthRate *= resourceAvailability;

            // Climate suitability
            float climateSuitability = CalculateClimateSuitability(species, biome);
            baseGrowthRate *= climateSuitability;

            return baseGrowthRate;
        }

        private float CalculateResourceAvailability(EcosystemSpeciesData species, Biome biome)
        {
            float availability = 1f;

            foreach (var requirement in species.resourceRequirements)
            {
                if (biome.resources.TryGetValue(requirement.Key, out var resource))
                {
                    float resourceRatio = resource.currentAmount / (resource.maxAmount + 1f);
                    availability *= math.min(1f, resourceRatio / requirement.Value);
                }
                else
                {
                    availability *= 0.5f; // Penalty for missing resource
                }
            }

            return math.clamp(availability, 0.1f, 1f);
        }

        private float CalculateClimateSuitability(EcosystemSpeciesData species, Biome biome)
        {
            float temperatureSuitability = 1f - math.abs(biome.climateConditions.temperature - species.optimalTemperature) / 20f;
            float humiditySuitability = 1f - math.abs(biome.climateConditions.humidity - species.optimalHumidity);

            return math.clamp((temperatureSuitability + humiditySuitability) / 2f, 0.1f, 1f);
        }

        private void ApplyEnvironmentalStress(EcosystemSpeciesData species, float deltaTime)
        {
            if (species.environmentalStress > 0.5f)
            {
                // High stress reduces population growth and increases mortality
                species.populationGrowthRate *= (1f - species.environmentalStress * 0.5f);

                // Gradually reduce stress over time
                species.environmentalStress = math.max(0f, species.environmentalStress - 0.1f * deltaTime);
            }
        }

        private void UpdateMigrationPatterns(float deltaTime)
        {
            migrationManager.UpdateMigrationPatterns(ecosystemNodes.Values, activeBiomes, deltaTime);
        }

        private void UpdateSpatialConnectivity(float deltaTime)
        {
            spatialManager.UpdateConnectivity(ecosystemNodes.Values, deltaTime);
        }

        private void ProcessEcologicalSuccession(float deltaTime)
        {
            foreach (var biome in activeBiomes.Values)
            {
                successionManager.ProcessSuccession(biome, deltaTime);
            }
        }

        private void ProcessCatastrophicEvents(float deltaTime)
        {
            if (UnityEngine.Random.value < catastropheFrequency * deltaTime)
            {
                var catastrophe = catastropheManager.GenerateCatastrophe(catastropheFrequency, deltaTime);
                if (catastrophe.intensity > 0)
                {
                    ApplyCatastrophicEvent(catastrophe);
                    OnCatastrophicEvent?.Invoke(catastrophe);
                }
            }
        }

        private void ApplyCatastrophicEvent(EcologicalCatastrophe catastrophe)
        {
            foreach (var biomeId in catastrophe.affectedBiomes)
            {
                if (activeBiomes.TryGetValue(biomeId, out var biome))
                {
                    // Apply disturbance
                    var disturbance = new DisturbanceEvent
                    {
                        disturbanceType = (DisturbanceType)catastrophe.catastropheType,
                        severity = catastrophe.intensity,
                        timestamp = Time.time,
                        duration = catastrophe.recoveryTime
                    };

                    biome.disturbanceHistory.Add(disturbance);

                    // Apply immediate effects
                    ApplyDisturbanceEffects(biome, disturbance);
                }
            }

            UnityEngine.Debug.Log($"Catastrophic event: {catastrophe.catastropheType} affecting {catastrophe.affectedBiomes.Count} biomes");
        }

        private void ApplyDisturbanceEffects(Biome biome, DisturbanceEvent disturbance)
        {
            float severityMultiplier = disturbance.severity;

            // Reduce species populations
            foreach (var species in biome.species)
            {
                float populationLoss = severityMultiplier * 0.3f; // Up to 30% population loss
                species.population *= (1f - populationLoss);
                species.environmentalStress += severityMultiplier * 0.5f;
            }

            // Affect resources
            foreach (var resource in biome.resources.Values)
            {
                float resourceLoss = severityMultiplier * 0.4f; // Up to 40% resource loss
                resource.currentAmount *= (1f - resourceLoss);
            }

            // Reduce biome stability
            biome.stabilityIndex *= (1f - severityMultiplier * 0.2f);
        }

        private void UpdateBiodiversityMetrics()
        {
            foreach (var biome in activeBiomes.Values)
            {
                biome.biodiversityIndex = biodiversityTracker.CalculateBiodiversity(biome);
            }
        }

        private void UpdateEcosystemHealth()
        {
            foreach (var biome in activeBiomes.Values)
            {
                if (biomeHealthMetrics.TryGetValue(biome.biomeId, out var health))
                {
                    UpdateBiomeHealth(biome, health);
                }
            }
        }

        private void UpdateBiomeHealth(Biome biome, BiomeHealth health)
        {
            // Species diversity component
            health.speciesDiversity = biome.biodiversityIndex;

            // Resource availability component
            float totalResourceRatio = biome.resources.Values.Average(r => r.currentAmount / r.maxAmount);
            health.resourceAvailability = totalResourceRatio;

            // Climate stress component
            health.climateStress = CalculateClimateStress(biome);

            // Resilience based on stability and connectivity
            health.resilienceScore = (biome.stabilityIndex + biome.connectivityIndex) / 2f;

            // Overall health calculation
            health.overallHealth = (health.speciesDiversity + health.resourceAvailability +
                                   (1f - health.climateStress) + health.resilienceScore) / 4f;

            health.overallHealth = math.clamp(health.overallHealth, 0f, 1f);
        }

        private void UpdateGlobalMetrics()
        {
            globalMetrics.totalBiomes = activeBiomes.Count;
            globalMetrics.totalSpecies = activeBiomes.Values.Sum(b => b.species.Count);
            globalMetrics.averageBiodiversity = activeBiomes.Values.Average(b => b.biodiversityIndex);
            globalMetrics.averageStability = activeBiomes.Values.Average(b => b.stabilityIndex);
            globalMetrics.totalArea = activeBiomes.Values.Sum(b => b.area);
            globalMetrics.climateVariability = CalculateClimateVariability();
            globalMetrics.connectivityIndex = spatialManager.CalculateGlobalConnectivity(ecosystemNodes.Values);
            globalMetrics.extinctionRate = CalculateExtinctionRate();
        }

        private float CalculateClimateVariability()
        {
            if (activeBiomes.Count == 0) return 0f;

            var temperatures = activeBiomes.Values.Select(b => b.climateConditions.temperature).ToList();
            var precipitations = activeBiomes.Values.Select(b => b.climateConditions.precipitation).ToList();

            float tempVariance = CalculateVariance(temperatures);
            float precipVariance = CalculateVariance(precipitations);

            return (tempVariance + precipVariance) / 2f;
        }

        private float CalculateVariance(List<float> values)
        {
            if (values.Count <= 1) return 0f;

            float mean = values.Average();
            float sumSquaredDiffs = values.Sum(v => (v - mean) * (v - mean));

            return sumSquaredDiffs / (values.Count - 1);
        }

        private float CalculateExtinctionRate()
        {
            int recentExtinctions = eventHistory
                .Count(e => e.eventType == EventType.SpeciesExtinction &&
                           Time.time - e.timestamp < 100f);

            int totalSpeciesTime = globalMetrics.totalSpecies * 100; // Species-time units

            return totalSpeciesTime > 0 ? (float)recentExtinctions / totalSpeciesTime : 0f;
        }

        /// <summary>
        /// Generates comprehensive ecosystem analysis report
        /// </summary>
        public EcosystemAnalysisReport GenerateEcosystemReport()
        {
            return new EcosystemAnalysisReport
            {
                globalMetrics = globalMetrics,
                climateAnalysis = AnalyzeClimate(),
                biomeAnalysis = AnalyzeBiomes(),
                speciesAnalysis = AnalyzeSpecies(),
                foodWebAnalysis = AnalyzeFoodWeb(),
                connectivityAnalysis = spatialManager.AnalyzeConnectivity(globalMetrics),
                disturbanceAnalysis = AnalyzeDisturbances(),
                sustainabilityAssessment = AssessSustainability(),
                conservationPriorities = IdentifyConservationPriorities(),
                managementRecommendations = GenerateManagementRecommendations()
            };
        }

        private ClimateAnalysis AnalyzeClimate()
        {
            return new ClimateAnalysis
            {
                globalTemperature = globalClimate.globalTemperature,
                globalPrecipitation = globalClimate.globalPrecipitation,
                climateStability = globalClimate.climateStability,
                temperatureVariability = CalculateTemperatureVariability(),
                precipitationVariability = CalculatePrecipitationVariability(),
                climateChangeRate = climateEngine.GetClimateChangeRate(),
                seasonalVariation = globalClimate.seasonalVariation
            };
        }

        private float CalculateTemperatureVariability()
        {
            var temperatures = activeBiomes.Values.Select(b => b.climateConditions.temperature).ToList();
            return CalculateVariance(temperatures);
        }

        private float CalculatePrecipitationVariability()
        {
            var precipitations = activeBiomes.Values.Select(b => b.climateConditions.precipitation).ToList();
            return CalculateVariance(precipitations);
        }

        private BiomeAnalysis AnalyzeBiomes()
        {
            var biomeTypeDistribution = activeBiomes.Values
                .GroupBy(b => System.Enum.Parse<BiomeType>(b.biomeType.ToString()))
                .ToDictionary(g => g.Key, g => g.Count());

            return new BiomeAnalysis
            {
                totalBiomes = activeBiomes.Count,
                biomeTypeDistribution = biomeTypeDistribution,
                averageArea = activeBiomes.Values.Average(b => b.area),
                averageStability = activeBiomes.Values.Average(b => b.stabilityIndex),
                averageBiodiversity = activeBiomes.Values.Average(b => b.biodiversityIndex),
                successionDistribution = AnalyzeSuccessionStages(),
                healthMetrics = biomeHealthMetrics.Values.ToList()
            };
        }

        private Dictionary<SuccessionStage, int> AnalyzeSuccessionStages()
        {
            return activeBiomes.Values
                .GroupBy(b => b.successionStage)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private SpeciesAnalysis AnalyzeSpecies()
        {
            var allSpecies = activeBiomes.Values.SelectMany(b => b.species).ToList();

            return new SpeciesAnalysis
            {
                totalSpecies = allSpecies.Count,
                averagePopulation = allSpecies.Average(s => s.population),
                endangeredSpecies = allSpecies.Count(s => s.population < s.maxPopulation * 0.1f),
                extinctionRisk = CalculateExtinctionRisk(allSpecies),
                trophicLevelDistribution = AnalyzeTrophicLevels(allSpecies),
                endemicSpecies = CountEndemicSpecies(allSpecies),
                migratorySpecies = CountMigratorySpecies(allSpecies)
            };
        }

        private float CalculateExtinctionRisk(List<EcosystemSpeciesData> species)
        {
            int highRiskSpecies = species.Count(s => s.population < s.maxPopulation * 0.2f);
            return species.Count > 0 ? (float)highRiskSpecies / species.Count : 0f;
        }

        private Dictionary<TrophicLevel, int> AnalyzeTrophicLevels(List<EcosystemSpeciesData> species)
        {
            return species
                .GroupBy(s => s.trophicLevel)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private int CountEndemicSpecies(List<EcosystemSpeciesData> allSpecies)
        {
            // Species found in only one biome
            var speciesBiomeCount = new Dictionary<uint, int>();

            foreach (var biome in activeBiomes.Values)
            {
                foreach (var species in biome.species)
                {
                    speciesBiomeCount[species.speciesId] = speciesBiomeCount.GetValueOrDefault(species.speciesId, 0) + 1;
                }
            }

            return speciesBiomeCount.Values.Count(count => count == 1);
        }

        private int CountMigratorySpecies(List<EcosystemSpeciesData> species)
        {
            return species.Count(s => s.migrationCapability > 0.5f);
        }

        private FoodWebAnalysis AnalyzeFoodWeb()
        {
            return new FoodWebAnalysis
            {
                totalRelationships = foodWeb.Count,
                predatorPreyPairs = foodWeb.Count(r => r.relationshipType == RelationshipType.Predation),
                mutualisticPairs = foodWeb.Count(r => r.relationshipType == RelationshipType.Mutualism),
                competitiveRelationships = foodWeb.Count(r => r.relationshipType == RelationshipType.Competition),
                webConnectance = CalculateWebConnectance(),
                trophicLevels = CalculateTrophicLevelCount(),
                keystoneSpecies = IdentifyKeystoneSpecies()
            };
        }

        private float CalculateWebConnectance()
        {
            int totalSpecies = activeBiomes.Values.Sum(b => b.species.Count);
            int possibleConnections = totalSpecies * (totalSpecies - 1) / 2;

            return possibleConnections > 0 ? (float)foodWeb.Count / possibleConnections : 0f;
        }

        private int CalculateTrophicLevelCount()
        {
            return activeBiomes.Values
                .SelectMany(b => b.species)
                .Select(s => s.trophicLevel)
                .Distinct()
                .Count();
        }

        private List<uint> IdentifyKeystoneSpecies()
        {
            // Simplified keystone species identification based on relationship centrality
            var speciesConnections = new Dictionary<uint, int>();

            foreach (var relationship in foodWeb)
            {
                speciesConnections[(uint)relationship.speciesA] = speciesConnections.GetValueOrDefault((uint)relationship.speciesA, 0) + 1;
                speciesConnections[(uint)relationship.speciesB] = speciesConnections.GetValueOrDefault((uint)relationship.speciesB, 0) + 1;
            }

            float averageConnections = (float)speciesConnections.Values.Average();

            return speciesConnections
                .Where(kvp => kvp.Value > averageConnections * 1.5f)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        private DisturbanceAnalysis AnalyzeDisturbances()
        {
            var allDisturbances = activeBiomes.Values
                .SelectMany(b => b.disturbanceHistory)
                .Where(d => Time.time - d.timestamp < 500f) // Recent disturbances
                .ToList();

            return new DisturbanceAnalysis
            {
                totalDisturbances = allDisturbances.Count,
                averageSeverity = allDisturbances.Any() ? allDisturbances.Average(d => d.severity) : 0f,
                disturbanceFrequency = allDisturbances.Count / 500f, // Per time unit
                recoveryRate = CalculateRecoveryRate(allDisturbances),
                mostVulnerableBiomes = IdentifyVulnerableBiomes().Select(id => (int)id).ToArray()
            };
        }

        private float CalculateRecoveryRate(List<DisturbanceEvent> disturbances)
        {
            int recoveredDisturbances = disturbances.Count(d => Time.time - d.timestamp > d.recoveryTime);
            return disturbances.Count > 0 ? (float)recoveredDisturbances / disturbances.Count : 1f;
        }

        private List<uint> IdentifyVulnerableBiomes()
        {
            return activeBiomes.Values
                .Where(b => b.stabilityIndex < 0.4f || b.disturbanceHistory.Count(d => Time.time - d.timestamp < 100f) > 2)
                .Select(b => b.biomeId)
                .ToList();
        }

        private SustainabilityAssessment AssessSustainability()
        {
            return new SustainabilityAssessment
            {
                overallSustainability = CalculateOverallSustainability(),
                resourceSustainability = AssessResourceSustainability(),
                biodiversitySustainability = AssessBiodiversitySustainability(),
                climateSustainability = AssessClimateSustainability(),
                resilienceScore = CalculateResilienceScore(),
                sustainabilityTrends = new float[] { 0.5f, 0.6f, 0.7f } // Placeholder trends
            };
        }

        private float CalculateOverallSustainability()
        {
            float resourceScore = AssessResourceSustainability();
            float biodiversityScore = AssessBiodiversitySustainability();
            float climateScore = AssessClimateSustainability();
            float resilienceScore = CalculateResilienceScore();

            return (resourceScore + biodiversityScore + climateScore + resilienceScore) / 4f;
        }

        private float AssessResourceSustainability()
        {
            float totalSustainability = 0f;
            int resourceCount = 0;

            foreach (var biome in activeBiomes.Values)
            {
                foreach (var resource in biome.resources.Values)
                {
                    float utilizationRate = 1f - (resource.currentAmount / resource.maxAmount);
                    float sustainabilityScore = utilizationRate < 0.8f ? 1f : (1f - utilizationRate) / 0.2f;
                    totalSustainability += sustainabilityScore;
                    resourceCount++;
                }
            }

            return resourceCount > 0 ? totalSustainability / resourceCount : 1f;
        }

        private float AssessBiodiversitySustainability()
        {
            float averageBiodiversity = activeBiomes.Values.Average(b => b.biodiversityIndex);
            float extinctionRate = CalculateExtinctionRate();

            return math.clamp(averageBiodiversity - extinctionRate * 10f, 0f, 1f);
        }

        private float AssessClimateSustainability()
        {
            float climateChangeRate = climateEngine.GetClimateChangeRate();
            float climateStability = globalClimate.climateStability;

            return math.clamp(climateStability - climateChangeRate * 5f, 0f, 1f);
        }

        private float CalculateResilienceScore()
        {
            float averageStability = activeBiomes.Values.Average(b => b.stabilityIndex);
            float averageConnectivity = activeBiomes.Values.Average(b => b.connectivityIndex);

            return (averageStability + averageConnectivity) / 2f;
        }

        private List<string> IdentifySustainabilityTrends()
        {
            var trends = new List<string>();

            if (CalculateExtinctionRate() > 0.01f)
                trends.Add("Increasing extinction rate threatens biodiversity");

            if (globalClimate.climateStability < 0.6f)
                trends.Add("Climate instability affecting ecosystem stability");

            if (AssessResourceSustainability() < 0.7f)
                trends.Add("Resource depletion patterns emerging");

            if (activeBiomes.Values.Average(b => b.stabilityIndex) > 0.7f)
                trends.Add("High ecosystem stability promoting resilience");

            return trends;
        }

        private List<ConservationPriority> IdentifyConservationPriorities()
        {
            var priorities = new List<ConservationPriority>();

            // High biodiversity biomes
            var highBiodiversityBiomes = activeBiomes.Values
                .Where(b => b.biodiversityIndex > 0.8f)
                .Select(b => new ConservationPriority
                {
                    biomeId = b.biomeId,
                    priority = ConservationLevel.High,
                    reason = "High biodiversity hotspot",
                    urgency = b.stabilityIndex < 0.5f ? UrgencyLevel.Critical : UrgencyLevel.High
                });

            priorities.AddRange(highBiodiversityBiomes);

            // Vulnerable species habitats
            var vulnerableHabitats = activeBiomes.Values
                .Where(b => b.species.Any(s => s.population < s.maxPopulation * 0.2f))
                .Select(b => new ConservationPriority
                {
                    biomeId = b.biomeId,
                    priority = ConservationLevel.High,
                    reason = "Critical habitat for endangered species",
                    urgency = UrgencyLevel.Critical
                });

            priorities.AddRange(vulnerableHabitats);

            return priorities.Take(10).ToList(); // Top 10 priorities
        }

        private List<string> GenerateManagementRecommendations()
        {
            var recommendations = new List<string>();

            if (globalMetrics.averageStability < 0.6f)
                recommendations.Add("Implement ecosystem stabilization measures in vulnerable biomes");

            if (CalculateExtinctionRate() > 0.005f)
                recommendations.Add("Establish species conservation programs for endangered populations");

            if (globalClimate.climateStability < 0.7f)
                recommendations.Add("Develop climate adaptation strategies for ecosystem resilience");

            if (globalMetrics.connectivityIndex < 0.5f)
                recommendations.Add("Create wildlife corridors to improve habitat connectivity");

            if (AssessResourceSustainability() < 0.8f)
                recommendations.Add("Implement sustainable resource management practices");

            recommendations.Add("Monitor keystone species populations for early warning indicators");
            recommendations.Add("Maintain genetic diversity through habitat protection");

            return recommendations;
        }

        // ID generation
        private uint GenerateBiomeId() => (uint)UnityEngine.Random.Range(1, int.MaxValue);

        // Apply climate change to biome - method was cut off earlier
        private void ApplyClimateChangeToBiome(Biome biome, ClimateChange change)
        {
            biome.climateConditions.temperature += change.temperatureChange;
            biome.climateConditions.precipitation += change.precipitationChange;
            biome.climateConditions.humidity += change.humidityChange;

            // Clamp values to reasonable ranges
            biome.climateConditions.temperature = math.clamp(biome.climateConditions.temperature, -50f, 60f);
            biome.climateConditions.precipitation = math.clamp(biome.climateConditions.precipitation, 0f, 5000f);
            biome.climateConditions.humidity = math.clamp(biome.climateConditions.humidity, 0f, 100f);
        }
    }

    // Unique enums not defined in EcosystemTypes.cs
    public enum EcologicalEventType
    {
        SpeciesIntroduction,
        SpeciesExtinction,
        BiomeTransition,
        ClimateShift,
        Disturbance,
        Migration,
        PopulationBoom,
        ResourceDepletion
    }


    // Additional supporting classes and data structures would be implemented here...
    // The full implementation would include all the supporting classes referenced in the main class

    // EcosystemMetrics moved to EcosystemTypes.cs

    [System.Serializable]
    public class EcosystemAnalysisReport
    {
        public EcosystemMetrics globalMetrics;
        public ClimateAnalysis climateAnalysis;
        public BiomeAnalysis biomeAnalysis;
        public SpeciesAnalysis speciesAnalysis;
        public FoodWebAnalysis foodWebAnalysis;
        public ConnectivityAnalysis connectivityAnalysis;
        public DisturbanceAnalysis disturbanceAnalysis;
        public SustainabilityAssessment sustainabilityAssessment;
        public List<ConservationPriority> conservationPriorities;
        public List<string> managementRecommendations;
    }

    // Analysis result classes
    [System.Serializable]
    public class ClimateAnalysis
    {
        public float globalTemperature;
        public float globalPrecipitation;
        public float climateStability;
        public float temperatureVariability;
        public float precipitationVariability;
        public float climateChangeRate;
        public float seasonalVariation;
    }

    [System.Serializable]
    public class BiomeAnalysis
    {
        public int totalBiomes;
        public Dictionary<BiomeType, int> biomeTypeDistribution;
        public float averageArea;
        public float averageStability;
        public float averageBiodiversity;
        public Dictionary<SuccessionStage, int> successionDistribution;
        public List<BiomeHealth> healthMetrics;
    }

    [System.Serializable]
    public class SpeciesAnalysis
    {
        public int totalSpecies;
        public float averagePopulation;
        public int endangeredSpecies;
        public float extinctionRisk;
        public Dictionary<TrophicLevel, int> trophicLevelDistribution;
        public int endemicSpecies;
        public int migratorySpecies;
    }

    [System.Serializable]
    public class FoodWebAnalysis
    {
        public int totalRelationships;
        public int predatorPreyPairs;
        public int mutualisticPairs;
        public int competitiveRelationships;
        public float webConnectance;
        public int trophicLevels;
        public List<uint> keystoneSpecies;
    }

    // Supporting system placeholder classes
    public class ResourceNetwork
    {
        public ResourceNetwork(int resourceTypes) { }
        public Dictionary<string, Resource> InitializeBiomeResources(BiomeType biomeType) { return new Dictionary<string, Resource>(); }
        public void UpdateBiomeResources(Biome biome, float deltaTime) { }
        public void ProcessResourceFlow(ResourceFlow flow, float deltaTime) { }
    }

    public class BiomeTransitionMatrix
    {
        public void SetTransition(BiomeType from, BiomeType to, float probability, ClimateCondition condition) { }
        public BiomeType? EvaluateTransition(BiomeType current, ClimateCondition conditions) { return null; }
    }

    // Duplicate types removed - now defined in EcosystemTypes.cs

    // SeasonalModifier moved to EcosystemTypes.cs


    // These types are now properly defined in EcosystemTypes.cs
    public class SeasonalCycleManager
    {
        public SeasonalCycleManager(bool enableSeasonalCycles) { }
        public void UpdateSeason(float deltaTime) { }
        public Season GetCurrentSeason() => Season.Spring;
    }
    public class ClimateEvolutionEngine
    {
        public ClimateEvolutionEngine(float temperatureVariance, float precipitationVariance) { }
        public float GetClimateChangeRate() => 0.1f;
        public void UpdateClimate(ClimateSystem climate, float deltaTime) { }
    }
    public class SuccessionManager
    {
        public void ProcessSuccession(Biome biome, float deltaTime) { }
        public SuccessionStage AdvanceSuccession(SuccessionStage currentStage, Biome biome)
        {
            // Simple advancement logic
            switch (currentStage)
            {
                case SuccessionStage.Pioneer: return SuccessionStage.Early;
                case SuccessionStage.Early: return SuccessionStage.Mid;
                case SuccessionStage.Mid: return SuccessionStage.Late;
                case SuccessionStage.Late: return SuccessionStage.Climax;
                default: return currentStage;
            }
        }
    }
    public class MigrationManager
    {
        public MigrationManager(float migrationThreshold) { }
        public void UpdateMigrationPatterns(IEnumerable<EcosystemNode> nodes, Dictionary<uint, Biome> biomes, float deltaTime)
        {
            // Stub implementation
        }
    }
    public class ExtinctionPreventionSystem
    {
        public ExtinctionPreventionSystem(float extinctionThreshold) { }
        public bool ShouldPreventExtinction(EcosystemSpeciesData species, Biome biome)
        {
            return species.population < 10f; // Simple threshold check
        }
    }
    public class BiodiversityTracker
    {
        public float CalculateBiodiversity(Biome biome) => 0.5f;
    }
    public class TemporalEvolutionEngine
    {
        public TemporalEvolutionEngine(float timeScale, bool enableGeologicalTime) { }
    }
    public class SpatialConnectivityManager
    {
        public void UpdateConnectivity(IEnumerable<EcosystemNode> nodes, float deltaTime) { }
        public float CalculateGlobalConnectivity(IEnumerable<EcosystemNode> nodes) => 0.5f;
        public ConnectivityAnalysis AnalyzeConnectivity(EcosystemMetrics metrics) => new ConnectivityAnalysis();
    }
    public class CatastropheManager
    {
        public CatastropheManager(float catastropheFrequency) { }
        public EcologicalCatastrophe GenerateCatastrophe(float frequency, float deltaTime)
        {
            return new EcologicalCatastrophe
            {
                catastropheType = CatastropheType.Drought,
                affectedBiomes = new List<uint>(),
                recoveryTime = 100f,
                intensity = 0.5f,
                epicenter = new float3(0, 0, 0)
            };
        }
    }
    public class DisturbanceAnalysis
    {
        public int totalDisturbances;
        public float averageSeverity;
        public float disturbanceFrequency;
        public float recoveryRate;
        public int[] mostVulnerableBiomes;
    }
    public class SustainabilityAssessment
    {
        public float overallSustainability;
        public float resourceSustainability;
        public float biodiversitySustainability;
        public float climateSustainability;
        public float resilienceScore;
        public float[] sustainabilityTrends;
    }
    public class ConservationPriority
    {
        public uint biomeId;
        public ConservationLevel priority;
        public string reason;
        public UrgencyLevel urgency;
    }
    public class BiomeData
    {
        public float temperature;
        public float precipitation;
        public float humidity;
    }

    public class ClimateChange
    {
        public float temperatureChange;
        public float precipitationChange;
        public float humidityChange;
    }
    public class ConnectivityAnalysis { }
}