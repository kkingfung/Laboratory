using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Laboratory.Chimera.ECS;
using UnityEngine;
using System.Linq;

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
            
            Entities
                .ForEach((Entity entity, ref CreatureAgeComponent age, ref CreatureLifecycleComponent lifecycle) =>
                {
                    // Age the creature
                    age.AgeInDays += (int)ageIncrement;

                    // Update adult status - simplified without non-existent properties
                    age.IsAdult = age.AgeInDays >= 30f; // Adult at 30 days

                    // Check for life stage transitions
                    var newLifeStage = CalculateLifeStage(age.AgeInDays, 365f); // Assume 1 year lifespan

                    if (newLifeStage != age.LifeStage)
                    {
                        age.LifeStage = newLifeStage;
                        lifecycle.StageProgress = age.AgeInDays / 365f;

                        // Fire maturation event through event bus if available
                        // Note: This would need to be done in main thread for event bus
                    }

                    // Update evolution capability
                    lifecycle.CanEvolve = age.IsAdult && age.AgeInDays > 100f;

                    // Apply aging effects to health
                    if (age.LifeStage >= LifeStage.Elder)
                    {
                        // Elderly creatures slowly lose health
                        // This would require access to health component
                    }
                    
                }).ScheduleParallel();
        }
        
        private static LifeStage CalculateLifeStage(float ageInDays, float lifeExpectancy)
        {
            float ageRatio = ageInDays / lifeExpectancy;

            if (ageRatio < 0.1f) return LifeStage.Egg;
            if (ageRatio < 0.25f) return LifeStage.Juvenile;
            if (ageRatio < 0.8f) return LifeStage.Adult;

            return LifeStage.Elder;
        }
        
        private static float3 CalculateCurrentSize(float maturationProgress, LifeStage stage)
        {
            float sizeMultiplier = stage switch
            {
                LifeStage.Egg => 0.1f,
                LifeStage.Juvenile => math.lerp(0.5f, 0.8f, maturationProgress),
                LifeStage.Adult => math.lerp(0.8f, 1f, maturationProgress),
                LifeStage.Elder => 0.95f, // Slightly smaller due to age
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
            
            Entities
                .ForEach((ref CreatureNeedsComponent needs, in CreaturePersonalityComponent personality) =>
                {
                    // Decay needs over time using actual component properties
                    needs.Hunger = math.max(0f, needs.Hunger - 0.1f * hourlyDecay);
                    needs.Thirst = math.max(0f, needs.Thirst - 0.15f * hourlyDecay);
                    needs.Energy = math.max(0f, needs.Energy - 0.08f * hourlyDecay);
                    needs.Social = math.max(0f, needs.Social - 0.05f * hourlyDecay);

                    // Calculate overall happiness based on needs satisfaction
                    float needsSatisfaction = (needs.Hunger + needs.Thirst + needs.Energy + needs.Social + needs.Comfort) / 5f;

                    // Happiness trends toward needs satisfaction but changes slowly
                    needs.Happiness = math.lerp(needs.Happiness, needsSatisfaction, 0.1f * deltaTime);

                    // Clamp all values to valid ranges
                    needs.Hunger = math.clamp(needs.Hunger, 0f, 1f);
                    needs.Thirst = math.clamp(needs.Thirst, 0f, 1f);
                    needs.Energy = math.clamp(needs.Energy, 0f, 1f);
                    needs.Social = math.clamp(needs.Social, 0f, 1f);
                    needs.Comfort = math.clamp(needs.Comfort, 0f, 1f);
                    needs.Happiness = math.clamp(needs.Happiness, 0f, 1f);
                    
                }).ScheduleParallel();
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
            
            Entities
                .ForEach((ref CreatureBehaviorComponent behavior,
                         in CreaturePersonalityComponent personality,
                         in CreatureNeedsComponent needs,
                         in CreatureGeneticsComponent genetics) =>
                {
                    // Update behavior based on genetics - simplified without non-existent properties

                    // Determine appropriate behavior based on needs and personality
                    AIState newState = DetermineBehaviorState(behavior, personality, needs, currentTime);

                    if (newState != behavior.currentState)
                    {
                        behavior.currentState = newState;
                        behavior.stateChangeTime = currentTime;
                    }

                    // Update behavior type based on personality
                    behavior.behaviorType = DetermineBehaviorType(personality, genetics);
                    
                }).ScheduleParallel();
        }
        
        private static AIState DetermineBehaviorState(CreatureBehaviorComponent behavior, 
                                                     CreaturePersonalityComponent personality, 
                                                     CreatureNeedsComponent needs, 
                                                     float currentTime)
        {
            // Behavior based on actual component properties
            if (needs.Hunger < 0.3f || needs.Thirst < 0.3f)
            {
                return AIState.Search; // Search for resources
            }

            if (needs.Social < 0.4f && personality.Sociability > 0.6f)
            {
                return AIState.Follow; // Seek companionship
            }

            if (needs.Energy < 0.3f)
            {
                return AIState.Rest; // Need to rest
            }

            if (personality.Curiosity > 0.5f)
            {
                return AIState.Patrol; // Explore
            }

            // Default to idle if needs are met
            return AIState.Idle;
        }
        
        private static int DetermineBehaviorType(CreaturePersonalityComponent personality,
                                                           CreatureGeneticsComponent genetics)
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

            if (personality.Fearfulness > 0.6f)
            {
                return 3; // Passive
            }

            if (personality.Independence > 0.6f)
            {
                return 6; // Guardian
            }

            return 2; // Default neutral behavior
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
            
            Entities
                .ForEach((ref CreatureEnvironmentalComponent environmental,
                         ref CreatureBiomeComponent biome,
                         ref CreatureNeedsComponent needs) =>
                {
                    // Simplified environmental adaptation using actual component properties
                    float adaptationSpeed = 0.01f * deltaTime;

                    // Calculate basic environmental stress
                    environmental.EnvironmentalStress = math.clamp(environmental.EnvironmentalStress, 0f, 1f);

                    // Update biome comfort level
                    biome.BiomeComfort = 1f - environmental.EnvironmentalStress;

                    // Apply environmental effects to creature needs
                    needs.Comfort = math.max(0.2f, biome.BiomeComfort);
                    
                }).ScheduleParallel();
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
            
            Entities
                .ForEach((ref CreatureEnvironmentalComponent environmental) =>
                {
                    // Update basic environmental stress
                    environmental.EnvironmentalStress = math.clamp(environmental.EnvironmentalStress + deltaTime * 0.001f, 0f, 0.3f);
                    
                }).ScheduleParallel();
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
            
            Entities
                .ForEach((ref CreatureBreedingComponent breeding,
                         in CreatureAgeComponent age,
                         in CreatureNeedsComponent needs,
                         in CreaturePersonalityComponent personality) =>
                {
                    // Update breeding readiness using actual component properties
                    breeding.IsReadyToBreed = age.LifeStage >= LifeStage.Adult;

                    // Update breeding based on needs and personality
                    if (breeding.IsReadyToBreed)
                    {
                        float baseDesire = 0.3f;

                        // Happy, well-fed creatures are more interested in breeding
                        float needsBonus = needs.Happiness * 0.3f;
                        float socialBonus = personality.Sociability * 0.2f;

                        // Reduce desire if recently bred
                        float timeSinceLastBreeding = currentTime - breeding.LastBreedTime;
                        float cooldownPenalty = math.max(0f, (86400f - timeSinceLastBreeding) / 86400f) * 0.4f; // 1 day cooldown

                        float totalDesire = math.clamp(baseDesire + needsBonus + socialBonus - cooldownPenalty, 0f, 1f);
                    }

                    // Handle pregnancy progression
                    if (breeding.IsPregnant && breeding.PregnancyProgress < 1f)
                    {
                        breeding.PregnancyProgress += deltaTime / 86400f; // 1 day gestation

                        if (breeding.PregnancyProgress >= 1f)
                        {
                            // Give birth - this would trigger offspring creation
                            breeding.IsPregnant = false;
                            breeding.PregnancyProgress = 0f;
                            breeding.OffspringCount += 1;
                            breeding.LastBreedTime = currentTime;
                        }
                    }
                    
                }).ScheduleParallel();
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
            
            Entities
                .ForEach((ref CreatureBondingComponent bonding,
                         in CreaturePersonalityComponent personality) =>
                {
                    // Bond strength naturally increases over time with loyal creatures
                    if (personality.Loyalty > 0.5f)
                    {
                        float bondGrowthRate = personality.Loyalty * 0.01f * deltaTime;
                        bonding.BondStrength = math.min(1f, bonding.BondStrength + bondGrowthRate);
                    }

                    // Trust increases with positive interactions
                    float timeSinceInteraction = currentTime - bonding.LastInteraction;
                    if (timeSinceInteraction > 3600f) // 1 hour without interaction
                    {
                        // Trust decreases without interaction
                        bonding.BondStrength = math.max(0f, bonding.BondStrength - 0.01f * deltaTime);
                    }

                    // Bonding system completed - needs updates would require separate system
                    
                }).ScheduleParallel();
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
            Entities
                .ForEach((ref CreatureStatsComponent stats,
                         in CreatureGeneticsComponent genetics,
                         in CreatureAgeComponent age,
                         in CreatureNeedsComponent needs) =>
                {
                    // Simplified genetics modifiers
                    float geneticModifier = genetics.GeneticPurity;

                    // Age affects stats
                    float ageModifier = age.LifeStage switch
                    {
                        LifeStage.Egg => 0.1f,
                        LifeStage.Juvenile => 0.7f,
                        LifeStage.Adult => 1.0f,
                        LifeStage.Elder => 0.9f,
                        _ => 1.0f
                    };

                    // Health regeneration based on energy
                    if (needs.Energy > 0.8f && stats.CurrentHealth < stats.BaseStats.health)
                    {
                        float regenRate = geneticModifier * 0.05f;
                        int healthGain = (int)(stats.BaseStats.health * regenRate);
                        stats.CurrentHealth = math.min(stats.BaseStats.health, stats.CurrentHealth + healthGain);
                    }

                }).ScheduleParallel();
        }
    }
    
    #endregion
    
    #region Sync Systems
    
    /// <summary>
    /// Synchronizes ECS data back to GameObjects for visual representation
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class GameObjectSyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Use SystemAPI.Query instead of Entities.ForEach for GameObject access
            foreach (var (link, transform) in SystemAPI.Query<RefRO<GameObjectLinkComponent>, RefRO<LocalTransform>>())
            {
                // Find GameObject by InstanceID and sync position/rotation
                var gameObject = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>()
                    .FirstOrDefault(go => go.GetInstanceID() == link.ValueRO.InstanceID);

                if (gameObject != null && gameObject.activeInHierarchy)
                {
                    gameObject.transform.SetPositionAndRotation(transform.ValueRO.Position, transform.ValueRO.Rotation);
                }
            }
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

            Entities
                .ForEach((ref LocalTransform transform,
                         ref CreatureMovementComponent movement,
                         in CreatureStatsComponent stats,
                         in CreaturePersonalityComponent personality) =>
                {
                    if (!movement.IsMoving || !movement.HasDestination)
                        return;

                    // Calculate movement towards target
                    float3 direction = math.normalize(movement.TargetPosition - transform.Position);
                    float3 moveStep = direction * movement.CurrentSpeed * deltaTime;

                    // Check if we've reached the destination
                    float distanceToTarget = math.distance(transform.Position, movement.TargetPosition);
                    if (distanceToTarget <= 0.1f)
                    {
                        movement.IsMoving = false;
                        movement.HasDestination = false;
                        transform.Position = movement.TargetPosition;
                    }
                    else
                    {
                        transform.Position += moveStep;

                        // Rotate towards movement direction
                        if (math.lengthsq(direction) > 0.001f)
                        {
                            quaternion targetRotation = quaternion.LookRotationSafe(direction, math.up());
                            transform.Rotation = math.slerp(transform.Rotation, targetRotation,
                                                          movement.RotationSpeed * deltaTime);
                        }
                    }

                }).ScheduleParallel();
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
            foreach (var (health, needs, genetics, entity) in SystemAPI.Query<RefRW<CreatureHealthComponent>, RefRO<CreatureNeedsComponent>, RefRO<CreatureGeneticsComponent>>().WithEntityAccess())
            {
                if (!health.ValueRO.IsAlive)
                    continue;

                var healthComp = health.ValueRW;

                // Natural health regeneration when well-fed and rested
                if (needs.ValueRO.Energy > 0.7f && needs.ValueRO.Hunger > 0.6f && healthComp.CurrentHealth < healthComp.MaxHealth)
                {
                    float regenRate = healthComp.RegenerationRate * genetics.ValueRO.GeneticPurity;
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
                if (healthComp.DamageTaken > 0)
                {
                    healthComp.LastDamageTime = currentTime;
                    healthComp.DamageTaken = 0; // Reset damage counter
                }

                health.ValueRW = healthComp;
            }

            ecb.Playback(EntityManager);
        }
    }

    /// <summary>
    /// Processes creature death and cleanup
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

            // Handle dead creatures
            foreach (var (link, entity) in SystemAPI.Query<RefRO<GameObjectLinkComponent>>().WithAll<DeadTag>().WithEntityAccess())
            {
                // Find and deactivate the associated GameObject
                var gameObject = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>()
                    .FirstOrDefault(go => go.GetInstanceID() == link.ValueRO.InstanceID);

                if (gameObject != null)
                {
                    gameObject.SetActive(false);
                }

                // Destroy the entity after a delay (could add corpse mechanics here)
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(EntityManager);
        }
    }

    #endregion
}