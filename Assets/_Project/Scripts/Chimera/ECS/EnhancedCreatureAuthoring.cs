using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.ECS.Components;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// Enhanced authoring component for creatures with full genetic integration
    /// Converts creature data into ECS components for high-performance simulation
    /// </summary>
    public class EnhancedCreatureAuthoring : MonoBehaviour
    {
        [Header("Creature Data")]
        [SerializeField] private CreatureDefinition creatureDefinition;
        [SerializeField] private GeneticProfile geneticProfile;
        [SerializeField] private bool useRandomGenetics = true;
        
        [Header("Behavior Configuration")]
        [SerializeField] private bool enableAI = true;
        [SerializeField] private bool enableBreeding = true;
        [SerializeField] private bool enableVisualGenetics = true;
        
        [Header("ECS Components")]
        [SerializeField] private bool includeMovement = true;
        [SerializeField] private bool includeHealth = true;
        [SerializeField] private bool includeGenetics = true;
        [SerializeField] private bool includeBonding = true;
        [SerializeField] private bool includeNeeds = true;
        [SerializeField] private bool includePersonality = true;
        
        // Note: This authoring component is prepared for Unity.Entities ECS conversion
        // When Unity.Entities is properly installed, the Baker pattern can be implemented

        /// <summary>
        /// Manual entity conversion method for when Baker is not available
        /// </summary>
        public void ConvertToEntityManual()
        {
            Debug.Log($"Enhanced Creature Authoring: Converting {gameObject.name} to ECS entity");

            try
            {
                // Check if Unity.Entities is available
                var worldType = System.Type.GetType("Unity.Entities.World, Unity.Entities");
                if (worldType == null)
                {
                    Debug.LogWarning("Unity.Entities package not available. Cannot create ECS entity.");
                    return;
                }

                var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
                if (world == null)
                {
                    Debug.LogError("No default ECS world available for entity creation.");
                    return;
                }

                var entityManager = world.EntityManager;
                var entity = entityManager.CreateEntity();

                // Set entity name for debugging
                entityManager.SetName(entity, gameObject.name + "_Entity");

                // Add core components based on configuration
                AddCoreComponents(entityManager, entity);

                Debug.Log($"Successfully created ECS entity for {gameObject.name} with ID: {entity}");
                Debug.Log($"Configuration: {GetConfigurationSummary()}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to create ECS entity for {gameObject.name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Add ECS components to the entity based on configuration
        /// </summary>
        private void AddCoreComponents(EntityManager entityManager, Entity entity)
        {
            // Always add creature definition component
            if (creatureDefinition != null)
            {
                var definitionComponent = new CreatureDefinitionComponent
                {
                    SpeciesId = creatureDefinition.GetInstanceID(),
                    MaxHealth = creatureDefinition.baseStats.health,
                    BaseAttack = creatureDefinition.baseStats.attack,
                    BaseDefense = creatureDefinition.baseStats.defense,
                    BaseSpeed = creatureDefinition.baseStats.speed
                };
                entityManager.AddComponentData(entity, definitionComponent);
            }

            // Add movement component if enabled
            if (includeMovement)
            {
                var movementComponent = new CreatureMovementComponent
                {
                    BaseSpeed = creatureDefinition?.baseStats.speed ?? 5.0f,
                    CurrentSpeed = creatureDefinition?.baseStats.speed ?? 5.0f,
                    RotationSpeed = 180f,
                    TargetPosition = transform.position,
                    IsMoving = false,
                    HasDestination = false
                };
                entityManager.AddComponentData(entity, movementComponent);
            }

            // Add health component if enabled
            if (includeHealth)
            {
                var healthComponent = new CreatureHealthComponent
                {
                    MaxHealth = creatureDefinition?.baseStats.health ?? 100,
                    CurrentHealth = creatureDefinition?.baseStats.health ?? 100,
                    RegenerationRate = 1.0f,
                    IsAlive = true,
                    IsInvulnerable = false,
                    LastDamageTime = 0f,
                    DamageTaken = 0
                };
                entityManager.AddComponentData(entity, healthComponent);
            }

            // Add genetics component if enabled
            if (includeGenetics && geneticProfile != null)
            {
                var geneticsComponent = new CreatureGeneticsComponent
                {
                    Generation = geneticProfile.Generation,
                    GeneticPurity = geneticProfile.GetGeneticPurity(),
                    IsShiny = false, // Would be calculated from genetics
                    ParentId1 = System.Guid.Empty,
                    ParentId2 = System.Guid.Empty,
                    ActiveGeneCount = geneticProfile.Mutations?.Count ?? 0,
                    StrengthTrait = 0.5f,
                    VitalityTrait = 0.5f,
                    AgilityTrait = 0.5f,
                    ResilienceTrait = 0.5f,
                    IntellectTrait = 0.5f,
                    CharmTrait = 0.5f,
                    LineageId = System.Guid.NewGuid()
                };
                entityManager.AddComponentData(entity, geneticsComponent);
            }

            // Add bonding component if enabled
            if (includeBonding)
            {
                var bondingComponent = new CreatureBondingComponent
                {
                    BondedToPlayer = false,
                    BondStrength = 0f,
                    BondedEntity = Entity.Null,
                    SocialNeed = 0.5f,
                    LastInteraction = 0f,
                    FriendCount = 0,
                    TrustLevel = 0f,
                    Obedience = 0f
                };
                entityManager.AddComponentData(entity, bondingComponent);
            }

            // Add needs component if enabled
            if (includeNeeds)
            {
                var needsComponent = new CreatureNeedsComponent
                {
                    Hunger = 0.5f,
                    Thirst = 0.5f,
                    Energy = 1.0f,
                    Social = 0.5f,
                    Comfort = 0.5f,
                    Happiness = 0.7f,
                    LastFed = 0f,
                    LastDrank = 0f,
                    LastRested = 0f,
                    Rest = 1.0f,
                    Exercise = 0.5f,
                    Mental = 0.5f,
                    Stress = 0.2f
                };
                entityManager.AddComponentData(entity, needsComponent);
            }

            // Add personality component if enabled
            if (includePersonality)
            {
                var personalityComponent = new CreaturePersonalityComponent
                {
                    Aggression = creatureDefinition?.behaviorProfile.aggression ?? 0.5f,
                    Curiosity = creatureDefinition?.behaviorProfile.curiosity ?? 0.5f,
                    Sociability = 0.5f,
                    Loyalty = creatureDefinition?.behaviorProfile.loyalty ?? 0.5f,
                    Playfulness = creatureDefinition?.behaviorProfile.playfulness ?? 0.5f,
                    Independence = creatureDefinition?.behaviorProfile.independence ?? 0.5f,
                    Fearfulness = 0.3f,
                    Bravery = 0.7f,
                    SocialNeed = 0.5f
                };
                entityManager.AddComponentData(entity, personalityComponent);
            }

            // Add stats component with computed values
            var statsComponent = new CreatureStatsComponent
            {
                BaseStats = creatureDefinition?.baseStats ?? new CreatureStats(100, 20, 15, 10, 5, 5),
                CurrentHealth = creatureDefinition?.baseStats.health ?? 100,
                MaxHealth = creatureDefinition?.baseStats.health ?? 100,
                Level = 1,
                Experience = 0,
                HealthRegenRate = 1.0f
            };
            entityManager.AddComponentData(entity, statsComponent);

            Debug.Log($"Added {GetComponentCount()} components to entity");
        }

        /// <summary>
        /// Count how many components will be added based on configuration
        /// </summary>
        private int GetComponentCount()
        {
            int count = 1; // Always add stats component
            if (creatureDefinition != null) count++;
            if (includeMovement) count++;
            if (includeHealth) count++;
            if (includeGenetics && geneticProfile != null) count++;
            if (includeBonding) count++;
            if (includeNeeds) count++;
            if (includePersonality) count++;
            return count;
        }

        /// <summary>
        /// Get configuration summary for debugging
        /// </summary>
        public string GetConfigurationSummary()
        {
            return $"Creature: {creatureDefinition?.speciesName ?? "None"}, " +
                   $"AI: {enableAI}, Breeding: {enableBreeding}, VisualGenetics: {enableVisualGenetics}, " +
                   $"Components: Movement({includeMovement}), Health({includeHealth}), " +
                   $"Genetics({includeGenetics}), Bonding({includeBonding}), " +
                   $"Needs({includeNeeds}), Personality({includePersonality})";
        }
        
        #region Unity Lifecycle
        
        private void OnValidate()
        {
            // Auto-assign creature definition if available
            if (creatureDefinition == null)
            {
                var creatureInstance = GetComponent<CreatureInstanceComponent>();
                if (creatureInstance != null && creatureInstance.Instance?.Definition != null)
                {
                    creatureDefinition = creatureInstance.Instance.Definition;
                    geneticProfile = creatureInstance.Instance.GeneticProfile;
                }
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Set the creature definition for this authoring component
        /// </summary>
        public void SetCreatureDefinition(CreatureDefinition definition)
        {
            creatureDefinition = definition;
        }
        
        /// <summary>
        /// Set the genetic profile for this authoring component
        /// </summary>
        public void SetGeneticProfile(GeneticProfile genetics)
        {
            geneticProfile = genetics;
            useRandomGenetics = false;
        }
        
        /// <summary>
        /// Enable or disable random genetics generation
        /// </summary>
        public void SetUseRandomGenetics(bool useRandom)
        {
            useRandomGenetics = useRandom;
        }

        /// <summary>
        /// Get genetic summary for UI display
        /// </summary>
        public string GetGeneticSummary()
        {
            if (geneticProfile == null)
                return "No genetic data available";

            return $"Generation: {geneticProfile.Generation}, " +
                   $"Purity: {geneticProfile.GetGeneticPurity():P1}, " +
                   $"Mutations: {geneticProfile.Mutations?.Count ?? 0}";
        }

        /// <summary>
        /// Get personality description for UI display
        /// </summary>
        public string GetPersonalityDescription()
        {
            if (creatureDefinition?.behaviorProfile == null)
                return "No personality data available";

            return $"A creature with balanced traits"; // Placeholder implementation
        }

        #endregion
    }
}
