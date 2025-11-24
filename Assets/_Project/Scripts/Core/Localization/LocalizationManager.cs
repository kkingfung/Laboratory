using UnityEngine;
using System;
using System.Collections.Generic;

namespace Laboratory.Core.Localization
{
    /// <summary>
    /// ScriptableObject-based localization manager for Project Chimera
    /// Provides runtime language switching and text translation
    /// Compatible with Unity Localization package or standalone
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationManager", menuName = "Chimera/Localization/Localization Manager")]
    public class LocalizationManager : ScriptableObject
    {
        [Header("Configuration")]
        [SerializeField] private SystemLanguage defaultLanguage = SystemLanguage.English;
        [SerializeField] private List<LocalizationDatabase> languageDatabases = new List<LocalizationDatabase>();

        [Header("Settings")]
        [SerializeField] private bool useSystemLanguage = true;
        [SerializeField] private bool logMissingKeys = true;
        [SerializeField] private string missingKeyPrefix = "[MISSING: ";
        [SerializeField] private string missingKeySuffix = "]";

        // Runtime state
        private SystemLanguage _currentLanguage;
        private LocalizationDatabase _currentDatabase;
        private Dictionary<SystemLanguage, LocalizationDatabase> _databaseLookup;
        private bool _isInitialized = false;

        // Events
        public event Action<SystemLanguage> OnLanguageChanged;

        /// <summary>
        /// Singleton instance (loaded from Resources)
        /// </summary>
        private static LocalizationManager _instance;
        public static LocalizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<LocalizationManager>("Localization/LocalizationManager");
                    if (_instance == null)
                    {
                        Debug.LogWarning("LocalizationManager not found in Resources/Localization/. Creating temporary instance.");
                        _instance = CreateInstance<LocalizationManager>();
                    }
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Current selected language
        /// </summary>
        public SystemLanguage CurrentLanguage => _currentLanguage;

        /// <summary>
        /// Available languages in the system
        /// </summary>
        public List<SystemLanguage> AvailableLanguages
        {
            get
            {
                var languages = new List<SystemLanguage>();
                foreach (var db in languageDatabases)
                {
                    if (db != null)
                        languages.Add(db.Language);
                }
                return languages;
            }
        }

        /// <summary>
        /// Initializes the localization system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Build database lookup
            _databaseLookup = new Dictionary<SystemLanguage, LocalizationDatabase>();
            foreach (var db in languageDatabases)
            {
                if (db != null && !_databaseLookup.ContainsKey(db.Language))
                {
                    _databaseLookup[db.Language] = db;
                }
            }

            // Determine initial language
            SystemLanguage initialLanguage = defaultLanguage;
            if (useSystemLanguage && _databaseLookup.ContainsKey(Application.systemLanguage))
            {
                initialLanguage = Application.systemLanguage;
            }

            // Load saved language preference
            string savedLanguageCode = PlayerPrefs.GetString("language_code", "");
            if (!string.IsNullOrEmpty(savedLanguageCode))
            {
                SystemLanguage savedLanguage = ParseLanguageCode(savedLanguageCode);
                if (_databaseLookup.ContainsKey(savedLanguage))
                {
                    initialLanguage = savedLanguage;
                }
            }

            // Set initial language
            SetLanguage(initialLanguage);

            _isInitialized = true;
            Debug.Log($"[Localization] Initialized with {languageDatabases.Count} languages. Current: {_currentLanguage}");
        }

        /// <summary>
        /// Changes the current language
        /// </summary>
        public bool SetLanguage(SystemLanguage language)
        {
            if (!_databaseLookup.TryGetValue(language, out var database))
            {
                Debug.LogWarning($"[Localization] Language '{language}' not found. Available: {string.Join(", ", AvailableLanguages)}");
                return false;
            }

            _currentLanguage = language;
            _currentDatabase = database;

            // Save preference
            PlayerPrefs.SetString("language_code", GetLanguageCode(language));
            PlayerPrefs.Save();

            // Notify listeners
            OnLanguageChanged?.Invoke(language);

            Debug.Log($"[Localization] Language changed to: {language}");
            return true;
        }

        /// <summary>
        /// Gets a localized string by key
        /// </summary>
        public string GetText(string key, params object[] args)
        {
            if (_currentDatabase == null)
            {
                if (logMissingKeys)
                    Debug.LogWarning($"[Localization] No database loaded for language: {_currentLanguage}");
                return $"{missingKeyPrefix}{key}{missingKeySuffix}";
            }

            string text = _currentDatabase.GetText(key);

            if (string.IsNullOrEmpty(text))
            {
                if (logMissingKeys)
                    Debug.LogWarning($"[Localization] Missing key '{key}' in language '{_currentLanguage}'");
                return $"{missingKeyPrefix}{key}{missingKeySuffix}";
            }

            // Format with arguments if provided
            if (args != null && args.Length > 0)
            {
                try
                {
                    text = string.Format(text, args);
                }
                catch (FormatException e)
                {
                    Debug.LogError($"[Localization] Format error for key '{key}': {e.Message}");
                }
            }

            return text;
        }

        /// <summary>
        /// Checks if a key exists in the current language
        /// </summary>
        public bool HasKey(string key)
        {
            return _currentDatabase != null && _currentDatabase.HasKey(key);
        }

        /// <summary>
        /// Gets all keys in the current language database
        /// </summary>
        public List<string> GetAllKeys()
        {
            return _currentDatabase != null ? _currentDatabase.GetAllKeys() : new List<string>();
        }

        /// <summary>
        /// Converts language code to SystemLanguage enum
        /// </summary>
        private SystemLanguage ParseLanguageCode(string code)
        {
            return code.ToLower() switch
            {
                "en" => SystemLanguage.English,
                "es" => SystemLanguage.Spanish,
                "fr" => SystemLanguage.French,
                "de" => SystemLanguage.German,
                "ja" or "jp" => SystemLanguage.Japanese,
                "zh-cn" or "zh" => SystemLanguage.ChineseSimplified,
                "pt" => SystemLanguage.Portuguese,
                "ru" => SystemLanguage.Russian,
                "ko" => SystemLanguage.Korean,
                "it" => SystemLanguage.Italian,
                _ => defaultLanguage
            };
        }

        /// <summary>
        /// Converts SystemLanguage enum to language code
        /// </summary>
        private string GetLanguageCode(SystemLanguage language)
        {
            return language switch
            {
                SystemLanguage.English => "en",
                SystemLanguage.Spanish => "es",
                SystemLanguage.French => "fr",
                SystemLanguage.German => "de",
                SystemLanguage.Japanese => "ja",
                SystemLanguage.ChineseSimplified => "zh-cn",
                SystemLanguage.Portuguese => "pt",
                SystemLanguage.Russian => "ru",
                SystemLanguage.Korean => "ko",
                SystemLanguage.Italian => "it",
                _ => "en"
            };
        }

        /// <summary>
        /// Editor utility: Validates all databases
        /// </summary>
        [ContextMenu("Validate Databases")]
        private void ValidateDatabases()
        {
            Debug.Log($"[Localization] Validating {languageDatabases.Count} databases...");

            foreach (var db in languageDatabases)
            {
                if (db == null)
                {
                    Debug.LogError("[Localization] Found null database reference!");
                    continue;
                }

                Debug.Log($"[Localization] {db.Language}: {db.GetAllKeys().Count} keys");
            }
        }
    }
}
