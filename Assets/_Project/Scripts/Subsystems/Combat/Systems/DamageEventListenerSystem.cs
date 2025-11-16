using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Laboratory.Models.ECS.Components;
using Laboratory.Core.Health;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using CoreDamageType = Laboratory.Core.Enums.DamageType;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Listens for damage events from the event bus and triggers appropriate UI and audio responses.
    /// Processes damage indicators, sound effects, and haptic feedback.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class DamageEventListenerSystem : SystemBase
    {
        #region Fields

        /// <summary>
        /// Entity representing the damage event bus
        /// </summary>
        private Entity _eventBusEntity;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initializes the event bus entity for damage events.
        /// </summary>
        protected override void OnCreate()
        {
            _eventBusEntity = DamageEventBus.Create(EntityManager);
        }

        /// <summary>
        /// Processes all pending damage events from the event bus.
        /// </summary>
        protected override void OnUpdate()
        {
            if (!EntityManager.Exists(_eventBusEntity)) 
                return;

            var buffer = EntityManager.GetBuffer<DamageTakenEventBufferElement>(_eventBusEntity);

            if (buffer.Length == 0) 
                return;

            ProcessDamageEvents(buffer);
            ClearProcessedEvents(buffer);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes all damage events in the buffer and triggers appropriate responses.
        /// </summary>
        /// <param name="buffer">Buffer containing damage events to process</param>
        private void ProcessDamageEvents(DynamicBuffer<DamageTakenEventBufferElement> buffer)
        {
            var events = buffer.ToNativeArray(Allocator.Temp);

            foreach (var evt in events)
            {
                ProcessSingleDamageEvent(evt.Value);
            }

            events.Dispose();
        }

        /// <summary>
        /// Processes a single damage event and triggers UI/audio responses.
        /// </summary>
        /// <param name="damageEvent">The damage event to process</param>
        private void ProcessSingleDamageEvent(Laboratory.Models.ECS.Components.DamageTakenEvent damageEvent)
        {
            // The DamageSource entity should contain the source information
            var sourceEntity = damageEvent.DamageSource;
            
            // Check if the source entity still exists
            if (!EntityManager.Exists(sourceEntity))
                return;

            // For the target, we need to find it differently since TargetEntityId doesn't exist
            // We'll use the damage position and other available data
            TriggerDamageIndicator(damageEvent);
        }

        /// <summary>
        /// Triggers visual damage indicator by publishing an event to the unified event bus.
        /// This decouples the system from direct UI dependencies.
        /// </summary>
        /// <param name="damageEvent">The damage event data</param>
        private void TriggerDamageIndicator(Laboratory.Models.ECS.Components.DamageTakenEvent damageEvent)
        {
            // Convert ECS damage type to Core damage type
            var coreDamageType = ConvertToCoreDamageType(damageEvent.DamageType);
            
            // Create and publish damage indicator event to unified event bus
            var damageIndicatorEvent = new DamageIndicatorRequestedEvent(
                sourcePosition: damageEvent.DamagePosition,
                damageAmount: (int?)damageEvent.DamageAmount, // Convert float to int?
                damageType: coreDamageType,
                playSound: true,
                triggerVibration: true,
                targetClientId: 0 // Default to 0 since we don't have client ID info
            );
            
            EventBusService.Instance.Publish(damageIndicatorEvent);
        }

        /// <summary>
        /// Converts ECS DamageType to Core DamageType for event publishing.
        /// </summary>
        /// <param name="ecsDamageType">ECS damage type</param>
        /// <returns>Corresponding Core damage type</returns>
        private CoreDamageType ConvertToCoreDamageType(Laboratory.Models.ECS.Components.DamageType ecsDamageType)
        {
            return ecsDamageType switch
            {
                Laboratory.Models.ECS.Components.DamageType.Critical => CoreDamageType.Critical,
                Laboratory.Models.ECS.Components.DamageType.Fire => CoreDamageType.Fire,
                Laboratory.Models.ECS.Components.DamageType.Ice => CoreDamageType.Ice,
                _ => CoreDamageType.Normal
            };
        }

        /// <summary>
        /// Clears all processed events from the buffer.
        /// </summary>
        /// <param name="buffer">Buffer to clear</param>
        private void ClearProcessedEvents(DynamicBuffer<DamageTakenEventBufferElement> buffer)
        {
            buffer.Clear();
        }

        #endregion
    }
}
