using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Subsystems.AssetPipeline
{
    #region Core Asset Events

    [Serializable]
    public class CreatureAssetGeneratedEvent
    {
        public string creatureId;
        public GeneratedAssetData assetData;
        public TimeSpan generationTime;
        public DateTime timestamp;
    }

    [Serializable]
    public class LODUpdateEvent
    {
        public string creatureId;
        public LODLevel previousLOD;
        public LODLevel newLOD;
        public float distance;
        public DateTime timestamp;
    }

    [Serializable]
    public class TextureGeneratedEvent
    {
        public string textureId;
        public TextureType textureType;
        public Vector2Int resolution;
        public float generationTime;
        public DateTime timestamp;
    }

    [Serializable]
    public class AssetOptimizationEvent
    {
        public string optimizationType;
        public int assetsOptimized;
        public float memoryFreed;
        public DateTime timestamp;
    }

    #endregion

    #region Asset Generation

    [Serializable]
    public class AssetGenerationRequest
    {
        public string requestId;
        public string creatureId;
        public Laboratory.Subsystems.Genetics.GeneticProfile geneticProfile;
        public AssetGenerationType requestType;
        public AssetGenerationPriority priority;
        public DateTime timestamp;
        public Dictionary<string, object> parameters = new();
    }

    [Serializable]
    public class GeneratedAssetData : IDisposable
    {
        public string assetId;
        public string creatureId;
        public Mesh[] meshVariants;
        public Material[] materialVariants;
        public Texture2D[] textureVariants;
        public AnimationClip[] animationClips;
        public AudioClip[] audioClips;
        public Vector3 boundingBoxSize;
        public float complexityScore;
        public DateTime creationTime;
        public Dictionary<string, object> metadata = new();

        public void Dispose()
        {
            // Dispose Unity objects safely
            if (meshVariants != null)
            {
                foreach (var mesh in meshVariants)
                {
                    if (mesh != null && Application.isPlaying)
                        UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            if (textureVariants != null)
            {
                foreach (var texture in textureVariants)
                {
                    if (texture != null && Application.isPlaying)
                        UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }
    }

    [Serializable]
    public class AssetCacheEntry
    {
        public GeneratedAssetData assetData;
        public DateTime creationTime;
        public DateTime lastAccessTime;
        public int accessCount;
        public bool IsExpired => DateTime.Now - lastAccessTime > TimeSpan.FromMinutes(30);
    }

    public enum AssetGenerationType
    {
        Complete,
        NewOffspring,
        Mutation,
        Preload,
        LODUpdate,
        TextureOnly
    }

    public enum AssetGenerationPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    #endregion

    #region LOD Management

    [Serializable]
    public class LODSettings
    {
        [Header("Distance Thresholds")]
        public float highQualityDistance = 10f;
        public float mediumQualityDistance = 25f;
        public float lowQualityDistance = 50f;
        public float cullingDistance = 100f;

        [Header("Quality Settings")]
        public LODQualitySettings highQuality = new LODQualitySettings
        {
            meshComplexityMultiplier = 1f,
            textureResolutionMultiplier = 1f,
            animationFPS = 60f,
            enableParticleEffects = true,
            enableSecondaryAnimations = true
        };

        public LODQualitySettings mediumQuality = new LODQualitySettings
        {
            meshComplexityMultiplier = 0.6f,
            textureResolutionMultiplier = 0.5f,
            animationFPS = 30f,
            enableParticleEffects = true,
            enableSecondaryAnimations = false
        };

        public LODQualitySettings lowQuality = new LODQualitySettings
        {
            meshComplexityMultiplier = 0.3f,
            textureResolutionMultiplier = 0.25f,
            animationFPS = 15f,
            enableParticleEffects = false,
            enableSecondaryAnimations = false
        };

        [Header("Performance")]
        public int maxHighQualityCreatures = 10;
        public int maxMediumQualityCreatures = 50;
        public float importanceThreshold = 0.5f;
    }

    [Serializable]
    public class LODQualitySettings
    {
        public float meshComplexityMultiplier = 1f;
        public float textureResolutionMultiplier = 1f;
        public float animationFPS = 60f;
        public bool enableParticleEffects = true;
        public bool enableSecondaryAnimations = true;
        public bool enableDetailTextures = true;
        public bool enableNormalMaps = true;
        public int shadowCastingLevel = 2; // 0=off, 1=on, 2=two-sided
    }

    [Serializable]
    public class CreatureLODData
    {
        public string creatureId;
        public LODLevel currentLOD;
        public float distance;
        public float importanceScore;
        public DateTime lastUpdate;
        public bool isVisible;
        public Camera[] renderingCameras;
    }

    public enum LODLevel
    {
        High,
        Medium,
        Low,
        Culled
    }

    #endregion

    #region Procedural Generation

    [Serializable]
    public class ProceduralAssetTemplate
    {
        public string templateName;
        public string speciesId;
        public Mesh baseMesh;
        public Material baseMaterial;
        public List<MorphTarget> morphTargets = new();
        public List<ColorVariant> colorVariants = new();
        public List<PatternVariant> patternVariants = new();
        public List<SizeVariant> sizeVariants = new();
        public ProceduralGenerationRules generationRules = new();
    }

    [Serializable]
    public class MorphTarget
    {
        public string traitName;
        public Vector3[] vertexOffsets;
        public float minInfluence = 0f;
        public float maxInfluence = 1f;
        public AnimationCurve influenceCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }

    [Serializable]
    public class ColorVariant
    {
        public string variantName;
        public Color baseColor = Color.white;
        public Color secondaryColor = Color.gray;
        public Color accentColor = Color.black;
        public float metallicValue = 0f;
        public float smoothnessValue = 0.5f;
        public List<string> associatedTraits = new();
    }

    [Serializable]
    public class PatternVariant
    {
        public string patternName;
        public Texture2D patternTexture;
        public Vector2 tiling = Vector2.one;
        public Vector2 offset = Vector2.zero;
        public float intensity = 1f;
        public BlendMode blendMode = BlendMode.Multiply;
        public List<string> requiredTraits = new();
    }

    [Serializable]
    public class SizeVariant
    {
        public string sizeName;
        public Vector3 scaleMultiplier = Vector3.one;
        public float massMultiplier = 1f;
        public bool affectsBoundingBox = true;
        public List<string> associatedTraits = new();
    }

    [Serializable]
    public class ProceduralGenerationRules
    {
        public float randomSeed = 0f;
        public int maxMorphTargets = 5;
        public int maxColorVariants = 3;
        public int maxPatternVariants = 2;
        public float traitInfluenceStrength = 1f;
        public bool enableRandomVariation = true;
        public bool preserveSymmetry = true;
        public float qualityThreshold = 0.5f;
    }

    public enum BlendMode
    {
        Multiply,
        Add,
        Overlay,
        Screen,
        SoftLight
    }

    #endregion

    #region Dynamic Textures

    [Serializable]
    public class TextureGenerationRequest
    {
        public string requestId;
        public Laboratory.Subsystems.Genetics.GeneticProfile geneticProfile;
        public TextureType textureType;
        public Vector2Int resolution = new Vector2Int(512, 512);
        public TextureFormat format = TextureFormat.RGBA32;
        public Dictionary<string, object> parameters = new();
    }

    [Serializable]
    public class TextureGenerationSettings
    {
        public Vector2Int baseResolution = new Vector2Int(1024, 1024);
        public Vector2Int lowResolution = new Vector2Int(256, 256);
        public Vector2Int highResolution = new Vector2Int(2048, 2048);
        public TextureFormat preferredFormat = TextureFormat.RGBA32;
        public FilterMode filterMode = FilterMode.Bilinear;
        public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        public bool generateMipmaps = true;
        public bool enableCompression = true;
    }

    public enum TextureType
    {
        Diffuse,
        Normal,
        Specular,
        Emission,
        Pattern,
        Displacement,
        Mask,
        Detail
    }

    #endregion

    #region Performance Metrics

    [Serializable]
    public class AssetPerformanceMetrics
    {
        public int totalAssetsGenerated;
        public int failedGenerations;
        public float averageGenerationTime; // milliseconds
        public float currentMemoryUsage; // MB
        public float peakMemoryUsage; // MB
        public int activeAssetCount;
        public int cachedAssetCount;
        public int queuedRequests;
        public int culledCreatures;
        public float averageLODUpdateTime;
        public Dictionary<LODLevel, int> lodDistribution = new();
    }

    [Serializable]
    public class AssetOptimizationMetrics
    {
        public int texturesOptimized;
        public int meshesOptimized;
        public int materialsOptimized;
        public float memoryFreed; // MB
        public float optimizationTime; // seconds
        public DateTime lastOptimization;
    }

    #endregion

    #region Service Interfaces

    /// <summary>
    /// Procedural asset generation service
    /// </summary>
    public interface IProceduralGenerationService
    {
        Task<bool> InitializeAsync();
        GeneratedAssetData GenerateCreatureAsset(Laboratory.Subsystems.Genetics.GeneticProfile geneticProfile, string creatureId);
        Mesh GenerateCreatureMesh(Laboratory.Subsystems.Genetics.GeneticProfile geneticProfile, LODLevel lodLevel = LODLevel.High);
        Material GenerateCreatureMaterial(Laboratory.Subsystems.Genetics.GeneticProfile geneticProfile);
        List<ProceduralAssetTemplate> GetAvailableTemplates();
        bool RegisterTemplate(ProceduralAssetTemplate template);
    }

    /// <summary>
    /// Level of Detail management service
    /// </summary>
    public interface ILODManagementService
    {
        Task<bool> InitializeAsync();
        void UpdateCreatureLOD(string creatureId, float distance, float importance = 1f);
        void UpdateAllLODs();
        LODLevel GetOptimalLODLevel(float distance, float importance);
        CreatureLODData GetCreatureLODData(string creatureId);
        void SetLODSettings(LODSettings settings);
        int GetCreatureCountByLOD(LODLevel lodLevel);
    }

    /// <summary>
    /// Dynamic texture generation service
    /// </summary>
    public interface IDynamicTextureService
    {
        Task<bool> InitializeAsync();
        Task<Texture2D> GenerateTextureAsync(Laboratory.Subsystems.Genetics.GeneticProfile geneticProfile, TextureType textureType);
        Texture2D GeneratePatternTexture(Laboratory.Subsystems.Genetics.GeneticProfile geneticProfile);
        Texture2D GenerateColorTexture(Laboratory.Subsystems.Genetics.GeneticProfile geneticProfile);
        void UpdateTextureStreaming();
        bool CacheTexture(string textureId, Texture2D texture);
        Texture2D GetCachedTexture(string textureId);
    }

    /// <summary>
    /// Asset optimization and memory management service
    /// </summary>
    public interface IAssetOptimizationService
    {
        Task<bool> InitializeAsync();
        void OptimizeMemoryUsage();
        void PerformBackgroundOptimization();
        AssetOptimizationMetrics GetOptimizationMetrics();
        void CompressTextures(List<Texture2D> textures);
        void OptimizeMeshes(List<Mesh> meshes);
        void UnloadUnusedAssets();
    }

    #endregion

    #region Utility Structures

    [Serializable]
    public class GeneticTraitInfluence
    {
        public string traitName;
        public float influence; // 0.0 to 1.0
        public InfluenceType influenceType;
        public AnimationCurve influenceCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }

    public enum InfluenceType
    {
        Additive,
        Multiplicative,
        Override,
        Blend
    }

    [Serializable]
    public class AssetVariant
    {
        public string variantName;
        public int variantIndex;
        public float probability = 1f;
        public List<string> requiredTraits = new();
        public List<string> incompatibleTraits = new();
        public Dictionary<string, float> traitThresholds = new();
    }

    [Serializable]
    public class AssetStreamingSettings
    {
        public int maxConcurrentGenerations = 3;
        public int textureStreamingPoolSize = 100;
        public float memoryBudgetMB = 500f;
        public bool enableAsyncGeneration = true;
        public bool enableGarbageCollection = true;
        public int framesBetweenCleanup = 300;
    }

    #endregion
}