using System;
using Laboratory.Chimera.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Chimera.Ecosystem.Data
{
    /// <summary>
    /// Core data structures for ecosystem evolution simulation
    /// </summary>

    [Serializable]
    public struct EcosystemState
    {
        public float Temperature;
        public float Humidity;
        public float Rainfall;
        public float SoilQuality;
        public float Biodiversity;
        public float Stability;
        public SeasonType CurrentSeason;
        public float SeasonProgress;
        public ClimateType ClimateZone;
        public DateTime LastUpdate;
    }

    [Serializable]
    public struct BiomeTransition
    {
        public BiomeType FromBiome;
        public BiomeType ToBiome;
        public float TransitionRate;
        public float RequiredTime;
        public float Progress;
        public List<string> RequiredConditions;
        public bool IsActive;
    }

    [Serializable]
    public struct ResourceFlow
    {
        public ResourceType Type;
        public float Availability;
        public float ConsumptionRate;
        public float RegenerationRate;
        public float SeasonalModifier;
        public Vector2 Location;
        public float Quality;
        public bool IsRenewable;
    }

    [Serializable]
    public struct SpeciesInteraction
    {
        public uint Species1Id;
        public uint Species2Id;
        public InteractionType Type;
        public float Strength;
        public float EffectOnSpecies1;
        public float EffectOnSpecies2;
        public Vector2 LocationInfluence;
        public bool IsActive;
        public float LastInteractionTime;
    }

    [Serializable]
    public struct MigrationPattern
    {
        public uint SpeciesId;
        public Vector2 SourceLocation;
        public Vector2 DestinationLocation;
        public SeasonType TriggerSeason;
        public float MigrationPressure;
        public float CompletionRate;
        public bool IsActive;
        public List<Vector2> MigrationPath;
    }

    [Serializable]
    public struct CatastrophicEvent
    {
        public CatastropheType Type;
        public Vector2 EpicenterLocation;
        public float Intensity;
        public float AffectedRadius;
        public float Duration;
        public float TimeRemaining;
        public Dictionary<string, float> Effects;
        public bool IsActive;
        public float RecoveryTime;
    }

    [Serializable]
    public struct EcosystemHealth
    {
        public float BiodiversityIndex;
        public float TrophicBalance;
        public float ResourceSustainability;
        public float PopulationStability;
        public float HabitatQuality;
        public float OverallHealthScore;
        public List<string> Threats;
        public List<string> Opportunities;
        public DateTime LastAssessment;
    }

    [Serializable]
    public struct ClimateData
    {
        public float GlobalTemperature;
        public float SeaLevel;
        public float AtmosphericCO2;
        public float OzoneLevel;
        public Vector2 TemperatureRange;
        public float ClimateStability;
        public List<WeatherPattern> ActivePatterns;
        public float ClimateChangeRate;
    }

    [Serializable]
    public struct WeatherPattern
    {
        public WeatherType Type;
        public Vector2 Location;
        public float Intensity;
        public float Duration;
        public Vector2 MovementDirection;
        public float Speed;
        public bool IsActive;
        public Dictionary<string, float> LocalEffects;
    }

    [Serializable]
    public struct EcosystemMetrics
    {
        public int TotalSpeciesCount;
        public int EndangeredSpeciesCount;
        public float AveragePopulationSize;
        public float PopulationStability;
        public float ExtinctionRate;
        public float SpeciationRate;
        public float CarryingCapacityUtilization;
        public float GeneticDiversity;
        public float EcosystemResilience;
        public Dictionary<BiomeType, float> BiomeDistribution;
        public Dictionary<TrophicLevel, int> TrophicDistribution;
    }

    [Serializable]
    public struct EcosystemConfiguration
    {
        public float TimeScale;
        public float SimulationSpeed;
        public bool EnableClimateChange;
        public bool EnableCatastrophes;
        public bool EnableMigration;
        public bool EnableEvolution;
        public float MaxPopulationDensity;
        public float ResourceRegenerationRate;
        public float MutationRate;
        public float EnvironmentalPressure;
    }

    [Serializable]
    public class EcosystemCache
    {
        public Dictionary<Vector2, EcosystemState> RegionalStates = new();
        public Dictionary<uint, List<SpeciesInteraction>> SpeciesInteractions = new();
        public Dictionary<BiomeType, List<ResourceFlow>> BiomeResources = new();
        public List<CatastrophicEvent> ActiveEvents = new();
        public List<MigrationPattern> ActiveMigrations = new();
        public DateTime LastCacheUpdate;
        public string CurrentCacheHash;
    }

    public enum SeasonType
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    public enum ClimateType
    {
        Tropical,
        Temperate,
        Arid,
        Polar,
        Mediterranean,
        Continental,
        Oceanic,
        Highland
    }


    public enum ResourceType
    {
        Water,
        Food,
        Shelter,
        Minerals,
        Energy,
        Territory,
        MatingPartners,
        Sunlight,
        Nutrients,
        Oxygen
    }

    public enum InteractionType
    {
        Predation,
        Competition,
        Mutualism,
        Commensalism,
        Parasitism,
        Amensalism,
        Neutralism,
        Cooperation,
        Territorial,
        Mating
    }

    public enum CatastropheType
    {
        Wildfire,
        Flood,
        Drought,
        Earthquake,
        VolcanicEruption,
        Hurricane,
        Plague,
        Meteor,
        IceAge,
        Pollution,
        HabitatDestruction,
        ClimateShift
    }

    public enum WeatherType
    {
        Rain,
        Snow,
        Storm,
        Drought,
        Heatwave,
        Cold,
        Wind,
        Fog,
        Tornado,
        Hurricane
    }

    public enum TrophicLevel
    {
        Producer,
        PrimaryConsumer,
        SecondaryConsumer,
        TertiaryConsumer,
        Decomposer,
        Omnivore,
        Carnivore,
        Herbivore
    }
}