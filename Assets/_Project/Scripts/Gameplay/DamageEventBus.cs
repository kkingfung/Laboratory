using System;
using System.Collections.Generic;
using Unity.Entities;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Gameplay.Combat
{
    /// <summary>
    /// Publishes and subscribes to damage events for combat systems.
    /// Also provides ECS entity creation for DOTS integration.
    /// </summary>
    public class DamageEventBus
    {
        #region Fields

        private readonly List<Action<DamageEvent>> _subscribers = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Publishes a damage event to all subscribers.
        /// </summary>
        public void Publish(DamageEvent damageEvent)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber?.Invoke(damageEvent);
            }
        }

        /// <summary>
        /// Subscribes to damage events.
        /// </summary>
        public void Subscribe(Action<DamageEvent> handler)
        {
            if (!_subscribers.Contains(handler))
                _subscribers.Add(handler);
        }

        /// <summary>
        /// Unsubscribes from damage events.
        /// </summary>
        public void Unsubscribe(Action<DamageEvent> handler)
        {
            _subscribers.Remove(handler);
        }

        /// <summary>
        /// Creates an entity for handling damage events in DOTS.
        /// </summary>
        /// <param name="entityManager">The EntityManager to create the entity with</param>
        /// <returns>The created damage event bus entity</returns>
        public static Entity Create(EntityManager entityManager)
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddBuffer<DamageTakenEventBufferElement>(entity);
            return entity;
        }

        #endregion

        #region Inner Classes, Enums

        /// <summary>
        /// Represents a damage event.
        /// </summary>
        public readonly struct DamageEvent
        {
            public int SourceId { get; }
            public int TargetId { get; }
            public float Amount { get; }

            public DamageEvent(int sourceId, int targetId, float amount)
            {
                SourceId = sourceId;
                TargetId = targetId;
                Amount = amount;
            }
        }

        #endregion
    }
}
