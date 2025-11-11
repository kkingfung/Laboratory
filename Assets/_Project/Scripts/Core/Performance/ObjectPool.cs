using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

namespace Laboratory.Core.Performance
{
    /// <summary>
    /// High-performance generic object pooling system for GameObjects and Components.
    /// Eliminates Instantiate/Destroy calls to reduce GC pressure and improve performance.
    /// Supports automatic expansion, capacity limits, and warmup preloading.
    /// </summary>
    /// <typeparam name="T">Type to pool (GameObject or Component)</typeparam>
    public class ObjectPool<T> where T : Component
    {
        #region Fields

        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _availableObjects;
        private readonly HashSet<T> _activeObjects;
        private readonly int _initialSize;
        private readonly int _maxSize;
        private readonly bool _autoExpand;

        private static readonly ProfilerMarker s_GetMarker = new ProfilerMarker("ObjectPool.Get");
        private static readonly ProfilerMarker s_ReturnMarker = new ProfilerMarker("ObjectPool.Return");

        #endregion

        #region Properties

        public int AvailableCount => _availableObjects.Count;
        public int ActiveCount => _activeObjects.Count;
        public int TotalCount => AvailableCount + ActiveCount;

        #endregion

        #region Constructor

        /// <summary>
        /// Create a new object pool
        /// </summary>
        /// <param name="prefab">Prefab to instantiate</param>
        /// <param name="initialSize">Initial pool size</param>
        /// <param name="maxSize">Maximum pool size (0 = unlimited)</param>
        /// <param name="parent">Parent transform for pooled objects</param>
        /// <param name="autoExpand">Allow pool to grow beyond initial size</param>
        public ObjectPool(T prefab, int initialSize = 10, int maxSize = 100, Transform parent = null, bool autoExpand = true)
        {
            _prefab = prefab ?? throw new ArgumentNullException(nameof(prefab));
            _initialSize = initialSize;
            _maxSize = maxSize;
            _parent = parent;
            _autoExpand = autoExpand;

            _availableObjects = new Queue<T>(initialSize);
            _activeObjects = new HashSet<T>();

            WarmUp();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get an object from the pool
        /// </summary>
        public T Get()
        {
            using (s_GetMarker.Auto())
            {
                T obj;

                if (_availableObjects.Count > 0)
                {
                    obj = _availableObjects.Dequeue();
                }
                else if (_autoExpand && (_maxSize == 0 || TotalCount < _maxSize))
                {
                    obj = CreateNewObject();
                }
                else
                {
                    Debug.LogWarning($"[ObjectPool] Pool exhausted! Active: {ActiveCount}, Max: {_maxSize}");
                    return null;
                }

                obj.gameObject.SetActive(true);
                _activeObjects.Add(obj);

                return obj;
            }
        }

        /// <summary>
        /// Get an object with position and rotation
        /// </summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = Get();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[ObjectPool] Attempted to return null object");
                return;
            }

            using (s_ReturnMarker.Auto())
            {
                if (!_activeObjects.Remove(obj))
                {
                    Debug.LogWarning($"[ObjectPool] Attempted to return object that wasn't from this pool: {obj.name}");
                    return;
                }

                obj.gameObject.SetActive(false);

                if (_parent != null)
                {
                    obj.transform.SetParent(_parent, false);
                }

                _availableObjects.Enqueue(obj);
            }
        }

        /// <summary>
        /// Return all active objects to the pool
        /// </summary>
        public void ReturnAll()
        {
            var activeList = new List<T>(_activeObjects);
            foreach (var obj in activeList)
            {
                Return(obj);
            }
        }

        /// <summary>
        /// Clear the pool and destroy all objects
        /// </summary>
        public void Clear()
        {
            ReturnAll();

            while (_availableObjects.Count > 0)
            {
                var obj = _availableObjects.Dequeue();
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj.gameObject);
                }
            }

            _activeObjects.Clear();
        }

        /// <summary>
        /// Warm up the pool by pre-creating objects
        /// </summary>
        public void WarmUp(int count = -1)
        {
            int targetCount = count >= 0 ? count : _initialSize;

            for (int i = 0; i < targetCount; i++)
            {
                if (_maxSize > 0 && TotalCount >= _maxSize)
                    break;

                T obj = CreateNewObject();
                obj.gameObject.SetActive(false);
                _availableObjects.Enqueue(obj);
            }

            Debug.Log($"[ObjectPool] Warmed up pool for {_prefab.name}: {_availableObjects.Count} objects ready");
        }

        #endregion

        #region Private Methods

        private T CreateNewObject()
        {
            T obj = UnityEngine.Object.Instantiate(_prefab, _parent);
            obj.name = $"{_prefab.name}_Pooled_{TotalCount}";
            return obj;
        }

        #endregion
    }

    /// <summary>
    /// Simplified GameObject pool (non-generic)
    /// </summary>
    public class GameObjectPool
    {
        private readonly GameObject _prefab;
        private readonly ObjectPool<Transform> _pool;

        public int AvailableCount => _pool.AvailableCount;
        public int ActiveCount => _pool.ActiveCount;

        public GameObjectPool(GameObject prefab, int initialSize = 10, int maxSize = 100, Transform parent = null, bool autoExpand = true)
        {
            _prefab = prefab;
            _pool = new ObjectPool<Transform>(prefab.transform, initialSize, maxSize, parent, autoExpand);
        }

        public GameObject Get() => _pool.Get()?.gameObject;
        public GameObject Get(Vector3 position, Quaternion rotation) => _pool.Get(position, rotation)?.gameObject;
        public void Return(GameObject obj) => _pool.Return(obj.transform);
        public void ReturnAll() => _pool.ReturnAll();
        public void Clear() => _pool.Clear();
        public void WarmUp(int count = -1) => _pool.WarmUp(count);
    }
}
