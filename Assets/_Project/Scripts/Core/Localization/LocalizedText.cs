using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace Laboratory.Core.Localization
{
    /// <summary>
    /// Component that automatically updates UI text with localized strings
    /// Supports both Unity UI Text and TextMeshPro
    /// Automatically updates when language changes
    /// </summary>
    [ExecuteAlways]
    public class LocalizedText : MonoBehaviour
    {
        [Header("Localization Settings")]
        [SerializeField] private string localizationKey;
        [SerializeField] private bool updateOnLanguageChange = true;

        [Header("Format Arguments (Optional)")]
        [SerializeField] private string[] formatArgs = new string[0];
        [SerializeField] private bool useFormatArgs = false;

        [Header("Fallback")]
        [SerializeField] private string fallbackText = "";

        // Component references
        private Text _uiText;
        private TextMeshProUGUI _tmpText;
        private TextMeshPro _tmpProText;

        private bool _isInitialized = false;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (!_isInitialized)
                Initialize();

            UpdateText();

            if (updateOnLanguageChange && LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
        }

        private void OnDisable()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Update text in editor when key changes
            if (!Application.isPlaying)
            {
                Initialize();
                UpdateText();
            }
        }
#endif

        /// <summary>
        /// Initializes component references
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized) return;

            // Find text component (prefer TMP over legacy UI Text)
            _tmpText = GetComponent<TextMeshProUGUI>();
            if (_tmpText == null)
            {
                _tmpProText = GetComponent<TextMeshPro>();
                if (_tmpProText == null)
                {
                    _uiText = GetComponent<Text>();
                }
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Sets the localization key and updates the text
        /// </summary>
        public void SetKey(string key)
        {
            localizationKey = key;
            UpdateText();
        }

        /// <summary>
        /// Sets format arguments and updates the text
        /// </summary>
        public void SetFormatArgs(params string[] args)
        {
            formatArgs = args;
            useFormatArgs = args != null && args.Length > 0;
            UpdateText();
        }

        /// <summary>
        /// Updates the displayed text with the current localization
        /// </summary>
        public void UpdateText()
        {
            if (string.IsNullOrEmpty(localizationKey))
            {
                SetTextValue(fallbackText);
                return;
            }

            // Get localized text
            string localizedText;

            if (LocalizationManager.Instance == null)
            {
                // Localization system not initialized, use fallback
                localizedText = !string.IsNullOrEmpty(fallbackText) ? fallbackText : localizationKey;
            }
            else
            {
                if (useFormatArgs && formatArgs != null && formatArgs.Length > 0)
                {
                    // Convert string args to object args for formatting
                    object[] objectArgs = new object[formatArgs.Length];
                    for (int i = 0; i < formatArgs.Length; i++)
                    {
                        objectArgs[i] = formatArgs[i];
                    }
                    localizedText = LocalizationManager.Instance.GetText(localizationKey, objectArgs);
                }
                else
                {
                    localizedText = LocalizationManager.Instance.GetText(localizationKey);
                }

                // Use fallback if key not found (check for missing key format)
                if (localizedText.StartsWith("[MISSING:") && !string.IsNullOrEmpty(fallbackText))
                {
                    localizedText = fallbackText;
                }
            }

            SetTextValue(localizedText);
        }

        /// <summary>
        /// Sets the text value on the appropriate component
        /// </summary>
        private void SetTextValue(string value)
        {
            if (_tmpText != null)
            {
                _tmpText.text = value;
            }
            else if (_tmpProText != null)
            {
                _tmpProText.text = value;
            }
            else if (_uiText != null)
            {
                _uiText.text = value;
            }
            else
            {
                Debug.LogWarning($"[LocalizedText] No Text component found on {gameObject.name}!", this);
            }
        }

        /// <summary>
        /// Called when the language changes
        /// </summary>
        private void OnLanguageChanged(SystemLanguage newLanguage)
        {
            UpdateText();
        }

        /// <summary>
        /// Editor utility: Preview current localization
        /// </summary>
        [ContextMenu("Preview Localization")]
        private void PreviewLocalization()
        {
            Initialize();
            UpdateText();

            string currentText = GetCurrentText();
            Debug.Log($"[LocalizedText] Key: '{localizationKey}' → Text: '{currentText}'", this);
        }

        /// <summary>
        /// Gets the current text value
        /// </summary>
        private string GetCurrentText()
        {
            if (_tmpText != null) return _tmpText.text;
            if (_tmpProText != null) return _tmpProText.text;
            if (_uiText != null) return _uiText.text;
            return "";
        }

        /// <summary>
        /// Editor utility: Set fallback to current text
        /// </summary>
        [ContextMenu("Set Fallback to Current Text")]
        private void SetFallbackToCurrentText()
        {
            Initialize();
            fallbackText = GetCurrentText();
            Debug.Log($"[LocalizedText] Fallback set to: '{fallbackText}'", this);
        }
    }
}
