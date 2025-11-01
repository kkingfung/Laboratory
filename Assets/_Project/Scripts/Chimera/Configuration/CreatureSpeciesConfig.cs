using System;
using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Breeding;
using Laboratory.Core.Enums;
using CoreTraitType = Laboratory.Core.Enums.TraitType;

namespace Laboratory.Chimera.Configuration
{
    // Updated to fix compilation errors - 2025
    /// <summary>
    /// Simple creature species configuration for Project Chimera.
    /// This creates the ScriptableObject menu items you can use to design creatures.
    /// </summary>
    
    [CreateAssetMenu(fileName = "New Creature Species", menuName = "Chimera/Creature Species", order = 1)]
    public class CreatureSpeciesConfig : ScriptableObject
    {
        [Header("Basic Information")]
        public string speciesName = "Unnamed Species";
        public string scientificName = "Creaturius unknownius";
        [TextArea(3, 5)]
        public string description = "A mysterious creature with unknown origins...";
        public Sprite icon;
        public GameObject visualPrefab;
        public RuntimeAnimatorController animatorController;
        
        [Header("Physical Characteristics")]
        public CreatureSize baseSize = CreatureSize.Medium;
        [Range(0.1f, 10f)]
        public float sizeVariation = 0.2f; // ±20% size variation
        public Laboratory.Core.Enums.BiomeType nativeBiome = Laboratory.Core.Enums.BiomeType.Forest;
        public Laboratory.Core.Enums.BiomeType[] compatibleBiomes = { Laboratory.Core.Enums.BiomeType.Forest, Laboratory.Core.Enums.BiomeType.Grassland };
        
        [Header("Base Stats")]
        public CreatureStats baseStats = new CreatureStats
        {
            health = 100,
            attack = 15,
            defense = 10,
            speed = 12,
            intelligence = 8,
            charisma = 10
        };
        
        [Header("Breeding")]
        [Range(0f, 1f)]
        public float fertilityRate = 0.7f;
        [Range(1, 365)]
        public int maturationAge = 90; // Days to reach adulthood
        [Range(1f, 30f)]
        public float pregnancyDuration = 7f; // Days
        [Range(1, 10)]
        public int averageOffspring = 2;
        
        [Header("Behavior")]
        public CreatureAIBehaviorType defaultBehavior = CreatureAIBehaviorType.Wild;
        [Range(0f, 1f)]
        public float baseAggression = 0.3f;
        [Range(0f, 1f)]
        public float socialNeed = 0.5f;
        [Range(0f, 1f)]
        public float territorialness = 0.4f;
        
        [Header("Genetic Template")]
        public GeneticTemplateConfig geneticTemplate;
        
        [Header("Rarity & Discovery")]
        [Range(0f, 1f)]
        public float spawnRarity = 0.5f; // How rare this species is in wild
        [Range(0f, 1f)]
        public float shinyChance = 0.001f; // Chance for rare coloration
        public bool isLegendary = false;
        public bool requiresSpecialConditions = false;
        [TextArea(2, 3)]
        public string discoveryHint = "Can be found in temperate forests during spring...";
        
        [Header("Ecosystem Role")]
        public DietType ecosystemRole = DietType.Herbivore;
        [Range(1f, 100f)]
        public float biomassContribution = 10f;
        public FoodType[] preferredFoods = { FoodType.Grass, FoodType.Berries, FoodType.Insects };
        public PredatorType[] predators = { PredatorType.LargeMammal };
        
        [Header("Audio")]
        public AudioClip[] idleSounds;
        public AudioClip[] combatSounds;
        public AudioClip[] happySounds;
        public AudioClip[] stressedSounds;
        
        /// <summary>
        /// Generate a creature instance from this species config
        /// </summary>
        public CreatureInstance CreateInstance(bool isWild = true)
        {
            var genetics = geneticTemplate != null ? 
                geneticTemplate.GenerateGeneticProfile() : 
                new GeneticProfile();
                
            return new CreatureInstance
            {
                Definition = ConvertToCreatureDefinition(),
                GeneticProfile = genetics,
                AgeInDays = isWild ? UnityEngine.Random.Range(maturationAge, maturationAge * 3) : 0,
                CurrentHealth = baseStats.health,
                Happiness = UnityEngine.Random.Range(0.4f, 0.8f),
                IsWild = isWild,
                BirthDate = DateTime.UtcNow - TimeSpan.FromDays(isWild ? UnityEngine.Random.Range(0, 365) : 0)
            };
        }
        
        /// <summary>
        /// Convert to CreatureDefinition for compatibility
        /// </summary>
        public CreatureDefinition ConvertToCreatureDefinition()
        {
            var definition = CreateInstance<CreatureDefinition>();
            definition.speciesName = speciesName;
            definition.description = description;
            definition.icon = icon;
            definition.prefab = visualPrefab;
            definition.baseStats = baseStats;
            definition.size = baseSize;
            definition.fertilityRate = fertilityRate;
            definition.maturationAge = maturationAge;
            definition.preferredBiomes = compatibleBiomes;
            definition.behaviorProfile.aggression = baseAggression;
            definition.dietType = ecosystemRole;
            return definition;
        }
    }
    
    // === GENETIC TEMPLATE SYSTEM ===
    
    [CreateAssetMenu(fileName = "New Genetic Template", menuName = "Chimera/Genetic Template", order = 2)]
    public class GeneticTemplateConfig : ScriptableObject
    {
        [Header("Gene Pool")]
        public GeneConfig[] availableGenes = new GeneConfig[0];
        
        [Header("Generation Settings")]
        [Range(0f, 1f)]
        public float baseMutationRate = 0.02f;
        [Range(5, 20)]
        public int minGenes = 8;
        [Range(10, 30)]
        public int maxGenes = 15;
        
        [Header("Trait Weights")]
        [Range(0f, 1f)]
        public float physicalTraitWeight = 0.3f;
        [Range(0f, 1f)]
        public float mentalTraitWeight = 0.2f;
        [Range(0f, 1f)]
        public float combatTraitWeight = 0.2f;
        [Range(0f, 1f)]
        public float socialTraitWeight = 0.15f;
        [Range(0f, 1f)]
        public float magicalTraitWeight = 0.1f;
        [Range(0f, 1f)]
        public float utilityTraitWeight = 0.05f;
        
        /// <summary>
        /// Generate a genetic profile from this template
        /// </summary>
        public GeneticProfile GenerateGeneticProfile()
        {
            var selectedGenes = new System.Collections.Generic.List<Gene>();
            int geneCount = UnityEngine.Random.Range(minGenes, maxGenes + 1);
            
            // Create some default genes if none configured
            if (availableGenes.Length == 0)
            {
                selectedGenes.Add(CreateDefaultGene(CoreTraitType.Strength));
                selectedGenes.Add(CreateDefaultGene(CoreTraitType.Agility));
                selectedGenes.Add(CreateDefaultGene(CoreTraitType.Intelligence));
                selectedGenes.Add(CreateDefaultGene(CoreTraitType.Aggression));
                selectedGenes.Add(CreateDefaultGene(CoreTraitType.Loyalty));
                selectedGenes.Add(CreateDefaultGene(CoreTraitType.ColorPattern));
                selectedGenes.Add(CreateDefaultGene(CoreTraitType.Size));
            }
            else
            {
                // Weighted selection of genes
                for (int i = 0; i < geneCount; i++)
                {
                    var geneConfig = SelectWeightedGene();
                    if (geneConfig != null)
                    {
                        selectedGenes.Add(geneConfig.GenerateGene());
                    }
                }
            }
            
            return new GeneticProfile(selectedGenes.ToArray(), 1, "");
        }
        
        private Gene CreateDefaultGene(CoreTraitType traitType)
        {
            return new Gene
            {
                traitName = traitType.GetDisplayName(),
                traitType = traitType,
                value = UnityEngine.Random.Range(0.2f, 0.8f),
                dominance = UnityEngine.Random.Range(0.3f, 0.7f),
                expression = GeneExpression.Normal,
                isActive = true
            };
        }
        
        private GeneConfig SelectWeightedGene()
        {
            if (availableGenes.Length == 0) return null;
            
            return availableGenes[UnityEngine.Random.Range(0, availableGenes.Length)];
        }
    }
    
    [System.Serializable]
    public class GeneConfig
    {
        public CoreTraitType traitType = CoreTraitType.Size;
        [Range(0f, 1f)]
        public float minValue = 0.2f;
        [Range(0f, 1f)]
        public float maxValue = 0.8f;
        [Range(0f, 1f)]
        public float baseDominance = 0.5f;
        [Range(0f, 0.3f)]
        public float dominanceVariation = 0.1f;
        public GeneExpression defaultExpression = GeneExpression.Normal;
        [Range(0f, 1f)]
        public float rarityModifier = 1f; // Higher = more common
        
        public Gene GenerateGene()
        {
            return new Gene
            {
                traitName = traitType.GetDisplayName(),
                traitType = traitType,
                value = UnityEngine.Random.Range(minValue, maxValue),
                dominance = Mathf.Clamp01(baseDominance + UnityEngine.Random.Range(-dominanceVariation, dominanceVariation)),
                expression = defaultExpression,
                isActive = true
            };
        }
    }
    
    // === GAME CONFIGURATION ===
    
    [CreateAssetMenu(fileName = "Chimera Game Config", menuName = "Chimera/Game Configuration", order = 10)]
    public class ChimeraGameConfig : ScriptableObject
    {
        [Header("Core Game Settings")]
        public string gameVersion = "1.0.0";
        [Range(0.1f, 10f)]
        public float timeScale = 1f;
        [Range(1, 30)]
        public int dayDurationMinutes = 10; // Real minutes per game day
        
        [Header("Population Settings")]
        [Range(100, 10000)]
        public int maxWorldPopulation = 2000;
        [Range(0.1f, 10f)]
        public float populationGrowthRate = 1f;
        [Range(0f, 1f)]
        public float wildSpawnRate = 0.3f;
        
        [Header("Genetic System")]
        [Range(0f, 0.2f)]
        public float globalMutationRate = 0.02f;
        [Range(0f, 1f)]
        public float evolutionPressureIntensity = 0.5f;
        [Range(1, 100)]
        public int maxGenerationsTracked = 20;
        
        [Header("Debug & Development")]
        public bool enableDebugMode = false;
        public bool enableCheatCommands = false;
        public bool enableDetailedLogging = false;
        public bool skipTutorial = false;
        
        /// <summary>
        /// Apply configuration settings to game systems
        /// </summary>
        public void ApplySettings()
        {
            Time.timeScale = timeScale;
            UnityEngine.Debug.Log($"Applied Chimera game settings: timeScale={timeScale}, maxPop={maxWorldPopulation}");
        }
        
        /// <summary>
        /// Reset to default values
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            timeScale = 1f;
            dayDurationMinutes = 10;
            maxWorldPopulation = 2000;
            globalMutationRate = 0.02f;
            enableDebugMode = false;
            
            UnityEngine.Debug.Log("Configuration reset to defaults");
        }
    }
    
    /// <summary>
    /// AI behavior types for creatures (local copy to avoid assembly dependencies)
    /// </summary>
    public enum CreatureAIBehaviorType
    {
        Companion,    // Follows player, moderate aggression
        Aggressive,   // Seeks out enemies actively
        Defensive,    // Only fights when player is threatened
        Passive,      // Never fights, only follows
        Guard,        // Patrols area, defends territory
        Wild,         // Natural wild creature behavior
        Predator,     // Active hunting behavior
        Herbivore     // Peaceful grazing behavior
    }
}