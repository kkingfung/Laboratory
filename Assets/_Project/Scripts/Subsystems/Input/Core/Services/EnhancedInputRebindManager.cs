using System;
using UnityEngine;
using Laboratory.Core.Input.Interfaces;
using Laboratory.Core.Input.Events;
using Laboratory.Core.DI;

namespace Laboratory.Core.Input.Services
{
    /// <summary>
    /// Enhanced input rebinding manager that integrates with the unified input system.
    /// Provides centralized rebinding functionality with better error handling and validation.
    /// </summary>
    public class EnhancedInputRebindManager : MonoBehaviour
    {
        #region Constants
        
        private const string REBINDS_KEY = "enhanced_input_rebinds";
        
        #endregion
        
        #region Static Properties
        
        /// <summary>Singleton instance of the EnhancedInputRebindManager.</summary>
        public static EnhancedInputRebindManager Instance { get; private set; }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private bool autoSave = true;
        [SerializeField] private bool debugLogging = false;
        
        #endregion
        
        #region Private Fields
        
        private IInputService _inputService;
        private bool _isInitialized = false;
        
        #endregion
        
        #region Events
        
        /// <summary>Fired when rebinding starts for any action.</summary>
        public event Action<string> OnRebindStarted;
        
        /// <summary>Fired when rebinding completes successfully.</summary>
        public event Action<string> OnRebindCompleted;
        
        /// <summary>Fired when rebinding is cancelled.</summary>
        public event Action<string> OnRebindCancelled;
        
        /// <summary>Fired when rebinding fails.</summary>
        public event Action<string, string> OnRebindFailed;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeSingleton();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }
        
        #endregion
        
        #region Public Methods - Save/Load Operations
        
        /// <summary>
        /// Saves the current input rebindings.
        /// </summary>
        /// <returns>True if save was successful</returns>
        public bool SaveRebinds()
        {
            if (!_isInitialized)
            {
                LogWarning("Cannot save rebindings - manager not initialized");
                return false;
            }
            
            try
            {
                _inputService.SaveConfiguration();
                LogDebug("Input rebindings saved successfully");
                
                // Trigger configuration changed event
                InputEvents.TriggerConfigurationChanged(new InputConfigurationChangedEventArgs
                {
                    PropertyName = "Rebindings",
                    NewValue = _inputService.Configuration,
                    OldValue = null,
                    Timestamp = (float)Time.unscaledTimeAsDouble
                });
                
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to save rebindings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads input rebindings from saved data.
        /// </summary>
        /// <returns>True if load was successful</returns>
        public bool LoadRebinds()
        {
            if (!_isInitialized)
            {
                LogWarning("Cannot load rebindings - manager not initialized");
                return false;
            }
            
            try
            {
                _inputService.LoadConfiguration();
                LogDebug("Input rebindings loaded successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to load rebindings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resets all input rebindings to their default values.
        /// </summary>
        /// <returns>True if reset was successful</returns>
        public bool ResetRebinds()
        {
            if (!_isInitialized)
            {
                LogWarning("Cannot reset rebindings - manager not initialized");
                return false;
            }
            
            try
            {
                _inputService.ResetConfiguration();
                LogDebug("Input rebindings reset to defaults");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Failed to reset rebindings: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Public Methods - Rebinding Operations
        
        /// <summary>
        /// Starts interactive rebinding for a specific action.
        /// </summary>
        /// <param name="actionName">Name of the action to rebind</param>
        /// <param name="bindingIndex">Index of the binding to rebind (default: 0)</param>
        /// <returns>True if rebinding started successfully</returns>
        public bool StartRebind(string actionName, int bindingIndex = 0)
        {
            if (!_isInitialized)
            {
                LogWarning("Cannot start rebind - manager not initialized");
                return false;
            }
            
            if (string.IsNullOrEmpty(actionName))
            {
                LogError("Cannot start rebind - action name is null or empty");
                return false;
            }
            
            if (!_inputService.HasAction(actionName))
            {
                LogError($"Cannot start rebind - action '{actionName}' not found");
                return false;
            }
            
            try
            {
                OnRebindStarted?.Invoke(actionName);
                LogDebug($"Starting rebind for action '{actionName}', binding index {bindingIndex}");
                
                _inputService.StartRebind(actionName, bindingIndex, (success) =>
                {
                    if (success)
                    {
                        OnRebindCompleted?.Invoke(actionName);
                        LogDebug($"Rebind completed for action '{actionName}'");
                        
                        if (autoSave)
                        {
                            SaveRebinds();
                        }
                    }
                    else
                    {
                        OnRebindCancelled?.Invoke(actionName);
                        LogDebug($"Rebind cancelled for action '{actionName}'");
                    }
                });
                
                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to start rebind for '{actionName}': {ex.Message}";
                LogError(errorMessage);
                OnRebindFailed?.Invoke(actionName, errorMessage);
                return false;
            }
        }
        
        /// <summary>
        /// Gets the display string for a specific action binding.
        /// </summary>
        /// <param name="actionName">Name of the action</param>
        /// <param name="bindingIndex">Index of the binding (default: 0)</param>
        /// <returns>Display string or null if not found</returns>
        public string GetBindingDisplayString(string actionName, int bindingIndex = 0)
        {
            if (!_isInitialized || string.IsNullOrEmpty(actionName))
            {
                return null;
            }
            
            try
            {
                // This would need to be implemented in the IInputService interface
                // For now, return a placeholder
                return $"{actionName}[{bindingIndex}]";
            }
            catch (Exception ex)
            {
                LogError($"Failed to get binding display string for '{actionName}': {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a specific action can be rebound.
        /// </summary>
        /// <param name="actionName">Name of the action to check</param>
        /// <returns>True if the action can be rebound</returns>
        public bool CanRebindAction(string actionName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(actionName))
            {
                return false;
            }
            
            return _inputService.HasAction(actionName);
        }
        
        /// <summary>
        /// Gets all available actions that can be rebound.
        /// </summary>
        /// <returns>Array of action names</returns>
        public string[] GetRebindableActions()
        {
            if (!_isInitialized)
            {
                return Array.Empty<string>();
            }
            
            // Return known actions from the input system
            return new[]
            {
                "GoEast", "GoWest", "GoNorth", "GoSouth",
                "AttackOrThrow", "Jump", "Roll", "ActionOrCraft",
                "CharSkill", "WeaponSkill", "Pause"
            };
        }
        
        #endregion
        
        #region Public Methods - Configuration
        
        /// <summary>
        /// Sets whether rebindings should be automatically saved.
        /// </summary>
        /// <param name="enabled">True to enable auto-save</param>
        public void SetAutoSave(bool enabled)
        {
            autoSave = enabled;
            LogDebug($"Auto-save set to: {enabled}");
        }
        
        /// <summary>
        /// Gets the current input configuration.
        /// </summary>
        /// <returns>Current input configuration or null if not available</returns>
        public IInputConfiguration GetCurrentConfiguration()
        {
            return _inputService?.Configuration;
        }
        
        /// <summary>
        /// Checks if the rebinding manager is properly initialized.
        /// </summary>
        /// <returns>True if initialized and ready to use</returns>
        public bool IsInitialized()
        {
            return _isInitialized && _inputService != null;
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                LogWarning("Multiple EnhancedInputRebindManager instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LogDebug("EnhancedInputRebindManager singleton initialized");
        }
        
        private void Initialize()
        {
            try
            {
                _inputService = GlobalServiceProvider.Instance.Resolve<IInputService>();
                
                if (_inputService == null)
                {
                    LogError("Failed to get IInputService from service container");
                    return;
                }
                
                _isInitialized = true;
                LogDebug("EnhancedInputRebindManager initialized successfully");
                
                // Load existing rebindings
                LoadRebinds();
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize: {ex.Message}");
                _isInitialized = false;
            }
        }
        
        private void Cleanup()
        {
            try
            {
                if (_isInitialized && autoSave)
                {
                    SaveRebinds();
                }
                
                _inputService = null;
                _isInitialized = false;
                
                LogDebug("EnhancedInputRebindManager cleanup completed");
            }
            catch (Exception ex)
            {
                LogError($"Error during cleanup: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Logging
        
        private void LogDebug(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"[EnhancedInputRebindManager] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[EnhancedInputRebindManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[EnhancedInputRebindManager] {message}");
        }
        
        #endregion
    }
}
