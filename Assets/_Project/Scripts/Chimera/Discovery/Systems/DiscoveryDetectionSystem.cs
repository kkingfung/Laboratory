using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Discovery.Core;
using System.Collections.Generic;
using DiscoveryType = Laboratory.Chimera.Discovery.Core.DiscoveryType;
using DiscoveryRarity = Laboratory.Chimera.Discovery.Core.DiscoveryRarity;

namespace Laboratory.Chimera.Discovery.Systems
{
    /// <summary>
    /// ECS system that detects genetic discoveries during breeding and creature generation
    /// Analyzes genetic data to identify rare traits, mutations, and special combinations
    /// </summary>

    public partial struct DiscoveryDetectionSystem : ISystem
    {
        private EntityQuery _newCreatureQuery;
        private ComponentLookup<VisualGeneticData> _geneticsLookup;
        private ComponentLookup<Laboratory.Chimera.Discovery.Core.DiscoveryEvent> _discoveryLookup;

        // Discovery tracking
        private NativeHashMap<uint, bool> _discoveredTraits;
        private NativeList<Laboratory.Chimera.Discovery.Core.DiscoveryEvent> _pendingDiscoveries;


        public void OnCreate(ref SystemState state)
        {
            _newCreatureQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<VisualGeneticData>(),
                ComponentType.Exclude<Laboratory.Chimera.Discovery.Core.DiscoveryEvent>()
            );

            _geneticsLookup = SystemAPI.GetComponentLookup<VisualGeneticData>(true);
            _discoveryLookup = SystemAPI.GetComponentLookup<Laboratory.Chimera.Discovery.Core.DiscoveryEvent>(false);

            _discoveredTraits = new NativeHashMap<uint, bool>(10000, Allocator.Persistent);
            _pendingDiscoveries = new NativeList<Laboratory.Chimera.Discovery.Core.DiscoveryEvent>(100, Allocator.Persistent);

            state.RequireForUpdate(_newCreatureQuery);
        }


        public void OnDestroy(ref SystemState state)
        {
            if (_discoveredTraits.IsCreated)
                _discoveredTraits.Dispose();
            if (_pendingDiscoveries.IsCreated)
                _pendingDiscoveries.Dispose();
        }


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
                uint traitHash = DiscoveryDetectionJob.CalculateTraitHash(discovery.DiscoveredGenetics);
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
    }

    /// <summary>
    /// Job for parallel discovery detection
    /// </summary>

    /// <summary>
    /// Burst-compatible string constants for discovery names
    /// Uses manual byte assignment to avoid BC1016 (managed String.get_Length calls)
    /// </summary>
    public static class DiscoveryStrings
    {
        public static FixedString32Bytes Enhanced { get { var s = new FixedString32Bytes(); s.Length = 8; s[0]=69; s[1]=110; s[2]=104; s[3]=97; s[4]=110; s[5]=99; s[6]=101; s[7]=100; return s; } }
        public static FixedString32Bytes Mutant { get { var s = new FixedString32Bytes(); s.Length = 6; s[0]=77; s[1]=117; s[2]=116; s[3]=97; s[4]=110; s[5]=116; return s; } }
        public static FixedString32Bytes Marked { get { var s = new FixedString32Bytes(); s.Length = 6; s[0]=77; s[1]=97; s[2]=114; s[3]=107; s[4]=101; s[5]=100; return s; } }
        public static FixedString32Bytes Perfect { get { var s = new FixedString32Bytes(); s.Length = 7; s[0]=80; s[1]=101; s[2]=114; s[3]=102; s[4]=101; s[5]=99; s[6]=116; return s; } }
        public static FixedString32Bytes Hybrid { get { var s = new FixedString32Bytes(); s.Length = 6; s[0]=72; s[1]=121; s[2]=98; s[3]=114; s[4]=105; s[5]=100; return s; } }
        public static FixedString32Bytes Legendary { get { var s = new FixedString32Bytes(); s.Length = 9; s[0]=76; s[1]=101; s[2]=103; s[3]=101; s[4]=110; s[5]=100; s[6]=97; s[7]=114; s[8]=121; return s; } }
        public static FixedString32Bytes Unknown { get { var s = new FixedString32Bytes(); s.Length = 7; s[0]=85; s[1]=110; s[2]=107; s[3]=110; s[4]=111; s[5]=119; s[6]=110; return s; } }

        public static FixedString32Bytes Titan { get { var s = new FixedString32Bytes(); s.Length = 5; s[0]=84; s[1]=105; s[2]=116; s[3]=97; s[4]=110; return s; } }
        public static FixedString32Bytes Eternal { get { var s = new FixedString32Bytes(); s.Length = 7; s[0]=69; s[1]=116; s[2]=101; s[3]=114; s[4]=110; s[5]=97; s[6]=108; return s; } }
        public static FixedString32Bytes Swift { get { var s = new FixedString32Bytes(); s.Length = 5; s[0]=83; s[1]=119; s[2]=105; s[3]=102; s[4]=116; return s; } }
        public static FixedString32Bytes Genius { get { var s = new FixedString32Bytes(); s.Length = 6; s[0]=71; s[1]=101; s[2]=110; s[3]=105; s[4]=117; s[5]=115; return s; } }
        public static FixedString32Bytes Evolved { get { var s = new FixedString32Bytes(); s.Length = 7; s[0]=69; s[1]=118; s[2]=111; s[3]=108; s[4]=118; s[5]=101; s[6]=100; return s; } }
        public static FixedString32Bytes Alpha { get { var s = new FixedString32Bytes(); s.Length = 5; s[0]=65; s[1]=108; s[2]=112; s[3]=104; s[4]=97; return s; } }
        public static FixedString32Bytes Balanced { get { var s = new FixedString32Bytes(); s.Length = 8; s[0]=66; s[1]=97; s[2]=108; s[3]=97; s[4]=110; s[5]=99; s[6]=101; s[7]=100; return s; } }

        public static FixedString32Bytes Lumina { get { var s = new FixedString32Bytes(); s.Length = 7; s[0]=32; s[1]=76; s[2]=117; s[3]=109; s[4]=105; s[5]=110; s[6]=97; return s; } }
        public static FixedString32Bytes Elemental { get { var s = new FixedString32Bytes(); s.Length = 10; s[0]=32; s[1]=69; s[2]=108; s[3]=101; s[4]=109; s[5]=101; s[6]=110; s[7]=116; s[8]=97; s[9]=108; return s; } }
        public static FixedString32Bytes Prime { get { var s = new FixedString32Bytes(); s.Length = 6; s[0]=32; s[1]=80; s[2]=114; s[3]=105; s[4]=109; s[5]=101; return s; } }
        public static FixedString32Bytes HybridSuffix { get { var s = new FixedString32Bytes(); s.Length = 7; s[0]=32; s[1]=72; s[2]=121; s[3]=98; s[4]=114; s[5]=105; s[6]=100; return s; } }
        public static FixedString32Bytes Rex { get { var s = new FixedString32Bytes(); s.Length = 4; s[0]=32; s[1]=82; s[2]=101; s[3]=120; return s; } }
    }

    [BurstCompile]
    public partial struct DiscoveryDetectionJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<VisualGeneticData> GeneticsLookup;
        [ReadOnly] public NativeHashMap<uint, bool> DiscoveredTraits;
        public NativeList<Laboratory.Chimera.Discovery.Core.DiscoveryEvent> PendingDiscoveries;
        public EntityCommandBuffer.ParallelWriter ECB;
        public uint CurrentTime;


        public void Execute(Entity entity, [EntityIndexInQuery] int entityInQueryIndex)
        {
            if (!GeneticsLookup.TryGetComponent(entity, out var genetics))
                return;

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


        private NativeList<Laboratory.Chimera.Discovery.Core.DiscoveryEvent> AnalyzeGenetics(Entity entity, in VisualGeneticData genetics)
        {
            var discoveries = new NativeList<Laboratory.Chimera.Discovery.Core.DiscoveryEvent>(5, Allocator.Temp);

            // Check for perfect genetics
            if (IsPerfectGenetics(genetics))
            {
                CreateDiscovery(out Laboratory.Chimera.Discovery.Core.DiscoveryEvent discovery, Laboratory.Chimera.Discovery.Core.DiscoveryType.PerfectGenetics, Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Legendary, entity, genetics);
                discoveries.Add(discovery);
            }

            // Check for rare mutations
            if (HasRareMutation(genetics))
            {
                CreateDiscovery(out Laboratory.Chimera.Discovery.Core.DiscoveryEvent discovery, Laboratory.Chimera.Discovery.Core.DiscoveryType.RareMutation, Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Epic, entity, genetics);
                discoveries.Add(discovery);
            }

            // Check for special markers
            if (genetics.SpecialMarkers != GeneticMarkerFlags.None)
            {
                var markerRarity = CalculateMarkerRarity(genetics.SpecialMarkers);
                CreateDiscovery(out Laboratory.Chimera.Discovery.Core.DiscoveryEvent discovery, Laboratory.Chimera.Discovery.Core.DiscoveryType.SpecialMarker, markerRarity, entity, genetics);
                discoveries.Add(discovery);
            }

            // Check for new trait combinations
            uint traitHash = CalculateTraitHash(genetics);
            if (!DiscoveredTraits.ContainsKey(traitHash))
            {
                var rarity = CalculateTraitRarity(genetics);
                CreateDiscovery(out Laboratory.Chimera.Discovery.Core.DiscoveryEvent discovery, Laboratory.Chimera.Discovery.Core.DiscoveryType.NewTrait, rarity, entity, genetics);
                discoveries.Add(discovery);
            }

            // Check for legendary lineage (placeholder - would need breeding history)
            if (HasLegendaryPotential(genetics))
            {
                CreateDiscovery(out Laboratory.Chimera.Discovery.Core.DiscoveryEvent discovery, Laboratory.Chimera.Discovery.Core.DiscoveryType.LegendaryLineage, Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Mythical, entity, genetics);
                discoveries.Add(discovery);
            }

            return discoveries;
        }


        private void CreateDiscovery(out Laboratory.Chimera.Discovery.Core.DiscoveryEvent discovery, Laboratory.Chimera.Discovery.Core.DiscoveryType type, Laboratory.Chimera.Discovery.Core.DiscoveryRarity rarity, Entity entity, in VisualGeneticData genetics)
        {
            bool isFirstTime = !DiscoveredTraits.ContainsKey(CalculateTraitHash(genetics));
            bool isWorldFirst = isFirstTime; // Simplified - would check global database

            discovery = new Laboratory.Chimera.Discovery.Core.DiscoveryEvent
            {
                Type = type,
                Rarity = rarity,
                SignificanceScore = CalculateSignificanceBurst(type, rarity, isFirstTime, isWorldFirst),
                DiscoveredCreature = entity,
                DiscoveredGenetics = genetics,
                SpecialMarkers = genetics.SpecialMarkers,
                CelebrationIntensity = CalculateCelebrationIntensity(rarity, isWorldFirst),
                IsFirstTimeDiscovery = isFirstTime,
                IsWorldFirst = isWorldFirst,
                DiscoveryName = GenerateDiscoveryNameBurstWrapper(type, genetics, genetics.SpecialMarkers)
            };
        }


        private static bool IsPerfectGenetics(in VisualGeneticData genetics)
        {
            return genetics.Strength >= 95 && genetics.Vitality >= 95 && genetics.Agility >= 95 &&
                   genetics.Intelligence >= 95 && genetics.Adaptability >= 95 && genetics.Social >= 95;
        }


        private static bool HasRareMutation(in VisualGeneticData genetics)
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


        private static bool HasLegendaryPotential(in VisualGeneticData genetics)
        {
            int totalStats = genetics.Strength + genetics.Vitality + genetics.Agility +
                           genetics.Intelligence + genetics.Adaptability + genetics.Social;
            return totalStats >= 500 && genetics.SpecialMarkers != GeneticMarkerFlags.None;
        }


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


        private static DiscoveryRarity CalculateTraitRarity(in VisualGeneticData genetics)
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


        public static uint CalculateTraitHash(in VisualGeneticData genetics)
        {
            uint hash = 0;
            hash = math.hash(new uint4(genetics.Strength, genetics.Vitality, genetics.Agility, genetics.Intelligence));
            hash ^= math.hash(new uint2(genetics.Adaptability, genetics.Social));
            hash ^= (uint)genetics.SpecialMarkers;
            return hash;
        }


        private static float CalculateSignificanceBurst(Laboratory.Chimera.Discovery.Core.DiscoveryType type, Laboratory.Chimera.Discovery.Core.DiscoveryRarity rarity, bool isFirstTime, bool isWorldFirst)
        {
            float baseScore = rarity switch
            {
                Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Common => 10f,
                Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Uncommon => 25f,
                Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Rare => 50f,
                Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Epic => 100f,
                Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Legendary => 250f,
                Laboratory.Chimera.Discovery.Core.DiscoveryRarity.Mythical => 500f,
                _ => 1f
            };

            float typeMultiplier = type switch
            {
                Laboratory.Chimera.Discovery.Core.DiscoveryType.NewTrait => 1.0f,
                Laboratory.Chimera.Discovery.Core.DiscoveryType.RareMutation => 1.5f,
                Laboratory.Chimera.Discovery.Core.DiscoveryType.SpecialMarker => 2.0f,
                Laboratory.Chimera.Discovery.Core.DiscoveryType.PerfectGenetics => 3.0f,
                Laboratory.Chimera.Discovery.Core.DiscoveryType.NewSpecies => 5.0f,
                Laboratory.Chimera.Discovery.Core.DiscoveryType.LegendaryLineage => 10.0f,
                _ => 1.0f
            };

            float contextBonus = 1.0f;
            if (isFirstTime) contextBonus += 0.5f;
            if (isWorldFirst) contextBonus += 2.0f;

            return baseScore * typeMultiplier * contextBonus;
        }

        private static FixedString64Bytes GenerateDiscoveryNameBurstWrapper(Laboratory.Chimera.Discovery.Core.DiscoveryType type, in VisualGeneticData genetics, GeneticMarkerFlags markers)
        {
            GenerateDiscoveryNameBurst(type, genetics, markers, out FixedString64Bytes result);
            return result;
        }

        private static void GenerateDiscoveryNameBurst(Laboratory.Chimera.Discovery.Core.DiscoveryType type, in VisualGeneticData genetics, GeneticMarkerFlags markers, out FixedString64Bytes result)
        {
            // Get prefix based on type
            FixedString32Bytes prefix = type switch
            {
                Laboratory.Chimera.Discovery.Core.DiscoveryType.NewTrait => DiscoveryStrings.Enhanced,
                Laboratory.Chimera.Discovery.Core.DiscoveryType.RareMutation => DiscoveryStrings.Mutant,
                Laboratory.Chimera.Discovery.Core.DiscoveryType.SpecialMarker => DiscoveryStrings.Marked,
                Laboratory.Chimera.Discovery.Core.DiscoveryType.PerfectGenetics => DiscoveryStrings.Perfect,
                Laboratory.Chimera.Discovery.Core.DiscoveryType.NewSpecies => DiscoveryStrings.Hybrid,
                Laboratory.Chimera.Discovery.Core.DiscoveryType.LegendaryLineage => DiscoveryStrings.Legendary,
                _ => DiscoveryStrings.Unknown
            };

            // Get descriptor based on highest trait
            GetGeneticDescriptorBurst(genetics, out FixedString32Bytes descriptor);

            // Get marker suffix
            GetMarkerSuffixBurst(markers, out FixedString32Bytes markerSuffix);

            // Combine parts using Unicode for space character
            result = new FixedString64Bytes();
            result.Append(prefix);
            result.Append((byte)' ');
            result.Append(descriptor);
            result.Append(markerSuffix);
        }

        private static void GetGeneticDescriptorBurst(in VisualGeneticData genetics, out FixedString32Bytes result)
        {
            byte maxTrait = (byte)math.max((int)genetics.Strength, math.max((int)genetics.Vitality,
                           math.max((int)genetics.Agility, math.max((int)genetics.Intelligence,
                           math.max((int)genetics.Adaptability, (int)genetics.Social)))));

            if (genetics.Strength == maxTrait) { result = DiscoveryStrings.Titan; return; }
            if (genetics.Vitality == maxTrait) { result = DiscoveryStrings.Eternal; return; }
            if (genetics.Agility == maxTrait) { result = DiscoveryStrings.Swift; return; }
            if (genetics.Intelligence == maxTrait) { result = DiscoveryStrings.Genius; return; }
            if (genetics.Adaptability == maxTrait) { result = DiscoveryStrings.Evolved; return; }
            if (genetics.Social == maxTrait) { result = DiscoveryStrings.Alpha; return; }

            result = DiscoveryStrings.Balanced;
        }

        private static void GetMarkerSuffixBurst(GeneticMarkerFlags markers, out FixedString32Bytes result)
        {
            if ((markers & GeneticMarkerFlags.Bioluminescent) != 0) { result = DiscoveryStrings.Lumina; return; }
            if ((markers & GeneticMarkerFlags.ElementalAffinity) != 0) { result = DiscoveryStrings.Elemental; return; }
            if ((markers & GeneticMarkerFlags.RareLineage) != 0) { result = DiscoveryStrings.Prime; return; }
            if ((markers & GeneticMarkerFlags.HybridVigor) != 0) { result = DiscoveryStrings.HybridSuffix; return; }
            if ((markers & GeneticMarkerFlags.PackLeader) != 0) { result = DiscoveryStrings.Rex; return; }

            result = new FixedString32Bytes();
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