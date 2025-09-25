using UnityEngine;
using Unity.Entities;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.AI;
using Laboratory.Chimera.ECS.Components;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using System.Collections.Generic;

namespace Laboratory.Chimera.Integration
{
    /// <summary>
    /// BRIDGE SYSTEM - Connects unified ECS architecture with existing MonoBehaviour systems
    /// PURPOSE: Allow both systems to work together, sharing data and coordinating behavior
    /// FEATURES: Bidirectional data sync, configuration integration, system coordination
    /// </summary>
    public class ChimeraSystemBridge : MonoBehaviour
    {
        [Header("🔗 SYSTEM INTEGRATION")]
        [SerializeField] private bool enableECSIntegration = true;
        [SerializeField] private bool enableMonoBehaviourIntegration = true;
        [SerializeField] private bool enableBidirectionalSync = true;

        [Header("📊 CONFIGURATION REFERENCES")]
        [SerializeField] private ChimeraUniverseConfiguration unifiedConfig;
        [SerializeField] private GeneticTraitLibrary traitLibrary;
        [SerializeField] private ChimeraBiomeConfig biomeConfig;
        [SerializeField] private ChimeraSpeciesConfig speciesConfig;

        [Header("🧠 AI SYSTEM REFERENCES")]
        [SerializeField] private ChimeraAIManager aiManager;
        [SerializeField] private List<ChimeraMonsterAI> monsterAIs = new List<ChimeraMonsterAI>();

        [Header("🔧 INTEGRATION SETTINGS")]
        [SerializeField] private float syncInterval = 0.5f;
        [SerializeField] private int maxEntitiesPerFrame = 100;
        [SerializeField] private bool logIntegrationEvents = false;

        // System references
        private EntityManager _entityManager;
        private ChimeraBehaviorSystem _behaviorSystem;
        private ChimeraBreedingSystem _breedingSystem;

        // Bridge data
        private Dictionary<Entity, ChimeraMonsterAI> _entityToMonoBehaviour = new Dictionary<Entity, ChimeraMonsterAI>();
        private Dictionary<ChimeraMonsterAI, Entity> _monoBehaviourToEntity = new Dictionary<ChimeraMonsterAI, Entity>();
        private float _lastSyncTime;

        private void Start()
        {
            InitializeBridge();
        }

        private void Update()
        {
            if (enableBidirectionalSync && Time.time - _lastSyncTime > syncInterval)
            {
                SyncSystems();
                _lastSyncTime = Time.time;
            }
        }

        private void InitializeBridge()
        {
            // Get ECS system references
            var world = World.DefaultGameObjectInjectionWorld;
            _entityManager = world?.EntityManager;
            _behaviorSystem = world?.GetOrCreateSystemManaged<ChimeraBehaviorSystem>();
            _breedingSystem = world?.GetOrCreateSystemManaged<ChimeraBreedingSystem>();

            // Load configurations if not assigned
            if (unifiedConfig == null)
                unifiedConfig = Resources.Load<ChimeraUniverseConfiguration>("Configs/ChimeraUniverse");

            if (traitLibrary == null)
                traitLibrary = FindObjectOfType<GeneticTraitLibrary>();

            // Initialize AI manager integration
            if (aiManager == null)
                aiManager = FindObjectOfType<ChimeraAIManager>();

            // Create initial bridges for existing MonoBehaviour creatures
            CreateInitialBridges();

            if (logIntegrationEvents)
                Debug.Log("🔗 ChimeraSystemBridge initialized - connecting ECS and MonoBehaviour systems");
        }

        private void CreateInitialBridges()
        {
            // Find all existing MonoBehaviour creatures and create ECS counterparts
            var existingAIs = FindObjectsByType<ChimeraMonsterAI>(FindObjectsSortMode.None);

            foreach (var monsterAI in existingAIs)
            {
                CreateECSBridge(monsterAI);
            }
        }

        /// <summary>
        /// Create an ECS entity that mirrors a MonoBehaviour creature
        /// </summary>
        public Entity CreateECSBridge(ChimeraMonsterAI monoBehaviourCreature)
        {
            if (_entityManager == null || monoBehaviourCreature == null) return Entity.Null;

            // Create ECS entity
            var entity = _entityManager.CreateEntity();

            // Add core components based on MonoBehaviour data
            _entityManager.AddComponentData(entity, ConvertToCreatureIdentity(monoBehaviourCreature));
            _entityManager.AddComponentData(entity, ConvertToGeneticData(monoBehaviourCreature));
            _entityManager.AddComponentData(entity, ConvertToBehaviorState(monoBehaviourCreature));
            _entityManager.AddComponentData(entity, CreateDefaultNeeds());
            _entityManager.AddComponentData(entity, CreateDefaultSocialTerritory());
            _entityManager.AddComponentData(entity, ConvertToEnvironmental(monoBehaviourCreature));
            _entityManager.AddComponentData(entity, CreateDefaultBreeding());

            // Add transform component
            _entityManager.AddComponentData(entity, new Unity.Transforms.LocalToWorld
            {
                Value = monoBehaviourCreature.transform.localToWorldMatrix
            });

            // Store bidirectional references
            _entityToMonoBehaviour[entity] = monoBehaviourCreature;
            _monoBehaviourToEntity[monoBehaviourCreature] = entity;

            // Add bridge component for identification
            _entityManager.AddComponentData(entity, new BridgedCreatureComponent
            {
                monoBehaviourInstanceID = monoBehaviourCreature.GetInstanceID(),
                syncEnabled = true
            });

            if (logIntegrationEvents)
                Debug.Log($"🔗 Created ECS bridge for MonoBehaviour creature: {monoBehaviourCreature.name}");

            return entity;
        }

        /// <summary>
        /// Create a MonoBehaviour creature that mirrors an ECS entity
        /// </summary>
        public ChimeraMonsterAI CreateMonoBehaviourBridge(Entity ecsEntity)
        {
            if (_entityManager == null || !_entityManager.Exists(ecsEntity)) return null;

            // Get ECS data
            var identity = _entityManager.GetComponentData<CreatureIdentityComponent>(ecsEntity);
            var genetics = _entityManager.GetComponentData<GeneticDataComponent>(ecsEntity);
            var transform = _entityManager.GetComponentData<Unity.Transforms.LocalToWorld>(ecsEntity);

            // Create GameObject
            var go = new GameObject($"Bridged_{identity.CreatureName}");
            go.transform.position = transform.Position;

            // Add ChimeraMonsterAI component
            var monsterAI = go.AddComponent<ChimeraMonsterAI>();

            // Configure MonoBehaviour based on ECS data
            ConfigureMonoBehaviourFromECS(monsterAI, identity, genetics);

            // Store bidirectional references
            _entityToMonoBehaviour[ecsEntity] = monsterAI;
            _monoBehaviourToEntity[monsterAI] = ecsEntity;

            // Add to AI manager if available
            if (aiManager != null)
                aiManager.AddMonster(monsterAI);

            if (logIntegrationEvents)
                Debug.Log($"🔗 Created MonoBehaviour bridge for ECS entity: {identity.CreatureName}");

            return monsterAI;
        }

        private void SyncSystems()
        {
            if (!enableBidirectionalSync) return;

            int processed = 0;

            // Sync ECS to MonoBehaviour
            if (enableMonoBehaviourIntegration)
            {
                foreach (var kvp in _entityToMonoBehaviour)
                {
                    if (processed >= maxEntitiesPerFrame) break;

                    SyncECSToMonoBehaviour(kvp.Key, kvp.Value);
                    processed++;
                }
            }

            // Sync MonoBehaviour to ECS
            if (enableECSIntegration)
            {
                foreach (var kvp in _monoBehaviourToEntity)
                {
                    if (processed >= maxEntitiesPerFrame) break;

                    SyncMonoBehaviourToECS(kvp.Key, kvp.Value);
                    processed++;
                }
            }
        }

        private void SyncECSToMonoBehaviour(Entity ecsEntity, ChimeraMonsterAI monoBehaviour)
        {
            if (_entityManager == null || !_entityManager.Exists(ecsEntity) || monoBehaviour == null)
                return;

            // Sync position
            var transform = _entityManager.GetComponentData<Unity.Transforms.LocalToWorld>(ecsEntity);
            monoBehaviour.transform.position = transform.Position;

            // Sync behavior state
            if (_entityManager.HasComponent<BehaviorStateComponent>(ecsEntity))
            {
                var behaviorState = _entityManager.GetComponentData<BehaviorStateComponent>(ecsEntity);

                // Convert ECS behavior to MonoBehaviour behavior
                var monoBehaviorType = ConvertToMonoBehaviourBehaviorType(behaviorState.CurrentBehavior);
                monoBehaviour.SetBehaviorType(monoBehaviorType);

                // Sync stress and satisfaction
                monoBehaviour.SetStressLevel(behaviorState.Stress);
            }

            // Sync needs to MonoBehaviour creature state
            if (_entityManager.HasComponent<CreatureNeedsComponent>(ecsEntity))
            {
                var needs = _entityManager.GetComponentData<CreatureNeedsComponent>(ecsEntity);
                monoBehaviour.SetHunger(1f - needs.Hunger); // MonoBehaviour uses hunger as need, ECS as satisfaction
                monoBehaviour.SetEnergyLevel(needs.Energy);
            }
        }

        private void SyncMonoBehaviourToECS(ChimeraMonsterAI monoBehaviour, Entity ecsEntity)
        {
            if (monoBehaviour == null || _entityManager == null || !_entityManager.Exists(ecsEntity))
                return;

            // Sync position
            _entityManager.SetComponentData(ecsEntity, new Unity.Transforms.LocalToWorld
            {
                Value = monoBehaviour.transform.localToWorldMatrix
            });

            // Sync behavior state
            var behaviorState = _entityManager.GetComponentData<BehaviorStateComponent>(ecsEntity);
            behaviorState.CurrentBehavior = ConvertToECSBehaviorType(monoBehaviour.GetCurrentBehaviorType());
            behaviorState.Stress = monoBehaviour.GetStressLevel();
            _entityManager.SetComponentData(ecsEntity, behaviorState);

            // Sync needs
            var needs = _entityManager.GetComponentData<CreatureNeedsComponent>(ecsEntity);
            needs.Hunger = 1f - monoBehaviour.GetHunger(); // Convert MonoBehaviour hunger to ECS satisfaction
            needs.Energy = monoBehaviour.GetEnergyLevel();
            _entityManager.SetComponentData(ecsEntity, needs);
        }

        #region Data Conversion Methods

        private CreatureIdentityComponent ConvertToCreatureIdentity(ChimeraMonsterAI monoBehaviour)
        {
            return new CreatureIdentityComponent
            {
                Species = monoBehaviour.GetSpeciesName(),
                CreatureName = monoBehaviour.name,
                UniqueID = (uint)monoBehaviour.GetInstanceID(),
                Generation = monoBehaviour.GetGeneration(),
                Age = monoBehaviour.GetAge(),
                MaxLifespan = monoBehaviour.GetMaxLifespan(),
                CurrentLifeStage = ConvertToLifeStage(monoBehaviour.GetLifeStage()),
                Rarity = ConvertToRarityLevel(monoBehaviour.GetRarity())
            };
        }

        private GeneticDataComponent ConvertToGeneticData(ChimeraMonsterAI monoBehaviour)
        {
            var genetics = monoBehaviour.GetGeneticsData();

            return new GeneticDataComponent
            {
                Aggression = genetics.GetTraitValue("Aggression", 0.5f),
                Sociability = genetics.GetTraitValue("Sociability", 0.5f),
                Curiosity = genetics.GetTraitValue("Curiosity", 0.5f),
                Caution = genetics.GetTraitValue("Caution", 0.5f),
                Intelligence = genetics.GetTraitValue("Intelligence", 0.5f),
                Metabolism = genetics.GetTraitValue("Metabolism", 1.0f),
                Fertility = genetics.GetTraitValue("Fertility", 0.5f),
                Dominance = genetics.GetTraitValue("Dominance", 0.5f),
                Size = genetics.GetTraitValue("Size", 1.0f),
                Speed = genetics.GetTraitValue("Speed", 1.0f),
                Stamina = genetics.GetTraitValue("Stamina", 1.0f),
                Camouflage = genetics.GetTraitValue("Camouflage", 0.5f),
                HeatTolerance = genetics.GetTraitValue("HeatTolerance", 0.5f),
                ColdTolerance = genetics.GetTraitValue("ColdTolerance", 0.5f),
                WaterAffinity = genetics.GetTraitValue("WaterAffinity", 0.5f),
                Adaptability = genetics.GetTraitValue("Adaptability", 0.5f),
                OverallFitness = genetics.CalculateFitness(),
                NativeBiome = ConvertToBiomeType(genetics.GetNativeBiome()),
                MutationRate = genetics.GetMutationRate()
            };
        }

        private BehaviorStateComponent ConvertToBehaviorState(ChimeraMonsterAI monoBehaviour)
        {
            return new BehaviorStateComponent
            {
                CurrentBehavior = ConvertToECSBehaviorType(monoBehaviour.GetCurrentBehaviorType()),
                BehaviorIntensity = monoBehaviour.GetBehaviorIntensity(),
                Stress = monoBehaviour.GetStressLevel(),
                Satisfaction = monoBehaviour.GetSatisfactionLevel(),
                DecisionConfidence = 0.5f // Default value, could be enhanced
            };
        }

        private CreatureNeedsComponent CreateDefaultNeeds()
        {
            return new CreatureNeedsComponent
            {
                Hunger = 0.7f,
                Thirst = 0.8f,
                Energy = 0.6f,
                Comfort = 0.5f,
                Safety = 0.6f,
                SocialConnection = 0.4f,
                BreedingUrge = 0.3f,
                Exploration = 0.5f,
                HungerDecayRate = 0.01f,
                EnergyRecoveryRate = 0.05f,
                SocialDecayRate = 0.002f
            };
        }

        private SocialTerritoryComponent CreateDefaultSocialTerritory()
        {
            return new SocialTerritoryComponent
            {
                HasTerritory = false,
                TerritoryRadius = 10f,
                TerritoryQuality = 0.5f,
                PreferredPackSize = UnityEngine.Random.Range(2, 6),
                PackLoyalty = 0.5f
            };
        }

        private EnvironmentalComponent ConvertToEnvironmental(ChimeraMonsterAI monoBehaviour)
        {
            return new EnvironmentalComponent
            {
                CurrentBiome = ConvertToBiomeType(monoBehaviour.GetCurrentBiome()),
                CurrentPosition = monoBehaviour.transform.position,
                LocalTemperature = monoBehaviour.GetLocalTemperature(),
                LocalHumidity = monoBehaviour.GetLocalHumidity(),
                BiomeComfortLevel = monoBehaviour.GetBiomeComfortLevel(),
                BiomeAdaptation = 0.8f,
                AdaptationRate = 0.1f,
                HomeRangeRadius = monoBehaviour.GetHomeRangeRadius(),
                ForagingEfficiency = monoBehaviour.GetForagingEfficiency()
            };
        }

        private BreedingComponent CreateDefaultBreeding()
        {
            return new BreedingComponent
            {
                Status = BreedingStatus.NotReady,
                BreedingReadiness = 0.5f,
                Selectiveness = 0.6f,
                RequiresTerritory = true,
                SeasonalBreeder = false,
                ParentalInvestment = 0.7f
            };
        }

        private void ConfigureMonoBehaviourFromECS(ChimeraMonsterAI monsterAI, CreatureIdentityComponent identity, GeneticDataComponent genetics)
        {
            // Configure MonoBehaviour properties based on ECS data
            monsterAI.SetSpeciesName(identity.Species.ToString());
            monsterAI.SetAge(identity.Age);
            monsterAI.SetGeneration(identity.Generation);

            // Set genetic traits
            var geneticsData = monsterAI.GetGeneticsData();
            geneticsData.SetTraitValue("Aggression", genetics.Aggression);
            geneticsData.SetTraitValue("Sociability", genetics.Sociability);
            geneticsData.SetTraitValue("Intelligence", genetics.Intelligence);
            geneticsData.SetTraitValue("Size", genetics.Size);
            geneticsData.SetTraitValue("Speed", genetics.Speed);
        }

        #endregion

        #region Enum Conversion Methods

        private LifeStage ConvertToLifeStage(string lifeStageString)
        {
            if (System.Enum.TryParse<LifeStage>(lifeStageString, out var result))
                return result;
            return LifeStage.Adult;
        }

        private RarityLevel ConvertToRarityLevel(string rarityString)
        {
            if (System.Enum.TryParse<RarityLevel>(rarityString, out var result))
                return result;
            return RarityLevel.Common;
        }

        private BiomeType ConvertToBiomeType(string biomeString)
        {
            if (System.Enum.TryParse<BiomeType>(biomeString, out var result))
                return result;
            return BiomeType.Grassland;
        }

        private CreatureBehaviorType ConvertToECSBehaviorType(AIBehaviorType monoBehaviorType)
        {
            switch (monoBehaviorType)
            {
                case AIBehaviorType.Idle: return CreatureBehaviorType.Idle;
                case AIBehaviorType.Patrol: return CreatureBehaviorType.Exploring;
                case AIBehaviorType.Hunt: return CreatureBehaviorType.Hunting;
                case AIBehaviorType.Flee: return CreatureBehaviorType.Fleeing;
                case AIBehaviorType.Companion: return CreatureBehaviorType.Social;
                case AIBehaviorType.Territorial: return CreatureBehaviorType.Territorial;
                case AIBehaviorType.Foraging: return CreatureBehaviorType.Foraging;
                default: return CreatureBehaviorType.Idle;
            }
        }

        private AIBehaviorType ConvertToMonoBehaviourBehaviorType(CreatureBehaviorType ecsBehaviorType)
        {
            switch (ecsBehaviorType)
            {
                case CreatureBehaviorType.Idle: return AIBehaviorType.Idle;
                case CreatureBehaviorType.Exploring: return AIBehaviorType.Patrol;
                case CreatureBehaviorType.Hunting: return AIBehaviorType.Hunt;
                case CreatureBehaviorType.Fleeing: return AIBehaviorType.Flee;
                case CreatureBehaviorType.Social: return AIBehaviorType.Companion;
                case CreatureBehaviorType.Territorial: return AIBehaviorType.Territorial;
                case CreatureBehaviorType.Foraging: return AIBehaviorType.Foraging;
                default: return AIBehaviorType.Idle;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the MonoBehaviour creature associated with an ECS entity
        /// </summary>
        public ChimeraMonsterAI GetMonoBehaviourCreature(Entity ecsEntity)
        {
            return _entityToMonoBehaviour.TryGetValue(ecsEntity, out var creature) ? creature : null;
        }

        /// <summary>
        /// Get the ECS entity associated with a MonoBehaviour creature
        /// </summary>
        public Entity GetECSEntity(ChimeraMonsterAI monoBehaviourCreature)
        {
            return _monoBehaviourToEntity.TryGetValue(monoBehaviourCreature, out var entity) ? entity : Entity.Null;
        }

        /// <summary>
        /// Force immediate sync between systems
        /// </summary>
        public void ForceSyncSystems()
        {
            SyncSystems();
        }

        /// <summary>
        /// Get integration statistics
        /// </summary>
        public (int bridgedEntities, int monoBehaviours, bool systemsConnected) GetStats()
        {
            return (_entityToMonoBehaviour.Count, _monoBehaviourToEntity.Count,
                    _behaviorSystem != null && _breedingSystem != null);
        }

        #endregion
    }

    /// <summary>
    /// Component to mark ECS entities that are bridged with MonoBehaviour creatures
    /// </summary>
    public struct BridgedCreatureComponent : IComponentData
    {
        public int monoBehaviourInstanceID;
        public bool syncEnabled;
        public float lastSyncTime;
    }
}