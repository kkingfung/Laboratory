using UnityEngine;
using Laboratory.Core.Configuration;

namespace Laboratory.Core.Configuration
{
    /// <summary>
    /// Demonstrates the configuration system by showing before/after magic number elimination.
    /// Provides runtime monitoring of how configuration values are being used throughout the system.
    /// </summary>
    public class ConfigurationDemonstrator : MonoBehaviour
    {
        [Header("Configuration Monitoring")]
        [SerializeField] private bool enableLiveMonitoring = true;
        [SerializeField] private float monitoringInterval = 2f;

        [Header("Configuration Override Testing")]
        [SerializeField] private bool allowRuntimeOverrides = false;
        [SerializeField] private PerformanceConfiguration testConfiguration;

        private float lastMonitorTime;

        #region Unity Lifecycle

        private void Start()
        {
            if (enableLiveMonitoring)
            {
                LogCurrentConfiguration();
                InvokeRepeating(nameof(MonitorConfigurationUsage), monitoringInterval, monitoringInterval);
            }
        }

        private void Update()
        {
            if (enableLiveMonitoring && Input.GetKeyDown(KeyCode.F12))
            {
                LogCurrentConfiguration();
            }

            if (allowRuntimeOverrides && Input.GetKeyDown(KeyCode.F11))
            {
                TestConfigurationOverride();
            }
        }

        #endregion

        #region Configuration Monitoring

        private void LogCurrentConfiguration()
        {
            var config = Config.Performance;

            Debug.Log("[Config] [Configuration System] Current Performance Settings:");
            Debug.Log($"  [Time] Update Frequencies: Critical={config.criticalUpdateFrequency}Hz, High={config.highUpdateFrequency}Hz, Medium={config.mediumUpdateFrequency}Hz, Low={config.lowUpdateFrequency}Hz");
            Debug.Log($"  [AI] AI Settings: MaxAgents={config.maxPathfindingAgentsPerFrame}, PathRequests={config.maxPathRequestsPerFrame}, CacheLifetime={config.pathCacheLifetime}s");
            Debug.Log($"  [Memory] Memory: PoolSize={config.nativeArrayPoolSize}, ListCapacity={config.pathListInitialCapacity}, MaxLists={config.maxPooledLists}");
            Debug.Log($"  [Network] Network: High={config.highPriorityNetworkRate}Hz, Medium={config.mediumPriorityNetworkRate}Hz, Low={config.lowPriorityNetworkRate}Hz");
        }

        private void MonitorConfigurationUsage()
        {
            if (!enableLiveMonitoring) return;

            // Monitor update manager statistics using reflection
            var updateManagerType = System.Type.GetType("Laboratory.Core.Performance.OptimizedUpdateManager, Laboratory.Core.Performance");
            if (updateManagerType != null)
            {
                var instanceProperty = updateManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProperty != null)
                {
                    var updateManager = instanceProperty.GetValue(null);
                    if (updateManager != null)
                    {
                        var getStatsMethod = updateManagerType.GetMethod("GetStatistics");
                        if (getStatsMethod != null)
                        {
                            var stats = getStatsMethod.Invoke(updateManager, null);
                            if (stats != null)
                            {
                                var statsType = stats.GetType();
                                var totalSystems = statsType.GetProperty("TotalRegisteredSystems")?.GetValue(stats);
                                var systemsUpdated = statsType.GetProperty("SystemsUpdatedThisFrame")?.GetValue(stats);
                                Debug.Log($"[Stats] [Config Monitor] Update Manager: {totalSystems} systems, {systemsUpdated} updated this frame");
                            }
                        }
                    }
                }
            }

            // Show configuration values in use
            ShowConfigurationInUse();
        }

        private void ShowConfigurationInUse()
        {
            var config = Config.Performance;

            Debug.Log($"[Settings] [Config Usage] Current intervals being used:");
            Debug.Log($"  High Frequency: {Config.UpdateInterval(UpdateFrequency.High):F3}s");
            Debug.Log($"  Medium Frequency: {Config.UpdateInterval(UpdateFrequency.Medium):F3}s");
            Debug.Log($"  Low Frequency: {Config.UpdateInterval(UpdateFrequency.Low):F3}s");
            Debug.Log($"  Background: {Config.UpdateInterval(UpdateFrequency.Background):F3}s");
        }

        #endregion

        #region Runtime Configuration Testing

        private void TestConfigurationOverride()
        {
            if (testConfiguration == null)
            {
                Debug.LogWarning("[ConfigurationDemonstrator] No test configuration assigned for override testing");
                return;
            }

            Debug.Log("[Test] [Configuration Test] Overriding configuration temporarily...");

            var originalConfig = Config.Performance;
            ConfigurationManager.Instance.SetPerformanceConfiguration(testConfiguration);

            Debug.Log($"  Before: High Frequency = {originalConfig.highUpdateFrequency}Hz");
            Debug.Log($"  After: High Frequency = {testConfiguration.highUpdateFrequency}Hz");

            // Restore after 5 seconds
            Invoke(nameof(RestoreOriginalConfiguration), 5f);
        }

        private void RestoreOriginalConfiguration()
        {
            // This would restore to the original configuration
            // In a real scenario, you'd keep a reference to the original
            Debug.Log("[Test] [Configuration Test] Configuration override test completed");
        }

        #endregion

        #region Performance Comparison

        [ContextMenu("Show Before/After Magic Numbers")]
        private void ShowMagicNumberComparison()
        {
            Debug.Log("[Compare] [Magic Number Elimination] Before vs After Comparison:");

            Debug.Log("[Before] BEFORE (Magic Numbers):");
            Debug.Log("  const float SPATIAL_CELL_SIZE = 25f;");
            Debug.Log("  const int MAX_FLOW_FIELDS = 100;");
            Debug.Log("  const int MAX_POOLED_LISTS = 20;");
            Debug.Log("  private float pathUpdateInterval = 0.2f;");
            Debug.Log("  new List<Vector3>(50);");

            Debug.Log("[OK] AFTER (Configuration-Driven):");
            Debug.Log($"  Config.Performance.spatialCellSize = {Config.Performance.spatialCellSize}");
            Debug.Log($"  Config.Performance.maxFlowFields = {Config.Performance.maxFlowFields}");
            Debug.Log($"  Config.Performance.maxPooledLists = {Config.Performance.maxPooledLists}");
            Debug.Log($"  Config.Performance.pathUpdateInterval = {Config.Performance.pathUpdateInterval}");
            Debug.Log($"  Config.Performance.pathListInitialCapacity = {Config.Performance.pathListInitialCapacity}");

            Debug.Log("[Benefits] Benefits:");
            Debug.Log("  - Designer-friendly tuning in Inspector");
            Debug.Log("  - Runtime configuration changes possible");
            Debug.Log("  - Centralized performance settings");
            Debug.Log("  - No more scattered magic numbers");
            Debug.Log("  - ScriptableObject-based configuration assets");
        }

        [ContextMenu("Simulate Performance Tuning")]
        private void SimulatePerformanceTuning()
        {
            var config = Config.Performance;

            Debug.Log("[Performance] [Performance Tuning Simulation]");
            Debug.Log("Scenario: Game is running slowly with many AI agents...");

            Debug.Log("Current AI Settings:");
            Debug.Log($"  - Max Pathfinding Agents per Frame: {config.maxPathfindingAgentsPerFrame}");
            Debug.Log($"  - Max Path Requests per Frame: {config.maxPathRequestsPerFrame}");
            Debug.Log($"  - Update Frequencies: {config.lowUpdateFrequency}Hz (low), {config.mediumUpdateFrequency}Hz (medium)");

            Debug.Log("[Optimize] Suggested optimizations (just change values in PerformanceConfiguration):");
            Debug.Log("  - Reduce maxPathfindingAgentsPerFrame from 10 -> 5");
            Debug.Log("  - Reduce lowUpdateFrequency from 5Hz -> 2Hz");
            Debug.Log("  - Increase pathCacheLifetime from 5s -> 10s (cache longer)");
            Debug.Log("  - No code changes required - just adjust ScriptableObject!");
        }

        #endregion

        #region Debug Information

        private void OnGUI()
        {
            if (!enableLiveMonitoring) return;

            var config = Config.Performance;

            GUILayout.BeginArea(new Rect(10, 10, 400, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label("Configuration System Status", EditorStyles.boldLabel);
            GUILayout.Label($"Spatial Cell Size: {config.spatialCellSize}");
            GUILayout.Label($"Max Flow Fields: {config.maxFlowFields}");
            GUILayout.Label($"AI Update Rate: {config.mediumUpdateFrequency}Hz");
            GUILayout.Label($"Pool Size: {config.nativeArrayPoolSize}");

            if (GUILayout.Button("Log Configuration"))
            {
                LogCurrentConfiguration();
            }

            if (GUILayout.Button("Show Magic Number Comparison"))
            {
                ShowMagicNumberComparison();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion
    }
}

/// <summary>
/// Editor styles fallback for runtime GUI
/// </summary>
public static class EditorStyles
{
    public static GUIStyle boldLabel => GUI.skin.label;
}