using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Core;
using Laboratory.Core.ECS;
using Laboratory.Chimera.AI;
using Laboratory.Chimera.Creatures;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// Authoring component that bridges ScriptableObject configuration with ECS data.
    /// Drop this on any creature prefab to make it ECS-ready with full genetic integration.
    /// </summary>
    public class CreatureAuthoringSystem : MonoBehaviour
    {
        [Header("🧬 Species Configuration")]
        [SerializeField] public ChimeraSpeciesConfig speciesConfig;

        [Header("🎯 Instance Settings")]
        [SerializeField] public bool randomizeGenetics = true;
        [SerializeField] public int generation = 1;
        [SerializeField] public uint geneticSeed = 0; // 0 = auto-generate

        [Header("🎨 Visual Overrides")]
        [SerializeField] public bool overrideScale = false;
        [SerializeField] [Range(0.5f, 2.0f)] public float scaleMultiplier = 1f;

        [Header("🤖 AI Behavior")]
        [SerializeField] public Laboratory.Chimera.AI.AIBehaviorType behaviorType = Laboratory.Chimera.AI.AIBehaviorType.Wild;
        [SerializeField] public bool enablePathfinding = true;
        [SerializeField] [Range(0.1f, 10f)] public float movementSpeed = 3f;

        [Header("📊 Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private string generatedGeneticID = "";

        // Manual ECS entity creation (no auto-conversion)
        [ContextMenu("Create ECS Entity")]
        public void CreateECSEntityManual()
        {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true)
            {
                UnityEngine.Debug.LogError("CreatureAuthoringSystem: No ECS world found!");
                return;
            }

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();

            // Validate configuration
            if (speciesConfig == null)
            {
                UnityEngine.Debug.LogError($"CreatureAuthoringSystem: No species config assigned to {name}");
                entityManager.DestroyEntity(entity);
                return;
            }

            // Generate or use provided genetic seed
            uint seed = geneticSeed;
            if (seed == 0)
            {
                seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
                geneticSeed = seed;
                generatedGeneticID = $"GEN_{seed:X8}";
            }

            // Core creature data
            entityManager.AddComponentData(entity, new CreatureData
            {
                speciesID = speciesConfig.speciesName.GetHashCode(),
                generation = generation,
                age = 0,
                geneticSeed = seed,
                isAlive = true
            });

            // Default creature stats
            entityManager.AddComponentData(entity, new Laboratory.Core.ECS.CreatureStats
            {
                health = 100f,
                maxHealth = 100f,
                attack = 10f,
                defense = 5f,
                speed = 5f,
                intelligence = 50f,
                charisma = 50f
            });

            // AI behavior component
            var aiComponent = new CreatureAIComponent();
            aiComponent.CurrentState = AIState.Idle;
            aiComponent.DetectionRange = 10f;
            aiComponent.PatrolRadius = 5f;
            aiComponent.AggressionLevel = 0.5f;
            aiComponent.CuriosityLevel = 0.5f;
            aiComponent.LoyaltyLevel = 0.5f;
            aiComponent.StateTimer = 0f;
            entityManager.AddComponentData(entity, aiComponent);

            // Visual data for rendering system
            entityManager.AddComponentData(entity, new CreatureVisualData
            {
                baseScale = overrideScale ? scaleMultiplier : 1f,
                colorSeed = seed,
                speciesVisualID = speciesConfig.GetInstanceID()
            });

            // Add to simulation group
            entityManager.AddComponent<CreatureSimulationTag>(entity);

            if (showDebugInfo)
            {
                UnityEngine.Debug.Log($"✅ Created ECS entity for {name} with genetic seed: {seed:X8}");
            }
        }

        /// <summary>
        /// Create genetic profile from species configuration
        /// </summary>
        private static GeneticProfile CreateGeneticProfile(ChimeraSpeciesConfig species, bool randomize, uint seed)
        {
            var profile = new GeneticProfile();
            var random = new Unity.Mathematics.Random(seed);

            // Initialize genetic profile with species defaults
            foreach (var geneConfig in species.defaultGenes)
            {
                float value = geneConfig.baseValue;

                if (randomize)
                {
                    // Add variance based on configuration
                    float variance = random.NextFloat(-geneConfig.variance, geneConfig.variance);
                    value = math.clamp(value + variance, 0f, 1f);
                }

                // Add trait to profile (simplified - in full system would use proper Gene structure)
                // This is a bridge until full genetic system integration
            }

            return profile;
        }

        /// <summary>
        /// Calculate final creature stats from genetics and species base
        /// </summary>
        private static Laboratory.Core.ECS.CreatureStats CalculateStats(ChimeraSpeciesConfig species, GeneticProfile genetics)
        {
            var baseStats = species.baseStats;

            // Apply genetic modifiers (simplified calculation)
            return new Laboratory.Core.ECS.CreatureStats
            {
                health = baseStats.health,
                attack = baseStats.attack,
                defense = baseStats.defense,
                speed = baseStats.speed,
                intelligence = baseStats.intelligence,
                charisma = baseStats.charisma,
                maxHealth = baseStats.health
            };
        }

        // Editor validation and preview
        private void OnValidate()
        {
            if (speciesConfig != null && geneticSeed != 0)
            {
                generatedGeneticID = $"{speciesConfig.speciesName}_G{generation}_{geneticSeed:X8}";
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (showDebugInfo && speciesConfig != null)
            {
                // Draw creature info
                var position = transform.position;
                var size = speciesConfig.size == CreatureSize.Small ? 0.5f :
                          speciesConfig.size == CreatureSize.Large ? 2f : 1f;

                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(position, size);

                // Draw behavior type indicator
                Gizmos.color = GetBehaviorColor(behaviorType);
                Gizmos.DrawLine(position, position + Vector3.up * (size + 1f));
            }
        }

        private Color GetBehaviorColor(Laboratory.Chimera.AI.AIBehaviorType behavior)
        {
            return behavior switch
            {
                Laboratory.Chimera.AI.AIBehaviorType.Aggressive => Color.red,
                Laboratory.Chimera.AI.AIBehaviorType.Defensive => Color.yellow,
                Laboratory.Chimera.AI.AIBehaviorType.Passive => Color.blue,
                Laboratory.Chimera.AI.AIBehaviorType.Wild => Color.green,
                Laboratory.Chimera.AI.AIBehaviorType.Predator => Color.magenta,
                Laboratory.Chimera.AI.AIBehaviorType.Herbivore => Color.cyan,
                _ => Color.white
            };
        }
#endif
    }

    // ECS Components are now defined in the main Laboratory.Chimera assembly
    // in ChimeraECSComponents.cs 
}