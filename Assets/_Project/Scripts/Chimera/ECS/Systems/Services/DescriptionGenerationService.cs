using Laboratory.Chimera.Core;
using Laboratory.Chimera.Ecosystem;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.ECS.Services
{
    /// <summary>
    /// Service responsible for generating emergency descriptions and consequences.
    /// Provides consistent messaging across different emergency types.
    /// Extracted from EmergencyConservationSystem to improve maintainability.
    /// </summary>
    public class DescriptionGenerationService
    {
        /// <summary>
        /// Generates description for population collapse emergency
        /// </summary>
        public string GeneratePopulationDescription(SpeciesPopulationData populationData)
        {
            return $"Population has declined to {populationData.currentPopulation} individuals " +
                   $"(trend: {populationData.populationTrend:F2}). Immediate intervention required to prevent extinction.";
        }

        /// <summary>
        /// Generates description for breeding failure emergency
        /// </summary>
        public string GenerateBreedingDescription(SpeciesPopulationData populationData)
        {
            return $"Reproductive success has dropped to {populationData.reproductiveSuccess:P1}. " +
                   $"Average breeding age has increased to {populationData.breedingAge:F1} indicating breeding crisis.";
        }

        /// <summary>
        /// Generates description for juvenile mortality emergency
        /// </summary>
        public string GenerateJuvenileDescription(SpeciesPopulationData populationData)
        {
            return $"Juvenile survival rate has fallen to {populationData.juvenileSurvivalRate:P1}. " +
                   $"Critical intervention needed to protect young individuals.";
        }

        /// <summary>
        /// Generates description for ecosystem collapse emergency
        /// </summary>
        public string GenerateEcosystemDescription(EcosystemData ecosystemData, EcosystemHealth health)
        {
            return $"Ecosystem health has deteriorated to {health.overallHealth:P1}. " +
                   $"Multiple species and ecological processes are at risk.";
        }

        /// <summary>
        /// Generates description for genetic bottleneck emergency
        /// </summary>
        public string GenerateGeneticDescription(SpeciesPopulationData populationData, in CreatureGeneticsComponent genetics, float diversity)
        {
            return $"Genetic diversity has dropped to critical levels ({diversity:P1}). " +
                   $"Population of {populationData.currentPopulation} shows signs of inbreeding.";
        }

        /// <summary>
        /// Generates description for habitat destruction emergency
        /// </summary>
        public string GenerateHabitatDescription(EcosystemData ecosystemData)
        {
            return $"Habitat is being destroyed at {ecosystemData.habitatLossRate:P1} rate. " +
                   $"Critical habitat protection measures needed immediately.";
        }

        /// <summary>
        /// Generates description for disease outbreak emergency
        /// </summary>
        public string GenerateDiseaseDescription(SpeciesPopulationData populationData)
        {
            return $"Disease outbreak affecting {populationData.diseasePrevalence:P1} of population. " +
                   $"Quarantine and treatment protocols must be implemented.";
        }

        /// <summary>
        /// Generates description for climate emergency
        /// </summary>
        public string GenerateClimateDescription(EcosystemData ecosystemData)
        {
            return $"Climate stress level at {ecosystemData.climateStressLevel:P1}. " +
                   $"Ecosystem adaptation strategies urgently needed.";
        }

        /// <summary>
        /// Gets potential consequences for population collapse
        /// </summary>
        public string[] GetPopulationConsequences(SpeciesPopulationData populationData)
        {
            return new[]
            {
                "Species extinction within months",
                "Loss of genetic diversity",
                "Ecosystem function disruption",
                "Cascading effects on food web"
            };
        }

        /// <summary>
        /// Gets potential consequences for breeding failure
        /// </summary>
        public string[] GetBreedingConsequences(SpeciesPopulationData populationData)
        {
            return new[]
            {
                "Population collapse within 2-3 generations",
                "Increased inbreeding depression",
                "Loss of reproductive fitness",
                "Eventual species extinction"
            };
        }

        /// <summary>
        /// Gets potential consequences for juvenile mortality
        /// </summary>
        public string[] GetJuvenileConsequences(SpeciesPopulationData populationData)
        {
            return new[]
            {
                "Population cannot replace itself",
                "Aging population structure",
                "Reduced genetic contribution from new generations",
                "Species decline accelerates"
            };
        }

        /// <summary>
        /// Gets potential consequences for ecosystem collapse
        /// </summary>
        public string[] GetEcosystemConsequences(EcosystemData ecosystemData, EcosystemHealth health)
        {
            return new[]
            {
                "Complete ecosystem collapse",
                "Loss of multiple species",
                "Breakdown of ecological services",
                "Irreversible environmental damage"
            };
        }

        /// <summary>
        /// Gets potential consequences for genetic bottleneck
        /// </summary>
        public string[] GetGeneticConsequences(SpeciesPopulationData populationData, float diversity)
        {
            return new[]
            {
                "Inbreeding depression",
                "Reduced disease resistance",
                "Loss of adaptive potential",
                "Evolutionary dead end"
            };
        }

        /// <summary>
        /// Gets potential consequences for habitat destruction
        /// </summary>
        public string[] GetHabitatConsequences(EcosystemData ecosystemData)
        {
            return new[]
            {
                "Complete habitat loss",
                "Species displacement",
                "Fragmented populations",
                "Reduced carrying capacity"
            };
        }

        /// <summary>
        /// Gets potential consequences for disease outbreak
        /// </summary>
        public string[] GetDiseaseConsequences(SpeciesPopulationData populationData)
        {
            return new[]
            {
                "Population-wide mortality",
                "Transmission to other species",
                "Weakened immune systems",
                "Secondary infection outbreaks"
            };
        }

        /// <summary>
        /// Gets potential consequences for climate emergency
        /// </summary>
        public string[] GetClimateConsequences(EcosystemData ecosystemData)
        {
            return new[]
            {
                "Habitat unsuitable for native species",
                "Mass species migration or extinction",
                "Ecosystem regime shift",
                "Permanent biodiversity loss"
            };
        }

        /// <summary>
        /// Gets recovery factors for successful conservation
        /// </summary>
        public string[] GetRecoveryFactors(SpeciesPopulationData populationData)
        {
            return new[]
            {
                "Timely intervention",
                "Community involvement",
                "Scientific monitoring",
                "Habitat protection measures",
                "Adequate funding"
            };
        }

        /// <summary>
        /// Gets success factors from emergency resolution
        /// </summary>
        public string[] GetEmergencySuccessFactors(ConservationEmergency emergency)
        {
            return new[]
            {
                "Rapid response time",
                "Coordinated player actions",
                "Effective conservation strategies",
                "Sustained monitoring",
                "Community engagement"
            };
        }
    }
}
