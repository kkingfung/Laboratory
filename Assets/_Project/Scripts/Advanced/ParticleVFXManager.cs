using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Advanced
{
    /// <summary>
    /// Particle and VFX management system with object pooling.
    /// Handles particle lifecycle, pooling, culling, and performance optimization.
    /// Prevents memory spikes from frequent particle instantiation/destruction.
    /// </summary>
    public class ParticleVFXManager : MonoBehaviour
    {
        #region Configuration

        [Header("Pooling")]
        [SerializeField] private bool enablePooling = true;
        [SerializeField] private int defaultPoolSize = 20;
        [SerializeField] private int maxPoolSize = 100;
        [SerializeField] private bool preWarmPools = true;

        [Header("Performance")]
        [SerializeField] private int maxActiveParticles = 500;
        [SerializeField] private bool enableDistanceCulling = true;
        [SerializeField] private float cullingDistance = 100f;
        [SerializeField] private bool enableFrustumCulling = true;

        [Header("Quality")]
        [SerializeField] private VFXQualityLevel qualityLevel = VFXQualityLevel.High;
        [SerializeField] private float particleMultiplier = 1f; // Quality scaling

        [Header("Auto-Cleanup")]
        [SerializeField] private bool autoCleanup = true;
        [SerializeField] private float cleanupInterval = 10f;

        #endregion

        #region Private Fields

        private static ParticleVFXManager _instance;

        // Particle pools
        private readonly Dictionary<string, ParticlePool> _particlePools = new Dictionary<string, ParticlePool>();
        private readonly List<ParticleVFXInstance> _activeParticles = new List<ParticleVFXInstance>();

        // Camera reference
        private Camera _mainCamera;

        // Cleanup
        private float _lastCleanupTime = 0f;

        // Statistics
        private int _totalParticlesSpawned = 0;
        private int _totalParticlesPooled = 0;
        private int _totalParticlesCulled = 0;
        private int _poolHits = 0;

        // Events
        public event Action<string, Vector3> OnParticleSpawned;
        public event Action<ParticleVFXInstance> OnParticleReturned;
        public event Action<VFXQualityLevel> OnQualityChanged;

        #endregion

        #region Properties

        public static ParticleVFXManager Instance => _instance;
        public int ActiveParticleCount => _activeParticles.Count;
        public int PooledParticleCount => _particlePools.Sum(p => p.Value.availableCount);
        public int TotalPoolSize => _particlePools.Sum(p => p.Value.totalSize);

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
            UpdateActiveParticles();

            if (autoCleanup && Time.time - _lastCleanupTime >= cleanupInterval)
            {
                CleanupFinishedParticles();
                _lastCleanupTime = Time.time;
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[ParticleVFXManager] Initializing...");

            _mainCamera = Camera.main;

            Debug.Log("[ParticleVFXManager] Initialized");
        }

        #endregion

        #region Pool Management

        /// <summary>
        /// Register a particle prefab for pooling.
        /// </summary>
        public void RegisterParticlePrefab(string particleName, GameObject prefab, int poolSize = 0)
        {
            if (_particlePools.ContainsKey(particleName))
            {
                Debug.LogWarning($"[ParticleVFXManager] Particle already registered: {particleName}");
                return;
            }

            if (poolSize <= 0)
                poolSize = defaultPoolSize;

            var pool = new ParticlePool
            {
                prefab = prefab,
                totalSize = poolSize
            };

            _particlePools[particleName] = pool;

            if (preWarmPools)
            {
                PreWarmPool(pool, poolSize);
            }

            Debug.Log($"[ParticleVFXManager] Registered particle: {particleName} (pool size: {poolSize})");
        }

        private void PreWarmPool(ParticlePool pool, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var instance = CreateNewInstance(pool.prefab);
                instance.gameObject.SetActive(false);
                pool.available.Enqueue(instance);
                _totalParticlesPooled++;
            }
        }

        #endregion

        #region Particle Spawning

        /// <summary>
        /// Spawn a particle effect.
        /// </summary>
        public GameObject SpawnParticle(string particleName, Vector3 position, Quaternion rotation, Transform parent = null, float duration = -1f)
        {
            // Check active particle limit
            if (_activeParticles.Count >= maxActiveParticles)
            {
                Debug.LogWarning("[ParticleVFXManager] Max active particles reached");
                return null;
            }

            if (!_particlePools.TryGetValue(particleName, out var pool))
            {
                Debug.LogWarning($"[ParticleVFXManager] Particle not registered: {particleName}");
                return null;
            }

            ParticleVFXInstance instance = null;

            // Try to get from pool
            if (enablePooling && pool.available.Count > 0)
            {
                instance = pool.available.Dequeue();
                _poolHits++;
            }
            else
            {
                // Create new instance
                instance = CreateNewInstance(pool.prefab);

                if (pool.totalSize >= maxPoolSize)
                {
                    Debug.LogWarning($"[ParticleVFXManager] Max pool size reached for: {particleName}");
                }
                else
                {
                    pool.totalSize++;
                    _totalParticlesPooled++;
                }
            }

            // Configure instance
            instance.gameObject.SetActive(true);
            instance.transform.position = position;
            instance.transform.rotation = rotation;

            if (parent != null)
            {
                instance.transform.SetParent(parent);
            }

            instance.poolName = particleName;
            instance.spawnTime = Time.time;
            instance.duration = duration;

            // Auto-determine duration from particle system
            if (duration < 0 && instance.particleSystem != null)
            {
                instance.duration = instance.particleSystem.main.duration + instance.particleSystem.main.startLifetime.constantMax;
            }

            // Play particle system
            if (instance.particleSystem != null)
            {
                // Apply quality scaling
                ScaleParticleSystem(instance.particleSystem);
                instance.particleSystem.Play();
            }

            _activeParticles.Add(instance);
            _totalParticlesSpawned++;

            OnParticleSpawned?.Invoke(particleName, position);

            return instance.gameObject;
        }

        /// <summary>
        /// Spawn particle effect with auto-cleanup.
        /// </summary>
        public GameObject SpawnOneShot(string particleName, Vector3 position, Quaternion rotation = default, Transform parent = null)
        {
            if (rotation == default)
                rotation = Quaternion.identity;

            return SpawnParticle(particleName, position, rotation, parent, -1f);
        }

        private ParticleVFXInstance CreateNewInstance(GameObject prefab)
        {
            var go = Instantiate(prefab, transform);
            var instance = go.AddComponent<ParticleVFXInstance>();
            instance.particleSystem = go.GetComponent<ParticleSystem>();

            if (instance.particleSystem == null)
            {
                instance.particleSystem = go.GetComponentInChildren<ParticleSystem>();
            }

            return instance;
        }

        #endregion

        #region Particle Management

        private void UpdateActiveParticles()
        {
            for (int i = _activeParticles.Count - 1; i >= 0; i--)
            {
                var instance = _activeParticles[i];

                if (instance == null)
                {
                    _activeParticles.RemoveAt(i);
                    continue;
                }

                // Check if finished
                if (IsParticleFinished(instance))
                {
                    ReturnToPool(instance);
                    _activeParticles.RemoveAt(i);
                    continue;
                }

                // Distance culling
                if (enableDistanceCulling && _mainCamera != null)
                {
                    float distance = Vector3.Distance(instance.transform.position, _mainCamera.transform.position);

                    if (distance > cullingDistance)
                    {
                        if (instance.particleSystem != null && instance.particleSystem.isPlaying)
                        {
                            instance.particleSystem.Pause();
                            _totalParticlesCulled++;
                        }
                    }
                    else
                    {
                        if (instance.particleSystem != null && instance.particleSystem.isPaused)
                        {
                            instance.particleSystem.Play();
                        }
                    }
                }

                // Frustum culling
                if (enableFrustumCulling && _mainCamera != null)
                {
                    var renderer = instance.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                    {
                        bool isVisible = GeometryUtility.TestPlanesAABB(
                            GeometryUtility.CalculateFrustumPlanes(_mainCamera),
                            renderer.bounds
                        );

                        if (!isVisible && instance.particleSystem != null && instance.particleSystem.isPlaying)
                        {
                            instance.particleSystem.Pause();
                        }
                        else if (isVisible && instance.particleSystem != null && instance.particleSystem.isPaused)
                        {
                            instance.particleSystem.Play();
                        }
                    }
                }
            }
        }

        private bool IsParticleFinished(ParticleVFXInstance instance)
        {
            // Duration-based check
            if (instance.duration > 0 && Time.time - instance.spawnTime >= instance.duration)
            {
                return true;
            }

            // Particle system check
            if (instance.particleSystem != null)
            {
                return !instance.particleSystem.isPlaying && instance.particleSystem.particleCount == 0;
            }

            return false;
        }

        private void ReturnToPool(ParticleVFXInstance instance)
        {
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(transform);

            if (instance.particleSystem != null)
            {
                instance.particleSystem.Stop();
                instance.particleSystem.Clear();
            }

            if (_particlePools.TryGetValue(instance.poolName, out var pool))
            {
                pool.available.Enqueue(instance);
            }
            else
            {
                // Pool doesn't exist anymore, destroy
                Destroy(instance.gameObject);
            }

            OnParticleReturned?.Invoke(instance);
        }

        private void CleanupFinishedParticles()
        {
            int cleanedUp = 0;

            for (int i = _activeParticles.Count - 1; i >= 0; i--)
            {
                var instance = _activeParticles[i];

                if (instance == null || IsParticleFinished(instance))
                {
                    if (instance != null)
                    {
                        ReturnToPool(instance);
                    }

                    _activeParticles.RemoveAt(i);
                    cleanedUp++;
                }
            }

            if (cleanedUp > 0)
            {
                Debug.Log($"[ParticleVFXManager] Cleaned up {cleanedUp} finished particles");
            }
        }

        #endregion

        #region Quality Management

        /// <summary>
        /// Set VFX quality level.
        /// </summary>
        public void SetQualityLevel(VFXQualityLevel level)
        {
            if (qualityLevel == level) return;

            qualityLevel = level;

            // Update particle multiplier
            particleMultiplier = level switch
            {
                VFXQualityLevel.Low => 0.25f,
                VFXQualityLevel.Medium => 0.5f,
                VFXQualityLevel.High => 1f,
                VFXQualityLevel.Ultra => 1.5f,
                _ => 1f
            };

            // Update all active particles
            foreach (var instance in _activeParticles)
            {
                if (instance.particleSystem != null)
                {
                    ScaleParticleSystem(instance.particleSystem);
                }
            }

            OnQualityChanged?.Invoke(level);

            Debug.Log($"[ParticleVFXManager] Quality level set to: {level}");
        }

        private void ScaleParticleSystem(ParticleSystem ps)
        {
            var main = ps.main;
            main.maxParticles = Mathf.RoundToInt(main.maxParticles * particleMultiplier);

            var emission = ps.emission;
            emission.rateOverTimeMultiplier *= particleMultiplier;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Stop all active particles.
        /// </summary>
        public void StopAllParticles()
        {
            foreach (var instance in _activeParticles)
            {
                if (instance?.particleSystem != null)
                {
                    instance.particleSystem.Stop();
                }
            }

            Debug.Log("[ParticleVFXManager] All particles stopped");
        }

        /// <summary>
        /// Clear all pools.
        /// </summary>
        public void ClearPools()
        {
            foreach (var pool in _particlePools.Values)
            {
                while (pool.available.Count > 0)
                {
                    var instance = pool.available.Dequeue();
                    if (instance != null)
                    {
                        Destroy(instance.gameObject);
                    }
                }
            }

            _particlePools.Clear();

            Debug.Log("[ParticleVFXManager] All pools cleared");
        }

        /// <summary>
        /// Get VFX manager statistics.
        /// </summary>
        public VFXManagerStats GetStats()
        {
            return new VFXManagerStats
            {
                activeParticles = _activeParticles.Count,
                pooledParticles = PooledParticleCount,
                totalPoolSize = TotalPoolSize,
                totalParticlesSpawned = _totalParticlesSpawned,
                totalParticlesPooled = _totalParticlesPooled,
                totalParticlesCulled = _totalParticlesCulled,
                poolHits = _poolHits,
                qualityLevel = qualityLevel,
                particleMultiplier = particleMultiplier
            };
        }

        #endregion

        #region Context Menu

        [ContextMenu("Stop All Particles")]
        private void StopAllParticlesMenu()
        {
            StopAllParticles();
        }

        [ContextMenu("Cleanup Finished")]
        private void CleanupFinishedMenu()
        {
            CleanupFinishedParticles();
        }

        [ContextMenu("Clear Pools")]
        private void ClearPoolsMenu()
        {
            ClearPools();
        }

        [ContextMenu("Set Quality: Low")]
        private void SetQualityLow()
        {
            SetQualityLevel(VFXQualityLevel.Low);
        }

        [ContextMenu("Set Quality: Ultra")]
        private void SetQualityUltra()
        {
            SetQualityLevel(VFXQualityLevel.Ultra);
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Particle VFX Manager Statistics ===\n" +
                      $"Active Particles: {stats.activeParticles}\n" +
                      $"Pooled Particles: {stats.pooledParticles}\n" +
                      $"Total Pool Size: {stats.totalPoolSize}\n" +
                      $"Total Spawned: {stats.totalParticlesSpawned}\n" +
                      $"Total Pooled: {stats.totalParticlesPooled}\n" +
                      $"Total Culled: {stats.totalParticlesCulled}\n" +
                      $"Pool Hits: {stats.poolHits}\n" +
                      $"Quality: {stats.qualityLevel}\n" +
                      $"Particle Multiplier: {stats.particleMultiplier:F2}x");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Particle pool.
    /// </summary>
    public class ParticlePool
    {
        public GameObject prefab;
        public Queue<ParticleVFXInstance> available = new Queue<ParticleVFXInstance>();
        public int totalSize;

        public int availableCount => available.Count;
    }

    /// <summary>
    /// Particle VFX instance.
    /// </summary>
    public class ParticleVFXInstance : MonoBehaviour
    {
        public ParticleSystem particleSystem;
        public string poolName;
        public float spawnTime;
        public float duration;
    }

    /// <summary>
    /// VFX quality levels.
    /// </summary>
    public enum VFXQualityLevel
    {
        Low,
        Medium,
        High,
        Ultra
    }

    /// <summary>
    /// VFX manager statistics.
    /// </summary>
    [Serializable]
    public struct VFXManagerStats
    {
        public int activeParticles;
        public int pooledParticles;
        public int totalPoolSize;
        public int totalParticlesSpawned;
        public int totalParticlesPooled;
        public int totalParticlesCulled;
        public int poolHits;
        public VFXQualityLevel qualityLevel;
        public float particleMultiplier;
    }

    #endregion
}
