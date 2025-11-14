using System;
using System.Collections.Generic;
using Unity.Collections;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Ecosystem;

namespace Laboratory.Chimera.ECS.Services
{
    /// <summary>
    /// Service responsible for emergency resolution logic and requirement checking.
    /// Determines when emergencies are resolved and handles escalation.
    /// Extracted from EmergencyConservationSystem to improve maintainability.
    /// </summary>
    public class EmergencyResolutionService
    {
        private readonly EmergencyConservationConfig _config;

        public EmergencyResolutionService(EmergencyConservationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Checks if an emergency has been resolved
        /// </summary>
        public bool IsEmergencyResolved(ConservationEmergency emergency)
        {
            return CheckSuccessRequirementTypes(emergency);
        }

        /// <summary>
        /// Checks if an emergency should escalate to crisis level
        /// </summary>
        public bool ShouldEscalate(ConservationEmergency emergency)
        {
            return emergency.timeRemaining < emergency.originalDuration * 0.3f &&
                   emergency.severity >= EmergencySeverity.Severe &&
                   !emergency.hasEscalated;
        }

        /// <summary>
        /// Escalates an emergency to crisis level
        /// </summary>
        public (ConservationEmergency updatedEmergency, ConservationCrisis crisis) EscalateEmergency(
            ConservationEmergency emergency,
            float currentTime)
        {
            var updatedEmergency = emergency;
            updatedEmergency.hasEscalated = true;
            updatedEmergency.severity = EmergencySeverity.Critical;
            updatedEmergency.urgencyLevel = ConservationUrgencyLevel.Immediate;

            var crisis = new ConservationCrisis
            {
                originalEmergency = emergency,
                escalationReason = "Time running out with insufficient response",
                newRequirementTypes = GetEscalatedRequirementTypes(emergency),
                timestamp = currentTime
            };

            return (updatedEmergency, crisis);
        }

        /// <summary>
        /// Creates emergency outcome from resolution
        /// </summary>
        public EmergencyOutcome CreateOutcome(
            ConservationEmergency emergency,
            Dictionary<int, float> playerContributions,
            float currentTime)
        {
            return new EmergencyOutcome
            {
                emergency = emergency,
                isSuccessful = IsEmergencyResolved(emergency),
                finalStatus = GetFinalStatus(emergency),
                playerContributions = ConvertPlayerContributionsToFixedList(playerContributions),
                timestamp = currentTime
            };
        }

        /// <summary>
        /// Creates conservation success from resolved emergency
        /// </summary>
        public ConservationSuccess CreateConservationSuccessFromEmergency(
            ConservationEmergency emergency,
            float currentTime)
        {
            return new ConservationSuccess
            {
                successType = GetSuccessTypeFromEmergency(emergency.type),
                emergencyId = emergency.emergencyId,
                achievementDescription = $"Successfully resolved {emergency.title}",
                contributingFactors = ConvertStringArrayToFixedList128(GetEmergencySuccessFactors(emergency)),
                timestamp = currentTime
            };
        }

        /// <summary>
        /// Creates conservation success from species recovery
        /// </summary>
        public ConservationSuccess CreateConservationSuccess(
            SpeciesPopulationData populationData,
            float currentTime)
        {
            return new ConservationSuccess
            {
                successType = ConservationSuccessType.SpeciesRecovery,
                speciesId = populationData.speciesId,
                achievementDescription = $"Successfully recovered {populationData.speciesName} from endangered status",
                finalPopulation = populationData.currentPopulation,
                recoveryTime = CalculateRecoveryTime(populationData, currentTime),
                contributingFactors = ConvertStringArrayToFixedList128(GetRecoveryFactors()),
                timestamp = currentTime
            };
        }

        /// <summary>
        /// Checks if specific requirement has been met
        /// </summary>
        public bool IsRequirementTypeMet(RequirementType requirement, ConservationEmergency emergency)
        {
            switch (requirement)
            {
                case RequirementType.PopulationIncrease:
                    return CheckPopulationRequirement(emergency);
                case RequirementType.ReproductiveSuccess:
                    return CheckReproductiveRequirement(emergency);
                case RequirementType.HabitatProtection:
                    return CheckHabitatRequirement(emergency);
                case RequirementType.HabitatQuality:
                    return CheckHabitatQualityRequirement(emergency);
                case RequirementType.EcosystemHealth:
                    return CheckEcosystemHealthRequirement(emergency);
                case RequirementType.JuvenileSurvival:
                    return CheckJuvenileSurvivalRequirement(emergency);
                default:
                    return false;
            }
        }

        #region Private Helper Methods

        private bool CheckSuccessRequirementTypes(ConservationEmergency emergency)
        {
            foreach (var requirement in emergency.successRequirementTypes)
            {
                if (!IsRequirementTypeMet(requirement, emergency))
                    return false;
            }
            return true;
        }

        private bool CheckPopulationRequirement(ConservationEmergency emergency)
        {
            return false;
        }

        private bool CheckReproductiveRequirement(ConservationEmergency emergency)
        {
            return false;
        }

        private bool CheckHabitatRequirement(ConservationEmergency emergency)
        {
            return false;
        }

        private bool CheckHabitatQualityRequirement(ConservationEmergency emergency)
        {
            return false;
        }

        private bool CheckEcosystemHealthRequirement(ConservationEmergency emergency)
        {
            return false;
        }

        private bool CheckJuvenileSurvivalRequirement(ConservationEmergency emergency)
        {
            return false;
        }

        private EmergencyStatus GetFinalStatus(ConservationEmergency emergency)
        {
            if (IsEmergencyResolved(emergency))
                return EmergencyStatus.Resolved;
            if (emergency.hasEscalated)
                return EmergencyStatus.Failed;
            return EmergencyStatus.TimeExpired;
        }

        private FixedList32Bytes<RequirementType> GetEscalatedRequirementTypes(ConservationEmergency emergency)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.PopulationTarget);
            requirementTypes.Add(RequirementType.HabitatRestoration);
            requirementTypes.Add(RequirementType.ThreatReduction);
            return requirementTypes;
        }

        private ConservationSuccessType GetSuccessTypeFromEmergency(EmergencyType emergencyType)
        {
            switch (emergencyType)
            {
                case EmergencyType.PopulationCollapse:
                    return ConservationSuccessType.SpeciesRecovery;
                case EmergencyType.EcosystemCollapse:
                    return ConservationSuccessType.EcosystemRestoration;
                case EmergencyType.HabitatDestruction:
                    return ConservationSuccessType.HabitatProtection;
                case EmergencyType.GeneticBottleneck:
                    return ConservationSuccessType.GeneticDiversification;
                case EmergencyType.DiseaseOutbreak:
                    return ConservationSuccessType.DiseaseControl;
                default:
                    return ConservationSuccessType.General;
            }
        }

        private float CalculateRecoveryTime(SpeciesPopulationData populationData, float currentTime)
        {
            return currentTime - populationData.endangeredSince;
        }

        private string[] GetRecoveryFactors()
        {
            return new[]
            {
                "Habitat protection measures",
                "Breeding program success",
                "Threat reduction efforts",
                "Community involvement"
            };
        }

        private string[] GetEmergencySuccessFactors(ConservationEmergency emergency)
        {
            return new[]
            {
                "Rapid response implementation",
                "Multi-stakeholder collaboration",
                "Adequate resource allocation",
                "Effective monitoring programs"
            };
        }

        private FixedList128Bytes<PlayerContribution> ConvertPlayerContributionsToFixedList(Dictionary<int, float> contributions)
        {
            var fixedList = new FixedList128Bytes<PlayerContribution>();
            foreach (var kvp in contributions)
            {
                if (fixedList.Length < fixedList.Capacity)
                {
                    fixedList.Add(new PlayerContribution { playerId = kvp.Key, contribution = kvp.Value });
                }
            }
            return fixedList;
        }

        private FixedList64Bytes<FixedString128Bytes> ConvertStringArrayToFixedList128(string[] strings)
        {
            var fixedList = new FixedList64Bytes<FixedString128Bytes>();
            if (strings != null)
            {
                for (int i = 0; i < strings.Length && fixedList.Length < fixedList.Capacity; i++)
                {
                    fixedList.Add(strings[i]);
                }
            }
            return fixedList;
        }

        #endregion
    }

    #region Enums

    /// <summary>
    /// Emergency outcome status
    /// </summary>
    public enum EmergencyStatus
    {
        Active,
        Resolved,
        Failed,
        TimeExpired
    }

    /// <summary>
    /// Conservation success types
    /// </summary>
    public enum ConservationSuccessType
    {
        General,
        SpeciesRecovery,
        EcosystemRestoration,
        HabitatProtection,
        GeneticDiversification,
        DiseaseControl
    }

    #endregion
}
