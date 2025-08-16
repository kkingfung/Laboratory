using System;
using UnityEngine;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Manages character customization features including hair styles, armor sets, and color customization.
    /// Handles saving and loading customization data to PlayerPrefs.
    /// </summary>
    public class CharacterCustomizationManager : MonoBehaviour
    {
        #region Constants
        
        private const string SAVE_KEY = "CharacterCustomization";
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Character Parts")]
        [SerializeField] private SkinnedMeshRenderer hairRenderer;
        [SerializeField] private SkinnedMeshRenderer bodyRenderer;
        [SerializeField] private SkinnedMeshRenderer armorRenderer;

        [Header("Hair Styles")]
        [SerializeField] private Mesh[] hairMeshes;

        [Header("Armor Sets")]
        [SerializeField] private Mesh[] armorMeshes;

        [Header("Material Instances")]
        [SerializeField] private Material hairMaterial;
        [SerializeField] private Material skinMaterial;
        [SerializeField] private Material armorMaterial;
        
        #endregion
        
        #region Private Fields
        
        private int currentHairIndex = 0;
        private int currentArmorIndex = 0;
        private Color currentHairColor = Color.black;
        private Color currentSkinColor = Color.white;
        private Color currentArmorColor = Color.gray;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Initializes the component and loads saved customization data.
        /// </summary>
        private void Start()
        {
            LoadCustomization();
        }
        
        #endregion
        
        #region Public Methods - Hair Customization
        
        /// <summary>
        /// Cycles to the next available hair style.
        /// </summary>
        public void NextHair()
        {
            if (hairMeshes == null || hairMeshes.Length == 0) return;
            
            currentHairIndex = (currentHairIndex + 1) % hairMeshes.Length;
            ApplyHairMesh();
        }

        /// <summary>
        /// Cycles to the previous hair style.
        /// </summary>
        public void PreviousHair()
        {
            if (hairMeshes == null || hairMeshes.Length == 0) return;
            
            currentHairIndex = (currentHairIndex - 1 + hairMeshes.Length) % hairMeshes.Length;
            ApplyHairMesh();
        }

        /// <summary>
        /// Sets the hair color to the specified color.
        /// </summary>
        /// <param name="color">The new hair color</param>
        public void SetHairColor(Color color)
        {
            currentHairColor = color;
            if (hairMaterial != null)
            {
                hairMaterial.color = currentHairColor;
            }
        }
        
        #endregion
        
        #region Public Methods - Armor Customization
        
        /// <summary>
        /// Cycles to the next available armor set.
        /// </summary>
        public void NextArmor()
        {
            if (armorMeshes == null || armorMeshes.Length == 0) return;
            
            currentArmorIndex = (currentArmorIndex + 1) % armorMeshes.Length;
            ApplyArmorMesh();
        }

        /// <summary>
        /// Cycles to the previous armor set.
        /// </summary>
        public void PreviousArmor()
        {
            if (armorMeshes == null || armorMeshes.Length == 0) return;
            
            currentArmorIndex = (currentArmorIndex - 1 + armorMeshes.Length) % armorMeshes.Length;
            ApplyArmorMesh();
        }

        /// <summary>
        /// Sets the armor color to the specified color.
        /// </summary>
        /// <param name="color">The new armor color</param>
        public void SetArmorColor(Color color)
        {
            currentArmorColor = color;
            if (armorMaterial != null)
            {
                armorMaterial.color = currentArmorColor;
            }
        }
        
        #endregion
        
        #region Public Methods - Skin Customization
        
        /// <summary>
        /// Sets the skin color to the specified color.
        /// </summary>
        /// <param name="color">The new skin color</param>
        public void SetSkinColor(Color color)
        {
            currentSkinColor = color;
            if (skinMaterial != null)
            {
                skinMaterial.color = currentSkinColor;
            }
        }
        
        #endregion
        
        #region Public Methods - Save/Load
        
        /// <summary>
        /// Saves the current customization settings to PlayerPrefs.
        /// </summary>
        public void SaveCustomization()
        {
            var data = new CharacterCustomizationData()
            {
                hairIndex = currentHairIndex,
                armorIndex = currentArmorIndex,
                hairColor = new SerializableColor(currentHairColor),
                skinColor = new SerializableColor(currentSkinColor),
                armorColor = new SerializableColor(currentArmorColor)
            };

            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads customization settings from PlayerPrefs and applies them to the character.
        /// </summary>
        public void LoadCustomization()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return;
            
            try
            {
                string json = PlayerPrefs.GetString(SAVE_KEY);
                var data = JsonUtility.FromJson<CharacterCustomizationData>(json);

                currentHairIndex = Mathf.Clamp(data.hairIndex, 0, hairMeshes?.Length - 1 ?? 0);
                currentArmorIndex = Mathf.Clamp(data.armorIndex, 0, armorMeshes?.Length - 1 ?? 0);
                currentHairColor = data.hairColor.ToColor();
                currentSkinColor = data.skinColor.ToColor();
                currentArmorColor = data.armorColor.ToColor();

                ApplyAllCustomizations();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load character customization: {e.Message}");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Applies the current hair mesh to the hair renderer.
        /// </summary>
        private void ApplyHairMesh()
        {
            if (hairRenderer != null && hairMeshes != null && currentHairIndex < hairMeshes.Length)
            {
                hairRenderer.sharedMesh = hairMeshes[currentHairIndex];
            }
        }
        
        /// <summary>
        /// Applies the current armor mesh to the armor renderer.
        /// </summary>
        private void ApplyArmorMesh()
        {
            if (armorRenderer != null && armorMeshes != null && currentArmorIndex < armorMeshes.Length)
            {
                armorRenderer.sharedMesh = armorMeshes[currentArmorIndex];
            }
        }
        
        /// <summary>
        /// Applies all current customizations to the character.
        /// </summary>
        private void ApplyAllCustomizations()
        {
            ApplyHairMesh();
            ApplyArmorMesh();
            
            if (hairMaterial != null) hairMaterial.color = currentHairColor;
            if (skinMaterial != null) skinMaterial.color = currentSkinColor;
            if (armorMaterial != null) armorMaterial.color = currentArmorColor;
        }
        
        #endregion
    }

    #region Data Structures
    
    /// <summary>
    /// Serializable data structure for storing character customization settings.
    /// </summary>
    [Serializable]
    public class CharacterCustomizationData
    {
        public int hairIndex;
        public int armorIndex;
        public SerializableColor hairColor;
        public SerializableColor skinColor;
        public SerializableColor armorColor;
    }

    /// <summary>
    /// Serializable wrapper for Unity's Color struct to enable JSON serialization.
    /// </summary>
    [Serializable]
    public struct SerializableColor
    {
        #region Fields
        
        public float r, g, b, a;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Creates a SerializableColor from a Unity Color.
        /// </summary>
        /// <param name="color">The Unity Color to convert</param>
        public SerializableColor(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Converts this SerializableColor back to a Unity Color.
        /// </summary>
        /// <returns>A Unity Color with the same RGBA values</returns>
        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
        
        #endregion
    }
    
    #endregion
}
