using UnityEngine;
using UnityEngine.UI;
using Unity.Profiling;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Genetics.Advanced;
using Laboratory.Systems.Ecosystem;
using Laboratory.Core.Enums;
using TMPro;

// Extension methods for breeding calculations
public static class BreedingExtensions
{
    public static float Variance(this List<float> values)
    {
        if (values.Count == 0) return 0f;

        float mean = values.Average();
        float sumOfSquares = values.Sum(x => (x - mean) * (x - mean));
        return sumOfSquares / values.Count;
    }
}

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
        private IPersonalityManager personalityManager;
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
        private List<BreedingRecord> generationHistory = new List<BreedingRecord>();

        // Performance profiling
        private static readonly ProfilerMarker s_UpdateBreedingMarker = new ProfilerMarker("AdvancedBreedingSimulator.UpdateActiveBreedingSessions");
        private static readonly ProfilerMarker s_UpdateUIMarker = new ProfilerMarker("AdvancedBreedingSimulator.UpdateUI");

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

            using (s_UpdateBreedingMarker.Auto())
            {
                UpdateActiveBreedingSessions();
            }

            using (s_UpdateUIMarker.Auto())
            {
                UpdateUI();
            }
        }

        private void InitializeBreedingSimulator()
        {
            Debug.Log("Initializing Advanced Breeding Simulator");

            // Initialize UI lists
            parentAList = new List<CreatureUI>();
            parentBList = new List<CreatureUI>();

            // Initialize selection
            currentSelection = new BreedingSelection();

            Debug.Log("Advanced Breeding Simulator initialized");
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
            personalityManager = PersonalityManager.GetInstance();

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
        /// Checks if breeding can be started with current conditions
        /// </summary>
        private bool CanStartBreeding()
        {
            // Check if we have valid parent selection
            if (currentSelection == null || !currentSelection.hasValidSelection)
            {
                return false;
            }

            // Check if there's already an active breeding session
            if (activeBreedingSessions.Any(session => session.status == BreedingStatus.InProgress))
            {
                return false;
            }

            // Check if parents are the same (can't breed with self)
            if (currentSelection.parentAId == currentSelection.parentBId)
            {
                return false;
            }

            // Check if parents exist in available parents
            if (!availableParents.ContainsKey(currentSelection.parentAId) ||
                !availableParents.ContainsKey(currentSelection.parentBId))
            {
                return false;
            }

            // Additional breeding condition checks could be added here:
            // - Species compatibility
            // - Genetic distance requirements
            // - Resource availability
            // - Cooldown periods

            return true;
        }

        /// <summary>
        /// Starts a breeding session with selected parents
        /// </summary>
        public BreedingSession StartBreeding()
        {
            if (!CanStartBreeding())
            {
                Debug.LogWarning("Cannot start breeding: conditions not met");
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

            Debug.Log($"Started breeding session {session.id}: Parent {session.parentAId} x Parent {session.parentBId}");

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
                Debug.Log($"Cancelled breeding session {currentSession.id}");
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
        /// Clears genetic analysis displays and data
        /// </summary>
        private void ClearGeneticAnalysis()
        {
            // Clear active trait visualizations
            foreach (var viz in activeTraitVisualizations)
            {
                if (viz.visualElement != null)
                    DestroyImmediate(viz.visualElement);
            }
            activeTraitVisualizations.Clear();

            // Clear breeding predictions and compatibility data
            // This would reset any UI displays showing:
            // - Compatibility scores
            // - Trait predictions
            // - Breeding recommendations
            // - Offspring previews

            if (selectionStatusText != null)
            {
                selectionStatusText.text = "Selection Status: No parents selected";
            }

            Debug.Log("Genetic analysis cleared");
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
                    Debug.Log($"Accepted offspring from breeding session {completedSession.id}");
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

                Debug.Log($"Rejected offspring from breeding session {completedSession.id}");
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

                Debug.Log($"Breeding session {session.id} completed. Offspring ID: {session.offspring.id}, Fitness: {session.offspring.fitness:F3}");
            }
            else
            {
                session.status = BreedingStatus.Failed;
                analytics.totalBreedingFailures++;
                Debug.LogWarning($"Breeding session {session.id} failed to generate offspring");
            }
        }

        private void RefreshAvailableParents()
        {
            availableParents.Clear();

            // Get creatures from genetic algorithm
            if (geneticAlgorithm != null)
            {
                var report = geneticAlgorithm.GeneratePopulationReport();
                foreach (var creature in report.topPerformers)
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
                    generation = (uint)Random.Range(1, 5),
                    fitness = Random.Range(0.3f, 0.9f),
                    traits = new Dictionary<TraitType, GeneticTrait>
                    {
                        [TraitType.Aggression] = new GeneticTrait { name = "aggression", value = Random.Range(0f, 1f), dominance = 0.5f, mutationRate = 0.1f },
                        [TraitType.Speed] = new GeneticTrait { name = "speed", value = Random.Range(0f, 1f), dominance = 0.5f, mutationRate = 0.1f },
                        [TraitType.Intelligence] = new GeneticTrait { name = "intelligence", value = Random.Range(0f, 1f), dominance = 0.5f, mutationRate = 0.1f },
                        [TraitType.Stamina] = new GeneticTrait { name = "stamina", value = Random.Range(0f, 1f), dominance = 0.5f, mutationRate = 0.1f },
                        [TraitType.Adaptability] = new GeneticTrait { name = "adaptability", value = Random.Range(0f, 1f), dominance = 0.5f, mutationRate = 0.1f }
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
            var traitValues = PredictTraitInheritance(parentA, parentB);
            predictions.traitPredictions = new Dictionary<TraitType, Vector2>();
            foreach (var trait in traitValues)
            {
                // Convert float to Vector2 range (predicted value ± 10% variance)
                float traitVariance = trait.Value * 0.1f;
                predictions.traitPredictions[trait.Key] = new Vector2(
                    Mathf.Max(0f, trait.Value - traitVariance),
                    Mathf.Min(1f, trait.Value + traitVariance)
                );
            }

            // Calculate confidence level
            predictions.confidenceLevel = CalculatePredictionConfidence();

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
                if (parentB.traits.TryGetValue(traitA.Key, out GeneticTrait traitB))
                {
                    totalDistance += Mathf.Abs(traitA.Value.value - traitB.value);
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

        private void UpdateBreedingProgress(BreedingSession session)
        {
            if (session != null)
            {
                session.progress = Mathf.Clamp01((Time.time - session.startTime) / session.expectedDuration);
            }
        }

        private void UpdateSelectionStatusText()
        {
            if (selectionStatusText != null)
            {
                selectionStatusText.text = "Selection Status: Ready";
            }
        }

        private float CalculateInbreedingCoefficient(CreatureGenome parentA, CreatureGenome parentB)
        {
            // Simple implementation - calculate based on shared ancestry
            if (parentA.parentA == parentB.parentA || parentA.parentB == parentB.parentB)
                return 0.5f;
            return 0.0f;
        }

        private Dictionary<TraitType, float> PredictTraitInheritance(CreatureGenome parentA, CreatureGenome parentB)
        {
            var predictions = new Dictionary<TraitType, float>();
            foreach (var trait in parentA.traits.Keys)
            {
                if (parentB.traits.ContainsKey(trait))
                {
                    predictions[trait] = (parentA.traits[trait].value + parentB.traits[trait].value) / 2f;
                }
            }
            return predictions;
        }

        private float CalculatePredictionConfidence()
        {
            return Random.Range(0.7f, 0.95f);
        }

        private float CalculateGenerationCompatibility(CreatureGenome parentA, CreatureGenome parentB)
        {
            float generationDiff = Mathf.Abs(parentA.generation - parentB.generation);
            return Mathf.Max(0f, 1f - (generationDiff * 0.1f));
        }

        private string DetermineRecommendationReason(BreedingPredictions predictions)
        {
            if (predictions.compatibilityScore > 0.8f)
                return "High genetic compatibility suggests strong offspring potential";
            else if (predictions.compatibilityScore > 0.6f)
                return "Good balance of traits with moderate compatibility";
            else if (predictions.fitnessRange.y > 0.7f)
                return "High fitness potential despite lower compatibility";
            else if (predictions.inbreedingCoefficient < 0.1f)
                return "Low inbreeding risk with diverse genetic background";
            else
                return "Moderate compatibility with average expected results";
        }

        private CreatureGenome GenerateOffspringFallback(CreatureGenome parentA, CreatureGenome parentB)
        {
            var offspring = new CreatureGenome
            {
                id = (uint)Random.Range(1000, 9999),
                generation = (uint)Mathf.Max(parentA.generation, parentB.generation) + 1,
                traits = new Dictionary<TraitType, GeneticTrait>()
            };

            // Combine traits from both parents
            var allTraitTypes = new HashSet<TraitType>();
            foreach (var trait in parentA.traits.Keys)
                allTraitTypes.Add(trait);
            foreach (var trait in parentB.traits.Keys)
                allTraitTypes.Add(trait);

            foreach (var traitType in allTraitTypes)
            {
                var traitA = parentA.traits.ContainsKey(traitType) ? parentA.traits[traitType] : new GeneticTrait { value = 0.5f };
                var traitB = parentB.traits.ContainsKey(traitType) ? parentB.traits[traitType] : new GeneticTrait { value = 0.5f };

                // Simple trait inheritance with some randomness
                float inheritedValue = Random.Range(0f, 1f) < 0.5f ? traitA.value : traitB.value;
                inheritedValue += Random.Range(-0.1f, 0.1f); // Small mutation
                inheritedValue = Mathf.Clamp01(inheritedValue);

                offspring.traits[traitType] = new GeneticTrait
                {
                    value = inheritedValue,
                    dominance = (traitA.dominance + traitB.dominance) / 2f,
                    mutationRate = (traitA.mutationRate + traitB.mutationRate) / 2f
                };
            }

            // Calculate fitness based on traits
            offspring.fitness = offspring.traits.Values.Average(t => t.value) * Random.Range(0.8f, 1.2f);
            offspring.fitness = Mathf.Clamp01(offspring.fitness);

            return offspring;
        }

        private BreedingResults AnalyzeActualResults(CreatureGenome offspring, BreedingPredictions predictions)
        {
            var results = new BreedingResults
            {
                actualFitness = offspring.fitness,
                predictedAccuracy = 1f - Mathf.Abs(offspring.fitness - (predictions.fitnessRange.x + predictions.fitnessRange.y) / 2f),
                traitInheritance = new Dictionary<TraitType, float>(),
                unexpectedTraits = new List<TraitType>(),
                performanceMetrics = new Dictionary<PerformanceMetric, float>()
            };

            // Analyze trait inheritance
            foreach (var trait in offspring.traits)
            {
                results.traitInheritance[trait.Key] = trait.Value.value;

                // Check for unexpected trait values
                if (trait.Value.value > 0.9f || trait.Value.value < 0.1f)
                {
                    results.unexpectedTraits.Add(trait.Key);
                }
            }

            // Calculate performance metrics
            results.performanceMetrics[PerformanceMetric.FitnessAccuracy] = results.predictedAccuracy;
            results.performanceMetrics[PerformanceMetric.TraitDiversity] = offspring.traits.Values.Select(t => t.value).ToList().Variance();
            results.performanceMetrics[PerformanceMetric.GenerationImprovement] = offspring.fitness - 0.5f; // Baseline comparison

            return results;
        }

        private void RecordGenerationData(BreedingSession session)
        {
            var record = new BreedingRecord
            {
                timestamp = Time.time,
                generation = session.offspring.generation,
                parentAId = session.parentAId,
                parentBId = session.parentBId,
                offspringId = session.offspring.id,
                parentAFitness = session.parentA.fitness,
                parentBFitness = session.parentB.fitness,
                offspringFitness = session.offspring.fitness,
                predictedFitness = (session.predictions.fitnessRange.x + session.predictions.fitnessRange.y) / 2f,
                compatibilityScore = session.predictions.compatibilityScore
            };

            generationHistory.Add(record);

            // Limit history size
            if (generationHistory.Count > 1000)
            {
                generationHistory.RemoveAt(0);
            }

            // Update analytics
            analytics.averageFitness = generationHistory.Average(r => r.offspringFitness);
            analytics.generationCount = generationHistory.Count;
            analytics.lastGenerationTime = Time.time;

            Debug.Log($"Recorded generation data: Gen {record.generation}, Fitness {record.offspringFitness:F3}");
        }

        private void UpdateOffspringPreview(BreedingSession session)
        {
            if (session.offspring == null) return;

            // Update offspring preview UI
            if (offspringPreviewPanel != null && offspringPreviewPanel.activeSelf)
            {
                // Update offspring stats display
                var statsText = offspringPreviewPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (statsText != null)
                {
                    statsText.text = $"Offspring ID: {session.offspring.id}\n" +
                                   $"Generation: {session.offspring.generation}\n" +
                                   $"Fitness: {session.offspring.fitness:F3}\n" +
                                   $"Traits: {session.offspring.traits.Count}";
                }

                // Update trait visualizations
                UpdateTraitVisualizations(session.offspring);
            }
        }

        private void UpdateParentLists()
        {
            // Clear existing UI elements
            foreach (var ui in parentAList)
            {
                if (ui.uiElement != null)
                    DestroyImmediate(ui.uiElement);
            }
            foreach (var ui in parentBList)
            {
                if (ui.uiElement != null)
                    DestroyImmediate(ui.uiElement);
            }

            parentAList.Clear();
            parentBList.Clear();

            // Create UI elements for each available parent
            foreach (var parent in availableParents.Values)
            {
                CreateParentUIElement(parent, true);  // For parent A list
                CreateParentUIElement(parent, false); // For parent B list
            }
        }

        private void CreateParentUIElement(CreatureGenome parent, bool isParentA)
        {
            // This would create UI elements for parent selection
            // For now, just add to the appropriate list
            var creatureUI = new CreatureUI
            {
                creatureId = parent.id,
                uiElement = null, // Would be created UI element
                selectionButton = null,
                statsText = null,
                fitnessBar = null
            };

            if (isParentA)
                parentAList.Add(creatureUI);
            else
                parentBList.Add(creatureUI);
        }

        private void UpdateTraitVisualizations(CreatureGenome creature)
        {
            // Clear existing visualizations
            foreach (var viz in activeTraitVisualizations)
            {
                if (viz.visualElement != null)
                    DestroyImmediate(viz.visualElement);
            }
            activeTraitVisualizations.Clear();

            // Create new visualizations for each trait
            foreach (var trait in creature.traits)
            {
                var visualization = new TraitVisualization
                {
                    traitName = trait.Key.ToString(),
                    visualElement = null, // Would be created UI element
                    traitBar = null,
                    traitLabel = null,
                    traitColor = GetTraitColor(trait.Key.ToString())
                };

                activeTraitVisualizations.Add(visualization);
            }
        }

        private Color GetTraitColor(string traitName)
        {
            // Return different colors for different traits
            return traitName switch
            {
                "strength" => Color.red,
                "speed" => Color.yellow,
                "intelligence" => Color.blue,
                "resilience" => Color.green,
                "adaptability" => Color.magenta,
                _ => Color.white
            };
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        // Editor menu items
#if UNITY_EDITOR
        [UnityEditor.MenuItem("🧪 Laboratory/Breeding/Show Breeding Simulator", false, 400)]
        private static void MenuShowBreedingSimulator()
        {
            if (Application.isPlaying && Instance != null)
            {
                Instance.ShowBreedingUI();
                Debug.Log("Breeding Simulator UI opened");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Breeding/Get Breeding Recommendations", false, 401)]
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
#endif

        /// <summary>
        /// Calculates the expected duration for a breeding session
        /// </summary>
        private float CalculateBreedingDuration()
        {
            // Base breeding duration with some randomization
            float baseDuration = 30f; // 30 seconds base
            float variability = Random.Range(0.8f, 1.2f); // ±20% variation
            return baseDuration * variability;
        }

        /// <summary>
        /// Updates the breeding selection UI display
        /// </summary>
        private void UpdateSelectionUI()
        {
            // Update UI elements to reflect current selection
            if (currentSelection.parentA != null)
            {
                Debug.Log($"Parent A selected: {currentSelection.parentA.id}");
            }
            if (currentSelection.parentB != null)
            {
                Debug.Log($"Parent B selected: {currentSelection.parentB.id}");
            }

            // This would typically update UI elements like:
            // - Selected creature portraits
            // - Trait displays
            // - Compatibility indicators
        }

        /// <summary>
        /// Analyzes breeding compatibility between selected parents
        /// </summary>
        private void AnalyzeBreedingCompatibility()
        {
            if (currentSelection.parentA == null || currentSelection.parentB == null)
                return;

            var compatibility = CalculateBreedingCompatibility(currentSelection.parentA, currentSelection.parentB);

            Debug.Log($"Breeding compatibility: {compatibility.compatibilityScore:F2}");
            Debug.Log($"Genetic diversity: {compatibility.geneticDiversity:F2}");
            Debug.Log($"Trait combination potential: {compatibility.traitSynergy:F2}");

            // This would typically:
            // - Update compatibility UI indicators
            // - Show potential offspring previews
            // - Display breeding recommendations
        }

        /// <summary>
        /// Calculates detailed breeding compatibility between two parent creatures
        /// </summary>
        private BreedingCompatibility CalculateBreedingCompatibility(CreatureGenome parentA, CreatureGenome parentB)
        {
            var compatibility = new BreedingCompatibility();

            // Calculate compatibility score using existing method
            compatibility.compatibilityScore = CalculateCompatibilityScore(parentA, parentB);

            // Calculate genetic diversity (higher is better for breeding)
            float sharedTraits = 0f;
            float totalTraits = 0f;

            var allTraitTypes = new HashSet<TraitType>();
            if (parentA.traits != null) foreach (var trait in parentA.traits.Keys) allTraitTypes.Add(trait);
            if (parentB.traits != null) foreach (var trait in parentB.traits.Keys) allTraitTypes.Add(trait);

            foreach (var traitType in allTraitTypes)
            {
                float valueA = parentA.traits?.ContainsKey(traitType) == true ? parentA.traits[traitType].value : 0.5f;
                float valueB = parentB.traits?.ContainsKey(traitType) == true ? parentB.traits[traitType].value : 0.5f;

                float difference = Mathf.Abs(valueA - valueB);
                sharedTraits += (1f - difference); // More similar = higher shared traits
                totalTraits += 1f;
            }

            compatibility.geneticDiversity = totalTraits > 0 ? 1f - (sharedTraits / totalTraits) : 0.5f;

            // Calculate trait synergy (how well traits work together)
            compatibility.traitSynergy = (compatibility.compatibilityScore + compatibility.geneticDiversity) / 2f;

            return compatibility;
        }

        /// <summary>
        /// Handles breeding completion events from the genetic algorithm system
        /// </summary>
        private void HandleSystemBreedingComplete(CreatureGenome parentA, CreatureGenome parentB, CreatureGenome offspring)
        {
            // Find the breeding session that matches these parents
            var matchingSession = activeBreedingSessions.FirstOrDefault(session =>
                (session.parentA?.id == parentA.id && session.parentB?.id == parentB.id) ||
                (session.parentA?.id == parentB.id && session.parentB?.id == parentA.id));

            if (matchingSession != null)
            {
                // Update the session with the offspring
                matchingSession.offspring = offspring;
                matchingSession.status = BreedingStatus.Completed;
                matchingSession.completionTime = Time.time;

                // Trigger our own breeding completion event
                OnBreedingCompleted?.Invoke(matchingSession, offspring);

                Debug.Log($"System breeding completed: Parents {parentA.id} x {parentB.id} produced offspring {offspring.id}");
            }
            else
            {
                Debug.LogWarning($"Received breeding completion for unknown session: Parents {parentA.id} x {parentB.id}");
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
        public Dictionary<TraitType, Vector2> traitPredictions = new Dictionary<TraitType, Vector2>();
        public float confidenceLevel;
    }

    [System.Serializable]
    public class BreedingCompatibility
    {
        public float compatibilityScore;
        public float geneticDiversity;
        public float traitSynergy;
    }

    [System.Serializable]
    public class BreedingResults
    {
        public float actualFitness;
        public Dictionary<TraitType, float> actualTraits = new Dictionary<TraitType, float>();
        public float predictionAccuracy;
        public bool exceededExpectations;
        public float predictedAccuracy;
        public Dictionary<TraitType, float> traitInheritance = new Dictionary<TraitType, float>();
        public List<TraitType> unexpectedTraits = new List<TraitType>();
        public Dictionary<PerformanceMetric, float> performanceMetrics = new Dictionary<PerformanceMetric, float>();
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
        public float averageFitness;
        public int generationCount;
        public float lastGenerationTime;
    }

    [System.Serializable]
    public class BreedingRecord
    {
        public float timestamp;
        public uint generation;
        public uint parentAId;
        public uint parentBId;
        public uint offspringId;
        public float parentAFitness;
        public float parentBFitness;
        public float offspringFitness;
        public float predictedFitness;
        public float compatibilityScore;
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