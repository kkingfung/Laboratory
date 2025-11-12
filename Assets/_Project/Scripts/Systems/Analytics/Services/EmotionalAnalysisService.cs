using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Shared.Types;
using Laboratory.Core.Enums;

namespace Laboratory.Systems.Analytics.Services
{
    /// <summary>
    /// Tracks and analyzes player emotional responses during gameplay.
    /// Extracted from PlayerAnalyticsTracker for single responsibility.
    /// </summary>
    public class EmotionalAnalysisService
    {
        // Events
        public System.Action<EmotionalState, float> OnEmotionalResponseTracked;
        public System.Action<EmotionalMoment> OnEmotionalMomentDetected;

        // Emotional tracking
        private Dictionary<EmotionalState, float> _emotionalHistory = new Dictionary<EmotionalState, float>();
        private List<EmotionalMoment> _emotionalMoments = new List<EmotionalMoment>();
        private EmotionalState _currentDominantEmotion;
        private float _lastEmotionalUpdate;

        // Configuration
        private bool _enableEmotionalAnalysis;
        private float _emotionalUpdateInterval = 5f;

        public IReadOnlyDictionary<EmotionalState, float> EmotionalHistory => _emotionalHistory;
        public EmotionalState CurrentDominantEmotion => _currentDominantEmotion;
        public IReadOnlyList<EmotionalMoment> EmotionalMoments => _emotionalMoments.AsReadOnly();

        public EmotionalAnalysisService(bool enableEmotionalAnalysis = true)
        {
            _enableEmotionalAnalysis = enableEmotionalAnalysis;
            InitializeEmotionalTracking();
        }

        /// <summary>
        /// Initializes emotional tracking with default values
        /// </summary>
        private void InitializeEmotionalTracking()
        {
            _emotionalHistory[EmotionalState.Neutral] = 0.5f;
            _emotionalHistory[EmotionalState.Excited] = 0f;
            _emotionalHistory[EmotionalState.Frustrated] = 0f;
            _emotionalHistory[EmotionalState.Satisfied] = 0f;
            _emotionalHistory[EmotionalState.Curious] = 0f;
            _emotionalHistory[EmotionalState.Anxious] = 0f;
            _emotionalHistory[EmotionalState.Bored] = 0f;

            _currentDominantEmotion = EmotionalState.Neutral;
        }

        /// <summary>
        /// Tracks an emotional response
        /// </summary>
        public void TrackEmotionalResponse(EmotionalState emotionalState, float intensity, string trigger = "")
        {
            if (!_enableEmotionalAnalysis) return;

            // Clamp intensity
            intensity = Mathf.Clamp01(intensity);

            // Update emotional history
            if (!_emotionalHistory.ContainsKey(emotionalState))
            {
                _emotionalHistory[emotionalState] = 0f;
            }

            // Use exponential moving average for smoothing
            float alpha = 0.3f; // Smoothing factor
            _emotionalHistory[emotionalState] = Mathf.Lerp(
                _emotionalHistory[emotionalState],
                intensity,
                alpha
            );

            // Record significant emotional moments
            if (intensity >= 0.7f)
            {
                RecordEmotionalMoment(emotionalState, intensity, trigger);
            }

            OnEmotionalResponseTracked?.Invoke(emotionalState, intensity);
        }

        /// <summary>
        /// Records a significant emotional moment
        /// </summary>
        private void RecordEmotionalMoment(EmotionalState state, float intensity, string trigger)
        {
            var moment = new EmotionalMoment
            {
                emotionalState = state,
                intensity = intensity,
                trigger = trigger,
                timestamp = Time.time
            };

            _emotionalMoments.Add(moment);

            // Keep only recent moments (last 50)
            if (_emotionalMoments.Count > 50)
            {
                _emotionalMoments.RemoveAt(0);
            }

            OnEmotionalMomentDetected?.Invoke(moment);
            Debug.Log($"[EmotionalAnalysisService] Emotional moment: {state} ({intensity:F2}) - {trigger}");
        }

        /// <summary>
        /// Updates the dominant emotional state
        /// </summary>
        public void UpdateDominantEmotion()
        {
            if (Time.time - _lastEmotionalUpdate < _emotionalUpdateInterval)
                return;

            if (_emotionalHistory.Count == 0)
                return;

            // Find the emotion with highest intensity
            var dominantEmotion = _emotionalHistory
                .OrderByDescending(kvp => kvp.Value)
                .First();

            if (dominantEmotion.Key != _currentDominantEmotion && dominantEmotion.Value > 0.5f)
            {
                _currentDominantEmotion = dominantEmotion.Key;
                Debug.Log($"[EmotionalAnalysisService] Dominant emotion changed to: {_currentDominantEmotion}");
            }

            _lastEmotionalUpdate = Time.time;
        }

        /// <summary>
        /// Analyzes emotional patterns from recent actions
        /// </summary>
        public void AnalyzeEmotionalPatterns(PlayerAction action)
        {
            if (!_enableEmotionalAnalysis) return;

            // Infer emotional response from action type
            float inferredIntensity = 0.3f;
            EmotionalState inferredState = EmotionalState.Neutral;

            switch (action.actionType)
            {
                case "Victory":
                case "Achievement":
                    inferredState = EmotionalState.Excited;
                    inferredIntensity = 0.7f;
                    break;

                case "Defeat":
                case "Failure":
                    inferredState = EmotionalState.Frustrated;
                    inferredIntensity = 0.6f;
                    break;

                case "Discovery":
                case "Exploration":
                    inferredState = EmotionalState.Curious;
                    inferredIntensity = 0.5f;
                    break;

                case "Completion":
                case "Reward":
                    inferredState = EmotionalState.Satisfied;
                    inferredIntensity = 0.6f;
                    break;

                case "Idle":
                case "Wait":
                    inferredState = EmotionalState.Bored;
                    inferredIntensity = 0.4f;
                    break;
            }

            if (inferredState != EmotionalState.Neutral)
            {
                TrackEmotionalResponse(inferredState, inferredIntensity, action.actionType);
            }
        }

        /// <summary>
        /// Gets emotional profile summary
        /// </summary>
        public EmotionalProfile GetEmotionalProfile()
        {
            return new EmotionalProfile
            {
                dominantEmotion = _currentDominantEmotion,
                emotionalRange = CalculateEmotionalRange(),
                positiveEmotionRatio = CalculatePositiveEmotionRatio(),
                emotionalStability = CalculateEmotionalStability(),
                significantMomentsCount = _emotionalMoments.Count
            };
        }

        /// <summary>
        /// Calculates the range of emotions experienced
        /// </summary>
        private float CalculateEmotionalRange()
        {
            if (_emotionalHistory.Count == 0) return 0f;

            int experiencedEmotions = _emotionalHistory.Count(kvp => kvp.Value > 0.3f);
            int totalEmotions = System.Enum.GetValues(typeof(EmotionalState)).Length;

            return experiencedEmotions / (float)totalEmotions;
        }

        /// <summary>
        /// Calculates ratio of positive emotions
        /// </summary>
        private float CalculatePositiveEmotionRatio()
        {
            float positive = 0f;
            float negative = 0f;

            foreach (var kvp in _emotionalHistory)
            {
                switch (kvp.Key)
                {
                    case EmotionalState.Excited:
                    case EmotionalState.Satisfied:
                    case EmotionalState.Curious:
                        positive += kvp.Value;
                        break;

                    case EmotionalState.Frustrated:
                    case EmotionalState.Anxious:
                    case EmotionalState.Bored:
                        negative += kvp.Value;
                        break;
                }
            }

            float total = positive + negative;
            return total > 0 ? positive / total : 0.5f;
        }

        /// <summary>
        /// Calculates emotional stability (lower = more stable)
        /// </summary>
        private float CalculateEmotionalStability()
        {
            if (_emotionalHistory.Count == 0) return 1f;

            // Calculate variance of emotional intensities
            float mean = _emotionalHistory.Values.Average();
            float variance = _emotionalHistory.Values.Sum(v => Mathf.Pow(v - mean, 2)) / _emotionalHistory.Count;

            // Inverse variance for stability (lower variance = more stable)
            return 1f - Mathf.Clamp01(variance);
        }

        /// <summary>
        /// Resets emotional tracking (for new session)
        /// </summary>
        public void Reset()
        {
            _emotionalHistory.Clear();
            _emotionalMoments.Clear();
            _currentDominantEmotion = EmotionalState.Neutral;
            _lastEmotionalUpdate = 0f;
            InitializeEmotionalTracking();
        }

        /// <summary>
        /// Gets emotional analysis statistics
        /// </summary>
        public EmotionalAnalysisStats GetEmotionalStats()
        {
            return new EmotionalAnalysisStats
            {
                dominantEmotion = _currentDominantEmotion,
                emotionalRange = CalculateEmotionalRange(),
                positiveRatio = CalculatePositiveEmotionRatio(),
                stability = CalculateEmotionalStability(),
                momentCount = _emotionalMoments.Count
            };
        }
    }

    /// <summary>
    /// Emotional moment data
    /// </summary>
    public struct EmotionalMoment
    {
        public EmotionalState emotionalState;
        public float intensity;
        public string trigger;
        public float timestamp;
    }

    /// <summary>
    /// Emotional profile summary
    /// </summary>
    public struct EmotionalProfile
    {
        public EmotionalState dominantEmotion;
        public float emotionalRange;
        public float positiveEmotionRatio;
        public float emotionalStability;
        public int significantMomentsCount;
    }

    /// <summary>
    /// Emotional analysis statistics summary
    /// </summary>
    public struct EmotionalAnalysisStats
    {
        public EmotionalState dominantEmotion;
        public float emotionalRange;
        public float positiveRatio;
        public float stability;
        public int momentCount;
    }
}
