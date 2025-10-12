using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Subsystems.AssetPipeline
{
    /// <summary>
    /// Configuration ScriptableObject for the Asset Pipeline Subsystem.
    /// Controls procedural generation, LOD management, texture generation, and memory optimization.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetPipelineSubsystemConfig", menuName = "Project Chimera/Subsystems/Asset Pipeline Config")]
    public class AssetPipelineSubsystemConfig : ScriptableObject
    {
        [Header("Core Settings")]
        [Tooltip("Background processing interval in milliseconds")]
        [Range(100, 5000)]
        public int backgroundProcessingIntervalMs = 1000;

        [Tooltip("Maximum concurrent asset generations")]
        [Range(1, 10)]
        public int maxConcurrentGenerations = 3;

        [Tooltip("Enable debug logging for asset operations")]
        public bool enableDebugLogging = false;

        [Header("Memory Management")]
        [Tooltip("Memory threshold in MB before triggering optimization")]
        [Range(100f, 2000f)]
        public float memoryThresholdMB = 500f;

        [Tooltip("Asset cache timeout in minutes")]
        [Range(5f, 120f)]
        public float assetCacheTimeoutMinutes = 30f;

        [Tooltip("Minimum access count to retain asset in cache")]
        [Range(1, 10)]
        public int minAccessCountForRetention = 2;

        [Tooltip("Enable automatic garbage collection")]
        public bool enableAutoGarbageCollection = true;

        [Header("LOD System")]
        [Tooltip("LOD settings for distance-based quality")]
        public LODSettings lodSettings = new LODSettings();

        [Tooltip("LOD update frequency (frames)")]
        [Range(1, 60)]
        public int lodUpdateFrequency = 10;

        [Tooltip("Enable importance-based LOD")]
        public bool enableImportanceBasedLOD = true;

        [Tooltip("Player creature importance multiplier")]
        [Range(1f, 10f)]
        public float playerCreatureImportanceMultiplier = 3f;

        [Header("Procedural Generation")]
        [Tooltip("Procedural asset templates")]
        public List<ProceduralAssetTemplate> proceduralAssetTemplates = new List<ProceduralAssetTemplate>();

        [Tooltip("Default generation quality")]
        [Range(0.1f, 1f)]
        public float defaultGenerationQuality = 0.8f;

        [Tooltip("Enable trait-based variation")]
        public bool enableTraitBasedVariation = true;

        [Tooltip("Maximum morph targets per creature")]
        [Range(1, 20)]
        public int maxMorphTargetsPerCreature = 8;

        [Tooltip("Enable procedural animation generation")]
        public bool enableProceduralAnimations = false; // Disabled for now - complex feature

        [Header("Dynamic Textures")]
        [Tooltip("Texture generation settings")]
        public TextureGenerationSettings textureSettings = new TextureGenerationSettings();

        [Tooltip("Enable texture compression")]
        public bool enableTextureCompression = true;

        [Tooltip("Enable mipmap generation")]
        public bool enableMipmapGeneration = true;

        [Tooltip("Maximum texture cache size (count)")]
        [Range(50, 500)]
        public int maxTextureCacheSize = 200;

        [Tooltip("Texture quality based on distance")]
        public bool enableDistanceBasedTextureQuality = true;

        [Header("Performance Optimization")]
        [Tooltip("Enable mesh optimization")]
        public bool enableMeshOptimization = true;

        [Tooltip("Enable material batching")]
        public bool enableMaterialBatching = true;

        [Tooltip("Enable texture atlasing")]
        public bool enableTextureAtlasing = false; // Complex feature - disabled for now

        [Tooltip("Frame budget for asset operations (ms)")]
        [Range(1f, 16f)]
        public float frameBudgetMs = 5f;

        [Header("Streaming")]
        [Tooltip("Asset streaming settings")]
        public AssetStreamingSettings streamingSettings = new AssetStreamingSettings();

        [Tooltip("Enable predictive loading")]
        public bool enablePredictiveLoading = true;

        [Tooltip("Preload distance for breeding pairs")]
        [Range(5f, 50f)]
        public float breedingPreloadDistance = 15f;

        [Header("Quality Presets")]
        [Tooltip("Current quality preset")]
        public AssetQualityPreset qualityPreset = AssetQualityPreset.High;

        [Tooltip("Quality settings for different presets")]
        public List<QualityPresetSettings> qualityPresets = new List<QualityPresetSettings>
        {
            new QualityPresetSettings
            {
                preset = AssetQualityPreset.Low,
                meshComplexityMultiplier = 0.3f,
                textureResolutionMultiplier = 0.25f,
                maxActiveAssets = 20,
                enableDetailTextures = false,
                enableParticleEffects = false
            },
            new QualityPresetSettings
            {
                preset = AssetQualityPreset.Medium,
                meshComplexityMultiplier = 0.6f,
                textureResolutionMultiplier = 0.5f,
                maxActiveAssets = 50,
                enableDetailTextures = false,
                enableParticleEffects = true
            },
            new QualityPresetSettings
            {
                preset = AssetQualityPreset.High,
                meshComplexityMultiplier = 1f,
                textureResolutionMultiplier = 1f,
                maxActiveAssets = 100,
                enableDetailTextures = true,
                enableParticleEffects = true
            }
        };

        [Header("Debug Settings")]
        [Tooltip("Visualize LOD boundaries in scene view")]
        public bool visualizeLODBoundaries = false;

        [Tooltip("Show asset generation debug info")]
        public bool showGenerationDebugInfo = false;

        [Tooltip("Enable performance profiling")]
        public bool enablePerformanceProfiling = false;

        #region Validation

        private void OnValidate()
        {
            // Ensure reasonable values
            backgroundProcessingIntervalMs = Mathf.Max(100, backgroundProcessingIntervalMs);
            maxConcurrentGenerations = Mathf.Max(1, maxConcurrentGenerations);
            memoryThresholdMB = Mathf.Max(100f, memoryThresholdMB);
            assetCacheTimeoutMinutes = Mathf.Max(5f, assetCacheTimeoutMinutes);
            lodUpdateFrequency = Mathf.Max(1, lodUpdateFrequency);
            maxMorphTargetsPerCreature = Mathf.Max(1, maxMorphTargetsPerCreature);

            // Ensure LOD settings are valid
            if (lodSettings != null)
            {
                ValidateLODSettings();
            }

            // Ensure texture settings are valid
            if (textureSettings != null)
            {
                ValidateTextureSettings();
            }

            // Ensure streaming settings are valid
            if (streamingSettings != null)
            {
                ValidateStreamingSettings();
            }

            // Ensure quality presets have all required entries
            EnsureQualityPresets();
        }

        private void ValidateLODSettings()
        {
            lodSettings.highQualityDistance = Mathf.Max(1f, lodSettings.highQualityDistance);
            lodSettings.mediumQualityDistance = Mathf.Max(lodSettings.highQualityDistance + 1f, lodSettings.mediumQualityDistance);
            lodSettings.lowQualityDistance = Mathf.Max(lodSettings.mediumQualityDistance + 1f, lodSettings.lowQualityDistance);
            lodSettings.cullingDistance = Mathf.Max(lodSettings.lowQualityDistance + 1f, lodSettings.cullingDistance);

            lodSettings.maxHighQualityCreatures = Mathf.Max(1, lodSettings.maxHighQualityCreatures);
            lodSettings.maxMediumQualityCreatures = Mathf.Max(lodSettings.maxHighQualityCreatures, lodSettings.maxMediumQualityCreatures);
        }

        private void ValidateTextureSettings()
        {
            textureSettings.baseResolution.x = Mathf.Max(64, textureSettings.baseResolution.x);
            textureSettings.baseResolution.y = Mathf.Max(64, textureSettings.baseResolution.y);
            textureSettings.lowResolution.x = Mathf.Max(32, textureSettings.lowResolution.x);
            textureSettings.lowResolution.y = Mathf.Max(32, textureSettings.lowResolution.y);
            textureSettings.highResolution.x = Mathf.Max(textureSettings.baseResolution.x, textureSettings.highResolution.x);
            textureSettings.highResolution.y = Mathf.Max(textureSettings.baseResolution.y, textureSettings.highResolution.y);
        }

        private void ValidateStreamingSettings()
        {
            streamingSettings.maxConcurrentGenerations = Mathf.Max(1, streamingSettings.maxConcurrentGenerations);
            streamingSettings.textureStreamingPoolSize = Mathf.Max(10, streamingSettings.textureStreamingPoolSize);
            streamingSettings.memoryBudgetMB = Mathf.Max(50f, streamingSettings.memoryBudgetMB);
            streamingSettings.framesBetweenCleanup = Mathf.Max(60, streamingSettings.framesBetweenCleanup);
        }

        private void EnsureQualityPresets()
        {
            var requiredPresets = new[] { AssetQualityPreset.Low, AssetQualityPreset.Medium, AssetQualityPreset.High };

            foreach (var preset in requiredPresets)
            {
                if (!qualityPresets.Exists(q => q.preset == preset))
                {
                    qualityPresets.Add(CreateDefaultQualityPreset(preset));
                }
            }

            // Sort by quality level
            qualityPresets.Sort((a, b) => a.preset.CompareTo(b.preset));
        }

        private QualityPresetSettings CreateDefaultQualityPreset(AssetQualityPreset preset)
        {
            return preset switch
            {
                AssetQualityPreset.Low => new QualityPresetSettings
                {
                    preset = AssetQualityPreset.Low,
                    meshComplexityMultiplier = 0.3f,
                    textureResolutionMultiplier = 0.25f,
                    maxActiveAssets = 20,
                    enableDetailTextures = false,
                    enableParticleEffects = false
                },
                AssetQualityPreset.Medium => new QualityPresetSettings
                {
                    preset = AssetQualityPreset.Medium,
                    meshComplexityMultiplier = 0.6f,
                    textureResolutionMultiplier = 0.5f,
                    maxActiveAssets = 50,
                    enableDetailTextures = false,
                    enableParticleEffects = true
                },
                AssetQualityPreset.High => new QualityPresetSettings
                {
                    preset = AssetQualityPreset.High,
                    meshComplexityMultiplier = 1f,
                    textureResolutionMultiplier = 1f,
                    maxActiveAssets = 100,
                    enableDetailTextures = true,
                    enableParticleEffects = true
                },
                _ => new QualityPresetSettings()
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets quality settings for current preset
        /// </summary>
        public QualityPresetSettings GetCurrentQualitySettings()
        {
            return qualityPresets.Find(q => q.preset == qualityPreset) ?? qualityPresets[0];
        }

        /// <summary>
        /// Sets quality preset and applies settings
        /// </summary>
        public void SetQualityPreset(AssetQualityPreset preset)
        {
            qualityPreset = preset;
            ApplyQualitySettings();
        }

        /// <summary>
        /// Gets optimal texture resolution for distance
        /// </summary>
        public Vector2Int GetOptimalTextureResolution(float distance)
        {
            if (!enableDistanceBasedTextureQuality)
                return textureSettings.baseResolution;

            if (distance <= lodSettings.highQualityDistance)
                return textureSettings.highResolution;
            else if (distance <= lodSettings.mediumQualityDistance)
                return textureSettings.baseResolution;
            else
                return textureSettings.lowResolution;
        }

        /// <summary>
        /// Checks if a feature is enabled for current quality preset
        /// </summary>
        public bool IsFeatureEnabledForCurrentQuality(string featureName)
        {
            var currentSettings = GetCurrentQualitySettings();

            return featureName.ToLower() switch
            {
                "detail_textures" => currentSettings.enableDetailTextures,
                "particle_effects" => currentSettings.enableParticleEffects,
                "normal_maps" => currentSettings.enableNormalMaps,
                "shadows" => currentSettings.enableShadows,
                "reflections" => currentSettings.enableReflections,
                _ => true
            };
        }

        /// <summary>
        /// Gets mesh complexity multiplier for current quality
        /// </summary>
        public float GetMeshComplexityMultiplier()
        {
            return GetCurrentQualitySettings().meshComplexityMultiplier;
        }

        /// <summary>
        /// Gets texture resolution multiplier for current quality
        /// </summary>
        public float GetTextureResolutionMultiplier()
        {
            return GetCurrentQualitySettings().textureResolutionMultiplier;
        }

        /// <summary>
        /// Gets maximum active assets for current quality
        /// </summary>
        public int GetMaxActiveAssets()
        {
            return GetCurrentQualitySettings().maxActiveAssets;
        }

        /// <summary>
        /// Calculates memory budget for asset type
        /// </summary>
        public float GetMemoryBudgetForAssetType(AssetType assetType)
        {
            var totalBudget = streamingSettings.memoryBudgetMB;

            return assetType switch
            {
                AssetType.Meshes => totalBudget * 0.3f,
                AssetType.Textures => totalBudget * 0.5f,
                AssetType.Materials => totalBudget * 0.1f,
                AssetType.Animations => totalBudget * 0.1f,
                _ => totalBudget * 0.1f
            };
        }

        #endregion

        #region Private Methods

        private void ApplyQualitySettings()
        {
            var settings = GetCurrentQualitySettings();

            // Apply settings to subsystem components
            if (textureSettings != null)
            {
                var baseRes = textureSettings.baseResolution;
                textureSettings.baseResolution = new Vector2Int(
                    Mathf.RoundToInt(baseRes.x * settings.textureResolutionMultiplier),
                    Mathf.RoundToInt(baseRes.y * settings.textureResolutionMultiplier)
                );
            }

            if (enableDebugLogging)
            {
                Debug.Log($"[AssetPipelineConfig] Applied quality preset: {qualityPreset}");
            }
        }

        #endregion
    }

    #region Supporting Types

    [System.Serializable]
    public class QualityPresetSettings
    {
        public AssetQualityPreset preset;
        [Range(0.1f, 2f)]
        public float meshComplexityMultiplier = 1f;
        [Range(0.1f, 2f)]
        public float textureResolutionMultiplier = 1f;
        [Range(10, 200)]
        public int maxActiveAssets = 100;
        public bool enableDetailTextures = true;
        public bool enableParticleEffects = true;
        public bool enableNormalMaps = true;
        public bool enableShadows = true;
        public bool enableReflections = false;
    }

    public enum AssetQualityPreset
    {
        Low,
        Medium,
        High
    }

    public enum AssetType
    {
        Meshes,
        Textures,
        Materials,
        Animations,
        Audio
    }

    #endregion
}