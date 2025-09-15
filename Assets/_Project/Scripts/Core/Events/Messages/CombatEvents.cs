#nullable enable
using UnityEngine;
using Laboratory.Core.Health;

namespace Laboratory.Core.Events.Messages
{
    #region Combat Events
    
    /// <summary>
    /// Event fired when an entity's health changes.
    /// </summary>
    public class HealthChangedEvent
    {
        /// <summary>The GameObject whose health changed.</summary>
        public GameObject Target { get; }
        
        /// <summary>Previous health value.</summary>
        public int OldHealth { get; }
        
        /// <summary>New health value.</summary>
        public int NewHealth { get; }
        
        /// <summary>Maximum health value.</summary>
        public int MaxHealth { get; }
        
        /// <summary>The source of the health change (can be null).</summary>
        public GameObject? Source { get; }
        
        /// <summary>Network client ID of the target (0 if not networked).</summary>
        public ulong TargetClientId { get; }

        public HealthChangedEvent(GameObject target, int oldHealth, int newHealth, int maxHealth, 
            GameObject? source = null, ulong targetClientId = 0)
        {
            Target = target;
            OldHealth = oldHealth;
            NewHealth = newHealth;
            MaxHealth = maxHealth;
            Source = source;
            TargetClientId = targetClientId;
        }
    }

    /// <summary>
    /// Unified damage event that replaces fragmented damage events across the system.
    /// Contains comprehensive damage information for consistent damage handling.
    /// </summary>
    public class DamageEvent
    {
        /// <summary>The GameObject that received damage.</summary>
        public GameObject Target { get; }
        
        /// <summary>The GameObject that dealt the damage (can be null for environmental damage).</summary>
        public GameObject? Source { get; }
        
        /// <summary>Amount of damage dealt.</summary>
        public float Amount { get; }
        
        /// <summary>Type of damage (Normal, Fire, Ice, etc.).</summary>
        public DamageType Type { get; }
        
        /// <summary>Direction of the damage (used for knockback, hit effects).</summary>
        public Vector3 Direction { get; }
        
        /// <summary>Network client ID of the target (0 if not networked).</summary>
        public ulong TargetClientId { get; }
        
        /// <summary>Network client ID of the attacker (0 if not networked).</summary>
        public ulong AttackerClientId { get; }

        public DamageEvent(GameObject target, GameObject? source, float amount, 
            DamageType type, Vector3 direction, 
            ulong targetClientId = 0, ulong attackerClientId = 0)
        {
            Target = target;
            Source = source;
            Amount = amount;
            Type = type;
            Direction = direction;
            TargetClientId = targetClientId;
            AttackerClientId = attackerClientId;
        }
    }

    /// <summary>
    /// Unified death event that replaces fragmented death events across the system.
    /// Contains comprehensive death information for consistent death handling.
    /// </summary>
    public class DeathEvent
    {
        /// <summary>The GameObject that died.</summary>
        public GameObject Target { get; }
        
        /// <summary>The GameObject that caused the death (can be null).</summary>
        public GameObject? Source { get; }
        
        /// <summary>Network client ID of the victim (0 if not networked).</summary>
        public ulong VictimClientId { get; }
        
        /// <summary>Network client ID of the killer (0 if not networked).</summary>
        public ulong KillerClientId { get; }

        public DeathEvent(GameObject target, GameObject? source = null,
            ulong victimClientId = 0, ulong killerClientId = 0)
        {
            Target = target;
            Source = source;
            VictimClientId = victimClientId;
            KillerClientId = killerClientId;
        }
    }
    
    /// <summary>
    /// Event fired when an entity is healed.
    /// </summary>
    public class HealingEvent
    {
        /// <summary>The GameObject that was healed.</summary>
        public GameObject Target { get; }
        
        /// <summary>The GameObject that provided the healing (can be null).</summary>
        public GameObject? Source { get; }
        
        /// <summary>Amount of healing applied.</summary>
        public float Amount { get; }
        
        /// <summary>Type of healing (instant, over time, etc.).</summary>
        public HealingType Type { get; }

        public HealingEvent(GameObject target, GameObject? source, float amount, HealingType type)
        {
            Target = target;
            Source = source;
            Amount = amount;
            Type = type;
        }
    }
    
    /// <summary>
    /// Types of healing effects.
    /// </summary>
    public enum HealingType
    {
        /// <summary>Instant healing.</summary>
        Instant,
        /// <summary>Healing over time effect.</summary>
        OverTime,
        /// <summary>Regeneration effect.</summary>
        Regeneration
    }
    
    /// <summary>
    /// Event fired when a damage indicator should be displayed.
    /// Used to decouple combat systems from UI systems.
    /// </summary>
    public class DamageIndicatorRequestedEvent
    {
        /// <summary>World position where the damage originated from.</summary>
        public Vector3 SourcePosition { get; }
        
        /// <summary>Amount of damage to display (can be null to hide amount).</summary>
        public int? DamageAmount { get; }
        
        /// <summary>Type of damage for visual styling.</summary>
        public DamageType DamageType { get; }
        
        /// <summary>Whether to play sound effects.</summary>
        public bool PlaySound { get; }
        
        /// <summary>Whether to trigger haptic feedback/vibration.</summary>
        public bool TriggerVibration { get; }
        
        /// <summary>Network client ID of the target (for filtering local events).</summary>
        public ulong TargetClientId { get; }

        public DamageIndicatorRequestedEvent(Vector3 sourcePosition, int? damageAmount, 
            DamageType damageType, bool playSound = true, bool triggerVibration = false, 
            ulong targetClientId = 0)
        {
            SourcePosition = sourcePosition;
            DamageAmount = damageAmount;
            DamageType = damageType;
            PlaySound = playSound;
            TriggerVibration = triggerVibration;
            TargetClientId = targetClientId;
        }
    }
    
    /// <summary>
    /// Event fired when a player spawns or respawns.
    /// Used to notify systems about player spawn events.
    /// </summary>
    public class PlayerSpawnedEvent
    {
        /// <summary>ID of the player that spawned.</summary>
        public int PlayerId { get; }
        
        /// <summary>ID of the spawn point used.</summary>
        public int SpawnPointId { get; }
        
        /// <summary>World position where the player spawned.</summary>
        public Vector3 Position { get; }
        
        /// <summary>Team ID of the spawned player.</summary>
        public int TeamId { get; }
        
        /// <summary>Timestamp when the spawn occurred.</summary>
        public float SpawnTime { get; }

        public PlayerSpawnedEvent(int playerId, int spawnPointId, Vector3 position, int teamId)
        {
            PlayerId = playerId;
            SpawnPointId = spawnPointId;
            Position = position;
            TeamId = teamId;
            SpawnTime = Time.time;
        }
    }
    
    #endregion
}
