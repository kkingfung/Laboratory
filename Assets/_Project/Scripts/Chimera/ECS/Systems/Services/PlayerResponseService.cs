using System;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Ecosystem;

namespace Laboratory.Chimera.ECS.Services
{
    /// <summary>
    /// Service responsible for processing player responses to emergencies.
    /// Handles response effectiveness calculation, progress tracking, and effect application.
    /// Extracted from EmergencyConservationSystem to improve maintainability.
    /// </summary>
    public class PlayerResponseService
    {
        private readonly EmergencyConservationConfig _config;

        public PlayerResponseService(EmergencyConservationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Creates a new player response to an emergency
        /// </summary>
        public EmergencyResponse CreateResponse(
            int playerId,
            int emergencyId,
            EmergencyActionType actionType,
            float resourceCommitment,
            float currentTime)
        {
            return new EmergencyResponse
            {
                playerId = playerId,
                emergencyId = emergencyId,
                actionType = actionType,
                resourcesCommitted = resourceCommitment,
                startTime = currentTime,
                maxDuration = _config.GetActionDuration(actionType),
                progress = 0f,
                effectiveness = 0f,
                timeInvested = 0f,
                isComplete = false
            };
        }

        /// <summary>
        /// Updates response progress and effectiveness
        /// </summary>
        public EmergencyResponse UpdateResponse(EmergencyResponse response, float deltaTime)
        {
            var updatedResponse = response;

            updatedResponse.timeInvested += deltaTime;
            updatedResponse.effectiveness += CalculateEffectivenessGain(response, deltaTime);
            updatedResponse.progress += updatedResponse.effectiveness * deltaTime;
            updatedResponse.progress = UnityEngine.Mathf.Clamp01(updatedResponse.progress);

            return updatedResponse;
        }

        /// <summary>
        /// Checks if a response is complete
        /// </summary>
        public bool IsResponseComplete(EmergencyResponse response)
        {
            return response.progress >= 1f || response.timeInvested >= response.maxDuration;
        }

        /// <summary>
        /// Calculates effectiveness gain per time unit
        /// </summary>
        public float CalculateEffectivenessGain(EmergencyResponse response, float deltaTime)
        {
            return response.resourcesCommitted * deltaTime * _config.responseEffectivenessMultiplier;
        }

        /// <summary>
        /// Applies response effects to the emergency
        /// </summary>
        public ConservationEmergency ApplyResponseToEmergency(
            ConservationEmergency emergency,
            EmergencyResponse response)
        {
            var updatedEmergency = emergency;

            switch (response.actionType)
            {
                case EmergencyActionType.PopulationSupport:
                    updatedEmergency = ApplyPopulationSupport(updatedEmergency, response);
                    break;
                case EmergencyActionType.HabitatProtection:
                    updatedEmergency = ApplyHabitatProtection(updatedEmergency, response);
                    break;
                case EmergencyActionType.BreedingProgram:
                    updatedEmergency = ApplyBreedingProgram(updatedEmergency, response);
                    break;
                case EmergencyActionType.DiseaseControl:
                    updatedEmergency = ApplyDiseaseControl(updatedEmergency, response);
                    break;
                case EmergencyActionType.ClimateAdaptation:
                    updatedEmergency = ApplyClimateAdaptation(updatedEmergency, response);
                    break;
                case EmergencyActionType.GeneticManagement:
                    updatedEmergency = ApplyGeneticManagement(updatedEmergency, response);
                    break;
                case EmergencyActionType.EcosystemRestoration:
                    updatedEmergency = ApplyEcosystemRestoration(updatedEmergency, response);
                    break;
            }

            return updatedEmergency;
        }

        /// <summary>
        /// Tracks player contributions for an emergency
        /// </summary>
        public Dictionary<int, float> UpdatePlayerContributions(
            Dictionary<int, float> contributions,
            EmergencyResponse response)
        {
            var updatedContributions = new Dictionary<int, float>(contributions);

            if (updatedContributions.ContainsKey(response.playerId))
            {
                updatedContributions[response.playerId] += response.effectiveness;
            }
            else
            {
                updatedContributions[response.playerId] = response.effectiveness;
            }

            return updatedContributions;
        }

        #region Action-Specific Application Methods

        private ConservationEmergency ApplyPopulationSupport(
            ConservationEmergency emergency,
            EmergencyResponse response)
        {
            var updated = emergency;
            updated.timeRemaining += response.effectiveness * 10f;
            return updated;
        }

        private ConservationEmergency ApplyHabitatProtection(
            ConservationEmergency emergency,
            EmergencyResponse response)
        {
            var updated = emergency;
            updated.timeRemaining += response.effectiveness * 15f;
            return updated;
        }

        private ConservationEmergency ApplyBreedingProgram(
            ConservationEmergency emergency,
            EmergencyResponse response)
        {
            var updated = emergency;
            updated.timeRemaining += response.effectiveness * 12f;
            return updated;
        }

        private ConservationEmergency ApplyDiseaseControl(
            ConservationEmergency emergency,
            EmergencyResponse response)
        {
            var updated = emergency;
            updated.timeRemaining += response.effectiveness * 8f;
            return updated;
        }

        private ConservationEmergency ApplyClimateAdaptation(
            ConservationEmergency emergency,
            EmergencyResponse response)
        {
            var updated = emergency;
            updated.timeRemaining += response.effectiveness * 20f;
            return updated;
        }

        private ConservationEmergency ApplyGeneticManagement(
            ConservationEmergency emergency,
            EmergencyResponse response)
        {
            var updated = emergency;
            updated.timeRemaining += response.effectiveness * 14f;
            return updated;
        }

        private ConservationEmergency ApplyEcosystemRestoration(
            ConservationEmergency emergency,
            EmergencyResponse response)
        {
            var updated = emergency;
            updated.timeRemaining += response.effectiveness * 18f;
            return updated;
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates if a player can respond to an emergency
        /// </summary>
        public bool CanRespondToEmergency(
            ConservationEmergency emergency,
            EmergencyActionType actionType)
        {
            return emergency.requiredActionTypes.Contains(actionType);
        }

        /// <summary>
        /// Validates resource commitment level
        /// </summary>
        public bool IsValidResourceCommitment(float resourceCommitment)
        {
            return resourceCommitment >= _config.minimumResourceCommitment &&
                   resourceCommitment <= _config.maximumResourceCommitment;
        }

        #endregion
    }
}
