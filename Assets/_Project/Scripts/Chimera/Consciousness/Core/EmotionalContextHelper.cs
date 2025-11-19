using Unity.Entities;
using Unity.Collections;

namespace Laboratory.Chimera.Consciousness.Core
{
    /// <summary>
    /// EMOTIONAL CONTEXT HELPER
    ///
    /// Utility for adding emotional context to chimeras from other systems
    /// Makes it easy to trigger emotional responses
    ///
    /// Usage Example:
    /// - Player feeds chimera → AddContext(entity, EmotionalTrigger.FedFavoriteFood, 0.8f)
    /// - Win activity together → AddContext(entity, EmotionalTrigger.WonActivity, 1.0f)
    /// - Ignore for too long → AddContext(entity, EmotionalTrigger.Ignored, 0.6f)
    ///
    /// Integration:
    /// - Call from activity systems, bonding systems, etc.
    /// - EmotionalIndicatorSystem will process context and update emotions
    /// </summary>
    public static class EmotionalContextHelper
    {
        /// <summary>
        /// Adds emotional context to a creature
        /// </summary>
        /// <param name="entityManager">EntityManager to use</param>
        /// <param name="entity">Creature entity</param>
        /// <param name="trigger">What happened</param>
        /// <param name="intensity">How much it affects them (0.0-1.0)</param>
        /// <param name="currentTime">Current game time</param>
        /// <param name="source">Optional description of source</param>
        public static void AddEmotionalContext(
            EntityManager entityManager,
            Entity entity,
            EmotionalTrigger trigger,
            float intensity,
            float currentTime,
            string source = "")
        {
            if (!entityManager.Exists(entity))
                return;

            // Ensure entity has context buffer
            if (!entityManager.HasBuffer<EmotionalContext>(entity))
            {
                entityManager.AddBuffer<EmotionalContext>(entity);
            }

            var contextBuffer = entityManager.GetBuffer<EmotionalContext>(entity);

            // Determine decay rate based on trigger type
            float decayRate = IsPositiveTrigger(trigger) ? 0.1f : 0.05f;

            // Add new context entry
            contextBuffer.Add(new EmotionalContext
            {
                triggerType = trigger,
                intensity = intensity,
                timestamp = currentTime,
                decayRate = decayRate,
                source = string.IsNullOrEmpty(source) ? trigger.ToString() : source
            });

            // Keep buffer size reasonable (max 20 entries)
            if (contextBuffer.Length > 20)
            {
                contextBuffer.RemoveAt(0); // Remove oldest
            }
        }

        /// <summary>
        /// Adds emotional context using an EntityCommandBuffer (for job-safe operations)
        /// </summary>
        public static void AddEmotionalContextDeferred(
            EntityCommandBuffer ecb,
            Entity entity,
            EmotionalTrigger trigger,
            float intensity,
            float currentTime,
            string source = "")
        {
            // Create a temporary entity to store the context addition request
            var requestEntity = ecb.CreateEntity();
            ecb.AddComponent(requestEntity, new EmotionalContextAddRequest
            {
                targetEntity = entity,
                trigger = trigger,
                intensity = intensity,
                timestamp = currentTime,
                source = string.IsNullOrEmpty(source) ? trigger.ToString() : source
            });
        }

        /// <summary>
        /// Quick helpers for common emotional triggers
        /// </summary>
        public static class QuickTriggers
        {
            public static void PlayerInteracted(EntityManager em, Entity entity, float quality, float currentTime)
            {
                AddEmotionalContext(em, entity, EmotionalTrigger.PlayerInteraction, quality, currentTime, "Player interaction");
            }

            public static void ReceivedGift(EntityManager em, Entity entity, float howMuchTheyLikeIt, float currentTime)
            {
                AddEmotionalContext(em, entity, EmotionalTrigger.ReceivedGift, howMuchTheyLikeIt, currentTime, "Received gift");
            }

            public static void WonTogether(EntityManager em, Entity entity, float currentTime)
            {
                AddEmotionalContext(em, entity, EmotionalTrigger.WonActivity, 1.0f, currentTime, "Won activity together");
            }

            public static void FeltIgnored(EntityManager em, Entity entity, float howLongIgnored, float currentTime)
            {
                float intensity = UnityEngine.Mathf.Min(1.0f, howLongIgnored / 86400f); // Max after 1 day
                AddEmotionalContext(em, entity, EmotionalTrigger.Ignored, intensity, currentTime, "Feeling ignored");
            }

            public static void GotScared(EntityManager em, Entity entity, float scariness, float currentTime)
            {
                AddEmotionalContext(em, entity, EmotionalTrigger.Scared, scariness, currentTime, "Scared by event");
            }

            public static void PlayedTogether(EntityManager em, Entity entity, float funLevel, float currentTime)
            {
                AddEmotionalContext(em, entity, EmotionalTrigger.PlayedTogether, funLevel, currentTime, "Played together");
            }

            public static void LostActivity(EntityManager em, Entity entity, float currentTime)
            {
                AddEmotionalContext(em, entity, EmotionalTrigger.Lost, 0.6f, currentTime, "Lost activity");
            }

            public static void FedFavoriteFood(EntityManager em, Entity entity, float currentTime)
            {
                AddEmotionalContext(em, entity, EmotionalTrigger.FedFavoriteFood, 0.9f, currentTime, "Fed favorite food");
            }
        }

        private static bool IsPositiveTrigger(EmotionalTrigger trigger)
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
    }

    /// <summary>
    /// Request to add emotional context (for deferred/job-safe operations)
    /// </summary>
    public struct EmotionalContextAddRequest : IComponentData
    {
        public Entity targetEntity;
        public EmotionalTrigger trigger;
        public float intensity;
        public float timestamp;
        public FixedString32Bytes source;
    }

    /// <summary>
    /// System that processes deferred emotional context add requests
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EmotionalIndicatorSystem))]
    public partial class EmotionalContextProcessorSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            _ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = _ecbSystem.CreateCommandBuffer();

            foreach (var (request, entity) in
                SystemAPI.Query<RefRO<EmotionalContextAddRequest>>().WithEntityAccess())
            {
                var targetEntity = request.ValueRO.targetEntity;

                if (!EntityManager.Exists(targetEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                // Ensure buffer exists
                if (!EntityManager.HasBuffer<EmotionalContext>(targetEntity))
                {
                    ecb.AddBuffer<EmotionalContext>(targetEntity);
                }

                var contextBuffer = EntityManager.GetBuffer<EmotionalContext>(targetEntity);

                // Determine decay rate
                bool isPositive = request.ValueRO.trigger switch
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

                float decayRate = isPositive ? 0.1f : 0.05f;

                // Add context
                contextBuffer.Add(new EmotionalContext
                {
                    triggerType = request.ValueRO.trigger,
                    intensity = request.ValueRO.intensity,
                    timestamp = request.ValueRO.timestamp,
                    decayRate = decayRate,
                    source = request.ValueRO.source
                });

                // Keep buffer size reasonable
                if (contextBuffer.Length > 20)
                {
                    contextBuffer.RemoveAt(0);
                }

                ecb.DestroyEntity(entity);
            }
        }
    }
}
