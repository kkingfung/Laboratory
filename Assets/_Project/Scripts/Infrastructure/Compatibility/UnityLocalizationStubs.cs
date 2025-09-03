using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Localization
{
    /// <summary>
    /// Stub implementation for Unity Localization Locale
    /// </summary>
    public class Locale
    {
        public string LocaleName { get; set; } = "English";
        public LocaleIdentifier Identifier { get; set; } = new LocaleIdentifier();
        
        public Locale()
        {
            Identifier = new LocaleIdentifier { Code = "en" };
        }
        
        public Locale(string code, string name)
        {
            Identifier = new LocaleIdentifier { Code = code };
            LocaleName = name;
        }
    }

    /// <summary>
    /// Stub implementation for locale identifier
    /// </summary>
    public class LocaleIdentifier
    {
        public string Code { get; set; } = "en";
    }

    /// <summary>
    /// Stub implementation for available locales
    /// </summary>
    public class AvailableLocales
    {
        private static List<Locale> _locales = new List<Locale>
        {
            new Locale("en", "English"),
            new Locale("es", "Spanish"),
            new Locale("fr", "French")
        };

        public List<Locale> Locales => _locales;
    }
    
    /// <summary>
    /// Stub implementation for initialization operation
    /// </summary>
    public class InitializationOperation : CustomYieldInstruction
    {
        private bool _isDone = false;
        
        public override bool keepWaiting => !_isDone;
        
        public InitializationOperation()
        {
            // Simulate async initialization
            UnityEngine.Object.FindFirstObjectByType<MonoBehaviour>()?.StartCoroutine(CompleteAfterFrame());
        }
        
        private IEnumerator CompleteAfterFrame()
        {
            yield return null;
            _isDone = true;
        }
    }
    
    public static class LocalizationSettings
    {
        public static SelectedLocaleChangedEvent SelectedLocaleChanged { get; } = new SelectedLocaleChangedEvent();
    }
    
    public class SelectedLocaleChangedEvent
    {
        public void AddListener(Action<Locale> listener) { }
        public void RemoveListener(Action<Locale> listener) { }
    }
}

namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// Stub implementation for LocalizationSettings with proper API
    /// </summary>
    public static class LocalizationSettings
    {
        private static UnityEngine.Localization.AvailableLocales _availableLocales = new UnityEngine.Localization.AvailableLocales();
        private static UnityEngine.Localization.Locale _selectedLocale = new UnityEngine.Localization.Locale("en", "English");
        
        /// <summary>
        /// Gets the initialization operation
        /// </summary>
        public static UnityEngine.Localization.InitializationOperation InitializationOperation { get; } = new UnityEngine.Localization.InitializationOperation();
        
        /// <summary>
        /// Gets the available locales
        /// </summary>
        public static UnityEngine.Localization.AvailableLocales AvailableLocales => _availableLocales;
        
        /// <summary>
        /// Gets or sets the selected locale
        /// </summary>
        public static UnityEngine.Localization.Locale SelectedLocale 
        { 
            get => _selectedLocale;
            set 
            {
                if (_selectedLocale != value)
                {
                    _selectedLocale = value;
                    Debug.Log($"[LocalizationStub] Selected locale changed to: {value?.LocaleName}");
                }
            }
        }
        
        public static UnityEngine.Localization.SelectedLocaleChangedEvent SelectedLocaleChanged { get; } = new UnityEngine.Localization.SelectedLocaleChangedEvent();
    }
}
