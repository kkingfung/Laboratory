using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Laboratory.Core.ECS.Components;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.Breeding;
using UnityEngine;

namespace Laboratory.Core.ECS.Systems
{
    /// <summary>
    /// ECS BREEDING SYSTEM - Integrates with unified Chimera architecture
    /// FEATURES: Genetics-driven mate selection, territorial breeding requirements, job parallelization
    /// INTEGRATION: Works seamlessly with ChimeraBehaviorSystem and configuration
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ChimeraBehaviorSystem))]
    public partial class ChimeraBreedingSystem : SystemBase
    {
        private ChimeraUniverseConfiguration _config;
        private EntityQuery _breedingReadyQuery;
        private EntityQuery _pregnantQuery;
        private EntityQuery _caringQuery;

        // Spatial hash for mate finding
        private NativeMultiHashMap<int, BreedingCandidate> _spatialBreedingHash;

        // Legacy system integration
        private Laboratory.Chimera.Breeding.BreedingSystem _legacyBreedingSystem;

        protected override void OnCreate()
        {
            // Load configuration
            _config = Resources.Load<ChimeraUniverseConfiguration>("Configs/ChimeraUniverse");
            if (_config == null)
            {
                UnityEngine.Debug.LogError("ChimeraUniverseConfiguration not found! Using defaults.");
                _config = ChimeraUniverseConfiguration.CreateDefault();
            }

            // Create entity queries for different breeding states
            _breedingReadyQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<BreedingComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>(),
                ComponentType.ReadOnly<CreatureIdentityComponent>(),
                ComponentType.ReadOnly<SocialTerritoryComponent>(),
                ComponentType.ReadOnly<EnvironmentalComponent>(),
                ComponentType.ReadOnly<BehaviorStateComponent>(),
                ComponentType.ReadOnly<LocalToWorld>()
            }, new ComponentType[]
            {
                ComponentType.Exclude<PregnantTag>(),
                ComponentType.Exclude<CaringTag>()
            });

            _pregnantQuery = GetEntityQuery(ComponentType.ReadWrite<BreedingComponent>(),
                                           ComponentType.ReadOnly<PregnantTag>());

            _caringQuery = GetEntityQuery(ComponentType.ReadWrite<BreedingComponent>(),
                                         ComponentType.ReadOnly<CaringTag>());

            // Initialize spatial hash
            _spatialBreedingHash = new NativeMultiHashMap<int, BreedingCandidate>(1000, Allocator.Persistent);

            // Initialize legacy system for complex breeding calculations
            _legacyBreedingSystem = new Laboratory.Chimera.Breeding.BreedingSystem(null);

            RequireForUpdate(_breedingReadyQuery);
        }

        protected override void OnDestroy()
        {
            if (_spatialBreedingHash.IsCreated) _spatialBreedingHash.Dispose();
            _legacyBreedingSystem?.Dispose();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Step 1: Update pregnancy progress
            UpdatePregnancies(deltaTime);

            // Step 2: Update parental care
            UpdateParentalCare(deltaTime);

            // Step 3: Process breeding attempts for ready creatures
            ProcessBreedingAttempts(deltaTime, currentTime);
        }

        private void UpdatePregnancies(float deltaTime)
        {
            var pregnancyUpdateJob = new PregnancyUpdateJob
            {
                config = _config,
                deltaTime = deltaTime,
                currentTime = (float)SystemAPI.Time.ElapsedTime,
                commandBuffer = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(World.Unmanaged).AsParallelWriter(),
                entityTypeHandle = GetEntityTypeHandle(),
                breedingTypeHandle = GetComponentTypeHandle<BreedingComponent>(false)
            };

            Dependency = pregnancyUpdateJob.ScheduleParallel(_pregnantQuery, Dependency);
        }

        private void UpdateParentalCare(float deltaTime)
        {
            var parentalCareJob = new ParentalCareUpdateJob
            {
                config = _config,
                deltaTime = deltaTime,
                commandBuffer = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(World.Unmanaged).AsParallelWriter(),
                entityTypeHandle = GetEntityTypeHandle(),
                breedingTypeHandle = GetComponentTypeHandle<BreedingComponent>(false)
            };

            Dependency = parentalCareJob.ScheduleParallel(_caringQuery, Dependency);
        }

        private void ProcessBreedingAttempts(float deltaTime, float currentTime)
        {
            int breedingReadyCount = _breedingReadyQuery.CalculateEntityCount();
            if (breedingReadyCount == 0) return;

            // Clear and rebuild spatial hash
            _spatialBreedingHash.Clear();

            // Step 1: Build spatial hash for mate finding
            var buildHashJob = new BuildBreedingSpatialHashJob
            {
                spatialHash = _spatialBreedingHash.AsParallelWriter(),
                cellSize = _config.Performance.spatialHashCellSize,
                entityTypeHandle = GetEntityTypeHandle(),
                transformTypeHandle = GetComponentTypeHandle<LocalToWorld>(true),
                breedingTypeHandle = GetComponentTypeHandle<BreedingComponent>(true),
                geneticsTypeHandle = GetComponentTypeHandle<GeneticDataComponent>(true),
                identityTypeHandle = GetComponentTypeHandle<CreatureIdentityComponent>(true)
            };

            var hashHandle = buildHashJob.ScheduleParallel(_breedingReadyQuery, Dependency);

            // Step 2: Find mates and attempt breeding
            var breedingAttemptJob = new BreedingAttemptJob
            {
                config = _config,
                spatialHash = _spatialBreedingHash,
                deltaTime = deltaTime,
                currentTime = currentTime,
                randomSeed = (uint)currentTime,
                commandBuffer = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(World.Unmanaged).AsParallelWriter(),
                entityTypeHandle = GetEntityTypeHandle(),
                transformTypeHandle = GetComponentTypeHandle<LocalToWorld>(true),
                breedingTypeHandle = GetComponentTypeHandle<BreedingComponent>(false),
                geneticsTypeHandle = GetComponentTypeHandle<GeneticDataComponent>(true),
                identityTypeHandle = GetComponentTypeHandle<CreatureIdentityComponent>(true),
                territoryTypeHandle = GetComponentTypeHandle<SocialTerritoryComponent>(true),
                behaviorTypeHandle = GetComponentTypeHandle<BehaviorStateComponent>(true)
            };

            Dependency = breedingAttemptJob.ScheduleParallel(_breedingReadyQuery, hashHandle);
        }

        [BurstCompile]
        struct BuildBreedingSpatialHashJob : IJobChunk
        {
            [WriteOnly] public NativeMultiHashMap<int, BreedingCandidate>.ParallelWriter spatialHash;
            [ReadOnly] public float cellSize;
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> transformTypeHandle;
            [ReadOnly] public ComponentTypeHandle<BreedingComponent> breedingTypeHandle;
            [ReadOnly] public ComponentTypeHandle<GeneticDataComponent> geneticsTypeHandle;
            [ReadOnly] public ComponentTypeHandle<CreatureIdentityComponent> identityTypeHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(entityTypeHandle);
                var transforms = chunk.GetNativeArray(ref transformTypeHandle);
                var breeding = chunk.GetNativeArray(ref breedingTypeHandle);
                var genetics = chunk.GetNativeArray(ref geneticsTypeHandle);
                var identities = chunk.GetNativeArray(ref identityTypeHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    if (breeding[i].Status != BreedingStatus.Seeking) continue;

                    var position = transforms[i].Position;
                    int cellKey = GetSpatialHashKey(position, cellSize);

                    var candidate = new BreedingCandidate
                    {
                        entity = entities[i],
                        position = position,
                        genetics = genetics[i],
                        identity = identities[i],
                        breeding = breeding[i]
                    };

                    spatialHash.Add(cellKey, candidate);
                }
            }

            private static int GetSpatialHashKey(float3 position, float cellSize)
            {
                int3 cell = (int3)math.floor(position / cellSize);
                return cell.x + cell.y * 1000 + cell.z * 1000000;
            }
        }

        [BurstCompile]
        struct BreedingAttemptJob : IJobChunk
        {
            [ReadOnly] public ChimeraUniverseConfiguration config;
            [ReadOnly] public NativeMultiHashMap<int, BreedingCandidate> spatialHash;
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float currentTime;
            [ReadOnly] public uint randomSeed;
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<LocalToWorld> transformTypeHandle;
            public ComponentTypeHandle<BreedingComponent> breedingTypeHandle;
            [ReadOnly] public ComponentTypeHandle<GeneticDataComponent> geneticsTypeHandle;
            [ReadOnly] public ComponentTypeHandle<CreatureIdentityComponent> identityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<SocialTerritoryComponent> territoryTypeHandle;
            [ReadOnly] public ComponentTypeHandle<BehaviorStateComponent> behaviorTypeHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(entityTypeHandle);
                var transforms = chunk.GetNativeArray(ref transformTypeHandle);
                var breeding = chunk.GetNativeArray(ref breedingTypeHandle);
                var genetics = chunk.GetNativeArray(ref geneticsTypeHandle);
                var identities = chunk.GetNativeArray(ref identityTypeHandle);
                var territories = chunk.GetNativeArray(ref territoryTypeHandle);
                var behaviors = chunk.GetNativeArray(ref behaviorTypeHandle);

                var random = new Unity.Mathematics.Random(randomSeed + (uint)unfilteredChunkIndex);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    var breedingComp = breeding[i];

                    // Only process creatures actively seeking mates
                    if (breedingComp.Status != BreedingStatus.Seeking) continue;
                    if (behaviors[i].CurrentBehavior != CreatureBehaviorType.Breeding) continue;

                    // Check breeding readiness
                    if (breedingComp.BreedingReadiness < 0.7f) continue;

                    // Check territory requirement if needed
                    if (breedingComp.RequiresTerritory && !territories[i].HasTerritory) continue;

                    // Find potential mates in nearby area
                    var position = transforms[i].Position;
                    var potentialMate = FindBestMate(entity, position, genetics[i], identities[i],
                                                   breedingComp, ref random);

                    if (potentialMate.entity != Entity.Null)
                    {
                        // Calculate breeding success chance
                        float successChance = CalculateBreedingSuccessChance(genetics[i], potentialMate.genetics,
                                                                           identities[i], potentialMate.identity);

                        if (random.NextFloat() < successChance)
                        {
                            // Successful breeding attempt
                            StartBreeding(entity, potentialMate.entity, unfilteredChunkIndex);
                        }
                        else
                        {
                            // Failed attempt - increment counter and set cooldown
                            breedingComp.CourtshipAttempts++;
                            breedingComp.BreedingCooldown = config.Breeding.breedingCooldown * 0.5f;
                        }
                    }

                    breeding[i] = breedingComp;
                }
            }

            private BreedingCandidate FindBestMate(Entity self, float3 position, GeneticDataComponent selfGenetics,
                                                 CreatureIdentityComponent selfIdentity, BreedingComponent selfBreeding,
                                                 ref Unity.Mathematics.Random random)
            {
                int cellKey = GetSpatialHashKey(position, config.Performance.spatialHashCellSize);
                var bestMate = new BreedingCandidate { entity = Entity.Null };
                float bestScore = 0f;

                // Check current and nearby cells
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int searchKey = cellKey + dx + dz * 1000;

                        if (spatialHash.TryGetFirstValue(searchKey, out var candidate, out var iterator))
                        {
                            do
                            {
                                if (candidate.entity.Equals(self)) continue;

                                float distance = math.distance(position, candidate.position);
                                if (distance > config.Breeding.maxBreedingDistance) continue;

                                // Check basic compatibility
                                if (!IsCompatibleSpecies(selfIdentity.Species, candidate.identity.Species)) continue;
                                if (!IsCompatibleAge(selfIdentity, candidate.identity)) continue;

                                // Calculate mate quality score
                                float mateScore = CalculateMateScore(selfGenetics, candidate.genetics,
                                                                   selfIdentity, candidate.identity,
                                                                   selfBreeding, distance);

                                if (mateScore > bestScore)
                                {
                                    bestScore = mateScore;
                                    bestMate = candidate;
                                }

                            } while (spatialHash.TryGetNextValue(out candidate, ref iterator));
                        }
                    }
                }

                return bestMate;
            }

            private bool IsCompatibleSpecies(FixedString64Bytes species1, FixedString64Bytes species2)
            {
                // For now, only same species can breed
                return species1.Equals(species2);
            }

            private bool IsCompatibleAge(CreatureIdentityComponent identity1, CreatureIdentityComponent identity2)
            {
                // Both must be adult
                return identity1.CurrentLifeStage == LifeStage.Adult &&
                       identity2.CurrentLifeStage == LifeStage.Adult;
            }

            private float CalculateMateScore(GeneticDataComponent selfGenetics, GeneticDataComponent mateGenetics,
                                           CreatureIdentityComponent selfIdentity, CreatureIdentityComponent mateIdentity,
                                           BreedingComponent selfBreeding, float distance)
            {
                float score = 1f;

                // Genetic diversity preference (avoid too similar or too different)
                float geneticSimilarity = CalculateGeneticSimilarity(selfGenetics, mateGenetics);
                float diversityScore = GetDiversityScore(geneticSimilarity);
                score *= diversityScore;

                // Fitness preference
                float mateFitness = mateGenetics.OverallFitness;
                score *= config.Breeding.fitnessPreference * mateFitness + (1f - config.Breeding.fitnessPreference);

                // Distance penalty (closer is better, up to a point)
                float distanceScore = math.saturate(1f - distance / config.Breeding.maxBreedingDistance);
                score *= distanceScore;

                // Age compatibility (prefer similar ages)
                float ageDifference = math.abs(selfIdentity.Age - mateIdentity.Age) / selfIdentity.MaxLifespan;
                float ageScore = 1f - math.saturate(ageDifference * 2f);
                score *= ageScore;

                return score;
            }

            private float CalculateGeneticSimilarity(GeneticDataComponent genetics1, GeneticDataComponent genetics2)
            {
                // Compare key genetic traits
                float similarity = 0f;
                similarity += 1f - math.abs(genetics1.Aggression - genetics2.Aggression);
                similarity += 1f - math.abs(genetics1.Sociability - genetics2.Sociability);
                similarity += 1f - math.abs(genetics1.Intelligence - genetics2.Intelligence);
                similarity += 1f - math.abs(genetics1.Size - genetics2.Size);
                similarity += 1f - math.abs(genetics1.Metabolism - genetics2.Metabolism);

                return similarity / 5f;
            }

            private float GetDiversityScore(float similarity)
            {
                // Sweet spot for genetic diversity
                if (similarity < 0.3f) return 0.5f; // Too different
                if (similarity < 0.7f) return 1.0f; // Good diversity
                if (similarity < 0.9f) return 0.8f; // Acceptable
                return 0.3f; // Too similar (inbreeding risk)
            }

            private float CalculateBreedingSuccessChance(GeneticDataComponent parent1, GeneticDataComponent parent2,
                                                       CreatureIdentityComponent identity1, CreatureIdentityComponent identity2)
            {
                float baseChance = 0.7f;

                // Fertility affects success
                float fertilityFactor = (parent1.Fertility + parent2.Fertility) / 2f;
                baseChance *= fertilityFactor;

                // Age affects breeding success
                float ageRatio1 = identity1.Age / identity1.MaxLifespan;
                float ageRatio2 = identity2.Age / identity2.MaxLifespan;
                float optimalAge = 0.5f; // Peak breeding age
                float ageFactor1 = 1f - math.abs(ageRatio1 - optimalAge) * 2f;
                float ageFactor2 = 1f - math.abs(ageRatio2 - optimalAge) * 2f;
                float avgAgeFactor = (ageFactor1 + ageFactor2) / 2f;
                baseChance *= math.max(0.3f, avgAgeFactor);

                // Genetic diversity affects success
                float similarity = CalculateGeneticSimilarity(parent1, parent2);
                float diversityBonus = GetDiversityScore(similarity);
                baseChance *= diversityBonus;

                return math.saturate(baseChance);
            }

            private void StartBreeding(Entity parent1, Entity parent2, int chunkIndex)
            {
                // Set both parents to mating status
                commandBuffer.SetComponent(chunkIndex, parent1, new BreedingComponent
                {
                    Status = BreedingStatus.Mating,
                    Partner = parent2,
                    CourtshipProgress = 1f,
                    BreedingReadiness = 1f
                });

                commandBuffer.SetComponent(chunkIndex, parent2, new BreedingComponent
                {
                    Status = BreedingStatus.Mating,
                    Partner = parent1,
                    CourtshipProgress = 1f,
                    BreedingReadiness = 1f
                });

                // Add mating behavior tags
                commandBuffer.AddComponent<BreedingBehaviorTag>(chunkIndex, parent1, new BreedingBehaviorTag { BreedingTarget = parent2 });
                commandBuffer.AddComponent<BreedingBehaviorTag>(chunkIndex, parent2, new BreedingBehaviorTag { BreedingTarget = parent1 });
            }

            private static int GetSpatialHashKey(float3 position, float cellSize)
            {
                int3 cell = (int3)math.floor(position / cellSize);
                return cell.x + cell.y * 1000 + cell.z * 1000000;
            }
        }

        [BurstCompile]
        struct PregnancyUpdateJob : IJobChunk
        {
            [ReadOnly] public ChimeraUniverseConfiguration config;
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float currentTime;
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<BreedingComponent> breedingTypeHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(entityTypeHandle);
                var breeding = chunk.GetNativeArray(ref breedingTypeHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    var breedingComp = breeding[i];

                    // Update pregnancy progress
                    breedingComp.PregnancyProgress += deltaTime / config.Breeding.gestationTime;

                    if (breedingComp.PregnancyProgress >= 1f)
                    {
                        // Give birth!
                        GiveBirth(entity, breedingComp, unfilteredChunkIndex);

                        // Transition to caring status
                        breedingComp.Status = BreedingStatus.Caring;
                        breedingComp.PregnancyProgress = 0f;
                        breedingComp.BreedingCooldown = config.Breeding.breedingCooldown;

                        commandBuffer.RemoveComponent<PregnantTag>(unfilteredChunkIndex, entity);
                        commandBuffer.AddComponent<CaringTag>(unfilteredChunkIndex, entity);
                        commandBuffer.AddComponent<ParentingBehaviorTag>(unfilteredChunkIndex, entity, new ParentingBehaviorTag { Offspring = Entity.Null });
                    }

                    breeding[i] = breedingComp;
                }
            }

            private void GiveBirth(Entity parent, BreedingComponent breeding, int chunkIndex)
            {
                // Create offspring entities based on expected count
                for (int offspring = 0; offspring < breeding.ExpectedOffspring; offspring++)
                {
                    var baby = commandBuffer.CreateEntity(chunkIndex);

                    // Add basic components to offspring
                    commandBuffer.AddComponent<CreatureIdentityComponent>(chunkIndex, baby, new CreatureIdentityComponent
                    {
                        Species = "OffspringSpecies", // Would be determined from parents
                        CreatureName = $"Baby_{offspring}",
                        UniqueID = (uint)currentTime + (uint)offspring,
                        Generation = 1, // Would be parent generation + 1
                        Age = 0f,
                        MaxLifespan = 100f,
                        CurrentLifeStage = LifeStage.Juvenile,
                        Rarity = RarityLevel.Common,
                        OriginalParent1 = parent,
                        OriginalParent2 = breeding.Partner
                    });

                    // Add other required components (genetics would be inherited from parents)
                    commandBuffer.AddComponent<GeneticDataComponent>(chunkIndex, baby, new GeneticDataComponent
                    {
                        // Simplified offspring genetics - in full system would be properly inherited
                        Aggression = 0.5f,
                        Sociability = 0.5f,
                        Fertility = 0.5f,
                        Size = 0.3f, // Baby size
                        NativeBiome = BiomeType.Grassland
                    });

                    commandBuffer.AddComponent<BehaviorStateComponent>(chunkIndex, baby);
                    commandBuffer.AddComponent<CreatureNeedsComponent>(chunkIndex, baby);
                    commandBuffer.AddComponent<SocialTerritoryComponent>(chunkIndex, baby);
                    commandBuffer.AddComponent<EnvironmentalComponent>(chunkIndex, baby);
                    commandBuffer.AddComponent<BreedingComponent>(chunkIndex, baby, new BreedingComponent
                    {
                        Status = BreedingStatus.NotReady // Too young to breed
                    });
                }
            }
        }

        [BurstCompile]
        struct ParentalCareUpdateJob : IJobChunk
        {
            [ReadOnly] public ChimeraUniverseConfiguration config;
            [ReadOnly] public float deltaTime;
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            public ComponentTypeHandle<BreedingComponent> breedingTypeHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(entityTypeHandle);
                var breeding = chunk.GetNativeArray(ref breedingTypeHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entities[i];
                    var breedingComp = breeding[i];

                    // Update parental care timer
                    breedingComp.BreedingCooldown -= deltaTime;

                    if (breedingComp.BreedingCooldown <= 0f)
                    {
                        // Parental care period ended
                        breedingComp.Status = BreedingStatus.Cooldown;
                        breedingComp.BreedingCooldown = config.Breeding.breedingCooldown;

                        commandBuffer.RemoveComponent<CaringTag>(unfilteredChunkIndex, entity);
                        commandBuffer.RemoveComponent<ParentingBehaviorTag>(unfilteredChunkIndex, entity);
                    }

                    breeding[i] = breedingComp;
                }
            }
        }
    }

    // Supporting structures
    public struct BreedingCandidate
    {
        public Entity entity;
        public float3 position;
        public GeneticDataComponent genetics;
        public CreatureIdentityComponent identity;
        public BreedingComponent breeding;
    }

    // Tag components for breeding states
    public struct PregnantTag : IComponentData { }
    public struct CaringTag : IComponentData { }
}