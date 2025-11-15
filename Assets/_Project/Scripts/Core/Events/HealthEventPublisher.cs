using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events.Messages;
using Laboratory.Core.Enums;

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Implementation of IHealthEventPublisher that publishes actual health events
    /// This class breaks the circular dependency by implementing the interface in the Events assembly
    /// </summary>
    public class HealthEventPublisher : IHealthEventPublisher
    {
        private readonly IEventBus _eventBus;

        public HealthEventPublisher(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new System.ArgumentNullException(nameof(eventBus));
        }

        public void PublishDamageEvent(GameObject target, object source, float damage, int damageType, Vector3 direction)
        {
            var damageEvent = new Messages.DamageEvent(
                target,
                source,
                damage,
                (DamageType)damageType,
                direction
            );

            _eventBus?.Publish(damageEvent);
        }

        public void PublishDeathEvent(GameObject target, object source)
        {
            var deathEvent = new Messages.DeathEvent(target, source);
            _eventBus?.Publish(deathEvent);
        }
    }
}