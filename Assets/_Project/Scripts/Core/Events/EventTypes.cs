using System;
using UnityEngine;

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Simple entity ID for non-ECS systems
    /// </summary>
    public struct EntityId
    {
        public int Value;
        public static EntityId Null => new EntityId { Value = -1 };
        
        public EntityId(int value)
        {
            Value = value;
        }
        
        public static implicit operator EntityId(int value)
        {
            return new EntityId(value);
        }
    }
    
    /// <summary>
    /// Event arguments for health changes
    /// </summary>
    public class HealthChangedEventArgs : EventArgs
    {
        public float OldHealth { get; }
        public float NewHealth { get; }
        public float MaxHealth { get; }
        public GameObject Source { get; }
        public float Timestamp { get; }

        public HealthChangedEventArgs(float oldHealth, float newHealth, float maxHealth, GameObject source = null)
        {
            OldHealth = oldHealth;
            NewHealth = newHealth;
            MaxHealth = maxHealth;
            Source = source;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Event arguments for death events
    /// </summary>
    public class DeathEventArgs : EventArgs
    {
        public GameObject DeadEntity { get; }
        public GameObject Killer { get; }
        public Vector3 DeathPosition { get; }
        public float Timestamp { get; }

        public DeathEventArgs(GameObject deadEntity, GameObject killer = null, Vector3 deathPosition = default)
        {
            DeadEntity = deadEntity;
            Killer = killer;
            DeathPosition = deathPosition;
            Timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// Event arguments for damage taken
    /// </summary>
    public class DamageTakenEvent : EventArgs
    {
        public float DamageAmount { get; }
        public EntityId TargetEntityId { get; }
        public Vector3 SourcePosition { get; }
        public GameObject Source { get; }
        public GameObject Target { get; }
        public float Timestamp { get; }

        public DamageTakenEvent(float damage, EntityId targetEntity, Vector3 sourcePos, GameObject source = null, GameObject target = null)
        {
            DamageAmount = damage;
            TargetEntityId = targetEntity;
            SourcePosition = sourcePos;
            Source = source;
            Target = target;
            Timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// Event for player respawn (changed to class to satisfy reference type constraint)
    /// </summary>
    public class PlayerRespawnedEvent
    {
        public EntityId PlayerEntity { get; }
        public Vector3 RespawnPosition { get; }
        public float RespawnTime { get; }
        
        public PlayerRespawnedEvent(EntityId entity, Vector3 position)
        {
            PlayerEntity = entity;
            RespawnPosition = position;
            RespawnTime = UnityEngine.Time.time;
        }
    }
    
    /// <summary>
    /// Chat message event for networking
    /// </summary>
    public class ChatMessageEvent
    {
        public string PlayerName { get; }
        public string Message { get; }
        public float Timestamp { get; }

        public ChatMessageEvent(string playerName, string message)
        {
            PlayerName = playerName ?? "Unknown";
            Message = message ?? "";
            Timestamp = UnityEngine.Time.time;
        }
    }
    
    /// <summary>
    /// Damage event for networking and ECS (renamed to avoid conflict with Messages.DamageEvent)
    /// </summary>
    public struct NetworkDamageEvent
    {
        public float Amount;
        public EntityId Source;
        public EntityId Target;
        public Vector3 Position;
        public int DamageType;
        public ulong targetClientId;

        public NetworkDamageEvent(float amount, EntityId source, EntityId target, Vector3 position, int damageType = 0)
        {
            Amount = amount;
            Source = source;
            Target = target;
            Position = position;
            DamageType = damageType;
            targetClientId = 0;
        }
    }
}
