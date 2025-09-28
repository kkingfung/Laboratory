using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Laboratory.Core.Performance
{
    /// <summary>
    /// GPU-accelerated procedural generation system for Project Chimera
    /// Features: Compute shader-based terrain/biome generation, GPU particle systems,
    /// Massive ecosystem generation with 100k+ objects processed on GPU
    /// </summary>
    public class GPUProceduralGeneration : MonoBehaviour
    {
        [Header("GPU Configuration")]
        [SerializeField] private ComputeShader terrainGenerationShader;
        [SerializeField] private ComputeShader ecosystemGenerationShader;
        [SerializeField] private ComputeShader biomeDistributionShader;
        [SerializeField] private ComputeShader creatureSpawningShader;

        [Header("Generation Settings")]
        [SerializeField] private int terrainResolution = 1024;
        [SerializeField] private int maxEcosystemObjects = 100000;
        [SerializeField] private int biomeChunkSize = 64; // Size of biome processing chunks for parallel generation
        [SerializeField] private float noiseScale = 0.01f; // Default noise scale for procedural generation
        [SerializeField] private float elevationMultiplier = 100f; // Default terrain elevation scaling factor

        [Header("Performance")]
        [SerializeField] private int computeThreadGroupSize = 64;
        [SerializeField] private bool useAsyncGPUReadback = true;
        [SerializeField] private int maxGenerationBudgetMs = 5; // Maximum milliseconds per frame for generation

        // GPU buffers for massive data processing
        private ComputeBuffer _heightmapBuffer;
        private ComputeBuffer _biomeBuffer;
        private ComputeBuffer _ecosystemObjectsBuffer;
        private ComputeBuffer _creatureSpawnPointsBuffer;
        private ComputeBuffer _noiseParametersBuffer;

        // GPU textures for high-resolution data
        private RenderTexture _heightmapTexture;
        private RenderTexture _biomeMapTexture;
        private RenderTexture _moistureMapTexture;
        private RenderTexture _temperatureMapTexture;

        // Async GPU readback for non-blocking operations
        private Queue<AsyncGPUReadbackRequest> _pendingReadbacks;
        private System.Action<NativeArray<float>> _onTerrainGenerated;
        private System.Action<NativeArray<BiomeData>> _onBiomeGenerated;

        // GPU data structures
        private GPUTerrainData[] _gpuTerrainData;
        private GPUEcosystemObject[] _gpuEcosystemObjects;
        private GPUCreatureSpawnPoint[] _gpuCreatureSpawns;

        // Performance tracking
        private float _lastGenerationTime;

        #region Initialization

        private void Awake()
        {
            InitializeGPUResources();
        }

        private void InitializeGPUResources()
        {
            // Create GPU buffers for massive parallel processing
            _heightmapBuffer = new ComputeBuffer(terrainResolution * terrainResolution, sizeof(float));
            _biomeBuffer = new ComputeBuffer(terrainResolution * terrainResolution, System.Runtime.InteropServices.Marshal.SizeOf<BiomeData>());
            _ecosystemObjectsBuffer = new ComputeBuffer(maxEcosystemObjects, System.Runtime.InteropServices.Marshal.SizeOf<GPUEcosystemObject>());
            _creatureSpawnPointsBuffer = new ComputeBuffer(maxEcosystemObjects / 10, System.Runtime.InteropServices.Marshal.SizeOf<GPUCreatureSpawnPoint>());

            // Create high-resolution GPU textures
            _heightmapTexture = new RenderTexture(terrainResolution, terrainResolution, 0, RenderTextureFormat.RFloat);
            _heightmapTexture.enableRandomWrite = true;
            _heightmapTexture.Create();

            _biomeMapTexture = new RenderTexture(terrainResolution, terrainResolution, 0, RenderTextureFormat.ARGB32);
            _biomeMapTexture.enableRandomWrite = true;
            _biomeMapTexture.Create();

            _moistureMapTexture = new RenderTexture(terrainResolution, terrainResolution, 0, RenderTextureFormat.RFloat);
            _moistureMapTexture.enableRandomWrite = true;
            _moistureMapTexture.Create();

            _temperatureMapTexture = new RenderTexture(terrainResolution, terrainResolution, 0, RenderTextureFormat.RFloat);
            _temperatureMapTexture.enableRandomWrite = true;
            _temperatureMapTexture.Create();

            // Initialize readback queue
            _pendingReadbacks = new Queue<AsyncGPUReadbackRequest>();

            Debug.Log($"[GPUProceduralGeneration] Initialized GPU resources: {terrainResolution}x{terrainResolution} terrain, {maxEcosystemObjects} ecosystem objects");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Generates high-resolution terrain heightmaps using GPU compute shaders for massive performance
        /// </summary>
        /// <param name="parameters">Terrain generation settings (noise scale, elevation, world offset)</param>
        /// <param name="onComplete">Callback invoked when terrain generation completes (optional)</param>
        public void GenerateTerrainGPU(TerrainGenerationParams parameters, System.Action<NativeArray<float>> onComplete = null)
        {
            // Check performance budget before starting generation
            float timeSinceLastGeneration = Time.realtimeSinceStartup - _lastGenerationTime;
            if (timeSinceLastGeneration < (maxGenerationBudgetMs / 1000f))
            {
                Debug.LogWarning($"[GPUProceduralGeneration] Generation throttled due to performance budget ({maxGenerationBudgetMs}ms)");
                return;
            }

            _lastGenerationTime = Time.realtimeSinceStartup;
            _onTerrainGenerated = onComplete;

            // Set compute shader parameters (use provided values or defaults)
            float actualNoiseScale = parameters.noiseScale > 0 ? parameters.noiseScale : noiseScale;
            float actualElevationMultiplier = parameters.elevationMultiplier > 0 ? parameters.elevationMultiplier : elevationMultiplier;

            terrainGenerationShader.SetFloat("_NoiseScale", actualNoiseScale);
            terrainGenerationShader.SetFloat("_ElevationMultiplier", actualElevationMultiplier);
            terrainGenerationShader.SetVector("_WorldOffset", new Vector4(parameters.worldOffset.x, parameters.worldOffset.y, 0, 0));
            terrainGenerationShader.SetInt("_TerrainResolution", terrainResolution);

            // Bind GPU resources
            terrainGenerationShader.SetTexture(0, "_HeightmapTexture", _heightmapTexture);
            terrainGenerationShader.SetBuffer(0, "_HeightmapBuffer", _heightmapBuffer);

            // Dispatch GPU compute threads
            int threadGroups = Mathf.CeilToInt((float)terrainResolution / computeThreadGroupSize);
            terrainGenerationShader.Dispatch(0, threadGroups, threadGroups, 1);

            // Async readback for non-blocking operation
            if (useAsyncGPUReadback && onComplete != null)
            {
                var request = AsyncGPUReadback.Request(_heightmapBuffer);
                _pendingReadbacks.Enqueue(request);
            }
        }

        /// <summary>
        /// Generates massive ecosystem with 100k+ objects using GPU parallel processing
        /// Distributes vegetation, resources, and environmental features across the terrain
        /// </summary>
        /// <param name="parameters">Ecosystem configuration (vegetation density, resource distribution, water influence)</param>
        /// <param name="onComplete">Callback invoked with generated ecosystem objects array</param>
        public void GenerateEcosystemGPU(EcosystemGenerationParams parameters, System.Action<GPUEcosystemObject[]> onComplete = null)
        {
            // Initialize ecosystem objects array
            _gpuEcosystemObjects = new GPUEcosystemObject[maxEcosystemObjects];

            // Set ecosystem generation parameters
            ecosystemGenerationShader.SetFloat("_VegetationDensity", parameters.vegetationDensity);
            ecosystemGenerationShader.SetFloat("_ResourceDensity", parameters.resourceDensity);
            ecosystemGenerationShader.SetFloat("_WaterInfluence", parameters.waterInfluence);
            ecosystemGenerationShader.SetInt("_MaxObjects", maxEcosystemObjects);
            ecosystemGenerationShader.SetVector("_GenerationBounds", parameters.generationBounds);

            // Bind GPU resources
            ecosystemGenerationShader.SetTexture(0, "_HeightmapTexture", _heightmapTexture);
            ecosystemGenerationShader.SetTexture(0, "_BiomeMapTexture", _biomeMapTexture);
            ecosystemGenerationShader.SetTexture(0, "_MoistureMapTexture", _moistureMapTexture);
            ecosystemGenerationShader.SetBuffer(0, "_EcosystemObjectsBuffer", _ecosystemObjectsBuffer);

            // Dispatch massive parallel generation
            int threadGroups = Mathf.CeilToInt((float)maxEcosystemObjects / computeThreadGroupSize);
            ecosystemGenerationShader.Dispatch(0, threadGroups, 1, 1);

            // Process results
            ProcessEcosystemResults(onComplete);
        }

        /// <summary>
        /// Generates realistic biome distribution using advanced multi-layer noise algorithms on GPU
        /// Creates temperature, moisture, and elevation-based biome classification
        /// </summary>
        /// <param name="parameters">Biome generation settings (temperature/moisture noise scales, elevation influence)</param>
        public void GenerateBiomeDistributionGPU(BiomeGenerationParams parameters)
        {
            // Multi-layer noise for realistic biome distribution
            biomeDistributionShader.SetFloat("_TemperatureNoiseScale", parameters.temperatureNoiseScale);
            biomeDistributionShader.SetFloat("_MoistureNoiseScale", parameters.moistureNoiseScale);
            biomeDistributionShader.SetFloat("_ElevationInfluence", parameters.elevationInfluence);
            biomeDistributionShader.SetVector("_BiomeSeeds", parameters.biomeSeeds);
            biomeDistributionShader.SetInt("_BiomeChunkSize", biomeChunkSize); // Use configured chunk size for processing

            // Bind all texture resources
            biomeDistributionShader.SetTexture(0, "_HeightmapTexture", _heightmapTexture);
            biomeDistributionShader.SetTexture(0, "_BiomeMapTexture", _biomeMapTexture);
            biomeDistributionShader.SetTexture(0, "_TemperatureMapTexture", _temperatureMapTexture);
            biomeDistributionShader.SetTexture(0, "_MoistureMapTexture", _moistureMapTexture);

            // Generate biome data using optimized chunk-based processing
            int threadGroups = Mathf.CeilToInt((float)terrainResolution / biomeChunkSize);
            biomeDistributionShader.Dispatch(0, threadGroups, threadGroups, 1);
        }

        /// <summary>
        /// GPU-based intelligent creature spawning with advanced ecosystem suitability analysis
        /// Analyzes biome compatibility, resource availability, and territorial requirements
        /// </summary>
        /// <param name="parameters">Creature spawn configuration (density, biome compatibility, resource requirements)</param>
        /// <param name="onComplete">Callback invoked with calculated spawn points and suitability data</param>
        public void GenerateCreatureSpawnsGPU(CreatureSpawnParams parameters, System.Action<GPUCreatureSpawnPoint[]> onComplete = null)
        {
            // Advanced creature spawning based on ecosystem health, resources, and biome compatibility
            creatureSpawningShader.SetFloat("_SpawnDensity", parameters.spawnDensity);
            creatureSpawningShader.SetFloat("_BiomeCompatibility", parameters.biomeCompatibility);
            creatureSpawningShader.SetFloat("_ResourceRequirement", parameters.resourceRequirement);
            creatureSpawningShader.SetInt("_MaxSpawns", maxEcosystemObjects / 10);

            // Bind ecosystem analysis textures
            creatureSpawningShader.SetTexture(0, "_BiomeMapTexture", _biomeMapTexture);
            creatureSpawningShader.SetTexture(0, "_HeightmapTexture", _heightmapTexture);
            creatureSpawningShader.SetBuffer(0, "_EcosystemObjectsBuffer", _ecosystemObjectsBuffer);
            creatureSpawningShader.SetBuffer(0, "_CreatureSpawnPointsBuffer", _creatureSpawnPointsBuffer);

            // Execute GPU creature spawning analysis
            int threadGroups = Mathf.CeilToInt((float)(maxEcosystemObjects / 10) / computeThreadGroupSize);
            creatureSpawningShader.Dispatch(0, threadGroups, 1, 1);

            // Process spawn points
            ProcessCreatureSpawnResults(onComplete);
        }

        #endregion

        #region GPU Data Processing

        private void Update()
        {
            ProcessAsyncGPUReadbacks();
        }

        private void ProcessAsyncGPUReadbacks()
        {
            while (_pendingReadbacks.Count > 0)
            {
                var request = _pendingReadbacks.Peek();

                if (request.done)
                {
                    _pendingReadbacks.Dequeue();

                    if (!request.hasError)
                    {
                        var data = request.GetData<float>();
                        _onTerrainGenerated?.Invoke(data);
                    }
                }
                else
                {
                    break; // Wait for completion
                }
            }
        }

        private void ProcessEcosystemResults(System.Action<GPUEcosystemObject[]> onComplete)
        {
            if (useAsyncGPUReadback)
            {
                AsyncGPUReadback.Request(_ecosystemObjectsBuffer, (request) =>
                {
                    if (!request.hasError)
                    {
                        var data = request.GetData<GPUEcosystemObject>();
                        var results = data.ToArray();
                        onComplete?.Invoke(results);
                    }
                });
            }
            else
            {
                // Synchronous readback (blocking)
                _ecosystemObjectsBuffer.GetData(_gpuEcosystemObjects);
                onComplete?.Invoke(_gpuEcosystemObjects);
            }
        }

        private void ProcessCreatureSpawnResults(System.Action<GPUCreatureSpawnPoint[]> onComplete)
        {
            if (useAsyncGPUReadback)
            {
                AsyncGPUReadback.Request(_creatureSpawnPointsBuffer, (request) =>
                {
                    if (!request.hasError)
                    {
                        var data = request.GetData<GPUCreatureSpawnPoint>();
                        var results = data.ToArray();
                        onComplete?.Invoke(results);
                    }
                });
            }
            else
            {
                _gpuCreatureSpawns = new GPUCreatureSpawnPoint[maxEcosystemObjects / 10];
                _creatureSpawnPointsBuffer.GetData(_gpuCreatureSpawns);
                onComplete?.Invoke(_gpuCreatureSpawns);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets comprehensive GPU memory usage statistics for performance monitoring
        /// </summary>
        /// <returns>Detailed breakdown of GPU buffer and texture memory consumption</returns>
        public GPUMemoryStats GetGPUMemoryStats()
        {
            long totalMemory = 0;
            totalMemory += _heightmapBuffer?.count * sizeof(float) ?? 0;
            totalMemory += _biomeBuffer?.count * System.Runtime.InteropServices.Marshal.SizeOf<BiomeData>() ?? 0;
            totalMemory += _ecosystemObjectsBuffer?.count * System.Runtime.InteropServices.Marshal.SizeOf<GPUEcosystemObject>() ?? 0;

            // Add texture memory
            totalMemory += terrainResolution * terrainResolution * 4 * 4; // 4 textures, 4 bytes each

            return new GPUMemoryStats
            {
                TotalMemoryMB = totalMemory / (1024f * 1024f),
                BufferCount = 4,
                TextureCount = 4,
                ActiveReadbacks = _pendingReadbacks.Count
            };
        }

        /// <summary>
        /// Clears all GPU buffers and textures, resetting the system for new generation cycles
        /// </summary>
        public void ClearGPUData()
        {
            // Clear all compute buffers
            ClearComputeBuffer(_heightmapBuffer);
            ClearComputeBuffer(_biomeBuffer);
            ClearComputeBuffer(_ecosystemObjectsBuffer);
            ClearComputeBuffer(_creatureSpawnPointsBuffer);

            // Clear render textures
            Graphics.SetRenderTarget(_heightmapTexture);
            GL.Clear(true, true, Color.black);
            Graphics.SetRenderTarget(_biomeMapTexture);
            GL.Clear(true, true, Color.black);
            Graphics.SetRenderTarget(_moistureMapTexture);
            GL.Clear(true, true, Color.black);
            Graphics.SetRenderTarget(_temperatureMapTexture);
            GL.Clear(true, true, Color.black);
            Graphics.SetRenderTarget(null);
        }

        private void ClearComputeBuffer(ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                // Use compute shader to clear buffer efficiently
                var clearShader = Resources.Load<ComputeShader>("ClearBuffer");
                if (clearShader != null)
                {
                    clearShader.SetBuffer(0, "BufferToClear", buffer);
                    clearShader.Dispatch(0, Mathf.CeilToInt(buffer.count / 64f), 1, 1);
                }
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Dispose all GPU resources
            _heightmapBuffer?.Dispose();
            _biomeBuffer?.Dispose();
            _ecosystemObjectsBuffer?.Dispose();
            _creatureSpawnPointsBuffer?.Dispose();

            if (_heightmapTexture != null) _heightmapTexture.Release();
            if (_biomeMapTexture != null) _biomeMapTexture.Release();
            if (_moistureMapTexture != null) _moistureMapTexture.Release();
            if (_temperatureMapTexture != null) _temperatureMapTexture.Release();

            // Cancel pending readbacks
            while (_pendingReadbacks.Count > 0)
            {
                _pendingReadbacks.Dequeue();
            }
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public struct TerrainGenerationParams
    {
        public float noiseScale;
        public float elevationMultiplier;
        public Vector2 worldOffset;
        public int octaves;
        public float persistence;
        public float lacunarity;
    }

    [System.Serializable]
    public struct EcosystemGenerationParams
    {
        public float vegetationDensity;
        public float resourceDensity;
        public float waterInfluence;
        public Vector4 generationBounds;
        public int biomeFilter;
    }

    [System.Serializable]
    public struct BiomeGenerationParams
    {
        public float temperatureNoiseScale;
        public float moistureNoiseScale;
        public float elevationInfluence;
        public Vector4 biomeSeeds;
    }

    [System.Serializable]
    public struct CreatureSpawnParams
    {
        public float spawnDensity;
        public float biomeCompatibility;
        public float resourceRequirement;
        public int speciesFilter;
        public float territorySize;
    }

    public struct BiomeData
    {
        public int biomeType;
        public float temperature;
        public float moisture;
        public float elevation;
        public float fertility;
    }

    public struct GPUTerrainData
    {
        public float height;
        public Vector3 normal;
        public int textureIndex;
        public float moisture;
    }

    public struct GPUEcosystemObject
    {
        public Vector3 position;
        public int objectType;
        public float scale;
        public float health;
        public int biomeType;
        public float resourceValue;
    }

    public struct GPUCreatureSpawnPoint
    {
        public Vector3 position;
        public int speciesType;
        public float suitability;
        public float territoryRadius;
        public int groupSize;
        public float aggression;
    }

    public struct GPUMemoryStats
    {
        public float TotalMemoryMB;
        public int BufferCount;
        public int TextureCount;
        public int ActiveReadbacks;

        public override string ToString()
        {
            return $"GPU Memory: {TotalMemoryMB:F2}MB, Buffers: {BufferCount}, Textures: {TextureCount}, Readbacks: {ActiveReadbacks}";
        }
    }

    #endregion
}