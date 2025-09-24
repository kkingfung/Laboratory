using UnityEngine;
using UnityEngine.AI;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.AI;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;

namespace Laboratory.Chimera.Testing
{
    /// <summary>
    /// SUPER SIMPLE creature spawner for quick testing of genetic behaviors.
    /// Just drag this onto any GameObject and press F1 in play mode!
    /// </summary>
    public class QuickGeneticCreatureSpawner : MonoBehaviour
    {
        [Header("🧬 Quick Spawn Settings")]
        [SerializeField] private int spawnCount = 1;
        [SerializeField] private float spawnRadius = 5f;
        [SerializeField] private CreatureArchetype selectedArchetype = CreatureArchetype.AlphaWolf;
        
        [Header("🎮 Controls")]
        [SerializeField] private KeyCode spawnKey = KeyCode.F1;
        [SerializeField] private KeyCode randomKey = KeyCode.F2;
        [SerializeField] private KeyCode clearKey = KeyCode.F3;
        
        [Header("📊 Debug")]
        [SerializeField] private bool showSpawnInfo = true;
        [SerializeField] private bool enableBehaviorDebug = true;

        private void Update()
        {
            if (Input.GetKeyDown(spawnKey))
            {
                SpawnCreatureWithArchetype(selectedArchetype);
            }
            
            if (Input.GetKeyDown(randomKey))
            {
                SpawnRandomCreature();
            }
            
            if (Input.GetKeyDown(clearKey))
            {
                ClearAllSpawnedCreatures();
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, Screen.height - 150, 300, 140));
            
            GUILayout.Label("🧬 QUICK GENETIC SPAWNER", new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold });
            GUILayout.Label($"F1: Spawn {selectedArchetype}");
            GUILayout.Label($"F2: Spawn Random Creature");
            GUILayout.Label($"F3: Clear All Creatures");
            GUILayout.Space(10);
            
            GUILayout.Label("Current Archetype:");
            if (GUILayout.Button(selectedArchetype.ToString()))
            {
                CycleArchetype();
            }
            
            GUILayout.EndArea();
        }

        [ContextMenu("Spawn Selected Archetype")]
        public void SpawnCreatureWithArchetype(CreatureArchetype archetype)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPos = GetRandomSpawnPosition();
                GameObject creature = CreateCreatureGameObject($"{archetype}_Creature_{i + 1}", spawnPos);

                SetupCreatureComponents(creature);
                ApplyGeneticArchetype(creature, archetype);

                if (showSpawnInfo)
                {
                    var integration = creature.GetComponent<GeneticBehaviorIntegration>();
                    Debug.Log($"[QuickSpawner] Spawned {archetype} at {spawnPos}");

                    if (integration != null)
                    {
                        Debug.Log($"[QuickSpawner] {integration.GetComprehensiveBehaviorReport()}");
                    }
                }
            }
        }

        [ContextMenu("Spawn Random Creature")]
        public void SpawnRandomCreature()
        {
            var archetypes = System.Enum.GetValues(typeof(CreatureArchetype));
            var randomArchetype = (CreatureArchetype)archetypes.GetValue(Random.Range(0, archetypes.Length));
            SpawnCreatureWithArchetype(randomArchetype);
        }

        [ContextMenu("Clear All Spawned Creatures")]
        public void ClearAllSpawnedCreatures()
        {
            var spawnedCreatures = GameObject.FindGameObjectsWithTag("SpawnedCreature");
            foreach (var creature in spawnedCreatures)
            {
                DestroyImmediate(creature);
            }
            
            Debug.Log($"[QuickSpawner] Cleared {spawnedCreatures.Length} spawned creatures");
        }

        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 randomDirection = Random.insideUnitSphere * spawnRadius;
            randomDirection.y = 0f; // Keep on ground level
            Vector3 spawnPos = transform.position + randomDirection;
            
            // Try to place on NavMesh
            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, spawnRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return spawnPos; // Fallback to original position
        }

        private GameObject CreateCreatureGameObject(string name, Vector3 position)
        {
            GameObject creature = new GameObject(name);
            creature.transform.position = position;
            creature.tag = "SpawnedCreature"; // For easy cleanup
            
            // Add visual representation
            AddSimpleVisualRepresentation(creature);
            
            return creature;
        }

        private void AddSimpleVisualRepresentation(GameObject creature)
        {
            // Create simple capsule visual
            var meshRenderer = creature.AddComponent<MeshRenderer>();
            var meshFilter = creature.AddComponent<MeshFilter>();
            
            // Create capsule mesh
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            meshFilter.mesh = primitive.GetComponent<MeshFilter>().mesh;
            DestroyImmediate(primitive);
            
            // Random color for each creature
            var material = new Material(Shader.Find("Standard"));
            material.color = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f);
            meshRenderer.material = material;
            
            // Add collider
            var collider = creature.AddComponent<CapsuleCollider>();
            collider.radius = 0.5f;
            collider.height = 2f;
        }

        private void SetupCreatureComponents(GameObject creature)
        {
            // Add NavMeshAgent
            var navAgent = creature.AddComponent<NavMeshAgent>();
            navAgent.speed = Random.Range(2f, 6f);
            navAgent.angularSpeed = 180f;
            navAgent.radius = 0.5f;
            navAgent.height = 2f;

            // Add Animator (placeholder)
            var animator = creature.AddComponent<Animator>();

            // Add core creature components
            var creatureInstance = creature.AddComponent<CreatureInstanceComponent>();
            var monsterAI = creature.AddComponent<ChimeraMonsterAI>();
            var geneticAdapter = creature.AddComponent<GeneticBehaviorAdapter>();
            var advancedExtensions = creature.AddComponent<AdvancedGeneticBehaviorComponent>();
            var integration = creature.AddComponent<GeneticBehaviorIntegration>();
            
            // Enable debug features
            if (enableBehaviorDebug)
            {
                // Note: Enable debug features through inspector instead of code
                // as these properties may be private serialized fields
            }
        }

        private void ApplyGeneticArchetype(GameObject creature, CreatureArchetype archetype)
        {
            var creatureInstance = creature.GetComponent<CreatureInstanceComponent>();
            if (creatureInstance == null) return;

            Gene[] genes = CreateGeneticArchetype(archetype);
            var creatureData = new CreatureInstance();
            creatureData.GeneticProfile = new GeneticProfile(genes, 1);
            creatureInstance.Initialize(creatureData);
            
            // Update creature name to reflect personality
            var extensions = creature.GetComponent<AdvancedGeneticBehaviorComponent>();
            if (extensions != null)
            {
                StartCoroutine(UpdateCreatureNameAfterInit(creature, extensions));
            }
        }

        private System.Collections.IEnumerator UpdateCreatureNameAfterInit(GameObject creature, AdvancedGeneticBehaviorComponent extensions)
        {
            yield return new WaitForSeconds(0.1f); // Wait for initialization
            
            string personality = extensions.GetPersonalityDescription();
            creature.name = $"{selectedArchetype}_{personality}";
        }

        private Gene[] CreateGeneticArchetype(CreatureArchetype archetype)
        {
            switch (archetype)
            {
                case CreatureArchetype.AlphaWolf:
                    return CreateGenes(aggression: 0.9f, dominance: 0.9f, packInstinct: 0.8f, territoriality: 0.7f, courage: 0.8f);
                    
                case CreatureArchetype.CuriousCat:
                    return CreateGenes(curiosity: 0.9f, playfulness: 0.8f, intelligence: 0.7f, packInstinct: 0.2f, independence: 0.8f);
                    
                case CreatureArchetype.LoyalDog:
                    return CreateGenes(loyalty: 0.9f, social: 0.8f, aggression: 0.3f, packInstinct: 0.7f, parentalCare: 0.6f);
                    
                case CreatureArchetype.NightHunter:
                    return CreateGenes(huntingDrive: 0.9f, nightVision: 0.9f, courage: 0.8f, territoriality: 0.6f, aggression: 0.7f);
                    
                case CreatureArchetype.PeacefulHerbivore:
                    return CreateGenes(aggression: 0.1f, social: 0.7f, playfulness: 0.6f, adaptability: 0.8f, emotionalStability: 0.8f);
                    
                case CreatureArchetype.TerritorialGuardian:
                    return CreateGenes(territoriality: 0.9f, parentalCare: 0.8f, courage: 0.9f, size: 0.8f, dominance: 0.7f);
                    
                case CreatureArchetype.SmartTrickster:
                    return CreateGenes(intelligence: 0.9f, curiosity: 0.8f, adaptability: 0.9f, playfulness: 0.7f, aggression: 0.4f);
                    
                case CreatureArchetype.SocialHerdAnimal:
                    return CreateGenes(social: 0.9f, packInstinct: 0.8f, loyalty: 0.6f, emotionalStability: 0.7f, aggression: 0.3f);
                    
                case CreatureArchetype.WiseElder:
                    return CreateGenes(intelligence: 0.9f, emotionalStability: 0.9f, territoriality: 0.5f, nightVision: 0.8f, adaptability: 0.8f);
                    
                case CreatureArchetype.ApexPredator:
                    return CreateGenes(huntingDrive: 0.9f, aggression: 0.8f, courage: 0.9f, size: 0.9f, packInstinct: 0.2f);
                    
                default:
                    return CreateRandomGenes();
            }
        }

        private Gene[] CreateGenes(
            float aggression = -1f, float curiosity = -1f, float intelligence = -1f,
            float loyalty = -1f, float social = -1f, float playfulness = -1f,
            float courage = -1f, float territoriality = -1f, float packInstinct = -1f,
            float huntingDrive = -1f, float parentalCare = -1f, float dominance = -1f,
            float nightVision = -1f, float size = -1f, float independence = -1f,
            float emotionalStability = -1f, float adaptability = -1f)
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
                new Gene { traitName = "Size", value = size >= 0 ? size : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "EmotionalStability", value = emotionalStability >= 0 ? emotionalStability : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true },
                new Gene { traitName = "Adaptability", value = adaptability >= 0 ? adaptability : Random.Range(0.3f, 0.7f), dominance = 0.5f, isActive = true }
            };
        }

        private Gene[] CreateRandomGenes()
        {
            return CreateGenes(); // All parameters default to random values
        }

        private void CycleArchetype()
        {
            var archetypes = System.Enum.GetValues(typeof(CreatureArchetype));
            int currentIndex = System.Array.IndexOf(archetypes, selectedArchetype);
            int nextIndex = (currentIndex + 1) % archetypes.Length;
            selectedArchetype = (CreatureArchetype)archetypes.GetValue(nextIndex);
        }
    }

    public enum CreatureArchetype
    {
        AlphaWolf,
        CuriousCat,
        LoyalDog,
        NightHunter,
        PeacefulHerbivore,
        TerritorialGuardian,
        SmartTrickster,
        SocialHerdAnimal,
        WiseElder,
        ApexPredator
    }
}