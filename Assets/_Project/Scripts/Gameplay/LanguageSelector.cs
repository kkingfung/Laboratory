using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Laboratory.Infrastructure.Localization
{
    /// <summary>
    /// Manages language selection UI and handles switching between available locales.
    /// Integrates with Unity's Localization system to provide runtime language switching.
    /// </summary>
    public class LanguageSelector : MonoBehaviour
    {
        #region Constants
        
        private const string PREF_LANGUAGE_KEY = "language_code";
        
        #endregion
        
        #region Serialized Fields
        
        [Header("UI Elements")]
        [SerializeField] private Dropdown languageDropdown;
        
        #endregion
        
        #region Private Fields
        
        private bool isLoading = false;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Initializes the language selector and sets up available language options.
        /// </summary>
        private void Start()
        {
            StartCoroutine(SetupLanguageOptionsCoroutine());
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Handles language selection change from the dropdown UI.
        /// </summary>
        /// <param name="index">The selected language index</param>
        public void OnLanguageChanged(int index)
        {
            if (isLoading) return;
            
            StartCoroutine(ChangeLanguageCoroutine(index));
        }
        
        #endregion
        
        #region Private Methods - Setup
        
        /// <summary>
        /// Coroutine that sets up the language dropdown options after localization system initializes.
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator SetupLanguageOptionsCoroutine()
        {
            // Wait for localization system to initialize
            yield return LocalizationSettings.InitializationOperation;
            
            if (languageDropdown == null)
            {
                Debug.LogError("Language dropdown is not assigned!", this);
                yield break;
            }
            
            // Get available locales and create dropdown options
            var locales = LocalizationSettings.AvailableLocales.Locales;
            var options = new List<string>();
            int currentLocaleIndex = 0;
            
            for (int i = 0; i < locales.Count; i++)
            {
                options.Add(locales[i].LocaleName);
                
                // Determine the current locale index based on saved preference or current selection
                if (ShouldSelectLocale(locales[i]))
                {
                    currentLocaleIndex = i;
                }
            }
            
            // Configure the dropdown
            SetupDropdown(options, currentLocaleIndex);
        }
        
        /// <summary>
        /// Determines whether a specific locale should be selected based on saved preferences.
        /// </summary>
        /// <param name="locale">The locale to check</param>
        /// <returns>True if this locale should be selected</returns>
        private bool ShouldSelectLocale(Locale locale)
        {
            // Check if we have a saved language preference
            if (PlayerPrefs.HasKey(PREF_LANGUAGE_KEY))
            {
                return locale.Identifier.Code == PlayerPrefs.GetString(PREF_LANGUAGE_KEY);
            }
            
            // Fall back to currently selected locale if no preference is saved
            return locale == LocalizationSettings.SelectedLocale;
        }
        
        /// <summary>
        /// Configures the dropdown with language options and sets up event listener.
        /// </summary>
        /// <param name="options">List of language options to display</param>
        /// <param name="selectedIndex">The index to select by default</param>
        private void SetupDropdown(List<string> options, int selectedIndex)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(options);
            languageDropdown.value = selectedIndex;
            languageDropdown.RefreshShownValue();
            
            // Add event listener for dropdown changes
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }
        
        #endregion
        
        #region Private Methods - Language Switching
        
        /// <summary>
        /// Coroutine that handles the language switching process asynchronously.
        /// </summary>
        /// <param name="index">The index of the language to switch to</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator ChangeLanguageCoroutine(int index)
        {
            isLoading = true;
            
            var locales = LocalizationSettings.AvailableLocales.Locales;
            
            // Validate index
            if (index < 0 || index >= locales.Count)
            {
                Debug.LogError($"Invalid language index: {index}. Available locales: {locales.Count}", this);
                isLoading = false;
                yield break;
            }
            
            var selectedLocale = locales[index];
            
            try
            {
                // Switch to the selected locale
                yield return LocalizationSettings.SelectLocaleAsync(selectedLocale);
                
                // Save the language preference
                SaveLanguagePreference(selectedLocale.Identifier.Code);
                
                Debug.Log($"Language switched to: {selectedLocale.LocaleName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to switch language to {selectedLocale.LocaleName}: {e.Message}", this);
            }
            finally
            {
                isLoading = false;
            }
        }
        
        /// <summary>
        /// Saves the selected language code to PlayerPrefs.
        /// </summary>
        /// <param name="languageCode">The language code to save</param>
        private void SaveLanguagePreference(string languageCode)
        {
            PlayerPrefs.SetString(PREF_LANGUAGE_KEY, languageCode);
            PlayerPrefs.Save();
        }
        
        #endregion
    }
}
