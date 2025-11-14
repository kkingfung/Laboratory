using System;
using Unity.Collections;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Ecosystem;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.ECS.Services
{
    /// <summary>
    /// Static utility service for creating emergency objects across different emergency types.
    /// Provides factory methods for constructing ConservationEmergency instances.
    /// Converted to static for zero-allocation performance optimization.
    /// </summary>
    public static class EmergencyCreationService
    {

        /// <summary>
        /// Creates a population collapse emergency
        /// </summary>
        public static ConservationEmergency CreatePopulationEmergency(
            EmergencyConservationConfig config,
            SpeciesPopulationData populationData,
            float currentTime)
        {
            string description = DescriptionGenerationService.GeneratePopulationDescription(populationData);
            string[] consequences = DescriptionGenerationService.GetPopulationConsequences(populationData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.PopulationCollapse,
                severity = SeverityCalculationService.CalculatePopulationSeverity(config, populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = config.GetEmergencyDuration(EmergencyType.PopulationCollapse),
                originalDuration = config.GetEmergencyDuration(EmergencyType.PopulationCollapse),
                requiredActionTypes = GetRequiredActionTypes(config, EmergencyType.PopulationCollapse),
                title = $"Critical Population Decline: {populationData.speciesName}",
                description = description,
                urgencyLevel = UrgencyCalculationService.CalculatePopulationUrgency(config, populationData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetPopulationSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a breeding failure emergency
        /// </summary>
        public static ConservationEmergency CreateBreedingEmergency(
            EmergencyConservationConfig config,
            SpeciesPopulationData populationData,
            float currentTime)
        {
            string description = DescriptionGenerationService.GenerateBreedingDescription(populationData);
            string[] consequences = DescriptionGenerationService.GetBreedingConsequences(populationData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.BreedingFailure,
                severity = SeverityCalculationService.CalculateBreedingSeverity(config, populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = config.GetEmergencyDuration(EmergencyType.BreedingFailure),
                originalDuration = config.GetEmergencyDuration(EmergencyType.BreedingFailure),
                requiredActionTypes = GetRequiredActionTypes(config, EmergencyType.BreedingFailure),
                title = $"Breeding Crisis: {populationData.speciesName}",
                description = description,
                urgencyLevel = UrgencyCalculationService.CalculateBreedingUrgency(config, populationData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetBreedingSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a juvenile mortality emergency
        /// </summary>
        public static ConservationEmergency CreateJuvenileEmergency(
            EmergencyConservationConfig config,
            SpeciesPopulationData populationData,
            float currentTime)
        {
            string description = DescriptionGenerationService.GenerateJuvenileDescription(populationData);
            string[] consequences = DescriptionGenerationService.GetJuvenileConsequences(populationData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.JuvenileMortality,
                severity = SeverityCalculationService.CalculateJuvenileSeverity(config, populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = config.GetEmergencyDuration(EmergencyType.JuvenileMortality),
                originalDuration = config.GetEmergencyDuration(EmergencyType.JuvenileMortality),
                requiredActionTypes = GetRequiredActionTypes(config, EmergencyType.JuvenileMortality),
                title = $"Juvenile Mortality Crisis: {populationData.speciesName}",
                description = description,
                urgencyLevel = UrgencyCalculationService.CalculateJuvenileUrgency(populationData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetJuvenileSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates an ecosystem collapse emergency
        /// </summary>
        public static ConservationEmergency CreateEcosystemEmergency(
            EmergencyConservationConfig config,
            EcosystemData ecosystemData,
            EcosystemHealth health,
            float currentTime)
        {
            string description = DescriptionGenerationService.GenerateEcosystemDescription(ecosystemData, health);
            string[] consequences = DescriptionGenerationService.GetEcosystemConsequences(ecosystemData, health);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.EcosystemCollapse,
                severity = SeverityCalculationService.CalculateEcosystemSeverity(config, health),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = config.GetEmergencyDuration(EmergencyType.EcosystemCollapse),
                originalDuration = config.GetEmergencyDuration(EmergencyType.EcosystemCollapse),
                requiredActionTypes = GetRequiredActionTypes(config, EmergencyType.EcosystemCollapse),
                title = $"Ecosystem Collapse Warning: {ecosystemData.ecosystemName}",
                description = description,
                urgencyLevel = UrgencyCalculationService.CalculateEcosystemUrgency(health),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetEcosystemSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a genetic bottleneck emergency
        /// </summary>
        public static ConservationEmergency CreateGeneticEmergency(
            EmergencyConservationConfig config,
            SpeciesPopulationData populationData,
            float diversity,
            float currentTime)
        {
            string description = DescriptionGenerationService.GenerateGeneticDescription(populationData, default, diversity);
            string[] consequences = DescriptionGenerationService.GetGeneticConsequences(populationData, diversity);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.GeneticBottleneck,
                severity = SeverityCalculationService.CalculateGeneticSeverity(config, diversity),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = config.GetEmergencyDuration(EmergencyType.GeneticBottleneck),
                originalDuration = config.GetEmergencyDuration(EmergencyType.GeneticBottleneck),
                requiredActionTypes = GetRequiredActionTypes(config, EmergencyType.GeneticBottleneck),
                title = $"Genetic Bottleneck Crisis: {populationData.speciesName}",
                description = description,
                urgencyLevel = UrgencyCalculationService.CalculateGeneticUrgency(diversity),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetGeneticSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a habitat destruction emergency
        /// </summary>
        public static ConservationEmergency CreateHabitatDestructionEmergency(
            EmergencyConservationConfig config,
            EcosystemData ecosystemData,
            float currentTime)
        {
            string description = DescriptionGenerationService.GenerateHabitatDescription(ecosystemData);
            string[] consequences = DescriptionGenerationService.GetHabitatConsequences(ecosystemData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.HabitatDestruction,
                severity = SeverityCalculationService.CalculateHabitatSeverity(config, ecosystemData),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = config.GetEmergencyDuration(EmergencyType.HabitatDestruction),
                originalDuration = config.GetEmergencyDuration(EmergencyType.HabitatDestruction),
                requiredActionTypes = GetRequiredActionTypes(config, EmergencyType.HabitatDestruction),
                title = $"Rapid Habitat Loss: {ecosystemData.ecosystemName}",
                description = description,
                urgencyLevel = UrgencyCalculationService.CalculateHabitatUrgency(ecosystemData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetHabitatSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a disease outbreak emergency
        /// </summary>
        public static ConservationEmergency CreateDiseaseEmergency(
            EmergencyConservationConfig config,
            SpeciesPopulationData populationData,
            float currentTime)
        {
            string description = DescriptionGenerationService.GenerateDiseaseDescription(populationData);
            string[] consequences = DescriptionGenerationService.GetDiseaseConsequences(populationData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.DiseaseOutbreak,
                severity = SeverityCalculationService.CalculateDiseaseSeverity(config, populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = config.GetEmergencyDuration(EmergencyType.DiseaseOutbreak),
                originalDuration = config.GetEmergencyDuration(EmergencyType.DiseaseOutbreak),
                requiredActionTypes = GetRequiredActionTypes(config, EmergencyType.DiseaseOutbreak),
                title = $"Disease Outbreak: {populationData.speciesName}",
                description = description,
                urgencyLevel = UrgencyCalculationService.CalculateDiseaseUrgency(populationData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetDiseaseSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a climate change emergency
        /// </summary>
        public static ConservationEmergency CreateClimateEmergency(
            EmergencyConservationConfig config,
            EcosystemData ecosystemData,
            float currentTime)
        {
            string description = DescriptionGenerationService.GenerateClimateDescription(ecosystemData);
            string[] consequences = DescriptionGenerationService.GetClimateConsequences(ecosystemData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.ClimateChange,
                severity = SeverityCalculationService.CalculateClimateSeverity(config, ecosystemData),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = config.GetEmergencyDuration(EmergencyType.ClimateChange),
                originalDuration = config.GetEmergencyDuration(EmergencyType.ClimateChange),
                requiredActionTypes = GetRequiredActionTypes(config, EmergencyType.ClimateChange),
                title = $"Climate Emergency: {ecosystemData.ecosystemName}",
                description = description,
                urgencyLevel = UrgencyCalculationService.CalculateClimateUrgency(ecosystemData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetClimateSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a food web disruption emergency
        /// </summary>
        public static ConservationEmergency CreateFoodWebEmergency(
            EmergencyConservationConfig config,
            EcosystemData ecosystemData,
            EcosystemHealth health,
            float currentTime)
        {
            string description = DescriptionGenerationService.GenerateFoodWebDescription(health);
            string[] consequences = DescriptionGenerationService.GetFoodWebConsequences(health);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.FoodWebDisruption,
                severity = SeverityCalculationService.CalculateFoodWebSeverity(config, health),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = config.GetEmergencyDuration(EmergencyType.FoodWebDisruption),
                originalDuration = config.GetEmergencyDuration(EmergencyType.FoodWebDisruption),
                requiredActionTypes = GetRequiredActionTypes(config, EmergencyType.FoodWebDisruption),
                title = $"Food Web Disruption: {ecosystemData.ecosystemName}",
                description = description,
                urgencyLevel = UrgencyCalculationService.CalculateFoodWebUrgency(health),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetFoodWebSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a habitat fragmentation emergency
        /// </summary>
        public static ConservationEmergency CreateHabitatFragmentationEmergency(
            EmergencyConservationConfig config,
            EcosystemData ecosystemData,
            EcosystemHealth health,
            float currentTime)
        {
            string description = DescriptionGenerationService.GenerateHabitatFragmentationDescription(health);
            string[] consequences = DescriptionGenerationService.GetHabitatFragmentationConsequences(health);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.HabitatFragmentation,
                severity = SeverityCalculationService.CalculateHabitatFragmentationSeverity(config, health),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = config.GetEmergencyDuration(EmergencyType.HabitatFragmentation),
                originalDuration = config.GetEmergencyDuration(EmergencyType.HabitatFragmentation),
                requiredActionTypes = GetRequiredActionTypes(config, EmergencyType.HabitatFragmentation),
                title = $"Habitat Fragmentation: {ecosystemData.ecosystemName}",
                description = description,
                urgencyLevel = UrgencyCalculationService.CalculateHabitatFragmentationUrgency(health),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetHabitatSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        #region Helper Methods

        private static int GenerateEmergencyId()
        {
            return UnityEngine.Random.Range(100000, 999999);
        }

        private static FixedList64Bytes<EmergencyActionType> GetRequiredActionTypes(EmergencyConservationConfig config, EmergencyType type)
        {
            var actions = config.GetRequiredActions(type);
            var actionTypes = new FixedList64Bytes<EmergencyActionType>();
            foreach (var action in actions)
            {
                actionTypes.Add(action.type);
            }
            return actionTypes;
        }

        private static FixedList64Bytes<FixedString64Bytes> ConvertStringArrayToFixedList(string[] strings)
        {
            var fixedList = new FixedList64Bytes<FixedString64Bytes>();
            if (strings != null)
            {
                foreach (var str in strings)
                {
                    if (fixedList.Length < fixedList.Capacity)
                    {
                        fixedList.Add(str);
                    }
                }
            }
            return fixedList;
        }

        private static FixedList32Bytes<RequirementType> GetPopulationSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.PopulationIncrease);
            requirementTypes.Add(RequirementType.ReproductiveSuccess);
            requirementTypes.Add(RequirementType.HabitatProtection);
            return requirementTypes;
        }

        private static FixedList32Bytes<RequirementType> GetBreedingSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.ReproductiveSuccess);
            requirementTypes.Add(RequirementType.HabitatQuality);
            requirementTypes.Add(RequirementType.PopulationManagement);
            return requirementTypes;
        }

        private static FixedList32Bytes<RequirementType> GetJuvenileSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.JuvenileSurvival);
            requirementTypes.Add(RequirementType.HabitatProtection);
            return requirementTypes;
        }

        private static FixedList32Bytes<RequirementType> GetEcosystemSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.EcosystemHealth);
            requirementTypes.Add(RequirementType.SpeciesDiversity);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            return requirementTypes;
        }

        private static FixedList32Bytes<RequirementType> GetGeneticSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.PopulationIncrease);
            requirementTypes.Add(RequirementType.BreedingManagement);
            return requirementTypes;
        }

        private static FixedList32Bytes<RequirementType> GetHabitatSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.HabitatProtection);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            return requirementTypes;
        }

        private static FixedList32Bytes<RequirementType> GetDiseaseSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.HealthMonitoring);
            requirementTypes.Add(RequirementType.QuarantineProtocol);
            return requirementTypes;
        }

        private static FixedList32Bytes<RequirementType> GetClimateSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.ClimateAdaptation);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            requirementTypes.Add(RequirementType.EcosystemResilience);
            return requirementTypes;
        }

        private static FixedList32Bytes<RequirementType> GetFoodWebSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.SpeciesDiversity);
            requirementTypes.Add(RequirementType.EcosystemHealth);
            return requirementTypes;
        }

        #endregion
    }
}
