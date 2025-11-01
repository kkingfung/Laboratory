using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;
using Laboratory.Core.ECS.Components;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Genetics;
using GeneticProfile = Laboratory.Chimera.Genetics.GeneticProfile;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Core;
using Laboratory.Core.Enums;

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
        [SerializeField] private Laboratory.Core.Enums.BiomeType startingBiome = Laboratory.Core.Enums.BiomeType.Forest;
        
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
            // Create core creature identity component
            var identityComponent = new CreatureIdentityComponent
            {
                Species = creatureDefinition.speciesName,
                CreatureName = creatureDefinition.speciesName,
                UniqueID = (uint)creatureInstance.UniqueId.GetHashCode(),
                Generation = creatureInstance.GeneticProfile?.Generation ?? 1,
                Age = creatureInstance.AgeInDays / 365f, // Convert days to years
                MaxLifespan = creatureDefinition.maxLifespan,
                CurrentLifeStage = DetermineLifeStage(creatureInstance.AgeInDays),
                Rarity = DetermineRarity()
            };
            entityManager.AddComponentData(creatureEntity, identityComponent);

            // Create creature stats component
            var statsComponent = new Laboratory.Core.ECS.CreatureStats
            {
                health = creatureInstance.CurrentHealth,
                maxHealth = creatureDefinition.baseStats.health,
                attack = creatureDefinition.baseStats.attack,
                defense = creatureDefinition.baseStats.defense,
                speed = creatureDefinition.baseStats.speed,
                intelligence = creatureDefinition.baseStats.intelligence,
                charisma = creatureDefinition.baseStats.charisma
            };
            entityManager.AddComponentData(creatureEntity, statsComponent);

            // Create genetics component from genetic profile
            var geneticsComponent = ConvertGeneticProfile(creatureInstance.GeneticProfile);
            entityManager.AddComponentData(creatureEntity, geneticsComponent);

            Log($"Created ECS entity for {creatureInstance.Definition?.speciesName ?? "Unknown"}");
        }
        
        /// <summary>
        /// Convert behavior and personality data to ECS components
        /// </summary>
        private void ConvertBehaviorComponents(EntityManager entityManager)
        {
            // Create AI behavior component
            var aiComponent = new CreatureAIComponent
            {
                CurrentState = AIState.Idle,
                DetectionRange = 10f,
                PatrolRadius = isWild ? 20f : 5f,
                AggressionLevel = GetGeneticTraitValue("Aggression", 0.5f),
                CuriosityLevel = GetGeneticTraitValue("Curiosity", 0.5f),
                LoyaltyLevel = GetGeneticTraitValue("Loyalty", isWild ? 0.2f : 0.8f),
                StateTimer = 0f
            };
            entityManager.AddComponentData(creatureEntity, aiComponent);

            // Create behavior state component
            var behaviorComponent = new BehaviorStateComponent
            {
                CurrentBehavior = CreatureBehaviorType.Idle,
                BehaviorIntensity = 0.5f,
                Stress = isWild ? 0.3f : 0.1f,
                Satisfaction = 0.7f,
                DecisionConfidence = 0.5f
            };
            entityManager.AddComponentData(creatureEntity, behaviorComponent);

            // Create needs component
            var needsComponent = new CreatureNeedsComponent
            {
                Hunger = UnityEngine.Random.Range(0.6f, 0.9f),
                Thirst = UnityEngine.Random.Range(0.7f, 0.9f),
                Energy = UnityEngine.Random.Range(0.5f, 0.8f),
                Comfort = UnityEngine.Random.Range(0.4f, 0.7f),
                Safety = UnityEngine.Random.Range(0.5f, 0.8f),
                SocialConnection = UnityEngine.Random.Range(0.3f, 0.6f),
                BreedingUrge = creatureInstance.IsAdult ? UnityEngine.Random.Range(0.2f, 0.5f) : 0f,
                Exploration = UnityEngine.Random.Range(0.4f, 0.8f),
                HungerDecayRate = 0.01f,
                EnergyRecoveryRate = 0.05f,
                SocialDecayRate = 0.002f
            };
            entityManager.AddComponentData(creatureEntity, needsComponent);

            Log($"Created behavior components at position {transform.position}");
        }
        
        /// <summary>
        /// Convert lifecycle and breeding data to ECS components
        /// </summary>
        private void ConvertLifecycleComponents(EntityManager entityManager)
        {
            // Create breeding component
            var breedingComponent = new BreedingComponent
            {
                Status = creatureInstance.IsAdult ? BreedingStatus.Seeking : BreedingStatus.NotReady,
                BreedingReadiness = creatureInstance.IsAdult ? GetGeneticTraitValue("Fertility", 0.5f) : 0f,
                Selectiveness = GetGeneticTraitValue("Social", 0.5f),
                RequiresTerritory = isWild,
                SeasonalBreeder = UnityEngine.Random.value > 0.5f,
                ParentalInvestment = GetGeneticTraitValue("Loyalty", 0.6f)
            };
            entityManager.AddComponentData(creatureEntity, breedingComponent);

            // Create social territory component
            var socialComponent = new SocialTerritoryComponent
            {
                HasTerritory = isWild,
                TerritoryRadius = isWild ? UnityEngine.Random.Range(10f, 30f) : 0f,
                TerritoryQuality = 0.5f,
                PreferredPackSize = (int)GetGeneticTraitValue("Social", 1f) * 5 + 1,
                PackLoyalty = GetGeneticTraitValue("Loyalty", 0.5f)
            };
            entityManager.AddComponentData(creatureEntity, socialComponent);

            Log("Created lifecycle components");
        }
        
        /// <summary>
        /// Convert environmental data to ECS components
        /// </summary>
        private void ConvertEnvironmentalComponents(EntityManager entityManager)
        {
            // Create environmental component
            var environmentalComponent = new EnvironmentalComponent
            {
                CurrentBiome = startingBiome,
                CurrentPosition = transform.position,
                LocalTemperature = GetBiomeTemperature(startingBiome),
                LocalHumidity = GetBiomeHumidity(startingBiome),
                LocalResourceDensity = 0.7f,
                BiomeComfortLevel = 0.8f, // Start comfortable in native biome
                BiomeAdaptation = 0.9f,
                AdaptationRate = GetGeneticTraitValue("Adaptability", 0.2f),
                HomeRangeRadius = isWild ? UnityEngine.Random.Range(20f, 80f) : 10f,
                ForagingEfficiency = GetGeneticTraitValue("Intelligence", 0.5f),
                ResourceConsumptionRate = 1f / GetGeneticTraitValue("Vitality", 0.5f)
            };
            entityManager.AddComponentData(creatureEntity, environmentalComponent);

            Log($"Created biome component for {startingBiome}");
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
            genes.Add(CreateRandomGene("Strength", TraitType.Strength));
            genes.Add(CreateRandomGene("Vitality", TraitType.Stamina));
            genes.Add(CreateRandomGene("Agility", TraitType.Agility));
            genes.Add(CreateRandomGene("Resilience", TraitType.Strength));
            
            // Mental traits
            genes.Add(CreateRandomGene("Intellect", TraitType.Intelligence));
            genes.Add(CreateRandomGene("Charm", TraitType.Communication));
            
            // Behavioral traits
            genes.Add(CreateRandomGene("Aggression", TraitType.Aggression));
            genes.Add(CreateRandomGene("Loyalty", TraitType.Loyalty));
            genes.Add(CreateRandomGene("Curiosity", TraitType.Curiosity));
            genes.Add(CreateRandomGene("Social", TraitType.Sociability));
            
            // Environmental adaptation traits
            genes.Add(CreateBiomeAdaptationGene(startingBiome));
            
            // Rare traits (10% chance each)
            if (UnityEngine.Random.value < 0.1f)
                genes.Add(CreateRandomGene("Night Vision", TraitType.NightVision));
            
            if (UnityEngine.Random.value < 0.1f)
                genes.Add(CreateRandomGene("Pack Leader", TraitType.Leadership));
            
            if (UnityEngine.Random.value < 0.1f)
                genes.Add(CreateRandomGene("Magical Affinity", TraitType.MagicalAffinity));
            
            return new GeneticProfile(genes.ToArray(), 1, "wild");
        }
        
        /// <summary>
        /// Generate basic genetic profile for bred creatures
        /// </summary>
        private GeneticProfile GenerateBasicGeneticProfile()
        {
            var genes = new List<Gene>();
            
            // More balanced traits for bred creatures
            genes.Add(CreateBalancedGene("Strength", TraitType.Strength, 0.5f));
            genes.Add(CreateBalancedGene("Vitality", TraitType.Stamina, 0.6f));
            genes.Add(CreateBalancedGene("Agility", TraitType.Agility, 0.5f));
            genes.Add(CreateBalancedGene("Resilience", TraitType.Strength, 0.5f));
            genes.Add(CreateBalancedGene("Intellect", TraitType.Intelligence, 0.4f));
            genes.Add(CreateBalancedGene("Charm", TraitType.Communication, 0.4f));
            
            // Higher loyalty for bred creatures
            genes.Add(CreateBalancedGene("Loyalty", TraitType.Loyalty, 0.8f));
            genes.Add(CreateBalancedGene("Aggression", TraitType.Aggression, 0.3f));
            genes.Add(CreateBalancedGene("Curiosity", TraitType.Curiosity, 0.6f));
            genes.Add(CreateBalancedGene("Social", TraitType.Sociability, 0.7f));
            
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
        private Gene CreateBiomeAdaptationGene(Laboratory.Core.Enums.BiomeType biome)
        {
            string traitName = biome switch
            {
                Laboratory.Core.Enums.BiomeType.Desert => "Heat Resistance",
                Laboratory.Core.Enums.BiomeType.Tundra => "Cold Resistance",
                Laboratory.Core.Enums.BiomeType.Ocean => "Swimming",
                Laboratory.Core.Enums.BiomeType.Mountain => "Climbing",
                Laboratory.Core.Enums.BiomeType.Forest => "Camouflage",
                Laboratory.Core.Enums.BiomeType.Volcanic => "Fire Resistance",
                Laboratory.Core.Enums.BiomeType.Swamp => "Poison Resistance",
                Laboratory.Core.Enums.BiomeType.Underground => "Dark Vision",
                Laboratory.Core.Enums.BiomeType.Sky => "Flight",
                _ => "Adaptability"
            };
            
            return new Gene
            {
                traitName = traitName,
                traitType = TraitType.PrimaryColor,
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
            
            // Update happiness from needs component
            if (entityManager.HasComponent<CreatureNeedsComponent>(creatureEntity))
            {
                var needs = entityManager.GetComponentData<CreatureNeedsComponent>(creatureEntity);
                // Calculate happiness from average satisfaction of needs
                var happiness = (needs.Hunger + needs.Thirst + needs.Energy + needs.Comfort + needs.Safety + needs.SocialConnection) / 6f;
                creatureInstance.Happiness = Mathf.Clamp01(happiness);
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
                UnityEngine.Debug.Log($"[CreatureAuthoring:{name}] {message}");
            }
        }
        
        private void LogError(string message)
        {
            UnityEngine.Debug.LogError($"[CreatureAuthoring:{name}] {message}");
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

        // Helper methods for component creation
        private LifeStage DetermineLifeStage(float ageInDays)
        {
            var ageInYears = ageInDays / 365f;
            var maxLifespan = creatureDefinition?.maxLifespan ?? 10f;
            var lifeProgress = ageInYears / maxLifespan;

            return lifeProgress switch
            {
                < 0.1f => LifeStage.Embryo,
                < 0.3f => LifeStage.Juvenile,
                < 0.8f => LifeStage.Adult,
                _ => LifeStage.Elder
            };
        }

        private RarityLevel DetermineRarity()
        {
            var rarityRoll = UnityEngine.Random.value;
            return rarityRoll switch
            {
                < 0.6f => RarityLevel.Common,
                < 0.85f => RarityLevel.Uncommon,
                < 0.95f => RarityLevel.Rare,
                < 0.99f => RarityLevel.Epic,
                _ => RarityLevel.Legendary
            };
        }

        private CreatureGeneticsComponent ConvertGeneticProfile(GeneticProfile profile)
        {
            if (profile == null)
            {
                // Create default genetics
                return new CreatureGeneticsComponent
                {
                    StrengthTrait = 0.5f,
                    VitalityTrait = 0.5f,
                    AgilityTrait = 0.5f,
                    ResilienceTrait = 0.5f,
                    IntellectTrait = 0.5f,
                    CharmTrait = 0.5f
                };
            }

            return new CreatureGeneticsComponent
            {
                StrengthTrait = GetGeneTraitValue(profile, "Strength", 0.5f),
                VitalityTrait = GetGeneTraitValue(profile, "Vitality", 0.5f),
                AgilityTrait = GetGeneTraitValue(profile, "Agility", 0.5f),
                ResilienceTrait = GetGeneTraitValue(profile, "Resilience", 0.5f),
                IntellectTrait = GetGeneTraitValue(profile, "Intellect", 0.5f),
                CharmTrait = GetGeneTraitValue(profile, "Charm", 0.5f)
            };
        }

        private float GetGeneTraitValue(GeneticProfile profile, string traitName, float defaultValue)
        {
            if (profile?.Genes == null) return defaultValue;

            foreach (var gene in profile.Genes)
            {
                if (gene.traitName == traitName && gene.isActive)
                {
                    return gene.value ?? defaultValue;
                }
            }
            return defaultValue;
        }

        private float GetGeneticTraitValue(string traitName, float defaultValue)
        {
            return GetGeneTraitValue(creatureInstance?.GeneticProfile, traitName, defaultValue);
        }


        private float GetBiomeTemperature(Laboratory.Core.Enums.BiomeType biome)
        {
            return biome switch
            {
                Laboratory.Core.Enums.BiomeType.Desert => 40f,
                Laboratory.Core.Enums.BiomeType.Tundra => -15f,
                Laboratory.Core.Enums.BiomeType.Volcanic => 60f,
                Laboratory.Core.Enums.BiomeType.Mountain => 5f,
                Laboratory.Core.Enums.BiomeType.Ocean => 15f,
                Laboratory.Core.Enums.BiomeType.Underground => 12f,
                Laboratory.Core.Enums.BiomeType.Sky => 0f,
                Laboratory.Core.Enums.BiomeType.Forest => 20f,
                Laboratory.Core.Enums.BiomeType.Swamp => 25f,
                _ => 18f
            };
        }

        private float GetBiomeHumidity(Laboratory.Core.Enums.BiomeType biome)
        {
            return biome switch
            {
                Laboratory.Core.Enums.BiomeType.Desert => 0.1f,
                Laboratory.Core.Enums.BiomeType.Ocean => 1f,
                Laboratory.Core.Enums.BiomeType.Swamp => 0.9f,
                Laboratory.Core.Enums.BiomeType.Forest => 0.7f,
                Laboratory.Core.Enums.BiomeType.Tundra => 0.3f,
                Laboratory.Core.Enums.BiomeType.Volcanic => 0.2f,
                Laboratory.Core.Enums.BiomeType.Underground => 0.6f,
                Laboratory.Core.Enums.BiomeType.Sky => 0.4f,
                _ => 0.5f
            };
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
            
            genes.Add(CreateGene("Strength", TraitType.Strength, strength));
            genes.Add(CreateGene("Vitality", TraitType.Stamina, vitality));
            genes.Add(CreateGene("Agility", TraitType.Agility, agility));
            genes.Add(CreateGene("Resilience", TraitType.Strength, resilience));
            genes.Add(CreateGene("Intellect", TraitType.Intelligence, intellect));
            genes.Add(CreateGene("Charm", TraitType.Communication, charm));
            genes.Add(CreateGene("Aggression", TraitType.Aggression, aggression));
            genes.Add(CreateGene("Loyalty", TraitType.Loyalty, loyalty));
            genes.Add(CreateGene("Curiosity", TraitType.Curiosity, curiosity));
            genes.Add(CreateGene("Social", TraitType.Sociability, social));
            
            if (nightVision > 0f)
                genes.Add(CreateGene("Night Vision", TraitType.NightVision, nightVision));
            if (magicalAffinity > 0f)
                genes.Add(CreateGene("Magical Affinity", TraitType.MagicalAffinity, magicalAffinity));
            if (packLeader > 0f)
                genes.Add(CreateGene("Pack Leader", TraitType.Leadership, packLeader));
            
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