using UnityEngine;
using System;

namespace Laboratory.Core.Player
{
    /// <summary>
    /// Configuration settings for the Player Subsystem
    /// Contains all tunable parameters for player behavior, health, movement, and combat
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerSubsystemConfig", menuName = "Laboratory/Configuration/Player Subsystem Config")]
    public class PlayerSubsystemConfig : ScriptableObject
    {
        [Header("General Settings")]
        [Tooltip("Enable debug logging for player subsystem")]
        public bool enableDebugLogging = false;
        
        [Tooltip("Subsystem update frequency (updates per second)")]
        [Range(30, 120)]
        public float updateFrequency = 60f;

        [Tooltip("Movement detection threshold (minimum distance to detect movement)")]
        [Range(0.001f, 1f)]
        public float MovementThreshold = 0.01f;

        [Header("Health System")]
        public HealthSystemSettings healthSettings = new HealthSystemSettings();

        [Header("Movement System")]
        public MovementSystemSettings movementSettings = new MovementSystemSettings();

        [Header("Combat System")]
        public CombatSystemSettings combatSettings = new CombatSystemSettings();

        [Header("Event System")]
        public EventSystemSettings eventSettings = new EventSystemSettings();

        [Header("Performance")]
        public PerformanceSettings performanceSettings = new PerformanceSettings();

        /// <summary>
        /// Validates all settings and applies corrections if needed
        /// </summary>
        public void ValidateSettings()
        {
            healthSettings.ValidateSettings();
            movementSettings.ValidateSettings();
            combatSettings.ValidateSettings();
            eventSettings.ValidateSettings();
            performanceSettings.ValidateSettings();
            
            // Clamp update frequency
            updateFrequency = Mathf.Clamp(updateFrequency, 30f, 120f);
        }

        /// <summary>
        /// Creates a default configuration
        /// </summary>
        public static PlayerSubsystemConfig CreateDefault()
        {
            var config = CreateInstance<PlayerSubsystemConfig>();
            config.name = "Default Player Config";
            config.ValidateSettings();
            return config;
        }

        private void OnValidate()
        {
            ValidateSettings();
        }
    }

    [Serializable]
    public class HealthSystemSettings
    {
        [Header("Health Values")]
        [Range(10f, 1000f)]
        public float maxHealth = 100f;
        
        [Range(0f, 50f)]
        public float healthRegenRate = 2f;
        
        [Range(0f, 10f)]
        public float healthRegenDelay = 3f;

        [Header("Damage System")]
        public bool enableInvulnerabilityFrames = true;
        
        [Range(0.1f, 3f)]
        public float invulnerabilityDuration = 0.5f;

        [Header("Knockback")]
        public bool enableKnockback = true;
        
        [Range(0f, 50f)]
        public float knockbackForce = 10f;
        
        [Range(0.1f, 2f)]
        public float knockbackDuration = 0.3f;

        public void ValidateSettings()
        {
            maxHealth = Mathf.Max(10f, maxHealth);
            healthRegenRate = Mathf.Max(0f, healthRegenRate);
            healthRegenDelay = Mathf.Max(0f, healthRegenDelay);
            
            if (enableInvulnerabilityFrames)
            {
                invulnerabilityDuration = Mathf.Max(0.1f, invulnerabilityDuration);
            }
            
            if (enableKnockback)
            {
                knockbackForce = Mathf.Max(0f, knockbackForce);
                knockbackDuration = Mathf.Max(0.1f, knockbackDuration);
            }
        }
    }

    [Serializable]
    public class MovementSystemSettings
    {
        [Header("Basic Movement")]
        [Range(1f, 20f)]
        public float walkSpeed = 3.5f;
        
        [Range(2f, 30f)]
        public float runSpeed = 7f;
        
        [Range(0.1f, 2f)]
        public float acceleration = 1f;
        
        [Range(0.1f, 2f)]
        public float deceleration = 1.5f;

        [Header("Jumping")]
        public bool enableJumping = true;
        
        [Range(1f, 20f)]
        public float jumpHeight = 4f;
        
        [Range(1, 3)]
        public int maxJumps = 1;

        [Header("Air Control")]
        [Range(0f, 1f)]
        public float airControl = 0.8f;
        
        [Range(0.5f, 3f)]
        public float fallMultiplier = 2f;

        public void ValidateSettings()
        {
            walkSpeed = Mathf.Max(1f, walkSpeed);
            runSpeed = Mathf.Max(walkSpeed + 0.5f, runSpeed);
            acceleration = Mathf.Max(0.1f, acceleration);
            deceleration = Mathf.Max(0.1f, deceleration);
            
            if (enableJumping)
            {
                jumpHeight = Mathf.Max(1f, jumpHeight);
                maxJumps = Mathf.Max(1, maxJumps);
            }
            
            airControl = Mathf.Clamp01(airControl);
            fallMultiplier = Mathf.Max(0.5f, fallMultiplier);
        }
    }

    [Serializable]
    public class CombatSystemSettings
    {
        [Header("Combat Values")]
        [Range(5f, 100f)]
        public float baseDamage = 20f;
        
        [Range(0.1f, 5f)]
        public float attackCooldown = 1f;
        
        [Range(1f, 10f)]
        public float attackRange = 2f;

        [Header("Combo System")]
        public bool enableCombos = true;
        
        [Range(1, 5)]
        public int maxComboCount = 3;
        
        [Range(0.5f, 3f)]
        public float comboWindow = 1.5f;

        [Header("Critical Hits")]
        public bool enableCriticalHits = true;
        
        [Range(0f, 1f)]
        public float criticalChance = 0.1f;
        
        [Range(1.1f, 5f)]
        public float criticalMultiplier = 2f;

        public void ValidateSettings()
        {
            baseDamage = Mathf.Max(5f, baseDamage);
            attackCooldown = Mathf.Max(0.1f, attackCooldown);
            attackRange = Mathf.Max(1f, attackRange);
            
            if (enableCombos)
            {
                maxComboCount = Mathf.Max(1, maxComboCount);
                comboWindow = Mathf.Max(0.5f, comboWindow);
            }
            
            if (enableCriticalHits)
            {
                criticalChance = Mathf.Clamp01(criticalChance);
                criticalMultiplier = Mathf.Max(1.1f, criticalMultiplier);
            }
        }
    }

    [Serializable]
    public class EventSystemSettings
    {
        [Header("Event Publishing")]
        public bool enableGlobalEvents = true;
        
        [Range(10, 1000)]
        public int maxEventQueueSize = 100;
        
        [Range(0.01f, 1f)]
        public float eventProcessingInterval = 0.1f;

        [Header("Event Types")]
        public bool publishHealthEvents = true;
        public bool publishMovementEvents = true;
        public bool publishCombatEvents = true;
        public bool publishStateEvents = true;

        public void ValidateSettings()
        {
            maxEventQueueSize = Mathf.Max(10, maxEventQueueSize);
            eventProcessingInterval = Mathf.Clamp(eventProcessingInterval, 0.01f, 1f);
        }
    }

    [Serializable]
    public class PerformanceSettings
    {
        [Header("Update Optimization")]
        public bool enableAdaptiveUpdates = true;
        
        [Range(10, 120)]
        public int targetFPS = 60;
        
        [Range(0.001f, 0.1f)]
        public float maxUpdateTime = 0.016f; // ~60 FPS

        [Header("Memory Management")]
        public bool enableObjectPooling = true;
        
        [Range(10, 100)]
        public int initialPoolSize = 20;

        [Header("LOD System")]
        public bool enableLOD = true;
        
        [Range(5f, 50f)]
        public float lodDistance = 25f;

        public void ValidateSettings()
        {
            targetFPS = Mathf.Clamp(targetFPS, 10, 120);
            maxUpdateTime = Mathf.Clamp(maxUpdateTime, 0.001f, 0.1f);
            
            if (enableObjectPooling)
            {
                initialPoolSize = Mathf.Max(10, initialPoolSize);
            }
            
            if (enableLOD)
            {
                lodDistance = Mathf.Max(5f, lodDistance);
            }
        }
    }
}
