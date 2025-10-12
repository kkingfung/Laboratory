using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Ecosystem;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// Emergency conservation events system.
    /// Creates urgent scenarios requiring immediate action to save endangered species and ecosystems.
    /// </summary>
    public partial class EmergencyConservationSystem : SystemBase
    {
        private EmergencyConservationConfig _config;
        private EntityQuery _ecosystemsQuery;
        private EntityQuery _speciesPopulationsQuery;
        private EntityQuery _activeEmergenciesQuery;

        // Emergency tracking
        private List<ConservationEmergency> _activeEmergencies = new List<ConservationEmergency>();
        private Dictionary<int, EmergencyResponse> _playerResponses = new Dictionary<int, EmergencyResponse>();
        private float _emergencyCheckTimer = 0f;

        // Event system
        public static event Action<ConservationEmergency> OnEmergencyDeclared;
        public static event Action<ConservationEmergency, EmergencyOutcome> OnEmergencyResolved;
        public static event Action<int, EmergencyResponse> OnPlayerResponseRecorded;
        public static event Action<ConservationCrisis> OnCrisisEscalated;
        public static event Action<ConservationSuccess> OnConservationSuccess;

        protected override void OnCreate()
        {
            _config = Resources.Load<EmergencyConservationConfig>("Configs/EmergencyConservationConfig");
            if (_config == null)
            {
                UnityEngine.Debug.LogError("EmergencyConservationConfig not found in Resources/Configs/");
                return;
            }

            _ecosystemsQuery = GetEntityQuery(
                ComponentType.ReadWrite<EcosystemData>(),
                ComponentType.ReadOnly<EcosystemHealth>()
            );

            _speciesPopulationsQuery = GetEntityQuery(
                ComponentType.ReadWrite<SpeciesPopulationData>(),
                ComponentType.ReadOnly<CreatureGeneticsComponent>()
            );

            _activeEmergenciesQuery = GetEntityQuery(
                ComponentType.ReadWrite<EmergencyConservationData>()
            );
        }

        protected override void OnUpdate()
        {
            if (_config == null) return;

            float deltaTime = SystemAPI.Time.DeltaTime;

            // Check for new emergencies
            _emergencyCheckTimer += deltaTime;
            if (_emergencyCheckTimer >= _config.emergencyCheckInterval)
            {
                CheckForNewEmergencies();
                _emergencyCheckTimer = 0f;
            }

            // Update active emergencies
            UpdateActiveEmergencies(deltaTime);

            // Process player responses
            ProcessPlayerResponses(deltaTime);

            // Monitor ecosystem health
            MonitorEcosystemHealth();

            // Handle emergency escalations
            ProcessEmergencyEscalations(deltaTime);

            // Check for conservation successes
            CheckConservationSuccesses();
        }

        private void CheckForNewEmergencies()
        {
            // Check population-based emergencies
            CheckPopulationEmergencies();

            // Check ecosystem-based emergencies
            CheckEcosystemEmergencies();

            // Check genetic diversity emergencies
            CheckGeneticDiversityEmergencies();

            // Check habitat destruction emergencies
            CheckHabitatEmergencies();

            // Check disease outbreak emergencies
            CheckDiseaseEmergencies();

            // Check climate-related emergencies
            CheckClimateEmergencies();
        }

        private void CheckPopulationEmergencies()
        {
            Entities.WithAll<SpeciesPopulationData>().ForEach((Entity entity, ref SpeciesPopulationData populationData) =>
            {
                // Check for critical population decline
                if (populationData.currentPopulation <= _config.criticalPopulationThreshold &&
                    populationData.populationTrend < 0f &&
                    !HasActiveEmergency(populationData.speciesId, EmergencyType.PopulationCollapse))
                {
                    CreatePopulationEmergency(entity, populationData);
                }

                // Check for breeding failure
                if (populationData.reproductiveSuccess < _config.breedingFailureThreshold &&
                    populationData.breedingAge > _config.breedingAgeThreshold &&
                    !HasActiveEmergency(populationData.speciesId, EmergencyType.BreedingFailure))
                {
                    CreateBreedingEmergency(entity, populationData);
                }

                // Check for juvenile mortality crisis
                if (populationData.juvenileSurvivalRate < _config.juvenileSurvivalThreshold &&
                    !HasActiveEmergency(populationData.speciesId, EmergencyType.JuvenileMortality))
                {
                    CreateJuvenileEmergency(entity, populationData);
                }
            }).WithoutBurst().Run();
        }

        private void CheckEcosystemEmergencies()
        {
            foreach (var (ecosystemData, health, entity) in SystemAPI.Query<RefRW<EcosystemData>, RefRW<EcosystemHealth>>().WithEntityAccess())
            {
                // Check for ecosystem collapse
                if (health.ValueRO.overallHealth < _config.ecosystemCollapseThreshold &&
                    health.ValueRO.healthTrend < 0f &&
                    !HasActiveEmergency(ecosystemData.ValueRO.ecosystemId, EmergencyType.EcosystemCollapse))
                {
                    CreateEcosystemEmergency(entity, ecosystemData.ValueRO, health.ValueRO);
                }

                // Check for food web disruption
                if (health.ValueRO.foodWebStability < _config.foodWebStabilityThreshold &&
                    !HasActiveEmergency(ecosystemData.ValueRO.ecosystemId, EmergencyType.FoodWebDisruption))
                {
                    CreateFoodWebEmergency(entity, ecosystemData.ValueRO, health.ValueRO);
                }

                // Check for habitat fragmentation
                if (health.ValueRO.habitatConnectivity < _config.habitatConnectivityThreshold &&
                    !HasActiveEmergency(ecosystemData.ValueRO.ecosystemId, EmergencyType.HabitatFragmentation))
                {
                    CreateHabitatEmergency(entity, ecosystemData.ValueRO, health.ValueRO);
                }
            }
        }

        private void CheckGeneticDiversityEmergencies()
        {
            Entities.WithAll<SpeciesPopulationData, CreatureGeneticsComponent>().ForEach((Entity entity, ref SpeciesPopulationData populationData, in CreatureGeneticsComponent genetics) =>
            {
                // Calculate genetic diversity
                float geneticDiversity = CalculateGeneticDiversity(genetics, populationData);

                if (geneticDiversity < _config.geneticDiversityThreshold &&
                    !HasActiveEmergency(populationData.speciesId, EmergencyType.GeneticBottleneck))
                {
                    CreateGeneticEmergency(entity, populationData, genetics, geneticDiversity);
                }
            }).WithoutBurst().Run();
        }

        private void CheckHabitatEmergencies()
        {
            // Check for rapid habitat loss
            Entities.WithAll<EcosystemData>().ForEach((Entity entity, ref EcosystemData ecosystemData) =>
            {
                if (ecosystemData.habitatLossRate > _config.habitatLossRateThreshold &&
                    !HasActiveEmergency(ecosystemData.ecosystemId, EmergencyType.HabitatDestruction))
                {
                    CreateHabitatDestructionEmergency(entity, ecosystemData);
                }
            }).WithoutBurst().Run();
        }

        private void CheckDiseaseEmergencies()
        {
            Entities.WithAll<SpeciesPopulationData>().ForEach((Entity entity, ref SpeciesPopulationData populationData) =>
            {
                if (populationData.diseasePrevalence > _config.diseaseOutbreakThreshold &&
                    !HasActiveEmergency(populationData.speciesId, EmergencyType.DiseaseOutbreak))
                {
                    CreateDiseaseEmergency(entity, populationData);
                }
            }).WithoutBurst().Run();
        }

        private void CheckClimateEmergencies()
        {
            // Check for climate-related threats
            Entities.WithAll<EcosystemData>().ForEach((Entity entity, ref EcosystemData ecosystemData) =>
            {
                if (ecosystemData.climateStressLevel > _config.climateStressThreshold &&
                    !HasActiveEmergency(ecosystemData.ecosystemId, EmergencyType.ClimateChange))
                {
                    CreateClimateEmergency(entity, ecosystemData);
                }
            }).WithoutBurst().Run();
        }

        private void UpdateActiveEmergencies(float deltaTime)
        {
            for (int i = _activeEmergencies.Count - 1; i >= 0; i--)
            {
                var emergency = _activeEmergencies[i];
                emergency.timeRemaining -= deltaTime;

                // Update emergency severity based on current conditions
                UpdateEmergencySeverity(ref emergency);

                // Check if emergency should escalate
                if (ShouldEscalate(emergency))
                {
                    EscalateEmergency(ref emergency);
                }

                // Check if emergency is resolved or expired
                if (emergency.timeRemaining <= 0f || IsEmergencyResolved(emergency))
                {
                    ResolveEmergency(emergency);
                    _activeEmergencies.RemoveAt(i);
                }
                else
                {
                    _activeEmergencies[i] = emergency;
                }
            }
        }

        private void ProcessPlayerResponses(float deltaTime)
        {
            foreach (var kvp in _playerResponses.ToList())
            {
                var playerId = kvp.Key;
                var response = kvp.Value;

                response.timeInvested += deltaTime;

                // Process response effectiveness
                ProcessResponseEffectiveness(ref response, deltaTime);

                // Update response progress
                UpdateResponseProgress(ref response, deltaTime);

                // Check for response completion
                if (IsResponseComplete(response))
                {
                    CompletePlayerResponse(playerId, response);
                    _playerResponses.Remove(playerId);
                }
                else
                {
                    _playerResponses[playerId] = response;
                }
            }
        }

        private void MonitorEcosystemHealth()
        {
            Entities.WithAll<EcosystemHealth>().ForEach((Entity entity, ref EcosystemHealth health) =>
            {
                // Track health trends for early warning
                UpdateHealthTrends(ref health);

                // Check for early warning signs
                CheckEarlyWarningSigns(entity, health);
            }).WithoutBurst().Run();
        }

        private void ProcessEmergencyEscalations(float deltaTime)
        {
            foreach (var emergency in _activeEmergencies.Where(e => e.hasEscalated))
            {
                ProcessEscalatedEmergency(emergency, deltaTime);
            }
        }

        private void CheckConservationSuccesses()
        {
            // Check for successful conservation outcomes
            Entities.WithAll<SpeciesPopulationData>().ForEach((Entity entity, ref SpeciesPopulationData populationData) =>
            {
                if (populationData.wasEndangered &&
                    populationData.currentPopulation > _config.recoveryPopulationThreshold &&
                    populationData.populationTrend > 0f)
                {
                    CreateConservationSuccess(entity, populationData);
                }
            }).WithoutBurst().Run();
        }

        #region Emergency Creation Methods

        private void CreatePopulationEmergency(Entity entity, SpeciesPopulationData populationData)
        {
            var emergency = new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.PopulationCollapse,
                severity = CalculatePopulationSeverity(populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.PopulationCollapse),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.PopulationCollapse),
                title = $"Critical Population Decline: {populationData.speciesName}",
                description = GeneratePopulationDescription(populationData),
                urgencyLevel = CalculateConservationUrgencyLevel(populationData.currentPopulation, _config.criticalPopulationThreshold),
                potentialConsequences = ConvertStringArrayToFixedList(GetPopulationConsequences(populationData)),
                successRequirementTypes = GetSuccessRequirementTypes(populationData),
                declaredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddEmergency(emergency);
        }

        private void CreateBreedingEmergency(Entity entity, SpeciesPopulationData populationData)
        {
            var emergency = new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.BreedingFailure,
                severity = CalculateBreedingSeverity(populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.BreedingFailure),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.BreedingFailure),
                title = $"Breeding Crisis: {populationData.speciesName}",
                description = GenerateBreedingDescription(populationData),
                urgencyLevel = CalculateBreedingUrgency(populationData),
                potentialConsequences = ConvertStringArrayToFixedList(GetBreedingConsequences(populationData)),
                successRequirementTypes = GetBreedingSuccessRequirementTypes(populationData),
                declaredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddEmergency(emergency);
        }

        private void CreateJuvenileEmergency(Entity entity, SpeciesPopulationData populationData)
        {
            var emergency = new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.JuvenileMortality,
                severity = CalculateJuvenileSeverity(populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.JuvenileMortality),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.JuvenileMortality),
                title = $"Juvenile Mortality Crisis: {populationData.speciesName}",
                description = GenerateJuvenileDescription(populationData),
                urgencyLevel = CalculateJuvenileUrgency(populationData),
                potentialConsequences = ConvertStringArrayToFixedList(GetJuvenileConsequences(populationData)),
                successRequirementTypes = GetJuvenileSuccessRequirementTypes(populationData),
                declaredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddEmergency(emergency);
        }

        private void CreateEcosystemEmergency(Entity entity, EcosystemData ecosystemData, EcosystemHealth health)
        {
            var emergency = new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.EcosystemCollapse,
                severity = CalculateEcosystemSeverity(health),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.EcosystemCollapse),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.EcosystemCollapse),
                title = $"Ecosystem Collapse Warning: {ecosystemData.ecosystemName}",
                description = GenerateEcosystemDescription(ecosystemData, health),
                urgencyLevel = CalculateEcosystemUrgency(health),
                potentialConsequences = ConvertStringArrayToFixedList(GetEcosystemConsequences(ecosystemData, health)),
                successRequirementTypes = GetEcosystemSuccessRequirementTypes(ecosystemData, health),
                declaredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddEmergency(emergency);
        }

        private void CreateGeneticEmergency(Entity entity, SpeciesPopulationData populationData, in CreatureGeneticsComponent genetics, float diversity)
        {
            var emergency = new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.GeneticBottleneck,
                severity = CalculateGeneticSeverity(diversity),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.GeneticBottleneck),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.GeneticBottleneck),
                title = $"Genetic Bottleneck Crisis: {populationData.speciesName}",
                description = GenerateGeneticDescription(populationData, genetics, diversity),
                urgencyLevel = CalculateGeneticUrgency(diversity),
                potentialConsequences = ConvertStringArrayToFixedList(GetGeneticConsequences(populationData, diversity)),
                successRequirementTypes = GetGeneticSuccessRequirementTypes(populationData, diversity),
                declaredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddEmergency(emergency);
        }

        private void CreateHabitatDestructionEmergency(Entity entity, EcosystemData ecosystemData)
        {
            var emergency = new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.HabitatDestruction,
                severity = CalculateHabitatSeverity(ecosystemData),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.HabitatDestruction),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.HabitatDestruction),
                title = $"Rapid Habitat Loss: {ecosystemData.ecosystemName}",
                description = GenerateHabitatDescription(ecosystemData),
                urgencyLevel = CalculateHabitatUrgency(ecosystemData),
                potentialConsequences = ConvertStringArrayToFixedList(GetHabitatConsequences(ecosystemData)),
                successRequirementTypes = GetHabitatSuccessRequirementTypes(ecosystemData),
                declaredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddEmergency(emergency);
        }

        private void CreateDiseaseEmergency(Entity entity, SpeciesPopulationData populationData)
        {
            var emergency = new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.DiseaseOutbreak,
                severity = CalculateDiseaseSeverity(populationData),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.DiseaseOutbreak),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.DiseaseOutbreak),
                title = $"Disease Outbreak: {populationData.speciesName}",
                description = GenerateDiseaseDescription(populationData),
                urgencyLevel = CalculateDiseaseUrgency(populationData),
                potentialConsequences = ConvertStringArrayToFixedList(GetDiseaseConsequences(populationData)),
                successRequirementTypes = GetDiseaseSuccessRequirementTypes(populationData),
                declaredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddEmergency(emergency);
        }

        private void CreateClimateEmergency(Entity entity, EcosystemData ecosystemData)
        {
            var emergency = new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.ClimateChange,
                severity = CalculateClimateSeverity(ecosystemData),
                affectedEcosystemId = ecosystemData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.ClimateChange),
                requiredActionTypes = GetRequiredActionTypes(EmergencyType.ClimateChange),
                title = $"Climate Emergency: {ecosystemData.ecosystemName}",
                description = GenerateClimateDescription(ecosystemData),
                urgencyLevel = CalculateClimateUrgency(ecosystemData),
                potentialConsequences = ConvertStringArrayToFixedList(GetClimateConsequences(ecosystemData)),
                successRequirementTypes = GetClimateSuccessRequirementTypes(ecosystemData),
                declaredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddEmergency(emergency);
        }

        #endregion

        #region Helper Methods

        private void AddEmergency(ConservationEmergency emergency)
        {
            _activeEmergencies.Add(emergency);
            OnEmergencyDeclared?.Invoke(emergency);

            UnityEngine.Debug.Log($"Conservation Emergency Declared: {emergency.title} (Severity: {emergency.severity})");
        }

        private bool HasActiveEmergency(int targetId, EmergencyType type)
        {
            return _activeEmergencies.Any(e =>
                e.type == type &&
                (e.affectedSpeciesId == targetId || e.affectedEcosystemId == targetId));
        }

        private int GenerateEmergencyId()
        {
            return UnityEngine.Random.Range(100000, 999999);
        }

        private float CalculateGeneticDiversity(in CreatureGeneticsComponent genetics, SpeciesPopulationData populationData)
        {
            // Calculate genetic diversity based on trait variety and population size
            float traitDiversity = genetics.ActiveGeneCount;
            float populationFactor = Mathf.Log(populationData.currentPopulation + 1f) / 10f;
            return (traitDiversity * populationFactor) / 20f; // Normalized to 0-1
        }

        private EmergencySeverity CalculatePopulationSeverity(SpeciesPopulationData populationData)
        {
            float ratio = populationData.currentPopulation / (float)_config.criticalPopulationThreshold;
            if (ratio <= 0.1f) return EmergencySeverity.Critical;
            if (ratio <= 0.3f) return EmergencySeverity.Severe;
            if (ratio <= 0.6f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        private EmergencySeverity CalculateBreedingSeverity(SpeciesPopulationData populationData)
        {
            if (populationData.reproductiveSuccess <= 0.1f) return EmergencySeverity.Critical;
            if (populationData.reproductiveSuccess <= 0.3f) return EmergencySeverity.Severe;
            if (populationData.reproductiveSuccess <= 0.5f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        private EmergencySeverity CalculateJuvenileSeverity(SpeciesPopulationData populationData)
        {
            if (populationData.juvenileSurvivalRate <= 0.2f) return EmergencySeverity.Critical;
            if (populationData.juvenileSurvivalRate <= 0.4f) return EmergencySeverity.Severe;
            if (populationData.juvenileSurvivalRate <= 0.6f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        private EmergencySeverity CalculateEcosystemSeverity(EcosystemHealth health)
        {
            if (health.overallHealth <= 0.2f) return EmergencySeverity.Critical;
            if (health.overallHealth <= 0.4f) return EmergencySeverity.Severe;
            if (health.overallHealth <= 0.6f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        private EmergencySeverity CalculateGeneticSeverity(float diversity)
        {
            if (diversity <= 0.1f) return EmergencySeverity.Critical;
            if (diversity <= 0.3f) return EmergencySeverity.Severe;
            if (diversity <= 0.5f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        private EmergencySeverity CalculateHabitatSeverity(EcosystemData ecosystemData)
        {
            if (ecosystemData.habitatLossRate >= 0.8f) return EmergencySeverity.Critical;
            if (ecosystemData.habitatLossRate >= 0.6f) return EmergencySeverity.Severe;
            if (ecosystemData.habitatLossRate >= 0.4f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        private EmergencySeverity CalculateDiseaseSeverity(SpeciesPopulationData populationData)
        {
            if (populationData.diseasePrevalence >= 0.8f) return EmergencySeverity.Critical;
            if (populationData.diseasePrevalence >= 0.6f) return EmergencySeverity.Severe;
            if (populationData.diseasePrevalence >= 0.4f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        private EmergencySeverity CalculateClimateSeverity(EcosystemData ecosystemData)
        {
            if (ecosystemData.climateStressLevel >= 0.8f) return EmergencySeverity.Critical;
            if (ecosystemData.climateStressLevel >= 0.6f) return EmergencySeverity.Severe;
            if (ecosystemData.climateStressLevel >= 0.4f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        private ConservationUrgencyLevel CalculateConservationUrgencyLevel(float currentValue, float threshold)
        {
            float ratio = currentValue / threshold;
            if (ratio <= 0.1f) return ConservationUrgencyLevel.Immediate;
            if (ratio <= 0.3f) return ConservationUrgencyLevel.Urgent;
            if (ratio <= 0.6f) return ConservationUrgencyLevel.Important;
            return ConservationUrgencyLevel.Moderate;
        }

        private ConservationUrgencyLevel CalculateBreedingUrgency(SpeciesPopulationData populationData)
        {
            if (populationData.reproductiveSuccess <= 0.1f && populationData.breedingAge > _config.breedingAgeThreshold * 1.5f)
                return ConservationUrgencyLevel.Immediate;
            if (populationData.reproductiveSuccess <= 0.3f)
                return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        private ConservationUrgencyLevel CalculateJuvenileUrgency(SpeciesPopulationData populationData)
        {
            if (populationData.juvenileSurvivalRate <= 0.2f)
                return ConservationUrgencyLevel.Immediate;
            if (populationData.juvenileSurvivalRate <= 0.4f)
                return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        private ConservationUrgencyLevel CalculateEcosystemUrgency(EcosystemHealth health)
        {
            if (health.overallHealth <= 0.2f && health.healthTrend < -0.1f)
                return ConservationUrgencyLevel.Immediate;
            if (health.overallHealth <= 0.4f)
                return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        private ConservationUrgencyLevel CalculateGeneticUrgency(float diversity)
        {
            if (diversity <= 0.1f) return ConservationUrgencyLevel.Immediate;
            if (diversity <= 0.3f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        private ConservationUrgencyLevel CalculateHabitatUrgency(EcosystemData ecosystemData)
        {
            if (ecosystemData.habitatLossRate >= 0.8f) return ConservationUrgencyLevel.Immediate;
            if (ecosystemData.habitatLossRate >= 0.6f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        private ConservationUrgencyLevel CalculateDiseaseUrgency(SpeciesPopulationData populationData)
        {
            if (populationData.diseasePrevalence >= 0.8f) return ConservationUrgencyLevel.Immediate;
            if (populationData.diseasePrevalence >= 0.6f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        private ConservationUrgencyLevel CalculateClimateUrgency(EcosystemData ecosystemData)
        {
            if (ecosystemData.climateStressLevel >= 0.8f) return ConservationUrgencyLevel.Immediate;
            if (ecosystemData.climateStressLevel >= 0.6f) return ConservationUrgencyLevel.Urgent;
            return ConservationUrgencyLevel.Important;
        }

        private EmergencyAction[] GetRequiredActions(EmergencyType type)
        {
            var ecosystemActions = _config.GetRequiredActions(type);
            var ecsActions = new EmergencyAction[ecosystemActions.Length];

            for (int i = 0; i < ecosystemActions.Length; i++)
            {
                ecsActions[i] = ConvertToECSEmergencyAction(ecosystemActions[i]);
            }

            return ecsActions;
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
            foreach (var str in strings)
            {
                if (fixedList.Length < fixedList.Capacity)
                {
                    fixedList.Add(str);
                }
            }
            return fixedList;
        }

        private FixedList32Bytes<RequirementType> GetSuccessRequirementTypes(SpeciesPopulationData populationData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.PopulationIncrease);
            requirementTypes.Add(RequirementType.ReproductiveSuccess);
            requirementTypes.Add(RequirementType.HabitatProtection);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetBreedingSuccessRequirementTypes(SpeciesPopulationData populationData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.ReproductiveSuccess);
            requirementTypes.Add(RequirementType.HabitatQuality);
            requirementTypes.Add(RequirementType.PopulationManagement);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetJuvenileSuccessRequirementTypes(SpeciesPopulationData populationData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.JuvenileSurvival);
            requirementTypes.Add(RequirementType.HabitatProtection);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetEcosystemSuccessRequirementTypes(EcosystemData ecosystemData, EcosystemHealth health)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.EcosystemHealth);
            requirementTypes.Add(RequirementType.SpeciesDiversity);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetGeneticSuccessRequirementTypes(SpeciesPopulationData populationData, float diversity)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.PopulationIncrease);
            requirementTypes.Add(RequirementType.BreedingManagement);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetHabitatSuccessRequirementTypes(EcosystemData ecosystemData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.HabitatProtection);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetDiseaseSuccessRequirementTypes(SpeciesPopulationData populationData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.HealthMonitoring);
            requirementTypes.Add(RequirementType.QuarantineProtocol);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetClimateSuccessRequirementTypes(EcosystemData ecosystemData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.ClimateAdaptation);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            requirementTypes.Add(RequirementType.EcosystemResilience);
            return requirementTypes;
        }

        private FixedList32Bytes<RequirementType> GetEscalatedRequirementTypes(ConservationEmergency emergency)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.PopulationTarget);
            requirementTypes.Add(RequirementType.HabitatRestoration);
            requirementTypes.Add(RequirementType.ThreatReduction);
            return requirementTypes;
        }

        private string GeneratePopulationDescription(SpeciesPopulationData populationData)
        {
            return $"Population has declined to {populationData.currentPopulation} individuals " +
                   $"(trend: {populationData.populationTrend:F2}). Immediate intervention required to prevent extinction.";
        }

        private string GenerateBreedingDescription(SpeciesPopulationData populationData)
        {
            return $"Reproductive success has dropped to {populationData.reproductiveSuccess:P1}. " +
                   $"Average breeding age has increased to {populationData.breedingAge:F1} indicating breeding crisis.";
        }

        private string GenerateJuvenileDescription(SpeciesPopulationData populationData)
        {
            return $"Juvenile survival rate has fallen to {populationData.juvenileSurvivalRate:P1}. " +
                   $"Critical intervention needed to protect young individuals.";
        }

        private string GenerateEcosystemDescription(EcosystemData ecosystemData, EcosystemHealth health)
        {
            return $"Ecosystem health has deteriorated to {health.overallHealth:P1}. " +
                   $"Multiple species and ecological processes are at risk.";
        }

        private string GenerateGeneticDescription(SpeciesPopulationData populationData, in CreatureGeneticsComponent genetics, float diversity)
        {
            return $"Genetic diversity has dropped to critical levels ({diversity:P1}). " +
                   $"Population of {populationData.currentPopulation} shows signs of inbreeding.";
        }

        private string GenerateHabitatDescription(EcosystemData ecosystemData)
        {
            return $"Habitat is being destroyed at {ecosystemData.habitatLossRate:P1} rate. " +
                   $"Critical habitat protection measures needed immediately.";
        }

        private string GenerateDiseaseDescription(SpeciesPopulationData populationData)
        {
            return $"Disease outbreak affecting {populationData.diseasePrevalence:P1} of population. " +
                   $"Quarantine and treatment protocols must be implemented.";
        }

        private string GenerateClimateDescription(EcosystemData ecosystemData)
        {
            return $"Climate stress level at {ecosystemData.climateStressLevel:P1}. " +
                   $"Ecosystem adaptation strategies urgently needed.";
        }

        private string[] GetPopulationConsequences(SpeciesPopulationData populationData)
        {
            return new[]
            {
                "Species extinction within months",
                "Loss of genetic diversity",
                "Ecosystem function disruption",
                "Cascading effects on food web"
            };
        }

        private string[] GetBreedingConsequences(SpeciesPopulationData populationData)
        {
            return new[]
            {
                "Population collapse within 2-3 generations",
                "Increased inbreeding depression",
                "Loss of reproductive fitness",
                "Eventual species extinction"
            };
        }

        private string[] GetJuvenileConsequences(SpeciesPopulationData populationData)
        {
            return new[]
            {
                "Population cannot replace itself",
                "Aging population structure",
                "Reduced genetic contribution from new generations",
                "Species decline accelerates"
            };
        }

        private string[] GetEcosystemConsequences(EcosystemData ecosystemData, EcosystemHealth health)
        {
            return new[]
            {
                "Complete ecosystem collapse",
                "Loss of multiple species",
                "Breakdown of ecological services",
                "Irreversible environmental damage"
            };
        }

        private string[] GetGeneticConsequences(SpeciesPopulationData populationData, float diversity)
        {
            return new[]
            {
                "Inbreeding depression",
                "Reduced disease resistance",
                "Loss of adaptive potential",
                "Evolutionary dead end"
            };
        }

        private string[] GetHabitatConsequences(EcosystemData ecosystemData)
        {
            return new[]
            {
                "Complete habitat loss",
                "Species displacement",
                "Fragmented populations",
                "Reduced carrying capacity"
            };
        }

        private string[] GetDiseaseConsequences(SpeciesPopulationData populationData)
        {
            return new[]
            {
                "Population-wide mortality",
                "Transmission to other species",
                "Weakened immune systems",
                "Secondary infection outbreaks"
            };
        }

        private string[] GetClimateConsequences(EcosystemData ecosystemData)
        {
            return new[]
            {
                "Habitat unsuitable for native species",
                "Migration of invasive species",
                "Disrupted seasonal cycles",
                "Altered precipitation patterns"
            };
        }


        private void UpdateEmergencySeverity(ref ConservationEmergency emergency)
        {
            // Update severity based on current conditions
            // This would check current population/ecosystem status
        }

        private bool ShouldEscalate(ConservationEmergency emergency)
        {
            return emergency.timeRemaining < emergency.originalDuration * 0.3f &&
                   emergency.severity >= EmergencySeverity.Severe &&
                   !emergency.hasEscalated;
        }

        private void EscalateEmergency(ref ConservationEmergency emergency)
        {
            emergency.hasEscalated = true;
            emergency.severity = EmergencySeverity.Critical;
            emergency.urgencyLevel = ConservationUrgencyLevel.Immediate;

            var crisis = new ConservationCrisis
            {
                originalEmergency = emergency,
                escalationReason = "Time running out with insufficient response",
                newRequirementTypes = GetEscalatedRequirementTypes(emergency),
                timestamp = (float)SystemAPI.Time.ElapsedTime
            };

            OnCrisisEscalated?.Invoke(crisis);
        }

        private ConservationRequirement[] GetEscalatedRequirements(ConservationEmergency emergency)
        {
            // Return more stringent requirements for escalated emergencies
            return new ConservationRequirement[0]; // Simplified for now
        }

        private bool IsEmergencyResolved(ConservationEmergency emergency)
        {
            // Check if emergency conditions have been met
            return CheckSuccessRequirementTypes(emergency);
        }

        private bool CheckSuccessRequirementTypes(ConservationEmergency emergency)
        {
            // Check if all success requirements have been met
            foreach (var requirement in emergency.successRequirementTypes)
            {
                if (!IsRequirementTypeMet(requirement, emergency))
                    return false;
            }
            return true;
        }

        private bool IsRequirementTypeMet(RequirementType requirement, ConservationEmergency emergency)
        {
            // Check specific requirement types
            switch (requirement)
            {
                case RequirementType.PopulationIncrease:
                    return CheckPopulationRequirement(emergency);
                case RequirementType.ReproductiveSuccess:
                    return CheckReproductiveRequirement(emergency);
                case RequirementType.HabitatProtection:
                    return CheckHabitatRequirement(emergency);
                default:
                    return false;
            }
        }

        private bool CheckPopulationRequirement(ConservationEmergency emergency)
        {
            // Check current population against requirement
            // This would query current population data
            return false; // Simplified for example
        }

        private bool CheckReproductiveRequirement(ConservationEmergency emergency)
        {
            // Check current reproductive success
            return false; // Simplified for example
        }

        private bool CheckHabitatRequirement(ConservationEmergency emergency)
        {
            // Check current habitat status
            return false; // Simplified for example
        }

        private void ResolveEmergency(ConservationEmergency emergency)
        {
            var outcome = new EmergencyOutcome
            {
                emergency = emergency,
                isSuccessful = IsEmergencyResolved(emergency),
                finalStatus = GetFinalStatus(emergency),
                playerContributions = ConvertPlayerContributionsToFixedList(GetPlayerContributions(emergency)),
                timestamp = (float)SystemAPI.Time.ElapsedTime
            };

            OnEmergencyResolved?.Invoke(emergency, outcome);

            if (outcome.isSuccessful)
            {
                CreateConservationSuccessFromEmergency(emergency);
            }
        }

        private EmergencyStatus GetFinalStatus(ConservationEmergency emergency)
        {
            if (IsEmergencyResolved(emergency))
                return EmergencyStatus.Resolved;
            if (emergency.hasEscalated)
                return EmergencyStatus.Failed;
            return EmergencyStatus.TimeExpired;
        }

        private Dictionary<int, float> GetPlayerContributions(ConservationEmergency emergency)
        {
            // Calculate player contributions to this emergency
            return new Dictionary<int, float>();
        }

        private void ProcessResponseEffectiveness(ref EmergencyResponse response, float deltaTime)
        {
            // Calculate response effectiveness based on action type and timing
            response.effectiveness += CalculateEffectivenessGain(response, deltaTime);
        }

        private float CalculateEffectivenessGain(EmergencyResponse response, float deltaTime)
        {
            return response.resourcesCommitted * deltaTime * _config.responseEffectivenessMultiplier;
        }

        private void UpdateResponseProgress(ref EmergencyResponse response, float deltaTime)
        {
            response.progress += response.effectiveness * deltaTime;
            response.progress = Mathf.Clamp01(response.progress);
        }

        private bool IsResponseComplete(EmergencyResponse response)
        {
            return response.progress >= 1f || response.timeInvested >= response.maxDuration;
        }

        private void CompletePlayerResponse(int playerId, EmergencyResponse response)
        {
            OnPlayerResponseRecorded?.Invoke(playerId, response);

            // Apply response effects to emergency
            ApplyResponseEffects(response);
        }

        private void ApplyResponseEffects(EmergencyResponse response)
        {
            // Apply the effects of the player response to the related emergency
            var emergency = _activeEmergencies.FirstOrDefault(e => e.emergencyId == response.emergencyId);
            if (emergency.emergencyId != 0)
            {
                // Apply positive effects based on response type and effectiveness
                ApplyResponseToEmergency(response, ref emergency);
            }
        }

        private void ApplyResponseToEmergency(EmergencyResponse response, ref ConservationEmergency emergency)
        {
            // Apply specific response effects to emergency
            switch (response.actionType)
            {
                case EmergencyActionType.PopulationSupport:
                    // Improve population metrics
                    break;
                case EmergencyActionType.HabitatProtection:
                    // Improve habitat conditions
                    break;
                case EmergencyActionType.BreedingProgram:
                    // Improve reproductive success
                    break;
                case EmergencyActionType.DiseaseControl:
                    // Reduce disease prevalence
                    break;
            }
        }

        private void UpdateHealthTrends(ref EcosystemHealth health)
        {
            // Update health trend tracking for early warning
            health.healthHistory[health.historyIndex] = health.overallHealth;
            health.historyIndex = (health.historyIndex + 1) % health.healthHistory.Length;

            // Calculate trend
            float oldHealth = health.healthHistory[health.historyIndex];
            health.healthTrend = health.overallHealth - oldHealth;
        }

        private void CheckEarlyWarningSigns(Entity entity, EcosystemHealth health)
        {
            // Check for patterns that indicate impending emergencies
            if (health.healthTrend < -0.05f && health.overallHealth < 0.8f)
            {
                // Issue early warning
                IssueEarlyWarning(entity, health);
            }
        }

        private void IssueEarlyWarning(Entity entity, EcosystemHealth health)
        {
            // Issue early warning notification
            UnityEngine.Debug.Log($"Early Warning: Ecosystem health declining rapidly (Current: {health.overallHealth:P1}, Trend: {health.healthTrend:F3})");
        }

        private void ProcessEscalatedEmergency(ConservationEmergency emergency, float deltaTime)
        {
            // Process escalated emergencies with more severe consequences
            // This might involve faster deterioration or additional requirements
        }

        private void CreateConservationSuccess(Entity entity, SpeciesPopulationData populationData)
        {
            var success = new ConservationSuccess
            {
                successType = ConservationSuccessType.SpeciesRecovery,
                speciesId = populationData.speciesId,
                achievementDescription = $"Successfully recovered {populationData.speciesName} from endangered status",
                finalPopulation = populationData.currentPopulation,
                recoveryTime = CalculateRecoveryTime(populationData),
                contributingFactors = ConvertStringArrayToFixedList128(GetRecoveryFactors(populationData)),
                timestamp = (float)SystemAPI.Time.ElapsedTime
            };

            OnConservationSuccess?.Invoke(success);
            populationData.wasEndangered = false; // Reset flag
        }

        private void CreateConservationSuccessFromEmergency(ConservationEmergency emergency)
        {
            var success = new ConservationSuccess
            {
                successType = GetSuccessTypeFromEmergency(emergency.type),
                emergencyId = emergency.emergencyId,
                achievementDescription = $"Successfully resolved {emergency.title}",
                contributingFactors = ConvertStringArrayToFixedList128(GetEmergencySuccessFactors(emergency)),
                timestamp = (float)SystemAPI.Time.ElapsedTime
            };

            OnConservationSuccess?.Invoke(success);
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
                default:
                    return ConservationSuccessType.General;
            }
        }

        private float CalculateRecoveryTime(SpeciesPopulationData populationData)
        {
            // Calculate time since species was first endangered
            return (float)(SystemAPI.Time.ElapsedTime - populationData.endangeredSince);
        }

        private string[] GetRecoveryFactors(SpeciesPopulationData populationData)
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

        private EmergencyAction ConvertToECSEmergencyAction(Laboratory.Chimera.Ecosystem.EmergencyAction ecosystemAction)
        {
            return new EmergencyAction
            {
                type = (EmergencyActionType)ecosystemAction.type,
                name = ecosystemAction.name,
                description = ecosystemAction.description,
                resourceRequirement = ecosystemAction.resourceRequirement,
                timeRequirement = ecosystemAction.timeRequirement,
                effectiveness = ecosystemAction.effectiveness,
                prerequisites = ConvertStringArrayToFixedList32(ecosystemAction.prerequisites)
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

        private FixedList32Bytes<FixedString64Bytes> ConvertStringArrayToFixedList32(string[] strings)
        {
            var fixedList = new FixedList32Bytes<FixedString64Bytes>();
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

        #region Public Methods for Player Interaction

        public void StartEmergencyResponse(int playerId, int emergencyId, EmergencyActionType actionType, float resourceCommitment)
        {
            var emergency = _activeEmergencies.FirstOrDefault(e => e.emergencyId == emergencyId);
            if (emergency.emergencyId == 0)
            {
                UnityEngine.Debug.LogWarning($"Emergency {emergencyId} not found");
                return;
            }

            var response = new EmergencyResponse
            {
                playerId = playerId,
                emergencyId = emergencyId,
                actionType = actionType,
                resourcesCommitted = resourceCommitment,
                startTime = (float)SystemAPI.Time.ElapsedTime,
                maxDuration = _config.GetActionDuration(actionType),
                progress = 0f,
                effectiveness = 0f,
                timeInvested = 0f
            };

            _playerResponses[playerId] = response;
            UnityEngine.Debug.Log($"Player {playerId} started response to emergency {emergencyId}");
        }

        public ConservationEmergency[] GetActiveEmergencies()
        {
            return _activeEmergencies.ToArray();
        }

        public ConservationEmergency[] GetEmergenciesByType(EmergencyType type)
        {
            return _activeEmergencies.Where(e => e.type == type).ToArray();
        }

        public ConservationEmergency[] GetEmergenciesBySeverity(EmergencySeverity severity)
        {
            return _activeEmergencies.Where(e => e.severity == severity).ToArray();
        }

        public EmergencyResponse GetPlayerResponse(int playerId)
        {
            return _playerResponses.ContainsKey(playerId) ? _playerResponses[playerId] : default;
        }

        #endregion

        #region Missing Methods

        private void CreateFoodWebEmergency(Entity entity, EcosystemData ecosystemData, EcosystemHealth health)
        {
            // Create food web disruption emergency
            var emergency = new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.FoodWebDisruption,
                affectedEcosystemId = ecosystemData.ecosystemId,
                severity = CalculateFoodWebSeverity(health),
                urgencyLevel = ConservationUrgencyLevel.High,
                declaredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddEmergency(emergency);
        }

        private void CreateHabitatEmergency(Entity entity, EcosystemData ecosystemData, EcosystemHealth health)
        {
            // Create habitat fragmentation emergency
            var emergency = new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.HabitatFragmentation,
                affectedEcosystemId = ecosystemData.ecosystemId,
                severity = CalculateHabitatSeverity(health),
                urgencyLevel = ConservationUrgencyLevel.Critical,
                declaredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddEmergency(emergency);
        }

        private EmergencySeverity CalculateFoodWebSeverity(EcosystemHealth health)
        {
            float instability = 1f - health.foodWebStability;
            if (instability >= 0.8f) return EmergencySeverity.Critical;
            if (instability >= 0.6f) return EmergencySeverity.Severe;
            if (instability >= 0.4f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        private EmergencySeverity CalculateHabitatSeverity(EcosystemHealth health)
        {
            float disconnection = 1f - health.habitatConnectivity;
            if (disconnection >= 0.8f) return EmergencySeverity.Critical;
            if (disconnection >= 0.6f) return EmergencySeverity.Severe;
            if (disconnection >= 0.4f) return EmergencySeverity.Moderate;
            return EmergencySeverity.Minor;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Component for emergency conservation tracking
    /// </summary>
    [Serializable]
    public struct EmergencyConservationData : IComponentData
    {
        public int emergencyId;
        public EmergencyType type;
        public EmergencySeverity severity;
        public float timeRemaining;
        public bool isActive;
        public int affectedSpeciesCount;
        public float playerResponse;
    }

    /// <summary>
    /// Complete conservation emergency data
    /// </summary>
    [Serializable]
    public struct ConservationEmergency
    {
        public int emergencyId;
        public EmergencyType type;
        public EmergencySeverity severity;
        public ConservationUrgencyLevel urgencyLevel;
        public int affectedSpeciesId;
        public int affectedEcosystemId;
        public float timeRemaining;
        public float originalDuration;
        public FixedList64Bytes<EmergencyActionType> requiredActionTypes;
        public FixedList32Bytes<RequirementType> successRequirementTypes;
        public FixedString128Bytes title;
        public FixedString512Bytes description;
        public FixedList64Bytes<FixedString64Bytes> potentialConsequences;
        public bool hasEscalated;
        public float declaredAt;
    }

    /// <summary>
    /// Player response to emergency
    /// </summary>
    [Serializable]
    public struct EmergencyResponse
    {
        public int playerId;
        public int emergencyId;
        public EmergencyActionType actionType;
        public float resourcesCommitted;
        public float startTime;
        public float timeInvested;
        public float maxDuration;
        public float progress;
        public float effectiveness;
        public bool isComplete;
    }

    /// <summary>
    /// Conservation requirement for success
    /// </summary>
    [Serializable]
    public struct ConservationRequirement
    {
        public RequirementType type;
        public float targetValue;
        public FixedString512Bytes description;
        public bool isMet;
        public float currentValue;
    }

    /// <summary>
    /// Emergency action definition
    /// </summary>
    [Serializable]
    public struct EmergencyAction
    {
        public EmergencyActionType type;
        public FixedString64Bytes name;
        public FixedString512Bytes description;
        public float resourceRequirement;
        public float timeRequirement;
        public float effectiveness;
        public FixedList32Bytes<FixedString64Bytes> prerequisites;
    }

    /// <summary>
    /// Emergency outcome data
    /// </summary>
    [Serializable]
    public struct EmergencyOutcome
    {
        public ConservationEmergency emergency;
        public bool isSuccessful;
        public EmergencyStatus finalStatus;
        public FixedList128Bytes<PlayerContribution> playerContributions;
        public FixedList64Bytes<FixedString128Bytes> lessonsLearned;
        public float timestamp;
    }

    public struct PlayerContribution
    {
        public int playerId;
        public float contribution;
    }

    /// <summary>
    /// Conservation crisis escalation
    /// </summary>
    [Serializable]
    public struct ConservationCrisis
    {
        public ConservationEmergency originalEmergency;
        public FixedString512Bytes escalationReason;
        public FixedList32Bytes<RequirementType> newRequirementTypes;
        public float timestamp;
    }

    /// <summary>
    /// Conservation success story
    /// </summary>
    [Serializable]
    public struct ConservationSuccess
    {
        public ConservationSuccessType successType;
        public int emergencyId;
        public int speciesId;
        public int ecosystemId;
        public FixedString512Bytes achievementDescription;
        public int finalPopulation;
        public float recoveryTime;
        public FixedList64Bytes<FixedString128Bytes> contributingFactors;
        public float timestamp;
    }

    /// <summary>
    /// Ecosystem health tracking
    /// </summary>
    [Serializable]
    public struct EcosystemHealth : IComponentData
    {
        public int ecosystemId;
        public float overallHealth;
        public float healthTrend;
        public float foodWebStability;
        public float habitatConnectivity;
        public FixedList64Bytes<float> healthHistory;
        public int historyIndex;
    }

    /// <summary>
    /// Species population data
    /// </summary>
    [Serializable]
    public struct SpeciesPopulationData : IComponentData
    {
        public int speciesId;
        public FixedString64Bytes speciesName;
        public int ecosystemId;
        public int currentPopulation;
        public float populationTrend;
        public float reproductiveSuccess;
        public float juvenileSurvivalRate;
        public float breedingAge;
        public float diseasePrevalence;
        public bool wasEndangered;
        public float endangeredSince;
    }

    /// <summary>
    /// Ecosystem data
    /// </summary>
    [Serializable]
    public struct EcosystemData : IComponentData
    {
        public int ecosystemId;
        public FixedString64Bytes ecosystemName;
        public float habitatLossRate;
        public float climateStressLevel;
        public int speciesCount;
        public float biodiversityIndex;
    }

    #endregion
}