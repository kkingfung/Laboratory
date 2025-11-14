using Laboratory.Chimera.Core;
using Laboratory.Chimera.Ecosystem;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.ECS.Services
{
    /// <summary>
    /// Static utility service for generating emergency descriptions and consequences.
    /// Provides consistent messaging across different emergency types.
    /// Converted to static for zero-allocation performance optimization.
    /// </summary>
    public static class DescriptionGenerationService
    {
        /// <summary>
        /// Generates description for population collapse emergency
        /// </summary>
        public static string GeneratePopulationDescription(SpeciesPopulationData populationData)
        {
            return $"Population has declined to {populationData.currentPopulation} individuals " +
                   $"(trend: {populationData.populationTrend:F2}). Immediate intervention required to prevent extinction.";
        }

        /// <summary>
        /// Generates description for breeding failure emergency
        /// </summary>
        public static string GenerateBreedingDescription(SpeciesPopulationData populationData)
        {
            return $"Reproductive success has dropped to {populationData.reproductiveSuccess:P1}. " +
                   $"Average breeding age has increased to {populationData.breedingAge:F1} indicating breeding crisis.";
        }

        /// <summary>
        /// Generates description for juvenile mortality emergency
        /// </summary>
        public static string GenerateJuvenileDescription(SpeciesPopulationData populationData)
        {
            return $"Juvenile survival rate has fallen to {populationData.juvenileSurvivalRate:P1}. " +
                   $"Critical intervention needed to protect young individuals.";
        }

        /// <summary>
        /// Generates description for ecosystem collapse emergency
        /// </summary>
        public static string GenerateEcosystemDescription(EcosystemData ecosystemData, EcosystemHealth health)
        {
            return $"Ecosystem health has deteriorated to {health.overallHealth:P1}. " +
                   $"Multiple species and ecological processes are at risk.";
        }

        /// <summary>
        /// Generates description for genetic bottleneck emergency
        /// </summary>
        public static string GenerateGeneticDescription(SpeciesPopulationData populationData, in CreatureGeneticsComponent genetics, float diversity)
        {
            return $"Genetic diversity has dropped to critical levels ({diversity:P1}). " +
                   $"Population of {populationData.currentPopulation} shows signs of inbreeding.";
        }

        /// <summary>
        /// Generates description for habitat destruction emergency
        /// </summary>
        public static string GenerateHabitatDescription(EcosystemData ecosystemData)
        {
            return $"Habitat is being destroyed at {ecosystemData.habitatLossRate:P1} rate. " +
                   $"Critical habitat protection measures needed immediately.";
        }

        /// <summary>
        /// Generates description for disease outbreak emergency
        /// </summary>
        public static string GenerateDiseaseDescription(SpeciesPopulationData populationData)
        {
            return $"Disease outbreak affecting {populationData.diseasePrevalence:P1} of population. " +
                   $"Quarantine and treatment protocols must be implemented.";
        }

        /// <summary>
        /// Generates description for climate emergency
        /// </summary>
        public static string GenerateClimateDescription(EcosystemData ecosystemData)
        {
            return $"Climate stress level at {ecosystemData.climateStressLevel:P1}. " +
                   $"Ecosystem adaptation strategies urgently needed.";
        }

        /// <summary>
        /// Generates description for food web disruption emergency
        /// </summary>
        public static string GenerateFoodWebDescription(EcosystemHealth health)
        {
            return $"Food web stability has declined critically to {health.foodWebStability:P1}. " +
                   $"Predator-prey relationships are breaking down, threatening ecosystem collapse.";
        }

        /// <summary>
        /// Generates description for habitat fragmentation emergency
        /// </summary>
        public static string GenerateHabitatFragmentationDescription(EcosystemHealth health)
        {
            return $"Habitat connectivity has degraded to {health.habitatConnectivity:P1}. " +
                   $"Population isolation increases inbreeding risk and limits gene flow.";
        }

        /// <summary>
        /// Gets potential consequences for population collapse
        /// </summary>
        public static string[] GetPopulationConsequences(SpeciesPopulationData populationData)
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
        public static string[] GetBreedingConsequences(SpeciesPopulationData populationData)
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
        public static string[] GetJuvenileConsequences(SpeciesPopulationData populationData)
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
        public static string[] GetEcosystemConsequences(EcosystemData ecosystemData, EcosystemHealth health)
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
        public static string[] GetGeneticConsequences(SpeciesPopulationData populationData, float diversity)
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
        public static string[] GetHabitatConsequences(EcosystemData ecosystemData)
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
        public static string[] GetDiseaseConsequences(SpeciesPopulationData populationData)
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
        public static string[] GetClimateConsequences(EcosystemData ecosystemData)
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
        /// Gets potential consequences for food web disruption
        /// </summary>
        public static string[] GetFoodWebConsequences(EcosystemHealth health)
        {
            return new[]
            {
                "Ecosystem collapse due to trophic cascade",
                "Species extinction cascades",
                "Loss of keystone species",
                "Irreversible food web breakdown"
            };
        }

        /// <summary>
        /// Gets potential consequences for habitat fragmentation
        /// </summary>
        public static string[] GetHabitatFragmentationConsequences(EcosystemHealth health)
        {
            return new[]
            {
                "Population isolation and genetic bottlenecks",
                "Reduced species dispersal and gene flow",
                "Edge effects degrading habitat quality",
                "Local extinction of area-sensitive species"
            };
        }

        /// <summary>
        /// Gets recovery factors for successful conservation
        /// </summary>
        public static string[] GetRecoveryFactors(SpeciesPopulationData populationData)
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
        public static string[] GetEmergencySuccessFactors(ConservationEmergency emergency)
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
