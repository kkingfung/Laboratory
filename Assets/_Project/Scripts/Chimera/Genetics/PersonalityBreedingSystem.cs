using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Consciousness.Core;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// PERSONALITY BREEDING SYSTEM
    ///
    /// Handles personality inheritance during breeding
    ///
    /// NEW VISION: Personality is genetic and inheritable
    ///
    /// Responsibilities:
    /// - Initialize personality genetics from CreaturePersonality
    /// - Calculate personality compatibility for breeding
    /// - Blend parent personalities for offspring
    /// - Apply personality mutations during breeding
    /// - Track inheritance records for each trait
    /// - Set genetic baseline for elderly personality system
    ///
    /// Inheritance Model:
    /// - Each trait averaged from both parents
    /// - ±15 random variation for uniqueness
    /// - 5% mutation chance per trait (can shift by ±30)
    /// - Compatible personalities improve breeding success
    ///
    /// Integration:
    /// - PersonalityStabilitySystem: Uses genetic baseline for elderly
    /// - BreedingEngine: Provides personality compatibility score
    /// - CreaturePersonality: Syncs with genetic personality
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PersonalityBreedingSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            Debug.Log("Personality Breeding System initialized - personality is now genetic!");
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Initialize personality genetics for creatures without it
            InitializePersonalityGenetics();

            // Calculate personality compatibility for breeding pairs
            CalculatePersonalityCompatibility(currentTime);

            // Process personality breeding requests
            ProcessPersonalityBreedingRequests(currentTime);

            // Sync genetic personality with current personality
            SyncGeneticPersonality();
        }

        /// <summary>
        /// Initialize personality genetics for creatures that have personality but no genetics
        /// </summary>
        private void InitializePersonalityGenetics()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (personality, entity) in
                SystemAPI.Query<RefRO<CreaturePersonality>>()
                .WithEntityAccess()
                .WithNone<PersonalityGeneticComponent>())
            {
                // Convert current personality to genetic baseline
                var geneticPersonality = new PersonalityGeneticComponent
                {
                    geneticCuriosity = personality.ValueRO.Curiosity,
                    geneticPlayfulness = personality.ValueRO.Playfulness,
                    geneticAggression = personality.ValueRO.Aggression,
                    geneticAffection = personality.ValueRO.Affection,
                    geneticIndependence = personality.ValueRO.Independence,
                    geneticNervousness = personality.ValueRO.Nervousness,
                    geneticStubbornness = personality.ValueRO.Stubbornness,
                    geneticLoyalty = personality.ValueRO.Loyalty,
                    parent1Influence = 500, // 50/50 (no parents)
                    parent2Influence = 500,
                    mutationCount = 0,
                    hasPersonalityMutation = false,
                    personalityFitness = 0.7f,
                    temperamentStability = 0.8f
                };

                // Calculate fitness
                geneticPersonality.personalityFitness = PersonalityGeneticsHelper.CalculatePersonalityFitness(geneticPersonality);

                ecb.AddComponent(entity, geneticPersonality);

                // Add inheritance tracking buffer
                ecb.AddBuffer<PersonalityInheritanceRecord>(entity);
            }
        }

        /// <summary>
        /// Calculate personality compatibility between breeding pairs
        /// </summary>
        private void CalculatePersonalityCompatibility(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (compatRequest, entity) in
                SystemAPI.Query<RefRO<PersonalityCompatibilityData>>().WithEntityAccess())
            {
                var parent1Entity = compatRequest.ValueRO.parent1Entity;
                var parent2Entity = compatRequest.ValueRO.parent2Entity;

                if (!EntityManager.Exists(parent1Entity) || !EntityManager.Exists(parent2Entity))
                {
                    ecb.RemoveComponent<PersonalityCompatibilityData>(entity);
                    continue;
                }

                if (!EntityManager.HasComponent<PersonalityGeneticComponent>(parent1Entity) ||
                    !EntityManager.HasComponent<PersonalityGeneticComponent>(parent2Entity))
                {
                    continue;
                }

                var parent1 = EntityManager.GetComponentData<PersonalityGeneticComponent>(parent1Entity);
                var parent2 = EntityManager.GetComponentData<PersonalityGeneticComponent>(parent2Entity);

                // Calculate compatibility for each trait
                float curiosityCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                    parent1.geneticCuriosity, parent2.geneticCuriosity);
                float playfulnessCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                    parent1.geneticPlayfulness, parent2.geneticPlayfulness);
                float aggressionCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                    parent1.geneticAggression, parent2.geneticAggression);
                float affectionCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                    parent1.geneticAffection, parent2.geneticAffection);
                float independenceCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                    parent1.geneticIndependence, parent2.geneticIndependence);
                float nervousnessCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                    parent1.geneticNervousness, parent2.geneticNervousness);
                float stubbornnessCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                    parent1.geneticStubbornness, parent2.geneticStubbornness);
                float loyaltyCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                    parent1.geneticLoyalty, parent2.geneticLoyalty);

                // Overall compatibility (average of all traits)
                float overallCompat = (curiosityCompat + playfulnessCompat + aggressionCompat +
                                      affectionCompat + independenceCompat + nervousnessCompat +
                                      stubbornnessCompat + loyaltyCompat) / 8f;

                // Bonus for diversity (different personalities)
                float diversityBonus = CalculateDiversityBonus(parent1, parent2);

                // Penalty for extreme combinations
                float extremesPenalty = CalculateExtremesPenalty(parent1, parent2);

                // Final compatibility
                float finalCompat = overallCompat + diversityBonus - extremesPenalty;
                finalCompat = math.clamp(finalCompat, 0f, 1f);

                // Update compatibility data
                var updatedCompat = compatRequest.ValueRO;
                updatedCompat.overallCompatibility = finalCompat;
                updatedCompat.curiosityCompatibility = curiosityCompat;
                updatedCompat.playfulnessCompatibility = playfulnessCompat;
                updatedCompat.aggressionCompatibility = aggressionCompat;
                updatedCompat.affectionCompatibility = affectionCompat;
                updatedCompat.independenceCompatibility = independenceCompat;
                updatedCompat.nervousnessCompatibility = nervousnessCompat;
                updatedCompat.stubbornnessCompatibility = stubbornnessCompat;
                updatedCompat.loyaltyCompatibility = loyaltyCompat;
                updatedCompat.diversityBonus = diversityBonus;
                updatedCompat.extremesPenalty = extremesPenalty;
                updatedCompat.calculationTime = currentTime;
                updatedCompat.isViableMatch = finalCompat >= PersonalityGeneticsHelper.MIN_BREEDING_COMPATIBILITY;

                EntityManager.SetComponentData(entity, updatedCompat);
            }
        }

        /// <summary>
        /// Process breeding requests with personality inheritance
        /// </summary>
        private void ProcessPersonalityBreedingRequests(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (breedRequest, entity) in
                SystemAPI.Query<RefRO<PersonalityBreedingRequest>>().WithEntityAccess())
            {
                var parent1Entity = breedRequest.ValueRO.parent1Entity;
                var parent2Entity = breedRequest.ValueRO.parent2Entity;

                if (!EntityManager.Exists(parent1Entity) || !EntityManager.Exists(parent2Entity))
                {
                    Debug.LogWarning("Cannot breed: Parent entities don't exist");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                if (!EntityManager.HasComponent<PersonalityGeneticComponent>(parent1Entity) ||
                    !EntityManager.HasComponent<PersonalityGeneticComponent>(parent2Entity))
                {
                    Debug.LogWarning("Cannot breed: Parents missing personality genetics");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var parent1 = EntityManager.GetComponentData<PersonalityGeneticComponent>(parent1Entity);
                var parent2 = EntityManager.GetComponentData<PersonalityGeneticComponent>(parent2Entity);

                // Calculate compatibility
                float compatibility = CalculateQuickCompatibility(parent1, parent2);

                // Check minimum compatibility
                if (compatibility < PersonalityGeneticsHelper.MIN_BREEDING_COMPATIBILITY)
                {
                    Debug.LogWarning($"Breeding failed: Personality compatibility too low ({compatibility:F2})");
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Create offspring entity
                var offspringEntity = EntityManager.CreateEntity();

                // Generate offspring personality
                var offspringGenetics = CreateOffspringPersonality(
                    parent1, parent2,
                    breedRequest.ValueRO.allowPersonalityMutations,
                    breedRequest.ValueRO.mutationRate,
                    (uint)System.DateTime.UtcNow.Ticks,
                    out var inheritanceRecords);

                // Add offspring components
                ecb.AddComponent(offspringEntity, offspringGenetics);
                ecb.AddBuffer<PersonalityInheritanceRecord>(offspringEntity);

                // Add inheritance records
                var inheritanceBuffer = EntityManager.GetBuffer<PersonalityInheritanceRecord>(offspringEntity);
                foreach (var record in inheritanceRecords)
                {
                    inheritanceBuffer.Add(record);
                }

                // Create CreaturePersonality from genetics
                var offspringPersonality = new CreaturePersonality
                {
                    Curiosity = offspringGenetics.geneticCuriosity,
                    Playfulness = offspringGenetics.geneticPlayfulness,
                    Aggression = offspringGenetics.geneticAggression,
                    Affection = offspringGenetics.geneticAffection,
                    Independence = offspringGenetics.geneticIndependence,
                    Nervousness = offspringGenetics.geneticNervousness,
                    Stubbornness = offspringGenetics.geneticStubbornness,
                    Loyalty = offspringGenetics.geneticLoyalty,
                    PersonalitySeed = (uint)System.DateTime.UtcNow.Ticks,
                    LearningRate = 0.5f,
                    MemoryStrength = 50,
                    StressLevel = 0.2f,
                    HappinessLevel = 0.7f,
                    EnergyLevel = 0.8f
                };

                ecb.AddComponent(offspringEntity, offspringPersonality);

                // Create breeding result
                var resultEntity = EntityManager.CreateEntity();
                ecb.AddComponent(resultEntity, new PersonalityBreedingResult
                {
                    offspringEntity = offspringEntity,
                    parent1Entity = parent1Entity,
                    parent2Entity = parent2Entity,
                    parent1Contribution = offspringGenetics.parent1Influence,
                    parent2Contribution = offspringGenetics.parent2Influence,
                    offspringCuriosity = offspringGenetics.geneticCuriosity,
                    offspringPlayfulness = offspringGenetics.geneticPlayfulness,
                    offspringAggression = offspringGenetics.geneticAggression,
                    offspringAffection = offspringGenetics.geneticAffection,
                    offspringIndependence = offspringGenetics.geneticIndependence,
                    offspringNervousness = offspringGenetics.geneticNervousness,
                    offspringStubbornness = offspringGenetics.geneticStubbornness,
                    offspringLoyalty = offspringGenetics.geneticLoyalty,
                    mutationCount = offspringGenetics.mutationCount,
                    hadSignificantMutation = offspringGenetics.hasPersonalityMutation,
                    personalityBalance = offspringGenetics.personalityFitness,
                    compatibility = compatibility,
                    offspringFitness = offspringGenetics.personalityFitness,
                    timestamp = currentTime,
                    summary = PersonalityGeneticsHelper.GetPersonalityDescription(offspringGenetics)
                });

                Debug.Log($"Personality breeding successful! Offspring: {offspringGenetics.geneticCuriosity} curiosity, " +
                         $"{offspringGenetics.geneticPlayfulness} playfulness, mutations: {offspringGenetics.mutationCount}");

                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Sync genetic personality with current personality (for generation 1 chimeras)
        /// </summary>
        private void SyncGeneticPersonality()
        {
            foreach (var (personality, genetics, entity) in
                SystemAPI.Query<RefRO<CreaturePersonality>, RefRW<PersonalityGeneticComponent>>().WithEntityAccess())
            {
                // Keep genetic baseline in sync with current personality for young chimeras
                // Elderly chimeras will have PersonalityStabilitySystem maintain separation
                if (genetics.ValueRO.mutationCount == 0)
                {
                    // No mutations yet, keep baseline synced
                    genetics.ValueRW.geneticCuriosity = personality.ValueRO.Curiosity;
                    genetics.ValueRW.geneticPlayfulness = personality.ValueRO.Playfulness;
                    genetics.ValueRW.geneticAggression = personality.ValueRO.Aggression;
                    genetics.ValueRW.geneticAffection = personality.ValueRO.Affection;
                    genetics.ValueRW.geneticIndependence = personality.ValueRO.Independence;
                    genetics.ValueRW.geneticNervousness = personality.ValueRO.Nervousness;
                    genetics.ValueRW.geneticStubbornness = personality.ValueRO.Stubbornness;
                    genetics.ValueRW.geneticLoyalty = personality.ValueRO.Loyalty;
                }
            }
        }

        // Helper methods

        private PersonalityGeneticComponent CreateOffspringPersonality(
            PersonalityGeneticComponent parent1,
            PersonalityGeneticComponent parent2,
            bool allowMutations,
            float mutationRate,
            uint seed,
            out System.Collections.Generic.List<PersonalityInheritanceRecord> records)
        {
            var random = new Unity.Mathematics.Random(seed);
            records = new System.Collections.Generic.List<PersonalityInheritanceRecord>();

            var offspring = new PersonalityGeneticComponent();
            byte mutationCount = 0;

            // Inherit each trait
            offspring.geneticCuriosity = InheritTrait(parent1.geneticCuriosity, parent2.geneticCuriosity,
                PersonalityTrait.Curiosity, allowMutations, mutationRate, ref random, ref mutationCount, records);

            offspring.geneticPlayfulness = InheritTrait(parent1.geneticPlayfulness, parent2.geneticPlayfulness,
                PersonalityTrait.Playfulness, allowMutations, mutationRate, ref random, ref mutationCount, records);

            offspring.geneticAggression = InheritTrait(parent1.geneticAggression, parent2.geneticAggression,
                PersonalityTrait.Aggression, allowMutations, mutationRate, ref random, ref mutationCount, records);

            offspring.geneticAffection = InheritTrait(parent1.geneticAffection, parent2.geneticAffection,
                PersonalityTrait.Affection, allowMutations, mutationRate, ref random, ref mutationCount, records);

            offspring.geneticIndependence = InheritTrait(parent1.geneticIndependence, parent2.geneticIndependence,
                PersonalityTrait.Independence, allowMutations, mutationRate, ref random, ref mutationCount, records);

            offspring.geneticNervousness = InheritTrait(parent1.geneticNervousness, parent2.geneticNervousness,
                PersonalityTrait.Nervousness, allowMutations, mutationRate, ref random, ref mutationCount, records);

            offspring.geneticStubbornness = InheritTrait(parent1.geneticStubbornness, parent2.geneticStubbornness,
                PersonalityTrait.Stubbornness, allowMutations, mutationRate, ref random, ref mutationCount, records);

            offspring.geneticLoyalty = InheritTrait(parent1.geneticLoyalty, parent2.geneticLoyalty,
                PersonalityTrait.Loyalty, allowMutations, mutationRate, ref random, ref mutationCount, records);

            // Track inheritance
            offspring.parent1Influence = 500; // Default 50/50
            offspring.parent2Influence = 500;
            offspring.mutationCount = mutationCount;
            offspring.hasPersonalityMutation = mutationCount > 0;

            // Calculate fitness
            offspring.personalityFitness = PersonalityGeneticsHelper.CalculatePersonalityFitness(offspring);

            return offspring;
        }

        private byte InheritTrait(
            byte parent1Value,
            byte parent2Value,
            PersonalityTrait trait,
            bool allowMutations,
            float mutationRate,
            ref Unity.Mathematics.Random random,
            ref byte mutationCount,
            System.Collections.Generic.List<PersonalityInheritanceRecord> records)
        {
            // Blend parents
            byte offspringValue = PersonalityGeneticsHelper.BlendTrait(parent1Value, parent2Value, random);
            sbyte mutationDelta = 0;
            bool wasMutated = false;

            // Check for mutation
            if (allowMutations && random.NextFloat() < mutationRate)
            {
                offspringValue = PersonalityGeneticsHelper.ApplyMutation(offspringValue, random, out mutationDelta);
                mutationCount++;
                wasMutated = true;
            }

            // Record inheritance
            records.Add(new PersonalityInheritanceRecord
            {
                trait = trait,
                parentValue = (byte)((parent1Value + parent2Value) / 2),
                offspringValue = offspringValue,
                wasInherited = !wasMutated,
                source = wasMutated ? ParentSource.Mutation : ParentSource.Blend,
                mutationDelta = mutationDelta
            });

            return offspringValue;
        }

        private float CalculateDiversityBonus(PersonalityGeneticComponent parent1, PersonalityGeneticComponent parent2)
        {
            // Reward for having diverse personalities (not too similar)
            float totalDifference = 0f;
            totalDifference += math.abs(parent1.geneticCuriosity - parent2.geneticCuriosity);
            totalDifference += math.abs(parent1.geneticPlayfulness - parent2.geneticPlayfulness);
            totalDifference += math.abs(parent1.geneticAggression - parent2.geneticAggression);
            totalDifference += math.abs(parent1.geneticAffection - parent2.geneticAffection);
            totalDifference += math.abs(parent1.geneticIndependence - parent2.geneticIndependence);
            totalDifference += math.abs(parent1.geneticNervousness - parent2.geneticNervousness);
            totalDifference += math.abs(parent1.geneticStubbornness - parent2.geneticStubbornness);
            totalDifference += math.abs(parent1.geneticLoyalty - parent2.geneticLoyalty);

            float averageDifference = totalDifference / 8f;

            // Optimal diversity is 20-40 points difference
            if (averageDifference > 20f && averageDifference < 40f)
                return 0.1f; // +10% bonus

            return 0f;
        }

        private float CalculateExtremesPenalty(PersonalityGeneticComponent parent1, PersonalityGeneticComponent parent2)
        {
            // Penalty for extreme trait combinations (e.g., very aggressive + very nervous)
            float penalty = 0f;

            // Check for problematic combinations
            if (parent1.geneticAggression > 80 && parent2.geneticNervousness > 80)
                penalty += 0.15f;

            if (parent1.geneticIndependence > 85 && parent2.geneticAffection > 85)
                penalty += 0.1f;

            return math.min(penalty, 0.3f); // Max 30% penalty
        }

        private float CalculateQuickCompatibility(PersonalityGeneticComponent parent1, PersonalityGeneticComponent parent2)
        {
            // Quick compatibility check without full calculation
            float curiosityCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1.geneticCuriosity, parent2.geneticCuriosity);
            float playfulnessCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1.geneticPlayfulness, parent2.geneticPlayfulness);
            float aggressionCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1.geneticAggression, parent2.geneticAggression);
            float affectionCompat = PersonalityGeneticsHelper.CalculateTraitCompatibility(
                parent1.geneticAffection, parent2.geneticAffection);

            return (curiosityCompat + playfulnessCompat + aggressionCompat + affectionCompat) / 4f;
        }
    }
}
