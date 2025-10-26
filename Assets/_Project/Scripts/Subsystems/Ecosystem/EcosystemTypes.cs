using Laboratory.Shared.Types;

namespace Laboratory.Subsystems.Ecosystem
{
    /// <summary>
    /// Weather types for ecosystem simulation
    /// </summary>
    public enum WeatherType
    {
        Sunny,
        Cloudy,
        Rainy,
        Stormy,
        Snowy,
        Foggy,
        Windy,
        Any // For event requirements
    }

    /// <summary>
    /// Seasons for ecosystem cycling
    /// </summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    /// <summary>
    /// Environmental event types that can affect ecosystems
    /// </summary>
    public enum EnvironmentalEventType
    {
        Drought,
        Flood,
        Wildfire,
        Disease,
        FoodAbundance,
        PredatorInvasion,
        ClimateShift,
        Earthquake,
        VolcanicActivity,
        Migration
    }

    /// <summary>
    /// Conservation status levels for species monitoring
    /// </summary>
    public enum ConservationStatus
    {
        Abundant,
        Stable,
        Threatened,
        Endangered,
        CriticallyEndangered,
        Extinct
    }


    /// <summary>
    /// Types of biome changes that can occur
    /// </summary>
    public enum BiomeChangeType
    {
        HealthChange,
        CapacityChange,
        TemperatureChange,
        HumidityChange,
        SpeciesIntroduction,
        SpeciesExtinction,
        TraitModification
    }

    /// <summary>
    /// Types of population changes
    /// </summary>
    public enum PopulationChangeType
    {
        Growth,
        Decline,
        Migration,
        Introduction,
        Extinction,
        Recovery,
        Stabilization
    }

    /// <summary>
    /// Types of weather changes
    /// </summary>
    public enum WeatherChangeType
    {
        TypeChange,
        IntensityChange,
        SeasonChange,
        TemperatureShift,
        PressureChange
    }
}