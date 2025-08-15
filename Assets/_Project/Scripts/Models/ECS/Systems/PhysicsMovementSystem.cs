using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Core;
using Laboratory.Infrastructure.AsyncUtils;
using Laboratory.Models.ECS.Components;

namespace Laboratory.ECS.Systems
{
    /// <summary>
    /// System responsible for handling physics-based movement calculations for entities.
    /// This system integrates physics velocity with entity positions and provides custom
    /// physics movement logic that runs before the Unity Physics simulation group.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSimulationGroup))]
    public partial struct PhysicsMovementSystem : ISystem
    {
        #region Constants
        
        /// <summary>
        /// Maximum allowed velocity magnitude to prevent physics instability
        /// </summary>
        private const float MaxVelocityMagnitude = 100f;
        
        /// <summary>
        /// Minimum delta time threshold to prevent division by zero and ensure stable physics
        /// </summary>
        private const float MinDeltaTime = 0.0001f;
        
        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called when the system is created. Initializes any required system state.
        /// </summary>
        /// <param name="state">The system state reference</param>
        public void OnCreate(ref SystemState state)
        {
            // System initialization if needed
            // Currently no initialization required for this system
        }

        /// <summary>
        /// Called every frame during simulation. Updates entity positions based on physics velocity
        /// and applies custom movement logic before physics simulation runs.
        /// </summary>
        /// <param name="state">The system state reference</param>
        public void OnUpdate(ref SystemState state)
        {
            try
            {
                ProcessPhysicsMovement(ref state);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error in PhysicsMovementSystem.OnUpdate: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when the system is destroyed. Cleans up any system resources.
        /// </summary>
        /// <param name="state">The system state reference</param>
        public void OnDestroy(ref SystemState state)
        {
            // Cleanup logic if needed
            // Currently no cleanup required for this system
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes physics movement for all relevant entities with player tags and physics components
        /// </summary>
        /// <param name="state">The system state reference</param>
        private void ProcessPhysicsMovement(ref SystemState state)
        {
            // Validate physics world availability
            if (!SystemAPI.TryGetSingleton<PhysicsWorldSingleton>(out var physicsWorld))
            {
                Debug.LogWarning("PhysicsWorldSingleton not available, skipping physics movement update");
                return;
            }

            float deltaTime = ValidateDeltaTime(SystemAPI.Time.DeltaTime);
            if (deltaTime <= 0)
            {
                return; // Skip update if delta time is invalid
            }

            // Process entities with player tags and physics components
            var movementJob = new PhysicsMovementJob
            {
                DeltaTime = deltaTime,
                MaxVelocity = MaxVelocityMagnitude
            };

            // Use WithAll to filter entities that have PlayerTag component
            state.Dependency = movementJob.WithAll<PlayerTag>().ScheduleParallel(state.Dependency);
        }

        /// <summary>
        /// Validates and clamps delta time to ensure stable physics calculations
        /// </summary>
        /// <param name="deltaTime">The raw delta time value</param>
        /// <returns>Validated delta time value</returns>
        private float ValidateDeltaTime(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < MinDeltaTime)
            {
                if (deltaTime != 0) // Don't log warning for zero delta time (common on first frame)
                {
                    Debug.LogWarning($"Invalid delta time detected: {deltaTime}, using minimum value");
                }
                return MinDeltaTime;
            }

            // Clamp extremely large delta times to prevent physics instability
            const float maxDeltaTime = 0.1f; // 10 FPS minimum
            if (deltaTime > maxDeltaTime)
            {
                Debug.LogWarning($"Large delta time detected: {deltaTime}, clamping to {maxDeltaTime}");
                return maxDeltaTime;
            }

            return deltaTime;
        }

        #endregion

        #region Jobs

        /// <summary>
        /// Burst-compiled job for processing physics movement calculations in parallel
        /// </summary>
        [Unity.Burst.BurstCompile]
        private partial struct PhysicsMovementJob : IJobEntity
        {
            /// <summary>
            /// Delta time for this frame's movement calculations
            /// </summary>
            public float DeltaTime;
            
            /// <summary>
            /// Maximum allowed velocity magnitude for stability
            /// </summary>
            public float MaxVelocity;

            /// <summary>
            /// Executes movement logic for a single entity with player tag and physics components
            /// </summary>
            /// <param name="transform">The entity's transform component (position/rotation)</param>
            /// <param name="velocity">The entity's physics velocity component</param>
            public void Execute(ref LocalTransform transform, in PhysicsVelocity velocity)
            {
                // Validate velocity to prevent physics instability
                var clampedVelocity = ValidateVelocity(velocity.Linear);
                
                // Apply physics-based movement by integrating velocity over time
                var deltaPosition = clampedVelocity * DeltaTime;
                transform.Position += deltaPosition;

                // Optional: Add custom movement logic here
                // Examples: gravity application, ground clamping, collision response, etc.
                ApplyCustomMovementLogic(ref transform, clampedVelocity);
            }

            /// <summary>
            /// Validates and clamps velocity values to prevent physics instability
            /// </summary>
            /// <param name="velocity">The raw velocity vector</param>
            /// <returns>Validated and clamped velocity vector</returns>
            private float3 ValidateVelocity(float3 velocity)
            {
                // Check for invalid values
                if (math.any(math.isnan(velocity)) || math.any(math.isinf(velocity)))
                {
                    return float3.zero; // Reset invalid velocities
                }

                // Clamp velocity magnitude to prevent excessive speeds
                float velocityMagnitude = math.length(velocity);
                if (velocityMagnitude > MaxVelocity)
                {
                    return math.normalize(velocity) * MaxVelocity;
                }

                return velocity;
            }

            /// <summary>
            /// Applies custom movement logic specific to the game's requirements
            /// </summary>
            /// <param name="transform">The entity's transform component</param>
            /// <param name="velocity">The validated velocity vector</param>
            private void ApplyCustomMovementLogic(ref LocalTransform transform, float3 velocity)
            {
                // TODO: Add custom physics or movement logic here
                // Examples:
                // - Ground clamping: keep entities on ground surface
                // - Gravity application: apply downward force
                // - Movement constraints: limit movement to certain areas
                // - Animation blending: update animation states based on movement
                
                // Example: Simple ground clamping (optional)
                // const float groundLevel = 0f;
                // if (transform.Position.y < groundLevel)
                // {
                //     var position = transform.Position;
                //     position.y = groundLevel;
                //     transform.Position = position;
                // }
            }
        }

        #endregion
    }
}
