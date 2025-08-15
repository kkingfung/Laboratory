using UnityEngine;
using UnityEngine.UI;
using System;

namespace Laboratory.UI.Tools
{
    /// <summary>
    /// Interactive color picker component using RGB sliders.
    /// Provides real-time color selection and change notifications.
    /// </summary>
    public class ColorPicker : MonoBehaviour
    {
        #region Events
        
        /// <summary>
        /// Invoked whenever the color value changes through user interaction.
        /// </summary>
        public event Action<Color> OnColorChanged;
        
        #endregion
        
        #region Fields
        
        [Header("UI References")]
        [Tooltip("Red color channel slider")]
        [SerializeField] private Slider rSlider;
        
        [Tooltip("Green color channel slider")]
        [SerializeField] private Slider gSlider;
        
        [Tooltip("Blue color channel slider")]
        [SerializeField] private Slider bSlider;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Initializes slider event listeners and sets initial color.
        /// </summary>
        private void Start()
        {
            RegisterSliderEvents();
            UpdateColor(0f); // Initialize with current slider values
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Programmatically sets the color and updates all slider values.
        /// </summary>
        /// <param name="color">The new color to set</param>
        public void SetColor(Color color)
        {
            rSlider.value = color.r;
            gSlider.value = color.g;
            bSlider.value = color.b;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Registers value change events for all color sliders.
        /// </summary>
        private void RegisterSliderEvents()
        {
            rSlider.onValueChanged.AddListener(UpdateColor);
            gSlider.onValueChanged.AddListener(UpdateColor);
            bSlider.onValueChanged.AddListener(UpdateColor);
        }
        
        /// <summary>
        /// Updates the current color based on slider values and notifies listeners.
        /// </summary>
        /// <param name="_">Unused parameter from slider callback</param>
        private void UpdateColor(float _)
        {
            Color newColor = new Color(rSlider.value, gSlider.value, bSlider.value);
            OnColorChanged?.Invoke(newColor);
        }
        
        #endregion
    }
}
