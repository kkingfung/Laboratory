using System;
using UnityEngine;
using Laboratory.Core.Health;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Core.Health.Configuration
{
    /// <summary>
    /// Scriptable Object configuration for the Health System.
    /// Provides centralized, tweakable settings for health mechanics.
    /// </summary>
    [CreateAssetMenu(fileName = "HealthSystemSettings", menuName = "Laboratory/Health/Health System Settings")]
    public class HealthSystemSettings : ScriptableObject
    {
        [Header("General Settings")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _enableStatistics = true;
        [SerializeField] private bool _autoRegisterComponents = true;
        
        [Header("Default Health Values")]
        [SerializeField] private int _defaultMaxHealth = 100;
        [SerializeField] private int _defaultPlayerMaxHealth = 150;
        [SerializeField] private int _defaultAIMaxHealth = 80;
        
        [Header("Damage Processing")]
        [SerializeField] private bool _enableDamageProcessors = true;
        [SerializeField] private bool _enableArmorSystem = true;
        [SerializeField] private float _globalDamageMultiplier = 1.0f;
        [SerializeField] private float _globalHealingMultiplier = 1.0f;
        
        [Header("Network Settings")]
        [SerializeField] private bool _enableNetworkValidation = true;
        [SerializeField] private float _networkDamageTimeout = 2.0f;
        [SerializeField] private int _maxDamageRequestsPerSecond = 10;
        
        [Header("Performance")]
        [SerializeField] private int _maxRegisteredComponents = 1000;
        [SerializeField] private float _statisticsUpdateInterval = 1.0f;
        [SerializeField] private bool _enablePerformanceMetrics = false;

        #region Properties

        public bool EnableDebugLogging => _enableDebugLogging;
        public bool EnableStatistics => _enableStatistics;
        public bool AutoRegisterComponents => _autoRegisterComponents;
        
        public int DefaultMaxHealth => _defaultMaxHealth;
        public int DefaultPlayerMaxHealth => _defaultPlayerMaxHealth;
        public int DefaultAIMaxHealth => _defaultAIMaxHealth;
        
        public bool EnableDamageProcessors => _enableDamageProcessors;
        public bool EnableArmorSystem => _enableArmorSystem;
        public float GlobalDamageMultiplier => _globalDamageMultiplier;
        public float GlobalHealingMultiplier => _globalHealingMultiplier;
        
        public bool EnableNetworkValidation => _enableNetworkValidation;
        public float NetworkDamageTimeout => _networkDamageTimeout;
        public int MaxDamageRequestsPerSecond => _maxDamageRequestsPerSecond;
        
        public int MaxRegisteredComponents => _maxRegisteredComponents;
        public float StatisticsUpdateInterval => _statisticsUpdateInterval;
        public bool EnablePerformanceMetrics => _enablePerformanceMetrics;

        #endregion

        #region Validation

        private void OnValidate()
        {
            // Clamp values to reasonable ranges
            _defaultMaxHealth = Mathf.Clamp(_defaultMaxHealth, 1, 10000);
            _defaultPlayerMaxHealth = Mathf.Clamp(_defaultPlayerMaxHealth, 1, 10000);
            _defaultAIMaxHealth = Mathf.Clamp(_defaultAIMaxHealth, 1, 10000);
            
            _globalDamageMultiplier = Mathf.Clamp(_globalDamageMultiplier, 0.1f, 10.0f);
            _globalHealingMultiplier = Mathf.Clamp(_globalHealingMultiplier, 0.1f, 10.0f);
            
            _networkDamageTimeout = Mathf.Clamp(_networkDamageTimeout, 0.1f, 10.0f);
            _maxDamageRequestsPerSecond = Mathf.Clamp(_maxDamageRequestsPerSecond, 1, 100);
            
            _maxRegisteredComponents = Mathf.Clamp(_maxRegisteredComponents, 10, 10000);
            _statisticsUpdateInterval = Mathf.Clamp(_statisticsUpdateInterval, 0.1f, 10.0f);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the default max health value for a specific component type.
        /// </summary>
        public int GetDefaultMaxHealthFor(Type componentType)
        {
            if (componentType == null) return _defaultMaxHealth;
            
            var typeName = componentType.Name;
            if (typeName.Contains("Player"))
                return _defaultPlayerMaxHealth;
            if (typeName.Contains("AI") || typeName.Contains("NPC"))
                return _defaultAIMaxHealth;
                
            return _defaultMaxHealth;
        }

        /// <summary>
        /// Applies global damage multiplier to a damage amount.
        /// </summary>
        public float ApplyDamageMultiplier(float damage)
        {
            return damage * _globalDamageMultiplier;
        }

        /// <summary>
        /// Applies global healing multiplier to a healing amount.
        /// </summary>
        public float ApplyHealingMultiplier(float healing)
        {
            return healing * _globalHealingMultiplier;
        }

        /// <summary>
        /// Validates if the current configuration is valid.
        /// </summary>
        public bool ValidateConfiguration(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (_defaultMaxHealth <= 0)
            {
                errorMessage = "Default max health must be greater than 0";
                return false;
            }
            
            if (_globalDamageMultiplier <= 0)
            {
                errorMessage = "Global damage multiplier must be greater than 0";
                return false;
            }
            
            if (_globalHealingMultiplier <= 0)
            {
                errorMessage = "Global healing multiplier must be greater than 0";
                return false;
            }
            
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Damage-specific configuration settings.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageSettings", menuName = "Laboratory/Health/Damage Settings")]
    public class DamageSettings : ScriptableObject
    {
        [Header("Damage Type Multipliers")]
        [SerializeField] private DamageTypeMultiplier[] _damageTypeMultipliers = new DamageTypeMultiplier[]
        {
            new DamageTypeMultiplier { Type = DamageType.Normal, Multiplier = 1.0f },
            new DamageTypeMultiplier { Type = DamageType.Critical, Multiplier = 2.0f },
            new DamageTypeMultiplier { Type = DamageType.Fire, Multiplier = 1.2f },
            new DamageTypeMultiplier { Type = DamageType.Ice, Multiplier = 1.1f },
            new DamageTypeMultiplier { Type = DamageType.Lightning, Multiplier = 1.3f },
            new DamageTypeMultiplier { Type = DamageType.Poison, Multiplier = 0.8f },
            new DamageTypeMultiplier { Type = DamageType.Explosive, Multiplier = 1.5f }
        };
        
        [Header("Status Effect Settings")]
        [SerializeField] private bool _enableStatusEffects = true;
        [SerializeField] private float _burnDuration = 3.0f;
        [SerializeField] private float _freezeDuration = 2.0f;
        [SerializeField] private float _stunDuration = 1.5f;
        [SerializeField] private float _poisonDuration = 5.0f;
        
        [Header("Armor Settings")]
        [SerializeField] private bool _enableArmorReduction = true;
        [SerializeField] private float _maxArmorReduction = 0.8f;
        [SerializeField] private ArmorEffectiveness[] _armorEffectiveness = new ArmorEffectiveness[]
        {
            new ArmorEffectiveness { Type = DamageType.Normal, Effectiveness = 1.0f },
            new ArmorEffectiveness { Type = DamageType.Fire, Effectiveness = 0.7f },
            new ArmorEffectiveness { Type = DamageType.Ice, Effectiveness = 0.8f },
            new ArmorEffectiveness { Type = DamageType.Lightning, Effectiveness = 0.6f },
            new ArmorEffectiveness { Type = DamageType.Explosive, Effectiveness = 0.9f }
        };

        #region Properties

        public bool EnableStatusEffects => _enableStatusEffects;
        public float BurnDuration => _burnDuration;
        public float FreezeDuration => _freezeDuration;
        public float StunDuration => _stunDuration;
        public float PoisonDuration => _poisonDuration;
        
        public bool EnableArmorReduction => _enableArmorReduction;
        public float MaxArmorReduction => _maxArmorReduction;

        #endregion

        #region Public API

        /// <summary>
        /// Gets the damage multiplier for a specific damage type.
        /// </summary>
        public float GetDamageMultiplier(DamageType damageType)
        {
            var multiplier = Array.Find(_damageTypeMultipliers, m => m.Type == damageType);
            return multiplier?.Multiplier ?? 1.0f;
        }

        /// <summary>
        /// Gets the armor effectiveness against a specific damage type.
        /// </summary>
        public float GetArmorEffectiveness(DamageType damageType)
        {
            var effectiveness = Array.Find(_armorEffectiveness, a => a.Type == damageType);
            return effectiveness?.Effectiveness ?? 1.0f;
        }

        /// <summary>
        /// Gets the status effect duration for a specific damage type.
        /// </summary>
        public float GetStatusEffectDuration(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Fire => _burnDuration,
                DamageType.Ice => _freezeDuration,
                DamageType.Lightning => _stunDuration,
                DamageType.Poison => _poisonDuration,
                _ => 0f
            };
        }

        #endregion
    }

    #region Supporting Data Structures

    [Serializable]
    public class DamageTypeMultiplier
    {
        public DamageType Type;
        [Range(0.1f, 5.0f)]
        public float Multiplier = 1.0f;
    }

    [Serializable]
    public class ArmorEffectiveness
    {
        public DamageType Type;
        [Range(0.0f, 1.0f)]
        public float Effectiveness = 1.0f;
    }

    #endregion
}
