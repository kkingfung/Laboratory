using System;
using Unity.Collections;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Ecosystem;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.ECS.Services
{
    /// <summary>
    /// Service responsible for creating emergency objects across different emergency types.
    /// Provides factory methods for constructing ConservationEmergency instances.
    /// Extracted from EmergencyConservationSystem to improve maintainability.
    /// </summary>
    public class EmergencyCreationService
    {
        private readonly EmergencyConservationConfig _config;
        private readonly SeverityCalculationService _severityService;
        private readonly UrgencyCalculationService _urgencyService;
        private readonly DescriptionGenerationService _descriptionService;

        public EmergencyCreationService(
            EmergencyConservationConfig config,
            SeverityCalculationService severityService,
            UrgencyCalculationService urgencyService,
            DescriptionGenerationService descriptionService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _severityService = severityService ?? throw new ArgumentNullException(nameof(severityService));
            _urgencyService = urgencyService ?? throw new ArgumentNullException(nameof(urgencyService));
            _descriptionService = descriptionService ?? throw new ArgumentNullException(nameof(descriptionService));
        }

        /// <summary>
        /// Creates a population collapse emergency
        /// </summary>
        public ConservationEmergency CreatePopulationEmergency(SpeciesPopulationData populationData, float currentTime)
        {
            var (description, consequences) = _descriptionService.GeneratePopulationDescription(populationData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.PopulationCollapse,
                severity = _severityService.CalculatePopulationSeverity(populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.PopulationCollapse),
                originalDuration = _config.GetEmergencyDuration(EmergencyType.PopulationCollapse),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.PopulationCollapse),
                title = $"Critical Population Decline: {populationData.speciesName}",
                description = description,
                urgencyLevel = _urgencyService.CalculatePopulationUrgency(populationData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetPopulationSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a breeding failure emergency
        /// </summary>
        public ConservationEmergency CreateBreedingEmergency(SpeciesPopulationData populationData, float currentTime)
        {
            var (description, consequences) = _descriptionService.GenerateBreedingDescription(populationData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.BreedingFailure,
                severity = _severityService.CalculateBreedingSeverity(populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.BreedingFailure),
                originalDuration = _config.GetEmergencyDuration(EmergencyType.BreedingFailure),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.BreedingFailure),
                title = $"Breeding Crisis: {populationData.speciesName}",
                description = description,
                urgencyLevel = _urgencyService.CalculateBreedingUrgency(populationData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetBreedingSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a juvenile mortality emergency
        /// </summary>
        public ConservationEmergency CreateJuvenileEmergency(SpeciesPopulationData populationData, float currentTime)
        {
            var (description, consequences) = _descriptionService.GenerateJuvenileDescription(populationData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.JuvenileMortality,
                severity = _severityService.CalculateJuvenileSeverity(populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.JuvenileMortality),
                originalDuration = _config.GetEmergencyDuration(EmergencyType.JuvenileMortality),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.JuvenileMortality),
                title = $"Juvenile Mortality Crisis: {populationData.speciesName}",
                description = description,
                urgencyLevel = _urgencyService.CalculateJuvenileUrgency(populationData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetJuvenileSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates an ecosystem collapse emergency
        /// </summary>
        public ConservationEmergency CreateEcosystemEmergency(EcosystemData ecosystemData, EcosystemHealth health, float currentTime)
        {
            var (description, consequences) = _descriptionService.GenerateEcosystemDescription(ecosystemData, health);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.EcosystemCollapse,
                severity = _severityService.CalculateEcosystemSeverity(health),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.EcosystemCollapse),
                originalDuration = _config.GetEmergencyDuration(EmergencyType.EcosystemCollapse),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.EcosystemCollapse),
                title = $"Ecosystem Collapse Warning: {ecosystemData.ecosystemName}",
                description = description,
                urgencyLevel = _urgencyService.CalculateEcosystemUrgency(health),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetEcosystemSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a genetic bottleneck emergency
        /// </summary>
        public ConservationEmergency CreateGeneticEmergency(SpeciesPopulationData populationData, float diversity, float currentTime)
        {
            var (description, consequences) = _descriptionService.GenerateGeneticDescription(populationData, diversity);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.GeneticBottleneck,
                severity = _severityService.CalculateGeneticSeverity(diversity),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.GeneticBottleneck),
                originalDuration = _config.GetEmergencyDuration(EmergencyType.GeneticBottleneck),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.GeneticBottleneck),
                title = $"Genetic Bottleneck Crisis: {populationData.speciesName}",
                description = description,
                urgencyLevel = _urgencyService.CalculateGeneticUrgency(diversity),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetGeneticSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a habitat destruction emergency
        /// </summary>
        public ConservationEmergency CreateHabitatDestructionEmergency(EcosystemData ecosystemData, float currentTime)
        {
            var (description, consequences) = _descriptionService.GenerateHabitatDescription(ecosystemData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.HabitatDestruction,
                severity = _severityService.CalculateHabitatSeverity(ecosystemData),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.HabitatDestruction),
                originalDuration = _config.GetEmergencyDuration(EmergencyType.HabitatDestruction),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.HabitatDestruction),
                title = $"Rapid Habitat Loss: {ecosystemData.ecosystemName}",
                description = description,
                urgencyLevel = _urgencyService.CalculateHabitatUrgency(ecosystemData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetHabitatSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a disease outbreak emergency
        /// </summary>
        public ConservationEmergency CreateDiseaseEmergency(SpeciesPopulationData populationData, float currentTime)
        {
            var (description, consequences) = _descriptionService.GenerateDiseaseDescription(populationData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.DiseaseOutbreak,
                severity = _severityService.CalculateDiseaseSeverity(populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.DiseaseOutbreak),
                originalDuration = _config.GetEmergencyDuration(EmergencyType.DiseaseOutbreak),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.DiseaseOutbreak),
                title = $"Disease Outbreak: {populationData.speciesName}",
                description = description,
                urgencyLevel = _urgencyService.CalculateDiseaseUrgency(populationData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetDiseaseSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a climate change emergency
        /// </summary>
        public ConservationEmergency CreateClimateEmergency(EcosystemData ecosystemData, float currentTime)
        {
            var (description, consequences) = _descriptionService.GenerateClimateDescription(ecosystemData);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.ClimateChange,
                severity = _severityService.CalculateClimateSeverity(ecosystemData),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.ClimateChange),
                originalDuration = _config.GetEmergencyDuration(EmergencyType.ClimateChange),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.ClimateChange),
                title = $"Climate Emergency: {ecosystemData.ecosystemName}",
                description = description,
                urgencyLevel = _urgencyService.CalculateClimateUrgency(ecosystemData),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetClimateSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a food web disruption emergency
        /// </summary>
        public ConservationEmergency CreateFoodWebEmergency(EcosystemData ecosystemData, EcosystemHealth health, float currentTime)
        {
            var (description, consequences) = _descriptionService.GenerateFoodWebDescription(health);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.FoodWebDisruption,
                severity = _severityService.CalculateFoodWebSeverity(health),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.FoodWebDisruption),
                originalDuration = _config.GetEmergencyDuration(EmergencyType.FoodWebDisruption),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.FoodWebDisruption),
                title = $"Food Web Disruption: {ecosystemData.ecosystemName}",
                description = description,
                urgencyLevel = _urgencyService.CalculateFoodWebUrgency(health),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetFoodWebSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        /// <summary>
        /// Creates a habitat fragmentation emergency
        /// </summary>
        public ConservationEmergency CreateHabitatFragmentationEmergency(EcosystemData ecosystemData, EcosystemHealth health, float currentTime)
        {
            var (description, consequences) = _descriptionService.GenerateHabitatFragmentationDescription(health);

            return new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.HabitatFragmentation,
                severity = _severityService.CalculateHabitatSeverity(health),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.HabitatFragmentation),
                originalDuration = _config.GetEmergencyDuration(EmergencyType.HabitatFragmentation),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.HabitatFragmentation),
                title = $"Habitat Fragmentation: {ecosystemData.ecosystemName}",
                description = description,
                urgencyLevel = _urgencyService.CalculateHabitatFragmentationUrgency(health),
                potentialConsequences = ConvertStringArrayToFixedList(consequences),
                successRequirementTypes = GetHabitatSuccessRequirementTypes(),
                declaredAt = currentTime,
                hasEscalated = false
            };
        }

        #region Helper Methods

        private int GenerateEmergencyId()
        {
            return UnityEngine.Random.Range(100000, 999999);
        }

        private FixedList64Bytes<EmergencyActionType> GetRequiredActionTypes(EmergencyType type)
        {
            var actions = _config.GetRequiredActions(type);
            var actionTypes = new FixedList64Bytes<EmergencyActionType>();
            foreach (var action in actions)
            {
                actionTypes.Add(action.type);
            }
            return actionTypes;
        }

        private FixedList64Bytes<FixedString64Bytes> ConvertStringArrayToFixedList(string[] strings)
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

        private FixedList32Bytes<RequirementType> GetPopulationSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.PopulationIncrease);
            requirementTypes.Add(RequirementType.ReproductiveSuccess);
            requirementTypes.Add(RequirementType.HabitatProtection);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetBreedingSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.ReproductiveSuccess);
            requirementTypes.Add(RequirementType.HabitatQuality);
            requirementTypes.Add(RequirementType.PopulationManagement);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetJuvenileSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.JuvenileSurvival);
            requirementTypes.Add(RequirementType.HabitatProtection);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetEcosystemSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.EcosystemHealth);
            requirementTypes.Add(RequirementType.SpeciesDiversity);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetGeneticSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.PopulationIncrease);
            requirementTypes.Add(RequirementType.BreedingManagement);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetHabitatSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.HabitatProtection);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetDiseaseSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.HealthMonitoring);
            requirementTypes.Add(RequirementType.QuarantineProtocol);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetClimateSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.ClimateAdaptation);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            requirementTypes.Add(RequirementType.EcosystemResilience);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetFoodWebSuccessRequirementTypes()
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.SpeciesDiversity);
            requirementTypes.Add(RequirementType.EcosystemHealth);
            return requirementTypes;
        }

        #endregion
    }
}
