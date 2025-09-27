#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Chimera.Configuration;
using Laboratory.Core.ECS;
using Laboratory.Core.Configuration;
using System.Collections;

namespace Laboratory.Chimera.Spawning
{
    /// <summary>
    /// Designer-friendly creature spawner that works with ScriptableObject configurations.
    /// Drop this into scenes for easy creature population setup.
    /// </summary>
    public class CreatureSpawnerAuthoring : MonoBehaviour
    {
        [Header("🎯 Spawning Configuration")]
        [SerializeField] public Laboratory.Core.Configuration.ChimeraGameConfig gameConfig;
        [SerializeField] public SpawnMode spawnMode = SpawnMode.OnStart;

        [Header("📊 Population Settings")]
        [SerializeField] [Range(1, 1000)] public int targetPopulation = 50;
        [SerializeField] [Range(0f, 100f)] public float spawnRadius = 50f;
        [SerializeField] public LayerMask groundLayer = 1;

        [Header("🧬 Species Distribution")]
        [SerializeField] public SpeciesSpawnWeight[] speciesWeights = new SpeciesSpawnWeight[0];

        [Header("⚡ Performance")]
        [SerializeField] [Range(1, 100)] public int spawnBatchSize = 10;
        [SerializeField] [Range(0.1f, 5f)] public float spawnInterval = 0.1f;
        [SerializeField] public bool enableContinuousSpawning = false;
        [SerializeField] [Range(10f, 300f)] public float respawnCheckInterval = 30f;

        [Header("🎨 Randomization")]
        [SerializeField] public bool randomizeGenetics = true;
        [SerializeField] public bool randomizeAI = true;
        [SerializeField] [Range(0.1f, 2f)] public float scaleVariation = 0.2f;

        [Header("📍 Spawn Areas")]
        [SerializeField] public Transform[] spawnPoints = new Transform[0];
        [SerializeField] public bool useSpawnPointsOnly = false;

        [Header("📊 Runtime Status")]
        [SerializeField] private int currentPopulation = 0;
        [SerializeField] private int totalSpawned = 0;
        [SerializeField] private bool spawningInProgress = false;

        // Runtime data
        private EntityManager entityManager;
        private Entity spawnerEntity;
        private System.Collections.Generic.List<Entity> spawnedCreatures = new System.Collections.Generic.List<Entity>();

        [System.Serializable]
        public struct SpeciesSpawnWeight
        {
            public ChimeraSpeciesConfig species;
            [Range(0f, 1f)] public float weight;
            [Range(1, 100)] public int maxCount;
        }

        public enum SpawnMode
        {
            OnStart,
            OnTrigger,
            Continuous,
            Manual
        }

        private void Start()
        {
            InitializeSpawner();

            if (spawnMode == SpawnMode.OnStart)
            {
                StartCoroutine(SpawnInitialPopulation());
            }
            else if (spawnMode == SpawnMode.Continuous || enableContinuousSpawning)
            {
                StartCoroutine(ContinuousSpawningLoop());
            }
        }

        private void InitializeSpawner()
        {
            entityManager = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;

            // Validate configuration
            if (gameConfig == null)
            {
                UnityEngine.Debug.LogError($"CreatureSpawnerAuthoring: No game config assigned to {name}!");
                return;
            }

            if (speciesWeights.Length == 0)
            {
                UnityEngine.Debug.LogWarning($"CreatureSpawnerAuthoring: No species weights configured on {name}. Auto-populating from game config.");
                AutoConfigureSpeciesWeights();
            }

            // Create spawner entity for ECS integration
            spawnerEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(spawnerEntity, new CreatureSpawnerData
            {
                targetPopulation = targetPopulation,
                currentPopulation = 0,
                spawnRadius = spawnRadius,
                isActive = true
            });

            UnityEngine.Debug.Log($"✅ CreatureSpawner '{name}' initialized with {speciesWeights.Length} species types");
        }

        private void AutoConfigureSpeciesWeights()
        {
            if (gameConfig?.availableSpecies == null) return;

            var weights = new System.Collections.Generic.List<SpeciesSpawnWeight>();
            float equalWeight = 1f / gameConfig.availableSpecies.Length;

            foreach (var species in gameConfig.availableSpecies)
            {
                if (species != null && species is ChimeraSpeciesConfig speciesConfig)
                {
                    weights.Add(new SpeciesSpawnWeight
                    {
                        species = speciesConfig,
                        weight = equalWeight,
                        maxCount = targetPopulation / gameConfig.availableSpecies.Length
                    });
                }
            }

            speciesWeights = weights.ToArray();
        }

        private IEnumerator SpawnInitialPopulation()
        {
            spawningInProgress = true;
            UnityEngine.Debug.Log($"🐾 Starting initial population spawn: {targetPopulation} creatures");

            int spawned = 0;
            while (spawned < targetPopulation)
            {
                int batchSize = Mathf.Min(spawnBatchSize, targetPopulation - spawned);

                for (int i = 0; i < batchSize; i++)
                {
                    if (SpawnRandomCreature())
                    {
                        spawned++;
                        totalSpawned++;
                    }
                }

                currentPopulation = spawned;

                // Update spawner entity
                if (entityManager.Exists(spawnerEntity))
                {
                    var spawnerData = entityManager.GetComponentData<CreatureSpawnerData>(spawnerEntity);
                    spawnerData.currentPopulation = currentPopulation;
                    entityManager.SetComponentData(spawnerEntity, spawnerData);
                }

                yield return new WaitForSeconds(spawnInterval);
            }

            spawningInProgress = false;
            UnityEngine.Debug.Log($"✅ Initial population spawning complete: {spawned} creatures spawned");
        }

        private IEnumerator ContinuousSpawningLoop()
        {
            while (enableContinuousSpawning)
            {
                yield return new WaitForSeconds(respawnCheckInterval);

                if (!spawningInProgress)
                {
                    CheckAndMaintainPopulation();
                }
            }
        }

        private void CheckAndMaintainPopulation()
        {
            // Remove destroyed/invalid entities from tracking
            spawnedCreatures.RemoveAll(entity => !entityManager.Exists(entity));
            currentPopulation = spawnedCreatures.Count;

            // Spawn new creatures if below target
            int deficit = targetPopulation - currentPopulation;
            if (deficit > 0)
            {
                StartCoroutine(SpawnDeficitCreatures(deficit));
            }
        }

        private IEnumerator SpawnDeficitCreatures(int count)
        {
            spawningInProgress = true;

            for (int i = 0; i < count; i++)
            {
                if (SpawnRandomCreature())
                {
                    totalSpawned++;
                }

                if (i % spawnBatchSize == 0)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            spawningInProgress = false;
        }

        private bool SpawnRandomCreature()
        {
            // Select species based on weights
            var selectedSpecies = SelectRandomSpecies();
            if (selectedSpecies == null) return false;

            // Find spawn position
            var spawnPosition = GetRandomSpawnPosition();
            if (spawnPosition == Vector3.zero) return false;

            // Create creature entity
            var creatureEntity = CreateCreatureEntity(selectedSpecies, spawnPosition);
            if (creatureEntity == Entity.Null) return false;

            // Track spawned creature
            spawnedCreatures.Add(creatureEntity);
            currentPopulation++;

            return true;
        }

        private ChimeraSpeciesConfig SelectRandomSpecies()
        {
            if (speciesWeights.Length == 0) return null;

            // Calculate total weight
            float totalWeight = 0f;
            foreach (var weight in speciesWeights)
            {
                if (weight.species != null)
                    totalWeight += weight.weight;
            }

            if (totalWeight <= 0f) return null;

            // Random selection based on weight
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var weight in speciesWeights)
            {
                if (weight.species == null) continue;

                currentWeight += weight.weight;
                if (randomValue <= currentWeight)
                {
                    // Check if we haven't exceeded max count for this species
                    int currentCount = CountSpecies(weight.species);
                    if (currentCount < weight.maxCount)
                    {
                        return weight.species;
                    }
                }
            }

            return speciesWeights[0].species; // Fallback
        }

        private int CountSpecies(ChimeraSpeciesConfig species)
        {
            int count = 0;
            int speciesID = species.speciesName.GetHashCode();

            foreach (var entity in spawnedCreatures)
            {
                if (entityManager.Exists(entity) && entityManager.HasComponent<CreatureData>(entity))
                {
                    var data = entityManager.GetComponentData<CreatureData>(entity);
                    if (data.speciesID == speciesID && data.isAlive)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 basePosition = transform.position;

            // Use spawn points if configured
            if (useSpawnPointsOnly && spawnPoints.Length > 0)
            {
                var randomPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                return randomPoint != null ? randomPoint.position : basePosition;
            }

            // Random position within radius
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 randomPosition = basePosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // Raycast to ground
            if (Physics.Raycast(randomPosition + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, groundLayer))
            {
                return hit.point;
            }

            return randomPosition; // Fallback to flat position
        }

        private Entity CreateCreatureEntity(ChimeraSpeciesConfig species, Vector3 position)
        {
            // Create entity with archetype
            var entity = entityManager.CreateEntity();

            // Add required components
            entityManager.AddComponentData(entity, new CreatureData
            {
                speciesID = species.speciesName.GetHashCode(),
                generation = 1,
                age = 0,
                geneticSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue),
                isAlive = true
            });

            entityManager.AddComponentData(entity, new Unity.Transforms.LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = UnityEngine.Random.Range(1f - scaleVariation, 1f + scaleVariation)
            });

            entityManager.AddComponent<CreatureSimulationTag>(entity);

            // Add other required components...
            // (This would be expanded based on the full creature system)

            return entity;
        }

        /// <summary>
        /// Manual spawning trigger for designer/debug use
        /// </summary>
        [ContextMenu("Spawn Batch")]
        public void ManualSpawnBatch()
        {
            if (!Application.isPlaying) return;

            StartCoroutine(SpawnBatchCoroutine(spawnBatchSize));
        }

        private IEnumerator SpawnBatchCoroutine(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnRandomCreature();
                if (i % 5 == 0) yield return null; // Spread over frames
            }
        }

        /// <summary>
        /// Clear all spawned creatures
        /// </summary>
        [ContextMenu("Clear All Creatures")]
        public void ClearAllCreatures()
        {
            foreach (var entity in spawnedCreatures)
            {
                if (entityManager.Exists(entity))
                {
                    entityManager.DestroyEntity(entity);
                }
            }

            spawnedCreatures.Clear();
            currentPopulation = 0;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw spawn radius
            Gizmos.color = Color.green;
#if UNITY_EDITOR
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, spawnRadius);
#endif

            // Draw spawn points
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Gizmos.color = Color.yellow;
                foreach (var point in spawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 2f);
                        Gizmos.DrawLine(transform.position, point.position);
                    }
                }
            }

            // Draw population info
            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                var pos = transform.position + Vector3.up * 5f;
                Gizmos.DrawWireCube(pos, Vector3.one);
            }
        }

        private void OnValidate()
        {
            if (targetPopulation > gameConfig?.maxSimultaneousCreatures)
            {
                targetPopulation = gameConfig?.maxSimultaneousCreatures ?? 500;
            }

            // Ensure spawn batch size doesn't exceed target population
            spawnBatchSize = Mathf.Min(spawnBatchSize, targetPopulation);
        }
#endif
    }

    // ECS component for the spawner
    public struct CreatureSpawnerData : IComponentData
    {
        public int targetPopulation;
        public int currentPopulation;
        public float spawnRadius;
        public bool isActive;
    }
}