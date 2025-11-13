using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Subsystems.AIDirector.Services
{
    /// <summary>
    /// Service responsible for analyzing player behavior patterns and detecting struggle.
    /// Tracks action sequences, behavioral metrics, and engagement patterns.
    /// Extracted from AIDirectorSubsystemManager to improve maintainability.
    /// </summary>
    public class BehavioralAnalysisService
    {
        private readonly Dictionary<string, PlayerProfile> _playerProfiles;

        public BehavioralAnalysisService(Dictionary<string, PlayerProfile> playerProfiles)
        {
            _playerProfiles = playerProfiles ?? throw new ArgumentNullException(nameof(playerProfiles));
        }

        #region Pattern Analysis

        /// <summary>
        /// Analyzes player action patterns and updates behavioral metrics
        /// </summary>
        public void AnalyzePlayerActionPattern(PlayerProfile profile, PlayerActionEvent actionEvent)
        {
            // Track action frequency and patterns using behaviorPattern.activityPreferences
            var actionKey = actionEvent.actionType;
            var currentCount = profile.behaviorPattern.activityPreferences.GetValueOrDefault(actionKey, 0f);
            profile.behaviorPattern.activityPreferences[actionKey] = currentCount + 1f;

            // Track action sequences using commonActionSequences
            if (profile.behaviorPattern.commonActionSequences.Count > 0)
            {
                var lastAction = profile.behaviorPattern.commonActionSequences.LastOrDefault();
                if (!string.IsNullOrEmpty(lastAction))
                {
                    var sequence = $"{lastAction}->{actionEvent.actionType}";
                    var sequenceKey = $"sequence_{sequence}";
                    var sequenceCount = profile.behaviorPattern.activityPreferences.GetValueOrDefault(sequenceKey, 0f);
                    profile.behaviorPattern.activityPreferences[sequenceKey] = sequenceCount + 1f;
                }
            }

            // Add current action to sequence history (keep last 5 actions)
            profile.behaviorPattern.commonActionSequences.Add(actionEvent.actionType);
            if (profile.behaviorPattern.commonActionSequences.Count > 5)
            {
                profile.behaviorPattern.commonActionSequences.RemoveAt(0);
            }

            // Update behavioral metrics based on action patterns
            UpdateBehavioralMetrics(profile, actionEvent);

            // Update engagement based on action type
            var engagementDelta = actionEvent.actionType switch
            {
                "breeding_attempt" => 0.05f,
                "discovery_made" => 0.1f,
                "collaboration_start" => 0.08f,
                "research_published" => 0.15f,
                "exploration" => 0.03f,
                "experiment_success" => 0.08f,
                "experiment_failure" => -0.02f,
                "idle_timeout" => -0.01f,
                _ => 0.01f
            };

            profile.engagementScore = Mathf.Clamp(profile.engagementScore + engagementDelta, 0f, 1f);

            // Update confidence based on success/failure patterns
            if (actionEvent.actionType.Contains("success") || actionEvent.actionType == "discovery_made")
            {
                profile.confidenceLevel = Mathf.Clamp(profile.confidenceLevel + 0.02f, 0f, 1f);
            }
            else if (actionEvent.actionType.Contains("failure") || actionEvent.actionType.Contains("error"))
            {
                profile.confidenceLevel = Mathf.Clamp(profile.confidenceLevel - 0.01f, 0f, 1f);
            }

            // Track exploration vs exploitation tendencies
            if (actionEvent.actionType == "exploration" || actionEvent.actionType == "discovery_made")
            {
                profile.behaviorPattern.explorationTendency = Mathf.Clamp(
                    profile.behaviorPattern.explorationTendency + 0.01f, 0f, 1f);
            }
            else if (actionEvent.actionType == "breeding_attempt" || actionEvent.actionType.Contains("repeat"))
            {
                profile.behaviorPattern.explorationTendency = Mathf.Clamp(
                    profile.behaviorPattern.explorationTendency - 0.005f, 0f, 1f);
            }
        }

        /// <summary>
        /// Updates behavioral metrics based on action patterns
        /// </summary>
        private void UpdateBehavioralMetrics(PlayerProfile profile, PlayerActionEvent actionEvent)
        {
            // Update persistence level based on retry patterns
            var retryActions = profile.behaviorPattern.activityPreferences.Where(
                kvp => kvp.Key.Contains("retry") || kvp.Key.Contains("failure")).Sum(kvp => kvp.Value);

            if (retryActions > 0)
            {
                var totalActions = profile.behaviorPattern.activityPreferences.Values.Sum();
                var retryRatio = retryActions / Math.Max(totalActions, 1f);
                profile.behaviorPattern.persistenceLevel = Mathf.Clamp(retryRatio, 0f, 1f);
            }

            // Update social interaction tendency
            if (actionEvent.actionType.Contains("collaboration") || actionEvent.actionType.Contains("social"))
            {
                profile.behaviorPattern.socialInteraction = Mathf.Clamp(
                    profile.behaviorPattern.socialInteraction + 0.02f, 0f, 1f);
            }

            // Update creativity index based on experimentation
            if (actionEvent.actionType.Contains("experiment") || actionEvent.actionType == "discovery_made")
            {
                profile.behaviorPattern.creativityIndex = Mathf.Clamp(
                    profile.behaviorPattern.creativityIndex + 0.01f, 0f, 1f);
            }

            // Update risk-taking tendency
            if (actionEvent.actionType.Contains("risk") || actionEvent.actionType.Contains("experimental"))
            {
                profile.behaviorPattern.riskTaking = Mathf.Clamp(
                    profile.behaviorPattern.riskTaking + 0.015f, 0f, 1f);
            }
            else if (actionEvent.actionType.Contains("safe") || actionEvent.actionType.Contains("conservative"))
            {
                profile.behaviorPattern.riskTaking = Mathf.Clamp(
                    profile.behaviorPattern.riskTaking - 0.01f, 0f, 1f);
            }
        }

        #endregion

        #region Struggle Detection

        /// <summary>
        /// Detects if player is struggling based on patterns
        /// </summary>
        public bool DetectPlayerStruggle(PlayerProfile profile, PlayerActionEvent actionEvent)
        {
            // Detect struggle patterns
            if (actionEvent.actionType == "breeding_failure" &&
                profile.behaviorPattern.activityPreferences.GetValueOrDefault("breeding_failure", 0) > 3)
            {
                return true;
            }

            if (profile.engagementScore < 0.3f && profile.confidenceLevel < 0.4f)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Helper Types

        /// <summary>
        /// Represents a player action event for analysis
        /// </summary>
        public class PlayerActionEvent
        {
            public string playerId;
            public string actionType;
            public DateTime timestamp;
            public Dictionary<string, object> actionData;
        }

        #endregion
    }
}
