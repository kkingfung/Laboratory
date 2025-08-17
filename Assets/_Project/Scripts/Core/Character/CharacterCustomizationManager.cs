using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Manages character customization including appearance, equipment, and visual modifications.
    /// Handles dynamic loading of customization assets and maintains character state.
    /// </summary>
    public class CharacterCustomizationManager : MonoBehaviour
    {
        #region Fields

        [Header("Character References")]
        [SerializeField] private SkinnedMeshRenderer _characterMeshRenderer;
        [SerializeField] private Transform _characterRoot;
        [SerializeField] private Animator _characterAnimator;

        [Header("Customization Categories")]
        [SerializeField] private Transform _hairParent;
        [SerializeField] private Transform _clothingParent;
        [SerializeField] private Transform _accessoriesParent;
        [SerializeField] private Transform _weaponsParent;

        [Header("Default Assets")]
        [SerializeField] private GameObject _defaultHair;
        [SerializeField] private GameObject _defaultClothing;
        [SerializeField] private GameObject _defaultAccessories;

        // Runtime state
        private Dictionary<string, GameObject> _activeCustomizations = new Dictionary<string, GameObject>();
        private CharacterCustomizationData _currentCustomization;
        private bool _isInitialized = false;

        #endregion

        #region Properties

        /// <summary>
        /// Current character customization data
        /// </summary>
        public CharacterCustomizationData CurrentCustomization => _currentCustomization;

        /// <summary>
        /// Whether the customization system has been initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Character's skinned mesh renderer for material modifications
        /// </summary>
        public SkinnedMeshRenderer CharacterMeshRenderer => _characterMeshRenderer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeCustomizationSystem();
        }

        private void Start()
        {
            ApplyDefaultCustomization();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies a complete customization set to the character
        /// </summary>
        /// <param name="customizationData">Customization data to apply</param>
        public void ApplyCustomization(CharacterCustomizationData customizationData)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("Customization system not initialized");
                return;
            }

            _currentCustomization = customizationData;
            ApplyHairCustomization(customizationData.HairId);
            ApplyClothingCustomization(customizationData.ClothingId);
            ApplyAccessoryCustomization(customizationData.AccessoryIds);
            ApplyWeaponCustomization(customizationData.WeaponId);
            ApplyColorCustomization(customizationData.ColorScheme);
        }

        /// <summary>
        /// Applies only hair customization
        /// </summary>
        /// <param name="hairId">Hair asset identifier</param>
        public void ApplyHairCustomization(string hairId)
        {
            if (string.IsNullOrEmpty(hairId)) return;

            RemoveCustomization("hair");
            var hairPrefab = LoadCustomizationAsset(hairId, "Hair");
            if (hairPrefab != null)
            {
                var hairInstance = Instantiate(hairPrefab, _hairParent);
                _activeCustomizations["hair"] = hairInstance;
            }
        }

        /// <summary>
        /// Applies only clothing customization
        /// </summary>
        /// <param name="clothingId">Clothing asset identifier</param>
        public void ApplyClothingCustomization(string clothingId)
        {
            if (string.IsNullOrEmpty(clothingId)) return;

            RemoveCustomization("clothing");
            var clothingPrefab = LoadCustomizationAsset(clothingId, "Clothing");
            if (clothingPrefab != null)
            {
                var clothingInstance = Instantiate(clothingPrefab, _clothingParent);
                _activeCustomizations["clothing"] = clothingInstance;
            }
        }

        /// <summary>
        /// Applies accessory customizations
        /// </summary>
        /// <param name="accessoryIds">Array of accessory identifiers</param>
        public void ApplyAccessoryCustomization(string[] accessoryIds)
        {
            if (accessoryIds == null || accessoryIds.Length == 0) return;

            RemoveCustomization("accessories");
            foreach (var accessoryId in accessoryIds)
            {
                var accessoryPrefab = LoadCustomizationAsset(accessoryId, "Accessories");
                if (accessoryPrefab != null)
                {
                    var accessoryInstance = Instantiate(accessoryPrefab, _accessoriesParent);
                    if (!_activeCustomizations.ContainsKey("accessories"))
                        _activeCustomizations["accessories"] = accessoryInstance;
                }
            }
        }

        /// <summary>
        /// Applies weapon customization
        /// </summary>
        /// <param name="weaponId">Weapon asset identifier</param>
        public void ApplyWeaponCustomization(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return;

            RemoveCustomization("weapon");
            var weaponPrefab = LoadCustomizationAsset(weaponId, "Weapons");
            if (weaponPrefab != null)
            {
                var weaponInstance = Instantiate(weaponPrefab, _weaponsParent);
                _activeCustomizations["weapon"] = weaponInstance;
            }
        }

        /// <summary>
        /// Applies color scheme to character materials
        /// </summary>
        /// <param name="colorScheme">Color scheme data</param>
        public void ApplyColorCustomization(ColorScheme colorScheme)
        {
            if (colorScheme == null || _characterMeshRenderer == null) return;

            var materials = _characterMeshRenderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (colorScheme.TryGetColorForMaterial(i, out Color color))
                {
                    materials[i].color = color;
                }
            }
            _characterMeshRenderer.materials = materials;
        }

        /// <summary>
        /// Removes a specific customization category
        /// </summary>
        /// <param name="category">Customization category to remove</param>
        public void RemoveCustomization(string category)
        {
            if (_activeCustomizations.TryGetValue(category, out GameObject customization))
            {
                DestroyImmediate(customization);
                _activeCustomizations.Remove(category);
            }
        }

        /// <summary>
        /// Removes all customizations and returns to default state
        /// </summary>
        public void RemoveAllCustomizations()
        {
            foreach (var customization in _activeCustomizations.Values)
            {
                if (customization != null)
                    DestroyImmediate(customization);
            }
            _activeCustomizations.Clear();
            ApplyDefaultCustomization();
        }

        /// <summary>
        /// Saves current customization to persistent storage
        /// </summary>
        public void SaveCustomization()
        {
            if (_currentCustomization != null)
            {
                string json = JsonUtility.ToJson(_currentCustomization);
                PlayerPrefs.SetString("CharacterCustomization", json);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Loads customization from persistent storage
        /// </summary>
        public void LoadCustomization()
        {
            if (PlayerPrefs.HasKey("CharacterCustomization"))
            {
                string json = PlayerPrefs.GetString("CharacterCustomization");
                var customization = JsonUtility.FromJson<CharacterCustomizationData>(json);
                if (customization != null)
                {
                    ApplyCustomization(customization);
                }
            }
        }

        #endregion

        #region Private Methods

        private void InitializeCustomizationSystem()
        {
            ValidateReferences();
            _isInitialized = true;
        }

        private void ValidateReferences()
        {
            if (_characterMeshRenderer == null)
                _characterMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

            if (_characterRoot == null)
                _characterRoot = transform;

            if (_characterAnimator == null)
                _characterAnimator = GetComponent<Animator>();
        }

        private void ApplyDefaultCustomization()
        {
            if (_defaultHair != null)
            {
                var hairInstance = Instantiate(_defaultHair, _hairParent);
                _activeCustomizations["hair"] = hairInstance;
            }

            if (_defaultClothing != null)
            {
                var clothingInstance = Instantiate(_defaultClothing, _clothingParent);
                _activeCustomizations["clothing"] = clothingInstance;
            }

            if (_defaultAccessories != null)
            {
                var accessoriesInstance = Instantiate(_defaultAccessories, _accessoriesParent);
                _activeCustomizations["accessories"] = accessoriesInstance;
            }
        }

        private GameObject LoadCustomizationAsset(string assetId, string category)
        {
            // This would typically load from Resources, Addressables, or asset bundles
            // For now, return null to indicate asset not found
            Debug.Log($"Loading {category} asset: {assetId}");
            return null;
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Data structure for character customization
    /// </summary>
    [System.Serializable]
    public class CharacterCustomizationData
    {
        public string HairId;
        public string ClothingId;
        public string[] AccessoryIds;
        public string WeaponId;
        public ColorScheme ColorScheme;
    }

    /// <summary>
    /// Color scheme for character materials
    /// </summary>
    [System.Serializable]
    public class ColorScheme
    {
        public Color[] MaterialColors = new Color[4];

        public bool TryGetColorForMaterial(int materialIndex, out Color color)
        {
            if (materialIndex >= 0 && materialIndex < MaterialColors.Length)
            {
                color = MaterialColors[materialIndex];
                return true;
            }
            color = Color.white;
            return false;
        }
    }

    #endregion
}
