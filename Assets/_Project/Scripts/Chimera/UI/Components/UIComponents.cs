using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using Laboratory.Chimera;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.UI;
using System.Linq;

namespace Laboratory.Chimera.UI.Components
{
    #region TraitPreviewItem
    
    /// <summary>
    /// Component for displaying genetic trait predictions in breeding UI.
    /// </summary>
    public class TraitPreviewItem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI traitNameText;
        [SerializeField] private Image traitValueBar;
        [SerializeField] private TextMeshProUGUI traitValueText;
        [SerializeField] private TextMeshProUGUI confidenceText;
        [SerializeField] private Image inheritanceIcon;
        
        [Header("Advanced Mode")]
        [SerializeField] private GameObject advancedInfoPanel;
        [SerializeField] private TextMeshProUGUI parent1ContributionText;
        [SerializeField] private TextMeshProUGUI parent2ContributionText;
        [SerializeField] private TextMeshProUGUI mutationChanceText;
        
        private TraitPrediction traitData;
        private bool isAdvancedMode;
        
        public void SetupTraitPreview(TraitPrediction prediction, bool advancedMode)
        {
            traitData = prediction;
            isAdvancedMode = advancedMode;
            
            UpdateDisplay();
        }
        
        public void SetAdvancedMode(bool enabled)
        {
            isAdvancedMode = enabled;
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            if (traitData == null) return;
            
            // Basic display
            traitNameText.text = traitData.TraitName;
            traitValueText.text = traitData.PredictedValue.ToString("F2");
            confidenceText.text = $"{traitData.Confidence:P0}";
            
            // Value bar
            traitValueBar.fillAmount = traitData.PredictedValue;
            traitValueBar.color = GetTraitColor(traitData.PredictedValue);
            
            // Inheritance icon (dominant/recessive indicator)
            if (inheritanceIcon != null)
            {
                inheritanceIcon.color = traitData.IsDominant ? Color.yellow : Color.gray;
            }
            
            // Advanced mode information
            if (advancedInfoPanel != null)
            {
                advancedInfoPanel.SetActive(isAdvancedMode);
                
                if (isAdvancedMode)
                {
                    parent1ContributionText.text = $"P1: {traitData.Parent1Contribution:F2}";
                    parent2ContributionText.text = $"P2: {traitData.Parent2Contribution:F2}";
                    mutationChanceText.text = $"Mutation: {traitData.MutationChance:P1}";
                }
            }
        }
        
        private Color GetTraitColor(float value)
        {
            if (value > 0.8f) return Color.Lerp(Color.yellow, Color.red, (value - 0.8f) * 5f);
            if (value > 0.6f) return Color.Lerp(Color.green, Color.yellow, (value - 0.6f) * 5f);
            if (value > 0.4f) return Color.green;
            if (value > 0.2f) return Color.Lerp(Color.gray, Color.green, (value - 0.2f) * 5f);
            return Color.gray;
        }
    }
    
    #endregion
    
    #region ProbabilityBar
    
    /// <summary>
    /// Visual component for displaying probability percentages with color-coded bars.
    /// Used in breeding prediction UI to show mutation chances, trait probabilities, etc.
    /// </summary>
    public class ProbabilityBar : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image fillBar;
        [SerializeField] private TextMeshProUGUI percentageText;
        [SerializeField] private Image backgroundBar;
        
        [Header("Color Coding")]
        [SerializeField] private Gradient probabilityGradient;
        [SerializeField] private bool useGradient = true;
        [SerializeField] private Color lowProbabilityColor = Color.red;
        [SerializeField] private Color mediumProbabilityColor = Color.yellow;
        [SerializeField] private Color highProbabilityColor = Color.green;
        
        [Header("Animation")]
        [SerializeField] private bool animateOnSetup = true;
        [SerializeField] private float animationDuration = 1f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        public void Setup(string label, float probability)
        {
            labelText.text = label;
            
            if (animateOnSetup)
            {
                StartCoroutine(AnimateProbability(probability));
            }
            else
            {
                SetProbability(probability);
            }
        }
        
        private void SetProbability(float probability)
        {
            // Clamp probability between 0 and 1
            probability = Mathf.Clamp01(probability);
            
            // Set fill amount
            fillBar.fillAmount = probability;
            
            // Set percentage text
            percentageText.text = $"{probability:P0}";
            
            // Set color
            if (useGradient && probabilityGradient != null)
            {
                fillBar.color = probabilityGradient.Evaluate(probability);
            }
            else
            {
                fillBar.color = GetProbabilityColor(probability);
            }
        }
        
        private Color GetProbabilityColor(float probability)
        {
            if (probability < 0.33f)
                return Color.Lerp(lowProbabilityColor, mediumProbabilityColor, probability * 3f);
            else if (probability < 0.66f)
                return Color.Lerp(mediumProbabilityColor, highProbabilityColor, (probability - 0.33f) * 3f);
            else
                return highProbabilityColor;
        }
        
        private System.Collections.IEnumerator AnimateProbability(float targetProbability)
        {
            float startProbability = 0f;
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                float animatedValue = animationCurve.Evaluate(t);
                float currentProbability = Mathf.Lerp(startProbability, targetProbability, animatedValue);
                
                SetProbability(currentProbability);
                yield return null;
            }
            
            SetProbability(targetProbability);
        }
        
        public void UpdateProbability(float newProbability)
        {
            if (animateOnSetup)
            {
                StartCoroutine(AnimateProbability(newProbability));
            }
            else
            {
                SetProbability(newProbability);
            }
        }
    }
    
    #endregion
    
    #region RecommendationItem
    
    /// <summary>
    /// UI component for displaying breeding recommendations in the breeding UI.
    /// Shows compatibility, expected outcomes, and allows quick selection.
    /// </summary>
    public class RecommendationItem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image parent1Portrait;
        [SerializeField] private Image parent2Portrait;
        [SerializeField] private TextMeshProUGUI parent1NameText;
        [SerializeField] private TextMeshProUGUI parent2NameText;
        [SerializeField] private TextMeshProUGUI compatibilityText;
        [SerializeField] private Image compatibilityBar;
        [SerializeField] private TextMeshProUGUI reasonText;
        [SerializeField] private Button selectButton;
        
        [Header("Visual Effects")]
        [SerializeField] private Image highlightBorder;
        [SerializeField] private ParticleSystem selectionEffect;
        [SerializeField] private Animator itemAnimator;
        
        private Laboratory.Chimera.UI.BreedingRecommendation recommendationData;
        private System.Action<Laboratory.Chimera.UI.BreedingRecommendation> onSelected;

        public void Setup(Laboratory.Chimera.UI.BreedingRecommendation recommendation, System.Action<Laboratory.Chimera.UI.BreedingRecommendation> selectionCallback)
        {
            recommendationData = recommendation;
            onSelected = selectionCallback;
            
            UpdateDisplay();
            SetupInteractions();
        }
        
        private void UpdateDisplay()
        {
            if (recommendationData == null) return;
            
            // Parent information
            parent1NameText.text = recommendationData.Parent1.name;
            parent2NameText.text = recommendationData.Parent2.name;
            
            // Portraits (placeholder - would use actual portrait system)
            // if (parent1Portrait != null)
            //     parent1Portrait.sprite = recommendationData.Parent1.GetPortraitSprite();
            // if (parent2Portrait != null)
            //     parent2Portrait.sprite = recommendationData.Parent2.GetPortraitSprite();
            
            // Compatibility
            compatibilityText.text = $"{recommendationData.Compatibility:P0}";
            compatibilityBar.fillAmount = recommendationData.Compatibility;
            compatibilityBar.color = GetCompatibilityColor(recommendationData.Compatibility);
            
            // Reason
            reasonText.text = recommendationData.Reason;
            
            // Visual quality indicator
            UpdateQualityIndicator();
        }
        
        private void UpdateQualityIndicator()
        {
            if (highlightBorder == null) return;
            
            if (recommendationData.Compatibility >= 0.9f)
            {
                highlightBorder.gameObject.SetActive(true);
                highlightBorder.color = Color.Lerp(Color.yellow, Color.white, 0.5f); // Gold color
            }
            else if (recommendationData.Compatibility >= 0.8f)
            {
                highlightBorder.gameObject.SetActive(true);
                highlightBorder.color = Color.gray; // Silver color
            }
            else
            {
                highlightBorder.gameObject.SetActive(false);
            }
        }
        
        private Color GetCompatibilityColor(float compatibility)
        {
            if (compatibility >= 0.8f) return Color.green;
            if (compatibility >= 0.6f) return Color.yellow;
            if (compatibility >= 0.4f) return Color.Lerp(Color.yellow, Color.red, 0.5f);
            return Color.red;
        }
        
        private void SetupInteractions()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnItemSelected);
            }
            
            // Make the entire item clickable
            var button = GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();
            
            button.onClick.AddListener(OnItemSelected);
        }
        
        private void OnItemSelected()
        {
            // Play selection effect
            if (selectionEffect != null)
                selectionEffect.Play();
            
            // Animate selection
            if (itemAnimator != null)
                itemAnimator.SetTrigger("Selected");
            
            // Invoke callback
            onSelected?.Invoke(recommendationData);
        }
        
        public void HighlightAsRecommended()
        {
            if (itemAnimator != null)
                itemAnimator.SetBool("IsRecommended", true);
        }
    }
    
    #endregion
    
    #region NotificationComponent
    
    /// <summary>
    /// Component for individual notification items that appear in the notification system.
    /// Handles different notification types with appropriate styling and interactions.
    /// </summary>
    public class NotificationComponent : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI actionButtonText;
        
        [Header("Visual Styling")]
        [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.3f, 0.9f);
        [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        [SerializeField] private Color errorColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color infoColor = new Color(0.3f, 0.6f, 1f, 0.9f);
        [SerializeField] private Color achievementColor = new Color(1f, 0.6f, 0.2f, 0.9f);
        
        [Header("Icon Sprites")]
        [SerializeField] private Sprite successIcon;
        [SerializeField] private Sprite warningIcon;
        [SerializeField] private Sprite errorIcon;
        [SerializeField] private Sprite infoIcon;
        [SerializeField] private Sprite achievementIcon;
        [SerializeField] private Sprite breedingIcon;
        [SerializeField] private Sprite creatureIcon;
        
        private Laboratory.Chimera.UI.NotificationData notificationData;
        private System.Action onActionClicked;

        public void Setup(Laboratory.Chimera.UI.NotificationData data)
        {
            notificationData = data;
            
            // Set text content
            titleText.text = data.Title;
            messageText.text = data.Message;
            
            // Set visual styling based on type
            ApplyNotificationStyling(data.Type);
            
            // Setup action button if needed
            SetupActionButton(data);
            
            // Setup close button
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => {
                    StartCoroutine(FadeOutAndDestroy());
                });
            }
        }
        
        private void ApplyNotificationStyling(Laboratory.Chimera.UI.NotificationType type)
        {
            Color bgColor;
            Sprite icon;
            
            switch (type)
            {
                case Laboratory.Chimera.UI.NotificationType.BreedingSuccess:
                case Laboratory.Chimera.UI.NotificationType.Success:
                    bgColor = successColor;
                    icon = successIcon ?? breedingIcon;
                    break;

                case Laboratory.Chimera.UI.NotificationType.BreedingFailure:
                case Laboratory.Chimera.UI.NotificationType.Error:
                    bgColor = errorColor;
                    icon = errorIcon;
                    break;

                case Laboratory.Chimera.UI.NotificationType.Warning:
                case Laboratory.Chimera.UI.NotificationType.HealthWarning:
                    bgColor = warningColor;
                    icon = warningIcon;
                    break;

                case Laboratory.Chimera.UI.NotificationType.Achievement:
                case Laboratory.Chimera.UI.NotificationType.Milestone:
                    bgColor = achievementColor;
                    icon = achievementIcon;
                    break;

                case Laboratory.Chimera.UI.NotificationType.CreatureSpawned:
                    bgColor = infoColor;
                    icon = creatureIcon;
                    break;

                default:
                    bgColor = infoColor;
                    icon = infoIcon;
                    break;
            }
            
            if (backgroundImage != null)
                backgroundImage.color = bgColor;
                
            if (iconImage != null && icon != null)
                iconImage.sprite = icon;
        }
        
        private void SetupActionButton(Laboratory.Chimera.UI.NotificationData data)
        {
            bool hasAction = false;
            
            switch (data.Type)
            {
                case Laboratory.Chimera.UI.NotificationType.BreedingSuccess:
                    if (data.AdditionalData is Laboratory.Chimera.UI.BreedingResultData breedingData)
                    {
                        actionButtonText.text = "View Creature";
                        onActionClicked = () => ViewCreature(breedingData.OffspringName);
                        hasAction = true;
                    }
                    break;

                case Laboratory.Chimera.UI.NotificationType.Achievement:
                    actionButtonText.text = "View Achievements";
                    onActionClicked = () => OpenAchievementPanel();
                    hasAction = true;
                    break;

                case Laboratory.Chimera.UI.NotificationType.HealthWarning:
                    actionButtonText.text = "View Creature";
                    onActionClicked = () => OpenCreatureDetails();
                    hasAction = true;
                    break;
            }
            
            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(hasAction);
                if (hasAction)
                {
                    actionButton.onClick.AddListener(() => {
                        onActionClicked?.Invoke();
                        StartCoroutine(FadeOutAndDestroy());
                    });
                }
            }
        }
        
        private void ViewCreature(string creatureName)
        {
            // Find creature and show details
            var collectionManager = FindFirstObjectByType<CreatureCollectionManager>();
            var creatures = collectionManager?.GetCreatures();
            var creature = creatures?.FirstOrDefault(c => c.name == creatureName);
            
            if (creature != null)
            {
                var uiManager = FindFirstObjectByType<ChimeraUIManager>();
                if (uiManager != null)
                {
                    // uiManager.ShowCreatureDetails(creature);
                }
            }
        }
        
        private void OpenAchievementPanel()
        {
            var achievementUI = FindFirstObjectByType<AchievementUI>();
            if (achievementUI != null)
            {
                // achievementUI.ShowAchievements();
            }
        }
        
        private void OpenCreatureDetails()
        {
            // Implementation for opening creature details
        }
        
        private System.Collections.IEnumerator FadeOutAndDestroy()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / duration);
                yield return null;
            }
            
            Destroy(gameObject);
        }
    }
    
    #endregion
    
    #region CreatureCard
    
    /// <summary>
    /// Reusable creature card component for displaying creatures in collections,
    /// selection screens, and breeding interfaces.
    /// </summary>
    public class CreatureCard : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private Image creaturePortrait;
        [SerializeField] private TextMeshProUGUI creatureNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI statsText;
        
        [Header("Interactive Elements")]
        [SerializeField] private Button cardButton;
        [SerializeField] private Button favoriteButton;
        [SerializeField] private Toggle selectionToggle;
        [SerializeField] private Image favoriteIcon;
        [SerializeField] private Image selectionCheckmark;
        
        [Header("Status Indicators")]
        [SerializeField] private Image healthBar;
        [SerializeField] private Image energyBar;
        [SerializeField] private GameObject breedingCooldownIndicator;
        [SerializeField] private GameObject rarityIndicator;
        [SerializeField] private Image rarityGlow;
        
        [Header("Visual Effects")]
        [SerializeField] private Animator cardAnimator;
        [SerializeField] private ParticleSystem selectionEffect;
        [SerializeField] private Image highlightBorder;
        
        // Component state
        public CreatureInstanceComponent AssignedCreature { get; private set; }
        private bool isFavorite;
        private bool isSelected;
        private bool isSelectionMode;
        
        // Callbacks
        private System.Action<CreatureInstanceComponent> onCardClicked;
        private System.Action<CreatureInstanceComponent> onFavoriteToggled;
        private System.Action<CreatureInstanceComponent> onSelectionChanged;
        
        public void Setup(CreatureInstanceComponent creature, bool favorite, bool selectionMode, bool selected,
            System.Action<CreatureInstanceComponent> cardCallback,
            System.Action<CreatureInstanceComponent> favoriteCallback,
            System.Action<CreatureInstanceComponent> selectionCallback)
        {
            AssignedCreature = creature;
            isFavorite = favorite;
            isSelectionMode = selectionMode;
            isSelected = selected;
            
            onCardClicked = cardCallback;
            onFavoriteToggled = favoriteCallback;
            onSelectionChanged = selectionCallback;
            
            SetupEventListeners();
            RefreshDisplay(favorite, selectionMode, selected);
        }
        
        private void SetupEventListeners()
        {
            cardButton?.onClick.AddListener(() => onCardClicked?.Invoke(AssignedCreature));
            favoriteButton?.onClick.AddListener(() => onFavoriteToggled?.Invoke(AssignedCreature));
            selectionToggle?.onValueChanged.AddListener((value) => {
                isSelected = value;
                onSelectionChanged?.Invoke(AssignedCreature);
                UpdateSelectionVisuals();
            });
        }
        
        public void RefreshDisplay(bool favorite, bool selectionMode, bool selected)
        {
            isFavorite = favorite;
            isSelectionMode = selectionMode;
            isSelected = selected;
            
            UpdateCreatureInfo();
            UpdateStatusIndicators();
            UpdateInteractiveElements();
            UpdateVisualEffects();
        }
        
        private void UpdateCreatureInfo()
        {
            if (AssignedCreature == null) return;
            
            // Basic info
            creatureNameText.text = AssignedCreature.name;
            levelText.text = $"Lv.{AssignedCreature.CreatureData?.Level ?? 1}";
            
            // Portrait (placeholder - would use actual portrait system)
            // if (creaturePortrait != null)
            // {
            //     creaturePortrait.sprite = AssignedCreature.GetPortraitSprite();
            // }
            
            // Stats summary
            if (statsText != null && AssignedCreature.CreatureData?.GeneticProfile != null)
            {
                var genetics = AssignedCreature.CreatureData?.GeneticProfile;
                var strength = GetGeneticValue(genetics, "Strength");
                var agility = GetGeneticValue(genetics, "Agility");
                var intelligence = GetGeneticValue(genetics, "Intelligence");
                
                statsText.text = $"STR:{strength:F1} AGI:{agility:F1} INT:{intelligence:F1}";
            }
        }
        
        private void UpdateStatusIndicators()
        {
            if (AssignedCreature == null) return;
            
            // Health bar
            if (healthBar != null)
            {
                healthBar.fillAmount = AssignedCreature.CreatureData?.CurrentHealth ?? 0 / 100f;
                healthBar.color = GetHealthColor(AssignedCreature.CreatureData?.CurrentHealth ?? 0);
            }
            
            // Energy bar (if applicable)
            if (energyBar != null)
            {
                energyBar.fillAmount = AssignedCreature.CreatureData?.Happiness ?? 0f / 100f;
            }
            
            // Breeding cooldown
            if (breedingCooldownIndicator != null)
            {
                breedingCooldownIndicator.SetActive(!AssignedCreature.CanBreed);
            }
            
            // Rarity indicator
            UpdateRarityIndicator();
        }
        
        private void UpdateRarityIndicator()
        {
            if (rarityIndicator == null || AssignedCreature?.CreatureData?.GeneticProfile == null) return;
            
            var rarity = CalculateGeneticRarity(AssignedCreature.CreatureData?.GeneticProfile);
            
            if (rarity > 0.8f)
            {
                rarityIndicator.SetActive(true);
                if (rarityGlow != null)
                {
                    rarityGlow.color = Color.Lerp(Color.yellow, Color.magenta, (rarity - 0.8f) * 5f);
                }
            }
            else if (rarity > 0.6f)
            {
                rarityIndicator.SetActive(true);
                if (rarityGlow != null)
                {
                    rarityGlow.color = Color.Lerp(Color.white, Color.yellow, (rarity - 0.6f) * 5f);
                }
            }
            else
            {
                rarityIndicator.SetActive(false);
            }
        }
        
        private void UpdateInteractiveElements()
        {
            // Favorite button
            if (favoriteButton != null)
            {
                favoriteButton.gameObject.SetActive(!isSelectionMode);
            }
            
            if (favoriteIcon != null)
            {
                favoriteIcon.color = isFavorite ? Color.yellow : Color.gray;
            }
            
            // Selection toggle
            if (selectionToggle != null)
            {
                selectionToggle.gameObject.SetActive(isSelectionMode);
                selectionToggle.isOn = isSelected;
            }
            
            UpdateSelectionVisuals();
        }
        
        private void UpdateSelectionVisuals()
        {
            if (selectionCheckmark != null)
            {
                selectionCheckmark.gameObject.SetActive(isSelectionMode && isSelected);
            }
            
            if (highlightBorder != null)
            {
                highlightBorder.gameObject.SetActive(isSelected);
            }
        }
        
        private void UpdateVisualEffects()
        {
            // Play selection effect
            if (isSelected && selectionEffect != null)
            {
                selectionEffect.Play();
            }
            
            // Animate card state changes
            if (cardAnimator != null)
            {
                cardAnimator.SetBool("IsSelected", isSelected);
                cardAnimator.SetBool("IsFavorite", isFavorite);
                cardAnimator.SetBool("IsRare", CalculateGeneticRarity(AssignedCreature?.CreatureData?.GeneticProfile) > 0.7f);
            }
        }
        
        public void SetAdvancedMode(bool enabled)
        {
            // Show/hide advanced information
            if (enabled && AssignedCreature?.CreatureData?.GeneticProfile != null)
            {
                var genetics = AssignedCreature.CreatureData?.GeneticProfile;
                statsText.text += $"\nGen:{genetics.GenerationNumber} Mut:{genetics.Mutations?.Count ?? 0}";
            }
            else
            {
                UpdateCreatureInfo(); // Reset to basic display
            }
        }
        
        #region Utility Methods
        
        private float GetGeneticValue(GeneticProfile genetics, string traitName)
        {
            if (genetics?.TraitExpressions == null) return 0f;
            
            var trait = genetics.TraitExpressions.FirstOrDefault(t =>
                t.Key.Equals(traitName, StringComparison.OrdinalIgnoreCase));
            
            return trait.Key != null ? trait.Value.Value : 0f;
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
        
        private Color GetHealthColor(float health)
        {
            if (health > 70f) return Color.green;
            if (health > 30f) return Color.yellow;
            return Color.red;
        }
        
        #endregion
    }
    
    #endregion

    #region BreedingHistoryEntryDisplay

    /// <summary>
    /// Component for displaying individual breeding history entries in the breeding history UI.
    /// Shows parent information, offspring details, and breeding outcome.
    /// </summary>
    public class BreedingHistoryEntryDisplay : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private Image parent1Portrait;
        [SerializeField] private Image parent2Portrait;
        [SerializeField] private TextMeshProUGUI parent1NameText;
        [SerializeField] private TextMeshProUGUI parent2NameText;
        [SerializeField] private TextMeshProUGUI outcomeText;
        [SerializeField] private Image outcomeIcon;
        [SerializeField] private Button viewDetailsButton;
        [SerializeField] private GameObject successIndicator;
        [SerializeField] private GameObject failureIndicator;

        [Header("Colors")]
        [SerializeField] private Color successColor = Color.green;
        [SerializeField] private Color failureColor = Color.red;

        private BreedingHistoryEntry entryData;
        private System.Action<BreedingHistoryEntry> onViewDetails;

        public void Setup(BreedingHistoryEntry entry, System.Action<BreedingHistoryEntry> detailsCallback)
        {
            entryData = entry;
            onViewDetails = detailsCallback;

            UpdateDisplay();
            SetupInteractions();
        }

        public void SetupEntry(BreedingHistoryEntry entry, object manager)
        {
            entryData = entry;
            onViewDetails = null; // No callback when called from manager

            UpdateDisplay();
            SetupInteractions();
        }

        private void UpdateDisplay()
        {
            if (entryData == null) return;

            // Timestamp
            if (timestampText != null)
                timestampText.text = entryData.Timestamp.ToString("yyyy/MM/dd HH:mm");

            // Parent names
            if (parent1NameText != null)
                parent1NameText.text = entryData.Parent1Name;
            if (parent2NameText != null)
                parent2NameText.text = entryData.Parent2Name;

            // Outcome
            if (entryData.WasSuccessful)
            {
                if (outcomeText != null)
                    outcomeText.text = $"Success: {entryData.OffspringName}";
                if (outcomeText != null)
                    outcomeText.color = successColor;
                if (successIndicator != null)
                    successIndicator.SetActive(true);
                if (failureIndicator != null)
                    failureIndicator.SetActive(false);
            }
            else
            {
                if (outcomeText != null)
                    outcomeText.text = "Failed";
                if (outcomeText != null)
                    outcomeText.color = failureColor;
                if (successIndicator != null)
                    successIndicator.SetActive(false);
                if (failureIndicator != null)
                    failureIndicator.SetActive(true);
            }
        }

        private void SetupInteractions()
        {
            if (viewDetailsButton != null)
            {
                viewDetailsButton.onClick.AddListener(() => onViewDetails?.Invoke(entryData));
            }
        }
    }

    #endregion

    #region UI Manager Classes

    /// <summary>
    /// Main UI manager for the Chimera system - placeholder implementation
    /// </summary>
    public class ChimeraUIManager : MonoBehaviour
    {
        public void ShowCreatureDetails(CreatureInstanceComponent creature)
        {
            Debug.Log($"Showing details for creature: {creature.name}");
            // Placeholder implementation
        }
    }

    /// <summary>
    /// Achievement UI manager - placeholder implementation
    /// </summary>
    public class AchievementUI : MonoBehaviour
    {
        public void ShowAchievements()
        {
            Debug.Log("Showing achievements");
            // Placeholder implementation
        }
    }

    #endregion

    #region Supporting Data Structures
    
    [System.Serializable]
    public class TraitPrediction
    {
        public string TraitName;
        public float PredictedValue;
        public float Confidence;
        public bool IsDominant;
        public float Parent1Contribution;
        public float Parent2Contribution;
        public float MutationChance;
    }
    

    [System.Serializable]
    public class BreedingHistoryEntry
    {
        public System.Guid Id;
        public System.DateTime Date;
        public System.DateTime Timestamp;
        public string Parent1Name;
        public string Parent2Name;
        public string Parent1Id;
        public string Parent2Id;
        public System.Guid Parent1Guid;
        public System.Guid Parent2Guid;
        public bool WasSuccessful;
        public string OffspringName;
        public string OffspringId;
        public System.Guid OffspringGuid;
        public float CompatibilityScore;
        public string FailureReason;
        public float BreedingTimeSeconds;
        public int OffspringGeneration;
        public float OffspringPurity;
        public int MutationCount;
        public string OffspringTraits;
        public bool HasRareTraits;
        public bool HasMagicalTraits;
        public Laboratory.Chimera.Core.BiomeType BiomeType;
        public bool IsFavorite;
    }


    #endregion
}