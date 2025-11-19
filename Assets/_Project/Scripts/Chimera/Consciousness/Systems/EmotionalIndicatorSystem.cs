using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Consciousness.Core
{
    /// <summary>
    /// EMOTIONAL INDICATOR SYSTEM
    ///
    /// Updates visual emotional feedback for chimeras
    /// Shows age-appropriate emotions based on personality, bonds, and experiences
    ///
    /// Responsibilities:
    /// - Calculate current emotional state from personality + context
    /// - Update emotional icons/emojis for UI display
    /// - Track emotional transitions and stability
    /// - Age-appropriate emotional depth
    /// - Integrate with bond strength and recent interactions
    ///
    /// Design Philosophy:
    /// "Every chimera wears their heart on their sleeve (age-appropriately)"
    /// - Babies: Simple, frequent mood changes
    /// - Teens: Volatile, dramatic emotions
    /// - Adults: Nuanced, stable moods
    /// - Elderly: Serene, profound emotions
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    // NOTE: UpdateAfter(AgeSensitivitySystem) removed to avoid circular dependency with Social assembly
    public partial class EmotionalIndicatorSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        // Update frequencies (different ages update at different rates)
        private const float BABY_UPDATE_INTERVAL = 2.0f;      // Fast mood changes
        private const float CHILD_UPDATE_INTERVAL = 5.0f;     // Moderate changes
        private const float TEEN_UPDATE_INTERVAL = 3.0f;      // Volatile changes
        private const float ADULT_UPDATE_INTERVAL = 10.0f;    // Stable moods
        private const float ELDERLY_UPDATE_INTERVAL = 15.0f;  // Very stable, serene

        // Emotional context decay rates
        private const float POSITIVE_DECAY = 0.1f;  // Positive feelings fade slowly
        private const float NEGATIVE_DECAY = 0.05f; // Negative feelings linger (especially for adults/elderly)

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            Debug.Log("Emotional Indicator System initialized - chimeras show their feelings!");
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Initialize emotional indicators for creatures that need them
            InitializeEmotionalIndicators();

            // Update age-based emotional ranges
            UpdateAgeEmotionalRanges(deltaTime);

            // Update emotional context (decay recent experiences)
            UpdateEmotionalContext(deltaTime);

            // Calculate and update emotional states
            UpdateEmotionalStates(deltaTime, currentTime);

            // Clean up old context entries
            CleanupExpiredContext(currentTime);
        }

        /// <summary>
        /// Initializes emotional indicator components for creatures that have personality
        /// </summary>
        private void InitializeEmotionalIndicators()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (personality, identity, entity) in
                SystemAPI.Query<RefRO<CreaturePersonality>, RefRO<CreatureIdentityComponent>>()
                .WithEntityAccess()
                .WithNone<EmotionalIndicatorComponent>())
            {
                ecb.AddComponent(entity, new EmotionalIndicatorComponent
                {
                    currentIcon = EmotionalIcon.Neutral,
                    currentEmoji = "😐",
                    emotionDescription = "Calm and observing",
                    emotionalIntensity = 0.5f,
                    timeSinceLastChange = 0f,
                    previousIcon = EmotionalIcon.Neutral,
                    isFluctuating = false,
                    isSerene = identity.ValueRO.CurrentLifeStage == LifeStage.Elderly,
                    displayPriority = 1.0f
                });

                // Add context buffer for tracking experiences
                ecb.AddBuffer<EmotionalContext>(entity);

                // Add age emotional range
                ecb.AddComponent(entity, new AgeEmotionalRange
                {
                    lifeStage = identity.ValueRO.CurrentLifeStage,
                    canFeelSimpleEmotions = true,
                    canFeelComplexEmotions = identity.ValueRO.CurrentLifeStage >= LifeStage.Teen,
                    canFeelProfoundEmotions = identity.ValueRO.CurrentLifeStage == LifeStage.Elderly,
                    emotionalNuance = GetEmotionalNuanceForAge(identity.ValueRO.CurrentLifeStage),
                    moodStability = GetMoodStabilityForAge(identity.ValueRO.CurrentLifeStage),
                    transitionSpeed = GetTransitionSpeedForAge(identity.ValueRO.CurrentLifeStage),
                    showsPlayfulness = identity.ValueRO.CurrentLifeStage <= LifeStage.Child,
                    showsWisdom = identity.ValueRO.CurrentLifeStage == LifeStage.Elderly,
                    showsAngst = identity.ValueRO.CurrentLifeStage == LifeStage.Teen
                });
            }
        }

        /// <summary>
        /// Updates age-based emotional ranges when chimeras age up
        /// </summary>
        private void UpdateAgeEmotionalRanges(float deltaTime)
        {
            foreach (var (identity, emotionalRange, entity) in
                SystemAPI.Query<RefRO<CreatureIdentityComponent>, RefRW<AgeEmotionalRange>>().WithEntityAccess())
            {
                var currentStage = identity.ValueRO.CurrentLifeStage;

                if (emotionalRange.ValueRO.lifeStage != currentStage)
                {
                    // Age transition - update emotional capabilities
                    emotionalRange.ValueRW.lifeStage = currentStage;
                    emotionalRange.ValueRW.canFeelSimpleEmotions = true;
                    emotionalRange.ValueRW.canFeelComplexEmotions = currentStage >= LifeStage.Teen;
                    emotionalRange.ValueRW.canFeelProfoundEmotions = currentStage == LifeStage.Elderly;
                    emotionalRange.ValueRW.emotionalNuance = GetEmotionalNuanceForAge(currentStage);
                    emotionalRange.ValueRW.moodStability = GetMoodStabilityForAge(currentStage);
                    emotionalRange.ValueRW.transitionSpeed = GetTransitionSpeedForAge(currentStage);
                    emotionalRange.ValueRW.showsPlayfulness = currentStage <= LifeStage.Child;
                    emotionalRange.ValueRW.showsWisdom = currentStage == LifeStage.Elderly;
                    emotionalRange.ValueRW.showsAngst = currentStage == LifeStage.Teen;

                    Debug.Log($"Emotional range updated: {currentStage} - " +
                             $"Nuance: {emotionalRange.ValueRO.emotionalNuance:F2}, " +
                             $"Stability: {emotionalRange.ValueRO.moodStability:F2}");
                }
            }
        }

        /// <summary>
        /// Decays emotional context over time (memories fade)
        /// </summary>
        private void UpdateEmotionalContext(float deltaTime)
        {
            // Use Job to modify DynamicBuffer in parallel
            Entities
                .ForEach((DynamicBuffer<EmotionalContext> contextBuffer) =>
                {
                    for (int i = 0; i < contextBuffer.Length; i++)
                    {
                        var context = contextBuffer[i];

                        // Decay intensity over time
                        context.intensity = math.max(0f, context.intensity - (context.decayRate * deltaTime));

                        contextBuffer[i] = context;
                    }
                })
                .Schedule();

            this.CompleteDependency();
        }

        /// <summary>
        /// Updates emotional states based on personality, bonds, and context
        /// </summary>
        private void UpdateEmotionalStates(float deltaTime, float currentTime)
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (personality, identity, indicator, emotionalRange, contextBuffer, entity) in
                SystemAPI.Query<RefRO<CreaturePersonality>, RefRO<CreatureIdentityComponent>,
                    RefRW<EmotionalIndicatorComponent>, RefRO<AgeEmotionalRange>, DynamicBuffer<EmotionalContext>>()
                .WithEntityAccess())
            {
                // Update based on age-appropriate interval
                float updateInterval = GetUpdateIntervalForAge(identity.ValueRO.CurrentLifeStage);
                indicator.ValueRW.timeSinceLastChange += deltaTime;

                if (indicator.ValueRO.timeSinceLastChange < updateInterval)
                    continue;

                // Get bond strength from personality (already tracked there)
                float bondStrength = personality.ValueRO.PlayerBondStrength;

                // Analyze recent context
                bool hasRecentPositive = HasRecentContext(contextBuffer, isPositive: true, currentTime);
                bool hasRecentNegative = HasRecentContext(contextBuffer, isPositive: false, currentTime);

                // Determine appropriate emotional icon
                var previousIcon = indicator.ValueRO.currentIcon;
                var newIcon = EmotionalIconMapper.DetermineIcon(
                    personality.ValueRO,
                    identity.ValueRO.CurrentLifeStage,
                    bondStrength,
                    hasRecentPositive,
                    hasRecentNegative
                );

                // Apply age-based filtering (can this age feel this emotion?)
                newIcon = FilterEmotionByAge(newIcon, emotionalRange.ValueRO);

                // Update if changed
                if (newIcon != previousIcon)
                {
                    indicator.ValueRW.previousIcon = previousIcon;
                    indicator.ValueRW.currentIcon = newIcon;
                    indicator.ValueRW.currentEmoji = EmotionalIconMapper.GetEmoji(newIcon);
                    indicator.ValueRW.emotionDescription = EmotionalIconMapper.GetDescription(newIcon);
                    indicator.ValueRW.timeSinceLastChange = 0f;

                    // Calculate intensity based on personality state
                    indicator.ValueRW.emotionalIntensity = CalculateEmotionalIntensity(
                        personality.ValueRO,
                        newIcon,
                        bondStrength
                    );

                    // Update fluctuation/serenity flags
                    indicator.ValueRW.isFluctuating = identity.ValueRO.CurrentLifeStage == LifeStage.Teen &&
                                                      personality.ValueRO.StressLevel > 0.5f;
                    indicator.ValueRW.isSerene = identity.ValueRO.CurrentLifeStage == LifeStage.Elderly &&
                                                bondStrength > 0.7f;

                    // Emit transition event
                    var transitionEvent = EntityManager.CreateEntity();
                    ecb.AddComponent(transitionEvent, new EmotionalTransitionEvent
                    {
                        creature = entity,
                        previousEmotion = previousIcon,
                        newEmotion = newIcon,
                        currentAge = identity.ValueRO.CurrentLifeStage,
                        transitionIntensity = indicator.ValueRO.emotionalIntensity,
                        timestamp = currentTime,
                        reason = DetermineTransitionReason(hasRecentPositive, hasRecentNegative, bondStrength)
                    });

                    Debug.Log($"Emotional transition: {previousIcon} → {newIcon} " +
                             $"({identity.ValueRO.CurrentLifeStage}) - {indicator.ValueRO.emotionDescription}");
                }
            }
        }

        /// <summary>
        /// Removes expired emotional context entries
        /// </summary>
        private void CleanupExpiredContext(float currentTime)
        {
            foreach (var contextBuffer in SystemAPI.Query<DynamicBuffer<EmotionalContext>>())
            {
                for (int i = contextBuffer.Length - 1; i >= 0; i--)
                {
                    if (contextBuffer[i].intensity <= 0f ||
                        (currentTime - contextBuffer[i].timestamp) > 300f) // 5 minutes max
                    {
                        contextBuffer.RemoveAt(i);
                    }
                }
            }
        }

        // Helper methods

        private float GetEmotionalNuanceForAge(LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Baby => 0.2f,      // Simple emotions
                LifeStage.Child => 0.4f,     // Developing nuance
                LifeStage.Teen => 0.6f,      // Complex but volatile
                LifeStage.Adult => 0.8f,     // Nuanced emotions
                LifeStage.Elderly => 1.0f,   // Profound emotional depth
                _ => 0.5f
            };
        }

        private float GetMoodStabilityForAge(LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Baby => 0.3f,      // Moods change quickly
                LifeStage.Child => 0.5f,     // Moderately stable
                LifeStage.Teen => 0.2f,      // Volatile teen emotions
                LifeStage.Adult => 0.8f,     // Stable moods
                LifeStage.Elderly => 0.95f,  // Very stable, serene
                _ => 0.5f
            };
        }

        private float GetTransitionSpeedForAge(LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Baby => 2.0f,      // Fast transitions
                LifeStage.Child => 1.5f,     // Quick changes
                LifeStage.Teen => 1.8f,      // Dramatic swings
                LifeStage.Adult => 0.8f,     // Slow, gradual
                LifeStage.Elderly => 0.5f,   // Very gradual
                _ => 1.0f
            };
        }

        private float GetUpdateIntervalForAge(LifeStage stage)
        {
            return stage switch
            {
                LifeStage.Baby => BABY_UPDATE_INTERVAL,
                LifeStage.Child => CHILD_UPDATE_INTERVAL,
                LifeStage.Teen => TEEN_UPDATE_INTERVAL,
                LifeStage.Adult => ADULT_UPDATE_INTERVAL,
                LifeStage.Elderly => ELDERLY_UPDATE_INTERVAL,
                _ => ADULT_UPDATE_INTERVAL
            };
        }

        private bool HasRecentContext(DynamicBuffer<EmotionalContext> buffer, bool isPositive, float currentTime)
        {
            float recentThreshold = 60f; // Last minute

            foreach (var context in buffer)
            {
                if ((currentTime - context.timestamp) > recentThreshold)
                    continue;

                bool contextIsPositive = IsPositiveTrigger(context.triggerType);
                if (contextIsPositive == isPositive && context.intensity > 0.3f)
                    return true;
            }

            return false;
        }

        private bool IsPositiveTrigger(EmotionalTrigger trigger)
        {
            return trigger switch
            {
                EmotionalTrigger.PlayerInteraction => true,
                EmotionalTrigger.ReceivedGift => true,
                EmotionalTrigger.PlayedTogether => true,
                EmotionalTrigger.FedFavoriteFood => true,
                EmotionalTrigger.WonActivity => true,
                EmotionalTrigger.MadeNewFriend => true,
                EmotionalTrigger.ExploredTogether => true,
                EmotionalTrigger.Praised => true,
                _ => false
            };
        }

        private EmotionalIcon FilterEmotionByAge(EmotionalIcon icon, AgeEmotionalRange range)
        {
            // Profound emotions only for elderly
            if (!range.canFeelProfoundEmotions)
            {
                if (icon >= EmotionalIcon.Wise && icon <= EmotionalIcon.Bittersweet)
                    return EmotionalIcon.Happy; // Fallback to simple emotion
            }

            // Complex emotions only for teen+
            if (!range.canFeelComplexEmotions)
            {
                if (icon >= EmotionalIcon.Frustrated && icon <= EmotionalIcon.Proud)
                    return EmotionalIcon.Happy; // Fallback to simple emotion
            }

            return icon;
        }

        private float CalculateEmotionalIntensity(CreaturePersonality personality, EmotionalIcon icon, float bondStrength)
        {
            // Base intensity from personality traits
            float baseIntensity = (personality.HappinessLevel + (1f - personality.StressLevel)) / 2f;

            // Boost intensity for strong emotions
            if (icon == EmotionalIcon.Loving || icon == EmotionalIcon.Devoted || icon == EmotionalIcon.Fulfilled)
                baseIntensity = math.min(1f, baseIntensity + (bondStrength * 0.5f));

            // Reduce intensity for neutral states
            if (icon == EmotionalIcon.Neutral || icon == EmotionalIcon.Serene)
                baseIntensity *= 0.6f;

            return math.clamp(baseIntensity, 0f, 1f);
        }

        private FixedString64Bytes DetermineTransitionReason(bool hasPositive, bool hasNegative, float bondStrength)
        {
            if (hasPositive && bondStrength > 0.7f)
                return "Bond strengthened";
            if (hasPositive)
                return "Positive interaction";
            if (hasNegative && bondStrength < 0.4f)
                return "Feeling neglected";
            if (hasNegative)
                return "Recent upset";
            if (bondStrength > 0.8f)
                return "Deep bond";
            if (bondStrength < 0.3f)
                return "Weak bond";

            return "Natural change";
        }
    }
}
