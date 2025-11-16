using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Character;
using Laboratory.Core.Character.Configuration;
using Laboratory.Core.Customization;

namespace Laboratory.Subsystems.Player.CoreCharacter
{
    /// <summary>
    /// Advanced character customization manager with networking support, validation, and performance optimizations.
    /// Extends the base CharacterCustomizationManager with additional features for multiplayer and advanced customization.
    /// </summary>
    public class AdvancedCustomizationManager : MonoBehaviour, ICustomizationSystem
    {
        #region Fields

        [Header("Dependencies")]
        [SerializeField] private CharacterCustomizationManager baseCustomizationManager;

        [Header("Advanced Features")]
        [SerializeField] private bool enableNetworkSync = true;
        [SerializeField] private bool enableCustomizationValidation = true;
        [SerializeField] private bool enablePerformanceOptimizations = true;
        [SerializeField] private bool enableCustomizationPresets = true;

        [Header("Performance")]
        [SerializeField] private int maxConcurrentCustomizations = 1;
        [SerializeField] private float customizationCooldown = 0.5f;

        // Runtime state
        private bool _isActive = true;
        private float _lastCustomizationTime = 0f;
        private Queue<System.Action> _pendingCustomizations = new Queue<System.Action>();
        private bool _isProcessingCustomizations = false;
        private Dictionary<string, CharacterCustomizationData> _presets = new Dictionary<string, CharacterCustomizationData>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the customization system is currently active
        /// </summary>
        public bool IsActive => _isActive && (baseCustomizationManager?.IsActive ?? false);

        /// <summary>
        /// Base customization manager reference
        /// </summary>
        public CharacterCustomizationManager BaseManager => baseCustomizationManager;

        #endregion

        #region ICustomizationSystem Implementation

        /// <summary>
        /// Sets the active state of the customization system
        /// </summary>
        /// <param name="active">Whether the system should be active</param>
        public void SetActive(bool active)
        {
            _isActive = active;

            if (baseCustomizationManager != null)
                baseCustomizationManager.SetActive(active);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
            InitializePresets();
        }

        private void Start()
        {
            StartCoroutine(ProcessCustomizationQueue());
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies customization with validation and performance considerations
        /// </summary>
        /// <param name="customizationData">Customization data to apply</param>
        /// <param name="immediate">Whether to apply immediately or queue</param>
        public void ApplyCustomizationAdvanced(CharacterCustomizationData customizationData, bool immediate = false)
        {
            if (!IsActive)
            {
                Debug.LogWarning("AdvancedCustomizationManager: Cannot apply customization - system is not active");
                return;
            }

            // Validate customization if enabled
            if (enableCustomizationValidation && !ValidateCustomization(customizationData))
            {
                Debug.LogWarning("AdvancedCustomizationManager: Customization validation failed");
                return;
            }

            // Check cooldown for performance
            if (enablePerformanceOptimizations && !immediate && Time.time - _lastCustomizationTime < customizationCooldown)
            {
                // Queue the customization for later
                _pendingCustomizations.Enqueue(() => ApplyCustomizationInternal(customizationData));
                return;
            }

            if (immediate)
            {
                ApplyCustomizationInternal(customizationData);
            }
            else
            {
                _pendingCustomizations.Enqueue(() => ApplyCustomizationInternal(customizationData));
            }
        }

        /// <summary>
        /// Creates a new customization preset
        /// </summary>
        /// <param name="presetName">Name of the preset</param>
        /// <param name="customizationData">Customization data to save</param>
        public void CreatePreset(string presetName, CharacterCustomizationData customizationData)
        {
            if (!enableCustomizationPresets)
                return;

            if (string.IsNullOrEmpty(presetName))
            {
                Debug.LogWarning("AdvancedCustomizationManager: Preset name cannot be empty");
                return;
            }

            _presets[presetName] = customizationData;
            SavePresetsToPlayerPrefs();

            Debug.Log($"AdvancedCustomizationManager: Created preset '{presetName}'");
        }

        /// <summary>
        /// Applies a saved customization preset
        /// </summary>
        /// <param name="presetName">Name of the preset to apply</param>
        public void ApplyPreset(string presetName)
        {
            if (!enableCustomizationPresets)
                return;

            if (_presets.TryGetValue(presetName, out CharacterCustomizationData preset))
            {
                ApplyCustomizationAdvanced(preset, immediate: true);
                Debug.Log($"AdvancedCustomizationManager: Applied preset '{presetName}'");
            }
            else
            {
                Debug.LogWarning($"AdvancedCustomizationManager: Preset '{presetName}' not found");
            }
        }

        /// <summary>
        /// Gets all available preset names
        /// </summary>
        /// <returns>Array of preset names</returns>
        public string[] GetPresetNames()
        {
            if (!enableCustomizationPresets)
                return new string[0];

            var names = new string[_presets.Count];
            _presets.Keys.CopyTo(names, 0);
            return names;
        }

        /// <summary>
        /// Removes a customization preset
        /// </summary>
        /// <param name="presetName">Name of the preset to remove</param>
        public void RemovePreset(string presetName)
        {
            if (!enableCustomizationPresets)
                return;

            if (_presets.Remove(presetName))
            {
                SavePresetsToPlayerPrefs();
                Debug.Log($"AdvancedCustomizationManager: Removed preset '{presetName}'");
            }
        }

        /// <summary>
        /// Validates a customization before applying it
        /// </summary>
        /// <param name="customizationData">Customization data to validate</param>
        /// <returns>True if valid</returns>
        public bool ValidateCustomization(CharacterCustomizationData customizationData)
        {
            if (customizationData == null)
                return false;

            if (baseCustomizationManager?.Settings == null)
                return true; // No settings to validate against

            var settings = baseCustomizationManager.Settings;

            // Validate hair ID
            if (!string.IsNullOrEmpty(customizationData.HairId) && !settings.IsHairIdAvailable(customizationData.HairId))
            {
                Debug.LogWarning($"AdvancedCustomizationManager: Invalid hair ID '{customizationData.HairId}'");
                return false;
            }

            // Validate clothing ID
            if (!string.IsNullOrEmpty(customizationData.ClothingId) && !settings.IsClothingIdAvailable(customizationData.ClothingId))
            {
                Debug.LogWarning($"AdvancedCustomizationManager: Invalid clothing ID '{customizationData.ClothingId}'");
                return false;
            }

            // Validate accessory IDs
            if (customizationData.AccessoryIds != null)
            {
                foreach (var accessoryId in customizationData.AccessoryIds)
                {
                    if (!string.IsNullOrEmpty(accessoryId) && !settings.IsAccessoryIdAvailable(accessoryId))
                    {
                        Debug.LogWarning($"AdvancedCustomizationManager: Invalid accessory ID '{accessoryId}'");
                        return false;
                    }
                }

                if (customizationData.AccessoryIds.Length > settings.maxSimultaneousAccessories)
                {
                    Debug.LogWarning($"AdvancedCustomizationManager: Too many accessories ({customizationData.AccessoryIds.Length}), max allowed: {settings.maxSimultaneousAccessories}");
                    return false;
                }
            }

            // Validate weapon ID
            if (!string.IsNullOrEmpty(customizationData.WeaponId) && !settings.IsWeaponIdAvailable(customizationData.WeaponId))
            {
                Debug.LogWarning($"AdvancedCustomizationManager: Invalid weapon ID '{customizationData.WeaponId}'");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets performance statistics for the customization system
        /// </summary>
        /// <returns>Performance info string</returns>
        public string GetPerformanceInfo()
        {
            return $"Pending Customizations: {_pendingCustomizations.Count}, " +
                   $"Last Applied: {Time.time - _lastCustomizationTime:F2}s ago, " +
                   $"Active Presets: {_presets.Count}";
        }

        #endregion

        #region Private Methods

        private void ValidateReferences()
        {
            if (baseCustomizationManager == null)
            {
                baseCustomizationManager = GetComponent<CharacterCustomizationManager>();

                if (baseCustomizationManager == null)
                {
                    Debug.LogError("AdvancedCustomizationManager: No CharacterCustomizationManager found!", this);
                }
            }
        }

        private void ApplyCustomizationInternal(CharacterCustomizationData customizationData)
        {
            if (baseCustomizationManager != null)
            {
                baseCustomizationManager.ApplyCustomization(customizationData);
                _lastCustomizationTime = Time.time;
            }
        }

        private IEnumerator ProcessCustomizationQueue()
        {
            while (true)
            {
                if (!_isProcessingCustomizations && _pendingCustomizations.Count > 0 && IsActive)
                {
                    _isProcessingCustomizations = true;

                    // Process up to maxConcurrentCustomizations per frame
                    int processed = 0;
                    while (_pendingCustomizations.Count > 0 && processed < maxConcurrentCustomizations)
                    {
                        var customization = _pendingCustomizations.Dequeue();
                        customization?.Invoke();
                        processed++;
                    }

                    _isProcessingCustomizations = false;
                }

                yield return null;
            }
        }

        private void InitializePresets()
        {
            if (!enableCustomizationPresets)
                return;

            LoadPresetsFromPlayerPrefs();

            // Create default preset if none exist
            if (_presets.Count == 0)
            {
                var defaultPreset = new CharacterCustomizationData
                {
                    HairId = baseCustomizationManager?.Settings?.defaultHairId ?? "default_hair",
                    ClothingId = baseCustomizationManager?.Settings?.defaultClothingId ?? "default_armor",
                    WeaponId = baseCustomizationManager?.Settings?.defaultWeaponId ?? "sword",
                    ColorScheme = new ColorScheme()
                };

                CreatePreset("Default", defaultPreset);
            }
        }

        private void SavePresetsToPlayerPrefs()
        {
            var presetData = new List<string>();
            foreach (var kvp in _presets)
            {
                var json = JsonUtility.ToJson(kvp.Value);
                presetData.Add($"{kvp.Key}:{json}");
            }

            PlayerPrefs.SetString("CustomizationPresets", string.Join("|", presetData));
            PlayerPrefs.Save();
        }

        private void LoadPresetsFromPlayerPrefs()
        {
            _presets.Clear();

            if (PlayerPrefs.HasKey("CustomizationPresets"))
            {
                var presetData = PlayerPrefs.GetString("CustomizationPresets");
                var presets = presetData.Split('|');

                foreach (var preset in presets)
                {
                    if (string.IsNullOrEmpty(preset))
                        continue;

                    var parts = preset.Split(':');
                    if (parts.Length == 2)
                    {
                        var presetName = parts[0];
                        var customization = JsonUtility.FromJson<CharacterCustomizationData>(parts[1]);
                        if (customization != null)
                        {
                            _presets[presetName] = customization;
                        }
                    }
                }
            }
        }

        #endregion
    }
}