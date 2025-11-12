using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Events;
using Laboratory.Core.Infrastructure;

namespace Laboratory.Subsystems.Spawning
{
    /// <summary>
    /// Unified spawning system for all game objects (players, enemies, items, etc.)
    /// </summary>
    public class UnifiedSpawningManager : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private SpawnConfig defaultSpawnConfig;
        [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        
        [Header("Runtime Settings")]
        [SerializeField] private bool enableSpawning = true;
        [SerializeField] private float spawnCooldown = 1f;
        
        private Dictionary<string, SpawnConfig> spawnConfigs = new Dictionary<string, SpawnConfig>();
        private Dictionary<string, List<GameObject>> spawnedObjectsByTag = new Dictionary<string, List<GameObject>>();
        private float lastSpawnTime;

        private void Awake()
        {
            // Initialize spawn configurations
            if (defaultSpawnConfig != null)
            {
                spawnConfigs["default"] = defaultSpawnConfig;
            }
        }

        private void Start()
        {
            // Initialize spawn points if none exist
            if (spawnPoints.Count == 0)
            {
                CreateDefaultSpawnPoints();
            }
        }

        /// <summary>
        /// Spawn an object at a specified spawn point
        /// </summary>
        public GameObject SpawnObject(GameObject prefab, string spawnPointTag = "default", string configKey = "default")
        {
            if (!enableSpawning || prefab == null) return null;
            
            // Check cooldown
            if (Time.time - lastSpawnTime < spawnCooldown) return null;

            // Find spawn point
            SpawnPoint spawnPoint = GetSpawnPoint(spawnPointTag);
            if (spawnPoint == null) return null;

            // Get spawn configuration
            SpawnConfig config = GetSpawnConfig(configKey);
            
            // Spawn the object
            GameObject spawnedObject = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);

            // Track spawned object by tag for performance-optimized cleanup
            TrackSpawnedObject(spawnedObject, spawnPointTag);

            // Apply spawn configuration
            ApplySpawnConfig(spawnedObject, config);
            
            // Update cooldown
            lastSpawnTime = Time.time;

            // Fire spawn event
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null && serviceContainer.TryResolve<IEventBus>(out var eventBus))
            {
                eventBus.Publish(new ObjectSpawnedEvent
                {
                    SpawnedObject = spawnedObject,
                    SpawnPoint = spawnPoint,
                    SpawnConfig = config
                });
            }

            return spawnedObject;
        }

        /// <summary>
        /// Register a new spawn configuration
        /// </summary>
        public void RegisterSpawnConfig(string key, SpawnConfig config)
        {
            spawnConfigs[key] = config;
        }

        /// <summary>
        /// Add a new spawn point
        /// </summary>
        public void AddSpawnPoint(SpawnPoint spawnPoint)
        {
            if (spawnPoint != null && !spawnPoints.Contains(spawnPoint))
            {
                spawnPoints.Add(spawnPoint);
            }
        }

        /// <summary>
        /// Get spawn point by tag
        /// </summary>
        private SpawnPoint GetSpawnPoint(string tag)
        {
            foreach (var point in spawnPoints)
            {
                if (point != null && point.SpawnTag == tag)
                {
                    return point;
                }
            }
            
            // Return first available if tag not found
            return spawnPoints.Count > 0 ? spawnPoints[0] : null;
        }

        /// <summary>
        /// Get spawn configuration by key
        /// </summary>
        private SpawnConfig GetSpawnConfig(string key)
        {
            return spawnConfigs.ContainsKey(key) ? spawnConfigs[key] : defaultSpawnConfig;
        }

        /// <summary>
        /// Apply spawn configuration to spawned object
        /// </summary>
        private void ApplySpawnConfig(GameObject obj, SpawnConfig config)
        {
            if (obj == null || config == null) return;

            // Apply scale
            if (config.ApplyScale)
            {
                obj.transform.localScale = config.SpawnScale;
            }

            // Apply initial velocity if rigidbody exists
            if (config.ApplyInitialVelocity)
            {
                var rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = config.InitialVelocity;
                }
            }

            // Set layer
            if (config.OverrideLayer)
            {
                obj.layer = config.SpawnLayer;
            }
        }

        /// <summary>
        /// Create default spawn points if none exist
        /// </summary>
        private void CreateDefaultSpawnPoints()
        {
            GameObject defaultSpawnGO = new GameObject("DefaultSpawnPoint");
            defaultSpawnGO.transform.SetParent(transform);
            defaultSpawnGO.transform.position = transform.position;
            
            SpawnPoint defaultSpawn = defaultSpawnGO.AddComponent<SpawnPoint>();
            defaultSpawn.SpawnTag = "default";
            
            spawnPoints.Add(defaultSpawn);
        }

        /// <summary>
        /// Get all available spawn points with a specific tag
        /// </summary>
        public List<SpawnPoint> GetSpawnPointsByTag(string tag)
        {
            List<SpawnPoint> points = new List<SpawnPoint>();
            foreach (var point in spawnPoints)
            {
                if (point != null && point.SpawnTag == tag)
                {
                    points.Add(point);
                }
            }
            return points;
        }

        /// <summary>
        /// Track spawned object by tag for performance-optimized cleanup
        /// </summary>
        private void TrackSpawnedObject(GameObject obj, string tag)
        {
            if (!spawnedObjectsByTag.ContainsKey(tag))
            {
                spawnedObjectsByTag[tag] = new List<GameObject>();
            }
            spawnedObjectsByTag[tag].Add(obj);
        }

        /// <summary>
        /// Clear all spawned objects with a specific tag - PERFORMANCE OPTIMIZED
        /// </summary>
        public void ClearSpawnedObjects(string tag)
        {
            if (spawnedObjectsByTag.ContainsKey(tag))
            {
                var objectsToDestroy = spawnedObjectsByTag[tag];
                for (int i = objectsToDestroy.Count - 1; i >= 0; i--)
                {
                    if (objectsToDestroy[i] != null)
                    {
                        Destroy(objectsToDestroy[i]);
                    }
                }
                spawnedObjectsByTag[tag].Clear();

                Debug.Log($"[UnifiedSpawningManager] Cleared {objectsToDestroy.Count} objects with tag '{tag}' - NO scene scan required!");
            }
        }

        /// <summary>
        /// Clean up null references in tracked objects - call this periodically
        /// </summary>
        public void CleanupNullReferences()
        {
            foreach (var tag in spawnedObjectsByTag.Keys.ToList())
            {
                spawnedObjectsByTag[tag].RemoveAll(obj => obj == null);
            }
        }
    }

    /// <summary>
    /// Event fired when an object is spawned
    /// </summary>
    public class ObjectSpawnedEvent : BaseEvent
    {
        public GameObject SpawnedObject { get; set; }
        public SpawnPoint SpawnPoint { get; set; }
        public SpawnConfig SpawnConfig { get; set; }
    }
}
