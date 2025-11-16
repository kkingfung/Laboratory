using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Shared.Types;
using Laboratory.Core.Enums;
using Laboratory.Chimera.Social.Types;

namespace Laboratory.Systems.Analytics.Services
{
    /// <summary>
    /// Manages gameplay session lifecycle including session creation, tracking, and metrics calculation.
    /// Extracted from PlayerAnalyticsTracker for single responsibility.
    /// </summary>
    public class PlayerSessionManager
    {
        // Events
        public System.Action<AnalyticsSessionData> OnSessionStarted;
        public System.Action<AnalyticsSessionData> OnSessionEnded;

        // Session state
        private AnalyticsSessionData _currentSession;
        private List<AnalyticsSessionData> _sessionHistory = new List<AnalyticsSessionData>();
        private List<PlayerAction> _currentSessionActions = new List<PlayerAction>();
        private Dictionary<EmotionalState, float> _emotionalHistory = new Dictionary<EmotionalState, float>();
        private List<MilestoneType> _sessionMilestones = new List<MilestoneType>();
        private float _sessionStartTime;

        // Configuration
        private int _maxSessionActions;

        public AnalyticsSessionData CurrentSession => _currentSession;
        public IReadOnlyList<AnalyticsSessionData> SessionHistory => _sessionHistory.AsReadOnly();
        public IReadOnlyList<PlayerAction> CurrentSessionActions => _currentSessionActions.AsReadOnly();
        public float SessionDuration => _currentSession != null ? Time.time - _sessionStartTime : 0f;

        public PlayerSessionManager(int maxSessionActions = 10000)
        {
            _maxSessionActions = maxSessionActions;
        }

        /// <summary>
        /// Starts a new gameplay session
        /// </summary>
        public void StartNewSession(PlayerProfile playerProfile)
        {
            _currentSession = new AnalyticsSessionData
            {
                sessionId = (uint)UnityEngine.Random.Range(1000000, 9999999),
                startTime = Time.time,
                playerArchetype = playerProfile?.dominantArchetype ?? ArchetypeType.Unknown
            };

            _sessionStartTime = Time.time;
            _currentSessionActions.Clear();
            _sessionMilestones.Clear();
            _emotionalHistory.Clear();

            OnSessionStarted?.Invoke(_currentSession);
            Debug.Log($"[PlayerSessionManager] Started session: {_currentSession.sessionId}");
        }

        /// <summary>
        /// Ends the current session and calculates final metrics
        /// </summary>
        public void EndSession(PlayerProfile playerProfile)
        {
            if (_currentSession == null)
            {
                Debug.LogWarning("[PlayerSessionManager] Attempted to end null session");
                return;
            }

            _currentSession.endTime = Time.time;
            _currentSession.duration = _currentSession.endTime - _currentSession.startTime - _currentSession.totalPauseTime;
            _currentSession.totalActions = _currentSessionActions.Count;
            _currentSession.uniqueActionTypes = _currentSessionActions.Select(a => a.actionType).Distinct().Count();
            _currentSession.actions = new List<PlayerAction>(_currentSessionActions);

            // Calculate final metrics
            _currentSession.engagementMetrics = CalculateEngagementMetrics();
            _currentSession.behaviorMetrics = CalculateBehaviorMetrics();
            _currentSession.emotionalProfile = new Dictionary<EmotionalState, float>(_emotionalHistory);

            // Add to history
            _sessionHistory.Add(_currentSession);

            OnSessionEnded?.Invoke(_currentSession);
            Debug.Log($"[PlayerSessionManager] Ended session {_currentSession.sessionId}: {_currentSession.duration:F1}s, {_currentSession.totalActions} actions");

            _currentSession = null;
        }

        /// <summary>
        /// Records a player action in the current session
        /// </summary>
        public void RecordAction(PlayerAction action)
        {
            if (_currentSession == null)
            {
                Debug.LogWarning("[PlayerSessionManager] Cannot record action - no active session");
                return;
            }

            _currentSessionActions.Add(action);

            // Enforce max actions limit
            if (_currentSessionActions.Count > _maxSessionActions)
            {
                _currentSessionActions.RemoveAt(0);
            }
        }

        /// <summary>
        /// Records an emotional state in the current session
        /// </summary>
        public void RecordEmotionalState(EmotionalState state, float intensity)
        {
            if (!_emotionalHistory.ContainsKey(state))
            {
                _emotionalHistory[state] = 0f;
            }

            _emotionalHistory[state] = Mathf.Max(_emotionalHistory[state], intensity);
        }

        /// <summary>
        /// Adds a milestone to the current session
        /// </summary>
        public void AddMilestone(MilestoneType milestone)
        {
            if (_currentSession == null) return;

            if (!_sessionMilestones.Contains(milestone))
            {
                _sessionMilestones.Add(milestone);
                Debug.Log($"[PlayerSessionManager] Milestone achieved: {milestone}");
            }
        }

        /// <summary>
        /// Checks if a milestone has been achieved in the current session
        /// </summary>
        public bool HasMilestone(MilestoneType milestone)
        {
            return _sessionMilestones.Contains(milestone);
        }

        /// <summary>
        /// Calculates engagement metrics for the current session
        /// </summary>
        private EngagementMetrics CalculateEngagementMetrics()
        {
            float sessionDuration = _currentSession.duration;
            int actionCount = _currentSessionActions.Count;

            return new EngagementMetrics
            {
                sessionDuration = sessionDuration,
                actionsPerMinute = sessionDuration > 0 ? (actionCount / (sessionDuration / 60f)) : 0f,
                averageActionInterval = actionCount > 1 ? sessionDuration / actionCount : 0f,
                peakEngagementPeriod = CalculatePeakEngagementPeriod(),
                sessionQuality = CalculateSessionQuality(sessionDuration, actionCount)
            };
        }

        /// <summary>
        /// Calculates behavior metrics for the current session
        /// </summary>
        private Dictionary<string, float> CalculateBehaviorMetrics()
        {
            var metrics = new Dictionary<string, float>();

            if (_currentSessionActions.Count == 0) return metrics;

            // Calculate action type distribution
            var actionTypeCounts = _currentSessionActions
                .GroupBy(a => a.actionType)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kvp in actionTypeCounts)
            {
                metrics[$"ActionType_{kvp.Key}"] = kvp.Value / (float)_currentSessionActions.Count;
            }

            // Calculate emotional diversity
            metrics["EmotionalDiversity"] = _emotionalHistory.Count / (float)System.Enum.GetValues(typeof(EmotionalState)).Length;

            // Calculate milestone completion rate
            metrics["MilestoneCompletion"] = _sessionMilestones.Count / 10f; // Assume 10 possible milestones

            return metrics;
        }

        private float CalculatePeakEngagementPeriod()
        {
            // Simple implementation - could be enhanced
            return _sessionStartTime;
        }

        private float CalculateSessionQuality(float duration, int actionCount)
        {
            // Quality based on duration and action density
            float durationQuality = Mathf.Clamp01(duration / 3600f); // 1 hour = max quality
            float actionQuality = Mathf.Clamp01(actionCount / 1000f); // 1000 actions = max quality
            return (durationQuality + actionQuality) / 2f;
        }

        /// <summary>
        /// Gets session statistics summary
        /// </summary>
        public SessionStats GetSessionStats()
        {
            return new SessionStats
            {
                totalSessions = _sessionHistory.Count,
                averageSessionDuration = _sessionHistory.Count > 0 ? _sessionHistory.Average(s => s.duration) : 0f,
                totalPlayTime = _sessionHistory.Sum(s => s.duration),
                totalActions = _sessionHistory.Sum(s => s.totalActions),
                currentSessionDuration = SessionDuration,
                currentSessionActions = _currentSessionActions.Count
            };
        }
    }

    /// <summary>
    /// Session statistics summary
    /// </summary>
    public struct SessionStats
    {
        public int totalSessions;
        public float averageSessionDuration;
        public float totalPlayTime;
        public int totalActions;
        public float currentSessionDuration;
        public int currentSessionActions;
    }
}
