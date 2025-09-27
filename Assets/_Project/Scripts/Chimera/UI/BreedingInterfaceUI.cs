using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Standard .NET async support
using Laboratory.Chimera.Breeding;
using Laboratory.Core.ECS;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.ECS;
using Laboratory.Core.Events;
using Laboratory.Core.DI;

namespace Laboratory.Chimera.UI
{
    /// <summary>
    /// Interactive breeding interface that allows players to select creatures and initiate breeding.
    /// Shows compatibility, predicted offspring, and breeding progress.
    /// </summary>
    public class BreedingInterfaceUI : MonoBehaviour
    {
        [Header("🧬 Creature Selection")]
        [SerializeField] private Button parent1Button;
        [SerializeField] private Button parent2Button;
        [SerializeField] private TextMeshProUGUI parent1NameText;
        [SerializeField] private TextMeshProUGUI parent2NameText;
        [SerializeField] private Image parent1Portrait;
        [SerializeField] private Image parent2Portrait;
        
        [Header("💕 Compatibility Display")]
        [SerializeField] private Slider compatibilitySlider;
        [SerializeField] private TextMeshProUGUI compatibilityText;
        [SerializeField] private TextMeshProUGUI compatibilityReasonText;
        [SerializeField] private Image compatibilityIcon;
        
        [Header("🎯 Breeding Controls")]
        [SerializeField] private Button breedButton;
        [SerializeField] private Button clearSelectionButton;
        [SerializeField] private Slider breedingProgressSlider;
        [SerializeField] private TextMeshProUGUI breedingStatusText;
        [SerializeField] private Transform availableCreaturesList;
        
        [Header("👶 Offspring Prediction")]
        [SerializeField] private GameObject offspringPredictionPanel;
        [SerializeField] private TextMeshProUGUI offspringCountText;
        [SerializeField] private TextMeshProUGUI predictedTraitsText;
        [SerializeField] private Slider strengthPredictionSlider;
        [SerializeField] private Slider vitalityPredictionSlider;
        [SerializeField] private Slider agilityPredictionSlider;
        [SerializeField] private Slider intelligencePredictionSlider;
        
        [Header("🌍 Environmental Settings")]
        [SerializeField] private TMP_Dropdown biomeDropdown;
        [SerializeField] private Slider temperatureSlider;
        [SerializeField] private Slider foodAvailabilitySlider;
        [SerializeField] private TextMeshProUGUI environmentalModifierText;
        
        [Header("📊 Results Display")]
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TextMeshProUGUI resultsText;
        [SerializeField] private Transform offspringContainer;
        [SerializeField] private GameObject offspringCardPrefab;
        
        [Header("🔧 Configuration")]
        [SerializeField] private bool autoSelectNearbyCreatures = true;
        [SerializeField] private float maxSelectionDistance = 15f;
        [SerializeField] private bool showAdvancedPredictions = true;
        [SerializeField] private bool enableEnvironmentalEffects = true;
        
        // Runtime state
        private EnhancedCreatureAuthoring selectedParent1;
        private EnhancedCreatureAuthoring selectedParent2;
        private IBreedingSystem breedingSystem;
        private IEventBus eventBus;
        private List<EnhancedCreatureAuthoring> availableCreatures = new List<EnhancedCreatureAuthoring>();
        private BreedingResult lastBreedingResult;
        private bool isBreedingInProgress = false;
        
        // Colors for UI feedback
        private readonly Color compatibleColor = Color.green;
        private readonly Color incompatibleColor = Color.red;
        private readonly Color neutralColor = Color.yellow;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            SetupUICallbacks();
            InitializeBiomeDropdown();
        }
        
        private void Start()
        {
            InitializeSystems();
            RefreshAvailableCreatures();
            UpdateUI();
        }
        
        private void OnEnable()
        {
            SubscribeToEvents();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion
        
        #region Initialization
        
        private void SetupUICallbacks()
        {
            if (parent1Button != null)
                parent1Button.onClick.AddListener(() => SelectParent(1));
                
            if (parent2Button != null)
                parent2Button.onClick.AddListener(() => SelectParent(2));
                
            if (breedButton != null)
                breedButton.onClick.AddListener(StartBreeding);
                
            if (clearSelectionButton != null)
                clearSelectionButton.onClick.AddListener(ClearSelection);
                
            if (biomeDropdown != null)
                biomeDropdown.onValueChanged.AddListener(OnBiomeChanged);
                
            if (temperatureSlider != null)
                temperatureSlider.onValueChanged.AddListener(OnEnvironmentChanged);
                
            if (foodAvailabilitySlider != null)
                foodAvailabilitySlider.onValueChanged.AddListener(OnEnvironmentChanged);
        }
        
        private void InitializeBiomeDropdown()
        {
            if (biomeDropdown != null)
            {
                biomeDropdown.ClearOptions();
                var biomeNames = System.Enum.GetNames(typeof(Laboratory.Chimera.Core.BiomeType)).ToList();
                biomeDropdown.AddOptions(biomeNames);
                biomeDropdown.value = 0; // Default to first biome
            }
        }
        
        private void InitializeSystems()
        {
            // Initialize event bus
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.TryResolve<IEventBus>(out eventBus);
            }
            
            if (eventBus == null)
            {
                eventBus = new UnifiedEventBus();
            }
            
            // Initialize breeding system
            breedingSystem = new BreedingSystem(eventBus);
        }
        
        #endregion
        
        #region Creature Selection
        
        private void SelectParent(int parentNumber)
        {
            var creatures = FindAvailableCreatures();
            
            if (creatures.Count == 0)
            {
                ShowMessage("No creatures available for breeding");
                return;
            }
            
            // For now, cycle through available creatures
            // In a full implementation, this would open a creature selection UI
            if (parentNumber == 1)
            {
                int currentIndex = creatures.IndexOf(selectedParent1);
                selectedParent1 = creatures[(currentIndex + 1) % creatures.Count];
            }
            else
            {
                int currentIndex = creatures.IndexOf(selectedParent2);
                selectedParent2 = creatures[(currentIndex + 1) % creatures.Count];
            }
            
            UpdateUI();
            UpdateCompatibility();
            UpdateOffspringPrediction();
        }
        
        private void RefreshAvailableCreatures()
        {
            var creatures = FindAvailableCreatures();

            // Update UI elements with available creatures
            if (availableCreaturesList != null)
            {
                // Clear existing creatures
                foreach (Transform child in availableCreaturesList.transform)
                {
                    if (child != availableCreaturesList.transform)
                        Destroy(child.gameObject);
                }

                // Add new creature entries
                foreach (var creature in creatures)
                {
                    // Create creature entry UI elements here
                    // This would typically instantiate prefabs for creature selection
                }
            }
        }

        private List<EnhancedCreatureAuthoring> FindAvailableCreatures()
        {
            var creatures = FindObjectsByType<EnhancedCreatureAuthoring>(FindObjectsSortMode.None).ToList();
            
            if (autoSelectNearbyCreatures)
            {
                var playerPos = Camera.main?.transform.position ?? transform.position;
                creatures = creatures.Where(c => 
                    Vector3.Distance(c.transform.position, playerPos) <= maxSelectionDistance).ToList();
            }
            
            // Filter to breeding-capable creatures
            creatures = creatures.Where(c => IsBreedingCapable(c)).ToList();
            
            return creatures;
        }
        
        private bool IsBreedingCapable(EnhancedCreatureAuthoring creature)
        {
            // Check if creature is adult and not currently breeding
            // This would check the ECS components in a full implementation
            return creature != null; // Simplified for now
        }
        
        public void SetSelectedCreature(EnhancedCreatureAuthoring creature, int parentSlot)
        {
            if (parentSlot == 1)
                selectedParent1 = creature;
            else if (parentSlot == 2)
                selectedParent2 = creature;
                
            UpdateUI();
            UpdateCompatibility();
            UpdateOffspringPrediction();
        }
        
        private void ClearSelection()
        {
            selectedParent1 = null;
            selectedParent2 = null;
            lastBreedingResult = null;
            
            UpdateUI();
            
            if (resultsPanel != null)
                resultsPanel.SetActive(false);
        }
        
        #endregion
        
        #region Breeding Process
        
        private void StartBreeding()
        {
            if (selectedParent1 == null || selectedParent2 == null)
            {
                ShowMessage("Please select both parents");
                return;
            }
            
            if (isBreedingInProgress)
            {
                ShowMessage("Breeding already in progress");
                return;
            }
            
            isBreedingInProgress = true;
            UpdateBreedingUI();
            
            try
            {
                // Create creature instances from authoring components
                var parent1Instance = CreateCreatureInstance(selectedParent1);
                var parent2Instance = CreateCreatureInstance(selectedParent2);
                
                // Create breeding environment
                var environment = CreateBreedingEnvironment();
                
                // Show progress
                var progress = new System.Progress<float>(UpdateBreedingProgress);
                
                // Perform breeding (using synchronous call since UniTask is not available)
                var result = breedingSystem.AttemptBreeding(parent1Instance, parent2Instance);
                
                // Handle results
                lastBreedingResult = result;
                ShowBreedingResults(result);
            }
            catch (System.Exception ex)
            {
                ShowMessage($"Breeding failed: {ex.Message}");
                UnityEngine.Debug.LogError($"Breeding error: {ex}");
            }
            finally
            {
                isBreedingInProgress = false;
                UpdateBreedingUI();
            }
        }
        
        private CreatureInstance CreateCreatureInstance(EnhancedCreatureAuthoring authoring)
        {
            // This would extract data from the authoring component
            // For now, create a basic instance
            return new CreatureInstance
            {
                AgeInDays = 120, // Adult
                CurrentHealth = 100,
                Happiness = 0.8f,
                Level = 1,
                IsWild = false
            };
        }
        
        private BreedingEnvironment CreateBreedingEnvironment()
        {
            var biomeType = (Laboratory.Chimera.Core.BiomeType)biomeDropdown.value;
            
            return new BreedingEnvironment
            {
                BiomeType = biomeType,
                Temperature = temperatureSlider?.value ?? 22f,
                FoodAvailability = foodAvailabilitySlider?.value ?? 0.8f,
                PredatorPressure = 0.3f,
                PopulationDensity = 0.4f
            };
        }
        
        #endregion
        
        #region UI Updates
        
        private void UpdateUI()
        {
            UpdateParentDisplay();
            UpdateBreedingUI();
        }
        
        private void UpdateParentDisplay()
        {
            // Update parent 1
            if (parent1NameText != null)
            {
                parent1NameText.text = selectedParent1?.gameObject.name ?? "Select Creature";
            }
            
            if (parent1Button != null)
            {
                parent1Button.interactable = !isBreedingInProgress;
            }
            
            // Update parent 2
            if (parent2NameText != null)
            {
                parent2NameText.text = selectedParent2?.gameObject.name ?? "Select Creature";
            }
            
            if (parent2Button != null)
            {
                parent2Button.interactable = !isBreedingInProgress;
            }
        }
        
        private void UpdateCompatibility()
        {
            if (selectedParent1 == null || selectedParent2 == null)
            {
                SetCompatibilityDisplay(0.5f, "Select both parents", "Choose two creatures to check compatibility");
                return;
            }
            
            // Calculate compatibility
            var compatibility = CalculateBreedingCompatibility();
            
            string status = compatibility.IsCompatible ? "Compatible" : "Incompatible";
            Color statusColor = compatibility.IsCompatible ? compatibleColor : incompatibleColor;
            
            SetCompatibilityDisplay(compatibility.IsCompatible ? 0.8f : 0.2f, status, compatibility.Reason);
            
            // Update compatibility slider color
            if (compatibilitySlider != null)
            {
                var colors = compatibilitySlider.colors;
                colors.normalColor = statusColor;
                compatibilitySlider.colors = colors;
            }
        }
        
        private void SetCompatibilityDisplay(float value, string status, string reason)
        {
            if (compatibilitySlider != null)
                compatibilitySlider.value = value;
                
            if (compatibilityText != null)
                compatibilityText.text = status;
                
            if (compatibilityReasonText != null)
                compatibilityReasonText.text = reason;
        }
        
        private void UpdateOffspringPrediction()
        {
            if (!showAdvancedPredictions || selectedParent1 == null || selectedParent2 == null)
            {
                if (offspringPredictionPanel != null)
                    offspringPredictionPanel.SetActive(false);
                return;
            }
            
            if (offspringPredictionPanel != null)
                offspringPredictionPanel.SetActive(true);
            
            // Predict offspring characteristics
            var prediction = PredictOffspringTraits();
            
            if (offspringCountText != null)
                offspringCountText.text = $"Expected: {prediction.expectedCount} offspring";
                
            if (predictedTraitsText != null)
                predictedTraitsText.text = prediction.dominantTraits;
                
            // Update trait prediction sliders
            SetSlider(strengthPredictionSlider, prediction.strengthAverage);
            SetSlider(vitalityPredictionSlider, prediction.vitalityAverage);
            SetSlider(agilityPredictionSlider, prediction.agilityAverage);
            SetSlider(intelligencePredictionSlider, prediction.intelligenceAverage);
        }
        
        private void UpdateBreedingUI()
        {
            bool canBreed = selectedParent1 != null && selectedParent2 != null && !isBreedingInProgress;
            
            if (breedButton != null)
            {
                breedButton.interactable = canBreed;
                breedButton.GetComponentInChildren<TextMeshProUGUI>().text = 
                    isBreedingInProgress ? "Breeding..." : "Start Breeding";
            }
            
            if (breedingProgressSlider != null)
            {
                breedingProgressSlider.gameObject.SetActive(isBreedingInProgress);
            }
            
            if (breedingStatusText != null)
            {
                breedingStatusText.text = isBreedingInProgress ? "Breeding in progress..." : "Ready to breed";
            }
        }
        
        private void UpdateBreedingProgress(float progress)
        {
            if (breedingProgressSlider != null)
            {
                breedingProgressSlider.value = progress;
            }
            
            if (breedingStatusText != null)
            {
                breedingStatusText.text = $"Breeding... {progress:P0}";
            }
        }
        
        #endregion
        
        #region Results Display
        
        private void ShowBreedingResults(BreedingResult result)
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(true);
            }
            
            if (resultsText != null)
            {
                if (result.Success)
                {
                    resultsText.text = $"🎉 Breeding successful! {result.Offspring.Length} offspring born.\n" +
                                     $"Breeding chance was {result.BreedingChance:P0}";
                    resultsText.color = compatibleColor;
                }
                else
                {
                    resultsText.text = $"💔 Breeding failed: {result.FailureReason}\n" +
                                     $"Breeding chance was {result.BreedingChance:P0}";
                    resultsText.color = incompatibleColor;
                }
            }
            
            // Create offspring cards
            if (result.Success && offspringContainer != null && offspringCardPrefab != null)
            {
                CreateOffspringCards(new[] { result.Offspring });
            }
        }
        
        private void CreateOffspringCards(CreatureInstance[] offspring)
        {
            // Clear existing cards
            foreach (Transform child in offspringContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create new cards
            foreach (var baby in offspring)
            {
                var card = Instantiate(offspringCardPrefab, offspringContainer);
                var cardText = card.GetComponentInChildren<TextMeshProUGUI>();
                
                if (cardText != null)
                {
                    cardText.text = $"{baby.Definition?.speciesName ?? "Baby"}\n" +
                                   $"Health: {baby.CurrentHealth}\n" +
                                   $"Generation: {baby.GeneticProfile?.Generation ?? 1}";
                }
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private CompatibilityResult CalculateBreedingCompatibility()
        {
            // Simplified compatibility check
            if (selectedParent1 == selectedParent2)
                return new CompatibilityResult(false, "Cannot breed creature with itself");
                
            // In a full implementation, this would check species compatibility,
            // age, health, breeding cooldowns, etc.
            return new CompatibilityResult(true, "Species are genetically compatible");
        }
        
        private OffspringPrediction PredictOffspringTraits()
        {
            // Simplified prediction - in reality this would use the genetics system
            return new OffspringPrediction
            {
                expectedCount = Random.Range(1, 4),
                dominantTraits = "Strong, Agile",
                strengthAverage = Random.Range(0.4f, 0.8f),
                vitalityAverage = Random.Range(0.4f, 0.8f),
                agilityAverage = Random.Range(0.4f, 0.8f),
                intelligenceAverage = Random.Range(0.4f, 0.8f)
            };
        }
        
        private void SetSlider(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.value = Mathf.Clamp01(value);
            }
        }
        
        private void ShowMessage(string message)
        {
            UnityEngine.Debug.Log($"[BreedingUI] {message}");
            // In a full implementation, this would show a toast notification
        }
        
        #endregion
        
        #region Event Handling
        
        private void SubscribeToEvents()
        {
            if (eventBus != null)
            {
                eventBus.Subscribe<BreedingSuccessfulEvent>(OnBreedingSuccess);
                eventBus.Subscribe<BreedingFailedEvent>(OnBreedingFailure);
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            // R3 handles automatic unsubscription
        }
        
        private void OnBreedingSuccess(BreedingSuccessfulEvent evt)
        {
            ShowMessage($"Breeding successful! {evt.Offspring.Length} offspring born!");
        }
        
        private void OnBreedingFailure(BreedingFailedEvent evt)
        {
            ShowMessage($"Breeding failed: {evt.Reason}");
        }
        
        private void OnBiomeChanged(int biomeIndex)
        {
            UpdateEnvironmentalModifiers();
        }
        
        private void OnEnvironmentChanged(float value)
        {
            UpdateEnvironmentalModifiers();
        }
        
        private void UpdateEnvironmentalModifiers()
        {
            if (!enableEnvironmentalEffects) return;
            
            var environment = CreateBreedingEnvironment();
            
            if (environmentalModifierText != null)
            {
                environmentalModifierText.text = $"Biome: {environment.BiomeType}\n" +
                                               $"Temp: {environment.Temperature:F1}°C\n" +
                                               $"Food: {environment.FoodAvailability:P0}";
            }
        }
        
        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            breedingSystem?.Dispose();
        }
        
        #endregion
        
        #region Data Structures
        
        private struct CompatibilityResult
        {
            public bool IsCompatible;
            public string Reason;
            
            public CompatibilityResult(bool compatible, string reason)
            {
                IsCompatible = compatible;
                Reason = reason;
            }
        }
        
        private struct OffspringPrediction
        {
            public int expectedCount;
            public string dominantTraits;
            public float strengthAverage;
            public float vitalityAverage;
            public float agilityAverage;
            public float intelligenceAverage;
        }
        
        #endregion
    }
    
    // Events for breeding system
    public class BreedingSuccessfulEvent
    {
        public CreatureInstance[] Offspring { get; }
        
        public BreedingSuccessfulEvent(BreedingResult result)
        {
            Offspring = result.Offspring != null ? new[] { result.Offspring } : new CreatureInstance[0];
        }
    }
    
    public class BreedingFailedEvent
    {
        public string Reason { get; }
        
        public BreedingFailedEvent(CreatureInstance parent1, CreatureInstance parent2, string reason, float breedingChance = 0f)
        {
            Reason = reason;
        }
    }
}