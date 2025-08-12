using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.Mathematics;

namespace Models.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial struct PhysicsMovementSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            // Get the singleton PhysicsWorldSingleton for physics queries
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            float deltaTime = SystemAPI.Time.DeltaTime;

            Entities
                .WithAll<PlayerTag>() // Adjust tag/component filter as needed
                .ForEach((ref Translation translation, in PhysicsVelocity velocity) =>
                {
                    // Simple example: move entity by velocity integrated over deltaTime
                    translation.Value += velocity.Linear * deltaTime;

                    // TODO: Add your custom physics or DOTS movement logic here
                }).Schedule();
        }
    }
}
