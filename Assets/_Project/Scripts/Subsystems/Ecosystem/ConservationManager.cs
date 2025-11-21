using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Events;

namespace Laboratory.Subsystems.Ecosystem
{
    /// <summary>
    /// Conservation manager for tracking and managing species conservation status.
    /// Monitors population health, genetic diversity, and ecosystem balance.
    /// </summary>
    public class ConservationManager : MonoBehaviour, IConservationService
    {
        [Header("Conservation Tracking")]
        [SerializeField] private bool enableConservationTracking = true;
        [SerializeField] private bool enableAutomaticAssessment = true;
        [SerializeField] [Range(10f, 300f)] private float assessmentInterval = 60f;

        [Header("Conservation Thresholds")]
        [SerializeField] [Range(10, 1000)] private int criticalPopulationThreshold = 50;
        [SerializeField] [Range(50, 2000)] private int endangeredPopulationThreshold = 200;
        [SerializeField] [Range(100, 5000)] private int vulnerablePopulationThreshold = 500;
        [SerializeField] [Range(0.1f, 1f)] private float geneticDiversityThreshold = 0.7f;

        [Header("Conservation Actions")]
        [SerializeField] private bool enableAutomaticInterventions = true;
        [SerializeField] private bool enableBreedingPrograms = true;
        [SerializeField] private bool enableHabitatProtection = true;
        [SerializeField] private bool enableReintroduction = true;

        // Conservation data
        private readonly Dictionary<string, ConservationRecord> _conservationRecords = new();
        private readonly Dictionary<string, ConservationAction> _activeActions = new();
        private readonly List<ConservationAlert> _activeAlerts = new();

        // Assessment
        private float _lastAssessmentTime;
        private ConservationReport _latestReport;

        // Events
        public event Action<ConservationEvent> OnConservationStatusChanged;
        public event Action<ConservationAlert> OnConservationAlert;
        public event Action<ConservationAction> OnConservationActionStarted;
        public event Action<ConservationAction> OnConservationActionCompleted;

        // Properties
        public ConservationReport LatestReport => _latestReport;
        public ConservationRecord[] AllRecords => _conservationRecords.Values.ToArray();
        public ConservationAlert[] ActiveAlerts => _activeAlerts.ToArray();

        #region Unity Lifecycle

        private void Update()
        {
            if (enableConservationTracking && enableAutomaticAssessment)
            {
                if (Time.time - _lastAssessmentTime >= assessmentInterval)
                {
                    PerformConservationAssessment();
                    _lastAssessmentTime = Time.time;
                }
            }
        }

        #endregion

        #region Initialization

        public void Initialize(EcosystemSubsystemConfig config)
        {
            if (config?.ConservationConfig != null)
            {
                enableConservationTracking = config.ConservationConfig.TrackExtinctionEvents; // Use closest available property
                enableAutomaticAssessment = config.ConservationConfig.AlertOnStatusChanges; // Use closest available property
                assessmentInterval = config.ConservationConfig.ConservationResponseTime; // Use response time as assessment interval
                enableAutomaticInterventions = config.ConservationConfig.EnableAutomaticConservation;
            }

            InitializeConservationRecords();
            Debug.Log($"[ConservationManager] Initialized - Tracking: {enableConservationTracking}");
        }

        public async Task InitializeAsync(EcosystemSubsystemConfig config)
        {
            Initialize(config);
            await Task.CompletedTask;
        }

        private void InitializeConservationRecords()
        {
            _conservationRecords.Clear();
            _activeActions.Clear();
            _activeAlerts.Clear();

            // Initialize records for existing species
            // This would integrate with the genetics/species system
        }

        #endregion

        #region Conservation Assessment

        public void PerformConservationAssessment()
        {
            if (!enableConservationTracking)
                return;

            var report = new ConservationReport
            {
                assessmentDate = DateTime.UtcNow,
                speciesAssessed = _conservationRecords.Count,
                conservationActions = _activeActions.Count,
                activeAlerts = _activeAlerts.Count
            };

            var statusCounts = new Dictionary<ConservationStatus, int>();
            var previousStatuses = new Dictionary<string, ConservationStatus>();

            // Store previous statuses for comparison
            foreach (var record in _conservationRecords.Values)
            {
                previousStatuses[record.speciesId] = record.conservationStatus;
            }

            // Assess each species
            foreach (var record in _conservationRecords.Values.ToList())
            {
                AssessSpeciesConservation(record);

                // Count status occurrences
                if (!statusCounts.ContainsKey(record.conservationStatus))
                    statusCounts[record.conservationStatus] = 0;
                statusCounts[record.conservationStatus]++;

                // Check for status changes
                if (previousStatuses.TryGetValue(record.speciesId, out var previousStatus) &&
                    previousStatus != record.conservationStatus)
                {
                    HandleConservationStatusChange(record, previousStatus);
                }
            }

            // Update report
            report.extinctSpecies = statusCounts.GetValueOrDefault(ConservationStatus.Extinct, 0);
            report.criticallyEndangeredSpecies = statusCounts.GetValueOrDefault(ConservationStatus.CriticallyEndangered, 0);
            report.endangeredSpecies = statusCounts.GetValueOrDefault(ConservationStatus.Endangered, 0);
            report.vulnerableSpecies = statusCounts.GetValueOrDefault(ConservationStatus.Threatened, 0);
            report.stableSpecies = statusCounts.GetValueOrDefault(ConservationStatus.Stable, 0);

            // Calculate overall ecosystem health
            report.overallEcosystemHealth = CalculateEcosystemHealth(statusCounts);

            _latestReport = report;

            // Trigger interventions if needed
            if (enableAutomaticInterventions)
            {
                TriggerAutomaticInterventions();
            }

            Debug.Log($"[ConservationManager] Assessment completed - Health: {report.overallEcosystemHealth:F2}");
        }

        private void AssessSpeciesConservation(ConservationRecord record)
        {
            var previousStatus = record.conservationStatus;

            // Update population data (this would come from population management system)
            UpdatePopulationData(record);

            // Assess based on population size
            var populationBasedStatus = AssessPopulationStatus(record.currentPopulation);

            // Assess based on genetic diversity
            var geneticBasedStatus = AssessGeneticDiversityStatus(record.geneticDiversity);

            // Assess based on habitat quality
            var habitatBasedStatus = AssessHabitatStatus(record.habitatQuality);

            // Determine overall status (worst case scenario)
            record.conservationStatus = GetWorstConservationStatus(populationBasedStatus, geneticBasedStatus, habitatBasedStatus);

            // Update assessment timestamp
            record.lastAssessment = DateTime.UtcNow;

            // Calculate trends
            CalculateConservationTrends(record);

            // Check for alerts
            CheckForConservationAlerts(record);
        }

        private ConservationStatus AssessPopulationStatus(int population)
        {
            if (population == 0)
                return ConservationStatus.Extinct;
            if (population <= criticalPopulationThreshold)
                return ConservationStatus.CriticallyEndangered;
            if (population <= endangeredPopulationThreshold)
                return ConservationStatus.Endangered;
            if (population <= vulnerablePopulationThreshold)
                return ConservationStatus.Threatened;

            return ConservationStatus.Stable;
        }

        private ConservationStatus AssessGeneticDiversityStatus(float geneticDiversity)
        {
            if (geneticDiversity < 0.3f)
                return ConservationStatus.CriticallyEndangered;
            if (geneticDiversity < 0.5f)
                return ConservationStatus.Endangered;
            if (geneticDiversity < geneticDiversityThreshold)
                return ConservationStatus.Threatened;

            return ConservationStatus.Stable;
        }

        private ConservationStatus AssessHabitatStatus(float habitatQuality)
        {
            if (habitatQuality < 0.2f)
                return ConservationStatus.CriticallyEndangered;
            if (habitatQuality < 0.4f)
                return ConservationStatus.Endangered;
            if (habitatQuality < 0.6f)
                return ConservationStatus.Threatened;

            return ConservationStatus.Stable;
        }

        private ConservationStatus GetWorstConservationStatus(params ConservationStatus[] statuses)
        {
            var worst = ConservationStatus.Stable;
            foreach (var status in statuses)
            {
                if ((int)status > (int)worst)
                    worst = status;
            }
            return worst;
        }

        #endregion

        #region Conservation Records Management

        public void RegisterSpecies(string speciesId, string speciesName, int initialPopulation)
        {
            if (_conservationRecords.ContainsKey(speciesId))
            {
                Debug.LogWarning($"[ConservationManager] Species {speciesId} already registered");
                return;
            }

            var record = new ConservationRecord
            {
                speciesId = speciesId,
                speciesName = speciesName,
                currentPopulation = initialPopulation,
                historicalPopulation = new List<PopulationDataPoint>
                {
                    new PopulationDataPoint { timestamp = DateTime.UtcNow, population = initialPopulation }
                },
                conservationStatus = ConservationStatus.Stable,
                geneticDiversity = 1f,
                habitatQuality = 1f,
                registrationDate = DateTime.UtcNow,
                lastAssessment = DateTime.UtcNow
            };

            _conservationRecords[speciesId] = record;
            Debug.Log($"[ConservationManager] Registered species: {speciesName} ({speciesId})");
        }

        public void UpdateSpeciesPopulation(string speciesId, int newPopulation)
        {
            if (!_conservationRecords.TryGetValue(speciesId, out var record))
            {
                Debug.LogWarning($"[ConservationManager] Species {speciesId} not found for population update");
                return;
            }

            var oldPopulation = record.currentPopulation;
            record.currentPopulation = newPopulation;

            // Add to historical data
            record.historicalPopulation.Add(new PopulationDataPoint
            {
                timestamp = DateTime.UtcNow,
                population = newPopulation
            });

            // Limit historical data size
            if (record.historicalPopulation.Count > 1000)
            {
                record.historicalPopulation.RemoveAt(0);
            }

            Debug.Log($"[ConservationManager] Updated population for {record.speciesName}: {oldPopulation} -> {newPopulation}");
        }

        public void UpdateSpeciesGeneticDiversity(string speciesId, float geneticDiversity)
        {
            if (!_conservationRecords.TryGetValue(speciesId, out var record))
            {
                Debug.LogWarning($"[ConservationManager] Species {speciesId} not found for genetic diversity update");
                return;
            }

            record.geneticDiversity = Mathf.Clamp01(geneticDiversity);
        }

        public void UpdateSpeciesHabitatQuality(string speciesId, float habitatQuality)
        {
            if (!_conservationRecords.TryGetValue(speciesId, out var record))
            {
                Debug.LogWarning($"[ConservationManager] Species {speciesId} not found for habitat quality update");
                return;
            }

            record.habitatQuality = Mathf.Clamp01(habitatQuality);
        }

        public ConservationRecord GetSpeciesRecord(string speciesId)
        {
            return _conservationRecords.TryGetValue(speciesId, out var record) ? record : null;
        }

        public ConservationStatus GetSpeciesStatus(string speciesId)
        {
            return _conservationRecords.TryGetValue(speciesId, out var record)
                ? record.conservationStatus
                : ConservationStatus.Stable;
        }

        // Interface implementation
        public ConservationStatus GetConservationStatus(string speciesId)
        {
            return GetSpeciesStatus(speciesId);
        }

        public Task<bool> TriggerConservationActionAsync(string speciesId)
        {
            if (!_conservationRecords.TryGetValue(speciesId, out var record))
            {
                Debug.LogWarning($"[ConservationManager] Species {speciesId} not found for conservation action");
                return Task.FromResult(false);
            }

            // Determine appropriate action based on conservation status
            ConservationActionType actionType = record.conservationStatus switch
            {
                ConservationStatus.CriticallyEndangered => ConservationActionType.GeneticRescue,
                ConservationStatus.Endangered => ConservationActionType.BreedingProgram,
                ConservationStatus.Threatened => ConservationActionType.HabitatRestoration,
                _ => ConservationActionType.PopulationMonitoring
            };

            StartConservationAction(speciesId, actionType);
            return Task.FromResult(true);
        }

        public Dictionary<string, ConservationStatus> GetAllConservationStatus()
        {
            var result = new Dictionary<string, ConservationStatus>();
            foreach (var record in _conservationRecords.Values)
            {
                result[record.speciesId] = record.conservationStatus;
            }
            return result;
        }

        #endregion

        #region Conservation Actions

        public void StartConservationAction(string speciesId, ConservationActionType actionType, float duration = 300f)
        {
            if (_activeActions.ContainsKey($"{speciesId}_{actionType}"))
            {
                Debug.LogWarning($"[ConservationManager] Action {actionType} already active for species {speciesId}");
                return;
            }

            var action = new ConservationAction
            {
                actionId = Guid.NewGuid().ToString(),
                speciesId = speciesId,
                actionType = actionType,
                startTime = DateTime.UtcNow,
                duration = duration,
                isActive = true,
                progress = 0f
            };

            _activeActions[$"{speciesId}_{actionType}"] = action;
            OnConservationActionStarted?.Invoke(action);

            Debug.Log($"[ConservationManager] Started conservation action: {actionType} for species {speciesId}");
        }

        public void CompleteConservationAction(string speciesId, ConservationActionType actionType)
        {
            var key = $"{speciesId}_{actionType}";
            if (!_activeActions.TryGetValue(key, out var action))
            {
                Debug.LogWarning($"[ConservationManager] No active action {actionType} found for species {speciesId}");
                return;
            }

            action.isActive = false;
            action.progress = 1f;
            action.endTime = DateTime.UtcNow;

            ApplyConservationActionEffects(action);

            _activeActions.Remove(key);
            OnConservationActionCompleted?.Invoke(action);

            Debug.Log($"[ConservationManager] Completed conservation action: {actionType} for species {speciesId}");
        }

        private void ApplyConservationActionEffects(ConservationAction action)
        {
            if (!_conservationRecords.TryGetValue(action.speciesId, out var record))
                return;

            switch (action.actionType)
            {
                case ConservationActionType.BreedingProgram:
                    // Increase genetic diversity and population
                    record.geneticDiversity = Mathf.Min(1f, record.geneticDiversity + 0.1f);
                    UpdateSpeciesPopulation(action.speciesId, record.currentPopulation + UnityEngine.Random.Range(5, 15));
                    break;

                case ConservationActionType.HabitatRestoration:
                    // Improve habitat quality
                    record.habitatQuality = Mathf.Min(1f, record.habitatQuality + 0.2f);
                    break;

                case ConservationActionType.Reintroduction:
                    // Increase population
                    UpdateSpeciesPopulation(action.speciesId, record.currentPopulation + UnityEngine.Random.Range(10, 25));
                    break;

                case ConservationActionType.GeneticRescue:
                    // Significantly improve genetic diversity
                    record.geneticDiversity = Mathf.Min(1f, record.geneticDiversity + 0.3f);
                    break;
            }
        }

        #endregion

        #region Update Methods

        public void UpdateConservationStatus(float deltaTime, List<PopulationData> populations)
        {
            if (!enableConservationTracking)
                return;

            // Update conservation records with latest population data
            foreach (var population in populations)
            {
                if (_conservationRecords.TryGetValue(population.speciesId, out var record))
                {
                    UpdateSpeciesPopulation(population.speciesId, population.currentPopulation);
                }
                else
                {
                    // Register new species found in population data
                    RegisterSpecies(population.speciesId, population.speciesId, population.currentPopulation);
                }
            }
        }

        #endregion

        #region Helper Methods

        private void UpdatePopulationData(ConservationRecord record)
        {
            // This would integrate with the actual population management system
            // For now, we'll simulate some population dynamics
        }

        private void CalculateConservationTrends(ConservationRecord record)
        {
            if (record.historicalPopulation.Count < 2)
                return;

            var recent = record.historicalPopulation.TakeLast(10).ToList();
            if (recent.Count >= 2)
            {
                var trend = recent.Last().population - recent.First().population;
                record.populationTrend = trend > 0 ? PopulationTrend.Increasing :
                                       trend < 0 ? PopulationTrend.Decreasing :
                                       PopulationTrend.Stable;
            }
        }

        private void CheckForConservationAlerts(ConservationRecord record)
        {
            // Check for critical population decline
            if (record.populationTrend == PopulationTrend.Decreasing &&
                record.conservationStatus >= ConservationStatus.Endangered)
            {
                CreateConservationAlert(record.speciesId, ConservationAlertType.RapidDecline,
                    $"Species {record.speciesName} showing rapid population decline");
            }

            // Check for genetic bottleneck
            if (record.geneticDiversity < 0.3f)
            {
                CreateConservationAlert(record.speciesId, ConservationAlertType.GeneticBottleneck,
                    $"Species {record.speciesName} experiencing genetic bottleneck");
            }

            // Check for habitat loss
            if (record.habitatQuality < 0.4f)
            {
                CreateConservationAlert(record.speciesId, ConservationAlertType.HabitatLoss,
                    $"Species {record.speciesName} suffering from habitat degradation");
            }
        }

        private void CreateConservationAlert(string speciesId, ConservationAlertType alertType, string message)
        {
            // Check if alert already exists
            if (_activeAlerts.Any(a => a.speciesId == speciesId && a.alertType == alertType))
                return;

            var alert = new ConservationAlert
            {
                alertId = Guid.NewGuid().ToString(),
                speciesId = speciesId,
                alertType = alertType,
                message = message,
                timestamp = DateTime.UtcNow,
                severity = GetAlertSeverity(alertType),
                isResolved = false
            };

            _activeAlerts.Add(alert);
            OnConservationAlert?.Invoke(alert);
        }

        private ConservationAlertSeverity GetAlertSeverity(ConservationAlertType alertType)
        {
            return alertType switch
            {
                ConservationAlertType.RapidDecline => ConservationAlertSeverity.Critical,
                ConservationAlertType.GeneticBottleneck => ConservationAlertSeverity.High,
                ConservationAlertType.HabitatLoss => ConservationAlertSeverity.Medium,
                ConservationAlertType.InbreedingDetected => ConservationAlertSeverity.High,
                _ => ConservationAlertSeverity.Low
            };
        }

        private void HandleConservationStatusChange(ConservationRecord record, ConservationStatus previousStatus)
        {
            var conservationEvent = new ConservationEvent
            {
                speciesId = record.speciesId,
                speciesName = record.speciesName,
                previousStatus = previousStatus,
                newStatus = record.conservationStatus,
                timestamp = DateTime.UtcNow,
                currentPopulation = record.currentPopulation,
                geneticDiversity = record.geneticDiversity
            };

            OnConservationStatusChanged?.Invoke(conservationEvent);

            Debug.Log($"[ConservationManager] Status change for {record.speciesName}: {previousStatus} -> {record.conservationStatus}");
        }

        private void TriggerAutomaticInterventions()
        {
            foreach (var record in _conservationRecords.Values)
            {
                if (record.conservationStatus >= ConservationStatus.Endangered)
                {
                    // Trigger breeding programs for endangered species
                    if (enableBreedingPrograms && !_activeActions.ContainsKey($"{record.speciesId}_{ConservationActionType.BreedingProgram}"))
                    {
                        StartConservationAction(record.speciesId, ConservationActionType.BreedingProgram, 600f);
                    }

                    // Trigger habitat restoration for species with poor habitat
                    if (enableHabitatProtection && record.habitatQuality < 0.5f &&
                        !_activeActions.ContainsKey($"{record.speciesId}_{ConservationActionType.HabitatRestoration}"))
                    {
                        StartConservationAction(record.speciesId, ConservationActionType.HabitatRestoration, 900f);
                    }

                    // Trigger reintroduction for critically endangered species with good habitat
                    if (enableReintroduction && record.conservationStatus == ConservationStatus.CriticallyEndangered &&
                        record.habitatQuality > 0.6f &&
                        !_activeActions.ContainsKey($"{record.speciesId}_{ConservationActionType.Reintroduction}"))
                    {
                        StartConservationAction(record.speciesId, ConservationActionType.Reintroduction, 1200f);
                    }
                }
            }
        }

        private float CalculateEcosystemHealth(Dictionary<ConservationStatus, int> statusCounts)
        {
            var totalSpecies = statusCounts.Values.Sum();
            if (totalSpecies == 0)
                return 1f;

            var healthScore = 0f;
            healthScore += statusCounts.GetValueOrDefault(ConservationStatus.Stable, 0) * 1f;
            healthScore += statusCounts.GetValueOrDefault(ConservationStatus.Threatened, 0) * 0.8f;
            healthScore += statusCounts.GetValueOrDefault(ConservationStatus.Threatened, 0) * 0.6f;
            healthScore += statusCounts.GetValueOrDefault(ConservationStatus.Endangered, 0) * 0.4f;
            healthScore += statusCounts.GetValueOrDefault(ConservationStatus.CriticallyEndangered, 0) * 0.2f;
            // Extinct species contribute 0

            return healthScore / totalSpecies;
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Perform Assessment")]
        private void DebugPerformAssessment()
        {
            PerformConservationAssessment();
        }

        [ContextMenu("Add Test Species")]
        private void DebugAddTestSpecies()
        {
            RegisterSpecies("test_species_1", "Test Creature Alpha", 150);
            RegisterSpecies("test_species_2", "Test Creature Beta", 75);
        }

        #endregion
    }

    #region Supporting Classes and Enums

    [Serializable]
    public class ConservationRecord
    {
        public string speciesId;
        public string speciesName;
        public int currentPopulation;
        public List<PopulationDataPoint> historicalPopulation;
        public ConservationStatus conservationStatus;
        public PopulationTrend populationTrend;
        public float geneticDiversity;
        public float habitatQuality;
        public DateTime registrationDate;
        public DateTime lastAssessment;
    }

    [Serializable]
    public class PopulationDataPoint
    {
        public DateTime timestamp;
        public int population;
    }

    [Serializable]
    public class ConservationReport
    {
        public DateTime assessmentDate;
        public int speciesAssessed;
        public int extinctSpecies;
        public int criticallyEndangeredSpecies;
        public int endangeredSpecies;
        public int vulnerableSpecies;
        public int stableSpecies;
        public float overallEcosystemHealth;
        public int conservationActions;
        public int activeAlerts;
    }

    [Serializable]
    public class ConservationAction
    {
        public string actionId;
        public string speciesId;
        public ConservationActionType actionType;
        public DateTime startTime;
        public DateTime? endTime;
        public float duration;
        public bool isActive;
        public float progress;
    }

    [Serializable]
    public class ConservationAlert
    {
        public string alertId;
        public string speciesId;
        public ConservationAlertType alertType;
        public string message;
        public DateTime timestamp;
        public ConservationAlertSeverity severity;
        public bool isResolved;
    }


    public enum PopulationTrend
    {
        Increasing,
        Stable,
        Decreasing
    }

    public enum ConservationActionType
    {
        BreedingProgram,
        HabitatRestoration,
        Reintroduction,
        GeneticRescue,
        PopulationMonitoring,
        ThreatMitigation
    }

    public enum ConservationAlertType
    {
        RapidDecline,
        GeneticBottleneck,
        HabitatLoss,
        InbreedingDetected,
        PopulationCrash,
        ExtinctionRisk
    }

    public enum ConservationAlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}

