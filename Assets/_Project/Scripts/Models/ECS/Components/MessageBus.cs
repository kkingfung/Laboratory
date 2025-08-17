using System;
using UnityEngine;

#nullable enable

namespace Laboratory.Models.ECS.Components
{
    #region Event Structs

    /// <summary>
    /// Represents a damage event between clients.
    /// </summary>
    public readonly struct DamageEvent
    {
        public readonly ulong TargetClientId;
        public readonly ulong AttackerClientId;
        public readonly float DamageAmount;
        public readonly Vector3 HitDirection;

        public DamageEvent(ulong targetClientId, ulong attackerClientId, float damageAmount, Vector3 hitDirection)
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
        public static event Action<DamageEvent>? OnDamage;
        public static event Action<DeathEvent>? OnDeath;

        public static void Publish(DamageEvent evt) => OnDamage?.Invoke(evt);
        public static void Publish(DeathEvent evt) => OnDeath?.Invoke(evt);
    }

    #endregion
}
