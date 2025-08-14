using System.Collections.Concurrent;
using Unity.Entities;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Manages bidirectional mapping between network entity IDs and ECS entities.
    /// Provides thread-safe operations for synchronizing network updates with ECS world.
    /// </summary>
    public class NetworkEntityMapper
    {
        #region Fields

        /// <summary>Thread-safe mapping from network entity IDs to ECS entities.</summary>
        private readonly ConcurrentDictionary<int, Entity> _networkToEntity = new();

        /// <summary>Thread-safe reverse mapping from ECS entities to network IDs.</summary>
        private readonly ConcurrentDictionary<Entity, int> _entityToNetwork = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a bidirectional mapping between network ID and ECS entity.
        /// </summary>
        /// <param name="networkId">Network-assigned entity ID.</param>
        /// <param name="entity">ECS Entity instance.</param>
        public void RegisterMapping(int networkId, Entity entity)
        {
            _networkToEntity[networkId] = entity;
            _entityToNetwork[entity] = networkId;
        }

        /// <summary>
        /// Removes mapping by network ID and clears reverse mapping.
        /// </summary>
        /// <param name="networkId">Network ID to remove.</param>
        /// <returns>True if mapping was found and removed; otherwise, false.</returns>
        public bool RemoveByNetworkId(int networkId)
        {
            if (_networkToEntity.TryRemove(networkId, out var entity))
            {
                _entityToNetwork.TryRemove(entity, out _);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes mapping by ECS entity and clears reverse mapping.
        /// </summary>
        /// <param name="entity">ECS entity to remove.</param>
        /// <returns>True if mapping was found and removed; otherwise, false.</returns>
        public bool RemoveByEntity(Entity entity)
        {
            if (_entityToNetwork.TryRemove(entity, out var networkId))
            {
                _networkToEntity.TryRemove(networkId, out _);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to retrieve ECS entity from network ID.
        /// </summary>
        /// <param name="networkId">Network ID to look up.</param>
        /// <param name="entity">Output ECS entity if found.</param>
        /// <returns>True if entity was found; otherwise, false.</returns>
        public bool TryGetEntity(int networkId, out Entity entity)
        {
            return _networkToEntity.TryGetValue(networkId, out entity);
        }

        /// <summary>
        /// Attempts to retrieve network ID from ECS entity.
        /// </summary>
        /// <param name="entity">ECS entity to look up.</param>
        /// <param name="networkId">Output network ID if found.</param>
        /// <returns>True if network ID was found; otherwise, false.</returns>
        public bool TryGetNetworkId(Entity entity, out int networkId)
        {
            return _entityToNetwork.TryGetValue(entity, out networkId);
        }

        /// <summary>
        /// Clears all mappings from both dictionaries.
        /// </summary>
        public void Clear()
        {
            _networkToEntity.Clear();
            _entityToNetwork.Clear();
        }

        /// <summary>
        /// Gets the current number of mapped entities.
        /// </summary>
        /// <returns>Count of currently mapped entities.</returns>
        public int Count => _networkToEntity.Count;

        #endregion
    }
}
