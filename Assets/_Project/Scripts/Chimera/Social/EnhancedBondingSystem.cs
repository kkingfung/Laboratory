using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Enhanced creature bonding system with generational memory and emotional inheritance.
    /// Creates deep emotional connections that persist across creature generations.
    /// </summary>
    public partial class EnhancedBondingSystem : SystemBase
    {
        private EntityQuery _creatureQuery;
        private EntityQuery _bondingQuery;
        private EntityQuery _familyTreeQuery;

        private EnhancedBondingConfig _config;

        public static event Action<Entity, Entity, BondType, float> OnBondFormed;
        public static event Action<Entity, BondingMilestone> OnBondingMilestone;
        public static event Action<Entity, GenerationalMemory> OnMemoryActivated;
        public static event Action<Entity, Entity, float> OnBondStrengthChanged;

        protected override void OnCreate()
        {
            _config = Resources.Load<EnhancedBondingConfig>("Configs/EnhancedBondingConfig");
            if (_config == null)
            {
                UnityEngine.Debug.LogError("EnhancedBondingConfig not found in Resources/Configs/");
                return;
            }

            _creatureQuery = GetEntityQuery(
                ComponentType.ReadOnly<CreatureProfile>(),
                ComponentType.ReadWrite<BondingComponent>(),
                ComponentType.ReadOnly<Transform>()
            );

            _bondingQuery = GetEntityQuery(
                ComponentType.ReadWrite<ActiveBond>(),
                ComponentType.ReadOnly<BondingComponent>()
            );

            _familyTreeQuery = GetEntityQuery(
                ComponentType.ReadOnly<FamilyLineage>(),
                ComponentType.ReadWrite<GenerationalMemoryComponent>()
            );
        }

        protected override void OnUpdate()
        {
            if (_config == null) return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            ProcessActiveBonds(deltaTime);
            UpdateGenerationalMemories(deltaTime);
            CheckBondingOpportunities();
            ProcessEmotionalInheritance();
            UpdateBondingMilestones();
        }

        private void ProcessActiveBonds(float deltaTime)
        {
            var ecb = new Unity.Entities.EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (bond, bonding, entity) in SystemAPI.Query<RefRW<ActiveBond>, RefRO<BondingComponent>>().WithEntityAccess())
            {
                if (!EntityManager.Exists(bond.ValueRO.TargetEntity))
                {
                    ecb.RemoveComponent<ActiveBond>(entity);
                    continue;
                }

                bond.ValueRW.TimeElapsed += deltaTime;
                float previousStrength = bond.ValueRO.Strength;

                // Calculate distance factor
                var sourcePos = EntityManager.GetComponentData<LocalTransform>(entity).Position;
                var targetPos = EntityManager.GetComponentData<LocalTransform>(bond.ValueRO.TargetEntity).Position;
                float distance = math.distance(sourcePos, targetPos);

                // Update bond strength based on proximity and time
                if (distance <= _config.ProximityBondingRange)
                {
                    float strengthGain = _config.BaseBondGrowthRate * deltaTime;

                    // Apply bond type modifiers
                    switch (bond.ValueRO.BondType)
                    {
                        case BondType.Parent:
                            strengthGain *= _config.ParentBondMultiplier;
                            break;
                        case BondType.Mate:
                            strengthGain *= _config.MateBondMultiplier;
                            break;
                        case BondType.Offspring:
                            strengthGain *= _config.OffspringBondMultiplier;
                            break;
                        case BondType.Companion:
                            strengthGain *= _config.CompanionBondMultiplier;
                            break;
                    }

                    bond.ValueRW.Strength = math.min(1f, bond.ValueRO.Strength + strengthGain);
                }
                else if (distance > _config.SeparationDistanceThreshold)
                {
                    // Bond weakens with distance
                    float decay = _config.SeparationDecayRate * deltaTime;
                    bond.ValueRW.Strength = math.max(0f, bond.ValueRO.Strength - decay);
                }

                // Process generational memory influence
                if (EntityManager.HasComponent<GenerationalMemoryComponent>(entity))
                {
                    var memory = EntityManager.GetComponentData<GenerationalMemoryComponent>(entity);
                    var bondValue = bond.ValueRW;
                    ApplyGenerationalMemoryToBond(ref bondValue, memory);
                    bond.ValueRW = bondValue;
                }

                // Check for bond strength changes
                if (math.abs(bond.ValueRO.Strength - previousStrength) > 0.01f)
                {
                    OnBondStrengthChanged?.Invoke(entity, bond.ValueRO.TargetEntity, bond.ValueRO.Strength);
                }

                // Remove bond if it becomes too weak
                if (bond.ValueRO.Strength <= _config.MinimumBondStrength)
                {
                    ecb.RemoveComponent<ActiveBond>(entity);
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void UpdateGenerationalMemories(float deltaTime)
        {
            foreach (var (memory, entity) in SystemAPI.Query<RefRW<GenerationalMemoryComponent>>().WithEntityAccess())
            {
                // Decay memory strength over time
                for (int i = memory.ValueRO.Memories.Length - 1; i >= 0; i--)
                {
                    var mem = memory.ValueRO.Memories[i];
                    mem.Strength = math.max(0f, mem.Strength - _config.MemoryDecayRate * deltaTime);

                    if (mem.Strength <= 0f)
                    {
                        // Remove weak memories
                        var memoryValue = memory.ValueRW;
                        RemoveMemoryAtIndex(ref memoryValue, i);
                        memory.ValueRW = memoryValue;
                    }
                    else
                    {
                        // Update the memory in the FixedList
                        var memories = memory.ValueRW.Memories;
                        memories[i] = mem;
                        memory.ValueRW.Memories = memories;
                    }
                }

                // Update emotional resonance
                memory.ValueRW.EmotionalResonance = CalculateEmotionalResonance(memory.ValueRO);
            }
        }

        private void CheckBondingOpportunities()
        {
            var creatures = _creatureQuery.ToEntityArray(Allocator.TempJob);
            var positions = _creatureQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            var profiles = _creatureQuery.ToComponentDataArray<CreatureProfile>(Allocator.TempJob);

            for (int i = 0; i < creatures.Length; i++)
            {
                for (int j = i + 1; j < creatures.Length; j++)
                {
                    float distance = math.distance(positions[i].Position, positions[j].Position);

                    if (distance <= _config.BondingDetectionRange)
                    {
                        TryFormBond(creatures[i], creatures[j], profiles[i], profiles[j]);
                    }
                }
            }

            creatures.Dispose();
            positions.Dispose();
            profiles.Dispose();
        }

        private void TryFormBond(Entity creature1, Entity creature2, CreatureProfile profile1, CreatureProfile profile2)
        {
            // Check if bond already exists
            if (HasExistingBond(creature1, creature2)) return;

            // Determine bond type and compatibility
            var bondType = DetermineBondType(profile1, profile2);
            float compatibility = CalculateBondCompatibility(profile1, profile2, bondType);

            if (compatibility >= _config.MinimumBondCompatibility)
            {
                // Create bonds for both creatures
                CreateBond(creature1, creature2, bondType, compatibility);
                CreateBond(creature2, creature1, GetReciprocalBondType(bondType), compatibility);

                OnBondFormed?.Invoke(creature1, creature2, bondType, compatibility);
            }
        }

        private void CreateBond(Entity source, Entity target, BondType bondType, float initialStrength)
        {
            var bond = new ActiveBond
            {
                TargetEntity = target,
                BondType = bondType,
                Strength = initialStrength * _config.InitialBondStrength,
                TimeElapsed = 0f,
                LastInteraction = (float)SystemAPI.Time.ElapsedTime,
                MemoryContribution = CalculateMemoryContribution(bondType)
            };

            EntityManager.AddComponentData(source, bond);

            // Create generational memory entry
            if (EntityManager.HasComponent<GenerationalMemoryComponent>(source))
            {
                var memory = EntityManager.GetComponentData<GenerationalMemoryComponent>(source);
                AddBondMemory(ref memory, target, bondType, initialStrength);
                EntityManager.SetComponentData(source, memory);
            }
            else
            {
                CreateInitialGenerationalMemory(source, target, bondType, initialStrength);
            }
        }

        private void ProcessEmotionalInheritance()
        {
            foreach (var (memory, lineage, entity) in SystemAPI.Query<RefRW<GenerationalMemoryComponent>, RefRO<FamilyLineage>>().WithEntityAccess())
            {
                // Inherit memories from parents
                if (EntityManager.Exists(lineage.ValueRO.Parent1) && EntityManager.HasComponent<GenerationalMemoryComponent>(lineage.ValueRO.Parent1))
                {
                    var parentMemory = EntityManager.GetComponentData<GenerationalMemoryComponent>(lineage.ValueRO.Parent1);
                    var memoryValue = memory.ValueRW;
                    InheritParentMemories(ref memoryValue, parentMemory, _config.ParentMemoryInheritanceRate);
                    memory.ValueRW = memoryValue;
                }

                if (EntityManager.Exists(lineage.ValueRO.Parent2) && EntityManager.HasComponent<GenerationalMemoryComponent>(lineage.ValueRO.Parent2))
                {
                    var parentMemory = EntityManager.GetComponentData<GenerationalMemoryComponent>(lineage.ValueRO.Parent2);
                    var memoryValue = memory.ValueRW;
                    InheritParentMemories(ref memoryValue, parentMemory, _config.ParentMemoryInheritanceRate);
                    memory.ValueRW = memoryValue;
                }
            }
        }

        private void UpdateBondingMilestones()
        {
            foreach (var (bond, bonding, entity) in SystemAPI.Query<RefRW<ActiveBond>, RefRW<BondingComponent>>().WithEntityAccess())
            {
                // Check for milestone achievements
                if (bond.ValueRO.Strength >= 0.9f && !bonding.ValueRO.HasDeepBond)
                {
                    bonding.ValueRW.HasDeepBond = true;
                    OnBondingMilestone?.Invoke(entity, BondingMilestone.DeepBond);
                }

                if (bond.ValueRO.TimeElapsed >= _config.LifelongBondTimeThreshold && !bonding.ValueRO.HasLifelongBond)
                {
                    bonding.ValueRW.HasLifelongBond = true;
                    OnBondingMilestone?.Invoke(entity, BondingMilestone.LifelongBond);
                }

                if (bond.ValueRO.BondType == BondType.Mate && bond.ValueRO.Strength >= 0.8f && !bonding.ValueRO.HasSoulmate)
                {
                    bonding.ValueRW.HasSoulmate = true;
                    OnBondingMilestone?.Invoke(entity, BondingMilestone.Soulmate);
                }
            }
        }

        // Helper methods
        private void ApplyGenerationalMemoryToBond(ref ActiveBond bond, GenerationalMemoryComponent memory)
        {
            // Find relevant memories that affect this bond
            for (int i = 0; i < memory.Memories.Length; i++)
            {
                var mem = memory.Memories[i];
                if (mem.TargetEntity.Equals(bond.TargetEntity) || IsSimilarBondType(mem.BondType, bond.BondType))
                {
                    float memoryInfluence = mem.Strength * _config.MemoryInfluenceStrength;
                    bond.Strength += memoryInfluence * SystemAPI.Time.DeltaTime;
                }
            }
        }

        private float CalculateEmotionalResonance(GenerationalMemoryComponent memory)
        {
            float totalResonance = 0f;
            int memoryCount = 0;

            for (int i = 0; i < memory.Memories.Length; i++)
            {
                var mem = memory.Memories[i];
                totalResonance += mem.Strength * mem.EmotionalWeight;
                memoryCount++;
            }

            return memoryCount > 0 ? totalResonance / memoryCount : 0f;
        }

        private void AddBondMemory(ref GenerationalMemoryComponent memory, Entity target, BondType bondType, float strength)
        {
            var newMemory = new GenerationalMemory
            {
                TargetEntity = target,
                BondType = bondType,
                Strength = strength,
                EmotionalWeight = CalculateEmotionalWeight(bondType),
                CreationTime = (float)SystemAPI.Time.ElapsedTime,
                GenerationsRemaining = _config.MaxGenerationalDepth
            };

            var memories = memory.Memories;

            // Add to memory FixedList (implement circular buffer if needed)
            if (memories.Length < memories.Capacity)
            {
                memories.Add(newMemory);
                memory.Memories = memories;
            }
            else
            {
                // Replace oldest memory
                int oldestIndex = FindOldestMemoryIndex(memory);
                if (oldestIndex >= 0)
                {
                    memories[oldestIndex] = newMemory;
                    memory.Memories = memories;
                }
            }
        }

        private void CreateInitialGenerationalMemory(Entity entity, Entity target, BondType bondType, float strength)
        {
            var memory = new GenerationalMemoryComponent
            {
                Memories = new FixedList64Bytes<GenerationalMemory>(),
                EmotionalResonance = strength,
                LastMemoryUpdate = (float)SystemAPI.Time.ElapsedTime
            };

            // Add the initial memory
            var memories = memory.Memories;
            memories.Add(new GenerationalMemory
            {
                TargetEntity = target,
                BondType = bondType,
                Strength = strength,
                EmotionalWeight = CalculateEmotionalWeight(bondType),
                CreationTime = (float)SystemAPI.Time.ElapsedTime,
                GenerationsRemaining = _config.MaxGenerationalDepth
            });
            memory.Memories = memories;

            EntityManager.AddComponentData(entity, memory);
        }

        private void InheritParentMemories(ref GenerationalMemoryComponent childMemory, GenerationalMemoryComponent parentMemory, float inheritanceRate)
        {
            for (int i = 0; i < parentMemory.Memories.Length; i++)
            {
                var parentMem = parentMemory.Memories[i];
                if (parentMem.GenerationsRemaining > 0)
                {
                    var inheritedMemory = new GenerationalMemory
                    {
                        TargetEntity = parentMem.TargetEntity,
                        BondType = parentMem.BondType,
                        Strength = parentMem.Strength * inheritanceRate,
                        EmotionalWeight = parentMem.EmotionalWeight,
                        CreationTime = (float)SystemAPI.Time.ElapsedTime,
                        GenerationsRemaining = parentMem.GenerationsRemaining - 1
                    };

                    if (inheritedMemory.Strength >= _config.MinimumInheritedMemoryStrength)
                    {
                        AddBondMemory(ref childMemory, inheritedMemory.TargetEntity, inheritedMemory.BondType, inheritedMemory.Strength);
                        OnMemoryActivated?.Invoke(EntityManager.CreateEntity(), inheritedMemory);
                    }
                }
            }
        }

        private bool HasExistingBond(Entity creature1, Entity creature2)
        {
            if (!EntityManager.HasComponent<ActiveBond>(creature1)) return false;

            var bond = EntityManager.GetComponentData<ActiveBond>(creature1);
            return bond.TargetEntity.Equals(creature2);
        }

        private BondType DetermineBondType(CreatureProfile profile1, CreatureProfile profile2)
        {
            // Check family relationships first
            if (IsParentChild(profile1, profile2)) return BondType.Parent;
            if (IsOffspring(profile1, profile2)) return BondType.Offspring;
            if (AreMateCompatible(profile1, profile2)) return BondType.Mate;

            return BondType.Companion;
        }

        private float CalculateBondCompatibility(CreatureProfile profile1, CreatureProfile profile2, BondType bondType)
        {
            float compatibility = 0.5f; // Base compatibility

            // Species compatibility
            if (profile1.Species == profile2.Species)
                compatibility += 0.3f;
            else if (AreCompatibleSpecies(profile1.Species, profile2.Species))
                compatibility += 0.1f;

            // Personality compatibility
            compatibility += CalculatePersonalityCompatibility(profile1, profile2);

            // Bond type specific modifiers
            switch (bondType)
            {
                case BondType.Mate:
                    compatibility += CalculateMateCompatibility(profile1, profile2);
                    break;
                case BondType.Parent:
                case BondType.Offspring:
                    compatibility += 0.4f; // Family bonds are naturally strong
                    break;
            }

            return math.clamp(compatibility, 0f, 1f);
        }

        private float CalculatePersonalityCompatibility(CreatureProfile profile1, CreatureProfile profile2)
        {
            // Implement personality trait comparison
            // This would need to be expanded based on your personality system
            return UnityEngine.Random.Range(-0.2f, 0.3f);
        }

        private float CalculateMateCompatibility(CreatureProfile profile1, CreatureProfile profile2)
        {
            // Check genetic diversity, age compatibility, etc.
            float geneticDiversity = CalculateGeneticDiversity(profile1, profile2);
            float ageCompatibility = CalculateAgeCompatibility(profile1, profile2);

            return (geneticDiversity + ageCompatibility) * 0.5f;
        }

        private float CalculateGeneticDiversity(CreatureProfile profile1, CreatureProfile profile2)
        {
            // This would integrate with your genetic system
            return UnityEngine.Random.Range(0.3f, 0.8f);
        }

        private float CalculateAgeCompatibility(CreatureProfile profile1, CreatureProfile profile2)
        {
            float ageDifference = math.abs(profile1.Age - profile2.Age);
            float maxCompatibleAge = _config.MaxMateAgeDifference;

            return math.max(0f, 1f - (ageDifference / maxCompatibleAge));
        }

        private float CalculateMemoryContribution(BondType bondType)
        {
            switch (bondType)
            {
                case BondType.Parent: return 0.9f;
                case BondType.Mate: return 0.8f;
                case BondType.Offspring: return 0.7f;
                case BondType.Companion: return 0.5f;
                default: return 0.3f;
            }
        }

        private float CalculateEmotionalWeight(BondType bondType)
        {
            switch (bondType)
            {
                case BondType.Parent: return 1.0f;
                case BondType.Mate: return 0.9f;
                case BondType.Offspring: return 0.8f;
                case BondType.Companion: return 0.6f;
                default: return 0.4f;
            }
        }

        private BondType GetReciprocalBondType(BondType bondType)
        {
            switch (bondType)
            {
                case BondType.Parent: return BondType.Offspring;
                case BondType.Offspring: return BondType.Parent;
                default: return bondType;
            }
        }

        private bool IsSimilarBondType(BondType type1, BondType type2)
        {
            if (type1 == type2) return true;

            // Family bonds are similar
            if ((type1 == BondType.Parent || type1 == BondType.Offspring) &&
                (type2 == BondType.Parent || type2 == BondType.Offspring))
                return true;

            return false;
        }

        private void RemoveMemoryAtIndex(ref GenerationalMemoryComponent memory, int index)
        {
            if (index < 0 || index >= memory.Memories.Length) return;

            var memories = memory.Memories;
            memories.RemoveAtSwapBack(index);
            memory.Memories = memories;
        }

        private int FindOldestMemoryIndex(GenerationalMemoryComponent memory)
        {
            if (memory.Memories.Length == 0) return -1;

            int oldestIndex = 0;
            float oldestTime = memory.Memories[0].CreationTime;

            for (int i = 1; i < memory.Memories.Length; i++)
            {
                if (memory.Memories[i].CreationTime < oldestTime)
                {
                    oldestTime = memory.Memories[i].CreationTime;
                    oldestIndex = i;
                }
            }

            return oldestIndex;
        }

        // Placeholder methods that would integrate with other systems
        private bool IsParentChild(CreatureProfile profile1, CreatureProfile profile2) => false;
        private bool IsOffspring(CreatureProfile profile1, CreatureProfile profile2) => false;
        private bool AreMateCompatible(CreatureProfile profile1, CreatureProfile profile2) => true;
        private bool AreCompatibleSpecies(FixedString64Bytes species1, FixedString64Bytes species2) => species1.Equals(species2);
    }

    #region Component Data Structures

    [Serializable]
    public struct BondingComponent : IComponentData
    {
        public float SocialNeed;
        public float BondingCapacity;
        [MarshalAs(UnmanagedType.U1)]
        public bool HasDeepBond;
        [MarshalAs(UnmanagedType.U1)]
        public bool HasLifelongBond;
        [MarshalAs(UnmanagedType.U1)]
        public bool HasSoulmate;
        public double LastSocialInteraction;
    }

    [Serializable]
    public struct ActiveBond : IComponentData
    {
        public Entity TargetEntity;
        public BondType BondType;
        public float Strength;
        public float TimeElapsed;
        public double LastInteraction;
        public float MemoryContribution;
    }

    [Serializable]
    public struct GenerationalMemoryComponent : IComponentData
    {
        public FixedList64Bytes<GenerationalMemory> Memories;
        public float EmotionalResonance;
        public float LastMemoryUpdate;
    }

    [Serializable]
    public struct GenerationalMemory
    {
        public Entity TargetEntity;
        public BondType BondType;
        public float Strength;
        public float EmotionalWeight;
        public float CreationTime;
        public int GenerationsRemaining;
    }

    [Serializable]
    public struct FamilyLineage : IComponentData
    {
        public Entity Parent1;
        public Entity Parent2;
        public FixedList64Bytes<Entity> Offspring;
        public int Generation;
    }

    public enum BondType
    {
        Companion,
        Mate,
        Parent,
        Offspring,
        Rival,
        Mentor,
        Student
    }

    public enum BondingMilestone
    {
        FirstBond,
        DeepBond,
        LifelongBond,
        Soulmate,
        FamilyBond,
        LegacyBond
    }

    /// <summary>
    /// Creature profile for bonding calculations
    /// </summary>
    public struct CreatureProfile : IComponentData
    {
        public uint creatureId;
        public FixedString64Bytes name;
        public float socialability;
        public float trustLevel;
        public float emotionalIntelligence;
        public Vector3 position;
        public int age;
        public FixedString64Bytes species;

        // Properties for compatibility
        public int Age => age;
        public FixedString64Bytes Species => species;
    }

    #endregion
}