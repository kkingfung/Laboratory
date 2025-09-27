using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.UI
{
    /// <summary>
    /// UI component for displaying genetic trait previews
    /// </summary>
    public class TraitPreviewItem : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI traitNameText;
        [SerializeField] private TextMeshProUGUI traitValueText;
        [SerializeField] private Image traitBar;
        [SerializeField] private Image traitIcon;
        
        private TraitExpression currentTrait;
        
        public void SetupTrait(TraitExpression trait)
        {
            currentTrait = trait;
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            if (currentTrait == null) return;
            
            if (traitNameText != null)
                traitNameText.text = currentTrait.traitName;
                
            if (traitValueText != null)
                traitValueText.text = $"{currentTrait.expressionLevel:P0}";
                
            if (traitBar != null)
            {
                traitBar.fillAmount = currentTrait.expressionLevel;
                traitBar.color = GetTraitColor(currentTrait.traitType);
            }
            
            if (traitIcon != null)
                traitIcon.color = GetTraitColor(currentTrait.traitType);
        }
        
        private Color GetTraitColor(TraitType traitType)
        {
            switch (traitType)
            {
                case TraitType.Physical: return Color.green;
                case TraitType.Mental: return Color.blue;
                case TraitType.Magical: return Color.magenta;
                case TraitType.Social: return Color.yellow;
                case TraitType.Combat: return Color.red;
                default: return Color.gray;
            }
        }

        // Missing methods that are referenced
        public void SetupTraitPreview(object traitPrediction, bool isAdvancedMode)
        {
            UnityEngine.Debug.Log($"Setting up trait preview - Advanced mode: {isAdvancedMode}");
            // Placeholder implementation for trait prediction display
        }

        public void SetAdvancedMode(bool enabled)
        {
            UnityEngine.Debug.Log($"Setting advanced mode: {enabled}");
            // Placeholder implementation for advanced mode toggle
        }
    }
}