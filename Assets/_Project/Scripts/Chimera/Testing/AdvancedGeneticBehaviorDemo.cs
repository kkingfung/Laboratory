using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.AI;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Testing
{
    /// <summary>
    /// ULTIMATE genetic behavior testing scene that demonstrates the incredible
    /// emergent behaviors from the advanced genetic system.
    /// </summary>
    public class AdvancedGeneticBehaviorDemo : MonoBehaviour
    {
        [Header("🧪 Demo Configuration")]
        [SerializeField] private int numberOfCreatures = 10;
        [SerializeField] private float spawnRadius = 20f;
        [SerializeField] private bool enableRealTimeDemo = true;
        [SerializeField] private bool showBehaviorDebug = true;

        [Header("🧬 Genetic Presets")]
        [SerializeField] private GeneticPreset[] geneticPresets;

        [Header("🎯 Demo Scenarios")]
        [SerializeField] private DemoScenario currentScenario = DemoScenario.NaturalBehavior;
        [SerializeField] private float scenarioTimer = 60f; // Switch scenarios every minute

        [Header("📊 Real-time Monitoring")]
        [SerializeField] private bool enableBehaviorAnalytics = true;
        [SerializeField] private BehaviorAnalytics analytics;

        // Demo state
        private List<GameObject> spawnedCreatures = new List<GameObject>();
        private float lastScenarioChange = 0f;
        private int currentPresetIndex = 0;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeDemo();
            SpawnTestCreatures();
            
            if (enableRealTimeDemo)
            {
                StartCoroutine(RunDemoScenarios());
            }
        }

        private void Update()
        {
            if (enableBehaviorAnalytics)
            {
                UpdateBehaviorAnalytics();
            }

            if (showBehaviorDebug && Time.frameCount % 60 == 0) // Every second
            {
                LogCreatureBehaviors();
            }
        }

        private void OnGUI()
        {
            if (!enableBehaviorAnalytics) return;

            DisplayBehaviorAnalytics();
        }

        #endregion

        #region Demo Initialization

        private void InitializeDemo()
        {
            analytics = new BehaviorAnalytics();
            
            // Create genetic presets if not assigned
            if (geneticPresets == null || geneticPresets.Length == 0)
            {
                CreateDefaultGeneticPresets();
            }

            Debug.Log("[GeneticDemo] Advanced Genetic Behavior Demo initialized!");
        }

        private void CreateDefaultGeneticPresets()
        {
            geneticPresets = new GeneticPreset[]
            {
                CreatePreset("🐺 Alpha Wolf", 
                    aggression: 0.9f, dominance: 0.9f, packInstinct: 0.8f, territoriality: 0.7f),
                
                CreatePreset("🐱 Curious Cat", 
                    curiosity: 0.9f, playfulness: 0.8f, intelligence: 0.7f, independence: 0.8f),
                
                CreatePreset("🐕 Loyal Dog", 
                    loyalty: 0.9f, social: 0.8f, aggression: 0.3f, packInstinct: 0.7f),
                
                CreatePreset("🦅 Night Hunter", 
                    huntingDrive: 0.9f, nightVision: 0.9f, courage: 0.8f, territoriality: 0.6f),
                
                CreatePreset("🐰 Peaceful Herbivore", 
                    aggression: 0.1f, social: 0.7f, playfulness: 0.6f, adaptability: 0.8f),
                
                CreatePreset("🐻 Territorial Guardian", 
                    territoriality: 0.9f, parentalCare: 0.8f, courage: 0.9f, size: 0.8f),
                
                CreatePreset("🦊 Smart Trickster", 
                    intelligence: 0.9f, curiosity: 0.8f, adaptability: 0.9f, playfulness: 0.7f),
                
                CreatePreset("🐎 Social Herd Animal", 
                    social: 0.9f, packInstinct: 0.8f, loyalty: 0.6f, emotionalStability: 0.7f),
                
                CreatePreset("🦉 Wise Elder", 
                    intelligence: 0.9f, emotionalStability: 0.9f, territoriality: 0.5f, nightVision: 0.8f),
                
                CreatePreset("🐅 Apex Predator", 
                    huntingDrive: 0.9f, aggression: 0.8f, courage: 0.9f, size: 0.9f, independence: 0.8f)
            };
        }

        private GeneticPreset CreatePreset(string name, 
            float aggression = 0.5f, float curiosity = 0.5f, float intelligence = 0.5f, 
            float loyalty = 0.5f, float social = 0.5f, float playfulness = 0.5f,
            float courage = 0.5f, float territoriality = 0.5f, float packInstinct = 0.5f,
            float huntingDrive = 0.5f, float parentalCare = 0.5f, float dominance = 0.5f,
            float nightVision = 0.5f, float size = 0.5f, float independence = 0.5f,
            float emotionalStability = 0.5f, float adaptability = 0.5f)
        {
            var genes = new List<Gene>
            {
                new Gene { traitName = "Aggression", value = aggression, dominance = 0.5f, isActive = true },
                new Gene { traitName = "Curiosity", value = curiosity, dominance = 0.5f, isActive = true },
                new Gene { traitName = "Intelligence", value = intelligence, dominance = 0.5f, isActive = true },
                new Gene { traitName = "Loyalty", value = loyalty, dominance = 0.5f, isActive = true },
                new Gene { traitName = "Social", value = social, dominance = 0.5f, isActive = true },
                new Gene { traitName = "Playfulness", value = playfulness, dominance = 0.5f, isActive = true },
                new Gene { traitName = "Courage", value = courage, dominance = 0.5f, isActive = true },
                new Gene { traitName = "Territoriality", value = territoriality, dominance = 0.5f, isActive = true },
                new Gene { traitName = "PackInstinct", value = packInstinct, dominance = 0.5f, isActive = true },
                new Gene { traitName = "HuntingDrive", value = huntingDrive, dominance = 0.5f, isActive = true },
                new Gene { traitName = "ParentalCare", value = parentalCare, dominance = 0.5f, isActive = true },
                new Gene { traitName = "Dominance", value = dominance, dominance = 0.5f, isActive = true },
                new Gene { traitName = "NightVision", value = nightVision, dominance = 0.5f, isActive = true },
                new Gene { traitName = "Size", value = size, dominance = 0.5f, isActive = true },
                new Gene { traitName = "EmotionalStability", value = emotionalStability, dominance = 0.5f, isActive = true },
                new Gene { traitName = "Adaptability", value = adaptability, dominance = 0.5f, isActive = true }
            };

            return new GeneticPreset
            {
                name = name,
                genes = genes.ToArray()
            };
        }

        #endregion

        #region Creature Spawning

        private void SpawnTestCreatures()
        {
            for (int i = 0; i < numberOfCreatures; i++)
            {
                SpawnCreatureWithPreset(i % geneticPresets.Length);
            }

            Debug.Log($"[GeneticDemo] Spawned {numberOfCreatures} creatures with advanced genetic behaviors!");
        }

        private void SpawnCreatureWithPreset(int presetIndex)
        {
            // Create basic creature GameObject
            var creatureGO = new GameObject($"Creature_{presetIndex}_{geneticPresets[presetIndex].name}");
            
            // Position randomly around spawn center
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * spawnRadius;
            spawnPos.y = transform.position.y;
            creatureGO.transform.position = spawnPos;

            // Add core components
            SetupCreatureComponents(creatureGO, presetIndex);
            
            spawnedCreatures.Add(creatureGO);
        }

        private void SetupCreatureComponents(GameObject creatureGO, int presetIndex)
        {
            // Add NavMeshAgent
            var navAgent = creatureGO.AddComponent<UnityEngine.AI.NavMeshAgent>();
            navAgent.speed = Random.Range(2f, 6f);
            navAgent.angularSpeed = 180f;
            navAgent.radius = 0.5f;

            // Add Animator (placeholder)
            var animator = creatureGO.AddComponent<Animator>();

            // Add CreatureInstance with genetic profile
            var creatureInstance = creatureGO.AddComponent<CreatureInstanceComponent>();
            var preset = geneticPresets[presetIndex];
            var creatureData = new Laboratory.Chimera.Breeding.CreatureInstance();
            creatureData.GeneticProfile = new GeneticProfile(preset.genes, 1);
            creatureData.AgeInDays = 30; // Adult
            creatureData.Happiness = 0.8f;
            creatureData.CurrentHealth = 100;
            creatureInstance.Initialize(creatureData);

            // Add visual representation
            AddVisualRepresentation(creatureGO, presetIndex);

            // Add AI components
            var monsterAI = creatureGO.AddComponent<ChimeraMonsterAI>();
            var geneticAdapter = creatureGO.AddComponent<GeneticBehaviorAdapter>();
            var advancedExtensions = creatureGO.AddComponent<AdvancedGeneticBehaviorComponent>();
            var integration = creatureGO.AddComponent<GeneticBehaviorIntegration>();

            // Configure AI based on genetics
            ConfigureAIFromGenetics(monsterAI, preset);

            Debug.Log($"[GeneticDemo] Created {preset.name} with advanced genetic behaviors");
        }

        private void AddVisualRepresentation(GameObject creatureGO, int presetIndex)
        {
            // Create simple visual representation
            var renderer = creatureGO.AddComponent<MeshRenderer>();
            var meshFilter = creatureGO.AddComponent<MeshFilter>();
            
            // Use primitive shapes
            meshFilter.mesh = CreateCreatureMesh();
            
            // Color based on genetic preset
            var material = new Material(Shader.Find("Standard"));
            material.color = GetPresetColor(presetIndex);
            renderer.material = material;

            // Add collider
            var collider = creatureGO.AddComponent<CapsuleCollider>();
            collider.radius = 0.5f;
            collider.height = 1f;
        }

        private Mesh CreateCreatureMesh()
        {
            // Simple capsule mesh for creatures
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var mesh = primitive.GetComponent<MeshFilter>().mesh;
            DestroyImmediate(primitive);
            return mesh;
        }

        private Color GetPresetColor(int presetIndex)
        {
            Color[] colors = new Color[]
            {
                Color.red,      // Alpha Wolf
                Color.yellow,   // Curious Cat
                Color.blue,     // Loyal Dog
                Color.black,    // Night Hunter
                Color.green,    // Peaceful Herbivore
                Color.brown,    // Territorial Guardian
                Color.orange,   // Smart Trickster
                Color.white,    // Social Herd Animal
                Color.gray,     // Wise Elder
                Color.magenta   // Apex Predator
            };

            return colors[presetIndex % colors.Length];
        }

        private void ConfigureAIFromGenetics(ChimeraMonsterAI monsterAI, GeneticPreset preset)
        {
            // Set initial behavior based on genetic makeup
            var aggression = GetGeneValue(preset, "Aggression");
            var huntingDrive = GetGeneValue(preset, "HuntingDrive");
            var loyalty = GetGeneValue(preset, "Loyalty");
            var territoriality = GetGeneValue(preset, "Territoriality");

            if (huntingDrive > 0.7f && aggression > 0.6f)
                monsterAI.SetBehaviorType(AIBehaviorType.Predator);
            else if (territoriality > 0.7f)
                monsterAI.SetBehaviorType(AIBehaviorType.Guard);
            else if (loyalty > 0.7f)
                monsterAI.SetBehaviorType(AIBehaviorType.Companion);
            else if (aggression < 0.3f)
                monsterAI.SetBehaviorType(AIBehaviorType.Herbivore);
            else
                monsterAI.SetBehaviorType(AIBehaviorType.Wild);
        }

        private float GetGeneValue(GeneticPreset preset, string traitName)
        {
            foreach (var gene in preset.genes)
            {
                if (gene.traitName == traitName)
                    return gene.value ?? 0.5f;
            }
            return 0.5f;
        }

        #endregion

        #region Demo Scenarios

        private System.Collections.IEnumerator RunDemoScenarios()
        {
            while (true)
            {
                yield return new WaitForSeconds(scenarioTimer);

                // Track time since last scenario change
                lastScenarioChange = Time.time;

                // Cycle through demo scenarios
                currentScenario = (DemoScenario)(((int)currentScenario + 1) % System.Enum.GetValues(typeof(DemoScenario)).Length);
                ApplyDemoScenario();

                // Use preset index for cycling behavior
                currentPresetIndex = (currentPresetIndex + 1) % 5; // Cycle through 5 presets

                Debug.Log($"[GeneticDemo] Switched to scenario: {currentScenario} (Preset {currentPresetIndex})");
            }
        }

        private void ApplyDemoScenario()
        {
            switch (currentScenario)
            {
                case DemoScenario.NaturalBehavior:
                    // Let creatures behave naturally
                    break;
                    
                case DemoScenario.PackFormation:
                    DemonstratePackFormation();
                    break;
                    
                case DemoScenario.TerritorialDispute:
                    DemonstrateTerritorialDispute();
                    break;
                    
                case DemoScenario.PredatorPrey:
                    DemonstratePredatorPrey();
                    break;
                    
                case DemoScenario.SocialBonding:
                    DemonstrateSocialBonding();
                    break;
                    
                case DemoScenario.NightBehavior:
                    DemonstrateNightBehavior();
                    break;
            }
        }

        private void DemonstratePackFormation()
        {
            // Bring social creatures closer together to trigger pack formation
            var socialCreatures = GetCreaturesByTrait("Social", 0.6f);
            
            Vector3 centerPoint = Vector3.zero;
            foreach (var creature in socialCreatures)
            {
                centerPoint += creature.transform.position;
            }
            centerPoint /= socialCreatures.Count;

            foreach (var creature in socialCreatures)
            {
                Vector3 targetPos = centerPoint + Random.insideUnitSphere * 5f;
                targetPos.y = creature.transform.position.y;
                
                var ai = creature.GetComponent<ChimeraMonsterAI>();
                if (ai != null)
                {
                    ai.SetDestinationPublic(targetPos);
                }
            }
        }

        private void DemonstrateTerritorialDispute()
        {
            // Move territorial creatures into each other's territories
            var territorialCreatures = GetCreaturesByTrait("Territoriality", 0.6f);
            
            if (territorialCreatures.Count >= 2)
            {
                var creature1 = territorialCreatures[0];
                var creature2 = territorialCreatures[1];
                
                // Move them close to each other
                Vector3 midpoint = (creature1.transform.position + creature2.transform.position) / 2f;
                
                creature1.transform.position = midpoint + Vector3.left * 3f;
                creature2.transform.position = midpoint + Vector3.right * 3f;
            }
        }

        private void DemonstratePredatorPrey()
        {
            var predators = GetCreaturesByTrait("HuntingDrive", 0.7f);
            var prey = GetCreaturesByTrait("Aggression", 0.3f, true); // Low aggression = prey
            
            if (predators.Count > 0 && prey.Count > 0)
            {
                var predator = predators[0];
                var target = prey[0];
                
                // Set predator to hunt prey
                var predatorAI = predator.GetComponent<ChimeraMonsterAI>();
                if (predatorAI != null)
                {
                    predatorAI.SetTarget(target.transform);
                }
            }
        }

        private void DemonstrateSocialBonding()
        {
            // Trigger positive social interactions
            var socialCreatures = GetCreaturesByTrait("Social", 0.5f);
            
            foreach (var creature in socialCreatures)
            {
                var extensions = creature.GetComponent<AdvancedGeneticBehaviorComponent>();
                if (extensions != null)
                {
                    // Simulate positive social experience by manipulating emotional state
                    // This would be done through events in the actual system
                }
            }
        }

        private void DemonstrateNightBehavior()
        {
            // Simulate night time for nocturnal creatures
            var nightCreatures = GetCreaturesByTrait("NightVision", 0.6f);
            
            foreach (var creature in nightCreatures)
            {
                var extensions = creature.GetComponent<AdvancedGeneticBehaviorComponent>();
                if (extensions != null)
                {
                    // Night vision creatures should become more active
                    var ai = creature.GetComponent<ChimeraMonsterAI>();
                    if (ai != null)
                    {
                        ai.SetGeneticPatrolRadius(ai.GetGeneticPatrolRadius() * 1.5f);
                    }
                }
            }
        }

        private List<GameObject> GetCreaturesByTrait(string traitName, float minValue, bool inverse = false)
        {
            var result = new List<GameObject>();
            
            foreach (var creature in spawnedCreatures)
            {
                var creatureInstance = creature.GetComponent<CreatureInstanceComponent>();
                if (creatureInstance?.CreatureData?.GeneticProfile != null)
                {
                    var gene = creatureInstance.CreatureData.GeneticProfile.Genes.FirstOrDefault(g => g.traitName == traitName);
                    if (gene.traitName != null) // Check if gene was found (Gene is a struct)
                    {
                        float geneValue = gene.value ?? 0.5f;
                        bool matches = inverse ? geneValue <= minValue : geneValue >= minValue;
                        if (matches)
                        {
                            result.Add(creature);
                        }
                    }
                }
            }
            
            return result;
        }

        #endregion

        #region Behavior Analytics

        private void UpdateBehaviorAnalytics()
        {
            analytics.Update();

            // Check if enough time has passed since last scenario change
            float timeSinceLastChange = Time.time - lastScenarioChange;
            bool shouldTrackBehavior = timeSinceLastChange > 2f; // Allow creatures to adapt to new scenario

            foreach (var creature in spawnedCreatures)
            {
                var integration = creature.GetComponent<GeneticBehaviorIntegration>();
                var ai = creature.GetComponent<ChimeraMonsterAI>();
                var extensions = creature.GetComponent<AdvancedGeneticBehaviorComponent>();

                if (integration != null && ai != null && extensions != null && shouldTrackBehavior)
                {
                    // Track behavior data with preset index information
                    string behaviorData = $"{ai.CurrentState}|Preset{currentPresetIndex}";
                    analytics.TrackCreatureBehavior(creature.name, behaviorData,
                        extensions.GetPersonalityDescription(), extensions.GetPackMembers().Count);
                }
            }
        }

        private void DisplayBehaviorAnalytics()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 600));
            
            GUILayout.Label("🧬 ADVANCED GENETIC BEHAVIOR ANALYTICS", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });
            GUILayout.Space(10);
            
            GUILayout.Label($"📊 Current Scenario: {currentScenario}");
            GUILayout.Label($"🧪 Active Creatures: {spawnedCreatures.Count}");
            GUILayout.Space(10);
            
            // Behavior state distribution
            GUILayout.Label("🎭 Behavior States:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            foreach (var statePair in analytics.GetBehaviorStateDistribution())
            {
                GUILayout.Label($"  {statePair.Key}: {statePair.Value}");
            }
            GUILayout.Space(10);
            
            // Personality distribution
            GUILayout.Label("🧠 Personality Types:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            foreach (var personalityPair in analytics.GetPersonalityDistribution())
            {
                GUILayout.Label($"  {personalityPair.Key}: {personalityPair.Value}");
            }
            GUILayout.Space(10);
            
            // Pack formation stats
            GUILayout.Label("🐺 Pack Statistics:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"  Average Pack Size: {analytics.GetAveragePackSize():F1}");
            GUILayout.Label($"  Creatures in Packs: {analytics.GetCreaturesInPacks()}");
            GUILayout.Space(10);
            
            // Demo controls
            if (GUILayout.Button("🔄 Switch Scenario"))
            {
                currentScenario = (DemoScenario)(((int)currentScenario + 1) % System.Enum.GetValues(typeof(DemoScenario)).Length);
                ApplyDemoScenario();
            }
            
            if (GUILayout.Button("🧬 Mutate Random Creature"))
            {
                MutateRandomCreature();
            }
            
            if (GUILayout.Button("🌍 Reset Demo"))
            {
                ResetDemo();
            }
            
            GUILayout.EndArea();
        }

        private void LogCreatureBehaviors()
        {
            foreach (var creature in spawnedCreatures)
            {
                var integration = creature.GetComponent<GeneticBehaviorIntegration>();
                if (integration != null)
                {
                    Debug.Log($"[BehaviorLog] {creature.name}: {integration.GetComprehensiveBehaviorReport()}");
                }
            }
        }

        #endregion

        #region Demo Controls

        [ContextMenu("Mutate Random Creature")]
        private void MutateRandomCreature()
        {
            if (spawnedCreatures.Count == 0) return;
            
            var randomCreature = spawnedCreatures[Random.Range(0, spawnedCreatures.Count)];
            var creatureInstance = randomCreature.GetComponent<CreatureInstanceComponent>();
            
            if (creatureInstance?.CreatureData?.GeneticProfile != null)
            {
                // Simple mutation - modify a random gene
                var genes = creatureInstance.CreatureData.GeneticProfile.Genes;
                var randomGene = genes[Random.Range(0, genes.Count)];
                randomGene.value = Mathf.Clamp01((randomGene.value ?? 0.5f) + Random.Range(-0.3f, 0.3f));
                
                // Refresh behavior
                var geneticAdapter = randomCreature.GetComponent<GeneticBehaviorAdapter>();
                geneticAdapter?.RefreshGeneticBehavior();
                
                Debug.Log($"[GeneticDemo] Mutated {randomCreature.name} - {randomGene.traitName}: {randomGene.value:F2}");
            }
        }

        [ContextMenu("Reset Demo")]
        private void ResetDemo()
        {
            // Destroy all spawned creatures
            foreach (var creature in spawnedCreatures)
            {
                if (creature != null)
                {
                    DestroyImmediate(creature);
                }
            }
            
            spawnedCreatures.Clear();
            
            // Respawn creatures
            SpawnTestCreatures();
            
            Debug.Log("[GeneticDemo] Demo reset complete!");
        }

        #endregion
    }

    #region Supporting Structures

    [System.Serializable]
    public struct GeneticPreset
    {
        public string name;
        public Gene[] genes;
    }

    public enum DemoScenario
    {
        NaturalBehavior,
        PackFormation,
        TerritorialDispute,
        PredatorPrey,
        SocialBonding,
        NightBehavior
    }

    [System.Serializable]
    public class BehaviorAnalytics
    {
        private Dictionary<string, int> behaviorStates = new Dictionary<string, int>();
        private Dictionary<string, int> personalityTypes = new Dictionary<string, int>();
        private int totalPackSize = 0;
        private int creaturesInPacks = 0;

        public void Update()
        {
            behaviorStates.Clear();
            personalityTypes.Clear();
            totalPackSize = 0;
            creaturesInPacks = 0;
        }

        public void TrackCreatureBehavior(string creatureName, string behaviorState, string personality, int packSize)
        {
            // Track behavior state
            if (behaviorStates.ContainsKey(behaviorState))
                behaviorStates[behaviorState]++;
            else
                behaviorStates[behaviorState] = 1;

            // Track personality
            if (personalityTypes.ContainsKey(personality))
                personalityTypes[personality]++;
            else
                personalityTypes[personality] = 1;

            // Track pack data
            if (packSize > 0)
            {
                totalPackSize += packSize;
                creaturesInPacks++;
            }
        }

        public Dictionary<string, int> GetBehaviorStateDistribution() => behaviorStates;
        public Dictionary<string, int> GetPersonalityDistribution() => personalityTypes;
        public float GetAveragePackSize() => creaturesInPacks > 0 ? (float)totalPackSize / creaturesInPacks : 0f;
        public int GetCreaturesInPacks() => creaturesInPacks;
    }

    #endregion
}
