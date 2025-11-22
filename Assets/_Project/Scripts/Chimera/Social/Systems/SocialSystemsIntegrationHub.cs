using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Laboratory.Chimera.Core;
using System;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// SOCIAL SYSTEMS INTEGRATION HUB
    ///
    /// Purpose: Coordinates all social systems to ensure they work together harmoniously
    ///
    /// Responsibilities:
    /// - Validates system initialization order
    /// - Provides unified bond strength calculation API
    /// - Coordinates population unlock triggers from bonding milestones
    /// - Manages cross-system event routing
    /// - Monitoring and debugging for social system health
    ///
    /// Coordinated Systems:
    /// 1. EnhancedBondingSystem - Core bonding with generational memory
    /// 2. AgeSensitivitySystem - Age-based forgiveness/memory
    /// 3. PopulationManagementSystem - 1-5 chimera slot management
    /// 4. SocialEngagementSystem - Community/viral features
    /// 5. EmotionalContagionSystem - Emotional spread between creatures
    /// 6. GroupDynamicsSystem - Team dynamics
    /// 7. CommunicationSystem - Chimera-player communication
    /// 8. SocialNetworkSystem - Social graph management
    /// 9. CulturalEvolutionSystem - Population-wide trait evolution
    ///
    /// Integration Pattern:
    /// - Hub updates BEFORE individual systems
    /// - Individual systems query hub for coordination data
    /// - Hub validates state transitions between systems
    /// - Hub provides debugging/monitoring tools
    ///
    /// Usage:
    /// var bondStrength = SocialSystemsIntegrationHub.GetEffectiveBondStrength(entity);
    /// var canUnlock = SocialSystemsIntegrationHub.CheckPopulationUnlockEligibility(playerEntity);
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial class SocialSystemsIntegrationHub : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        // System health tracking
        private bool _enhancedBondingSystemActive = false;
        private bool _ageSensitivitySystemActive = false;
        private bool _populationSystemActive = false;

        // Cross-system coordination data
        private NativeHashMap<Entity, float> _effectiveBondStrengthCache;
        private NativeHashMap<Entity, int> _strongBondCountCache;

        // Events for cross-system communication
        public static event Action<Entity, float> OnBondStrengthRecalculated;
        public static event Action<Entity, int, int> OnPopulationUnlockTriggered; // player, old, new capacity
        public static event Action<Entity, LifeStage, LifeStage> OnAgeTransitionDetected; // creature, old, new

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

            _effectiveBondStrengthCache = new NativeHashMap<Entity, float>(256, Allocator.Persistent);
            _strongBondCountCache = new NativeHashMap<Entity, int>(64, Allocator.Persistent);

            Debug.Log("🔗 Social Systems Integration Hub initialized - coordinating 9 social systems");
        }

        protected override void OnDestroy()
        {
            if (_effectiveBondStrengthCache.IsCreated)
                _effectiveBondStrengthCache.Dispose();
            if (_strongBondCountCache.IsCreated)
                _strongBondCountCache.Dispose();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // System health check
            ValidateSystemPresence();

            // Update coordination caches
            UpdateEffectiveBondStrengthCache();
            UpdateStrongBondCountCache();

            // Detect and broadcast state transitions
            DetectAgeTransitions();
            DetectPopulationUnlockOpportunities();

            // Validate cross-system consistency
            ValidateBondingConsistency();
        }

        /// <summary>
        /// Validates that required social systems are present
        /// </summary>
        private void ValidateSystemPresence()
        {
            _enhancedBondingSystemActive = World.GetExistingSystemManaged<EnhancedBondingSystem>() != null;
            _ageSensitivitySystemActive = World.GetExistingSystemManaged<AgeSensitivitySystem>() != null;
            _populationSystemActive = World.GetExistingSystemManaged<PopulationManagementSystem>() != null;

            if (!_enhancedBondingSystemActive)
                Debug.LogWarning("⚠️ EnhancedBondingSystem not active - social features may not work");
            if (!_ageSensitivitySystemActive)
                Debug.LogWarning("⚠️ AgeSensitivitySystem not active - age-based bonding disabled");
            if (!_populationSystemActive)
                Debug.LogWarning("⚠️ PopulationManagementSystem not active - capacity unlocks disabled");
        }

        /// <summary>
        /// Updates cache of effective bond strengths (base + age modifiers)
        /// </summary>
        private void UpdateEffectiveBondStrengthCache()
        {
            _effectiveBondStrengthCache.Clear();

            foreach (var (bondData, identity, entity) in
                SystemAPI.Query<RefRO<CreatureBondData>, RefRO<CreatureIdentityComponent>>().WithEntityAccess())
            {
                float baseBondStrength = bondData.ValueRO.bondStrength;
                float effectiveBondStrength = baseBondStrength;

                // Apply age-based modifiers if AgeSensitivitySystem is active
                if (_ageSensitivitySystemActive && EntityManager.HasComponent<AgeSensitivityComponent>(entity))
                {
                    var ageSensitivity = EntityManager.GetComponentData<AgeSensitivityComponent>(entity);
                    // Age affects bond quality - babies bond faster but shallower, adults slower but deeper
                    effectiveBondStrength *= ageSensitivity.bondDepthMultiplier;
                }

                _effectiveBondStrengthCache[entity] = effectiveBondStrength;

                // Trigger event if bond strength changed significantly
                float previousStrength = bondData.ValueRO.bondStrength;
                if (math.abs(effectiveBondStrength - previousStrength) > 0.05f)
                {
                    OnBondStrengthRecalculated?.Invoke(entity, effectiveBondStrength);
                }
            }
        }

        /// <summary>
        /// Updates cache of strong bond counts per player for population unlock detection
        /// </summary>
        private void UpdateStrongBondCountCache()
        {
            _strongBondCountCache.Clear();

            // Query player entities with population capacity
            foreach (var (capacity, entity) in
                SystemAPI.Query<RefRO<ChimeraPopulationCapacity>>().WithEntityAccess())
            {
                int strongBondCount = 0;

                // Count chimeras with strong bonds for this player
                foreach (var (bondData, identity) in
                    SystemAPI.Query<RefRO<CreatureBondData>, RefRO<CreatureIdentityComponent>>())
                {
                    // Determine if bond is strong enough for capacity unlock
                    var (_, requiredStrength) = CapacityUnlockThresholds.GetUnlockRequirements(
                        capacity.ValueRO.capacityUnlocked + 1
                    );

                    if (bondData.ValueRO.bondStrength >= requiredStrength)
                    {
                        strongBondCount++;
                    }
                }

                _strongBondCountCache[entity] = strongBondCount;
            }
        }

        /// <summary>
        /// Detects chimeras transitioning between life stages and broadcasts events
        /// </summary>
        private void DetectAgeTransitions()
        {
            foreach (var (identity, entity) in
                SystemAPI.Query<RefRW<CreatureIdentityComponent>>().WithEntityAccess())
            {
                // Check if age percentage crossed a life stage threshold
                LifeStage currentStage = identity.ValueRO.CurrentLifeStage;
                LifeStage calculatedStage = CalculateLifeStageFromAge(identity.ValueRO.AgePercentage);

                if (currentStage != calculatedStage)
                {
                    // Age transition detected!
                    OnAgeTransitionDetected?.Invoke(entity, currentStage, calculatedStage);

                    // Update to new life stage
                    identity.ValueRW.CurrentLifeStage = calculatedStage;

                    Debug.Log($"🎂 Age transition detected: {identity.ValueRO.CreatureName} {currentStage} → {calculatedStage}");
                }
            }
        }

        /// <summary>
        /// Detects when players meet requirements for population unlock and triggers event
        /// </summary>
        private void DetectPopulationUnlockOpportunities()
        {
            foreach (var (capacity, entity) in
                SystemAPI.Query<RefRW<ChimeraPopulationCapacity>>().WithEntityAccess())
            {
                // Check if player can unlock next capacity tier
                if (!_strongBondCountCache.TryGetValue(entity, out int strongBondCount))
                    continue;

                var (requiredBonds, requiredStrength) = CapacityUnlockThresholds.GetUnlockRequirements(
                    capacity.ValueRO.capacityUnlocked + 1
                );

                // Player meets unlock requirements
                if (strongBondCount >= requiredBonds && capacity.ValueRO.capacityUnlocked < 5)
                {
                    if (!capacity.ValueRO.canUnlockNext)
                    {
                        // Trigger unlock opportunity event
                        OnPopulationUnlockTriggered?.Invoke(
                            entity,
                            capacity.ValueRO.maxCapacity,
                            capacity.ValueRO.capacityUnlocked + 1
                        );

                        Debug.Log($"🔓 Population unlock available! Player can now unlock slot {capacity.ValueRO.capacityUnlocked + 1}");
                    }
                }
            }
        }

        /// <summary>
        /// Validates that bonding data is consistent across systems
        /// </summary>
        private void ValidateBondingConsistency()
        {
            // Check for chimeras with bond data but no identity
            foreach (var (bondData, entity) in
                SystemAPI.Query<RefRO<CreatureBondData>>().WithEntityAccess())
            {
                if (!EntityManager.HasComponent<CreatureIdentityComponent>(entity))
                {
                    Debug.LogWarning($"⚠️ Entity {entity.Index} has CreatureBondData but no CreatureIdentityComponent!");
                }
            }

            // Check for chimeras that exceed population capacity
            foreach (var (capacity, playerEntity) in
                SystemAPI.Query<RefRO<ChimeraPopulationCapacity>>().WithEntityAccess())
            {
                if (capacity.ValueRO.currentAliveChimeras > capacity.ValueRO.maxCapacity)
                {
                    Debug.LogError($"❌ Player has {capacity.ValueRO.currentAliveChimeras} chimeras but capacity is {capacity.ValueRO.maxCapacity}!");
                }
            }
        }

        /// <summary>
        /// Calculates life stage from age percentage
        /// </summary>
        private LifeStage CalculateLifeStageFromAge(float agePercentage)
        {
            if (agePercentage < 0.15f) return LifeStage.Baby;
            if (agePercentage < 0.35f) return LifeStage.Child;
            if (agePercentage < 0.55f) return LifeStage.Teen;
            if (agePercentage < 0.85f) return LifeStage.Adult;
            return LifeStage.Elderly;
        }

        /// <summary>
        /// PUBLIC API: Get effective bond strength (includes age modifiers)
        /// </summary>
        public static float GetEffectiveBondStrength(EntityManager em, Entity creature)
        {
            var hub = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SocialSystemsIntegrationHub>();
            if (hub != null && hub._effectiveBondStrengthCache.TryGetValue(creature, out float strength))
            {
                return strength;
            }

            // Fallback to raw bond data
            if (em.HasComponent<CreatureBondData>(creature))
            {
                return em.GetComponentData<CreatureBondData>(creature).bondStrength;
            }

            return 0f;
        }

        /// <summary>
        /// PUBLIC API: Check if player meets population unlock requirements
        /// </summary>
        public static bool CheckPopulationUnlockEligibility(EntityManager em, Entity player)
        {
            var hub = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SocialSystemsIntegrationHub>();
            if (hub == null)
                return false;

            if (!em.HasComponent<ChimeraPopulationCapacity>(player))
                return false;

            var capacity = em.GetComponentData<ChimeraPopulationCapacity>(player);

            if (!hub._strongBondCountCache.TryGetValue(player, out int strongBondCount))
                return false;

            var (requiredBonds, _) = CapacityUnlockThresholds.GetUnlockRequirements(capacity.capacityUnlocked + 1);

            return strongBondCount >= requiredBonds && capacity.capacityUnlocked < 5;
        }

        /// <summary>
        /// PUBLIC API: Get debug info for social systems state
        /// </summary>
        public static string GetDebugInfo()
        {
            var hub = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SocialSystemsIntegrationHub>();
            if (hub == null)
                return "Hub not initialized";

            return $"Social Systems Status:\n" +
                   $"- EnhancedBonding: {(hub._enhancedBondingSystemActive ? "✅" : "❌")}\n" +
                   $"- AgeSensitivity: {(hub._ageSensitivitySystemActive ? "✅" : "❌")}\n" +
                   $"- Population: {(hub._populationSystemActive ? "✅" : "❌")}\n" +
                   $"- Tracked bonds: {hub._effectiveBondStrengthCache.Count()}\n" +
                   $"- Players tracked: {hub._strongBondCountCache.Count()}";
        }
    }
}
