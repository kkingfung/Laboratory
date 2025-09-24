using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;
using Laboratory.Chimera.ECS.Components;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// Authoring component that converts existing Chimera creature systems to ECS components.
    /// This bridges your sophisticated genetics and AI systems with high-performance ECS simulation.
    /// 
    /// Usage: Drop this on any GameObject with a CreatureInstance to convert to ECS
    /// </summary>
    public class CreatureAuthoring : MonoBehaviour
    {
        [Header("Creature Configuration")]
        [SerializeField] private CreatureDefinition creatureDefinition;
        [SerializeField] private bool convertToECS = true;
        [SerializeField] private bool isWild = true;
        [SerializeField] private BiomeType startingBiome = BiomeType.Forest;
        
        [Header("Initial Stats Override (Optional)")]
        [SerializeField] private bool overrideStats = false;
        [SerializeField] private int customHealth = 100;
        [SerializeField] private int customAttack = 15;
        [SerializeField] private int customDefense = 12;
        [SerializeField] private int customSpeed = 10;
        [SerializeField] private int customIntelligence = 8;
        [SerializeField] private int customCharisma = 6;
        
        [Header("Genetic Profile Override (Optional)")]
        [SerializeField] private bool useCustomGenetics = false;
        [SerializeField] private GeneticProfileData customGenetics;
        
        [Header("Debug & Development")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private bool showGizmosInScene = true;
        
        // Runtime references
        private CreatureInstance creatureInstance;
        private Entity creatureEntity;
        
        /// <summary>
        /// The creature instance this authoring component manages
        /// </summary>
        public CreatureInstance CreatureInstance => creatureInstance;
        
        /// <summary>
        /// The ECS entity created from this authoring component
        /// </summary>
        public Entity CreatureEntity => creatureEntity;
        
        /// <summary>
        /// Whether this creature has been converted to ECS
        /// </summary>
        public bool IsConvertedToECS => creatureEntity != Entity.Null;
        
        private void Awake()
        {
            InitializeCreatureInstance();
        }
        
        private void Start()
        {
            if (convertToECS)
            {
                ConvertToECS();
            }
        }
        
        /// <summary>
        /// Initialize the creature instance from definition and settings
        /// </summary>
        private void InitializeCreatureInstance()
        {
            if (creatureDefinition == null)
            {
                LogError("CreatureDefinition is null! Please assign a creature definition.");
                return;
            }
            
            // Create creature instance
            creatureInstance = new CreatureInstance
            {
                Definition = creatureDefinition,
                AgeInDays = isWild ? UnityEngine.Random.Range(30, 365) : 0, // Wild creatures have random age
                IsWild = isWild,
                Happiness = UnityEngine.Random.Range(0.4f, 0.8f),
                Experience = 0,
                Level = 1
            };
            
            // Apply custom stats if specified
            if (overrideStats && creatureDefinition != null)
            {
                var customStats = new CreatureStats
                {
                    health = customHealth,
                    attack = customAttack,
                    defense = customDefense,
                    speed = customSpeed,
                    intelligence = customIntelligence,
                    charisma = customCharisma
                };
                
                // Update the definition stats (in runtime copy)
                creatureDefinition.baseStats = customStats;
            }
            
            // Generate or assign genetic profile
            if (useCustomGenetics && customGenetics != null)
            {
                creatureInstance.GeneticProfile = customGenetics.ToGeneticProfile();
            }
            else
            {
                // Generate random genetics for wild creatures or basic genetics for bred ones
                creatureInstance.GeneticProfile = GenerateGeneticProfile();
            }
            
            // Set current health to max
            creatureInstance.CurrentHealth = creatureDefinition.baseStats.health;
            
            Log($"Creature instance initialized: {creatureInstance.UniqueId} ({creatureDefinition.speciesName})");
        }
        
        /// <summary>
        /// Convert this creature to ECS for high-performance simulation
        /// </summary>
        public void ConvertToECS()
        {
            if (creatureInstance == null)
            {
                LogError("Cannot convert to ECS: CreatureInstance is null");
                return;
            }
            
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            
            // Create entity with all necessary components
            creatureEntity = entityManager.CreateEntity();
            
            // Set entity name for debugging
            entityManager.SetName(creatureEntity, $"Creature_{creatureDefinition.speciesName}_{creatureInstance.UniqueId[..8]}");
            
            // Convert to ECS components
            ConvertCoreComponents(entityManager);
            ConvertBehaviorComponents(entityManager);
            ConvertLifecycleComponents(entityManager);
            ConvertEnvironmentalComponents(entityManager);
            
            // Add tags based on creature type
            if (isWild)
            {
                entityManager.AddComponent<WildCreatureTag>(creatureEntity);
            }
            else
            {
                var ownedTag = new OwnedCreatureTag { owner = Entity.Null }; // Set player entity when available
                entityManager.AddComponentData(creatureEntity, ownedTag);
            }
            
            // Add breeding readiness if adult
            if (creatureInstance.IsAdult)
            {
                entityManager.AddComponent<ReadyToBreedTag>(creatureEntity);
            }
            
            Log($"Creature converted to ECS: Entity {creatureEntity.Index}");
        }
        
        /// <summary>
        /// Convert core creature data to ECS components
        /// </summary>
        private void ConvertCoreComponents(EntityManager entityManager)
        {
            // Note: ECS component creation temporarily disabled due to Unity.Entities compatibility issues
            // Components don't have Create() methods - they should be initialized directly

            Debug.Log($"[CreatureAuthoring] Would create ECS entity for {creatureInstance.Definition?.speciesName ?? "Unknown"}");
            Debug.Log($"- Health: {creatureInstance.CurrentHealth}");
            Debug.Log($"- Age: {creatureInstance.AgeInDays} days");
            Debug.Log($"- Level: {creatureInstance.Level}");
        }
        
        /// <summary>
        /// Convert behavior and personality data to ECS components
        /// </summary>
        private void ConvertBehaviorComponents(EntityManager entityManager)
        {
            var geneticsComponent = entityManager.GetComponentData<CreatureGeneticsComponent>(creatureEntity);
            
            // Note: Behavior component creation disabled - Create() methods don't exist
            Debug.Log($"[CreatureAuthoring] Would create behavior components at position {transform.position}");
            
            // Bonding component (if not wild)
            if (!isWild)
            {
                Debug.Log("[CreatureAuthoring] Would create bonding component for non-wild creature");
            }
        }
        
        /// <summary>
        /// Convert lifecycle and breeding data to ECS components
        /// </summary>
        private void ConvertLifecycleComponents(EntityManager entityManager)
        {
            var ageComponent = entityManager.GetComponentData<CreatureAgeComponent>(creatureEntity);
            var geneticsComponent = entityManager.GetComponentData<CreatureGeneticsComponent>(creatureEntity);
            var personalityComponent = entityManager.GetComponentData<CreaturePersonalityComponent>(creatureEntity);
            
            // Note: Lifecycle component creation disabled
            Debug.Log("[CreatureAuthoring] Would create lifecycle component");
            
            // Note: Breeding component creation disabled
            Debug.Log("[CreatureAuthoring] Would create breeding component if adult");
        }
        
        /// <summary>
        /// Convert environmental data to ECS components
        /// </summary>
        private void ConvertEnvironmentalComponents(EntityManager entityManager)
        {
            var geneticsComponent = entityManager.GetComponentData<CreatureGeneticsComponent>(creatureEntity);
            
            // Note: Environmental component creation disabled
            Debug.Log($"[CreatureAuthoring] Would create biome component for {startingBiome}");
        }
        
        /// <summary>
        /// Generate a genetic profile for this creature
        /// </summary>
        private GeneticProfile GenerateGeneticProfile()
        {
            if (isWild)
            {
                return GenerateWildGeneticProfile();
            }
            else
            {
                return GenerateBasicGeneticProfile();
            }
        }
        
        /// <summary>
        /// Generate genetic profile for wild creatures (more random)
        /// </summary>
        private GeneticProfile GenerateWildGeneticProfile()
        {
            var genes = new List<Gene>();
            
            // Core physical traits
            genes.Add(CreateRandomGene("Strength", TraitType.Physical));
            genes.Add(CreateRandomGene("Vitality", TraitType.Physical));
            genes.Add(CreateRandomGene("Agility", TraitType.Physical));
            genes.Add(CreateRandomGene("Resilience", TraitType.Physical));
            
            // Mental traits
            genes.Add(CreateRandomGene("Intellect", TraitType.Mental));
            genes.Add(CreateRandomGene("Charm", TraitType.Social));
            
            // Behavioral traits
            genes.Add(CreateRandomGene("Aggression", TraitType.Combat));
            genes.Add(CreateRandomGene("Loyalty", TraitType.Social));
            genes.Add(CreateRandomGene("Curiosity", TraitType.Mental));
            genes.Add(CreateRandomGene("Social", TraitType.Social));
            
            // Environmental adaptation traits
            genes.Add(CreateBiomeAdaptationGene(startingBiome));
            
            // Rare traits (10% chance each)
            if (UnityEngine.Random.value < 0.1f)
                genes.Add(CreateRandomGene("Night Vision", TraitType.Sensory));
            
            if (UnityEngine.Random.value < 0.1f)
                genes.Add(CreateRandomGene("Pack Leader", TraitType.Social));
            
            if (UnityEngine.Random.value < 0.1f)
                genes.Add(CreateRandomGene("Magical Affinity", TraitType.Magical));
            
            return new GeneticProfile(genes.ToArray(), 1, "wild");
        }
        
        /// <summary>
        /// Generate basic genetic profile for bred creatures
        /// </summary>
        private GeneticProfile GenerateBasicGeneticProfile()
        {
            var genes = new List<Gene>();
            
            // More balanced traits for bred creatures
            genes.Add(CreateBalancedGene("Strength", TraitType.Physical, 0.5f));
            genes.Add(CreateBalancedGene("Vitality", TraitType.Physical, 0.6f));
            genes.Add(CreateBalancedGene("Agility", TraitType.Physical, 0.5f));
            genes.Add(CreateBalancedGene("Resilience", TraitType.Physical, 0.5f));
            genes.Add(CreateBalancedGene("Intellect", TraitType.Mental, 0.4f));
            genes.Add(CreateBalancedGene("Charm", TraitType.Social, 0.4f));
            
            // Higher loyalty for bred creatures
            genes.Add(CreateBalancedGene("Loyalty", TraitType.Social, 0.8f));
            genes.Add(CreateBalancedGene("Aggression", TraitType.Combat, 0.3f));
            genes.Add(CreateBalancedGene("Curiosity", TraitType.Mental, 0.6f));
            genes.Add(CreateBalancedGene("Social", TraitType.Social, 0.7f));
            
            return new GeneticProfile(genes.ToArray(), 1, "domestic");
        }
        
        /// <summary>
        /// Create a random gene with normal distribution
        /// </summary>
        private Gene CreateRandomGene(string traitName, TraitType traitType)
        {
            return new Gene
            {
                traitName = traitName,
                traitType = traitType,
                dominance = UnityEngine.Random.Range(0.2f, 0.8f),
                value = Mathf.Clamp01(GenerateNormalRandom(0.5f, 0.2f)), // Normal distribution around 0.5
                expression = GeneExpression.Normal,
                isActive = true
            };
        }
        
        /// <summary>
        /// Create a balanced gene with specified center value
        /// </summary>
        private Gene CreateBalancedGene(string traitName, TraitType traitType, float centerValue)
        {
            return new Gene
            {
                traitName = traitName,
                traitType = traitType,
                dominance = UnityEngine.Random.Range(0.4f, 0.7f),
                value = Mathf.Clamp01(GenerateNormalRandom(centerValue, 0.15f)),
                expression = GeneExpression.Normal,
                isActive = true
            };
        }
        
        /// <summary>
        /// Create biome-specific adaptation gene
        /// </summary>
        private Gene CreateBiomeAdaptationGene(BiomeType biome)
        {
            string traitName = biome switch
            {
                BiomeType.Desert => "Heat Resistance",
                BiomeType.Tundra => "Cold Resistance",
                BiomeType.Ocean => "Swimming",
                BiomeType.Mountain => "Climbing",
                BiomeType.Forest => "Camouflage",
                BiomeType.Volcanic => "Fire Resistance",
                BiomeType.Swamp => "Poison Resistance",
                BiomeType.Underground => "Dark Vision",
                BiomeType.Sky => "Flight",
                _ => "Adaptability"
            };
            
            return new Gene
            {
                traitName = traitName,
                traitType = TraitType.Physical,
                dominance = UnityEngine.Random.Range(0.6f, 0.9f), // Biome adaptation is usually dominant
                value = UnityEngine.Random.Range(0.7f, 0.95f), // High adaptation to native biome
                expression = GeneExpression.Normal,
                isActive = true
            };
        }
        
        /// <summary>
        /// Generate normally distributed random value
        /// </summary>
        private float GenerateNormalRandom(float mean, float stdDev)
        {
            // Box-Muller transform for normal distribution
            if (UnityEngine.Random.value > 0.5f)
            {
                float u1 = 1f - UnityEngine.Random.value;
                float u2 = 1f - UnityEngine.Random.value;
                float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
                return mean + stdDev * randStdNormal;
            }
            else
            {
                return mean + (UnityEngine.Random.value - 0.5f) * stdDev * 2f;
            }
        }
        
        /// <summary>
        /// Update creature from ECS data (for UI display)
        /// </summary>
        public void UpdateFromECS()
        {
            if (!IsConvertedToECS) return;
            
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            
            if (!entityManager.Exists(creatureEntity)) return;
            
            // Update creature instance from ECS components
            if (entityManager.HasComponent<CreatureStatsComponent>(creatureEntity))
            {
                var stats = entityManager.GetComponentData<CreatureStatsComponent>(creatureEntity);
                creatureInstance.CurrentHealth = stats.CurrentHealth;
            }
            
            if (entityManager.HasComponent<CreatureAgeComponent>(creatureEntity))
            {
                var age = entityManager.GetComponentData<CreatureAgeComponent>(creatureEntity);
                creatureInstance.AgeInDays = age.AgeInDays;
            }
            
            if (entityManager.HasComponent<CreatureNeedsComponent>(creatureEntity))
            {
                var needs = entityManager.GetComponentData<CreatureNeedsComponent>(creatureEntity);
                creatureInstance.Happiness = needs.Happiness;
            }
        }
        
        /// <summary>
        /// Get current creature status for debugging
        /// </summary>
        public string GetStatusText()
        {
            if (creatureInstance == null) return "Not initialized";
            
            UpdateFromECS();
            
            string status = $"Species: {creatureDefinition?.speciesName ?? "Unknown"}\n";
            status += $"Age: {creatureInstance.AgeInDays} days\n";
            status += $"Health: {creatureInstance.CurrentHealth}/{creatureDefinition?.baseStats.health ?? 0}\n";
            status += $"Happiness: {creatureInstance.Happiness:F2}\n";
            status += $"Generation: {creatureInstance.GeneticProfile?.Generation ?? 1}\n";
            status += $"Wild: {isWild}\n";
            
            if (IsConvertedToECS)
            {
                status += $"ECS Entity: {creatureEntity.Index}";
            }
            else
            {
                status += "Not converted to ECS";
            }
            
            return status;
        }
        
        private void Log(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[CreatureAuthoring:{name}] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[CreatureAuthoring:{name}] {message}");
        }
        
        private void OnDrawGizmos()
        {
            if (!showGizmosInScene) return;
            
            // Draw creature info
            Gizmos.color = isWild ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 1f);
            
            // Draw genetic purity indicator
            if (creatureInstance?.GeneticProfile != null)
            {
                float purity = creatureInstance.GeneticProfile.GetGeneticPurity();
                Gizmos.color = Color.Lerp(Color.red, Color.blue, purity);
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showGizmosInScene) return;
            
            // Draw detailed creature information
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, GetStatusText());
        }
    }
    
    /// <summary>
    /// Serializable genetic profile data for inspector
    /// </summary>
    [System.Serializable]
    public class GeneticProfileData
    {
        [Header("Core Traits (0-1)")]
        [Range(0f, 1f)] public float strength = 0.5f;
        [Range(0f, 1f)] public float vitality = 0.5f;
        [Range(0f, 1f)] public float agility = 0.5f;
        [Range(0f, 1f)] public float resilience = 0.5f;
        [Range(0f, 1f)] public float intellect = 0.5f;
        [Range(0f, 1f)] public float charm = 0.5f;
        
        [Header("Behavioral Traits (0-1)")]
        [Range(0f, 1f)] public float aggression = 0.3f;
        [Range(0f, 1f)] public float loyalty = 0.7f;
        [Range(0f, 1f)] public float curiosity = 0.5f;
        [Range(0f, 1f)] public float social = 0.5f;
        
        [Header("Special Traits")]
        [Range(0f, 1f)] public float nightVision = 0f;
        [Range(0f, 1f)] public float magicalAffinity = 0f;
        [Range(0f, 1f)] public float packLeader = 0f;
        
        public GeneticProfile ToGeneticProfile()
        {
            var genes = new List<Gene>();
            
            genes.Add(CreateGene("Strength", TraitType.Physical, strength));
            genes.Add(CreateGene("Vitality", TraitType.Physical, vitality));
            genes.Add(CreateGene("Agility", TraitType.Physical, agility));
            genes.Add(CreateGene("Resilience", TraitType.Physical, resilience));
            genes.Add(CreateGene("Intellect", TraitType.Mental, intellect));
            genes.Add(CreateGene("Charm", TraitType.Social, charm));
            genes.Add(CreateGene("Aggression", TraitType.Combat, aggression));
            genes.Add(CreateGene("Loyalty", TraitType.Social, loyalty));
            genes.Add(CreateGene("Curiosity", TraitType.Mental, curiosity));
            genes.Add(CreateGene("Social", TraitType.Social, social));
            
            if (nightVision > 0f)
                genes.Add(CreateGene("Night Vision", TraitType.Sensory, nightVision));
            if (magicalAffinity > 0f)
                genes.Add(CreateGene("Magical Affinity", TraitType.Magical, magicalAffinity));
            if (packLeader > 0f)
                genes.Add(CreateGene("Pack Leader", TraitType.Social, packLeader));
            
            return new GeneticProfile(genes.ToArray(), 1, "custom");
        }
        
        private Gene CreateGene(string name, TraitType type, float value)
        {
            return new Gene
            {
                traitName = name,
                traitType = type,
                dominance = UnityEngine.Random.Range(0.4f, 0.8f),
                value = Mathf.Clamp01(value),
                expression = GeneExpression.Normal,
                isActive = value > 0f
            };
        }
    }
}