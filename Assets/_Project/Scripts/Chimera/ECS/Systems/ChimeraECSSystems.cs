using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Laboratory.Core.ECS;
using Laboratory.Chimera.ECS;
using Laboratory.Chimera.Core;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Laboratory.Chimera.ECS.Systems
{
    /// <summary>
    /// Core ECS systems for Project Chimera creature simulation.
    /// Handles aging, metabolism, genetics expression, and lifecycle management.
    /// </summary>
    
    #region Lifecycle Systems
    
    /// <summary>
    /// Ages creatures over time and handles life stage transitions
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CreatureAgingSystem : SystemBase
    {
        private float lastUpdateTime;
        
        protected override void OnCreate()
        {
            lastUpdateTime = (float)SystemAPI.Time.ElapsedTime;
        }
        
        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            float deltaTime = currentTime - lastUpdateTime;
            
            // Only update every second for performance (aging doesn't need frame-perfect precision)
            if (deltaTime < 1f) return;
            
            lastUpdateTime = currentTime;
            
            // Accelerated time for development - 30x real time (30 seconds = 1 day)
            float ageIncrement = deltaTime / 2880f; // 86400 / 30 = 2880 seconds per day
            
            foreach (var (age, lifecycle, entity) in SystemAPI.Query<RefRW<Laboratory.Chimera.ECS.CreatureAgeComponent>, RefRW<Laboratory.Chimera.ECS.CreatureLifecycleComponent>>().WithEntityAccess())
            {
                // Age the creature
                age.ValueRW.AgeInDays += (int)ageIncrement;

                // Check for life stage transitions
                var newLifeStage = CalculateLifeStage(age.ValueRO.AgeInDays, 365f);

                if (newLifeStage != lifecycle.ValueRO.CurrentStage)
                {
                    lifecycle.ValueRW.CurrentStage = newLifeStage;
                    lifecycle.ValueRW.StageProgress = age.ValueRO.AgeInDays / 365f;
                }
            }
        }
        
        private static LifeStage CalculateLifeStage(float ageInDays, float lifeExpectancy)
        {
            float ageRatio = ageInDays / lifeExpectancy;

            if (ageRatio < 0.1f) return LifeStage.Baby;
            if (ageRatio < 0.25f) return LifeStage.Child;
            if (ageRatio < 0.5f) return LifeStage.Teen;
            if (ageRatio < 0.8f) return LifeStage.Adult;

            return LifeStage.Elderly;
        }

        private static float3 CalculateCurrentSize(float maturationProgress, LifeStage stage)
        {
            float sizeMultiplier = stage switch
            {
                LifeStage.Baby => 0.1f,
                LifeStage.Child => math.lerp(0.3f, 0.6f, maturationProgress),
                LifeStage.Teen => math.lerp(0.6f, 0.9f, maturationProgress),
                LifeStage.Adult => math.lerp(0.9f, 1f, maturationProgress),
                LifeStage.Elderly => 0.95f, // Slightly smaller due to age
                _ => 1f
            };

            return new float3(sizeMultiplier, sizeMultiplier, sizeMultiplier);
        }
        
        private static float CalculateDeathProbability(float ageInDays, float lifeExpectancy)
        {
            float ageRatio = ageInDays / lifeExpectancy;
            if (ageRatio < 0.8f) return 0f;

            return math.pow(ageRatio - 0.8f, 3f) * 0.1f;
        }
    }
    
    /// <summary>
    /// Manages creature needs (hunger, thirst, social, etc.) and their decay over time
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CreatureNeedsSystem : SystemBase
    {
        private float lastUpdateTime;
        
        protected override void OnCreate()
        {
            lastUpdateTime = (float)SystemAPI.Time.ElapsedTime;
        }
        
        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            float deltaTime = currentTime - lastUpdateTime;
            
            // Update every 5 seconds for performance
            if (deltaTime < 5f) return;
            
            lastUpdateTime = currentTime;
            float hourlyDecay = deltaTime / 3600f; // Convert to hours
            
            foreach (var (needs, personality) in SystemAPI.Query<RefRW<Laboratory.Chimera.ECS.CreatureNeedsComponent>, RefRO<Laboratory.Chimera.ECS.CreaturePersonalityComponent>>())
            {
                // Decay needs over time using actual component properties
                var currentNeeds = needs.ValueRO;
                currentNeeds.Hunger = math.max(0f, currentNeeds.Hunger - 0.1f * hourlyDecay);
                currentNeeds.Thirst = math.max(0f, currentNeeds.Thirst - 0.15f * hourlyDecay);
                currentNeeds.Energy = math.max(0f, currentNeeds.Energy - 0.08f * hourlyDecay);
                currentNeeds.SocialConnection = math.max(0f, currentNeeds.SocialConnection - 0.05f * hourlyDecay);
                currentNeeds.Play = math.max(0f, currentNeeds.Play - 0.03f * hourlyDecay);

                // Clamp all values to valid ranges
                currentNeeds.Hunger = math.clamp(currentNeeds.Hunger, 0f, 1f);
                currentNeeds.Thirst = math.clamp(currentNeeds.Thirst, 0f, 1f);
                currentNeeds.Energy = math.clamp(currentNeeds.Energy, 0f, 1f);
                currentNeeds.SocialConnection = math.clamp(currentNeeds.SocialConnection, 0f, 1f);
                currentNeeds.Play = math.clamp(currentNeeds.Play, 0f, 1f);

                needs.ValueRW = currentNeeds;
            }
        }
    }
    
    #endregion
    
    #region Behavior Systems
    
    /// <summary>
    /// Updates creature behavior based on genetics, personality, and current needs
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CreatureBehaviorSystem : SystemBase
    {
        private float lastUpdateTime;
        
        protected override void OnCreate()
        {
            lastUpdateTime = (float)SystemAPI.Time.ElapsedTime;
        }
        
        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            float deltaTime = currentTime - lastUpdateTime;
            
            // Update every 2 seconds for behavior changes
            if (deltaTime < 2f) return;
            
            lastUpdateTime = currentTime;
            
            foreach (var (behavior, personality, needs, genetics) in SystemAPI.Query<RefRW<Laboratory.Chimera.ECS.CreatureBehaviorComponent>, RefRO<Laboratory.Chimera.ECS.CreaturePersonalityComponent>, RefRO<Laboratory.Chimera.ECS.CreatureNeedsComponent>, RefRO<Laboratory.Chimera.ECS.CreatureGeneticsComponent>>())
            {
                // Determine appropriate behavior based on needs and personality
                Laboratory.Chimera.ECS.AIState newState = DetermineBehaviorState(behavior.ValueRO, personality.ValueRO, needs.ValueRO, currentTime);

                if (newState != behavior.ValueRO.currentState)
                {
                    behavior.ValueRW.currentState = newState;
                    behavior.ValueRW.stateChangeTime = currentTime;
                }

                // Update behavior type based on personality
                behavior.ValueRW.behaviorType = DetermineBehaviorType(personality.ValueRO, genetics.ValueRO);
            }
        }
        
        private static Laboratory.Chimera.ECS.AIState DetermineBehaviorState(Laboratory.Chimera.ECS.CreatureBehaviorComponent behavior,
                                                     Laboratory.Chimera.ECS.CreaturePersonalityComponent personality,
                                                     Laboratory.Chimera.ECS.CreatureNeedsComponent needs,
                                                     float currentTime)
        {
            // Behavior based on actual component properties
            if (needs.Hunger < 0.3f || needs.Thirst < 0.3f)
            {
                return Laboratory.Chimera.ECS.AIState.Feed; // Search for resources
            }

            if (needs.SocialConnection < 0.4f && personality.Loyalty > 0.6f)
            {
                return Laboratory.Chimera.ECS.AIState.Follow; // Seek companionship
            }

            if (needs.Energy < 0.3f)
            {
                return Laboratory.Chimera.ECS.AIState.Rest; // Need to rest
            }

            if (personality.Curiosity > 0.5f)
            {
                return Laboratory.Chimera.ECS.AIState.Patrol; // Explore
            }

            // Default to idle if needs are met
            return Laboratory.Chimera.ECS.AIState.Idle;
        }
        
        private static int DetermineBehaviorType(Laboratory.Chimera.ECS.CreaturePersonalityComponent personality,
                                                           Laboratory.Chimera.ECS.CreatureGeneticsComponent genetics)
        {
            // Simplified behavior type determination using actual properties
            if (personality.Aggression > 0.7f)
            {
                return 1; // Aggressive
            }

            if (personality.Loyalty > 0.6f)
            {
                return 0; // Companion
            }

            if (personality.Curiosity > 0.8f)
            {
                return 2; // Defensive
            }

            if (personality.Fearfulness > 0.7f)
            {
                return 3; // Passive
            }

            return 5; // Wild - Default behavior
        }
    }
    
    #endregion
    
    #region Environmental Systems
    
    /// <summary>
    /// Handles environmental adaptation and biome comfort levels
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EnvironmentalAdaptationSystem : SystemBase
    {
        private float lastUpdateTime;
        
        protected override void OnCreate()
        {
            lastUpdateTime = (float)SystemAPI.Time.ElapsedTime;
        }
        
        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            float deltaTime = currentTime - lastUpdateTime;
            
            // Update every 10 seconds for environmental effects
            if (deltaTime < 10f) return;
            
            lastUpdateTime = currentTime;
            
            foreach (var (environmental, biome) in SystemAPI.Query<RefRW<Laboratory.Chimera.ECS.CreatureEnvironmentalComponent>, RefRW<Laboratory.Chimera.ECS.CreatureBiomeComponent>>())
            {
                // Simplified environmental adaptation using actual component properties
                float adaptationSpeed = 0.01f * deltaTime;

                // Calculate basic environmental stress
                var envData = environmental.ValueRO;
                envData.TemperatureTolerance = math.clamp(envData.TemperatureTolerance, 0f, 1f);
                envData.HumidityTolerance = math.clamp(envData.HumidityTolerance, 0f, 1f);
                envData.AltitudeTolerance = math.clamp(envData.AltitudeTolerance, 0f, 1f);
                environmental.ValueRW = envData;

                // Update biome comfort level
                var biomeData = biome.ValueRO;
                biomeData.BiomeComfortLevel = math.lerp(biomeData.BiomeComfortLevel, biomeData.AdaptationLevel, adaptationSpeed);
                biome.ValueRW = biomeData;
            }
        }
    }
    
    /// <summary>
    /// Synchronizes world environmental conditions to all creatures
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BiomeConditionsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Simplified without world conditions singleton
            // Update environmental conditions directly
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach (var environmental in SystemAPI.Query<RefRW<Laboratory.Chimera.ECS.CreatureEnvironmentalComponent>>())
            {
                // Update basic environmental conditions
                var envData = environmental.ValueRO;
                envData.TemperatureTolerance = math.clamp(envData.TemperatureTolerance + deltaTime * 0.01f, 0.4f, 0.8f);
                envData.HumidityTolerance = math.clamp(envData.HumidityTolerance + deltaTime * 0.01f, 0.4f, 0.7f);
                environmental.ValueRW = envData;
            }
        }
    }
    
    #endregion
    
    #region Breeding Systems
    
    /// <summary>
    /// Manages creature breeding readiness and mating behavior
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CreatureBreedingSystem : SystemBase
    {
        private float lastUpdateTime;
        
        protected override void OnCreate()
        {
            lastUpdateTime = (float)SystemAPI.Time.ElapsedTime;
        }
        
        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            float deltaTime = currentTime - lastUpdateTime;
            
            // Update every 30 seconds for breeding checks
            if (deltaTime < 30f) return;
            
            lastUpdateTime = currentTime;
            
            foreach (var (breeding, age, needs, personality) in SystemAPI.Query<RefRW<Laboratory.Chimera.ECS.CreatureBreedingComponent>, RefRO<Laboratory.Chimera.ECS.CreatureAgeComponent>, RefRO<Laboratory.Chimera.ECS.CreatureNeedsComponent>, RefRO<Laboratory.Chimera.ECS.CreaturePersonalityComponent>>())
            {
                // Update breeding readiness using actual component properties
                var breedingData = breeding.ValueRO;
                breedingData.IsReadyToBreed = age.ValueRO.AgeInDays >= 100; // Adult at 100 days

                // Update breeding based on needs and personality
                if (breedingData.IsReadyToBreed)
                {
                    float baseDesire = 0.3f;

                    // Social creatures are more interested in breeding
                    float socialBonus = personality.ValueRO.Loyalty * 0.2f;

                    // Reduce desire if recently bred
                    float timeSinceLastBreeding = currentTime - breedingData.LastBreedTime;
                    float cooldownPenalty = math.max(0f, (86400f - timeSinceLastBreeding) / 86400f) * 0.4f; // 1 day cooldown

                    float totalDesire = math.clamp(baseDesire + socialBonus - cooldownPenalty, 0f, 1f);
                }

                // Handle offspring count updates
                if (breedingData.OffspringCount < 10) // Maximum offspring limit
                {
                    breedingData.FertilityScore = math.clamp(breedingData.FertilityScore, 0.1f, 1f);
                }
                breeding.ValueRW = breedingData;
            }
        }
    }
    
    #endregion
    
    #region Social Systems
    
    /// <summary>
    /// Manages creature bonding with players and other creatures
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CreatureBondingSystem : SystemBase
    {
        private float lastUpdateTime;
        
        protected override void OnCreate()
        {
            lastUpdateTime = (float)SystemAPI.Time.ElapsedTime;
        }
        
        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            float deltaTime = currentTime - lastUpdateTime;
            
            // Update every 5 seconds for bonding
            if (deltaTime < 5f) return;
            
            lastUpdateTime = currentTime;
            
            foreach (var (bonding, personality) in SystemAPI.Query<RefRW<Laboratory.Chimera.ECS.CreatureBondingComponent>, RefRO<Laboratory.Chimera.ECS.CreaturePersonalityComponent>>())
            {
                // Bond strength naturally increases over time with loyal creatures
                var bondingData = bonding.ValueRO;
                if (personality.ValueRO.Loyalty > 0.5f)
                {
                    float bondGrowthRate = personality.ValueRO.Loyalty * 0.01f * deltaTime;
                    bondingData.BondStrength = math.min(1f, bondingData.BondStrength + bondGrowthRate);
                }

                // Update social need based on personality
                bondingData.SocialNeed = personality.ValueRO.SocialNeed;
                bonding.ValueRW = bondingData;
            }
        }
    }
    
    #endregion
    
    #region Statistics Systems
    
    /// <summary>
    /// Updates creature statistics based on genetics, age, and environmental factors
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CreatureStatsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (stats, genetics, age, needs) in SystemAPI.Query<RefRW<Laboratory.Chimera.ECS.CreatureStatsComponent>, RefRO<Laboratory.Chimera.ECS.CreatureGeneticsComponent>, RefRO<Laboratory.Chimera.ECS.CreatureAgeComponent>, RefRO<Laboratory.Chimera.ECS.CreatureNeedsComponent>>())
            {
                // Simplified genetics modifiers
                float geneticModifier = (genetics.ValueRO.GeneticPurity + 0.5f) * 0.5f; // 0.25 - 0.75 range

                // Age affects stats
                float ageRatio = age.ValueRO.AgeInDays / 365f;
                float ageModifier = ageRatio < 0.3f ? 0.7f : (ageRatio < 0.8f ? 1.0f : 0.9f);

                // Update base stats with genetic and age modifiers (using base stats from component)
                var statsData = stats.ValueRO;
                var baseStats = statsData.BaseStats;
                baseStats.attack = (int)math.clamp(baseStats.attack * geneticModifier * ageModifier, 10f, 100f);
                baseStats.defense = (int)math.clamp(baseStats.defense * geneticModifier * ageModifier, 10f, 100f);
                baseStats.speed = (int)math.clamp(baseStats.speed * geneticModifier, 10f, 100f);
                baseStats.intelligence = (int)math.clamp(baseStats.intelligence * geneticModifier, 10f, 100f);
                baseStats.charisma = (int)math.clamp(baseStats.charisma * ageModifier, 10f, 100f);
                statsData.BaseStats = baseStats;
                stats.ValueRW = statsData;
            }
        }
    }
    
    #endregion
    
    #region Sync Systems
    
    /// <summary>
    /// High-performance GameObject synchronization system with cached lookups
    /// Optimized to eliminate expensive FindObjectsOfTypeAll calls
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class GameObjectSyncSystem : SystemBase
    {
        internal static readonly Dictionary<int, UnityEngine.GameObject> _gameObjectCache = new Dictionary<int, UnityEngine.GameObject>();
        private static float _lastCacheRefreshTime = 0f;
        private const float CACHE_REFRESH_INTERVAL = 5f; // Refresh cache every 5 seconds

        protected override void OnCreate()
        {
            RefreshGameObjectCache();
        }

        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Periodically refresh cache to handle destroyed/created GameObjects
            if (currentTime - _lastCacheRefreshTime > CACHE_REFRESH_INTERVAL)
            {
                RefreshGameObjectCache();
                _lastCacheRefreshTime = currentTime;
            }

            // High-performance sync using cached lookups
            foreach (var (link, transform) in SystemAPI.Query<RefRO<GameObjectLinkComponent>, RefRO<LocalTransform>>())
            {
                int instanceID = link.ValueRO.InstanceID;

                if (_gameObjectCache.TryGetValue(instanceID, out var gameObject) &&
                    gameObject != null && gameObject.activeInHierarchy)
                {
                    gameObject.transform.SetPositionAndRotation(transform.ValueRO.Position, transform.ValueRO.Rotation);
                }
                else if (gameObject == null)
                {
                    // Remove invalid entries from cache
                    _gameObjectCache.Remove(instanceID);
                }
            }
        }

        private static void RefreshGameObjectCache()
        {
            _gameObjectCache.Clear();

            // Only search once during cache refresh instead of every frame
            var allGameObjects = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
            foreach (var go in allGameObjects)
            {
                if (go != null)
                {
                    _gameObjectCache[go.GetInstanceID()] = go;
                }
            }

            UnityEngine.Debug.Log($"GameObjectSyncSystem: Cached {_gameObjectCache.Count} GameObjects");
        }

        protected override void OnDestroy()
        {
            _gameObjectCache.Clear();
        }
    }
    
    #endregion

    #region Movement Systems

    /// <summary>
    /// Handles creature movement and pathfinding
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CreatureMovementSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, movement, stats, personality) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Laboratory.Chimera.ECS.CreatureMovementComponent>, RefRO<Laboratory.Chimera.ECS.CreatureStatsComponent>, RefRO<Laboratory.Chimera.ECS.CreaturePersonalityComponent>>())
            {
                if (!movement.ValueRO.IsMoving)
                    continue;

                // Update movement speed based on stats
                float currentSpeed = movement.ValueRO.BaseSpeed * (stats.ValueRO.BaseStats.speed / 100f);
                var movementData = movement.ValueRO;
                movementData.CurrentSpeed = currentSpeed;
                movement.ValueRW = movementData;

                // Simple random movement for now
                float3 randomDirection = new float3(
                    UnityEngine.Random.Range(-1f, 1f),
                    0f,
                    UnityEngine.Random.Range(-1f, 1f)
                );
                randomDirection = math.normalize(randomDirection);
                float3 moveStep = randomDirection * currentSpeed * deltaTime;
                transform.ValueRW.Position += moveStep;

                // Rotate towards movement direction
                if (math.lengthsq(randomDirection) > 0.001f)
                {
                    quaternion targetRotation = quaternion.LookRotationSafe(randomDirection, math.up());
                    transform.ValueRW.Rotation = math.slerp(transform.ValueRO.Rotation, targetRotation,
                                                          movement.ValueRO.RotationSpeed * deltaTime);
                }
            }
        }
    }

    #endregion

    #region Health Systems

    /// <summary>
    /// Manages creature health, damage, and death
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CreatureHealthSystem : SystemBase
    {
        private EntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            var ecb = ecbSystem.CreateCommandBuffer();

            // Handle health regeneration and death checks
            foreach (var (health, needs, genetics, entity) in SystemAPI.Query<RefRW<Laboratory.Chimera.ECS.CreatureHealthComponent>, RefRO<Laboratory.Chimera.ECS.CreatureNeedsComponent>, RefRO<Laboratory.Chimera.ECS.CreatureGeneticsComponent>>().WithEntityAccess())
            {
                if (!health.ValueRO.IsAlive)
                    continue;

                var healthComp = health.ValueRO;

                // Natural health regeneration when well-fed and rested
                if (needs.ValueRO.Energy > 0.7f && needs.ValueRO.Hunger > 0.6f && healthComp.CurrentHealth < healthComp.MaxHealth)
                {
                    float regenRate = healthComp.RegenerationRate * (genetics.ValueRO.GeneticPurity + 0.5f);
                    healthComp.CurrentHealth = math.min(healthComp.MaxHealth,
                                                       healthComp.CurrentHealth + (int)(regenRate * deltaTime));
                }

                // Check for death conditions
                if (healthComp.CurrentHealth <= 0)
                {
                    healthComp.IsAlive = false;
                    ecb.AddComponent<DeadTag>(entity);
                }

                // Update last damage time tracking
                // Track damage (simplified without DamageTaken field)
                if (healthComp.CurrentHealth < healthComp.MaxHealth * 0.9f)
                {
                    healthComp.LastDamageTime = currentTime;
                }

                health.ValueRW = healthComp;
            }

            ecb.Playback(EntityManager);
        }
    }

    /// <summary>
    /// High-performance creature death processing system with cached lookups
    /// Optimized to eliminate expensive FindObjectsOfTypeAll calls
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CreatureDeathSystem : SystemBase
    {
        private EntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer();

            // Handle dead creatures using cached GameObject lookup
            foreach (var (link, entity) in SystemAPI.Query<RefRO<GameObjectLinkComponent>>().WithAll<DeadTag>().WithEntityAccess())
            {
                int instanceID = link.ValueRO.InstanceID;

                // Use the shared cache from GameObjectSyncSystem for consistency
                if (GameObjectSyncSystem._gameObjectCache.TryGetValue(instanceID, out var gameObject) && gameObject != null)
                {
                    gameObject.SetActive(false);

                    // Optional: Add death effects here
                    // PlayDeathEffects(gameObject);
                }

                // Destroy the entity (could add corpse mechanics here)
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
        }
    }

    #endregion
}