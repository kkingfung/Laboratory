#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Examples
{
    /// <summary>
    /// Configuration factory for creating example species and biomes.
    /// Use this to quickly generate test configurations in editor.
    /// </summary>
    public static class ChimeraConfigurationFactory
    {
        [MenuItem("🐲 Chimera/Create Example Configurations/Complete Ecosystem")]
        public static void CreateCompleteEcosystem()
        {
            CreateForestBiome();
            CreateForestDragon();
            CreateCompanionWolf();
            CreateWildRabbit();
            
            Debug.Log("✅ Created complete forest ecosystem with 3 species!");
        }
        
        [MenuItem("🐲 Chimera/Create Example Configurations/Forest Biome")]
        public static void CreateForestBiome()
        {
            var biome = ScriptableObject.CreateInstance<ChimeraBiomeConfig>();
            
            biome.biomeName = "Enchanted Forest";
            biome.biomeType = BiomeType.Forest;
            biome.description = "A lush forest teeming with magical creatures and abundant resources.";
            
            // Environmental conditions
            biome.baseTemperature = 22f;
            biome.baseHumidity = 65f;
            biome.baseAltitude = 200f;
            biome.foodAvailability = 0.8f;
            biome.waterAvailability = 0.9f;
            biome.shelterAvailability = 0.7f;
            
            // Environmental pressures
            biome.predatorPressure = 0.3f;
            biome.competitionPressure = 0.4f;
            biome.weatherHarshness = 0.2f;
            biome.territorialDensity = 0.5f;
            
            // Genetic selection pressures
            biome.selectionPressures = new GeneticPressure[]
            {
                new GeneticPressure { traitName = "Intelligence", pressure = 0.2f, description = "Dense forest rewards problem-solving" },
                new GeneticPressure { traitName = "Agility", pressure = 0.15f, description = "Tree navigation requires nimbleness" },
                new GeneticPressure { traitName = "Social", pressure = 0.1f, description = "Pack coordination aids survival" }
            };
            
            // Seasonal variations
            biome.seasonalVariations = new SeasonalConfig[]
            {
                new SeasonalConfig { season = Season.Spring, temperatureModifier = 0f, humidityModifier = 0.1f, foodModifier = 0.3f },
                new SeasonalConfig { season = Season.Summer, temperatureModifier = 0.15f, humidityModifier = -0.05f, foodModifier = 0.2f },
                new SeasonalConfig { season = Season.Autumn, temperatureModifier = -0.1f, humidityModifier = 0f, foodModifier = -0.1f },
                new SeasonalConfig { season = Season.Winter, temperatureModifier = -0.25f, humidityModifier = -0.15f, foodModifier = -0.4f }
            };
            
            // Resource spawns
            biome.resourceSpawns = new ResourceSpawn[]
            {
                new ResourceSpawn { resourceType = "Food", spawnRate = 0.8f, quality = 0.7f, renewalRate = 0.6f },
                new ResourceSpawn { resourceType = "Water", spawnRate = 0.9f, quality = 0.9f, renewalRate = 0.8f },
                new ResourceSpawn { resourceType = "Shelter", spawnRate = 0.6f, quality = 0.8f, renewalRate = 0.2f },
                new ResourceSpawn { resourceType = "Magical Herbs", spawnRate = 0.3f, quality = 0.9f, renewalRate = 0.3f }
            };
            
            SaveAsset(biome, "ForestBiome_Config");
        }
        
        [MenuItem("🐲 Chimera/Create Example Configurations/Forest Dragon")]
        public static void CreateForestDragon()
        {
            var dragon = ScriptableObject.CreateInstance<ChimeraSpeciesConfig>();
            
            dragon.speciesName = "Forest Dragon";
            dragon.description = "Majestic dragons that have adapted to forest life. Highly intelligent and magical.";
            dragon.size = CreatureSize.Large;
            dragon.breedingCompatibilityGroup = 1;
            
            // Lifecycle
            dragon.maturationAge = 365; // 1 year to mature
            dragon.maxLifespan = 3650; // 10 years lifespan
            dragon.fertilityRate = 0.6f;
            dragon.maxOffspringPerBreeding = 2;
            
            // Stats (powerful but slow)
            dragon.baseStats = new CreatureStats
            {
                health = 200,
                attack = 45,
                defense = 35,
                speed = 15,
                intelligence = 25,
                charisma = 20
            };
            
            // Genetics (high intelligence and magical traits)
            dragon.defaultGenes = new GeneticTraitConfig[]
            {
                new GeneticTraitConfig { traitName = "Strength", baseValue = 0.8f, variance = 0.15f },
                new GeneticTraitConfig { traitName = "Vitality", baseValue = 0.9f, variance = 0.1f },
                new GeneticTraitConfig { traitName = "Agility", baseValue = 0.4f, variance = 0.2f },
                new GeneticTraitConfig { traitName = "Resilience", baseValue = 0.8f, variance = 0.15f },
                new GeneticTraitConfig { traitName = "Intellect", baseValue = 0.9f, variance = 0.1f },
                new GeneticTraitConfig { traitName = "Charm", baseValue = 0.7f, variance = 0.2f }
            };
            
            // Behavior (intelligent, territorial, but can be loyal)
            dragon.behaviorGenes = new BehaviorTraitConfig[]
            {
                new BehaviorTraitConfig { traitName = "Aggression", baseValue = 0.6f, variance = 0.3f, behaviorWeight = 1.2f },
                new BehaviorTraitConfig { traitName = "Loyalty", baseValue = 0.4f, variance = 0.4f, behaviorWeight = 1.5f },
                new BehaviorTraitConfig { traitName = "Curiosity", baseValue = 0.8f, variance = 0.2f, behaviorWeight = 1.1f },
                new BehaviorTraitConfig { traitName = "Social", baseValue = 0.3f, variance = 0.3f, behaviorWeight = 0.8f },
                new BehaviorTraitConfig { traitName = "Playfulness", baseValue = 0.2f, variance = 0.2f, behaviorWeight = 0.6f }
            };
            
            // Environmental preferences
            dragon.biomePreferences = new BiomePreference[]
            {
                new BiomePreference { biome = BiomeType.Forest, preference = 1.0f },
                new BiomePreference { biome = BiomeType.Mountain, preference = 0.8f },
                new BiomePreference { biome = BiomeType.Shadow, preference = 0.7f },
                new BiomePreference { biome = BiomeType.Desert, preference = 0.2f },
                new BiomePreference { biome = BiomeType.Arctic, preference = 0.1f },
                new BiomePreference { biome = BiomeType.Ocean, preference = 0.3f }
            };
            
            // AI configuration (powerful and territorial)
            dragon.aiConfig = new AIBehaviorConfig
            {
                baseDetectionRange = 25f,
                baseMovementSpeed = 8f,
                basePatrolRadius = 20f,
                baseCombatRange = 5f,
                baseFollowDistance = 8f,
                combatAggressionBase = 0.7f,
                territorialBehavior = 0.8f,
                packInstinct = 0.3f
            };
            
            SaveAsset(dragon, "ForestDragon_Species");
        }
        
        [MenuItem("🐲 Chimera/Create Example Configurations/Companion Wolf")]
        public static void CreateCompanionWolf()
        {
            var wolf = ScriptableObject.CreateInstance<ChimeraSpeciesConfig>();
            
            wolf.speciesName = "Companion Wolf";
            wolf.description = "Loyal wolves bred for companionship. Excellent pack hunters and guardians.";
            wolf.size = CreatureSize.Medium;
            wolf.breedingCompatibilityGroup = 2;
            
            // Lifecycle (shorter than dragons)
            wolf.maturationAge = 180; // 6 months to mature
            wolf.maxLifespan = 2190; // 6 years lifespan
            wolf.fertilityRate = 0.8f;
            wolf.maxOffspringPerBreeding = 4;
            
            // Stats (balanced, pack-oriented)
            wolf.baseStats = new CreatureStats
            {
                health = 120,
                attack = 30,
                defense = 25,
                speed = 25,
                intelligence = 15,
                charisma = 18
            };
            
            // Genetics (balanced with high social traits)
            wolf.defaultGenes = new GeneticTraitConfig[]
            {
                new GeneticTraitConfig { traitName = "Strength", baseValue = 0.6f, variance = 0.2f },
                new GeneticTraitConfig { traitName = "Vitality", baseValue = 0.7f, variance = 0.2f },
                new GeneticTraitConfig { traitName = "Agility", baseValue = 0.8f, variance = 0.15f },
                new GeneticTraitConfig { traitName = "Resilience", baseValue = 0.6f, variance = 0.2f },
                new GeneticTraitConfig { traitName = "Intellect", baseValue = 0.6f, variance = 0.2f },
                new GeneticTraitConfig { traitName = "Charm", baseValue = 0.7f, variance = 0.2f }
            };
            
            // Behavior (loyal, social, moderate aggression)
            wolf.behaviorGenes = new BehaviorTraitConfig[]
            {
                new BehaviorTraitConfig { traitName = "Aggression", baseValue = 0.5f, variance = 0.2f, behaviorWeight = 1.0f },
                new BehaviorTraitConfig { traitName = "Loyalty", baseValue = 0.8f, variance = 0.15f, behaviorWeight = 1.5f },
                new BehaviorTraitConfig { traitName = "Curiosity", baseValue = 0.6f, variance = 0.2f, behaviorWeight = 0.9f },
                new BehaviorTraitConfig { traitName = "Social", baseValue = 0.9f, variance = 0.1f, behaviorWeight = 1.3f },
                new BehaviorTraitConfig { traitName = "Playfulness", baseValue = 0.6f, variance = 0.3f, behaviorWeight = 1.0f }
            };
            
            // Environmental preferences (adaptable)
            wolf.biomePreferences = new BiomePreference[]
            {
                new BiomePreference { biome = BiomeType.Forest, preference = 0.9f },
                new BiomePreference { biome = BiomeType.Mountain, preference = 0.7f },
                new BiomePreference { biome = BiomeType.Arctic, preference = 0.8f },
                new BiomePreference { biome = BiomeType.Desert, preference = 0.4f },
                new BiomePreference { biome = BiomeType.Ocean, preference = 0.2f },
                new BiomePreference { biome = BiomeType.Shadow, preference = 0.5f }
            };
            
            // AI configuration (pack-oriented, loyal)
            wolf.aiConfig = new AIBehaviorConfig
            {
                baseDetectionRange = 20f,
                baseMovementSpeed = 12f,
                basePatrolRadius = 15f,
                baseCombatRange = 3f,
                baseFollowDistance = 4f,
                combatAggressionBase = 0.6f,
                territorialBehavior = 0.4f,
                packInstinct = 0.9f
            };
            
            SaveAsset(wolf, "CompanionWolf_Species");
        }
        
        [MenuItem("🐲 Chimera/Create Example Configurations/Wild Rabbit")]
        public static void CreateWildRabbit()
        {
            var rabbit = ScriptableObject.CreateInstance<ChimeraSpeciesConfig>();
            
            rabbit.speciesName = "Forest Rabbit";
            rabbit.description = "Small, quick creatures that serve as prey and environmental enrichment.";
            rabbit.size = CreatureSize.Tiny;
            rabbit.breedingCompatibilityGroup = 3;
            
            // Lifecycle (fast reproduction)
            rabbit.maturationAge = 30; // 1 month to mature
            rabbit.maxLifespan = 730; // 2 years lifespan
            rabbit.fertilityRate = 0.95f;
            rabbit.maxOffspringPerBreeding = 6;
            
            // Stats (weak but very fast)
            rabbit.baseStats = new CreatureStats
            {
                health = 40,
                attack = 5,
                defense = 8,
                speed = 35,
                intelligence = 8,
                charisma = 12
            };
            
            // Genetics (high agility, low everything else)
            rabbit.defaultGenes = new GeneticTraitConfig[]
            {
                new GeneticTraitConfig { traitName = "Strength", baseValue = 0.2f, variance = 0.1f },
                new GeneticTraitConfig { traitName = "Vitality", baseValue = 0.4f, variance = 0.2f },
                new GeneticTraitConfig { traitName = "Agility", baseValue = 0.9f, variance = 0.1f },
                new GeneticTraitConfig { traitName = "Resilience", baseValue = 0.3f, variance = 0.2f },
                new GeneticTraitConfig { traitName = "Intellect", baseValue = 0.3f, variance = 0.2f },
                new GeneticTraitConfig { traitName = "Charm", baseValue = 0.6f, variance = 0.2f }
            };
            
            // Behavior (timid, social, high fear)
            rabbit.behaviorGenes = new BehaviorTraitConfig[]
            {
                new BehaviorTraitConfig { traitName = "Aggression", baseValue = 0.1f, variance = 0.1f, behaviorWeight = 0.5f },
                new BehaviorTraitConfig { traitName = "Loyalty", baseValue = 0.3f, variance = 0.2f, behaviorWeight = 0.7f },
                new BehaviorTraitConfig { traitName = "Curiosity", baseValue = 0.7f, variance = 0.2f, behaviorWeight = 1.1f },
                new BehaviorTraitConfig { traitName = "Social", baseValue = 0.8f, variance = 0.15f, behaviorWeight = 1.2f },
                new BehaviorTraitConfig { traitName = "Playfulness", baseValue = 0.8f, variance = 0.2f, behaviorWeight = 1.1f }
            };
            
            // Environmental preferences (forest specialists)
            rabbit.biomePreferences = new BiomePreference[]
            {
                new BiomePreference { biome = BiomeType.Forest, preference = 1.0f },
                new BiomePreference { biome = BiomeType.Mountain, preference = 0.6f },
                new BiomePreference { biome = BiomeType.Arctic, preference = 0.3f },
                new BiomePreference { biome = BiomeType.Desert, preference = 0.2f },
                new BiomePreference { biome = BiomeType.Ocean, preference = 0.1f },
                new BiomePreference { biome = BiomeType.Shadow, preference = 0.4f }
            };
            
            // AI configuration (fast, skittish)
            rabbit.aiConfig = new AIBehaviorConfig
            {
                baseDetectionRange = 15f,
                baseMovementSpeed = 15f,
                basePatrolRadius = 8f,
                baseCombatRange = 1f,
                baseFollowDistance = 2f,
                combatAggressionBase = 0.1f,
                territorialBehavior = 0.1f,
                packInstinct = 0.7f
            };
            
            SaveAsset(rabbit, "ForestRabbit_Species");
        }
        
        [MenuItem("🐲 Chimera/Create Example Configurations/Desert Biome")]
        public static void CreateDesertBiome()
        {
            var biome = ScriptableObject.CreateInstance<ChimeraBiomeConfig>();
            
            biome.biomeName = "Scorching Desert";
            biome.biomeType = BiomeType.Desert;
            biome.description = "A harsh, unforgiving desert that tests survival skills to the limit.";
            
            // Environmental conditions (harsh)
            biome.baseTemperature = 45f;
            biome.baseHumidity = 15f;
            biome.baseAltitude = 300f;
            biome.foodAvailability = 0.3f;
            biome.waterAvailability = 0.2f;
            biome.shelterAvailability = 0.4f;
            
            // High environmental pressures
            biome.predatorPressure = 0.6f;
            biome.competitionPressure = 0.8f;
            biome.weatherHarshness = 0.9f;
            biome.territorialDensity = 0.3f;
            
            // Strong genetic selection pressures
            biome.selectionPressures = new GeneticPressure[]
            {
                new GeneticPressure { traitName = "Resilience", pressure = 0.4f, description = "Desert survival requires toughness" },
                new GeneticPressure { traitName = "Strength", pressure = 0.3f, description = "Competition for resources is fierce" },
                new GeneticPressure { traitName = "Aggression", pressure = 0.2f, description = "Territorial disputes are common" }
            };
            
            SaveAsset(biome, "DesertBiome_Config");
        }
        
        private static void SaveAsset(ScriptableObject asset, string filename)
        {
            string path = $"Assets/_Project/Configurations/{filename}.asset";
            
            // Create directory if it doesn't exist
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"✅ Created {asset.GetType().Name}: {filename} at {path}");
        }
    }
}
#endif