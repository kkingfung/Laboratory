using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Gameplay
{
    /// <summary>
    /// Dynamic difficulty scaling system that adapts to player performance.
    /// Analyzes player skill metrics and adjusts enemy stats, spawn rates, and rewards.
    /// Provides smooth difficulty curves for better player experience and retention.
    /// </summary>
    public class DifficultyScalingSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Scaling Settings")]
        [SerializeField] private bool enableDifficultyScaling = true;
        [SerializeField] private DifficultyMode difficultyMode = DifficultyMode.Adaptive;
        [SerializeField] private float scalingSpeed = 0.1f; // How fast difficulty changes
        [SerializeField] private bool logDifficultyChanges = true;

        [Header("Skill Analysis")]
        [SerializeField] private int performanceWindowSize = 10; // Last N encounters
        [SerializeField] private float targetWinRate = 0.7f; // 70% win rate target
        [SerializeField] private float winRateTolerance = 0.1f; // ±10% tolerance

        [Header("Difficulty Bounds")]
        [SerializeField] private float minDifficultyMultiplier = 0.5f;
        [SerializeField] private float maxDifficultyMultiplier = 2.5f;
        [SerializeField] private float startingDifficultyMultiplier = 1.0f;

        [Header("Stat Scaling")]
        [SerializeField] private bool scaleEnemyHealth = true;
        [SerializeField] private bool scaleEnemyDamage = true;
        [SerializeField] private bool scaleEnemySpeed = false;
        [SerializeField] private bool scaleSpawnRates = true;
        [SerializeField] private bool scaleRewards = true;

        #endregion

        #region Private Fields

        private static DifficultyScalingSystem _instance;

        // Current difficulty state
        private float _currentDifficultyMultiplier;
        private DifficultyLevel _currentDifficultyLevel;

        // Performance tracking
        private readonly Queue<PerformanceMetric> _performanceHistory = new Queue<PerformanceMetric>();
        private readonly List<EncounterResult> _encounterHistory = new List<EncounterResult>();

        // Player skill metrics
        private float _estimatedSkillLevel;
        private float _recentWinRate;
        private float _averageCombatDuration;
        private float _averageDamageTaken;

        // Statistics
        private int _totalEncounters;
        private int _totalWins;
        private int _totalLosses;
        private int _difficultyAdjustments;

        // Events
        public event Action<float, DifficultyLevel> OnDifficultyChanged;

        #endregion

        #region Properties

        public static DifficultyScalingSystem Instance => _instance;
        public bool IsEnabled => enableDifficultyScaling;
        public float CurrentDifficultyMultiplier => _currentDifficultyMultiplier;
        public DifficultyLevel CurrentDifficultyLevel => _currentDifficultyLevel;
        public float EstimatedSkillLevel => _estimatedSkillLevel;
        public float RecentWinRate => _recentWinRate;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!enableDifficultyScaling) return;

            // Update difficulty based on mode
            if (difficultyMode == DifficultyMode.Adaptive)
            {
                UpdateAdaptiveDifficulty();
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[DifficultyScalingSystem] Initializing...");

            _currentDifficultyMultiplier = startingDifficultyMultiplier;
            _currentDifficultyLevel = CalculateDifficultyLevel(_currentDifficultyMultiplier);

            Debug.Log($"[DifficultyScalingSystem] Initialized. Starting difficulty: {_currentDifficultyLevel} ({_currentDifficultyMultiplier:F2}x)");
        }

        #endregion

        #region Performance Tracking

        /// <summary>
        /// Record the result of an encounter (combat, puzzle, etc.).
        /// </summary>
        public void RecordEncounterResult(bool success, float duration, float damageTaken, float damageDealt)
        {
            var result = new EncounterResult
            {
                success = success,
                duration = duration,
                damageTaken = damageTaken,
                damageDealt = damageDealt,
                timestamp = Time.time,
                difficultyMultiplier = _currentDifficultyMultiplier
            };

            _encounterHistory.Add(result);
            _totalEncounters++;

            if (success)
                _totalWins++;
            else
                _totalLosses++;

            // Update performance metrics
            UpdatePerformanceMetrics();

            if (logDifficultyChanges)
            {
                Debug.Log($"[DifficultyScalingSystem] Encounter: {(success ? "WIN" : "LOSS")} (Duration: {duration:F1}s, Damage: {damageTaken:F0})");
            }
        }

        /// <summary>
        /// Record a performance metric.
        /// </summary>
        public void RecordPerformanceMetric(string metricName, float value)
        {
            var metric = new PerformanceMetric
            {
                metricName = metricName,
                value = value,
                timestamp = Time.time
            };

            _performanceHistory.Enqueue(metric);

            // Trim history
            while (_performanceHistory.Count > performanceWindowSize)
            {
                _performanceHistory.Dequeue();
            }
        }

        private void UpdatePerformanceMetrics()
        {
            // Get recent encounters
            var recentEncounters = _encounterHistory
                .Skip(Math.Max(0, _encounterHistory.Count - performanceWindowSize))
                .ToList();

            if (recentEncounters.Count == 0) return;

            // Calculate win rate
            _recentWinRate = recentEncounters.Count(e => e.success) / (float)recentEncounters.Count;

            // Calculate average combat duration
            _averageCombatDuration = recentEncounters.Average(e => e.duration);

            // Calculate average damage taken
            _averageDamageTaken = recentEncounters.Average(e => e.damageTaken);

            // Estimate skill level (0-1 scale)
            _estimatedSkillLevel = CalculateSkillLevel(recentEncounters);
        }

        private float CalculateSkillLevel(List<EncounterResult> encounters)
        {
            // Skill based on: win rate, efficiency (duration), damage mitigation
            float winRateScore = _recentWinRate;

            // Lower duration = higher skill (normalized)
            float maxDuration = encounters.Max(e => e.duration);
            float avgDuration = _averageCombatDuration;
            float durationScore = maxDuration > 0 ? 1f - (avgDuration / maxDuration) : 0.5f;

            // Lower damage taken = higher skill (normalized)
            float maxDamage = encounters.Max(e => e.damageTaken);
            float avgDamage = _averageDamageTaken;
            float damageScore = maxDamage > 0 ? 1f - (avgDamage / maxDamage) : 0.5f;

            // Weighted average
            return (winRateScore * 0.5f) + (durationScore * 0.25f) + (damageScore * 0.25f);
        }

        #endregion

        #region Difficulty Adjustment

        private void UpdateAdaptiveDifficulty()
        {
            if (_encounterHistory.Count < performanceWindowSize / 2)
                return; // Need enough data

            // Check if difficulty needs adjustment
            float desiredDifficulty = CalculateDesiredDifficulty();
            float difference = desiredDifficulty - _currentDifficultyMultiplier;

            // Apply gradual change
            if (Math.Abs(difference) > 0.01f)
            {
                float change = difference * scalingSpeed * Time.deltaTime;
                SetDifficultyMultiplier(_currentDifficultyMultiplier + change);
            }
        }

        private float CalculateDesiredDifficulty()
        {
            // If win rate is too high, increase difficulty
            // If win rate is too low, decrease difficulty

            float winRateDifference = _recentWinRate - targetWinRate;

            if (Math.Abs(winRateDifference) <= winRateTolerance)
            {
                // Within tolerance, keep current difficulty
                return _currentDifficultyMultiplier;
            }

            // Calculate adjustment based on win rate difference
            float adjustment = winRateDifference * 0.5f; // Max ±50% adjustment per step

            float desiredDifficulty = _currentDifficultyMultiplier + adjustment;

            // Clamp to bounds
            return Mathf.Clamp(desiredDifficulty, minDifficultyMultiplier, maxDifficultyMultiplier);
        }

        /// <summary>
        /// Manually set the difficulty multiplier.
        /// </summary>
        public void SetDifficultyMultiplier(float multiplier)
        {
            float oldMultiplier = _currentDifficultyMultiplier;
            _currentDifficultyMultiplier = Mathf.Clamp(multiplier, minDifficultyMultiplier, maxDifficultyMultiplier);

            DifficultyLevel oldLevel = _currentDifficultyLevel;
            _currentDifficultyLevel = CalculateDifficultyLevel(_currentDifficultyMultiplier);

            if (oldLevel != _currentDifficultyLevel)
            {
                _difficultyAdjustments++;
                OnDifficultyChanged?.Invoke(_currentDifficultyMultiplier, _currentDifficultyLevel);

                if (logDifficultyChanges)
                {
                    Debug.Log($"[DifficultyScalingSystem] Difficulty changed: {oldLevel} ({oldMultiplier:F2}x) → {_currentDifficultyLevel} ({_currentDifficultyMultiplier:F2}x)");
                }
            }
        }

        /// <summary>
        /// Set difficulty to a specific level.
        /// </summary>
        public void SetDifficultyLevel(DifficultyLevel level)
        {
            float multiplier = level switch
            {
                DifficultyLevel.VeryEasy => 0.5f,
                DifficultyLevel.Easy => 0.75f,
                DifficultyLevel.Normal => 1.0f,
                DifficultyLevel.Hard => 1.5f,
                DifficultyLevel.VeryHard => 2.0f,
                DifficultyLevel.Extreme => 2.5f,
                _ => 1.0f
            };

            SetDifficultyMultiplier(multiplier);
        }

        private DifficultyLevel CalculateDifficultyLevel(float multiplier)
        {
            if (multiplier <= 0.6f) return DifficultyLevel.VeryEasy;
            if (multiplier <= 0.9f) return DifficultyLevel.Easy;
            if (multiplier <= 1.2f) return DifficultyLevel.Normal;
            if (multiplier <= 1.75f) return DifficultyLevel.Hard;
            if (multiplier <= 2.25f) return DifficultyLevel.VeryHard;
            return DifficultyLevel.Extreme;
        }

        #endregion

        #region Stat Scaling

        /// <summary>
        /// Get scaled health for an enemy.
        /// </summary>
        public float GetScaledHealth(float baseHealth)
        {
            if (!scaleEnemyHealth) return baseHealth;
            return baseHealth * _currentDifficultyMultiplier;
        }

        /// <summary>
        /// Get scaled damage for an enemy.
        /// </summary>
        public float GetScaledDamage(float baseDamage)
        {
            if (!scaleEnemyDamage) return baseDamage;
            return baseDamage * _currentDifficultyMultiplier;
        }

        /// <summary>
        /// Get scaled speed for an enemy.
        /// </summary>
        public float GetScaledSpeed(float baseSpeed)
        {
            if (!scaleEnemySpeed) return baseSpeed;

            // Speed scales less aggressively (square root)
            return baseSpeed * Mathf.Sqrt(_currentDifficultyMultiplier);
        }

        /// <summary>
        /// Get scaled spawn rate multiplier.
        /// </summary>
        public float GetSpawnRateMultiplier()
        {
            if (!scaleSpawnRates) return 1f;

            // Higher difficulty = more enemies
            return _currentDifficultyMultiplier;
        }

        /// <summary>
        /// Get scaled reward multiplier.
        /// </summary>
        public float GetRewardMultiplier()
        {
            if (!scaleRewards) return 1f;

            // Higher difficulty = better rewards
            return _currentDifficultyMultiplier * 0.8f + 0.2f; // Min 20% bonus
        }

        /// <summary>
        /// Apply difficulty scaling to a creature's stats.
        /// </summary>
        public void ApplyDifficultyScaling(ref float health, ref float damage, ref float speed)
        {
            health = GetScaledHealth(health);
            damage = GetScaledDamage(damage);
            speed = GetScaledSpeed(speed);
        }

        #endregion

        #region Difficulty Modes

        /// <summary>
        /// Set the difficulty mode.
        /// </summary>
        public void SetDifficultyMode(DifficultyMode mode)
        {
            difficultyMode = mode;

            if (logDifficultyChanges)
            {
                Debug.Log($"[DifficultyScalingSystem] Difficulty mode changed to: {mode}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get difficulty scaling statistics.
        /// </summary>
        public DifficultyStats GetStats()
        {
            return new DifficultyStats
            {
                currentMultiplier = _currentDifficultyMultiplier,
                currentLevel = _currentDifficultyLevel,
                estimatedSkillLevel = _estimatedSkillLevel,
                recentWinRate = _recentWinRate,
                totalEncounters = _totalEncounters,
                totalWins = _totalWins,
                totalLosses = _totalLosses,
                difficultyAdjustments = _difficultyAdjustments,
                difficultyMode = difficultyMode,
                isEnabled = enableDifficultyScaling
            };
        }

        /// <summary>
        /// Reset difficulty to starting values.
        /// </summary>
        public void ResetDifficulty()
        {
            _currentDifficultyMultiplier = startingDifficultyMultiplier;
            _currentDifficultyLevel = CalculateDifficultyLevel(_currentDifficultyMultiplier);

            _performanceHistory.Clear();
            _encounterHistory.Clear();

            _estimatedSkillLevel = 0f;
            _recentWinRate = 0f;
            _averageCombatDuration = 0f;
            _averageDamageTaken = 0f;

            _totalEncounters = 0;
            _totalWins = 0;
            _totalLosses = 0;
            _difficultyAdjustments = 0;

            Debug.Log("[DifficultyScalingSystem] Difficulty reset to starting values");
        }

        /// <summary>
        /// Get encounter history.
        /// </summary>
        public List<EncounterResult> GetEncounterHistory()
        {
            return new List<EncounterResult>(_encounterHistory);
        }

        #endregion

        #region Context Menu

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Difficulty Scaling Statistics ===\n" +
                      $"Current Level: {stats.currentLevel} ({stats.currentMultiplier:F2}x)\n" +
                      $"Estimated Skill: {stats.estimatedSkillLevel:P0}\n" +
                      $"Recent Win Rate: {stats.recentWinRate:P0}\n" +
                      $"Total Encounters: {stats.totalEncounters} ({stats.totalWins}W / {stats.totalLosses}L)\n" +
                      $"Difficulty Adjustments: {stats.difficultyAdjustments}\n" +
                      $"Mode: {stats.difficultyMode}\n" +
                      $"Enabled: {stats.isEnabled}");
        }

        [ContextMenu("Reset Difficulty")]
        private void ResetDifficultyMenu()
        {
            ResetDifficulty();
        }

        [ContextMenu("Simulate Easy Win")]
        private void SimulateEasyWin()
        {
            RecordEncounterResult(true, 5f, 10f, 100f);
        }

        [ContextMenu("Simulate Hard Loss")]
        private void SimulateHardLoss()
        {
            RecordEncounterResult(false, 60f, 200f, 20f);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Result of an encounter (combat, puzzle, etc.).
    /// </summary>
    [Serializable]
    public struct EncounterResult
    {
        public bool success;
        public float duration;
        public float damageTaken;
        public float damageDealt;
        public float timestamp;
        public float difficultyMultiplier;
    }

    /// <summary>
    /// A performance metric.
    /// </summary>
    [Serializable]
    public struct PerformanceMetric
    {
        public string metricName;
        public float value;
        public float timestamp;
    }

    /// <summary>
    /// Statistics for the difficulty scaling system.
    /// </summary>
    [Serializable]
    public struct DifficultyStats
    {
        public float currentMultiplier;
        public DifficultyLevel currentLevel;
        public float estimatedSkillLevel;
        public float recentWinRate;
        public int totalEncounters;
        public int totalWins;
        public int totalLosses;
        public int difficultyAdjustments;
        public DifficultyMode difficultyMode;
        public bool isEnabled;
    }

    /// <summary>
    /// Difficulty levels.
    /// </summary>
    public enum DifficultyLevel
    {
        VeryEasy,
        Easy,
        Normal,
        Hard,
        VeryHard,
        Extreme
    }

    /// <summary>
    /// Difficulty scaling modes.
    /// </summary>
    public enum DifficultyMode
    {
        Fixed,      // Difficulty never changes
        Adaptive,   // Difficulty adapts to player performance
        Progressive // Difficulty increases over time
    }

    #endregion
}
