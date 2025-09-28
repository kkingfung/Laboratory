using UnityEngine;
using Unity.Mathematics;
using Laboratory.Core;

namespace Laboratory.UI
{
    /// <summary>
    /// Chimera UI Configuration - ScriptableObject-based UI/UX settings
    /// PURPOSE: Designer-friendly configuration for all UI behavior, theming, and performance
    /// FEATURES: Theme management, accessibility options, performance settings, responsive design
    /// ARCHITECTURE: Integrates with ChimeraGameConfig and provides unified UI configuration
    /// </summary>

    [CreateAssetMenu(fileName = "ChimeraUIConfig", menuName = "Laboratory/UI/UI Configuration")]
    public class ChimeraUIConfiguration : ScriptableObject
    {
        [Header("Core UI Settings")]
        [Tooltip("UI update rate in Hz (lower = better performance)")]
        [Range(10, 120)]
        public int uiUpdateRate = 60;

        [Tooltip("Enable UI animations and transitions")]
        public bool enableUIAnimations = true;

        [Tooltip("Default UI animation duration")]
        [Range(0.1f, 2f)]
        public float defaultAnimationDuration = 0.3f;

        [Tooltip("Enable UI sound effects")]
        public bool enableUISounds = true;

        [Tooltip("UI master volume")]
        [Range(0f, 1f)]
        public float uiVolume = 0.7f;

        [Header("Performance Settings")]
        [Tooltip("Enable UI object pooling")]
        public bool enableObjectPooling = true;

        [Tooltip("Maximum pooled UI objects per type")]
        [Range(10, 500)]
        public int maxPooledObjects = 100;

        [Tooltip("Show performance metrics in debug builds")]
        public bool showPerformanceMetrics = true;

        [Tooltip("Enable UI culling for off-screen elements")]
        public bool enableUICulling = true;

        [Tooltip("UI LOD system for distant elements")]
        public bool enableUILOD = true;

        [Header("Multiplayer UI")]
        [Tooltip("Enable multiplayer-specific UI elements")]
        public bool enableMultiplayerUI = true;

        [Tooltip("Network status update interval (seconds)")]
        [Range(0.1f, 5f)]
        public float networkStatusUpdateInterval = 1f;

        [Tooltip("Show player names above creatures")]
        public bool showPlayerNames = true;

        [Tooltip("Show latency indicators")]
        public bool showLatencyIndicators = true;

        [Header("Accessibility")]
        [SerializeField]
        private AccessibilityOptions accessibilityOptions = new AccessibilityOptions();

        [Header("Theme Settings")]
        [SerializeField]
        private UITheme[] availableThemes = new UITheme[]
        {
            new UITheme
            {
                themeName = "Default",
                primaryColor = new Color(0.2f, 0.6f, 1f, 1f),
                secondaryColor = new Color(0.8f, 0.8f, 0.8f, 1f),
                backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f),
                textColor = Color.white,
                accentColor = new Color(1f, 0.6f, 0.2f, 1f)
            },
            new UITheme
            {
                themeName = "High Contrast",
                primaryColor = Color.white,
                secondaryColor = Color.black,
                backgroundColor = Color.black,
                textColor = Color.white,
                accentColor = Color.yellow
            },
            new UITheme
            {
                themeName = "Protanopia Friendly",
                primaryColor = new Color(0f, 0.4f, 1f, 1f),
                secondaryColor = new Color(0.8f, 0.8f, 0.2f, 1f),
                backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f),
                textColor = Color.white,
                accentColor = new Color(0.8f, 0.8f, 0.2f, 1f)
            }
        };

        [Header("Responsive Design")]
        [SerializeField]
        private ResponsiveSettings responsiveSettings = new ResponsiveSettings();

        [Header("Notification Settings")]
        [Tooltip("Maximum notifications to show at once")]
        [Range(1, 10)]
        public int maxSimultaneousNotifications = 5;

        [Tooltip("Default notification duration")]
        [Range(1f, 10f)]
        public float defaultNotificationDuration = 4f;

        [Tooltip("Notification fade animation duration")]
        [Range(0.1f, 2f)]
        public float notificationFadeDuration = 0.5f;

        [Header("Input Settings")]
        [Tooltip("Enable keyboard navigation")]
        public bool enableKeyboardNavigation = true;

        [Tooltip("Enable controller support")]
        public bool enableControllerSupport = true;

        [Tooltip("Input repeat delay (seconds)")]
        [Range(0.1f, 1f)]
        public float inputRepeatDelay = 0.5f;

        [Tooltip("Input repeat rate (Hz)")]
        [Range(1, 30)]
        public int inputRepeatRate = 10;

        [Header("Debug Settings")]
        [Tooltip("Enable UI debug logging")]
        public bool enableDebugLogging = false;

        [Tooltip("Show UI element bounds in debug")]
        public bool showUIBounds = false;

        [Tooltip("Show UI performance overlay")]
        public bool showPerformanceOverlay = false;

        /// <summary>
        /// Get UI theme by name
        /// </summary>
        public UITheme GetTheme(string themeName)
        {
            foreach (var theme in availableThemes)
            {
                if (theme.themeName == themeName)
                    return theme;
            }
            return availableThemes[0]; // Return default theme
        }

        /// <summary>
        /// Get UI theme by index
        /// </summary>
        public UITheme GetTheme(int index)
        {
            if (index >= 0 && index < availableThemes.Length)
                return availableThemes[index];
            return availableThemes[0];
        }

        /// <summary>
        /// Get current accessibility options
        /// </summary>
        public AccessibilityOptions GetAccessibilityOptions()
        {
            return accessibilityOptions;
        }

        /// <summary>
        /// Get responsive settings for current screen resolution
        /// </summary>
        public ResponsiveBreakpoint GetResponsiveBreakpoint()
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;

            if (screenWidth <= responsiveSettings.mobileBreakpoint.x || screenHeight <= responsiveSettings.mobileBreakpoint.y)
                return ResponsiveBreakpoint.Mobile;
            else if (screenWidth <= responsiveSettings.tabletBreakpoint.x || screenHeight <= responsiveSettings.tabletBreakpoint.y)
                return ResponsiveBreakpoint.Tablet;
            else
                return ResponsiveBreakpoint.Desktop;
        }

        /// <summary>
        /// Calculate UI scale based on screen size and settings
        /// </summary>
        public float CalculateUIScale()
        {
            var breakpoint = GetResponsiveBreakpoint();
            var baseScale = responsiveSettings.baseUIScale;

            return breakpoint switch
            {
                ResponsiveBreakpoint.Mobile => baseScale * responsiveSettings.mobileScaleMultiplier,
                ResponsiveBreakpoint.Tablet => baseScale * responsiveSettings.tabletScaleMultiplier,
                ResponsiveBreakpoint.Desktop => baseScale * responsiveSettings.desktopScaleMultiplier,
                _ => baseScale
            };
        }

        /// <summary>
        /// Validate configuration settings
        /// </summary>
        public bool ValidateConfiguration(out string[] errors)
        {
            var errorList = new System.Collections.Generic.List<string>();

            // Validate basic settings
            if (uiUpdateRate <= 0)
                errorList.Add("UI update rate must be greater than 0");

            if (defaultAnimationDuration <= 0)
                errorList.Add("Default animation duration must be greater than 0");

            if (maxPooledObjects <= 0)
                errorList.Add("Max pooled objects must be greater than 0");

            // Validate themes
            if (availableThemes == null || availableThemes.Length == 0)
                errorList.Add("At least one UI theme is required");

            // Check for duplicate theme names
            var themeNames = new System.Collections.Generic.HashSet<string>();
            foreach (var theme in availableThemes)
            {
                if (string.IsNullOrEmpty(theme.themeName))
                    errorList.Add("All themes must have a name");
                else if (!themeNames.Add(theme.themeName))
                    errorList.Add($"Duplicate theme name: {theme.themeName}");
            }

            // Validate responsive settings
            if (responsiveSettings.baseUIScale <= 0)
                errorList.Add("Base UI scale must be greater than 0");

            if (responsiveSettings.mobileBreakpoint.x <= 0 || responsiveSettings.mobileBreakpoint.y <= 0)
                errorList.Add("Mobile breakpoint values must be greater than 0");

            errors = errorList.ToArray();
            return errorList.Count == 0;
        }

        /// <summary>
        /// Reset to default values
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            uiUpdateRate = 60;
            enableUIAnimations = true;
            defaultAnimationDuration = 0.3f;
            enableUISounds = true;
            uiVolume = 0.7f;
            enableObjectPooling = true;
            maxPooledObjects = 100;
            showPerformanceMetrics = true;
            enableUICulling = true;
            enableUILOD = true;
            enableMultiplayerUI = true;
            networkStatusUpdateInterval = 1f;
            showPlayerNames = true;
            showLatencyIndicators = true;
            maxSimultaneousNotifications = 5;
            defaultNotificationDuration = 4f;
            notificationFadeDuration = 0.5f;
            enableKeyboardNavigation = true;
            enableControllerSupport = true;
            inputRepeatDelay = 0.5f;
            inputRepeatRate = 10;
            enableDebugLogging = false;
            showUIBounds = false;
            showPerformanceOverlay = false;

            // Reset accessibility options
            accessibilityOptions = new AccessibilityOptions();

            // Reset responsive settings
            responsiveSettings = new ResponsiveSettings();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure values stay within valid ranges
            uiUpdateRate = Mathf.Clamp(uiUpdateRate, 10, 120);
            defaultAnimationDuration = Mathf.Max(0.1f, defaultAnimationDuration);
            uiVolume = Mathf.Clamp01(uiVolume);
            maxPooledObjects = Mathf.Max(10, maxPooledObjects);
            networkStatusUpdateInterval = Mathf.Max(0.1f, networkStatusUpdateInterval);
            maxSimultaneousNotifications = Mathf.Clamp(maxSimultaneousNotifications, 1, 10);
            defaultNotificationDuration = Mathf.Max(1f, defaultNotificationDuration);
            notificationFadeDuration = Mathf.Max(0.1f, notificationFadeDuration);
            inputRepeatDelay = Mathf.Max(0.1f, inputRepeatDelay);
            inputRepeatRate = Mathf.Clamp(inputRepeatRate, 1, 30);
        }
#endif
    }

    [System.Serializable]
    public struct UITheme
    {
        [Tooltip("Name of the theme")]
        public string themeName;

        [Tooltip("Primary UI color")]
        public Color primaryColor;

        [Tooltip("Secondary UI color")]
        public Color secondaryColor;

        [Tooltip("Background color")]
        public Color backgroundColor;

        [Tooltip("Text color")]
        public Color textColor;

        [Tooltip("Accent color for highlights")]
        public Color accentColor;

        [Tooltip("Success color")]
        public Color successColor;

        [Tooltip("Warning color")]
        public Color warningColor;

        [Tooltip("Error color")]
        public Color errorColor;

        [Tooltip("Disabled element color")]
        public Color disabledColor;
    }

    [System.Serializable]
    public struct AccessibilityOptions
    {
        [Tooltip("Enable high contrast mode")]
        public bool highContrastMode;

        [Tooltip("Large text size multiplier")]
        [Range(1f, 3f)]
        public float textSizeMultiplier;

        [Tooltip("Enable screen reader support")]
        public bool screenReaderSupport;

        [Tooltip("Reduce UI animations and motion")]
        public bool reduceMotion;

        [Tooltip("Enable colorblind-friendly colors")]
        public bool colorblindFriendly;

        [Tooltip("Extended interaction timeouts")]
        public bool extendedTimeouts;

        [Tooltip("Audio cues for UI interactions")]
        public bool audioCues;

        [Tooltip("Focus indicator strength")]
        [Range(1f, 5f)]
        public float focusIndicatorStrength;
    }

    [System.Serializable]
    public struct ResponsiveSettings
    {
        [Tooltip("Base UI scale factor")]
        [Range(0.5f, 3f)]
        public float baseUIScale;

        [Tooltip("Mobile device breakpoint (width, height)")]
        public Vector2Int mobileBreakpoint;

        [Tooltip("Tablet device breakpoint (width, height)")]
        public Vector2Int tabletBreakpoint;

        [Tooltip("UI scale multiplier for mobile")]
        [Range(0.5f, 2f)]
        public float mobileScaleMultiplier;

        [Tooltip("UI scale multiplier for tablet")]
        [Range(0.7f, 1.5f)]
        public float tabletScaleMultiplier;

        [Tooltip("UI scale multiplier for desktop")]
        [Range(0.8f, 1.2f)]
        public float desktopScaleMultiplier;

        [Tooltip("Enable adaptive UI layout")]
        public bool enableAdaptiveLayout;

        [Tooltip("Minimum touch target size")]
        [Range(32, 64)]
        public int minTouchTargetSize;

        // Constructor to initialize default values
        public ResponsiveSettings(bool setDefaults)
        {
            baseUIScale = 1f;
            mobileBreakpoint = new Vector2Int(768, 1024);
            tabletBreakpoint = new Vector2Int(1024, 1366);
            mobileScaleMultiplier = 1.2f;
            tabletScaleMultiplier = 1.1f;
            desktopScaleMultiplier = 1f;
            enableAdaptiveLayout = true;
            minTouchTargetSize = 44;
        }
    }

    public enum ResponsiveBreakpoint
    {
        Mobile = 0,
        Tablet = 1,
        Desktop = 2
    }

    /// <summary>
    /// UI Theme Applier - Applies themes to UI elements
    /// </summary>
    public static class UIThemeApplier
    {
        /// <summary>
        /// Apply theme to a UI hierarchy
        /// </summary>
        public static void ApplyTheme(GameObject rootObject, UITheme theme)
        {
            var images = rootObject.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            var buttons = rootObject.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            var texts = rootObject.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);

            // Apply theme to images
            foreach (var image in images)
            {
                if (image.CompareTag("UI_Primary"))
                    image.color = theme.primaryColor;
                else if (image.CompareTag("UI_Secondary"))
                    image.color = theme.secondaryColor;
                else if (image.CompareTag("UI_Background"))
                    image.color = theme.backgroundColor;
                else if (image.CompareTag("UI_Accent"))
                    image.color = theme.accentColor;
            }

            // Apply theme to buttons
            foreach (var button in buttons)
            {
                var colors = button.colors;
                colors.normalColor = theme.primaryColor;
                colors.highlightedColor = theme.accentColor;
                colors.pressedColor = theme.secondaryColor;
                colors.disabledColor = theme.disabledColor;
                button.colors = colors;
            }

            // Apply theme to text
            foreach (var text in texts)
            {
                if (!text.CompareTag("UI_CustomColor"))
                    text.color = theme.textColor;
            }
        }

        /// <summary>
        /// Apply accessibility settings to a UI hierarchy
        /// </summary>
        public static void ApplyAccessibilitySettings(GameObject rootObject, AccessibilityOptions options)
        {
            if (options.textSizeMultiplier > 1f)
            {
                var texts = rootObject.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach (var text in texts)
                {
                    text.fontSize *= options.textSizeMultiplier;
                }
            }

            if (options.focusIndicatorStrength > 1f)
            {
                var selectables = rootObject.GetComponentsInChildren<UnityEngine.UI.Selectable>(true);
                foreach (var selectable in selectables)
                {
                    // Enhance focus indicators
                    // This would need custom implementation based on your UI system
                }
            }

            if (options.reduceMotion)
            {
                // Disable or reduce animations
                var animators = rootObject.GetComponentsInChildren<Animator>(true);
                foreach (var animator in animators)
                {
                    animator.speed = 0.1f; // Very slow animations instead of disabling
                }
            }
        }
    }
}