using UnityEngine;

namespace Laboratory.Core.Configuration
{
    /// <summary>
    /// Centralized performance configuration for all Laboratory systems.
    /// Eliminates magic numbers and provides designer-friendly tuning interface.
    /// All timing, frequency, and performance-related constants are defined here.
    /// </summary>
    [CreateAssetMenu(fileName = "PerformanceConfiguration", menuName = "Laboratory/Configuration/Performance Configuration")]
    public class PerformanceConfiguration : ScriptableObject
    {
        [Header("🕐 Update Frequency Settings")]
        [Space(5)]

        [Tooltip("Update frequency for critical systems (60 FPS)")]
        public float criticalUpdateFrequency = 60f;

        [Tooltip("Update frequency for high-priority systems (30 FPS)")]
        public float highUpdateFrequency = 30f;

        [Tooltip("Update frequency for medium-priority systems (15 FPS)")]
        public float mediumUpdateFrequency = 15f;

        [Tooltip("Update frequency for low-priority systems (5 FPS)")]
        public float lowUpdateFrequency = 5f;

        [Tooltip("Update frequency for background systems (1 FPS)")]
        public float backgroundUpdateFrequency = 1f;

        [Header("🧠 AI & Pathfinding Settings")]
        [Space(5)]

        [Tooltip("Maximum number of pathfinding agents processed per frame")]
        [Range(1, 50)]
        public int maxPathfindingAgentsPerFrame = 10;

        [Tooltip("Maximum number of path requests processed per frame")]
        [Range(1, 20)]
        public int maxPathRequestsPerFrame = 5;

        [Tooltip("How often to update agent paths (seconds)")]
        [Range(0.1f, 2f)]
        public float pathUpdateInterval = 0.2f;

        [Tooltip("How long to cache calculated paths (seconds)")]
        [Range(1f, 30f)]
        public float pathCacheLifetime = 5f;

        [Tooltip("Maximum number of cached paths to keep in memory")]
        [Range(10, 500)]
        public int maxCachedPaths = 100;

        [Tooltip("Size of spatial grid cells for flow field optimization")]
        [Range(5f, 100f)]
        public float spatialCellSize = 25f;

        [Tooltip("Maximum number of active flow fields")]
        [Range(10, 500)]
        public int maxFlowFields = 100;

        [Tooltip("Maximum flow field generation requests per frame")]
        [Range(1, 20)]
        public int maxFlowFieldRequestsPerFrame = 5;

        [Header("🎯 Player & Input Settings")]
        [Space(5)]

        [Tooltip("How often to check if player is grounded (seconds)")]
        [Range(0.05f, 0.5f)]
        public float groundCheckInterval = 0.1f;

        [Tooltip("Distance for ground detection")]
        [Range(0.1f, 2f)]
        public float groundCheckDistance = 0.4f;

        [Tooltip("How often to update player animations (FPS)")]
        [Range(15f, 60f)]
        public float playerAnimationUpdateRate = 30f;

        [Header("💾 Memory Management")]
        [Space(5)]

        [Tooltip("Maximum number of pooled List<Vector3> objects")]
        [Range(5, 100)]
        public int maxPooledLists = 20;

        [Tooltip("Initial capacity for pooled path lists")]
        [Range(10, 200)]
        public int pathListInitialCapacity = 50;

        [Tooltip("Maximum size for NativeArray pools")]
        [Range(10, 200)]
        public int nativeArrayPoolSize = 50;

        [Tooltip("Default capacity for NativeArray pools")]
        [Range(100, 5000)]
        public int nativeArrayDefaultCapacity = 1000;

        [Header("🎮 Game Management")]
        [Space(5)]

        [Tooltip("Delay before respawning player (seconds)")]
        [Range(1f, 10f)]
        public float playerRespawnDelay = 3f;

        [Tooltip("Delay before restarting game after game over (seconds)")]
        [Range(1f, 10f)]
        public float gameRestartDelay = 3f;

        [Tooltip("Delay before loading next level (seconds)")]
        [Range(0.5f, 5f)]
        public float levelTransitionDelay = 2f;

        [Tooltip("How often to check win conditions (FPS)")]
        [Range(1f, 30f)]
        public float winConditionCheckRate = 5f;

        [Header("📊 Performance Monitoring")]
        [Space(5)]

        [Tooltip("How often to log performance statistics (seconds)")]
        [Range(1f, 30f)]
        public float performanceLogInterval = 5f;

        [Tooltip("Smoothing factor for FPS calculations")]
        [Range(0.01f, 1f)]
        public float fpsSmoothing = 0.1f;

        [Tooltip("How often to update memory usage display (seconds)")]
        [Range(0.5f, 10f)]
        public float memoryUpdateInterval = 1f;

        [Header("🔫 Combat & Physics")]
        [Space(5)]

        [Tooltip("Probability of generating test hits (0-1)")]
        [Range(0f, 1f)]
        public float testHitProbability = 0.01f;

        [Tooltip("Delay before starting ragdoll blend-back animation")]
        [Range(0.1f, 2f)]
        public float ragdollBlendDelay = 0.3f;

        [Tooltip("Default attack cooldown duration (seconds)")]
        [Range(0.1f, 5f)]
        public float defaultAttackCooldown = 1f;

        [Header("🌐 Network Synchronization")]
        [Space(5)]

        [Tooltip("High priority network sync rate (Hz)")]
        [Range(10f, 60f)]
        public float highPriorityNetworkRate = 20f;

        [Tooltip("Medium priority network sync rate (Hz)")]
        [Range(5f, 30f)]
        public float mediumPriorityNetworkRate = 10f;

        [Tooltip("Low priority network sync rate (Hz)")]
        [Range(1f, 15f)]
        public float lowPriorityNetworkRate = 5f;

        [Tooltip("Very low priority network sync rate (Hz)")]
        [Range(0.5f, 5f)]
        public float veryLowPriorityNetworkRate = 2f;

        #region Computed Properties

        /// <summary>
        /// Get update interval for the specified frequency
        /// </summary>
        public float GetUpdateInterval(UpdateFrequency frequency)
        {
            return frequency switch
            {
                UpdateFrequency.Critical => 1f / criticalUpdateFrequency,
                UpdateFrequency.High => 1f / highUpdateFrequency,
                UpdateFrequency.Medium => 1f / mediumUpdateFrequency,
                UpdateFrequency.Low => 1f / lowUpdateFrequency,
                UpdateFrequency.Background => 1f / backgroundUpdateFrequency,
                _ => 1f / mediumUpdateFrequency
            };
        }

        /// <summary>
        /// Get network sync interval for the specified priority
        /// </summary>
        public float GetNetworkSyncInterval(NetworkPriority priority)
        {
            return priority switch
            {
                NetworkPriority.High => 1f / highPriorityNetworkRate,
                NetworkPriority.Medium => 1f / mediumPriorityNetworkRate,
                NetworkPriority.Low => 1f / lowPriorityNetworkRate,
                NetworkPriority.VeryLow => 1f / veryLowPriorityNetworkRate,
                _ => 1f / mediumPriorityNetworkRate
            };
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Ensure frequencies are in logical order
            criticalUpdateFrequency = Mathf.Max(criticalUpdateFrequency, 30f);
            highUpdateFrequency = Mathf.Min(highUpdateFrequency, criticalUpdateFrequency);
            mediumUpdateFrequency = Mathf.Min(mediumUpdateFrequency, highUpdateFrequency);
            lowUpdateFrequency = Mathf.Min(lowUpdateFrequency, mediumUpdateFrequency);
            backgroundUpdateFrequency = Mathf.Min(backgroundUpdateFrequency, lowUpdateFrequency);

            // Ensure reasonable minimums
            highUpdateFrequency = Mathf.Max(highUpdateFrequency, 15f);
            mediumUpdateFrequency = Mathf.Max(mediumUpdateFrequency, 10f);
            lowUpdateFrequency = Mathf.Max(lowUpdateFrequency, 1f);
            backgroundUpdateFrequency = Mathf.Max(backgroundUpdateFrequency, 0.5f);
        }

        #endregion
    }

    /// <summary>
    /// Update frequency categories for systems
    /// </summary>
    public enum UpdateFrequency
    {
        Critical,    // 60 FPS - Input handling, critical gameplay
        High,        // 30 FPS - Player movement, camera
        Medium,      // 15 FPS - AI behavior, UI updates
        Low,         // 5 FPS - Game management, coordination
        Background   // 1 FPS - Cleanup, statistics
    }

    /// <summary>
    /// Network synchronization priority levels
    /// </summary>
    public enum NetworkPriority
    {
        High,    // Critical gameplay elements
        Medium,  // Important but not critical
        Low,     // Background synchronization
        VeryLow  // Housekeeping
    }
}