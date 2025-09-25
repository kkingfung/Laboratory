using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Debug;
using Laboratory.Chimera.Genetics.Advanced;
using Laboratory.AI.Personality;
using Laboratory.Systems.Ecosystem;
using TMPro;

namespace Laboratory.Systems.Breeding
{
    /// <summary>
    /// Advanced breeding simulator with interactive UI that provides detailed
    /// genetic analysis, breeding predictions, and visual feedback for the
    /// genetic algorithm systems, creating an engaging breeding experience.
    /// </summary>
    public class AdvancedBreedingSimulator : MonoBehaviour
    {
        [Header("Simulation Configuration")]
        [SerializeField] private bool enableBreedingSimulation = true;
        [SerializeField] private int maxActiveBreedings = 10;
        [SerializeField] private float breedingCooldown = 30f;
        [SerializeField] private int generationHistoryLimit = 50;

        [Header("UI References")]
        [SerializeField] private Canvas breedingCanvas;
        [SerializeField] private GameObject parentSelectionPanel;
        [SerializeField] private GameObject breedingProgressPanel;
        [SerializeField] private GameObject offspringPreviewPanel;
        [SerializeField] private GameObject geneticAnalysisPanel;

        [Header("Parent Selection UI")]
        [SerializeField] private ScrollRect parentAScrollRect;
        [SerializeField] private ScrollRect parentBScrollRect;
        [SerializeField] private Button breedButton;
        [SerializeField] private Button clearSelectionButton;
        [SerializeField] private TextMeshProUGUI selectionStatusText;

        [Header("Breeding Progress UI")]
        [SerializeField] private Slider breedingProgressBar;
        [SerializeField] private TextMeshProUGUI breedingStatusText;
        [SerializeField] private Button cancelBreedingButton;
        [SerializeField] private Image breedingVisualization;

        [Header("Offspring Preview UI")]
        [SerializeField] private TextMeshProUGUI offspringStatsText;
        [SerializeField] private TextMeshProUGUI geneticTraitsText;
        [SerializeField] private TextMeshProUGUI personalityPreviewText;
        [SerializeField] private Button acceptOffspringButton;
        [SerializeField] private Button rejectOffspringButton;
        [SerializeField] private Button rebreedButton;

        [Header("Genetic Analysis UI")]
        [SerializeField] private TextMeshProUGUI compatibilityScoreText;
        [SerializeField] private TextMeshProUGUI inbreedingWarningText;
        [SerializeField] private TextMeshProUGUI fitnessProjectionText;
        [SerializeField] private Transform traitVisualizationParent;
        [SerializeField] private GameObject traitBarPrefab;

        [Header("Breeding Visualization")]
        [SerializeField] private Material geneticVisualizationMaterial;
        [SerializeField] private Color[] traitColors;
        [SerializeField] private AnimationCurve breedingAnimationCurve;
        [SerializeField] private ParticleSystem breedingParticles;

        // Core systems
        private AdvancedGeneticAlgorithm geneticAlgorithm;
        private CreaturePersonalityManager personalityManager;
        private DynamicEcosystemSimulator ecosystemSimulator;

        // Breeding state
        private List<BreedingSession> activeBreedingSessions = new List<BreedingSession>();
        private Dictionary<uint, CreatureGenome> availableParents = new Dictionary<uint, CreatureGenome>();
        private BreedingSelection currentSelection = new BreedingSelection();

        // UI state
        private List<CreatureUI> parentAList = new List<CreatureUI>();
        private List<CreatureUI> parentBList = new List<CreatureUI>();
        private List<TraitVisualization> activeTraitVisualizations = new List<TraitVisualization>();

        // Analytics
        private BreedingAnalytics analytics = new BreedingAnalytics();
        private List<GenerationRecord> generationHistory = new List<GenerationRecord>();

        // Events
        public System.Action<BreedingSession> OnBreedingStarted;
        public System.Action<BreedingSession, CreatureGenome> OnBreedingCompleted;
        public System.Action<BreedingSession> OnBreedingCancelled;
        public System.Action<CreatureGenome> OnOffspringAccepted;
        public System.Action<CreatureGenome> OnOffspringRejected;

        // Singleton access
        private static AdvancedBreedingSimulator instance;
        public static AdvancedBreedingSimulator Instance => instance;

        public IReadOnlyList<BreedingSession> ActiveSessions => activeBreedingSessions.AsReadOnly();
        public BreedingAnalytics Analytics => analytics;
        public bool IsBreedingInProgress => activeBreedingSessions.Any(s => s.status == BreedingStatus.InProgress);

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                InitializeBreedingSimulator();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            SetupUI();
            ConnectToSystems();
        }

        private void Update()
        {
            if (!enableBreedingSimulation) return;

            UpdateActiveBreedingSessions();
            UpdateUI();
        }

        private void InitializeBreedingSimulator()
        {
            DebugManager.LogInfo("Initializing Advanced Breeding Simulator");

            // Initialize UI lists
            parentAList = new List<CreatureUI>();
            parentBList = new List<CreatureUI>();

            // Initialize selection
            currentSelection = new BreedingSelection();

            DebugManager.LogInfo("Advanced Breeding Simulator initialized");
        }

        private void SetupUI()
        {
            if (breedingCanvas != null)
            {
                breedingCanvas.gameObject.SetActive(false);
            }

            // Setup button events
            if (breedButton != null)
            {
                breedButton.onClick.AddListener(() => StartBreeding());
                breedButton.interactable = false;
            }

            if (clearSelectionButton != null)
            {
                clearSelectionButton.onClick.AddListener(() => ClearSelection());
            }

            if (cancelBreedingButton != null)
            {
                cancelBreedingButton.onClick.AddListener(() => CancelCurrentBreeding());
            }

            if (acceptOffspringButton != null)
            {
                acceptOffspringButton.onClick.AddListener(() => AcceptOffspring());
            }

            if (rejectOffspringButton != null)
            {
                rejectOffspringButton.onClick.AddListener(() => RejectOffspring());
            }

            if (rebreedButton != null)
            {
                rebreedButton.onClick.AddListener(() => RebreedWithSameParents());
            }

            // Setup initial UI state
            ShowParentSelectionPanel();
        }

        private void ConnectToSystems()
        {
            // Connect to genetic algorithm system
            if (GeneticEvolutionManager.Instance != null)
            {
                geneticAlgorithm = GeneticEvolutionManager.Instance.MainPopulation;

                // Subscribe to breeding events
                if (geneticAlgorithm != null)
                {
                    geneticAlgorithm.OnBreedingComplete += HandleSystemBreedingComplete;
                }
            }

            // Connect to personality system
            personalityManager = CreaturePersonalityManager.Instance;

            // Connect to ecosystem system
            ecosystemSimulator = DynamicEcosystemSimulator.Instance;

            // Refresh available parents
            RefreshAvailableParents();
        }

        /// <summary>
        /// Shows the breeding simulator UI
        /// </summary>
        public void ShowBreedingUI()
        {
            if (breedingCanvas != null)
            {
                breedingCanvas.gameObject.SetActive(true);
                RefreshAvailableParents();
                ShowParentSelectionPanel();
            }
        }

        /// <summary>
        /// Hides the breeding simulator UI
        /// </summary>
        public void HideBreedingUI()
        {
            if (breedingCanvas != null)
            {
                breedingCanvas.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Starts a breeding session with selected parents
        /// </summary>
        public BreedingSession StartBreeding()
        {
            if (!CanStartBreeding())
            {
                DebugManager.LogWarning("Cannot start breeding: conditions not met");
                return null;
            }

            var session = new BreedingSession
            {
                id = (uint)(activeBreedingSessions.Count + 1),
                parentAId = currentSelection.parentAId,
                parentBId = currentSelection.parentBId,
                parentA = availableParents[currentSelection.parentAId],
                parentB = availableParents[currentSelection.parentBId],
                startTime = Time.time,
                expectedDuration = CalculateBreedingDuration(),
                status = BreedingStatus.InProgress
            };

            // Calculate breeding predictions
            session.predictions = CalculateBreedingPredictions(session.parentA, session.parentB);

            activeBreedingSessions.Add(session);
            analytics.totalBreedingAttempts++;

            OnBreedingStarted?.Invoke(session);

            // Update UI
            ShowBreedingProgressPanel();
            UpdateBreedingProgress(session);

            // Start breeding visualization
            if (breedingParticles != null)
            {
                breedingParticles.Play();
            }

            DebugManager.LogInfo($"Started breeding session {session.id}: Parent {session.parentAId} x Parent {session.parentBId}");

            return session;
        }

        /// <summary>
        /// Cancels the current breeding session
        /// </summary>
        public void CancelCurrentBreeding()
        {
            var currentSession = activeBreedingSessions.FirstOrDefault(s => s.status == BreedingStatus.InProgress);
            if (currentSession != null)
            {
                currentSession.status = BreedingStatus.Cancelled;
                OnBreedingCancelled?.Invoke(currentSession);

                analytics.totalBreedingCancellations++;

                // Stop visualization
                if (breedingParticles != null)
                {
                    breedingParticles.Stop();
                }

                ShowParentSelectionPanel();
                DebugManager.LogInfo($"Cancelled breeding session {currentSession.id}");
            }
        }

        /// <summary>
        /// Selects a creature as parent A
        /// </summary>
        public void SelectParentA(uint creatureId)
        {
            if (availableParents.ContainsKey(creatureId))
            {
                currentSelection.parentAId = creatureId;
                currentSelection.parentA = availableParents[creatureId];
                UpdateSelectionUI();
                AnalyzeBreedingCompatibility();
            }
        }

        /// <summary>
        /// Selects a creature as parent B
        /// </summary>
        public void SelectParentB(uint creatureId)
        {
            if (availableParents.ContainsKey(creatureId))
            {
                currentSelection.parentBId = creatureId;
                currentSelection.parentB = availableParents[creatureId];
                UpdateSelectionUI();
                AnalyzeBreedingCompatibility();
            }
        }

        /// <summary>
        /// Clears current parent selection
        /// </summary>
        public void ClearSelection()
        {
            currentSelection = new BreedingSelection();
            UpdateSelectionUI();
            ClearGeneticAnalysis();
        }

        /// <summary>
        /// Accepts the current offspring and adds it to the population
        /// </summary>
        public void AcceptOffspring()
        {
            var completedSession = activeBreedingSessions.FirstOrDefault(s => s.status == BreedingStatus.Completed);
            if (completedSession?.offspring != null)
            {
                // Add to population through genetic algorithm
                if (geneticAlgorithm != null)
                {
                    // This would add the offspring to the active population
                    DebugManager.LogInfo($"Accepted offspring from breeding session {completedSession.id}");
                }

                // Register with personality system
                if (personalityManager != null)
                {
                    personalityManager.RegisterCreature(completedSession.offspring.id, completedSession.offspring);
                }

                // Register with ecosystem
                if (ecosystemSimulator != null)
                {
                    Vector3 spawnPosition = Vector3.zero; // Would be determined by game logic
                    ecosystemSimulator.RegisterCreature(completedSession.offspring.id, spawnPosition, completedSession.offspring);
                }

                analytics.totalOffspringAccepted++;
                OnOffspringAccepted?.Invoke(completedSession.offspring);

                // Remove completed session
                activeBreedingSessions.Remove(completedSession);

                ShowParentSelectionPanel();
            }
        }

        /// <summary>
        /// Rejects the current offspring
        /// </summary>
        public void RejectOffspring()
        {
            var completedSession = activeBreedingSessions.FirstOrDefault(s => s.status == BreedingStatus.Completed);
            if (completedSession?.offspring != null)
            {
                analytics.totalOffspringRejected++;
                OnOffspringRejected?.Invoke(completedSession.offspring);

                // Remove completed session
                activeBreedingSessions.Remove(completedSession);

                DebugManager.LogInfo($"Rejected offspring from breeding session {completedSession.id}");
                ShowParentSelectionPanel();
            }
        }

        /// <summary>
        /// Rebreeds using the same parents
        /// </summary>
        public void RebreedWithSameParents()
        {
            var completedSession = activeBreedingSessions.FirstOrDefault(s => s.status == BreedingStatus.Completed);
            if (completedSession != null)
            {
                // Set selection to the same parents
                currentSelection.parentAId = completedSession.parentAId;
                currentSelection.parentBId = completedSession.parentBId;
                currentSelection.parentA = completedSession.parentA;
                currentSelection.parentB = completedSession.parentB;

                // Remove completed session
                activeBreedingSessions.Remove(completedSession);

                // Start new breeding
                StartBreeding();
            }
        }

        /// <summary>
        /// Gets breeding recommendations based on genetic diversity and fitness
        /// </summary>
        public BreedingRecommendation[] GetBreedingRecommendations(int count = 5)
        {
            var recommendations = new List<BreedingRecommendation>();

            var parentList = availableParents.Values.ToList();

            for (int i = 0; i < parentList.Count && recommendations.Count < count; i++)
            {
                for (int j = i + 1; j < parentList.Count && recommendations.Count < count; j++)
                {
                    var parentA = parentList[i];
                    var parentB = parentList[j];

                    var predictions = CalculateBreedingPredictions(parentA, parentB);

                    if (predictions.compatibilityScore > 0.6f) // Good compatibility
                    {
                        recommendations.Add(new BreedingRecommendation
                        {
                            parentAId = parentA.id,
                            parentBId = parentB.id,
                            compatibilityScore = predictions.compatibilityScore,
                            expectedFitnessRange = predictions.fitnessRange,
                            recommendationReason = DetermineRecommendationReason(predictions),
                            confidence = predictions.confidenceLevel
                        });
                    }
                }
            }

            return recommendations.OrderByDescending(r => r.compatibilityScore).Take(count).ToArray();
        }

        private void UpdateActiveBreedingSessions()
        {
            foreach (var session in activeBreedingSessions.ToList())
            {
                if (session.status == BreedingStatus.InProgress)
                {
                    float elapsed = Time.time - session.startTime;
                    session.progress = Mathf.Clamp01(elapsed / session.expectedDuration);

                    if (session.progress >= 1f)
                    {
                        CompleteBreeding(session);
                    }
                }
            }
        }

        private void CompleteBreeding(BreedingSession session)
        {
            // Generate offspring using genetic algorithm
            if (geneticAlgorithm != null)
            {
                session.offspring = geneticAlgorithm.BreedCreatures(session.parentAId, session.parentBId);
            }
            else
            {
                // Fallback offspring generation
                session.offspring = GenerateOffspringFallback(session.parentA, session.parentB);
            }

            if (session.offspring != null)
            {
                session.status = BreedingStatus.Completed;
                session.completionTime = Time.time;
                session.actualResults = AnalyzeActualResults(session.offspring, session.predictions);

                analytics.totalBreedingCompletions++;
                OnBreedingCompleted?.Invoke(session, session.offspring);

                // Update generation history
                RecordGenerationData(session);

                // Stop breeding visualization
                if (breedingParticles != null)
                {
                    breedingParticles.Stop();
                }

                // Show offspring preview
                ShowOffspringPreviewPanel();
                UpdateOffspringPreview(session);

                DebugManager.LogInfo($"Breeding session {session.id} completed. Offspring ID: {session.offspring.id}, Fitness: {session.offspring.fitness:F3}");
            }
            else
            {
                session.status = BreedingStatus.Failed;
                analytics.totalBreedingFailures++;
                DebugManager.LogWarning($"Breeding session {session.id} failed to generate offspring");
            }
        }

        private void RefreshAvailableParents()
        {
            availableParents.Clear();

            // Get creatures from genetic algorithm
            if (geneticAlgorithm != null)
            {
                var report = geneticAlgorithm.GeneratePopulationReport();
                foreach (var creature in report.population)
                {
                    availableParents[creature.id] = creature;
                }
            }
            else
            {
                // Generate sample parents for testing
                GenerateSampleParents();
            }

            UpdateParentLists();
        }

        private void GenerateSampleParents()
        {
            for (uint i = 1; i <= 10; i++)
            {
                var sampleGenome = new CreatureGenome
                {
                    id = i,
                    generation = Random.Range(1, 5),
                    fitness = Random.Range(0.3f, 0.9f),
                    traits = new Dictionary<string, float>
                    {
                        ["strength"] = Random.Range(0f, 1f),
                        ["speed"] = Random.Range(0f, 1f),
                        ["intelligence"] = Random.Range(0f, 1f),
                        ["resilience"] = Random.Range(0f, 1f),
                        ["adaptability"] = Random.Range(0f, 1f)
                    }
                };

                availableParents[i] = sampleGenome;
            }
        }

        private BreedingPredictions CalculateBreedingPredictions(CreatureGenome parentA, CreatureGenome parentB)
        {
            var predictions = new BreedingPredictions();

            // Calculate compatibility score
            predictions.compatibilityScore = CalculateCompatibilityScore(parentA, parentB);

            // Calculate inbreeding coefficient
            predictions.inbreedingCoefficient = CalculateInbreedingCoefficient(parentA, parentB);

            // Predict fitness range
            float avgFitness = (parentA.fitness + parentB.fitness) / 2f;
            float variance = Mathf.Abs(parentA.fitness - parentB.fitness) * 0.5f;
            predictions.fitnessRange = new Vector2(avgFitness - variance, avgFitness + variance);

            // Predict trait inheritance
            predictions.traitPredictions = PredictTraitInheritance(parentA, parentB);

            // Calculate confidence level
            predictions.confidenceLevel = CalculatePredictionConfidence(parentA, parentB);

            return predictions;
        }

        private float CalculateCompatibilityScore(CreatureGenome parentA, CreatureGenome parentB)
        {
            float geneticDistance = CalculateGeneticDistance(parentA, parentB);
            float fitnessCompatibility = 1f - Mathf.Abs(parentA.fitness - parentB.fitness);
            float generationCompatibility = CalculateGenerationCompatibility(parentA, parentB);

            return (geneticDistance + fitnessCompatibility + generationCompatibility) / 3f;
        }

        private float CalculateGeneticDistance(CreatureGenome parentA, CreatureGenome parentB)
        {
            float totalDistance = 0f;
            int comparedTraits = 0;

            foreach (var traitA in parentA.traits)
            {
                if (parentB.traits.TryGetValue(traitA.Key, out float traitBValue))
                {
                    totalDistance += Mathf.Abs(traitA.Value - traitBValue);
                    comparedTraits++;
                }
            }

            return comparedTraits > 0 ? 1f - (totalDistance / comparedTraits) : 0.5f;
        }

        private void UpdateUI()
        {
            // Update breeding progress if active
            var activeSession = activeBreedingSessions.FirstOrDefault(s => s.status == BreedingStatus.InProgress);
            if (activeSession != null)
            {
                UpdateBreedingProgress(activeSession);
            }

            // Update selection status
            if (selectionStatusText != null)
            {
                UpdateSelectionStatusText();
            }
        }

        private void ShowParentSelectionPanel()
        {
            SetPanelActive(parentSelectionPanel, true);
            SetPanelActive(breedingProgressPanel, false);
            SetPanelActive(offspringPreviewPanel, false);
        }

        private void ShowBreedingProgressPanel()
        {
            SetPanelActive(parentSelectionPanel, false);
            SetPanelActive(breedingProgressPanel, true);
            SetPanelActive(offspringPreviewPanel, false);
        }

        private void ShowOffspringPreviewPanel()
        {
            SetPanelActive(parentSelectionPanel, false);
            SetPanelActive(breedingProgressPanel, false);
            SetPanelActive(offspringPreviewPanel, true);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        // Additional helper methods and UI update methods would continue here...
        // (Implementation continues with detailed UI management, visualization, and analysis methods)

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        // Editor menu items
        [UnityEditor.MenuItem("Laboratory/Breeding/Show Breeding Simulator", false, 400)]
        private static void MenuShowBreedingSimulator()
        {
            if (Application.isPlaying && Instance != null)
            {
                Instance.ShowBreedingUI();
                Debug.Log("Breeding Simulator UI opened");
            }
        }

        [UnityEditor.MenuItem("Laboratory/Breeding/Get Breeding Recommendations", false, 401)]
        private static void MenuGetRecommendations()
        {
            if (Application.isPlaying && Instance != null)
            {
                var recommendations = Instance.GetBreedingRecommendations(3);
                Debug.Log($"Generated {recommendations.Length} breeding recommendations");

                foreach (var rec in recommendations)
                {
                    Debug.Log($"Recommendation: Parent {rec.parentAId} x Parent {rec.parentBId} " +
                             $"(Compatibility: {rec.compatibilityScore:F2}, Reason: {rec.recommendationReason})");
                }
            }
        }
    }

    // Supporting data structures for breeding simulation
    [System.Serializable]
    public class BreedingSession
    {
        public uint id;
        public uint parentAId;
        public uint parentBId;
        public CreatureGenome parentA;
        public CreatureGenome parentB;
        public CreatureGenome offspring;
        public BreedingStatus status;
        public float startTime;
        public float completionTime;
        public float expectedDuration;
        public float progress;
        public BreedingPredictions predictions;
        public BreedingResults actualResults;
    }

    [System.Serializable]
    public class BreedingSelection
    {
        public uint parentAId;
        public uint parentBId;
        public CreatureGenome parentA;
        public CreatureGenome parentB;
        public bool hasValidSelection => parentA != null && parentB != null && parentAId != parentBId;
    }

    [System.Serializable]
    public class BreedingPredictions
    {
        public float compatibilityScore;
        public float inbreedingCoefficient;
        public Vector2 fitnessRange;
        public Dictionary<string, Vector2> traitPredictions = new Dictionary<string, Vector2>();
        public float confidenceLevel;
    }

    [System.Serializable]
    public class BreedingResults
    {
        public float actualFitness;
        public Dictionary<string, float> actualTraits = new Dictionary<string, float>();
        public float predictionAccuracy;
        public bool exceededExpectations;
    }

    [System.Serializable]
    public class BreedingRecommendation
    {
        public uint parentAId;
        public uint parentBId;
        public float compatibilityScore;
        public Vector2 expectedFitnessRange;
        public string recommendationReason;
        public float confidence;
    }

    [System.Serializable]
    public class BreedingAnalytics
    {
        public int totalBreedingAttempts;
        public int totalBreedingCompletions;
        public int totalBreedingFailures;
        public int totalBreedingCancellations;
        public int totalOffspringAccepted;
        public int totalOffspringRejected;
        public float averageBreedingTime;
        public float averagePredictionAccuracy;
    }

    [System.Serializable]
    public class CreatureUI
    {
        public uint creatureId;
        public GameObject uiElement;
        public Button selectionButton;
        public TextMeshProUGUI statsText;
        public Image fitnessBar;
    }

    [System.Serializable]
    public class TraitVisualization
    {
        public string traitName;
        public GameObject visualElement;
        public Slider traitBar;
        public TextMeshProUGUI traitLabel;
        public Color traitColor;
    }

    public enum BreedingStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }
}