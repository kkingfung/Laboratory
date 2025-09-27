using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.UI
{
    /// <summary>
    /// Interactive creature card for the collection UI.
    /// Displays creature info, handles selection, and provides quick actions.
    /// </summary>
    public class CreatureCard : MonoBehaviour
    {
        [Header("Card Display")]
        [SerializeField] private Image creaturePortrait;
        [SerializeField] private Image rarityBorder;
        [SerializeField] private Image backgroundGradient;
        [SerializeField] private TextMeshProUGUI creatureNameText;
        [SerializeField] private TextMeshProUGUI generationText;
        [SerializeField] private TextMeshProUGUI purityText;
        [SerializeField] private TextMeshProUGUI traitsText;
        
        [Header("Status Indicators")]
        [SerializeField] private GameObject favoriteIcon;
        [SerializeField] private GameObject breedableIcon;
        [SerializeField] private GameObject mutationIcon;
        [SerializeField] private GameObject magicalIcon;
        [SerializeField] private GameObject injuredIcon;
        [SerializeField] private GameObject unhappyIcon;
        [SerializeField] private GameObject storageIcon;
        
        [Header("Health & Happiness Bars")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider happinessBar;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image happinessFill;
        
        [Header("Selection & Interaction")]
        [SerializeField] private Image selectionOverlay;
        [SerializeField] private Button cardButton;
        [SerializeField] private Button quickActionButton;
        [SerializeField] private Button favoriteButton;
        
        [Header("Animation")]
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private CollectionCreature creature;
        private CreatureCollectionManager collectionManager;
        private bool isSelected = false;
        private Vector3 originalScale;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            originalScale = transform.localScale;
            SetupEventHandlers();
        }
        
        private void OnEnable()
        {
            UpdateSelectionDisplay();
        }
        
        #endregion
        
        #region Setup and Configuration
        
        /// <summary>
        /// Set up this card with creature data
        /// </summary>
        public void SetupCard(CollectionCreature creatureData, CreatureCollectionManager manager)
        {
            creature = creatureData;
            collectionManager = manager;
            
            UpdateCardDisplay();
            UpdateStatusIndicators();
            UpdateHealthBars();
            SetupRarityVisuals();
        }
        
        private void SetupEventHandlers()
        {
            if (cardButton != null)
            {
                cardButton.onClick.AddListener(OnCardClicked);
            }
            
            if (quickActionButton != null)
                quickActionButton.onClick.AddListener(OnQuickActionClicked);
                
            if (favoriteButton != null)
                favoriteButton.onClick.AddListener(OnFavoriteClicked);
        }
        
        #endregion
        
        #region Display Updates
        
        private void UpdateCardDisplay()
        {
            if (creature == null) return;
            
            // Update basic info
            if (creatureNameText != null)
                creatureNameText.text = creature.Name;
                
            if (generationText != null)
                generationText.text = $"Gen {creature.Generation}";
                
            if (purityText != null)
            {
                purityText.text = $"{creature.GeneticPurity:P0}";
                purityText.color = GetPurityColor(creature.GeneticPurity);
            }
            
            if (traitsText != null)
                traitsText.text = creature.DominantTraits != null ? string.Join(", ", creature.DominantTraits) : "No dominant traits";
            
            // Update portrait
            if (creaturePortrait != null)
            {
                creaturePortrait.color = creature.PrimaryColor;
            }
            
            // Update background gradient
            if (backgroundGradient != null)
            {
                Color gradientColor = Color.Lerp(Color.black, creature.PrimaryColor, 0.3f);
                backgroundGradient.color = gradientColor;
            }
        }
        
        private void UpdateStatusIndicators()
        {
            if (favoriteIcon != null)
                favoriteIcon.SetActive(creature.IsFavorite);
                
            if (breedableIcon != null)
                breedableIcon.SetActive(creature.CanBreed);
                
            if (mutationIcon != null)
                mutationIcon.SetActive(creature.MutationCount > 0);
                
            if (magicalIcon != null)
                magicalIcon.SetActive(creature.HasMagicalTraits);
                
            if (injuredIcon != null)
                injuredIcon.SetActive(creature.Health < 80);
                
            if (unhappyIcon != null)
                unhappyIcon.SetActive(creature.Happiness < 0.6f);
                
            if (storageIcon != null)
                storageIcon.SetActive(creature.IsInStorage);
        }
        
        private void UpdateHealthBars()
        {
            if (healthBar != null)
            {
                healthBar.value = creature.Health / 100f;
                
                if (healthFill != null)
                {
                    Color healthColor = Color.Lerp(Color.red, Color.green, healthBar.value);
                    healthFill.color = healthColor;
                }
            }
            
            if (happinessBar != null)
            {
                happinessBar.value = creature.Happiness;
                
                if (happinessFill != null)
                {
                    Color happinessColor = Color.Lerp(Color.red, Color.yellow, happinessBar.value);
                    happinessFill.color = happinessColor;
                }
            }
        }
        
        private void SetupRarityVisuals()
        {
            if (rarityBorder == null) return;
            
            Color rarityColor = GetRarityColor(creature.RarityLevel);
            rarityBorder.color = rarityColor;
            
            // Animate legendary creatures
            if (creature.RarityLevel == RarityLevel.Legendary)
            {
                StartCoroutine(AnimateLegendaryBorder());
            }
        }
        
        private Color GetRarityColor(RarityLevel rarity)
        {
            switch (rarity)
            {
                case RarityLevel.Common: return Color.gray;
                case RarityLevel.Uncommon: return Color.green;
                case RarityLevel.Rare: return Color.blue;
                case RarityLevel.Epic: return Color.magenta;
                case RarityLevel.Legendary: return Color.gold;
                default: return Color.white;
            }
        }
        
        private Color GetPurityColor(float purity)
        {
            if (purity > 0.9f) return Color.gold;
            if (purity > 0.8f) return Color.cyan;
            if (purity > 0.7f) return Color.green;
            if (purity > 0.5f) return Color.yellow;
            return Color.red;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnCardClicked()
        {
            UnityEngine.Debug.Log($"🎯 Clicked creature card: {creature.Name}");
        }
        
        private void OnQuickActionClicked()
        {
            ShowQuickActionMenu();
        }
        
        private void OnFavoriteClicked()
        {
            ToggleFavorite();
        }
        
        #endregion
        
        #region Selection Management
        
        /// <summary>
        /// Set the selection state of this card
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateSelectionDisplay();
        }
        
        /// <summary>
        /// Get whether this card is currently selected
        /// </summary>
        public bool IsSelected()
        {
            return isSelected;
        }
        
        private void UpdateSelectionDisplay()
        {
            if (selectionOverlay != null)
            {
                selectionOverlay.gameObject.SetActive(isSelected);
                
                if (isSelected)
                {
                    selectionOverlay.color = new Color(1f, 1f, 1f, 0.3f);
                }
            }
        }
        
        #endregion
        
        #region Quick Actions
        
        private void ShowQuickActionMenu()
        {
            UnityEngine.Debug.Log($"📋 Quick action menu for {creature.Name}");
            
            // Show available actions based on creature state
            if (creature.CanBreed)
            {
                UnityEngine.Debug.Log("  • Breed");
            }
            
            if (creature.Health < 100)
            {
                UnityEngine.Debug.Log("  • Heal");
            }
            
            if (creature.Happiness < 1f)
            {
                UnityEngine.Debug.Log("  • Feed");
            }
            
            UnityEngine.Debug.Log("  • Store/Retrieve");
            UnityEngine.Debug.Log("  • Sell");
            UnityEngine.Debug.Log("  • Release");
        }
        
        private void QuickBreed()
        {
            UnityEngine.Debug.Log($"🧬 Quick breed requested for {creature.Name}");
            
            // Find suitable partner and start breeding
            var breedingUI = FindFirstObjectByType<AdvancedBreedingUI>();
            if (breedingUI != null)
            {
                var creatureComponent = creature.GameObjectInstance?.GetComponent<CreatureInstanceComponent>();
                if (creatureComponent != null)
                {
                    breedingUI.SelectCreatureAsParent(creatureComponent, 1);
                    breedingUI.ShowBreedingUI();
                }
            }
        }
        
        private void QuickHeal()
        {
            creature.Health = 100;
            
            if (creature.GameObjectInstance != null)
            {
                var creatureInstance = creature.GameObjectInstance.GetComponent<CreatureInstanceComponent>();
                if (creatureInstance?.CreatureData != null)
                {
                    creatureInstance.CreatureData.CurrentHealth = 100;
                }
            }
            
            UpdateHealthBars();
            UnityEngine.Debug.Log($"💚 Healed {creature.Name}");
        }
        
        private void QuickFeed()
        {
            creature.Happiness = Mathf.Min(1f, creature.Happiness + 0.2f);
            
            if (creature.GameObjectInstance != null)
            {
                var creatureInstance = creature.GameObjectInstance.GetComponent<CreatureInstanceComponent>();
                if (creatureInstance?.CreatureData != null)
                {
                    creatureInstance.CreatureData.Happiness = creature.Happiness;
                }
            }
            
            UpdateHealthBars();
            UnityEngine.Debug.Log($"🍖 Fed {creature.Name}");
        }
        
        private void ToggleFavorite()
        {
            creature.IsFavorite = !creature.IsFavorite;
            UpdateStatusIndicators();
            
            UnityEngine.Debug.Log($"⭐ {creature.Name} favorite status: {creature.IsFavorite}");
        }
        
        #endregion
        
        #region Animations
        
        private System.Collections.IEnumerator AnimateLegendaryBorder()
        {
            if (rarityBorder == null) yield break;
            
            while (gameObject.activeInHierarchy)
            {
                // Pulse the legendary border
                float pulse = Mathf.Sin(Time.time * 2f) * 0.5f + 0.5f;
                Color legendaryColor = Color.Lerp(Color.gold, Color.white, pulse);
                rarityBorder.color = legendaryColor;
                
                yield return null;
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get the creature data for this card
        /// </summary>
        public CollectionCreature GetCreature()
        {
            return creature;
        }
        
        /// <summary>
        /// Refresh the card display with updated creature data
        /// </summary>
        public void RefreshCard()
        {
            if (creature != null)
            {
                UpdateCardDisplay();
                UpdateStatusIndicators();
                UpdateHealthBars();
            }
        }
        
        #endregion
    }
}