using System;
using UnityEngine;

#nullable enable

namespace Laboratory.Models.ECS.Components
{
    #region Event Structs

    /// <summary>
    /// Represents a damage event between clients - renamed to avoid conflicts
    /// </summary>
    public readonly struct NetworkDamageEvent
    {
        public readonly ulong TargetClientId;
        public readonly ulong AttackerClientId;
        public readonly float DamageAmount;
        public readonly Vector3 HitDirection;

        public NetworkDamageEvent(ulong targetClientId, ulong attackerClientId, float damageAmount, Vector3 hitDirection)
        {
            TargetClientId = targetClientId;
            AttackerClientId = attackerClientId;
            DamageAmount = damageAmount;
            HitDirection = hitDirection;
        }
    }

    /// <summary>
    /// Represents a death event between clients.
    /// </summary>
    public readonly struct DeathEvent
    {
        public readonly ulong VictimClientId;
        public readonly ulong KillerClientId;

        public DeathEvent(ulong victimClientId, ulong killerClientId)
        {
            VictimClientId = victimClientId;
            KillerClientId = killerClientId;
        }
    }

    #endregion

    #region Message Bus

    /// <summary>
    /// Simple message bus for damage and death events.
    /// </summary>
    public static class MessageBus
    {
        public static event Action<NetworkDamageEvent>? OnDamageEvent;
        public static event Action<DeathEvent>? OnDeathEvent;
        
        // Legacy compatibility events
        public static event Action<DamageEvent>? OnDamage;
        public static event Action<DeathEvent>? OnDeath;

        public static void PublishDamage(NetworkDamageEvent damageEvent)
        {
            OnDamageEvent?.Invoke(damageEvent);
        }

        public static void PublishDeath(DeathEvent deathEvent)
        {
            OnDeathEvent?.Invoke(deathEvent);
            OnDeath?.Invoke(deathEvent);
        }
        
        /// <summary>
        /// Generic publish method for backwards compatibility
        /// </summary>
        public static void Publish<T>(T eventData)
        {
            if (eventData is NetworkDamageEvent damage)
            {
                PublishDamage(damage);
            }
            else if (eventData is DeathEvent death)
            {
                PublishDeath(death);
            }
            else if (eventData is DamageEvent legacyDamage)
            {
                // Trigger the legacy OnDamage event for compatibility
                OnDamage?.Invoke(legacyDamage);
            }
        }
    }

    #endregion
}
