using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// ECS Health Component - properly implementing IComponentData
    /// </summary>
    public struct ECSHealthComponent : IComponentData
    {
        public float CurrentHealth;
        public float MaxHealth;
        public float HealthRegenRate;
        public float LastDamageTime;
        public bool IsAlive;
        public bool IsInvulnerable;
        public float InvulnerabilityEndTime;
        
        public readonly float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
        
        public static ECSHealthComponent Create(float maxHealth)
        {
            return new ECSHealthComponent
            {
                CurrentHealth = maxHealth,
                MaxHealth = maxHealth,
                HealthRegenRate = 0f,
                LastDamageTime = 0f,
                IsAlive = true,
                IsInvulnerable = false,
                InvulnerabilityEndTime = 0f
            };
        }
        
        /// <summary>
        /// Takes damage and updates health state (creates a new instance)
        /// </summary>
        public readonly ECSHealthComponent TakeDamage(float damageAmount)
        {
            if (IsInvulnerable || !IsAlive) return this;
            
            var newHealth = math.max(0f, CurrentHealth - damageAmount);
            var newIsAlive = newHealth > 0f;
            
            return new ECSHealthComponent
            {
                CurrentHealth = newHealth,
                MaxHealth = MaxHealth,
                HealthRegenRate = HealthRegenRate,
                LastDamageTime = UnityEngine.Time.time,
                IsAlive = newIsAlive,
                IsInvulnerable = IsInvulnerable,
                InvulnerabilityEndTime = InvulnerabilityEndTime
            };
        }
        
        /// <summary>
        /// Heals the entity by the specified amount (creates a new instance)
        /// </summary>
        public readonly ECSHealthComponent Heal(float healAmount)
        {
            if (!IsAlive) return this;
            
            var newHealth = math.min(MaxHealth, CurrentHealth + healAmount);
            
            return new ECSHealthComponent
            {
                CurrentHealth = newHealth,
                MaxHealth = MaxHealth,
                HealthRegenRate = HealthRegenRate,
                LastDamageTime = LastDamageTime,
                IsAlive = IsAlive,
                IsInvulnerable = IsInvulnerable,
                InvulnerabilityEndTime = InvulnerabilityEndTime
            };
        }
    }
    
    // PlayerInputComponent is defined in separate file - removing duplicate
    
    // PlayerStateComponent is defined in separate file - removing duplicate
    
    /// <summary>
    /// Respawn timer component
    /// </summary>
    public struct RespawnTimer : IComponentData
    {
        public float TimeRemaining;
        public float TotalTime;
        public bool IsActive;
        public float3 RespawnPosition;
        public quaternion RespawnRotation;
        
        public readonly float Progress => TotalTime > 0 ? (TotalTime - TimeRemaining) / TotalTime : 1f;
    }
    
    /// <summary>
    /// Dead tag component
    /// </summary>
    public struct DeadTag : IComponentData
    {
        public float DeathTime;
        public Entity Killer;
        public float3 DeathPosition;
    }
    
    /// <summary>
    /// Player tag component
    /// </summary>
    public struct PlayerTag : IComponentData
    {
        public int PlayerID;
        public bool IsLocalPlayer;
    }
    
    /// <summary>
    /// Damage event for ECS systems
    /// </summary>
    public struct DamageEvent : IComponentData
    {
        public float Amount;
        public DamageType Type;
        public Entity Source;
        public Entity Target;
        public float3 Position;
        public float3 Direction;
        public float Timestamp;
        public bool IsCritical;
        
        // Legacy properties for compatibility
        public float DamageAmount => Amount;
        public float3 HitDirection => Direction;
        public ulong TargetClientId;
        public ulong AttackerClientId;
    }
    
    /// <summary>
    /// Damage taken event
    /// </summary>
    public struct DamageTakenEvent : IComponentData
    {
        public float DamageAmount;
        public DamageType DamageType;
        public Entity DamageSource;
        public float3 DamagePosition;
        public float DamageTime;
        public bool IsCritical;
        public float3 DamageDirection;
    }
    
    /// <summary>
    /// Buffer element for storing damage taken events
    /// </summary>
    public struct DamageTakenEventBufferElement : IBufferElementData
    {
        public DamageTakenEvent DamageEvent;
        public bool IsProcessed;
        
        // Compatibility property
        public DamageTakenEvent Value => DamageEvent;
    }
    
    /// <summary>
    /// Death animation trigger component
    /// </summary>
    public struct DeathAnimationTrigger : IComponentData
    {
        public float TriggerTime;
        public int AnimationType; // 0 = normal, 1 = explosion, 2 = dissolve, etc.
        public bool HasTriggered;
        public float Duration;
        public Entity DeathCause;
        
        // Compatibility property
        public bool Triggered
        {
            get => HasTriggered;
            set => HasTriggered = value;
        }
        
        public static DeathAnimationTrigger Create(int animationType = 0, float duration = 2.0f)
        {
            return new DeathAnimationTrigger
            {
                TriggerTime = UnityEngine.Time.time,
                AnimationType = animationType,
                HasTriggered = false,
                Duration = duration,
                DeathCause = Entity.Null
            };
        }
    }
}
