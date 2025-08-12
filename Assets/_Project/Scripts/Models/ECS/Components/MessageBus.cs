// MessageBus.cs
using System;

public readonly struct DamageEvent
{
    public readonly ulong TargetClientId;
    public readonly ulong AttackerClientId;
    public readonly float DamageAmount;
    public readonly UnityEngine.Vector3 HitDirection;

    public DamageEvent(ulong targetClientId, ulong attackerClientId, float damageAmount, UnityEngine.Vector3 hitDirection)
    {
        TargetClientId = targetClientId;
        AttackerClientId = attackerClientId;
        DamageAmount = damageAmount;
        HitDirection = hitDirection;
    }
}

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

public static class MessageBus
{
    public static event Action<DamageEvent>? OnDamage;
    public static event Action<DeathEvent>? OnDeath;

    public static void Publish(DamageEvent evt) => OnDamage?.Invoke(evt);
    public static void Publish(DeathEvent evt) => OnDeath?.Invoke(evt);
}
