using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Core;
using Laboratory.Core.ECS;
using Laboratory.Chimera.Core;

namespace Laboratory.Networking.Entities
{
    /// <summary>
    /// Netcode Configuration System - ScriptableObject-based network settings
    /// PURPOSE: Designer-friendly configuration for all multiplayer networking parameters
    /// FEATURES: Server/client settings, bandwidth optimization, authority distribution, security settings
    /// ARCHITECTURE: Integrates seamlessly with existing ChimeraGameConfig system
    /// </summary>

    [CreateAssetMenu(fileName = "NetcodeConfig", menuName = "Laboratory/Networking/Netcode Configuration")]
    public class NetcodeConfiguration : ScriptableObject
    {
        [Header("Connection Settings")]
        [Tooltip("Maximum number of players per server instance")]
        public int maxPlayers = 20;

        [Tooltip("Server tick rate (Hz) - affects simulation accuracy and bandwidth")]
        [Range(10, 120)]
        public int serverTickRate = 60;

        [Tooltip("Client update rate (Hz) - how often clients send updates")]
        [Range(5, 60)]
        public int clientUpdateRate = 20;

        [Tooltip("Connection timeout in seconds")]
        [Range(5, 60)]
        public float connectionTimeout = 30f;

        [Header("Bandwidth Optimization")]
        [Tooltip("Target bandwidth per client in KB/s")]
        [Range(50, 2000)]
        public float targetBandwidthPerClient = 500f;

        [Tooltip("Enable dynamic quality scaling based on network conditions")]
        public bool enableDynamicQuality = true;

        [Tooltip("Compression level for network data (0=none, 9=max)")]
        [Range(0, 9)]
        public int compressionLevel = 6;

        [Tooltip("Delta compression threshold - smaller changes ignored")]
        [Range(0.001f, 0.1f)]
        public float deltaCompressionThreshold = 0.01f;

        [Header("Authority Distribution")]
        [Tooltip("Default authority type for spawned creatures")]
        public NetworkAuthorityType defaultCreatureAuthority = NetworkAuthorityType.Server;

        [Tooltip("Player entities authority (usually client for responsiveness)")]
        public NetworkAuthorityType playerEntityAuthority = NetworkAuthorityType.Client;

        [Tooltip("Breeding system authority (server for consistency)")]
        public NetworkAuthorityType breedingSystemAuthority = NetworkAuthorityType.Server;

        [Tooltip("Market transactions authority (always server for security)")]
        public NetworkAuthorityType marketAuthorityType = NetworkAuthorityType.Server;

        [Header("Sync Priority Configuration")]
        [Tooltip("Base sync priority for player-owned creatures")]
        [Range(0, 255)]
        public byte playerCreaturePriority = 200;

        [Tooltip("Base sync priority for wild creatures")]
        [Range(0, 255)]
        public byte wildCreaturePriority = 100;

        [Tooltip("Base sync priority for breeding creatures")]
        [Range(0, 255)]
        public byte breedingCreaturePriority = 150;

        [Tooltip("Base sync priority for market creatures")]
        [Range(0, 255)]
        public byte marketCreaturePriority = 120;

        [Header("Prediction & Lag Compensation")]
        [Tooltip("Enable client-side prediction for smooth movement")]
        public bool enableClientPrediction = true;

        [Tooltip("Maximum prediction time in seconds")]
        [Range(0.1f, 1f)]
        public float maxPredictionTime = 0.5f;

        [Tooltip("Reconciliation threshold - when to snap vs interpolate")]
        [Range(0.5f, 5f)]
        public float reconciliationThreshold = 2f;

        [Tooltip("Interpolation smoothing factor")]
        [Range(0.1f, 1f)]
        public float interpolationSmoothness = 0.8f;

        [Header("Security Settings")]
        [Tooltip("Enable anti-cheat validation")]
        public bool enableAntiCheat = true;

        [Tooltip("Maximum allowed movement speed for validation")]
        [Range(1f, 50f)]
        public float maxAllowedSpeed = 20f;

        [Tooltip("Position validation tolerance")]
        [Range(0.1f, 2f)]
        public float positionValidationTolerance = 1f;

        [Tooltip("Command rate limiting (commands per second per player)")]
        [Range(5, 100)]
        public int maxCommandsPerSecond = 30;

        [Header("Ecosystem Synchronization")]
        [Tooltip("Sync interval for ecosystem state in seconds")]
        [Range(1f, 30f)]
        public float ecosystemSyncInterval = 5f;

        [Tooltip("Number of creatures to sync per frame (performance limiting)")]
        [Range(10, 500)]
        public int creaturesPerFrameLimit = 100;

        [Tooltip("Maximum distance for creature synchronization")]
        [Range(50f, 1000f)]
        public float maxSyncDistance = 200f;

        [Header("Performance Scaling")]
        [SerializeField]
        private NetworkPerformanceProfile[] performanceProfiles = new NetworkPerformanceProfile[]
        {
            new NetworkPerformanceProfile
            {
                profileName = "High Performance",
                maxSyncedCreatures = 1000,
                syncRadius = 200f,
                updateRate = 60,
                compressionLevel = 3
            },
            new NetworkPerformanceProfile
            {
                profileName = "Balanced",
                maxSyncedCreatures = 500,
                syncRadius = 150f,
                updateRate = 30,
                compressionLevel = 6
            },
            new NetworkPerformanceProfile
            {
                profileName = "Low Bandwidth",
                maxSyncedCreatures = 200,
                syncRadius = 100f,
                updateRate = 15,
                compressionLevel = 9
            }
        };

        [System.Serializable]
        public struct NetworkPerformanceProfile
        {
            public string profileName;
            public int maxSyncedCreatures;
            public float syncRadius;
            public int updateRate;
            public int compressionLevel;
        }

        [Header("Integration Settings")]
        [Tooltip("Reference to main game configuration")]
        public ChimeraGameConfig gameConfig;

        [Tooltip("Enable debug logging for network events")]
        public bool enableNetworkDebugging = false;

        [Tooltip("Network statistics update interval")]
        [Range(0.5f, 5f)]
        public float statsUpdateInterval = 1f;

        /// <summary>
        /// Get performance profile by name or index
        /// </summary>
        public NetworkPerformanceProfile GetPerformanceProfile(string profileName)
        {
            foreach (var profile in performanceProfiles)
            {
                if (profile.profileName == profileName)
                    return profile;
            }
            return performanceProfiles[0]; // Default to first profile
        }

        /// <summary>
        /// Get performance profile by index
        /// </summary>
        public NetworkPerformanceProfile GetPerformanceProfile(int index)
        {
            if (index >= 0 && index < performanceProfiles.Length)
                return performanceProfiles[index];
            return performanceProfiles[0];
        }

        /// <summary>
        /// Calculate optimal sync interval for given priority
        /// </summary>
        public float CalculateSyncInterval(byte priority)
        {
            // Higher priority = faster sync
            float normalizedPriority = priority / 255f;
            float minInterval = 1f / serverTickRate; // Minimum interval based on tick rate
            float maxInterval = 1f; // Maximum 1 second interval

            return math.lerp(maxInterval, minInterval, normalizedPriority);
        }

        /// <summary>
        /// Validate configuration settings
        /// </summary>
        public bool ValidateConfiguration(out string[] errors)
        {
            var errorList = new System.Collections.Generic.List<string>();

            // Validate basic settings
            if (maxPlayers <= 0 || maxPlayers > 100)
                errorList.Add("Max players must be between 1 and 100");

            if (serverTickRate < clientUpdateRate)
                errorList.Add("Server tick rate should be equal or higher than client update rate");

            if (targetBandwidthPerClient < 50)
                errorList.Add("Target bandwidth too low - may cause synchronization issues");

            if (performanceProfiles == null || performanceProfiles.Length == 0)
                errorList.Add("At least one performance profile is required");

            // Validate performance profiles
            for (int i = 0; i < performanceProfiles.Length; i++)
            {
                var profile = performanceProfiles[i];
                if (string.IsNullOrEmpty(profile.profileName))
                    errorList.Add($"Performance profile {i} needs a name");

                if (profile.maxSyncedCreatures <= 0)
                    errorList.Add($"Performance profile '{profile.profileName}' must sync at least 1 creature");
            }

            errors = errorList.ToArray();
            return errorList.Count == 0;
        }

        /// <summary>
        /// Create default configuration
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            maxPlayers = 20;
            serverTickRate = 60;
            clientUpdateRate = 20;
            connectionTimeout = 30f;
            targetBandwidthPerClient = 500f;
            enableDynamicQuality = true;
            compressionLevel = 6;
            deltaCompressionThreshold = 0.01f;
            defaultCreatureAuthority = NetworkAuthorityType.Server;
            playerEntityAuthority = NetworkAuthorityType.Client;
            breedingSystemAuthority = NetworkAuthorityType.Server;
            marketAuthorityType = NetworkAuthorityType.Server;
            playerCreaturePriority = 200;
            wildCreaturePriority = 100;
            breedingCreaturePriority = 150;
            marketCreaturePriority = 120;
            enableClientPrediction = true;
            maxPredictionTime = 0.5f;
            reconciliationThreshold = 2f;
            interpolationSmoothness = 0.8f;
            enableAntiCheat = true;
            maxAllowedSpeed = 20f;
            positionValidationTolerance = 1f;
            maxCommandsPerSecond = 30;
            ecosystemSyncInterval = 5f;
            creaturesPerFrameLimit = 100;
            maxSyncDistance = 200f;
            enableNetworkDebugging = false;
            statsUpdateInterval = 1f;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validate settings when changed in inspector
        /// </summary>
        private void OnValidate()
        {
            // Ensure sensible constraints
            maxPlayers = Mathf.Clamp(maxPlayers, 1, 100);
            serverTickRate = Mathf.Clamp(serverTickRate, 10, 120);
            clientUpdateRate = Mathf.Clamp(clientUpdateRate, 5, serverTickRate);
            targetBandwidthPerClient = Mathf.Clamp(targetBandwidthPerClient, 50f, 2000f);

            // Ensure performance profiles are valid
            for (int i = 0; i < performanceProfiles.Length; i++)
            {
                performanceProfiles[i].maxSyncedCreatures = Mathf.Max(1, performanceProfiles[i].maxSyncedCreatures);
                performanceProfiles[i].syncRadius = Mathf.Max(10f, performanceProfiles[i].syncRadius);
                performanceProfiles[i].updateRate = Mathf.Clamp(performanceProfiles[i].updateRate, 5, 120);
                performanceProfiles[i].compressionLevel = Mathf.Clamp(performanceProfiles[i].compressionLevel, 0, 9);
            }
        }
#endif
    }

    /// <summary>
    /// Authoring component for easy Netcode integration in scenes
    /// </summary>
    public class NetcodeConfigurationAuthoring : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Netcode configuration asset")]
        public NetcodeConfiguration netcodeConfig;

        [Tooltip("Auto-initialize on scene start")]
        public bool autoInitialize = true;

        [Tooltip("Performance profile to use (index)")]
        [Range(0, 2)]
        public int performanceProfileIndex = 1; // Default to "Balanced"

        [Header("Testing")]
        [Tooltip("Enable network simulation for testing")]
        public bool enableNetworkSimulation = false;

        [Tooltip("Simulated latency in milliseconds")]
        [Range(0, 500)]
        public int simulatedLatency = 100;

        [Tooltip("Simulated packet loss percentage")]
        [Range(0f, 10f)]
        public float simulatedPacketLoss = 1f;

        private void Start()
        {
            if (autoInitialize && netcodeConfig != null)
            {
                InitializeNetcode();
            }
        }

        /// <summary>
        /// Initialize netcode with current configuration
        /// </summary>
        public void InitializeNetcode()
        {
            if (netcodeConfig == null)
            {
                Debug.LogError("NetcodeConfiguration is null! Please assign a configuration asset.");
                return;
            }

            // Validate configuration
            if (!netcodeConfig.ValidateConfiguration(out string[] errors))
            {
                Debug.LogError($"Netcode configuration validation failed: {string.Join(", ", errors)}");
                return;
            }

            // Apply performance profile
            var profile = netcodeConfig.GetPerformanceProfile(performanceProfileIndex);
            ApplyPerformanceProfile(profile);

            // Initialize network systems
            InitializeNetworkSystems();

            if (netcodeConfig.enableNetworkDebugging)
            {
                Debug.Log($"Netcode initialized with profile: {profile.profileName}");
            }
        }

        private void ApplyPerformanceProfile(NetcodeConfiguration.NetworkPerformanceProfile profile)
        {
            // This would apply the performance settings to the network systems
            // In a real implementation, this would configure the actual netcode systems

            if (netcodeConfig.enableNetworkDebugging)
            {
                Debug.Log($"Applied performance profile: {profile.profileName} " +
                         $"(Max Creatures: {profile.maxSyncedCreatures}, " +
                         $"Sync Radius: {profile.syncRadius}m, " +
                         $"Update Rate: {profile.updateRate}Hz)");
            }
        }

        private void InitializeNetworkSystems()
        {
            // Initialize ECS network systems with configuration
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                // Configure network systems with our settings
                var netcodeManager = world.GetOrCreateSystemManaged<NetcodeEntityManager>();

                if (netcodeConfig.enableNetworkDebugging)
                {
                    Debug.Log("Network systems initialized successfully");
                }
            }
        }

        /// <summary>
        /// Change performance profile at runtime
        /// </summary>
        public void ChangePerformanceProfile(int profileIndex)
        {
            if (netcodeConfig == null) return;

            performanceProfileIndex = profileIndex;
            var profile = netcodeConfig.GetPerformanceProfile(profileIndex);
            ApplyPerformanceProfile(profile);
        }

        /// <summary>
        /// Get current network statistics
        /// </summary>
        public NetworkStatistics GetNetworkStatistics()
        {
            // This would return real network statistics
            return new NetworkStatistics
            {
                connectedPlayers = 1, // Placeholder
                syncedCreatures = 100, // Placeholder
                bandwidthUsage = 250f, // Placeholder
                averageLatency = simulatedLatency,
                packetLoss = simulatedPacketLoss
            };
        }

        [System.Serializable]
        public struct NetworkStatistics
        {
            public int connectedPlayers;
            public int syncedCreatures;
            public float bandwidthUsage; // KB/s
            public float averageLatency; // ms
            public float packetLoss; // percentage
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor utility to create default configuration
        /// </summary>
        [ContextMenu("Create Default Configuration")]
        private void CreateDefaultConfiguration()
        {
            var config = ScriptableObject.CreateInstance<NetcodeConfiguration>();
            config.ResetToDefaults();

            UnityEditor.AssetDatabase.CreateAsset(config, "Assets/_Project/Settings/DefaultNetcodeConfig.asset");
            UnityEditor.AssetDatabase.SaveAssets();

            netcodeConfig = config;
        }
#endif
    }

    /// <summary>
    /// Network statistics monitoring system
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class NetworkStatisticsSystem : SystemBase
    {
        private NetcodeConfiguration _config;
        private float _lastStatsUpdate;
        private int _framesSinceLastUpdate;

        protected override void OnCreate()
        {
            // Find configuration in scene
            var authoring = Object.FindObjectOfType<NetcodeConfigurationAuthoring>();
            if (authoring != null)
            {
                _config = authoring.netcodeConfig;
            }
        }

        protected override void OnUpdate()
        {
            if (_config == null) return;

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            _framesSinceLastUpdate++;

            if (currentTime - _lastStatsUpdate >= _config.statsUpdateInterval)
            {
                UpdateNetworkStatistics(currentTime);
                _lastStatsUpdate = currentTime;
                _framesSinceLastUpdate = 0;
            }
        }

        private void UpdateNetworkStatistics(float currentTime)
        {
            if (!_config.enableNetworkDebugging) return;

            // Calculate network statistics
            var networkedEntities = GetEntityQuery(ComponentType.ReadOnly<NetworkOwnership>()).CalculateEntityCount();
            var syncedEntities = GetEntityQuery(ComponentType.ReadOnly<ReplicatedCreatureState>()).CalculateEntityCount();

            // Estimate bandwidth usage
            float estimatedBandwidth = (syncedEntities * 64f * _config.clientUpdateRate) / 1000f; // KB/s

            if (_framesSinceLastUpdate > 0)
            {
                float avgFPS = _framesSinceLastUpdate / _config.statsUpdateInterval;

                Debug.Log($"Network Stats - Entities: {networkedEntities}, Synced: {syncedEntities}, " +
                         $"Bandwidth: {estimatedBandwidth:F1} KB/s, FPS: {avgFPS:F1}");
            }
        }
    }
}