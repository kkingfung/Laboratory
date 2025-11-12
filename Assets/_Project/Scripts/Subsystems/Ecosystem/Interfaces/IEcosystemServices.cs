using System.Collections.Generic;
using System.Threading.Tasks;
using Laboratory.Core.Enums;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.Shared.Types;

namespace Laboratory.Subsystems.Ecosystem
{
    /// <summary>
    /// Core biome management service interface
    /// </summary>
    public interface IBiomeService
    {
        /// <summary>
        /// Creates a new biome from configuration
        /// </summary>
        Task<BiomeData> CreateBiomeAsync(BiomeConfig config);

        /// <summary>
        /// Gets environmental factors for a biome
        /// </summary>
        EnvironmentalFactors GetEnvironmentalFactors(BiomeData biome, WeatherData weather);

        /// <summary>
        /// Updates biome state based on populations and weather
        /// </summary>
        void UpdateBiomes(float deltaTime, WeatherData weather, List<PopulationData> populations);

        /// <summary>
        /// Event fired when biome properties change
        /// </summary>
        event System.Action<BiomeChangedEvent> OnBiomeChanged;
    }

    /// <summary>
    /// Population management service interface
    /// </summary>
    public interface IPopulationService
    {
        /// <summary>
        /// Initializes populations for a biome
        /// </summary>
        Task<List<PopulationData>> InitializeBiomePopulationsAsync(BiomeData biome);

        /// <summary>
        /// Updates population dynamics
        /// </summary>
        void UpdatePopulations(float deltaTime, WeatherData weather);

        /// <summary>
        /// Triggers migration between biomes
        /// </summary>
        Task<bool> TriggerMigrationAsync(string speciesId, string fromBiome, string toBiome, float percentage);

        /// <summary>
        /// Introduces a new species to a biome
        /// </summary>
        Task<PopulationData> IntroduceSpeciesAsync(string speciesId, BiomeData biome, int initialCount);

        /// <summary>
        /// Applies an environmental event effect to a population
        /// </summary>
        void ApplyEventEffect(PopulationData population, PopulationEffect effect);

        /// <summary>
        /// Combines multiple population data sets
        /// </summary>
        PopulationData CombinePopulations(PopulationData[] populations);

        /// <summary>
        /// Event fired when population changes occur
        /// </summary>
        event System.Action<PopulationEvent> OnPopulationChanged;
    }

    /// <summary>
    /// Weather system service interface
    /// </summary>
    public interface IWeatherService
    {
        /// <summary>
        /// Updates weather simulation
        /// </summary>
        void UpdateWeather(float deltaTime);

        /// <summary>
        /// Forces a specific weather type
        /// </summary>
        void SetWeather(WeatherType type, float intensity, float duration);

        /// <summary>
        /// Gets current weather data
        /// </summary>
        WeatherData GetCurrentWeather();

        /// <summary>
        /// Predicts weather changes
        /// </summary>
        WeatherForecast GetWeatherForecast(float hoursAhead = 24f);

        /// <summary>
        /// Event fired when weather changes
        /// </summary>
        event System.Action<WeatherEvent> OnWeatherChanged;
    }

    /// <summary>
    /// Conservation tracking service interface
    /// </summary>
    public interface IConservationService
    {
        /// <summary>
        /// Updates conservation status for all populations
        /// </summary>
        void UpdateConservationStatus(float deltaTime, List<PopulationData> populations);

        /// <summary>
        /// Gets conservation status for all species
        /// </summary>
        Dictionary<string, ConservationStatus> GetAllConservationStatus();

        /// <summary>
        /// Gets conservation status for a specific species
        /// </summary>
        ConservationStatus GetConservationStatus(string speciesId);

        /// <summary>
        /// Triggers conservation action for endangered species
        /// </summary>
        Task<bool> TriggerConservationActionAsync(string speciesId);

        /// <summary>
        /// Event fired when conservation status changes
        /// </summary>
        event System.Action<ConservationEvent> OnConservationStatusChanged;
    }

    /// <summary>
    /// Environmental event system service interface
    /// </summary>
    public interface IEnvironmentalEventService
    {
        /// <summary>
        /// Updates environmental event system
        /// </summary>
        void UpdateEvents(float deltaTime, WeatherData weather, List<PopulationData> populations);

        /// <summary>
        /// Manually triggers an environmental event
        /// </summary>
        void TriggerEvent(EnvironmentalEventType eventType, string biomeId, float severity);

        /// <summary>
        /// Gets active environmental events
        /// </summary>
        List<EnvironmentalEvent> GetActiveEvents();

        /// <summary>
        /// Event fired when environmental events occur
        /// </summary>
        event System.Action<EnvironmentalEvent> OnEnvironmentalEvent;
    }

    #region Data Transfer Objects

    /// <summary>
    /// Biome configuration data
    /// </summary>
    [System.Serializable]
    public class BiomeConfig
    {
        public string biomeId;
        public string biomeName;
        public BiomeType biomeType;
        public float baseTemperature;
        public float baseHumidity;
        public float carryingCapacity;
        public string[] nativeSpecies;
        public BiomeTraits traits;
    }

    /// <summary>
    /// Runtime biome data
    /// </summary>
    public class BiomeData
    {
        public string biomeId;
        public string biomeName;
        public BiomeType biomeType;
        public float currentTemperature;
        public float currentHumidity;
        public float healthIndex;
        public float carryingCapacity;
        public float currentCapacityUsed;
        public List<string> presentSpecies;
        public BiomeTraits traits;
        public System.DateTime lastUpdated;
    }

    /// <summary>
    /// Biome-specific traits and characteristics
    /// </summary>
    [System.Serializable]
    public class BiomeTraits
    {
        public float foodAbundance = 1.0f;
        public float predatorPressure = 0.5f;
        public float migrationAccessibility = 1.0f;
        public float weatherStability = 1.0f;
        public Dictionary<string, float> traitModifiers = new();
    }

    /// <summary>
    /// Population data for a species in a biome
    /// </summary>
    public class PopulationData
    {
        public string populationId;
        public string speciesId;
        public string biomeId;
        public int currentPopulation;
        public int carryingCapacity;
        public float healthIndex;
        public float growthRate;
        public ConservationStatus conservationStatus;
        public PopulationTrends trends;
        public List<PopulationEvent> recentEvents;
        public System.DateTime lastUpdated;
    }

    /// <summary>
    /// Population trend data
    /// </summary>
    public class PopulationTrends
    {
        public float[] populationHistory; // Last 30 data points
        public float averageGrowthRate;
        public float volatility;
        public float seasonalVariation;
        public bool isIncreasing;
        public bool isStable;
    }

    /// <summary>
    /// Weather data
    /// </summary>
    public class WeatherData
    {
        public WeatherType weatherType;
        public float intensity;
        public float temperature;
        public float humidity;
        public float pressure;
        public float windSpeed;
        public UnityEngine.Vector3 windDirection;
        public Season currentSeason;
        public float seasonProgress; // 0-1 through the season
        public float remainingDuration; // Hours
        public System.DateTime timestamp;
        public System.DateTime startTime;
    }

    /// <summary>
    /// Weather forecast data
    /// </summary>
    public class WeatherForecast
    {
        public WeatherPrediction[] predictions;
        public float confidence;
        public string[] warnings;
    }

    /// <summary>
    /// Individual weather prediction
    /// </summary>
    public class WeatherPrediction
    {
        public float hoursFromNow;
        public WeatherType predictedWeather;
        public float expectedIntensity;
        public float confidence;
    }

    /// <summary>
    /// Effect of environmental event on population
    /// </summary>
    public class PopulationEffect
    {
        public float populationMultiplier = 1.0f;
        public float survivalModifier = 0f;
        public float reproductionModifier = 0f;
        public float migrationPressure = 0f;
        public float healthImpact = 0f;
        public float duration = 1.0f; // Hours
    }

    /// <summary>
    /// Environmental event data
    /// </summary>
    public class EnvironmentalEvent
    {
        public string eventId;
        public EnvironmentalEventType eventType;
        public string name;
        public string description;
        public string affectedBiomeId;
        public List<string> affectedBiomes;
        public float severity;
        public float intensity;
        public float duration;
        public float remainingTime;
        public List<string> affectedSpecies;
        public Dictionary<string, PopulationEffect> effects;
        public bool isActive;
        public System.DateTime startTime;
        public System.DateTime? endTime;
        public EventSeverity severityEnum;
    }

    #endregion

    #region Event Data Objects

    /// <summary>
    /// Event fired when biome properties change
    /// </summary>
    public class BiomeChangedEvent
    {
        public string biomeId;
        public BiomeData newBiomeData;
        public BiomeChangeType changeType;
        public string description;
        public System.DateTime timestamp;
    }

    /// <summary>
    /// Event fired when population changes occur
    /// </summary>
    public class PopulationEvent
    {
        public string populationId;
        public string speciesId;
        public string biomeId;
        public PopulationData newPopulationData;
        public PopulationChangeType changeType;
        public int previousPopulation;
        public int newPopulation;
        public string description;
        public System.DateTime timestamp;
    }

    /// <summary>
    /// Event fired when weather changes
    /// </summary>
    public class WeatherEvent
    {
        public WeatherData previousWeatherData;
        public WeatherData newWeatherData;
        public WeatherChangeType changeType;
        public List<string> affectedBiomes;
        public string description;
        public System.DateTime timestamp;
    }

    /// <summary>
    /// Event fired when conservation status changes
    /// </summary>
    public class ConservationEvent
    {
        public string speciesId;
        public string speciesName;
        public ConservationStatus previousStatus;
        public ConservationStatus newStatus;
        public int totalPopulation;
        public int currentPopulation;
        public float geneticDiversity;
        public List<string> affectedBiomes;
        public string description;
        public System.DateTime timestamp;
    }

    #endregion

    // Note: Enums moved to EcosystemTypes.cs for better compilation dependency management
}