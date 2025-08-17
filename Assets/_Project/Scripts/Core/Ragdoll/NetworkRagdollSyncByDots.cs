using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// ECS-based network synchronization system for ragdoll physics using Unity DOTS.
    /// Provides efficient network transmission of ragdoll state for multiplayer games.
    /// </summary>
    public class NetworkRagdollSyncByDots : MonoBehaviour
    {
        #region Fields

        [Header("Network Settings")]
        [SerializeField] private float _syncRate = 20f; // Hz
        [SerializeField] private float _positionThreshold = 0.01f;
        [SerializeField] private float _rotationThreshold = 0.1f;
        [SerializeField] private bool _interpolateMovement = true;
        [SerializeField] private float _interpolationDelay = 0.1f;

        [Header("Compression Settings")]
        [SerializeField] private bool _useCompression = true;
        [SerializeField] private float _positionPrecision = 0.001f;
        [SerializeField] private float _rotationPrecision = 0.01f;
        [SerializeField] private int _compressionLevel = 2;

        [Header("Performance")]
        [SerializeField] private int _maxSyncedBones = 32;
        [SerializeField] private bool _useLOD = true;
        [SerializeField] private float _lodDistance = 10f;
        [SerializeField] private bool _prioritizeVisibleBones = true;

        [Header("Debug")]
        [SerializeField] private bool _showSyncInfo = false;
        [SerializeField] private bool _logNetworkEvents = false;
        [SerializeField] private Color _syncGizmoColor = Color.blue;

        // Runtime state
        private EntityManager _entityManager;
        private World _defaultWorld;
        private bool _isInitialized = false;
        private float _lastSyncTime = 0f;
        private int _syncedBoneCount = 0;
        private bool _isServer = false;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the system has been initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Current number of synced bones
        /// </summary>
        public int SyncedBoneCount => _syncedBoneCount;

        /// <summary>
        /// Whether this is running on the server
        /// </summary>
        public bool IsServer => _isServer;

        /// <summary>
        /// Network synchronization rate in Hz
        /// </summary>
        public float SyncRate => _syncRate;

        /// <summary>
        /// Position threshold for triggering sync
        /// </summary>
        public float PositionThreshold => _positionThreshold;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeDotsSystem();
        }

        private void Start()
        {
            ValidateConfiguration();
            DetectNetworkRole();
        }

        private void Update()
        {
            if (_isInitialized)
            {
                UpdateNetworkSync();
            }
        }

        private void OnDestroy()
        {
            CleanupDotsSystem();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the network synchronization rate
        /// </summary>
        /// <param name="rate">Sync rate in Hz</param>
        public void SetSyncRate(float rate)
        {
            _syncRate = Mathf.Max(1f, rate);
        }

        /// <summary>
        /// Sets the position threshold for synchronization
        /// </summary>
        /// <param name="threshold">Position threshold value</param>
        public void SetPositionThreshold(float threshold)
        {
            _positionThreshold = Mathf.Max(0.001f, threshold);
        }

        /// <summary>
        /// Sets the rotation threshold for synchronization
        /// </summary>
        /// <param name="threshold">Rotation threshold value</param>
        public void SetRotationThreshold(float threshold)
        {
            _rotationThreshold = Mathf.Max(0.01f, threshold);
        }

        /// <summary>
        /// Enables or disables movement interpolation
        /// </summary>
        /// <param name="enabled">Whether interpolation should be enabled</param>
        public void SetInterpolationEnabled(bool enabled)
        {
            _interpolateMovement = enabled;
        }

        /// <summary>
        /// Sets the interpolation delay
        /// </summary>
        /// <param name="delay">Interpolation delay in seconds</param>
        public void SetInterpolationDelay(float delay)
        {
            _interpolationDelay = Mathf.Max(0f, delay);
        }

        /// <summary>
        /// Enables or disables compression
        /// </summary>
        /// <param name="enabled">Whether compression should be enabled</param>
        public void SetCompressionEnabled(bool enabled)
        {
            _useCompression = enabled;
        }

        /// <summary>
        /// Sets the compression level
        /// </summary>
        /// <param name="level">Compression level (1-5)</param>
        public void SetCompressionLevel(int level)
        {
            _compressionLevel = Mathf.Clamp(level, 1, 5);
        }

        /// <summary>
        /// Sets the maximum number of synced bones
        /// </summary>
        /// <param name="maxBones">Maximum bone count</param>
        public void SetMaxSyncedBones(int maxBones)
        {
            _maxSyncedBones = Mathf.Max(1, maxBones);
        }

        /// <summary>
        /// Enables or disables LOD system
        /// </summary>
        /// <param name="enabled">Whether LOD should be enabled</param>
        public void SetLODEnabled(bool enabled)
        {
            _useLOD = enabled;
        }

        /// <summary>
        /// Sets the LOD distance
        /// </summary>
        /// <param name="distance">LOD distance in units</param>
        public void SetLODDistance(float distance)
        {
            _lodDistance = Mathf.Max(1f, distance);
        }

        /// <summary>
        /// Forces a network synchronization update
        /// </summary>
        public void ForceSync()
        {
            if (_isInitialized)
            {
                PerformNetworkSync();
            }
        }

        #endregion

        #region Private Methods

        private void InitializeDotsSystem()
        {
            try
            {
                _defaultWorld = World.DefaultGameObjectInjectionWorld;
                if (_defaultWorld != null && _defaultWorld.IsCreated)
                {
                    _entityManager = _defaultWorld.EntityManager;
                    _isInitialized = true;
                    Debug.Log("NetworkRagdollSyncByDots: DOTS system initialized successfully");
                }
                else
                {
                    Debug.LogWarning("NetworkRagdollSyncByDots: Default world not available");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NetworkRagdollSyncByDots: Failed to initialize DOTS system: {e.Message}");
                _isInitialized = false;
            }
        }

        private void ValidateConfiguration()
        {
            if (_syncRate <= 0f)
            {
                Debug.LogWarning("NetworkRagdollSyncByDots: Sync rate should be greater than 0");
                _syncRate = 20f;
            }

            if (_positionThreshold <= 0f)
            {
                Debug.LogWarning("NetworkRagdollSyncByDots: Position threshold should be greater than 0");
                _positionThreshold = 0.01f;
            }

            if (_rotationThreshold <= 0f)
            {
                Debug.LogWarning("NetworkRagdollSyncByDots: Rotation threshold should be greater than 0");
                _rotationThreshold = 0.1f;
            }

            if (_maxSyncedBones <= 0)
            {
                Debug.LogWarning("NetworkRagdollSyncByDots: Max synced bones should be greater than 0");
                _maxSyncedBones = 32;
            }

            if (_compressionLevel < 1 || _compressionLevel > 5)
            {
                Debug.LogWarning("NetworkRagdollSyncByDots: Compression level should be between 1 and 5");
                _compressionLevel = 2;
            }
        }

        private void DetectNetworkRole()
        {
            // Detect if we're running on server or client
            // This is a simplified detection - in a real implementation you'd check your networking framework
            _isServer = Application.isEditor || !Application.isPlaying;
            
            if (_logNetworkEvents)
            {
                Debug.Log($"NetworkRagdollSyncByDots: Running as {(_isServer ? "Server" : "Client")}");
            }
        }

        private void UpdateNetworkSync()
        {
            if (Time.time - _lastSyncTime >= 1f / _syncRate)
            {
                PerformNetworkSync();
                _lastSyncTime = Time.time;
            }
        }

        private void PerformNetworkSync()
        {
            if (!_isInitialized) return;

            try
            {
                if (_isServer)
                {
                    // Server: Send ragdoll state to clients
                    SendRagdollState();
                }
                else
                {
                    // Client: Receive and apply ragdoll state
                    ReceiveRagdollState();
                }

                if (_logNetworkEvents)
                {
                    Debug.Log($"NetworkRagdollSyncByDots: Network sync performed at {Time.time}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"NetworkRagdollSyncByDots: Failed to perform network sync: {e.Message}");
            }
        }

        private void SendRagdollState()
        {
            if (_entityManager == null) return;

            // Create network message entity
            var networkMessage = _entityManager.CreateEntity();
            
            // Add network message components
            _entityManager.AddComponentData(networkMessage, new NetworkMessage
            {
                MessageType = NetworkMessageType.RagdollState,
                Timestamp = Time.time,
                EntityId = GetInstanceID()
            });

            // Add ragdoll state data
            var ragdollState = new RagdollStateData
            {
                Position = transform.position,
                Rotation = transform.rotation,
                BoneCount = _syncedBoneCount,
                SyncTimestamp = Time.time
            };

            _entityManager.AddComponentData(networkMessage, ragdollState);

            if (_logNetworkEvents)
            {
                Debug.Log($"NetworkRagdollSyncByDots: Sent ragdoll state for {_syncedBoneCount} bones");
            }
        }

        private void ReceiveRagdollState()
        {
            if (_entityManager == null) return;

            // Query for incoming ragdoll state messages
            var query = _entityManager.CreateEntityQuery(typeof(NetworkMessage), typeof(RagdollStateData));
            
            if (query.CalculateEntityCount() > 0)
            {
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                
                foreach (var entity in entities)
                {
                    var message = _entityManager.GetComponentData<NetworkMessage>(entity);
                    var ragdollState = _entityManager.GetComponentData<RagdollStateData>(entity);

                    if (message.MessageType == NetworkMessageType.RagdollState)
                    {
                        ApplyRagdollState(ragdollState);
                        
                        // Remove processed message
                        _entityManager.DestroyEntity(entity);
                    }
                }

                entities.Dispose();
            }
        }

        private void ApplyRagdollState(RagdollStateData state)
        {
            if (_interpolateMovement)
            {
                // Apply interpolated movement
                StartCoroutine(InterpolateToState(state));
            }
            else
            {
                // Apply immediate movement
                transform.position = state.Position;
                transform.rotation = state.Rotation;
            }

            _syncedBoneCount = state.BoneCount;

            if (_logNetworkEvents)
            {
                Debug.Log($"NetworkRagdollSyncByDots: Applied ragdoll state with {state.BoneCount} bones");
            }
        }

        private System.Collections.IEnumerator InterpolateToState(RagdollStateData targetState)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;
            float duration = _interpolationDelay;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                transform.position = Vector3.Lerp(startPosition, targetState.Position, t);
                transform.rotation = Quaternion.Slerp(startRotation, targetState.Rotation, t);

                yield return null;
            }

            // Ensure we reach the exact target
            transform.position = targetState.Position;
            transform.rotation = targetState.Rotation;
        }

        private void CleanupDotsSystem()
        {
            if (_isInitialized)
            {
                _isInitialized = false;
                _entityManager = null;
                _defaultWorld = null;
                Debug.Log("NetworkRagdollSyncByDots: DOTS system cleaned up");
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_showSyncInfo)
            {
                // Draw sync radius
                Gizmos.color = _syncGizmoColor;
                Gizmos.DrawWireSphere(transform.position, _lodDistance);

                // Draw sync info text
                if (_isInitialized)
                {
                    Gizmos.color = Color.white;
                    Vector3 textPosition = transform.position + Vector3.up * 2f;
                    // Note: Gizmos can't draw text, this is just for visualization
                }
            }
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Network message types for ragdoll synchronization
    /// </summary>
    public enum NetworkMessageType
    {
        RagdollState,
        RagdollUpdate,
        RagdollReset
    }

    /// <summary>
    /// Network message component for ECS
    /// </summary>
    public struct NetworkMessage : IComponentData
    {
        public NetworkMessageType MessageType;
        public float Timestamp;
        public int EntityId;
    }

    /// <summary>
    /// Ragdoll state data for network transmission
    /// </summary>
    public struct RagdollStateData : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public int BoneCount;
        public float SyncTimestamp;
    }

    #endregion
}
