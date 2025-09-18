using UnityEngine;
using Unity.Entities;
using Laboratory.Models;
using Laboratory.Core.Spawning;
using SpawnPointData = Laboratory.Core.Spawning.SpawnPointData;
using SpawnPointSafety = Laboratory.Core.Spawning.SpawnPointSafety;

namespace Laboratory.Gameplay.Spawning
{
    /// <summary>
    /// MonoBehaviour component for easily setting up spawn points in the Unity editor.
    /// Automatically registers spawn points with the ECS spawn point system.
    /// </summary>
    public class SpawnPointAuthoring : MonoBehaviour
    {
        #region Fields

        [Header("Spawn Point Configuration")]
        [SerializeField] private int teamId = 0;
        [SerializeField] private int priority = 1;
        [SerializeField] private float safetyRadius = 10f;
        [SerializeField] private float cooldownDuration = 5f;
        [SerializeField] private bool isActive = true;

        [Header("Visual Settings")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = Color.green;

        private Entity _spawnPointEntity;
        private bool _isRegistered = false;

        #endregion

        #region Properties

        /// <summary>
        /// Team that can use this spawn point (0 = any team).
        /// </summary>
        public int TeamId
        {
            get => teamId;
            set
            {
                teamId = value;
                UpdateSpawnPointData();
            }
        }

        /// <summary>
        /// Priority of this spawn point (higher = more preferred).
        /// </summary>
        public int Priority
        {
            get => priority;
            set
            {
                priority = Mathf.Max(0, value);
                UpdateSpawnPointData();
            }
        }

        /// <summary>
        /// Radius to check for enemies around spawn point.
        /// </summary>
        public float SafetyRadius
        {
            get => safetyRadius;
            set
            {
                safetyRadius = Mathf.Max(0f, value);
                UpdateSpawnPointData();
            }
        }

        /// <summary>
        /// Whether this spawn point is currently active/usable.
        /// </summary>
        public bool IsActive
        {
            get => isActive;
            set
            {
                isActive = value;
                UpdateSpawnPointData();
            }
        }

        /// <summary>
        /// Gets whether this spawn point is registered with the ECS system.
        /// </summary>
        public bool IsRegistered => _isRegistered;

        /// <summary>
        /// Gets the ECS entity representing this spawn point.
        /// </summary>
        public Entity SpawnPointEntity => _spawnPointEntity;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Register spawn point when component starts.
        /// </summary>
        private void Start()
        {
            RegisterSpawnPoint();
        }

        /// <summary>
        /// Update spawn point position if transform changes.
        /// </summary>
        private void Update()
        {
            if (_isRegistered && transform.hasChanged)
            {
                UpdateSpawnPointPosition();
                transform.hasChanged = false;
            }
        }

        /// <summary>
        /// Unregister spawn point when component is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            UnregisterSpawnPoint();
        }

        /// <summary>
        /// Draw gizmos for spawn point visualization.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            // Draw spawn point position
            Gizmos.color = isActive ? gizmoColor : Color.gray;
            Gizmos.DrawWireSphere(transform.position, 1f);
            
            // Draw safety radius
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, safetyRadius);
            
            // Draw team indicator
            if (teamId > 0)
            {
                Gizmos.color = GetTeamColor(teamId);
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
            }
        }

        /// <summary>
        /// Draw gizmos when selected for better visibility.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;

            // Highlight selected spawn point
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1.2f);
            
            // Show safety radius more prominently
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Gizmos.DrawSphere(transform.position, safetyRadius);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually register this spawn point with the ECS system.
        /// </summary>
        public void RegisterSpawnPoint()
        {
            if (_isRegistered) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            // Create spawn point entity directly
            var entityManager = world.EntityManager;
            _spawnPointEntity = entityManager.CreateEntity();
            
            // Add SpawnPointTag
            entityManager.AddComponentData(_spawnPointEntity, new Laboratory.Core.Spawning.SpawnPointTag());
            
            // Add SpawnPointData
            entityManager.AddComponentData(_spawnPointEntity, new SpawnPointData
            {
                SpawnPointId = GetInstanceID(),
                TeamId = teamId,
                Priority = priority,
                IsActive = isActive,
                IsSafe = true,
                LastUsedTime = 0f,
                CooldownDuration = cooldownDuration
            });
            
            // Add SpawnPointSafety
            entityManager.AddComponentData(_spawnPointEntity, new SpawnPointSafety
            {
                SafetyRadius = safetyRadius,
                CheckInterval = 1f,
                LastCheckedTime = 0f
            });
            
            // Add transform components
            entityManager.AddComponentData(_spawnPointEntity, new Unity.Transforms.LocalToWorld
            {
                Value = Unity.Mathematics.float4x4.TRS(
                    transform.position,
                    transform.rotation,
                    new Unity.Mathematics.float3(1f, 1f, 1f)
                )
            });

            _isRegistered = true;
            Debug.Log($"SpawnPointAuthoring: Registered spawn point at {transform.position} for team {teamId}");
        }

        /// <summary>
        /// Manually unregister this spawn point from the ECS system.
        /// </summary>
        public void UnregisterSpawnPoint()
        {
            if (!_isRegistered) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.EntityManager.Exists(_spawnPointEntity))
            {
                world.EntityManager.DestroyEntity(_spawnPointEntity);
                Debug.Log($"SpawnPointAuthoring: Unregistered spawn point at {transform.position}");
            }

            _isRegistered = false;
            _spawnPointEntity = Entity.Null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates spawn point data in the ECS system.
        /// </summary>
        private void UpdateSpawnPointData()
        {
            if (!_isRegistered) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            if (!entityManager.Exists(_spawnPointEntity)) return;

            if (entityManager.HasComponent<SpawnPointData>(_spawnPointEntity))
            {
                var spawnData = entityManager.GetComponentData<SpawnPointData>(_spawnPointEntity);
                spawnData.TeamId = teamId;
                spawnData.Priority = priority;
                spawnData.CooldownDuration = cooldownDuration;
                spawnData.IsActive = isActive;
                entityManager.SetComponentData(_spawnPointEntity, spawnData);
            }

            if (entityManager.HasComponent<SpawnPointSafety>(_spawnPointEntity))
            {
                var safetyData = entityManager.GetComponentData<SpawnPointSafety>(_spawnPointEntity);
                safetyData.SafetyRadius = safetyRadius;
                entityManager.SetComponentData(_spawnPointEntity, safetyData);
            }
        }

        /// <summary>
        /// Updates spawn point position in the ECS system.
        /// </summary>
        private void UpdateSpawnPointPosition()
        {
            if (!_isRegistered) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            if (!entityManager.Exists(_spawnPointEntity)) return;

            if (entityManager.HasComponent<Unity.Transforms.LocalToWorld>(_spawnPointEntity))
            {
                var localToWorld = entityManager.GetComponentData<Unity.Transforms.LocalToWorld>(_spawnPointEntity);
                localToWorld.Value = Unity.Mathematics.float4x4.TRS(
                    transform.position,
                    transform.rotation,
                    new Unity.Mathematics.float3(1f, 1f, 1f)
                );
                entityManager.SetComponentData(_spawnPointEntity, localToWorld);
            }
        }

        /// <summary>
        /// Gets a color for team visualization.
        /// </summary>
        /// <param name="team">Team ID</param>
        /// <returns>Color representing the team</returns>
        private Color GetTeamColor(int team)
        {
            return team switch
            {
                1 => Color.red,
                2 => Color.blue,
                3 => Color.green,
                4 => Color.yellow,
                _ => Color.white
            };
        }

        #endregion

        #region Editor Support

        /// <summary>
        /// Validates configuration in the editor.
        /// </summary>
        private void OnValidate()
        {
            priority = Mathf.Max(0, priority);
            safetyRadius = Mathf.Max(0f, safetyRadius);
            cooldownDuration = Mathf.Max(0f, cooldownDuration);
        }

        #endregion
    }
}
