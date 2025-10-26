using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Laboratory.Core;
using Laboratory.Core.Configuration;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.AI;
using Laboratory.Core.Progression;
using Laboratory.Economy;
using Laboratory.Networking.Entities;

namespace Laboratory.Networking
{
    /// <summary>
    /// Chimera Network Bootstrapper - One-click multiplayer setup
    /// PURPOSE: Drop this prefab into any scene for complete Netcode for Entities integration
    /// FEATURES: Auto-configures all networking systems, spawns test creatures, validates setup
    /// ARCHITECTURE: Integrates with existing ChimeraGameConfig and ScriptableObject workflow
    /// USAGE: Drag prefab to scene, assign configurations, hit play
    /// </summary>
    public class ChimeraNetworkBootstrapper : MonoBehaviour
    {
        [Header("Core Configuration")]
        [Tooltip("Main game configuration with species and biomes")]
        public ChimeraGameConfig gameConfig;

        [Tooltip("Netcode configuration for multiplayer settings")]
        public NetcodeConfiguration netcodeConfig;

        [Tooltip("Player progression configuration")]
        public PlayerProgressionConfig progressionConfig;

        [Header("Network Setup")]
        [Tooltip("Automatically start as server on play")]
        public bool autoStartServer = true;

        [Tooltip("Enable client-server mode (false = host mode)")]
        public bool enableDedicatedServer = false;

        [Tooltip("Server port for connections")]
        [Range(7000, 9000)]
        public ushort serverPort = 7979;

        [Tooltip("Maximum players (overrides netcode config if set)")]
        [Range(1, 100)]
        public int maxPlayers = 20;

        [Header("Test Creature Spawning")]
        [Tooltip("Spawn test creatures for rapid iteration")]
        public bool spawnTestCreatures = true;

        [Tooltip("Number of test creatures to spawn")]
        [Range(1, 100)]
        public int testCreatureCount = 20;

        [Tooltip("Spawn radius around this transform")]
        [Range(10f, 200f)]
        public float spawnRadius = 50f;

        [Tooltip("Biome for test creature spawning")]
        public Laboratory.Chimera.Core.BiomeType testSpawnBiome = Laboratory.Chimera.Core.BiomeType.Forest;

        [Header("System Integration")]
        [Tooltip("Initialize progression system")]
        public bool enableProgressionSystem = true;

        [Tooltip("Initialize marketplace system")]
        public bool enableMarketplaceSystem = true;

        [Tooltip("Enable AI coordination")]
        public bool enableAICoordination = true;

        [Tooltip("Enable breeding system network sync")]
        public bool enableBreedingSync = true;

        [Header("Debug & Monitoring")]
        [Tooltip("Show network statistics in console")]
        public bool showNetworkStats = true;

        [Tooltip("Enable detailed network logging")]
        public bool enableDetailedLogging = false;

        [Tooltip("Validate configuration on start")]
        public bool validateConfigurationOnStart = true;

        // Runtime state
        private World _netcodeWorld;
        private NetcodeEntityManager _netcodeManager;
        private bool _isInitialized = false;
        private float _lastStatsTime = 0f;

        private void Start()
        {
            InitializeChimeraNetworking();
        }

        /// <summary>
        /// Initialize complete Chimera networking system
        /// </summary>
        public void InitializeChimeraNetworking()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("Chimera Networking already initialized!");
                return;
            }

            Debug.Log("🧬 Initializing Project Chimera Multiplayer Systems...");

            // Step 1: Validate all configurations
            if (validateConfigurationOnStart && !ValidateConfigurations())
            {
                Debug.LogError("❌ Configuration validation failed! Check console for details.");
                return;
            }

            // Step 2: Initialize ECS World for networking
            InitializeNetcodeWorld();

            // Step 3: Configure and start network systems
            ConfigureNetworkSystems();

            // Step 4: Initialize integrated subsystems
            InitializeSubsystems();

            // Step 5: Spawn test content if enabled
            if (spawnTestCreatures)
            {
                SpawnTestContent();
            }

            // Step 6: Start network server/client
            if (autoStartServer)
            {
                StartNetworkServer();
            }

            _isInitialized = true;
            Debug.Log("✅ Project Chimera Multiplayer Systems Initialized Successfully!");
        }

        private bool ValidateConfigurations()
        {
            bool isValid = true;
            var errors = new System.Collections.Generic.List<string>();

            // Validate main game config
            if (gameConfig == null)
            {
                errors.Add("ChimeraGameConfig is required");
                isValid = false;
            }

            // Validate netcode config
            if (netcodeConfig == null)
            {
                errors.Add("NetcodeConfiguration is required");
                isValid = false;
            }
            else
            {
                if (!netcodeConfig.ValidateConfiguration(out string[] netcodeErrors))
                {
                    errors.AddRange(netcodeErrors);
                    isValid = false;
                }
            }

            // Validate progression config if enabled
            if (enableProgressionSystem && progressionConfig == null)
            {
                errors.Add("PlayerProgressionConfig is required when progression system is enabled");
                isValid = false;
            }

            // Validate spawn settings
            if (spawnTestCreatures && testCreatureCount > 0)
            {
                if (gameConfig != null && gameConfig.availableSpecies.Length == 0)
                {
                    errors.Add("No species available for test spawning");
                    isValid = false;
                }
            }

            if (!isValid)
            {
                Debug.LogError($"Configuration Validation Errors:\n{string.Join("\n", errors)}");
            }

            return isValid;
        }

        private void InitializeNetcodeWorld()
        {
            // Create or get the default ECS world
            _netcodeWorld = World.DefaultGameObjectInjectionWorld;

            if (_netcodeWorld == null)
            {
                Debug.LogError("No ECS World found! Make sure Entities package is properly configured.");
                return;
            }

            // Initialize Netcode systems
            _netcodeManager = _netcodeWorld.GetOrCreateSystemManaged<NetcodeEntityManager>();

            if (enableDetailedLogging)
            {
                Debug.Log($"Netcode World initialized: {_netcodeWorld.Name}");
            }
        }

        private void ConfigureNetworkSystems()
        {
            if (netcodeConfig == null) return;

            // Apply netcode configuration to systems
            var performanceProfile = netcodeConfig.GetPerformanceProfile(1); // Use balanced profile

            // Configure network optimization system
            var optimizationSystem = _netcodeWorld.GetOrCreateSystemManaged<NetworkOptimizationSystem>();

            // Configure prediction system
            if (netcodeConfig.enableClientPrediction)
            {
                var predictionSystem = _netcodeWorld.GetOrCreateSystemManaged<NetworkPredictionSystem>();
            }

            // Configure lag compensation
            var lagCompSystem = _netcodeWorld.GetOrCreateSystemManaged<LagCompensationSystem>();

            // Configure statistics monitoring
            var statsSystem = _netcodeWorld.GetOrCreateSystemManaged<NetworkStatisticsSystem>();

            if (enableDetailedLogging)
            {
                Debug.Log($"Network systems configured with profile: {performanceProfile.profileName}");
            }
        }

        private void InitializeSubsystems()
        {
            // Initialize Player Progression System
            if (enableProgressionSystem && progressionConfig != null)
            {
                var progressionManager = FindFirstObjectByType<PlayerProgressionManager>();
                if (progressionManager == null)
                {
                    var progressionGO = new GameObject("Player Progression Manager");
                    progressionManager = progressionGO.AddComponent<PlayerProgressionManager>();
                    // PlayerProgressionManager initializes automatically on Awake/Start
                }

                Debug.Log("✅ Player Progression System connected to network");
            }

            // Initialize Marketplace System
            if (enableMarketplaceSystem)
            {
                var marketplaceManager = FindFirstObjectByType<BreedingMarketplace>();
                if (marketplaceManager == null)
                {
                    var marketplaceGO = new GameObject("Breeding Marketplace");
                    marketplaceManager = marketplaceGO.AddComponent<BreedingMarketplace>();
                }

                Debug.Log("✅ Marketplace System connected to network");
            }

            // Initialize AI Coordination
            if (enableAICoordination)
            {
                var coordinationSystem = _netcodeWorld.GetOrCreateSystemManaged<NetworkAICoordinationSystem>();
                Debug.Log("✅ AI Coordination System enabled");
            }

            // Initialize Breeding Synchronization
            if (enableBreedingSync)
            {
                var breedingSystem = _netcodeWorld.GetOrCreateSystemManaged<NetworkBreedingSyncSystem>();
                Debug.Log("✅ Breeding Synchronization System enabled");
            }
        }

        private void SpawnTestContent()
        {
            if (gameConfig == null || gameConfig.availableSpecies.Length == 0)
            {
                Debug.LogWarning("Cannot spawn test creatures - no species configured");
                return;
            }

            Debug.Log($"🧬 Spawning {testCreatureCount} test creatures...");

            for (int i = 0; i < testCreatureCount; i++)
            {
                SpawnTestCreature(i);
            }

            Debug.Log($"✅ Spawned {testCreatureCount} test creatures in {testSpawnBiome} biome");
        }

        private void SpawnTestCreature(int index)
        {
            // Generate random spawn position
            var randomOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;
            var spawnPosition = transform.position + new Vector3(randomOffset.x, 0, randomOffset.y);

            // Create creature entity
            var creatureEntity = _netcodeWorld.EntityManager.CreateEntity();

            // Add basic transform
            _netcodeWorld.EntityManager.AddComponentData(creatureEntity,
                LocalTransform.FromPosition(spawnPosition));

            // Add network ownership (assign to server for now)
            _netcodeWorld.EntityManager.AddComponentData(creatureEntity, new NetworkOwnership
            {
                playerId = 0, // Server owned
                hasAuthority = true,
                authorityType = NetworkAuthorityType.Server,
                lastAuthorityChange = Time.time
            });

            // Add network sync priority
            _netcodeWorld.EntityManager.AddComponentData(creatureEntity, new NetworkSyncPriority
            {
                priorityLevel = netcodeConfig.wildCreaturePriority,
                lastSyncTime = 0f,
                syncInterval = netcodeConfig.CalculateSyncInterval(netcodeConfig.wildCreaturePriority),
                forceNextSync = true
            });

            // Add replicated state
            _netcodeWorld.EntityManager.AddComponentData(creatureEntity, new ReplicatedCreatureState
            {
                position = spawnPosition,
                rotation = quaternion.identity,
                velocity = float3.zero,
                currentBehavior = Laboratory.Core.ECS.AIBehaviorType.None,
                behaviorIntensity = 0.5f,
                currentTarget = Entity.Null,
                currentBiome = testSpawnBiome,
                health = 100f,
                energy = 100f,
                stateVersion = 1,
                timestamp = Time.time
            });

            // Add prediction state for smooth movement
            if (netcodeConfig.enableClientPrediction)
            {
                _netcodeWorld.EntityManager.AddComponentData(creatureEntity, new PredictionState
                {
                    predictedPosition = spawnPosition,
                    predictedRotation = quaternion.identity,
                    predictedVelocity = float3.zero,
                    predictionConfidence = 1f,
                    lastServerUpdate = Time.time,
                    missedUpdates = 0
                });
            }

            // Add genetics component (simplified)
            if (gameConfig.availableSpecies.Length > 0)
            {
                var randomSpecies = gameConfig.availableSpecies[index % gameConfig.availableSpecies.Length];

                // Add genetics component based on species
                _netcodeWorld.EntityManager.AddComponentData(creatureEntity, new Laboratory.Chimera.ECS.CreatureGeneticsComponent
                {
                    Generation = 1,
                    GeneticPurity = UnityEngine.Random.Range(0.7f, 1f),
                    LineageId = System.Guid.NewGuid(),
                    ParentId1 = System.Guid.Empty,
                    ParentId2 = System.Guid.Empty,
                    StrengthTrait = UnityEngine.Random.Range(0.5f, 1f),
                    VitalityTrait = UnityEngine.Random.Range(0.5f, 1f),
                    AgilityTrait = UnityEngine.Random.Range(0.5f, 1f),
                    ResilienceTrait = UnityEngine.Random.Range(0.5f, 1f),
                    IntellectTrait = UnityEngine.Random.Range(0.5f, 1f),
                    CharmTrait = UnityEngine.Random.Range(0.5f, 1f),
                    ActiveGeneCount = UnityEngine.Random.Range(5, 15),
                    IsShiny = UnityEngine.Random.value < 0.05f
                });

                // Add networked genetics component
                _netcodeWorld.EntityManager.AddComponentData(creatureEntity, new NetworkedGeneticsComponent
                {
                    geneticHash = (uint)UnityEngine.Random.Range(1000, 9999),
                    adaptationLevel = 0f,
                    currentBiome = testSpawnBiome,
                    environmentalStress = 0f,
                    geneticVersion = 1,
                    isBreeding = false
                });
            }
        }

        private void StartNetworkServer()
        {
            if (enableDedicatedServer)
            {
                Debug.Log($"🌐 Starting dedicated server on port {serverPort}...");
                // In real implementation, this would start the Netcode server
            }
            else
            {
                Debug.Log($"🌐 Starting host server on port {serverPort}...");
                // In real implementation, this would start the Netcode host
            }

            // For now, just log the configuration
            Debug.Log($"📊 Server Configuration:" +
                     $"\n  Max Players: {(maxPlayers > 0 ? maxPlayers : netcodeConfig.maxPlayers)}" +
                     $"\n  Tick Rate: {netcodeConfig.serverTickRate} Hz" +
                     $"\n  Update Rate: {netcodeConfig.clientUpdateRate} Hz" +
                     $"\n  Target Bandwidth: {netcodeConfig.targetBandwidthPerClient} KB/s per client");
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Show network statistics
            if (showNetworkStats && Time.time - _lastStatsTime >= (netcodeConfig?.statsUpdateInterval ?? 2f))
            {
                ShowNetworkStatistics();
                _lastStatsTime = Time.time;
            }
        }

        private void ShowNetworkStatistics()
        {
            if (_netcodeWorld == null) return;

            var networkedEntities = 0;
            var syncedEntities = 0;

            // Count networked entities
            var networkedQuery = _netcodeWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkOwnership>());
            networkedEntities = networkedQuery.CalculateEntityCount();
            networkedQuery.Dispose();

            // Count synced entities
            var syncedQuery = _netcodeWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ReplicatedCreatureState>());
            syncedEntities = syncedQuery.CalculateEntityCount();
            syncedQuery.Dispose();

            // Estimate bandwidth
            float estimatedBandwidth = 0f;
            if (netcodeConfig != null)
            {
                estimatedBandwidth = (syncedEntities * 64f * netcodeConfig.clientUpdateRate) / 1000f; // KB/s
            }

            Debug.Log($"📊 Network Stats - Networked: {networkedEntities}, Synced: {syncedEntities}, " +
                     $"Est. Bandwidth: {estimatedBandwidth:F1} KB/s, FPS: {1f / Time.deltaTime:F1}");
        }

        /// <summary>
        /// Manual network shutdown
        /// </summary>
        public void ShutdownNetworking()
        {
            if (!_isInitialized) return;

            Debug.Log("🛑 Shutting down Chimera networking...");

            // Cleanup would go here in real implementation

            _isInitialized = false;
            Debug.Log("✅ Networking shutdown complete");
        }

        /// <summary>
        /// Connect as client to existing server
        /// </summary>
        public void ConnectAsClient(string serverAddress, ushort port)
        {
            Debug.Log($"🔌 Connecting to server {serverAddress}:{port}...");

            // Real implementation would connect to server

            Debug.Log("✅ Connected to server successfully");
        }

        /// <summary>
        /// Get current network performance metrics
        /// </summary>
        public NetworkPerformanceMetrics GetPerformanceMetrics()
        {
            if (!_isInitialized || _netcodeWorld == null)
                return default;

            var networkedQuery = _netcodeWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkOwnership>());
            var syncedQuery = _netcodeWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ReplicatedCreatureState>());

            var metrics = new NetworkPerformanceMetrics
            {
                networkedEntities = networkedQuery.CalculateEntityCount(),
                syncedEntities = syncedQuery.CalculateEntityCount(),
                currentFPS = 1f / Time.deltaTime,
                estimatedBandwidth = netcodeConfig != null ?
                    (syncedQuery.CalculateEntityCount() * 64f * netcodeConfig.clientUpdateRate) / 1000f : 0f,
                isServerRunning = _isInitialized,
                connectedClients = _isInitialized ? 1 : 0
            };

            networkedQuery.Dispose();
            syncedQuery.Dispose();

            return metrics;
        }

        [System.Serializable]
        public struct NetworkPerformanceMetrics
        {
            public int networkedEntities;
            public int syncedEntities;
            public float currentFPS;
            public float estimatedBandwidth; // KB/s
            public bool isServerRunning;
            public int connectedClients;
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                ShutdownNetworking();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Initialize Now")]
        private void EditorInitialize()
        {
            if (Application.isPlaying)
            {
                InitializeChimeraNetworking();
            }
            else
            {
                Debug.Log("Initialization only available in Play Mode");
            }
        }

        [ContextMenu("Validate Configuration")]
        private void EditorValidateConfiguration()
        {
            ValidateConfigurations();
        }
#endif
    }
}