using UnityEngine;
using UnityEngine.AI;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera;
using Laboratory.Chimera.Creatures;

namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// Helper script to automatically setup Chimera monsters with AI components.
    /// Attach this to any monster prefab or GameObject to configure it for AI behavior.
    /// </summary>
    public class ChimeraMonsterSetup : MonoBehaviour
    {
        [Header("Auto-Setup Configuration")]
        [SerializeField] private bool setupOnAwake = true;
        [SerializeField] private AIBehaviorType defaultBehavior = AIBehaviorType.Companion;
        [SerializeField] private bool addToAIManager = true;

        [Header("AI Parameters")]
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float followDistance = 3f;

        [Header("Detection Settings")]
        [SerializeField] private bool enableAdvancedDetection = true;
        [SerializeField] private bool enableHearing = true;
        [SerializeField] private bool enableSmell = false;

        [Header("Animation")]
        [SerializeField] private RuntimeAnimatorController animatorController;

        private void Awake()
        {
            if (setupOnAwake)
            {
                SetupMonsterAI();
            }
        }

        [ContextMenu("Setup Monster AI")]
        public void SetupMonsterAI()
        {
            UnityEngine.Debug.Log($"[MonsterSetup] Setting up AI for {gameObject.name}");

            // Ensure NavMeshAgent exists
            SetupNavMeshAgent();

            // Ensure Animator exists
            SetupAnimator();

            // Setup main AI controller
            SetupAIController();

            // Setup advanced detection if requested
            if (enableAdvancedDetection)
            {
                SetupDetectionSystem();
            }

            // Setup creature instance if it doesn't exist
            SetupCreatureInstance();

            // Add to AI Manager if requested
            if (addToAIManager)
            {
                RegisterWithAIManager();
            }

            UnityEngine.Debug.Log($"[MonsterSetup] ✅ AI setup complete for {gameObject.name}");
        }

        private void SetupNavMeshAgent()
        {
            var navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<NavMeshAgent>();
                UnityEngine.Debug.Log("[MonsterSetup] Added NavMeshAgent component");
            }

            // Configure NavMeshAgent with reasonable defaults
            navAgent.speed = 3.5f;
            navAgent.angularSpeed = 120f;
            navAgent.acceleration = 8f;
            navAgent.stoppingDistance = attackRange * 0.8f;
            navAgent.radius = 0.5f;
            navAgent.height = 2f;
        }

        private void SetupAnimator()
        {
            var animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = gameObject.AddComponent<Animator>();
                UnityEngine.Debug.Log("[MonsterSetup] Added Animator component");
            }

            if (animatorController != null && animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = animatorController;
            }
        }

        private void SetupAIController()
        {
            var aiController = GetComponent<ChimeraMonsterAI>();
            if (aiController == null)
            {
                aiController = gameObject.AddComponent<ChimeraMonsterAI>();
                UnityEngine.Debug.Log("[MonsterSetup] Added ChimeraMonsterAI component");

                // Configure AI parameters using reflection since the fields are private
                var aiType = typeof(ChimeraMonsterAI);
                var detectionRangeField = aiType.GetField("detectionRange", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var attackRangeField = aiType.GetField("attackRange", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var followDistanceField = aiType.GetField("followDistance", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                detectionRangeField?.SetValue(aiController, detectionRange);
                attackRangeField?.SetValue(aiController, attackRange);
                followDistanceField?.SetValue(aiController, followDistance);
            }

            // Set behavior type
            aiController.SetBehaviorType(defaultBehavior);
        }

        private void SetupDetectionSystem()
        {
            var detectionSystem = GetComponent<EnemyDetectionSystem>();
            if (detectionSystem == null)
            {
                detectionSystem = gameObject.AddComponent<EnemyDetectionSystem>();
                UnityEngine.Debug.Log("[MonsterSetup] Added EnemyDetectionSystem component");

                // Configure detection parameters using reflection
                var detectionType = typeof(EnemyDetectionSystem);
                var visionRangeField = detectionType.GetField("visionRange", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var enableHearingField = detectionType.GetField("enableHearing", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var enableSmellField = detectionType.GetField("enableSmell", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                visionRangeField?.SetValue(detectionSystem, detectionRange);
                enableHearingField?.SetValue(detectionSystem, enableHearing);
                enableSmellField?.SetValue(detectionSystem, enableSmell);
            }
        }

        private void SetupCreatureInstance()
        {
            // Check if CreatureInstanceComponent already exists
            var existingComponent = GetComponent<CreatureInstanceComponent>();
            if (existingComponent != null)
            {
                UnityEngine.Debug.Log("[MonsterSetup] CreatureInstanceComponent already exists");
                return;
            }
            
            // Add CreatureInstanceComponent for breeding system integration
            var creatureComponent = gameObject.AddComponent<CreatureInstanceComponent>();
            UnityEngine.Debug.Log("[MonsterSetup] Added CreatureInstanceComponent");
            
            // Create a basic creature definition if none exists
            var creatureDefinition = CreateBasicCreatureDefinition();
            
            // Create basic genetic profile
            var basicGenetics = CreateBasicGeneticProfile();
            
            // Create creature instance data
            var creatureInstance = new Laboratory.Chimera.Breeding.CreatureInstance(creatureDefinition, basicGenetics)
            {
                AgeInDays = Random.Range(90, 200), // Random adult age
                CurrentHealth = creatureDefinition.baseStats.health,
                Happiness = Random.Range(0.6f, 0.9f),
                Level = 1,
                IsWild = true,
                BirthDate = System.DateTime.UtcNow.AddDays(-Random.Range(90, 200))
            };
            
            // Initialize the component with the creature data
            creatureComponent.InitializeFromInstance(creatureInstance);
            
            UnityEngine.Debug.Log("[MonsterSetup] Integrated with breeding system successfully");
        }
        
        private Laboratory.Chimera.Genetics.GeneticProfile CreateBasicGeneticProfile()
        {
            // Create diverse genetic traits for interesting AI behavior
            var genes = new Laboratory.Chimera.Genetics.Gene[]
            {
                new Laboratory.Chimera.Genetics.Gene 
                { 
                    traitName = "Aggression", 
                    traitType = Laboratory.Chimera.Genetics.TraitType.Mental,
                    value = Random.Range(0.2f, 0.8f), 
                    dominance = Random.Range(0.3f, 0.7f), 
                    isActive = true 
                },
                new Laboratory.Chimera.Genetics.Gene 
                { 
                    traitName = "Intelligence", 
                    traitType = Laboratory.Chimera.Genetics.TraitType.Mental,
                    value = Random.Range(0.3f, 0.9f), 
                    dominance = Random.Range(0.4f, 0.8f), 
                    isActive = true 
                },
                new Laboratory.Chimera.Genetics.Gene 
                { 
                    traitName = "Loyalty", 
                    traitType = Laboratory.Chimera.Genetics.TraitType.Social,
                    value = Random.Range(0.4f, 0.9f), 
                    dominance = Random.Range(0.3f, 0.7f), 
                    isActive = true 
                },
                new Laboratory.Chimera.Genetics.Gene 
                { 
                    traitName = "Curiosity", 
                    traitType = Laboratory.Chimera.Genetics.TraitType.Mental,
                    value = Random.Range(0.2f, 0.8f), 
                    dominance = Random.Range(0.2f, 0.6f), 
                    isActive = true 
                },
                new Laboratory.Chimera.Genetics.Gene 
                { 
                    traitName = "Speed", 
                    traitType = Laboratory.Chimera.Genetics.TraitType.Physical,
                    value = Random.Range(0.3f, 0.9f), 
                    dominance = Random.Range(0.4f, 0.8f), 
                    isActive = true 
                },
                new Laboratory.Chimera.Genetics.Gene 
                { 
                    traitName = "Strength", 
                    traitType = Laboratory.Chimera.Genetics.TraitType.Physical,
                    value = Random.Range(0.3f, 0.9f), 
                    dominance = Random.Range(0.4f, 0.8f), 
                    isActive = true 
                },
                new Laboratory.Chimera.Genetics.Gene 
                { 
                    traitName = "Size", 
                    traitType = Laboratory.Chimera.Genetics.TraitType.Physical,
                    value = Random.Range(0.4f, 0.8f), 
                    dominance = Random.Range(0.5f, 0.9f), 
                    isActive = true 
                },
                new Laboratory.Chimera.Genetics.Gene 
                { 
                    traitName = "Color", 
                    traitType = Laboratory.Chimera.Genetics.TraitType.Physical,
                    value = Random.Range(0.0f, 1.0f), // Full hue range for colors
                    dominance = Random.Range(0.3f, 0.7f), 
                    isActive = true 
                }
            };
            
            return new Laboratory.Chimera.Genetics.GeneticProfile(genes, 1);
        }

        private CreatureDefinition CreateBasicCreatureDefinition()
        {
            var definition = ScriptableObject.CreateInstance<CreatureDefinition>();
            definition.speciesName = gameObject.name + " Species";
            definition.size = CreatureSize.Medium;
            definition.breedingCompatibilityGroup = 1;
            definition.fertilityRate = 0.75f;
            definition.maturationAge = 90;
            definition.maxLifespan = 365 * 5;
            definition.baseStats = new CreatureStats
            {
                health = 100,
                attack = 20,
                defense = 15,
                speed = 10,
                intelligence = 5,
                charisma = 5
            };

            return definition;
        }

        private void RegisterWithAIManager()
        {
            var aiManager = FindFirstObjectByType<ChimeraAIManager>();
            if (aiManager == null)
            {
                // Create AI Manager if it doesn't exist
                var managerGO = new GameObject("Chimera AI Manager");
                aiManager = managerGO.AddComponent<ChimeraAIManager>();
                UnityEngine.Debug.Log("[MonsterSetup] Created ChimeraAIManager");
            }

            var monsterAI = GetComponent<ChimeraMonsterAI>();
            if (monsterAI != null)
            {
                aiManager.RegisterMonster(monsterAI);
            }
        }

        #region Utility Methods

        /// <summary>
        /// Setup multiple monsters at once
        /// </summary>
        [ContextMenu("Setup All Monsters in Scene")]
        public void SetupAllMonstersInScene()
        {
            var allSetupScripts = FindObjectsByType<ChimeraMonsterSetup>(FindObjectsSortMode.None);
            foreach (var setup in allSetupScripts)
            {
                setup.SetupMonsterAI();
            }
            UnityEngine.Debug.Log($"[MonsterSetup] Setup complete for {allSetupScripts.Length} monsters");
        }

        /// <summary>
        /// Remove all AI components (for cleanup)
        /// </summary>
        [ContextMenu("Remove AI Components")]
        public void RemoveAIComponents()
        {
            var components = new System.Type[]
            {
                typeof(ChimeraMonsterAI),
                typeof(EnemyDetectionSystem),
                typeof(NavMeshAgent),
                typeof(CreatureInstanceComponent) // Updated to use the correct component
            };

            foreach (var componentType in components)
            {
                var component = GetComponent(componentType);
                if (component != null)
                {
                    if (Application.isPlaying)
                        Destroy(component);
                    else
                        DestroyImmediate(component);
                }
            }

            UnityEngine.Debug.Log($"[MonsterSetup] Removed AI components from {gameObject.name}");
        }

        /// <summary>
        /// Validate current setup
        /// </summary>
        [ContextMenu("Validate Setup")]
        public void ValidateSetup()
        {
            var issues = new System.Collections.Generic.List<string>();

            if (GetComponent<NavMeshAgent>() == null)
                issues.Add("Missing NavMeshAgent");

            if (GetComponent<Animator>() == null)
                issues.Add("Missing Animator");

            if (GetComponent<ChimeraMonsterAI>() == null)
                issues.Add("Missing ChimeraMonsterAI");

            if (GetComponent<CreatureInstanceComponent>() == null)
                issues.Add("Missing CreatureInstanceComponent");

            if (issues.Count == 0)
            {
                UnityEngine.Debug.Log($"[MonsterSetup] ✅ {gameObject.name} setup is valid");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[MonsterSetup] ⚠️ {gameObject.name} has issues: {string.Join(", ", issues)}");
            }
        }

        #endregion
    }
}