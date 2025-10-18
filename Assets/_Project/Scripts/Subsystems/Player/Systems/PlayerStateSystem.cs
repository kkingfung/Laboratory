using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Laboratory.Infrastructure.Networking;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Job that processes player state updates in parallel.
    /// </summary>

    public struct PlayerStateJob : IJobChunk
    {
        public float DeltaTime;
        public float StaminaRegenRate;
        
        public ComponentTypeHandle<PlayerStateComponent> PlayerStateHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var stateArray = chunk.GetNativeArray(ref PlayerStateHandle);
            
            for (int i = 0; i < chunk.Count; i++)
            {
                var state = stateArray[i];
                
                // Skip processing for dead players
                if (!state.IsAlive)
                    continue;

                // Regenerate stamina up to maximum capacity
                state.Stamina += StaminaRegenRate * DeltaTime;
                
                if (state.Stamina > state.MaxStamina)
                {
                    state.Stamina = state.MaxStamina;
                }

                // Check for death condition and handle state transition
                if (state.CurrentHP <= 0)
                {
                    state.IsAlive = false;
                    state.CurrentHP = 0;
                }
                
                stateArray[i] = state;
            }
        }
    }

    /// <summary>
    /// Manages player state updates including stamina regeneration, death detection, and status effects.
    /// Runs during simulation group and processes all living players for state changes.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PlayerStateSystem : SystemBase
    {
        #region Constants

        /// <summary>
        /// Rate at which stamina regenerates per second when not being consumed.
        /// </summary>
        private const float StaminaRegenRate = 5f;

        #endregion

        #region Fields

        private EntityQuery m_PlayerQuery;
        private ComponentTypeHandle<PlayerStateComponent> m_PlayerStateHandle;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize the system and create entity queries.
        /// </summary>
        protected override void OnCreate()
        {
            // Create a query for entities with PlayerStateComponent
            m_PlayerQuery = GetEntityQuery(ComponentType.ReadWrite<PlayerStateComponent>());
            
            // Get component type handle
            m_PlayerStateHandle = GetComponentTypeHandle<PlayerStateComponent>();
        }

        /// <summary>
        /// Updates player states each frame, handling stamina regeneration and death detection.
        /// </summary>
        protected override void OnUpdate()
        {
            // Update the component type handle
            m_PlayerStateHandle.Update(this);
            
            var job = new PlayerStateJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                StaminaRegenRate = StaminaRegenRate,
                PlayerStateHandle = m_PlayerStateHandle
            };

            // Schedule the job
            Dependency = job.ScheduleParallel(m_PlayerQuery, Dependency);
        }

        #endregion
    }
}
