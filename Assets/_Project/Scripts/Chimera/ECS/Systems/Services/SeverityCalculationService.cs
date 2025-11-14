using Laboratory.Chimera.Core;
using Laboratory.Chimera.Ecosystem;

namespace Laboratory.Chimera.ECS.Services
{
    /// <summary>
    /// Static utility service for calculating emergency severity levels.
    /// Provides consistent severity assessment across different emergency types.
    /// Converted to static for zero-allocation performance optimization.
    /// </summary>
    public static class SeverityCalculationService
    {
        /// <summary>
        /// Calculates severity for population-based emergencies
        /// </summary>
        public static EmergencySeverity CalculatePopulationSeverity(EmergencyConservationConfig config, SpeciesPopulationData populationData)
        {
            float populationRatio = populationData.currentPopulation / config.criticalPopulationThreshold;
            if (populationRatio <= 0.25f) return EmergencySeverity.Critical;
            if (populationRatio <= 0.5f) return EmergencySeverity.Severe;
            if (populationRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for breeding failure emergencies
        /// </summary>
        public static EmergencySeverity CalculateBreedingSeverity(EmergencyConservationConfig config, SpeciesPopulationData populationData)
        {
            float successRatio = populationData.reproductiveSuccess / config.breedingFailureThreshold;
            if (successRatio <= 0.25f) return EmergencySeverity.Critical;
            if (successRatio <= 0.5f) return EmergencySeverity.Severe;
            if (successRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for juvenile mortality emergencies
        /// </summary>
        public static EmergencySeverity CalculateJuvenileSeverity(EmergencyConservationConfig config, SpeciesPopulationData populationData)
        {
            float survivalRatio = populationData.juvenileSurvivalRate / config.juvenileSurvivalThreshold;
            if (survivalRatio <= 0.25f) return EmergencySeverity.Critical;
            if (survivalRatio <= 0.5f) return EmergencySeverity.Severe;
            if (survivalRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for ecosystem collapse emergencies
        /// </summary>
        public static EmergencySeverity CalculateEcosystemSeverity(EmergencyConservationConfig config, EcosystemHealth health)
        {
            float healthRatio = health.overallHealth / config.ecosystemCollapseThreshold;
            if (healthRatio <= 0.25f) return EmergencySeverity.Critical;
            if (healthRatio <= 0.5f) return EmergencySeverity.Severe;
            if (healthRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for genetic diversity emergencies
        /// </summary>
        public static EmergencySeverity CalculateGeneticSeverity(EmergencyConservationConfig config, float diversity)
        {
            float diversityRatio = diversity / config.geneticDiversityThreshold;
            if (diversityRatio <= 0.25f) return EmergencySeverity.Critical;
            if (diversityRatio <= 0.5f) return EmergencySeverity.Severe;
            if (diversityRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for habitat destruction emergencies
        /// </summary>
        public static EmergencySeverity CalculateHabitatSeverity(EmergencyConservationConfig config, EcosystemData ecosystemData)
        {
            float lossRatio = ecosystemData.habitatLossRate / config.habitatLossRateThreshold;
            if (lossRatio >= 4f) return EmergencySeverity.Critical;
            if (lossRatio >= 3f) return EmergencySeverity.Severe;
            if (lossRatio >= 2f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for disease outbreak emergencies
        /// </summary>
        public static EmergencySeverity CalculateDiseaseSeverity(EmergencyConservationConfig config, SpeciesPopulationData populationData)
        {
            float infectionRate = populationData.diseasePrevalence;
            if (infectionRate >= 0.5f) return EmergencySeverity.Critical;
            if (infectionRate >= 0.35f) return EmergencySeverity.Severe;
            if (infectionRate >= 0.2f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for climate change emergencies
        /// </summary>
        public static EmergencySeverity CalculateClimateSeverity(EmergencyConservationConfig config, EcosystemData ecosystemData)
        {
            float temperatureChange = UnityEngine.Mathf.Abs(ecosystemData.temperatureChangeRate);
            if (temperatureChange >= config.climateChangeRateThreshold * 2f) return EmergencySeverity.Critical;
            if (temperatureChange >= config.climateChangeRateThreshold * 1.5f) return EmergencySeverity.Severe;
            if (temperatureChange >= config.climateChangeRateThreshold) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for food web disruption emergencies
        /// </summary>
        public static EmergencySeverity CalculateFoodWebSeverity(EmergencyConservationConfig config, EcosystemHealth health)
        {
            float stabilityRatio = health.foodWebStability / config.foodWebStabilityThreshold;
            if (stabilityRatio <= 0.25f) return EmergencySeverity.Critical;
            if (stabilityRatio <= 0.5f) return EmergencySeverity.Severe;
            if (stabilityRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for habitat fragmentation emergencies
        /// </summary>
        public static EmergencySeverity CalculateHabitatFragmentationSeverity(EmergencyConservationConfig config, EcosystemHealth health)
        {
            float connectivityRatio = health.habitatConnectivity / config.habitatConnectivityThreshold;
            if (connectivityRatio <= 0.25f) return EmergencySeverity.Critical;
            if (connectivityRatio <= 0.5f) return EmergencySeverity.Severe;
            if (connectivityRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }
    }
}
