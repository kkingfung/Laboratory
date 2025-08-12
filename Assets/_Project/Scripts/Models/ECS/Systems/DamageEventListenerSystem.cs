using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class DamageEventListenerSystem : SystemBase
{
    private Entity _eventBusEntity;

    protected override void OnCreate()
    {
        _eventBusEntity = DamageEventBus.Create(EntityManager);
    }

    protected override void OnUpdate()
    {
        if (!EntityManager.Exists(_eventBusEntity)) return;

        var buffer = EntityManager.GetBuffer<DamageTakenEventBufferElement>(_eventBusEntity);

        // Early out if no events
        if (buffer.Length == 0) return;

        // Copy events to local array for iteration (to avoid modifying buffer during iteration)
        var events = buffer.ToNativeArray(Unity.Collections.Allocator.Temp);

        foreach (var evt in events)
        {
            var damageEvent = evt.Value;

            if (EntityManager.HasComponent<Unity.Netcode.NetworkObject>(damageEvent.TargetEntity))
            {
                var networkObject = EntityManager.GetComponentObject<Unity.Netcode.NetworkObject>(damageEvent.TargetEntity);
                var go = networkObject.gameObject;

                var damageIndicatorUI = GameObject.FindObjectOfType<DamageIndicatorUI>();
                damageIndicatorUI?.SpawnIndicator(
                    damageEvent.SourcePosition,
                    damageEvent.DamageAmount,
                    damageEvent.DamageType,
                    playSound: true,
                    vibrate: true);
            }

            // TODO: Trigger other effects (sounds, particles, gameplay triggers)
        }

        // Clear buffer after processing
        buffer.Clear();

        events.Dispose();
    }
}
