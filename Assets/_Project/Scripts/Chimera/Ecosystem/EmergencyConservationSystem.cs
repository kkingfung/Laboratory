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

namespace Laboratory.Chimera.Ecosystem
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
                ComponentType.ReadOnly<GeneticProfile>()
            );

            _activeEmergenciesQuery = GetEntityQuery(
                ComponentType.ReadWrite<EmergencyConservationData>()
            );
        }

        protected override void OnUpdate()
        {
            if (_config == null) return;

            float deltaTime = Time.DeltaTime;

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
            }).Run();
        }

        private void CheckEcosystemEmergencies()
        {
            Entities.WithAll<EcosystemData, EcosystemHealth>().ForEach((Entity entity, ref EcosystemData ecosystemData, ref EcosystemHealth health) =>
            {
                // Check for ecosystem collapse
                if (health.overallHealth < _config.ecosystemCollapseThreshold &&
                    health.healthTrend < 0f &&
                    !HasActiveEmergency(ecosystemData.ecosystemId, EmergencyType.EcosystemCollapse))
                {
                    CreateEcosystemEmergency(entity, ecosystemData, health);
                }

                // Check for food web disruption
                if (health.foodWebStability < _config.foodWebStabilityThreshold &&
                    !HasActiveEmergency(ecosystemData.ecosystemId, EmergencyType.FoodWebDisruption))
                {
                    CreateFoodWebEmergency(entity, ecosystemData, health);
                }

                // Check for habitat fragmentation
                if (health.habitatConnectivity < _config.habitatConnectivityThreshold &&
                    !HasActiveEmergency(ecosystemData.ecosystemId, EmergencyType.HabitatFragmentation))
                {
                    CreateHabitatEmergency(entity, ecosystemData, health);
                }
            }).Run();
        }

        private void CheckGeneticDiversityEmergencies()
        {
            Entities.WithAll<SpeciesPopulationData, GeneticProfile>().ForEach((Entity entity, ref SpeciesPopulationData populationData, ref GeneticProfile genetics) =>
            {
                // Calculate genetic diversity
                float geneticDiversity = CalculateGeneticDiversity(genetics, populationData);

                if (geneticDiversity < _config.geneticDiversityThreshold &&
                    !HasActiveEmergency(populationData.speciesId, EmergencyType.GeneticBottleneck))
                {
                    CreateGeneticEmergency(entity, populationData, genetics, geneticDiversity);
                }
            }).Run();
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
            }).Run();
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
            }).Run();
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
            }).Run();
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
                CheckEarlyWarningSignS(entity, health);
            }).Run();
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
            }).Run();
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
                requiredActions = GetRequiredActions(EmergencyType.PopulationCollapse),
                title = $"Critical Population Decline: {populationData.speciesName}",
                description = GeneratePopulationDescription(populationData),
                urgencyLevel = CalculateConservationUrgencyLevel(populationData.currentPopulation, _config.criticalPopulationThreshold),
                potentialConsequences = GetPopulationConsequences(populationData),
                successRequirements = GetPopulationSuccessRequirements(populationData),
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
                requiredActions = GetRequiredActions(EmergencyType.BreedingFailure),
                title = $"Breeding Crisis: {populationData.speciesName}",
                description = GenerateBreedingDescription(populationData),
                urgencyLevel = CalculateBreedingUrgency(populationData),
                potentialConsequences = GetBreedingConsequences(populationData),
                successRequirements = GetBreedingSuccessRequirements(populationData),
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
                requiredActions = GetRequiredActions(EmergencyType.JuvenileMortality),
                title = $"Juvenile Mortality Crisis: {populationData.speciesName}",
                description = GenerateJuvenileDescription(populationData),
                urgencyLevel = CalculateJuvenileUrgency(populationData),
                potentialConsequences = GetJuvenileConsequences(populationData),
                successRequirements = GetJuvenileSuccessRequirements(populationData),
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
                requiredActions = GetRequiredActions(EmergencyType.EcosystemCollapse),
                title = $"Ecosystem Collapse Warning: {ecosystemData.ecosystemName}",
                description = GenerateEcosystemDescription(ecosystemData, health),
                urgencyLevel = CalculateEcosystemUrgency(health),
                potentialConsequences = GetEcosystemConsequences(ecosystemData, health),
                successRequirements = GetEcosystemSuccessRequirements(ecosystemData, health),
                declaredAt = (float)SystemAPI.Time.ElapsedTime
            };

            AddEmergency(emergency);
        }

        private void CreateGeneticEmergency(Entity entity, SpeciesPopulationData populationData, GeneticProfile genetics, float diversity)
        {
            var emergency = new ConservationEmergency
            {
                emergencyId = GenerateEmergencyId(),
                type = EmergencyType.GeneticBottleneck,
                severity = CalculateGeneticSeverity(diversity),
                affectedSpeciesId = populationData.speciesId,
                affectedEcosystemId = populationData.ecosystemId,
                timeRemaining = _config.GetEmergencyDuration(EmergencyType.GeneticBottleneck),
                requiredActions = GetRequiredActions(EmergencyType.GeneticBottleneck),
                title = $"Genetic Bottleneck Crisis: {populationData.speciesName}",
                description = GenerateGeneticDescription(populationData, genetics, diversity),
                urgencyLevel = CalculateGeneticUrgency(diversity),
                potentialConsequences = GetGeneticConsequences(populationData, diversity),
                successRequirements = GetGeneticSuccessRequirements(populationData, diversity),
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
                requiredActions = GetRequiredActions(EmergencyType.HabitatDestruction),
                title = $"Rapid Habitat Loss: {ecosystemData.ecosystemName}",
                description = GenerateHabitatDescription(ecosystemData),
                urgencyLevel = CalculateHabitatUrgency(ecosystemData),
                potentialConsequences = GetHabitatConsequences(ecosystemData),
                successRequirements = GetHabitatSuccessRequirements(ecosystemData),
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
                requiredActions = GetRequiredActions(EmergencyType.DiseaseOutbreak),
                title = $"Disease Outbreak: {populationData.speciesName}",
                description = GenerateDiseaseDescription(populationData),
                urgencyLevel = CalculateDiseaseUrgency(populationData),
                potentialConsequences = GetDiseaseConsequences(populationData),
                successRequirements = GetDiseaseSuccessRequirements(populationData),
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
                requiredActions = GetRequiredActions(EmergencyType.ClimateChange),
                title = $"Climate Emergency: {ecosystemData.ecosystemName}",
                description = GenerateClimateDescription(ecosystemData),
                urgencyLevel = CalculateClimateUrgency(ecosystemData),
                potentialConsequences = GetClimateConsequences(ecosystemData),
                successRequirements = GetClimateSuccessRequirements(ecosystemData),
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

        private float CalculateGeneticDiversity(GeneticProfile genetics, SpeciesPopulationData populationData)
        {
            // Calculate genetic diversity based on trait variety and population size
            float traitDiversity = genetics.GetTraitNames().Count;
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
            return _config.GetRequiredActions(type);
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

        private string GenerateGeneticDescription(SpeciesPopulationData populationData, GeneticProfile genetics, float diversity)
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

        private ConservationRequirement[] GetPopulationSuccessRequirements(SpeciesPopulationData populationData)
        {
            return new[]
            {
                new ConservationRequirement { type = RequirementType.PopulationIncrease, targetValue = _config.recoveryPopulationThreshold, description = "Increase population above recovery threshold" },
                new ConservationRequirement { type = RequirementType.ReproductiveSuccess, targetValue = 0.7f, description = "Achieve sustainable breeding rates" },
                new ConservationRequirement { type = RequirementType.HabitatProtection, targetValue = 0.8f, description = "Secure adequate habitat" }
            };
        }

        private ConservationRequirement[] GetBreedingSuccessRequirements(SpeciesPopulationData populationData)
        {
            return new[]
            {
                new ConservationRequirement { type = RequirementType.ReproductiveSuccess, targetValue = 0.6f, description = "Restore breeding success rates" },
                new ConservationRequirement { type = RequirementType.HabitatQuality, targetValue = 0.8f, description = "Improve breeding habitat quality" },
                new ConservationRequirement { type = RequirementType.PopulationManagement, targetValue = 1.0f, description = "Implement breeding management program" }
            };
        }

        private ConservationRequirement[] GetJuvenileSuccessRequirements(SpeciesPopulationData populationData)
        {
            return new[]
            {
                new ConservationRequirement { type = RequirementType.JuvenileSurvival, targetValue = 0.7f, description = "Improve juvenile survival rates" },
                new ConservationRequirement { type = RequirementType.HabitatProtection, targetValue = 0.9f, description = "Protect nursery areas" },
                new ConservationRequirement { type = RequirementType.ThreatReduction, targetValue = 0.8f, description = "Reduce juvenile mortality threats" }
            };
        }

        private ConservationRequirement[] GetEcosystemSuccessRequirements(EcosystemData ecosystemData, EcosystemHealth health)
        {
            return new[]
            {
                new ConservationRequirement { type = RequirementType.EcosystemHealth, targetValue = 0.7f, description = "Restore ecosystem health" },
                new ConservationRequirement { type = RequirementType.SpeciesDiversity, targetValue = 0.8f, description = "Maintain species diversity" },
                new ConservationRequirement { type = RequirementType.HabitatConnectivity, targetValue = 0.9f, description = "Restore habitat connectivity" }
            };
        }

        private ConservationRequirement[] GetGeneticSuccessRequirements(SpeciesPopulationData populationData, float diversity)
        {
            return new[]
            {
                new ConservationRequirement { type = RequirementType.GeneticDiversity, targetValue = 0.6f, description = "Increase genetic diversity" },
                new ConservationRequirement { type = RequirementType.PopulationIncrease, targetValue = _config.recoveryPopulationThreshold, description = "Expand population size" },
                new ConservationRequirement { type = RequirementType.BreedingManagement, targetValue = 1.0f, description = "Implement genetic management" }
            };
        }

        private ConservationRequirement[] GetHabitatSuccessRequirements(EcosystemData ecosystemData)
        {
            return new[]
            {
                new ConservationRequirement { type = RequirementType.HabitatProtection, targetValue = 0.9f, description = "Stop habitat destruction" },
                new ConservationRequirement { type = RequirementType.HabitatRestoration, targetValue = 0.5f, description = "Restore degraded areas" },
                new ConservationRequirement { type = RequirementType.HabitatConnectivity, targetValue = 0.8f, description = "Create habitat corridors" }
            };
        }

        private ConservationRequirement[] GetDiseaseSuccessRequirements(SpeciesPopulationData populationData)
        {
            return new[]
            {
                new ConservationRequirement { type = RequirementType.DiseaseControl, targetValue = 0.1f, description = "Reduce disease prevalence" },
                new ConservationRequirement { type = RequirementType.HealthMonitoring, targetValue = 1.0f, description = "Implement health monitoring" },
                new ConservationRequirement { type = RequirementType.QuarantineProtocol, targetValue = 1.0f, description = "Establish quarantine protocols" }
            };
        }

        private ConservationRequirement[] GetClimateSuccessRequirements(EcosystemData ecosystemData)
        {
            return new[]
            {
                new ConservationRequirement { type = RequirementType.ClimateAdaptation, targetValue = 0.8f, description = "Implement adaptation strategies" },
                new ConservationRequirement { type = RequirementType.HabitatConnectivity, targetValue = 0.9f, description = "Create migration corridors" },
                new ConservationRequirement { type = RequirementType.EcosystemResilience, targetValue = 0.7f, description = "Build ecosystem resilience" }
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
                newRequirements = GetEscalatedRequirements(emergency),
                timestamp = (float)SystemAPI.Time.ElapsedTime
            };

            OnCrisisEscalated?.Invoke(crisis);
        }

        private ConservationRequirement[] GetEscalatedRequirements(ConservationEmergency emergency)
        {
            // Return more stringent requirements for escalated emergencies
            return emergency.successRequirements;
        }

        private bool IsEmergencyResolved(ConservationEmergency emergency)
        {
            // Check if emergency conditions have been met
            return CheckSuccessRequirements(emergency);
        }

        private bool CheckSuccessRequirements(ConservationEmergency emergency)
        {
            // Check if all success requirements have been met
            foreach (var requirement in emergency.successRequirements)
            {
                if (!IsRequirementMet(requirement, emergency))
                    return false;
            }
            return true;
        }

        private bool IsRequirementMet(ConservationRequirement requirement, ConservationEmergency emergency)
        {
            // Check specific requirement types
            switch (requirement.type)
            {
                case RequirementType.PopulationIncrease:
                    return CheckPopulationRequirement(requirement, emergency);
                case RequirementType.ReproductiveSuccess:
                    return CheckReproductiveRequirement(requirement, emergency);
                case RequirementType.HabitatProtection:
                    return CheckHabitatRequirement(requirement, emergency);
                default:
                    return false;
            }
        }

        private bool CheckPopulationRequirement(ConservationRequirement requirement, ConservationEmergency emergency)
        {
            // Check current population against requirement
            // This would query current population data
            return false; // Simplified for example
        }

        private bool CheckReproductiveRequirement(ConservationRequirement requirement, ConservationEmergency emergency)
        {
            // Check current reproductive success
            return false; // Simplified for example
        }

        private bool CheckHabitatRequirement(ConservationRequirement requirement, ConservationEmergency emergency)
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
                playerContributions = GetPlayerContributions(emergency),
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

        private void CheckEarlyWarningSignS(Entity entity, EcosystemHealth health)
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
                contributingFactors = GetRecoveryFactors(populationData),
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
                contributingFactors = GetEmergencySuccessFactors(emergency),
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
        public EmergencyAction[] requiredActions;
        public ConservationRequirement[] successRequirements;
        public string title;
        public string description;
        public string[] potentialConsequences;
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
        public string description;
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
        public string name;
        public string description;
        public float resourceRequirement;
        public float timeRequirement;
        public float effectiveness;
        public string[] prerequisites;
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
        public Dictionary<int, float> playerContributions;
        public string[] lessonsLearned;
        public float timestamp;
    }

    /// <summary>
    /// Conservation crisis escalation
    /// </summary>
    [Serializable]
    public struct ConservationCrisis
    {
        public ConservationEmergency originalEmergency;
        public string escalationReason;
        public ConservationRequirement[] newRequirements;
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
        public string achievementDescription;
        public int finalPopulation;
        public float recoveryTime;
        public string[] contributingFactors;
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
        public string speciesName;
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
        public string ecosystemName;
        public float habitatLossRate;
        public float climateStressLevel;
        public int speciesCount;
        public float biodiversityIndex;
    }

    #endregion

    #region Enums

    public enum EmergencyType
    {
        PopulationCollapse,
        BreedingFailure,
        JuvenileMortality,
        EcosystemCollapse,
        FoodWebDisruption,
        HabitatFragmentation,
        HabitatDestruction,
        GeneticBottleneck,
        DiseaseOutbreak,
        ClimateChange,
        InvasiveSpecies,
        Pollution
    }

    public enum EmergencySeverity
    {
        Minor,
        Moderate,
        Severe,
        Critical
    }

    public enum ConservationConservationUrgencyLevel
    {
        Moderate,
        Important,
        Urgent,
        Immediate
    }

    public enum EmergencyActionType
    {
        PopulationSupport,
        BreedingProgram,
        HabitatProtection,
        HabitatRestoration,
        DiseaseControl,
        GeneticManagement,
        ClimateAdaptation,
        ThreatReduction,
        Monitoring,
        Research
    }

    public enum RequirementType
    {
        PopulationIncrease,
        ReproductiveSuccess,
        JuvenileSurvival,
        HabitatProtection,
        HabitatRestoration,
        HabitatQuality,
        HabitatConnectivity,
        GeneticDiversity,
        DiseaseControl,
        ThreatReduction,
        EcosystemHealth,
        SpeciesDiversity,
        FoodWebStability,
        ClimateAdaptation,
        EcosystemResilience,
        PopulationManagement,
        BreedingManagement,
        HealthMonitoring,
        QuarantineProtocol
    }

    public enum EmergencyStatus
    {
        Active,
        Resolved,
        Failed,
        TimeExpired,
        Escalated
    }

    public enum ConservationSuccessType
    {
        SpeciesRecovery,
        EcosystemRestoration,
        HabitatProtection,
        GeneticRescue,
        DiseaseEradication,
        ClimateAdaptation,
        General
    }

    /// <summary>
    /// Conservation urgency levels for emergency response
    /// </summary>
    public enum ConservationUrgencyLevel : byte
    {
        Low,        // Stable populations, monitoring phase
        Moderate,   // Slight decline, preventive measures
        High,       // Significant threat, active intervention
        Important,  // Elevated concern, increased monitoring
        Critical,   // Immediate danger, emergency response
        Urgent,     // Time-sensitive intervention needed
        Immediate,  // Urgent intervention required
        Extreme     // Imminent extinction, last resort measures
    }

    #endregion

    #region Missing Methods

    private void CreateFoodWebEmergency(Entity entity, EcosystemData ecosystemData, EcosystemHealth health)
    {
        // Create food web disruption emergency
        var emergency = new ConservationEmergency
        {
            emergencyId = GenerateEmergencyId(),
            emergencyType = EmergencyType.FoodWebDisruption,
            ecosystemId = ecosystemData.ecosystemId,
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
            emergencyType = EmergencyType.HabitatFragmentation,
            ecosystemId = ecosystemData.ecosystemId,
            severity = CalculateHabitatSeverity(health),
            urgencyLevel = ConservationUrgencyLevel.Critical,
            declaredAt = (float)SystemAPI.Time.ElapsedTime
        };

        AddEmergency(emergency);
    }

    private float CalculateFoodWebSeverity(EcosystemHealth health)
    {
        return 1f - health.foodWebStability;
    }

    private float CalculateHabitatSeverity(EcosystemHealth health)
    {
        return 1f - health.habitatConnectivity;
    }

    #endregion
}