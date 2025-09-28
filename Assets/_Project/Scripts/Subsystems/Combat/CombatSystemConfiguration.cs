using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Core;
using Laboratory.Chimera.Core;
using CreatureGeneticsComponent = Laboratory.Chimera.ECS.CreatureGeneticsComponent;
using Laboratory.Subsystems.Combat.Advanced;

namespace Laboratory.Subsystems.Combat
{
    /// <summary>
    /// Combat System Configuration - ScriptableObject-based combat settings
    /// PURPOSE: Designer-friendly configuration for all combat mechanics and balancing
    /// FEATURES: Genetic combat scaling, elemental interactions, formation tactics, difficulty scaling
    /// ARCHITECTURE: Integrates with ChimeraGameConfig and existing combat systems
    /// </summary>

    [CreateAssetMenu(fileName = "CombatSystemConfig", menuName = "Laboratory/Combat/Combat System Configuration")]
    public class CombatSystemConfiguration : ScriptableObject
    {
        [Header("Core Combat Settings")]
        [Tooltip("Base damage multiplier for all combat")]
        [Range(0.1f, 5f)]
        public float baseDamageMultiplier = 1f;

        [Tooltip("Base health multiplier for all creatures")]
        [Range(0.1f, 10f)]
        public float baseHealthMultiplier = 1f;

        [Tooltip("Combat range for auto-targeting")]
        [Range(5f, 100f)]
        public float combatRange = 25f;

        [Tooltip("Time to exit combat after last action")]
        [Range(1f, 30f)]
        public float combatTimeout = 8f;

        [Header("Genetic Combat Scaling")]
        [Tooltip("How much genetics affect combat abilities")]
        [Range(0f, 2f)]
        public float geneticInfluenceStrength = 1.2f;

        [Tooltip("Genetic purity combat bonus")]
        [Range(0f, 1f)]
        public float geneticPurityBonus = 0.3f;

        [Tooltip("Generation scaling factor")]
        [Range(0f, 0.1f)]
        public float generationScaling = 0.03f;

        [Tooltip("Shiny creature combat bonus")]
        [Range(0f, 2f)]
        public float shinyCombatBonus = 0.5f;

        [Header("Elemental Combat System")]
        [SerializeField]
        private ElementalInteractionMatrix elementalInteractions = new ElementalInteractionMatrix();

        [Header("Formation Combat")]
        [Tooltip("Enable formation combat bonuses")]
        public bool enableFormationCombat = true;

        [Tooltip("Maximum formation bonus")]
        [Range(0f, 2f)]
        public float maxFormationBonus = 0.8f;

        [Tooltip("Formation cohesion decay rate")]
        [Range(0.01f, 1f)]
        public float cohesionDecayRate = 0.1f;

        [Tooltip("Leadership range for formation commands")]
        [Range(5f, 50f)]
        public float leadershipRange = 20f;

        [Header("Status Effects")]
        [SerializeField]
        private StatusEffectSettings[] statusEffectConfigs = new StatusEffectSettings[]
        {
            new StatusEffectSettings
            {
                effectType = StatusEffectType.Burning,
                baseDamage = 10f,
                baseDuration = 5f,
                stackable = true,
                maxStacks = 5
            },
            new StatusEffectSettings
            {
                effectType = StatusEffectType.Poison,
                baseDamage = 5f,
                baseDuration = 8f,
                stackable = true,
                maxStacks = 3
            },
            new StatusEffectSettings
            {
                effectType = StatusEffectType.Strengthened,
                baseDamage = 0f,
                baseDuration = 10f,
                stackable = false,
                maxStacks = 1
            }
        };

        [Header("Combat Specializations")]
        [SerializeField]
        private SpecializationSettings[] specializationConfigs = new SpecializationSettings[]
        {
            new SpecializationSettings
            {
                specialization = CombatSpecialization.Berserker,
                damageMultiplier = 1.5f,
                defenseMultiplier = 0.7f,
                speedMultiplier = 1.2f,
                healthMultiplier = 0.8f,
                description = "High damage, low defense glass cannon"
            },
            new SpecializationSettings
            {
                specialization = CombatSpecialization.Tank,
                damageMultiplier = 0.8f,
                defenseMultiplier = 2f,
                speedMultiplier = 0.7f,
                healthMultiplier = 1.5f,
                description = "High defense, high health protector"
            },
            new SpecializationSettings
            {
                specialization = CombatSpecialization.Assassin,
                damageMultiplier = 1.8f,
                defenseMultiplier = 0.6f,
                speedMultiplier = 1.8f,
                healthMultiplier = 0.6f,
                description = "High damage, high speed, very fragile"
            }
        };

        [Header("AI Combat Settings")]
        [Tooltip("AI reaction time in seconds")]
        [Range(0.1f, 2f)]
        public float aiReactionTime = 0.5f;

        [Tooltip("AI tactical intelligence scaling")]
        [Range(0.1f, 3f)]
        public float aiIntelligenceScaling = 1f;

        [Tooltip("AI aggression randomness")]
        [Range(0f, 1f)]
        public float aiAggressionVariance = 0.2f;

        [Header("Multiplayer Combat")]
        [Tooltip("Enable lag compensation")]
        public bool enableLagCompensation = true;

        [Tooltip("Prediction confidence threshold")]
        [Range(0.1f, 1f)]
        public float predictionThreshold = 0.7f;

        [Tooltip("Combat state sync rate (Hz)")]
        [Range(5, 60)]
        public int combatSyncRate = 30;

        [Header("Difficulty Scaling")]
        [Tooltip("Player level scaling curve")]
        public AnimationCurve playerLevelScaling = AnimationCurve.Linear(1f, 1f, 100f, 3f);

        [Tooltip("Enemy difficulty multiplier")]
        [Range(0.1f, 5f)]
        public float enemyDifficultyMultiplier = 1f;

        [Tooltip("Group size damage scaling")]
        [Range(0f, 2f)]
        public float groupSizeScaling = 0.1f;

        [Header("Performance Settings")]
        [Tooltip("Maximum active combat entities")]
        [Range(50, 2000)]
        public int maxActiveCombatEntities = 500;

        [Tooltip("Combat update frequency (lower = better performance)")]
        [Range(1, 10)]
        public int combatUpdateFrequency = 3; // Every 3rd frame

        [Tooltip("Status effect processing batch size")]
        [Range(10, 200)]
        public int statusEffectBatchSize = 50;

        /// <summary>
        /// Get elemental damage multiplier between two elements
        /// </summary>
        public float GetElementalMultiplier(ElementalAffinity attacker, ElementalAffinity defender)
        {
            return elementalInteractions.GetMultiplier(attacker, defender);
        }

        /// <summary>
        /// Get status effect configuration by type
        /// </summary>
        public StatusEffectSettings GetStatusEffectConfig(StatusEffectType effectType)
        {
            foreach (var config in statusEffectConfigs)
            {
                if (config.effectType == effectType)
                    return config;
            }
            return default;
        }

        /// <summary>
        /// Get specialization configuration by type
        /// </summary>
        public SpecializationSettings GetSpecializationConfig(CombatSpecialization specialization)
        {
            foreach (var config in specializationConfigs)
            {
                if (config.specialization == specialization)
                    return config;
            }
            return default;
        }

        /// <summary>
        /// Calculate genetic combat modifier based on creature genetics
        /// </summary>
        public float CalculateGeneticCombatModifier(float traitValue, float geneticPurity, int generation, bool isShiny)
        {
            float baseModifier = traitValue * geneticInfluenceStrength;
            float purityBonus = geneticPurity * geneticPurityBonus;
            float generationBonus = generation * generationScaling;
            float shinyBonus = isShiny ? shinyCombatBonus : 0f;

            return baseModifier + purityBonus + generationBonus + shinyBonus;
        }

        /// <summary>
        /// Calculate player level scaling factor
        /// </summary>
        public float GetPlayerLevelScaling(int playerLevel)
        {
            return playerLevelScaling.Evaluate(playerLevel);
        }

        /// <summary>
        /// Validate configuration settings
        /// </summary>
        public bool ValidateConfiguration(out string[] errors)
        {
            var errorList = new System.Collections.Generic.List<string>();

            // Validate basic settings
            if (baseDamageMultiplier <= 0)
                errorList.Add("Base damage multiplier must be greater than 0");

            if (baseHealthMultiplier <= 0)
                errorList.Add("Base health multiplier must be greater than 0");

            if (combatRange <= 0)
                errorList.Add("Combat range must be greater than 0");

            // Validate elemental interactions
            if (!elementalInteractions.IsValid())
                errorList.Add("Elemental interaction matrix is invalid");

            // Validate status effect configs
            if (statusEffectConfigs == null || statusEffectConfigs.Length == 0)
                errorList.Add("At least one status effect configuration is required");

            // Validate specialization configs
            if (specializationConfigs == null || specializationConfigs.Length == 0)
                errorList.Add("At least one specialization configuration is required");

            // Check for duplicate specializations
            var specializationTypes = new System.Collections.Generic.HashSet<CombatSpecialization>();
            foreach (var config in specializationConfigs)
            {
                if (!specializationTypes.Add(config.specialization))
                    errorList.Add($"Duplicate specialization configuration: {config.specialization}");
            }

            errors = errorList.ToArray();
            return errorList.Count == 0;
        }

        /// <summary>
        /// Reset to default values
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            baseDamageMultiplier = 1f;
            baseHealthMultiplier = 1f;
            combatRange = 25f;
            combatTimeout = 8f;
            geneticInfluenceStrength = 1.2f;
            geneticPurityBonus = 0.3f;
            generationScaling = 0.03f;
            shinyCombatBonus = 0.5f;
            enableFormationCombat = true;
            maxFormationBonus = 0.8f;
            cohesionDecayRate = 0.1f;
            leadershipRange = 20f;
            aiReactionTime = 0.5f;
            aiIntelligenceScaling = 1f;
            aiAggressionVariance = 0.2f;
            enableLagCompensation = true;
            predictionThreshold = 0.7f;
            combatSyncRate = 30;
            enemyDifficultyMultiplier = 1f;
            groupSizeScaling = 0.1f;
            maxActiveCombatEntities = 500;
            combatUpdateFrequency = 3;
            statusEffectBatchSize = 50;

            // Reset curves
            playerLevelScaling = AnimationCurve.Linear(1f, 1f, 100f, 3f);

            // Reset elemental interactions to defaults
            elementalInteractions = new ElementalInteractionMatrix();
            elementalInteractions.InitializeDefaults();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure values stay within valid ranges
            baseDamageMultiplier = Mathf.Max(0.1f, baseDamageMultiplier);
            baseHealthMultiplier = Mathf.Max(0.1f, baseHealthMultiplier);
            combatRange = Mathf.Max(1f, combatRange);
            combatTimeout = Mathf.Max(1f, combatTimeout);
            geneticInfluenceStrength = Mathf.Max(0f, geneticInfluenceStrength);
        }
#endif
    }

    [System.Serializable]
    public struct ElementalInteractionMatrix
    {
        [SerializeField] private float[] interactions; // 11x11 matrix flattened

        /// <summary>
        /// Get damage multiplier for attacker element vs defender element
        /// </summary>
        public float GetMultiplier(ElementalAffinity attacker, ElementalAffinity defender)
        {
            if (interactions == null || interactions.Length != 121) // 11x11
                return 1f;

            int index = (int)attacker * 11 + (int)defender;
            return index < interactions.Length ? interactions[index] : 1f;
        }

        /// <summary>
        /// Set damage multiplier for attacker element vs defender element
        /// </summary>
        public void SetMultiplier(ElementalAffinity attacker, ElementalAffinity defender, float multiplier)
        {
            if (interactions == null)
                interactions = new float[121];

            int index = (int)attacker * 11 + (int)defender;
            if (index < interactions.Length)
                interactions[index] = multiplier;
        }

        /// <summary>
        /// Initialize with default elemental interactions
        /// </summary>
        public void InitializeDefaults()
        {
            interactions = new float[121];

            // Fill with neutral 1.0f interactions
            for (int i = 0; i < interactions.Length; i++)
                interactions[i] = 1f;

            // Set up classic elemental weaknesses/strengths
            SetMultiplier(ElementalAffinity.Fire, ElementalAffinity.Ice, 2f);     // Fire > Ice
            SetMultiplier(ElementalAffinity.Fire, ElementalAffinity.Nature, 2f);  // Fire > Nature
            SetMultiplier(ElementalAffinity.Fire, ElementalAffinity.Water, 0.5f); // Fire < Water

            SetMultiplier(ElementalAffinity.Water, ElementalAffinity.Fire, 2f);   // Water > Fire
            SetMultiplier(ElementalAffinity.Water, ElementalAffinity.Earth, 2f);  // Water > Earth
            SetMultiplier(ElementalAffinity.Water, ElementalAffinity.Lightning, 0.5f); // Water < Lightning

            SetMultiplier(ElementalAffinity.Earth, ElementalAffinity.Lightning, 2f); // Earth > Lightning
            SetMultiplier(ElementalAffinity.Earth, ElementalAffinity.Air, 2f);       // Earth > Air
            SetMultiplier(ElementalAffinity.Earth, ElementalAffinity.Water, 0.5f);   // Earth < Water

            SetMultiplier(ElementalAffinity.Air, ElementalAffinity.Earth, 0.5f);     // Air < Earth
            SetMultiplier(ElementalAffinity.Air, ElementalAffinity.Fire, 1.5f);      // Air > Fire

            SetMultiplier(ElementalAffinity.Lightning, ElementalAffinity.Water, 2f); // Lightning > Water
            SetMultiplier(ElementalAffinity.Lightning, ElementalAffinity.Earth, 0.5f); // Lightning < Earth

            SetMultiplier(ElementalAffinity.Ice, ElementalAffinity.Nature, 1.5f);    // Ice > Nature
            SetMultiplier(ElementalAffinity.Ice, ElementalAffinity.Fire, 0.5f);      // Ice < Fire

            SetMultiplier(ElementalAffinity.Nature, ElementalAffinity.Earth, 1.5f);  // Nature > Earth
            SetMultiplier(ElementalAffinity.Nature, ElementalAffinity.Fire, 0.5f);   // Nature < Fire

            SetMultiplier(ElementalAffinity.Light, ElementalAffinity.Shadow, 2f);    // Light > Shadow
            SetMultiplier(ElementalAffinity.Shadow, ElementalAffinity.Light, 2f);    // Shadow > Light

            SetMultiplier(ElementalAffinity.Chaos, ElementalAffinity.None, 1.5f);    // Chaos > None
        }

        /// <summary>
        /// Validate that the matrix is properly initialized
        /// </summary>
        public bool IsValid()
        {
            return interactions != null && interactions.Length == 121;
        }
    }

    [System.Serializable]
    public struct StatusEffectSettings
    {
        [Tooltip("Type of status effect")]
        public StatusEffectType effectType;

        [Tooltip("Base damage/effect per tick")]
        [Range(0f, 100f)]
        public float baseDamage;

        [Tooltip("Base duration in seconds")]
        [Range(0.1f, 60f)]
        public float baseDuration;

        [Tooltip("Can this effect stack?")]
        public bool stackable;

        [Tooltip("Maximum number of stacks")]
        [Range(1, 10)]
        public int maxStacks;

        [Tooltip("Tick interval for periodic effects")]
        [Range(0.1f, 5f)]
        public float tickInterval;

        [Tooltip("Effect intensity curve over duration")]
        public AnimationCurve intensityCurve;

        [Tooltip("Visual effect prefab")]
        public GameObject visualEffect;
    }

    [System.Serializable]
    public struct SpecializationSettings
    {
        [Tooltip("Combat specialization type")]
        public CombatSpecialization specialization;

        [Tooltip("Damage output modifier")]
        [Range(0.1f, 5f)]
        public float damageMultiplier;

        [Tooltip("Damage resistance modifier")]
        [Range(0.1f, 5f)]
        public float defenseMultiplier;

        [Tooltip("Movement and attack speed modifier")]
        [Range(0.1f, 3f)]
        public float speedMultiplier;

        [Tooltip("Health points modifier")]
        [Range(0.1f, 5f)]
        public float healthMultiplier;

        [Tooltip("Energy/mana modifier")]
        [Range(0.1f, 3f)]
        public float energyMultiplier;

        [Tooltip("Special abilities available to this specialization")]
        public string[] specialAbilities;

        [Tooltip("Description of specialization")]
        [TextArea(2, 4)]
        public string description;
    }

    /// <summary>
    /// Combat System Authoring Component for scene integration
    /// </summary>
    public class CombatSystemAuthoring : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Combat system configuration asset")]
        public CombatSystemConfiguration combatConfig;

        [Tooltip("Auto-initialize combat systems on start")]
        public bool autoInitialize = true;

        [Header("Entity Assignment")]
        [Tooltip("Assign genetic combat abilities to creatures")]
        public bool assignGeneticAbilities = true;

        [Tooltip("Enable formation combat")]
        public bool enableFormationCombat = true;

        [Tooltip("Enable tactical AI")]
        public bool enableTacticalAI = true;

        [Tooltip("Enable multiplayer sync")]
        public bool enableMultiplayerSync = false;

        private void Start()
        {
            if (autoInitialize && combatConfig != null)
            {
                InitializeCombatSystems();
            }
        }

        /// <summary>
        /// Initialize combat systems with current configuration
        /// </summary>
        public void InitializeCombatSystems()
        {
            if (combatConfig == null)
            {
                Debug.LogError("Combat configuration is null! Please assign a configuration asset.");
                return;
            }

            // Validate configuration
            if (!combatConfig.ValidateConfiguration(out string[] errors))
            {
                Debug.LogError($"Combat configuration validation failed: {string.Join(", ", errors)}");
                return;
            }

            // Get ECS world
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("No ECS World found! Make sure Entities package is properly configured.");
                return;
            }

            // Initialize combat systems
            InitializeGeneticCombatSystem(world);

            if (enableFormationCombat)
                InitializeFormationCombatSystem(world);

            if (enableTacticalAI)
                InitializeTacticalAISystem(world);

            if (enableMultiplayerSync)
                InitializeMultiplayerCombatSystem(world);

            Debug.Log("Combat systems initialized successfully!");
        }

        private void InitializeGeneticCombatSystem(World world)
        {
            var system = world.GetOrCreateSystemManaged<GeneticCombatAbilitySystem>();
            // Configure system with our settings (would need system configuration API)
            Debug.Log("Genetic Combat Ability System initialized");
        }

        private void InitializeFormationCombatSystem(World world)
        {
            var system = world.GetOrCreateSystemManaged<FormationCombatSystem>();
            Debug.Log("Formation Combat System initialized");
        }

        private void InitializeTacticalAISystem(World world)
        {
            var system = world.GetOrCreateSystemManaged<TacticalCombatAISystem>();
            Debug.Log("Tactical Combat AI System initialized");
        }

        private void InitializeMultiplayerCombatSystem(World world)
        {
            var system = world.GetOrCreateSystemManaged<MultiplayerCombatSyncSystem>();
            Debug.Log("Multiplayer Combat Sync System initialized");
        }

        /// <summary>
        /// Assign combat components to an entity based on genetic data
        /// </summary>
        public void AssignCombatComponentsToEntity(Entity entity, CreatureGeneticsComponent genetics)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;

            // Add genetic combat abilities
            if (assignGeneticAbilities)
            {
                var combatAbilities = new GeneticCombatAbilities
                {
                    strengthMultiplier = combatConfig.CalculateGeneticCombatModifier(
                        genetics.StrengthTrait, genetics.GeneticPurity, genetics.Generation, genetics.IsShiny),
                    agilityMultiplier = combatConfig.CalculateGeneticCombatModifier(
                        genetics.AgilityTrait, genetics.GeneticPurity, genetics.Generation, genetics.IsShiny),
                    intellectMultiplier = combatConfig.CalculateGeneticCombatModifier(
                        genetics.IntellectTrait, genetics.GeneticPurity, genetics.Generation, genetics.IsShiny),
                    resilienceMultiplier = combatConfig.CalculateGeneticCombatModifier(
                        genetics.ResilienceTrait, genetics.GeneticPurity, genetics.Generation, genetics.IsShiny),
                    vitalityMultiplier = combatConfig.CalculateGeneticCombatModifier(
                        genetics.VitalityTrait, genetics.GeneticPurity, genetics.Generation, genetics.IsShiny),
                    charmMultiplier = combatConfig.CalculateGeneticCombatModifier(
                        genetics.CharmTrait, genetics.GeneticPurity, genetics.Generation, genetics.IsShiny),
                    geneticAbilityHash = (uint)genetics.LineageId.GetHashCode(),
                    adaptationLevel = 0.5f // Will be updated by environmental system
                };

                entityManager.AddComponentData(entity, combatAbilities);
            }

            // Add formation combat component
            if (enableFormationCombat)
            {
                var formation = new FormationCombatComponent
                {
                    formationLeader = Entity.Null,
                    formationType = FormationType.None,
                    formationPosition = float3.zero,
                    formationBonus = 0f,
                    cohesionLevel = 1f,
                    isFormationLeader = false,
                    formationSize = 1,
                    leadershipRange = combatConfig.leadershipRange
                };

                entityManager.AddComponentData(entity, formation);
            }

            // Add tactical AI
            if (enableTacticalAI)
            {
                var tacticalAI = new TacticalCombatAI
                {
                    personality = (TacticalPersonality)UnityEngine.Random.Range(0, 7),
                    aggressionLevel = UnityEngine.Random.Range(0.3f, 0.8f),
                    cooperationLevel = UnityEngine.Random.Range(0.2f, 0.9f),
                    adaptabilityLevel = genetics.IntellectTrait,
                    preferredStrategy = CombatStrategy.DirectAssault,
                    tacticalIntelligence = genetics.IntellectTrait * combatConfig.aiIntelligenceScaling,
                    primaryTarget = Entity.Null,
                    secondaryTarget = Entity.Null,
                    threatAssessment = 0.5f,
                    lastStrategyChange = 0f
                };

                entityManager.AddComponentData(entity, tacticalAI);
            }

            // Add status effect buffer
            entityManager.AddBuffer<StatusEffectComponent>(entity);

            Debug.Log($"Combat components assigned to entity with genetics: {genetics.LineageId}");
        }

#if UNITY_EDITOR
        [ContextMenu("Create Default Configuration")]
        private void CreateDefaultConfiguration()
        {
            var config = ScriptableObject.CreateInstance<CombatSystemConfiguration>();
            config.ResetToDefaults();

            UnityEditor.AssetDatabase.CreateAsset(config, "Assets/_Project/Settings/DefaultCombatConfig.asset");
            UnityEditor.AssetDatabase.SaveAssets();

            combatConfig = config;
        }
#endif
    }
}