using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Configuration;
using ProjectChimera.Core;

namespace Laboratory.Subsystems.Genetics
{
    /// <summary>
    /// ScriptableObject configuration for the Genetics Subsystem.
    /// Contains all settings for breeding mechanics, mutation rates, and trait definitions.
    /// Designed for easy designer workflow and runtime tweaking.
    /// </summary>
    [CreateAssetMenu(fileName = "GeneticsSubsystemConfig", menuName = "Project Chimera/Subsystems/Genetics Config", order = 1)]
    public class GeneticsSubsystemConfig : ScriptableObject
    {
        [Header("Core Configuration")]
        [SerializeField] private GeneticTraitLibrary defaultTraitLibrary;
        [SerializeField] private string[] defaultSpecies = new string[] { "DefaultSpecies", "BasicCreature" };
        [SerializeField] private float globalMutationRate = 0.02f;
        [SerializeField] private float extinctionThreshold = 0.1f;
        [SerializeField] private bool enableAdvancedInheritance = true;
        [SerializeField] private bool enableEnvironmentalEffects = true;

        [Header("Breeding Settings")]
        [SerializeField] private BreedingConfiguration breedingConfig = new();

        [Header("Mutation Settings")]
        [SerializeField] private MutationConfiguration mutationConfig = new();

        [Header("Performance Settings")]
        [SerializeField] private PerformanceConfiguration performanceConfig = new();

        [Header("Validation Settings")]
        [SerializeField] private ValidationConfiguration validationConfig = new();

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private bool enableBreedingPredictions = true;
        [SerializeField] private bool enableMutationTracking = true;

        // Public Properties
        public GeneticTraitLibrary DefaultTraitLibrary => defaultTraitLibrary;
        public string[] DefaultSpecies => defaultSpecies;
        public float GlobalMutationRate => globalMutationRate;
        public float ExtinctionThreshold => extinctionThreshold;
        public bool EnableAdvancedInheritance => enableAdvancedInheritance;
        public bool EnableEnvironmentalEffects => enableEnvironmentalEffects;
        public BreedingConfiguration BreedingConfig => breedingConfig;
        public MutationConfiguration MutationConfig => mutationConfig;
        public PerformanceConfiguration PerformanceConfig => performanceConfig;
        public ValidationConfiguration ValidationConfig => validationConfig;
        public bool EnableDebugLogging => enableDebugLogging;
        public bool EnableBreedingPredictions => enableBreedingPredictions;
        public bool EnableMutationTracking => enableMutationTracking;

        /// <summary>
        /// Validates the configuration on enable
        /// </summary>
        private void OnValidate()
        {
            // Clamp values to safe ranges
            globalMutationRate = Mathf.Clamp01(globalMutationRate);
            breedingConfig.ValidateSettings();
            mutationConfig.ValidateSettings();
            performanceConfig.ValidateSettings();
            validationConfig.ValidateSettings();
        }

        /// <summary>
        /// Creates a default configuration
        /// </summary>
        public static GeneticsSubsystemConfig CreateDefault()
        {
            var config = CreateInstance<GeneticsSubsystemConfig>();
            config.name = "DefaultGeneticsConfig";
            config.globalMutationRate = 0.02f;
            config.enableAdvancedInheritance = true;
            config.enableEnvironmentalEffects = true;
            config.breedingConfig = BreedingConfiguration.CreateDefault();
            config.mutationConfig = MutationConfiguration.CreateDefault();
            config.performanceConfig = PerformanceConfiguration.CreateDefault();
            config.validationConfig = ValidationConfiguration.CreateDefault();
            return config;
        }
    }

    /// <summary>
    /// Configuration for breeding mechanics
    /// </summary>
    [System.Serializable]
    public class BreedingConfiguration
    {
        [Header("Compatibility")]
        [SerializeField, Range(0f, 1f)] private float minimumCompatibility = 0.1f;
        [SerializeField, Range(0f, 1f)] private float optimalCompatibility = 0.7f;
        [SerializeField] private bool allowInbreeding = false;
        [SerializeField, Range(0f, 1f)] private float inbreedingPenalty = 0.3f;

        [Header("Generation Limits")]
        [SerializeField] private int maxGenerations = 50;
        [SerializeField] private bool enableGenerationBonuses = true;
        [SerializeField, Range(0f, 0.1f)] private float generationBonusPerLevel = 0.01f;

        [Header("Environmental Influence")]
        [SerializeField, Range(0f, 1f)] private float environmentalInfluence = 0.2f;
        [SerializeField] private bool enableSeasonalEffects = true;
        [SerializeField] private bool enableHabitatBonuses = true;

        [Header("Success Rates")]
        [SerializeField, Range(0f, 1f)] private float baseSuccessRate = 0.8f;
        [SerializeField, Range(0f, 1f)] private float criticalSuccessChance = 0.05f;
        [SerializeField, Range(0f, 1f)] private float criticalFailureChance = 0.02f;

        // Public Properties
        public float MinimumCompatibility => minimumCompatibility;
        public float OptimalCompatibility => optimalCompatibility;
        public bool AllowInbreeding => allowInbreeding;
        public float InbreedingPenalty => inbreedingPenalty;
        public int MaxGenerations => maxGenerations;
        public bool EnableGenerationBonuses => enableGenerationBonuses;
        public float GenerationBonusPerLevel => generationBonusPerLevel;
        public float EnvironmentalInfluence => environmentalInfluence;
        public bool EnableSeasonalEffects => enableSeasonalEffects;
        public bool EnableHabitatBonuses => enableHabitatBonuses;
        public float BaseSuccessRate => baseSuccessRate;
        public float CriticalSuccessChance => criticalSuccessChance;
        public float CriticalFailureChance => criticalFailureChance;

        public void ValidateSettings()
        {
            minimumCompatibility = Mathf.Clamp01(minimumCompatibility);
            optimalCompatibility = Mathf.Clamp01(optimalCompatibility);
            inbreedingPenalty = Mathf.Clamp01(inbreedingPenalty);
            maxGenerations = Mathf.Max(1, maxGenerations);
            generationBonusPerLevel = Mathf.Clamp(generationBonusPerLevel, 0f, 0.1f);
            environmentalInfluence = Mathf.Clamp01(environmentalInfluence);
            baseSuccessRate = Mathf.Clamp01(baseSuccessRate);
            criticalSuccessChance = Mathf.Clamp01(criticalSuccessChance);
            criticalFailureChance = Mathf.Clamp01(criticalFailureChance);
        }

        public static BreedingConfiguration CreateDefault()
        {
            return new BreedingConfiguration
            {
                minimumCompatibility = 0.1f,
                optimalCompatibility = 0.7f,
                allowInbreeding = false,
                inbreedingPenalty = 0.3f,
                maxGenerations = 50,
                enableGenerationBonuses = true,
                generationBonusPerLevel = 0.01f,
                environmentalInfluence = 0.2f,
                enableSeasonalEffects = true,
                enableHabitatBonuses = true,
                baseSuccessRate = 0.8f,
                criticalSuccessChance = 0.05f,
                criticalFailureChance = 0.02f
            };
        }
    }

    /// <summary>
    /// Configuration for mutation mechanics
    /// </summary>
    [System.Serializable]
    public class MutationConfiguration
    {
        [Header("Mutation Rates")]
        [SerializeField, Range(0f, 0.1f)] private float baseMutationRate = 0.02f;
        [SerializeField, Range(0f, 0.05f)] private float beneficialMutationRate = 0.01f;
        [SerializeField, Range(0f, 0.05f)] private float harmfulMutationRate = 0.005f;
        [SerializeField, Range(0f, 0.01f)] private float novelTraitRate = 0.001f;

        [Header("Mutation Severity")]
        [SerializeField, Range(0f, 1f)] private float minMutationSeverity = 0.05f;
        [SerializeField, Range(0f, 1f)] private float maxMutationSeverity = 0.3f;
        [SerializeField, Range(0f, 1f)] private float averageMutationSeverity = 0.15f;

        [Header("Environmental Factors")]
        [SerializeField] private bool enableEnvironmentalMutations = true;
        [SerializeField, Range(0f, 5f)] private float environmentalMutationMultiplier = 1.5f;
        [SerializeField] private bool enableRadiationEffects = false;
        [SerializeField] private bool enableChemicalEffects = false;

        [Header("Mutation Stability")]
        [SerializeField, Range(0f, 1f)] private float mutationStabilityBase = 0.7f;
        [SerializeField] private bool allowMutationReversal = true;
        [SerializeField, Range(0f, 0.1f)] private float reversalChance = 0.02f;

        // Public Properties
        public float BaseMutationRate => baseMutationRate;
        public float BeneficialMutationRate => beneficialMutationRate;
        public float HarmfulMutationRate => harmfulMutationRate;
        public float NovelTraitRate => novelTraitRate;
        public float MinMutationSeverity => minMutationSeverity;
        public float MaxMutationSeverity => maxMutationSeverity;
        public float AverageMutationSeverity => averageMutationSeverity;
        public bool EnableEnvironmentalMutations => enableEnvironmentalMutations;
        public float EnvironmentalMutationMultiplier => environmentalMutationMultiplier;
        public bool EnableRadiationEffects => enableRadiationEffects;
        public bool EnableChemicalEffects => enableChemicalEffects;
        public float MutationStabilityBase => mutationStabilityBase;
        public bool AllowMutationReversal => allowMutationReversal;
        public float ReversalChance => reversalChance;

        public void ValidateSettings()
        {
            baseMutationRate = Mathf.Clamp(baseMutationRate, 0f, 0.1f);
            beneficialMutationRate = Mathf.Clamp(beneficialMutationRate, 0f, 0.05f);
            harmfulMutationRate = Mathf.Clamp(harmfulMutationRate, 0f, 0.05f);
            novelTraitRate = Mathf.Clamp(novelTraitRate, 0f, 0.01f);
            minMutationSeverity = Mathf.Clamp01(minMutationSeverity);
            maxMutationSeverity = Mathf.Clamp01(maxMutationSeverity);
            averageMutationSeverity = Mathf.Clamp01(averageMutationSeverity);
            environmentalMutationMultiplier = Mathf.Max(0f, environmentalMutationMultiplier);
            mutationStabilityBase = Mathf.Clamp01(mutationStabilityBase);
            reversalChance = Mathf.Clamp(reversalChance, 0f, 0.1f);
        }

        public static MutationConfiguration CreateDefault()
        {
            return new MutationConfiguration
            {
                baseMutationRate = 0.02f,
                beneficialMutationRate = 0.01f,
                harmfulMutationRate = 0.005f,
                novelTraitRate = 0.001f,
                minMutationSeverity = 0.05f,
                maxMutationSeverity = 0.3f,
                averageMutationSeverity = 0.15f,
                enableEnvironmentalMutations = true,
                environmentalMutationMultiplier = 1.5f,
                enableRadiationEffects = false,
                enableChemicalEffects = false,
                mutationStabilityBase = 0.7f,
                allowMutationReversal = true,
                reversalChance = 0.02f
            };
        }
    }

    /// <summary>
    /// Configuration for performance optimization
    /// </summary>
    [System.Serializable]
    public class PerformanceConfiguration
    {
        [Header("Processing Limits")]
        [SerializeField] private int maxConcurrentBreedings = 10;
        [SerializeField] private int maxConcurrentMutations = 20;
        [SerializeField] private int maxActiveProfiles = 1000;
        [SerializeField] private int minimumViablePopulation = 10;

        [Header("Caching")]
        [SerializeField] private bool enableProfileCaching = true;
        [SerializeField] private int maxCachedProfiles = 500;
        [SerializeField] private float cacheTimeout = 300f; // 5 minutes

        [Header("Background Processing")]
        [SerializeField] private bool enableBackgroundBreeding = true;
        [SerializeField] private bool enableBackgroundMutations = true;
        [SerializeField] private int backgroundProcessingBatchSize = 5;

        [Header("Memory Management")]
        [SerializeField] private bool enableMemoryOptimization = true;
        [SerializeField] private float memoryCleanupInterval = GameConstants.MEMORY_CLEANUP_INTERVAL; // 1 minute
        [SerializeField] private int maxMemoryUsageMB = 100;

        // Public Properties
        public int MaxConcurrentBreedings => maxConcurrentBreedings;
        public int MaxConcurrentMutations => maxConcurrentMutations;
        public int MaxActiveProfiles => maxActiveProfiles;
        public int MinimumViablePopulation => minimumViablePopulation;
        public bool EnableProfileCaching => enableProfileCaching;
        public int MaxCachedProfiles => maxCachedProfiles;
        public float CacheTimeout => cacheTimeout;
        public bool EnableBackgroundBreeding => enableBackgroundBreeding;
        public bool EnableBackgroundMutations => enableBackgroundMutations;
        public int BackgroundProcessingBatchSize => backgroundProcessingBatchSize;
        public bool EnableMemoryOptimization => enableMemoryOptimization;
        public float MemoryCleanupInterval => memoryCleanupInterval;
        public int MaxMemoryUsageMB => maxMemoryUsageMB;

        public void ValidateSettings()
        {
            maxConcurrentBreedings = Mathf.Max(1, maxConcurrentBreedings);
            maxConcurrentMutations = Mathf.Max(1, maxConcurrentMutations);
            maxActiveProfiles = Mathf.Max(10, maxActiveProfiles);
            maxCachedProfiles = Mathf.Max(0, maxCachedProfiles);
            cacheTimeout = Mathf.Max(0f, cacheTimeout);
            backgroundProcessingBatchSize = Mathf.Max(1, backgroundProcessingBatchSize);
            memoryCleanupInterval = Mathf.Max(10f, memoryCleanupInterval);
            maxMemoryUsageMB = Mathf.Max(10, maxMemoryUsageMB);
        }

        public static PerformanceConfiguration CreateDefault()
        {
            return new PerformanceConfiguration
            {
                maxConcurrentBreedings = 10,
                maxConcurrentMutations = 20,
                maxActiveProfiles = 1000,
                minimumViablePopulation = 10,
                enableProfileCaching = true,
                maxCachedProfiles = 500,
                cacheTimeout = 300f,
                enableBackgroundBreeding = true,
                enableBackgroundMutations = true,
                backgroundProcessingBatchSize = 5,
                enableMemoryOptimization = true,
                memoryCleanupInterval = GameConstants.MEMORY_CLEANUP_INTERVAL,
                maxMemoryUsageMB = 100
            };
        }
    }

    /// <summary>
    /// Configuration for genetic validation
    /// </summary>
    [System.Serializable]
    public class ValidationConfiguration
    {
        [Header("Profile Validation")]
        [SerializeField] private bool enableProfileValidation = true;
        [SerializeField] private bool strictTraitCompatibility = false;
        [SerializeField] private bool allowMissingTraits = true;
        [SerializeField] private bool validateGeneticRanges = true;

        [Header("Breeding Validation")]
        [SerializeField] private bool validateBreedingCompatibility = true;
        [SerializeField] private bool requireMinimumTraits = true;
        [SerializeField] private int minimumRequiredTraits = 5;
        [SerializeField] private bool allowEmptyProfiles = false;

        [Header("Error Handling")]
        [SerializeField] private bool autoFixInvalidProfiles = true;
        [SerializeField] private bool logValidationErrors = true;
        [SerializeField] private bool throwOnCriticalErrors = false;

        // Public Properties
        public bool EnableProfileValidation => enableProfileValidation;
        public bool StrictTraitCompatibility => strictTraitCompatibility;
        public bool AllowMissingTraits => allowMissingTraits;
        public bool ValidateGeneticRanges => validateGeneticRanges;
        public bool ValidateBreedingCompatibility => validateBreedingCompatibility;
        public bool RequireMinimumTraits => requireMinimumTraits;
        public int MinimumRequiredTraits => minimumRequiredTraits;
        public bool AllowEmptyProfiles => allowEmptyProfiles;
        public bool AutoFixInvalidProfiles => autoFixInvalidProfiles;
        public bool LogValidationErrors => logValidationErrors;
        public bool ThrowOnCriticalErrors => throwOnCriticalErrors;

        public void ValidateSettings()
        {
            minimumRequiredTraits = Mathf.Max(0, minimumRequiredTraits);
        }

        public static ValidationConfiguration CreateDefault()
        {
            return new ValidationConfiguration
            {
                enableProfileValidation = true,
                strictTraitCompatibility = false,
                allowMissingTraits = true,
                validateGeneticRanges = true,
                validateBreedingCompatibility = true,
                requireMinimumTraits = true,
                minimumRequiredTraits = 5,
                allowEmptyProfiles = false,
                autoFixInvalidProfiles = true,
                logValidationErrors = true,
                throwOnCriticalErrors = false
            };
        }
    }
}