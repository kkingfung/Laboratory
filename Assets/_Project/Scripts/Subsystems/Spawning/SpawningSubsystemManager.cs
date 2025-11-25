using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Subsystems.Spawning
{
    /// <summary>
    /// Subsystem manager for Spawning
    /// Coordinates object pooling and spawning for 1000+ creatures
    /// Follows Project Chimera architecture pattern
    /// </summary>
    public class SpawningSubsystemManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SpawnConfig defaultConfig;

        [Header("Pool Manager")]
        [SerializeField] private AdvancedPoolManager poolManager;

        [Header("Prefab Registry")]
        [SerializeField] private List<GameObject> commonPrefabs = new List<GameObject>();

        // Singleton
        private static SpawningSubsystemManager _instance;
        public static SpawningSubsystemManager Instance => _instance;

        // State
        private bool _isInitialized = false;
        private Dictionary<string, GameObject> _prefabRegistry = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeComponents();
        }

        private void Start()
        {
            InitializeSubsystem();
        }

        /// <summary>
        /// Initialize required components
        /// </summary>
        private void InitializeComponents()
        {
            // Find or create pool manager
            if (poolManager == null)
            {
                poolManager = GetComponentInChildren<AdvancedPoolManager>();
                if (poolManager == null)
                {
                    poolManager = AdvancedPoolManager.Instance;
                }

                if (poolManager == null)
                {
                    GameObject poolObj = new GameObject("AdvancedPoolManager");
                    poolObj.transform.SetParent(transform);
                    poolManager = poolObj.AddComponent<AdvancedPoolManager>();
                }
            }
        }

        /// <summary>
        /// Initialize spawning subsystem
        /// </summary>
        private void InitializeSubsystem()
        {
            if (_isInitialized) return;

            // Register common prefabs
            RegisterCommonPrefabs();

            // Create pools for common prefabs
            PrewarmCommonPools();

            _isInitialized = true;
            Debug.Log("[SpawningSubsystem] Initialized");
        }

        /// <summary>
        /// Register common prefabs
        /// </summary>
        private void RegisterCommonPrefabs()
        {
            foreach (GameObject prefab in commonPrefabs)
            {
                if (prefab != null)
                {
                    RegisterPrefab(prefab.name, prefab);
                }
            }

            Debug.Log($"[SpawningSubsystem] Registered {_prefabRegistry.Count} common prefabs");
        }

        /// <summary>
        /// Prewarm pools for common prefabs
        /// </summary>
        private void PrewarmCommonPools()
        {
            foreach (GameObject prefab in commonPrefabs)
            {
                if (prefab != null && poolManager != null)
                {
                    poolManager.CreatePool(prefab);
                }
            }

            Debug.Log($"[SpawningSubsystem] Prewarmed {commonPrefabs.Count} pools");
        }

        /// <summary>
        /// Register a prefab by name
        /// </summary>
        public void RegisterPrefab(string name, GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[SpawningSubsystem] Cannot register null prefab for name: {name}");
                return;
            }

            _prefabRegistry[name] = prefab;
        }

        /// <summary>
        /// Get prefab by name
        /// </summary>
        public GameObject GetPrefab(string name)
        {
            if (_prefabRegistry.TryGetValue(name, out GameObject prefab))
            {
                return prefab;
            }

            Debug.LogWarning($"[SpawningSubsystem] Prefab not found: {name}");
            return null;
        }

        /// <summary>
        /// Spawn object by prefab
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (poolManager != null)
            {
                return poolManager.Spawn(prefab, position, rotation);
            }

            Debug.LogError("[SpawningSubsystem] Pool manager not found!");
            return null;
        }

        /// <summary>
        /// Spawn object by name
        /// </summary>
        public GameObject Spawn(string prefabName, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = GetPrefab(prefabName);
            if (prefab != null)
            {
                return Spawn(prefab, position, rotation);
            }

            return null;
        }

        /// <summary>
        /// Spawn object at position with default rotation
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position)
        {
            return Spawn(prefab, position, Quaternion.identity);
        }

        /// <summary>
        /// Spawn object by name at position
        /// </summary>
        public GameObject Spawn(string prefabName, Vector3 position)
        {
            return Spawn(prefabName, position, Quaternion.identity);
        }

        /// <summary>
        /// Recycle object
        /// </summary>
        public void Recycle(GameObject obj)
        {
            if (poolManager != null)
            {
                poolManager.Recycle(obj);
            }
        }

        /// <summary>
        /// Recycle all objects
        /// </summary>
        public void RecycleAll()
        {
            if (poolManager != null)
            {
                poolManager.RecycleAll();
            }
        }

        /// <summary>
        /// Create pool for prefab
        /// </summary>
        public void CreatePool(GameObject prefab, int initialSize = 50, int maxSize = 1000)
        {
            if (poolManager != null)
            {
                poolManager.CreatePool(prefab, initialSize, maxSize);
            }
        }

        /// <summary>
        /// Get total active objects
        /// </summary>
        public int GetTotalActiveObjects()
        {
            return poolManager != null ? poolManager.GetTotalActiveObjects() : 0;
        }

        /// <summary>
        /// Get all pool statistics
        /// </summary>
        public List<PoolStats> GetAllPoolStats()
        {
            return poolManager != null ? poolManager.GetAllPoolStats() : new List<PoolStats>();
        }

        /// <summary>
        /// Get pool manager
        /// </summary>
        public AdvancedPoolManager GetPoolManager()
        {
            return poolManager;
        }

        /// <summary>
        /// Get default configuration
        /// </summary>
        public SpawnConfig GetDefaultConfig()
        {
            return defaultConfig;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
