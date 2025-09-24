using UnityEngine;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Core;
using System.Collections.Generic;

namespace Laboratory.Chimera.Configuration
{
    /// <summary>
    /// Designer-friendly ScriptableObject for configuring creature species.
    /// Integrates with existing genetics system and provides easy authoring workflow.
    /// </summary>
    [CreateAssetMenu(fileName = "New Species Config", menuName = "Chimera/Species Configuration", order = 1)]
    public class ChimeraSpeciesConfig : ScriptableObject
    {
        [Header("Basic Species Info")]
        [SerializeField] public string speciesName = "New Species";
        [SerializeField] public string description = "";
        [SerializeField] public CreatureSize size = CreatureSize.Medium;
        [SerializeField] public int breedingCompatibilityGroup = 1;
        [SerializeField] public Sprite speciesIcon;
        [SerializeField] public GameObject visualPrefab;
        
        [Header("Lifecycle Settings")]
        [SerializeField] public int maturationAge = 90; // Days to adult
        [SerializeField] public int maxLifespan = 1825; // Days (5 years)
        [SerializeField] [Range(0f, 1f)] public float fertilityRate = 0.8f;
        [SerializeField] [Range(1, 8)] public int maxOffspringPerBreeding = 3;
        
        [Header("Base Statistics")]
        [SerializeField] public CreatureStats baseStats = new CreatureStats
        {
            health = 100,
            attack = 20,
            defense = 15,
            speed = 10,
            intelligence = 5,
            charisma = 5
        };
        
        [Header("Default Genetic Profile")]
        [SerializeField] public GeneticTraitConfig[] defaultGenes = new GeneticTraitConfig[]
        {
            new GeneticTraitConfig { traitName = "Strength", baseValue = 0.5f, variance = 0.2f },
            new GeneticTraitConfig { traitName = "Vitality", baseValue = 0.5f, variance = 0.2f },
            new GeneticTraitConfig { traitName = "Agility", baseValue = 0.5f, variance = 0.2f },
            new GeneticTraitConfig { traitName = "Resilience", baseValue = 0.5f, variance = 0.2f },
            new GeneticTraitConfig { traitName = "Intellect", baseValue = 0.5f, variance = 0.2f },
            new GeneticTraitConfig { traitName = "Charm", baseValue = 0.5f, variance = 0.2f }
        };
        
        [Header("Behavioral Traits")]
        [SerializeField] public BehaviorTraitConfig[] behaviorGenes = new BehaviorTraitConfig[]
        {
            new BehaviorTraitConfig { traitName = "Aggression", baseValue = 0.4f, variance = 0.3f, behaviorWeight = 1.0f },
            new BehaviorTraitConfig { traitName = "Loyalty", baseValue = 0.6f, variance = 0.2f, behaviorWeight = 1.2f },
            new BehaviorTraitConfig { traitName = "Curiosity", baseValue = 0.5f, variance = 0.3f, behaviorWeight = 0.8f },
            new BehaviorTraitConfig { traitName = "Social", baseValue = 0.5f, variance = 0.2f, behaviorWeight = 0.9f },
            new BehaviorTraitConfig { traitName = "Playfulness", baseValue = 0.5f, variance = 0.3f, behaviorWeight = 0.7f }
        };
        
        [Header("Environmental Preferences")]
        [SerializeField] public BiomePreference[] biomePreferences = new BiomePreference[]
        {
            new BiomePreference { biome = Laboratory.Chimera.Core.BiomeType.Forest, preference = 1.0f },
            new BiomePreference { biome = Laboratory.Chimera.Core.BiomeType.Desert, preference = 0.3f },
            new BiomePreference { biome = Laboratory.Chimera.Core.BiomeType.Ocean, preference = 0.5f },
            new BiomePreference { biome = Laboratory.Chimera.Core.BiomeType.Mountain, preference = 0.4f },
            new BiomePreference { biome = Laboratory.Chimera.Core.BiomeType.Arctic, preference = 0.2f }
        };
        
        [Header("AI Behavior Configuration")]
        [SerializeField] public AIBehaviorConfig aiConfig = new AIBehaviorConfig();
        
        [Header("Audio Configuration")]
        [SerializeField] public AudioClip[] idleSounds;
        [SerializeField] public AudioClip[] alertSounds;
        [SerializeField] public AudioClip[] attackSounds;
        [SerializeField] public AudioClip[] happySounds;
        [SerializeField] public AudioClip[] sadSounds;
        
        /// <summary>
        /// Creates a CreatureDefinition from this configuration
        /// </summary>
        public CreatureDefinition CreateCreatureDefinition()
        {
            var definition = CreateInstance<CreatureDefinition>();
            
            definition.speciesName = speciesName;
            definition.size = size;
            definition.breedingCompatibilityGroup = breedingCompatibilityGroup;
            definition.maturationAge = maturationAge;
            definition.maxLifespan = maxLifespan;
            definition.fertilityRate = fertilityRate;
            definition.baseStats = baseStats;
            
            return definition;
        }
        
        /// <summary>
        /// Generates a random genetic profile for this species
        /// </summary>
        public GeneticProfile GenerateRandomGeneticProfile(int generation = 1, string parentLineage = "")
        {
            var genes = new List<Gene>();
            
            // Add default physical traits
            foreach (var traitConfig in defaultGenes)
            {
                genes.Add(traitConfig.GenerateGene());
            }
            
            // Add behavioral traits
            foreach (var behaviorConfig in behaviorGenes)
            {
                genes.Add(behaviorConfig.GenerateGene());
            }
            
            return new GeneticProfile(genes.ToArray(), generation, parentLineage);
        }
        
        /// <summary>
        /// Gets biome preference value (0-1)
        /// </summary>
        public float GetBiomePreference(Laboratory.Chimera.Core.BiomeType biome)
        {
            foreach (var pref in biomePreferences)
            {
                if (pref.biome == biome)
                    return pref.preference;
            }
            return 0.5f; // Default neutral preference
        }
        
        /// <summary>
        /// Gets most preferred biome for this species
        /// </summary>
        public Laboratory.Chimera.Core.BiomeType GetPreferredBiome()
        {
            Laboratory.Chimera.Core.BiomeType preferred = Laboratory.Chimera.Core.BiomeType.Forest;
            float maxPreference = 0f;
            
            foreach (var pref in biomePreferences)
            {
                if (pref.preference > maxPreference)
                {
                    maxPreference = pref.preference;
                    preferred = pref.biome;
                }
            }
            
            return preferred;
        }
    }
    
    [System.Serializable]
    public class GeneticTraitConfig
    {
        [SerializeField] public string traitName = "";
        [SerializeField] [Range(0f, 1f)] public float baseValue = 0.5f;
        [SerializeField] [Range(0f, 0.5f)] public float variance = 0.2f;
        [SerializeField] [Range(0f, 1f)] public float dominanceChance = 0.5f;
        [SerializeField] public Laboratory.Chimera.Genetics.TraitType traitType = Laboratory.Chimera.Genetics.TraitType.Physical;
        
        public Gene GenerateGene()
        {
            float variation = Random.Range(-variance, variance);
            float finalValue = Mathf.Clamp01(baseValue + variation);
            
            return new Gene
            {
                traitName = traitName,
                traitType = traitType,
                value = finalValue,
                dominance = Random.Range(0.2f, 0.8f),
                expression = GeneExpression.Normal,
                isActive = true
            };
        }
    }
    
    [System.Serializable]
    public class BehaviorTraitConfig
    {
        [SerializeField] public string traitName = "";
        [SerializeField] [Range(0f, 1f)] public float baseValue = 0.5f;
        [SerializeField] [Range(0f, 0.5f)] public float variance = 0.2f;
        [SerializeField] [Range(0f, 2f)] public float behaviorWeight = 1.0f;
        
        public Gene GenerateGene()
        {
            float variation = Random.Range(-variance, variance);
            float finalValue = Mathf.Clamp01(baseValue + variation);
            
            return new Gene
            {
                traitName = traitName,
                traitType = Laboratory.Chimera.Genetics.TraitType.Mental,
                value = finalValue,
                dominance = Random.Range(0.3f, 0.7f),
                expression = GeneExpression.Normal,
                isActive = true
            };
        }
    }
    
    [System.Serializable]
    public class BiomePreference
    {
        [SerializeField] public BiomeType biome = BiomeType.Forest;
        [SerializeField] [Range(0f, 1f)] public float preference = 0.5f;
    }
    
    [System.Serializable]
    public class AIBehaviorConfig
    {
        [Header("Detection Settings")]
        [SerializeField] [Range(1f, 50f)] public float baseDetectionRange = 15f;
        [SerializeField] [Range(0.1f, 5f)] public float detectionSpeedMultiplier = 1f;
        [SerializeField] [Range(0f, 180f)] public float fieldOfView = 90f;
        
        [Header("Movement Settings")]
        [SerializeField] [Range(1f, 20f)] public float baseMovementSpeed = 5f;
        [SerializeField] [Range(0.1f, 3f)] public float movementSpeedMultiplier = 1f;
        [SerializeField] [Range(1f, 30f)] public float basePatrolRadius = 10f;
        
        [Header("Combat Settings")]
        [SerializeField] [Range(1f, 10f)] public float baseCombatRange = 3f;
        [SerializeField] [Range(0.5f, 5f)] public float attackCooldown = 1.5f;
        [SerializeField] [Range(0f, 1f)] public float combatAggressionBase = 0.5f;
        
        [Header("Following Settings")]
        [SerializeField] [Range(1f, 15f)] public float baseFollowDistance = 5f;
        [SerializeField] [Range(0.5f, 3f)] public float followDistanceMultiplier = 1f;
        [SerializeField] [Range(1f, 30f)] public float maxFollowRange = 20f;
        
        [Header("Social Settings")]
        [SerializeField] [Range(0f, 1f)] public float baseSocialNeed = 0.5f;
        [SerializeField] [Range(0f, 1f)] public float packInstinct = 0.5f;
        [SerializeField] [Range(0f, 1f)] public float territorialBehavior = 0.3f;
    }
}