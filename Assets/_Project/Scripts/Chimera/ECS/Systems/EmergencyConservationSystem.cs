using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Ecosystem;
using Laboratory.Chimera.ECS.Services;

namespace Laboratory.Chimera.ECS
{
    /// <summary>
    /// Emergency conservation events system.
    /// Creates urgent scenarios requiring immediate action to save endangered species and ecosystems.
    /// Refactored to use service-oriented architecture with performance profiling.
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
        private Dictionary<int, Dictionary<int, float>> _emergencyPlayerContributions = new Dictionary<int, Dictionary<int, float>>();
        private float _emergencyCheckTimer = 0f;

        // Performance profiling markers
        private static readonly ProfilerMarker s_CheckForNewEmergenciesMarker = new ProfilerMarker("EmergencyConservation.CheckForNewEmergencies");
        private static readonly ProfilerMarker s_UpdateActiveEmergenciesMarker = new ProfilerMarker("EmergencyConservation.UpdateActiveEmergencies");
        private static readonly ProfilerMarker s_ProcessPlayerResponsesMarker = new ProfilerMarker("EmergencyConservation.ProcessPlayerResponses");
        private static readonly ProfilerMarker s_MonitorEcosystemHealthMarker = new ProfilerMarker("EmergencyConservation.MonitorEcosystemHealth");
        private static readonly ProfilerMarker s_CheckPopulationEmergenciesMarker = new ProfilerMarker("EmergencyConservation.CheckPopulationEmergencies");
        private static readonly ProfilerMarker s_CheckEcosystemEmergenciesMarker = new ProfilerMarker("EmergencyConservation.CheckEcosystemEmergencies");

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

            // Services are now static utility classes - no initialization needed

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

        void CheckForNewEmergencies()
        {
            using (s_CheckForNewEmergenciesMarker.Auto())
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
        }

        void CheckPopulationEmergencies()
        {
            using (s_CheckPopulationEmergenciesMarker.Auto())
            {
                float currentTime = (float)SystemAPI.Time.ElapsedTime;

                foreach (var (populationData, entity) in SystemAPI.Query<RefRO<SpeciesPopulationData>>().WithEntityAccess())
                {
                    // Check for critical population decline
                    if (populationData.ValueRO.currentPopulation <= _config.criticalPopulationThreshold &&
                        populationData.ValueRO.populationTrend < 0f &&
                        !HasActiveEmergency(populationData.ValueRO.speciesId, EmergencyType.PopulationCollapse))
                    {
                        var emergency = EmergencyCreationService.CreatePopulationEmergency(_config, populationData.ValueRO, currentTime);
                        AddEmergency(emergency);
                    }

                    // Check for breeding failure
                    if (populationData.ValueRO.reproductiveSuccess < _config.breedingFailureThreshold &&
                        populationData.ValueRO.breedingAge > _config.breedingAgeThreshold &&
                        !HasActiveEmergency(populationData.ValueRO.speciesId, EmergencyType.BreedingFailure))
                    {
                        var emergency = EmergencyCreationService.CreateBreedingEmergency(_config, populationData.ValueRO, currentTime);
                        AddEmergency(emergency);
                    }

                    // Check for juvenile mortality crisis
                    if (populationData.ValueRO.juvenileSurvivalRate < _config.juvenileSurvivalThreshold &&
                        !HasActiveEmergency(populationData.ValueRO.speciesId, EmergencyType.JuvenileMortality))
                    {
                        var emergency = EmergencyCreationService.CreateJuvenileEmergency(_config, populationData.ValueRO, currentTime);
                        AddEmergency(emergency);
                    }
                }
            }
        }

        void CheckEcosystemEmergencies()
        {
            using (s_CheckEcosystemEmergenciesMarker.Auto())
            {
                float currentTime = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (ecosystemData, health, entity) in SystemAPI.Query<RefRW<EcosystemData>, RefRW<EcosystemHealth>>().WithEntityAccess())
            {
                // Check for ecosystem collapse
                if (health.ValueRO.overallHealth < _config.ecosystemCollapseThreshold &&
                    health.ValueRO.healthTrend < 0f &&
                    !HasActiveEmergency(ecosystemData.ValueRO.ecosystemId, EmergencyType.EcosystemCollapse))
                {
                    var emergency = EmergencyCreationService.CreateEcosystemEmergency(_config, ecosystemData.ValueRO, health.ValueRO, currentTime);
                    AddEmergency(emergency);
                }

                // Check for food web disruption
                if (health.ValueRO.foodWebStability < _config.foodWebStabilityThreshold &&
                    !HasActiveEmergency(ecosystemData.ValueRO.ecosystemId, EmergencyType.FoodWebDisruption))
                {
                    var emergency = EmergencyCreationService.CreateFoodWebEmergency(_config, ecosystemData.ValueRO, health.ValueRO, currentTime);
                    AddEmergency(emergency);
                }

                // Check for habitat fragmentation
                if (health.ValueRO.habitatConnectivity < _config.habitatConnectivityThreshold &&
                    !HasActiveEmergency(ecosystemData.ValueRO.ecosystemId, EmergencyType.HabitatFragmentation))
                {
                    var emergency = EmergencyCreationService.CreateHabitatFragmentationEmergency(_config, ecosystemData.ValueRO, health.ValueRO, currentTime);
                    AddEmergency(emergency);
                }
            }
            }
        }

        void CheckGeneticDiversityEmergencies()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (populationData, genetics, entity) in SystemAPI.Query<RefRO<SpeciesPopulationData>, RefRO<CreatureGeneticsComponent>>().WithEntityAccess())
            {
                // Calculate genetic diversity
                float geneticDiversity = CalculateGeneticDiversity(genetics.ValueRO, populationData.ValueRO);

                if (geneticDiversity < _config.geneticDiversityThreshold &&
                    !HasActiveEmergency(populationData.ValueRO.speciesId, EmergencyType.GeneticBottleneck))
                {
                    var emergency = EmergencyCreationService.CreateGeneticEmergency(_config, populationData.ValueRO, geneticDiversity, currentTime);
                    AddEmergency(emergency);
                }
            }
        }

        void CheckHabitatEmergencies()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Check for rapid habitat loss
            foreach (var (ecosystemData, entity) in SystemAPI.Query<RefRO<EcosystemData>>().WithEntityAccess())
            {
                if (ecosystemData.ValueRO.habitatLossRate > _config.habitatLossRateThreshold &&
                    !HasActiveEmergency(ecosystemData.ValueRO.ecosystemId, EmergencyType.HabitatDestruction))
                {
                    var emergency = EmergencyCreationService.CreateHabitatDestructionEmergency(_config, ecosystemData.ValueRO, currentTime);
                    AddEmergency(emergency);
                }
            }
        }

        void CheckDiseaseEmergencies()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (populationData, entity) in SystemAPI.Query<RefRO<SpeciesPopulationData>>().WithEntityAccess())
            {
                if (populationData.ValueRO.diseasePrevalence > _config.diseaseOutbreakThreshold &&
                    !HasActiveEmergency(populationData.ValueRO.speciesId, EmergencyType.DiseaseOutbreak))
                {
                    var emergency = EmergencyCreationService.CreateDiseaseEmergency(_config, populationData.ValueRO, currentTime);
                    AddEmergency(emergency);
                }
            }
        }

        void CheckClimateEmergencies()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Check for climate-related threats
            foreach (var (ecosystemData, entity) in SystemAPI.Query<RefRO<EcosystemData>>().WithEntityAccess())
            {
                if (ecosystemData.ValueRO.climateStressLevel > _config.climateStressThreshold &&
                    !HasActiveEmergency(ecosystemData.ValueRO.ecosystemId, EmergencyType.ClimateChange))
                {
                    var emergency = EmergencyCreationService.CreateClimateEmergency(_config, ecosystemData.ValueRO, currentTime);
                    AddEmergency(emergency);
                }
            }
        }

        void UpdateActiveEmergencies(float deltaTime)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            for (int i = _activeEmergencies.Count - 1; i >= 0; i--)
            {
                var emergency = _activeEmergencies[i];
                emergency.timeRemaining -= deltaTime;

                // Check if emergency should escalate
                if (EmergencyResolutionService.ShouldEscalate(emergency))
                {
                    var (updatedEmergency, crisis) = EmergencyResolutionService.EscalateEmergency(emergency, currentTime);
                    emergency = updatedEmergency;
                    OnCrisisEscalated?.Invoke(crisis);
                }

                // Check if emergency is resolved or expired
                if (emergency.timeRemaining <= 0f || EmergencyResolutionService.IsEmergencyResolved(emergency))
                {
                    ResolveEmergency(emergency, currentTime);
                    _activeEmergencies.RemoveAt(i);
                }
                else
                {
                    _activeEmergencies[i] = emergency;
                }
            }
        }

        void ProcessPlayerResponses(float deltaTime)
        {
            foreach (var kvp in _playerResponses.ToList())
            {
                var playerId = kvp.Key;
                var response = PlayerResponseService.UpdateResponse(_config, kvp.Value, deltaTime);

                // Check for response completion
                if (PlayerResponseService.IsResponseComplete(response))
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

        void MonitorEcosystemHealth()
        {
            foreach (var (health, entity) in SystemAPI.Query<RefRW<EcosystemHealth>>().WithEntityAccess())
            {
                // Track health trends for early warning
                var healthValue = health.ValueRW;
                UpdateHealthTrends(ref healthValue);
                health.ValueRW = healthValue;

                // Check for early warning signs
                CheckEarlyWarningSigns(entity, health.ValueRO);
            }
        }

        void ProcessEmergencyEscalations(float deltaTime)
        {
            foreach (var emergency in _activeEmergencies.Where(e => e.hasEscalated))
            {
                ProcessEscalatedEmergency(emergency, deltaTime);
            }
        }

        void CheckConservationSuccesses()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Check for successful conservation outcomes
            foreach (var (populationData, entity) in SystemAPI.Query<RefRW<SpeciesPopulationData>>().WithEntityAccess())
            {
                if (populationData.ValueRO.wasEndangered &&
                    populationData.ValueRO.currentPopulation > _config.recoveryPopulationThreshold &&
                    populationData.ValueRO.populationTrend > 0f)
                {
                    var success = EmergencyResolutionService.CreateConservationSuccess(populationData.ValueRO, currentTime);
                    OnConservationSuccess?.Invoke(success);

                    var popData = populationData.ValueRW;
                    popData.wasEndangered = false;
                    populationData.ValueRW = popData;
                }
            }
        }

        #region Helper Methods

        void AddEmergency(ConservationEmergency emergency)
        {
            _activeEmergencies.Add(emergency);
            OnEmergencyDeclared?.Invoke(emergency);

            UnityEngine.Debug.Log($"Conservation Emergency Declared: {emergency.title} (Severity: {emergency.severity})");
        }

        bool HasActiveEmergency(int targetId, EmergencyType type)
        {
            return _activeEmergencies.Any(e =>
                e.type == type &&
                (e.affectedSpeciesId == targetId || e.affectedEcosystemId == targetId));
        }

        int GenerateEmergencyId()
        {
            return UnityEngine.Random.Range(100000, 999999);
        }

        float CalculateGeneticDiversity(in CreatureGeneticsComponent genetics, SpeciesPopulationData populationData)
        {
            // Calculate genetic diversity based on trait variety and population size
            float traitDiversity = genetics.ActiveGeneCount;
            float populationFactor = Mathf.Log(populationData.currentPopulation + 1f) / 10f;
            return (traitDiversity * populationFactor) / 20f; // Normalized to 0-1
        }

        EmergencyAction[] GetRequiredActions(EmergencyType type)
        {
            var ecosystemActions = _config.GetRequiredActions(type);
            var ecsActions = new EmergencyAction[ecosystemActions.Length];

            for (int i = 0; i < ecosystemActions.Length; i++)
            {
                ecsActions[i] = ConvertToECSEmergencyAction(ecosystemActions[i]);
            }

            return ecsActions;
        }

        FixedList64Bytes<EmergencyActionType> GetRequiredActionTypes(EmergencyType type)
        {
            var actions = _config.GetRequiredActions(type);
            var actionTypes = new FixedList64Bytes<EmergencyActionType>();
            foreach (var action in actions)
            {
                actionTypes.Add(action.type);
            }
            return actionTypes;
        }

        FixedList64Bytes<FixedString64Bytes> ConvertStringArrayToFixedList(string[] strings)
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

        FixedList32Bytes<RequirementType> GetSuccessRequirementTypes(SpeciesPopulationData populationData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.PopulationIncrease);
            requirementTypes.Add(RequirementType.ReproductiveSuccess);
            requirementTypes.Add(RequirementType.HabitatProtection);
            return requirementTypes;
        }

        FixedList32Bytes<RequirementType> GetBreedingSuccessRequirementTypes(SpeciesPopulationData populationData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.ReproductiveSuccess);
            requirementTypes.Add(RequirementType.HabitatQuality);
            requirementTypes.Add(RequirementType.PopulationManagement);
            return requirementTypes;
        }

        FixedList32Bytes<RequirementType> GetJuvenileSuccessRequirementTypes(SpeciesPopulationData populationData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.JuvenileSurvival);
            requirementTypes.Add(RequirementType.HabitatProtection);
            return requirementTypes;
        }

        FixedList32Bytes<RequirementType> GetEcosystemSuccessRequirementTypes(EcosystemData ecosystemData, EcosystemHealth health)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.EcosystemHealth);
            requirementTypes.Add(RequirementType.SpeciesDiversity);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            return requirementTypes;
        }

        FixedList32Bytes<RequirementType> GetGeneticSuccessRequirementTypes(SpeciesPopulationData populationData, float diversity)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.PopulationIncrease);
            requirementTypes.Add(RequirementType.BreedingManagement);
            return requirementTypes;
        }

        FixedList32Bytes<RequirementType> GetHabitatSuccessRequirementTypes(EcosystemData ecosystemData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.HabitatProtection);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            return requirementTypes;
        }

        FixedList32Bytes<RequirementType> GetDiseaseSuccessRequirementTypes(SpeciesPopulationData populationData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.HealthMonitoring);
            requirementTypes.Add(RequirementType.QuarantineProtocol);
            return requirementTypes;
        }

        FixedList32Bytes<RequirementType> GetClimateSuccessRequirementTypes(EcosystemData ecosystemData)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.ClimateAdaptation);
            requirementTypes.Add(RequirementType.HabitatConnectivity);
            requirementTypes.Add(RequirementType.EcosystemResilience);
            return requirementTypes;
        }

        FixedList32Bytes<RequirementType> GetEscalatedRequirementTypes(ConservationEmergency emergency)
        {
            var requirementTypes = new FixedList32Bytes<RequirementType>();
            requirementTypes.Add(RequirementType.PopulationTarget);
            requirementTypes.Add(RequirementType.HabitatRestoration);
            requirementTypes.Add(RequirementType.ThreatReduction);
            return requirementTypes;
        }

        void UpdateEmergencySeverity(ref ConservationEmergency emergency)
        {
            // Update severity based on current conditions
            // This would check current population/ecosystem status
        }

        void ResolveEmergency(ConservationEmergency emergency, float currentTime)
        {
            var playerContributions = GetPlayerContributionsForEmergency(emergency.emergencyId);
            var outcome = EmergencyResolutionService.CreateOutcome(emergency, playerContributions, currentTime);

            OnEmergencyResolved?.Invoke(emergency, outcome);

            if (outcome.isSuccessful)
            {
                var success = EmergencyResolutionService.CreateConservationSuccessFromEmergency(emergency, currentTime);
                OnConservationSuccess?.Invoke(success);
            }
        }

        Dictionary<int, float> GetPlayerContributionsForEmergency(int emergencyId)
        {
            if (_emergencyPlayerContributions.TryGetValue(emergencyId, out var contributions))
            {
                return contributions;
            }
            return new Dictionary<int, float>();
        }

        void CompletePlayerResponse(int playerId, EmergencyResponse response)
        {
            OnPlayerResponseRecorded?.Invoke(playerId, response);

            // Track player contribution for this emergency
            if (!_emergencyPlayerContributions.ContainsKey(response.emergencyId))
            {
                _emergencyPlayerContributions[response.emergencyId] = new Dictionary<int, float>();
            }
            _emergencyPlayerContributions[response.emergencyId] = PlayerResponseService.UpdatePlayerContributions(
                _emergencyPlayerContributions[response.emergencyId], response);

            // Apply response effects to emergency
            for (int i = 0; i < _activeEmergencies.Count; i++)
            {
                if (_activeEmergencies[i].emergencyId == response.emergencyId)
                {
                    var updatedEmergency = PlayerResponseService.ApplyResponseToEmergency(_activeEmergencies[i], response);
                    _activeEmergencies[i] = updatedEmergency;
                    break;
                }
            }
        }

        void UpdateHealthTrends(ref EcosystemHealth health)
        {
            // Update health trend tracking for early warning
            health.healthHistory[health.historyIndex] = health.overallHealth;
            health.historyIndex = (health.historyIndex + 1) % health.healthHistory.Length;

            // Calculate trend
            float oldHealth = health.healthHistory[health.historyIndex];
            health.healthTrend = health.overallHealth - oldHealth;
        }

        void CheckEarlyWarningSigns(Entity entity, EcosystemHealth health)
        {
            // Check for patterns that indicate impending emergencies
            if (health.healthTrend < -0.05f && health.overallHealth < 0.8f)
            {
                // Issue early warning
                IssueEarlyWarning(entity, health);
            }
        }

        void IssueEarlyWarning(Entity entity, EcosystemHealth health)
        {
            // Issue early warning notification
            UnityEngine.Debug.Log($"Early Warning: Ecosystem health declining rapidly (Current: {health.overallHealth:P1}, Trend: {health.healthTrend:F3})");
        }

        void ProcessEscalatedEmergency(ConservationEmergency emergency, float deltaTime)
        {
            // Process escalated emergencies with more severe consequences
            // This might involve faster deterioration or additional requirements
        }

        EmergencyAction ConvertToECSEmergencyAction(Laboratory.Chimera.Ecosystem.EmergencyAction ecosystemAction)
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

        FixedList128Bytes<PlayerContribution> ConvertPlayerContributionsToFixedList(Dictionary<int, float> contributions)
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

        FixedList64Bytes<FixedString128Bytes> ConvertStringArrayToFixedList128(string[] strings)
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

        FixedList32Bytes<FixedString64Bytes> ConvertStringArrayToFixedList32(string[] strings)
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

        void StartEmergencyResponse(int playerId, int emergencyId, EmergencyActionType actionType, float resourceCommitment)
        {
            var emergency = _activeEmergencies.FirstOrDefault(e => e.emergencyId == emergencyId);
            if (emergency.emergencyId == 0)
            {
                UnityEngine.Debug.LogWarning($"Emergency {emergencyId} not found");
                return;
            }

            if (!PlayerResponseService.CanRespondToEmergency(emergency, actionType))
            {
                UnityEngine.Debug.LogWarning($"Action type {actionType} is not valid for emergency {emergencyId}");
                return;
            }

            if (!PlayerResponseService.IsValidResourceCommitment(_config, resourceCommitment))
            {
                UnityEngine.Debug.LogWarning($"Invalid resource commitment: {resourceCommitment}");
                return;
            }

            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            var response = PlayerResponseService.CreateResponse(_config, playerId, emergencyId, actionType, resourceCommitment, currentTime);

            _playerResponses[playerId] = response;
            UnityEngine.Debug.Log($"Player {playerId} started response to emergency {emergencyId}");
        }

        ConservationEmergency[] GetActiveEmergencies()
        {
            return _activeEmergencies.ToArray();
        }

        ConservationEmergency[] GetEmergenciesByType(EmergencyType type)
        {
            return _activeEmergencies.Where(e => e.type == type).ToArray();
        }

        ConservationEmergency[] GetEmergenciesBySeverity(EmergencySeverity severity)
        {
            return _activeEmergencies.Where(e => e.severity == severity).ToArray();
        }

        EmergencyResponse GetPlayerResponse(int playerId)
        {
            return _playerResponses.ContainsKey(playerId) ? _playerResponses[playerId] : default;
        }

        #endregion

        #region Missing Methods

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