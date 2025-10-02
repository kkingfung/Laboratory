using System;
using UnityEngine;

namespace Laboratory.Chimera.Genetics.Visualization
{
    /// <summary>
    /// Configuration ScriptableObject for the Genetic Photography System.
    /// Allows designers to tune photography mechanics, visualization settings, and quality parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "GeneticPhotographyConfig", menuName = "Chimera/Genetics/Photography Config")]
    public class GeneticPhotographyConfig : ScriptableObject
    {
        [Header("Photography Mode Settings")]
        [Tooltip("Maximum duration of photography sessions (seconds)")]
        [Range(60f, 1800f)]
        public float maxPhotographySessionDuration = 600f;

        [Tooltip("Enable genetic overlays in photography mode")]
        public bool enableGeneticOverlays = true;

        [Tooltip("Enable automatic capture of rare genetic combinations")]
        public bool enableAutomaticCapture = true;

        [Tooltip("Quality threshold for automatic capture")]
        [Range(0.5f, 1f)]
        public float autoCapturethreshold = 0.8f;

        [Header("Photo Quality Settings")]
        [Tooltip("Default photo resolution")]
        public Vector2Int photoResolution = new Vector2Int(1920, 1080);

        [Tooltip("Anti-aliasing samples for photos")]
        [Range(1, 8)]
        public int antiAliasingSamples = 4;

        [Tooltip("Genetic rarity threshold for rare captures")]
        [Range(0.5f, 0.95f)]
        public float rareGeneticThreshold = 0.75f;

        [Header("Visualization Effects")]
        [Tooltip("Enable genetic aura effects around creatures")]
        public bool enableGeneticAuras = true;

        [Tooltip("Enable trait highlighting effects")]
        public bool enableTraitHighlighting = true;

        [Tooltip("Base intensity for genetic auras")]
        [Range(0.1f, 2f)]
        public float baseAuraIntensity = 0.8f;

        [Tooltip("Trait highlight pulse speed")]
        [Range(0.5f, 5f)]
        public float highlightPulseSpeed = 2f;

        [Header("Camera Settings")]
        [Tooltip("Default field of view for genetic photography")]
        [Range(30f, 120f)]
        public float defaultFieldOfView = 60f;

        [Tooltip("Default camera distance from subject")]
        [Range(1f, 20f)]
        public float defaultCameraDistance = 5f;

        [Tooltip("Default camera height above subject")]
        [Range(0f, 10f)]
        public float defaultCameraHeight = 2f;

        [Tooltip("Auto-focus speed")]
        [Range(1f, 10f)]
        public float autoFocusSpeed = 3f;

        [Header("Background Options")]
        [Tooltip("Available background colors for photography")]
        public Color[] backgroundColors = new Color[]
        {
            new Color(0.1f, 0.1f, 0.15f, 1f), // Dark blue
            new Color(0.9f, 0.9f, 0.9f, 1f), // Light gray
            new Color(0f, 0f, 0f, 1f),        // Black
            new Color(1f, 1f, 1f, 1f),        // White
            new Color(0.2f, 0.3f, 0.1f, 1f)   // Dark green
        };

        [Header("Overlay Settings")]
        [Tooltip("Default overlay opacity")]
        [Range(0.1f, 1f)]
        public float defaultOverlayOpacity = 0.7f;

        [Tooltip("Maximum number of trait labels to display")]
        [Range(3, 15)]
        public int maxTraitLabels = 8;

        [Tooltip("Overlay animation speed")]
        [Range(0.5f, 5f)]
        public float overlayAnimationSpeed = 1.5f;

        [Tooltip("Enable overlay animations")]
        public bool enableOverlayAnimations = true;

        [Header("Trait Color Mapping")]
        public TraitColorConfig[] traitColors = new TraitColorConfig[]
        {
            new TraitColorConfig { traitName = "Strength", color = Color.red },
            new TraitColorConfig { traitName = "Speed", color = Color.yellow },
            new TraitColorConfig { traitName = "Intelligence", color = Color.blue },
            new TraitColorConfig { traitName = "Resilience", color = Color.green },
            new TraitColorConfig { traitName = "Charisma", color = Color.magenta },
            new TraitColorConfig { traitName = "Stealth", color = Color.gray },
            new TraitColorConfig { traitName = "Combat", color = new Color(1f, 0.5f, 0f, 1f) },
            new TraitColorConfig { traitName = "Healing", color = Color.cyan }
        };

        [Header("Rarity Color Mapping")]
        public RarityColorConfig[] rarityColors = new RarityColorConfig[]
        {
            new RarityColorConfig { rarityRange = new Vector2(0f, 0.2f), color = Color.gray, name = "Common" },
            new RarityColorConfig { rarityRange = new Vector2(0.2f, 0.4f), color = Color.green, name = "Uncommon" },
            new RarityColorConfig { rarityRange = new Vector2(0.4f, 0.6f), color = Color.blue, name = "Rare" },
            new RarityColorConfig { rarityRange = new Vector2(0.6f, 0.8f), color = Color.magenta, name = "Epic" },
            new RarityColorConfig { rarityRange = new Vector2(0.8f, 1f), color = Color.yellow, name = "Legendary" }
        };

        [Header("Filter Presets")]
        public FilterPreset[] filterPresets = new FilterPreset[]
        {
            new FilterPreset
            {
                name = "Natural",
                brightness = 0f,
                contrast = 0f,
                saturation = 0f,
                colorTint = Color.white,
                enableBloom = false,
                bloomIntensity = 0f
            },
            new FilterPreset
            {
                name = "Vibrant",
                brightness = 0.1f,
                contrast = 0.2f,
                saturation = 0.3f,
                colorTint = Color.white,
                enableBloom = true,
                bloomIntensity = 0.5f
            },
            new FilterPreset
            {
                name = "Dramatic",
                brightness = -0.1f,
                contrast = 0.4f,
                saturation = 0.1f,
                colorTint = new Color(1f, 0.9f, 0.8f, 1f),
                enableBloom = false,
                bloomIntensity = 0f
            },
            new FilterPreset
            {
                name = "Mystical",
                brightness = 0.05f,
                contrast = 0.1f,
                saturation = 0.2f,
                colorTint = new Color(0.8f, 0.9f, 1f, 1f),
                enableBloom = true,
                bloomIntensity = 0.8f
            }
        };

        [Header("Achievement Thresholds")]
        [Tooltip("Number of photos for photography enthusiast achievement")]
        [Range(10, 1000)]
        public int enthusiastPhotoThreshold = 50;

        [Tooltip("Number of rare captures for collector achievement")]
        [Range(5, 100)]
        public int collectorRareThreshold = 25;

        [Tooltip("Quality score for perfectionist achievement")]
        [Range(0.8f, 1f)]
        public float perfectionistQualityThreshold = 0.95f;

        [Tooltip("Number of different species for explorer achievement")]
        [Range(10, 200)]
        public int explorerSpeciesThreshold = 50;

        [Header("Storage Settings")]
        [Tooltip("Maximum photos stored per player")]
        [Range(100, 10000)]
        public int maxPhotosPerPlayer = 1000;

        [Tooltip("Enable automatic cleanup of old photos")]
        public bool enableAutomaticCleanup = true;

        [Tooltip("Days after which uncategorized photos are cleaned up")]
        [Range(7, 365)]
        public int cleanupDaysThreshold = 30;

        [Tooltip("Compression quality for stored photos")]
        [Range(0.1f, 1f)]
        public float storageCompressionQuality = 0.8f;

        [Header("Sharing Settings")]
        [Tooltip("Enable photo sharing between players")]
        public bool enablePhotoSharing = true;

        [Tooltip("Enable public photo galleries")]
        public bool enablePublicGalleries = true;

        [Tooltip("Enable photo contests and competitions")]
        public bool enablePhotoContests = true;

        [Tooltip("Voting period for photo contests (hours)")]
        [Range(24f, 168f)]
        public float contestVotingPeriod = 72f;

        [Header("Performance Settings")]
        [Tooltip("Maximum photos processed per frame")]
        [Range(1, 20)]
        public int maxPhotosPerFrame = 5;

        [Tooltip("Texture streaming for large photo collections")]
        public bool enableTextureStreaming = true;

        [Tooltip("Photo cache size (MB)")]
        [Range(50, 1000)]
        public int photoCacheSizeMB = 200;

        [Tooltip("Render quality for real-time previews")]
        [Range(0.25f, 1f)]
        public float previewRenderQuality = 0.5f;

        /// <summary>
        /// Gets the color associated with a specific trait
        /// </summary>
        public Color GetTraitColor(string traitName)
        {
            foreach (var config in traitColors)
            {
                if (config.traitName.Equals(traitName, StringComparison.OrdinalIgnoreCase))
                    return config.color;
            }
            return Color.white; // Default color
        }

        /// <summary>
        /// Gets the rarity value for a trait (simplified version)
        /// </summary>
        public float GetTraitRarity(string traitName)
        {
            // This is a simplified version - in a real implementation,
            // this would reference actual trait rarity data
            switch (traitName.ToLower())
            {
                case "strength": return 0.3f;
                case "speed": return 0.4f;
                case "intelligence": return 0.6f;
                case "resilience": return 0.2f;
                case "charisma": return 0.7f;
                case "stealth": return 0.8f;
                case "combat": return 0.5f;
                case "healing": return 0.9f;
                default: return 0.3f;
            }
        }

        /// <summary>
        /// Gets the color for a specific rarity level
        /// </summary>
        public Color GetRarityColor(float rarity)
        {
            foreach (var config in rarityColors)
            {
                if (rarity >= config.rarityRange.x && rarity <= config.rarityRange.y)
                    return config.color;
            }
            return Color.white; // Default color
        }

        /// <summary>
        /// Gets the name for a specific rarity level
        /// </summary>
        public string GetRarityName(float rarity)
        {
            foreach (var config in rarityColors)
            {
                if (rarity >= config.rarityRange.x && rarity <= config.rarityRange.y)
                    return config.name;
            }
            return "Unknown";
        }

        /// <summary>
        /// Gets a filter preset by name
        /// </summary>
        public FilterPreset GetFilterPreset(string presetName)
        {
            foreach (var preset in filterPresets)
            {
                if (preset.name.Equals(presetName, StringComparison.OrdinalIgnoreCase))
                    return preset;
            }
            return filterPresets[0]; // Return first preset as default
        }

        /// <summary>
        /// Determines if a photo quality meets achievement threshold
        /// </summary>
        public bool MeetsQualityThreshold(float photoQuality, AchievementType achievementType)
        {
            switch (achievementType)
            {
                case AchievementType.TechnicalExcellence:
                    return photoQuality >= perfectionistQualityThreshold;
                case AchievementType.RareCapture:
                    return photoQuality >= rareGeneticThreshold;
                default:
                    return photoQuality >= 0.5f;
            }
        }

        /// <summary>
        /// Calculates photo storage requirements
        /// </summary>
        public float CalculatePhotoStorageSize(Vector2Int resolution, float compressionQuality)
        {
            // Rough calculation in MB
            float uncompressed = (resolution.x * resolution.y * 3f) / (1024f * 1024f);
            return uncompressed * compressionQuality;
        }

        /// <summary>
        /// Determines if photo cleanup is needed
        /// </summary>
        public bool ShouldCleanupPhoto(DateTime photoDate, bool isCategorized)
        {
            if (!enableAutomaticCleanup) return false;
            if (isCategorized) return false;

            var daysSinceCapture = (DateTime.Now - photoDate).TotalDays;
            return daysSinceCapture > cleanupDaysThreshold;
        }

        /// <summary>
        /// Gets default camera settings for specific photo types
        /// </summary>
        public PhotoCaptureSettings GetDefaultCaptureSettings(PhotoType photoType)
        {
            var settings = new PhotoCaptureSettings
            {
                fieldOfView = defaultFieldOfView,
                cameraDistance = defaultCameraDistance,
                cameraHeight = defaultCameraHeight,
                cameraAngle = 0f,
                backgroundColor = backgroundColors[0],
                showTraitLabels = true,
                showRarityIndicators = true,
                showGeneticConnections = false,
                overlayOpacity = defaultOverlayOpacity,
                enableFilters = false,
                addWatermark = false,
                compressPhoto = true,
                compressionQuality = storageCompressionQuality
            };

            // Customize based on photo type
            switch (photoType)
            {
                case PhotoType.Portrait:
                    settings.cameraDistance = 3f;
                    settings.fieldOfView = 50f;
                    settings.showGeneticConnections = false;
                    break;

                case PhotoType.Action:
                    settings.cameraDistance = 8f;
                    settings.fieldOfView = 80f;
                    settings.showTraitLabels = false;
                    break;

                case PhotoType.Scientific:
                    settings.showTraitLabels = true;
                    settings.showRarityIndicators = true;
                    settings.showGeneticConnections = true;
                    settings.backgroundColor = Color.white;
                    break;

                case PhotoType.Artistic:
                    settings.enableFilters = true;
                    settings.filterSettings = GetFilterPreset("Dramatic").ToFilterSettings();
                    settings.showTraitLabels = false;
                    settings.showRarityIndicators = false;
                    break;
            }

            return settings;
        }

        void OnValidate()
        {
            // Ensure reasonable values
            maxPhotographySessionDuration = Mathf.Clamp(maxPhotographySessionDuration, 60f, 3600f);
            autoCapturethreshold = Mathf.Clamp01(autoCapturethreshold);
            rareGeneticThreshold = Mathf.Clamp(rareGeneticThreshold, 0.5f, 0.95f);

            // Validate resolution
            photoResolution.x = Mathf.Clamp(photoResolution.x, 256, 4096);
            photoResolution.y = Mathf.Clamp(photoResolution.y, 256, 4096);

            // Validate anti-aliasing
            antiAliasingSamples = Mathf.Clamp(antiAliasingSamples, 1, 8);

            // Validate camera settings
            defaultFieldOfView = Mathf.Clamp(defaultFieldOfView, 15f, 150f);
            defaultCameraDistance = Mathf.Clamp(defaultCameraDistance, 0.5f, 50f);
            defaultCameraHeight = Mathf.Clamp(defaultCameraHeight, -5f, 20f);

            // Validate overlay settings
            defaultOverlayOpacity = Mathf.Clamp01(defaultOverlayOpacity);
            maxTraitLabels = Mathf.Clamp(maxTraitLabels, 1, 20);

            // Validate achievement thresholds
            enthusiastPhotoThreshold = Mathf.Clamp(enthusiastPhotoThreshold, 1, 10000);
            collectorRareThreshold = Mathf.Clamp(collectorRareThreshold, 1, 1000);
            perfectionistQualityThreshold = Mathf.Clamp01(perfectionistQualityThreshold);
            explorerSpeciesThreshold = Mathf.Clamp(explorerSpeciesThreshold, 1, 1000);

            // Validate storage settings
            maxPhotosPerPlayer = Mathf.Clamp(maxPhotosPerPlayer, 10, 100000);
            cleanupDaysThreshold = Mathf.Clamp(cleanupDaysThreshold, 1, 1000);
            storageCompressionQuality = Mathf.Clamp01(storageCompressionQuality);

            // Validate performance settings
            maxPhotosPerFrame = Mathf.Clamp(maxPhotosPerFrame, 1, 50);
            photoCacheSizeMB = Mathf.Clamp(photoCacheSizeMB, 10, 5000);
            previewRenderQuality = Mathf.Clamp01(previewRenderQuality);

            // Validate contest settings
            contestVotingPeriod = Mathf.Clamp(contestVotingPeriod, 1f, 720f);

            // Validate color arrays
            if (backgroundColors.Length == 0)
            {
                backgroundColors = new Color[] { Color.black };
            }

            // Validate trait colors
            for (int i = 0; i < traitColors.Length; i++)
            {
                if (string.IsNullOrEmpty(traitColors[i].traitName))
                {
                    traitColors[i].traitName = $"Trait{i}";
                }
            }

            // Validate rarity colors
            for (int i = 0; i < rarityColors.Length; i++)
            {
                rarityColors[i].rarityRange.x = Mathf.Clamp01(rarityColors[i].rarityRange.x);
                rarityColors[i].rarityRange.y = Mathf.Clamp01(rarityColors[i].rarityRange.y);

                if (rarityColors[i].rarityRange.x > rarityColors[i].rarityRange.y)
                {
                    var temp = rarityColors[i].rarityRange.x;
                    rarityColors[i].rarityRange.x = rarityColors[i].rarityRange.y;
                    rarityColors[i].rarityRange.y = temp;
                }

                if (string.IsNullOrEmpty(rarityColors[i].name))
                {
                    rarityColors[i].name = $"Rarity{i}";
                }
            }

            // Validate filter presets
            for (int i = 0; i < filterPresets.Length; i++)
            {
                if (string.IsNullOrEmpty(filterPresets[i].name))
                {
                    filterPresets[i].name = $"Filter{i}";
                }

                filterPresets[i].brightness = Mathf.Clamp(filterPresets[i].brightness, -1f, 1f);
                filterPresets[i].contrast = Mathf.Clamp(filterPresets[i].contrast, -1f, 1f);
                filterPresets[i].saturation = Mathf.Clamp(filterPresets[i].saturation, -1f, 1f);
                filterPresets[i].bloomIntensity = Mathf.Clamp01(filterPresets[i].bloomIntensity);
            }
        }
    }

    #region Configuration Data Structures

    /// <summary>
    /// Configuration for trait colors
    /// </summary>
    [Serializable]
    public struct TraitColorConfig
    {
        [Tooltip("Name of the trait")]
        public string traitName;

        [Tooltip("Color associated with this trait")]
        public Color color;

        [Tooltip("Glow intensity for this trait")]
        [Range(0f, 2f)]
        public float glowIntensity;

        [Tooltip("Enable particle effects for this trait")]
        public bool enableParticles;
    }

    /// <summary>
    /// Configuration for rarity colors
    /// </summary>
    [Serializable]
    public struct RarityColorConfig
    {
        [Tooltip("Rarity range (0-1)")]
        public Vector2 rarityRange;

        [Tooltip("Color for this rarity level")]
        public Color color;

        [Tooltip("Display name for this rarity")]
        public string name;

        [Tooltip("Special effect intensity")]
        [Range(0f, 2f)]
        public float effectIntensity;

        [Tooltip("Enable screen edge glow")]
        public bool enableScreenGlow;
    }

    /// <summary>
    /// Filter preset configuration
    /// </summary>
    [Serializable]
    public struct FilterPreset
    {
        [Tooltip("Name of the filter preset")]
        public string name;

        [Tooltip("Brightness adjustment")]
        [Range(-1f, 1f)]
        public float brightness;

        [Tooltip("Contrast adjustment")]
        [Range(-1f, 1f)]
        public float contrast;

        [Tooltip("Saturation adjustment")]
        [Range(-1f, 1f)]
        public float saturation;

        [Tooltip("Color tint")]
        public Color colorTint;

        [Tooltip("Enable bloom effect")]
        public bool enableBloom;

        [Tooltip("Bloom intensity")]
        [Range(0f, 2f)]
        public float bloomIntensity;

        [Tooltip("Description of the filter effect")]
        [TextArea(2, 3)]
        public string description;

        /// <summary>
        /// Converts preset to FilterSettings structure
        /// </summary>
        public FilterSettings ToFilterSettings()
        {
            return new FilterSettings
            {
                brightness = this.brightness,
                contrast = this.contrast,
                saturation = this.saturation,
                colorTint = this.colorTint,
                enableBloom = this.enableBloom,
                bloomIntensity = this.bloomIntensity
            };
        }
    }

    #endregion

    #region Helper Enums

    public enum PhotoType
    {
        Portrait,
        Action,
        Scientific,
        Artistic,
        Group,
        Environmental
    }

    public enum AchievementType
    {
        FirstPhoto,
        Enthusiast,
        Collector,
        TechnicalExcellence,
        ArtisticVision,
        RareCapture,
        Explorer
    }

    #endregion
}