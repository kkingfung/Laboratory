using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Subsystems.AIDirector
{
    /// <summary>
    /// Concrete implementation of dynamic difficulty adaptation service
    /// Manages difficulty scaling based on player performance and engagement
    /// </summary>
    public class DifficultyAdaptationService : IDifficultyAdaptationService
    {
        #region Fields

        private readonly AIDirectorSubsystemConfig _config;
        private Dictionary<string, DifficultyLevel> _playerDifficulties;
        private Dictionary<string, List<DifficultyAdjustment>> _adjustmentHistory;
        private Dictionary<string, DateTime> _lastAdjustmentTime;
        private Dictionary<string, float> _difficultyMultipliers;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public DifficultyAdaptationService(AIDirectorSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IDifficultyAdaptationService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _playerDifficulties = new Dictionary<string, DifficultyLevel>();
                _adjustmentHistory = new Dictionary<string, List<DifficultyAdjustment>>();
                _lastAdjustmentTime = new Dictionary<string, DateTime>();
                _difficultyMultipliers = new Dictionary<string, float>();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[DifficultyAdaptationService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DifficultyAdaptationService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void ApplyDifficultyAdjustment(string playerId, DifficultyAdjustment adjustment)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId) || adjustment == null)
                return;

            // Check cooldown
            if (_lastAdjustmentTime.TryGetValue(playerId, out var lastTime))
            {
                var timeSinceAdjustment = DateTime.Now - lastTime;
                if (timeSinceAdjustment.TotalMinutes < _config.difficultyAdjustmentCooldown)
                {
                    if (_config.enableDebugLogging)
                        Debug.Log($"[DifficultyAdaptationService] Difficulty adjustment for {playerId} blocked by cooldown");
                    return;
                }
            }

            // Apply the adjustment
            var currentDifficulty = GetCurrentDifficulty(playerId);
            var newDifficulty = CalculateNewDifficulty(currentDifficulty, adjustment);

            // Enforce min/max limits
            if (newDifficulty < _config.minimumDifficulty)
                newDifficulty = _config.minimumDifficulty;
            if (newDifficulty > _config.maximumDifficulty)
                newDifficulty = _config.maximumDifficulty;

            _playerDifficulties[playerId] = newDifficulty;
            _difficultyMultipliers[playerId] = CalculateDifficultyMultiplier(newDifficulty);
            _lastAdjustmentTime[playerId] = DateTime.Now;

            // Record adjustment
            if (!_adjustmentHistory.ContainsKey(playerId))
                _adjustmentHistory[playerId] = new List<DifficultyAdjustment>();

            adjustment.timestamp = DateTime.Now;
            _adjustmentHistory[playerId].Add(adjustment);

            // Keep history manageable
            if (_adjustmentHistory[playerId].Count > 50)
                _adjustmentHistory[playerId].RemoveAt(0);

            if (_config.enableDebugLogging)
                Debug.Log($"[DifficultyAdaptationService] Applied {adjustment.adjustmentType} adjustment to {playerId}: {currentDifficulty} -> {newDifficulty}");
        }

        public DifficultyLevel GetCurrentDifficulty(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return DifficultyLevel.Medium;

            return _playerDifficulties.GetValueOrDefault(playerId, DifficultyLevel.Medium);
        }

        public DifficultyAdjustment RecommendAdjustment(string playerId, PlayerProfile profile, DirectorContext context)
        {
            if (!_isInitialized || profile == null || context == null)
                return null;

            var recommendation = AnalyzePerformanceAndRecommend(playerId, profile, context);
            return recommendation;
        }

        public void ResetDifficulty(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            _playerDifficulties[playerId] = DifficultyLevel.Medium;
            _difficultyMultipliers[playerId] = 1.0f;

            var resetAdjustment = new DifficultyAdjustment
            {
                playerId = playerId,
                adjustmentType = DifficultyAdjustmentType.Reset,
                magnitude = 0f,
                reason = "Manual difficulty reset",
                timestamp = DateTime.Now,
                isTemporary = false
            };

            if (!_adjustmentHistory.ContainsKey(playerId))
                _adjustmentHistory[playerId] = new List<DifficultyAdjustment>();

            _adjustmentHistory[playerId].Add(resetAdjustment);

            if (_config.enableDebugLogging)
                Debug.Log($"[DifficultyAdaptationService] Reset difficulty for {playerId} to Medium");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the difficulty multiplier for a player
        /// </summary>
        public float GetDifficultyMultiplier(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return 1.0f;

            return _difficultyMultipliers.GetValueOrDefault(playerId, 1.0f);
        }

        /// <summary>
        /// Gets the adjustment history for a player
        /// </summary>
        public List<DifficultyAdjustment> GetAdjustmentHistory(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return new List<DifficultyAdjustment>();

            return _adjustmentHistory.GetValueOrDefault(playerId, new List<DifficultyAdjustment>());
        }

        /// <summary>
        /// Checks if a player can receive a difficulty adjustment
        /// </summary>
        public bool CanAdjustDifficulty(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return false;

            if (_lastAdjustmentTime.TryGetValue(playerId, out var lastTime))
            {
                var timeSinceAdjustment = DateTime.Now - lastTime;
                return timeSinceAdjustment.TotalMinutes >= _config.difficultyAdjustmentCooldown;
            }

            return true;
        }

        #endregion

        #region Private Methods

        private DifficultyLevel CalculateNewDifficulty(DifficultyLevel currentDifficulty, DifficultyAdjustment adjustment)
        {
            switch (adjustment.adjustmentType)
            {
                case DifficultyAdjustmentType.Increase:
                    return IncreaseDifficulty(currentDifficulty, adjustment.magnitude);

                case DifficultyAdjustmentType.Decrease:
                    return DecreaseDifficulty(currentDifficulty, adjustment.magnitude);

                case DifficultyAdjustmentType.Reset:
                    return DifficultyLevel.Medium;

                case DifficultyAdjustmentType.Adaptive:
                    return CalculateAdaptiveDifficulty(currentDifficulty, adjustment);

                case DifficultyAdjustmentType.Custom:
                    return CalculateCustomDifficulty(currentDifficulty, adjustment);

                default:
                    return currentDifficulty;
            }
        }

        private DifficultyLevel IncreaseDifficulty(DifficultyLevel current, float magnitude)
        {
            var currentValue = (int)current;
            var increase = Mathf.RoundToInt(magnitude * 4); // Max 4 levels
            var newValue = math.min(currentValue + increase, (int)DifficultyLevel.VeryHard);
            return (DifficultyLevel)newValue;
        }

        private DifficultyLevel DecreaseDifficulty(DifficultyLevel current, float magnitude)
        {
            var currentValue = (int)current;
            var decrease = Mathf.RoundToInt(magnitude * 4); // Max 4 levels
            var newValue = math.max(currentValue - decrease, (int)DifficultyLevel.VeryEasy);
            return (DifficultyLevel)newValue;
        }

        private DifficultyLevel CalculateAdaptiveDifficulty(DifficultyLevel current, DifficultyAdjustment adjustment)
        {
            // Adaptive difficulty based on adjustment parameters
            var targetDifficulty = adjustment.adjustmentParameters.GetValueOrDefault("targetDifficulty", current);
            if (targetDifficulty is DifficultyLevel target)
            {
                // Gradually move toward target
                var currentValue = (int)current;
                var targetValue = (int)target;

                if (targetValue > currentValue)
                    return IncreaseDifficulty(current, 0.5f);
                else if (targetValue < currentValue)
                    return DecreaseDifficulty(current, 0.5f);
                else
                    return current;
            }

            return current;
        }

        private DifficultyLevel CalculateCustomDifficulty(DifficultyLevel current, DifficultyAdjustment adjustment)
        {
            // Custom difficulty calculation based on parameters
            if (adjustment.adjustmentParameters.TryGetValue("difficultyLevel", out var customLevel))
            {
                if (customLevel is DifficultyLevel level)
                    return level;
            }

            return current;
        }

        private float CalculateDifficultyMultiplier(DifficultyLevel difficulty)
        {
            return difficulty switch
            {
                DifficultyLevel.VeryEasy => 0.5f,
                DifficultyLevel.Easy => 0.75f,
                DifficultyLevel.Medium => 1.0f,
                DifficultyLevel.Hard => 1.25f,
                DifficultyLevel.VeryHard => 1.5f,
                _ => 1.0f
            };
        }

        private DifficultyAdjustment AnalyzePerformanceAndRecommend(string playerId, PlayerProfile profile, DirectorContext context)
        {
            // Check if adjustment is needed based on engagement and performance
            var shouldIncrease = ShouldIncreaseDifficulty(profile, context);
            var shouldDecrease = ShouldDecreaseDifficulty(profile, context);

            if (shouldIncrease)
            {
                return new DifficultyAdjustment
                {
                    playerId = playerId,
                    adjustmentType = DifficultyAdjustmentType.Increase,
                    magnitude = _config.difficultyAdjustmentMagnitude,
                    reason = "High engagement and performance - increasing challenge",
                    timestamp = DateTime.Now,
                    isTemporary = false
                };
            }
            else if (shouldDecrease)
            {
                return new DifficultyAdjustment
                {
                    playerId = playerId,
                    adjustmentType = DifficultyAdjustmentType.Decrease,
                    magnitude = _config.difficultyAdjustmentMagnitude,
                    reason = "Low engagement or high frustration - reducing difficulty",
                    timestamp = DateTime.Now,
                    isTemporary = false
                };
            }

            return null; // No adjustment needed
        }

        private bool ShouldIncreaseDifficulty(PlayerProfile profile, DirectorContext context)
        {
            // Increase difficulty if player is highly engaged and performing well
            var highEngagement = context.engagement >= _config.difficultyIncreaseThreshold;
            var lowFrustration = context.frustrationLevel <= 0.3f;
            var goodProgress = context.progressRate >= 0.6f;
            var inFlowState = context.flowState >= 0.7f;

            return highEngagement && lowFrustration && (goodProgress || inFlowState);
        }

        private bool ShouldDecreaseDifficulty(PlayerProfile profile, DirectorContext context)
        {
            // Decrease difficulty if player is struggling or disengaged
            var lowEngagement = context.engagement <= _config.difficultyDecreaseThreshold;
            var highFrustration = context.frustrationLevel >= 0.7f;
            var poorProgress = context.progressRate <= 0.3f;
            var lowConfidence = profile.confidenceLevel <= 0.3f;

            return lowEngagement || highFrustration || (poorProgress && lowConfidence);
        }

        #endregion
    }
}