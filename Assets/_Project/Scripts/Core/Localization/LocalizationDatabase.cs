using UnityEngine;
using System;
using System.Collections.Generic;

namespace Laboratory.Core.Localization
{
    /// <summary>
    /// ScriptableObject database storing translations for a specific language
    /// Designer-friendly: Add/edit translations in the Unity Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationDatabase_", menuName = "Chimera/Localization/Language Database")]
    public class LocalizationDatabase : ScriptableObject
    {
        [Header("Language Configuration")]
        [SerializeField] private SystemLanguage language = SystemLanguage.English;
        [SerializeField] private string languageName = "English";
        [SerializeField] private string languageCode = "en";

        [Header("Translations")]
        [SerializeField] private List<LocalizationEntry> entries = new List<LocalizationEntry>();

        // Runtime lookup cache
        private Dictionary<string, string> _textLookup;
        private bool _isBuilt = false;

        /// <summary>
        /// Language this database represents
        /// </summary>
        public SystemLanguage Language => language;

        /// <summary>
        /// Human-readable language name
        /// </summary>
        public string LanguageName => languageName;

        /// <summary>
        /// ISO language code (en, es, fr, etc.)
        /// </summary>
        public string LanguageCode => languageCode;

        /// <summary>
        /// Total number of translation entries
        /// </summary>
        public int EntryCount => entries.Count;

        /// <summary>
        /// Builds the lookup dictionary for fast runtime access
        /// </summary>
        private void BuildLookup()
        {
            if (_isBuilt && _textLookup != null) return;

            _textLookup = new Dictionary<string, string>(entries.Count);

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.key))
                {
                    Debug.LogWarning($"[LocalizationDatabase:{languageName}] Found entry with empty key!");
                    continue;
                }

                if (_textLookup.ContainsKey(entry.key))
                {
                    Debug.LogWarning($"[LocalizationDatabase:{languageName}] Duplicate key: '{entry.key}'");
                    continue;
                }

                _textLookup[entry.key] = entry.value;
            }

            _isBuilt = true;
        }

        /// <summary>
        /// Gets the translated text for a key
        /// </summary>
        public string GetText(string key)
        {
            BuildLookup();

            if (_textLookup.TryGetValue(key, out string value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Checks if a key exists in this database
        /// </summary>
        public bool HasKey(string key)
        {
            BuildLookup();
            return _textLookup.ContainsKey(key);
        }

        /// <summary>
        /// Gets all keys in this database
        /// </summary>
        public List<string> GetAllKeys()
        {
            BuildLookup();
            return new List<string>(_textLookup.Keys);
        }

        /// <summary>
        /// Adds or updates a translation entry (Editor utility)
        /// </summary>
        public void SetText(string key, string value)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key)
                {
                    entries[i] = new LocalizationEntry { key = key, value = value };
                    _isBuilt = false; // Invalidate cache
                    return;
                }
            }

            // Key not found, add new entry
            entries.Add(new LocalizationEntry { key = key, value = value });
            _isBuilt = false; // Invalidate cache
        }

        /// <summary>
        /// Removes a translation entry (Editor utility)
        /// </summary>
        public void RemoveKey(string key)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].key == key)
                {
                    entries.RemoveAt(i);
                    _isBuilt = false; // Invalidate cache
                    return;
                }
            }
        }

        /// <summary>
        /// Editor utility: Sorts entries alphabetically by key
        /// </summary>
        [ContextMenu("Sort Entries Alphabetically")]
        private void SortEntries()
        {
            entries.Sort((a, b) => string.Compare(a.key, b.key, StringComparison.Ordinal));
            Debug.Log($"[LocalizationDatabase:{languageName}] Sorted {entries.Count} entries alphabetically");
        }

        /// <summary>
        /// Editor utility: Finds duplicate keys
        /// </summary>
        [ContextMenu("Find Duplicate Keys")]
        private void FindDuplicateKeys()
        {
            var seen = new HashSet<string>();
            var duplicates = new List<string>();

            foreach (var entry in entries)
            {
                if (!seen.Add(entry.key))
                {
                    duplicates.Add(entry.key);
                }
            }

            if (duplicates.Count > 0)
            {
                Debug.LogWarning($"[LocalizationDatabase:{languageName}] Found {duplicates.Count} duplicate keys: {string.Join(", ", duplicates)}");
            }
            else
            {
                Debug.Log($"[LocalizationDatabase:{languageName}] No duplicate keys found!");
            }
        }

        /// <summary>
        /// Editor utility: Finds empty values
        /// </summary>
        [ContextMenu("Find Empty Values")]
        private void FindEmptyValues()
        {
            var emptyKeys = new List<string>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.value))
                {
                    emptyKeys.Add(entry.key);
                }
            }

            if (emptyKeys.Count > 0)
            {
                Debug.LogWarning($"[LocalizationDatabase:{languageName}] Found {emptyKeys.Count} empty values: {string.Join(", ", emptyKeys)}");
            }
            else
            {
                Debug.Log($"[LocalizationDatabase:{languageName}] No empty values found!");
            }
        }

        /// <summary>
        /// Editor utility: Validates database integrity
        /// </summary>
        [ContextMenu("Validate Database")]
        private void ValidateDatabase()
        {
            Debug.Log($"[LocalizationDatabase:{languageName}] Validation started...");

            FindDuplicateKeys();
            FindEmptyValues();

            Debug.Log($"[LocalizationDatabase:{languageName}] Total entries: {entries.Count}");
            Debug.Log($"[LocalizationDatabase:{languageName}] Language: {language} ({languageCode})");
        }

        private void OnValidate()
        {
            // Invalidate cache when entries change in editor
            _isBuilt = false;
        }
    }

    /// <summary>
    /// Single localization key-value pair
    /// </summary>
    [Serializable]
    public struct LocalizationEntry
    {
        [Tooltip("Unique key identifier (e.g., 'ui.button.start')")]
        public string key;

        [TextArea(2, 5)]
        [Tooltip("Translated text value")]
        public string value;
    }
}
