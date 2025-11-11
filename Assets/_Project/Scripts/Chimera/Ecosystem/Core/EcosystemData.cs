using System;
using Laboratory.Chimera.Core;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Genetics.Core;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;

namespace Laboratory.Chimera.Ecosystem.Core
{
    /// <summary>
    /// ECS ecosystem component for entity-based ecosystem simulation.
    /// This is the COMPLETE ecosystem state used as an Entity Component.
    ///
    /// ARCHITECTURAL PURPOSE:
    /// This EcosystemState is specifically for ECS-based ecosystem simulation
    /// and is different from:
    /// - EnvironmentalState: Pure environmental/climate tracking (Data namespace)
    /// - BiomeResourceState: Resource management for biomes (Data namespace)
    ///
    /// This component contains ALL ecosystem data needed for entity-based
    /// ecosystem simulation including environmental conditions, resources,
    /// population dynamics, and genetic tracking.
    ///
    /// Used for: ECS ecosystem entities, complete ecosystem simulation state
    /// </summary>
    [Serializable]
    public struct EcosystemState : IComponentData
    {
        // Ecosystem identity
        public FixedString64Bytes EcosystemID;
        public BiomeType PrimaryBiome;
        public float BiomeStability;
        public float EnvironmentalPressure;

        // Environmental conditions
        public float Temperature;           // -50 to 50 degrees
        public float Humidity;             // 0.0 to 1.0
        public float Oxygen;               // 0.0 to 1.0
        public float FoodAvailability;     // 0.0 to 1.0
        public float WaterAvailability;    // 0.0 to 1.0
        public float Predation;            // 0.0 to 1.0

        // Population dynamics
        public int TotalPopulation;
        public int CarryingCapacity;
        public float PopulationGrowthRate;
        public float SurvivalPressure;
        public float DiversityIndex;       // Shannon diversity index

        // Temporal data
        public int DaysSinceGenesis;
        public Season CurrentSeason;
        public float SeasonProgress;       // 0.0 to 1.0 through current season
        public uint LastUpdate;

        // Property alias for backward compatibility
        public int DaysGenesis => DaysSinceGenesis;

        // Evolution tracking
        public float EvolutionaryPressure;
        public GeneticDrift DriftPattern;
        public FixedList128Bytes<SpeciesPopulation> SpeciesDistribution;

        /// <summary>
        /// Calculate ecosystem health score
        /// </summary>
        public float GetEcosystemHealth()
        {
            float baseHealth = 0.5f;

            // Environmental balance contributes to health
            float tempScore = 1.0f - math.abs(GetOptimalTemperature() - Temperature) / 50f;
            float humidityScore = math.abs(0.6f - Humidity) < 0.2f ? 1.0f : 0.5f;
            float resourceScore = (FoodAvailability + WaterAvailability) / 2f;

            baseHealth += (tempScore + humidityScore + resourceScore) / 6f;

            // Population balance
            float populationRatio = (float)TotalPopulation / CarryingCapacity;
            float populationScore = populationRatio > 1.2f ? 0.3f :
                                   populationRatio < 0.3f ? 0.4f :
                                   1.0f - math.abs(0.7f - populationRatio);

            baseHealth += populationScore / 3f;

            // Diversity bonus
            baseHealth += DiversityIndex / 6f;

            return math.clamp(baseHealth, 0f, 1f);
        }

        /// <summary>
        /// Get optimal temperature for this biome
        /// </summary>
        public float GetOptimalTemperature()
        {
            return PrimaryBiome switch
            {
                BiomeType.Tropical => 25f,
                BiomeType.Grassland => 28f,
                BiomeType.Desert => 35f,
                BiomeType.Forest => 15f,
                BiomeType.Temperate => 5f,
                BiomeType.Tundra => -10f,
                BiomeType.Mountain => 0f,
                BiomeType.Swamp => 20f,
                BiomeType.Ocean => 18f,
                BiomeType.Volcanic => 40f,
                _ => 20f
            };
        }

        /// <summary>
        /// Apply seasonal environmental changes
        /// </summary>
        public void UpdateSeasonalConditions(float deltaTime)
        {
            SeasonProgress += deltaTime / GetSeasonDuration();
            if (SeasonProgress >= 1.0f)
            {
                SeasonProgress = 0f;
                CurrentSeason = GetNextSeason();
            }

            // Apply seasonal modifiers
            float seasonalTempModifier = GetSeasonalTemperatureModifier();
            float seasonalResourceModifier = GetSeasonalResourceModifier();

            Temperature += seasonalTempModifier * deltaTime;
            FoodAvailability = math.clamp(FoodAvailability * seasonalResourceModifier, 0.1f, 1.0f);
            WaterAvailability = math.clamp(WaterAvailability * seasonalResourceModifier, 0.2f, 1.0f);
        }

        private float GetSeasonDuration()
        {
            return 30f; // 30 days per season
        }

        private Season GetNextSeason()
        {
            return CurrentSeason switch
            {
                Season.Spring => Season.Summer,
                Season.Summer => Season.Autumn,
                Season.Autumn => Season.Winter,
                Season.Winter => Season.Spring,
                _ => Season.Spring
            };
        }

        private float GetSeasonalTemperatureModifier()
        {
            float baseModifier = CurrentSeason switch
            {
                Season.Spring => 0.02f,
                Season.Summer => 0.05f,
                Season.Autumn => -0.03f,
                Season.Winter => -0.08f,
                _ => 0f
            };

            // Biome influences seasonal variation
            float biomeMultiplier = PrimaryBiome switch
            {
                BiomeType.Desert => 1.5f,
                BiomeType.Tundra => 2.0f,
                BiomeType.Tropical => 0.3f,
                BiomeType.Ocean => 0.5f,
                _ => 1.0f
            };

            return baseModifier * biomeMultiplier;
        }

        private float GetSeasonalResourceModifier()
        {
            return CurrentSeason switch
            {
                Season.Spring => 1.02f,
                Season.Summer => 1.05f,
                Season.Autumn => 0.98f,
                Season.Winter => 0.92f,
                _ => 1.0f
            };
        }
    }

    /// <summary>
    /// Individual creature population data within ecosystem
    /// </summary>
    [Serializable]
    public struct CreaturePopulation : IComponentData
    {
        public Entity SpeciesEntity;
        public FixedString64Bytes SpeciesName;
        public VisualGeneticData AverageGenetics;
        public PopulationMetrics Metrics;

        // Population dynamics
        public int CurrentPopulation;
        public int HealthyIndividuals;
        public int ReproductiveAdults;
        public int Juveniles;
        public int Elderly;

        // Survival data
        public float SurvivalRate;
        public float ReproductionRate;
        public float MigrationRate;
        public float AdaptationRate;

        // Genetic health
        public float GeneticDiversity;
        public float InbreedingCoefficient;
        public float MutationRate;
        public int GenerationNumber;

        // Environmental adaptation
        public float EnvironmentalFitness;
        public NicheSpecialization Niche;
        public FixedList64Bytes<TraitAdaptation> Adaptations;

        /// <summary>
        /// Calculate population viability
        /// </summary>
        public float GetViabilityScore()
        {
            // Population size factor
            float sizeScore = CurrentPopulation < 10 ? 0.2f :
                             CurrentPopulation < 50 ? 0.6f :
                             CurrentPopulation < 200 ? 0.9f : 1.0f;

            // Age distribution health
            float reproductiveRatio = (float)ReproductiveAdults / math.max(CurrentPopulation, 1);
            float ageScore = reproductiveRatio > 0.4f && reproductiveRatio < 0.7f ? 1.0f : 0.5f;

            // Genetic health
            float geneticScore = GeneticDiversity * (1.0f - InbreedingCoefficient);

            // Environmental adaptation
            float adaptationScore = EnvironmentalFitness;

            return math.clamp((sizeScore + ageScore + geneticScore + adaptationScore) / 4f, 0f, 1f);
        }

        /// <summary>
        /// Apply natural selection pressure
        /// </summary>
        public void ApplySelection(float selectionPressure, EcosystemState ecosystem)
        {
            // Calculate survival based on fitness
            float fitnessThreshold = 0.5f + selectionPressure * 0.3f;

            // Environmental pressure affects survival
            float environmentalSurvival = EnvironmentalFitness > fitnessThreshold ? 1.0f : 0.3f;

            // Apply population changes
            int survivors = (int)(CurrentPopulation * environmentalSurvival * SurvivalRate);
            int newBirths = (int)(ReproductiveAdults * ReproductionRate * ecosystem.FoodAvailability);

            CurrentPopulation = math.max(1, survivors + newBirths);

            // Update genetic composition
            if (survivors < CurrentPopulation * 0.8f)
            {
                // Strong selection occurred - genetic drift
                ApplyGeneticDrift(selectionPressure);
            }
        }

        private void ApplyGeneticDrift(float intensity)
        {
            // Simulate genetic changes from selection
            MutationRate = math.min(0.1f, MutationRate + intensity * 0.01f);

            if (CurrentPopulation < 50)
            {
                // Small population - higher drift
                InbreedingCoefficient = math.min(0.8f, InbreedingCoefficient + 0.02f);
                GeneticDiversity = math.max(0.1f, GeneticDiversity - 0.01f);
            }
        }
    }

    /// <summary>
    /// Resource availability and management
    /// </summary>
    [Serializable]
    public struct EcosystemResources : IComponentData
    {
        // Primary resources
        public float PlantBiomass;
        public float WaterSources;
        public float MineralDeposits;
        public float ShelterAvailability;

        // Food web resources
        public float PrimaryProducers;     // Plants, algae
        public float PrimaryConsumers;     // Herbivores
        public float SecondaryConsumers;   // Small carnivores
        public float TertiaryConsumers;    // Large carnivores
        public float Decomposers;          // Cleanup organisms

        // Resource regeneration rates
        public float PlantGrowthRate;
        public float WaterReplenishment;
        public float MineralFormation;
        public float ShelterDecay;

        // Seasonal modifiers
        public float SeasonalMultiplier;
        public float WeatherImpact;

        /// <summary>
        /// Update resource levels based on consumption and regeneration
        /// </summary>
        public void UpdateResources(float deltaTime, int totalPopulation, EcosystemState ecosystem)
        {
            // Calculate consumption pressure
            float consumptionPressure = (float)totalPopulation / ecosystem.CarryingCapacity;

            // Apply consumption
            float plantConsumption = consumptionPressure * 0.1f * deltaTime;
            float waterConsumption = consumptionPressure * 0.05f * deltaTime;
            float shelterUsage = consumptionPressure * 0.02f * deltaTime;

            PlantBiomass = math.max(0.1f, PlantBiomass - plantConsumption);
            WaterSources = math.max(0.2f, WaterSources - waterConsumption);
            ShelterAvailability = math.max(0.1f, ShelterAvailability - shelterUsage);

            // Apply regeneration
            float tempFactor = GetTemperatureGrowthFactor(ecosystem.Temperature);
            float humidityFactor = ecosystem.Humidity;

            PlantBiomass = math.min(1.0f, PlantBiomass + PlantGrowthRate * tempFactor * humidityFactor * deltaTime);
            WaterSources = math.min(1.0f, WaterSources + WaterReplenishment * ecosystem.Humidity * deltaTime);
            MineralDeposits = math.min(1.0f, MineralDeposits + MineralFormation * 0.001f * deltaTime);

            // Update food web balance
            UpdateFoodWeb(deltaTime, ecosystem);
        }

        private float GetTemperatureGrowthFactor(float temperature)
        {
            // Optimal growth around 20-30 degrees
            if (temperature < -10f || temperature > 45f) return 0.1f;
            if (temperature >= 15f && temperature <= 35f) return 1.0f;
            return 0.5f;
        }

        private void UpdateFoodWeb(float deltaTime, EcosystemState ecosystem)
        {
            // Simple predator-prey dynamics
            float plantGrowth = PlantGrowthRate * ecosystem.FoodAvailability * deltaTime;
            float herbivoreGrowth = PrimaryProducers * 0.1f * deltaTime;
            float carnivoreGrowth = PrimaryConsumers * 0.05f * deltaTime;

            PrimaryProducers = math.clamp(PrimaryProducers + plantGrowth - herbivoreGrowth, 0.1f, 2.0f);
            PrimaryConsumers = math.clamp(PrimaryConsumers + herbivoreGrowth - carnivoreGrowth, 0.05f, 1.5f);
            SecondaryConsumers = math.clamp(SecondaryConsumers + carnivoreGrowth * 0.5f, 0.02f, 1.0f);
        }
    }

    /// <summary>
    /// Environmental events that affect ecosystem dynamics
    /// </summary>
    [Serializable]
    public struct EnvironmentalEvent : IComponentData
    {
        public EcosystemEventType Type;
        public float Intensity;          // 0.0 to 1.0
        public float Duration;           // Days
        public float TimeRemaining;
        public Vector3 Location;         // If location-specific
        public float Radius;             // Area of effect

        // Effects on ecosystem
        public float TemperatureChange;
        public float HumidityChange;
        public float ResourceImpact;
        public float PopulationImpact;

        // Recovery data
        public float RecoveryRate;
        [MarshalAs(UnmanagedType.U1)]
        public bool IsRecovering;

        /// <summary>
        /// Apply event effects to ecosystem
        /// </summary>
        public void ApplyEffects(ref EcosystemState ecosystem, ref EcosystemResources resources, float deltaTime)
        {
            if (TimeRemaining <= 0f) return;

            float effectIntensity = Intensity * (TimeRemaining / Duration);

            // Apply environmental changes
            ecosystem.Temperature += TemperatureChange * effectIntensity * deltaTime;
            ecosystem.Humidity = math.clamp(ecosystem.Humidity + HumidityChange * effectIntensity * deltaTime, 0f, 1f);

            // Apply resource impacts
            float resourceMultiplier = 1.0f + ResourceImpact * effectIntensity;
            resources.PlantBiomass = math.clamp(resources.PlantBiomass * resourceMultiplier, 0.05f, 2.0f);
            resources.WaterSources = math.clamp(resources.WaterSources * resourceMultiplier, 0.1f, 1.5f);

            // Update ecosystem pressure
            ecosystem.EnvironmentalPressure = math.max(ecosystem.EnvironmentalPressure, effectIntensity);

            TimeRemaining -= deltaTime;

            // Start recovery phase
            if (TimeRemaining <= 0f && !IsRecovering)
            {
                IsRecovering = true;
                TimeRemaining = Duration * 0.5f; // Recovery takes half the event duration
            }
        }
    }

    /// <summary>
    /// Migration pattern data for population movement
    /// </summary>
    [Serializable]
    public struct MigrationPattern : IComponentData
    {
        public Entity SourceEcosystem;
        public Entity TargetEcosystem;
        public float MigrationPressure;    // What drives migration
        public int MigratingPopulation;
        public float MigrationRate;        // Individuals per day
        public MigrationType Type;

        // Migration triggers
        public float ResourceThreshold;    // Migrate when resources drop below this
        public float PopulationThreshold;  // Migrate when overcrowded
        public float SeasonalTrigger;      // Seasonal migration timing

        // Success factors
        public float SurvivalRate;         // Chance of surviving migration
        public float EstablishmentRate;    // Chance of establishing in new ecosystem
        public GeneticAdaptation RequiredAdaptation;
    }


    public enum Season : byte
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    public enum EcosystemEventType : byte
    {
        Drought,
        Flood,
        Wildfire,
        VolcanicEruption,
        Meteor,
        Disease,
        ClimateShift,
        ResourceDepletion,
        Migration,
        Mutation
    }

    public enum MigrationType : byte
    {
        Seasonal,
        Pressure,
        Exploration,
        Catastrophic
    }

    public enum GeneticDrift : byte
    {
        Stable,
        Bottleneck,
        Founder,
        Selection,
        Mutation
    }

    [Serializable]
    public struct SpeciesPopulation
    {
        public Entity SpeciesEntity;
        public int Population;
        public float Fitness;
    }

    [Serializable]
    public struct PopulationMetrics
    {
        public float BirthRate;
        public float DeathRate;
        public float GrowthRate;
        public float AgeStructureIndex;
    }

    [Serializable]
    public struct NicheSpecialization
    {
        public float TrophicLevel;        // 1.0 = herbivore, 2.0 = carnivore, etc.
        public float HabitatSpecificity; // How specialized to current habitat
        public float ResourceSpecificity; // How specialized resource needs are
        public FixedString32Bytes PrimaryFood;
    }

    [Serializable]
    public struct TraitAdaptation
    {
        public byte TraitType;           // Which genetic trait
        public float AdaptationDirection; // -1.0 to 1.0, negative = decreasing, positive = increasing
        public float AdaptationRate;     // How fast the trait is changing
        public float OptimalValue;       // Environmental optimum for this trait
    }

    [Serializable]
    public struct GeneticAdaptation
    {
        public VisualGeneticData RequiredTraits;
        public float AdaptationThreshold;
        public float AdaptationBonus;
    }
}