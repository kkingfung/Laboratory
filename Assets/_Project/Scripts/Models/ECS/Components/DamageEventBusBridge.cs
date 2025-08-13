using UnityEngine;
using Unity.Entities;
using System;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Bridges DOTS damage events to MonoBehaviour event system.
    /// </summary>
    public class DamageEventBusBridge : MonoBehaviour
    {
        #region Fields

        public static event Action<DamageTakenEvent>? OnDamageTaken;

        private Entity _busEntity;
        private EntityManager _entityManager;

        #endregion

        #region Unity Override Methods

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

        #endregion

        #region Public Methods

        // No public methods currently.

        #endregion

        #region Private Methods

        // No private methods currently.

        #endregion

        #region Inner Classes, Enums

        // No inner classes or enums currently.

        #endregion
    }
}
