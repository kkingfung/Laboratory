using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Handles combat interactions between entities.
    /// Processes attack inputs and applies damage to targets.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CombatSystem : SystemBase
    {
        #region Constants

        private const int AttackDamage = 10;

        #endregion
        
        #region Event Handling
        
        /// <summary>
        /// Raises a death event for the specified entity.
        /// </summary>
        /// <param name="entity">The entity that died</param>
        /// <param name="state">The final state of the entity</param>
        private void RaiseDeathEvent(Entity entity, PlayerStateComponent state)
        {
            try
            {
                // Create a death event component that other systems can process
                if (!EntityManager.HasComponent<PlayerDeathComponent>(entity))
                {
                    EntityManager.AddComponent<PlayerDeathComponent>(entity);
                    EntityManager.SetComponentData(entity, new PlayerDeathComponent
                    {
                        DeathTime = UnityEngine.Time.time,
                        FinalHP = state.CurrentHP
                    });
                }
                
                // Log the death event
                UnityEngine.Debug.Log($"Death event raised for entity {entity} at time {UnityEngine.Time.time}");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error raising death event for entity {entity}: {ex.Message}");
            }
        }
        
        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Processes combat actions for all entities with player input and state components.
        /// </summary>
        protected override void OnUpdate()
        {
            foreach (var (playerState, playerInput, entity) in 
                     SystemAPI.Query<RefRW<PlayerStateComponent>, RefRO<PlayerInputComponent>>()
                     .WithAll<PlayerInputComponent, PlayerStateComponent>()
                     .WithEntityAccess())
            {
                ProcessCombatInput(ref playerState.ValueRW, in playerInput.ValueRO, entity);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes combat-related input for a single entity.
        /// </summary>
        /// <param name="state">The player's current state</param>
        /// <param name="input">The player's input this frame</param>
        /// <param name="entity">The entity performing the combat action</param>
        private void ProcessCombatInput(ref PlayerStateComponent state, in PlayerInputComponent input, Entity entity)
        {
            if (!state.IsAlive) 
                return;

            if (input.AttackPressed)
            {
                // Apply damage to targets instead of self
                ApplyDamageToTargets(ref state, entity);
                
                // Check if the attacker somehow died (e.g., from reflected damage)
                CheckForDeath(ref state, entity);
            }
        }

        /// <summary>
        /// Applies damage to nearby enemy entities using proper target selection.
        /// </summary>
        /// <param name="attackerState">The state of the attacking entity</param>
        /// <param name="attackerEntity">The attacking entity</param>
        private void ApplyDamageToTargets(ref PlayerStateComponent attackerState, Entity attackerEntity)
        {
            // Get the attacker's transform for position-based targeting
            if (!SystemAPI.HasComponent<Unity.Transforms.LocalTransform>(attackerEntity))
                return;
                
            var attackerTransform = SystemAPI.GetComponent<Unity.Transforms.LocalTransform>(attackerEntity);
            float3 attackerPosition = attackerTransform.Position;
            
            // Define attack range and parameters
            const float attackRange = 3f;
            const float attackAngle = 90f; // 90-degree cone in front of attacker
            
            // Find and damage all valid targets within range
            foreach (var (targetState, targetTransform, targetEntity) in 
                     SystemAPI.Query<RefRW<PlayerStateComponent>, RefRO<Unity.Transforms.LocalTransform>>()
                     .WithEntityAccess()
                     .WithNone<PlayerTag>()) // Exclude player entities from being targets
            {
                if (!targetState.ValueRO.IsAlive) continue;
                if (targetEntity == attackerEntity) continue; // Don't attack self
                
                float3 targetPosition = targetTransform.ValueRO.Position;
                float distance = math.distance(attackerPosition, targetPosition);
                
                // Check if target is within attack range
                if (distance <= attackRange)
                {
                    // Check if target is within attack angle (cone in front of attacker)
                    float3 toTarget = math.normalize(targetPosition - attackerPosition);
                    float3 attackerForward = math.forward(attackerTransform.Rotation);
                    float dotProduct = math.dot(attackerForward, toTarget);
                    float angleToTarget = math.degrees(math.acos(math.clamp(dotProduct, -1f, 1f)));
                    
                    if (angleToTarget <= attackAngle * 0.5f)
                    {
                        // Apply damage to the target
                        ref var targetStateRW = ref targetState.ValueRW;
                        targetStateRW.CurrentHP -= AttackDamage;
                        
                        UnityEngine.Debug.Log($"Applied {AttackDamage} damage to target. Target HP: {targetStateRW.CurrentHP}");
                        
                        // Check if target died from this attack
                        CheckForDeath(ref targetStateRW, targetEntity);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the entity should die based on current health and handles death events.
        /// </summary>
        /// <param name="state">The state to check</param>
        /// <param name="entity">The entity to check for death</param>
        private void CheckForDeath(ref PlayerStateComponent state, Entity entity)
        {
            if (state.CurrentHP <= 0 && state.IsAlive)
            {
                state.IsAlive = false;
                state.CurrentHP = 0;
                
                // Add death tag component to mark entity as dead
                EntityManager.AddComponent<DeadTag>(entity);
                
                // Add death animation trigger if not already present
                if (!EntityManager.HasComponent<DeathAnimationTrigger>(entity))
                {
                    EntityManager.AddComponent<DeathAnimationTrigger>(entity);
                    EntityManager.SetComponentData(entity, new DeathAnimationTrigger { Triggered = false });
                }
                
                // Raise death event through event system
                RaiseDeathEvent(entity, state);
                
                UnityEngine.Debug.Log($"Entity {entity} has died");
            }
        }

        #endregion
    }
}
