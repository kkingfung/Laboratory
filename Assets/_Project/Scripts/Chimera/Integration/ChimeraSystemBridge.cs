using UnityEngine;
using Unity.Entities;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.AI;
using Laboratory.Chimera.ECS;
using Laboratory.Core.ECS.Systems;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Core;
using Laboratory.Core.Enums;
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
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world != null)
                _entityManager = world.EntityManager;
            _behaviorSystem = world?.GetOrCreateSystemManaged<ChimeraBehaviorSystem>();
            _breedingSystem = world?.GetOrCreateSystemManaged<ChimeraBreedingSystem>();

            // Load configurations if not assigned
            if (unifiedConfig == null)
                unifiedConfig = Resources.Load<ChimeraUniverseConfiguration>("Configs/ChimeraUniverse");

            if (traitLibrary == null)
                traitLibrary = FindFirstObjectByType<GeneticTraitLibrary>();

            // Initialize AI manager integration
            if (aiManager == null)
                aiManager = FindFirstObjectByType<ChimeraAIManager>();

            // Create initial bridges for existing MonoBehaviour creatures
            CreateInitialBridges();

            if (logIntegrationEvents)
                UnityEngine.Debug.Log("🔗 ChimeraSystemBridge initialized - connecting ECS and MonoBehaviour systems");
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

            // Transform synchronization handled through MonoBehaviour transforms
            // ECS position management coordinated through EnvironmentalComponent

            // Store bidirectional references
            _entityToMonoBehaviour[entity] = monoBehaviourCreature;
            _monoBehaviourToEntity[monoBehaviourCreature] = entity;

            // Add bridge component for identification
            _entityManager.AddComponentData(entity, new BridgedCreatureComponent
            {
                monoBehaviourInstanceID = monoBehaviourCreature.GetInstanceID(),
                syncEnabled = true,
                lastSyncTime = Time.time
            });

            if (logIntegrationEvents)
                UnityEngine.Debug.Log($"🔗 Created ECS bridge for MonoBehaviour creature: {monoBehaviourCreature.name}");

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
            var genetics = _entityManager.GetComponentData<ChimeraGeneticDataComponent>(ecsEntity);
            var environmental = _entityManager.GetComponentData<EnvironmentalComponent>(ecsEntity);

            // Create GameObject
            var go = new GameObject($"Bridged_{identity.CreatureName}");
            go.transform.position = environmental.CurrentPosition; // Use ECS position

            // Add ChimeraMonsterAI component
            var monsterAI = go.AddComponent<ChimeraMonsterAI>();

            // Configure MonoBehaviour based on ECS data
            ConfigureMonoBehaviourFromECS(monsterAI, identity, genetics);

            // Store bidirectional references
            _entityToMonoBehaviour[ecsEntity] = monsterAI;
            _monoBehaviourToEntity[monsterAI] = ecsEntity;

            // Add to AI manager if available
            if (aiManager != null)
            {
                aiManager.RegisterMonster(monsterAI);
            }

            if (logIntegrationEvents)
                UnityEngine.Debug.Log($"🔗 Created MonoBehaviour bridge for ECS entity: {identity.CreatureName}");

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

            // Update sync timestamp
            if (_entityManager.HasComponent<BridgedCreatureComponent>(ecsEntity))
            {
                var bridgeComponent = _entityManager.GetComponentData<BridgedCreatureComponent>(ecsEntity);
                bridgeComponent.lastSyncTime = Time.time;
                _entityManager.SetComponentData(ecsEntity, bridgeComponent);
            }

            // Sync position from ECS to MonoBehaviour
            if (_entityManager.HasComponent<EnvironmentalComponent>(ecsEntity))
            {
                var environmental = _entityManager.GetComponentData<EnvironmentalComponent>(ecsEntity);
                monoBehaviour.transform.position = environmental.CurrentPosition;
            }

            // Sync behavior state
            if (_entityManager.HasComponent<BehaviorStateComponent>(ecsEntity))
            {
                var behaviorState = _entityManager.GetComponentData<BehaviorStateComponent>(ecsEntity);

                // Convert ECS behavior to MonoBehaviour behavior
                var monoBehaviorType = ConvertToMonoBehaviourBehaviorType(behaviorState.CurrentBehavior);
                monoBehaviour.SetBehaviorType(monoBehaviorType);

                // Sync stress and satisfaction
                // Note: Stress is read-only in MonoBehaviour AI, managed through other systems
            }

            // Sync needs to MonoBehaviour creature state
            if (_entityManager.HasComponent<CreatureNeedsComponent>(ecsEntity))
            {
                var needs = _entityManager.GetComponentData<CreatureNeedsComponent>(ecsEntity);

                // Use needs data to influence MonoBehaviour AI behavior
                // Since MonoBehaviour AI doesn't have granular need setters, we derive behavior modifications
                if (needs.Hunger > 0.8f || needs.Thirst > 0.8f || needs.Energy < 0.2f)
                {
                    // High needs should increase foraging behavior
                    monoBehaviour.SetBehaviorType(AIBehaviorType.Foraging);
                }
                else if (needs.Safety < 0.3f)
                {
                    // Low safety should trigger flee behavior
                    monoBehaviour.SetBehaviorType(AIBehaviorType.Flee);
                }
                else if (needs.SocialConnection > 0.7f && needs.BreedingUrge > 0.6f)
                {
                    // High social/breeding needs should trigger social behavior
                    monoBehaviour.SetBehaviorType(AIBehaviorType.Companion);
                }

                if (logIntegrationEvents && (needs.Hunger > 0.9f || needs.Energy < 0.1f))
                {
                    UnityEngine.Debug.Log($"🔗 Critical needs detected for {monoBehaviour.name}: Hunger={needs.Hunger:F2}, Energy={needs.Energy:F2}");
                }
            }
        }

        private void SyncMonoBehaviourToECS(ChimeraMonsterAI monoBehaviour, Entity ecsEntity)
        {
            if (monoBehaviour == null || _entityManager == null || !_entityManager.Exists(ecsEntity))
                return;

            // Sync position from MonoBehaviour to ECS
            if (_entityManager.HasComponent<EnvironmentalComponent>(ecsEntity))
            {
                var environmental = _entityManager.GetComponentData<EnvironmentalComponent>(ecsEntity);
                environmental.CurrentPosition = monoBehaviour.transform.position;
                _entityManager.SetComponentData(ecsEntity, environmental);
            }

            // Sync behavior state
            var behaviorState = _entityManager.GetComponentData<BehaviorStateComponent>(ecsEntity);
            behaviorState.CurrentBehavior = ConvertToECSBehaviorType(monoBehaviour.GetCurrentBehaviorType());
            behaviorState.Stress = monoBehaviour.GetStressLevel(); // Use actual stress level
            behaviorState.BehaviorIntensity = monoBehaviour.GetBehaviorIntensity();
            behaviorState.Satisfaction = monoBehaviour.GetSatisfactionLevel();
            _entityManager.SetComponentData(ecsEntity, behaviorState);

            // Sync needs - MonoBehaviour AI doesn't expose granular needs
            // ECS system manages detailed need values independently
            var needs = _entityManager.GetComponentData<CreatureNeedsComponent>(ecsEntity);
            // Derive basic needs from behavior state and stress
            needs.Energy = Mathf.Clamp01(1.0f - behaviorState.Stress); // Higher stress = lower energy
            needs.Comfort = behaviorState.Satisfaction;
            needs.Safety = Mathf.Clamp01(1.0f - behaviorState.Stress * 0.8f);
            _entityManager.SetComponentData(ecsEntity, needs);
        }

        #region Data Conversion Methods

        private CreatureIdentityComponent ConvertToCreatureIdentity(ChimeraMonsterAI monoBehaviour)
        {
            // Extract data from MonoBehaviour or use intelligent defaults
            var geneticsData = monoBehaviour.GetGeneticsData();
            float size = geneticsData.GetTraitValue(TraitType.Size, 1.0f);
            float intelligence = geneticsData.GetTraitValue(TraitType.Intelligence, 0.5f);

            // Calculate derived values based on genetics
            float baseLifespan = 80f + (size * 40f) + (intelligence * 20f); // Larger, smarter creatures live longer
            float currentAge = UnityEngine.Random.Range(15f, baseLifespan * 0.8f); // Random adult age

            return new CreatureIdentityComponent
            {
                Species = DeriveSpeciesFromBehavior(monoBehaviour),
                CreatureName = monoBehaviour.name,
                UniqueID = (uint)monoBehaviour.GetInstanceID(),
                Generation = UnityEngine.Random.Range(1, 5), // Random generation 1-4
                Age = currentAge,
                MaxLifespan = baseLifespan,
                CurrentLifeStage = DeriveLifeStage(currentAge, baseLifespan),
                Rarity = DeriveRarityFromGenetics(geneticsData)
            };
        }

        private ChimeraGeneticDataComponent ConvertToGeneticData(ChimeraMonsterAI monoBehaviour)
        {
            var genetics = monoBehaviour.GetGeneticsData();

            return new ChimeraGeneticDataComponent
            {
                Aggression = genetics.GetTraitValue(TraitType.Aggression, 0.5f),
                Sociability = genetics.GetTraitValue(TraitType.Sociability, 0.5f),
                Curiosity = genetics.GetTraitValue(TraitType.Curiosity, 0.5f),
                Caution = genetics.GetTraitValue(TraitType.Caution, 0.5f),
                Intelligence = genetics.GetTraitValue(TraitType.Intelligence, 0.5f),
                Metabolism = genetics.GetTraitValue(TraitType.Metabolism, 1.0f),
                Fertility = genetics.GetTraitValue(TraitType.Fertility, 0.5f),
                Dominance = genetics.GetTraitValue(TraitType.Dominance, 0.5f),
                Size = genetics.GetTraitValue(TraitType.Size, 1.0f),
                Speed = genetics.GetTraitValue(TraitType.Speed, 1.0f),
                Stamina = genetics.GetTraitValue(TraitType.Stamina, 1.0f),
                Camouflage = genetics.GetTraitValue(TraitType.Camouflage, 0.5f),
                HeatTolerance = genetics.GetTraitValue(TraitType.HeatTolerance, 0.5f),
                ColdTolerance = genetics.GetTraitValue(TraitType.ColdTolerance, 0.5f),
                WaterAffinity = genetics.GetTraitValue(TraitType.WaterAffinity, 0.5f),
                Adaptability = genetics.GetTraitValue(TraitType.Adaptability, 0.5f),
                OverallFitness = 0.5f, // Default fitness
                NativeBiome = Laboratory.Core.Enums.BiomeType.Grassland, // Default biome
                MutationRate = 0.02f // Default mutation rate
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
                CurrentBiome = Laboratory.Core.Enums.BiomeType.Forest, // Default biome
                CurrentPosition = monoBehaviour.transform.position,
                LocalTemperature = 20f, // Default temperature
                LocalHumidity = 0.5f, // Default humidity
                BiomeComfortLevel = 0.7f, // Default comfort level
                BiomeAdaptation = 0.8f,
                AdaptationRate = 0.1f,
                HomeRangeRadius = 50f, // Default home range
                ForagingEfficiency = 0.6f // Default foraging efficiency
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

        private void ConfigureMonoBehaviourFromECS(ChimeraMonsterAI monsterAI, CreatureIdentityComponent identity, ChimeraGeneticDataComponent genetics)
        {
            // Configure MonoBehaviour properties based on ECS data
            // Set name from identity
            monsterAI.gameObject.name = $"{identity.Species}_{identity.CreatureName}";

            // Configure behavior based on genetic traits and life stage
            monsterAI.SetAggressionLevel(genetics.Aggression);

            // Adjust behavior based on life stage
            switch (identity.CurrentLifeStage)
            {
                case Laboratory.Chimera.ECS.LifeStage.Juvenile:
                    // Juveniles are more curious and less aggressive
                    monsterAI.SetAggressionLevel(genetics.Aggression * 0.6f);
                    break;
                case Laboratory.Chimera.ECS.LifeStage.Elder:
                    // Elders are less active but more territorial
                    monsterAI.SetAggressionLevel(genetics.Aggression * 1.2f);
                    break;
            }

            // Set genetic traits
            var geneticsData = monsterAI.GetGeneticsData();
            geneticsData.SetTraitValue("Aggression", genetics.Aggression);
            geneticsData.SetTraitValue("Sociability", genetics.Sociability);
            geneticsData.SetTraitValue("Intelligence", genetics.Intelligence);
            geneticsData.SetTraitValue("Size", genetics.Size);
            geneticsData.SetTraitValue("Speed", genetics.Speed);
            geneticsData.SetTraitValue("Adaptability", genetics.Adaptability);
            geneticsData.SetTraitValue("Dominance", genetics.Dominance);

            if (logIntegrationEvents)
            {
                UnityEngine.Debug.Log($"🔗 Configured {identity.Species} (Gen {identity.Generation}, Age {identity.Age:F1}) with {identity.CurrentLifeStage} traits");
            }
        }

        #endregion

        #region Helper Methods for Intelligent Defaults

        private string DeriveSpeciesFromBehavior(ChimeraMonsterAI monoBehaviour)
        {
            var genetics = monoBehaviour.GetGeneticsData();
            float aggression = genetics.GetTraitValue(TraitType.Aggression, 0.5f);
            float sociability = genetics.GetTraitValue(TraitType.Sociability, 0.5f);
            float size = genetics.GetTraitValue(TraitType.Size, 1.0f);

            // Derive species name based on dominant traits
            if (aggression > 0.7f && size > 1.2f)
                return "Apex Chimera";
            else if (sociability > 0.7f)
                return "Social Chimera";
            else if (size < 0.7f)
                return "Swift Chimera";
            else
                return "Common Chimera";
        }

        private Laboratory.Chimera.ECS.LifeStage DeriveLifeStage(float currentAge, float maxLifespan)
        {
            float ageRatio = currentAge / maxLifespan;

            if (ageRatio < 0.15f)
                return (Laboratory.Chimera.ECS.LifeStage)Laboratory.Chimera.Core.LifeStage.Juvenile;
            else if (ageRatio < 0.75f)
                return (Laboratory.Chimera.ECS.LifeStage)Laboratory.Chimera.Core.LifeStage.Adult;
            else
                return (Laboratory.Chimera.ECS.LifeStage)Laboratory.Chimera.Core.LifeStage.Elder;
        }

        private Laboratory.Chimera.ECS.RarityLevel DeriveRarityFromGenetics(GeneticProfile genetics)
        {
            // Calculate rarity based on unique trait combinations
            float uniqueness = 0f;

            // High values in rare combinations increase rarity
            float intelligence = genetics.GetTraitValue(TraitType.Intelligence, 0.5f);
            float adaptability = genetics.GetTraitValue(TraitType.Adaptability, 0.5f);
            float size = genetics.GetTraitValue(TraitType.Size, 1.0f);

            if (intelligence > 0.8f && adaptability > 0.8f)
                uniqueness += 0.4f;
            if (size > 1.5f || size < 0.3f)
                uniqueness += 0.3f;

            if (uniqueness > 0.6f)
                return (Laboratory.Chimera.ECS.RarityLevel)Laboratory.Chimera.Core.RarityLevel.Legendary;
            else if (uniqueness > 0.4f)
                return (Laboratory.Chimera.ECS.RarityLevel)Laboratory.Chimera.Core.RarityLevel.Rare;
            else if (uniqueness > 0.2f)
                return (Laboratory.Chimera.ECS.RarityLevel)Laboratory.Chimera.Core.RarityLevel.Uncommon;
            else
                return (Laboratory.Chimera.ECS.RarityLevel)Laboratory.Chimera.Core.RarityLevel.Common;
        }

        #endregion

        #region Enum Conversion Methods

        private Laboratory.Chimera.Core.LifeStage ConvertToLifeStage(string lifeStageString)
        {
            if (System.Enum.TryParse<Laboratory.Chimera.Core.LifeStage>(lifeStageString, out var result))
                return result;
            return Laboratory.Chimera.Core.LifeStage.Adult;
        }

        private Laboratory.Chimera.Core.RarityLevel ConvertToRarityLevel(string rarityString)
        {
            if (System.Enum.TryParse<Laboratory.Chimera.Core.RarityLevel>(rarityString, out var result))
                return result;
            return Laboratory.Chimera.Core.RarityLevel.Common;
        }

        private Laboratory.Core.Enums.BiomeType ConvertToBiomeType(string biomeString)
        {
            if (System.Enum.TryParse<Laboratory.Core.Enums.BiomeType>(biomeString, out var result))
                return result;
            return Laboratory.Core.Enums.BiomeType.Grassland;
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