using UnityEngine;
using Laboratory.Chimera.Core;

namespace Laboratory.Subsystems.Ecosystem
{
    /// <summary>
    /// ScriptableObject configuration for the Ecosystem Subsystem.
    /// Contains settings for biomes, populations, weather, and environmental events.
    /// Designed for easy designer workflow and runtime tweaking.
    /// </summary>
    [CreateAssetMenu(fileName = "EcosystemSubsystemConfig", menuName = "Project Chimera/Subsystems/Ecosystem Config", order = 2)]
    public class EcosystemSubsystemConfig : ScriptableObject
    {
        [Header("Default Biomes")]
        [SerializeField] private BiomeConfig[] defaultBiomes;

        [Header("Population Settings")]
        [SerializeField] private PopulationConfiguration populationConfig = new();

        [Header("Weather Settings")]
        [SerializeField] private WeatherConfiguration weatherConfig = new();

        [Header("Environmental Events")]
        [SerializeField] private EnvironmentalEventConfiguration eventConfig = new();

        [Header("Conservation Settings")]
        [SerializeField] private ConservationConfiguration conservationConfig = new();

        [Header("Performance Settings")]
        [SerializeField] private EcosystemPerformanceConfiguration performanceConfig = new();

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private bool enableEcosystemVisualization = true;
        [SerializeField] private bool enablePopulationGraphs = true;

        // Public Properties
        public BiomeConfig[] DefaultBiomes => defaultBiomes;
        public PopulationConfiguration PopulationConfig => populationConfig;
        public WeatherConfiguration WeatherConfig => weatherConfig;
        public EnvironmentalEventConfiguration EventConfig => eventConfig;
        public ConservationConfiguration ConservationConfig => conservationConfig;
        public EcosystemPerformanceConfiguration PerformanceConfig => performanceConfig;
        public bool EnableDebugLogging => enableDebugLogging;
        public bool EnableEcosystemVisualization => enableEcosystemVisualization;
        public bool EnablePopulationGraphs => enablePopulationGraphs;

        private void OnValidate()
        {
            populationConfig.ValidateSettings();
            weatherConfig.ValidateSettings();
            eventConfig.ValidateSettings();
            conservationConfig.ValidateSettings();
            performanceConfig.ValidateSettings();
        }

        public static EcosystemSubsystemConfig CreateDefault()
        {
            var config = CreateInstance<EcosystemSubsystemConfig>();
            config.name = "DefaultEcosystemConfig";
            config.populationConfig = PopulationConfiguration.CreateDefault();
            config.weatherConfig = WeatherConfiguration.CreateDefault();
            config.eventConfig = EnvironmentalEventConfiguration.CreateDefault();
            config.conservationConfig = ConservationConfiguration.CreateDefault();
            config.performanceConfig = EcosystemPerformanceConfiguration.CreateDefault();
            return config;
        }
    }

    /// <summary>
    /// Configuration for population dynamics
    /// </summary>
    [System.Serializable]
    public class PopulationConfiguration
    {
        [Header("Population Limits")]
        [SerializeField] private int maxPopulationPerSpecies = 1000;
        [SerializeField] private int minViablePopulation = 10;
        [SerializeField] private int extinctionThreshold = 5;

        [Header("Growth Rates")]
        [SerializeField, Range(0f, 2f)] private float baseGrowthRate = 1.1f;
        [SerializeField, Range(0f, 1f)] private float carryingCapacityEffect = 0.8f;
        [SerializeField, Range(0f, 0.1f)] private float naturalMortalityRate = 0.02f;

        [Header("Migration")]
        [SerializeField] private bool enableMigration = true;
        [SerializeField, Range(0f, 1f)] private float migrationTriggerThreshold = 0.3f;
        [SerializeField, Range(0f, 0.5f)] private float maxMigrationPercent = 0.2f;

        [Header("Environmental Response")]
        [SerializeField, Range(0f, 2f)] private float environmentalSensitivity = 1.0f;
        [SerializeField, Range(0f, 1f)] private float adaptationRate = 0.1f;
        [SerializeField] private bool enableSeasonalChanges = true;

        // Public Properties
        public int MaxPopulationPerSpecies => maxPopulationPerSpecies;
        public int MinViablePopulation => minViablePopulation;
        public int ExtinctionThreshold => extinctionThreshold;
        public float BaseGrowthRate => baseGrowthRate;
        public float CarryingCapacityEffect => carryingCapacityEffect;
        public float NaturalMortalityRate => naturalMortalityRate;
        public bool EnableMigration => enableMigration;
        public float MigrationTriggerThreshold => migrationTriggerThreshold;
        public float MaxMigrationPercent => maxMigrationPercent;
        public float EnvironmentalSensitivity => environmentalSensitivity;
        public float AdaptationRate => adaptationRate;
        public bool EnableSeasonalChanges => enableSeasonalChanges;

        public void ValidateSettings()
        {
            maxPopulationPerSpecies = Mathf.Max(100, maxPopulationPerSpecies);
            minViablePopulation = Mathf.Max(1, minViablePopulation);
            extinctionThreshold = Mathf.Max(1, extinctionThreshold);
            baseGrowthRate = Mathf.Clamp(baseGrowthRate, 0.5f, 3f);
            carryingCapacityEffect = Mathf.Clamp01(carryingCapacityEffect);
            naturalMortalityRate = Mathf.Clamp(naturalMortalityRate, 0f, 0.5f);
            migrationTriggerThreshold = Mathf.Clamp01(migrationTriggerThreshold);
            maxMigrationPercent = Mathf.Clamp(maxMigrationPercent, 0f, 1f);
            environmentalSensitivity = Mathf.Max(0f, environmentalSensitivity);
            adaptationRate = Mathf.Clamp01(adaptationRate);
        }

        public static PopulationConfiguration CreateDefault()
        {
            return new PopulationConfiguration
            {
                maxPopulationPerSpecies = 1000,
                minViablePopulation = 10,
                extinctionThreshold = 5,
                baseGrowthRate = 1.1f,
                carryingCapacityEffect = 0.8f,
                naturalMortalityRate = 0.02f,
                enableMigration = true,
                migrationTriggerThreshold = 0.3f,
                maxMigrationPercent = 0.2f,
                environmentalSensitivity = 1.0f,
                adaptationRate = 0.1f,
                enableSeasonalChanges = true
            };
        }
    }

    /// <summary>
    /// Configuration for weather systems
    /// </summary>
    [System.Serializable]
    public class WeatherConfiguration
    {
        [Header("Weather Patterns")]
        [SerializeField] private bool enableDynamicWeather = true;
        [SerializeField, Range(0.1f, 10f)] private float weatherChangeFrequency = 2f; // hours
        [SerializeField, Range(0f, 2f)] private float weatherIntensityVariation = 0.5f;

        [Header("Seasonal Cycles")]
        [SerializeField] private bool enableSeasons = true;
        [SerializeField, Range(1f, 100f)] private float seasonLengthDays = 30f;
        [SerializeField, Range(0f, 2f)] private float seasonalWeatherBias = 1.0f;

        [Header("Temperature Settings")]
        [SerializeField] private float baseTemperature = 22f; // Celsius
        [SerializeField] private float temperatureRange = 40f;
        [SerializeField] private float seasonalTemperatureShift = 15f;

        [Header("Precipitation")]
        [SerializeField, Range(0f, 100f)] private float baseHumidity = 50f;
        [SerializeField, Range(0f, 100f)] private float humidityRange = 80f;
        [SerializeField, Range(0f, 1f)] private float precipitationChance = 0.3f;

        // Public Properties
        public bool EnableDynamicWeather => enableDynamicWeather;
        public float WeatherChangeFrequency => weatherChangeFrequency;
        public float WeatherIntensityVariation => weatherIntensityVariation;
        public bool EnableSeasons => enableSeasons;
        public float SeasonLengthDays => seasonLengthDays;
        public float SeasonalWeatherBias => seasonalWeatherBias;
        public float BaseTemperature => baseTemperature;
        public float TemperatureRange => temperatureRange;
        public float SeasonalTemperatureShift => seasonalTemperatureShift;
        public float BaseHumidity => baseHumidity;
        public float HumidityRange => humidityRange;
        public float PrecipitationChance => precipitationChance;

        public void ValidateSettings()
        {
            weatherChangeFrequency = Mathf.Clamp(weatherChangeFrequency, 0.1f, 24f);
            weatherIntensityVariation = Mathf.Max(0f, weatherIntensityVariation);
            seasonLengthDays = Mathf.Clamp(seasonLengthDays, 1f, 365f);
            seasonalWeatherBias = Mathf.Max(0f, seasonalWeatherBias);
            baseTemperature = Mathf.Clamp(baseTemperature, -50f, 60f);
            temperatureRange = Mathf.Max(0f, temperatureRange);
            seasonalTemperatureShift = Mathf.Max(0f, seasonalTemperatureShift);
            baseHumidity = Mathf.Clamp(baseHumidity, 0f, 100f);
            humidityRange = Mathf.Clamp(humidityRange, 0f, 100f);
            precipitationChance = Mathf.Clamp01(precipitationChance);
        }

        public static WeatherConfiguration CreateDefault()
        {
            return new WeatherConfiguration
            {
                enableDynamicWeather = true,
                weatherChangeFrequency = 2f,
                weatherIntensityVariation = 0.5f,
                enableSeasons = true,
                seasonLengthDays = 30f,
                seasonalWeatherBias = 1.0f,
                baseTemperature = 22f,
                temperatureRange = 40f,
                seasonalTemperatureShift = 15f,
                baseHumidity = 50f,
                humidityRange = 80f,
                precipitationChance = 0.3f
            };
        }
    }

    /// <summary>
    /// Configuration for environmental events
    /// </summary>
    [System.Serializable]
    public class EnvironmentalEventConfiguration
    {
        [Header("Event Frequency")]
        [SerializeField] private bool enableEnvironmentalEvents = true;
        [SerializeField, Range(0.001f, 1f)] private float baseEventChance = 0.01f; // per hour
        [SerializeField, Range(0f, 5f)] private float weatherEventMultiplier = 2f;

        [Header("Event Types")]
        [SerializeField] private EnvironmentalEventSettings[] eventSettings;

        [Header("Event Duration")]
        [SerializeField, Range(0.1f, 168f)] private float minEventDuration = 2f; // hours
        [SerializeField, Range(1f, 720f)] private float maxEventDuration = 48f; // hours
        [SerializeField, Range(0f, 2f)] private float eventIntensityVariation = 0.3f;

        // Public Properties
        public bool EnableEnvironmentalEvents => enableEnvironmentalEvents;
        public float BaseEventChance => baseEventChance;
        public float WeatherEventMultiplier => weatherEventMultiplier;
        public EnvironmentalEventSettings[] EventSettings => eventSettings;
        public float MinEventDuration => minEventDuration;
        public float MaxEventDuration => maxEventDuration;
        public float EventIntensityVariation => eventIntensityVariation;

        public void ValidateSettings()
        {
            baseEventChance = Mathf.Clamp(baseEventChance, 0.0001f, 1f);
            weatherEventMultiplier = Mathf.Max(0f, weatherEventMultiplier);
            minEventDuration = Mathf.Clamp(minEventDuration, 0.1f, 168f);
            maxEventDuration = Mathf.Clamp(maxEventDuration, minEventDuration, 720f);
            eventIntensityVariation = Mathf.Max(0f, eventIntensityVariation);

            if (eventSettings != null)
            {
                foreach (var setting in eventSettings)
                {
                    setting.ValidateSettings();
                }
            }
        }

        public static EnvironmentalEventConfiguration CreateDefault()
        {
            return new EnvironmentalEventConfiguration
            {
                enableEnvironmentalEvents = true,
                baseEventChance = 0.01f,
                weatherEventMultiplier = 2f,
                minEventDuration = 2f,
                maxEventDuration = 48f,
                eventIntensityVariation = 0.3f,
                eventSettings = CreateDefaultEventSettings()
            };
        }

        private static EnvironmentalEventSettings[] CreateDefaultEventSettings()
        {
            return new EnvironmentalEventSettings[]
            {
                new EnvironmentalEventSettings
                {
                    eventType = EnvironmentalEventType.Drought,
                    relativeFrequency = 1f,
                    minSeverity = 0.3f,
                    maxSeverity = 0.9f,
                    requiredWeather = WeatherType.Sunny
                },
                new EnvironmentalEventSettings
                {
                    eventType = EnvironmentalEventType.Flood,
                    relativeFrequency = 0.7f,
                    minSeverity = 0.2f,
                    maxSeverity = 0.8f,
                    requiredWeather = WeatherType.Rainy
                },
                new EnvironmentalEventSettings
                {
                    eventType = EnvironmentalEventType.Wildfire,
                    relativeFrequency = 0.3f,
                    minSeverity = 0.4f,
                    maxSeverity = 1.0f,
                    requiredWeather = WeatherType.Sunny
                },
                new EnvironmentalEventSettings
                {
                    eventType = EnvironmentalEventType.Disease,
                    relativeFrequency = 0.5f,
                    minSeverity = 0.1f,
                    maxSeverity = 0.7f,
                    requiredWeather = WeatherType.Any
                },
                new EnvironmentalEventSettings
                {
                    eventType = EnvironmentalEventType.FoodAbundance,
                    relativeFrequency = 1.5f,
                    minSeverity = 0.2f,
                    maxSeverity = 0.8f,
                    requiredWeather = WeatherType.Any
                }
            };
        }
    }

    /// <summary>
    /// Settings for individual environmental event types
    /// </summary>
    [System.Serializable]
    public class EnvironmentalEventSettings
    {
        public EnvironmentalEventType eventType;
        [Range(0f, 5f)] public float relativeFrequency = 1f;
        [Range(0f, 1f)] public float minSeverity = 0.1f;
        [Range(0f, 1f)] public float maxSeverity = 1f;
        public WeatherType requiredWeather = WeatherType.Any;

        public void ValidateSettings()
        {
            relativeFrequency = Mathf.Max(0f, relativeFrequency);
            minSeverity = Mathf.Clamp01(minSeverity);
            maxSeverity = Mathf.Clamp(maxSeverity, minSeverity, 1f);
        }
    }

    /// <summary>
    /// Configuration for conservation tracking
    /// </summary>
    [System.Serializable]
    public class ConservationConfiguration
    {
        [Header("Conservation Thresholds")]
        [SerializeField] private int abundantThreshold = 500;
        [SerializeField] private int stableThreshold = 100;
        [SerializeField] private int threatenedThreshold = 50;
        [SerializeField] private int endangeredThreshold = 20;
        [SerializeField] private int criticallyEndangeredThreshold = 10;

        [Header("Conservation Actions")]
        [SerializeField] private bool enableAutomaticConservation = true;
        [SerializeField, Range(0f, 1f)] private float conservationEffectiveness = 0.7f;
        [SerializeField, Range(1f, 100f)] private float conservationResponseTime = 24f; // hours

        [Header("Tracking")]
        [SerializeField] private bool trackExtinctionEvents = true;
        [SerializeField] private bool trackRecoveryEvents = true;
        [SerializeField] private bool alertOnStatusChanges = true;

        // Public Properties
        public int AbundantThreshold => abundantThreshold;
        public int StableThreshold => stableThreshold;
        public int ThreatenedThreshold => threatenedThreshold;
        public int EndangeredThreshold => endangeredThreshold;
        public int CriticallyEndangeredThreshold => criticallyEndangeredThreshold;
        public bool EnableAutomaticConservation => enableAutomaticConservation;
        public float ConservationEffectiveness => conservationEffectiveness;
        public float ConservationResponseTime => conservationResponseTime;
        public bool TrackExtinctionEvents => trackExtinctionEvents;
        public bool TrackRecoveryEvents => trackRecoveryEvents;
        public bool AlertOnStatusChanges => alertOnStatusChanges;

        public void ValidateSettings()
        {
            abundantThreshold = Mathf.Max(100, abundantThreshold);
            stableThreshold = Mathf.Max(50, Mathf.Min(stableThreshold, abundantThreshold - 1));
            threatenedThreshold = Mathf.Max(20, Mathf.Min(threatenedThreshold, stableThreshold - 1));
            endangeredThreshold = Mathf.Max(10, Mathf.Min(endangeredThreshold, threatenedThreshold - 1));
            criticallyEndangeredThreshold = Mathf.Max(1, Mathf.Min(criticallyEndangeredThreshold, endangeredThreshold - 1));
            conservationEffectiveness = Mathf.Clamp01(conservationEffectiveness);
            conservationResponseTime = Mathf.Max(1f, conservationResponseTime);
        }

        public static ConservationConfiguration CreateDefault()
        {
            return new ConservationConfiguration
            {
                abundantThreshold = 500,
                stableThreshold = 100,
                threatenedThreshold = 50,
                endangeredThreshold = 20,
                criticallyEndangeredThreshold = 10,
                enableAutomaticConservation = true,
                conservationEffectiveness = 0.7f,
                conservationResponseTime = 24f,
                trackExtinctionEvents = true,
                trackRecoveryEvents = true,
                alertOnStatusChanges = true
            };
        }
    }

    /// <summary>
    /// Performance configuration for ecosystem systems
    /// </summary>
    [System.Serializable]
    public class EcosystemPerformanceConfiguration
    {
        [Header("Update Frequencies")]
        [SerializeField, Range(0.1f, 10f)] private float populationUpdateInterval = 1f; // seconds
        [SerializeField, Range(0.1f, 60f)] private float weatherUpdateInterval = 5f; // seconds
        [SerializeField, Range(1f, 3600f)] private float conservationUpdateInterval = 60f; // seconds

        [Header("Batch Processing")]
        [SerializeField] private int maxPopulationsPerFrame = 10;
        [SerializeField] private int maxBiomesPerFrame = 5;
        [SerializeField] private bool enableAsyncProcessing = true;

        [Header("Memory Management")]
        [SerializeField] private bool enableDataCaching = true;
        [SerializeField, Range(100, 10000)] private int maxCachedPopulations = 1000;
        [SerializeField, Range(10f, 600f)] private float cacheCleanupInterval = 300f; // seconds

        // Public Properties
        public float PopulationUpdateInterval => populationUpdateInterval;
        public float WeatherUpdateInterval => weatherUpdateInterval;
        public float ConservationUpdateInterval => conservationUpdateInterval;
        public int MaxPopulationsPerFrame => maxPopulationsPerFrame;
        public int MaxBiomesPerFrame => maxBiomesPerFrame;
        public bool EnableAsyncProcessing => enableAsyncProcessing;
        public bool EnableDataCaching => enableDataCaching;
        public int MaxCachedPopulations => maxCachedPopulations;
        public float CacheCleanupInterval => cacheCleanupInterval;

        public void ValidateSettings()
        {
            populationUpdateInterval = Mathf.Clamp(populationUpdateInterval, 0.1f, 10f);
            weatherUpdateInterval = Mathf.Clamp(weatherUpdateInterval, 0.1f, 60f);
            conservationUpdateInterval = Mathf.Clamp(conservationUpdateInterval, 1f, 3600f);
            maxPopulationsPerFrame = Mathf.Max(1, maxPopulationsPerFrame);
            maxBiomesPerFrame = Mathf.Max(1, maxBiomesPerFrame);
            maxCachedPopulations = Mathf.Clamp(maxCachedPopulations, 100, 10000);
            cacheCleanupInterval = Mathf.Clamp(cacheCleanupInterval, 10f, 3600f);
        }

        public static EcosystemPerformanceConfiguration CreateDefault()
        {
            return new EcosystemPerformanceConfiguration
            {
                populationUpdateInterval = 1f,
                weatherUpdateInterval = 5f,
                conservationUpdateInterval = 60f,
                maxPopulationsPerFrame = 10,
                maxBiomesPerFrame = 5,
                enableAsyncProcessing = true,
                enableDataCaching = true,
                maxCachedPopulations = 1000,
                cacheCleanupInterval = 300f
            };
        }
    }
}