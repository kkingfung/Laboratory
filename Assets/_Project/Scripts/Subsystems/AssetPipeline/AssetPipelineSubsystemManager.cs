using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;

namespace Laboratory.Subsystems.AssetPipeline
{
    /// <summary>
    /// Asset Pipeline Subsystem Manager for Project Chimera.
    /// Handles procedural creature generation, LOD management, dynamic texture generation,
    /// and memory optimization for visual assets based on genetic traits.
    /// </summary>
    public class AssetPipelineSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        [Header("Configuration")]
        [SerializeField] private AssetPipelineSubsystemConfig config;

        [Header("Services")]
        [SerializeField] private bool enableProceduralGeneration = true;
        [SerializeField] private bool enableLODManagement = true;
        [SerializeField] private bool enableDynamicTextures = true;
        [SerializeField] private bool enableMemoryOptimization = true;

        // Public Properties
        public bool IsInitialized { get; private set; }
        public string SubsystemName => "AssetPipeline";
        public float InitializationProgress { get; private set; }

        // Services
        public IProceduralGenerationService ProceduralGenerationService { get; private set; }
        public ILODManagementService LODManagementService { get; private set; }
        public IDynamicTextureService DynamicTextureService { get; private set; }
        public IAssetOptimizationService AssetOptimizationService { get; private set; }

        // Events
        public static event Action<CreatureAssetGeneratedEvent> OnCreatureAssetGenerated;
        public static event Action<LODUpdateEvent> OnLODUpdated;
        public static event Action<TextureGeneratedEvent> OnTextureGenerated;
        public static event Action<AssetOptimizationEvent> OnAssetOptimized;

        private readonly Dictionary<string, GeneratedAssetData> _generatedAssets = new();
        private readonly Dictionary<string, AssetCacheEntry> _assetCache = new();
        private readonly Queue<AssetGenerationRequest> _generationQueue = new();
        private readonly AssetPerformanceMetrics _performanceMetrics = new();

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateConfiguration();
            InitializeComponents();
        }

        private void Start()
        {
            _ = InitializeAsync();
        }

        private void Update()
        {
            ProcessGenerationQueue();
            UpdateLODSystem();
            UpdatePerformanceMetrics();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void ValidateConfiguration()
        {
            if (config == null)
            {
                Debug.LogError($"[{SubsystemName}] Configuration is missing! Please assign an AssetPipelineSubsystemConfig.");
                return;
            }

            if (config.proceduralAssetTemplates == null || config.proceduralAssetTemplates.Count == 0)
            {
                Debug.LogWarning($"[{SubsystemName}] No procedural asset templates configured. Procedural generation will be limited.");
            }

            if (config.lodSettings == null)
            {
                Debug.LogWarning($"[{SubsystemName}] LOD settings not configured. LOD management may not work optimally.");
            }
        }

        private void InitializeComponents()
        {
            InitializationProgress = 0.2f;

            // Initialize asset services
            ProceduralGenerationService = new ProceduralGenerationService(config);
            LODManagementService = new LODManagementService(config);
            DynamicTextureService = new DynamicTextureService(config);
            AssetOptimizationService = new AssetOptimizationService(config);

            InitializationProgress = 0.4f;
        }

        private async Task InitializeAsync()
        {
            try
            {
                InitializationProgress = 0.5f;

                // Initialize procedural generation
                if (enableProceduralGeneration)
                {
                    await ProceduralGenerationService.InitializeAsync();
                }
                InitializationProgress = 0.6f;

                // Initialize LOD management
                if (enableLODManagement)
                {
                    await LODManagementService.InitializeAsync();
                }
                InitializationProgress = 0.7f;

                // Initialize dynamic textures
                if (enableDynamicTextures)
                {
                    await DynamicTextureService.InitializeAsync();
                }
                InitializationProgress = 0.8f;

                // Initialize asset optimization
                if (enableMemoryOptimization)
                {
                    await AssetOptimizationService.InitializeAsync();
                }
                InitializationProgress = 0.9f;

                // Subscribe to game events
                SubscribeToGameEvents();

                // Register services
                RegisterServices();

                // Start background asset processing
                _ = StartAssetProcessingLoop();

                IsInitialized = true;
                InitializationProgress = 1.0f;

                Debug.Log($"[{SubsystemName}] Initialization complete. " +
                         $"Procedural Generation: {enableProceduralGeneration}, " +
                         $"LOD Management: {enableLODManagement}, " +
                         $"Dynamic Textures: {enableDynamicTextures}, " +
                         $"Memory Optimization: {enableMemoryOptimization}");

                // Notify system initialization
                EventBus.Publish(new SubsystemInitializedEvent(SubsystemName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Initialization failed: {ex.Message}");
                InitializationProgress = 0f;
            }
        }

        private void SubscribeToGameEvents()
        {
            // Subscribe to genetics events for procedural generation
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete += HandleBreedingComplete;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred += HandleMutationOccurred;

            // Subscribe to player events for LOD management
            // These would be connected when player system events are available
        }

        private void RegisterServices()
        {
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.Register<IProceduralGenerationService>(ProceduralGenerationService);
                ServiceContainer.Instance.Register<ILODManagementService>(LODManagementService);
                ServiceContainer.Instance.Register<IDynamicTextureService>(DynamicTextureService);
                ServiceContainer.Instance.Register<IAssetOptimizationService>(AssetOptimizationService);
                ServiceContainer.Instance.Register<AssetPipelineSubsystemManager>(this);
            }
        }

        #endregion

        #region Core Asset Operations

        /// <summary>
        /// Generates procedural assets for a creature based on genetic data
        /// </summary>
        public async Task<GeneratedAssetData> GenerateCreatureAssetsAsync(Laboratory.Subsystems.Genetics.GeneticProfile geneticProfile, string creatureId)
        {
            if (!IsInitialized || !enableProceduralGeneration)
                return null;

            // Check cache first
            if (_assetCache.TryGetValue(creatureId, out var cacheEntry) && !cacheEntry.IsExpired)
            {
                return cacheEntry.assetData;
            }

            var request = new AssetGenerationRequest
            {
                requestId = Guid.NewGuid().ToString(),
                creatureId = creatureId,
                geneticProfile = geneticProfile,
                requestType = AssetGenerationType.Complete,
                priority = AssetGenerationPriority.Normal,
                timestamp = DateTime.Now
            };

            _generationQueue.Enqueue(request);

            // Wait for generation to complete (in a real implementation, this would be more sophisticated)
            await Task.Delay(100);

            return _generatedAssets.GetValueOrDefault(creatureId);
        }

        /// <summary>
        /// Updates LOD levels for all active creatures based on distance and importance
        /// </summary>
        public void UpdateCreatureLOD(string creatureId, float distanceFromPlayer, float importanceScore = 1f)
        {
            if (!enableLODManagement)
                return;

            LODManagementService.UpdateCreatureLOD(creatureId, distanceFromPlayer, importanceScore);
        }

        /// <summary>
        /// Generates dynamic textures for creature traits
        /// </summary>
        public async Task<Texture2D> GenerateCreatureTextureAsync(Laboratory.Subsystems.Genetics.GeneticProfile geneticProfile, TextureType textureType)
        {
            if (!enableDynamicTextures)
                return null;

            return await DynamicTextureService.GenerateTextureAsync(geneticProfile, textureType);
        }

        /// <summary>
        /// Optimizes memory usage by unloading unused assets
        /// </summary>
        public void OptimizeMemoryUsage()
        {
            if (!enableMemoryOptimization)
                return;

            AssetOptimizationService.OptimizeMemoryUsage();
            CleanupExpiredAssets();
        }

        /// <summary>
        /// Gets current asset performance metrics
        /// </summary>
        public AssetPerformanceMetrics GetPerformanceMetrics()
        {
            return _performanceMetrics;
        }

        /// <summary>
        /// Preloads assets for upcoming genetic combinations
        /// </summary>
        public async Task PreloadAssetsAsync(List<Laboratory.Subsystems.Genetics.GeneticProfile> upcomingProfiles)
        {
            if (!enableProceduralGeneration)
                return;

            foreach (var profile in upcomingProfiles)
            {
                var preloadRequest = new AssetGenerationRequest
                {
                    requestId = Guid.NewGuid().ToString(),
                    creatureId = $"preload_{Guid.NewGuid()}",
                    geneticProfile = profile,
                    requestType = AssetGenerationType.Preload,
                    priority = AssetGenerationPriority.Low,
                    timestamp = DateTime.Now
                };

                _generationQueue.Enqueue(preloadRequest);
            }
        }

        #endregion

        #region Queue Processing

        private void ProcessGenerationQueue()
        {
            const int maxRequestsPerFrame = 3;
            int processedCount = 0;

            while (_generationQueue.Count > 0 && processedCount < maxRequestsPerFrame)
            {
                var request = _generationQueue.Dequeue();
                ProcessAssetGenerationRequest(request);
                processedCount++;
            }
        }

        private void ProcessAssetGenerationRequest(AssetGenerationRequest request)
        {
            try
            {
                var startTime = DateTime.Now;

                // Generate the asset based on genetic profile
                var assetData = ProceduralGenerationService.GenerateCreatureAsset(request.geneticProfile, request.creatureId);

                if (assetData != null)
                {
                    // Cache the generated asset
                    _generatedAssets[request.creatureId] = assetData;
                    _assetCache[request.creatureId] = new AssetCacheEntry
                    {
                        assetData = assetData,
                        creationTime = DateTime.Now,
                        lastAccessTime = DateTime.Now,
                        accessCount = 1
                    };

                    // Update performance metrics
                    var generationTime = DateTime.Now - startTime;
                    _performanceMetrics.totalAssetsGenerated++;
                    _performanceMetrics.averageGenerationTime =
                        (_performanceMetrics.averageGenerationTime * (_performanceMetrics.totalAssetsGenerated - 1) +
                         (float)generationTime.TotalMilliseconds) / _performanceMetrics.totalAssetsGenerated;

                    // Fire event
                    var generatedEvent = new CreatureAssetGeneratedEvent
                    {
                        creatureId = request.creatureId,
                        assetData = assetData,
                        generationTime = generationTime,
                        timestamp = DateTime.Now
                    };

                    OnCreatureAssetGenerated?.Invoke(generatedEvent);

                    Debug.Log($"[{SubsystemName}] Generated assets for creature {request.creatureId} in {generationTime.TotalMilliseconds:F1}ms");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Failed to generate assets for request {request.requestId}: {ex.Message}");
                _performanceMetrics.failedGenerations++;
            }
        }

        #endregion

        #region LOD Management

        private void UpdateLODSystem()
        {
            if (!enableLODManagement)
                return;

            // Update LOD system every few frames
            if (Time.frameCount % config.lodUpdateFrequency == 0)
            {
                LODManagementService.UpdateAllLODs();
            }
        }

        #endregion

        #region Performance Monitoring

        private void UpdatePerformanceMetrics()
        {
            // Update every second
            if (Time.unscaledTime % 1f < Time.unscaledDeltaTime)
            {
                _performanceMetrics.currentMemoryUsage = GC.GetTotalMemory(false) / (1024f * 1024f);
                _performanceMetrics.activeAssetCount = _generatedAssets.Count;
                _performanceMetrics.cachedAssetCount = _assetCache.Count;
                _performanceMetrics.queuedRequests = _generationQueue.Count;

                // Check memory thresholds
                if (_performanceMetrics.currentMemoryUsage > config.memoryThresholdMB)
                {
                    OptimizeMemoryUsage();
                }
            }
        }

        #endregion

        #region Memory Management

        private void CleanupExpiredAssets()
        {
            var now = DateTime.Now;
            var expiredKeys = new List<string>();

            foreach (var kvp in _assetCache)
            {
                var cacheEntry = kvp.Value;
                var age = now - cacheEntry.lastAccessTime;

                if (age.TotalMinutes > config.assetCacheTimeoutMinutes && cacheEntry.accessCount < config.minAccessCountForRetention)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }

            foreach (var key in expiredKeys)
            {
                if (_assetCache.TryGetValue(key, out var entry))
                {
                    // Dispose of Unity objects if needed
                    entry.assetData?.Dispose();
                    _assetCache.Remove(key);
                    _generatedAssets.Remove(key);
                }
            }

            if (expiredKeys.Count > 0)
            {
                Debug.Log($"[{SubsystemName}] Cleaned up {expiredKeys.Count} expired assets");
            }
        }

        #endregion

        #region Event Handlers

        private void HandleBreedingComplete(Laboratory.Subsystems.Genetics.GeneticBreedingResult result)
        {
            if (result?.offspring != null && enableProceduralGeneration)
            {
                // Queue asset generation for new offspring
                var request = new AssetGenerationRequest
                {
                    requestId = Guid.NewGuid().ToString(),
                    creatureId = result.offspringId,
                    geneticProfile = result.offspring,
                    requestType = AssetGenerationType.NewOffspring,
                    priority = AssetGenerationPriority.High,
                    timestamp = DateTime.Now
                };

                _generationQueue.Enqueue(request);
            }
        }

        private void HandleMutationOccurred(Laboratory.Subsystems.Genetics.MutationEvent mutationEvent)
        {
            // Check if we need to regenerate assets due to significant mutations
            if (mutationEvent.mutation.severity > 0.5f)
            {
                // Invalidate cached assets for this creature
                var creatureId = mutationEvent.creatureId;
                if (_assetCache.ContainsKey(creatureId))
                {
                    _assetCache.Remove(creatureId);
                    _generatedAssets.Remove(creatureId);

                    Debug.Log($"[{SubsystemName}] Invalidated assets for creature {creatureId} due to significant mutation");
                }
            }
        }

        #endregion

        #region Background Processing

        private async Task StartAssetProcessingLoop()
        {
            while (IsInitialized)
            {
                try
                {
                    await Task.Delay(config.backgroundProcessingIntervalMs);

                    // Perform background optimization
                    if (enableMemoryOptimization)
                    {
                        AssetOptimizationService.PerformBackgroundOptimization();
                    }

                    // Update texture streaming
                    if (enableDynamicTextures)
                    {
                        DynamicTextureService.UpdateTextureStreaming();
                    }

                    // Cleanup old cache entries
                    CleanupExpiredAssets();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{SubsystemName}] Background processing error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            // Unsubscribe from events
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete -= HandleBreedingComplete;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred -= HandleMutationOccurred;

            // Dispose of all cached assets
            foreach (var entry in _assetCache.Values)
            {
                entry.assetData?.Dispose();
            }

            // Clear collections
            _generatedAssets.Clear();
            _assetCache.Clear();
            _generationQueue.Clear();

            Debug.Log($"[{SubsystemName}] Cleanup complete. Disposed {_assetCache.Count} cached assets.");
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Force Memory Cleanup")]
        private void ForceMemoryCleanup()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Asset Pipeline subsystem not initialized");
                return;
            }

            OptimizeMemoryUsage();
        }

        [ContextMenu("Generate Test Asset")]
        private void GenerateTestAsset()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Asset Pipeline subsystem not initialized");
                return;
            }

            // Create a test genetic profile
            var testProfile = new Laboratory.Subsystems.Genetics.GeneticProfile();
            _ = GenerateCreatureAssetsAsync(testProfile, "TestCreature_" + Time.time);
        }

        [ContextMenu("Print Performance Metrics")]
        private void PrintPerformanceMetrics()
        {
            var metrics = GetPerformanceMetrics();
            Debug.Log($"Asset Pipeline Performance:\n" +
                     $"Generated Assets: {metrics.totalAssetsGenerated}\n" +
                     $"Failed Generations: {metrics.failedGenerations}\n" +
                     $"Average Generation Time: {metrics.averageGenerationTime:F1}ms\n" +
                     $"Memory Usage: {metrics.currentMemoryUsage:F1}MB\n" +
                     $"Active Assets: {metrics.activeAssetCount}\n" +
                     $"Cached Assets: {metrics.cachedAssetCount}\n" +
                     $"Queued Requests: {metrics.queuedRequests}");
        }

        #endregion
    }
}