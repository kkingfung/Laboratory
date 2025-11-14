using Laboratory.Chimera.Core;
using Laboratory.Chimera.Ecosystem;

namespace Laboratory.Chimera.ECS.Services
{
    /// <summary>
    /// Static utility service for calculating emergency urgency levels.
    /// Determines how quickly intervention is needed for different emergency types.
    /// Converted to static for zero-allocation performance optimization.
    /// </summary>
    public static class UrgencyCalculationService
    {
        /// <summary>
        /// Calculates generic urgency level based on current value vs threshold
        /// </summary>
        public static ConservationUrgencyLevel CalculateConservationUrgencyLevel(float currentValue, float threshold)
        {
            float ratio = currentValue / threshold;
            if (ratio <= 0.1f) return ConservationUrgencyLevel.Immediate;
            if (ratio <= 0.3f) return ConservationUrgencyLevel.Urgent;
            if (ratio <= 0.6f) return ConservationUrgencyLevel.Important;
            return ConservationUrgencyLevel.Moderate;
        }

        /// <summary>
        /// Calculates urgency for population-based emergencies
        /// </summary>
        public static ConservationUrgencyLevel CalculatePopulationUrgency(EmergencyConservationConfig config, SpeciesPopulationData populationData)
        {
            return CalculateConservationUrgencyLevel(populationData.currentPopulation, config.criticalPopulationThreshold);
        }

        /// <summary>
        /// Calculates urgency for breeding failure emergencies
        /// </summary>
        public static ConservationUrgencyLevel CalculateBreedingUrgency(EmergencyConservationConfig config, SpeciesPopulationData populationData)
        {
            if (populationData.reproductiveSuccess <= 0.1f && populationData.breedingAge > config.breedingAgeThreshold * 1.5f)
                return ConservationUrgencyLevel.Immediate;
            if (populationData.reproductiveSuccess <= 0.3f)
                return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for juvenile mortality emergencies
        /// </summary>
        public static ConservationUrgencyLevel CalculateJuvenileUrgency(SpeciesPopulationData populationData)
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
        public static ConservationUrgencyLevel CalculateEcosystemUrgency(EcosystemHealth health)
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
        public static ConservationUrgencyLevel CalculateGeneticUrgency(float diversity)
        {
            if (diversity <= 0.1f) return ConservationUrgencyLevel.Immediate;
            if (diversity <= 0.3f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for habitat destruction emergencies
        /// </summary>
        public static ConservationUrgencyLevel CalculateHabitatUrgency(EcosystemData ecosystemData)
        {
            if (ecosystemData.habitatLossRate >= 0.8f) return ConservationUrgencyLevel.Immediate;
            if (ecosystemData.habitatLossRate >= 0.6f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for disease outbreak emergencies
        /// </summary>
        public static ConservationUrgencyLevel CalculateDiseaseUrgency(SpeciesPopulationData populationData)
        {
            if (populationData.diseasePrevalence >= 0.8f) return ConservationUrgencyLevel.Immediate;
            if (populationData.diseasePrevalence >= 0.6f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for climate emergency
        /// </summary>
        public static ConservationUrgencyLevel CalculateClimateUrgency(EcosystemData ecosystemData)
        {
            if (ecosystemData.climateStressLevel >= 0.8f) return ConservationUrgencyLevel.Immediate;
            if (ecosystemData.climateStressLevel >= 0.6f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for food web disruption emergencies
        /// </summary>
        public static ConservationUrgencyLevel CalculateFoodWebUrgency(EcosystemHealth health)
        {
            float instability = 1f - health.foodWebStability;
            if (instability >= 0.8f) return ConservationUrgencyLevel.Immediate;
            if (instability >= 0.6f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        /// <summary>
        /// Calculates urgency for habitat fragmentation emergencies
        /// </summary>
        public static ConservationUrgencyLevel CalculateHabitatFragmentationUrgency(EcosystemHealth health)
        {
            float disconnection = 1f - health.habitatConnectivity;
            if (disconnection >= 0.8f) return ConservationUrgencyLevel.Immediate;
            if (disconnection >= 0.6f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }
    }
}
