using System;
using Laboratory.Chimera.Core;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Chimera.Ecosystem.Data
{
    /// <summary>
    /// Trophic levels in the ecosystem food chain
    /// </summary>
    public enum TrophicLevel
    {
        Producer = 0,           // Plants, algae, photosynthetic organisms
        PrimaryConsumer = 1,    // Herbivores
        SecondaryConsumer = 2,  // Carnivores that eat herbivores
        TertiaryConsumer = 3,   // Top predators
        Omnivore = 4,           // Organisms that eat both plants and animals
        Decomposer = 5,         // Organisms that break down dead matter
        Scavenger = 6          // Organisms that eat dead organisms
    }
    /// <summary>
    /// Species data for ecosystem simulation
    /// </summary>
    [Serializable]
    public struct EcosystemSpeciesData
    {
        public uint SpeciesId;
        public string SpeciesName;
        public int PopulationSize;
        public TrophicLevel TrophicLevel;
        public float MetabolicRate;
        public float BodySize;
        public float GrowthRate;
        public float CarryingCapacityContribution;
        public Vector2 PreferredTemperatureRange;
        public float AdaptabilityFactor;
        public List<uint> PreySpecies;
        public List<uint> PredatorSpecies;
        public float ReproductionRate;
        public float SurvivalRate;

        // For compatibility - maps to PopulationSize
        public int populationSize => PopulationSize;
        public TrophicLevel trophicLevel => TrophicLevel;
    }

    /// <summary>
    /// Resource data structure for ecosystem simulation
    /// </summary>
    [Serializable]
    public struct Resource
    {
        public float Amount;
        public float MaxCapacity;
        public float RegenerationRate;
        public ResourceType Type;

        public Resource(float amount)
        {
            Amount = amount;
            MaxCapacity = 1000f;
            RegenerationRate = 1f;
            Type = ResourceType.Food;
        }

        public static implicit operator float(Resource resource) => resource.Amount;
        public static implicit operator Resource(float amount) => new Resource(amount);
    }

    /// <summary>
    /// Core data structures for ecosystem evolution simulation
    ///
    /// STATE ARCHITECTURE OVERVIEW:
    ///
    /// EnvironmentalState: Pure environmental/climate tracking (temperature, humidity, seasons)
    /// - Used for: Weather systems, climate simulation, seasonal changes
    /// - Contains: Temperature, humidity, rainfall, seasons, climate zones
    ///
    /// BiomeResourceState: Resource management for individual biomes (defined in separate file)
    /// - Used for: Resource distribution, population limits, biome sustainability
    /// - Contains: Resource levels, carrying capacity, biome health
    ///
    /// EcosystemState (ECS Component): Complete ecosystem simulation state for entities
    /// - Used for: ECS-based ecosystem simulation, entity ecosystem components
    /// - Contains: All ecosystem data for entity-based simulation
    /// - Location: Laboratory.Chimera.Ecosystem.Core.EcosystemData.cs
    ///
    /// This separation provides clear responsibilities and prevents data structure conflicts.
    /// </summary>

    /// <summary>
    /// Pure environmental and climate tracking state.
    /// Handles weather, seasons, and natural environmental conditions.
    /// Used for: Climate simulation, seasonal changes, weather effects.
    /// </summary>
    [Serializable]
    public struct EnvironmentalState
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
        public Dictionary<Vector2, EnvironmentalState> RegionalStates = new();
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
}