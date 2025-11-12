using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Performance;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Specialized pool manager for monster/creature spawning in ChimeraOS.
    /// Manages multiple pools for different monster types and provides efficient spawning.
    /// Designed to support 1000+ creatures with minimal GC allocation.
    /// </summary>
    public class MonsterPoolManager : MonoBehaviour
    {
        #region Singleton

        private static MonsterPoolManager _instance;
        public static MonsterPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<MonsterPoolManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("MonsterPoolManager");
                        _instance = go.AddComponent<MonsterPoolManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Configuration

        [Header("Pool Configuration")]
        [SerializeField] private int defaultPoolSize = 50;
        [SerializeField] private int maxPoolSize = 200;
        [SerializeField] private bool preWarmPools = true;

        [Header("Monster Prefabs")]
        [SerializeField] private GameObject defaultMonsterPrefab;
        [SerializeField] private List<MonsterPrefabEntry> monsterPrefabs = new();

        #endregion

        #region State

        private Dictionary<string, GameObjectPool> _pools = new();
        private Transform _poolRoot;
        private int _spawnedCount = 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePools();
        }

        private void OnDestroy()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }
            _pools.Clear();
        }

        #endregion

        #region Initialization

        private void InitializePools()
        {
            // Create pool root
            _poolRoot = new GameObject("PooledMonsters").transform;
            _poolRoot.SetParent(transform);

            // Create default pool
            if (defaultMonsterPrefab != null)
            {
                CreatePool("Default", defaultMonsterPrefab);
            }

            // Create pools for configured prefabs
            foreach (var entry in monsterPrefabs)
            {
                if (entry.prefab != null && !string.IsNullOrEmpty(entry.monsterType))
                {
                    CreatePool(entry.monsterType, entry.prefab, entry.poolSize);
                }
            }

            Debug.Log($"[MonsterPoolManager] Initialized with {_pools.Count} pools");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Spawn a monster from the pool
        /// </summary>
        public GameObject SpawnMonster(string monsterType, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(monsterType, out var pool))
            {
                // Fallback to default pool
                if (!_pools.TryGetValue("Default", out pool))
                {
                    Debug.LogWarning($"[MonsterPoolManager] No pool found for type '{monsterType}' and no default pool!");
                    return null;
                }
            }

            GameObject monster = pool.Get(position, rotation);

            if (monster != null)
            {
                _spawnedCount++;

                // Reset monster state
                ResetMonster(monster);
            }

            return monster;
        }

        /// <summary>
        /// Spawn a monster at default position
        /// </summary>
        public GameObject SpawnMonster(string monsterType)
        {
            return SpawnMonster(monsterType, Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// Return a monster to the pool
        /// </summary>
        public void DespawnMonster(GameObject monster, string monsterType = "Default")
        {
            if (monster == null)
                return;

            if (_pools.TryGetValue(monsterType, out var pool))
            {
                pool.Return(monster);
            }
            else if (_pools.TryGetValue("Default", out var defaultPool))
            {
                defaultPool.Return(monster);
            }
            else
            {
                // No pool found, just destroy it
                Destroy(monster);
            }
        }

        /// <summary>
        /// Create a new pool for a specific monster type
        /// </summary>
        public void CreatePool(string monsterType, GameObject prefab, int initialSize = -1)
        {
            if (_pools.ContainsKey(monsterType))
            {
                Debug.LogWarning($"[MonsterPoolManager] Pool already exists for type '{monsterType}'");
                return;
            }

            int poolSize = initialSize >= 0 ? initialSize : defaultPoolSize;
            var pool = new GameObjectPool(prefab, poolSize, maxPoolSize, _poolRoot, autoExpand: true);

            if (preWarmPools)
            {
                pool.WarmUp(poolSize);
            }

            _pools[monsterType] = pool;
            Debug.Log($"[MonsterPoolManager] Created pool for '{monsterType}' with {poolSize} objects");
        }

        /// <summary>
        /// Get pool statistics
        /// </summary>
        public PoolStats GetPoolStats(string monsterType)
        {
            if (_pools.TryGetValue(monsterType, out var pool))
            {
                return new PoolStats
                {
                    monsterType = monsterType,
                    availableCount = pool.AvailableCount,
                    activeCount = pool.ActiveCount,
                    totalCount = pool.AvailableCount + pool.ActiveCount
                };
            }

            return new PoolStats { monsterType = monsterType };
        }

        /// <summary>
        /// Get all pool statistics
        /// </summary>
        public Dictionary<string, PoolStats> GetAllPoolStats()
        {
            var stats = new Dictionary<string, PoolStats>();
            foreach (var kvp in _pools)
            {
                stats[kvp.Key] = GetPoolStats(kvp.Key);
            }
            return stats;
        }

        /// <summary>
        /// Warm up all pools
        /// </summary>
        public void WarmUpAllPools()
        {
            foreach (var kvp in _pools)
            {
                kvp.Value.WarmUp();
            }
        }

        /// <summary>
        /// Return all active monsters to pools
        /// </summary>
        public void ReturnAllMonsters()
        {
            foreach (var pool in _pools.Values)
            {
                pool.ReturnAll();
            }
        }

        #endregion

        #region Private Methods

        private void ResetMonster(GameObject monster)
        {
            // Reset transform
            monster.transform.localScale = Vector3.one;

            // Reset any monster-specific components here
            // e.g., health, AI state, animations, etc.

            // Example: Reset velocity if has Rigidbody
            var rb = monster.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Print Pool Statistics")]
        private void PrintPoolStatistics()
        {
            Debug.Log("=== Monster Pool Statistics ===");
            Debug.Log($"Total Spawned (Lifetime): {_spawnedCount}");

            foreach (var kvp in _pools)
            {
                var stats = GetPoolStats(kvp.Key);
                Debug.Log($"{stats.monsterType}: Active={stats.activeCount}, Available={stats.availableCount}, Total={stats.totalCount}");
            }
        }

        #endregion

        #region Data Structures

        [System.Serializable]
        public class MonsterPrefabEntry
        {
            public string monsterType;
            public GameObject prefab;
            public int poolSize = 50;
        }

        public struct PoolStats
        {
            public string monsterType;
            public int availableCount;
            public int activeCount;
            public int totalCount;
        }

        #endregion
    }
}
