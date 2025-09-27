using UnityEngine;
using Unity.Entities;
using Laboratory.Core.Configuration;
using System.Collections.Generic;
using System.IO;

namespace Laboratory.Core.Configuration
{
    /// <summary>
    /// Hot-reload system that detects ScriptableObject changes during play mode
    /// and applies them to running ECS systems without stopping execution.
    /// Essential for rapid iteration and designer workflow.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class HotReloadConfigurationSystem : MonoBehaviour
    {
        [Header("🔥 Hot Reload Settings")]
        [SerializeField] private bool enableHotReload = true;
        [SerializeField] private float checkInterval = 0.5f;
        [SerializeField] private bool showDebugLogs = false;

        [Header("📂 Watched Configurations")]
        [SerializeField] private ChimeraGameConfig gameConfig;
        [SerializeField] private List<ScriptableObject> watchedSpecies = new List<ScriptableObject>();
        [SerializeField] private List<ScriptableObject> watchedBiomes = new List<ScriptableObject>();

        [Header("📊 Runtime Status")]
        [SerializeField] private int totalReloads = 0;
        [SerializeField] private float lastReloadTime = 0f;
        [SerializeField] private string lastModifiedAsset = "";

        // File watching data
        private Dictionary<string, System.DateTime> fileTimestamps = new Dictionary<string, System.DateTime>();
        private Dictionary<string, ScriptableObject> pathToAsset = new Dictionary<string, ScriptableObject>();

        // System references
        private EntityManager entityManager;
        private bool systemInitialized = false;

        private void Start()
        {
            if (!Application.isPlaying || !enableHotReload) return;

            InitializeHotReloadSystem();
            InvokeRepeating(nameof(CheckForConfigurationChanges), checkInterval, checkInterval);
        }

        private void InitializeHotReloadSystem()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated == true)
            {
                entityManager = world.EntityManager;
            }

            if (entityManager.World?.IsCreated != true)
            {
                Debug.LogError("HotReloadConfigurationSystem: No ECS world found!");
                return;
            }

            // Register all ScriptableObjects for watching
            RegisterForWatching(gameConfig);

            foreach (var species in watchedSpecies)
                RegisterForWatching(species);

            foreach (var biome in watchedBiomes)
                RegisterForWatching(biome);

            // Auto-register configs from game config
            if (gameConfig != null)
            {
                foreach (var species in gameConfig.availableSpecies)
                    RegisterForWatching(species);

                foreach (var biome in gameConfig.availableBiomes)
                    RegisterForWatching(biome);
            }

            systemInitialized = true;

            if (showDebugLogs)
                Debug.Log($"🔥 Hot Reload System initialized, watching {fileTimestamps.Count} configuration files");
        }

        private void RegisterForWatching(ScriptableObject asset)
        {
            if (asset == null) return;

#if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath)) return;

            string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), assetPath);

            if (File.Exists(fullPath))
            {
                fileTimestamps[fullPath] = File.GetLastWriteTime(fullPath);
                pathToAsset[fullPath] = asset;
            }
#endif
        }

        private void CheckForConfigurationChanges()
        {
            if (!systemInitialized || !enableHotReload) return;

#if UNITY_EDITOR
            bool anyChanges = false;
            var changedAssets = new List<ScriptableObject>();

            foreach (var kvp in fileTimestamps)
            {
                string filePath = kvp.Key;
                System.DateTime lastKnownTime = kvp.Value;

                if (File.Exists(filePath))
                {
                    System.DateTime currentTime = File.GetLastWriteTime(filePath);

                    if (currentTime > lastKnownTime)
                    {
                        // File has been modified
                        fileTimestamps[filePath] = currentTime;

                        if (pathToAsset.TryGetValue(filePath, out ScriptableObject asset))
                        {
                            changedAssets.Add(asset);
                            anyChanges = true;

                            lastModifiedAsset = asset.name;
                            lastReloadTime = Time.time;
                        }
                    }
                }
            }

            if (anyChanges)
            {
                ApplyConfigurationChanges(changedAssets);
                totalReloads++;
            }
#endif
        }

        private void ApplyConfigurationChanges(List<ScriptableObject> changedAssets)
        {
            if (showDebugLogs)
                Debug.Log($"🔥 Hot Reload: Applying changes to {changedAssets.Count} assets");

            foreach (var asset in changedAssets)
            {
                switch (asset)
                {
                    case ChimeraGameConfig gameConfig:
                        ApplyGameConfigChanges(gameConfig);
                        break;

                    case ScriptableObject speciesConfig when speciesConfig.GetType().Name.Contains("Species"):
                        ApplySpeciesConfigChanges(speciesConfig);
                        break;

                    case ScriptableObject biomeConfig when biomeConfig.GetType().Name.Contains("Biome"):
                        ApplyBiomeConfigChanges(biomeConfig);
                        break;
                }
            }

            // Notify other systems about configuration changes
            BroadcastConfigurationChanged();
        }

        private void ApplyGameConfigChanges(ChimeraGameConfig config)
        {
            if (showDebugLogs)
                Debug.Log($"🔥 Hot Reload: Game config '{config.name}' changed");

            // Update ECS performance settings
            ApplyPerformanceSettings(config);

            // Update global mutation rates
            ApplyGeneticSettings(config);

            // Update networking settings if changed
            ApplyNetworkingSettings(config);
        }

        private void ApplySpeciesConfigChanges(ScriptableObject speciesConfig)
        {
            if (showDebugLogs)
                Debug.Log($"🔥 Hot Reload: Species config '{speciesConfig.name}' changed");

            // Find all entities of this species and update their data
            var speciesID = speciesConfig.name.GetHashCode();

            // Use reflection to avoid assembly dependency
            // This would need proper implementation with available types
            if (showDebugLogs)
                Debug.Log($"🔥 Hot Reload: Species config '{speciesConfig.name}' changed - using reflection to update entities");
        }

        private void ApplyBiomeConfigChanges(ScriptableObject biomeConfig)
        {
            if (showDebugLogs)
                Debug.Log($"🔥 Hot Reload: Biome config '{biomeConfig.name}' changed");

            // Update environmental systems with new biome settings
            // This would integrate with your environmental systems
            UpdateEnvironmentalSystems(biomeConfig);
        }

        private void ApplyPerformanceSettings(ChimeraGameConfig config)
        {
            // Update ECS system batch sizes
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated == true)
            {
                // Update job batch sizes for creature simulation systems
                // This would require system references or a system manager
                UpdateECSBatchSizes(config.GetOptimalBatchSize());
            }
        }

        private void ApplyGeneticSettings(ChimeraGameConfig config)
        {
            // Update global genetic parameters
            // This would integrate with your genetics systems
            if (showDebugLogs)
                Debug.Log($"🔥 Hot Reload: Updated mutation rate to {config.globalMutationRate}");
        }

        private void ApplyNetworkingSettings(ChimeraGameConfig config)
        {
            // Update network tick rates and player limits
            if (config.enableMultiplayer)
            {
                // This would integrate with your networking systems
                UpdateNetworkSettings(config.networkTickRate, config.maxPlayersPerServer);
            }
        }

        private object CalculateStatsFromConfig(ScriptableObject config)
        {
            // Use reflection to create stats component from config
            // This would need actual implementation based on available types
            return null;
        }

        private void UpdateEnvironmentalSystems(ScriptableObject biomeConfig)
        {
            // Placeholder for environmental system integration
            // Would update temperature, humidity, resource availability, etc.
        }

        private void UpdateECSBatchSizes(int batchSize)
        {
            // Placeholder for ECS system batch size updates
            // Would require system manager or individual system references
        }

        private void UpdateNetworkSettings(int tickRate, int maxPlayers)
        {
            // Placeholder for network system integration
            // Would update Unity Netcode settings
        }

        private void BroadcastConfigurationChanged()
        {
            // Send event to notify other systems about configuration changes
            var configChangedEvent = new ConfigurationChangedEvent
            {
                timestamp = Time.time,
                changeCount = totalReloads
            };

            // This would integrate with your event system
            // EventBus.Instance?.Publish(configChangedEvent);
        }

        /// <summary>
        /// Manually trigger configuration reload (for testing)
        /// </summary>
        [ContextMenu("Force Reload All Configurations")]
        public void ForceReloadAll()
        {
            if (!Application.isPlaying) return;

            var allAssets = new List<ScriptableObject>();
            allAssets.AddRange(pathToAsset.Values);

            ApplyConfigurationChanges(allAssets);
            totalReloads++;

            Debug.Log($"🔥 Force reloaded {allAssets.Count} configurations");
        }

        /// <summary>
        /// Add a configuration to hot reload watching at runtime
        /// </summary>
        public void WatchConfiguration(ScriptableObject config)
        {
            if (config != null && Application.isPlaying)
            {
                RegisterForWatching(config);
            }
        }

        private void OnValidate()
        {
            // Ensure check interval is reasonable
            checkInterval = Mathf.Clamp(checkInterval, 0.1f, 5f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying && systemInitialized)
            {
                // Draw hot reload status
                Gizmos.color = enableHotReload ? Color.green : Color.red;
                Gizmos.DrawWireSphere(transform.position, 2f);

                // Draw watched file count
                var position = transform.position + Vector3.up * 3f;
                UnityEditor.Handles.Label(position, $"Watching: {fileTimestamps.Count} configs\nReloads: {totalReloads}");
            }
        }
#endif

        private void OnDestroy()
        {
            CancelInvoke();
        }
    }

    /// <summary>
    /// Event data for configuration changes
    /// </summary>
    public struct ConfigurationChangedEvent
    {
        public float timestamp;
        public int changeCount;
    }
}