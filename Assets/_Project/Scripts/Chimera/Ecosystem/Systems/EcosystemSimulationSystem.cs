using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Ecosystem.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Genetics.Core;
using System.Collections.Generic;
using EcosystemEventType = Laboratory.Chimera.Ecosystem.Core.EcosystemEventType;

namespace Laboratory.Chimera.Ecosystem.Systems
{
    /// <summary>
    /// Main ecosystem simulation system managing population dynamics, natural selection, and environmental changes
    /// Creates a living, breathing world where creatures adapt and evolve over time
    /// </summary>

    public partial struct EcosystemSimulationSystem : ISystem
    {
        private EntityQuery _ecosystemQuery;
        private EntityQuery _populationQuery;
        private EntityQuery _resourceQuery;
        private EntityQuery _eventQuery;

        private ComponentLookup<EcosystemState> _ecosystemLookup;
        private ComponentLookup<CreaturePopulation> _populationLookup;
        private ComponentLookup<EcosystemResources> _resourceLookup;
        private ComponentLookup<VisualGeneticData> _geneticsLookup;

        private double _lastEcosystemUpdate;
        private double _lastPopulationUpdate;
        private double _lastEnvironmentalUpdate;

        // Simulation parameters
        private const float ECOSYSTEM_UPDATE_INTERVAL = 1.0f;    // Update every second
        private const float POPULATION_UPDATE_INTERVAL = 5.0f;   // Population changes every 5 seconds
        private const float ENVIRONMENTAL_UPDATE_INTERVAL = 10.0f; // Environmental events every 10 seconds


        public void OnCreate(ref SystemState state)
        {
            _ecosystemQuery = SystemAPI.QueryBuilder()
                .WithAll<EcosystemState>()
                .Build();

            _populationQuery = SystemAPI.QueryBuilder()
                .WithAll<CreaturePopulation>()
                .Build();

            _resourceQuery = SystemAPI.QueryBuilder()
                .WithAll<EcosystemResources>()
                .Build();

            _eventQuery = SystemAPI.QueryBuilder()
                .WithAll<EnvironmentalEvent>()
                .Build();

            _ecosystemLookup = SystemAPI.GetComponentLookup<EcosystemState>(false);
            _populationLookup = SystemAPI.GetComponentLookup<CreaturePopulation>(false);
            _resourceLookup = SystemAPI.GetComponentLookup<EcosystemResources>(false);
            _geneticsLookup = SystemAPI.GetComponentLookup<VisualGeneticData>(true);

            _lastEcosystemUpdate = SystemAPI.Time.ElapsedTime;
            _lastPopulationUpdate = SystemAPI.Time.ElapsedTime;
            _lastEnvironmentalUpdate = SystemAPI.Time.ElapsedTime;

            state.RequireForUpdate(_ecosystemQuery);
        }


        public void OnUpdate(ref SystemState state)
        {
            double currentTime = SystemAPI.Time.ElapsedTime;
            float deltaTime = (float)SystemAPI.Time.DeltaTime;

            _ecosystemLookup.Update(ref state);
            _populationLookup.Update(ref state);
            _resourceLookup.Update(ref state);
            _geneticsLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Update ecosystems at different intervals for performance
            if (currentTime - _lastEcosystemUpdate >= ECOSYSTEM_UPDATE_INTERVAL)
            {
                UpdateEcosystemStates(ref state, deltaTime);
                _lastEcosystemUpdate = currentTime;
            }

            if (currentTime - _lastPopulationUpdate >= POPULATION_UPDATE_INTERVAL)
            {
                UpdatePopulationDynamics(ref state, deltaTime);
                _lastPopulationUpdate = currentTime;
            }

            if (currentTime - _lastEnvironmentalUpdate >= ENVIRONMENTAL_UPDATE_INTERVAL)
            {
                ProcessEnvironmentalEvents(ref state, deltaTime, ecb);
                _lastEnvironmentalUpdate = currentTime;
            }

            // Always process resource updates for responsive gameplay
            UpdateResourceSystems(ref state, deltaTime);
        }

        /// <summary>
        /// Update core ecosystem environmental conditions
        /// </summary>

        private void UpdateEcosystemStates(ref SystemState state, float deltaTime)
        {
            var ecosystemJob = new EcosystemUpdateJob
            {
                DeltaTime = deltaTime,
                CurrentDay = (int)(SystemAPI.Time.ElapsedTime / 86400.0), // Convert to days
                CurrentTime = (uint)SystemAPI.Time.ElapsedTime
            };

            state.Dependency = ecosystemJob.ScheduleParallel(_ecosystemQuery, state.Dependency);
        }

        /// <summary>
        /// Update creature population dynamics and natural selection
        /// </summary>
        private void UpdatePopulationDynamics(ref SystemState state, float deltaTime)
        {
            // Get all ecosystems for population context
            var ecosystems = new NativeList<EcosystemState>(10, Allocator.TempJob);
            var ecosystemEntities = new NativeList<Entity>(10, Allocator.TempJob);

            foreach (var (ecosystem, entity) in SystemAPI.Query<RefRO<EcosystemState>>().WithEntityAccess())
            {
                ecosystems.Add(ecosystem.ValueRO);
                ecosystemEntities.Add(entity);
            }

            var populationJob = new PopulationDynamicsJob
            {
                DeltaTime = deltaTime,
                Ecosystems = ecosystems.AsArray(),
                EcosystemEntities = ecosystemEntities.AsArray(),
                PopulationLookup = _populationLookup,
                GeneticsLookup = _geneticsLookup
            };

            state.Dependency = populationJob.ScheduleParallel(_populationQuery, state.Dependency);
            state.Dependency.Complete();

            ecosystems.Dispose();
            ecosystemEntities.Dispose();

            // Update ecosystem totals after population changes
            UpdateEcosystemPopulationTotals(ref state);
        }

        /// <summary>
        /// Update resource availability and consumption
        /// </summary>

        private void UpdateResourceSystems(ref SystemState state, float deltaTime)
        {
            var resourceJob = new ResourceUpdateJob
            {
                DeltaTime = deltaTime,
                EcosystemLookup = _ecosystemLookup
            };

            state.Dependency = resourceJob.ScheduleParallel(_resourceQuery, state.Dependency);
        }

        /// <summary>
        /// Process environmental events and their effects
        /// </summary>
        private void ProcessEnvironmentalEvents(ref SystemState state, float deltaTime, EntityCommandBuffer ecb)
        {
            // Process existing events
            foreach (var (envEvent, entity) in SystemAPI.Query<RefRW<EnvironmentalEvent>>().WithEntityAccess())
            {
                var eventData = envEvent.ValueRW;

                // Find affected ecosystems
                foreach (var (ecosystem, ecoEntity) in SystemAPI.Query<RefRW<EcosystemState>>().WithEntityAccess())
                {
                    var ecoData = ecosystem.ValueRW;

                    // Check if ecosystem is affected (simplified - all events affect all ecosystems for now)
                    if (_resourceLookup.TryGetComponent(ecoEntity, out EcosystemResources resources))
                    {
                        var resourceData = resources;
                        eventData.ApplyEffects(ref ecoData, ref resourceData, deltaTime);

                        ecosystem.ValueRW = ecoData;
                        _resourceLookup[ecoEntity] = resourceData;
                    }
                }

                envEvent.ValueRW = eventData;

                // Remove completed events
                if (eventData.TimeRemaining <= 0f && eventData.IsRecovering)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            // Randomly generate new environmental events
            GenerateRandomEvents(ref state, ecb);
        }

        /// <summary>
        /// Update ecosystem population totals after individual population changes
        /// </summary>
        private void UpdateEcosystemPopulationTotals(ref SystemState state)
        {
            // Create a map of ecosystem -> total population
            var ecosystemPopulations = new NativeHashMap<Entity, int>(10, Allocator.Temp);

            // Sum populations by ecosystem
            foreach (var (population, popEntity) in SystemAPI.Query<RefRO<CreaturePopulation>>().WithEntityAccess())
            {
                // For now, assume all populations belong to first ecosystem
                // In a real implementation, you'd have ecosystem references
                var ecosystemEntities = _ecosystemQuery.ToEntityArray(Allocator.Temp);
                if (ecosystemEntities.Length > 0)
                {
                    Entity ecoEntity = ecosystemEntities[0];
                    ecosystemPopulations.TryGetValue(ecoEntity, out int currentTotal);
                    ecosystemPopulations[ecoEntity] = currentTotal + population.ValueRO.CurrentPopulation;
                }
                ecosystemEntities.Dispose();
            }

            // Update ecosystem totals
            foreach (var kvp in ecosystemPopulations)
            {
                if (_ecosystemLookup.TryGetComponent(kvp.Key, out EcosystemState ecosystem))
                {
                    ecosystem.TotalPopulation = kvp.Value;
                    ecosystem.PopulationGrowthRate = CalculateGrowthRate(ecosystem);
                    ecosystem.DiversityIndex = CalculateDiversityIndex(kvp.Key, ref state);
                    _ecosystemLookup[kvp.Key] = ecosystem;
                }
            }

            ecosystemPopulations.Dispose();
        }

        /// <summary>
        /// Calculate population growth rate for ecosystem
        /// </summary>

        private float CalculateGrowthRate(EcosystemState ecosystem)
        {
            if (ecosystem.TotalPopulation == 0) return 0f;

            float carryingCapacityRatio = (float)ecosystem.TotalPopulation / ecosystem.CarryingCapacity;
            float resourceLimitation = ecosystem.FoodAvailability * ecosystem.WaterAvailability;
            float environmentalStress = 1.0f - ecosystem.EnvironmentalPressure;

            float baseGrowthRate = 0.05f; // 5% base daily growth
            float limitedGrowthRate = baseGrowthRate * resourceLimitation * environmentalStress;

            // Logistic growth model
            return limitedGrowthRate * (1.0f - carryingCapacityRatio);
        }

        /// <summary>
        /// Calculate Shannon diversity index for ecosystem
        /// </summary>
        private float CalculateDiversityIndex(Entity ecosystemEntity, ref SystemState state)
        {
            // Simplified diversity calculation
            var speciesCounts = new NativeList<int>(20, Allocator.Temp);

            foreach (var population in SystemAPI.Query<RefRO<CreaturePopulation>>())
            {
                speciesCounts.Add(population.ValueRO.CurrentPopulation);
            }

            float diversity = 0f;
            int totalPopulation = 0;

            for (int i = 0; i < speciesCounts.Length; i++)
            {
                totalPopulation += speciesCounts[i];
            }

            if (totalPopulation > 0)
            {
                for (int i = 0; i < speciesCounts.Length; i++)
                {
                    float proportion = (float)speciesCounts[i] / totalPopulation;
                    if (proportion > 0f)
                    {
                        diversity -= proportion * math.log(proportion);
                    }
                }
            }

            speciesCounts.Dispose();
            return diversity;
        }

        /// <summary>
        /// Generate random environmental events
        /// </summary>
        private void GenerateRandomEvents(ref SystemState state, EntityCommandBuffer ecb)
        {
            Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)(SystemAPI.Time.ElapsedTime * 1000));

            // 1% chance per update of generating an event
            if (random.NextFloat() < 0.01f)
            {
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType eventType = (Laboratory.Chimera.Ecosystem.Core.EcosystemEventType)random.NextInt(0, 10);
                float intensity = random.NextFloat(0.2f, 0.8f);
                float duration = random.NextFloat(1f, 10f); // 1-10 days

                var newEvent = new EnvironmentalEvent
                {
                    Type = eventType,
                    Intensity = intensity,
                    Duration = duration,
                    TimeRemaining = duration,
                    Location = new Vector3(random.NextFloat(-100f, 100f), 0f, random.NextFloat(-100f, 100f)),
                    Radius = random.NextFloat(10f, 50f),
                    TemperatureChange = GetEventTemperatureChange(eventType, intensity),
                    HumidityChange = GetEventHumidityChange(eventType, intensity),
                    ResourceImpact = GetEventResourceImpact(eventType, intensity),
                    PopulationImpact = GetEventPopulationImpact(eventType, intensity),
                    RecoveryRate = 0.1f,
                    IsRecovering = false
                };

                Entity eventEntity = ecb.CreateEntity();
                ecb.AddComponent(eventEntity, newEvent);

                #if UNITY_EDITOR
                int intensityPercent = (int)(intensity * 100);
                UnityEngine.Debug.Log($"🌍 Environmental Event: {eventType} (Intensity: {intensityPercent}%, Duration: {duration} days)");
                #endif
            }
        }


        private static float GetEventTemperatureChange(Laboratory.Chimera.Ecosystem.Core.EcosystemEventType eventType, float intensity)
        {
            return eventType switch
            {
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Wildfire => intensity * 10f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.VolcanicEruption => intensity * 15f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Drought => intensity * 5f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Flood => -intensity * 3f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.ClimateShift => intensity * 8f,
                _ => 0f
            };
        }


        private static float GetEventHumidityChange(Laboratory.Chimera.Ecosystem.Core.EcosystemEventType eventType, float intensity)
        {
            return eventType switch
            {
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Drought => -intensity * 0.5f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Flood => intensity * 0.3f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Wildfire => -intensity * 0.2f,
                _ => 0f
            };
        }


        private static float GetEventResourceImpact(Laboratory.Chimera.Ecosystem.Core.EcosystemEventType eventType, float intensity)
        {
            return eventType switch
            {
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Drought => -intensity * 0.6f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Flood => -intensity * 0.4f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Wildfire => -intensity * 0.8f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Disease => -intensity * 0.3f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.ResourceDepletion => -intensity * 0.9f,
                _ => -intensity * 0.2f
            };
        }


        private static float GetEventPopulationImpact(Laboratory.Chimera.Ecosystem.Core.EcosystemEventType eventType, float intensity)
        {
            return eventType switch
            {
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Disease => -intensity * 0.7f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Wildfire => -intensity * 0.5f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Meteor => -intensity * 0.9f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.VolcanicEruption => -intensity * 0.6f,
                Laboratory.Chimera.Ecosystem.Core.EcosystemEventType.Flood => -intensity * 0.3f,
                _ => -intensity * 0.1f
            };
        }
    }

    /// <summary>
    /// Job for updating ecosystem environmental conditions
    /// </summary>

    public partial struct EcosystemUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public int CurrentDay;
        public uint CurrentTime;


        public void Execute(ref EcosystemState ecosystem)
        {
            // Day counter is automatically calculated from read-only property

            // Apply seasonal changes
            ecosystem.UpdateSeasonalConditions(DeltaTime);

            // Update environmental pressure based on population density
            float populationPressure = (float)ecosystem.TotalPopulation / ecosystem.CarryingCapacity;
            ecosystem.EnvironmentalPressure = math.lerp(ecosystem.EnvironmentalPressure, populationPressure, DeltaTime * 0.1f);

            // Update biome stability
            float optimalTemp = ecosystem.GetOptimalTemperature();
            float tempDeviation = math.abs(ecosystem.Temperature - optimalTemp) / 50f;
            ecosystem.BiomeStability = math.max(0.1f, 1.0f - tempDeviation);

            // Natural recovery from environmental pressure
            ecosystem.EnvironmentalPressure = math.max(0f, ecosystem.EnvironmentalPressure - DeltaTime * 0.05f);

            ecosystem.LastUpdate = CurrentTime;
        }
    }

    /// <summary>
    /// Job for updating population dynamics and natural selection
    /// </summary>

    public partial struct PopulationDynamicsJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public NativeArray<EcosystemState> Ecosystems;
        [ReadOnly] public NativeArray<Entity> EcosystemEntities;
        [ReadOnly] public ComponentLookup<CreaturePopulation> PopulationLookup;
        [ReadOnly] public ComponentLookup<VisualGeneticData> GeneticsLookup;


        public void Execute(ref CreaturePopulation population)
        {
            if (Ecosystems.Length == 0) return;

            // Use first ecosystem for simplification
            var ecosystem = Ecosystems[0];

            // Calculate environmental fitness
            float environmentalFitness = CalculateEnvironmentalFitness(in population, in ecosystem);
            population.EnvironmentalFitness = environmentalFitness;

            // Apply natural selection
            float selectionPressure = ecosystem.EnvironmentalPressure + (1.0f - ecosystem.BiomeStability);
            population.ApplySelection(selectionPressure, ecosystem);

            // Update age structure
            UpdateAgeStructure(ref population, DeltaTime);

            // Update genetic composition over time
            UpdateGeneticComposition(ref population, in ecosystem, DeltaTime);

            // Calculate survival and reproduction rates
            population.SurvivalRate = math.clamp(environmentalFitness * ecosystem.FoodAvailability, 0.1f, 0.95f);
            population.ReproductionRate = math.clamp(population.SurvivalRate * ecosystem.WaterAvailability * 0.8f, 0.05f, 0.6f);
        }


        private static float CalculateEnvironmentalFitness(in CreaturePopulation population, in EcosystemState ecosystem)
        {
            // Temperature adaptation
            float optimalTemp = ecosystem.GetOptimalTemperature();
            float tempFitness = 1.0f - math.abs(ecosystem.Temperature - optimalTemp) / 100f;

            // Resource availability adaptation
            float resourceFitness = (ecosystem.FoodAvailability + ecosystem.WaterAvailability) / 2f;

            // Genetic trait adaptation (simplified)
            float traitFitness = (population.AverageGenetics.Adaptability + population.AverageGenetics.Vitality) / 200f;

            return math.clamp((tempFitness + resourceFitness + traitFitness) / 3f, 0.1f, 1.0f);
        }


        private static void UpdateAgeStructure(ref CreaturePopulation population, float deltaTime)
        {
            // Simplified aging - move individuals through life stages
            float agingRate = deltaTime / 365f; // Assume 1 year life cycle

            // Juveniles become adults
            int newAdults = (int)(population.Juveniles * agingRate * 0.8f); // 80% survival to adulthood
            population.ReproductiveAdults += newAdults;
            population.Juveniles = math.max(0, population.Juveniles - newAdults);

            // Adults become elderly
            int newElderly = (int)(population.ReproductiveAdults * agingRate * 0.1f); // 10% aging rate
            population.Elderly += newElderly;
            population.ReproductiveAdults = math.max(0, population.ReproductiveAdults - newElderly);

            // Elderly mortality
            int elderlyDeaths = (int)(population.Elderly * agingRate * 0.5f); // 50% elderly mortality
            population.Elderly = math.max(0, population.Elderly - elderlyDeaths);
            population.CurrentPopulation = population.Juveniles + population.ReproductiveAdults + population.Elderly;

            // Update healthy individuals
            population.HealthyIndividuals = (int)(population.CurrentPopulation * population.SurvivalRate);
        }


        private static void UpdateGeneticComposition(ref CreaturePopulation population, in EcosystemState ecosystem, float deltaTime)
        {
            // Environmental pressure drives genetic adaptation
            float adaptationRate = ecosystem.EnvironmentalPressure * deltaTime * 0.01f;

            // Increase adaptability under pressure
            if (ecosystem.EnvironmentalPressure > 0.6f)
            {
                population.AverageGenetics.Adaptability = (byte)math.min(100, population.AverageGenetics.Adaptability + adaptationRate * 100);
            }

            // Increase vitality under resource stress
            if (ecosystem.FoodAvailability < 0.4f)
            {
                population.AverageGenetics.Vitality = (byte)math.min(100, population.AverageGenetics.Vitality + adaptationRate * 50);
            }

            // Update mutation rate
            population.MutationRate = math.clamp(population.MutationRate + ecosystem.EnvironmentalPressure * 0.001f, 0.001f, 0.1f);

            // Update genetic diversity
            if (population.CurrentPopulation < 20)
            {
                // Small population - genetic bottleneck
                population.GeneticDiversity = math.max(0.1f, population.GeneticDiversity - deltaTime * 0.02f);
                population.InbreedingCoefficient = math.min(0.8f, population.InbreedingCoefficient + deltaTime * 0.01f);
            }
            else
            {
                // Large population - maintain diversity
                population.GeneticDiversity = math.min(1.0f, population.GeneticDiversity + deltaTime * 0.005f);
                population.InbreedingCoefficient = math.max(0.05f, population.InbreedingCoefficient - deltaTime * 0.005f);
            }
        }
    }

    /// <summary>
    /// Job for updating ecosystem resources
    /// </summary>

    public partial struct ResourceUpdateJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public ComponentLookup<EcosystemState> EcosystemLookup;


        public void Execute(Entity entity, ref EcosystemResources resources)
        {
            if (!EcosystemLookup.TryGetComponent(entity, out EcosystemState ecosystem))
                return;

            resources.UpdateResources(DeltaTime, ecosystem.TotalPopulation, ecosystem);

            // Update ecosystem food availability based on resources
            ecosystem.FoodAvailability = (resources.PlantBiomass + resources.PrimaryProducers) / 2f;
            ecosystem.WaterAvailability = resources.WaterSources;
        }
    }
}