using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Simple movement component to replace PhysicsVelocity dependency.
    /// Contains velocity data for entity movement.
    /// </summary>
    public struct MovementComponent : IComponentData
    {
        /// <summary>Linear velocity in world space.</summary>
        public float3 Velocity;
        
        /// <summary>Angular velocity for rotation.</summary>
        public float3 AngularVelocity;
        
        /// <summary>Movement speed multiplier.</summary>
        public float SpeedMultiplier;
        
        /// <summary>Whether movement is currently enabled.</summary>
        public bool IsEnabled;
    }
}
