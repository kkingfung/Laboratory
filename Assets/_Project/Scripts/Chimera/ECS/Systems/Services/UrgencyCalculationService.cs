using System;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Ecosystem;

namespace Laboratory.Chimera.ECS.Services
{
    /// <summary>
    /// Service responsible for calculating emergency urgency levels.
    /// Determines how quickly intervention is needed for different emergency types.
    /// Extracted from EmergencyConservationSystem to improve maintainability.
    /// </summary>
    public class UrgencyCalculationService
    {
        private readonly EmergencyConservationConfig _config;

        public UrgencyCalculationService(EmergencyConservationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Calculates generic urgency level based on current value vs threshold
        /// </summary>
        public ConservationUrgencyLevel CalculateConservationUrgencyLevel(float currentValue, float threshold)
        {
            float ratio = currentValue / threshold;
            if (ratio <= 0.1f) return ConservationUrgencyLevel.Immediate;
            if (ratio <= 0.3f) return ConservationUrgencyLevel.Urgent;
            if (ratio <= 0.6f) return ConservationUrgencyLevel.Important;
            return ConservationUrgencyLevel.Moderate;
        }

        /// <summary>
        /// Calculates urgency for breeding failure emergencies
        /// </summary>
        public ConservationUrgencyLevel CalculateBreedingUrgency(SpeciesPopulationData populationData)
        {
            if (populationData.reproductiveSuccess <= 0.1f && populationData.breedingAge > _config.breedingAgeThreshold * 1.5f)
                return ConservationUrgencyLevel.Immediate;
            if (populationData.reproductiveSuccess <= 0.3f)
                return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for juvenile mortality emergencies
        /// </summary>
        public ConservationUrgencyLevel CalculateJuvenileUrgency(SpeciesPopulationData populationData)
        {
            if (populationData.juvenileSurvivalRate <= 0.2f)
                return ConservationUrgencyLevel.Immediate;
            if (populationData.juvenileSurvivalRate <= 0.4f)
                return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for ecosystem collapse emergencies
        /// </summary>
        public ConservationUrgencyLevel CalculateEcosystemUrgency(EcosystemHealth health)
        {
            if (health.overallHealth <= 0.2f && health.healthTrend < -0.1f)
                return ConservationUrgencyLevel.Immediate;
            if (health.overallHealth <= 0.4f)
                return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for genetic diversity emergencies
        /// </summary>
        public ConservationUrgencyLevel CalculateGeneticUrgency(float diversity)
        {
            if (diversity <= 0.1f) return ConservationUrgencyLevel.Immediate;
            if (diversity <= 0.3f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for habitat destruction emergencies
        /// </summary>
        public ConservationUrgencyLevel CalculateHabitatUrgency(EcosystemData ecosystemData)
        {
            if (ecosystemData.habitatLossRate >= 0.8f) return ConservationUrgencyLevel.Immediate;
            if (ecosystemData.habitatLossRate >= 0.6f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for disease outbreak emergencies
        /// </summary>
        public ConservationUrgencyLevel CalculateDiseaseUrgency(SpeciesPopulationData populationData)
        {
            if (populationData.diseasePrevalence >= 0.8f) return ConservationUrgencyLevel.Immediate;
            if (populationData.diseasePrevalence >= 0.6f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for climate emergency
        /// </summary>
        public ConservationUrgencyLevel CalculateClimateUrgency(EcosystemData ecosystemData)
        {
            if (ecosystemData.climateStressLevel >= 0.8f) return ConservationUrgencyLevel.Immediate;
            if (ecosystemData.climateStressLevel >= 0.6f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }
    }
}
