using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// UI controller for character customization interface. 
    /// Handles user interactions and connects UI elements to the CharacterCustomizationManager.
    /// </summary>
    public class CharacterCustomizationUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Manager Reference")]
        [SerializeField] private Laboratory.Core.Character.CharacterCustomizationManager customizationManager;
        
        [Header("Color Pickers")]
        [SerializeField] private ColorPicker hairColorPicker;
        [SerializeField] private ColorPicker skinColorPicker;
        [SerializeField] private ColorPicker armorColorPicker;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Initializes UI event listeners on component start.
        /// </summary>
        private void Start()
        {
            SetupColorPickerEvents();
        }
        
        #endregion
        
        #region Public Methods - Hair Controls
        
        /// <summary>
        /// Cycles to the next hair style when UI button is pressed.
        /// </summary>
        public void NextHair()
        {
            if (customizationManager != null)
            {
                customizationManager.NextHair();
            }
        }
        
        /// <summary>
        /// Cycles to the previous hair style when UI button is pressed.
        /// </summary>
        public void PreviousHair()
        {
            if (customizationManager != null)
            {
                customizationManager.PreviousHair();
            }
        }
        
        #endregion
        
        #region Public Methods - Armor Controls
        
        /// <summary>
        /// Cycles to the next armor set when UI button is pressed.
        /// </summary>
        public void NextArmor()
        {
            if (customizationManager != null)
            {
                customizationManager.NextArmor();
            }
        }
        
        /// <summary>
        /// Cycles to the previous armor set when UI button is pressed.
        /// </summary>
        public void PreviousArmor()
        {
            if (customizationManager != null)
            {
                customizationManager.PreviousArmor();
            }
        }
        
        #endregion
        
        #region Public Methods - Save/Load
        
        /// <summary>
        /// Saves the current customization settings when save button is pressed.
        /// </summary>
        public void Save()
        {
            if (customizationManager != null)
            {
                customizationManager.SaveCustomization();
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Sets up event listeners for color picker components.
        /// </summary>
        private void SetupColorPickerEvents()
        {
            if (customizationManager == null)
            {
                Debug.LogError("CharacterCustomizationManager reference is not assigned!", this);
                return;
            }
            
            if (hairColorPicker != null)
            {
                hairColorPicker.onColorChanged += customizationManager.SetHairColor;
            }
            
            if (skinColorPicker != null)
            {
                skinColorPicker.onColorChanged += customizationManager.SetSkinColor;
            }
            
            if (armorColorPicker != null)
            {
                armorColorPicker.onColorChanged += customizationManager.SetArmorColor;
            }
        }
        
        #endregion
    }
}
