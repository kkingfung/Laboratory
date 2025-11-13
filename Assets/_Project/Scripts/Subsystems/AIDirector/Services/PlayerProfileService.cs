using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Laboratory.Subsystems.AIDirector.Services
{
    /// <summary>
    /// Service responsible for player profile management and tracking.
    /// Handles profile creation, updates, engagement tracking, and behavioral metrics.
    /// Extracted from AIDirectorSubsystemManager to improve maintainability.
    /// </summary>
    public class PlayerProfileService
    {
        private readonly Dictionary<string, PlayerProfile> _playerProfiles;
        private readonly AIDirectorSubsystemConfig _config;
        private readonly Queue<DirectorEvent> _eventQueue;

        public PlayerProfileService(
            Dictionary<string, PlayerProfile> playerProfiles,
            AIDirectorSubsystemConfig config,
            Queue<DirectorEvent> eventQueue)
        {
            _playerProfiles = playerProfiles ?? throw new ArgumentNullException(nameof(playerProfiles));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _eventQueue = eventQueue ?? throw new ArgumentNullException(nameof(eventQueue));
        }

        #region Profile Management

        /// <summary>
        /// Gets existing player profile or creates a new one if it doesn't exist
        /// </summary>
        public PlayerProfile GetOrCreatePlayerProfile(string playerId)
        {
            if (!_playerProfiles.ContainsKey(playerId))
            {
                _playerProfiles[playerId] = new PlayerProfile { playerId = playerId };
            }
            return _playerProfiles[playerId];
        }

        /// <summary>
        /// Gets existing player profile or null if it doesn't exist
        /// </summary>
        public PlayerProfile GetPlayerProfile(string playerId)
        {
            _playerProfiles.TryGetValue(playerId, out var profile);
            return profile;
        }

        /// <summary>
        /// Updates player profile from analysis results
        /// </summary>
        public void UpdatePlayerProfileFromAnalysis(PlayerProfile profile, PlayerAnalysis analysis)
        {
            profile.skillLevel = analysis.estimatedSkillLevel;
            profile.engagementScore = analysis.engagementScore;
            profile.learningVelocity = analysis.learningVelocity;
            profile.preferredChallengeLevel = analysis.preferredChallengeLevel;
            profile.collaborationFrequency = analysis.collaborationFrequency;
            profile.lastAnalysisUpdate = DateTime.Now;
        }

        #endregion

        #region Engagement Tracking

        /// <summary>
        /// Updates player engagement score based on an event
        /// </summary>
        public void UpdatePlayerEngagement(string playerId, DirectorEvent directorEvent)
        {
            var profile = GetOrCreatePlayerProfile(playerId);
            var engagementDelta = CalculateEngagementDelta(directorEvent);
            profile.engagementScore = math.clamp(profile.engagementScore + engagementDelta, 0f, 1f);
            profile.lastActivity = DateTime.Now;
        }

        /// <summary>
        /// Calculates engagement delta based on action type
        /// </summary>
        private float CalculateEngagementDelta(DirectorEvent directorEvent)
        {
            var actionType = directorEvent.data.GetValueOrDefault("actionType", "unknown").ToString();

            return actionType switch
            {
                "discovery" => 0.1f,
                "collaboration" => 0.08f,
                "achievement" => 0.05f,
                "exploration" => 0.03f,
                _ => 0.01f
            };
        }

        /// <summary>
        /// Calculates current engagement score with time decay
        /// </summary>
        public float CalculateEngagementScore(PlayerProfile profile)
        {
            var baseEngagement = profile.engagementScore;
            var timeFactor = CalculateTimeFactor(profile.lastActivity);
            return math.clamp(baseEngagement * timeFactor, 0f, 1f);
        }

        /// <summary>
        /// Calculates time-based decay factor for engagement
        /// </summary>
        private float CalculateTimeFactor(DateTime lastActivity)
        {
            var timeSinceActivity = DateTime.Now - lastActivity;
            var decayRate = _config.engagementDecayRate;
            return math.exp(-(float)timeSinceActivity.TotalMinutes * decayRate);
        }

        #endregion

        #region Confidence Tracking

        /// <summary>
        /// Updates player confidence level based on success/failure
        /// </summary>
        public void UpdatePlayerConfidence(string playerId, bool positive)
        {
            var profile = GetOrCreatePlayerProfile(playerId);
            var confidenceDelta = positive ? 0.05f : -0.03f;
            profile.confidenceLevel = math.clamp(profile.confidenceLevel + confidenceDelta, 0f, 1f);
        }

        #endregion

        #region Action Tracking

        /// <summary>
        /// Tracks a player action and queues it as a director event
        /// </summary>
        public void TrackPlayerAction(string playerId, string actionType, Dictionary<string, object> actionData = null)
        {
            var directorEvent = new DirectorEvent
            {
                eventType = DirectorEventType.PlayerAction,
                playerId = playerId,
                timestamp = DateTime.Now,
                data = actionData ?? new Dictionary<string, object>()
            };

            directorEvent.data["actionType"] = actionType;
            _eventQueue.Enqueue(directorEvent);
        }

        #endregion

        #region Metric Calculations

        /// <summary>
        /// Calculates normalized skill level (0-1 range)
        /// </summary>
        public float CalculateSkillLevel(PlayerProfile profile)
        {
            return (float)profile.skillLevel / (float)SkillLevel.Expert;
        }

        /// <summary>
        /// Gets player's progress rate (learning velocity)
        /// </summary>
        public float CalculateProgressRate(PlayerProfile profile)
        {
            return profile.learningVelocity;
        }

        /// <summary>
        /// Gets player's social activity level
        /// </summary>
        public float CalculateSocialActivity(PlayerProfile profile)
        {
            return profile.collaborationFrequency;
        }

        /// <summary>
        /// Calculates session time in minutes
        /// </summary>
        public float CalculateSessionTime(PlayerProfile profile)
        {
            var sessionTime = DateTime.Now - profile.sessionStartTime;
            return (float)sessionTime.TotalMinutes;
        }

        #endregion

        #region Discovery Tracking

        /// <summary>
        /// Reports a player discovery for narrative integration
        /// </summary>
        public void ReportDiscovery(string playerId, string discoveryType, object discoveryData = null)
        {
            var directorEvent = new DirectorEvent
            {
                eventType = DirectorEventType.Discovery,
                playerId = playerId,
                timestamp = DateTime.Now,
                data = new Dictionary<string, object>
                {
                    ["discoveryType"] = discoveryType,
                    ["discoveryData"] = discoveryData
                }
            };

            _eventQueue.Enqueue(directorEvent);
        }

        #endregion
    }
}
