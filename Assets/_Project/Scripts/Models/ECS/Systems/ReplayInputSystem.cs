using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Laboratory.Infrastructure;
using Laboratory.Models.ECS.Components;
using Laboratory.Tools;
using Laboratory.Core;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Handles replay input playback for recorded player movements and actions.
    /// Processes replay data and applies it to player entities during replay mode.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ReplayInputSystem : SystemBase
    {
        #region Constants

        /// <summary>
        /// Movement speed multiplier applied to replayed movement inputs.
        /// </summary>
        private const float MovementSpeedMultiplier = 5f;
        
        #endregion

        #region Fields

        /// <summary>
        /// Reference to the replay recorder that manages replay data and playback state.
        /// </summary>
        private ReplayRecorder _replayRecorder = null!;

        /// <summary>
        /// Flag indicating whether the system is currently in replay mode.
        /// </summary>
        private bool _isReplaying = false;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initializes the replay system and resolves dependencies from the service locator.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();

            // Resolve ReplayRecorder instance from ServiceLocator
            _replayRecorder = ServiceLocator.Instance.Resolve<ReplayRecorder>();

            // Initialize in non-replaying state
            _isReplaying = false;
        }

        /// <summary>
        /// Updates replay input processing each frame when in replay mode.
        /// Applies recorded movement, jump, and attack inputs to player entities.
        /// </summary>
        protected override void OnUpdate()
        {
            if (!_isReplaying)
                return;

            // Update replay recorder state
            _replayRecorder.Update();

            // Get current frame's input data from replay
            var (movement, jump, attack) = _replayRecorder.GetCurrentReplayInput();

            // Convert 2D movement to 3D direction vector
            float3 moveDirection = new float3(movement.x, 0, movement.y);

            // Apply replay input to all player entities
            ProcessReplayInputForPlayers(moveDirection, jump, attack);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initiates replay mode and begins playback of recorded input data.
        /// </summary>
        public void StartReplay()
        {
            _isReplaying = true;
            _replayRecorder.StartReplay();
        }

        /// <summary>
        /// Stops replay mode and halts playback of recorded input data.
        /// </summary>
        public void StopReplay()
        {
            _isReplaying = false;
            _replayRecorder.StopReplay();
        }

        /// <summary>
        /// Gets the current replay state of the system.
        /// </summary>
        /// <returns>True if currently replaying, false otherwise</returns>
        public bool IsReplaying => _isReplaying;

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes replay input for all player entities, applying movement and actions.
        /// </summary>
        /// <param name="moveDirection">3D movement direction vector from replay data</param>
        /// <param name="jump">Jump input state from replay data</param>
        /// <param name="attack">Attack input state from replay data</param>
        private void ProcessReplayInputForPlayers(float3 moveDirection, bool jump, bool attack)
        {
            Entities
                .WithAll<PlayerTag>() // Process only entities tagged as players
                .ForEach((ref PhysicsVelocity velocity, in LocalTransform transform) =>
                {
                    // Apply movement direction to physics velocity
                    velocity.Linear = moveDirection * MovementSpeedMultiplier;

                    // TODO: Handle jump and attack inputs
                    // Consider adding components or calling methods for:
                    // - Jump: Add JumpRequest component or set jump velocity
                    // - Attack: Add AttackRequest component or trigger attack sequence
                    
                }).ScheduleParallel();
        }

        #endregion
    }
}
