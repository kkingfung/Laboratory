using UnityEngine;
using Laboratory.Core.Equipment.Types;
using System.Collections.Generic;

namespace Laboratory.Chimera.Customization
{
    /// <summary>
    /// ScriptableObject configuration for Chimera Customization System.
    /// Designer-configurable settings for appearance generation, equipment visuals, and customization options.
    /// </summary>
    [CreateAssetMenu(fileName = "ChimeraCustomizationConfig", menuName = "Chimera/Customization Config")]
    public class ChimeraCustomizationConfig : ScriptableObject
    {
        [Header("🎭 General Settings")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private bool autoSaveCustomizations = true;
        [SerializeField] private float visualUpdateInterval = 0.5f;

        [Header("🧬 Genetic Appearance")]
        [SerializeField] private GeneticVisualsConfig geneticConfig;

        [Header("🎒 Equipment Visuals")]
        [SerializeField] private EquipmentVisualsConfig equipmentConfig;

        [Header("👗 Custom Outfits")]
        [SerializeField] private CustomOutfitConfig outfitConfig;

        [Header("🌈 Color Customization")]
        [SerializeField] private ColorCustomizationConfig colorConfig;

        [Header("⚡ Performance")]
        [SerializeField] private PerformanceConfig performanceConfig;

        #region Properties

        public bool EnableDebugLogging => enableDebugLogging;
        public bool AutoSaveCustomizations => autoSaveCustomizations;
        public float VisualUpdateInterval => visualUpdateInterval;
        public GeneticVisualsConfig GeneticConfig => geneticConfig;
        public EquipmentVisualsConfig EquipmentConfig => equipmentConfig;
        public CustomOutfitConfig OutfitConfig => outfitConfig;
        public ColorCustomizationConfig ColorConfig => colorConfig;
        public PerformanceConfig PerformanceConfig => performanceConfig;

        #endregion

        #region Asset Path Helpers

        public string GetEquipmentVisualPath(EquipmentType equipmentType, string itemId)
        {
            return equipmentConfig?.GetVisualPath(equipmentType, itemId) ??
                   $"Equipment/Chimera/{equipmentType}/{itemId}";
        }

        public string GetOutfitPiecePath(string category, string pieceId)
        {
            return outfitConfig?.GetPiecePath(category, pieceId) ??
                   $"Outfits/Chimera/{category}/{pieceId}";
        }

        public string GetPatternMaterialPath(string patternType)
        {
            return geneticConfig?.GetPatternPath(patternType) ??
                   $"Materials/Patterns/{patternType}";
        }

        public string GetEffectPath(string effectType, string effectName)
        {
            return $"Effects/{effectType}/{effectName}";
        }

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Clamp values to reasonable ranges
            visualUpdateInterval = Mathf.Clamp(visualUpdateInterval, 0.1f, 5.0f);

            if (performanceConfig != null)
            {
                performanceConfig.ValidateSettings();
            }
        }

        #endregion
    }

    #region Configuration Sub-Classes

    [System.Serializable]
    public class GeneticVisualsConfig
    {
        [Header("Body Scaling")]
        [SerializeField] private Vector2 bodySizeRange = new Vector2(0.7f, 1.3f);
        [SerializeField] private Vector2 limbScaleRange = new Vector2(0.8f, 1.2f);
        [SerializeField] private bool enableProportionalScaling = true;

        [Header("Color Generation")]
        [SerializeField] private ColorPalette[] availableColorPalettes;
        [SerializeField] private float colorVariationStrength = 0.3f;
        [SerializeField] private bool enableComplementaryColors = true;
        [SerializeField] private int maxColorsPerType = 3;

        [Header("Pattern Generation")]
        [SerializeField] private PatternTemplate[] availablePatterns;
        [SerializeField] private float patternComplexityMultiplier = 1.0f;
        [SerializeField] private bool enableMultiplePatterns = true;

        [Header("Magical Effects")]
        [SerializeField] private MagicalEffectTemplate[] magicalEffects;
        [SerializeField] private float magicalThreshold = 0.3f;
        [SerializeField] private bool enableElementalAffinities = true;

        #region Properties

        public Vector2 BodySizeRange => bodySizeRange;
        public Vector2 LimbScaleRange => limbScaleRange;
        public bool EnableProportionalScaling => enableProportionalScaling;
        public ColorPalette[] AvailableColorPalettes => availableColorPalettes;
        public float ColorVariationStrength => colorVariationStrength;
        public bool EnableComplementaryColors => enableComplementaryColors;
        public int MaxColorsPerType => maxColorsPerType;
        public PatternTemplate[] AvailablePatterns => availablePatterns;
        public float PatternComplexityMultiplier => patternComplexityMultiplier;
        public bool EnableMultiplePatterns => enableMultiplePatterns;
        public MagicalEffectTemplate[] MagicalEffects => magicalEffects;
        public float MagicalThreshold => magicalThreshold;
        public bool EnableElementalAffinities => enableElementalAffinities;

        #endregion

        public string GetPatternPath(string patternType)
        {
            var pattern = System.Array.Find(availablePatterns, p => p.PatternName == patternType);
            return pattern?.MaterialPath ?? $"Materials/Patterns/{patternType}";
        }

        public PatternTemplate GetPatternTemplate(string patternType)
        {
            return System.Array.Find(availablePatterns, p => p.PatternName == patternType);
        }

        public ColorPalette GetRandomColorPalette()
        {
            if (availableColorPalettes == null || availableColorPalettes.Length == 0)
                return new ColorPalette { Name = "Default", Colors = new Color[] { Color.white, Color.gray } };

            return availableColorPalettes[Random.Range(0, availableColorPalettes.Length)];
        }
    }

    [System.Serializable]
    public class EquipmentVisualsConfig
    {
        [Header("Equipment Paths")]
        [SerializeField] private string baseEquipmentPath = "Equipment/Chimera";
        [SerializeField] private EquipmentCategoryPath[] categoryPaths;

        [Header("Rarity Effects")]
        [SerializeField] private RarityEffectSettings[] rarityEffects;
        [SerializeField] private bool enableRarityGlow = true;
        [SerializeField] private bool enableRarityParticles = true;

        [Header("Stat Effects")]
        [SerializeField] private StatEffectSettings[] statEffects;
        [SerializeField] private bool enableStatVisualEffects = true;
        [SerializeField] private float statEffectThreshold = 10.0f;

        #region Properties

        public string BaseEquipmentPath => baseEquipmentPath;
        public EquipmentCategoryPath[] CategoryPaths => categoryPaths;
        public RarityEffectSettings[] RarityEffects => rarityEffects;
        public bool EnableRarityGlow => enableRarityGlow;
        public bool EnableRarityParticles => enableRarityParticles;
        public StatEffectSettings[] StatEffects => statEffects;
        public bool EnableStatVisualEffects => enableStatVisualEffects;
        public float StatEffectThreshold => statEffectThreshold;

        #endregion

        public string GetVisualPath(EquipmentType equipmentType, string itemId)
        {
            var categoryPath = System.Array.Find(categoryPaths, c => c.EquipmentType == equipmentType);
            string categoryFolder = categoryPath?.CustomPath ?? equipmentType.ToString();

            return $"{baseEquipmentPath}/{categoryFolder}/{itemId}";
        }

        public RarityEffectSettings GetRarityEffectSettings(Laboratory.Core.MonsterTown.EquipmentRarity rarity)
        {
            var convertedRarity = ConvertRarity(rarity);
            return System.Array.Find(rarityEffects, r => r.Rarity == convertedRarity);
        }

        public StatEffectSettings GetStatEffectSettings(Laboratory.Core.Equipment.Types.StatType statType)
        {
            return System.Array.Find(statEffects, s => s.StatType == statType);
        }

        private EquipmentRarity ConvertRarity(Laboratory.Core.MonsterTown.EquipmentRarity monsterTownRarity)
        {
            return monsterTownRarity switch
            {
                Laboratory.Core.MonsterTown.EquipmentRarity.Common => EquipmentRarity.Common,
                Laboratory.Core.MonsterTown.EquipmentRarity.Uncommon => EquipmentRarity.Uncommon,
                Laboratory.Core.MonsterTown.EquipmentRarity.Rare => EquipmentRarity.Rare,
                Laboratory.Core.MonsterTown.EquipmentRarity.Epic => EquipmentRarity.Epic,
                Laboratory.Core.MonsterTown.EquipmentRarity.Legendary => EquipmentRarity.Legendary,
                _ => EquipmentRarity.Common
            };
        }
    }

    [System.Serializable]
    public class CustomOutfitConfig
    {
        [Header("Outfit Paths")]
        [SerializeField] private string baseOutfitPath = "Outfits/Chimera";
        [SerializeField] private OutfitCategoryPath[] categoryPaths;

        [Header("Customization Options")]
        [SerializeField] private bool enableOutfitMixing = true;
        [SerializeField] private bool enableOutfitColoring = true;
        [SerializeField] private bool enableOutfitScaling = true;
        [SerializeField] private int maxOutfitPieces = 10;

        [Header("Default Outfits")]
        [SerializeField] private DefaultOutfitSet[] defaultOutfits;

        #region Properties

        public string BaseOutfitPath => baseOutfitPath;
        public OutfitCategoryPath[] CategoryPaths => categoryPaths;
        public bool EnableOutfitMixing => enableOutfitMixing;
        public bool EnableOutfitColoring => enableOutfitColoring;
        public bool EnableOutfitScaling => enableOutfitScaling;
        public int MaxOutfitPieces => maxOutfitPieces;
        public DefaultOutfitSet[] DefaultOutfits => defaultOutfits;

        #endregion

        public string GetPiecePath(string category, string pieceId)
        {
            var categoryPath = System.Array.Find(categoryPaths, c => c.Category == category);
            string categoryFolder = categoryPath?.CustomPath ?? category;

            return $"{baseOutfitPath}/{categoryFolder}/{pieceId}";
        }

        public DefaultOutfitSet GetDefaultOutfit(string outfitName)
        {
            return System.Array.Find(defaultOutfits, o => o.OutfitName == outfitName);
        }
    }

    [System.Serializable]
    public class ColorCustomizationConfig
    {
        [Header("Color Options")]
        [SerializeField] private ColorPreset[] colorPresets;
        [SerializeField] private bool enableCustomColors = true;
        [SerializeField] private bool enableGradients = false;

        [Header("Material Properties")]
        [SerializeField] private MaterialPropertyMapping[] materialMappings;
        [SerializeField] private bool enableMetallicCustomization = true;
        [SerializeField] private bool enableEmissionCustomization = true;

        #region Properties

        public ColorPreset[] ColorPresets => colorPresets;
        public bool EnableCustomColors => enableCustomColors;
        public bool EnableGradients => enableGradients;
        public MaterialPropertyMapping[] MaterialMappings => materialMappings;
        public bool EnableMetallicCustomization => enableMetallicCustomization;
        public bool EnableEmissionCustomization => enableEmissionCustomization;

        #endregion

        public MaterialPropertyMapping GetMaterialMapping(string targetName)
        {
            return System.Array.Find(materialMappings, m => m.TargetName == targetName);
        }
    }

    [System.Serializable]
    public class PerformanceConfig
    {
        [Header("Caching")]
        [SerializeField] private bool enableAssetCaching = true;
        [SerializeField] private int maxCachedAssets = 100;
        [SerializeField] private bool enableMaterialInstancing = true;

        [Header("LOD Settings")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float[] lodDistances = { 50f, 100f, 200f };
        [SerializeField] private int[] lodComplexityLevels = { 3, 2, 1 };

        [Header("Effect Limits")]
        [SerializeField] private int maxSimultaneousEffects = 5;
        [SerializeField] private int maxParticleEffects = 3;
        [SerializeField] private bool enableEffectPooling = true;

        #region Properties

        public bool EnableAssetCaching => enableAssetCaching;
        public int MaxCachedAssets => maxCachedAssets;
        public bool EnableMaterialInstancing => enableMaterialInstancing;
        public bool EnableLOD => enableLOD;
        public float[] LodDistances => lodDistances;
        public int[] LodComplexityLevels => lodComplexityLevels;
        public int MaxSimultaneousEffects => maxSimultaneousEffects;
        public int MaxParticleEffects => maxParticleEffects;
        public bool EnableEffectPooling => enableEffectPooling;

        #endregion

        public void ValidateSettings()
        {
            maxCachedAssets = Mathf.Clamp(maxCachedAssets, 10, 1000);
            maxSimultaneousEffects = Mathf.Clamp(maxSimultaneousEffects, 1, 20);
            maxParticleEffects = Mathf.Clamp(maxParticleEffects, 1, 10);

            if (lodDistances != null && lodComplexityLevels != null)
            {
                int minLength = Mathf.Min(lodDistances.Length, lodComplexityLevels.Length);
                if (lodDistances.Length != minLength)
                {
                    System.Array.Resize(ref lodDistances, minLength);
                }
                if (lodComplexityLevels.Length != minLength)
                {
                    System.Array.Resize(ref lodComplexityLevels, minLength);
                }
            }
        }
    }

    #endregion

    #region Supporting Data Structures

    [System.Serializable]
    public class ColorPalette
    {
        public string Name;
        public Color[] Colors;
        public string Description;
    }

    [System.Serializable]
    public class PatternTemplate
    {
        public string PatternName;
        public string MaterialPath;
        public Texture2D PatternTexture;
        public float DefaultIntensity = 0.5f;
        public Vector2 DefaultScale = Vector2.one;
        public bool SupportsColoring = true;
    }

    [System.Serializable]
    public class MagicalEffectTemplate
    {
        public string EffectName;
        public string EffectType; // "fire", "water", "earth", "air"
        public string PrefabPath;
        public Color DefaultColor = Color.white;
        public float DefaultIntensity = 0.5f;
        public bool RequiresParticleSystem = true;
    }

    [System.Serializable]
    public class EquipmentCategoryPath
    {
        public EquipmentType EquipmentType;
        public string CustomPath;
    }

    [System.Serializable]
    public class RarityEffectSettings
    {
        public EquipmentRarity Rarity;
        public Color GlowColor = Color.white;
        public float GlowIntensity = 0.5f;
        public string ParticleEffectPath;
        public bool EnableCustomGlow = true;
    }

    [System.Serializable]
    public class StatEffectSettings
    {
        public Laboratory.Core.Equipment.Types.StatType StatType;
        public string EffectPath;
        public Color EffectColor = Color.white;
        public float MinStatValue = 10.0f;
        public bool ScaleWithStatValue = true;
    }

    [System.Serializable]
    public class OutfitCategoryPath
    {
        public string Category;
        public string CustomPath;
    }

    [System.Serializable]
    public class DefaultOutfitSet
    {
        public string OutfitName;
        public string[] PieceIds;
        public string Description;
        public Sprite PreviewImage;
    }

    [System.Serializable]
    public class ColorPreset
    {
        public string PresetName;
        public Color PrimaryColor;
        public Color SecondaryColor;
        public Color AccentColor;
        public Sprite PreviewSwatch;
    }

    [System.Serializable]
    public class MaterialPropertyMapping
    {
        public string TargetName; // "primary", "secondary", "accent"
        public string MaterialProperty; // "_Color", "_EmissionColor", etc.
        public bool ApplyToAllMaterials = false;
    }

    #endregion
}