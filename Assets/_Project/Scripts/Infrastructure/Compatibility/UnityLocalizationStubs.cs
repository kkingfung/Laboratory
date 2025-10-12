using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Infrastructure.Localization.Stubs
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

namespace Laboratory.Infrastructure.Localization.Stubs.Settings
{
    /// <summary>
    /// Stub implementation for LocalizationSettings with proper API
    /// </summary>
    public static class LocalizationSettings
    {
        private static Laboratory.Infrastructure.Localization.Stubs.AvailableLocales _availableLocales = new Laboratory.Infrastructure.Localization.Stubs.AvailableLocales();
        private static Laboratory.Infrastructure.Localization.Stubs.Locale _selectedLocale = new Laboratory.Infrastructure.Localization.Stubs.Locale("en", "English");
        
        /// <summary>
        /// Gets the initialization operation
        /// </summary>
        public static Laboratory.Infrastructure.Localization.Stubs.InitializationOperation InitializationOperation { get; } = new Laboratory.Infrastructure.Localization.Stubs.InitializationOperation();
        
        /// <summary>
        /// Gets the available locales
        /// </summary>
        public static Laboratory.Infrastructure.Localization.Stubs.AvailableLocales AvailableLocales => _availableLocales;
        
        /// <summary>
        /// Gets or sets the selected locale
        /// </summary>
        public static Laboratory.Infrastructure.Localization.Stubs.Locale SelectedLocale 
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
        
        public static Laboratory.Infrastructure.Localization.Stubs.SelectedLocaleChangedEvent SelectedLocaleChanged { get; } = new Laboratory.Infrastructure.Localization.Stubs.SelectedLocaleChangedEvent();
    }
}
