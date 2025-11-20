using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// AGE-BASED SENSITIVITY SYSTEM
    ///
    /// Implements the core emotional partnership mechanic:
    /// "Baby chimeras are forgiving, adults are deeply affected by treatment"
    ///
    /// Responsibilities:
    /// - Update sensitivity modifiers as chimeras age
    /// - Process bond damage with age-appropriate severity
    /// - Manage recovery attempts (food, gifts, quality time)
    /// - Track emotional scars (adults) vs quick recovery (babies)
    /// - Alert players when relationships become more fragile
    ///
    /// Integration:
    /// - Works with EnhancedBondingSystem and CreatureBondingSystem
    /// - Modifies bond strength changes based on age
    /// - Creates permanent consequences for mistreating adults
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class AgeSensitivitySystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        // Age-based configuration
        private const float BABY_FORGIVENESS = 0.9f;      // Very forgiving
        private const float CHILD_FORGIVENESS = 0.7f;     // Moderately forgiving
        private const float TEEN_FORGIVENESS = 0.4f;      // Less forgiving
        private const float ADULT_FORGIVENESS = 0.2f;     // Consequences are real
        private const float ELDERLY_FORGIVENESS = 0.35f;  // Wise forgiveness (more than adults, less than teens)

        private const float BABY_MEMORY = 0.2f;           // Forget quickly
        private const float CHILD_MEMORY = 0.4f;          // Remember some
        private const float TEEN_MEMORY = 0.7f;           // Remember most
        private const float ADULT_MEMORY = 0.95f;         // Never forget
        private const float ELDERLY_MEMORY = 0.99f;       // Perfect memory - remembers everything

        private const float BABY_RECOVERY = 2.5f;         // Heal fast
        private const float CHILD_RECOVERY = 1.5f;        // Heal normally
        private const float TEEN_RECOVERY = 0.7f;         // Heal slowly
        private const float ADULT_RECOVERY = 0.3f;        // Heal very slowly
        private const float ELDERLY_RECOVERY = 0.4f;      // Slow but possible with genuine care

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            Debug.Log("Age Sensitivity System initialized - babies forgiving, adults remember!");
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Update age-based sensitivity modifiers
            UpdateAgeSensitivity(deltaTime);

            // Process bond damage events
            ProcessBondDamage(currentTime);

            // Process recovery attempts
            ProcessRecoveryAttempts(currentTime);

            // Natural healing over time (faster for babies)
            ProcessNaturalRecovery(deltaTime);

            // Check for age stage transitions
            CheckAgeTransitions(currentTime);

            // Update emotional scars (can partially heal with consistent care)
            UpdateEmotionalScars(deltaTime);
        }

        /// <summary>
        /// Updates sensitivity modifiers based on current age/life stage
        /// </summary>
        private void UpdateAgeSensitivity(float deltaTime)
        {
            foreach (var (identity, sensitivity, entity) in
                SystemAPI.Query<RefRO<CreatureIdentityComponent>, RefRW<AgeSensitivityComponent>>().WithEntityAccess())
            {
                var currentStage = identity.ValueRO.CurrentLifeStage;
                float age = identity.ValueRO.Age;
                float maxLifespan = identity.ValueRO.MaxLifespan;
                float agePercent = maxLifespan > 0 ? age / maxLifespan : 0f;

                // Update age tracking
                sensitivity.ValueRW.currentLifeStage = currentStage;
                sensitivity.ValueRW.agePercentage = agePercent;

                // Calculate sensitivity modifiers based on life stage
                // 5-STAGE EMOTIONAL JOURNEY: Baby → Child → Teen → Adult → Elderly
                switch (currentStage)
                {
                    case LifeStage.Baby:
                        sensitivity.ValueRW.forgivenessMultiplier = BABY_FORGIVENESS;
                        sensitivity.ValueRW.memoryStrength = BABY_MEMORY;
                        sensitivity.ValueRW.bondDamageMultiplier = 0.5f; // Half damage
                        sensitivity.ValueRW.recoverySpeed = BABY_RECOVERY;
                        sensitivity.ValueRW.emotionalResilience = 0.9f;
                        sensitivity.ValueRW.trustVulnerability = 0.2f;
                        break;

                    case LifeStage.Child:
                        sensitivity.ValueRW.forgivenessMultiplier = CHILD_FORGIVENESS;
                        sensitivity.ValueRW.memoryStrength = CHILD_MEMORY;
                        sensitivity.ValueRW.bondDamageMultiplier = 0.8f;
                        sensitivity.ValueRW.recoverySpeed = CHILD_RECOVERY;
                        sensitivity.ValueRW.emotionalResilience = 0.7f;
                        sensitivity.ValueRW.trustVulnerability = 0.4f;
                        break;

                    case LifeStage.Teen:
                        sensitivity.ValueRW.forgivenessMultiplier = TEEN_FORGIVENESS;
                        sensitivity.ValueRW.memoryStrength = TEEN_MEMORY;
                        sensitivity.ValueRW.bondDamageMultiplier = 1.5f; // More damage
                        sensitivity.ValueRW.recoverySpeed = TEEN_RECOVERY;
                        sensitivity.ValueRW.emotionalResilience = 0.4f;
                        sensitivity.ValueRW.trustVulnerability = 0.7f;
                        break;

                    case LifeStage.Adult:
                        sensitivity.ValueRW.forgivenessMultiplier = ADULT_FORGIVENESS;
                        sensitivity.ValueRW.memoryStrength = ADULT_MEMORY;
                        sensitivity.ValueRW.bondDamageMultiplier = 2.0f; // Double damage!
                        sensitivity.ValueRW.recoverySpeed = ADULT_RECOVERY;
                        sensitivity.ValueRW.emotionalResilience = 0.2f;
                        sensitivity.ValueRW.trustVulnerability = 0.95f;
                        break;

                    case LifeStage.Elderly:
                        // THE ULTIMATE PARTNERSHIP - Profound bonds, perfect memory
                        // Devastating if neglected, but wise enough to forgive genuine care
                        sensitivity.ValueRW.forgivenessMultiplier = ELDERLY_FORGIVENESS;
                        sensitivity.ValueRW.memoryStrength = ELDERLY_MEMORY;
                        sensitivity.ValueRW.bondDamageMultiplier = 1.8f; // Devastating but tempered by wisdom
                        sensitivity.ValueRW.recoverySpeed = ELDERLY_RECOVERY;
                        sensitivity.ValueRW.emotionalResilience = 0.15f; // Very vulnerable emotionally
                        sensitivity.ValueRW.trustVulnerability = 0.98f; // Nearly impossible to repair broken trust
                        break;

                    // Legacy support for deprecated stages handled in default case
                    default:
                        // Recalculate using age percentage for legacy stages
                        var correctedStage = LifeStageExtensions.CalculateLifeStageFromPercentage(agePercent);
                        sensitivity.ValueRW.currentLifeStage = correctedStage;
                        // Recursive call would happen next frame with corrected stage
                        break;
                }

                // Track time since negative interactions for recovery
                sensitivity.ValueRW.timeSinceLastNegativeInteraction += deltaTime;
            }
        }

        /// <summary>
        /// Processes bond damage events with age-appropriate severity
        /// Adults take much more damage than babies from same actions
        /// </summary>
        private void ProcessBondDamage(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (damageEvent, entity) in
                SystemAPI.Query<RefRO<BondDamageEvent>>().WithEntityAccess())
            {
                var targetEntity = damageEvent.ValueRO.targetCreature;

                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Get age sensitivity
                if (!EntityManager.HasComponent<AgeSensitivityComponent>(targetEntity))
                {
                    // Initialize if missing
                    ecb.AddComponent<AgeSensitivityComponent>(targetEntity);
                }

                var sensitivity = EntityManager.GetComponentData<AgeSensitivityComponent>(targetEntity);

                // Calculate actual damage with age multipliers
                float baseDamage = damageEvent.ValueRO.rawDamageAmount;
                float ageDamageMultiplier = sensitivity.bondDamageMultiplier;
                float actualDamage = baseDamage * ageDamageMultiplier;

                // Apply forgiveness (reduces damage)
                actualDamage *= (1.0f - sensitivity.forgivenessMultiplier);

                // Apply to bond if exists
                if (EntityManager.HasComponent<BondingComponent>(targetEntity))
                {
                    // Get current bonding data (from EnhancedBondingSystem components)
                    // This integrates with existing bonding infrastructure
                    ApplyBondDamage(targetEntity, actualDamage, damageEvent.ValueRO.damageType, ecb);
                }

                // For adults and elderly, severe damage creates emotional scars
                // NEW VISION: Only mature chimeras develop scars - babies/children are resilient
                if (sensitivity.currentLifeStage == LifeStage.Elderly)
                {
                    // Elderly are most vulnerable - even moderate damage can scar
                    if (actualDamage > 0.2f) // Lower threshold for elderly
                    {
                        CreateEmotionalScar(targetEntity, damageEvent.ValueRO, actualDamage * 1.1f, ecb);
                    }
                }
                else if (sensitivity.currentLifeStage == LifeStage.Adult)
                {
                    if (actualDamage > 0.3f) // Threshold for scarring
                    {
                        CreateEmotionalScar(targetEntity, damageEvent.ValueRO, actualDamage, ecb);
                    }
                }
                // Teens can develop minor scars from very severe trauma
                else if (sensitivity.currentLifeStage == LifeStage.Teen && actualDamage > 0.5f)
                {
                    CreateEmotionalScar(targetEntity, damageEvent.ValueRO, actualDamage * 0.7f, ecb);
                }

                // Reset recovery tracking
                sensitivity.timeSinceLastNegativeInteraction = 0f;
                sensitivity.consecutivePositiveInteractions = 0;
                EntityManager.SetComponentData(targetEntity, sensitivity);

                Debug.Log($"Bond damage dealt: {actualDamage:F2} (base: {baseDamage:F2}) " +
                         $"to {sensitivity.currentLifeStage} chimera. " +
                         $"Age multiplier: {ageDamageMultiplier:F2}x");

                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Processes recovery attempts - different methods have different effectiveness
        /// </summary>
        private void ProcessRecoveryAttempts(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (recovery, entity) in
                SystemAPI.Query<RefRO<BondRecoveryRequest>>().WithEntityAccess())
            {
                var targetEntity = recovery.ValueRO.targetCreature;

                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var sensitivity = EntityManager.GetComponentData<AgeSensitivityComponent>(targetEntity);

                // Calculate recovery amount based on method and age
                float baseRecovery = recovery.ValueRO.recoveryPotential;
                float methodMultiplier = GetRecoveryMethodMultiplier(recovery.ValueRO.method);
                float ageRecoveryMultiplier = sensitivity.recoverySpeed;

                // Genuine care is more effective, "gaming" the system less so
                float genuinessMultiplier = recovery.ValueRO.isGenuineApology ? 1.5f : 0.7f;

                // Pattern of positive behavior boosts recovery
                float patternBonus = math.min(recovery.ValueRO.recentPositiveActions * 0.1f, 0.5f);

                float totalRecovery = baseRecovery * methodMultiplier * ageRecoveryMultiplier *
                                     genuinessMultiplier + patternBonus;

                // Apply recovery
                ApplyBondRecovery(targetEntity, totalRecovery, recovery.ValueRO.method, ecb);

                // Track positive interactions
                sensitivity.consecutivePositiveInteractions++;
                EntityManager.SetComponentData(targetEntity, sensitivity);

                // Create positive memory
                CreatePositiveMemory(targetEntity, recovery.ValueRO.method, totalRecovery,
                    sensitivity.currentLifeStage, currentTime, ecb);

                Debug.Log($"Bond recovery: +{totalRecovery:F2} via {recovery.ValueRO.method} " +
                         $"(age: {sensitivity.currentLifeStage}, genuine: {recovery.ValueRO.isGenuineApology})");

                ecb.DestroyEntity(entity);
            }
        }

        /// <summary>
        /// Natural healing over time - faster for babies, slower for adults
        /// </summary>
        private void ProcessNaturalRecovery(float deltaTime)
        {
            foreach (var (sensitivity, entity) in
                SystemAPI.Query<RefRW<AgeSensitivityComponent>>().WithEntityAccess())
            {
                // Only heal naturally if no recent negative interactions AND consecutive positives
                if (sensitivity.ValueRO.timeSinceLastNegativeInteraction > 3600f && // 1 hour
                    sensitivity.ValueRO.consecutivePositiveInteractions > 5)
                {
                    float naturalHealRate = 0.001f * sensitivity.ValueRO.recoverySpeed * deltaTime;

                    // Apply to bond (very small amount)
                    if (EntityManager.HasComponent<BondingComponent>(entity))
                    {
                        var ecb = _ecbSystem.CreateCommandBuffer();
                        ApplyBondRecovery(entity, naturalHealRate, RecoveryMethod.QualityTime, ecb);
                    }
                }
            }
        }

        /// <summary>
        /// Checks for age stage transitions and alerts player
        /// </summary>
        private void CheckAgeTransitions(float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (identity, sensitivity, entity) in
                SystemAPI.Query<RefRO<CreatureIdentityComponent>, RefRW<AgeSensitivityComponent>>().WithEntityAccess())
            {
                var currentStage = identity.ValueRO.CurrentLifeStage;
                var trackedStage = sensitivity.ValueRO.currentLifeStage;

                // Detect transition
                if (currentStage != trackedStage)
                {
                    bool becameLessForgiving = GetForgivenessForStage(currentStage) <
                                               GetForgivenessForStage(trackedStage);

                    // Create transition event
                    var transitionEvent = EntityManager.CreateEntity();
                    ecb.AddComponent(transitionEvent, new AgeStageProgressionEvent
                    {
                        creature = entity,
                        previousStage = trackedStage,
                        newStage = currentStage,
                        currentBondStrength = GetCurrentBondStrength(entity),
                        timestamp = currentTime,
                        becameLessForgiving = becameLessForgiving,
                        memoriesNowPermanent = currentStage >= LifeStage.Teen // Teen and beyond have strong memory
                    });

                    Debug.LogWarning($"AGE TRANSITION: {trackedStage} → {currentStage}. " +
                                   $"Relationship now {(becameLessForgiving ? "MORE FRAGILE" : "unchanged")}!");
                }
            }
        }

        /// <summary>
        /// Updates emotional scars - can partially heal but never fully disappear
        /// </summary>
        private void UpdateEmotionalScars(float deltaTime)
        {
            foreach (var (sensitivity, entity) in
                SystemAPI.Query<RefRO<AgeSensitivityComponent>>().WithEntityAccess())
            {
                if (!EntityManager.HasBuffer<EmotionalScar>(entity))
                    continue;

                var scarBuffer = SystemAPI.GetBuffer<EmotionalScar>(entity);

                for (int i = 0; i < scarBuffer.Length; i++)
                {
                    var scar = scarBuffer[i];

                    // Scars can heal with LOTS of consistent positive care
                    if (sensitivity.ValueRO.consecutivePositiveInteractions > 20)
                    {
                        // Very slow healing
                        scar.healingProgress += 0.001f * deltaTime;
                        scar.healingProgress = math.min(scar.healingProgress, 0.8f); // Max 80% healed

                        if (scar.healingProgress > 0.1f && !scar.hasPartiallyHealed)
                        {
                            scar.hasPartiallyHealed = true;
                            Debug.Log($"Emotional scar beginning to heal: {scar.description}");
                        }

                        scarBuffer[i] = scar;
                    }
                }
            }
        }

        // Helper methods

        private void ApplyBondDamage(Entity target, float damage, BondDamageType type, EntityCommandBuffer ecb)
        {
            // Integrates with existing bonding components
            if (EntityManager.HasComponent<BondingComponent>(target))
            {
                var bonding = EntityManager.GetComponentData<BondingComponent>(target);
                // Damage reduces social need satisfaction and trust
                // This will be picked up by EnhancedBondingSystem
            }

            // Also works with CreatureBondingSystem components
            if (EntityManager.HasComponent<CreatureBondData>(target))
            {
                var bondData = EntityManager.GetComponentData<CreatureBondData>(target);
                bondData.bondStrength = math.max(0f, bondData.bondStrength - damage);
                bondData.negativeExperiences++;
                EntityManager.SetComponentData(target, bondData);
            }
        }

        private void ApplyBondRecovery(Entity target, float recovery, RecoveryMethod method, EntityCommandBuffer ecb)
        {
            if (EntityManager.HasComponent<CreatureBondData>(target))
            {
                var bondData = EntityManager.GetComponentData<CreatureBondData>(target);
                bondData.bondStrength = math.min(1f, bondData.bondStrength + recovery);
                bondData.positiveExperiences++;
                bondData.recentPositiveInteractions++;
                EntityManager.SetComponentData(target, bondData);
            }
        }

        private void CreateEmotionalScar(Entity target, BondDamageEvent damageEvent, float severity, EntityCommandBuffer ecb)
        {
            if (!EntityManager.HasBuffer<EmotionalScar>(target))
            {
                ecb.AddBuffer<EmotionalScar>(target);
            }

            var scarBuffer = EntityManager.GetBuffer<EmotionalScar>(target);

            var newScar = new EmotionalScar
            {
                sourceType = damageEvent.damageType,
                severityWhenReceived = severity,
                ageWhenReceived = damageEvent.creatureAgeAtDamage,
                timestamp = damageEvent.timestamp,
                hasPartiallyHealed = false,
                healingProgress = 0f,
                description = $"{damageEvent.damageType} at {damageEvent.creatureAgeAtDamage} stage"
            };

            scarBuffer.Add(newScar);

            Debug.LogWarning($"EMOTIONAL SCAR created: {newScar.description} (severity: {severity:F2})");
        }

        private void CreatePositiveMemory(Entity target, RecoveryMethod method, float intensity,
            LifeStage age, float time, EntityCommandBuffer ecb)
        {
            if (!EntityManager.HasBuffer<PositiveMemory>(target))
            {
                ecb.AddBuffer<PositiveMemory>(target);
            }

            var memoryBuffer = EntityManager.GetBuffer<PositiveMemory>(target);

            var memory = new PositiveMemory
            {
                type = ConvertRecoveryToMemoryType(method),
                intensityWhenCreated = intensity,
                ageWhenCreated = age,
                timestamp = time,
                currentStrength = intensity,
                description = $"{method} at {age} stage"
            };

            memoryBuffer.Add(memory);
        }

        private float GetRecoveryMethodMultiplier(RecoveryMethod method)
        {
            return method switch
            {
                RecoveryMethod.SpecialFood => 0.5f,
                RecoveryMethod.ThoughtfulGift => 0.8f,
                RecoveryMethod.QualityTime => 1.2f,
                RecoveryMethod.SharedVictory => 1.5f,
                RecoveryMethod.GenuineApology => 1.0f,
                RecoveryMethod.RescueFromDanger => 2.0f,
                RecoveryMethod.IntroducingFriends => 0.9f,
                _ => 1.0f
            };
        }

        private PositiveMemoryType ConvertRecoveryToMemoryType(RecoveryMethod method)
        {
            return method switch
            {
                RecoveryMethod.SharedVictory => PositiveMemoryType.FirstVictory,
                RecoveryMethod.ThoughtfulGift => PositiveMemoryType.ReceivedGift,
                RecoveryMethod.RescueFromDanger => PositiveMemoryType.RescuedByPartner,
                _ => PositiveMemoryType.PerfectCooperation
            };
        }

        private float GetForgivenessForStage(LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Baby => BABY_FORGIVENESS,
                LifeStage.Child or LifeStage.Juvenile => CHILD_FORGIVENESS,
                LifeStage.Teen or LifeStage.Adolescent => TEEN_FORGIVENESS,
                LifeStage.Adult => ADULT_FORGIVENESS,
                LifeStage.Elderly or LifeStage.Elder => ELDERLY_FORGIVENESS,
                _ => ADULT_FORGIVENESS
            };
        }

        private float GetCurrentBondStrength(Entity entity)
        {
            if (EntityManager.HasComponent<Laboratory.Chimera.ECS.CreatureBondData>(entity))
            {
                return EntityManager.GetComponentData<Laboratory.Chimera.ECS.CreatureBondData>(entity).bondStrength;
            }
            return 0.5f; // Default
        }
    }
}
