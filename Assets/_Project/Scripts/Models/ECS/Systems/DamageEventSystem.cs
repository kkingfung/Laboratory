using UnityEngine;

namespace Game.Combat
{
    public class DamageEventSystem : MonoBehaviour
    {
        public void ApplyDamage(DamageEvent damageEvent)
        {
            var targetHealth = NetworkManager.Singleton.SpawnManager
                .GetPlayerNetworkObject(damageEvent.TargetId)
                ?.GetComponent<HealthComponent>();

            if (targetHealth == null) return;

            targetHealth.ApplyDamage(damageEvent.DamageAmount);

            // Publish for other systems (e.g., UI, sound)
            MessageBus.Publish(damageEvent);

            if (targetHealth.CurrentHealth <= 0)
            {
                var deathEvent = new DeathEvent
                {
                    VictimId = damageEvent.TargetId,
                    KillerId = damageEvent.AttackerId
                };
                MessageBus.Publish(deathEvent);
            }
        }
    }
}
