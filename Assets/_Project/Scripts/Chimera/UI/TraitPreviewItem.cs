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
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillBar;

        private TraitExpression currentTrait;
        private string _traitName;
        private float? _currentValue;
        private bool _isAdvancedMode;
        
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
            return traitType.GetCategory().GetCategoryColor();
        }

        public void SetupTraitPreview(object traitPrediction, bool isAdvancedMode)
        {
            if (traitPrediction == null) return;

            // Extract trait data from prediction
            _traitName = traitPrediction.GetType().GetProperty("TraitName")?.GetValue(traitPrediction)?.ToString() ?? "Unknown Trait";
            var valueProperty = traitPrediction.GetType().GetProperty("PredictedValue") ??
                               traitPrediction.GetType().GetProperty("Value");

            if (valueProperty != null && float.TryParse(valueProperty.GetValue(traitPrediction)?.ToString(), out float value))
            {
                _currentValue = value;
            }

            // Basic trait preview implementation
            if (traitNameText != null)
            {
                traitNameText.text = _traitName;
            }

            if (traitValueText != null && _currentValue.HasValue)
            {
                if (isAdvancedMode)
                {
                    // Show detailed prediction information
                    traitValueText.text = $"{_currentValue:F1} (predicted)";
                }
                else
                {
                    // Show simple value
                    traitValueText.text = _currentValue.Value.ToString("F0");
                }
            }

            // Update visual elements based on prediction confidence
            UpdateVisualElements(isAdvancedMode);

            UnityEngine.Debug.Log($"Trait preview setup: {_traitName} - Advanced: {isAdvancedMode}");
        }

        public void SetAdvancedMode(bool enabled)
        {
            _isAdvancedMode = enabled;

            // Update display based on advanced mode
            if (traitValueText != null && _currentValue.HasValue)
            {
                if (enabled)
                {
                    traitValueText.text = $"{_currentValue:F2} ± {_currentValue * 0.1f:F2}";
                }
                else
                {
                    traitValueText.text = _currentValue.Value.ToString("F0");
                }
            }

            // Show/hide additional UI elements for advanced mode
            UpdateVisualElements(enabled);

            UnityEngine.Debug.Log($"Advanced mode toggled: {enabled} for trait {_traitName}");
        }

        private void UpdateVisualElements(bool isAdvancedMode)
        {
            // Update background color based on trait value
            if (backgroundImage != null && _currentValue.HasValue)
            {
                float normalizedValue = _currentValue.Value / 100f;
                Color traitColor = Color.Lerp(Color.red, Color.green, normalizedValue);

                if (isAdvancedMode)
                {
                    // More saturated colors in advanced mode
                    traitColor.a = 0.3f;
                }
                else
                {
                    // Subtle colors in simple mode
                    traitColor.a = 0.1f;
                }

                backgroundImage.color = traitColor;
            }

            // Update fill bar if available
            if (fillBar != null && _currentValue.HasValue)
            {
                fillBar.fillAmount = _currentValue.Value / 100f;
            }
        }
    }
}