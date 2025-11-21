using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Consciousness.Core
{
    /// <summary>
    /// PERSONALITY STABILITY SYSTEM
    ///
    /// Manages age-based personality resistance and auto-reversion
    ///
    /// Key Mechanics:
    /// 1. Babies have highly malleable personalities (100% change rate)
    /// 2. Adults have resistant personalities (20% change rate)
    /// 3. Elderly have LOCKED personalities (5% change rate)
    /// 4. Elderly automatically revert to baseline over time
    /// 5. Baseline snapshot taken when first reaching elderly stage
    ///
    /// Integration:
    /// - Works with CreaturePersonality component
    /// - Integrates with AgeSensitivitySystem
    /// - Modifies UpdateFromExperience learning rate
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    // NOTE: UpdateAfter(AgeSensitivitySystem) removed to avoid circular dependency with Social assembly
    public partial class PersonalityStabilitySystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        // Age-based malleability configuration
        private const float BABY_MALLEABILITY = 1.0f;      // 100% - personality highly flexible
        private const float CHILD_MALLEABILITY = 0.7f;     // 70% - still forming
        private const float TEEN_MALLEABILITY = 0.4f;      // 40% - solidifying
        private const float ADULT_MALLEABILITY = 0.2f;     // 20% - mostly fixed
        private const float ELDERLY_MALLEABILITY = 0.05f;  // 5% - EXTREMELY resistant to change

        // Reversion speed (how fast temporary changes revert to baseline)
        private const float BABY_REVERSION = 0.0f;         // No baseline yet
        private const float CHILD_REVERSION = 0.0f;        // Still forming
        private const float TEEN_REVERSION = 0.1f;         // Slight reversion
        private const float ADULT_REVERSION = 0.3f;        // Moderate reversion
        private const float ELDERLY_REVERSION = 1.0f;      // Strong reversion to lifetime baseline

        // Reversion thresholds
        private const float REVERSION_START_DELAY = 3600f;  // 1 hour before reversion starts
        private const float DEVIATION_THRESHOLD = 5f;       // Min 5-point difference to trigger reversion

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            Debug.Log("Personality Stability System initialized - elderly personalities LOCK!");
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Update personality stability based on age
            UpdateAgeBasedStability(deltaTime);

            // Lock baseline personality when transitioning to elderly
            LockElderlyBaselines(currentTime);

            // Process personality change events
            ProcessPersonalityChanges(currentTime);

            // Auto-revert elderly chimeras to baseline
            ProcessElderlyReversion(deltaTime, currentTime);

            // Check for age transitions affecting personality stability
            CheckStabilityTransitions(currentTime);
        }

        /// <summary>
        /// Updates personality malleability and reversion speed based on current age
        /// </summary>
        private void UpdateAgeBasedStability(float deltaTime)
        {
            foreach (var (identity, stability, personality, entity) in
                SystemAPI.Query<RefRO<CreatureIdentityComponent>, RefRW<PersonalityStabilityComponent>, RefRW<CreaturePersonality>>().WithEntityAccess())
            {
                var currentStage = identity.ValueRO.CurrentLifeStage;

                // Update tracking
                stability.ValueRW.currentLifeStage = currentStage;
                stability.ValueRW.timeSinceLastPersonalityChange += deltaTime;

                // Calculate malleability and reversion based on life stage
                switch (currentStage)
                {
                    case LifeStage.Baby:
                        stability.ValueRW.personalityMalleability = BABY_MALLEABILITY;
                        stability.ValueRW.reversionSpeed = BABY_REVERSION;
                        break;

                    case LifeStage.Child:
                        stability.ValueRW.personalityMalleability = CHILD_MALLEABILITY;
                        stability.ValueRW.reversionSpeed = CHILD_REVERSION;
                        break;

                    case LifeStage.Teen:
                        stability.ValueRW.personalityMalleability = TEEN_MALLEABILITY;
                        stability.ValueRW.reversionSpeed = TEEN_REVERSION;
                        break;

                    case LifeStage.Adult:
                        stability.ValueRW.personalityMalleability = ADULT_MALLEABILITY;
                        stability.ValueRW.reversionSpeed = ADULT_REVERSION;
                        break;

                    case LifeStage.Elderly:
                        // EXTREMELY resistant to change, strong reversion to baseline
                        stability.ValueRW.personalityMalleability = ELDERLY_MALLEABILITY;
                        stability.ValueRW.reversionSpeed = ELDERLY_REVERSION;
                        stability.ValueRW.timeSinceBaselineLock += deltaTime;
                        break;
                }

                // Apply malleability to personality learning rate
                // This makes elderly chimeras resistant to personality changes
                float baseLearningRate = 0.1f + (personality.ValueRO.MemoryStrength / 100f) * 0.4f;
                personality.ValueRW.LearningRate = baseLearningRate * stability.ValueRO.personalityMalleability;
            }
        }

        /// <summary>
        /// Locks baseline personality when chimera first reaches elderly stage
        ///
        /// PHASE 8 UPDATE: Prefers genetic baseline from PersonalityGeneticComponent
        /// If chimera has inherited genetics, use that as the baseline (their "true nature")
        /// Otherwise, use current personality as baseline (for generation 1 chimeras)
        /// </summary>
        private void LockElderlyBaselines(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (identity, stability, personality, entity) in
                SystemAPI.Query<RefRO<CreatureIdentityComponent>, RefRW<PersonalityStabilityComponent>, RefRO<CreaturePersonality>>().WithEntityAccess())
            {
                // Check if just transitioned to elderly and baseline not yet locked
                if (identity.ValueRO.CurrentLifeStage == LifeStage.Elderly && !stability.ValueRO.hasLockedBaseline)
                {
                    // Create baseline snapshot
                    if (!EntityManager.HasComponent<BaselinePersonalityComponent>(entity))
                    {
                        BaselinePersonalityComponent baseline;

                        // PHASE 8: Check if chimera has inherited personality genetics
                        if (EntityManager.HasComponent<PersonalityGeneticComponent>(entity))
                        {
                            // Use GENETIC baseline (inherited from parents)
                            var genetics = EntityManager.GetComponentData<PersonalityGeneticComponent>(entity);

                            baseline = new BaselinePersonalityComponent
                            {
                                baselineCuriosity = genetics.geneticCuriosity,
                                baselinePlayfulness = genetics.geneticPlayfulness,
                                baselineAggression = genetics.geneticAggression,
                                baselineAffection = genetics.geneticAffection,
                                baselineIndependence = genetics.geneticIndependence,
                                baselineNervousness = genetics.geneticNervousness,
                                baselineStubbornness = genetics.geneticStubbornness,
                                baselineLoyalty = genetics.geneticLoyalty,
                                lockTimestamp = currentTime,
                                wasLockedAtElderly = true
                            };

                            Debug.Log($"GENETIC BASELINE LOCKED for elderly chimera! " +
                                     $"Using INHERITED personality: Curiosity={genetics.geneticCuriosity}, " +
                                     $"Affection={genetics.geneticAffection}, Loyalty={genetics.geneticLoyalty}");
                        }
                        else
                        {
                            // Use current personality as baseline (generation 1 chimeras)
                            baseline = new BaselinePersonalityComponent
                            {
                                baselineCuriosity = personality.ValueRO.Curiosity,
                                baselinePlayfulness = personality.ValueRO.Playfulness,
                                baselineAggression = personality.ValueRO.Aggression,
                                baselineAffection = personality.ValueRO.Affection,
                                baselineIndependence = personality.ValueRO.Independence,
                                baselineNervousness = personality.ValueRO.Nervousness,
                                baselineStubbornness = personality.ValueRO.Stubbornness,
                                baselineLoyalty = personality.ValueRO.Loyalty,
                                lockTimestamp = currentTime,
                                wasLockedAtElderly = true
                            };

                            Debug.Log($"CURRENT BASELINE LOCKED for elderly chimera (no genetic data). " +
                                     $"Personality: Curiosity={personality.ValueRO.Curiosity}, " +
                                     $"Affection={personality.ValueRO.Affection}, Loyalty={personality.ValueRO.Loyalty}");
                        }

                        ecb.AddComponent(entity, baseline);
                        stability.ValueRW.hasLockedBaseline = true;
                        stability.ValueRW.timeSinceBaselineLock = 0f;
                    }
                }
            }
        }

        /// <summary>
        /// Processes personality change events and tracks deviations from baseline
        /// </summary>
        private void ProcessPersonalityChanges(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (changeEvent, entity) in
                SystemAPI.Query<RefRO<PersonalityChangeEvent>>().WithEntityAccess())
            {
                var targetEntity = changeEvent.ValueRO.targetCreature;

                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Update stability tracking
                if (EntityManager.HasComponent<PersonalityStabilityComponent>(targetEntity))
                {
                    var stability = EntityManager.GetComponentData<PersonalityStabilityComponent>(targetEntity);
                    stability.timeSinceLastPersonalityChange = 0f;

                    // Check if this is a deviation from baseline (elderly only)
                    if (stability.hasLockedBaseline && EntityManager.HasComponent<BaselinePersonalityComponent>(targetEntity))
                    {
                        var baseline = EntityManager.GetComponentData<BaselinePersonalityComponent>(targetEntity);
                        var personality = EntityManager.GetComponentData<CreaturePersonality>(targetEntity);

                        // Calculate total deviation
                        float deviation = CalculateTotalDeviation(personality, baseline);

                        stability.hasTemporaryDeviations = deviation > DEVIATION_THRESHOLD;
                        stability.deviationMagnitude = deviation / 800f; // Normalize (8 traits * 100 max = 800)

                        EntityManager.SetComponentData(targetEntity, stability);

                        if (stability.hasTemporaryDeviations)
                        {
                            Debug.LogWarning($"Elderly chimera personality deviation detected! " +
                                           $"Magnitude: {deviation:F1} - will auto-revert");
                        }
                    }
                }

                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Auto-reverts elderly chimeras back to their baseline personality over time
        /// </summary>
        private void ProcessElderlyReversion(float deltaTime, float currentTime)
        {
            foreach (var (baseline, stability, personality, entity) in
                SystemAPI.Query<RefRO<BaselinePersonalityComponent>, RefRW<PersonalityStabilityComponent>, RefRW<CreaturePersonality>>().WithEntityAccess())
            {
                // Only elderly chimeras revert
                if (stability.ValueRO.currentLifeStage != LifeStage.Elderly)
                    continue;

                // Must have deviations and some time since last change
                if (!stability.ValueRO.hasTemporaryDeviations)
                    continue;

                if (stability.ValueRO.timeSinceLastPersonalityChange < REVERSION_START_DELAY)
                    continue;

                // Calculate reversion amount (faster for larger deviations)
                float reversionRate = stability.ValueRO.reversionSpeed * deltaTime;
                float deviationBonus = stability.ValueRO.deviationMagnitude * 2f; // Larger deviations revert faster
                float totalReversion = reversionRate * (1f + deviationBonus);

                // Gradually pull each trait back toward baseline
                personality.ValueRW.Curiosity = RevertTrait(personality.ValueRO.Curiosity, baseline.ValueRO.baselineCuriosity, totalReversion);
                personality.ValueRW.Playfulness = RevertTrait(personality.ValueRO.Playfulness, baseline.ValueRO.baselinePlayfulness, totalReversion);
                personality.ValueRW.Aggression = RevertTrait(personality.ValueRO.Aggression, baseline.ValueRO.baselineAggression, totalReversion);
                personality.ValueRW.Affection = RevertTrait(personality.ValueRO.Affection, baseline.ValueRO.baselineAffection, totalReversion);
                personality.ValueRW.Independence = RevertTrait(personality.ValueRO.Independence, baseline.ValueRO.baselineIndependence, totalReversion);
                personality.ValueRW.Nervousness = RevertTrait(personality.ValueRO.Nervousness, baseline.ValueRO.baselineNervousness, totalReversion);
                personality.ValueRW.Stubbornness = RevertTrait(personality.ValueRO.Stubbornness, baseline.ValueRO.baselineStubbornness, totalReversion);
                personality.ValueRW.Loyalty = RevertTrait(personality.ValueRO.Loyalty, baseline.ValueRO.baselineLoyalty, totalReversion);

                // Check if reversion complete
                float remainingDeviation = CalculateTotalDeviation(personality.ValueRO, baseline.ValueRO);
                if (remainingDeviation < DEVIATION_THRESHOLD)
                {
                    stability.ValueRW.hasTemporaryDeviations = false;
                    stability.ValueRW.deviationMagnitude = 0f;
                    Debug.Log("Elderly chimera personality fully reverted to baseline!");
                }
            }
        }

        /// <summary>
        /// Checks for age transitions that affect personality stability
        /// </summary>
        private void CheckStabilityTransitions(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (identity, stability, entity) in
                SystemAPI.Query<RefRO<CreatureIdentityComponent>, RefRW<PersonalityStabilityComponent>>().WithEntityAccess())
            {
                var currentStage = identity.ValueRO.CurrentLifeStage;
                var trackedStage = stability.ValueRO.currentLifeStage;

                // Detect transition
                if (currentStage != trackedStage)
                {
                    float previousMalleability = GetMalleabilityForStage(trackedStage);
                    float newMalleability = GetMalleabilityForStage(currentStage);

                    bool becameLessMalleable = newMalleability < previousMalleability;
                    bool becameElderly = currentStage == LifeStage.Elderly;

                    // Create transition event
                    var transitionEvent = EntityManager.CreateEntity();
                    ecb.AddComponent(transitionEvent, new AgePersonalityProgressionEvent
                    {
                        creature = entity,
                        previousStage = trackedStage,
                        newStage = currentStage,
                        previousMalleability = previousMalleability,
                        newMalleability = newMalleability,
                        timestamp = currentTime,
                        personalityNowLocked = becameElderly,
                        personalityNowResistant = currentStage >= LifeStage.Teen && becameLessMalleable,
                        personalityNowFlexible = currentStage <= LifeStage.Child
                    });

                    if (becameElderly)
                    {
                        Debug.LogWarning($"PERSONALITY LOCKED: Chimera reached elderly stage! " +
                                       $"Baseline personality will resist changes and auto-revert.");
                    }
                    else if (becameLessMalleable)
                    {
                        Debug.Log($"Personality becoming more stable: {trackedStage} → {currentStage}. " +
                                 $"Malleability: {previousMalleability:F0}% → {newMalleability:F0}%");
                    }
                }
            }
        }

        // Helper methods

        private byte RevertTrait(byte currentValue, byte baselineValue, float reversionAmount)
        {
            if (currentValue == baselineValue)
                return currentValue;

            int difference = baselineValue - currentValue;
            int reversionStep = (int)(difference * reversionAmount);

            // Ensure at least 1 point movement if there's a difference
            if (reversionStep == 0 && difference != 0)
                reversionStep = difference > 0 ? 1 : -1;

            return (byte)Mathf.Clamp(currentValue + reversionStep, 0, 100);
        }

        private float CalculateTotalDeviation(CreaturePersonality current, BaselinePersonalityComponent baseline)
        {
            return math.abs(current.Curiosity - baseline.baselineCuriosity) +
                   math.abs(current.Playfulness - baseline.baselinePlayfulness) +
                   math.abs(current.Aggression - baseline.baselineAggression) +
                   math.abs(current.Affection - baseline.baselineAffection) +
                   math.abs(current.Independence - baseline.baselineIndependence) +
                   math.abs(current.Nervousness - baseline.baselineNervousness) +
                   math.abs(current.Stubbornness - baseline.baselineStubbornness) +
                   math.abs(current.Loyalty - baseline.baselineLoyalty);
        }

        private float GetMalleabilityForStage(LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Baby => BABY_MALLEABILITY * 100f,
                LifeStage.Child => CHILD_MALLEABILITY * 100f,
                LifeStage.Teen => TEEN_MALLEABILITY * 100f,
                LifeStage.Adult => ADULT_MALLEABILITY * 100f,
                LifeStage.Elderly => ELDERLY_MALLEABILITY * 100f,
                _ => ADULT_MALLEABILITY * 100f
            };
        }
    }
}
