using System;
using Unity.Entities;

public struct DamageEvent
{
    public Entity Target;
    public float Amount;
    public Entity Source;
}

public static class DamageEventBus
{
    public static event Action<DamageEvent> OnDamage;

    public static void Raise(DamageEvent damageEvent)
    {
        OnDamage?.Invoke(damageEvent);
    }
}
