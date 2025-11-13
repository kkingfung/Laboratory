using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Subsystems.AIDirector.Services
{
    /// <summary>
    /// Service responsible for executing AI Director decisions and orchestrating responses.
    /// Routes decisions to appropriate subsystems and handles celebration/collaboration events.
    /// Extracted from AIDirectorSubsystemManager to improve maintainability.
    /// </summary>
    public class DecisionExecutionService
    {
        private readonly Dictionary<string, PlayerProfile> _playerProfiles;
        private readonly AIDirectorSubsystemConfig _config;
        private readonly IDifficultyAdaptationService _difficultyAdaptationService;
        private readonly INarrativeGenerationService _narrativeGenerationService;
        private readonly IContentOrchestrationService _contentOrchestrationService;
        private readonly IEducationalScaffoldingService _educationalScaffoldingService;

        // Events for notification
        public event Action<DirectorDecision> OnDirectorDecisionMade;
        public event Action<DifficultyAdjustment> OnDifficultyAdjusted;
        public event Action<NarrativeEvent> OnNarrativeEventTriggered;

        public DecisionExecutionService(
            Dictionary<string, PlayerProfile> playerProfiles,
            AIDirectorSubsystemConfig config,
            IDifficultyAdaptationService difficultyAdaptationService,
            INarrativeGenerationService narrativeGenerationService,
            IContentOrchestrationService contentOrchestrationService,
            IEducationalScaffoldingService educationalScaffoldingService)
        {
            _playerProfiles = playerProfiles ?? throw new ArgumentNullException(nameof(playerProfiles));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _difficultyAdaptationService = difficultyAdaptationService;
            _narrativeGenerationService = narrativeGenerationService;
            _contentOrchestrationService = contentOrchestrationService;
            _educationalScaffoldingService = educationalScaffoldingService;
        }

        #region Decision Execution

        /// <summary>
        /// Executes a director decision by routing to appropriate handler
        /// </summary>
        public void ExecuteDirectorDecision(string playerId, DirectorDecision decision)
        {
            switch (decision.decisionType)
            {
                case DirectorDecisionType.AdjustDifficulty:
                    ExecuteDifficultyAdjustment(playerId, decision);
                    break;

                case DirectorDecisionType.TriggerNarrative:
                    ExecuteNarrativeTrigger(playerId, decision);
                    break;

                case DirectorDecisionType.SpawnContent:
                    ExecuteContentSpawn(playerId, decision);
                    break;

                case DirectorDecisionType.ProvideHint:
                    ExecuteHintProvision(playerId, decision);
                    break;

                case DirectorDecisionType.EncourageCollaboration:
                    ExecuteCollaborationEncouragement(playerId, decision);
                    break;

                case DirectorDecisionType.CelebrateAchievement:
                    ExecuteAchievementCelebration(playerId, decision);
                    break;
            }

            OnDirectorDecisionMade?.Invoke(decision);
        }

        /// <summary>
        /// Executes a difficulty adjustment decision
        /// </summary>
        private void ExecuteDifficultyAdjustment(string playerId, DirectorDecision decision)
        {
            var adjustment = new DifficultyAdjustment
            {
                playerId = playerId,
                adjustmentType = (DifficultyAdjustmentType)decision.parameters["adjustmentType"],
                magnitude = (float)decision.parameters["magnitude"],
                reason = decision.reasoning,
                timestamp = DateTime.Now
            };

            _difficultyAdaptationService?.ApplyDifficultyAdjustment(playerId, adjustment);
            OnDifficultyAdjusted?.Invoke(adjustment);
        }

        /// <summary>
        /// Executes a narrative trigger decision
        /// </summary>
        private void ExecuteNarrativeTrigger(string playerId, DirectorDecision decision)
        {
            var narrativeType = decision.parameters["narrativeType"].ToString();
            var narrativeEvent = _narrativeGenerationService?.GenerateNarrative(playerId, narrativeType);

            if (narrativeEvent != null)
            {
                OnNarrativeEventTriggered?.Invoke(narrativeEvent);
            }
        }

        /// <summary>
        /// Executes a content spawn decision
        /// </summary>
        private void ExecuteContentSpawn(string playerId, DirectorDecision decision)
        {
            var contentType = decision.parameters["contentType"].ToString();
            var spawnParameters = decision.parameters.GetValueOrDefault("spawnParameters", new Dictionary<string, object>());

            _contentOrchestrationService?.SpawnContent(playerId, contentType, spawnParameters);
        }

        /// <summary>
        /// Executes a hint provision decision
        /// </summary>
        private void ExecuteHintProvision(string playerId, DirectorDecision decision)
        {
            var hintType = decision.parameters["hintType"].ToString();
            var hintContent = decision.parameters["hintContent"].ToString();

            _educationalScaffoldingService?.ProvideHint(playerId, hintType, hintContent);
        }

        /// <summary>
        /// Executes a collaboration encouragement decision
        /// </summary>
        private void ExecuteCollaborationEncouragement(string playerId, DirectorDecision decision)
        {
            var collaborationType = decision.parameters["collaborationType"].ToString();
            var suggestedPartners = decision.parameters.GetValueOrDefault("suggestedPartners", new List<string>()) as List<string>;

            _contentOrchestrationService?.EncourageCollaboration(playerId, collaborationType, suggestedPartners);
        }

        /// <summary>
        /// Executes an achievement celebration decision
        /// </summary>
        private void ExecuteAchievementCelebration(string playerId, DirectorDecision decision)
        {
            var achievementType = decision.parameters["achievementType"].ToString();
            var celebrationType = decision.parameters["celebrationType"].ToString();

            _narrativeGenerationService?.CelebrateAchievement(playerId, achievementType, celebrationType);
        }

        #endregion

        #region Celebration & Encouragement

        /// <summary>
        /// Triggers a celebration event for player achievement
        /// </summary>
        public void TriggerCelebrationEvent(string playerId, string celebrationType, string achievementData)
        {
            var decision = new DirectorDecision
            {
                decisionType = DirectorDecisionType.CelebrateAchievement,
                playerId = playerId,
                priority = DirectorPriority.High,
                reasoning = $"Celebrating {celebrationType}: {achievementData}",
                parameters = new Dictionary<string, object>
                {
                    ["achievementType"] = celebrationType,
                    ["celebrationType"] = "world_first",
                    ["achievementData"] = achievementData
                }
            };

            ExecuteDirectorDecision(playerId, decision);
        }

        /// <summary>
        /// Encourages collaborative research by suggesting partners
        /// </summary>
        public void EncourageCollaborativeResearch(string playerId)
        {
            var decision = new DirectorDecision
            {
                decisionType = DirectorDecisionType.EncourageCollaboration,
                playerId = playerId,
                priority = DirectorPriority.Medium,
                reasoning = "Encouraging continued collaborative research",
                parameters = new Dictionary<string, object>
                {
                    ["collaborationType"] = "research",
                    ["suggestedPartners"] = GetPotentialCollaborators(playerId)
                }
            };

            ExecuteDirectorDecision(playerId, decision);
        }

        /// <summary>
        /// Finds potential collaborators with complementary skills
        /// </summary>
        private List<string> GetPotentialCollaborators(string playerId)
        {
            var collaborators = new List<string>();

            if (!_playerProfiles.ContainsKey(playerId))
                return collaborators;

            // Find players with complementary skills or research interests
            foreach (var otherProfile in _playerProfiles.Values)
            {
                if (otherProfile.playerId != playerId &&
                    otherProfile.isEducationalContext &&
                    Math.Abs((int)otherProfile.skillLevel - (int)_playerProfiles[playerId].skillLevel) <= 1)
                {
                    collaborators.Add(otherProfile.playerId);
                }
            }

            return collaborators.Take(3).ToList(); // Suggest up to 3 collaborators
        }

        #endregion
    }
}
