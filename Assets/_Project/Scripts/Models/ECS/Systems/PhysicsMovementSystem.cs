using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.Mathematics;

namespace Models.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BuildPhysicsWorld))]
    [UpdateBefore(typeof(StepPhysicsWorld))]
    public partial class PhysicsMovementSystem : SystemBase
    {
        // Movement speed in units per second
        private const float MoveSpeed = 5f;

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            Entities
                .WithAll<PlayerInputComponent>()
                .ForEach((ref PhysicsVelocity physicsVelocity, in PlayerInputComponent input) =>
                {
                    // Calculate desired velocity on XZ plane from input
                    float3 desiredVelocity = new float3(input.MoveDirection.x, 0, input.MoveDirection.y) * MoveSpeed;

                    // Apply desired linear velocity, keep existing vertical velocity (gravity/jump)
                    physicsVelocity.Linear.x = desiredVelocity.x;
                    physicsVelocity.Linear.z = desiredVelocity.z;

                }).ScheduleParallel();
        }
    }
}
