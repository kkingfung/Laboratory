using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Visuals;
using Laboratory.Chimera.UI.Components;
using Laboratory.Core.Events;
using System.Collections;

namespace Laboratory.Chimera.UI
{
    /// <summary>
    /// Complete Advanced Breeding UI System with parent selection, genetic preview,
    /// compatibility analysis, and breeding recommendations.
    /// </summary>
    public class AdvancedBreedingUI : MonoBehaviour
    {
        [Header("Parent Selection")]
        [SerializeField] private Transform parent1SelectionArea;
        [SerializeField] private Transform parent2SelectionArea;
        [SerializeField] private Button parent1SelectButton;
        [SerializeField] private Button parent2SelectButton;
        [SerializeField] private Image parent1Portrait;
        [SerializeField] private Image parent2Portrait;
        [SerializeField] private TextMeshProUGUI parent1NameText;
        [SerializeField] private TextMeshProUGUI parent2NameText;
        
        [Header("Genetic Preview")]
        [SerializeField] private Transform geneticPreviewArea;
        [SerializeField] private GameObject traitPreviewPrefab;
        [SerializeField] private ScrollRect geneticScrollView;
        [SerializeField] private Transform compatibilityIndicator;
        [SerializeField] private Slider compatibilitySlider;
        [SerializeField] private TextMeshProUGUI compatibilityText;
        
        [Header("Offspring Preview")]
        [SerializeField] private Transform offspringPreviewArea;
        [SerializeField] private Image offspringPreviewImage;
        [SerializeField] private TextMeshProUGUI predictedStatsText;
        [SerializeField] private Transform probabilityBarsParent;
        [SerializeField] private GameObject probabilityBarPrefab;
        
        [Header("Breeding Controls")]
        [SerializeField] private Button breedButton;
        [SerializeField] private Button previewButton;
        [SerializeField] private Button swapParentsButton;
        [SerializeField] private Button clearSelectionButton;
        [SerializeField] private Toggle advancedModeToggle;
        
        [Header("Cost & Requirements")]
        [SerializeField] private TextMeshProUGUI breedingCostText;
        [SerializeField] private TextMeshProUGUI requirementsText;
        [SerializeField] private Image energyBar;
        [SerializeField] private TextMeshProUGUI timeEstimateText;
        
        [Header("Recommendations")]
        [SerializeField] private Transform recommendationsPanel;
        [SerializeField] private GameObject recommendationItemPrefab;
        [SerializeField] private ScrollRect recommendationsScrollView;
        
        // Current breeding state
        private CreatureInstanceComponent selectedParent1;
        private CreatureInstanceComponent selectedParent2;
        private BreedingCompatibilityAnalyzer compatibilityAnalyzer;
        private GeneticPredictor geneticPredictor;
        private List<TraitPreviewItem> activeTraitPreviews = new List<TraitPreviewItem>();
        
        // UI State
        private bool isAdvancedMode = false;
        private bool isBreedingInProgress = false;
        private BreedingPrediction currentPrediction;
        
        private void Awake()
        {
            InitializeComponents();
            SetupEventListeners();
        }
        
        private void Start()
        {
            UpdateUI();
            LoadBreedingRecommendations();
        }
        
        #region Initialization
        
        private void InitializeComponents()
        {
            compatibilityAnalyzer = new BreedingCompatibilityAnalyzer();
            geneticPredictor = new GeneticPredictor();
            
            // Initialize UI state
            breedButton.interactable = false;
            previewButton.interactable = false;
            swapParentsButton.interactable = false;
        }
        
        private void SetupEventListeners()
        {
            // Parent selection buttons
            parent1SelectButton.onClick.AddListener(() => OpenParentSelection(1));
            parent2SelectButton.onClick.AddListener(() => OpenParentSelection(2));
            
            // Breeding controls
            breedButton.onClick.AddListener(StartBreedingProcess);
            previewButton.onClick.AddListener(GenerateOffspringPreview);
            swapParentsButton.onClick.AddListener(SwapParents);
            clearSelectionButton.onClick.AddListener(ClearParentSelection);
            
            // Advanced mode toggle
            advancedModeToggle.onValueChanged.AddListener(ToggleAdvancedMode);
            
            // Listen for creature selection events
            UnityEngine.Debug.Log("AdvancedBreedingUI: Event listeners setup (GameEvents system not implemented)");
        }
        
        #endregion
        
        #region Parent Selection
        
        private void OpenParentSelection(int parentSlot)
        {
            var collectionManager = FindFirstObjectByType<CreatureCollectionManager>();
            if (collectionManager != null)
            {
                collectionManager.OpenCreatureSelection((creature) => {
                    if (parentSlot == 1)
                        SetParent1(creature);
                    else
                        SetParent2(creature);
                });
            }
        }
        
        public void SetParent1(CreatureInstanceComponent creature)
        {
            selectedParent1 = creature;
            UpdateParent1Display();
            UpdateBreedingPreview();
            UpdateCompatibilityAnalysis();
            UpdateUI();
        }
        
        public void SetParent2(CreatureInstanceComponent creature)
        {
            selectedParent2 = creature;
            UpdateParent2Display();
            UpdateBreedingPreview();
            UpdateCompatibilityAnalysis();
            UpdateUI();
        }
        
        private void UpdateParent1Display()
        {
            if (selectedParent1 != null)
            {
                parent1NameText.text = selectedParent1.name;
                parent1Portrait.sprite = GetCreaturePortrait(selectedParent1);
                parent1Portrait.color = Color.white;
                parent1SelectButton.GetComponentInChildren<TextMeshProUGUI>().text = "Change Parent 1";
            }
            else
            {
                parent1NameText.text = "Select Parent 1";
                parent1Portrait.color = Color.gray;
                parent1SelectButton.GetComponentInChildren<TextMeshProUGUI>().text = "Select Parent 1";
            }
        }
        
        private void UpdateParent2Display()
        {
            if (selectedParent2 != null)
            {
                parent2NameText.text = selectedParent2.name;
                parent2Portrait.sprite = GetCreaturePortrait(selectedParent2);
                parent2Portrait.color = Color.white;
                parent2SelectButton.GetComponentInChildren<TextMeshProUGUI>().text = "Change Parent 2";
            }
            else
            {
                parent2NameText.text = "Select Parent 2";
                parent2Portrait.color = Color.gray;
                parent2SelectButton.GetComponentInChildren<TextMeshProUGUI>().text = "Select Parent 2";
            }
        }
        
        private void SwapParents()
        {
            if (selectedParent1 != null && selectedParent2 != null)
            {
                var temp = selectedParent1;
                selectedParent1 = selectedParent2;
                selectedParent2 = temp;
                
                UpdateParent1Display();
                UpdateParent2Display();
                UpdateBreedingPreview();
                UpdateCompatibilityAnalysis();
            }
        }
        
        private void ClearParentSelection()
        {
            selectedParent1 = null;
            selectedParent2 = null;
            
            UpdateParent1Display();
            UpdateParent2Display();
            ClearBreedingPreview();
            UpdateUI();
        }
        
        #endregion
        
        #region Genetic Preview & Compatibility
        
        private void UpdateCompatibilityAnalysis()
        {
            if (selectedParent1 == null || selectedParent2 == null)
            {
                compatibilitySlider.value = 0f;
                compatibilityText.text = "Select both parents";
                compatibilityIndicator.gameObject.SetActive(false);
                return;
            }
            
            var compatibility = compatibilityAnalyzer.AnalyzeCompatibility(
                selectedParent1.CreatureData?.GeneticProfile,
                selectedParent2.CreatureData?.GeneticProfile
            );
            
            compatibilitySlider.value = compatibility.OverallCompatibility;
            compatibilityText.text = GetCompatibilityDescription(compatibility.OverallCompatibility);
            compatibilityIndicator.gameObject.SetActive(true);
            
            // Update compatibility indicator color
            Color indicatorColor = Color.Lerp(Color.red, Color.green, compatibility.OverallCompatibility);
            compatibilitySlider.fillRect.GetComponent<Image>().color = indicatorColor;
            
            // Show detailed compatibility info in advanced mode
            if (isAdvancedMode)
            {
                DisplayDetailedCompatibility(compatibility);
            }
        }
        
        private string GetCompatibilityDescription(float compatibility)
        {
            if (compatibility >= 0.9f) return "Perfect Match!";
            if (compatibility >= 0.8f) return "Excellent";
            if (compatibility >= 0.7f) return "Very Good";
            if (compatibility >= 0.6f) return "Good";
            if (compatibility >= 0.5f) return "Fair";
            if (compatibility >= 0.4f) return "Poor";
            return "Incompatible";
        }
        
        private void UpdateBreedingPreview()
        {
            if (selectedParent1 == null || selectedParent2 == null)
            {
                ClearBreedingPreview();
                return;
            }
            
            // Generate breeding prediction
            currentPrediction = geneticPredictor.PredictOffspring(
                selectedParent1.CreatureData?.GeneticProfile,
                selectedParent2.CreatureData?.GeneticProfile
            );
            
            DisplayGeneticPreview();
            DisplayOffspringPreview();
            UpdateBreedingCosts();
        }
        
        private void DisplayGeneticPreview()
        {
            // Clear existing previews
            foreach (var preview in activeTraitPreviews)
            {
                if (preview != null)
                    Destroy(preview.gameObject);
            }
            activeTraitPreviews.Clear();
            
            if (currentPrediction?.PredictedTraits == null) return;
            
            // Create trait preview items
            foreach (var traitPrediction in currentPrediction.PredictedTraits)
            {
                var previewObj = Instantiate(traitPreviewPrefab, geneticPreviewArea);
                var previewItem = previewObj.GetComponent<TraitPreviewItem>();
                
                if (previewItem != null)
                {
                    previewItem.SetupTraitPreview(traitPrediction, isAdvancedMode);
                    activeTraitPreviews.Add(previewItem);
                }
            }
        }
        
        private void DisplayOffspringPreview()
        {
            if (currentPrediction == null) return;
            
            // Generate visual preview (this would use your visual system)
            var previewTexture = GenerateOffspringVisualPreview(currentPrediction);
            if (previewTexture != null)
            {
                offspringPreviewImage.sprite = Sprite.Create(
                    previewTexture,
                    new Rect(0, 0, previewTexture.width, previewTexture.height),
                    Vector2.one * 0.5f
                );
            }
            
            // Display predicted stats
            DisplayPredictedStats();
            
            // Display probability bars
            DisplayProbabilityBars();
        }
        
        private void DisplayPredictedStats()
        {
            if (currentPrediction?.PredictedTraits == null) return;
            
            var statsText = "Predicted Stats:\n";
            var importantTraits = new[] { "Strength", "Agility", "Intelligence", "Constitution" };
            
            foreach (var traitName in importantTraits)
            {
                var trait = currentPrediction.PredictedTraits.FirstOrDefault(t => t.TraitName == traitName);
                if (trait != null)
                {
                    statsText += $"{traitName}: {trait.PredictedValue:F1} ({trait.Confidence:P0})\n";
                }
            }
            
            predictedStatsText.text = statsText;
        }
        
        private void DisplayProbabilityBars()
        {
            // Clear existing probability bars
            foreach (Transform child in probabilityBarsParent)
            {
                Destroy(child.gameObject);
            }
            
            if (currentPrediction?.ProbabilityDistribution == null) return;
            
            // Create probability bars for different outcome categories
            var categories = new[]
            {
                ("Rare Traits", currentPrediction.RareTraitProbability),
                ("Superior Stats", currentPrediction.SuperiorStatsProbability),
                ("Mutations", currentPrediction.MutationProbability),
                ("Hybrid Vigor", currentPrediction.HybridVigorProbability)
            };
            
            foreach (var (category, probability) in categories)
            {
                var barObj = Instantiate(probabilityBarPrefab, probabilityBarsParent);
                var barComponent = barObj.GetComponent<ProbabilityBar>();
                if (barComponent != null)
                {
                    barComponent.Setup(category, probability);
                }
            }
        }
        
        private void ClearBreedingPreview()
        {
            foreach (var preview in activeTraitPreviews)
            {
                if (preview != null)
                    Destroy(preview.gameObject);
            }
            activeTraitPreviews.Clear();
            
            currentPrediction = null;
            predictedStatsText.text = "";
            offspringPreviewImage.sprite = null;
            
            // Clear probability bars
            foreach (Transform child in probabilityBarsParent)
            {
                Destroy(child.gameObject);
            }
        }
        
        #endregion
        
        #region Breeding Controls
        
        private void UpdateBreedingCosts()
        {
            if (selectedParent1 == null || selectedParent2 == null)
            {
                breedingCostText.text = "";
                requirementsText.text = "";
                timeEstimateText.text = "";
                return;
            }
            
            var costs = CalculateBreedingCosts();
            breedingCostText.text = $"Cost: {costs.EnergyCost} Energy, {costs.ResourceCost} Resources";
            
            var requirements = CheckBreedingRequirements();
            requirementsText.text = requirements.CanBreed ? "Ready to breed!" : $"Missing: {requirements.MissingRequirements}";
            requirementsText.color = requirements.CanBreed ? Color.green : Color.red;
            
            timeEstimateText.text = $"Est. Time: {costs.BreedingTime:F0} seconds";
            
            // Update energy bar
            var playerResources = FindFirstObjectByType<PlayerResourceManager>();
            if (playerResources != null)
            {
                energyBar.fillAmount = playerResources.CurrentEnergy / (float)costs.EnergyCost;
            }
        }
        
        private void StartBreedingProcess()
        {
            if (selectedParent1 == null || selectedParent2 == null || isBreedingInProgress)
                return;
                
            var requirements = CheckBreedingRequirements();
            if (!requirements.CanBreed)
            {
                ShowError($"Cannot breed: {requirements.MissingRequirements}");
                return;
            }
            
            isBreedingInProgress = true;
            breedButton.interactable = false;
            breedButton.GetComponentInChildren<TextMeshProUGUI>().text = "Breeding...";
            
            // Start breeding process
            var breedingManager = FindFirstObjectByType<BreedingOperationManager>();
            if (breedingManager != null)
            {
                breedingManager.StartBreeding(selectedParent1, selectedParent2, OnBreedingComplete);
            }
            
            // Record breeding attempt
            var historyManager = FindFirstObjectByType<BreedingHistoryManager>();
            historyManager?.RecordBreedingAttempt(selectedParent1, selectedParent2, 30f, true);
        }
        
        private void OnBreedingComplete(CreatureInstanceComponent offspring)
        {
            isBreedingInProgress = false;
            breedButton.interactable = true;
            breedButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Breeding";
            
            if (offspring != null)
            {
                // Show success notification
                var notificationSystem = FindFirstObjectByType<BreedingNotificationSystem>();
                notificationSystem?.ShowBreedingSuccess(
                    selectedParent1.name, 
                    selectedParent2.name, 
                    offspring.name,
                    offspring.CreatureData?.GeneticProfile
                );
                
                // Add to collection
                var collectionManager = FindFirstObjectByType<CreatureCollectionManager>();
                collectionManager?.AddCreatureToCollection(offspring);
                
                // Clear selection for next breeding
                ClearParentSelection();
            }
            else
            {
                ShowError("Breeding failed. Please try again.");
            }
        }
        
        #endregion
        
        #region Recommendations
        
        private void LoadBreedingRecommendations()
        {
            var collectionManager = FindFirstObjectByType<CreatureCollectionManager>();
            if (collectionManager == null) return;
            
            var recommendations = GenerateBreedingRecommendations(collectionManager.GetCreatures());
            DisplayRecommendations(recommendations);
        }
        
        private List<BreedingRecommendation> GenerateBreedingRecommendations(List<CreatureInstanceComponent> creatures)
        {
            var recommendations = new List<BreedingRecommendation>();
            
            // Find best compatibility pairs
            for (int i = 0; i < creatures.Count; i++)
            {
                for (int j = i + 1; j < creatures.Count; j++)
                {
                    var compatibility = compatibilityAnalyzer.AnalyzeCompatibility(
                        creatures[i].CreatureData?.GeneticProfile,
                        creatures[j].CreatureData?.GeneticProfile
                    );
                    
                    if (compatibility.OverallCompatibility > 0.7f)
                    {
                        recommendations.Add(new BreedingRecommendation
                        {
                            Parent1 = creatures[i],
                            Parent2 = creatures[j],
                            Compatibility = compatibility.OverallCompatibility,
                            Reason = GenerateRecommendationReason(compatibility)
                        });
                    }
                }
            }
            
            return recommendations.OrderByDescending(r => r.Compatibility).Take(5).ToList();
        }
        
        private void DisplayRecommendations(List<BreedingRecommendation> recommendations)
        {
            // Clear existing recommendations
            foreach (Transform child in recommendationsPanel)
            {
                Destroy(child.gameObject);
            }
            
            foreach (var recommendation in recommendations)
            {
                var recObj = Instantiate(recommendationItemPrefab, recommendationsPanel);
                var recComponent = recObj.GetComponent<RecommendationItem>();
                if (recComponent != null)
                {
                    recComponent.Setup(recommendation, OnRecommendationSelected);
                }
            }
        }
        
        private void OnRecommendationSelected(BreedingRecommendation recommendation)
        {
            SetParent1(recommendation.Parent1);
            SetParent2(recommendation.Parent2);
        }
        
        #endregion
        
        #region UI Management
        
        private void UpdateUI()
        {
            bool bothParentsSelected = selectedParent1 != null && selectedParent2 != null;
            bool canBreed = bothParentsSelected && !isBreedingInProgress && CheckBreedingRequirements().CanBreed;
            
            breedButton.interactable = canBreed;
            previewButton.interactable = bothParentsSelected;
            swapParentsButton.interactable = bothParentsSelected;
            clearSelectionButton.interactable = selectedParent1 != null || selectedParent2 != null;
        }
        
        private void ToggleAdvancedMode(bool enabled)
        {
            isAdvancedMode = enabled;
            
            // Show/hide advanced UI elements
            geneticPreviewArea.gameObject.SetActive(enabled);
            probabilityBarsParent.gameObject.SetActive(enabled);
            
            // Update existing previews
            foreach (var preview in activeTraitPreviews)
            {
                if (preview != null)
                    preview.SetAdvancedMode(enabled);
            }
        }
        
        private void HandleCreatureSelected(CreatureInstanceComponent creature)
        {
            // Auto-assign to first available parent slot
            if (selectedParent1 == null)
                SetParent1(creature);
            else if (selectedParent2 == null)
                SetParent2(creature);
        }
        
        private void HandleBreedingComplete(CreatureInstanceComponent offspring)
        {
            // This is called by the global event system
            LoadBreedingRecommendations(); // Update recommendations
        }
        
        private void ShowError(string message)
        {
            var notificationSystem = FindFirstObjectByType<BreedingNotificationSystem>();
            notificationSystem?.ShowError("Breeding Error", message);
        }
        
        public void RefreshUI()
        {
            UpdateBreedingPreview();
            UpdateCompatibilityAnalysis();
            UpdateUI();
            LoadBreedingRecommendations();
        }

        /// <summary>
        /// Quick breeding method for batch operations
        /// </summary>
        public void QuickBreed(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2)
        {
            if (parent1 == null || parent2 == null)
            {
                UnityEngine.Debug.LogWarning("Quick Breed: One or both parents are null");
                return;
            }

            SetParent1(parent1);
            SetParent2(parent2);

            // Trigger immediate breeding if possible
            if (selectedParent1 != null && selectedParent2 != null)
            {
                StartBreedingProcess();
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        private BreedingCosts CalculateBreedingCosts()
        {
            // Calculate costs based on parent genetics and compatibility
            float baseCost = 100f;
            float energyMultiplier = 1f;
            float timeMultiplier = 1f;
            
            if (selectedParent1?.CreatureData?.GeneticProfile != null && selectedParent2?.CreatureData?.GeneticProfile != null)
            {
                var rarity1 = CalculateGeneticRarity(selectedParent1.CreatureData.GeneticProfile);
                var rarity2 = CalculateGeneticRarity(selectedParent2.CreatureData.GeneticProfile);
                
                energyMultiplier = 1f + (rarity1 + rarity2) * 0.5f;
                timeMultiplier = 1f + (rarity1 + rarity2) * 0.3f;
            }
            
            return new BreedingCosts
            {
                EnergyCost = Mathf.RoundToInt(baseCost * energyMultiplier),
                ResourceCost = Mathf.RoundToInt(baseCost * 0.5f * energyMultiplier),
                BreedingTime = 30f * timeMultiplier
            };
        }
        
        private BreedingRequirements CheckBreedingRequirements()
        {
            var requirements = new BreedingRequirements { CanBreed = true };
            var missing = new List<string>();
            
            // Check energy
            var playerResources = FindFirstObjectByType<PlayerResourceManager>();
            if (playerResources != null)
            {
                var costs = CalculateBreedingCosts();
                if (playerResources.CurrentEnergy < costs.EnergyCost)
                    missing.Add($"{costs.EnergyCost} Energy");
                if (playerResources.CurrentResources < costs.ResourceCost)
                    missing.Add($"{costs.ResourceCost} Resources");
            }
            
            // Check parent health
            if (selectedParent1?.Health < 50)
                missing.Add("Parent 1 Health");
            if (selectedParent2?.Health < 50)
                missing.Add("Parent 2 Health");
            
            // Check breeding cooldown
            if (selectedParent1?.IsInBreedingCooldown == true)
                missing.Add("Parent 1 Cooldown");
            if (selectedParent2?.IsInBreedingCooldown == true)
                missing.Add("Parent 2 Cooldown");
            
            if (missing.Count > 0)
            {
                requirements.CanBreed = false;
                requirements.MissingRequirements = string.Join(", ", missing);
            }
            
            return requirements;
        }
        
        private float CalculateGeneticRarity(GeneticProfile genetics)
        {
            if (genetics?.Mutations == null) return 0f;
            
            float rarityScore = 0f;
            foreach (var mutation in genetics.Mutations)
            {
                rarityScore += mutation.severity * 0.25f;
            }
            
            return Mathf.Clamp01(rarityScore);
        }
        
        private Texture2D GenerateOffspringVisualPreview(BreedingPrediction prediction)
        {
            // This would integrate with your visual system to generate a preview
            // For now, return null and handle in the calling code
            return null;
        }
        
        private void DisplayDetailedCompatibility(BreedingCompatibility compatibility)
        {
            // Implementation for detailed compatibility display in advanced mode
        }
        
        private string GenerateRecommendationReason(BreedingCompatibility compatibility)
        {
            var reasons = new List<string>();
            
            if (compatibility.TraitSynergy > 0.8f)
                reasons.Add("Excellent trait synergy");
            if (compatibility.GeneticDiversity > 0.7f)
                reasons.Add("High genetic diversity");
            if (compatibility.MutationPotential > 0.6f)
                reasons.Add("Good mutation potential");
            
            return reasons.Count > 0 ? string.Join(", ", reasons) : "Compatible genetics";
        }
        
        private void GenerateOffspringPreview()
        {
            if (currentPrediction != null)
            {
                // Force refresh the preview
                DisplayOffspringPreview();
            }
        }
        
        private Sprite GetCreaturePortrait(CreatureInstanceComponent creature)
        {
            // This would integrate with your visual system to get creature portraits
            // For now, return null and handle in the calling code
            return null;
        }

        // Missing methods that are referenced by other UI components
        public void SelectCreatureAsParent(CreatureInstanceComponent creature, int parentSlot = 0)
        {
            UnityEngine.Debug.Log($"Selecting creature {creature.name} as parent {parentSlot}");
            if (parentSlot == 1 || selectedParent1 == null)
            {
                selectedParent1 = creature;
                UnityEngine.Debug.Log($"Set as parent 1: {creature.name}");
            }
            else if (parentSlot == 2 || selectedParent2 == null)
            {
                selectedParent2 = creature;
                UnityEngine.Debug.Log($"Set as parent 2: {creature.name}");
                UpdateBreedingPreview();
            }
            else
            {
                UnityEngine.Debug.Log("Both parent slots are full");
            }
        }

        public void ShowBreedingUI()
        {
            UnityEngine.Debug.Log("Showing breeding UI");
            gameObject.SetActive(true);
        }

        #endregion
    }
    
    #region Supporting Data Structures
    
    [System.Serializable]
    public class BreedingCosts
    {
        public int EnergyCost;
        public int ResourceCost;
        public float BreedingTime;
    }
    
    [System.Serializable]
    public class BreedingRequirements
    {
        public bool CanBreed;
        public string MissingRequirements;
    }
    
    [System.Serializable]
    public class BreedingRecommendation
    {
        public CreatureInstanceComponent Parent1;
        public CreatureInstanceComponent Parent2;
        public float Compatibility;
        public string Reason;
    }
    
    // Placeholder classes - these would be implemented in your breeding system
    public class PlayerResourceManager : MonoBehaviour
    {
        public int CurrentEnergy = 100;
        public int CurrentResources = 50;
    }

    public class BreedingOperationManager : MonoBehaviour
    {
        public void StartBreeding(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2, System.Action<CreatureInstanceComponent> onComplete)
        {
            UnityEngine.Debug.Log($"Starting breeding operation between {parent1.name} and {parent2.name}");

            // Simulate breeding delay
            StartCoroutine(SimulateBreeding(parent1, parent2, onComplete));
        }

        private System.Collections.IEnumerator SimulateBreeding(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2, System.Action<CreatureInstanceComponent> onComplete)
        {
            yield return new WaitForSeconds(2f); // Simulate breeding time

            // Create a simple offspring (this would be much more complex in a real system)
            var offspring = new GameObject($"{parent1.name}-{parent2.name} Offspring");
            var offspringComponent = offspring.AddComponent<CreatureInstanceComponent>();

            // Create offspring creature instance with simplified genetics
            if (parent1.CreatureData != null)
            {
                var offspringData = new CreatureInstance(parent1.CreatureData.Definition, parent1.CreatureData.GeneticProfile);
                offspringComponent.Initialize(offspringData);
            }

            onComplete?.Invoke(offspringComponent);
        }
    }

    #endregion
}
