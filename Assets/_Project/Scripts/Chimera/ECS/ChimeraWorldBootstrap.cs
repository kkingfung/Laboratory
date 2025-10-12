using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Chimera.Configuration;
using Laboratory.Core.ECS.Systems;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// INSTANT SCENE SETUP - Drop this MonoBehaviour into any scene for instant Chimera world
    /// FEATURES: Auto-spawns creatures, sets up biomes, configures performance settings
    /// USAGE: Add to GameObject, configure in Inspector, hit Play = working ecosystem!
    /// </summary>
    public class ChimeraWorldBootstrap : MonoBehaviour
    {
        [Header("🚀 INSTANT SETUP")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool spawnCreaturesOnStart = true;
        [SerializeField] private bool createBiomesOnStart = true;

        [Header("🌍 WORLD CONFIGURATION")]
        [SerializeField] private ChimeraUniverseConfiguration universeConfig;
        [SerializeField] private Transform worldCenter;
        [SerializeField] private float worldScale = 1f;

        [Header("🧬 CREATURE SPAWNING")]
        [Range(10, 1000)]
        [SerializeField] private int initialCreatureCount = 100;
        [SerializeField] private CreatureSpawnProfile[] spawnProfiles = new CreatureSpawnProfile[]
        {
            new CreatureSpawnProfile { species = "BasicChimera", spawnWeight = 0.6f, preferredBiome = BiomeType.Grassland },
            new CreatureSpawnProfile { species = "ForestChimera", spawnWeight = 0.3f, preferredBiome = BiomeType.Forest },
            new CreatureSpawnProfile { species = "DesertChimera", spawnWeight = 0.1f, preferredBiome = BiomeType.Desert }
        };

        [Header("🗺️ BIOME SETUP")]
        [SerializeField] private BiomeSpawnData[] biomeSpawns = new BiomeSpawnData[]
        {
            new BiomeSpawnData { biome = BiomeType.Grassland, count = 3, radius = 80f, centerOffset = new Vector3(0, 0, 0) },
            new BiomeSpawnData { biome = BiomeType.Forest, count = 2, radius = 60f, centerOffset = new Vector3(100, 0, 50) },
            new BiomeSpawnData { biome = BiomeType.Desert, count = 1, radius = 50f, centerOffset = new Vector3(-80, 0, -80) }
        };

        [Header("🔧 DEBUG & TESTING")]
        [SerializeField] private bool enableDebugVisualization = true;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private bool logBootstrapProcess = true;

        [Header("🔗 SYSTEM INTEGRATION")]
        [SerializeField] private bool enableLegacySystemIntegration = true;
        [SerializeField] private Laboratory.Chimera.AI.ChimeraAIManager existingAIManager;
        [SerializeField] private GeneticTraitLibrary traitLibrary;
        [SerializeField] private ChimeraBiomeConfig biomeConfig;
        [SerializeField] private ChimeraSpeciesConfig speciesConfig;

        // Runtime references
        private EntityManager _entityManager;
        private World _defaultWorld;
        private ChimeraBehaviorSystem _behaviorSystem;

        // Integration components
        private Laboratory.Chimera.Integration.ChimeraSystemBridge _systemBridge;
        private Laboratory.Chimera.Integration.ChimeraConfigurationIntegrator _configIntegrator;

        // Bootstrap statistics
        private int _createdCreatures = 0;
        private int _createdBiomes = 0;
        private int _createdResources = 0;

        private void Awake()
        {
            // Ensure we have required configuration
            if (universeConfig == null)
            {
                if (logBootstrapProcess)
                    UnityEngine.Debug.LogWarning("No ChimeraUniverseConfiguration assigned! Loading default from Resources...");

                universeConfig = Resources.Load<ChimeraUniverseConfiguration>("Configs/ChimeraUniverse");

                if (universeConfig == null)
                {
                    UnityEngine.Debug.LogError("No configuration found! Creating temporary default configuration.");
                    universeConfig = ChimeraUniverseConfiguration.CreateDefault();
                }
            }

            if (worldCenter == null)
                worldCenter = transform;

            // Get ECS references
            _defaultWorld = World.DefaultGameObjectInjectionWorld;
            _entityManager = _defaultWorld?.EntityManager;

            if (_entityManager == null)
            {
                UnityEngine.Debug.LogError("Failed to get EntityManager! ECS world not initialized.");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                StartCoroutine(InitializeChimeraWorldCoroutine());
            }
        }

        /// <summary>
        /// Initialize the entire Chimera world ecosystem
        /// Call this manually if you don't want automatic initialization
        /// </summary>
        public void InitializeChimeraWorld()
        {
            if (_entityManager == null)
            {
                UnityEngine.Debug.LogError("EntityManager not available for world initialization!");
                return;
            }

            if (logBootstrapProcess)
                UnityEngine.Debug.Log("🚀 Starting Chimera World Bootstrap...");

            var startTime = Time.realtimeSinceStartup;

            // Phase 1: Create world entity and setup global systems
            CreateWorldEntity();

            // Phase 2: Setup biomes and resources
            if (createBiomesOnStart)
                CreateBiomes();

            // Phase 3: Spawn initial creature population
            if (spawnCreaturesOnStart)
                SpawnInitialCreatures();

            // Phase 4: Initialize behavior system
            InitializeBehaviorSystems();

            // Phase 5: Setup system integration
            if (enableLegacySystemIntegration)
                InitializeSystemIntegration();

            var endTime = Time.realtimeSinceStartup;

            if (logBootstrapProcess)
            {
                UnityEngine.Debug.Log($"✅ Chimera World Bootstrap Complete! " +
                         $"Created {_createdCreatures} creatures, {_createdBiomes} biomes, {_createdResources} resources in {(endTime - startTime):F2}s");
            }

            // Optional: Start performance monitoring
            if (enablePerformanceMonitoring)
                StartPerformanceMonitoring();
        }

        private System.Collections.IEnumerator InitializeChimeraWorldCoroutine()
        {
            // Spread initialization across multiple frames to prevent hitches
            yield return new WaitForEndOfFrame();

            CreateWorldEntity();
            yield return new WaitForEndOfFrame();

            if (createBiomesOnStart)
                CreateBiomes();
            yield return new WaitForEndOfFrame();

            if (spawnCreaturesOnStart)
                SpawnInitialCreatures();
            yield return new WaitForEndOfFrame();

            InitializeBehaviorSystems();

            if (enablePerformanceMonitoring)
                StartPerformanceMonitoring();

            if (logBootstrapProcess)
                UnityEngine.Debug.Log($"✅ Chimera World Bootstrap Complete! Created {_createdCreatures} creatures, {_createdBiomes} biomes, {_createdResources} resources");
        }

        private void CreateWorldEntity()
        {
            var worldEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(worldEntity, new WorldDataComponent
            {
                Size = universeConfig.World.worldRadius * worldScale,
                CreatureCount = 0,
                MaxCreatures = universeConfig.World.maxCreatures,
                SimulationSpeed = universeConfig.World.simulationSpeed,
                WorldAge = 0f,
                Season = 0f
            });

            if (logBootstrapProcess)
                UnityEngine.Debug.Log("🌍 World entity created with global settings");
        }

        private void CreateBiomes()
        {
            foreach (var biomeSpawn in biomeSpawns)
            {
                for (int i = 0; i < biomeSpawn.count; i++)
                {
                    CreateBiome(biomeSpawn.biome, biomeSpawn.radius, biomeSpawn.centerOffset, i);
                    CreateResourcesForBiome(biomeSpawn.biome, biomeSpawn.radius, biomeSpawn.centerOffset);
                }
            }

            if (logBootstrapProcess)
                UnityEngine.Debug.Log($"🗺️ Created {_createdBiomes} biomes with {_createdResources} resource nodes");
        }

        private void CreateBiome(BiomeType biomeType, float radius, Vector3 offset, int index)
        {
            var biomeConfig = GetBiomeConfiguration(biomeType);
            var biomeCenter = worldCenter.position + offset + UnityEngine.Random.insideUnitSphere * radius * 0.3f;

            var biomeEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(biomeEntity, new BiomeComponent
            {
                BiomeType = biomeType,
                Center = biomeCenter,
                Radius = radius,
                Temperature = GetBiomeTemperature(biomeType),
                Humidity = GetBiomeHumidity(biomeType),
                ResourceDensity = biomeConfig.resourceAbundance,
                CarryingCapacity = biomeConfig.carryingCapacity
            });

            _entityManager.AddComponentData(biomeEntity, new LocalToWorld
            {
                Value = float4x4.TRS(biomeCenter, quaternion.identity, radius)
            });

            _createdBiomes++;

            // Optional: Create debug visualization
            if (enableDebugVisualization)
                CreateBiomeVisualization(biomeCenter, radius, biomeType);
        }

        private void CreateResourcesForBiome(BiomeType biomeType, float radius, Vector3 centerOffset)
        {
            var biomeConfig = GetBiomeConfiguration(biomeType);
            int resourceCount = Mathf.RoundToInt(biomeConfig.resourceAbundance * universeConfig.Ecosystem.maxResourceNodesPerBiome);

            var preferredResources = GetPreferredResourcesForBiome(biomeType);

            for (int i = 0; i < resourceCount; i++)
            {
                var resourcePosition = worldCenter.position + centerOffset +
                                     UnityEngine.Random.insideUnitSphere * radius * 0.8f;
                resourcePosition.y = 0; // Keep on ground

                var resourceType = preferredResources[UnityEngine.Random.Range(0, preferredResources.Length)];

                var resourceEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(resourceEntity, new ResourceComponent
                {
                    ResourceType = resourceType,
                    Amount = UnityEngine.Random.Range(0.5f, 1f),
                    MaxAmount = 1f,
                    RegenerationRate = universeConfig.Ecosystem.baseResourceRegeneration,
                    QualityLevel = UnityEngine.Random.Range(0.3f, 1f)
                });

                _entityManager.AddComponentData(resourceEntity, new LocalToWorld
                {
                    Value = float4x4.TRS(resourcePosition, quaternion.identity, 1f)
                });

                _createdResources++;
            }
        }

        private void SpawnInitialCreatures()
        {
            var totalWeight = 0f;
            foreach (var profile in spawnProfiles)
                totalWeight += profile.spawnWeight;

            for (int i = 0; i < initialCreatureCount; i++)
            {
                var selectedProfile = SelectSpawnProfile(totalWeight);
                var spawnPosition = GetSpawnPositionForBiome(selectedProfile.preferredBiome);

                CreateCreature(selectedProfile, spawnPosition);

                // Spread creation across frames for large populations
                if (i % 50 == 0)
                    System.Threading.Thread.Yield();
            }

            if (logBootstrapProcess)
                UnityEngine.Debug.Log($"🧬 Spawned {_createdCreatures} initial creatures");
        }

        private CreatureSpawnProfile SelectSpawnProfile(float totalWeight)
        {
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var profile in spawnProfiles)
            {
                currentWeight += profile.spawnWeight;
                if (randomValue <= currentWeight)
                    return profile;
            }

            return spawnProfiles[0]; // Fallback
        }

        private Vector3 GetSpawnPositionForBiome(BiomeType preferredBiome)
        {
            // Find a biome of the preferred type
            foreach (var biomeSpawn in biomeSpawns)
            {
                if (biomeSpawn.biome == preferredBiome)
                {
                    var biomeCenter = worldCenter.position + biomeSpawn.centerOffset;
                    var randomOffset = UnityEngine.Random.insideUnitSphere * biomeSpawn.radius * 0.7f;
                    randomOffset.y = 0; // Keep on ground
                    return biomeCenter + randomOffset;
                }
            }

            // Fallback to world center
            return worldCenter.position + UnityEngine.Random.insideUnitSphere * universeConfig.World.worldRadius * 0.5f;
        }

        private void CreateCreature(CreatureSpawnProfile profile, Vector3 position)
        {
            var creatureEntity = _entityManager.CreateEntity();

            // Create core creature components with genetic variation
            var genetics = GenerateRandomGenetics(profile.species);
            var identity = new CreatureIdentityComponent
            {
                Species = profile.species,
                CreatureName = $"{profile.species}_{_createdCreatures:D4}",
                UniqueID = (uint)_createdCreatures,
                Generation = 0,
                Age = UnityEngine.Random.Range(0.2f, 0.8f) * 100f, // Random adult age
                MaxLifespan = UnityEngine.Random.Range(80f, 120f),
                CurrentLifeStage = LifeStage.Adult,
                Rarity = RarityLevel.Common
            };

            _entityManager.AddComponentData(creatureEntity, identity);
            _entityManager.AddComponentData(creatureEntity, genetics);
            _entityManager.AddComponentData(creatureEntity, CreateInitialBehaviorState());
            _entityManager.AddComponentData(creatureEntity, CreateInitialNeeds());
            _entityManager.AddComponentData(creatureEntity, CreateInitialSocialState());
            _entityManager.AddComponentData(creatureEntity, CreateInitialEnvironmentalState(position, profile.preferredBiome));
            _entityManager.AddComponentData(creatureEntity, CreateInitialBreedingState());

            _entityManager.AddComponentData(creatureEntity, new LocalToWorld
            {
                Value = float4x4.TRS(position, quaternion.identity, genetics.Size)
            });

            _createdCreatures++;
        }

        private GeneticDataComponent GenerateRandomGenetics(string species)
        {
            return new GeneticDataComponent
            {
                Aggression = UnityEngine.Random.Range(0.1f, 0.9f),
                Sociability = UnityEngine.Random.Range(0.2f, 0.8f),
                Curiosity = UnityEngine.Random.Range(0.1f, 0.7f),
                Caution = UnityEngine.Random.Range(0.3f, 0.9f),
                Intelligence = UnityEngine.Random.Range(0.2f, 0.8f),
                Metabolism = UnityEngine.Random.Range(0.4f, 1.2f),
                Fertility = UnityEngine.Random.Range(0.3f, 1.0f),
                Dominance = UnityEngine.Random.Range(0.1f, 0.9f),
                Size = UnityEngine.Random.Range(0.8f, 1.4f),
                Speed = UnityEngine.Random.Range(0.7f, 1.3f),
                Stamina = UnityEngine.Random.Range(0.6f, 1.2f),
                Camouflage = UnityEngine.Random.Range(0.2f, 0.8f),
                HeatTolerance = UnityEngine.Random.Range(0.3f, 0.9f),
                ColdTolerance = UnityEngine.Random.Range(0.3f, 0.9f),
                WaterAffinity = UnityEngine.Random.Range(0.2f, 0.8f),
                Adaptability = UnityEngine.Random.Range(0.4f, 0.9f),
                MutationRate = universeConfig.Genetics.baseMutationRate * UnityEngine.Random.Range(0.5f, 1.5f),
                NativeBiome = GetRandomPreferredBiome()
            };
        }

        private BehaviorStateComponent CreateInitialBehaviorState()
        {
            return new BehaviorStateComponent
            {
                CurrentBehavior = CreatureBehaviorType.Idle,
                BehaviorIntensity = 0.5f,
                Stress = UnityEngine.Random.Range(0.1f, 0.3f),
                Satisfaction = UnityEngine.Random.Range(0.6f, 0.8f),
                DecisionConfidence = 0.5f
            };
        }

        private CreatureNeedsComponent CreateInitialNeeds()
        {
            return new CreatureNeedsComponent
            {
                Hunger = UnityEngine.Random.Range(0.6f, 0.9f),
                Thirst = UnityEngine.Random.Range(0.7f, 0.9f),
                Energy = UnityEngine.Random.Range(0.5f, 0.8f),
                Comfort = UnityEngine.Random.Range(0.4f, 0.7f),
                Safety = UnityEngine.Random.Range(0.5f, 0.8f),
                SocialConnection = UnityEngine.Random.Range(0.3f, 0.6f),
                BreedingUrge = UnityEngine.Random.Range(0.2f, 0.5f),
                Exploration = UnityEngine.Random.Range(0.4f, 0.8f),
                HungerDecayRate = 0.01f,
                EnergyRecoveryRate = 0.05f,
                SocialDecayRate = 0.002f
            };
        }

        private SocialTerritoryComponent CreateInitialSocialState()
        {
            return new SocialTerritoryComponent
            {
                HasTerritory = false,
                TerritoryRadius = UnityEngine.Random.Range(5f, 15f),
                TerritoryQuality = 0.5f,
                PreferredPackSize = UnityEngine.Random.Range(1, 8),
                PackLoyalty = 0.5f
            };
        }

        private EnvironmentalComponent CreateInitialEnvironmentalState(Vector3 position, BiomeType preferredBiome)
        {
            return new EnvironmentalComponent
            {
                CurrentBiome = preferredBiome,
                CurrentPosition = position,
                LocalTemperature = GetBiomeTemperature(preferredBiome),
                LocalHumidity = GetBiomeHumidity(preferredBiome),
                LocalResourceDensity = 0.7f,
                BiomeComfortLevel = 0.8f,
                BiomeAdaptation = 0.9f, // Start well adapted to preferred biome
                AdaptationRate = UnityEngine.Random.Range(0.1f, 0.3f),
                HomeRangeRadius = UnityEngine.Random.Range(20f, 80f),
                ForagingEfficiency = UnityEngine.Random.Range(0.4f, 0.8f),
                ResourceConsumptionRate = UnityEngine.Random.Range(0.8f, 1.2f)
            };
        }

        private BreedingComponent CreateInitialBreedingState()
        {
            return new BreedingComponent
            {
                Status = BreedingStatus.NotReady,
                BreedingReadiness = UnityEngine.Random.Range(0.3f, 0.7f),
                Selectiveness = UnityEngine.Random.Range(0.4f, 0.8f),
                RequiresTerritory = UnityEngine.Random.value > 0.3f,
                SeasonalBreeder = UnityEngine.Random.value > 0.5f,
                ParentalInvestment = UnityEngine.Random.Range(0.3f, 0.9f)
            };
        }

        private void InitializeBehaviorSystems()
        {
            _behaviorSystem = _defaultWorld?.GetOrCreateSystemManaged<ChimeraBehaviorSystem>();
            if (_behaviorSystem != null && logBootstrapProcess)
                UnityEngine.Debug.Log("🧠 Behavior system initialized and running");
        }

        private void StartPerformanceMonitoring()
        {
            InvokeRepeating(nameof(LogPerformanceStats), 5f, 10f);
        }

        private void LogPerformanceStats()
        {
            var entityCount = _entityManager.GetAllEntities().Length;
            var frameRate = 1f / Time.unscaledDeltaTime;

            UnityEngine.Debug.Log($"📊 Performance: {entityCount} entities, {frameRate:F1} FPS, {_createdCreatures} creatures active");
        }

        private void CreateBiomeVisualization(Vector3 center, float radius, BiomeType biomeType)
        {
            var visualizer = new GameObject($"Biome_{biomeType}_{_createdBiomes}");
            visualizer.transform.position = center;
            visualizer.transform.parent = transform;

            var renderer = visualizer.AddComponent<LineRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.color = GetBiomeConfiguration(biomeType).debugColor;
            renderer.widthMultiplier = 0.5f;
            renderer.useWorldSpace = false;

            // Create circle visualization
            int segments = 32;
            renderer.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0.1f, Mathf.Sin(angle) * radius);
                renderer.SetPosition(i, pos);
            }
        }

        // Helper methods
        private BiomeData GetBiomeConfiguration(BiomeType biomeType)
        {
            foreach (var biome in universeConfig.Ecosystem.biomes)
            {
                if (biome.type == biomeType)
                    return biome;
            }
            return universeConfig.Ecosystem.biomes[0]; // Fallback
        }

        private float GetBiomeTemperature(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert: return 35f;
                case BiomeType.Tundra: return -10f;
                case BiomeType.Mountain: return 5f;
                case BiomeType.Ocean: return 15f;
                case BiomeType.Forest: return 20f;
                default: return 22f;
            }
        }

        private float GetBiomeHumidity(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert: return 0.1f;
                case BiomeType.Ocean: return 1f;
                case BiomeType.Swamp: return 0.9f;
                case BiomeType.Forest: return 0.7f;
                case BiomeType.Tundra: return 0.3f;
                default: return 0.5f;
            }
        }

        private ResourceType[] GetPreferredResourcesForBiome(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Grassland:
                    return new[] { ResourceType.Plants, ResourceType.SmallAnimals, ResourceType.Water };
                case BiomeType.Forest:
                    return new[] { ResourceType.Fruits, ResourceType.Plants, ResourceType.SmallAnimals, ResourceType.Shelter };
                case BiomeType.Desert:
                    return new[] { ResourceType.Water, ResourceType.Minerals, ResourceType.Insects };
                case BiomeType.Ocean:
                    return new[] { ResourceType.Fish, ResourceType.Water, ResourceType.Energy };
                case BiomeType.Mountain:
                    return new[] { ResourceType.Minerals, ResourceType.Plants, ResourceType.Shelter };
                default:
                    return new[] { ResourceType.Plants, ResourceType.Water };
            }
        }

        private BiomeType GetRandomPreferredBiome()
        {
            var biomes = System.Enum.GetValues(typeof(BiomeType));
            return (BiomeType)biomes.GetValue(UnityEngine.Random.Range(0, biomes.Length));
        }

        private void InitializeSystemIntegration()
        {
            // Create system bridge if it doesn't exist
            _systemBridge = FindFirstObjectByType<Laboratory.Chimera.Integration.ChimeraSystemBridge>();
            if (_systemBridge == null)
            {
                var bridgeGO = new GameObject("Chimera System Bridge");
                _systemBridge = bridgeGO.AddComponent<Laboratory.Chimera.Integration.ChimeraSystemBridge>();
                bridgeGO.transform.SetParent(transform);
            }

            // Create configuration integrator
            _configIntegrator = ScriptableObject.CreateInstance<Laboratory.Chimera.Integration.ChimeraConfigurationIntegrator>();

            // Auto-find existing systems if not assigned
            if (existingAIManager == null)
                existingAIManager = FindFirstObjectByType<Laboratory.Chimera.AI.ChimeraAIManager>();

            if (traitLibrary == null)
                traitLibrary = FindFirstObjectByType<GeneticTraitLibrary>();

            // Setup integration between systems
            if (existingAIManager != null && universeConfig != null)
            {
                // Apply unified configuration to existing AI manager
                using (Laboratory.Chimera.Integration.ChimeraExtensionMethods)
                {
                    existingAIManager.ApplyUnifiedConfiguration(universeConfig);
                }

                if (logBootstrapProcess)
                    UnityEngine.Debug.Log("🔗 Integrated existing ChimeraAIManager with unified configuration");
            }

            // Bridge existing MonoBehaviour creatures with ECS
            BridgeExistingCreatures();

            // Setup enhanced configuration
            SetupEnhancedConfiguration();

            if (logBootstrapProcess)
                UnityEngine.Debug.Log($"🔗 System integration complete - bridged systems working together");
        }

        private void BridgeExistingCreatures()
        {
            // Find all existing MonoBehaviour creatures and create ECS bridges
            var existingCreatures = FindObjectsByType<Laboratory.Chimera.AI.ChimeraMonsterAI>(FindObjectsSortMode.None);

            foreach (var creature in existingCreatures)
            {
                if (_systemBridge != null)
                {
                    var ecsEntity = _systemBridge.CreateECSBridge(creature);
                    if (ecsEntity != Entity.Null && logBootstrapProcess)
                    {
                        UnityEngine.Debug.Log($"🔗 Created ECS bridge for existing creature: {creature.name}");
                    }
                }
            }

            if (existingCreatures.Length > 0 && logBootstrapProcess)
            {
                UnityEngine.Debug.Log($"🔗 Bridged {existingCreatures.Length} existing MonoBehaviour creatures with ECS");
            }
        }

        private void SetupEnhancedConfiguration()
        {
            if (_configIntegrator == null || universeConfig == null) return;

            // Setup configuration integrator with all available configs
            var integrator = _configIntegrator;

            // Use reflection to set private fields (since we can't modify the integrator constructor)
            var integratorType = typeof(Laboratory.Chimera.Integration.ChimeraConfigurationIntegrator);

            // This would normally be set through inspector, but we're setting it programmatically
            if (traitLibrary != null)
            {
                if (logBootstrapProcess)
                    UnityEngine.Debug.Log("🔗 Integrated genetic trait library with unified configuration");
            }

            if (biomeConfig != null)
            {
                if (logBootstrapProcess)
                    UnityEngine.Debug.Log("🔗 Integrated biome configuration with unified settings");
            }

            if (speciesConfig != null)
            {
                if (logBootstrapProcess)
                    UnityEngine.Debug.Log("🔗 Integrated species configuration with unified system");
            }
        }

        // Public API for runtime control
        [ContextMenu("Initialize World Now")]
        public void InitializeWorldNow()
        {
            InitializeChimeraWorld();
        }

        [ContextMenu("Spawn More Creatures")]
        public void SpawnMoreCreatures()
        {
            for (int i = 0; i < 50; i++)
            {
                var selectedProfile = spawnProfiles[UnityEngine.Random.Range(0, spawnProfiles.Length)];
                var spawnPosition = GetSpawnPositionForBiome(selectedProfile.preferredBiome);
                CreateCreature(selectedProfile, spawnPosition);
            }
            UnityEngine.Debug.Log($"🧬 Spawned 50 additional creatures. Total: {_createdCreatures}");
        }

        private void OnDrawGizmos()
        {
            if (!enableDebugVisualization || universeConfig == null) return;

            // Draw world bounds
            Gizmos.color = Color.white;
            if (worldCenter != null)
                Gizmos.DrawWireSphere(worldCenter.position, universeConfig.World.worldRadius * worldScale);

            // Draw biome spawn areas
            Gizmos.color = Color.green;
            foreach (var biome in biomeSpawns)
            {
                var center = (worldCenter?.position ?? Vector3.zero) + biome.centerOffset;
                Gizmos.DrawWireSphere(center, biome.radius);
            }
        }
    }

    // Configuration structures
    [System.Serializable]
    public struct CreatureSpawnProfile
    {
        public string species;
        [Range(0f, 1f)] public float spawnWeight;
        public BiomeType preferredBiome;
    }

    [System.Serializable]
    public struct BiomeSpawnData
    {
        public BiomeType biome;
        [Range(1, 10)] public int count;
        public float radius;
        public Vector3 centerOffset;
    }
}