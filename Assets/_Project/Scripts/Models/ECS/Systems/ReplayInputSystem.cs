using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Infrastructure;
// FIXME: tidyup after 8/29
namespace Models.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ReplayInputSystem : SystemBase
    {
        private ReplayRecorder _replayRecorder = null!;
        private bool _isReplaying = false;

        protected override void OnCreate()
        {
            base.OnCreate();

            // Resolve ReplayRecorder instance from ServiceLocator or create new
            _replayRecorder = Infrastructure.ServiceLocator.Instance.Resolve<ReplayRecorder>();

            // You might want a way to start/stop replay mode from GameStateManager or input
            _isReplaying = false;
        }

        protected override void OnUpdate()
        {
            if (!_isReplaying)
                return;

            _replayRecorder.Update();

            var (movement, jump, attack) = _replayRecorder.GetCurrentReplayInput();

            float3 moveDirection = new float3(movement.x, 0, movement.y);

            Entities
                .WithAll<PlayerTag>() // Assume you tag your player entities
                .ForEach((ref PhysicsVelocity velocity, in LocalTransform transform) =>
                {
                    // Example: apply movement direction to velocity
                    velocity.Linear = moveDirection * 5f; // Adjust speed multiplier as needed

                    // Here you could also set flags/components for jump or attack
                    // For example, add a JumpRequest component or call a method
                }).ScheduleParallel();
        }

        /// <summary>
        /// Call this method externally to start replay mode.
        /// </summary>
        public void StartReplay()
        {
            _isReplaying = true;
            _replayRecorder.StartReplay();
        }

        /// <summary>
        /// Call this method externally to stop replay mode.
        /// </summary>
        public void StopReplay()
        {
            _isReplaying = false;
            _replayRecorder.StopReplay();
        }
    }
}
