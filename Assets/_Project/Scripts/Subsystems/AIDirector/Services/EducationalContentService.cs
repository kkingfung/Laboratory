using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Subsystems.Genetics;
using Laboratory.Subsystems.Ecosystem;

namespace Laboratory.Subsystems.AIDirector.Services
{
    /// <summary>
    /// Service responsible for educational content triggering and adaptation.
    /// Handles educational scaffolding, difficulty adaptation, and contextual learning support.
    /// Extracted from AIDirectorSubsystemManager to improve maintainability.
    /// </summary>
    public class EducationalContentService
    {
        private readonly Dictionary<string, PlayerProfile> _playerProfiles;
        private readonly AIDirectorSubsystemConfig _config;
        private readonly Action<string, DirectorDecision> _executeDecisionCallback;

        public EducationalContentService(
            Dictionary<string, PlayerProfile> playerProfiles,
            AIDirectorSubsystemConfig config,
            Action<string, DirectorDecision> executeDecisionCallback)
        {
            _playerProfiles = playerProfiles ?? throw new ArgumentNullException(nameof(playerProfiles));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _executeDecisionCallback = executeDecisionCallback ?? throw new ArgumentNullException(nameof(executeDecisionCallback));
        }

        #region Educational Content Triggering

        /// <summary>
        /// Considers triggering educational content for mutation explanation
        /// </summary>
        public void ConsiderTriggeringMutationExplanation(string playerId, List<Mutation> mutations, PlayerProfile profile)
        {
            // Only provide explanations for players who might benefit
            if (profile.skillLevel <= SkillLevel.Intermediate)
            {
                foreach (var mutation in mutations)
                {
                    if (mutation.severity > 0.5f)
                    {
                        TriggerEducationalContent(playerId, "mutation_explanation", mutation);
                    }
                }
            }
        }

        /// <summary>
        /// Considers triggering educational content based on player context
        /// </summary>
        public void ConsiderTriggeringEducationalContent(string playerId, string contentType, object contentData, PlayerProfile profile)
        {
            // Check if player is in educational context
            if (profile.isEducationalContext && profile.engagementScore > 0.6f)
            {
                TriggerEducationalContent(playerId, contentType, contentData);
            }
        }

        /// <summary>
        /// Considers providing environmental education based on event severity
        /// </summary>
        public void ConsiderEnvironmentalEducation(string playerId, EnvironmentalEvent envEvent, PlayerProfile profile)
        {
            if (profile.isEducationalContext && envEvent.severity > 0.7f)
            {
                TriggerEducationalContent(playerId, "environmental_impact", envEvent);
            }
        }

        /// <summary>
        /// Triggers educational content as a director decision
        /// </summary>
        public void TriggerEducationalContent(string playerId, string contentType, object contentData)
        {
            var decision = new DirectorDecision
            {
                decisionType = DirectorDecisionType.TriggerNarrative,
                playerId = playerId,
                priority = DirectorPriority.Medium,
                reasoning = $"Providing educational content: {contentType}",
                parameters = new Dictionary<string, object>
                {
                    ["narrativeType"] = "educational",
                    ["contentType"] = contentType,
                    ["contentData"] = contentData
                }
            };

            _executeDecisionCallback(playerId, decision);
        }

        #endregion

        #region Difficulty Adaptation

        /// <summary>
        /// Adapts content difficulty based on player skill level progression
        /// </summary>
        public void AdaptContentDifficulty(string playerId, float newSkillLevel, PlayerProfile profile)
        {
            var oldSkillLevel = (float)profile.skillLevel / (float)SkillLevel.Expert;

            // If skill level increased significantly, increase content difficulty
            if (newSkillLevel > oldSkillLevel + 0.2f)
            {
                var decision = new DirectorDecision
                {
                    decisionType = DirectorDecisionType.AdjustDifficulty,
                    playerId = playerId,
                    priority = DirectorPriority.Medium,
                    reasoning = "Adapting to improved skill level",
                    parameters = new Dictionary<string, object>
                    {
                        ["adjustmentType"] = DifficultyAdjustmentType.Increase,
                        ["magnitude"] = 0.1f
                    }
                };

                _executeDecisionCallback(playerId, decision);
            }
        }

        #endregion

        #region Guidance and Support

        /// <summary>
        /// Provides guidance for players struggling with specific actions
        /// </summary>
        public void ConsiderProvidingGuidance(string playerId, string strugglingAction)
        {
            var decision = new DirectorDecision
            {
                decisionType = DirectorDecisionType.ProvideHint,
                playerId = playerId,
                priority = DirectorPriority.High,
                reasoning = $"Player struggling with {strugglingAction}",
                parameters = new Dictionary<string, object>
                {
                    ["hintType"] = "guidance",
                    ["hintContent"] = GenerateGuidanceForAction(strugglingAction)
                }
            };

            _executeDecisionCallback(playerId, decision);
        }

        /// <summary>
        /// Generates contextual guidance based on the action type
        /// </summary>
        private string GenerateGuidanceForAction(string strugglingAction)
        {
            return strugglingAction switch
            {
                "breeding_failure" => "Try selecting creatures with more compatible genetic traits, or experiment with different breeding combinations.",
                "research_difficulty" => "Consider collaborating with other researchers or focusing on smaller, more manageable research questions.",
                "ecosystem_management" => "Monitor population dynamics more closely and consider environmental factors affecting creature survival.",
                _ => "Take your time to explore different approaches. Don't hesitate to experiment and learn from each attempt."
            };
        }

        #endregion

        #region Environmental Awareness

        /// <summary>
        /// Updates player ecosystem awareness based on population events
        /// </summary>
        public void UpdatePlayerEcosystemAwareness(string playerId, PopulationEvent populationEvent, PlayerProfile profile)
        {
            // Update player's ecosystem awareness based on population changes
            if (populationEvent.changeType.ToString().Contains("Decline") || populationEvent.changeType.ToString().Contains("Extinction"))
            {
                // Increase environmental concern for negative changes
                var currentConcern = (float)(profile.customAttributes.GetValueOrDefault("environmentalConcern", 0f));
                profile.customAttributes["environmentalConcern"] = Mathf.Clamp(currentConcern + 0.1f, 0f, 1f);

                // Consider providing environmental education
                var updatedConcern = (float)(profile.customAttributes.GetValueOrDefault("environmentalConcern", 0f));
                if (profile.isEducationalContext && updatedConcern > 0.7f)
                {
                    var decision = new DirectorDecision
                    {
                        decisionType = DirectorDecisionType.TriggerNarrative,
                        playerId = playerId,
                        priority = DirectorPriority.Medium,
                        reasoning = "Player needs environmental awareness due to population decline",
                        parameters = new Dictionary<string, object>
                        {
                            ["narrativeType"] = "environmental_education",
                            ["populationEvent"] = populationEvent
                        }
                    };
                    _executeDecisionCallback(playerId, decision);
                }
            }
        }

        #endregion
    }
}
