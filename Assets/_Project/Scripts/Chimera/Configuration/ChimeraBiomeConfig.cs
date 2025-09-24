using UnityEngine;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using System.Collections.Generic;

namespace Laboratory.Chimera.Configuration
{
    /// <summary>
    /// Designer-friendly biome configuration for environmental systems.
    /// Integrates with breeding environments and genetic adaptation.
    /// </summary>
    [CreateAssetMenu(fileName = "New Biome Config", menuName = "Chimera/Biome Configuration", order = 2)]
    public class ChimeraBiomeConfig : ScriptableObject
    {
        [Header("Basic Biome Info")]
        [SerializeField] public string biomeName = "New Biome";
        [SerializeField] public Laboratory.Chimera.Core.BiomeType biomeType = Laboratory.Chimera.Core.BiomeType.Forest;
        [SerializeField] public string description = "";
        [SerializeField] public Sprite biomeIcon;
        [SerializeField] public Color biomeColor = Color.green;
        
        [Header("Environmental Conditions")]
        [SerializeField] [Range(-50f, 70f)] public float baseTemperature = 20f;
        [SerializeField] [Range(0f, 100f)] public float baseHumidity = 50f;
        [SerializeField] [Range(0f, 5000f)] public float baseAltitude = 100f;
        [SerializeField] [Range(0f, 1f)] public float foodAvailability = 0.7f;
        [SerializeField] [Range(0f, 1f)] public float waterAvailability = 0.8f;
        [SerializeField] [Range(0f, 1f)] public float shelterAvailability = 0.6f;
        
        [Header("Environmental Pressures")]
        [SerializeField] [Range(0f, 1f)] public float predatorPressure = 0.3f;
        [SerializeField] [Range(0f, 1f)] public float competitionPressure = 0.4f;
        [SerializeField] [Range(0f, 1f)] public float weatherHarshness = 0.3f;
        [SerializeField] [Range(0f, 1f)] public float territorialDensity = 0.4f;
        
        [Header("Genetic Selection Pressures")]
        [SerializeField] public GeneticPressure[] selectionPressures = new GeneticPressure[]
        {
            new GeneticPressure { traitName = "Strength", pressure = 0.1f, description = "Mild pressure for physical strength" },
            new GeneticPressure { traitName = "Intelligence", pressure = 0.05f, description = "Low pressure for problem solving" }
        };
        
        [Header("Seasonal Variations")]
        [SerializeField] public bool hasSeasons = true;
        [SerializeField] public SeasonalConfig[] seasonalVariations = new SeasonalConfig[]
        {
            new SeasonalConfig { season = Season.Spring, temperatureModifier = 0f, humidityModifier = 0.1f, foodModifier = 0.2f },
            new SeasonalConfig { season = Season.Summer, temperatureModifier = 0.2f, humidityModifier = -0.1f, foodModifier = 0.1f },
            new SeasonalConfig { season = Season.Autumn, temperatureModifier = -0.1f, humidityModifier = 0f, foodModifier = -0.1f },
            new SeasonalConfig { season = Season.Winter, temperatureModifier = -0.3f, humidityModifier = -0.2f, foodModifier = -0.3f }
        };
        
        [Header("Time of Day Variations")]
        [SerializeField] public bool hasDayNightCycle = true;
        [SerializeField] public TimeOfDayConfig dayConfig = new TimeOfDayConfig 
        { 
            temperatureModifier = 0.1f, 
            activityLevel = 0.8f, 
            predatorActivity = 0.4f,
            description = "Daytime conditions"
        };
        [SerializeField] public TimeOfDayConfig nightConfig = new TimeOfDayConfig 
        { 
            temperatureModifier = -0.2f, 
            activityLevel = 0.3f, 
            predatorActivity = 0.7f,
            description = "Nighttime conditions"
        };
        
        [Header("Resources and Spawning")]
        [SerializeField] public ResourceSpawn[] resourceSpawns = new ResourceSpawn[]
        {
            new ResourceSpawn { resourceType = "Food", spawnRate = 0.7f, quality = 0.6f, renewalRate = 0.5f },
            new ResourceSpawn { resourceType = "Water", spawnRate = 0.8f, quality = 0.8f, renewalRate = 0.9f },
            new ResourceSpawn { resourceType = "Shelter", spawnRate = 0.5f, quality = 0.7f, renewalRate = 0.1f }
        };
        
        [Header("Audio & Visual")]
        [SerializeField] public AudioClip ambientSounds;
        [SerializeField] public Material skyboxMaterial;
        [SerializeField] public GameObject[] environmentPrefabs;
        [SerializeField] public ParticleSystem weatherEffects;
        
        [Header("Creature Spawning")]
        [SerializeField] public CreatureSpawnConfig[] nativeSpecies = new CreatureSpawnConfig[0];
        [SerializeField] public CreatureSpawnConfig[] rareSpecies = new CreatureSpawnConfig[0];
        [SerializeField] [Range(0f, 1f)] public float wildCreatureDensity = 0.3f;
        [SerializeField] [Range(1, 50)] public int maxWildCreatures = 10;
        
        /// <summary>
        /// Creates a BreedingEnvironment for this biome with current conditions
        /// </summary>
        public BreedingEnvironment CreateBreedingEnvironment(Season currentSeason = Season.Spring, bool isDay = true)
        {
            var seasonal = GetSeasonalConfig(currentSeason);
            var timeOfDay = isDay ? dayConfig : nightConfig;
            
            return new BreedingEnvironment
            {
                BiomeType = biomeType,
                Temperature = baseTemperature + seasonal.temperatureModifier + timeOfDay.temperatureModifier,
                FoodAvailability = Mathf.Clamp01(foodAvailability + seasonal.foodModifier),
                PredatorPressure = Mathf.Clamp01(predatorPressure + (timeOfDay.predatorActivity - 0.5f)),
                PopulationDensity = territorialDensity
            };
        }
        
        /// <summary>
        /// Creates EnvironmentalFactors for genetic adaptation
        /// </summary>
        public EnvironmentalFactors CreateEnvironmentalFactors()
        {
            var factors = EnvironmentalFactors.FromBiome(biomeType);
            
            // Apply custom environmental pressures from configuration
            foreach (var pressure in selectionPressures)
            {
                factors.SetTraitBias(pressure.traitName, pressure.pressure);
            }
            
            factors.temperature = baseTemperature;
            factors.humidity = baseHumidity;
            factors.foodAvailability = foodAvailability;
            factors.predatorPressure = predatorPressure;
            factors.socialDensity = territorialDensity;
            
            return factors;
        }
        
        /// <summary>
        /// Gets environmental stress level for a creature in this biome
        /// </summary>
        public float CalculateEnvironmentalStress(ChimeraSpeciesConfig species)
        {
            float biomeCompatibility = species.GetBiomePreference(biomeType);
            float resourceStress = (1f - foodAvailability) * 0.3f + (1f - waterAvailability) * 0.2f;
            float threatStress = predatorPressure * 0.3f + competitionPressure * 0.2f;
            
            return Mathf.Clamp01((1f - biomeCompatibility) + resourceStress + threatStress);
        }
        
        /// <summary>
        /// Gets adaptation bonuses for creatures living in this biome
        /// </summary>
        public Dictionary<string, float> GetAdaptationBonuses()
        {
            var bonuses = new Dictionary<string, float>();
            
            foreach (var pressure in selectionPressures)
            {
                if (pressure.pressure > 0.1f) // Significant pressure
                {
                    bonuses[pressure.traitName] = pressure.pressure * 0.5f; // Convert to bonus
                }
            }
            
            return bonuses;
        }
        
        /// <summary>
        /// Gets current environmental conditions based on time and season
        /// </summary>
        public BiomeConditions GetCurrentConditions(Season season, float timeOfDay)
        {
            var seasonal = GetSeasonalConfig(season);
            bool isDay = timeOfDay > 0.25f && timeOfDay < 0.75f; // Day is 6am to 6pm
            var timeConfig = isDay ? dayConfig : nightConfig;
            
            return new BiomeConditions
            {
                temperature = baseTemperature + seasonal.temperatureModifier + timeConfig.temperatureModifier,
                humidity = Mathf.Clamp01(baseHumidity + seasonal.humidityModifier),
                foodAvailability = Mathf.Clamp01(foodAvailability + seasonal.foodModifier),
                activityLevel = timeConfig.activityLevel,
                predatorActivity = timeConfig.predatorActivity,
                weatherHarshness = this.weatherHarshness + seasonal.weatherModifier
            };
        }
        
        private SeasonalConfig GetSeasonalConfig(Season season)
        {
            foreach (var config in seasonalVariations)
            {
                if (config.season == season)
                    return config;
            }
            return seasonalVariations[0]; // Fallback to first season
        }
        
        /// <summary>
        /// Spawns appropriate wild creatures for this biome
        /// </summary>
        public List<ChimeraSpeciesConfig> GetSpawnableSpecies(bool includeRare = false)
        {
            var spawnList = new List<ChimeraSpeciesConfig>();
            
            foreach (var spawn in nativeSpecies)
            {
                if (spawn.species != null && Random.value < spawn.spawnChance)
                {
                    spawnList.Add(spawn.species);
                }
            }
            
            if (includeRare)
            {
                foreach (var spawn in rareSpecies)
                {
                    if (spawn.species != null && Random.value < spawn.spawnChance)
                    {
                        spawnList.Add(spawn.species);
                    }
                }
            }
            
            return spawnList;
        }
    }
    
    [System.Serializable]
    public class GeneticPressure
    {
        [SerializeField] public string traitName = "";
        [SerializeField] [Range(-0.5f, 0.5f)] public float pressure = 0f;
        [SerializeField] public string description = "";
    }
    
    [System.Serializable]
    public class SeasonalConfig
    {
        [SerializeField] public Season season = Season.Spring;
        [SerializeField] [Range(-0.5f, 0.5f)] public float temperatureModifier = 0f;
        [SerializeField] [Range(-0.5f, 0.5f)] public float humidityModifier = 0f;
        [SerializeField] [Range(-0.5f, 0.5f)] public float foodModifier = 0f;
        [SerializeField] [Range(-0.3f, 0.3f)] public float weatherModifier = 0f;
    }
    
    [System.Serializable]
    public class TimeOfDayConfig
    {
        [SerializeField] [Range(-0.5f, 0.5f)] public float temperatureModifier = 0f;
        [SerializeField] [Range(0f, 1f)] public float activityLevel = 0.5f;
        [SerializeField] [Range(0f, 1f)] public float predatorActivity = 0.5f;
        [SerializeField] public string description = "";
    }
    
    [System.Serializable]
    public class ResourceSpawn
    {
        [SerializeField] public string resourceType = "";
        [SerializeField] [Range(0f, 1f)] public float spawnRate = 0.5f;
        [SerializeField] [Range(0f, 1f)] public float quality = 0.5f;
        [SerializeField] [Range(0f, 1f)] public float renewalRate = 0.3f;
    }
    
    [System.Serializable]
    public class CreatureSpawnConfig
    {
        [SerializeField] public ChimeraSpeciesConfig species;
        [SerializeField] [Range(0f, 1f)] public float spawnChance = 0.3f;
        [SerializeField] [Range(1, 10)] public int minGroupSize = 1;
        [SerializeField] [Range(1, 20)] public int maxGroupSize = 3;
        [SerializeField] public bool isNocturnal = false;
    }
    
    public struct BiomeConditions
    {
        public float temperature;
        public float humidity;
        public float foodAvailability;
        public float activityLevel;
        public float predatorActivity;
        public float weatherHarshness;
    }
    
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }
}