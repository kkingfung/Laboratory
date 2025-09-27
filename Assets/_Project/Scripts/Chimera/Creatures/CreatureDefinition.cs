using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Laboratory.Chimera.Genetics;
using CoreBiomeType = Laboratory.Chimera.Core.BiomeType;

namespace Laboratory.Chimera.Creatures
{
    /// <summary>
    /// Core definition for a creature species in Project Chimera.
    /// Contains base stats, evolutionary potential, and behavioral patterns.
    /// </summary>
    [CreateAssetMenu(fileName = "New Creature", menuName = "Chimera/Creature Definition")]
    public class CreatureDefinition : ScriptableObject
    {
        [Header("Basic Information")]
        public string speciesName = "Unknown Species";
        [TextArea(3, 5)]
        public string description = "A mysterious creature...";
        public Sprite icon;
        public GameObject prefab;
        
        [Header("Base Stats")]
        public CreatureStats baseStats = new CreatureStats
        {
            health = 100,
            attack = 20,
            defense = 15,
            speed = 10,
            intelligence = 5,
            charisma = 5
        };
        
        [Header("Physical Characteristics")]
        public CreatureSize size = CreatureSize.Medium;
        public CreatureType primaryType = CreatureType.Beast;
        public CreatureType secondaryType = CreatureType.None;
        public float baseWeight = 50f; // kg
        public float baseHeight = 1.2f; // meters
        
        [Header("Breeding & Genetics")]
        public int breedingCompatibilityGroup = 1;
        public float fertilityRate = 0.75f;
        public int gestationPeriod = 30; // days
        public int maturationAge = 90; // days
        public int maxLifespan = 365 * 5; // days
        
        [Header("Environmental Preferences")]
        public CoreBiomeType[] preferredBiomes = { CoreBiomeType.Forest };
        public float[] biomeCompatibility = { 1.0f }; // matches preferredBiomes array
        public TemperatureRange temperatureRange = TemperatureRange.Temperate;
        public HumidityRange humidityRange = HumidityRange.Moderate;
        
        [Header("Behavior")]
        public CreatureBehaviorProfile behaviorProfile = new CreatureBehaviorProfile();
        public SocialStructure socialBehavior = SocialStructure.Solitary;
        public DietType dietType = DietType.Omnivore;
        
        [Header("Evolution")]
        public EvolutionPath[] possibleEvolutions = Array.Empty<EvolutionPath>();
        public TraitPotential[] geneticTraits = Array.Empty<TraitPotential>();
        
        [Header("Combat")]
        public AbilityReference[] baseAbilities = Array.Empty<AbilityReference>();
        public DamageType[] resistances = Array.Empty<DamageType>();
        public DamageType[] weaknesses = Array.Empty<DamageType>();
        
        [Header("Rarity & Discovery")]
        [Range(0f, 1f)]
        public float spawnRarity = 0.1f;
        public DiscoveryRequirement discoveryRequirement = new DiscoveryRequirement();
        
        /// <summary>
        /// Calculates effective stats based on level, genetics, and environmental factors
        /// </summary>
        public CreatureStats CalculateEffectiveStats(int level, GeneticProfile genetics, EnvironmentalModifiers environment)
        {
            var stats = baseStats;
            
            // Apply level scaling (each level adds 5% to base stats)
            float levelMultiplier = 1f + (level - 1) * 0.05f;
            stats = stats * levelMultiplier;
            
            // Apply genetic modifiers
            if (genetics != null)
            {
                stats = genetics.ApplyModifiers(stats);
            }
            
            // Apply environmental modifiers
            if (environment != null)
            {
                stats = environment.ApplyModifiers(stats, this);
            }
            
            return stats;
        }
        
        /// <summary>
        /// Checks if this creature can breed with another species
        /// </summary>
        public bool CanBreedWith(CreatureDefinition other)
        {
            return breedingCompatibilityGroup == other.breedingCompatibilityGroup &&
                   breedingCompatibilityGroup > 0; // 0 means no breeding compatibility
        }
        
        /// <summary>
        /// Gets the compatibility rating with a specific biome
        /// </summary>
        public float GetBiomeCompatibility(CoreBiomeType biome)
        {
            for (int i = 0; i < preferredBiomes.Length && i < biomeCompatibility.Length; i++)
            {
                if (preferredBiomes[i] == biome)
                    return biomeCompatibility[i];
            }
            return 0.2f; // Default low compatibility for non-preferred biomes
        }
    }
    
    [Serializable]
    public struct CreatureStats
    {
        public int health;
        public int attack;
        public int defense;
        public int speed;
        public int intelligence;
        public int charisma;
        
        // Additional properties for compatibility
        public int Strength => attack;
        public int Agility => speed;
        public int Endurance => defense;
        public int Intelligence => intelligence;
        
        public static CreatureStats operator *(CreatureStats stats, float multiplier)
        {
            return new CreatureStats
            {
                health = Mathf.RoundToInt(stats.health * multiplier),
                attack = Mathf.RoundToInt(stats.attack * multiplier),
                defense = Mathf.RoundToInt(stats.defense * multiplier),
                speed = Mathf.RoundToInt(stats.speed * multiplier),
                intelligence = Mathf.RoundToInt(stats.intelligence * multiplier),
                charisma = Mathf.RoundToInt(stats.charisma * multiplier)
            };
        }
        
        public int GetTotalPower()
        {
            return health + attack + defense + speed + intelligence + charisma;
        }
        
        /// <summary>
        /// Constructor for creating stats with all values
        /// </summary>
        public CreatureStats(int health, int attack, int defense, int speed, int intelligence, int charisma)
        {
            this.health = health;
            this.attack = attack;
            this.defense = defense;
            this.speed = speed;
            this.intelligence = intelligence;
            this.charisma = charisma;
        }
    }
    
    [Serializable]
    public class CreatureBehaviorProfile
    {
        [Range(0f, 1f)]
        public float aggression = 0.5f;
        [Range(0f, 1f)]
        public float curiosity = 0.5f;
        [Range(0f, 1f)]
        public float loyalty = 0.5f;
        [Range(0f, 1f)]
        public float independence = 0.5f;
        [Range(0f, 1f)]
        public float playfulness = 0.5f;
    }
    
    [Serializable]
    public struct EvolutionPath
    {
        public CreatureDefinition evolutionTarget;
        public EvolutionTrigger trigger;
        public int requiredLevel;
        public float requiredHappiness;
        public string[] requiredEnvironmentalConditions;
    }
    
    [Serializable]
    public struct TraitPotential
    {
        public string traitName;
        public Laboratory.Chimera.Genetics.TraitType traitType;
        [Range(0f, 1f)]
        public float inheritanceProbability;
        public float minValue;
        public float maxValue;
    }
    
    [Serializable]
    public struct AbilityReference
    {
        public string abilityId;
        public int learnLevel;
        [Range(0f, 1f)]
        public float learnProbability;
    }
    
    [Serializable]
    public class DiscoveryRequirement
    {
        public CoreBiomeType requiredBiome = CoreBiomeType.Grassland;
        public TimeOfDay requiredTimeOfDay = TimeOfDay.Any;
        public WeatherCondition requiredWeather = WeatherCondition.Any;
        public int minimumPlayerLevel = 1;
        public string[] prerequisiteDiscoveries = Array.Empty<string>();
    }
    
    public enum CreatureSize
    {
        Tiny,    // Mouse-sized
        Small,   // Cat-sized  
        Medium,  // Human-sized
        Large,   // Horse-sized
        Huge,    // Elephant-sized
        Colossal // Building-sized
    }
    
    public enum CreatureType
    {
        None,
        Beast,      // Physical, natural animals
        Elemental,  // Fire, water, earth, air
        Spirit,     // Ghostly, ethereal
        Plant,      // Botanical creatures
        Mechanical, // Constructed beings
        Aquatic,    // Water-dwelling
        Aerial,     // Flying creatures
        Psychic,    // Mind-based powers
        Dark,       // Shadow/dark magic
        Light,      // Holy/light magic
        Hybrid      // Mixed types
    }
    
    // BiomeType enum moved to Laboratory.Chimera.Core namespace
    
    public enum TemperatureRange
    {
        Freezing,   // Below 0°C
        Cold,       // 0-10°C
        Cool,       // 10-20°C
        Temperate,  // 20-30°C
        Warm,       // 30-40°C
        Hot,        // 40-50°C
        Scorching   // Above 50°C
    }
    
    public enum HumidityRange
    {
        Arid,       // 0-20%
        Dry,        // 20-40%
        Moderate,   // 40-60%
        Humid,      // 60-80%
        Wet         // 80-100%
    }
    
    public enum SocialStructure
    {
        Solitary,
        Pair,
        SmallGroup,
        Pack,
        Herd,
        Colony,
        Hive
    }
    
    public enum DietType
    {
        Herbivore,
        Carnivore,
        Omnivore,
        Insectivore,
        Piscivore,
        Frugivore,
        Energy      // Feeds on magical/electrical energy
    }
    
    public enum EvolutionTrigger
    {
        Level,
        Happiness,
        Environment,
        Time,
        Battle,
        Breeding,
        Item,
        Special
    }
    
    public enum DamageType
    {
        Physical,
        Fire,
        Water,
        Electric,
        Ice,
        Poison,
        Psychic,
        Dark,
        Light,
        Earth,
        Air
    }
    
    public enum TimeOfDay
    {
        Any,
        Dawn,
        Day,
        Dusk,
        Night
    }
    
    public enum WeatherCondition
    {
        Any,
        Clear,
        Rain,
        Storm,
        Snow,
        Fog,
        Wind,
        Extreme
    }
    
    // Forward declaration for missing class
    public class EnvironmentalModifiers
    {
        public CreatureStats ApplyModifiers(CreatureStats stats, CreatureDefinition definition) => stats;
    }
}
