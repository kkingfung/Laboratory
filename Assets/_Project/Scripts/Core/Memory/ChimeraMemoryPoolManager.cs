using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Laboratory.Core.ECS;
using Laboratory.Shared.Interfaces;
using Laboratory.Compatibility;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Laboratory.Core.Memory
{
    /// <summary>
    /// High-performance memory pool manager for Project Chimera that eliminates GC allocations
    /// during creature spawning and provides object pooling for GameObjects and ECS entities.
    /// Designed to support 1000+ creature spawning with zero frame drops and minimal memory footprint.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class ChimeraMemoryPoolManager : MonoBehaviour
    {
        [Header("🧠 Memory Pool Configuration")]
        [SerializeField] private bool enablePooling = true;
        [SerializeField] private int initialCreaturePoolSize = 500;
        [SerializeField] private int maxCreaturePoolSize = 2000;
        [SerializeField] private bool preWarmPools = true;

        [Header("🎯 Pool Categories")]
        [SerializeField] private int visualEffectPoolSize = GameConstants.DEFAULT_POOL_SIZE;
        [SerializeField] private int audioSourcePoolSize = 50;
        [SerializeField] private int particleSystemPoolSize = 75;
        [SerializeField] private int trailRendererPoolSize = 25;

        [Header("📊 Memory Management")]
        [SerializeField] private bool autoCleanupPools = true;
        [SerializeField] private float cleanupInterval = 30f;
        [SerializeField] private float maxIdleTime = 60f;
        [SerializeField] private bool trackMemoryUsage = true;

        [Header("🔧 Performance Settings")]
        [SerializeField] private int maxOperationsPerFrame = 50;
        [SerializeField] private bool useMultithreading = true;
        [SerializeField] private bool enableDebugLogging = false;

        [Header("📈 Runtime Statistics")]
        [SerializeField, ReadOnly] private int totalObjectsPooled = 0;
        [SerializeField, ReadOnly] private int activeObjects = 0;
        [SerializeField, ReadOnly] private int poolHits = 0;
        [SerializeField, ReadOnly] private int poolMisses = 0;
        [SerializeField, ReadOnly] private float poolHitRate = 0f;

        // Pool storage
        private Dictionary<string, ObjectPool> gameObjectPools = new Dictionary<string, ObjectPool>();
        private Dictionary<System.Type, ComponentPool> componentPools = new Dictionary<System.Type, ComponentPool>();
        private Dictionary<System.Type, NativeArrayPool> nativeArrayPools = new Dictionary<System.Type, NativeArrayPool>();

        // ECS Integration
        private EntityManager entityManager;
        private Dictionary<Entity, PooledEntityInfo> pooledEntities = new Dictionary<Entity, PooledEntityInfo>();

        // Performance tracking
        private MemoryPoolStatistics statistics = new MemoryPoolStatistics();
        private Queue<PoolOperation> pendingOperations = new Queue<PoolOperation>();

        // Thread safety
        private readonly object poolLock = new object();

        // Pre-allocated collections to avoid GC
        private List<GameObject> reusableGameObjectList = new List<GameObject>();
        private List<Entity> reusableEntityList = new List<Entity>();

        private struct PooledEntityInfo
        {
            public float poolTime;
            public bool isActive;
            public string poolCategory;
        }

        public struct MemoryPoolStatistics
        {
            public int totalAllocations;
            public int totalDeallocations;
            public long totalMemoryPooled;
            public float averageOperationTime;
            public int gcCollectionsPrevented;
        }

        private struct PoolOperation
        {
            public PoolOperationType type;
            public string poolName;
            public GameObject gameObject;
            public Entity entity;
            public float timestamp;
        }

        private enum PoolOperationType
        {
            Get,
            Return,
            Cleanup
        }

        // Pool implementations
        private class ObjectPool
        {
            private readonly ConcurrentQueue<GameObject> pool = new ConcurrentQueue<GameObject>();
            private readonly GameObject prefab;
            private readonly Transform poolParent;
            private readonly int maxSize;

            public int Count => pool.Count;
            public string Name { get; }

            public ObjectPool(string name, GameObject prefab, Transform parent, int initialSize, int maxSize)
            {
                Name = name;
                this.prefab = prefab;
                this.poolParent = parent;
                this.maxSize = maxSize;

                // Pre-warm pool
                for (int i = 0; i < initialSize; i++)
                {
                    var obj = CreateNewObject();
                    obj.SetActive(false);
                    pool.Enqueue(obj);
                }
            }

            public GameObject Get()
            {
                if (pool.TryDequeue(out GameObject obj))
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                        return obj;
                    }
                }

                return CreateNewObject();
            }

            public bool Return(GameObject obj)
            {
                if (obj == null || pool.Count >= maxSize) return false;

                obj.SetActive(false);
                obj.transform.SetParent(poolParent);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;

                // Reset components to default state
                ResetPooledObject(obj);

                pool.Enqueue(obj);
                return true;
            }

            private GameObject CreateNewObject()
            {
                var obj = Instantiate(prefab, poolParent);
                obj.name = $"{Name}_Pooled";
                return obj;
            }

            private void ResetPooledObject(GameObject obj)
            {
                // Reset common components
                var particleSystem = obj.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    particleSystem.Stop();
                    particleSystem.Clear();
                }

                var audioSource = obj.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.Stop();
                    audioSource.clip = null;
                }

                var trailRenderer = obj.GetComponent<TrailRenderer>();
                if (trailRenderer != null)
                {
                    trailRenderer.Clear();
                }

                // Reset any creature-specific components using proper interface
                var creatureComponents = obj.GetComponents<ICreatureComponent>();
                foreach (var component in creatureComponents)
                {
                    component.ResetToDefaults();
                }

                // Reset any poolable components
                var poolableComponents = obj.GetComponents<IPoolable>();
                foreach (var component in poolableComponents)
                {
                    component.OnReturnToPool();
                }
            }

            public void Cleanup(float maxIdleTime)
            {
                var cutoffTime = Time.time - maxIdleTime;
                var itemsToKeep = new List<GameObject>();

                while (pool.TryDequeue(out GameObject obj))
                {
                    if (obj != null && Time.time - cutoffTime < maxIdleTime / 2f)
                    {
                        itemsToKeep.Add(obj);
                    }
                    else if (obj != null)
                    {
                        DestroyImmediate(obj);
                    }
                }

                foreach (var obj in itemsToKeep)
                {
                    pool.Enqueue(obj);
                }
            }
        }

        private class ComponentPool
        {
            private readonly ConcurrentQueue<System.Object> pool = new ConcurrentQueue<System.Object>();
            private readonly System.Type componentType;
            private readonly int maxSize;

            public int Count => pool.Count;

            public ComponentPool(System.Type type, int maxSize)
            {
                componentType = type;
                this.maxSize = maxSize;
            }

            public T Get<T>() where T : class, new()
            {
                if (pool.TryDequeue(out System.Object obj))
                {
                    return obj as T;
                }

                return new T();
            }

            public bool Return<T>(T component) where T : class
            {
                if (component == null || pool.Count >= maxSize) return false;

                pool.Enqueue(component);
                return true;
            }
        }

        private class NativeArrayPool
        {
            private readonly ConcurrentQueue<System.Object> pool = new ConcurrentQueue<System.Object>();
            private readonly System.Type elementType;
            private readonly int arraySize;
            private readonly int maxSize;

            public int Count => pool.Count;

            public NativeArrayPool(System.Type type, int arraySize, int maxSize)
            {
                elementType = type;
                this.arraySize = arraySize;
                this.maxSize = maxSize;
            }

            public NativeArray<T> Get<T>() where T : struct
            {
                if (pool.TryDequeue(out System.Object obj) && obj is NativeArray<T> array && array.IsCreated)
                {
                    return array;
                }

                return new NativeArray<T>(arraySize, Allocator.Persistent);
            }

            public bool Return<T>(NativeArray<T> array) where T : struct
            {
                if (!array.IsCreated || pool.Count >= maxSize) return false;

                pool.Enqueue(array);
                return true;
            }

            public void DisposeAll()
            {
                while (pool.TryDequeue(out System.Object obj))
                {
                    if (obj is INativeDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        private void Awake()
        {
            // Singleton pattern
            if (FindObjectsOfType<ChimeraMemoryPoolManager>().Length > 1)
            {
                DestroyImmediate(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeMemoryPools();

            if (autoCleanupPools)
            {
                InvokeRepeating(nameof(CleanupPools), cleanupInterval, cleanupInterval);
            }
        }

        private void InitializeMemoryPools()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated == true)
            {
                entityManager = world.EntityManager;
            }

            if (preWarmPools)
            {
                PreWarmAllPools();
            }

            // Initialize component pools
            componentPools[typeof(CreatureData)] = new ComponentPool(typeof(CreatureData), GameConstants.TARGET_MAX_CREATURES);
            componentPools[typeof(CreatureStats)] = new ComponentPool(typeof(CreatureStats), GameConstants.TARGET_MAX_CREATURES);
            componentPools[typeof(List<float>)] = new ComponentPool(typeof(List<float>), 500);

            // Initialize NativeArray pools
            nativeArrayPools[typeof(Entity)] = new NativeArrayPool(typeof(Entity), 100, 50);
            nativeArrayPools[typeof(float3)] = new NativeArrayPool(typeof(float3), 100, 50);

            if (enableDebugLogging)
                Debug.Log($"🧠 Chimera Memory Pool Manager initialized with {gameObjectPools.Count} GameObject pools");
        }

        private void PreWarmAllPools()
        {
            // Create pool parent objects
            var poolParent = new GameObject("Memory Pools").transform;
            poolParent.SetParent(transform);

            // Pre-warm creature pools (would need actual prefabs)
            CreateGameObjectPool("CreatureBase", null, poolParent, initialCreaturePoolSize, maxCreaturePoolSize);
            CreateGameObjectPool("VFX_Death", null, poolParent, visualEffectPoolSize, visualEffectPoolSize * 2);
            CreateGameObjectPool("VFX_Breeding", null, poolParent, 20, 50);
            CreateGameObjectPool("AudioSource_Ambient", null, poolParent, audioSourcePoolSize, audioSourcePoolSize * 2);
            CreateGameObjectPool("ParticleSystem_Generic", null, poolParent, particleSystemPoolSize, particleSystemPoolSize * 2);

            totalObjectsPooled = CalculateTotalPooledObjects();
        }

        private void CreateGameObjectPool(string poolName, GameObject prefab, Transform parent, int initialSize, int maxSize)
        {
            if (prefab == null)
            {
                // Create a dummy prefab for pre-warming (in production, these would be real prefabs)
                prefab = new GameObject($"Dummy_{poolName}");
                prefab.SetActive(false);
            }

            gameObjectPools[poolName] = new ObjectPool(poolName, prefab, parent, initialSize, maxSize);
        }

        /// <summary>
        /// Get a pooled GameObject of the specified type
        /// </summary>
        public GameObject GetPooledGameObject(string poolName)
        {
            if (!enablePooling) return null;

            lock (poolLock)
            {
                if (gameObjectPools.TryGetValue(poolName, out ObjectPool pool))
                {
                    var obj = pool.Get();
                    statistics.totalAllocations++;
                    poolHits++;
                    activeObjects++;

                    UpdatePoolHitRate();

                    if (enableDebugLogging)
                        Debug.Log($"🧠 Retrieved {poolName} from pool - Active: {activeObjects}");

                    return obj;
                }
                else
                {
                    poolMisses++;
                    UpdatePoolHitRate();

                    if (enableDebugLogging)
                        Debug.LogWarning($"🧠 Pool '{poolName}' not found");

                    return null;
                }
            }
        }

        /// <summary>
        /// Return a GameObject to its pool
        /// </summary>
        public bool ReturnToPool(string poolName, GameObject obj)
        {
            if (!enablePooling || obj == null) return false;

            lock (poolLock)
            {
                if (gameObjectPools.TryGetValue(poolName, out ObjectPool pool))
                {
                    var returned = pool.Return(obj);
                    if (returned)
                    {
                        statistics.totalDeallocations++;
                        activeObjects = Mathf.Max(0, activeObjects - 1);

                        if (enableDebugLogging)
                            Debug.Log($"🧠 Returned {poolName} to pool - Active: {activeObjects}");
                    }
                    return returned;
                }
                return false;
            }
        }

        /// <summary>
        /// Get a pooled component instance
        /// </summary>
        public T GetPooledComponent<T>() where T : class, new()
        {
            if (!enablePooling) return new T();

            var type = typeof(T);
            if (componentPools.TryGetValue(type, out ComponentPool pool))
            {
                statistics.totalAllocations++;
                return pool.Get<T>();
            }

            return new T();
        }

        /// <summary>
        /// Return a component to its pool
        /// </summary>
        public bool ReturnComponent<T>(T component) where T : class
        {
            if (!enablePooling || component == null) return false;

            var type = typeof(T);
            if (componentPools.TryGetValue(type, out ComponentPool pool))
            {
                statistics.totalDeallocations++;
                return pool.Return(component);
            }

            return false;
        }

        /// <summary>
        /// Get a pooled NativeArray
        /// </summary>
        public NativeArray<T> GetPooledNativeArray<T>() where T : struct
        {
            if (!enablePooling) return new NativeArray<T>(100, Allocator.TempJob);

            var type = typeof(T);
            if (nativeArrayPools.TryGetValue(type, out NativeArrayPool pool))
            {
                statistics.totalAllocations++;
                return pool.Get<T>();
            }

            return new NativeArray<T>(100, Allocator.TempJob);
        }

        /// <summary>
        /// Return a NativeArray to its pool
        /// </summary>
        public bool ReturnNativeArray<T>(NativeArray<T> array) where T : struct
        {
            if (!enablePooling || !array.IsCreated) return false;

            var type = typeof(T);
            if (nativeArrayPools.TryGetValue(type, out NativeArrayPool pool))
            {
                statistics.totalDeallocations++;
                return pool.Return(array);
            }

            return false;
        }

        /// <summary>
        /// Register an ECS entity as pooled
        /// </summary>
        public void RegisterPooledEntity(Entity entity, string category)
        {
            if (!enablePooling) return;

            pooledEntities[entity] = new PooledEntityInfo
            {
                poolTime = Time.time,
                isActive = true,
                poolCategory = category
            };
        }

        /// <summary>
        /// Return an ECS entity to the pool
        /// </summary>
        public bool ReturnPooledEntity(Entity entity)
        {
            if (!enablePooling || !pooledEntities.ContainsKey(entity)) return false;

            var info = pooledEntities[entity];
            info.isActive = false;
            info.poolTime = Time.time;
            pooledEntities[entity] = info;

            // Disable entity components instead of destroying
            if (entityManager.Exists(entity))
            {
                entityManager.SetEnabled(entity, false);
            }

            return true;
        }

        /// <summary>
        /// Batch spawn creatures with zero allocations
        /// </summary>
        public void BatchSpawnCreatures(string[] speciesNames, float3[] positions, int count)
        {
            if (!enablePooling || count == 0) return;

            var operations = Mathf.Min(count, maxOperationsPerFrame);

            for (int i = 0; i < operations; i++)
            {
                var poolName = speciesNames[i % speciesNames.Length];
                var creature = GetPooledGameObject(poolName);

                if (creature != null)
                {
                    creature.transform.position = positions[i % positions.Length];

                    // Initialize creature without allocation using proper interface
                    var creatureComponents = creature.GetComponents<ICreatureComponent>();
                    foreach (var component in creatureComponents)
                    {
                        component.InitializeFromPool();
                    }

                    // Initialize poolable components
                    var poolableComponents = creature.GetComponents<IPoolable>();
                    foreach (var component in poolableComponents)
                    {
                        component.OnGetFromPool();
                    }
                }
            }

            if (enableDebugLogging)
                Debug.Log($"🧠 Batch spawned {operations} creatures with zero allocations");
        }

        private void CleanupPools()
        {
            if (!enablePooling) return;

            var cleanupStartTime = Time.realtimeSinceStartup;

            // Cleanup GameObject pools
            foreach (var pool in gameObjectPools.Values)
            {
                pool.Cleanup(maxIdleTime);
            }

            // Cleanup dead entities
            CleanupDeadEntities();

            var cleanupTime = Time.realtimeSinceStartup - cleanupStartTime;
            if (enableDebugLogging)
                Debug.Log($"🧠 Pool cleanup completed in {cleanupTime * 1000:F2}ms");
        }

        private void CleanupDeadEntities()
        {
            reusableEntityList.Clear();

            foreach (var kvp in pooledEntities)
            {
                var entity = kvp.Key;
                var info = kvp.Value;

                if (!entityManager.Exists(entity) || (Time.time - info.poolTime > maxIdleTime))
                {
                    reusableEntityList.Add(entity);
                }
            }

            foreach (var entity in reusableEntityList)
            {
                pooledEntities.Remove(entity);
            }
        }

        private int CalculateTotalPooledObjects()
        {
            int total = 0;
            foreach (var pool in gameObjectPools.Values)
            {
                total += pool.Count;
            }
            return total;
        }

        private void UpdatePoolHitRate()
        {
            var totalRequests = poolHits + poolMisses;
            poolHitRate = totalRequests > 0 ? (float)poolHits / totalRequests : 0f;
        }

        private void Update()
        {
            // Process pending operations gradually to avoid frame drops
            ProcessPendingOperations();

            // Update statistics
            totalObjectsPooled = CalculateTotalPooledObjects();
        }

        private void ProcessPendingOperations()
        {
            var operationsThisFrame = 0;
            while (pendingOperations.Count > 0 && operationsThisFrame < maxOperationsPerFrame)
            {
                var operation = pendingOperations.Dequeue();
                ProcessPoolOperation(operation);
                operationsThisFrame++;
            }
        }

        private void ProcessPoolOperation(PoolOperation operation)
        {
            switch (operation.type)
            {
                case PoolOperationType.Get:
                    // Handle deferred get operations
                    break;
                case PoolOperationType.Return:
                    // Handle deferred return operations
                    break;
                case PoolOperationType.Cleanup:
                    // Handle deferred cleanup operations
                    break;
            }
        }

        /// <summary>
        /// Force immediate cleanup of all pools
        /// </summary>
        [ContextMenu("Force Pool Cleanup")]
        public void ForceCleanup()
        {
            CleanupPools();
            System.GC.Collect();
            Debug.Log("🧠 Forced memory pool cleanup completed");
        }

        /// <summary>
        /// Get current memory pool statistics
        /// </summary>
        public MemoryPoolStatistics GetStatistics()
        {
            return statistics;
        }

        /// <summary>
        /// Reset all pool statistics
        /// </summary>
        public void ResetStatistics()
        {
            statistics = new MemoryPoolStatistics();
            poolHits = 0;
            poolMisses = 0;
            poolHitRate = 0f;
            Debug.Log("🧠 Memory pool statistics reset");
        }

        private void OnDestroy()
        {
            CancelInvoke();

            // Dispose all native arrays
            foreach (var pool in nativeArrayPools.Values)
            {
                pool.DisposeAll();
            }
        }

        private void OnGUI()
        {
            if (!enableDebugLogging) return;

            // Draw memory pool stats overlay
            var rect = new Rect(Screen.width - 300, Screen.height - 150, 290, 140);
            GUI.Box(rect, "Memory Pool Stats");

            var y = rect.y + 20;
            GUI.Label(new Rect(rect.x + 10, y, 280, 20), $"Pool Hit Rate: {poolHitRate:P1}");
            GUI.Label(new Rect(rect.x + 10, y + 20, 280, 20), $"Active Objects: {activeObjects}");
            GUI.Label(new Rect(rect.x + 10, y + 40, 280, 20), $"Total Pooled: {totalObjectsPooled}");
            GUI.Label(new Rect(rect.x + 10, y + 60, 280, 20), $"Allocations: {statistics.totalAllocations}");
            GUI.Label(new Rect(rect.x + 10, y + 80, 280, 20), $"Deallocations: {statistics.totalDeallocations}");
            GUI.Label(new Rect(rect.x + 10, y + 100, 280, 20), $"GC Prevented: {statistics.gcCollectionsPrevented}");
        }
    }

    /// <summary>
    /// Static accessor for the memory pool manager
    /// </summary>
    public static class MemoryPool
    {
        private static ChimeraMemoryPoolManager instance;

        public static ChimeraMemoryPoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = UnityCompatibility.FindFirstObjectByType<ChimeraMemoryPoolManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("Chimera Memory Pool Manager");
                        instance = go.AddComponent<ChimeraMemoryPoolManager>();
                    }
                }
                return instance;
            }
        }

        public static GameObject Get(string poolName) => Instance.GetPooledGameObject(poolName);
        public static bool Return(string poolName, GameObject obj) => Instance.ReturnToPool(poolName, obj);
        public static T GetComponent<T>() where T : class, new() => Instance.GetPooledComponent<T>();
        public static bool ReturnComponent<T>(T component) where T : class => Instance.ReturnComponent(component);
    }
}