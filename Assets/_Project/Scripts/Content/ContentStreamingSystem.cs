using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Content
{
    /// <summary>
    /// Content streaming system for progressive asset delivery.
    /// Prioritizes loading based on distance, visibility, and importance.
    /// Enables seamless open-world experiences with minimal loading screens.
    /// </summary>
    public class ContentStreamingSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Streaming Settings")]
        [SerializeField] private float streamingRadius = 200f;
        [SerializeField] private float unloadRadius = 300f;
        [SerializeField] private float updateInterval = 1f;

        [Header("Priority Settings")]
        [SerializeField] private int highPriorityDistance = 50;
        [SerializeField] private int mediumPriorityDistance = 100;
        [SerializeField] private int maxConcurrentLoads = 5;

        [Header("LOD Settings")]
        [SerializeField] private bool useLODStreaming = true;
        [SerializeField] private float lodDistance1 = 50f; // High detail
        [SerializeField] private float lodDistance2 = 100f; // Medium detail
        [SerializeField] private float lodDistance3 = 200f; // Low detail

        [Header("Performance")]
        [SerializeField] private int maxLoadBudgetPerFrame = 3;
        [SerializeField] private float memoryBudgetMB = 512f;
        [SerializeField] private bool useAdaptiveStreaming = true;

        #endregion

        #region Private Fields

        private static ContentStreamingSystem _instance;

        // Streaming zones
        private readonly Dictionary<string, StreamingZone> _zones = new Dictionary<string, StreamingZone>();
        private readonly Dictionary<string, StreamedContent> _streamedContent = new Dictionary<string, StreamedContent>();

        // Loading queue
        private readonly PriorityQueue<StreamingRequest> _loadQueue = new PriorityQueue<StreamingRequest>();
        private readonly List<StreamingRequest> _activeLoads = new List<StreamingRequest>();

        // Player tracking
        private Transform _playerTransform;
        private Vector3 _lastPlayerPosition;

        // State
        private float _lastUpdateTime = 0f;
        private int _loadsThisFrame = 0;

        // Statistics
        private int _totalZonesLoaded = 0;
        private int _totalZonesUnloaded = 0;
        private int _totalContentStreamed = 0;
        private float _currentMemoryUsageMB = 0f;

        // Events
        public event Action<string> OnZoneLoaded;
        public event Action<string> OnZoneUnloaded;
        public event Action<string, float> OnContentLoadProgress;
        public event Action<string> OnContentLoaded;
        public event Action<string> OnContentUnloaded;

        #endregion

        #region Properties

        public static ContentStreamingSystem Instance => _instance;
        public int LoadedZoneCount => _zones.Count(z => z.Value.isLoaded);
        public int StreamedContentCount => _streamedContent.Count;
        public float CurrentMemoryUsage => _currentMemoryUsageMB;
        public int QueuedLoadCount => _loadQueue.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                FindPlayer();
                return;
            }

            _loadsThisFrame = 0;

            // Update streaming based on player position
            if (Time.time - _lastUpdateTime >= updateInterval)
            {
                UpdateStreaming();
                _lastUpdateTime = Time.time;
            }

            // Process load queue
            ProcessLoadQueue();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[ContentStreamingSystem] Initializing...");
            Debug.Log("[ContentStreamingSystem] Initialized");
        }

        private void FindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                _lastPlayerPosition = _playerTransform.position;
                Debug.Log("[ContentStreamingSystem] Player found");
            }
        }

        #endregion

        #region Zone Management

        /// <summary>
        /// Register a streaming zone.
        /// </summary>
        public void RegisterZone(string zoneId, Vector3 center, float radius, string[] bundleNames, StreamingPriority priority = StreamingPriority.Medium)
        {
            if (_zones.ContainsKey(zoneId))
            {
                Debug.LogWarning($"[ContentStreamingSystem] Zone already registered: {zoneId}");
                return;
            }

            _zones[zoneId] = new StreamingZone
            {
                zoneId = zoneId,
                center = center,
                radius = radius,
                bundleNames = bundleNames,
                priority = priority,
                isLoaded = false
            };

            Debug.Log($"[ContentStreamingSystem] Zone registered: {zoneId} at {center}");
        }

        /// <summary>
        /// Unregister a streaming zone.
        /// </summary>
        public void UnregisterZone(string zoneId)
        {
            if (_zones.TryGetValue(zoneId, out var zone))
            {
                if (zone.isLoaded)
                {
                    UnloadZone(zoneId);
                }

                _zones.Remove(zoneId);
                Debug.Log($"[ContentStreamingSystem] Zone unregistered: {zoneId}");
            }
        }

        #endregion

        #region Streaming Update

        private void UpdateStreaming()
        {
            if (_playerTransform == null) return;

            Vector3 playerPosition = _playerTransform.position;

            // Check zones for loading/unloading
            foreach (var zone in _zones.Values)
            {
                float distance = Vector3.Distance(playerPosition, zone.center);

                // Load zone if in range
                if (!zone.isLoaded && distance <= streamingRadius)
                {
                    QueueZoneLoad(zone, distance);
                }
                // Unload zone if out of range
                else if (zone.isLoaded && distance > unloadRadius)
                {
                    UnloadZone(zone.zoneId);
                }
            }

            // Update LODs if enabled
            if (useLODStreaming)
            {
                UpdateLODs(playerPosition);
            }

            _lastPlayerPosition = playerPosition;
        }

        private void QueueZoneLoad(StreamingZone zone, float distance)
        {
            // Calculate priority based on distance and zone priority
            int priority = CalculatePriority(distance, zone.priority);

            var request = new StreamingRequest
            {
                zoneId = zone.zoneId,
                zone = zone,
                priority = priority,
                distance = distance
            };

            _loadQueue.Enqueue(request, priority);
        }

        private int CalculatePriority(float distance, StreamingPriority zonePriority)
        {
            int basePriority = 0;

            // Distance-based priority
            if (distance < highPriorityDistance)
                basePriority = 100;
            else if (distance < mediumPriorityDistance)
                basePriority = 50;
            else
                basePriority = 10;

            // Zone priority modifier
            int priorityModifier = zonePriority switch
            {
                StreamingPriority.Critical => 50,
                StreamingPriority.High => 25,
                StreamingPriority.Medium => 0,
                StreamingPriority.Low => -25,
                _ => 0
            };

            return basePriority + priorityModifier;
        }

        #endregion

        #region Loading

        private void ProcessLoadQueue()
        {
            // Check memory budget
            if (useAdaptiveStreaming && _currentMemoryUsageMB >= memoryBudgetMB)
            {
                Debug.LogWarning($"[ContentStreamingSystem] Memory budget exceeded: {_currentMemoryUsageMB:F1}MB / {memoryBudgetMB}MB");
                return;
            }

            // Check load budget
            if (_loadsThisFrame >= maxLoadBudgetPerFrame)
            {
                return;
            }

            // Check concurrent load limit
            if (_activeLoads.Count >= maxConcurrentLoads)
            {
                return;
            }

            // Start next load
            if (_loadQueue.TryDequeue(out var request))
            {
                _activeLoads.Add(request);
                StartCoroutine(LoadZoneCoroutine(request));
                _loadsThisFrame++;
            }
        }

        private IEnumerator LoadZoneCoroutine(StreamingRequest request)
        {
            var zone = request.zone;

            // Load all bundles for this zone
            foreach (var bundleName in zone.bundleNames)
            {
                bool loadComplete = false;
                bool loadSuccess = false;

                AssetBundleManager.Instance.LoadBundle(bundleName,
                    bundle =>
                    {
                        loadSuccess = true;
                        loadComplete = true;

                        // Track memory usage (approximate)
                        _currentMemoryUsageMB += 10f; // Placeholder - should calculate actual size
                    },
                    error =>
                    {
                        loadComplete = true;
                        Debug.LogError($"[ContentStreamingSystem] Failed to load bundle {bundleName}: {error}");
                    });

                // Wait for load
                while (!loadComplete)
                {
                    yield return null;
                }

                if (!loadSuccess)
                {
                    _activeLoads.Remove(request);
                    yield break;
                }
            }

            // Mark zone as loaded
            zone.isLoaded = true;
            zone.loadTime = Time.time;

            _totalZonesLoaded++;
            _activeLoads.Remove(request);

            OnZoneLoaded?.Invoke(zone.zoneId);

            Debug.Log($"[ContentStreamingSystem] Zone loaded: {zone.zoneId}");
        }

        #endregion

        #region Unloading

        private void UnloadZone(string zoneId)
        {
            if (!_zones.TryGetValue(zoneId, out var zone))
                return;

            if (!zone.isLoaded)
                return;

            // Unload all bundles
            foreach (var bundleName in zone.bundleNames)
            {
                AssetBundleManager.Instance.UnloadBundle(bundleName, false);

                // Update memory usage
                _currentMemoryUsageMB -= 10f; // Placeholder
                _currentMemoryUsageMB = Mathf.Max(0, _currentMemoryUsageMB);
            }

            zone.isLoaded = false;
            _totalZonesUnloaded++;

            OnZoneUnloaded?.Invoke(zoneId);

            Debug.Log($"[ContentStreamingSystem] Zone unloaded: {zoneId}");
        }

        #endregion

        #region LOD Streaming

        private void UpdateLODs(Vector3 playerPosition)
        {
            foreach (var content in _streamedContent.Values)
            {
                if (content.gameObject == null) continue;

                float distance = Vector3.Distance(playerPosition, content.position);

                LODLevel newLOD = CalculateLODLevel(distance);

                if (newLOD != content.currentLOD)
                {
                    ApplyLOD(content, newLOD);
                }
            }
        }

        private LODLevel CalculateLODLevel(float distance)
        {
            if (distance < lodDistance1)
                return LODLevel.High;
            else if (distance < lodDistance2)
                return LODLevel.Medium;
            else if (distance < lodDistance3)
                return LODLevel.Low;
            else
                return LODLevel.Culled;
        }

        private void ApplyLOD(StreamedContent content, LODLevel newLOD)
        {
            content.currentLOD = newLOD;

            // Apply LOD to game object (implement based on your LOD system)
            if (content.gameObject != null)
            {
                var lodGroup = content.gameObject.GetComponent<LODGroup>();
                if (lodGroup != null)
                {
                    // LODGroup handles this automatically
                }
                else
                {
                    // Manual LOD switching
                    content.gameObject.SetActive(newLOD != LODLevel.Culled);
                }
            }
        }

        #endregion

        #region Content Streaming

        /// <summary>
        /// Stream content (GameObject) at position.
        /// </summary>
        public void StreamContent(string contentId, string bundleName, string assetName, Vector3 position, Action<GameObject> onSuccess = null)
        {
            if (_streamedContent.ContainsKey(contentId))
            {
                Debug.LogWarning($"[ContentStreamingSystem] Content already streamed: {contentId}");
                return;
            }

            StartCoroutine(StreamContentCoroutine(contentId, bundleName, assetName, position, onSuccess));
        }

        private IEnumerator StreamContentCoroutine(string contentId, string bundleName, string assetName, Vector3 position, Action<GameObject> onSuccess)
        {
            bool loadComplete = false;
            GameObject loadedObject = null;

            AssetBundleManager.Instance.LoadAssetAsync<GameObject>(bundleName, assetName,
                prefab =>
                {
                    loadedObject = Instantiate(prefab, position, Quaternion.identity);
                    loadComplete = true;
                },
                error =>
                {
                    loadComplete = true;
                    Debug.LogError($"[ContentStreamingSystem] Failed to stream content: {error}");
                });

            while (!loadComplete)
            {
                yield return null;
            }

            if (loadedObject != null)
            {
                _streamedContent[contentId] = new StreamedContent
                {
                    contentId = contentId,
                    gameObject = loadedObject,
                    position = position,
                    bundleName = bundleName,
                    currentLOD = LODLevel.High
                };

                _totalContentStreamed++;

                OnContentLoaded?.Invoke(contentId);
                onSuccess?.Invoke(loadedObject);

                Debug.Log($"[ContentStreamingSystem] Content streamed: {contentId}");
            }
        }

        /// <summary>
        /// Unstream content.
        /// </summary>
        public void UnstreamContent(string contentId)
        {
            if (_streamedContent.TryGetValue(contentId, out var content))
            {
                if (content.gameObject != null)
                {
                    Destroy(content.gameObject);
                }

                _streamedContent.Remove(contentId);

                OnContentUnloaded?.Invoke(contentId);

                Debug.Log($"[ContentStreamingSystem] Content unstreamed: {contentId}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get streaming statistics.
        /// </summary>
        public StreamingStats GetStats()
        {
            return new StreamingStats
            {
                registeredZones = _zones.Count,
                loadedZones = LoadedZoneCount,
                totalZonesLoaded = _totalZonesLoaded,
                totalZonesUnloaded = _totalZonesUnloaded,
                streamedContent = _streamedContent.Count,
                totalContentStreamed = _totalContentStreamed,
                queuedLoads = _loadQueue.Count,
                activeLoads = _activeLoads.Count,
                memoryUsageMB = _currentMemoryUsageMB,
                memoryBudgetMB = memoryBudgetMB
            };
        }

        /// <summary>
        /// Set player transform for streaming.
        /// </summary>
        public void SetPlayerTransform(Transform player)
        {
            _playerTransform = player;
            _lastPlayerPosition = player.position;
            Debug.Log("[ContentStreamingSystem] Player transform set");
        }

        /// <summary>
        /// Force update streaming immediately.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateStreaming();
        }

        #endregion

        #region Context Menu

        [ContextMenu("Force Update")]
        private void ForceUpdateMenu()
        {
            ForceUpdate();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Content Streaming Statistics ===\n" +
                      $"Registered Zones: {stats.registeredZones}\n" +
                      $"Loaded Zones: {stats.loadedZones}\n" +
                      $"Total Zones Loaded: {stats.totalZonesLoaded}\n" +
                      $"Total Zones Unloaded: {stats.totalZonesUnloaded}\n" +
                      $"Streamed Content: {stats.streamedContent}\n" +
                      $"Total Content Streamed: {stats.totalContentStreamed}\n" +
                      $"Queued Loads: {stats.queuedLoads}\n" +
                      $"Active Loads: {stats.activeLoads}\n" +
                      $"Memory Usage: {stats.memoryUsageMB:F1}MB / {stats.memoryBudgetMB}MB");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Streaming zone definition.
    /// </summary>
    [Serializable]
    public class StreamingZone
    {
        public string zoneId;
        public Vector3 center;
        public float radius;
        public string[] bundleNames;
        public StreamingPriority priority;
        public bool isLoaded;
        public float loadTime;
    }

    /// <summary>
    /// Streamed content instance.
    /// </summary>
    [Serializable]
    public class StreamedContent
    {
        public string contentId;
        public GameObject gameObject;
        public Vector3 position;
        public string bundleName;
        public LODLevel currentLOD;
    }

    /// <summary>
    /// Streaming request.
    /// </summary>
    public class StreamingRequest
    {
        public string zoneId;
        public StreamingZone zone;
        public int priority;
        public float distance;
    }

    /// <summary>
    /// Streaming statistics.
    /// </summary>
    [Serializable]
    public struct StreamingStats
    {
        public int registeredZones;
        public int loadedZones;
        public int totalZonesLoaded;
        public int totalZonesUnloaded;
        public int streamedContent;
        public int totalContentStreamed;
        public int queuedLoads;
        public int activeLoads;
        public float memoryUsageMB;
        public float memoryBudgetMB;
    }

    /// <summary>
    /// Streaming priority levels.
    /// </summary>
    public enum StreamingPriority
    {
        Critical,
        High,
        Medium,
        Low
    }

    /// <summary>
    /// LOD levels.
    /// </summary>
    public enum LODLevel
    {
        High,
        Medium,
        Low,
        Culled
    }

    /// <summary>
    /// Simple priority queue implementation.
    /// </summary>
    public class PriorityQueue<T>
    {
        private List<(T item, int priority)> _elements = new List<(T, int)>();

        public int Count => _elements.Count;

        public void Enqueue(T item, int priority)
        {
            _elements.Add((item, priority));
            _elements.Sort((a, b) => b.priority.CompareTo(a.priority)); // Higher priority first
        }

        public bool TryDequeue(out T item)
        {
            if (_elements.Count > 0)
            {
                item = _elements[0].item;
                _elements.RemoveAt(0);
                return true;
            }

            item = default;
            return false;
        }
    }

    #endregion
}
