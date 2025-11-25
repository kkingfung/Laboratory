using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Subsystems.Spawning
{
    /// <summary>
    /// Generic object pool for performance optimization
    /// Eliminates allocations for frequently spawned objects
    /// Optimized for 1000+ creatures @ 60 FPS
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 50;
        [SerializeField] private int maxSize = 1000;
        [SerializeField] private bool allowGrowth = true;
        [SerializeField] private bool prewarmOnStart = true;

        // Pool storage
        private Queue<GameObject> _available = new Queue<GameObject>();
        private HashSet<GameObject> _inUse = new HashSet<GameObject>();
        private Transform _poolContainer;

        // Statistics
        private int _totalCreated = 0;
        private int _totalSpawned = 0;
        private int _totalRecycled = 0;
        private int _peakUsage = 0;

        // Events
        public event System.Action<GameObject> OnObjectSpawned;
        public event System.Action<GameObject> OnObjectRecycled;

        private void Awake()
        {
            // Create container for pooled objects
            _poolContainer = new GameObject($"Pool_{prefab.name}").transform;
            _poolContainer.SetParent(transform);
        }

        private void Start()
        {
            if (prewarmOnStart)
            {
                Prewarm();
            }
        }

        /// <summary>
        /// Prewarm the pool with initial objects
        /// </summary>
        public void Prewarm()
        {
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }

            Debug.Log($"[ObjectPool] Prewarmed {prefab.name} pool with {initialSize} objects");
        }

        /// <summary>
        /// Create a new object and add to pool
        /// </summary>
        private GameObject CreateNewObject()
        {
            GameObject obj = Instantiate(prefab, _poolContainer);
            obj.SetActive(false);
            _available.Enqueue(obj);
            _totalCreated++;
            return obj;
        }

        /// <summary>
        /// Spawn object from pool
        /// </summary>
        public GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            GameObject obj;

            // Get from pool or create new
            if (_available.Count > 0)
            {
                obj = _available.Dequeue();
            }
            else if (allowGrowth && _totalCreated < maxSize)
            {
                obj = CreateNewObject();
                Debug.Log($"[ObjectPool] Pool grew: {_totalCreated}/{maxSize} for {prefab.name}");
            }
            else
            {
                Debug.LogWarning($"[ObjectPool] Pool exhausted for {prefab.name}! Max: {maxSize}");
                return null;
            }

            // Configure object
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            // Track usage
            _inUse.Add(obj);
            _totalSpawned++;

            if (_inUse.Count > _peakUsage)
            {
                _peakUsage = _inUse.Count;
            }

            OnObjectSpawned?.Invoke(obj);

            return obj;
        }

        /// <summary>
        /// Recycle object back to pool
        /// </summary>
        public void Recycle(GameObject obj)
        {
            if (obj == null) return;

            if (!_inUse.Contains(obj))
            {
                Debug.LogWarning($"[ObjectPool] Attempted to recycle object not from this pool: {obj.name}");
                return;
            }

            // Reset object
            obj.SetActive(false);
            obj.transform.SetParent(_poolContainer);

            // Return to pool
            _inUse.Remove(obj);
            _available.Enqueue(obj);
            _totalRecycled++;

            OnObjectRecycled?.Invoke(obj);
        }

        /// <summary>
        /// Recycle all active objects
        /// </summary>
        public void RecycleAll()
        {
            // Copy to list to avoid collection modification during iteration
            List<GameObject> toRecycle = new List<GameObject>(_inUse);

            foreach (GameObject obj in toRecycle)
            {
                Recycle(obj);
            }

            Debug.Log($"[ObjectPool] Recycled all {toRecycle.Count} objects for {prefab.name}");
        }

        /// <summary>
        /// Clear pool and destroy all objects
        /// </summary>
        public void Clear()
        {
            RecycleAll();

            while (_available.Count > 0)
            {
                GameObject obj = _available.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            _totalCreated = 0;
            _totalSpawned = 0;
            _totalRecycled = 0;
            _peakUsage = 0;

            Debug.Log($"[ObjectPool] Cleared pool for {prefab.name}");
        }

        /// <summary>
        /// Get pool statistics
        /// </summary>
        public PoolStats GetStats()
        {
            return new PoolStats
            {
                PrefabName = prefab != null ? prefab.name : "None",
                TotalCreated = _totalCreated,
                Available = _available.Count,
                InUse = _inUse.Count,
                PeakUsage = _peakUsage,
                TotalSpawned = _totalSpawned,
                TotalRecycled = _totalRecycled,
                UtilizationPercentage = _totalCreated > 0 ? (_inUse.Count / (float)_totalCreated) * 100f : 0f
            };
        }

        private void OnDestroy()
        {
            Clear();
        }
    }

    /// <summary>
    /// Pool statistics structure
    /// </summary>
    public struct PoolStats
    {
        public string PrefabName;
        public int TotalCreated;
        public int Available;
        public int InUse;
        public int PeakUsage;
        public int TotalSpawned;
        public int TotalRecycled;
        public float UtilizationPercentage;
    }
}
