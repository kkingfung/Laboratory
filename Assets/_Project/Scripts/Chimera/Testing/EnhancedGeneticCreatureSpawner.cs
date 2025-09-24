using UnityEngine;
using UnityEngine.AI;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Visuals;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.AI;
using GeneticsTraitType = Laboratory.Chimera.Genetics.TraitType;

namespace Laboratory.Chimera.Testing
{
    /// <summary>
    /// ENHANCED creature spawner with advanced visual genetics integration!
    /// Press F1-F4 in play mode for different spawn types.
    /// Now with STUNNING procedural visuals based on genetics!
    /// </summary>
    public class EnhancedGeneticCreatureSpawner : MonoBehaviour
    {
        [Header("🧬 Enhanced Spawn Settings")]
        [SerializeField] private GameObject creaturePrefab;
        [SerializeField] private GeneticTraitLibrary traitLibrary;
        [SerializeField] private int spawnCount = 1;
        [SerializeField] private float spawnRadius = 5f;
        [SerializeField] private CreatureArchetype selectedArchetype = CreatureArchetype.AlphaWolf;
        [SerializeField] private Laboratory.Chimera.Core.BiomeType currentBiome = Laboratory.Chimera.Core.BiomeType.Temperate;
        
        [Header("🎨 Visual Enhancement Settings")]
        [SerializeField] private bool useAdvancedVisuals = true;
        [SerializeField] private bool enableMutations = true;
        [SerializeField] private bool showVisualDebug = true;
        [SerializeField] private Material[] customMaterials;
        
        [Header("🎮 Enhanced Controls")]
        [SerializeField] private KeyCode spawnArchetypeKey = KeyCode.F1;
        [SerializeField] private KeyCode spawnRandomKey = KeyCode.F2;
        [SerializeField] private KeyCode spawnBreedingPairKey = KeyCode.F3;
        [SerializeField] private KeyCode clearAllKey = KeyCode.F4;
        [SerializeField] private KeyCode cycleBiomeKey = KeyCode.B;
        [SerializeField] private KeyCode cycleArchetypeKey = KeyCode.N;
        
        [Header("📊 Debug & Performance")]
        [SerializeField] private bool showSpawnInfo = true;
        [SerializeField] private bool enableBehaviorDebug = true;
        [SerializeField] private bool showPerformanceMetrics = false;
        
        private int totalSpawnedCreatures = 0;
        private float lastSpawnTime = 0f;

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(spawnArchetypeKey))
            {
                SpawnCreatureWithArchetype(selectedArchetype);
            }
            
            if (Input.GetKeyDown(spawnRandomKey))
            {
                SpawnRandomEnhancedCreature();
            }
            
            if (Input.GetKeyDown(spawnBreedingPairKey))
            {
                SpawnBreedingPair();
            }
            
            if (Input.GetKeyDown(clearAllKey))
            {
                ClearAllSpawnedCreatures();
            }
            
            if (Input.GetKeyDown(cycleBiomeKey))
            {
                CycleBiome();
            }
            
            if (Input.GetKeyDown(cycleArchetypeKey))
            {
                CycleArchetype();
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, Screen.height - 250, 400, 240));
            
            GUILayout.Label("🧬 ENHANCED GENETIC SPAWNER", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            GUILayout.Space(5);
            
            GUILayout.Label($"F1: Spawn {selectedArchetype}");
            GUILayout.Label($"F2: Spawn Random Enhanced Creature");
            GUILayout.Label($"F3: Spawn Breeding Pair");
            GUILayout.Label($"F4: Clear All Creatures");
            GUILayout.Label($"B: Cycle Biome (Current: {currentBiome})");
            GUILayout.Label($"N: Cycle Archetype");
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Archetype:", GUILayout.Width(80));
            if (GUILayout.Button(selectedArchetype.ToString(), GUILayout.Width(150)))
            {
                CycleArchetype();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Biome:", GUILayout.Width(80));
            if (GUILayout.Button(currentBiome.ToString(), GUILayout.Width(150)))
            {
                CycleBiome();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            GUILayout.Label($"Advanced Visuals: {(useAdvancedVisuals ? "ON" : "OFF")}");
            GUILayout.Label($"Mutations: {(enableMutations ? "ON" : "OFF")}");
            GUILayout.Label($"Total Spawned: {totalSpawnedCreatures}");
            
            if (showPerformanceMetrics && totalSpawnedCreatures > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Last Spawn: {Time.time - lastSpawnTime:F1}s ago");
                GUILayout.Label($"FPS: {1f / Time.deltaTime:F0}");
            }
            
            GUILayout.EndArea();
        }

        #region Enhanced Spawn Methods

        /// <summary>
        /// Spawn creature with specific archetype and full visual genetics
        /// </summary>
        [ContextMenu("Spawn Enhanced Archetype")]
        public void SpawnCreatureWithArchetype(CreatureArchetype archetype)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                var genetics = CreateEnhancedGeneticArchetype(archetype);
                var creature = SpawnCreatureWithGenetics(genetics, $"{archetype}_Enhanced_{i + 1}");

                LogSpawnInfo(creature, archetype.ToString());
            }
        }

        /// <summary>
        /// Spawn creature with completely random enhanced genetics
        /// </summary>
        [ContextMenu("Spawn Random Enhanced")]
        public void SpawnRandomEnhancedCreature()
        {
            GeneticProfile genetics;
            
            if (traitLibrary != null)
            {
                genetics = traitLibrary.CreateRichGeneticProfile(currentBiome, 1);
                
                if (enableMutations)
                {
                    traitLibrary.ApplyRandomMutations(genetics);
                }
            }
            else
            {
                genetics = CreateEnhancedRandomGenetics();
            }
            
            var creature = SpawnCreatureWithGenetics(genetics, "Random_Enhanced");
            LogSpawnInfo(creature, "Random Enhanced");
        }

        /// <summary>
        /// Spawn a breeding pair with different genetics for testing offspring
        /// </summary>
        [ContextMenu("Spawn Breeding Pair")]
        public void SpawnBreedingPair()
        {
            Debug.Log("🧬 Spawning breeding pair for genetic diversity testing...");
            
            // Create two different archetypes
            var archetype1 = CreatureArchetype.AlphaWolf;
            var archetype2 = CreatureArchetype.CuriousCat;
            
            var parent1Genetics = CreateEnhancedGeneticArchetype(archetype1);
            var parent2Genetics = CreateEnhancedGeneticArchetype(archetype2);
            
            // Spawn parents at different positions
            Vector3 basePos = GetRandomSpawnPosition();
            Vector3 parent1Pos = basePos + Vector3.left * 2f;
            Vector3 parent2Pos = basePos + Vector3.right * 2f;
            
            var parent1 = SpawnCreatureWithGenetics(parent1Genetics, $"Parent1_{archetype1}", parent1Pos);
            var parent2 = SpawnCreatureWithGenetics(parent2Genetics, $"Parent2_{archetype2}", parent2Pos);
            
            // Create and spawn offspring
            var offspringGenetics = GeneticProfile.CreateOffspring(parent1Genetics, parent2Genetics);
            Vector3 offspringPos = basePos + Vector3.forward * 3f;
            var offspring = SpawnCreatureWithGenetics(offspringGenetics, "Offspring_Hybrid", offspringPos);
            
            Debug.Log($"✅ Spawned breeding family - compare the visual differences!");
            LogBreedingInfo(parent1, parent2, offspring);
        }

        #endregion

        #region Core Spawning Logic

        private GameObject SpawnCreatureWithGenetics(GeneticProfile genetics, string baseName, Vector3? position = null)
        {
            Vector3 spawnPos = position ?? GetRandomSpawnPosition();
            GameObject creature = CreateEnhancedCreatureGameObject(baseName, spawnPos);
            
            SetupEnhancedCreatureComponents(creature);
            ApplyGeneticsToCreature(creature, genetics);
            
            if (useAdvancedVisuals)
            {
                ApplyAdvancedVisualGenetics(creature);
            }
            
            totalSpawnedCreatures++;
            lastSpawnTime = Time.time;
            
            return creature;
        }

        private GameObject CreateEnhancedCreatureGameObject(string name, Vector3 position)
        {
            GameObject creature;
            
            // Use prefab if available, otherwise create procedural creature
            if (creaturePrefab != null)
            {
                creature = Instantiate(creaturePrefab, position, Quaternion.identity);
                creature.name = name;
            }
            else
            {
                creature = new GameObject(name);
                creature.transform.position = position;
                AddEnhancedVisualRepresentation(creature);
            }
            
            creature.tag = "SpawnedCreature"; // For easy cleanup
            return creature;
        }

        private void AddEnhancedVisualRepresentation(GameObject creature)
        {
            // Create main body
            var body = CreateBodyPart(creature, "Body", PrimitiveType.Capsule, Vector3.zero, new Vector3(1f, 1f, 1f));
            
            // Create head
            var head = CreateBodyPart(creature, "Head", PrimitiveType.Sphere, new Vector3(0, 1.2f, 0.5f), new Vector3(0.8f, 0.8f, 0.8f));
            
            // Create eyes
            var leftEye = CreateBodyPart(head, "LeftEye", PrimitiveType.Sphere, new Vector3(-0.2f, 0.1f, 0.3f), new Vector3(0.1f, 0.1f, 0.1f));
            var rightEye = CreateBodyPart(head, "RightEye", PrimitiveType.Sphere, new Vector3(0.2f, 0.1f, 0.3f), new Vector3(0.1f, 0.1f, 0.1f));
            
            // Create limbs
            CreateBodyPart(creature, "LeftArm", PrimitiveType.Capsule, new Vector3(-0.8f, 0, 0), new Vector3(0.3f, 0.8f, 0.3f));
            CreateBodyPart(creature, "RightArm", PrimitiveType.Capsule, new Vector3(0.8f, 0, 0), new Vector3(0.3f, 0.8f, 0.3f));
            CreateBodyPart(creature, "LeftLeg", PrimitiveType.Capsule, new Vector3(-0.3f, -1.2f, 0), new Vector3(0.3f, 0.8f, 0.3f));
            CreateBodyPart(creature, "RightLeg", PrimitiveType.Capsule, new Vector3(0.3f, -1.2f, 0), new Vector3(0.3f, 0.8f, 0.3f));
            
            // Add potential magical effect points
            CreateEffectPoint(creature, "AuraPoint", Vector3.up * 2f);
            CreateEffectPoint(creature, "GroundEffectPoint", Vector3.down * 1f);
        }

        private GameObject CreateBodyPart(GameObject parent, string name, PrimitiveType primitiveType, Vector3 localPos, Vector3 scale)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent.transform);
            primitive.transform.localPosition = localPos;
            primitive.transform.localScale = scale;
            
            // Remove the collider from visual parts (main collider will be on the root)
            var collider = primitive.GetComponent<Collider>();
            if (collider != null && primitive != parent)
                DestroyImmediate(collider);
            
            return primitive;
        }

        private void CreateEffectPoint(GameObject parent, string name, Vector3 localPos)
        {
            var effectPoint = new GameObject(name);
            effectPoint.transform.SetParent(parent.transform);
            effectPoint.transform.localPosition = localPos;
            
            // Add particle system for magical effects
            var particles = effectPoint.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
            main.maxParticles = 50;
            main.startSize = 0.1f;
            particles.Stop(); // Start stopped, will be enabled by genetics
            
            // Add light for magical glow
            var light = effectPoint.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 0f; // Start disabled
            light.enabled = false;
        }

        private void SetupEnhancedCreatureComponents(GameObject creature)
        {
            // Core navigation
            var navAgent = creature.AddComponent<NavMeshAgent>();
            navAgent.speed = Random.Range(2f, 6f);
            navAgent.angularSpeed = 180f;
            navAgent.radius = 0.5f;
            navAgent.height = 2f;

            // Core physics
            var rigidbody = creature.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = creature.AddComponent<Rigidbody>();
            }
            rigidbody.mass = Random.Range(50f, 150f);

            // Main collider
            var collider = creature.GetComponent<Collider>();
            if (collider == null)
            {
                var capsuleCollider = creature.AddComponent<CapsuleCollider>();
                capsuleCollider.radius = 0.5f;
                capsuleCollider.height = 2f;
            }

            // Animation
            var animator = creature.AddComponent<Animator>();

            // Core Chimera components
            var creatureInstance = creature.AddComponent<CreatureInstanceComponent>();
            
            // Visual genetics components
            if (useAdvancedVisuals)
            {
                var visualSystem = creature.AddComponent<ProceduralVisualSystem>();
                var visualIntegration = creature.AddComponent<GeneticVisualIntegration>();
            }

            // AI components (if available)
            try
            {
                var monsterAI = creature.AddComponent<ChimeraMonsterAI>();
                var geneticAdapter = creature.AddComponent<GeneticBehaviorAdapter>();
                
                // Enable debug features
                if (enableBehaviorDebug)
                {
                    // Note: Enable debug features through inspector instead of code
                    // as these properties may be private serialized fields
                }
            }
            catch (System.Exception)
            {
                Debug.LogWarning("AI components not found - creature will have visuals only");
            }
        }

        private void ApplyGeneticsToCreature(GameObject creature, GeneticProfile genetics)
        {
            var creatureInstance = creature.GetComponent<CreatureInstanceComponent>();
            if (creatureInstance == null) return;
            
            // Create creature data
            var creatureData = new Laboratory.Chimera.Breeding.CreatureInstance();
            creatureData.GeneticProfile = genetics;
            creatureData.AgeInDays = Random.Range(20, 50); // Adult age
            creatureData.Happiness = Random.Range(0.6f, 0.9f);
            creatureData.CurrentHealth = 100;
            
            // Initialize the creature
            creatureInstance.Initialize(creatureData);
        }

        private void ApplyAdvancedVisualGenetics(GameObject creature)
        {
            var creatureInstance = creature.GetComponent<CreatureInstanceComponent>();
            if (creatureInstance == null) return;
            
            // Apply enhanced visual genetics
            creatureInstance.ApplyEnhancedVisualGenetics();
            
            if (showVisualDebug)
            {
                var visualIntegration = creature.GetComponent<GeneticVisualIntegration>();
                if (visualIntegration != null)
                {
                    Debug.Log($"🎨 {visualIntegration.GetVisualDebugInfo()}");
                }
            }
        }

        #endregion

        #region Enhanced Genetics Creation

        private GeneticProfile CreateEnhancedGeneticArchetype(CreatureArchetype archetype)
        {
            var baseGenes = CreateArchetypeGenes(archetype);
            var enhancedGenes = AddEnhancedVisualGenes(baseGenes, archetype);
            
            return new GeneticProfile(enhancedGenes, 1);
        }

        private Gene[] CreateArchetypeGenes(CreatureArchetype archetype)
        {
            // Use the existing archetype creation logic from the original spawner
            switch (archetype)
            {
                case CreatureArchetype.AlphaWolf:
                    return CreateGenes(aggression: 0.9f, dominance: 0.9f, packInstinct: 0.8f, territoriality: 0.7f, courage: 0.8f);
                    
                case CreatureArchetype.CuriousCat:
                    return CreateGenes(curiosity: 0.9f, playfulness: 0.8f, intelligence: 0.7f, packInstinct: 0.2f);
                    
                case CreatureArchetype.LoyalDog:
                    return CreateGenes(loyalty: 0.9f, social: 0.8f, aggression: 0.3f, packInstinct: 0.7f);
                    
                case CreatureArchetype.NightHunter:
                    return CreateGenes(huntingDrive: 0.9f, nightVision: 0.9f, courage: 0.8f, territoriality: 0.6f, aggression: 0.7f);
                    
                case CreatureArchetype.PeacefulHerbivore:
                    return CreateGenes(aggression: 0.1f, social: 0.7f, playfulness: 0.6f);
                    
                case CreatureArchetype.TerritorialGuardian:
                    return CreateGenes(territoriality: 0.9f, courage: 0.9f, size: 0.8f, dominance: 0.7f);
                    
                case CreatureArchetype.SmartTrickster:
                    return CreateGenes(intelligence: 0.9f, curiosity: 0.8f, playfulness: 0.7f, aggression: 0.4f);
                    
                case CreatureArchetype.SocialHerdAnimal:
                    return CreateGenes(social: 0.9f, packInstinct: 0.8f, loyalty: 0.6f, aggression: 0.3f);
                    
                case CreatureArchetype.WiseElder:
                    return CreateGenes(intelligence: 0.9f, nightVision: 0.8f);
                    
                case CreatureArchetype.ApexPredator:
                    return CreateGenes(huntingDrive: 0.9f, aggression: 0.8f, courage: 0.9f, size: 0.9f, packInstinct: 0.2f);
                    
                default:
                    return CreateRandomGenes();
            }
        }

        private Gene[] AddEnhancedVisualGenes(Gene[] baseGenes, CreatureArchetype archetype)
        {
            var visualGenes = new System.Collections.Generic.List<Gene>(baseGenes);
            
            // Add color genetics based on archetype
            visualGenes.AddRange(CreateVisualGenes(archetype));
            
            // Add pattern genetics
            visualGenes.AddRange(CreatePatternGenes(archetype));
            
            // Add magical genetics (rare)
            if (Random.value < 0.3f)
            {
                visualGenes.AddRange(CreateMagicalGenes(archetype));
            }
            
            // Add biome adaptation
            visualGenes.AddRange(CreateBiomeGenes(currentBiome));
            
            return visualGenes.ToArray();
        }

        private Gene[] CreateVisualGenes(CreatureArchetype archetype)
        {
            Color archetypeColor = GetArchetypeColor(archetype);
            float hue, sat, val;
            Color.RGBToHSV(archetypeColor, out hue, out sat, out val);
            
            return new Gene[]
            {
                new Gene
                {
                    traitName = "Primary Color",
                    traitType = GeneticsTraitType.Physical,
                    value = hue,
                    dominance = 0.8f,
                    expression = GeneExpression.Normal,
                    isActive = true
                },
                new Gene
                {
                    traitName = "Secondary Color",
                    traitType = GeneticsTraitType.Physical,
                    value = (hue + 0.3f) % 1f, // Complementary
                    dominance = 0.6f,
                    expression = GeneExpression.Normal,
                    isActive = Random.value > 0.3f
                },
                new Gene
                {
                    traitName = "Eye Color",
                    traitType = GeneticsTraitType.Physical,
                    value = Random.value,
                    dominance = 0.7f,
                    expression = GeneExpression.Normal,
                    isActive = true
                }
            };
        }

        private Gene[] CreatePatternGenes(CreatureArchetype archetype)
        {
            var patterns = new string[] { "Stripe Pattern", "Spot Pattern", "Gradient Pattern" };
            
            if (ShouldHavePattern(archetype))
            {
                return new Gene[]
                {
                    new Gene
                    {
                        traitName = patterns[Random.Range(0, patterns.Length)],
                        traitType = GeneticsTraitType.Physical,
                        value = Random.Range(0.4f, 0.8f),
                        dominance = Random.Range(0.4f, 0.7f),
                        expression = GeneExpression.Normal,
                        isActive = true
                    }
                };
            }
            
            return new Gene[0];
        }

        private Gene[] CreateMagicalGenes(CreatureArchetype archetype)
        {
            var magicTypes = new string[] { "Fire Affinity", "Ice Affinity", "Lightning Affinity", "Shadow Affinity", "Light Affinity" };
            var chosenMagic = magicTypes[Random.Range(0, magicTypes.Length)];
            
            return new Gene[]
            {
                new Gene
                {
                    traitName = chosenMagic,
                    traitType = GeneticsTraitType.Magical,
                    value = Random.Range(0.5f, 0.9f),
                    dominance = Random.Range(0.5f, 0.8f),
                    expression = Random.value > 0.8f ? GeneExpression.Enhanced : GeneExpression.Normal,
                    isActive = true
                }
            };
        }

        private Gene[] CreateBiomeGenes(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert:
                    return new Gene[]
                    {
                        new Gene { traitName = "Desert Adaptation", traitType = GeneticsTraitType.Physical, value = 0.8f, dominance = 0.7f, isActive = true },
                        new Gene { traitName = "Heat Resistance", traitType = GeneticsTraitType.Metabolic, value = 0.7f, dominance = 0.6f, isActive = true }
                    };
                    
                case BiomeType.Arctic:
                    return new Gene[]
                    {
                        new Gene { traitName = "Arctic Adaptation", traitType = GeneticsTraitType.Physical, value = 0.8f, dominance = 0.7f, isActive = true },
                        new Gene { traitName = "Cold Resistance", traitType = GeneticsTraitType.Metabolic, value = 0.7f, dominance = 0.6f, isActive = true }
                    };
                    
                case BiomeType.Ocean:
                    return new Gene[]
                    {
                        new Gene { traitName = "Ocean Adaptation", traitType = GeneticsTraitType.Physical, value = 0.8f, dominance = 0.7f, isActive = true },
                        new Gene { traitName = "Swimming", traitType = GeneticsTraitType.Utility, value = 0.9f, dominance = 0.8f, isActive = true }
                    };
                    
                default:
                    return new Gene[0];
            }
        }

        private GeneticProfile CreateEnhancedRandomGenetics()
        {
            if (traitLibrary != null)
                return traitLibrary.CreateRichGeneticProfile(currentBiome, 1);
            
            // Fallback random genetics
            var randomGenes = CreateRandomGenes();
            var enhancedGenes = AddEnhancedVisualGenes(randomGenes, CreatureArchetype.AlphaWolf);
            return new GeneticProfile(enhancedGenes, 1);
        }

        #endregion

        #region Helper Methods

        private Color GetArchetypeColor(CreatureArchetype archetype)
        {
            switch (archetype)
            {
                case CreatureArchetype.AlphaWolf: return Color.grey;
                case CreatureArchetype.CuriousCat: return new Color(1f, 0.5f, 0f); // Orange
                case CreatureArchetype.LoyalDog: return new Color(0.6f, 0.4f, 0.2f); // Brown
                case CreatureArchetype.NightHunter: return Color.black;
                case CreatureArchetype.PeacefulHerbivore: return Color.green;
                case CreatureArchetype.TerritorialGuardian: return Color.red;
                case CreatureArchetype.SmartTrickster: return Color.blue;
                case CreatureArchetype.SocialHerdAnimal: return Color.white;
                case CreatureArchetype.WiseElder: return new Color(0.5f, 0.5f, 0.8f); // Wise purple
                case CreatureArchetype.ApexPredator: return Color.red;
                default: return Color.white;
            }
        }

        private bool ShouldHavePattern(CreatureArchetype archetype)
        {
            switch (archetype)
            {
                case CreatureArchetype.CuriousCat:
                case CreatureArchetype.NightHunter:
                case CreatureArchetype.ApexPredator:
                    return Random.value > 0.4f; // 60% chance
                default:
                    return Random.value > 0.7f; // 30% chance
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 randomDirection = Random.insideUnitSphere * spawnRadius;
            randomDirection.y = 0f;
            Vector3 spawnPos = transform.position + randomDirection;
            
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, spawnRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return spawnPos;
        }

        private void CycleBiome()
        {
            var biomes = System.Enum.GetValues(typeof(BiomeType));
            int currentIndex = System.Array.IndexOf(biomes, currentBiome);
            int nextIndex = (currentIndex + 1) % biomes.Length;
            currentBiome = (BiomeType)biomes.GetValue(nextIndex);
            
            Debug.Log($"🌍 Switched to biome: {currentBiome}");
        }

        private void CycleArchetype()
        {
            var archetypes = System.Enum.GetValues(typeof(CreatureArchetype));
            int currentIndex = System.Array.IndexOf(archetypes, selectedArchetype);
            int nextIndex = (currentIndex + 1) % archetypes.Length;
            selectedArchetype = (CreatureArchetype)archetypes.GetValue(nextIndex);
            
            Debug.Log($"🧬 Switched to archetype: {selectedArchetype}");
        }

        [ContextMenu("Clear All Spawned Creatures")]
        public void ClearAllSpawnedCreatures()
        {
            var spawnedCreatures = GameObject.FindGameObjectsWithTag("SpawnedCreature");
            foreach (var creature in spawnedCreatures)
            {
                DestroyImmediate(creature);
            }
            
            totalSpawnedCreatures = 0;
            Debug.Log($"🧹 Cleared {spawnedCreatures.Length} spawned creatures");
        }

        #endregion

        #region Logging and Debug

        private void LogSpawnInfo(GameObject creature, string type)
        {
            if (!showSpawnInfo) return;
            
            var creatureInstance = creature.GetComponent<CreatureInstanceComponent>();
            if (creatureInstance != null)
            {
                Debug.Log($"✅ Spawned {type}: {creature.name}");
                Debug.Log($"📊 {creatureInstance.GetInfoText()}");
                
                if (showVisualDebug)
                {
                    var genetics = creatureInstance.CreatureData?.GeneticProfile;
                    if (genetics != null)
                    {
                        Debug.Log($"🎨 Visual traits: {genetics.GetTraitSummary(5)}");
                        Debug.Log($"🧬 Genetic purity: {genetics.GetGeneticPurity():P1}");
                        Debug.Log($"🔀 Mutations: {genetics.Mutations.Count}");
                    }
                }
            }
        }

        private void LogBreedingInfo(GameObject parent1, GameObject parent2, GameObject offspring)
        {
            Debug.Log("👨‍👩‍👧‍👦 === BREEDING RESULTS ===");
            LogCreatureGenetics(parent1, "Parent 1");
            LogCreatureGenetics(parent2, "Parent 2");
            LogCreatureGenetics(offspring, "Offspring");
            Debug.Log("=== END BREEDING RESULTS ===");
        }

        private void LogCreatureGenetics(GameObject creature, string role)
        {
            var creatureInstance = creature.GetComponent<CreatureInstanceComponent>();
            if (creatureInstance?.CreatureData?.GeneticProfile != null)
            {
                var genetics = creatureInstance.CreatureData.GeneticProfile;
                Debug.Log($"{role}: Traits={genetics.GetTraitSummary(3)}, Purity={genetics.GetGeneticPurity():P1}, Gen={genetics.Generation}");
            }
        }

        // Include the CreateGenes method from the original spawner
        private Gene[] CreateGenes(
            float aggression = -1f, float curiosity = -1f, float intelligence = -1f,
            float loyalty = -1f, float social = -1f, float playfulness = -1f,
            float courage = -1f, float territoriality = -1f, float packInstinct = -1f,
            float huntingDrive = -1f, float parentalCare = -1f, float dominance = -1f,
            float nightVision = -1f, float size = -1f)
        {
            return new Gene[]
            {
                new Gene { traitName = "Aggression", value = aggression >= 0 ? aggression : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Curiosity", value = curiosity >= 0 ? curiosity : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Intelligence", value = intelligence >= 0 ? intelligence : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Loyalty", value = loyalty >= 0 ? loyalty : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Social", value = social >= 0 ? social : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Playfulness", value = playfulness >= 0 ? playfulness : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Courage", value = courage >= 0 ? courage : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Territoriality", value = territoriality >= 0 ? territoriality : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "PackInstinct", value = packInstinct >= 0 ? packInstinct : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "HuntingDrive", value = huntingDrive >= 0 ? huntingDrive : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "ParentalCare", value = parentalCare >= 0 ? parentalCare : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Dominance", value = dominance >= 0 ? dominance : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "NightVision", value = nightVision >= 0 ? nightVision : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Size", value = size >= 0 ? size : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true }
            };
        }

        private Gene[] CreateRandomGenes()
        {
            return CreateGenes(); // All parameters default to random values
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }

        #endregion
    }
}