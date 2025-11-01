using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Customization;
using Laboratory.Core.Equipment.Types;
using Laboratory.Core.Equipment;

namespace Laboratory.Chimera.UI
{
    /// <summary>
    /// Comprehensive UI system for Chimera customization.
    /// Provides intuitive interface for equipment, outfits, colors, and genetic appearance.
    /// </summary>
    public class ChimeraCustomizationUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("🎭 Main UI Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject equipmentPanel;
        [SerializeField] private GameObject outfitPanel;
        [SerializeField] private GameObject colorPanel;
        [SerializeField] private GameObject geneticPanel;

        [Header("🎮 Navigation")]
        [SerializeField] private Button equipmentTabButton;
        [SerializeField] private Button outfitTabButton;
        [SerializeField] private Button colorTabButton;
        [SerializeField] private Button geneticTabButton;
        [SerializeField] private Button closeButton;

        [Header("🎒 Equipment UI")]
        [SerializeField] private Transform equipmentSlotContainer;
        [SerializeField] private GameObject equipmentSlotPrefab;
        [SerializeField] private Transform availableEquipmentContainer;
        [SerializeField] private GameObject equipmentItemPrefab;
        [SerializeField] private TextMeshProUGUI equipmentInfoText;

        [Header("👗 Outfit UI")]
        [SerializeField] private Transform outfitCategoriesContainer;
        [SerializeField] private GameObject outfitCategoryPrefab;
        [SerializeField] private Transform outfitPiecesContainer;
        [SerializeField] private GameObject outfitPiecePrefab;
        [SerializeField] private Button saveOutfitButton;
        [SerializeField] private Button loadOutfitButton;

        [Header("🌈 Color UI")]
        [SerializeField] private Transform colorPresetsContainer;
        [SerializeField] private GameObject colorPresetPrefab;
        [SerializeField] private Button primaryColorButton;
        [SerializeField] private Button secondaryColorButton;
        [SerializeField] private Button accentColorButton;
        [SerializeField] private ColorPickerUI colorPicker;

        [Header("🧬 Genetic UI")]
        [SerializeField] private Button regenerateAppearanceButton;
        [SerializeField] private Slider bodyScaleSlider;
        [SerializeField] private Slider patternIntensitySlider;
        [SerializeField] private Toggle enablePatternsToggle;
        [SerializeField] private Transform patternContainer;
        [SerializeField] private GameObject patternTogglePrefab;

        [Header("💾 Save/Load")]
        [SerializeField] private Button saveCustomizationButton;
        [SerializeField] private Button loadCustomizationButton;
        [SerializeField] private Button resetToDefaultButton;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("🎯 Preview")]
        [SerializeField] private Transform previewContainer;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Button rotateLeftButton;
        [SerializeField] private Button rotateRightButton;

        #endregion

        #region Private Fields

        private ChimeraCustomizationManager customizationManager;
        private EquipmentManager equipmentManager;
        private ChimeraCustomizationConfig config;

        private Dictionary<EquipmentType, EquipmentSlotUI> equipmentSlots = new();
        private Dictionary<string, OutfitCategoryUI> outfitCategories = new();
        private List<ColorPresetUI> colorPresets = new();

        private string currentActiveTab = "equipment";
        private Color currentSelectedColor = Color.white;
        private string currentColorTarget = "primary";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            SetupEventListeners();
        }

        private void Start()
        {
            InitializeUI();
            ShowTab("equipment");
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            customizationManager = FindObjectOfType<ChimeraCustomizationManager>();
            equipmentManager = FindObjectOfType<EquipmentManager>();

            if (customizationManager != null)
            {
                config = customizationManager.GetComponent<ChimeraCustomizationManager>().GetComponent<ChimeraCustomizationConfig>();
            }

            ValidateUIReferences();
        }

        private void SetupEventListeners()
        {
            // Tab navigation
            equipmentTabButton?.onClick.AddListener(() => ShowTab("equipment"));
            outfitTabButton?.onClick.AddListener(() => ShowTab("outfit"));
            colorTabButton?.onClick.AddListener(() => ShowTab("color"));
            geneticTabButton?.onClick.AddListener(() => ShowTab("genetic"));
            closeButton?.onClick.AddListener(CloseUI);

            // Equipment events
            // (Individual slot listeners will be added when creating slots)

            // Outfit events
            saveOutfitButton?.onClick.AddListener(SaveCurrentOutfit);
            loadOutfitButton?.onClick.AddListener(LoadOutfit);

            // Color events
            primaryColorButton?.onClick.AddListener(() => OpenColorPicker("primary"));
            secondaryColorButton?.onClick.AddListener(() => OpenColorPicker("secondary"));
            accentColorButton?.onClick.AddListener(() => OpenColorPicker("accent"));

            // Genetic events
            regenerateAppearanceButton?.onClick.AddListener(RegenerateGeneticAppearance);
            bodyScaleSlider?.onValueChanged.AddListener(OnBodyScaleChanged);
            patternIntensitySlider?.onValueChanged.AddListener(OnPatternIntensityChanged);
            enablePatternsToggle?.onValueChanged.AddListener(OnPatternsToggleChanged);

            // Save/Load events
            saveCustomizationButton?.onClick.AddListener(SaveCustomization);
            loadCustomizationButton?.onClick.AddListener(LoadCustomization);
            resetToDefaultButton?.onClick.AddListener(ResetToDefault);

            // Preview events
            rotateLeftButton?.onClick.AddListener(() => RotatePreview(-30f));
            rotateRightButton?.onClick.AddListener(() => RotatePreview(30f));
        }

        private void ValidateUIReferences()
        {
            if (mainPanel == null)
                Debug.LogWarning("ChimeraCustomizationUI: Main panel not assigned");

            if (customizationManager == null)
                Debug.LogWarning("ChimeraCustomizationUI: No ChimeraCustomizationManager found in scene");

            if (equipmentManager == null)
                Debug.LogWarning("ChimeraCustomizationUI: No EquipmentManager found in scene");
        }

        #endregion

        #region UI Management

        public void ShowUI()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
                RefreshUI();
            }
        }

        public void HideUI()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }
        }

        public void CloseUI()
        {
            HideUI();
        }

        private void ShowTab(string tabName)
        {
            currentActiveTab = tabName;

            // Hide all panels
            equipmentPanel?.SetActive(false);
            outfitPanel?.SetActive(false);
            colorPanel?.SetActive(false);
            geneticPanel?.SetActive(false);

            // Show selected panel
            switch (tabName)
            {
                case "equipment":
                    equipmentPanel?.SetActive(true);
                    RefreshEquipmentUI();
                    break;
                case "outfit":
                    outfitPanel?.SetActive(true);
                    RefreshOutfitUI();
                    break;
                case "color":
                    colorPanel?.SetActive(true);
                    RefreshColorUI();
                    break;
                case "genetic":
                    geneticPanel?.SetActive(true);
                    RefreshGeneticUI();
                    break;
            }

            UpdateTabButtonStates();
        }

        private void UpdateTabButtonStates()
        {
            // Update tab button visual states based on current active tab
            UpdateButtonState(equipmentTabButton, currentActiveTab == "equipment");
            UpdateButtonState(outfitTabButton, currentActiveTab == "outfit");
            UpdateButtonState(colorTabButton, currentActiveTab == "color");
            UpdateButtonState(geneticTabButton, currentActiveTab == "genetic");
        }

        private void UpdateButtonState(Button button, bool isActive)
        {
            if (button == null) return;

            var colors = button.colors;
            colors.normalColor = isActive ? Color.yellow : Color.white;
            button.colors = colors;
        }

        private void RefreshUI()
        {
            switch (currentActiveTab)
            {
                case "equipment":
                    RefreshEquipmentUI();
                    break;
                case "outfit":
                    RefreshOutfitUI();
                    break;
                case "color":
                    RefreshColorUI();
                    break;
                case "genetic":
                    RefreshGeneticUI();
                    break;
            }
        }

        #endregion

        #region Equipment UI

        private void InitializeUI()
        {
            CreateEquipmentSlots();
            CreateOutfitCategories();
            CreateColorPresets();
            CreatePatternToggles();
        }

        private void CreateEquipmentSlots()
        {
            if (equipmentSlotContainer == null || equipmentSlotPrefab == null) return;

            // Create slots for each equipment type
            var equipmentTypes = System.Enum.GetValues(typeof(EquipmentType)).Cast<EquipmentType>();

            foreach (var equipmentType in equipmentTypes)
            {
                if (equipmentType == EquipmentType.None) continue;

                GameObject slotObject = Instantiate(equipmentSlotPrefab, equipmentSlotContainer);
                var slotUI = slotObject.GetComponent<EquipmentSlotUI>();

                if (slotUI != null)
                {
                    slotUI.Initialize(equipmentType, OnEquipmentSlotClicked, OnEquipmentSlotDropped);
                    equipmentSlots[equipmentType] = slotUI;
                }
            }
        }

        private void RefreshEquipmentUI()
        {
            if (customizationManager?.CurrentCustomization == null) return;

            // Update equipment slots with currently equipped items
            var equippedItems = customizationManager.CurrentCustomization.EquippedItems;

            foreach (var slot in equipmentSlots.Values)
            {
                var equippedItem = equippedItems.FirstOrDefault(e =>
                    ConvertToEquipmentType(e.EquipmentType) == slot.EquipmentType);

                slot.UpdateSlot(equippedItem);
            }

            // Refresh available equipment list
            RefreshAvailableEquipment();
        }

        private void RefreshAvailableEquipment()
        {
            if (availableEquipmentContainer == null || equipmentItemPrefab == null) return;

            // Clear existing items
            for (int i = availableEquipmentContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(availableEquipmentContainer.GetChild(i).gameObject);
            }

            // Add available equipment items
            // This would integrate with your equipment database
            var availableItems = GetAvailableEquipmentForChimera();

            foreach (var item in availableItems)
            {
                GameObject itemObject = Instantiate(equipmentItemPrefab, availableEquipmentContainer);
                var itemUI = itemObject.GetComponent<EquipmentItemUI>();

                if (itemUI != null)
                {
                    itemUI.Initialize(item, OnEquipmentItemClicked);
                }
            }
        }

        private void OnEquipmentSlotClicked(EquipmentType slotType)
        {
            // Handle equipment slot click (for unequipping)
            var convertedType = ConvertToMonsterTownEquipmentType(slotType);
            customizationManager?.UnequipItem(convertedType);
            RefreshEquipmentUI();
        }

        private void OnEquipmentSlotDropped(EquipmentType slotType, string itemId)
        {
            // Handle dropping equipment onto slot
            var item = GetEquipmentItemById(itemId);
            if (item != null)
            {
                customizationManager?.EquipItem(item);
                RefreshEquipmentUI();
            }
        }

        private void OnEquipmentItemClicked(string itemId)
        {
            // Handle clicking equipment item (for equipping)
            var item = GetEquipmentItemById(itemId);
            if (item != null)
            {
                customizationManager?.EquipItem(item);
                RefreshEquipmentUI();
                UpdateEquipmentInfo(item);
            }
        }

        private void UpdateEquipmentInfo(Laboratory.Core.MonsterTown.Equipment item)
        {
            if (equipmentInfoText == null) return;

            string infoText = $"<b>{item.Name}</b>\n";
            infoText += $"Rarity: {item.Rarity}\n";
            infoText += $"Level: {item.Level}\n";
            infoText += $"Type: {item.Type}\n\n";
            infoText += "Stat Bonuses:\n";

            foreach (var bonus in item.StatBonuses)
            {
                infoText += $"• {bonus.Key}: +{bonus.Value:F1}\n";
            }

            equipmentInfoText.text = infoText;
        }

        #endregion

        #region Outfit UI

        private void CreateOutfitCategories()
        {
            if (outfitCategoriesContainer == null || outfitCategoryPrefab == null) return;

            string[] categories = { "Body", "Head", "Limbs", "Accessories" };

            foreach (var category in categories)
            {
                GameObject categoryObject = Instantiate(outfitCategoryPrefab, outfitCategoriesContainer);
                var categoryUI = categoryObject.GetComponent<OutfitCategoryUI>();

                if (categoryUI != null)
                {
                    categoryUI.Initialize(category, OnOutfitCategorySelected);
                    outfitCategories[category] = categoryUI;
                }
            }
        }

        private void RefreshOutfitUI()
        {
            if (customizationManager?.CurrentCustomization?.CustomOutfit == null) return;

            var currentOutfit = customizationManager.CurrentCustomization.CustomOutfit;

            // Update category UI with current outfit pieces
            foreach (var category in outfitCategories)
            {
                var piecesInCategory = currentOutfit.OutfitPieces?.Where(p => p.Category == category.Key) ?? new OutfitPiece[0];
                category.Value.UpdatePieces(piecesInCategory.ToArray());
            }
        }

        private void OnOutfitCategorySelected(string category)
        {
            RefreshOutfitPieces(category);
        }

        private void RefreshOutfitPieces(string category)
        {
            if (outfitPiecesContainer == null || outfitPiecePrefab == null) return;

            // Clear existing pieces
            for (int i = outfitPiecesContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(outfitPiecesContainer.GetChild(i).gameObject);
            }

            // Get available pieces for category
            var availablePieces = GetAvailableOutfitPieces(category);

            foreach (var piece in availablePieces)
            {
                GameObject pieceObject = Instantiate(outfitPiecePrefab, outfitPiecesContainer);
                var pieceUI = pieceObject.GetComponent<OutfitPieceUI>();

                if (pieceUI != null)
                {
                    pieceUI.Initialize(piece, OnOutfitPieceSelected);
                }
            }
        }

        private void OnOutfitPieceSelected(OutfitPiece piece)
        {
            // Apply the selected outfit piece
            customizationManager?.CreateCustomOutfitPiece(piece);
            RefreshOutfitUI();
        }

        private void SaveCurrentOutfit()
        {
            if (customizationManager?.CurrentCustomization?.CustomOutfit == null) return;

            // Save current outfit configuration
            string outfitName = $"Custom_Outfit_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            var outfitData = customizationManager.CurrentCustomization.CustomOutfit;
            outfitData.OutfitName = outfitName;

            // Save to PlayerPrefs or file system
            string json = JsonUtility.ToJson(outfitData, true);
            PlayerPrefs.SetString($"SavedOutfit_{outfitName}", json);
            PlayerPrefs.Save();

            ShowStatus($"Outfit saved as '{outfitName}'");
        }

        private void LoadOutfit()
        {
            // Implementation for loading saved outfits
            // Would show a list of saved outfits to choose from
            ShowStatus("Load outfit functionality - to be implemented");
        }

        #endregion

        #region Color UI

        private void CreateColorPresets()
        {
            if (colorPresetsContainer == null || colorPresetPrefab == null || config?.ColorConfig?.ColorPresets == null) return;

            foreach (var preset in config.ColorConfig.ColorPresets)
            {
                GameObject presetObject = Instantiate(colorPresetPrefab, colorPresetsContainer);
                var presetUI = presetObject.GetComponent<ColorPresetUI>();

                if (presetUI != null)
                {
                    presetUI.Initialize(preset, OnColorPresetSelected);
                    colorPresets.Add(presetUI);
                }
            }
        }

        private void RefreshColorUI()
        {
            if (customizationManager?.CurrentCustomization?.ColorOverrides == null) return;

            var colorOverrides = customizationManager.CurrentCustomization.ColorOverrides;

            // Update color buttons with current colors
            UpdateColorButton(primaryColorButton, colorOverrides.GetValueOrDefault("primary", Color.white));
            UpdateColorButton(secondaryColorButton, colorOverrides.GetValueOrDefault("secondary", Color.gray));
            UpdateColorButton(accentColorButton, colorOverrides.GetValueOrDefault("accent", Color.yellow));
        }

        private void UpdateColorButton(Button button, Color color)
        {
            if (button == null) return;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        private void OnColorPresetSelected(ColorPreset preset)
        {
            var colorScheme = new Dictionary<string, Color>
            {
                ["primary"] = preset.PrimaryColor,
                ["secondary"] = preset.SecondaryColor,
                ["accent"] = preset.AccentColor
            };

            customizationManager?.ApplyCustomColors(colorScheme);
            RefreshColorUI();
        }

        private void OpenColorPicker(string colorTarget)
        {
            currentColorTarget = colorTarget;

            if (colorPicker != null)
            {
                var currentColor = GetCurrentColorForTarget(colorTarget);
                colorPicker.ShowColorPicker(currentColor, OnColorSelected);
            }
        }

        private Color GetCurrentColorForTarget(string target)
        {
            if (customizationManager?.CurrentCustomization?.ColorOverrides == null)
                return Color.white;

            return customizationManager.CurrentCustomization.ColorOverrides.GetValueOrDefault(target, Color.white);
        }

        private void OnColorSelected(Color color)
        {
            currentSelectedColor = color;

            switch (currentColorTarget)
            {
                case "primary":
                    customizationManager?.SetPrimaryColor(color);
                    break;
                case "secondary":
                    customizationManager?.SetSecondaryColor(color);
                    break;
                case "accent":
                    customizationManager?.SetAccentColor(color);
                    break;
            }

            RefreshColorUI();
        }

        #endregion

        #region Genetic UI

        private void CreatePatternToggles()
        {
            if (patternContainer == null || patternTogglePrefab == null || config?.GeneticConfig?.AvailablePatterns == null) return;

            foreach (var pattern in config.GeneticConfig.AvailablePatterns)
            {
                GameObject toggleObject = Instantiate(patternTogglePrefab, patternContainer);
                var toggle = toggleObject.GetComponent<Toggle>();
                var label = toggleObject.GetComponentInChildren<TextMeshProUGUI>();

                if (toggle != null && label != null)
                {
                    label.text = pattern.PatternName;
                    toggle.onValueChanged.AddListener((isOn) => OnPatternToggleChanged(pattern.PatternName, isOn));
                }
            }
        }

        private void RefreshGeneticUI()
        {
            if (customizationManager?.CurrentCustomization?.GeneticAppearance == null) return;

            var appearance = customizationManager.CurrentCustomization.GeneticAppearance;

            // Update sliders with current values
            if (bodyScaleSlider != null)
            {
                bodyScaleSlider.value = appearance.BodyScale.x; // Use X component as representative scale
            }

            if (patternIntensitySlider != null)
            {
                patternIntensitySlider.value = appearance.PatternIntensity;
            }

            if (enablePatternsToggle != null)
            {
                enablePatternsToggle.isOn = appearance.PatternTypes != null && appearance.PatternTypes.Length > 0;
            }
        }

        private void RegenerateGeneticAppearance()
        {
            customizationManager?.ResetToGeneticDefaults();
            RefreshGeneticUI();
            ShowStatus("Genetic appearance regenerated");
        }

        private void OnBodyScaleChanged(float scale)
        {
            // Update body scale in real-time
            if (customizationManager?.CreatureInstance != null)
            {
                var scaleVector = Vector3.one * scale;
                customizationManager.CreatureInstance.transform.localScale = scaleVector;

                // Update customization data
                if (customizationManager.CurrentCustomization?.GeneticAppearance != null)
                {
                    customizationManager.CurrentCustomization.GeneticAppearance.BodyScale = scaleVector;
                }
            }
        }

        private void OnPatternIntensityChanged(float intensity)
        {
            // Update pattern intensity
            if (customizationManager?.CurrentCustomization?.GeneticAppearance != null)
            {
                customizationManager.CurrentCustomization.GeneticAppearance.PatternIntensity = intensity;
                // Apply pattern intensity change
                ApplyPatternIntensityChange(intensity);
            }
        }

        private void OnPatternsToggleChanged(bool enabled)
        {
            // Enable/disable patterns
            if (patternContainer != null)
            {
                patternContainer.gameObject.SetActive(enabled);
            }
        }

        private void OnPatternToggleChanged(string patternName, bool enabled)
        {
            if (customizationManager?.CurrentCustomization?.GeneticAppearance == null) return;

            var appearance = customizationManager.CurrentCustomization.GeneticAppearance;
            var patterns = appearance.PatternTypes?.ToList() ?? new List<string>();

            if (enabled && !patterns.Contains(patternName))
            {
                patterns.Add(patternName);
            }
            else if (!enabled && patterns.Contains(patternName))
            {
                patterns.Remove(patternName);
            }

            appearance.PatternTypes = patterns.ToArray();
            ApplyPatternChanges();
        }

        private void ApplyPatternIntensityChange(float intensity)
        {
            // Apply pattern intensity change to visual system
            // This would integrate with the visual genetics system
        }

        private void ApplyPatternChanges()
        {
            // Apply pattern changes to the creature
            // This would integrate with the visual genetics system
        }

        #endregion

        #region Save/Load System

        private void SaveCustomization()
        {
            customizationManager?.SaveCustomization();
            ShowStatus("Customization saved");
        }

        private void LoadCustomization()
        {
            bool loaded = customizationManager?.LoadCustomization() ?? false;
            ShowStatus(loaded ? "Customization loaded" : "No saved customization found");

            if (loaded)
            {
                RefreshUI();
            }
        }

        private void ResetToDefault()
        {
            customizationManager?.ResetToGeneticDefaults();
            RefreshUI();
            ShowStatus("Reset to genetic defaults");
        }

        #endregion

        #region Preview System

        private void RotatePreview(float degrees)
        {
            if (previewContainer != null)
            {
                previewContainer.Rotate(0, degrees, 0);
            }
        }

        #endregion

        #region Utility Methods

        private void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                // Auto-clear after 3 seconds
                Invoke(nameof(ClearStatus), 3f);
            }
        }

        private void ClearStatus()
        {
            if (statusText != null)
            {
                statusText.text = "";
            }
        }

        private Laboratory.Core.MonsterTown.Equipment[] GetAvailableEquipmentForChimera()
        {
            // Implementation would return available equipment for chimeras
            // This is a placeholder
            return new Laboratory.Core.MonsterTown.Equipment[0];
        }

        private Laboratory.Core.MonsterTown.Equipment GetEquipmentItemById(string itemId)
        {
            // Implementation would fetch equipment by ID
            // This is a placeholder
            return null;
        }

        private OutfitPiece[] GetAvailableOutfitPieces(string category)
        {
            // Implementation would return available outfit pieces for category
            // This is a placeholder
            return new OutfitPiece[0];
        }

        private EquipmentType ConvertToEquipmentType(Laboratory.Core.MonsterTown.EquipmentType monsterTownType)
        {
            return monsterTownType switch
            {
                Laboratory.Core.MonsterTown.EquipmentType.Armor => EquipmentType.Armor,
                Laboratory.Core.MonsterTown.EquipmentType.Weapon => EquipmentType.Weapon,
                Laboratory.Core.MonsterTown.EquipmentType.Accessory => EquipmentType.Accessory,
                Laboratory.Core.MonsterTown.EquipmentType.RidingGear => EquipmentType.RidingGear,
                _ => EquipmentType.Accessory
            };
        }

        private Laboratory.Core.MonsterTown.EquipmentType ConvertToMonsterTownEquipmentType(EquipmentType equipmentType)
        {
            return equipmentType switch
            {
                EquipmentType.Armor => Laboratory.Core.MonsterTown.EquipmentType.Armor,
                EquipmentType.Weapon => Laboratory.Core.MonsterTown.EquipmentType.Weapon,
                EquipmentType.Accessory => Laboratory.Core.MonsterTown.EquipmentType.Accessory,
                EquipmentType.RidingGear => Laboratory.Core.MonsterTown.EquipmentType.RidingGear,
                _ => Laboratory.Core.MonsterTown.EquipmentType.Accessory
            };
        }

        #endregion
    }

    #region UI Component Classes

    public class EquipmentSlotUI : MonoBehaviour
    {
        [SerializeField] private Image slotIcon;
        [SerializeField] private TextMeshProUGUI slotLabel;
        [SerializeField] private Button slotButton;

        public EquipmentType EquipmentType { get; private set; }

        public void Initialize(EquipmentType equipmentType, System.Action<EquipmentType> onClicked, System.Action<EquipmentType, string> onDropped)
        {
            EquipmentType = equipmentType;

            if (slotLabel != null)
                slotLabel.text = equipmentType.ToString();

            if (slotButton != null)
                slotButton.onClick.AddListener(() => onClicked?.Invoke(equipmentType));
        }

        public void UpdateSlot(EquippedItemVisual equippedItem)
        {
            if (slotIcon != null)
            {
                // Update slot icon based on equipped item
                slotIcon.color = equippedItem != null ? Color.white : Color.gray;
            }
        }
    }

    public class EquipmentItemUI : MonoBehaviour
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemName;
        [SerializeField] private Button itemButton;

        public void Initialize(Laboratory.Core.MonsterTown.Equipment item, System.Action<string> onClicked)
        {
            if (itemName != null)
                itemName.text = item.Name;

            if (itemButton != null)
                itemButton.onClick.AddListener(() => onClicked?.Invoke(item.ItemId));
        }
    }

    public class OutfitCategoryUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI categoryLabel;
        [SerializeField] private Button categoryButton;

        public void Initialize(string category, System.Action<string> onSelected)
        {
            if (categoryLabel != null)
                categoryLabel.text = category;

            if (categoryButton != null)
                categoryButton.onClick.AddListener(() => onSelected?.Invoke(category));
        }

        public void UpdatePieces(OutfitPiece[] pieces)
        {
            // Update category display with current pieces
        }
    }

    public class OutfitPieceUI : MonoBehaviour
    {
        [SerializeField] private Image pieceIcon;
        [SerializeField] private TextMeshProUGUI pieceName;
        [SerializeField] private Button pieceButton;

        public void Initialize(OutfitPiece piece, System.Action<OutfitPiece> onSelected)
        {
            if (pieceName != null)
                pieceName.text = piece.PieceName;

            if (pieceButton != null)
                pieceButton.onClick.AddListener(() => onSelected?.Invoke(piece));
        }
    }

    public class ColorPresetUI : MonoBehaviour
    {
        [SerializeField] private Button presetButton;
        [SerializeField] private Image[] colorSwatches;

        public void Initialize(ColorPreset preset, System.Action<ColorPreset> onSelected)
        {
            if (colorSwatches != null && colorSwatches.Length >= 3)
            {
                colorSwatches[0].color = preset.PrimaryColor;
                colorSwatches[1].color = preset.SecondaryColor;
                colorSwatches[2].color = preset.AccentColor;
            }

            if (presetButton != null)
                presetButton.onClick.AddListener(() => onSelected?.Invoke(preset));
        }
    }

    public class ColorPickerUI : MonoBehaviour
    {
        [SerializeField] private GameObject colorPickerPanel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private System.Action<Color> onColorSelected;
        private Color selectedColor;

        public void ShowColorPicker(Color initialColor, System.Action<Color> onSelected)
        {
            selectedColor = initialColor;
            onColorSelected = onSelected;

            if (colorPickerPanel != null)
                colorPickerPanel.SetActive(true);
        }

        public void HideColorPicker()
        {
            if (colorPickerPanel != null)
                colorPickerPanel.SetActive(false);
        }

        private void Awake()
        {
            confirmButton?.onClick.AddListener(() => {
                onColorSelected?.Invoke(selectedColor);
                HideColorPicker();
            });

            cancelButton?.onClick.AddListener(HideColorPicker);
        }
    }

    #endregion
}