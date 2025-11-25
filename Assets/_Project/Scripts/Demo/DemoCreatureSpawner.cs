using UnityEngine;
using System.Collections.Generic;
using Laboratory.Subsystems.Spawning;

namespace Laboratory.Demo
{
    /// <summary>
    /// Demo creature spawner for testing object pooling
    /// Spawns creatures in patterns to validate 1000+ creatures @ 60 FPS
    /// </summary>
    public class DemoCreatureSpawner : MonoBehaviour
    {
        [Header("Spawning Configuration")]
        [SerializeField] private GameObject[] creaturePrefabs;
        [SerializeField] private int initialSpawnCount = 50;
        [SerializeField] private int maxCreatures = 1000;
        [SerializeField] private float spawnRadius = 50f;
        [SerializeField] private bool autoSpawnOnStart = true;

        [Header("Spawn Pattern")]
        [SerializeField] private SpawnPattern pattern = SpawnPattern.Random;
        [SerializeField] private float gridSpacing = 2f;
        [SerializeField] private float circleRadius = 25f;

        [Header("Performance Testing")]
        [SerializeField] private bool enablePerformanceTest = false;
        [SerializeField] private int performanceTestCount = 100;
        [SerializeField] private float performanceTestInterval = 0.5f;

        // State
        private List<GameObject> _spawnedCreatures = new List<GameObject>();
        private SpawningSubsystemManager _spawningSubsystem;
        private float _nextPerformanceSpawn = 0f;
        private int _performanceSpawnedCount = 0;

        // Statistics
        private int _totalSpawned = 0;
        private int _totalRecycled = 0;

        private void Start()
        {
            _spawningSubsystem = SpawningSubsystemManager.Instance;

            if (_spawningSubsystem == null)
            {
                Debug.LogError("[DemoSpawner] Spawning subsystem not found!");
                return;
            }

            if (autoSpawnOnStart)
            {
                SpawnInitialCreatures();
            }
        }

        private void Update()
        {
            // Handle input for manual spawning
            if (Input.GetKeyDown(KeyCode.G))
            {
                SpawnCreature(GetRandomPosition());
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                RecycleAllCreatures();
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                SpawnWave(10);
            }

            // Performance test spawning
            if (enablePerformanceTest && Time.time >= _nextPerformanceSpawn)
            {
                if (_performanceSpawnedCount < performanceTestCount)
                {
                    SpawnWave(10);
                    _performanceSpawnedCount += 10;
                    _nextPerformanceSpawn = Time.time + performanceTestInterval;
                }
            }
        }

        /// <summary>
        /// Spawn initial creatures
        /// </summary>
        private void SpawnInitialCreatures()
        {
            Debug.Log($"[DemoSpawner] Spawning {initialSpawnCount} initial creatures...");

            for (int i = 0; i < initialSpawnCount; i++)
            {
                Vector3 position = GetSpawnPosition(i, initialSpawnCount);
                SpawnCreature(position);
            }

            Debug.Log($"[DemoSpawner] Spawned {initialSpawnCount} creatures. Total: {_spawnedCreatures.Count}");
        }

        /// <summary>
        /// Spawn a single creature
        /// </summary>
        public GameObject SpawnCreature(Vector3 position)
        {
            if (_spawnedCreatures.Count >= maxCreatures)
            {
                Debug.LogWarning($"[DemoSpawner] Max creature count reached ({maxCreatures})");
                return null;
            }

            if (creaturePrefabs == null || creaturePrefabs.Length == 0)
            {
                Debug.LogWarning("[DemoSpawner] No creature prefabs assigned!");
                return null;
            }

            // Get random prefab
            GameObject prefab = creaturePrefabs[Random.Range(0, creaturePrefabs.Length)];

            // Spawn using spawning subsystem
            GameObject creature = _spawningSubsystem.Spawn(prefab, position, Quaternion.identity);

            if (creature != null)
            {
                _spawnedCreatures.Add(creature);
                _totalSpawned++;
            }

            return creature;
        }

        /// <summary>
        /// Spawn a wave of creatures
        /// </summary>
        public void SpawnWave(int count)
        {
            Debug.Log($"[DemoSpawner] Spawning wave of {count} creatures...");

            for (int i = 0; i < count; i++)
            {
                Vector3 position = GetRandomPosition();
                SpawnCreature(position);
            }

            Debug.Log($"[DemoSpawner] Wave spawned. Total creatures: {_spawnedCreatures.Count}");
        }

        /// <summary>
        /// Recycle a specific creature
        /// </summary>
        public void RecycleCreature(GameObject creature)
        {
            if (creature == null) return;

            _spawningSubsystem.Recycle(creature);
            _spawnedCreatures.Remove(creature);
            _totalRecycled++;
        }

        /// <summary>
        /// Recycle all spawned creatures
        /// </summary>
        public void RecycleAllCreatures()
        {
            Debug.Log($"[DemoSpawner] Recycling all {_spawnedCreatures.Count} creatures...");

            foreach (GameObject creature in _spawnedCreatures)
            {
                if (creature != null)
                {
                    _spawningSubsystem.Recycle(creature);
                    _totalRecycled++;
                }
            }

            _spawnedCreatures.Clear();

            Debug.Log("[DemoSpawner] All creatures recycled");
        }

        /// <summary>
        /// Get spawn position based on pattern
        /// </summary>
        private Vector3 GetSpawnPosition(int index, int totalCount)
        {
            switch (pattern)
            {
                case SpawnPattern.Random:
                    return GetRandomPosition();

                case SpawnPattern.Grid:
                    return GetGridPosition(index);

                case SpawnPattern.Circle:
                    return GetCirclePosition(index, totalCount);

                case SpawnPattern.Sphere:
                    return GetSpherePosition();

                default:
                    return GetRandomPosition();
            }
        }

        /// <summary>
        /// Get random position within spawn radius
        /// </summary>
        private Vector3 GetRandomPosition()
        {
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

        /// <summary>
        /// Get grid position
        /// </summary>
        private Vector3 GetGridPosition(int index)
        {
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(maxCreatures));
            int x = index % gridSize;
            int z = index / gridSize;

            Vector3 offset = new Vector3(x * gridSpacing, 0f, z * gridSpacing);
            offset -= new Vector3(gridSize * gridSpacing * 0.5f, 0f, gridSize * gridSpacing * 0.5f);

            return transform.position + offset;
        }

        /// <summary>
        /// Get circle position
        /// </summary>
        private Vector3 GetCirclePosition(int index, int totalCount)
        {
            float angle = (index / (float)totalCount) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * circleRadius;
            float z = Mathf.Sin(angle) * circleRadius;

            return transform.position + new Vector3(x, 0f, z);
        }

        /// <summary>
        /// Get sphere position
        /// </summary>
        private Vector3 GetSpherePosition()
        {
            return transform.position + Random.insideUnitSphere * spawnRadius;
        }

        /// <summary>
        /// Get spawn statistics
        /// </summary>
        public SpawnStats GetStats()
        {
            return new SpawnStats
            {
                ActiveCount = _spawnedCreatures.Count,
                TotalSpawned = _totalSpawned,
                TotalRecycled = _totalRecycled,
                MaxCreatures = maxCreatures
            };
        }

        private void OnDrawGizmosSelected()
        {
            // Draw spawn radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            // Draw circle pattern preview
            if (pattern == SpawnPattern.Circle)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, circleRadius);
            }
        }
    }

    /// <summary>
    /// Spawn pattern types
    /// </summary>
    public enum SpawnPattern
    {
        Random,
        Grid,
        Circle,
        Sphere
    }

    /// <summary>
    /// Spawn statistics
    /// </summary>
    public struct SpawnStats
    {
        public int ActiveCount;
        public int TotalSpawned;
        public int TotalRecycled;
        public int MaxCreatures;
    }
}
