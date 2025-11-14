using System;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Ecosystem;

namespace Laboratory.Chimera.ECS.Services
{
    /// <summary>
    /// Service responsible for calculating emergency severity levels.
    /// Provides consistent severity assessment across different emergency types.
    /// Extracted from EmergencyConservationSystem to improve maintainability.
    /// </summary>
    public class SeverityCalculationService
    {
        private readonly EmergencyConservationConfig _config;

        public SeverityCalculationService(EmergencyConservationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Calculates severity for population-based emergencies
        /// </summary>
        public EmergencySeverity CalculatePopulationSeverity(SpeciesPopulationData populationData)
        {
            float populationRatio = populationData.currentPopulation / _config.criticalPopulationThreshold;
            if (populationRatio <= 0.25f) return EmergencySeverity.Critical;
            if (populationRatio <= 0.5f) return EmergencySeverity.Severe;
            if (populationRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for breeding failure emergencies
        /// </summary>
        public EmergencySeverity CalculateBreedingSeverity(SpeciesPopulationData populationData)
        {
            float successRatio = populationData.reproductiveSuccess / _config.breedingFailureThreshold;
            if (successRatio <= 0.25f) return EmergencySeverity.Critical;
            if (successRatio <= 0.5f) return EmergencySeverity.Severe;
            if (successRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for juvenile mortality emergencies
        /// </summary>
        public EmergencySeverity CalculateJuvenileSeverity(SpeciesPopulationData populationData)
        {
            float survivalRatio = populationData.juvenileSurvivalRate / _config.juvenileSurvivalThreshold;
            if (survivalRatio <= 0.25f) return EmergencySeverity.Critical;
            if (survivalRatio <= 0.5f) return EmergencySeverity.Severe;
            if (survivalRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for ecosystem collapse emergencies
        /// </summary>
        public EmergencySeverity CalculateEcosystemSeverity(EcosystemHealth health)
        {
            float healthRatio = health.overallHealth / _config.ecosystemCollapseThreshold;
            if (healthRatio <= 0.25f) return EmergencySeverity.Critical;
            if (healthRatio <= 0.5f) return EmergencySeverity.Severe;
            if (healthRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for genetic diversity emergencies
        /// </summary>
        public EmergencySeverity CalculateGeneticSeverity(float diversity)
        {
            float diversityRatio = diversity / _config.geneticDiversityThreshold;
            if (diversityRatio <= 0.25f) return EmergencySeverity.Critical;
            if (diversityRatio <= 0.5f) return EmergencySeverity.Severe;
            if (diversityRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for habitat destruction emergencies
        /// </summary>
        public EmergencySeverity CalculateHabitatSeverity(EcosystemData ecosystemData)
        {
            float lossRatio = ecosystemData.habitatLossRate / _config.habitatLossRateThreshold;
            if (lossRatio >= 4f) return EmergencySeverity.Critical;
            if (lossRatio >= 3f) return EmergencySeverity.Severe;
            if (lossRatio >= 2f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for disease outbreak emergencies
        /// </summary>
        public EmergencySeverity CalculateDiseaseSeverity(SpeciesPopulationData populationData)
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
        public EmergencySeverity CalculateClimateSeverity(EcosystemData ecosystemData)
        {
            float temperatureChange = UnityEngine.Mathf.Abs(ecosystemData.temperatureChangeRate);
            if (temperatureChange >= _config.climateChangeRateThreshold * 2f) return EmergencySeverity.Critical;
            if (temperatureChange >= _config.climateChangeRateThreshold * 1.5f) return EmergencySeverity.Severe;
            if (temperatureChange >= _config.climateChangeRateThreshold) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for food web disruption emergencies
        /// </summary>
        public EmergencySeverity CalculateFoodWebSeverity(EcosystemHealth health)
        {
            float stabilityRatio = health.foodWebStability / _config.foodWebStabilityThreshold;
            if (stabilityRatio <= 0.25f) return EmergencySeverity.Critical;
            if (stabilityRatio <= 0.5f) return EmergencySeverity.Severe;
            if (stabilityRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        /// <summary>
        /// Calculates severity for habitat fragmentation emergencies
        /// </summary>
        public EmergencySeverity CalculateHabitatSeverity(EcosystemHealth health)
        {
            float connectivityRatio = health.habitatConnectivity / _config.habitatConnectivityThreshold;
            if (connectivityRatio <= 0.25f) return EmergencySeverity.Critical;
            if (connectivityRatio <= 0.5f) return EmergencySeverity.Severe;
            if (connectivityRatio <= 0.75f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }
    }
}
