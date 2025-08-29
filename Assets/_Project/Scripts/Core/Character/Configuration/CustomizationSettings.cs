using UnityEngine;

namespace Laboratory.Core.Character.Configuration
{
    /// <summary>
    /// Configuration settings for character customization system.
    /// Defines available customization options and behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "CustomizationSettings", menuName = "Laboratory/Character/Customization Settings")]
    public class CustomizationSettings : ScriptableObject
    {
        [Header("Asset Loading")]
        [SerializeField, Tooltip("Base path for customization assets")]
        public string assetBasePath = "Character/Customization";

        [SerializeField, Tooltip("Use Addressables for asset loading")]
        public bool useAddressables = true;

        [SerializeField, Tooltip("Cache loaded customization assets")]
        public bool cacheAssets = true;

        [SerializeField, Tooltip("Maximum number of cached assets")]
        [Range(10, 200)]
        public int maxCachedAssets = 50;

        [Header("Hair Customization")]
        [SerializeField, Tooltip("Available hair style IDs")]
        public string[] availableHairIds = { "default_hair", "hair_style_1", "hair_style_2", "hair_style_3" };

        [SerializeField, Tooltip("Default hair style ID")]
        public string defaultHairId = "default_hair";

        [Header("Clothing Customization")]
        [SerializeField, Tooltip("Available clothing/armor set IDs")]
        public string[] availableClothingIds = { "default_armor", "armor_set_1", "armor_set_2", "armor_set_3" };

        [SerializeField, Tooltip("Default clothing ID")]
        public string defaultClothingId = "default_armor";

        [Header("Accessory Customization")]
        [SerializeField, Tooltip("Available accessory IDs")]
        public string[] availableAccessoryIds = { "glasses", "hat", "necklace", "earrings" };

        [SerializeField, Tooltip("Maximum number of accessories that can be worn simultaneously")]
        [Range(1, 10)]
        public int maxSimultaneousAccessories = 3;

        [Header("Weapon Customization")]
        [SerializeField, Tooltip("Available weapon IDs")]
        public string[] availableWeaponIds = { "sword", "staff", "bow", "dagger" };

        [SerializeField, Tooltip("Default weapon ID")]
        public string defaultWeaponId = "sword";

        [Header("Color Customization")]
        [SerializeField, Tooltip("Available hair colors")]
        public Color[] availableHairColors = { Color.black, Color.brown, Color.blonde, Color.red };

        [SerializeField, Tooltip("Available skin colors")]
        public Color[] availableSkinColors = { new Color(1f, 0.8f, 0.6f), new Color(0.8f, 0.6f, 0.4f), new Color(0.6f, 0.4f, 0.2f) };

        [SerializeField, Tooltip("Available armor colors")]
        public Color[] availableArmorColors = { Color.gray, Color.blue, Color.red, Color.green };

        [Header("Performance")]
        [SerializeField, Tooltip("Update customization in real-time")]
        public bool realTimeUpdates = true;

        [SerializeField, Tooltip("Maximum time per frame for customization updates (ms)")]
        [Range(1f, 16f)]
        public float maxUpdateTimeMs = 5f;

        [SerializeField, Tooltip("Use LOD system for customization details")]
        public bool useLODSystem = true;

        [Header("Validation")]
        [SerializeField, Tooltip("Validate asset existence before loading")]
        public bool validateAssets = true;

        [SerializeField, Tooltip("Log customization changes")]
        public bool logChanges = false;

        /// <summary>
        /// Creates default customization settings
        /// </summary>
        /// <returns>CustomizationSettings with default values</returns>
        public static CustomizationSettings CreateDefault()
        {
            var settings = CreateInstance<CustomizationSettings>();
            settings.name = "Default Customization Settings";
            return settings;
        }

        /// <summary>
        /// Checks if a hair ID is available
        /// </summary>
        /// <param name="hairId">Hair ID to check</param>
        /// <returns>True if hair ID is available</returns>
        public bool IsHairIdAvailable(string hairId)
        {
            return System.Array.IndexOf(availableHairIds, hairId) >= 0;
        }

        /// <summary>
        /// Checks if a clothing ID is available
        /// </summary>
        /// <param name="clothingId">Clothing ID to check</param>
        /// <returns>True if clothing ID is available</returns>
        public bool IsClothingIdAvailable(string clothingId)
        {
            return System.Array.IndexOf(availableClothingIds, clothingId) >= 0;
        }

        /// <summary>
        /// Checks if an accessory ID is available
        /// </summary>
        /// <param name="accessoryId">Accessory ID to check</param>
        /// <returns>True if accessory ID is available</returns>
        public bool IsAccessoryIdAvailable(string accessoryId)
        {
            return System.Array.IndexOf(availableAccessoryIds, accessoryId) >= 0;
        }

        /// <summary>
        /// Checks if a weapon ID is available
        /// </summary>
        /// <param name="weaponId">Weapon ID to check</param>
        /// <returns>True if weapon ID is available</returns>
        public bool IsWeaponIdAvailable(string weaponId)
        {
            return System.Array.IndexOf(availableWeaponIds, weaponId) >= 0;
        }

        /// <summary>
        /// Gets the full asset path for a customization item
        /// </summary>
        /// <param name="category">Customization category</param>
        /// <param name="itemId">Item ID</param>
        /// <returns>Full asset path</returns>
        public string GetAssetPath(string category, string itemId)
        {
            return $"{assetBasePath}/{category}/{itemId}";
        }

        /// <summary>
        /// Validates all settings and logs warnings for issues
        /// </summary>
        public void ValidateSettings()
        {
            if (string.IsNullOrEmpty(assetBasePath))
            {
                Debug.LogWarning($"[{name}] AssetBasePath should not be empty");
            }

            if (availableHairIds == null || availableHairIds.Length == 0)
            {
                Debug.LogWarning($"[{name}] AvailableHairIds should not be empty");
            }

            if (availableClothingIds == null || availableClothingIds.Length == 0)
            {
                Debug.LogWarning($"[{name}] AvailableClothingIds should not be empty");
            }

            if (maxSimultaneousAccessories <= 0)
            {
                Debug.LogWarning($"[{name}] MaxSimultaneousAccessories should be greater than 0");
            }

            if (maxUpdateTimeMs <= 0f)
            {
                Debug.LogWarning($"[{name}] MaxUpdateTimeMs should be greater than 0");
            }

            if (maxCachedAssets <= 0)
            {
                Debug.LogWarning($"[{name}] MaxCachedAssets should be greater than 0");
            }
        }

        private void OnValidate()
        {
            ValidateSettings();
        }
    }
}
