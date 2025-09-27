using UnityEngine;
using Unity.Entities;
using Laboratory.Core.Configuration;
using System.Collections;

namespace Laboratory.Core.Bootstrap
{
    /// <summary>
    /// One-click scene bootstrap that initializes all Chimera systems.
    /// Drop this prefab into any scene to get full Project Chimera integration.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class ChimeraSceneBootstrapper : MonoBehaviour
    {
        [Header("🎮 Master Configuration")]
        [SerializeField] private ChimeraGameConfig gameConfig;

        [Header("🔧 Bootstrap Settings")]
        [SerializeField] private bool autoStartSystems = true;
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool spawnTestCreatures = false;
        [SerializeField] [Range(0, 50)] private int testCreatureCount = 10;

        [Header("📊 Runtime Status")]
        [SerializeField] private bool systemsInitialized = false;
        [SerializeField] private float initializationTime = 0f;

        // System references
        private World ecsWorld;
        private MonoBehaviour chimeraBootstrap;
        private MonoBehaviour pathfindingSystem;
        private MonoBehaviour aiManager;
        private GameObject debugManagerObject;

        private void Awake()
        {
            if (gameConfig == null)
            {
                Debug.LogError("ChimeraSceneBootstrapper: No game config assigned! Create a ChimeraGameConfig asset and assign it.");
                return;
            }

            if (autoStartSystems)
            {
                StartCoroutine(InitializeSystemsCoroutine());
            }
        }

        /// <summary>
        /// Initialize all systems in the correct order
        /// </summary>
        private IEnumerator InitializeSystemsCoroutine()
        {
            float startTime = Time.time;

            if (enableDebugLogging)
                Debug.Log("🚀 ChimeraSceneBootstrapper: Starting system initialization...");

            // 1. Core ECS World Setup
            yield return StartCoroutine(InitializeECSWorld());

            // 2. Debug & Monitoring
            yield return StartCoroutine(InitializeDebugSystems());

            // 3. AI & Pathfinding
            yield return StartCoroutine(InitializeAISystems());

            // 4. Chimera-specific systems
            yield return StartCoroutine(InitializeChimeraSystems());

            // 5. Test creature spawning (optional)
            if (spawnTestCreatures && testCreatureCount > 0)
            {
                yield return StartCoroutine(SpawnTestCreatures());
            }

            // Finalize
            initializationTime = Time.time - startTime;
            systemsInitialized = true;

            if (enableDebugLogging)
                Debug.Log($"✅ ChimeraSceneBootstrapper: All systems initialized in {initializationTime:F2}s");

            // Set target framerate
            Application.targetFrameRate = gameConfig.targetFramerate;
        }

        private IEnumerator InitializeECSWorld()
        {
            if (enableDebugLogging) Debug.Log("🔧 Initializing ECS World...");

            ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (ecsWorld == null)
            {
                Debug.LogError("ChimeraSceneBootstrapper: No default ECS world found!");
                yield break;
            }

            yield return null; // Wait one frame
        }

        private IEnumerator InitializeDebugSystems()
        {
            if (enableDebugLogging) Debug.Log("🔍 Initializing Debug Systems...");

            // Debug manager integration disabled to avoid namespace conflicts
            // Use Unity's built-in Debug.Log for debugging instead
            if (enableDebugLogging && gameConfig.enableDebugMode)
            {
                UnityEngine.Debug.Log("Debug mode enabled - using Unity's built-in debugging");
            }

            yield return null;
        }

        private IEnumerator InitializeAISystems()
        {
            if (enableDebugLogging) Debug.Log("🤖 Initializing AI Systems...");

            // Initialize pathfinding using reflection
            var pathfindingType = System.Type.GetType("Laboratory.AI.Pathfinding.EnhancedPathfindingSystem");
            pathfindingSystem = pathfindingType != null ? FindFirstObjectByType(pathfindingType) as MonoBehaviour : null;
            if (pathfindingSystem == null && pathfindingType != null)
            {
                var pathfindingGO = new GameObject("Enhanced Pathfinding System");
                pathfindingSystem = pathfindingGO.AddComponent(pathfindingType) as MonoBehaviour;
            }

            // Initialize AI manager using reflection
            var aiManagerType = System.Type.GetType("Laboratory.Chimera.AI.ChimeraAIManager");
            aiManager = aiManagerType != null ? FindFirstObjectByType(aiManagerType) as MonoBehaviour : null;
            if (aiManager == null && aiManagerType != null)
            {
                var aiGO = new GameObject("Chimera AI Manager");
                aiManager = aiGO.AddComponent(aiManagerType) as MonoBehaviour;
            }

            yield return null;
        }

        private IEnumerator InitializeChimeraSystems()
        {
            if (enableDebugLogging) Debug.Log("🧬 Initializing Chimera Systems...");

            // Initialize creature ECS bootstrap
            var bootstrapType = System.Type.GetType("Laboratory.Core.ECS.ChimeraSceneBootstrap");
            chimeraBootstrap = bootstrapType != null ? FindFirstObjectByType(bootstrapType) as MonoBehaviour : null;
            if (chimeraBootstrap == null && bootstrapType != null)
            {
                // Use reflection to find and add the bootstrap component
                var chimeraGO = new GameObject("Chimera ECS Bootstrap");
                chimeraBootstrap = chimeraGO.AddComponent(bootstrapType) as MonoBehaviour;
            }

            yield return null;
        }

        private IEnumerator SpawnTestCreatures()
        {
            if (enableDebugLogging) Debug.Log($"🐾 Spawning {testCreatureCount} test creatures...");

            if (gameConfig.availableSpecies.Length == 0)
            {
                Debug.LogWarning("ChimeraSceneBootstrapper: No species available for test spawning!");
                yield break;
            }

            // Get first available species
            var testSpecies = gameConfig.availableSpecies[0];
            if (testSpecies == null)
            {
                Debug.LogWarning("ChimeraSceneBootstrapper: Test species is null!");
                yield break;
            }

            // Spawn creatures in a circle around origin
            float radius = 20f;
            for (int i = 0; i < testCreatureCount; i++)
            {
                float angle = (i / (float)testCreatureCount) * Mathf.PI * 2f;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );

                // Simple instantiation - in production this would go through spawning system
                // Use reflection to get visualPrefab to avoid assembly dependency
                var visualPrefabField = testSpecies.GetType().GetField("visualPrefab");
                var visualPrefab = visualPrefabField?.GetValue(testSpecies) as GameObject;

                if (visualPrefab != null)
                {
                    var creature = Instantiate(visualPrefab, position, Quaternion.identity);
                    creature.name = $"Test_{testSpecies.name}_{i}";
                }

                // Spread spawning over multiple frames to prevent frame drops
                if (i % 5 == 0) yield return null;
            }
        }

        /// <summary>
        /// Public API to reinitialize systems (for runtime configuration changes)
        /// </summary>
        [ContextMenu("Reinitialize Systems")]
        public void ReinitializeSystems()
        {
            if (systemsInitialized)
            {
                StopAllCoroutines();
                systemsInitialized = false;
                StartCoroutine(InitializeSystemsCoroutine());
            }
        }

        /// <summary>
        /// Get runtime system health status
        /// </summary>
        public bool GetSystemHealth()
        {
            return systemsInitialized &&
                   ecsWorld != null && ecsWorld.IsCreated &&
                   (debugManagerObject != null || !gameConfig.enableDebugMode);
        }

        private void OnValidate()
        {
            if (testCreatureCount > gameConfig?.maxSimultaneousCreatures)
            {
                testCreatureCount = gameConfig.maxSimultaneousCreatures;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (spawnTestCreatures && testCreatureCount > 0)
            {
                // Draw spawn radius using wire sphere (DrawWireCircle doesn't exist in all Unity versions)
                Gizmos.color = Color.cyan;
                UnityEditor.Handles.color = Color.cyan;
                UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, 20f);

                // Draw spawn positions
                Gizmos.color = Color.yellow;
                float radius = 20f;
                for (int i = 0; i < testCreatureCount; i++)
                {
                    float angle = (i / (float)testCreatureCount) * Mathf.PI * 2f;
                    Vector3 position = transform.position + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius
                    );
                    Gizmos.DrawSphere(position, 0.5f);
                }
            }
        }
#endif
    }
}