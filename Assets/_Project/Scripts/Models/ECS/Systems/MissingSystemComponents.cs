using Unity.Entities;
using UnityEngine;
using Laboratory.Core.Events;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Spawn point service for handling player and entity spawning
    /// </summary>
    public class SpawnPointService
    {
        public Vector3 SelectSpawnPoint(Entity entity, PlayerSpawnPreference preference)
        {
            // Default spawn logic - can be enhanced
            return Vector3.zero;
        }
        
        public Vector3 GetSafeSpawnPosition()
        {
            return Vector3.zero;
        }
    }
    
    /// <summary>
    /// Player spawn preference data structure
    /// </summary>
    [System.Serializable]
    public struct PlayerSpawnPreference
    {
        public Vector3 PreferredPosition;
        public float SearchRadius;
        public LayerMask ObstacleLayerMask;
        public bool RandomizeNearby;
        
        public static PlayerSpawnPreference Default => new PlayerSpawnPreference
        {
            PreferredPosition = Vector3.zero,
            SearchRadius = 10f,
            ObstacleLayerMask = -1,
            RandomizeNearby = true
        };
    }
    
    /// <summary>
    /// Player data component for ECS systems
    /// </summary>
    public struct PlayerData : IComponentData
    {
        public int PlayerID;
        public Vector3 SpawnPosition;
        public bool IsAlive;
        public float LastSpawnTime;
    }
}

namespace Laboratory.Models.ECS.Events
{
    /// <summary>
    /// Event bus implementation for ECS systems
    /// </summary>
    public partial class ECSEventBus : SystemBase
    {
        private IEventBus _eventBus;
        
        protected override void OnCreate()
        {
            _eventBus = new UnifiedEventBus();
        }
        
        protected override void OnUpdate()
        {
            // Process events - this overrides the abstract method correctly
            // No specific update logic needed for event processing
        }
        
        public void Publish<T>(T eventData) where T : class
        {
            _eventBus.Publish(eventData);
        }
        
        public void Subscribe<T>(System.Action<T> handler) where T : class
        {
            _eventBus.Subscribe(handler);
        }
    }
}
