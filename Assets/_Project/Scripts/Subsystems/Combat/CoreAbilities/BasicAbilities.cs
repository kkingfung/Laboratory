using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.DI;

namespace Laboratory.Subsystems.Combat.CoreAbilities
{
    /// <summary>
    /// Core basic abilities that all characters can perform
    /// </summary>
    public static class BasicAbilities
    {
        /// <summary>
        /// Basic attack ability
        /// </summary>
        public static void PerformBasicAttack(GameObject attacker, GameObject target, float damage)
        {
            if (attacker == null || target == null) return;

            // Get health component from target - use fully qualified name to avoid conflicts
            var healthComponent = target.GetComponent<Laboratory.Core.Health.Components.LocalHealthComponent>();
            if (healthComponent != null)
            {
                var damageRequest = new Laboratory.Core.Health.DamageRequest(damage, attacker);
                healthComponent.TakeDamage(damageRequest);
                
                // Fire attack event
                if (GlobalServiceProvider.IsInitialized)
                {
                    var eventBus = GlobalServiceProvider.Resolve<IEventBus>();
                    eventBus.Publish(new AttackPerformedEvent
                    {
                        Attacker = attacker,
                        Target = target,
                        Damage = damage,
                        AttackType = "Basic"
                    });
                }
            }
        }

        /// <summary>
        /// Basic block ability
        /// </summary>
        public static bool AttemptBlock(GameObject blocker, float incomingDamage, out float blockedAmount)
        {
            blockedAmount = 0f;
            
            if (blocker == null) return false;

            // Simple block logic - blocks 50% of incoming damage
            float blockEfficiency = 0.5f;
            blockedAmount = incomingDamage * blockEfficiency;

            // Fire block event
            if (GlobalServiceProvider.IsInitialized)
            {
                var eventBus = GlobalServiceProvider.Resolve<IEventBus>();
                eventBus.Publish(new BlockPerformedEvent
                {
                    Blocker = blocker,
                    IncomingDamage = incomingDamage,
                    BlockedAmount = blockedAmount
                });
            }

            return true;
        }

        /// <summary>
        /// Basic dodge ability
        /// </summary>
        public static bool AttemptDodge(GameObject dodger, Vector3 dodgeDirection, float dodgeForce = 10f)
        {
            if (dodger == null) return false;

            var rigidbody = dodger.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                // Apply dodge movement
                rigidbody.AddForce(dodgeDirection.normalized * dodgeForce, ForceMode.Impulse);

                // Fire dodge event
                if (GlobalServiceProvider.IsInitialized)
                {
                    var eventBus = GlobalServiceProvider.Resolve<IEventBus>();
                    eventBus.Publish(new DodgePerformedEvent
                    {
                        Dodger = dodger,
                        DodgeDirection = dodgeDirection,
                        DodgeForce = dodgeForce
                    });
                }

                return true;
            }

            return false;
        }
    }

    // Event definitions for basic abilities
    public class AttackPerformedEvent : BaseEvent
    {
        public GameObject Attacker { get; set; }
        public GameObject Target { get; set; }
        public float Damage { get; set; }
        public string AttackType { get; set; }
    }

    public class BlockPerformedEvent : BaseEvent
    {
        public GameObject Blocker { get; set; }
        public float IncomingDamage { get; set; }
        public float BlockedAmount { get; set; }
    }

    public class DodgePerformedEvent : BaseEvent
    {
        public GameObject Dodger { get; set; }
        public Vector3 DodgeDirection { get; set; }
        public float DodgeForce { get; set; }
    }
}
