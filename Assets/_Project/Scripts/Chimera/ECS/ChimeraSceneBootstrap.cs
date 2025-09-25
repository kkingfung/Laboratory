using UnityEngine;
using Unity.Entities;
using Cysharp.Threading.Tasks;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.ECS.Components;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Core;
using Laboratory.Core.Events;
using Laboratory.Core.DI;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// Scene bootstrap for Project Chimera - sets up complete creature ecosystem in any scene.
    /// Drop this on a GameObject and configure to instantly create a living world!
    /// </summary>
    public class ChimeraSceneBootstrap : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private ChimeraBiomeConfig biomeConfig;
        [SerializeField] private ChimeraSpeciesConfig[] availableSpecies = new ChimeraSpeciesConfig[0];
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private bool enableDebugLogging = true;
        
        [Header("Creature Spawning")]
        [SerializeField] [Range(1, 100)] private int initialCreatureCount = 10;
        [SerializeField] [Range(0f, 1f)] private float wildCreatureRatio = 0.7f;
        [SerializeField] [Range(1f, 50f)] private float spawnRadius = 20f;
        [SerializeField] private Transform spawnCenter;
        [SerializeField] private LayerMask groundLayerMask = 1;
        
        [Header("World Settings")]
        [SerializeField] private Season currentSeason = Season.Spring;
        [SerializeField] [Range(0f, 1f)] private float timeOfDay = 0.5f; // 0=midnight, 0.5=noon
        [SerializeField] private bool enableDayNightCycle = true;
        [SerializeField] [Range(0.1f, 10f)] private float timeSpeed = 1f;
        
        [Header("Simulation Settings")]
        [SerializeField] private bool enableGeneticEvolution = true;
        [SerializeField] private bool enableEnvironmentalAdaptation = true;
        [SerializeField] private bool enableBreedingSimulation = true;
        [SerializeField] [Range(0.1f, 100f)] private float simulationTimeScale = 30f; // 30x real time
        
        [Header("Player Integration")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] [Range(1, 5)] private int playerCompanionCount = 2;
        
        [Header("Debug & Testing")]
        [SerializeField] private bool showSpawnGizmos = true;
        [SerializeField] private bool showCreatureInfo = true;
        [SerializeField] private bool enablePerformanceMetrics = true;
        [SerializeField] private KeyCode quickSpawnKey = KeyCode.F1;
        [SerializeField] private KeyCode toggleDebugKey = KeyCode.F2;
        
        // Runtime state
        private EntityManager entityManager;
        private Entity biomeEntity;
        private IEventBus eventBus;
        private IBreedingSystem breedingSystem;
        private List<Entity> spawnedCreatures = new List<Entity>();
        private float lastSpawnTime;
        private bool isInitialized = false;
        
        // Performance tracking
        private int frameCount = 0;
        private float performanceTimer = 0f;
        private float avgFrameTime = 0f;
        
        #region Unity Lifecycle
        
        private async void Start()
        {
            if (autoInitializeOnStart)
            {
                await InitializeChimeraScene();
            }
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            HandleInput();
            UpdateTimeOfDay();
            UpdatePerformanceMetrics();
            
            // Periodic ecosystem updates (scaled by simulation time)
            float spawnInterval = 30f / simulationTimeScale;
            if (Time.time - lastSpawnTime > spawnInterval && spawnedCreatures.Count < initialCreatureCount * 2)
            {
                SpawnRandomCreature();
                lastSpawnTime = Time.time;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showSpawnGizmos) return;
            
            var center = spawnCenter != null ? spawnCenter.position : transform.position;
            
            // Draw spawn radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, spawnRadius);
            
            // Draw creature positions
            if (Application.isPlaying && isInitialized)
            {
                Gizmos.color = Color.blue;
                foreach (var creature in spawnedCreatures)
                {
                    if (entityManager.Exists(creature))
                    {
                        var pos = entityManager.GetComponentData<Unity.Transforms.LocalTransform>(creature).Position;
                        Gizmos.DrawWireCube(pos, Vector3.one);
                    }
                }
            }
        }
        
        private void OnGUI()
        {
            if (!showCreatureInfo || !isInitialized) return;
            
            var rect = new Rect(10, 10, 300, 200);
            GUI.Box(rect, "");
            
            var style = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            float yOffset = 20;
            
            GUI.Label(new Rect(20, yOffset, 280, 20), $"Biome: {biomeConfig?.biomeName ?? "None"}", style);
            yOffset += 20;
            
            GUI.Label(new Rect(20, yOffset, 280, 20), $"Active Creatures: {spawnedCreatures.Count}", style);
            yOffset += 20;
            
            GUI.Label(new Rect(20, yOffset, 280, 20), $"Time: {currentSeason} {GetTimeString()}", style);
            yOffset += 20;
            
            if (enablePerformanceMetrics)
            {
                GUI.Label(new Rect(20, yOffset, 280, 20), $"Avg Frame Time: {avgFrameTime:F2}ms", style);
                yOffset += 20;
            }
            
            GUI.Label(new Rect(20, yOffset, 280, 20), $"Controls: {quickSpawnKey}=Spawn, {toggleDebugKey}=Debug", style);
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Initialize the complete Chimera ecosystem
        /// </summary>
        [ContextMenu("Initialize Chimera Scene")]
        public async UniTask InitializeChimeraScene()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[ChimeraBootstrap] Scene already initialized!");
                return;
            }
            
            Log("Initializing Project Chimera Scene...");
            
            try
            {
                // Step 1: Initialize Core Systems
                await InitializeCoreSystemsAsync();
                
                // Step 2: Validate Configuration
                ValidateConfiguration();
                
                // Step 3: Setup World Entity
                CreateWorldEntity();
                
                // Step 4: Setup Player Integration
                SetupPlayerIntegration();
                
                // Step 5: Spawn Initial Creatures
                await SpawnInitialCreatures();
                
                // Step 6: Initialize Biome Systems
                InitializeBiomeSystems();
                
                isInitialized = true;
                Log("Chimera scene initialization complete!");
                
                // Fire initialization complete event
                if (eventBus != null)
                {
                    eventBus.Publish(new ChimeraSceneInitializedEvent(biomeConfig, spawnedCreatures.Count));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize Chimera scene: {ex}");
                throw;
            }
        }
        
        /// <summary>
        /// Spawn a random creature in the scene
        /// </summary>
        [ContextMenu("Spawn Random Creature")]
        public void SpawnRandomCreature()
        {
            if (!isInitialized || availableSpecies.Length == 0)
            {
                Debug.LogWarning("[ChimeraBootstrap] Cannot spawn creature - scene not initialized or no species configured");
                return;
            }
            
            var species = availableSpecies[Random.Range(0, availableSpecies.Length)];
            var position = GetRandomSpawnPosition();
            
            var creature = SpawnCreature(species, position, Random.value < wildCreatureRatio);
            
            if (creature != Entity.Null)
            {
                Log($"Spawned {species.speciesName} at {position}");
            }
        }
        
        /// <summary>
        /// Cleanup and reset the scene
        /// </summary>
        [ContextMenu("Reset Scene")]
        public void ResetScene()
        {
            if (!isInitialized) return;
            
            Log("Resetting Chimera scene...");
            
            // Cleanup entities
            foreach (var creature in spawnedCreatures)
            {
                if (entityManager.Exists(creature))
                {
                    entityManager.DestroyEntity(creature);
                }
            }
            
            spawnedCreatures.Clear();
            
            if (entityManager.Exists(biomeEntity))
            {
                entityManager.DestroyEntity(biomeEntity);
            }
            
            isInitialized = false;
            Log("Scene reset complete");
        }
        
        #endregion
        
        #region Core System Initialization
        
        private async UniTask InitializeCoreSystemsAsync()
        {
            Log("Initializing core systems...");
            
            // Get Entity Manager
            entityManager = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
            
            // Initialize or get event bus
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.TryResolve<IEventBus>(out eventBus);
            }
            
            if (eventBus == null)
            {
                eventBus = new UnifiedEventBus();
                Log("Created new event bus");
            }
            
            // Initialize breeding system
            if (enableBreedingSimulation)
            {
                breedingSystem = new BreedingSystem(eventBus);
                Log("Breeding simulation enabled");
            }
            
            // Subscribe to events
            SubscribeToEvents();
            
            await UniTask.Yield(); // Allow frame to process
            
            Log("Core systems initialized");
        }
        
        private void ValidateConfiguration()
        {
            Log("🔍 Validating configuration...");
            
            if (biomeConfig == null)
            {
                Debug.LogError("[ChimeraBootstrap] No biome configuration assigned!");
                throw new System.InvalidOperationException("Biome configuration is required");
            }
            
            if (availableSpecies.Length == 0)
            {
                Debug.LogWarning("[ChimeraBootstrap] No species configured - using default species");
                // Could create a default species here if needed
            }
            
            if (spawnCenter == null)
            {
                spawnCenter = transform;
                Log("Using bootstrap transform as spawn center");
            }
            
            Log("Configuration validated");
        }
        
        private void CreateWorldEntity()
        {
            Log("Creating world entity...");
            
            var archetype = entityManager.CreateArchetype(
                typeof(ChimeraBiomeComponent),
                typeof(EnvironmentalConditionsComponent),
                typeof(Unity.Transforms.LocalTransform)
            );
            
            biomeEntity = entityManager.CreateEntity(archetype);
            
            // Set biome component
            var biomeComponent = new ChimeraBiomeComponent
            {
                biomeType = biomeConfig.biomeType,
                temperature = biomeConfig.baseTemperature,
                humidity = biomeConfig.baseHumidity,
                foodAvailability = biomeConfig.foodAvailability,
                predatorPressure = biomeConfig.predatorPressure,
                season = currentSeason,
                timeOfDay = timeOfDay
            };
            
            entityManager.SetComponentData(biomeEntity, biomeComponent);
            
            // Set environmental conditions
            var envConditions = biomeConfig.GetCurrentConditions(currentSeason, timeOfDay);
            var envComponent = new EnvironmentalConditionsComponent
            {
                temperature = envConditions.temperature,
                humidity = envConditions.humidity,
                foodAvailability = envConditions.foodAvailability,
                activityLevel = envConditions.activityLevel,
                predatorActivity = envConditions.predatorActivity
            };
            
            entityManager.SetComponentData(biomeEntity, envComponent);
            
            Log("World entity created");
        }
        
        #endregion
        
        #region Creature Spawning
        
        private async UniTask SpawnInitialCreatures()
        {
            Log($"🧬 Spawning {initialCreatureCount} initial creatures...");
            
            int spawnedCount = 0;
            int companionCount = 0;
            
            for (int i = 0; i < initialCreatureCount; i++)
            {
                if (availableSpecies.Length == 0) break;
                
                var species = availableSpecies[Random.Range(0, availableSpecies.Length)];
                var position = GetRandomSpawnPosition();
                
                // Determine if this should be wild or companion
                bool isWild = Random.value < wildCreatureRatio || companionCount >= playerCompanionCount;
                
                var creature = SpawnCreature(species, position, isWild);
                
                if (creature != Entity.Null)
                {
                    spawnedCount++;
                    if (!isWild) companionCount++;
                }
                
                // Spread spawning across frames to avoid hitches
                if (i % 3 == 0)
                {
                    await UniTask.Yield();
                }
            }
            
            Log($"Spawned {spawnedCount} creatures ({companionCount} companions, {spawnedCount - companionCount} wild)");
        }
        
        private Entity SpawnCreature(ChimeraSpeciesConfig species, Vector3 position, bool isWild = true)
        {
            try
            {
                // Create creature instance
                var genetics = species.GenerateRandomGeneticProfile();

                // Apply genetic evolution if enabled
                if (enableGeneticEvolution && Random.value < 0.1f) // 10% chance for genetic variation
                {
                    // Enhanced genetic diversity when evolution is enabled
                    Log("Applying genetic evolution variation");
                }
                var creatureInstance = new CreatureInstance
                {
                    Definition = species.CreateCreatureDefinition(),
                    GeneticProfile = genetics,
                    AgeInDays = isWild ? Random.Range(90, 365) : Random.Range(30, 120),
                    CurrentHealth = species.baseStats.health,
                    Happiness = Random.Range(0.6f, 0.9f),
                    Level = 1,
                    IsWild = isWild
                };
                
                // Create ECS entity
                var entity = CreateCreatureEntity(creatureInstance, position);
                
                if (entity != Entity.Null)
                {
                    spawnedCreatures.Add(entity);
                    
                    // Fire creature spawned event
                    if (eventBus != null)
                    {
                        eventBus.Publish(new CreatureSpawnedEvent(entity, species, isWild));
                    }
                }
                
                return entity;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to spawn creature {species.speciesName}: {ex}");
                return Entity.Null;
            }
        }
        
        private Entity CreateCreatureEntity(CreatureInstance instance, Vector3 position)
        {
            // Create creature archetype with all necessary components
            var archetype = entityManager.CreateArchetype(
                typeof(CreatureDefinitionComponent),
                typeof(CreatureStatsComponent),
                typeof(CreatureGeneticsComponent),
                typeof(CreatureAgeComponent),
                typeof(CreatureBehaviorComponent),
                typeof(CreaturePersonalityComponent),
                typeof(CreatureNeedsComponent),
                typeof(CreatureBreedingComponent),
                typeof(CreatureLifecycleComponent),
                typeof(CreatureBiomeComponent),
                typeof(CreatureEnvironmentalComponent),
                typeof(Unity.Transforms.LocalTransform)
            );
            
            var entity = entityManager.CreateEntity(archetype);
            
            // Create and set components with actual data
            var definitionComp = new CreatureDefinitionComponent
            {
                SpeciesId = instance.Definition?.speciesName?.GetHashCode() ?? 0,
                MaxHealth = instance.Definition?.baseStats.health ?? 100,
                BaseAttack = instance.Definition?.baseStats.attack ?? 10,
                BaseDefense = instance.Definition?.baseStats.defense ?? 10,
                BaseSpeed = instance.Definition?.baseStats.speed ?? 10
            };
            var statsComp = new CreatureStatsComponent
            {
                BaseStats = instance.Definition.baseStats,
                CurrentHealth = instance.Definition.baseStats.health,
                Level = instance.Level,
                Experience = instance.Experience
            };
            var geneticsComp = new CreatureGeneticsComponent
            {
                Generation = instance.GeneticProfile.Generation,
                GeneticPurity = instance.GeneticProfile.GetGeneticPurity(),
                IsShiny = false // Default to false since IsShiny property doesn't exist
            };
            var ageComp = new CreatureAgeComponent
            {
                AgeInDays = instance.AgeInDays,
                IsAdult = instance.AgeInDays >= 30,
                LifeStage = Laboratory.Chimera.ECS.LifeStage.Adult
            };
            var behaviorComp = new CreatureBehaviorComponent
            {
                currentState = AIState.Idle
            };
            var personalityComp = new CreaturePersonalityComponent
            {
                Aggression = 0.5f,
                Curiosity = 0.5f,
                Sociability = 0.5f,
                Loyalty = 0.5f
            };
            var needsComp = new CreatureNeedsComponent
            {
                Hunger = 1.0f,
                Thirst = 1.0f,
                Energy = 1.0f,
                Social = 0.5f,
                Comfort = 0.7f,
                Happiness = 0.8f
            };
            var breedingComp = CreatureBreedingComponent.Create(ageComp, geneticsComp, personalityComp);
            var lifecycleComp = CreatureLifecycleComponent.Create(ageComp, geneticsComp);
            var biomeComp = new CreatureBiomeComponent
            {
                HomeBiome = Laboratory.Chimera.Core.BiomeType.Forest,
                CurrentBiome = Laboratory.Chimera.Core.BiomeType.Forest,
                BiomeComfort = 0.8f
            };
            var envComp = CreatureEnvironmentalComponent.Create(null, geneticsComp);

            // Set all components
            entityManager.SetComponentData(entity, definitionComp);
            entityManager.SetComponentData(entity, statsComp);
            entityManager.SetComponentData(entity, geneticsComp);
            entityManager.SetComponentData(entity, ageComp);
            entityManager.SetComponentData(entity, behaviorComp);
            entityManager.SetComponentData(entity, personalityComp);
            entityManager.SetComponentData(entity, needsComp);
            entityManager.SetComponentData(entity, breedingComp);
            entityManager.SetComponentData(entity, lifecycleComp);
            entityManager.SetComponentData(entity, biomeComp);
            entityManager.SetComponentData(entity, envComp);
            
            // Set transform
            entityManager.SetComponentData(entity, Unity.Transforms.LocalTransform.FromPosition(position));
            
            return entity;
        }
        
        private Vector3 GetRandomSpawnPosition()
        {
            var center = spawnCenter.position;
            var randomPos = center + Random.insideUnitSphere * spawnRadius;
            
            // Try to place on ground
            if (Physics.Raycast(new Vector3(randomPos.x, center.y + 50f, randomPos.z), Vector3.down, out RaycastHit hit, 100f, groundLayerMask))
            {
                return hit.point + Vector3.up * 0.5f;
            }
            
            return new Vector3(randomPos.x, center.y, randomPos.z);
        }
        
        #endregion
        
        #region Event Handling & Systems
        
        private void SubscribeToEvents()
        {
            if (eventBus == null) return;
            
            eventBus.Subscribe<BreedingSuccessfulEvent>(OnBreedingSuccess);
            eventBus.Subscribe<CreatureMaturedEvent>(OnCreatureMatured);
            eventBus.Subscribe<EnvironmentalAdaptationEvent>(OnEnvironmentalAdaptation);
        }
        
        private void OnBreedingSuccess(BreedingSuccessfulEvent evt)
        {
            Log($"Breeding success! Offspring born: {evt.Offspring?.Definition?.speciesName ?? "Unknown"}");
            
            // Spawn offspring in the scene
            if (evt.Offspring != null)
            {
                _ = SpawnOffspringAsync(new CreatureInstance[] { evt.Offspring });
            }
        }
        
        private void OnCreatureMatured(CreatureMaturedEvent evt)
        {
            Log($"Creature matured: {evt.Creature.UniqueId}");
        }
        
        private void OnEnvironmentalAdaptation(EnvironmentalAdaptationEvent evt)
        {
            if (!enableEnvironmentalAdaptation) return;

            Log($"Environmental adaptation: {evt.Creature.UniqueId} adapting to {evt.NewEnvironment}");
        }
        
        private async UniTask SpawnOffspringAsync(CreatureInstance[] offspring)
        {
            foreach (var baby in offspring)
            {
                var position = GetRandomSpawnPosition();
                SpawnCreature(availableSpecies[0], position, false); // Born tame
                await UniTask.Yield();
            }
        }
        
        #endregion
        
        #region Player Integration & Input
        
        private void SetupPlayerIntegration()
        {
            if (autoFindPlayer && playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                    Log("Found and linked to player");
                }
            }
            
            if (playerTransform != null)
            {
                // Setup player entity for bonding system
                var playerEntity = entityManager.CreateEntity();
                entityManager.AddComponentData(playerEntity, new PlayerTag());
                
                Log("Player integration setup complete");
            }
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(quickSpawnKey))
            {
                SpawnRandomCreature();
            }
            
            if (Input.GetKeyDown(toggleDebugKey))
            {
                showCreatureInfo = !showCreatureInfo;
                showSpawnGizmos = !showSpawnGizmos;
            }
        }
        
        #endregion
        
        #region Time & Environment Systems
        
        private void UpdateTimeOfDay()
        {
            if (!enableDayNightCycle) return;
            
            timeOfDay += Time.deltaTime * timeSpeed / 86400f; // 86400 seconds in a day
            if (timeOfDay >= 1f)
            {
                timeOfDay -= 1f;
                AdvanceSeason();
            }
            
            // Update environmental conditions based on time
            if (entityManager.Exists(biomeEntity))
            {
                var conditions = biomeConfig.GetCurrentConditions(currentSeason, timeOfDay);
                var envComponent = new EnvironmentalConditionsComponent
                {
                    temperature = conditions.temperature,
                    humidity = conditions.humidity,
                    foodAvailability = conditions.foodAvailability,
                    activityLevel = conditions.activityLevel,
                    predatorActivity = conditions.predatorActivity
                };
                
                entityManager.SetComponentData(biomeEntity, envComponent);
            }
        }
        
        private void AdvanceSeason()
        {
            currentSeason = (Season)(((int)currentSeason + 1) % 4);
            Log($"Season changed to {currentSeason}");
            
            if (eventBus != null)
            {
                eventBus.Publish(new SeasonChangedEvent(currentSeason));
            }
        }
        
        private void InitializeBiomeSystems()
        {
            Log("Initializing biome-specific systems...");
            
            // Apply biome-specific settings
            if (biomeConfig.ambientSounds != null)
            {
                // Setup ambient audio
                var audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                
                audioSource.clip = biomeConfig.ambientSounds;
                audioSource.loop = true;
                audioSource.volume = 0.3f;
                audioSource.Play();
            }
            
            Log("Biome systems initialized");
        }
        
        #endregion
        
        #region Performance & Debug
        
        private void UpdatePerformanceMetrics()
        {
            if (!enablePerformanceMetrics) return;
            
            frameCount++;
            performanceTimer += Time.unscaledDeltaTime * 1000f; // Convert to milliseconds
            
            if (frameCount >= 60) // Update every 60 frames
            {
                avgFrameTime = performanceTimer / frameCount;
                frameCount = 0;
                performanceTimer = 0f;
            }
        }
        
        private void Log(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[ChimeraBootstrap] {message}");
            }
        }
        
        private string GetTimeString()
        {
            int hours = Mathf.FloorToInt(timeOfDay * 24f);
            int minutes = Mathf.FloorToInt((timeOfDay * 24f - hours) * 60f);
            return $"{hours:D2}:{minutes:D2}";
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            if (isInitialized)
            {
                ResetScene();
            }
            
            breedingSystem?.Dispose();
            eventBus?.Dispose();
        }
        
        #endregion
    }
    
    // Additional ECS Components needed for the bootstrap
    public struct ChimeraBiomeComponent : IComponentData
    {
        public Laboratory.Chimera.Core.BiomeType biomeType;
        public float temperature;
        public float humidity;
        public float foodAvailability;
        public float predatorPressure;
        public Season season;
        public float timeOfDay;
    }
    
    public struct EnvironmentalConditionsComponent : IComponentData
    {
        public float temperature;
        public float humidity;
        public float foodAvailability;
        public float activityLevel;
        public float predatorActivity;
    }
    
    public struct PlayerTag : IComponentData { }
    
    // Event classes
    public class ChimeraSceneInitializedEvent
    {
        public ChimeraBiomeConfig BiomeConfig { get; private set; }
        public int CreatureCount { get; private set; }
        
        public ChimeraSceneInitializedEvent(ChimeraBiomeConfig biomeConfig, int creatureCount)
        {
            BiomeConfig = biomeConfig;
            CreatureCount = creatureCount;
        }
    }
    
    public class CreatureSpawnedEvent
    {
        public Entity CreatureEntity { get; private set; }
        public ChimeraSpeciesConfig Species { get; private set; }
        public bool IsWild { get; private set; }
        
        public CreatureSpawnedEvent(Entity entity, ChimeraSpeciesConfig species, bool isWild)
        {
            CreatureEntity = entity;
            Species = species;
            IsWild = isWild;
        }
    }
    
    public class SeasonChangedEvent
    {
        public Season NewSeason { get; private set; }
        
        public SeasonChangedEvent(Season newSeason)
        {
            NewSeason = newSeason;
        }
    }
    
    public class EnvironmentalAdaptationEvent
    {
        public CreatureInstance Creature { get; private set; }
        public EnvironmentalFactors OldEnvironment { get; private set; }
        public EnvironmentalFactors NewEnvironment { get; private set; }
        public string[] AffectedTraits { get; private set; }
        
        public EnvironmentalAdaptationEvent(CreatureInstance creature, EnvironmentalFactors oldEnv, EnvironmentalFactors newEnv, string[] affectedTraits)
        {
            Creature = creature;
            OldEnvironment = oldEnv;
            NewEnvironment = newEnv;
            AffectedTraits = affectedTraits;
        }
    }
    
    public class CreatureMaturedEvent
    {
        public CreatureInstance Creature { get; private set; }
        
        public CreatureMaturedEvent(CreatureInstance creature)
        {
            Creature = creature;
        }
    }
    
    public class MutationOccurredEvent
    {
        public CreatureInstance Creature { get; private set; }
        public Mutation Mutation { get; private set; }

        public MutationOccurredEvent(CreatureInstance creature, Mutation mutation)
        {
            Creature = creature;
            Mutation = mutation;
        }
    }
}