using System.Collections.Concurrent;
using Unity.Entities;

namespace Infrastructure
{
    /// <summary>
    /// Manages mapping between network entity IDs and ECS entities.
    /// Used to synchronize network updates with ECS entities.
    /// </summary>
    public class NetworkEntityMapper
    {
        #region Fields

        // Thread-safe dictionary mapping network entity IDs to ECS Entities
        private readonly ConcurrentDictionary<int, Entity> _networkToEntity = new();

        // Optional reverse lookup (ECS entity to network ID)
        private readonly ConcurrentDictionary<Entity, int> _entityToNetwork = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Register mapping from network ID to ECS entity.
        /// </summary>
        /// <param name="networkId">Network-assigned entity ID.</param>
        /// <param name="entity">ECS Entity instance.</param>
        public void RegisterMapping(int networkId, Entity entity)
        {
            _networkToEntity[networkId] = entity;
            _entityToNetwork[entity] = networkId;
        }

        /// <summary>
        /// Remove mapping by network ID.
        /// </summary>
        /// <param name="networkId"></param>
        public void RemoveByNetworkId(int networkId)
        {
            if (_networkToEntity.TryRemove(networkId, out var entity))
            {
                _entityToNetwork.TryRemove(entity, out _);
            }
        }

        /// <summary>
        /// Remove mapping by ECS entity.
        /// </summary>
        /// <param name="entity"></param>
        public void RemoveByEntity(Entity entity)
        {
            if (_entityToNetwork.TryRemove(entity, out var networkId))
            {
                _networkToEntity.TryRemove(networkId, out _);
            }
        }

        /// <summary>
        /// Try get ECS entity from network ID.
        /// </summary>
        public bool TryGetEntity(int networkId, out Entity entity)
        {
            return _networkToEntity.TryGetValue(networkId, out entity);
        }

        /// <summary>
        /// Try get network ID from ECS entity.
        /// </summary>
        public bool TryGetNetworkId(Entity entity, out int networkId)
        {
            return _entityToNetwork.TryGetValue(entity, out networkId);
        }

        /// <summary>
        /// Clear all mappings.
        /// </summary>
        public void Clear()
        {
            _networkToEntity.Clear();
            _entityToNetwork.Clear();
        }

        #endregion
    }
}
