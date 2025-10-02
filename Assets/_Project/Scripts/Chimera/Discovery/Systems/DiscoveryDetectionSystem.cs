using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Genetics.Core;
using Laboratory.Chimera.Discovery.Core;
using System.Collections.Generic;

namespace Laboratory.Chimera.Discovery.Systems
{
    /// <summary>
    /// ECS system that detects genetic discoveries during breeding and creature generation
    /// Analyzes genetic data to identify rare traits, mutations, and special combinations
    /// </summary>
    [BurstCompile]
    public partial struct DiscoveryDetectionSystem : ISystem
    {
        private EntityQuery _newCreatureQuery;
        private ComponentLookup<VisualGeneticData> _geneticsLookup;
        private ComponentLookup<Laboratory.Chimera.Discovery.Core.DiscoveryEvent> _discoveryLookup;

        // Discovery tracking
        private NativeHashMap<uint, bool> _discoveredTraits;
        private NativeList<Laboratory.Chimera.Discovery.Core.DiscoveryEvent> _pendingDiscoveries;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _newCreatureQuery = SystemAPI.QueryBuilder()
                .WithAll<VisualGeneticData>()
                .WithNone<Laboratory.Chimera.Discovery.Core.DiscoveryEvent>()
                .Build();

            _geneticsLookup = SystemAPI.GetComponentLookup<VisualGeneticData>(true);
            _discoveryLookup = SystemAPI.GetComponentLookup<Laboratory.Chimera.Discovery.Core.DiscoveryEvent>(false);

            _discoveredTraits = new NativeHashMap<uint, bool>(10000, Allocator.Persistent);
            _pendingDiscoveries = new NativeList<Laboratory.Chimera.Discovery.Core.DiscoveryEvent>(100, Allocator.Persistent);

            state.RequireForUpdate(_newCreatureQuery);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_discoveredTraits.IsCreated)
                _discoveredTraits.Dispose();
            if (_pendingDiscoveries.IsCreated)
                _pendingDiscoveries.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _geneticsLookup.Update(ref state);
            _discoveryLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            _pendingDiscoveries.Clear();

            // Detect discoveries in new creatures
            var detectionJob = new DiscoveryDetectionJob
            {
                GeneticsLookup = _geneticsLookup,
                DiscoveredTraits = _discoveredTraits,
                PendingDiscoveries = _pendingDiscoveries,
                ECB = ecb.AsParallelWriter(),
                CurrentTime = (uint)SystemAPI.Time.ElapsedTime
            };

            state.Dependency = detectionJob.ScheduleParallel(_newCreatureQuery, state.Dependency);
            state.Dependency.Complete();

            // Process discoveries for celebration system
            ProcessDiscoveries(ref state, ecb);
        }

        private void ProcessDiscoveries(ref SystemState state, EntityCommandBuffer ecb)
        {
            for (int i = 0; i < _pendingDiscoveries.Length; i++)
            {
                var discovery = _pendingDiscoveries[i];

                // Create discovery entity for celebration system
                var discoveryEntity = ecb.CreateEntity();
                ecb.AddComponent(discoveryEntity, discovery);

                // Mark trait as discovered globally
                uint traitHash = CalculateTraitHash(discovery.DiscoveredGenetics);
                _discoveredTraits.TryAdd(traitHash, true);

                // Trigger celebration event
                var celebrationEvent = ecb.CreateEntity();
                ecb.AddComponent(celebrationEvent, new CelebrationTrigger
                {
                    DiscoveryEntity = discoveryEntity,
                    TriggerTime = discovery.DiscoveryTimestamp,
                    IntensityLevel = discovery.CelebrationIntensity
                });
            }
        }

        [BurstCompile]
        private static uint CalculateTraitHash(VisualGeneticData genetics)
        {
            uint hash = 0;
            hash = math.hash(new uint4(genetics.Strength, genetics.Vitality, genetics.Agility, genetics.Intelligence));
            hash ^= math.hash(new uint2(genetics.Adaptability, genetics.Social));
            hash ^= (uint)genetics.SpecialMarkers;
            return hash;
        }
    }

    /// <summary>
    /// Job for parallel discovery detection
    /// </summary>
    [BurstCompile]
    public partial struct DiscoveryDetectionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<VisualGeneticData> GeneticsLookup;
        [ReadOnly] public NativeHashMap<uint, bool> DiscoveredTraits;
        public NativeList<Laboratory.Chimera.Discovery.Core.DiscoveryEvent> PendingDiscoveries;
        public EntityCommandBuffer.ParallelWriter ECB;
        public uint CurrentTime;

        [BurstCompile]
        public void Execute(Entity entity, [EntityIndexInQuery] int entityInQueryIndex, in VisualGeneticData genetics)
        {
            var discoveries = AnalyzeGenetics(entity, genetics);

            for (int i = 0; i < discoveries.Length; i++)
            {
                var discovery = discoveries[i];
                discovery.DiscoveryTimestamp = CurrentTime;

                PendingDiscoveries.Add(discovery);
                ECB.AddComponent(entityInQueryIndex, entity, discovery);
            }

            discoveries.Dispose();
        }

        [BurstCompile]
        private NativeList<Laboratory.Chimera.Discovery.Core.DiscoveryEvent> AnalyzeGenetics(Entity entity, VisualGeneticData genetics)
        {
            var discoveries = new NativeList<Laboratory.Chimera.Discovery.Core.DiscoveryEvent>(5, Allocator.Temp);

            // Check for perfect genetics
            if (IsPerfectGenetics(genetics))
            {
                discoveries.Add(CreateDiscovery(Laboratory.Chimera.Discovery.Core.DiscoveryType.PerfectGenetics, Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Legendary, entity, genetics));
            }

            // Check for rare mutations
            if (HasRareMutation(genetics))
            {
                discoveries.Add(CreateDiscovery(Laboratory.Chimera.Discovery.Core.DiscoveryType.RareMutation, Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Epic, entity, genetics));
            }

            // Check for special markers
            if (genetics.SpecialMarkers != GeneticMarkerFlags.None)
            {
                var markerRarity = CalculateMarkerRarity(genetics.SpecialMarkers);
                discoveries.Add(CreateDiscovery(Laboratory.Chimera.Discovery.Core.DiscoveryType.SpecialMarker, markerRarity, entity, genetics));
            }

            // Check for new trait combinations
            uint traitHash = CalculateTraitHash(genetics);
            if (!DiscoveredTraits.ContainsKey(traitHash))
            {
                var rarity = CalculateTraitRarity(genetics);
                discoveries.Add(CreateDiscovery(DiscoveryType.NewTrait, rarity, entity, genetics));
            }

            // Check for legendary lineage (placeholder - would need breeding history)
            if (HasLegendaryPotential(genetics))
            {
                discoveries.Add(CreateDiscovery(Laboratory.Chimera.Discovery.Core.DiscoveryType.LegendaryLineage, Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Mythical, entity, genetics));
            }

            return discoveries;
        }

        [BurstCompile]
        private Laboratory.Chimera.Discovery.Core.DiscoveryEvent CreateDiscovery(Laboratory.Chimera.Discovery.Core.DiscoveryType type, Laboratory.Chimera.Discovery.Core.DiscoveryRarity rarity, Entity entity, VisualGeneticData genetics)
        {
            bool isFirstTime = !DiscoveredTraits.ContainsKey(CalculateTraitHash(genetics));
            bool isWorldFirst = isFirstTime; // Simplified - would check global database

            return new Laboratory.Chimera.Discovery.Core.DiscoveryEvent
            {
                Type = type,
                Rarity = rarity,
                SignificanceScore = Laboratory.Chimera.Discovery.Core.DiscoveryEvent.CalculateSignificance(type, rarity, isFirstTime, isWorldFirst),
                DiscoveredCreature = entity,
                DiscoveredGenetics = genetics,
                SpecialMarkers = genetics.SpecialMarkers,
                CelebrationIntensity = CalculateCelebrationIntensity(rarity, isWorldFirst),
                IsFirstTimeDiscovery = isFirstTime,
                IsWorldFirst = isWorldFirst,
                DiscoveryName = new FixedString64Bytes(Laboratory.Chimera.Discovery.Core.DiscoveryEvent.GenerateDiscoveryName(type, genetics, genetics.SpecialMarkers))
            };
        }

        [BurstCompile]
        private static bool IsPerfectGenetics(VisualGeneticData genetics)
        {
            return genetics.Strength >= 95 && genetics.Vitality >= 95 && genetics.Agility >= 95 &&
                   genetics.Intelligence >= 95 && genetics.Adaptability >= 95 && genetics.Social >= 95;
        }

        [BurstCompile]
        private static bool HasRareMutation(VisualGeneticData genetics)
        {
            // Check for statistical outliers that indicate mutations
            var traits = new NativeArray<byte>(6, Allocator.Temp);
            traits[0] = genetics.Strength;
            traits[1] = genetics.Vitality;
            traits[2] = genetics.Agility;
            traits[3] = genetics.Intelligence;
            traits[4] = genetics.Adaptability;
            traits[5] = genetics.Social;

            int extremeTraits = 0;
            for (int i = 0; i < traits.Length; i++)
            {
                if (traits[i] >= 90 || traits[i] <= 10)
                    extremeTraits++;
            }

            traits.Dispose();
            return extremeTraits >= 3;
        }

        [BurstCompile]
        private static bool HasLegendaryPotential(VisualGeneticData genetics)
        {
            int totalStats = genetics.Strength + genetics.Vitality + genetics.Agility +
                           genetics.Intelligence + genetics.Adaptability + genetics.Social;
            return totalStats >= 500 && genetics.SpecialMarkers != GeneticMarkerFlags.None;
        }

        [BurstCompile]
        private static DiscoveryRarity CalculateMarkerRarity(GeneticMarkerFlags markers)
        {
            int markerCount = math.countbits((uint)markers);
            return markerCount switch
            {
                1 => DiscoveryRarity.Uncommon,
                2 => DiscoveryRarity.Rare,
                3 => DiscoveryRarity.Epic,
                4 => DiscoveryRarity.Legendary,
                _ => DiscoveryRarity.Mythical
            };
        }

        [BurstCompile]
        private static DiscoveryRarity CalculateTraitRarity(VisualGeneticData genetics)
        {
            int totalStats = genetics.Strength + genetics.Vitality + genetics.Agility +
                           genetics.Intelligence + genetics.Adaptability + genetics.Social;

            return totalStats switch
            {
                >= 550 => DiscoveryRarity.Mythical,
                >= 500 => DiscoveryRarity.Legendary,
                >= 450 => DiscoveryRarity.Epic,
                >= 400 => DiscoveryRarity.Rare,
                >= 350 => DiscoveryRarity.Uncommon,
                _ => DiscoveryRarity.Common
            };
        }

        [BurstCompile]
        private static float CalculateCelebrationIntensity(DiscoveryRarity rarity, bool isWorldFirst)
        {
            float baseIntensity = rarity switch
            {
                DiscoveryRarity.Common => 0.2f,
                DiscoveryRarity.Uncommon => 0.4f,
                DiscoveryRarity.Rare => 0.6f,
                DiscoveryRarity.Epic => 0.8f,
                DiscoveryRarity.Legendary => 1.0f,
                DiscoveryRarity.Mythical => 1.5f,
                _ => 0.1f
            };

            return isWorldFirst ? baseIntensity * 2.0f : baseIntensity;
        }

        [BurstCompile]
        private static uint CalculateTraitHash(VisualGeneticData genetics)
        {
            uint hash = 0;
            hash = math.hash(new uint4(genetics.Strength, genetics.Vitality, genetics.Agility, genetics.Intelligence));
            hash ^= math.hash(new uint2(genetics.Adaptability, genetics.Social));
            hash ^= (uint)genetics.SpecialMarkers;
            return hash;
        }
    }

    /// <summary>
    /// Component to trigger celebration events
    /// </summary>
    public struct CelebrationTrigger : IComponentData
    {
        public Entity DiscoveryEntity;
        public uint TriggerTime;
        public float IntensityLevel;
    }
}