using UnityEngine;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Subsystems.Combat
{
    /// <summary>
    /// Configuration settings for the Combat Subsystem.
    /// ScriptableObject that can be customized per game mode or enemy type.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatSubsystemConfig", menuName = "Laboratory/Combat/Subsystem Config")]
    public class CombatSubsystemConfig : ScriptableObject
    {
        #region Combat Settings
        
        [Header("Combat Configuration")]
        [Tooltip("Time in seconds before exiting combat state")]
        [SerializeField, Range(1f, 30f)] 
        private float _combatTimeout = 5f;
        
        [Tooltip("Maximum range for combat interactions")]
        [SerializeField, Range(5f, 50f)] 
        private float _combatRange = 15f;
        
        [Tooltip("Enable friendly fire damage")]
        [SerializeField] 
        private bool _enableFriendlyFire = false;
        
        [Tooltip("Automatically clear out-of-range targets")]
        [SerializeField] 
        private bool _autoClearOutOfRangeTargets = true;
        
        #endregion
        
        #region Damage Settings
        
        [Header("Damage Configuration")]
        [Tooltip("Global damage multiplier")]
        [SerializeField, Range(0.1f, 5f)] 
        private float _globalDamageMultiplier = 1.0f;
        
        [Tooltip("Minimum damage threshold")]
        [SerializeField, Range(0.1f, 10f)] 
        private float _minimumDamage = 1.0f;
        
        [Tooltip("Critical hit chance (0-1)")]
        [SerializeField, Range(0f, 1f)] 
        private float _criticalHitChance = 0.05f;
        
        [Tooltip("Critical hit damage multiplier")]
        [SerializeField, Range(1.1f, 5f)] 
        private float _criticalHitMultiplier = 2.0f;
        
        #endregion
        
        #region Damage Over Time Settings
        
        [Header("Damage Over Time Configuration")]
        [Tooltip("Enable damage over time effects")]
        [SerializeField] 
        private bool _enableDamageOverTime = true;
        
        [Tooltip("Default DOT tick interval in seconds")]
        [SerializeField, Range(0.1f, 5f)] 
        private float _dotTickInterval = 1.0f;
        
        [Tooltip("Maximum number of DOT effects per entity")]
        [SerializeField, Range(1, 20)] 
        private int _maxDotEffects = 10;
        
        [Tooltip("DOT damage reduction over time")]
        [SerializeField, Range(0f, 0.1f)] 
        private float _dotDecayRate = 0.02f;
        
        #endregion
        
        #region Ability Settings
        
        [Header("Ability Configuration")]
        [Tooltip("Global ability cooldown multiplier")]
        [SerializeField, Range(0.1f, 5f)] 
        private float _abilityCooldownMultiplier = 1.0f;
        
        [Tooltip("Global ability damage multiplier")]
        [SerializeField, Range(0.1f, 5f)] 
        private float _abilityDamageMultiplier = 1.0f;
        
        [Tooltip("Enable ability interruption on damage")]
        [SerializeField] 
        private bool _enableAbilityInterruption = true;
        
        [Tooltip("Damage threshold for ability interruption")]
        [SerializeField, Range(5f, 100f)] 
        private float _interruptionDamageThreshold = 25f;
        
        #endregion
        
        #region Defensive Settings
        
        [Header("Defensive Configuration")]
        [Tooltip("Base armor value")]
        [SerializeField, Range(0f, 100f)] 
        private float _baseArmor = 0f;
        
        [Tooltip("Magic resistance value")]
        [SerializeField, Range(0f, 100f)] 
        private float _magicResistance = 0f;
        
        [Tooltip("Enable damage mitigation")]
        [SerializeField] 
        private bool _enableDamageMitigation = true;
        
        [Tooltip("Maximum damage reduction percentage")]
        [SerializeField, Range(0f, 0.95f)] 
        private float _maxDamageReduction = 0.75f;
        
        #endregion
        
        #region Healing Settings
        
        [Header("Healing Configuration")]
        [Tooltip("Global healing multiplier")]
        [SerializeField, Range(0.1f, 5f)] 
        private float _healingMultiplier = 1.0f;
        
        [Tooltip("Enable healing over time")]
        [SerializeField] 
        private bool _enableHealingOverTime = true;
        
        [Tooltip("Enable combat healing penalty")]
        [SerializeField] 
        private bool _enableCombatHealingPenalty = true;
        
        [Tooltip("Healing reduction during combat")]
        [SerializeField, Range(0f, 0.9f)] 
        private float _combatHealingReduction = 0.5f;
        
        #endregion
        
        #region AI Combat Settings
        
        [Header("AI Combat Configuration")]
        [Tooltip("AI aggression level")]
        [SerializeField, Range(0f, 2f)] 
        private float _aiAggression = 1.0f;
        
        [Tooltip("AI reaction time")]
        [SerializeField, Range(0.1f, 3f)] 
        private float _aiReactionTime = 0.5f;
        
        [Tooltip("AI target switching delay")]
        [SerializeField, Range(1f, 10f)] 
        private float _aiTargetSwitchDelay = 3f;
        
        [Tooltip("AI retreat health threshold")]
        [SerializeField, Range(0.1f, 0.8f)] 
        private float _aiRetreatThreshold = 0.2f;
        
        #endregion
        
        #region Visual Effects Settings
        
        [Header("Visual Effects Configuration")]
        [Tooltip("Enable damage numbers")]
        [SerializeField] 
        private bool _enableDamageNumbers = true;
        
        [Tooltip("Enable screen shake on damage")]
        [SerializeField] 
        private bool _enableScreenShake = true;
        
        [Tooltip("Screen shake intensity")]
        [SerializeField, Range(0.1f, 2f)] 
        private float _screenShakeIntensity = 0.5f;
        
        [Tooltip("Enable hit particles")]
        [SerializeField] 
        private bool _enableHitParticles = true;
        
        #endregion
        
        #region Audio Settings
        
        [Header("Audio Configuration")]
        [Tooltip("Combat music volume")]
        [SerializeField, Range(0f, 1f)] 
        private float _combatMusicVolume = 0.7f;
        
        [Tooltip("Sound effects volume")]
        [SerializeField, Range(0f, 1f)] 
        private float _soundEffectsVolume = 0.8f;
        
        [Tooltip("Enable dynamic audio")]
        [SerializeField] 
        private bool _enableDynamicAudio = true;
        
        [Tooltip("Audio fade time")]
        [SerializeField, Range(0.1f, 5f)] 
        private float _audioFadeTime = 1.5f;
        
        #endregion
        
        #region Debug Settings
        
        [Header("Debug Configuration")]
        [Tooltip("Enable combat debug visuals")]
        [SerializeField] 
        private bool _enableDebugVisuals = false;
        
        [Tooltip("Enable detailed combat logging")]
        [SerializeField] 
        private bool _enableDetailedLogging = false;
        
        [Tooltip("Show combat statistics")]
        [SerializeField] 
        private bool _showCombatStatistics = false;
        
        [Tooltip("Debug gizmo color")]
        [SerializeField] 
        private Color _debugGizmoColor = Color.red;
        
        #endregion
        
        #region Properties
        
        // Combat Properties
        public float CombatTimeout => _combatTimeout;
        public float CombatRange => _combatRange;
        public bool EnableFriendlyFire => _enableFriendlyFire;
        public bool AutoClearOutOfRangeTargets => _autoClearOutOfRangeTargets;
        
        // Damage Properties
        public float GlobalDamageMultiplier => _globalDamageMultiplier;
        public float MinimumDamage => _minimumDamage;
        public float CriticalHitChance => _criticalHitChance;
        public float CriticalHitMultiplier => _criticalHitMultiplier;
        
        // DOT Properties
        public bool EnableDamageOverTime => _enableDamageOverTime;
        public float DotTickInterval => _dotTickInterval;
        public int MaxDotEffects => _maxDotEffects;
        public float DotDecayRate => _dotDecayRate;
        
        // Ability Properties
        public float AbilityCooldownMultiplier => _abilityCooldownMultiplier;
        public float AbilityDamageMultiplier => _abilityDamageMultiplier;
        public bool EnableAbilityInterruption => _enableAbilityInterruption;
        public float InterruptionDamageThreshold => _interruptionDamageThreshold;
        
        // Defensive Properties
        public float BaseArmor => _baseArmor;
        public float MagicResistance => _magicResistance;
        public bool EnableDamageMitigation => _enableDamageMitigation;
        public float MaxDamageReduction => _maxDamageReduction;
        
        // Healing Properties
        public float HealingMultiplier => _healingMultiplier;
        public bool EnableHealingOverTime => _enableHealingOverTime;
        public bool EnableCombatHealingPenalty => _enableCombatHealingPenalty;
        public float CombatHealingReduction => _combatHealingReduction;
        
        // AI Properties
        public float AiAggression => _aiAggression;
        public float AiReactionTime => _aiReactionTime;
        public float AiTargetSwitchDelay => _aiTargetSwitchDelay;
        public float AiRetreatThreshold => _aiRetreatThreshold;
        
        // Visual Effects Properties
        public bool EnableDamageNumbers => _enableDamageNumbers;
        public bool EnableScreenShake => _enableScreenShake;
        public float ScreenShakeIntensity => _screenShakeIntensity;
        public bool EnableHitParticles => _enableHitParticles;
        
        // Audio Properties
        public float CombatMusicVolume => _combatMusicVolume;
        public float SoundEffectsVolume => _soundEffectsVolume;
        public bool EnableDynamicAudio => _enableDynamicAudio;
        public float AudioFadeTime => _audioFadeTime;
        
        // Debug Properties
        public bool EnableDebugVisuals => _enableDebugVisuals;
        public bool EnableDetailedLogging => _enableDetailedLogging;
        public bool ShowCombatStatistics => _showCombatStatistics;
        public Color DebugGizmoColor => _debugGizmoColor;
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            // Ensure sane values
            _combatTimeout = Mathf.Max(1f, _combatTimeout);
            _combatRange = Mathf.Max(5f, _combatRange);
            
            _globalDamageMultiplier = Mathf.Max(0.1f, _globalDamageMultiplier);
            _minimumDamage = Mathf.Max(0.1f, _minimumDamage);
            _criticalHitChance = Mathf.Clamp01(_criticalHitChance);
            _criticalHitMultiplier = Mathf.Max(1.1f, _criticalHitMultiplier);
            
            _dotTickInterval = Mathf.Max(0.1f, _dotTickInterval);
            _maxDotEffects = Mathf.Max(1, _maxDotEffects);
            _dotDecayRate = Mathf.Max(0f, _dotDecayRate);
            
            _abilityCooldownMultiplier = Mathf.Max(0.1f, _abilityCooldownMultiplier);
            _abilityDamageMultiplier = Mathf.Max(0.1f, _abilityDamageMultiplier);
            _interruptionDamageThreshold = Mathf.Max(5f, _interruptionDamageThreshold);
            
            _baseArmor = Mathf.Max(0f, _baseArmor);
            _magicResistance = Mathf.Max(0f, _magicResistance);
            _maxDamageReduction = Mathf.Clamp(_maxDamageReduction, 0f, 0.95f);
            
            _healingMultiplier = Mathf.Max(0.1f, _healingMultiplier);
            _combatHealingReduction = Mathf.Clamp01(_combatHealingReduction);
            
            _aiAggression = Mathf.Max(0f, _aiAggression);
            _aiReactionTime = Mathf.Max(0.1f, _aiReactionTime);
            _aiTargetSwitchDelay = Mathf.Max(1f, _aiTargetSwitchDelay);
            _aiRetreatThreshold = Mathf.Clamp(_aiRetreatThreshold, 0.1f, 0.8f);
            
            _screenShakeIntensity = Mathf.Max(0.1f, _screenShakeIntensity);
            
            _combatMusicVolume = Mathf.Clamp01(_combatMusicVolume);
            _soundEffectsVolume = Mathf.Clamp01(_soundEffectsVolume);
            _audioFadeTime = Mathf.Max(0.1f, _audioFadeTime);
        }
        
        #endregion
        
        #region Presets
        
        [ContextMenu("Reset to Default")]
        public void ResetToDefault()
        {
            _combatTimeout = 5f;
            _combatRange = 15f;
            _enableFriendlyFire = false;
            _autoClearOutOfRangeTargets = true;
            
            _globalDamageMultiplier = 1.0f;
            _minimumDamage = 1.0f;
            _criticalHitChance = 0.05f;
            _criticalHitMultiplier = 2.0f;
            
            _enableDamageOverTime = true;
            _dotTickInterval = 1.0f;
            _maxDotEffects = 10;
            _dotDecayRate = 0.02f;
            
            _abilityCooldownMultiplier = 1.0f;
            _abilityDamageMultiplier = 1.0f;
            _enableAbilityInterruption = true;
            _interruptionDamageThreshold = 25f;
            
            _baseArmor = 0f;
            _magicResistance = 0f;
            _enableDamageMitigation = true;
            _maxDamageReduction = 0.75f;
            
            _healingMultiplier = 1.0f;
            _enableHealingOverTime = true;
            _enableCombatHealingPenalty = true;
            _combatHealingReduction = 0.5f;
            
            _aiAggression = 1.0f;
            _aiReactionTime = 0.5f;
            _aiTargetSwitchDelay = 3f;
            _aiRetreatThreshold = 0.2f;
            
            _enableDamageNumbers = true;
            _enableScreenShake = true;
            _screenShakeIntensity = 0.5f;
            _enableHitParticles = true;
            
            _combatMusicVolume = 0.7f;
            _soundEffectsVolume = 0.8f;
            _enableDynamicAudio = true;
            _audioFadeTime = 1.5f;
            
            _enableDebugVisuals = false;
            _enableDetailedLogging = false;
            _showCombatStatistics = false;
            _debugGizmoColor = Color.red;
        }
        
        [ContextMenu("Set Easy Mode")]
        public void SetEasyMode()
        {
            ResetToDefault();
            
            _globalDamageMultiplier = 0.7f;
            _criticalHitChance = 0.02f;
            _healingMultiplier = 1.5f;
            _combatHealingReduction = 0.3f;
            _aiAggression = 0.7f;
            _aiReactionTime = 0.8f;
            _aiRetreatThreshold = 0.4f;
        }
        
        [ContextMenu("Set Hard Mode")]
        public void SetHardMode()
        {
            ResetToDefault();
            
            _globalDamageMultiplier = 1.5f;
            _criticalHitChance = 0.1f;
            _criticalHitMultiplier = 2.5f;
            _healingMultiplier = 0.7f;
            _combatHealingReduction = 0.7f;
            _aiAggression = 1.5f;
            _aiReactionTime = 0.3f;
            _aiRetreatThreshold = 0.1f;
            _enableAbilityInterruption = true;
            _interruptionDamageThreshold = 15f;
        }
        
        [ContextMenu("Set PvP Mode")]
        public void SetPvPMode()
        {
            ResetToDefault();
            
            _enableFriendlyFire = false; // Usually disabled in team PvP
            _globalDamageMultiplier = 1.2f;
            _criticalHitChance = 0.08f;
            _healingMultiplier = 0.8f;
            _enableCombatHealingPenalty = true;
            _combatHealingReduction = 0.6f;
            _abilityCooldownMultiplier = 0.8f; // Faster abilities for PvP
        }
        
        [ContextMenu("Set Performance Optimized")]
        public void SetPerformanceOptimized()
        {
            ResetToDefault();
            
            _enableDamageNumbers = false;
            _enableScreenShake = false;
            _enableHitParticles = false;
            _enableDebugVisuals = false;
            _enableDetailedLogging = false;
            _showCombatStatistics = false;
            _enableDynamicAudio = false;
            _maxDotEffects = 5; // Reduce DOT effects for performance
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Creates a runtime copy of this configuration for modification.
        /// </summary>
        public CombatSubsystemConfig CreateRuntimeCopy()
        {
            var copy = CreateInstance<CombatSubsystemConfig>();
            copy.CopyFrom(this);
            return copy;
        }
        
        /// <summary>
        /// Copies all values from another configuration.
        /// </summary>
        public void CopyFrom(CombatSubsystemConfig other)
        {
            _combatTimeout = other._combatTimeout;
            _combatRange = other._combatRange;
            _enableFriendlyFire = other._enableFriendlyFire;
            _autoClearOutOfRangeTargets = other._autoClearOutOfRangeTargets;
            
            _globalDamageMultiplier = other._globalDamageMultiplier;
            _minimumDamage = other._minimumDamage;
            _criticalHitChance = other._criticalHitChance;
            _criticalHitMultiplier = other._criticalHitMultiplier;
            
            _enableDamageOverTime = other._enableDamageOverTime;
            _dotTickInterval = other._dotTickInterval;
            _maxDotEffects = other._maxDotEffects;
            _dotDecayRate = other._dotDecayRate;
            
            _abilityCooldownMultiplier = other._abilityCooldownMultiplier;
            _abilityDamageMultiplier = other._abilityDamageMultiplier;
            _enableAbilityInterruption = other._enableAbilityInterruption;
            _interruptionDamageThreshold = other._interruptionDamageThreshold;
            
            _baseArmor = other._baseArmor;
            _magicResistance = other._magicResistance;
            _enableDamageMitigation = other._enableDamageMitigation;
            _maxDamageReduction = other._maxDamageReduction;
            
            _healingMultiplier = other._healingMultiplier;
            _enableHealingOverTime = other._enableHealingOverTime;
            _enableCombatHealingPenalty = other._enableCombatHealingPenalty;
            _combatHealingReduction = other._combatHealingReduction;
            
            _aiAggression = other._aiAggression;
            _aiReactionTime = other._aiReactionTime;
            _aiTargetSwitchDelay = other._aiTargetSwitchDelay;
            _aiRetreatThreshold = other._aiRetreatThreshold;
            
            _enableDamageNumbers = other._enableDamageNumbers;
            _enableScreenShake = other._enableScreenShake;
            _screenShakeIntensity = other._screenShakeIntensity;
            _enableHitParticles = other._enableHitParticles;
            
            _combatMusicVolume = other._combatMusicVolume;
            _soundEffectsVolume = other._soundEffectsVolume;
            _enableDynamicAudio = other._enableDynamicAudio;
            _audioFadeTime = other._audioFadeTime;
            
            _enableDebugVisuals = other._enableDebugVisuals;
            _enableDetailedLogging = other._enableDetailedLogging;
            _showCombatStatistics = other._showCombatStatistics;
            _debugGizmoColor = other._debugGizmoColor;
        }
        
        /// <summary>
        /// Calculates actual damage after applying all modifiers.
        /// </summary>
        public float CalculateDamage(float baseDamage, bool isCritical = false, bool isAbility = false)
        {
            float damage = baseDamage;
            
            // Apply global damage multiplier
            damage *= _globalDamageMultiplier;
            
            // Apply ability damage multiplier
            if (isAbility)
            {
                damage *= _abilityDamageMultiplier;
            }
            
            // Apply critical hit multiplier
            if (isCritical)
            {
                damage *= _criticalHitMultiplier;
            }
            
            // Ensure minimum damage
            damage = Mathf.Max(damage, _minimumDamage);
            
            return damage;
        }
        
        /// <summary>
        /// Calculates damage reduction based on armor and resistance.
        /// </summary>
        public float CalculateDamageReduction(DamageType damageType)
        {
            float reduction = 0f;
            
            if (!_enableDamageMitigation) return reduction;
            
            switch (damageType)
            {
                case DamageType.Physical:
                case DamageType.Piercing:
                case DamageType.Explosive:
                    // Physical damage reduced by armor
                    reduction = _baseArmor / (100f + _baseArmor);
                    break;
                
                case DamageType.Magic:
                case DamageType.Fire:
                case DamageType.Ice:
                case DamageType.Lightning:
                case DamageType.Poison:
                    // Magical damage reduced by magic resistance
                    reduction = _magicResistance / (100f + _magicResistance);
                    break;
                
                default:
                    // Normal damage uses average of both
                    var avgDefense = (_baseArmor + _magicResistance) / 2f;
                    reduction = avgDefense / (100f + avgDefense);
                    break;
            }
            
            // Cap the reduction
            return Mathf.Min(reduction, _maxDamageReduction);
        }
        
        /// <summary>
        /// Validates the configuration and returns any issues found.
        /// </summary>
        public string[] ValidateConfiguration()
        {
            var issues = new System.Collections.Generic.List<string>();
            
            if (_combatRange <= 0)
                issues.Add("Combat range must be greater than 0");
            
            if (_minimumDamage <= 0)
                issues.Add("Minimum damage must be greater than 0");
            
            if (_criticalHitChance < 0 || _criticalHitChance > 1)
                issues.Add("Critical hit chance must be between 0 and 1");
            
            if (_maxDotEffects <= 0)
                issues.Add("Max DOT effects must be greater than 0");
            
            if (_maxDamageReduction >= 1f)
                issues.Add("Max damage reduction must be less than 100%");
            
            return issues.ToArray();
        }
        
        #endregion
    }
}