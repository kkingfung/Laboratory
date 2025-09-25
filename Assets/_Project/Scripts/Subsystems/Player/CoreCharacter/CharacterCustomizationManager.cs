using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Customization;
using Laboratory.Core.Character.Configuration;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Manages character customization including appearance, equipment, and visual modifications.
    /// Handles dynamic loading of customization assets and maintains character state.
    /// Implements ICustomizationSystem for standardized interface.
    /// </summary>
    public class CharacterCustomizationManager : MonoBehaviour, ICustomizationSystem
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

        [Header("Configuration")]
        [SerializeField] private CustomizationSettings _customizationSettings;

        // Runtime state
        private Dictionary<string, GameObject> _activeCustomizations = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> _assetCache = new Dictionary<string, GameObject>();
        private CharacterCustomizationData _currentCustomization;
        private bool _isInitialized = false;
        private bool _isActive = true;
        
        // Navigation indices for cycling through options
        private int _currentHairIndex = 0;
        private int _currentArmorIndex = 0;

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

        /// <summary>
        /// Gets whether the customization system is currently active
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// Current customization settings configuration
        /// </summary>
        public CustomizationSettings Settings => _customizationSettings;

        #endregion

        #region ICustomizationSystem Implementation

        /// <summary>
        /// Sets the active state of the customization system
        /// </summary>
        /// <param name="active">Whether the system should be active</param>
        public void SetActive(bool active)
        {
            _isActive = active;

            if (_customizationSettings != null && _customizationSettings.logChanges)
            {
                Debug.Log($"CharacterCustomizationManager: Active state changed to {active}");
            }
        }

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
        
        /// <summary>
        /// Cycles to the next hair style
        /// </summary>
        public void NextHair()
        {
            if (_customizationSettings == null || _customizationSettings.availableHairIds.Length == 0) return;

            _currentHairIndex = (_currentHairIndex + 1) % _customizationSettings.availableHairIds.Length;
            ApplyHairCustomization(_customizationSettings.availableHairIds[_currentHairIndex]);

            // Update current customization data
            if (_currentCustomization == null)
                _currentCustomization = new CharacterCustomizationData();
            _currentCustomization.HairId = _customizationSettings.availableHairIds[_currentHairIndex];
        }
        
        /// <summary>
        /// Cycles to the previous hair style
        /// </summary>
        public void PreviousHair()
        {
            if (_customizationSettings == null || _customizationSettings.availableHairIds.Length == 0) return;

            _currentHairIndex = (_currentHairIndex - 1 + _customizationSettings.availableHairIds.Length) % _customizationSettings.availableHairIds.Length;
            ApplyHairCustomization(_customizationSettings.availableHairIds[_currentHairIndex]);

            // Update current customization data
            if (_currentCustomization == null)
                _currentCustomization = new CharacterCustomizationData();
            _currentCustomization.HairId = _customizationSettings.availableHairIds[_currentHairIndex];
        }
        
        /// <summary>
        /// Cycles to the next armor set
        /// </summary>
        public void NextArmor()
        {
            if (_customizationSettings == null || _customizationSettings.availableClothingIds.Length == 0) return;

            _currentArmorIndex = (_currentArmorIndex + 1) % _customizationSettings.availableClothingIds.Length;
            ApplyClothingCustomization(_customizationSettings.availableClothingIds[_currentArmorIndex]);

            // Update current customization data
            if (_currentCustomization == null)
                _currentCustomization = new CharacterCustomizationData();
            _currentCustomization.ClothingId = _customizationSettings.availableClothingIds[_currentArmorIndex];
        }
        
        /// <summary>
        /// Cycles to the previous armor set
        /// </summary>
        public void PreviousArmor()
        {
            if (_customizationSettings == null || _customizationSettings.availableClothingIds.Length == 0) return;

            _currentArmorIndex = (_currentArmorIndex - 1 + _customizationSettings.availableClothingIds.Length) % _customizationSettings.availableClothingIds.Length;
            ApplyClothingCustomization(_customizationSettings.availableClothingIds[_currentArmorIndex]);

            // Update current customization data
            if (_currentCustomization == null)
                _currentCustomization = new CharacterCustomizationData();
            _currentCustomization.ClothingId = _customizationSettings.availableClothingIds[_currentArmorIndex];
        }
        
        /// <summary>
        /// Sets the hair color
        /// </summary>
        /// <param name="color">New hair color</param>
        public void SetHairColor(Color color)
        {
            if (_activeCustomizations.TryGetValue("hair", out GameObject hairObject) && hairObject != null)
            {
                var renderers = hairObject.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.material != null)
                    {
                        renderer.material.color = color;
                    }
                }
            }
            
            // Update current customization data
            if (_currentCustomization == null)
                _currentCustomization = new CharacterCustomizationData();
            if (_currentCustomization.ColorScheme == null)
                _currentCustomization.ColorScheme = new ColorScheme();
            
            // Assuming hair uses material index 0 for hair color
            _currentCustomization.ColorScheme.MaterialColors[0] = color;
        }
        
        /// <summary>
        /// Sets the skin color
        /// </summary>
        /// <param name="color">New skin color</param>
        public void SetSkinColor(Color color)
        {
            if (_characterMeshRenderer != null && _characterMeshRenderer.materials.Length > 1)
            {
                var materials = _characterMeshRenderer.materials;
                // Assuming skin uses material index 1
                materials[1].color = color;
                _characterMeshRenderer.materials = materials;
            }
            
            // Update current customization data
            if (_currentCustomization == null)
                _currentCustomization = new CharacterCustomizationData();
            if (_currentCustomization.ColorScheme == null)
                _currentCustomization.ColorScheme = new ColorScheme();
            
            // Assuming skin uses material index 1
            if (_currentCustomization.ColorScheme.MaterialColors.Length > 1)
                _currentCustomization.ColorScheme.MaterialColors[1] = color;
        }
        
        /// <summary>
        /// Sets the armor color
        /// </summary>
        /// <param name="color">New armor color</param>
        public void SetArmorColor(Color color)
        {
            if (_activeCustomizations.TryGetValue("clothing", out GameObject armorObject) && armorObject != null)
            {
                var renderers = armorObject.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.material != null)
                    {
                        renderer.material.color = color;
                    }
                }
            }
            
            // Update current customization data
            if (_currentCustomization == null)
                _currentCustomization = new CharacterCustomizationData();
            if (_currentCustomization.ColorScheme == null)
                _currentCustomization.ColorScheme = new ColorScheme();
            
            // Assuming armor uses material index 2
            if (_currentCustomization.ColorScheme.MaterialColors.Length > 2)
                _currentCustomization.ColorScheme.MaterialColors[2] = color;
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
            if (!_isActive)
                return null;

            // Check cache first if caching is enabled
            if (_customizationSettings != null && _customizationSettings.cacheAssets)
            {
                string cacheKey = $"{category}_{assetId}";
                if (_assetCache.TryGetValue(cacheKey, out GameObject cachedAsset))
                {
                    return cachedAsset;
                }
            }

            // Get asset path from settings
            string assetPath = _customizationSettings?.GetAssetPath(category, assetId) ?? $"Character/Customization/{category}/{assetId}";

            // Try to load from Resources
            GameObject loadedAsset = Resources.Load<GameObject>(assetPath);

            // Log if configured
            if (_customizationSettings != null && _customizationSettings.logChanges)
            {
                Debug.Log($"Loading {category} asset: {assetId} from path: {assetPath} - {(loadedAsset != null ? "Success" : "Failed")}");
            }

            // Cache the asset if successful and caching is enabled
            if (loadedAsset != null && _customizationSettings != null && _customizationSettings.cacheAssets)
            {
                string cacheKey = $"{category}_{assetId}";

                // Check cache size limit
                if (_assetCache.Count < _customizationSettings.maxCachedAssets)
                {
                    _assetCache[cacheKey] = loadedAsset;
                }
                else if (_customizationSettings.logChanges)
                {
                    Debug.LogWarning($"Asset cache full ({_customizationSettings.maxCachedAssets}), not caching {cacheKey}");
                }
            }

            return loadedAsset;
        }

        /// <summary>
        /// Clears the asset cache to free memory
        /// </summary>
        public void ClearAssetCache()
        {
            _assetCache.Clear();

            if (_customizationSettings != null && _customizationSettings.logChanges)
            {
                Debug.Log("Character customization asset cache cleared");
            }
        }

        /// <summary>
        /// Preloads commonly used customization assets
        /// </summary>
        public void PreloadAssets()
        {
            if (_customizationSettings == null || !_customizationSettings.cacheAssets)
                return;

            // Preload default assets
            if (!string.IsNullOrEmpty(_customizationSettings.defaultHairId))
                LoadCustomizationAsset(_customizationSettings.defaultHairId, "Hair");

            if (!string.IsNullOrEmpty(_customizationSettings.defaultClothingId))
                LoadCustomizationAsset(_customizationSettings.defaultClothingId, "Clothing");

            if (!string.IsNullOrEmpty(_customizationSettings.defaultWeaponId))
                LoadCustomizationAsset(_customizationSettings.defaultWeaponId, "Weapons");

            if (_customizationSettings.logChanges)
            {
                Debug.Log($"Preloaded {_assetCache.Count} customization assets");
            }
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
