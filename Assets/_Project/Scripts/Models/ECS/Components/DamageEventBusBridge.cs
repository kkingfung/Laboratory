using Unity.Entities;
using System;

public class DamageEventBusBridge : MonoBehaviour
{
    public static event Action<DamageTakenEvent> OnDamageTaken;

    private Entity _busEntity;
    private EntityManager _entityManager;

    private void Start()
    {
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        _busEntity = DamageEventBus.Create(_entityManager);
    }

    private void Update()
    {
        if (!_entityManager.Exists(_busEntity)) return;

        var buffer = _entityManager.GetBuffer<DamageTakenEventBufferElement>(_busEntity);

        if (buffer.Length == 0) return;

        var events = buffer.ToNativeArray(Unity.Collections.Allocator.Temp);

        foreach (var evt in events)
        {
            OnDamageTaken?.Invoke(evt.Value);
        }

        buffer.Clear();

        events.Dispose();
    }
}
