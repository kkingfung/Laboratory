using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Laboratory.Models.ECS.Components;
using Laboratory.Gameplay.Combat;

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
        private void ProcessSingleDamageEvent(DamageTakenEvent damageEvent)
        {
            if (!EntityManager.HasComponent<Unity.Netcode.NetworkObject>(damageEvent.TargetEntity))
                return;

            var networkObject = EntityManager.GetComponentObject<Unity.Netcode.NetworkObject>(damageEvent.TargetEntity);
            var gameObject = networkObject.gameObject;

            TriggerDamageIndicator(damageEvent, gameObject);
        }

        /// <summary>
        /// Triggers visual damage indicator and associated feedback.
        /// </summary>
        /// <param name="damageEvent">The damage event data</param>
        /// <param name="targetGameObject">The target GameObject</param>
        private void TriggerDamageIndicator(DamageTakenEvent damageEvent, GameObject targetGameObject)
        {
            var damageIndicatorUI = GameObject.FindObjectOfType<DamageIndicatorUI>();
            damageIndicatorUI?.SpawnIndicator(
                damageEvent.SourcePosition,
                damageEvent.DamageAmount,
                damageEvent.DamageType,
                playSound: true,
                vibrate: true);
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
