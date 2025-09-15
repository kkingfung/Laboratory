using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Player state component containing player status information
    /// </summary>
    public struct PlayerStateComponent : IComponentData
    {
        public float3 Position;
        public quaternion Rotation;
        public float3 Velocity;
        public float CurrentHP;
        public float MaxHP;
        public float Stamina;
        public float MaxStamina;
        public uint StatusFlags;
        public float LastUpdateTime;
        
        // Legacy compatibility properties
        public float Health
        {
            get => CurrentHP;
            set => CurrentHP = value;
        }
        
        public float MaxHealth
        {
            get => MaxHP;
            set => MaxHP = value;
        }
        
        /// <summary>
        /// Gets or sets whether the player is alive
        /// </summary>
        public bool IsAlive 
        { 
            get => CurrentHP > 0f; 
            set 
            {
                if (value)
                {
                    if (CurrentHP <= 0f) CurrentHP = 1f; // Ensure alive state
                }
                else
                {
                    CurrentHP = 0f; // Set to dead
                }
            }
        }
        
        public static PlayerStateComponent CreateDefault()
        {
            return new PlayerStateComponent
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Velocity = float3.zero,
                CurrentHP = 100f,
                MaxHP = 100f,
                Stamina = 100f,
                MaxStamina = 100f,
                StatusFlags = 0,
                LastUpdateTime = UnityEngine.Time.time
            };
        }
    }
}
