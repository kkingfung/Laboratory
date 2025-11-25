using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Progression;
using Laboratory.Core.Utilities;
using Laboratory.Core.Configuration;
using Laboratory.Shared.Types;
using Unity.Entities;
using Laboratory.Models.ECS.Components;

namespace Laboratory.UI.Progression
{
    /// <summary>
    /// Comprehensive UI system for displaying player-chimera partnership progression
    /// including skill mastery in 7 genres, partnership quality metrics (cooperation, trust,
    /// understanding), milestone achievements, and cosmetic unlocks. NO LEVELS - victory
    /// through player skill and chimera cooperation!
    /// </summary>
    public class PlayerProgressionUI : MonoBehaviour
    {
        [Header("Core UI References")]
        [SerializeField] private Canvas progressionCanvas;
        [SerializeField] private GameObject progressionPanel;
        [SerializeField] private Button toggleProgressionButton;

        [Header("Mastery Tier Display (replaces levels)")]
        [SerializeField] private TextMeshProUGUI masteryTierText;
        [SerializeField] private TextMeshProUGUI masteryTitleText;
        [SerializeField] private TextMeshProUGUI activitiesCompletedText;

        [Header("Genre Skill Displays (7 categories)")]
        [SerializeField] private Transform genreSkillsContainer;
        [SerializeField] private GameObject genreSkillPrefab;
        [SerializeField] private Slider actionMasterySlider;
        [SerializeField] private Slider strategyMasterySlider;
        [SerializeField] private Slider puzzleMasterySlider;
        [SerializeField] private Slider racingMasterySlider;
        [SerializeField] private Slider rhythmMasterySlider;
        [SerializeField] private Slider explorationMasterySlider;
        [SerializeField] private Slider economicsMasterySlider;

        [Header("Partnership Quality Indicators")]
        [SerializeField] private Slider cooperationSlider;
        [SerializeField] private TextMeshProUGUI cooperationText;
        [SerializeField] private Slider trustSlider;
        [SerializeField] private TextMeshProUGUI trustText;
        [SerializeField] private Slider understandingSlider;
        [SerializeField] private TextMeshProUGUI understandingText;

        [Header("Skill Improvement Display (replaces XP)")]
        [SerializeField] private TextMeshProUGUI recentSuccessRateText;
        [SerializeField] private TextMeshProUGUI improvementTrendText;
        [SerializeField] private Image improvementTrendIndicator;

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
        [SerializeField] private GameObject skillMilestoneNotificationPrefab;
        [SerializeField] private GameObject researchUnlockNotificationPrefab;
        [SerializeField] private GameObject biomeUnlockNotificationPrefab;

        [Header("Animation Settings")]
        [SerializeField] private float skillAnimationSpeed = 2f;
        [SerializeField] private float milestoneAnimationDuration = 1.5f;
        [SerializeField] private AnimationCurve skillAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

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

        // Partnership skill tracking (ECS data access)
        private Unity.Entities.Entity currentPartnershipEntity;
        private float lastCooperationLevel;
        private float lastTrustLevel;
        private float lastUnderstandingLevel;

        // Animation tracking
        private float targetSkillValue;
        private float currentDisplayedSkillValue;
        private bool isAnimatingSkill = false;
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

            // Handle skill animation
            if (isAnimatingSkill)
            {
                AnimateSkillProgress();
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

            // Initialize genre mastery sliders
            InitializeSlider(actionMasterySlider);
            InitializeSlider(strategyMasterySlider);
            InitializeSlider(puzzleMasterySlider);
            InitializeSlider(racingMasterySlider);
            InitializeSlider(rhythmMasterySlider);
            InitializeSlider(explorationMasterySlider);
            InitializeSlider(economicsMasterySlider);

            // Initialize partnership quality sliders
            InitializeSlider(cooperationSlider);
            InitializeSlider(trustSlider);
            InitializeSlider(understandingSlider);
        }

        private void InitializeSlider(Slider slider)
        {
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = 0f;
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

            // Listen to progression events (updated for skill-based system)
            progressionManager.OnSkillMilestoneReached += HandleSkillMilestone;
            progressionManager.OnSkillImproved += HandleSkillImproved;
            progressionManager.OnBiomeSpecializationUp += HandleBiomeSpecializationUp;
            progressionManager.OnResearchUnlocked += HandleResearchUnlocked;
            progressionManager.OnTerritoryExpanded += HandleTerritoryExpanded;
            progressionManager.OnCreatureSlotsIncreased += HandleCreatureSlotsIncreased;
            progressionManager.OnPartnershipQualityChanged += HandlePartnershipQualityChanged;
        }

        /// <summary>
        /// Refreshes all UI elements with current progression data
        /// </summary>
        public void RefreshUI()
        {
            if (progressionManager == null) return;

            var stats = progressionManager.GetProgressionStats();
            lastKnownStats = stats;

            // Get partnership skill data from ECS
            var partnershipSkillData = GetCurrentPartnershipSkillData();

            UpdateMasteryDisplay(partnershipSkillData);
            UpdateGenreSkillsDisplay(partnershipSkillData);
            UpdatePartnershipQualityDisplay(partnershipSkillData);
            UpdateSkillProgressDisplay(partnershipSkillData);
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
            var partnershipSkillData = GetCurrentPartnershipSkillData();

            // Only update if there are changes
            if (HasStatsChanged(currentStats, lastKnownStats) || HasPartnershipQualityChanged(partnershipSkillData))
            {
                UpdateMasteryDisplay(partnershipSkillData);
                UpdateGenreSkillsDisplay(partnershipSkillData);
                UpdatePartnershipQualityDisplay(partnershipSkillData);
                UpdateSkillProgressDisplay(partnershipSkillData);
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

        private void UpdateMasteryDisplay(Laboratory.Chimera.Progression.PartnershipSkillComponent? skillData)
        {
            if (!skillData.HasValue) return;

            // Calculate highest mastery tier across all genres
            float highestMastery = Mathf.Max(
                skillData.Value.actionMastery,
                skillData.Value.strategyMastery,
                skillData.Value.puzzleMastery,
                skillData.Value.racingMastery,
                skillData.Value.rhythmMastery,
                skillData.Value.explorationMastery,
                skillData.Value.economicsMastery
            );

            string masteryTier = GetMasteryTierName(highestMastery);
            string masteryTitle = GetMasteryTitle(highestMastery);

            if (masteryTierText != null)
                masteryTierText.text = masteryTier;

            if (masteryTitleText != null)
                masteryTitleText.text = masteryTitle;

            if (activitiesCompletedText != null)
                activitiesCompletedText.text = $"Activities: {skillData.Value.totalActivitiesCompleted}";
        }

        private void UpdateGenreSkillsDisplay(Laboratory.Chimera.Progression.PartnershipSkillComponent? skillData)
        {
            if (!skillData.HasValue) return;

            UpdateSkillSlider(actionMasterySlider, skillData.Value.actionMastery, "Action");
            UpdateSkillSlider(strategyMasterySlider, skillData.Value.strategyMastery, "Strategy");
            UpdateSkillSlider(puzzleMasterySlider, skillData.Value.puzzleMastery, "Puzzle");
            UpdateSkillSlider(racingMasterySlider, skillData.Value.racingMastery, "Racing");
            UpdateSkillSlider(rhythmMasterySlider, skillData.Value.rhythmMastery, "Rhythm");
            UpdateSkillSlider(explorationMasterySlider, skillData.Value.explorationMastery, "Exploration");
            UpdateSkillSlider(economicsMasterySlider, skillData.Value.economicsMastery, "Economics");
        }

        private void UpdatePartnershipQualityDisplay(Laboratory.Chimera.Progression.PartnershipSkillComponent? skillData)
        {
            if (!skillData.HasValue) return;

            // Update cooperation
            if (cooperationSlider != null)
                cooperationSlider.value = skillData.Value.cooperationLevel;
            if (cooperationText != null)
                cooperationText.text = $"Cooperation: {skillData.Value.cooperationLevel:P0}";

            // Update trust
            if (trustSlider != null)
                trustSlider.value = skillData.Value.trustLevel;
            if (trustText != null)
                trustText.text = $"Trust: {skillData.Value.trustLevel:P0}";

            // Update understanding
            if (understandingSlider != null)
                understandingSlider.value = skillData.Value.understandingLevel;
            if (understandingText != null)
                understandingText.text = $"Understanding: {skillData.Value.understandingLevel:P0}";

            // Track changes for animations
            lastCooperationLevel = skillData.Value.cooperationLevel;
            lastTrustLevel = skillData.Value.trustLevel;
            lastUnderstandingLevel = skillData.Value.understandingLevel;
        }

        private void UpdateSkillProgressDisplay(Laboratory.Chimera.Progression.PartnershipSkillComponent? skillData)
        {
            if (!skillData.HasValue) return;

            if (recentSuccessRateText != null)
                recentSuccessRateText.text = $"Success Rate: {skillData.Value.recentSuccessRate:P0}";

            if (improvementTrendText != null)
            {
                float trend = skillData.Value.improvementTrend;
                string trendSymbol = trend > 0 ? "↑" : (trend < 0 ? "↓" : "→");
                improvementTrendText.text = $"Trend: {trendSymbol} {Mathf.Abs(trend):P1}";
            }

            if (improvementTrendIndicator != null)
            {
                float trend = skillData.Value.improvementTrend;
                improvementTrendIndicator.color = trend > 0 ? Color.green : (trend < 0 ? Color.red : Color.yellow);
            }
        }

        private void UpdateSkillSlider(Slider slider, float value, string genreName)
        {
            if (slider != null)
            {
                if (!isAnimatingSkill)
                {
                    slider.value = value;
                }
                else
                {
                    targetSkillValue = value;
                }
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

        private void AnimateSkillProgress()
        {
            // Animate all skill sliders smoothly
            AnimateSlider(actionMasterySlider);
            AnimateSlider(strategyMasterySlider);
            AnimateSlider(puzzleMasterySlider);
            AnimateSlider(racingMasterySlider);
            AnimateSlider(rhythmMasterySlider);
            AnimateSlider(explorationMasterySlider);
            AnimateSlider(economicsMasterySlider);

            // Check if animation is complete
            if (Mathf.Approximately(currentDisplayedSkillValue, targetSkillValue))
            {
                isAnimatingSkill = false;
            }
        }

        private void AnimateSlider(Slider slider)
        {
            if (slider == null) return;

            float currentValue = slider.value;
            float newValue = Mathf.MoveTowards(currentValue, targetSkillValue,
                skillAnimationSpeed * Time.deltaTime);

            slider.value = newValue;
            currentDisplayedSkillValue = newValue;
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
                ProgressionNotificationType.SkillMilestone => skillMilestoneNotificationPrefab,
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
        private void HandleSkillMilestone(string genre, string milestoneType, float masteryLevel)
        {
            pendingNotifications.Enqueue(new ProgressionNotification
            {
                type = ProgressionNotificationType.SkillMilestone,
                title = $"{genre} Milestone!",
                message = $"Reached {milestoneType} ({masteryLevel:P0})",
                displayDuration = milestoneAnimationDuration
            });

            // Trigger skill animation
            isAnimatingSkill = true;
        }

        private void HandleSkillImproved(string skillName, float oldValue, float newValue)
        {
            // Trigger skill bar animation
            isAnimatingSkill = true;
        }

        private void HandlePartnershipQualityChanged(float cooperation, float trust, float understanding)
        {
            // Update partnership quality display with animation
            lastCooperationLevel = cooperation;
            lastTrustLevel = trust;
            lastUnderstandingLevel = understanding;
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
                progressionManager.OnSkillMilestoneReached -= HandleSkillMilestone;
                progressionManager.OnSkillImproved -= HandleSkillImproved;
                progressionManager.OnBiomeSpecializationUp -= HandleBiomeSpecializationUp;
                progressionManager.OnResearchUnlocked -= HandleResearchUnlocked;
                progressionManager.OnTerritoryExpanded -= HandleTerritoryExpanded;
                progressionManager.OnCreatureSlotsIncreased -= HandleCreatureSlotsIncreased;
                progressionManager.OnPartnershipQualityChanged -= HandlePartnershipQualityChanged;
            }
        }

        // Helper methods for skill-based progression
        private string GetMasteryTierName(float mastery)
        {
            if (mastery >= 0.90f) return "Master";
            if (mastery >= 0.75f) return "Expert";
            if (mastery >= 0.50f) return "Proficient";
            if (mastery >= 0.25f) return "Competent";
            if (mastery >= 0.10f) return "Beginner";
            return "Novice";
        }

        private string GetMasteryTitle(float mastery)
        {
            if (mastery >= 0.90f) return "Elite Partnership - Master of Multiple Genres";
            if (mastery >= 0.75f) return "Expert Partnership - Highly Skilled Team";
            if (mastery >= 0.50f) return "Proficient Partnership - Growing Together";
            if (mastery >= 0.25f) return "Competent Partnership - Building Skills";
            if (mastery >= 0.10f) return "Beginner Partnership - Starting Journey";
            return "New Partnership - Welcome!";
        }

        private Laboratory.Chimera.Progression.PartnershipSkillComponent? GetCurrentPartnershipSkillData()
        {
            // Access ECS World to query for player's partnership entity
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return null;

            var entityManager = world.EntityManager;

            // Query for entities with LocalPlayerTag and PartnershipSkillComponent
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<LocalPlayerTag>(),
                ComponentType.ReadOnly<Laboratory.Chimera.Progression.PartnershipSkillComponent>()
            );

            if (query.CalculateEntityCount() > 0)
            {
                // Get the first matching entity (should only be one local player)
                var entity = query.GetSingletonEntity();

                if (entityManager.HasComponent<Laboratory.Chimera.Progression.PartnershipSkillComponent>(entity))
                {
                    return entityManager.GetComponentData<Laboratory.Chimera.Progression.PartnershipSkillComponent>(entity);
                }
            }

            // Fallback: Try querying without LocalPlayerTag (for testing scenarios)
            var fallbackQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<Laboratory.Chimera.Progression.PartnershipSkillComponent>()
            );

            if (fallbackQuery.CalculateEntityCount() > 0)
            {
                // Get the first partnership entity found
                var entities = fallbackQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                if (entities.Length > 0)
                {
                    var skillData = entityManager.GetComponentData<Laboratory.Chimera.Progression.PartnershipSkillComponent>(entities[0]);
                    entities.Dispose();
                    return skillData;
                }
                entities.Dispose();
            }

            return null;
        }

        private bool HasPartnershipQualityChanged(Laboratory.Chimera.Progression.PartnershipSkillComponent? skillData)
        {
            if (!skillData.HasValue) return false;

            return !Mathf.Approximately(skillData.Value.cooperationLevel, lastCooperationLevel) ||
                   !Mathf.Approximately(skillData.Value.trustLevel, lastTrustLevel) ||
                   !Mathf.Approximately(skillData.Value.understandingLevel, lastUnderstandingLevel);
        }

        // Editor utilities
        [ContextMenu("Refresh UI")]
        private void EditorRefreshUI()
        {
            RefreshUI();
        }

        [ContextMenu("Test Skill Milestone Notification")]
        private void EditorTestSkillMilestoneNotification()
        {
            HandleSkillMilestone("Action", "Expert", 0.75f);
        }
    }

    // Supporting UI component classes
    public class BiomeSpecializationUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI biomeNameText;
        [SerializeField] private TextMeshProUGUI specializationLevelText;
        [SerializeField] private Slider specializationSlider;
        [SerializeField] private Image biomeIcon;

        public void Initialize(Laboratory.Shared.Types.BiomeType biome, float specializationLevel)
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

        private void SetBiomeVisuals(Laboratory.Shared.Types.BiomeType biome)
        {
            if (biomeIcon != null)
            {
                Color biomeColor = biome switch
                {
                    Laboratory.Shared.Types.BiomeType.Forest => Color.green,
                    Laboratory.Shared.Types.BiomeType.Desert => Color.yellow,
                    Laboratory.Shared.Types.BiomeType.Arctic => Color.cyan,
                    Laboratory.Shared.Types.BiomeType.Volcanic => Color.red,
                    Laboratory.Shared.Types.BiomeType.DeepSea => Color.blue,
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
                ProgressionNotificationType.SkillMilestone => new Color(1f, 0.84f, 0f), // Gold
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