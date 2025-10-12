using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Chimera.Ecosystem
{
    /// <summary>
    /// Shared types and enums for the ecosystem and conservation systems
    /// </summary>

    public enum BiomeType
    {
        Forest,
        TemperateForest,
        Desert,
        Ocean,
        Mountains,
        Mountain,
        Grassland,
        Tundra,
        Arctic,
        Wetland,
        Urban,
        Taiga,
        Tropical_Rainforest,
        Temperate_Forest,
        Coastal
    }

    public enum ConservationLevel
    {
        None,
        Basic,
        Enhanced,
        High,
        Intensive,
        Emergency
    }

    public enum UrgencyLevel
    {
        Low,
        Medium,
        High,
        Critical,
        Catastrophic
    }

    public enum DisturbanceType
    {
        Natural,
        Anthropogenic,
        Climate,
        Pollution,
        Disease,
        Invasion
    }

    public enum CatastropheType
    {
        Wildfire,
        Flood,
        Drought,
        Storm,
        Earthquake,
        Volcano,
        PollutionSpill,
        DiseaseOutbreak
    }

    public enum RelationshipType
    {
        Predation,
        Competition,
        Symbiosis,
        Mutualism,
        Parasitism,
        Commensalism
    }

    public enum EventType
    {
        Migration,
        Breeding,
        Feeding,
        Territorial,
        Social,
        Environmental,
        SpeciesExtinction
    }

    public enum EmergencyType
    {
        PopulationCollapse,
        BreedingFailure,
        GeneticBottleneck,
        HabitatDestruction,
        DiseaseOutbreak,
        ClimateChange,
        ResourceDepletion,
        InvasiveSpecies,
        PollutionCrisis,
        EcosystemCollapse,
        JuvenileMortality,
        FoodWebDisruption,
        HabitatFragmentation
    }

    public enum EmergencySeverity
    {
        Minor,
        Low,
        Moderate,
        High,
        Severe,
        Critical,
        Catastrophic
    }

    public enum EventSeverity
    {
        Minor = 1,
        Low = 2,
        Moderate = 3,
        High = 4,
        Severe = 5,
        Critical = 6,
        Catastrophic = 7
    }

    public enum EmergencyActionType
    {
        ImmediateIntervention,
        PopulationBoost,
        HabitatRestoration,
        GeneticDiversification,
        BreedingProgram,
        MigrationAssistance,
        DiseaseControl,
        ResourceManagement,
        EducationCampaign,
        LegislationEnforcement,
        PopulationSupport,
        HabitatProtection,
        GeneticManagement,
        ClimateAdaptation,
        ThreatReduction,
        Monitoring,
        Research
    }

    public enum ConservationStatus
    {
        Stable,
        Monitor,
        Concern,
        Threatened,
        Endangered,
        Critical,
        Extinct
    }

    public enum ConservationUrgencyLevel
    {
        Moderate,
        Important,
        Urgent,
        Immediate,
        High,
        Critical
    }

    public enum EmergencyStatus
    {
        Active,
        InProgress,
        Resolved,
        Failed,
        TimeExpired
    }

    public enum RequirementType
    {
        PopulationTarget,
        HabitatRestoration,
        DiseaseControl,
        GeneticDiversity,
        ResourceAvailability,
        ClimateStability,
        ThreatReduction,
        PopulationIncrease,
        ReproductiveSuccess,
        HabitatProtection,
        HabitatQuality,
        PopulationManagement,
        JuvenileSurvival,
        EcosystemHealth,
        SpeciesDiversity,
        HabitatConnectivity,
        BreedingManagement,
        HealthMonitoring,
        QuarantineProtocol,
        ClimateAdaptation,
        EcosystemResilience
    }

    public enum ConservationSuccessType
    {
        PopulationRecovery,
        HabitatRestoration,
        GeneticDiversification,
        DiseaseEradication,
        EcosystemStabilization,
        SpeciesReintroduction,
        ClimateResilience,
        SpeciesRecovery,
        EcosystemRestoration,
        HabitatProtection,
        General
    }

    [System.Serializable]
    public struct EcologicalRelationship
    {
        public uint speciesId1;
        public uint speciesId2;
        public RelationshipType relationshipType;
        public float strength;
        public float3 location;

        // Additional properties expected by EcosystemEvolutionEngine
        public uint speciesA;
        public uint speciesB;
    }

    [System.Serializable]
    public struct EcologicalCatastrophe
    {
        public CatastropheType catastropheType;
        public List<uint> affectedBiomes;
        public float recoveryTime;
        public float intensity;
        public float3 epicenter;
    }

    [System.Serializable]
    public struct DisturbanceEvent
    {
        public DisturbanceType disturbanceType;
        public float intensity;
        public float duration;
        public float3 location;
        public uint[] affectedSpecies;
        public float timestamp;
        public float severity;
        public float recoveryTime;
    }

    [System.Serializable]
    public struct BiomeHealth
    {
        public float speciesDiversity;
        public float resourceAvailability;
        public float climateStress;
        public float resilienceScore;
        public float overallHealth;
        public float humanImpact;
    }

    [System.Serializable]
    public struct EcologicalEvent
    {
        public EventType eventType;
        public uint speciesId;
        public float3 location;
        public float timestamp;
        public float intensity;
        public List<uint> affectedBiomes;
        public string description;
        public float severity;
    }

    [System.Serializable]
    public struct ClimateSystem
    {
        public float globalTemperature;
        public float globalPrecipitation;
        public float atmosphericCO2;
        public float seasonalVariation;
        public float climateStability;
    }

    [System.Serializable]
    public class Biome
    {
        public uint biomeId;
        public BiomeType biomeType;
        public float3 location;
        public float area;
        public float size;
        public float carryingCapacity;
        public float creationTime;
        public ClimateCondition climateConditions;
        public Dictionary<string, Resource> resources;
        public List<EcosystemSpeciesData> species;
        public float biodiversityIndex;
        public float stabilityIndex;
        public float connectivityIndex;
        public SuccessionStage successionStage;
        public List<DisturbanceEvent> disturbanceHistory;
        public Dictionary<Season, SeasonalModifier> seasonalModifiers;
        public Dictionary<uint, int> speciesPopulations;
    }

    [System.Serializable]
    public struct EcosystemNode
    {
        public uint nodeId;
        public float3 position;
        public BiomeType biomeType;
        public float connectivity;
        public List<uint> connectedNodes;

        // Additional properties expected by EcosystemEvolutionEngine
        public Biome biome;
        public List<uint> connections;
        public List<MigrationRoute> migrationRoutes;
        public Dictionary<string, float> resourceFlows;
    }

    [System.Serializable]
    public struct MigrationRoute
    {
        public uint sourceNode;
        public uint destinationNode;
        public float difficulty;
        public float capacity;
        public Season preferredSeason;
    }

    [System.Serializable]
    public struct EcosystemMetrics
    {
        public float globalBiodiversity;
        public float totalBiomass;
        public float ecosystemStability;
        public float climaticStress;
        public int activeSpeciesCount;

        // Additional properties expected by EcosystemEvolutionEngine
        public int totalBiomes;
        public int totalSpecies;
        public float averageBiodiversity;
        public float averageStability;
        public float totalArea;
        public float climateVariability;
        public float connectivityIndex;
        public float extinctionRate;
    }

    // Additional supporting types
    [System.Serializable]
    public class ClimateCondition
    {
        public float temperature;
        public float precipitation;
        public float humidity;
        public float windSpeed;
        public float solarRadiation;
    }

    [System.Serializable]
    public class Resource
    {
        public string resourceType;
        public float maxAmount;
        public float currentAmount;
        public float regenerationRate;
        public float consumptionRate;
        public bool renewable;
    }

    [System.Serializable]
    public class EcosystemSpeciesData
    {
        public uint speciesId;
        public string speciesName;
        public float population;
        public float maxPopulation;
        public float intrinsicGrowthRate;
        public float populationGrowthRate;
        public TrophicLevel trophicLevel;
        public Dictionary<string, float> resourceRequirements;
        public EcologicalNiche ecologicalNiche;
        public float environmentalStress;
        public float optimalTemperature;
        public float optimalHumidity;
        public float migrationCapability;
        public float huntingSuccess;
        public float metabolicRate;
        public float bodySize;
        public float activityLevel;
    }

    [System.Serializable]
    public class EcologicalNiche
    {
        public float habitatSpecialization;
        public FeedingStrategy feedingStrategy;
        public ActivityPattern activityPattern;
        public float temperatureTolerance;
        public float moistureTolerance;
    }

    public enum TrophicLevel
    {
        Producer,
        Primary_Consumer,
        Secondary_Consumer,
        Tertiary_Consumer,
        Decomposer
    }

    public enum FeedingStrategy
    {
        Herbivore,
        Carnivore,
        Omnivore,
        Decomposer,
        Filter_Feeder,
        Parasite
    }

    public enum ActivityPattern
    {
        Diurnal,
        Nocturnal,
        Crepuscular,
        Cathemeral
    }

    public enum SuccessionStage
    {
        Pioneer,
        Early,
        Mid,
        Late,
        Climax
    }

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    public struct SeasonalModifier
    {
        public float temperature;
        public float precipitation;
        public float dayLength;
        public Season season;

        // Additional modifiers expected by EcosystemEvolutionEngine
        public float temperatureModifier;
        public float precipitationModifier;
        public float resourceModifier;
        public float breedingModifier;
    }

    [System.Serializable]
    public struct ResourceFlow
    {
        public string resourceType;
        public float flowRate;
        public uint sourceNode;
        public uint destinationNode;
        public float efficiency;
    }
}