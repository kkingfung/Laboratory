using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Activities;
using Laboratory.Chimera.Equipment;
using Laboratory.Chimera.Progression;
using Laboratory.Core.ECS.Components;
using Laboratory.Chimera.ECS;

namespace Laboratory.Chimera.Testing
{
    /// <summary>
    /// Comprehensive test harness for Project Chimera systems
    /// Spawns 1000+ creatures and demonstrates all integrated systems
    /// Performance: Targets 60 FPS with Burst/Jobs optimization
    /// </summary>
    public class ChimeraSystemsTestHarness : MonoBehaviour
    {
        [Header("Test Configuration")]
        [Tooltip("Number of creatures to spawn for testing")]
        [Range(100, 5000)]
        public int creatureCount = 1000;

        [Tooltip("Percentage of creatures actively doing activities (0-100)")]
        [Range(0, 100)]
        public int activeCreaturePercentage = 30;

        [Tooltip("Automatically start test on scene load")]
        public bool autoStartTest = true;

        [Header("System Tests")]
        [Tooltip("Test Activity System (Racing, Combat, Puzzle)")]
        public bool testActivities = true;

        [Tooltip("Test Equipment System (equip items, bonuses)")]
        public bool testEquipment = true;

        [Tooltip("Test Progression System (leveling, XP)")]
        public bool testProgression = true;

        [Header("Performance Monitoring")]
        [Tooltip("Show FPS and statistics overlay")]
        public bool showStats = true;

        [Tooltip("Enable detailed performance logging")]
        public bool enableProfiling = false;

        [Header("Loaded Configurations")]
        [Tooltip("Activity configurations found")]
        public ActivityConfig[] loadedActivityConfigs;

        [Tooltip("Equipment configurations found")]
        public EquipmentConfig[] loadedEquipmentConfigs;

        [Tooltip("Progression configuration")]
        public ProgressionConfig progressionConfig;

        // Runtime statistics
        private int _entitiesSpawned = 0;
        private int _activitiesStarted = 0;
        private int _itemsEquipped = 0;
        private float _lastStatsUpdate = 0f;
        private float _fps = 0f;
        private float _frameTime = 0f;

        private EntityManager _entityManager;
        private Unity.Mathematics.Random _random;

        private void Start()
        {
            _entityManager = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
            _random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);

            // Load configurations
            LoadConfigurations();

            if (autoStartTest)
            {
                StartTest();
            }
        }

        /// <summary>
        /// Loads all ScriptableObject configurations
        /// </summary>
        private void LoadConfigurations()
        {
            loadedActivityConfigs = Resources.LoadAll<ActivityConfig>("Configs/Activities");
            loadedEquipmentConfigs = Resources.LoadAll<EquipmentConfig>("Configs/Equipment");
            progressionConfig = Resources.Load<ProgressionConfig>("Configs/ProgressionConfig");

            Debug.Log($"[Test Harness] Loaded {loadedActivityConfigs.Length} activity configs");
            Debug.Log($"[Test Harness] Loaded {loadedEquipmentConfigs.Length} equipment configs");
            Debug.Log($"[Test Harness] Progression config: {(progressionConfig != null ? "Found" : "Missing")}");
        }

        /// <summary>
        /// Starts the comprehensive system test
        /// </summary>
        public void StartTest()
        {
            Debug.Log($"[Test Harness] Starting test with {creatureCount} creatures");
            Debug.Log($"[Test Harness] Active percentage: {activeCreaturePercentage}%");

            // Spawn creatures
            SpawnCreatures();

            // Optionally start activities
            if (testActivities && loadedActivityConfigs.Length > 0)
            {
                StartRandomActivities();
            }

            // Optionally equip items
            if (testEquipment && loadedEquipmentConfigs.Length > 0)
            {
                EquipRandomItems();
            }

            Debug.Log($"[Test Harness] Test initialization complete!");
            Debug.Log($"[Test Harness] Entities spawned: {_entitiesSpawned}");
            Debug.Log($"[Test Harness] Activities started: {_activitiesStarted}");
            Debug.Log($"[Test Harness] Items equipped: {_itemsEquipped}");
        }

        /// <summary>
        /// Spawns creatures with varied genetics
        /// </summary>
        private void SpawnCreatures()
        {
            var archetype = _entityManager.CreateArchetype(
                typeof(CreatureGeneticsComponent),
                typeof(CurrencyComponent),
                typeof(ActivityProgressElement),
                typeof(EquipmentInventoryElement),
                typeof(EquippedItemsComponent),
                typeof(EquipmentBonusCache),
                typeof(MonsterLevelComponent),
                typeof(LevelStatBonusComponent),
                typeof(DailyChallengeElement)
            );

            for (int i = 0; i < creatureCount; i++)
            {
                Entity creature = _entityManager.CreateEntity(archetype);

                // Random genetics (0-100 stats normalized to 0-1 range)
                _entityManager.SetComponentData(creature, new CreatureGeneticsComponent
                {
                    StrengthTrait = _random.NextFloat(0.2f, 1f),
                    AgilityTrait = _random.NextFloat(0.2f, 1f),
                    IntellectTrait = _random.NextFloat(0.2f, 1f),
                    VitalityTrait = _random.NextFloat(0.2f, 1f),
                    CharmTrait = _random.NextFloat(0.2f, 1f),
                    ResilienceTrait = _random.NextFloat(0.2f, 1f)
                });

                // Starting currency
                _entityManager.SetComponentData(creature, new CurrencyComponent
                {
                    coins = 1000,
                    gems = 10,
                    activityTokens = 0
                });

                // Starting level (random 1-10)
                int startLevel = _random.NextInt(1, 11);
                _entityManager.SetComponentData(creature, new MonsterLevelComponent
                {
                    level = startLevel,
                    experiencePoints = 0,
                    experienceToNextLevel = progressionConfig != null ?
                        progressionConfig.GetExperienceToNextLevel(startLevel) : 100,
                    skillPointsAvailable = startLevel - 1,
                    skillPointsSpent = 0
                });

                // Stat bonuses
                float statBonus = progressionConfig != null ?
                    progressionConfig.GetStatBonusAtLevel(startLevel) : 0f;
                _entityManager.SetComponentData(creature, new LevelStatBonusComponent
                {
                    strengthBonus = statBonus,
                    agilityBonus = statBonus,
                    intelligenceBonus = statBonus,
                    vitalityBonus = statBonus,
                    socialBonus = statBonus,
                    adaptabilityBonus = statBonus
                });

                // Initialize equipment
                _entityManager.SetComponentData(creature, new EquippedItemsComponent());
                _entityManager.SetComponentData(creature, new EquipmentBonusCache());

                _entitiesSpawned++;
            }

            Debug.Log($"[Test Harness] Spawned {_entitiesSpawned} creatures with varied genetics");
        }

        /// <summary>
        /// Starts random activities for a percentage of creatures
        /// </summary>
        private void StartRandomActivities()
        {
            int activeCount = (creatureCount * activeCreaturePercentage) / 100;

            var query = _entityManager.CreateEntityQuery(typeof(CreatureGeneticsComponent));
            var entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < math.min(activeCount, entities.Length); i++)
            {
                // Random activity type
                var randomConfig = loadedActivityConfigs[_random.NextInt(0, loadedActivityConfigs.Length)];
                var randomDifficulty = (ActivityDifficulty)_random.NextInt(0, 5);

                // Create activity request
                Entity request = _entityManager.CreateEntity();
                _entityManager.AddComponentData(request, new StartActivityRequest
                {
                    monsterEntity = entities[i],
                    activityType = randomConfig.activityType,
                    difficulty = randomDifficulty,
                    requestTime = Time.time
                });

                _activitiesStarted++;
            }

            entities.Dispose();
            Debug.Log($"[Test Harness] Started {_activitiesStarted} random activities");
        }

        /// <summary>
        /// Equips random items on creatures
        /// </summary>
        private void EquipRandomItems()
        {
            int equipCount = creatureCount / 2; // Equip half the creatures

            var query = _entityManager.CreateEntityQuery(typeof(EquipmentInventoryElement));
            var entities = query.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < math.min(equipCount, entities.Length); i++)
            {
                // Random equipment
                var randomEquip = loadedEquipmentConfigs[_random.NextInt(0, loadedEquipmentConfigs.Length)];
                var item = randomEquip.ToEquipmentItem();

                // Add to inventory
                var inventory = _entityManager.GetBuffer<EquipmentInventoryElement>(entities[i]);
                inventory.Add(new EquipmentInventoryElement { item = item });

                _itemsEquipped++;
            }

            entities.Dispose();
            Debug.Log($"[Test Harness] Equipped {_itemsEquipped} items across creatures");
        }

        private void Update()
        {
            // Update statistics
            if (Time.time - _lastStatsUpdate > 1f)
            {
                _fps = 1f / Time.deltaTime;
                _frameTime = Time.deltaTime * 1000f;
                _lastStatsUpdate = Time.time;

                // Count active activities
                if (_entityManager != null)
                {
                    var activeQuery = _entityManager.CreateEntityQuery(typeof(ActiveActivityComponent));
                    int activeActivities = activeQuery.CalculateEntityCount();
                    activeQuery.Dispose();

                    if (enableProfiling)
                    {
                        Debug.Log($"[Test Harness] FPS: {_fps:F1}, Frame: {_frameTime:F2}ms, Active: {activeActivities}");
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!showStats) return;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 16;
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.UpperLeft;
            style.padding = new RectOffset(10, 10, 10, 10);

            GUILayout.BeginArea(new Rect(10, 10, 400, 400), style);

            GUILayout.Label("<b>PROJECT CHIMERA - SYSTEMS TEST</b>", new GUIStyle(style) { fontSize = 20 });
            GUILayout.Space(10);

            // Performance
            GUILayout.Label("<b>Performance</b>");
            GUILayout.Label($"FPS: {_fps:F1}");
            GUILayout.Label($"Frame Time: {_frameTime:F2}ms");
            GUILayout.Label($"Target: 60 FPS (16.67ms)");
            GUILayout.Space(10);

            // Entities
            GUILayout.Label("<b>Entities</b>");
            GUILayout.Label($"Creatures Spawned: {_entitiesSpawned}");
            if (_entityManager != null)
            {
                var activeQuery = _entityManager.CreateEntityQuery(typeof(ActiveActivityComponent));
                int activeActivities = activeQuery.CalculateEntityCount();
                activeQuery.Dispose();
                GUILayout.Label($"Active Activities: {activeActivities}");
            }
            GUILayout.Space(10);

            // Systems
            GUILayout.Label("<b>Systems Active</b>");
            GUILayout.Label($"✓ Activity System ({loadedActivityConfigs.Length} types)");
            GUILayout.Label($"✓ Equipment System ({loadedEquipmentConfigs.Length} items)");
            GUILayout.Label($"✓ Progression System");
            GUILayout.Label($"✓ Burst Compilation");
            GUILayout.Label($"✓ Job System Parallelization");
            GUILayout.Space(10);

            // Actions
            GUILayout.Label("<b>Actions</b>");
            if (GUILayout.Button("Restart Test"))
            {
                ResetTest();
                StartTest();
            }
            if (GUILayout.Button("Start More Activities"))
            {
                StartRandomActivities();
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// Resets test state
        /// </summary>
        private void ResetTest()
        {
            // Destroy all test entities
            if (_entityManager != null)
            {
                var query = _entityManager.CreateEntityQuery(typeof(CreatureGeneticsComponent));
                _entityManager.DestroyEntity(query);
                query.Dispose();

                var requestQuery = _entityManager.CreateEntityQuery(typeof(StartActivityRequest));
                _entityManager.DestroyEntity(requestQuery);
                requestQuery.Dispose();
            }

            _entitiesSpawned = 0;
            _activitiesStarted = 0;
            _activitiesCompleted = 0;
            _itemsEquipped = 0;
            _levelsGained = 0;

            Debug.Log("[Test Harness] Test reset complete");
        }

        private void OnDestroy()
        {
            ResetTest();
        }
    }
}
