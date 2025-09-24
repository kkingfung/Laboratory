using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Laboratory.Chimera;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.UI
{
    public class CreatureListItem : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI creatureNameText;
        [SerializeField] private TextMeshProUGUI ageText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Toggle selectionToggle;
        
        [Header("Visual States")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.cyan;
        
        private CreatureInstanceComponent creature;
        private CreatureManagementUI managementUI;
        private bool isSelected = false;
        private bool isInitialized = false;
        
        public CreatureInstanceComponent Creature => creature;
        public bool IsSelected => isSelected;
        
        public void Initialize(CreatureInstanceComponent targetCreature, CreatureManagementUI managementUI)
        {
            this.creature = targetCreature;
            this.managementUI = managementUI;
            
            SetupEventHandlers();
            UpdateDisplay();
            
            isInitialized = true;
        }
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateSelectionVisuals();
            
            if (selectionToggle != null)
                selectionToggle.SetIsOnWithoutNotify(selected);
        }
        
        public void RefreshDisplay()
        {
            if (isInitialized)
                UpdateDisplay();
        }
        
        private void SetupEventHandlers()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelectClicked);
                
            if (selectionToggle != null)
                selectionToggle.onValueChanged.AddListener(OnSelectionToggled);
        }
        
        private void UpdateDisplay()
        {
            if (creature?.CreatureData == null) return;
            
            var data = creature.CreatureData;
            
            if (creatureNameText != null)
                creatureNameText.text = creature.name;
                
            if (ageText != null)
                ageText.text = $"{data.AgeInDays}d";
            
            UpdateBackgroundColor();
        }
        
        private void UpdateBackgroundColor()
        {
            if (backgroundImage == null) return;
            
            Color targetColor = isSelected ? selectedColor : normalColor;
            backgroundImage.color = targetColor;
        }
        
        private void UpdateSelectionVisuals()
        {
            UpdateBackgroundColor();
            
            if (selectionToggle != null)
                selectionToggle.SetIsOnWithoutNotify(isSelected);
        }
        
        private void OnSelectClicked()
        {
            if (managementUI != null)
                managementUI.ViewCreature(creature);
        }
        
        private void OnSelectionToggled(bool selected)
        {
            if (managementUI != null)
                managementUI.ToggleCreatureSelection(creature);
        }
    }
}