using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Progression;
using Laboratory.Core.Utilities;
using Laboratory.Core.Configuration;

namespace Laboratory.UI.Progression
{
    /// <summary>
    /// Comprehensive UI system for displaying player progression information including
    /// geneticist levels, experience tracking, biome specializations, research progress,
    /// territory status, and creature slot management with animated updates and notifications.
    /// </summary>
    public class PlayerProgressionUI : MonoBehaviour
    {
        [Header("Core UI References")]
        [SerializeField] private Canvas progressionCanvas;
        [SerializeField] private GameObject progressionPanel;
        [SerializeField] private Button toggleProgressionButton;

        [Header("Level and Experience Display")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private Slider experienceSlider;
        [SerializeField] private TextMeshProUGUI experienceText;
        [SerializeField] private Image experienceFillImage;

        [Header("Creature Slots Display")]
        [SerializeField] private TextMeshProUGUI creatureSlotsText;
        [SerializeField] private Transform creatureSlotsContainer;
        [SerializeField] private GameObject creatureSlotPrefab;
        [SerializeField] private Color availableSlotColor = Color.green;
        [SerializeField] private Color occupiedSlotColor = Color.blue;
        [SerializeField] private Color lockedSlotColor = Color.gray;

        [Header("Biome Specializations")]
        [SerializeField] private Transform biomeSpecializationContainer;
        [SerializeField] private GameObject biomeSpecializationPrefab;
        [SerializeField] private ScrollRect biomeScrollRect;

        [Header("Research Progress")]
        [SerializeField] private Transform researchContainer;
        [SerializeField] private GameObject researchItemPrefab;
        [SerializeField] private ScrollRect researchScrollRect;

        [Header("Territory Status")]
        [SerializeField] private TextMeshProUGUI territoryTierText;
        [SerializeField] private TextMeshProUGUI territoryDescriptionText;
        [SerializeField] private Button expandTerritoryButton;
        [SerializeField] private TextMeshProUGUI expansionCostText;

        [Header("Progress Notifications")]
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private GameObject levelUpNotificationPrefab;
        [SerializeField] private GameObject researchUnlockNotificationPrefab;
        [SerializeField] private GameObject biomeUnlockNotificationPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float experienceAnimationSpeed = 2f;
        [SerializeField] private float levelUpAnimationDuration = 1.5f;
        [SerializeField] private AnimationCurve experienceAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Update Settings")]
        [SerializeField] private float uiUpdateInterval = 0.5f;
        [SerializeField] private bool enableRealTimeUpdates = true;

        // UI state tracking
        private PlayerProgressionManager progressionManager;
        private PlayerProgressionConfig progressionConfig;
        private PlayerProgressionStats lastKnownStats;
        private List<BiomeSpecializationUI> biomeSpecializationUIs = new List<BiomeSpecializationUI>();
        private List<ResearchItemUI> researchItemUIs = new List<ResearchItemUI>();
        private Queue<ProgressionNotification> pendingNotifications = new Queue<ProgressionNotification>();

        // Animation tracking
        private float targetExperience;
        private float currentDisplayedExperience;
        private bool isAnimatingExperience = false;
        private float lastUIUpdate;

        // UI visibility
        private bool isProgressionPanelVisible = false;

        private void Awake()
        {
            InitializeUI();
        }

        private void Start()
        {
            ConnectToProgressionManager();
            SetupEventListeners();
            RefreshUI();
        }

        private void Update()
        {
            // Real-time UI updates
            if (enableRealTimeUpdates && Time.time - lastUIUpdate >= uiUpdateInterval)
            {
                UpdateProgressionDisplay();
                lastUIUpdate = Time.time;
            }

            // Handle experience animation
            if (isAnimatingExperience)
            {
                AnimateExperienceProgress();
            }

            // Process pending notifications
            ProcessNotificationQueue();
        }

        private void InitializeUI()
        {
            if (progressionCanvas == null)
                progressionCanvas = GetComponentInParent<Canvas>();

            // Initialize UI state
            if (progressionPanel != null)
                progressionPanel.SetActive(false);

            // Setup toggle button
            if (toggleProgressionButton != null)
            {
                toggleProgressionButton.onClick.AddListener(ToggleProgressionPanel);
            }

            // Setup territory expansion button
            if (expandTerritoryButton != null)
            {
                expandTerritoryButton.onClick.AddListener(AttemptTerritoryExpansion);
            }

            // Initialize experience slider
            if (experienceSlider != null)
            {
                experienceSlider.minValue = 0f;
                experienceSlider.maxValue = 1f;
                experienceSlider.value = 0f;
            }
        }

        private void ConnectToProgressionManager()
        {
            progressionManager = PlayerProgressionManager.Instance;
            if (progressionManager == null)
            {
                Debug.LogWarning("PlayerProgressionManager not found - UI will not function properly");
                return;
            }

            // Get progression configuration
            // This would typically be loaded from the progression manager or a config provider
        }

        private void SetupEventListeners()
        {
            if (progressionManager == null) return;

            // Listen to progression events
            progressionManager.OnLevelUp += HandleLevelUp;
            progressionManager.OnExperienceGained += HandleExperienceGained;
            progressionManager.OnBiomeSpecializationUp += HandleBiomeSpecializationUp;
            progressionManager.OnResearchUnlocked += HandleResearchUnlocked;
            progressionManager.OnTerritoryExpanded += HandleTerritoryExpanded;
            progressionManager.OnCreatureSlotsIncreased += HandleCreatureSlotsIncreased;
        }

        /// <summary>
        /// Refreshes all UI elements with current progression data
        /// </summary>
        public void RefreshUI()
        {
            if (progressionManager == null) return;

            var stats = progressionManager.GetProgressionStats();
            lastKnownStats = stats;

            UpdateLevelDisplay(stats);
            UpdateExperienceDisplay(stats);
            UpdateCreatureSlotsDisplay(stats);
            UpdateBiomeSpecializationDisplay(stats);
            UpdateResearchDisplay(stats);
            UpdateTerritoryDisplay(stats);
        }

        /// <summary>
        /// Updates progression display with latest data
        /// </summary>
        public void UpdateProgressionDisplay()
        {
            if (progressionManager == null) return;

            var currentStats = progressionManager.GetProgressionStats();

            // Only update if there are changes
            if (HasStatsChanged(currentStats, lastKnownStats))
            {
                UpdateLevelDisplay(currentStats);
                UpdateExperienceDisplay(currentStats);
                UpdateCreatureSlotsDisplay(currentStats);

                lastKnownStats = currentStats;
            }
        }

        /// <summary>
        /// Toggles the visibility of the progression panel
        /// </summary>
        public void ToggleProgressionPanel()
        {
            isProgressionPanelVisible = !isProgressionPanelVisible;

            if (progressionPanel != null)
            {
                progressionPanel.SetActive(isProgressionPanelVisible);

                if (isProgressionPanelVisible)
                {
                    RefreshUI();
                }
            }
        }

        private void UpdateLevelDisplay(PlayerProgressionStats stats)
        {
            if (levelText != null)
                levelText.text = $"Level {stats.geneticistLevel}";

            if (levelTitleText != null)
                levelTitleText.text = ProgressionUtilities.GetLevelTitle(stats.geneticistLevel);
        }

        private void UpdateExperienceDisplay(PlayerProgressionStats stats)
        {
            if (experienceSlider != null)
            {
                float progress = stats.experienceToNextLevel > 0
                    ? 1f - (stats.experienceToNextLevel / ProgressionUtilities.GetExperienceRequiredForLevel(stats.geneticistLevel + 1))
                    : 1f;

                if (!isAnimatingExperience)
                {
                    experienceSlider.value = progress;
                }
                else
                {
                    targetExperience = progress;
                }
            }

            if (experienceText != null)
            {
                experienceText.text = $"{stats.currentExperience:F0} / {(stats.currentExperience + stats.experienceToNextLevel):F0} XP";
            }
        }

        private void UpdateCreatureSlotsDisplay(PlayerProgressionStats stats)
        {
            if (creatureSlotsText != null)
                creatureSlotsText.text = $"Creature Slots: {stats.currentCreatureCount}/{stats.availableCreatureSlots}";

            // Update visual slot indicators
            UpdateCreatureSlotVisuals(stats);
        }

        private void UpdateCreatureSlotVisuals(PlayerProgressionStats stats)
        {
            if (creatureSlotsContainer == null || creatureSlotPrefab == null) return;

            // Clear existing slots
            foreach (Transform child in creatureSlotsContainer)
            {
                if (child != creatureSlotsContainer)
                    Destroy(child.gameObject);
            }

            // Create slot visuals
            for (int i = 0; i < stats.maxCreatureSlots; i++)
            {
                GameObject slotObj = Instantiate(creatureSlotPrefab, creatureSlotsContainer);
                Image slotImage = slotObj.GetComponent<Image>();

                if (slotImage != null)
                {
                    if (i < stats.currentCreatureCount)
                        slotImage.color = occupiedSlotColor;
                    else if (i < stats.availableCreatureSlots)
                        slotImage.color = availableSlotColor;
                    else
                        slotImage.color = lockedSlotColor;
                }
            }
        }

        private void UpdateBiomeSpecializationDisplay(PlayerProgressionStats stats)
        {
            if (biomeSpecializationContainer == null || biomeSpecializationPrefab == null) return;

            // Clear existing items
            biomeSpecializationUIs.Clear();
            foreach (Transform child in biomeSpecializationContainer)
            {
                Destroy(child.gameObject);
            }

            // Create biome specialization items
            foreach (var biome in stats.unlockedBiomes)
            {
                GameObject biomeObj = Instantiate(biomeSpecializationPrefab, biomeSpecializationContainer);
                BiomeSpecializationUI biomeUI = biomeObj.GetComponent<BiomeSpecializationUI>();

                if (biomeUI != null)
                {
                    float specializationLevel = stats.biomeSpecializationLevels.GetValueOrDefault(biome, 0f);
                    biomeUI.Initialize(biome, specializationLevel);
                    biomeSpecializationUIs.Add(biomeUI);
                }
            }
        }

        private void UpdateResearchDisplay(PlayerProgressionStats stats)
        {
            if (researchContainer == null || researchItemPrefab == null) return;

            // This would be implemented to show research progress
            // Similar pattern to biome specializations
        }

        private void UpdateTerritoryDisplay(PlayerProgressionStats stats)
        {
            if (territoryTierText != null)
                territoryTierText.text = stats.currentTerritoryTier.ToString();

            if (territoryDescriptionText != null)
            {
                territoryDescriptionText.text = GetTerritoryDescription(stats.currentTerritoryTier);
            }

            // Update expansion button
            UpdateTerritoryExpansionButton(stats);
        }

        private void UpdateTerritoryExpansionButton(PlayerProgressionStats stats)
        {
            if (expandTerritoryButton == null) return;

            var nextTier = GetNextTerritoryTier(stats.currentTerritoryTier);
            bool canExpand = nextTier != stats.currentTerritoryTier;

            expandTerritoryButton.interactable = canExpand;

            if (expansionCostText != null && canExpand)
            {
                float cost = GetTerritoryExpansionCost(nextTier);
                expansionCostText.text = $"Expand for {cost:F0}";
            }
        }

        private void AnimateExperienceProgress()
        {
            if (experienceSlider == null) return;

            float currentValue = experienceSlider.value;
            float newValue = Mathf.MoveTowards(currentValue, targetExperience,
                experienceAnimationSpeed * Time.deltaTime);

            experienceSlider.value = newValue;

            if (Mathf.Approximately(newValue, targetExperience))
            {
                isAnimatingExperience = false;
            }
        }

        private void ProcessNotificationQueue()
        {
            // Process pending notifications
            while (pendingNotifications.Count > 0)
            {
                var notification = pendingNotifications.Dequeue();
                ShowNotification(notification);
            }
        }

        private void ShowNotification(ProgressionNotification notification)
        {
            if (notificationContainer == null) return;

            GameObject notificationPrefab = notification.type switch
            {
                ProgressionNotificationType.LevelUp => levelUpNotificationPrefab,
                ProgressionNotificationType.ResearchUnlocked => researchUnlockNotificationPrefab,
                ProgressionNotificationType.BiomeUnlocked => biomeUnlockNotificationPrefab,
                _ => null
            };

            if (notificationPrefab != null)
            {
                GameObject notificationObj = Instantiate(notificationPrefab, notificationContainer);
                ProgressionNotificationUI notificationUI = notificationObj.GetComponent<ProgressionNotificationUI>();

                if (notificationUI != null)
                {
                    notificationUI.Initialize(notification);
                }
            }
        }

        // Event handlers
        private void HandleLevelUp(int oldLevel, int newLevel)
        {
            pendingNotifications.Enqueue(new ProgressionNotification
            {
                type = ProgressionNotificationType.LevelUp,
                title = "Level Up!",
                message = $"Reached Level {newLevel} (was {oldLevel})",
                displayDuration = levelUpAnimationDuration
            });

            // Trigger experience animation
            isAnimatingExperience = true;
        }

        private void HandleExperienceGained(float amount)
        {
            // Trigger experience bar animation
            isAnimatingExperience = true;
        }

        private void HandleBiomeSpecializationUp(BiomeType biome, int newLevel)
        {
            pendingNotifications.Enqueue(new ProgressionNotification
            {
                type = ProgressionNotificationType.BiomeSpecialization,
                title = "Specialization Up!",
                message = $"{biome} specialization reached level {newLevel}"
            });
        }

        private void HandleResearchUnlocked(ResearchType researchType)
        {
            pendingNotifications.Enqueue(new ProgressionNotification
            {
                type = ProgressionNotificationType.ResearchUnlocked,
                title = "Research Unlocked!",
                message = $"Unlocked: {researchType}"
            });
        }

        private void HandleTerritoryExpanded(TerritoryTier newTier)
        {
            pendingNotifications.Enqueue(new ProgressionNotification
            {
                type = ProgressionNotificationType.TerritoryExpanded,
                title = "Territory Expanded!",
                message = $"Upgraded to: {newTier}"
            });
        }

        private void HandleCreatureSlotsIncreased(int newSlotCount)
        {
            pendingNotifications.Enqueue(new ProgressionNotification
            {
                type = ProgressionNotificationType.MilestoneReached,
                title = "More Creature Slots!",
                message = $"Now have {newSlotCount} creature slots"
            });
        }

        private void AttemptTerritoryExpansion()
        {
            if (progressionManager == null) return;

            var nextTier = GetNextTerritoryTier(progressionManager.CurrentTerritoryTier);
            if (nextTier != progressionManager.CurrentTerritoryTier)
            {
                bool success = progressionManager.TryExpandTerritory(nextTier);
                if (success)
                {
                    RefreshUI();
                }
            }
        }

        // Helper methods
        private bool HasStatsChanged(PlayerProgressionStats current, PlayerProgressionStats previous)
        {
            if (previous == null) return true;

            return current.geneticistLevel != previous.geneticistLevel ||
                   !Mathf.Approximately(current.currentExperience, previous.currentExperience) ||
                   current.availableCreatureSlots != previous.availableCreatureSlots ||
                   current.currentTerritoryTier != previous.currentTerritoryTier;
        }

        private string GetTerritoryDescription(TerritoryTier tier)
        {
            return tier switch
            {
                TerritoryTier.StartingFacility => "A basic breeding facility with essential amenities",
                TerritoryTier.RanchUpgrade => "Expanded facility with specialized breeding environments",
                TerritoryTier.BiomeOutpost => "Multi-environment facility supporting diverse biomes",
                TerritoryTier.RegionalHub => "Advanced cross-biome breeding center with research facilities",
                TerritoryTier.ContinentalNetwork => "Massive facility network with automated breeding assistance",
                _ => "Unknown territory tier"
            };
        }

        private TerritoryTier GetNextTerritoryTier(TerritoryTier currentTier)
        {
            return currentTier switch
            {
                TerritoryTier.StartingFacility => TerritoryTier.RanchUpgrade,
                TerritoryTier.RanchUpgrade => TerritoryTier.BiomeOutpost,
                TerritoryTier.BiomeOutpost => TerritoryTier.RegionalHub,
                TerritoryTier.RegionalHub => TerritoryTier.ContinentalNetwork,
                _ => currentTier // Already at max tier
            };
        }

        private float GetTerritoryExpansionCost(TerritoryTier tier)
        {
            return tier switch
            {
                TerritoryTier.RanchUpgrade => 1000f,
                TerritoryTier.BiomeOutpost => 5000f,
                TerritoryTier.RegionalHub => 15000f,
                TerritoryTier.ContinentalNetwork => 50000f,
                _ => 0f
            };
        }

        private void OnDestroy()
        {
            // Cleanup event listeners
            if (progressionManager != null)
            {
                progressionManager.OnLevelUp -= HandleLevelUp;
                progressionManager.OnExperienceGained -= HandleExperienceGained;
                progressionManager.OnBiomeSpecializationUp -= HandleBiomeSpecializationUp;
                progressionManager.OnResearchUnlocked -= HandleResearchUnlocked;
                progressionManager.OnTerritoryExpanded -= HandleTerritoryExpanded;
                progressionManager.OnCreatureSlotsIncreased -= HandleCreatureSlotsIncreased;
            }
        }

        // Editor utilities
        [ContextMenu("Refresh UI")]
        private void EditorRefreshUI()
        {
            RefreshUI();
        }

        [ContextMenu("Test Level Up Notification")]
        private void EditorTestLevelUpNotification()
        {
            HandleLevelUp(10, 11);
        }
    }

    // Supporting UI component classes
    public class BiomeSpecializationUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI biomeNameText;
        [SerializeField] private TextMeshProUGUI specializationLevelText;
        [SerializeField] private Slider specializationSlider;
        [SerializeField] private Image biomeIcon;

        public void Initialize(BiomeType biome, float specializationLevel)
        {
            if (biomeNameText != null)
                biomeNameText.text = biome.ToString();

            if (specializationLevelText != null)
                specializationLevelText.text = $"Level {specializationLevel:F1}";

            if (specializationSlider != null)
            {
                specializationSlider.value = (specializationLevel % 1f);
            }

            // Set biome-specific visuals
            SetBiomeVisuals(biome);
        }

        private void SetBiomeVisuals(BiomeType biome)
        {
            if (biomeIcon != null)
            {
                Color biomeColor = biome switch
                {
                    BiomeType.Forest => Color.green,
                    BiomeType.Desert => Color.yellow,
                    BiomeType.Arctic => Color.cyan,
                    BiomeType.Volcanic => Color.red,
                    BiomeType.DeepSea => Color.blue,
                    _ => Color.white
                };

                biomeIcon.color = biomeColor;
            }
        }
    }

    // Notification system
    [System.Serializable]

    public class ProgressionNotificationUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private float displayDuration = 3f;

        public void Initialize(ProgressionNotification notification)
        {
            if (titleText != null)
                titleText.text = notification.title;

            if (messageText != null)
                messageText.text = notification.message;

            // Set type-specific styling
            SetNotificationStyle(notification.type);

            // Auto-destroy after duration
            Destroy(gameObject, displayDuration);
        }

        private void SetNotificationStyle(ProgressionNotificationType type)
        {
            if (backgroundImage == null) return;

            Color backgroundColor = type switch
            {
                ProgressionNotificationType.LevelUp => Color.gold,
                ProgressionNotificationType.ResearchUnlocked => Color.blue,
                ProgressionNotificationType.BiomeUnlocked => Color.green,
                ProgressionNotificationType.TerritoryExpanded => Color.magenta,
                _ => Color.white
            };

            backgroundImage.color = backgroundColor;
        }
    }

    /// <summary>
    /// UI component for displaying research item progress and status
    /// </summary>
    [System.Serializable]
    public class ResearchItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI researchNameText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image researchIcon;
        [SerializeField] private Button unlockButton;
        [SerializeField] private GameObject unlockedIndicator;

        public void Initialize(ResearchType researchType, ResearchProgress progress)
        {
            if (researchNameText != null)
                researchNameText.text = researchType.ToString().Replace("_", " ");

            if (progressText != null)
            {
                if (progress.isUnlocked)
                {
                    progressText.text = "UNLOCKED";
                }
                else
                {
                    progressText.text = $"{progress.progressPoints:F1}/{progress.requiredPoints:F1}";
                }
            }

            if (progressSlider != null)
            {
                progressSlider.value = progress.isUnlocked ? 1f : (progress.progressPoints / progress.requiredPoints);
            }

            if (unlockButton != null)
            {
                unlockButton.interactable = !progress.isUnlocked && progress.progressPoints >= progress.requiredPoints;
            }

            if (unlockedIndicator != null)
            {
                unlockedIndicator.SetActive(progress.isUnlocked);
            }

            // Set research-specific visuals
            SetResearchVisuals(researchType, progress.isUnlocked);
        }

        private void SetResearchVisuals(ResearchType researchType, bool isUnlocked)
        {
            if (researchIcon != null)
            {
                // Set icon color based on unlock status
                researchIcon.color = isUnlocked ? Color.white : Color.gray;
            }

            // Could add research-specific icons/colors here
        }

        public void OnUnlockButtonClicked()
        {
            // This would connect to the progression system to unlock research
            if (unlockButton != null && unlockButton.interactable)
            {
                unlockButton.interactable = false;
                // PlayerProgressionManager.Instance?.UnlockResearch(researchType);
            }
        }
    }
}