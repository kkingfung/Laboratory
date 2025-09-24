using UnityEngine;
using UnityEditor;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Visuals;
using Laboratory.Chimera.Configuration;
using GeneticsTraitType = Laboratory.Chimera.Genetics.TraitType;
using Laboratory.Chimera.Core;
using System.Linq;

namespace Laboratory.Chimera.Testing
{
    /// <summary>
    /// Comprehensive testing tool for the advanced procedural visual system.
    /// Use this to spawn creatures with specific genetic traits and see the visual results.
    /// </summary>
    public class VisualGeneticsTestingTool : MonoBehaviour
    {
        [Header("Testing Configuration")]
        [SerializeField] private GameObject creaturePrefab;
        [SerializeField] private GeneticTraitLibrary traitLibrary;
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Vector3 spawnOffset = Vector3.right * 3f;
        
        [Header("Spawn Settings")]
        [SerializeField] private int numberOfCreatures = 5;
        [SerializeField] private Laboratory.Chimera.Core.BiomeType testBiome = Laboratory.Chimera.Core.BiomeType.Temperate;
        [SerializeField] private bool enableMutations = true;
        [SerializeField] private bool showDebugInfo = true;
        
        [Header("Visual Testing")]
        [SerializeField] private bool testColorVariations = true;
        [SerializeField] private bool testSizeVariations = true;
        [SerializeField] private bool testPatternVariations = true;
        [SerializeField] private bool testMagicalVariations = true;
        [SerializeField] private bool testMutationEffects = true;
        
        [Header("Performance Testing")]
        [SerializeField] private bool enablePerformanceMetrics = true;
        [SerializeField] private int massSpawnCount = 100;
        
        private GameObject[] spawnedCreatures;
        private float lastSpawnTime;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (creaturePrefab == null)
            {
                Debug.LogError("VisualGeneticsTestingTool: No creature prefab assigned!");
                return;
            }
            
            if (traitLibrary == null)
            {
                Debug.LogError("VisualGeneticsTestingTool: No trait library assigned!");
                return;
            }
        }
        
        #endregion
        
        #region Public Testing Methods
        
        /// <summary>
        /// Spawn a set of genetically diverse creatures for visual testing
        /// </summary>
        [ContextMenu("Test Genetic Diversity")]
        public void TestGeneticDiversity()
        {
            ClearPreviousSpawns();
            
            Debug.Log($"🧬 Spawning {numberOfCreatures} genetically diverse creatures...");
            
            spawnedCreatures = new GameObject[numberOfCreatures];
            Vector3 basePosition = transform.position;
            
            for (int i = 0; i < numberOfCreatures; i++)
            {
                Vector3 spawnPosition = basePosition + (spawnOffset * i);
                var creature = SpawnCreatureWithRandomGenetics(spawnPosition, i + 1);
                spawnedCreatures[i] = creature;
                
                if (showDebugInfo)
                {
                    var creatureComponent = creature.GetComponent<CreatureInstanceComponent>();
                    Debug.Log($"✅ Spawned creature {i + 1}: {creatureComponent.GetInfoText()}");
                }
            }
            
            Debug.Log($"🎉 Spawned {numberOfCreatures} creatures successfully!");
        }
        
        /// <summary>
        /// Test specific genetic traits and their visual effects
        /// </summary>
        [ContextMenu("Test Specific Traits")]
        public void TestSpecificTraits()
        {
            ClearPreviousSpawns();
            
            Debug.Log("🎨 Testing specific genetic trait visuals...");
            
            var testCases = new[]
            {
                ("High Intelligence", CreateHighIntelligenceGenetics()),
                ("Fire Affinity", CreateFireAffinityGenetics()),
                ("Desert Adaptation", CreateDesertAdaptationGenetics()),
                ("Metallic Properties", CreateMetallicGenetics()),
                ("Mutation Heavy", CreateMutationHeavyGenetics())
            };
            
            spawnedCreatures = new GameObject[testCases.Length];
            Vector3 basePosition = transform.position;
            
            for (int i = 0; i < testCases.Length; i++)
            {
                Vector3 spawnPosition = basePosition + (spawnOffset * i);
                var creature = SpawnCreatureWithSpecificGenetics(spawnPosition, testCases[i].Item2, testCases[i].Item1);
                spawnedCreatures[i] = creature;
            }
            
            Debug.Log($"✅ Spawned {testCases.Length} test case creatures!");
        }
        
        /// <summary>
        /// Test breeding combinations and their offspring visuals
        /// </summary>
        [ContextMenu("Test Breeding Visuals")]
        public void TestBreedingVisuals()
        {
            ClearPreviousSpawns();
            
            Debug.Log("👨‍👩‍👧‍👦 Testing breeding combinations...");
            
            // Create two diverse parent genetics
            var parent1Genetics = traitLibrary.CreateRichGeneticProfile(BiomeType.Desert, 1);
            var parent2Genetics = traitLibrary.CreateRichGeneticProfile(BiomeType.Arctic, 1);
            
            // Create offspring genetics
            var offspringGenetics = GeneticProfile.CreateOffspring(parent1Genetics, parent2Genetics);
            
            // Spawn all three for comparison
            var spawns = new[]
            {
                ("Parent 1 (Desert)", parent1Genetics),
                ("Parent 2 (Arctic)", parent2Genetics),
                ("Offspring (Hybrid)", offspringGenetics)
            };
            
            spawnedCreatures = new GameObject[spawns.Length];
            Vector3 basePosition = transform.position;
            
            for (int i = 0; i < spawns.Length; i++)
            {
                Vector3 spawnPosition = basePosition + (spawnOffset * i);
                var creature = SpawnCreatureWithSpecificGenetics(spawnPosition, spawns[i].Item2, spawns[i].Item1);
                spawnedCreatures[i] = creature;
            }
            
            Debug.Log("🧬 Breeding test complete - compare parent vs offspring visuals!");
        }
        
        /// <summary>
        /// Performance test - spawn many creatures quickly
        /// </summary>
        [ContextMenu("Performance Test")]
        public void PerformanceTest()
        {
            if (!enablePerformanceMetrics)
            {
                Debug.LogWarning("Performance metrics disabled - enable in inspector");
                return;
            }
            
            ClearPreviousSpawns();
            
            Debug.Log($"⚡ Starting performance test - spawning {massSpawnCount} creatures...");
            
            float startTime = Time.realtimeSinceStartup;
            
            spawnedCreatures = new GameObject[massSpawnCount];
            
            for (int i = 0; i < massSpawnCount; i++)
            {
                // Spawn in a grid pattern
                int gridSize = Mathf.CeilToInt(Mathf.Sqrt(massSpawnCount));
                Vector3 gridPosition = new Vector3(
                    (i % gridSize) * 2f,
                    0,
                    (i / gridSize) * 2f
                );
                
                Vector3 spawnPosition = transform.position + gridPosition;
                var creature = SpawnCreatureWithRandomGenetics(spawnPosition, i + 1);
                spawnedCreatures[i] = creature;
            }
            
            float endTime = Time.realtimeSinceStartup;
            float totalTime = endTime - startTime;
            
            Debug.Log($"⚡ Performance test complete!");
            Debug.Log($"📊 Spawned {massSpawnCount} creatures in {totalTime:F2} seconds");
            Debug.Log($"📊 Average time per creature: {(totalTime / massSpawnCount) * 1000:F2}ms");
        }
        
        /// <summary>
        /// Test generation progression visuals
        /// </summary>
        [ContextMenu("Test Generation Progression")]
        public void TestGenerationProgression()
        {
            ClearPreviousSpawns();
            
            Debug.Log("📈 Testing generation progression visuals...");
            
            // Create base genetics
            var baseGenetics = traitLibrary.CreateRichGeneticProfile(testBiome, 1);
            
            // Simulate multiple generations
            var generations = new GeneticProfile[5];
            generations[0] = baseGenetics;
            
            for (int i = 1; i < generations.Length; i++)
            {
                // Each generation breeds with itself (simplified)
                generations[i] = GeneticProfile.CreateOffspring(generations[i - 1], generations[i - 1]);
            }
            
            spawnedCreatures = new GameObject[generations.Length];
            Vector3 basePosition = transform.position;
            
            for (int i = 0; i < generations.Length; i++)
            {
                Vector3 spawnPosition = basePosition + (spawnOffset * i);
                var creature = SpawnCreatureWithSpecificGenetics(spawnPosition, generations[i], $"Generation {i + 1}");
                spawnedCreatures[i] = creature;
            }
            
            Debug.Log("📈 Generation progression test complete!");
        }
        
        #endregion
        
        #region Creature Spawning
        
        private GameObject SpawnCreatureWithRandomGenetics(Vector3 position, int creatureNumber)
        {
            var genetics = traitLibrary.CreateRichGeneticProfile(testBiome, 1);
            
            if (enableMutations)
            {
                traitLibrary.ApplyRandomMutations(genetics);
            }
            
            return SpawnCreatureWithSpecificGenetics(position, genetics, $"Test Creature {creatureNumber}");
        }
        
        private GameObject SpawnCreatureWithSpecificGenetics(Vector3 position, GeneticProfile genetics, string creatureName)
        {
            // Instantiate creature
            var creature = Instantiate(creaturePrefab, position, Quaternion.identity, spawnParent);
            creature.name = creatureName;
            
            // Get required components
            var creatureInstance = creature.GetComponent<CreatureInstanceComponent>();
            if (creatureInstance == null)
            {
                creatureInstance = creature.AddComponent<CreatureInstanceComponent>();
            }
            
            // Add visual systems if not present
            var visualSystem = creature.GetComponent<ProceduralVisualSystem>();
            if (visualSystem == null)
            {
                visualSystem = creature.AddComponent<ProceduralVisualSystem>();
            }
            
            var visualIntegration = creature.GetComponent<GeneticVisualIntegration>();
            if (visualIntegration == null)
            {
                visualIntegration = creature.AddComponent<GeneticVisualIntegration>();
            }
            
            // Create creature data with the specific genetics
            var creatureData = new Laboratory.Chimera.Breeding.CreatureInstance();
            creatureData.GeneticProfile = genetics;
            creatureData.AgeInDays = 30; // Adult
            creatureData.Happiness = 0.8f;
            creatureData.CurrentHealth = 100;
            
            // Initialize the creature
            creatureInstance.Initialize(creatureData);
            
            // Apply enhanced visual genetics
            creatureInstance.ApplyEnhancedVisualGenetics();
            
            lastSpawnTime = Time.time;
            
            return creature;
        }
        
        #endregion
        
        #region Specific Genetics Creation
        
        private GeneticProfile CreateHighIntelligenceGenetics()
        {
            var genes = new System.Collections.Generic.List<Gene>
            {
                new Gene
                {
                    traitName = "Intelligence",
                    traitType = GeneticsTraitType.Mental,
                    value = 0.95f, // Very high intelligence
                    dominance = 0.8f,
                    expression = GeneExpression.Enhanced,
                    isActive = true
                },
                new Gene
                {
                    traitName = "Enhanced Vision",
                    traitType = GeneticsTraitType.Sensory,
                    value = 0.9f,
                    dominance = 0.7f,
                    expression = GeneExpression.Enhanced,
                    isActive = true
                }
            };

            // Add color variations if enabled
            if (testColorVariations)
            {
                genes.Add(new Gene
                {
                    traitName = "Primary Color",
                    traitType = GeneticsTraitType.Physical,
                    value = 0.6f, // Blue-ish hue
                    dominance = 0.8f,
                    expression = GeneExpression.Normal,
                    isActive = true
                });
            }

            // Add size variations if enabled
            if (testSizeVariations)
            {
                genes.Add(new Gene
                {
                    traitName = "Size Modifier",
                    traitType = GeneticsTraitType.Physical,
                    value = Random.Range(0.8f, 1.2f),
                    dominance = 0.7f,
                    expression = GeneExpression.Normal,
                    isActive = true
                });
            }

            // Add pattern variations if enabled
            if (testPatternVariations)
            {
                genes.Add(new Gene
                {
                    traitName = "Pattern Intensity",
                    traitType = GeneticsTraitType.Physical,
                    value = Random.Range(0.5f, 0.9f),
                    dominance = 0.6f,
                    expression = GeneExpression.Normal,
                    isActive = true
                });
            }

            // Add magical variations if enabled
            if (testMagicalVariations)
            {
                genes.Add(new Gene
                {
                    traitName = "Mystical Aura",
                    traitType = GeneticsTraitType.Magical,
                    value = Random.Range(0.3f, 0.8f),
                    dominance = 0.5f,
                    expression = GeneExpression.Enhanced,
                    isActive = true
                });
            }

            var genetics = new GeneticProfile(genes.ToArray(), 1);

            // Apply mutation effects if enabled
            if (testMutationEffects && traitLibrary != null)
            {
                traitLibrary.ApplyRandomMutations(genetics);
            }

            return genetics;
        }
        
        private GeneticProfile CreateFireAffinityGenetics()
        {
            var genes = new Gene[]
            {
                new Gene
                {
                    traitName = "Fire Affinity",
                    traitType = GeneticsTraitType.Magical,
                    value = 0.9f, // Strong fire magic
                    dominance = 0.8f,
                    expression = GeneExpression.Enhanced,
                    isActive = true
                },
                new Gene
                {
                    traitName = "Primary Color",
                    traitType = GeneticsTraitType.Physical,
                    value = 0.05f, // Red hue
                    dominance = 0.8f,
                    expression = GeneExpression.Normal,
                    isActive = true
                },
                new Gene
                {
                    traitName = "Metallic Properties",
                    traitType = GeneticsTraitType.Physical,
                    value = 0.6f,
                    dominance = 0.5f,
                    expression = GeneExpression.Normal,
                    isActive = true
                }
            };
            
            return new GeneticProfile(genes, 1);
        }
        
        private GeneticProfile CreateDesertAdaptationGenetics()
        {
            var genes = new Gene[]
            {
                new Gene
                {
                    traitName = "Desert Adaptation",
                    traitType = GeneticsTraitType.Physical,
                    value = 0.95f,
                    dominance = 0.9f,
                    expression = GeneExpression.Enhanced,
                    isActive = true
                },
                new Gene
                {
                    traitName = "Primary Color",
                    traitType = GeneticsTraitType.Physical,
                    value = 0.1f, // Sandy yellow
                    dominance = 0.8f,
                    expression = GeneExpression.Normal,
                    isActive = true
                },
                new Gene
                {
                    traitName = "Heat Resistance",
                    traitType = GeneticsTraitType.Metabolic,
                    value = 0.9f,
                    dominance = 0.7f,
                    expression = GeneExpression.Enhanced,
                    isActive = true
                }
            };
            
            return new GeneticProfile(genes, 1);
        }
        
        private GeneticProfile CreateMetallicGenetics()
        {
            var genes = new Gene[]
            {
                new Gene
                {
                    traitName = "Metallic Properties",
                    traitType = GeneticsTraitType.Physical,
                    value = 0.95f, // Very metallic
                    dominance = 0.9f,
                    expression = GeneExpression.Enhanced,
                    isActive = true
                },
                new Gene
                {
                    traitName = "Hardness",
                    traitType = GeneticsTraitType.Physical,
                    value = 0.9f,
                    dominance = 0.8f,
                    expression = GeneExpression.Enhanced,
                    isActive = true
                },
                new Gene
                {
                    traitName = "Armor Plating",
                    traitType = GeneticsTraitType.Combat,
                    value = 0.8f,
                    dominance = 0.7f,
                    expression = GeneExpression.Normal,
                    isActive = true
                }
            };
            
            return new GeneticProfile(genes, 1);
        }
        
        private GeneticProfile CreateMutationHeavyGenetics()
        {
            var genetics = traitLibrary.CreateRichGeneticProfile(testBiome, 3);
            
            // Apply many mutations
            for (int i = 0; i < 5; i++)
            {
                traitLibrary.ApplyRandomMutations(genetics);
            }
            
            return genetics;
        }
        
        #endregion
        
        #region Utility Methods
        
        private void ClearPreviousSpawns()
        {
            if (spawnedCreatures != null)
            {
                foreach (var creature in spawnedCreatures)
                {
                    if (creature != null)
                    {
                        DestroyImmediate(creature);
                    }
                }
            }
            
            spawnedCreatures = null;
        }
        
        /// <summary>
        /// Get performance metrics for the last spawn operation
        /// </summary>
        public string GetPerformanceMetrics()
        {
            if (spawnedCreatures == null) return "No creatures spawned";
            
            var info = $"🔍 Performance Metrics:\n";
            info += $"Creatures Spawned: {spawnedCreatures.Length}\n";
            info += $"Last Spawn Time: {Time.time - lastSpawnTime:F2}s ago\n";
            
            // Count visual components
            int totalRenderers = 0;
            int totalParticles = 0;
            int totalLights = 0;
            
            foreach (var creature in spawnedCreatures)
            {
                if (creature != null)
                {
                    totalRenderers += creature.GetComponentsInChildren<Renderer>().Length;
                    totalParticles += creature.GetComponentsInChildren<ParticleSystem>().Length;
                    totalLights += creature.GetComponentsInChildren<Light>().Length;
                }
            }
            
            info += $"Total Renderers: {totalRenderers}\n";
            info += $"Total Particle Systems: {totalParticles}\n";
            info += $"Total Lights: {totalLights}\n";
            
            return info;
        }
        
        #endregion
        
        #region Gizmos and Debug
        
        private void OnDrawGizmosSelected()
        {
            // Draw spawn positions
            Gizmos.color = Color.yellow;
            Vector3 basePosition = transform.position;
            
            for (int i = 0; i < numberOfCreatures; i++)
            {
                Vector3 spawnPosition = basePosition + (spawnOffset * i);
                Gizmos.DrawWireCube(spawnPosition, Vector3.one);
            }
        }
        
        #endregion
    }
}