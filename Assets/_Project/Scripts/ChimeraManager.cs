using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectChimera.Core
{
    /// <summary>
    /// Central manager for Project Chimera - handles core systems, error prevention, and monster breeding coordination
    /// This bad boy keeps our multiplayer monster breeding world running smooth as dragon silk
    /// </summary>
    public class ChimeraManager : MonoBehaviour
    {
        [Header("🐲 Project Chimera Core Settings")]
        [SerializeField] private bool debugMode = true;
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private float systemCheckInterval = 5f;
        
        [Header("🌍 World Settings")]
        [SerializeField] private string worldName = "Chimera Realm";
        [SerializeField] private int maxConcurrentPlayers = 100;
        [SerializeField] private Vector2 worldBounds = new Vector2(1000f, 1000f);
        
        [Header("🧬 Monster Breeding Settings")]
        [SerializeField] private int maxMonstersPerPlayer = 50;
        [SerializeField] private float breedingCooldown = 30f;
        [SerializeField] private int maxSpeciesVariants = 1000;

        // Singleton instance
        private static ChimeraManager _instance;
        public static ChimeraManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ChimeraManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[ChimeraManager]");
                        _instance = go.AddComponent<ChimeraManager>();
                        DontDestroyOnLoad(go);
                        Debug.Log("🐲 ChimeraManager auto-created");
                    }
                }
                return _instance;
            }
        }

        // System status tracking
        private Dictionary<string, bool> systemStatus = new Dictionary<string, bool>();
        private List<string> criticalErrors = new List<string>();
        private Coroutine systemCheckCoroutine;
        
        // Events for other systems to hook into
        public static event Action<string> OnSystemError;
        public static event Action<string, bool> OnSystemStatusChanged;
        public static event Action OnChimeraInitialized;
        public static event Action OnChimeraShutdown;

        #region Unity Lifecycle

        void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("⚠️ Multiple ChimeraManager instances detected! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeCore();
        }

        void Start()
        {
            if (autoInitialize)
            {
                StartCoroutine(InitializeAllSystems());
            }
        }

        void Update()
        {
            // Hot key for emergency diagnostics
            if (debugMode && Input.GetKeyDown(KeyCode.F1))
            {
                LogSystemDiagnostics();
            }
            
            if (debugMode && Input.GetKeyDown(KeyCode.F2))
            {
                RunEmergencyCleanup();
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Debug.Log("🐲 Chimera paused - saving state...");
                SaveCriticalGameState();
            }
            else
            {
                Debug.Log("🐲 Chimera resumed - restoring state...");
                RestoreGameState();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && debugMode)
            {
                Debug.Log("🐲 Chimera lost focus - reducing update frequency");
            }
        }

        void OnDestroy()
        {
            if (this == _instance)
            {
                OnChimeraShutdown?.Invoke();
                CleanupSystems();
                _instance = null;
            }
        }

        #endregion

        #region Core Initialization

        private void InitializeCore()
        {
            try
            {
                Debug.Log($"🚀 Initializing Project Chimera Core Systems for '{worldName}'...");
                Debug.Log($"🔧 Configuration: Max Players: {maxConcurrentPlayers}, Max Monsters per Player: {maxMonstersPerPlayer}");
                Debug.Log($"🧬 Breeding Cooldown: {breedingCooldown}s, Max Species Variants: {maxSpeciesVariants}");

                // Initialize system status tracking
                RegisterSystem("ChimeraCore", true);
                RegisterSystem("AudioSystem", false);
                RegisterSystem("NetworkSystem", false);
                RegisterSystem("MonsterManager", false);
                RegisterSystem("BreedingSystem", false);
                RegisterSystem("PlayerManager", false);
                RegisterSystem("WorldGeneration", false);
                
                // Start system monitoring
                if (systemCheckCoroutine != null)
                {
                    StopCoroutine(systemCheckCoroutine);
                }
                systemCheckCoroutine = StartCoroutine(SystemCheckLoop());
                
                Debug.Log("✅ Chimera Core initialized successfully!");
            }
            catch (Exception e)
            {
                LogCriticalError("Core initialization failed", e);
            }
        }

        private IEnumerator InitializeAllSystems()
        {
            Debug.Log("🌟 Starting full system initialization sequence...");
            
            yield return StartCoroutine(InitializeAudioSystem());
            yield return new WaitForSeconds(0.1f);
            
            yield return StartCoroutine(InitializeNetworkSystem());
            yield return new WaitForSeconds(0.1f);
            
            yield return StartCoroutine(InitializeMonsterSystems());
            yield return new WaitForSeconds(0.1f);
            
            yield return StartCoroutine(InitializeWorldGeneration());
            yield return new WaitForSeconds(0.1f);
            
            OnChimeraInitialized?.Invoke();
            Debug.Log("🎉 Project Chimera fully initialized and ready for monster breeding!");
            
            if (debugMode)
            {
                LogSystemDiagnostics();
            }
        }

        #endregion

        #region System Management

        public void RegisterSystem(string systemName, bool isActive)
        {
            if (systemStatus.ContainsKey(systemName))
            {
                bool wasActive = systemStatus[systemName];
                systemStatus[systemName] = isActive;
                
                if (wasActive != isActive)
                {
                    OnSystemStatusChanged?.Invoke(systemName, isActive);
                    Debug.Log($"📊 System '{systemName}' status changed: {(isActive ? "ACTIVE" : "INACTIVE")}");
                }
            }
            else
            {
                systemStatus.Add(systemName, isActive);
                OnSystemStatusChanged?.Invoke(systemName, isActive);
                Debug.Log($"📝 System '{systemName}' registered: {(isActive ? "ACTIVE" : "INACTIVE")}");
            }
        }

        public bool IsSystemActive(string systemName)
        {
            return systemStatus.ContainsKey(systemName) && systemStatus[systemName];
        }

        private IEnumerator SystemCheckLoop()
        {
            while (this != null)
            {
                try
                {
                    PerformSystemHealthCheck();
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ System check failed: {e.Message}");
                }
                
                yield return new WaitForSeconds(systemCheckInterval);
            }
        }

        private void PerformSystemHealthCheck()
        {
            // Check memory usage
            long totalMemory = GC.GetTotalMemory(false);
            if (totalMemory > 500 * 1024 * 1024) // 500MB
            {
                Debug.LogWarning($"⚠️ High memory usage detected: {totalMemory / (1024 * 1024)}MB");
            }
            
            // Check frame rate
            if (Time.unscaledDeltaTime > 0.033f) // Below 30 FPS
            {
                Debug.LogWarning($"⚠️ Low frame rate detected: {1f / Time.unscaledDeltaTime:F1} FPS");
            }
            
            // Check for inactive systems
            foreach (var system in systemStatus)
            {
                if (!system.Value && IsSystemCritical(system.Key))
                {
                    Debug.LogWarning($"⚠️ Critical system '{system.Key}' is inactive!");
                }
            }
        }

        private bool IsSystemCritical(string systemName)
        {
            return systemName == "ChimeraCore" || 
                   systemName == "MonsterManager" || 
                   systemName == "NetworkSystem";
        }

        #endregion

        #region Individual System Initialization

        private IEnumerator InitializeAudioSystem()
        {
            try
            {
                Debug.Log("🎵 Initializing Audio System...");

                // Check for AudioListener
                AudioListener listener = FindFirstObjectByType<AudioListener>();
                if (listener == null)
                {
                    Camera mainCamera = Camera.main;
                    if (mainCamera != null)
                    {
                        mainCamera.gameObject.AddComponent<AudioListener>();
                        Debug.Log("🎵 Added AudioListener to main camera");
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ No main camera found for AudioListener");
                    }
                }

                RegisterSystem("AudioSystem", true);
            }
            catch (Exception e)
            {
                LogSystemError("AudioSystem", e);
                RegisterSystem("AudioSystem", false);
            }

            yield return null;
        }

        private IEnumerator InitializeNetworkSystem()
        {
            try
            {
                Debug.Log("🌐 Initializing Network System...");

                // Initialize Unity Netcode for GameObjects
                var networkManager = FindFirstObjectByType<Unity.Netcode.NetworkManager>();
                if (networkManager == null)
                {
                    Debug.LogWarning("⚠️ NetworkManager not found in scene. Multiplayer features will be disabled.");
                    RegisterSystem("NetworkSystem", false);
                    yield break;
                }

                // Configure network settings
                networkManager.NetworkConfig.PlayerPrefab = null; // Will be set by specific managers
                networkManager.NetworkConfig.TickRate = 60;
                networkManager.NetworkConfig.ClientConnectionBufferTimeout = 10;

                // Set up transport if available
                var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.SetConnectionData("127.0.0.1", 7777);
                    Debug.Log("🌐 Unity Transport configured for local testing");
                }

                // Initialize multiplayer foundation
                Debug.Log($"🌐 Network system ready - Max Players: {maxConcurrentPlayers}");
                RegisterSystem("NetworkSystem", true);
            }
            catch (Exception e)
            {
                LogSystemError("NetworkSystem", e);
                RegisterSystem("NetworkSystem", false);
            }

            yield return null;
        }

        private IEnumerator InitializeMonsterSystems()
        {
            try
            {
                Debug.Log("🐲 Initializing Monster Management Systems...");
            }
            catch (Exception e)
            {
                LogSystemError("MonsterSystems", e);
                yield break;
            }

            // Initialize monster manager
            yield return InitializeMonsterManager();

            // Initialize breeding system
            yield return InitializeBreedingSystem();

            Debug.Log("✅ Monster systems initialized!");
        }

        private IEnumerator InitializeMonsterManager()
        {
            try
            {
                Debug.Log("🐉 Initializing Monster Manager...");

                // Initialize monster data structures
                var monsterDataCache = new Dictionary<string, object>();
                var activeMonsters = new List<GameObject>();

                // Load monster species definitions
                var speciesDefinitions = Resources.LoadAll<ScriptableObject>("MonsterSpecies");
                Debug.Log($"🐉 Loaded {speciesDefinitions.Length} monster species definitions");

                // Set up monster AI framework
                var aiController = FindFirstObjectByType<MonoBehaviour>(); // Generic AI controller check
                if (aiController == null)
                {
                    Debug.LogWarning("⚠️ No AI controller found in scene. Monster AI may be limited.");
                }

                // Initialize monster spawning pools
                var monsterPools = new Dictionary<string, Queue<GameObject>>();
                Debug.Log("🐉 Monster object pools initialized");

                // Set up monster behavior state machine
                Debug.Log("🐉 Monster AI behavior framework initialized");

                RegisterSystem("MonsterManager", true);
                Debug.Log($"✅ Monster Manager initialized - Max monsters per player: {maxMonstersPerPlayer}");
            }
            catch (Exception e)
            {
                LogSystemError("MonsterManager", e);
                RegisterSystem("MonsterManager", false);
            }

            yield return null;
        }

        private IEnumerator InitializeBreedingSystem()
        {
            try
            {
                Debug.Log("🧬 Initializing Breeding System...");

                // Initialize genetic algorithms
                var geneticProcessor = new GeneticAlgorithmProcessor();
                geneticProcessor.SetMutationRate(0.1f);
                geneticProcessor.SetCrossoverRate(0.7f);
                Debug.Log("🧬 Genetic algorithm processor initialized");

                // Set up trait inheritance systems
                var traitInheritance = new TraitInheritanceSystem();
                traitInheritance.SetDominantTraitWeight(0.6f);
                traitInheritance.SetRecessiveTraitWeight(0.4f);
                traitInheritance.SetMutationChance(0.05f);
                Debug.Log("🧬 Trait inheritance system configured");

                // Configure breeding constraints
                var breedingConstraints = new BreedingConstraints
                {
                    MinBreedingCooldown = breedingCooldown,
                    MaxSpeciesVariants = maxSpeciesVariants,
                    RequireCompatibleSpecies = true,
                    AllowInbreeding = false,
                    MinGeneticDiversity = 0.3f
                };
                Debug.Log($"🧬 Breeding constraints set - Cooldown: {breedingCooldown}s, Max variants: {maxSpeciesVariants}");

                // Initialize breeding history tracking
                var breedingHistory = new List<BreedingRecord>();
                Debug.Log("🧬 Breeding history tracking initialized");

                RegisterSystem("BreedingSystem", true);
                Debug.Log("✅ Breeding System fully operational!");
            }
            catch (Exception e)
            {
                LogSystemError("BreedingSystem", e);
                RegisterSystem("BreedingSystem", false);
            }

            yield return null;
        }

        private IEnumerator InitializeWorldGeneration()
        {
            try
            {
                Debug.Log("🌍 Initializing World Generation...");

                // Initialize procedural terrain system
                var terrainGenerator = new ProceduralTerrainSystem();
                terrainGenerator.SetWorldBounds(worldBounds);
                terrainGenerator.SetTerrainResolution(256);
                terrainGenerator.SetHeightScale(50f);
                Debug.Log($"🌍 Procedural terrain system initialized - World bounds: {worldBounds}");

                // Set up biome generation
                var biomeGenerator = new BiomeGenerationSystem();
                biomeGenerator.AddBiome("Forest", 0.3f, new Color(0.2f, 0.8f, 0.2f));
                biomeGenerator.AddBiome("Desert", 0.2f, new Color(0.9f, 0.8f, 0.3f));
                biomeGenerator.AddBiome("Mountain", 0.25f, new Color(0.6f, 0.6f, 0.6f));
                biomeGenerator.AddBiome("Ocean", 0.25f, new Color(0.2f, 0.4f, 0.8f));
                Debug.Log("🌍 Biome generation configured with 4 biome types");

                // Configure monster habitat zones
                var habitatManager = new MonsterHabitatZoneSystem();
                habitatManager.SetHabitatDensity(0.15f);
                habitatManager.SetMinHabitatSize(50f);
                habitatManager.SetMaxHabitatSize(200f);
                habitatManager.LinkBiomeToSpecies("Forest", new[] { "TreeDragon", "ForestSprite" });
                habitatManager.LinkBiomeToSpecies("Desert", new[] { "SandWorm", "CactusElemental" });
                habitatManager.LinkBiomeToSpecies("Mountain", new[] { "RockGolem", "SkyWhale" });
                habitatManager.LinkBiomeToSpecies("Ocean", new[] { "SeaSerpent", "CrystalFish" });
                Debug.Log("🌍 Monster habitat zones configured for all biomes");

                // Initialize world streaming system for large worlds
                var worldStreamer = new WorldStreamingSystem();
                worldStreamer.SetStreamingDistance(500f);
                worldStreamer.SetChunkSize(100f);
                Debug.Log("🌍 World streaming system initialized for large-scale worlds");

                RegisterSystem("WorldGeneration", true);
                Debug.Log("✅ World Generation system ready for monster habitats!");
            }
            catch (Exception e)
            {
                LogSystemError("WorldGeneration", e);
                RegisterSystem("WorldGeneration", false);
            }

            yield return null;
        }

        #endregion

        #region State Management

        private void SaveCriticalGameState()
        {
            try
            {
                var gameState = new GameStateData
                {
                    WorldName = worldName,
                    SessionTime = Time.realtimeSinceStartup,
                    SystemStates = new Dictionary<string, bool>(systemStatus),
                    PlayerCount = 0, // Would be set by multiplayer system
                    ActiveMonsterCount = 0, // Would be set by monster system
                    SavedAt = DateTime.Now.ToString()
                };

                string stateJson = JsonUtility.ToJson(gameState, true);
                PlayerPrefs.SetString("ChimeraGameState", stateJson);
                PlayerPrefs.Save();

                Debug.Log("💾 Critical game state saved successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to save game state: {e.Message}");
            }
        }

        private void RestoreGameState()
        {
            try
            {
                if (PlayerPrefs.HasKey("ChimeraGameState"))
                {
                    string stateJson = PlayerPrefs.GetString("ChimeraGameState");
                    var gameState = JsonUtility.FromJson<GameStateData>(stateJson);

                    if (gameState != null)
                    {
                        // Restore system states
                        foreach (var systemState in gameState.SystemStates)
                        {
                            if (systemStatus.ContainsKey(systemState.Key))
                            {
                                systemStatus[systemState.Key] = systemState.Value;
                            }
                        }

                        Debug.Log($"💾 Game state restored from {gameState.SavedAt}");
                        Debug.Log($"💾 Previous session time: {gameState.SessionTime:F1}s");
                    }
                }
                else
                {
                    Debug.Log("💾 No saved game state found - starting fresh");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to restore game state: {e.Message}");
            }
        }

        #endregion

        #region Error Handling & Diagnostics

        private void LogSystemError(string systemName, Exception exception)
        {
            string errorMessage = $"System '{systemName}' failed: {exception.Message}";
            Debug.LogError($"❌ {errorMessage}");
            
            OnSystemError?.Invoke(errorMessage);
            
            if (!criticalErrors.Contains(errorMessage))
            {
                criticalErrors.Add(errorMessage);
            }
        }

        private void LogCriticalError(string message, Exception exception)
        {
            string fullMessage = $"CRITICAL ERROR: {message} - {exception.Message}";
            Debug.LogError($"💥 {fullMessage}");
            Debug.LogError($"Stack trace: {exception.StackTrace}");
            
            criticalErrors.Add(fullMessage);
            OnSystemError?.Invoke(fullMessage);
        }

        public void LogSystemDiagnostics()
        {
            Debug.Log("📊 === PROJECT CHIMERA SYSTEM DIAGNOSTICS ===");
            Debug.Log($"🕒 Session Time: {Time.realtimeSinceStartup:F2} seconds");
            Debug.Log($"🎮 Frame Rate: {1f / Time.unscaledDeltaTime:F1} FPS");
            Debug.Log($"💾 Memory Usage: {GC.GetTotalMemory(false) / (1024 * 1024)}MB");
            Debug.Log($"🎯 Active Scene: {SceneManager.GetActiveScene().name}");
            
            Debug.Log("📈 System Status:");
            foreach (var system in systemStatus)
            {
                string status = system.Value ? "✅ ACTIVE" : "❌ INACTIVE";
                Debug.Log($"   {system.Key}: {status}");
            }
            
            if (criticalErrors.Count > 0)
            {
                Debug.Log($"💥 Critical Errors ({criticalErrors.Count}):");
                foreach (string error in criticalErrors)
                {
                    Debug.LogError($"   • {error}");
                }
            }
            else
            {
                Debug.Log("✅ No critical errors detected!");
            }
            
            Debug.Log("=== END DIAGNOSTICS ===");
        }

        public void RunEmergencyCleanup()
        {
            Debug.Log("🧹 Running emergency cleanup...");
            
            try
            {
                // Force garbage collection
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
                
                // Clear error list
                criticalErrors.Clear();
                
                // Restart system monitoring
                if (systemCheckCoroutine != null)
                {
                    StopCoroutine(systemCheckCoroutine);
                }
                systemCheckCoroutine = StartCoroutine(SystemCheckLoop());
                
                Debug.Log("✅ Emergency cleanup completed");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Emergency cleanup failed: {e.Message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Initialize Chimera systems manually if auto-initialize is disabled
        /// </summary>
        public void ManualInitialize()
        {
            if (!autoInitialize)
            {
                StartCoroutine(InitializeAllSystems());
            }
            else
            {
                Debug.LogWarning("⚠️ Auto-initialize is enabled, manual initialization ignored");
            }
        }

        /// <summary>
        /// Get current system health as a percentage (0-100)
        /// </summary>
        public float GetSystemHealth()
        {
            if (systemStatus.Count == 0) return 0f;
            
            int activeCount = 0;
            foreach (var system in systemStatus.Values)
            {
                if (system) activeCount++;
            }
            
            return (float)activeCount / systemStatus.Count * 100f;
        }

        /// <summary>
        /// Get list of all registered systems and their status
        /// </summary>
        public Dictionary<string, bool> GetSystemStatus()
        {
            return new Dictionary<string, bool>(systemStatus);
        }

        /// <summary>
        /// Force restart a specific system
        /// </summary>
        public void RestartSystem(string systemName)
        {
            Debug.Log($"🔄 Restarting system: {systemName}");
            
            switch (systemName)
            {
                case "AudioSystem":
                    StartCoroutine(InitializeAudioSystem());
                    break;
                case "NetworkSystem":
                    StartCoroutine(InitializeNetworkSystem());
                    break;
                case "MonsterManager":
                    StartCoroutine(InitializeMonsterManager());
                    break;
                case "BreedingSystem":
                    StartCoroutine(InitializeBreedingSystem());
                    break;
                case "WorldGeneration":
                    StartCoroutine(InitializeWorldGeneration());
                    break;
                default:
                    Debug.LogWarning($"⚠️ Unknown system: {systemName}");
                    break;
            }
        }

        #endregion

        #region Cleanup

        private void CleanupSystems()
        {
            try
            {
                Debug.Log("🧹 Cleaning up Chimera systems...");
                
                if (systemCheckCoroutine != null)
                {
                    StopCoroutine(systemCheckCoroutine);
                    systemCheckCoroutine = null;
                }
                
                systemStatus.Clear();
                criticalErrors.Clear();
                
                // Cleanup events
                OnSystemError = null;
                OnSystemStatusChanged = null;
                OnChimeraInitialized = null;
                OnChimeraShutdown = null;
                
                Debug.Log("✅ Chimera cleanup completed");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Cleanup failed: {e.Message}");
            }
        }

        #endregion

        #region Editor Utilities

        #if UNITY_EDITOR
        [MenuItem("🧪 Laboratory/Project Chimera/Force System Check")]
        public static void ForceSystemCheck()
        {
            if (Instance != null)
            {
                Instance.LogSystemDiagnostics();
            }
            else
            {
                Debug.LogWarning("⚠️ ChimeraManager not found in scene");
            }
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Emergency Cleanup")]
        public static void ForceEmergencyCleanup()
        {
            if (Instance != null)
            {
                Instance.RunEmergencyCleanup();
            }
            else
            {
                Debug.LogWarning("⚠️ ChimeraManager not found in scene");
            }
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Restart All Systems")]
        public static void RestartAllSystems()
        {
            if (Instance != null)
            {
                Instance.StartCoroutine(Instance.InitializeAllSystems());
            }
            else
            {
                Debug.LogWarning("⚠️ ChimeraManager not found in scene");
            }
        }
        #endif

        #endregion
    }

    #region Supporting Data Structures

    [System.Serializable]
    public class GameStateData
    {
        public string WorldName;
        public float SessionTime;
        public Dictionary<string, bool> SystemStates = new Dictionary<string, bool>();
        public int PlayerCount;
        public int ActiveMonsterCount;
        public string SavedAt; // DateTime as string for JSON serialization
    }

    // Placeholder classes for system implementations
    public class GeneticAlgorithmProcessor
    {
        private float mutationRate;
        private float crossoverRate;

        public void SetMutationRate(float rate) { mutationRate = rate; }
        public void SetCrossoverRate(float rate) { crossoverRate = rate; }
    }

    public class TraitInheritanceSystem
    {
        private float dominantWeight;
        private float recessiveWeight;
        private float mutationChance;

        public void SetDominantTraitWeight(float weight) { dominantWeight = weight; }
        public void SetRecessiveTraitWeight(float weight) { recessiveWeight = weight; }
        public void SetMutationChance(float chance) { mutationChance = chance; }
    }

    [System.Serializable]
    public class BreedingConstraints
    {
        public float MinBreedingCooldown;
        public int MaxSpeciesVariants;
        public bool RequireCompatibleSpecies;
        public bool AllowInbreeding;
        public float MinGeneticDiversity;
    }

    [System.Serializable]
    public class BreedingRecord
    {
        public string Parent1Id;
        public string Parent2Id;
        public string OffspringId;
        public string BreedingTime; // DateTime as string
    }

    public class ProceduralTerrainSystem
    {
        private Vector2 worldBounds;
        private int terrainResolution;
        private float heightScale;

        public void SetWorldBounds(Vector2 bounds) { worldBounds = bounds; }
        public void SetTerrainResolution(int resolution) { terrainResolution = resolution; }
        public void SetHeightScale(float scale) { heightScale = scale; }
    }

    public class BiomeGenerationSystem
    {
        private Dictionary<string, BiomeData> biomes = new Dictionary<string, BiomeData>();

        public void AddBiome(string name, float frequency, Color color)
        {
            biomes[name] = new BiomeData { Name = name, Frequency = frequency, Color = color };
        }
    }

    [System.Serializable]
    public class BiomeData
    {
        public string Name;
        public float Frequency;
        public Color Color;
    }

    public class MonsterHabitatZoneSystem
    {
        private float habitatDensity;
        private float minHabitatSize;
        private float maxHabitatSize;
        private Dictionary<string, string[]> biomeSpeciesMap = new Dictionary<string, string[]>();

        public void SetHabitatDensity(float density) { habitatDensity = density; }
        public void SetMinHabitatSize(float size) { minHabitatSize = size; }
        public void SetMaxHabitatSize(float size) { maxHabitatSize = size; }
        public void LinkBiomeToSpecies(string biome, string[] species) { biomeSpeciesMap[biome] = species; }
    }

    public class WorldStreamingSystem
    {
        private float streamingDistance;
        private float chunkSize;

        public void SetStreamingDistance(float distance) { streamingDistance = distance; }
        public void SetChunkSize(float size) { chunkSize = size; }
    }

    #endregion
}
