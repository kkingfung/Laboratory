using UnityEngine;
using UnityEngine.UI;
using Laboratory.Core.Input.Services;
using Laboratory.Core.Input.Events;

namespace Laboratory.UI.Input.Components
{
    /// <summary>
    /// Enhanced UI component for individual input rebinding controls.
    /// Integrates with the enhanced input rebinding system.
    /// </summary>
    public class EnhancedInputRebindUI : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("Input Configuration")]
        [SerializeField] private string actionName = "ActionOrCraft";
        [SerializeField] private int bindingIndex = 0;
        
        [Header("UI References")]
        [SerializeField] private Text displayText;
        [SerializeField] private Button rebindButton;
        
        [Header("UI States")]
        [SerializeField] private string defaultText = "Not Bound";
        [SerializeField] private string rebindingText = "Press a key...";
        [SerializeField] private string invalidActionText = "Invalid Action";
        
        [Header("Debug")]
        [SerializeField] private bool debugLogging = false;
        
        #endregion
        
        #region Private Fields
        
        private EnhancedInputRebindManager _rebindManager;
        private bool _isRebinding = false;
        private bool _isInitialized = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void OnEnable()
        {
            SubscribeToEvents();
            UpdateDisplay();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Starts the interactive rebinding process for the assigned input action.
        /// </summary>
        public void StartRebind()
        {
            if (!_isInitialized || _isRebinding)
            {
                LogWarning($"Cannot start rebind - initialized: {_isInitialized}, rebinding: {_isRebinding}");
                return;
            }
            
            if (string.IsNullOrEmpty(actionName))
            {
                LogError("Cannot start rebind - action name is not set");
                UpdateDisplayText(invalidActionText);
                return;
            }
            
            LogDebug($"Starting rebind for action '{actionName}', binding index {bindingIndex}");
            
            if (_rebindManager.StartRebind(actionName, bindingIndex))
            {
                SetRebindingState(true);
            }
        }
        
        /// <summary>
        /// Updates the action name at runtime.
        /// </summary>
        /// <param name="newActionName">New action name</param>
        public void SetActionName(string newActionName)
        {
            if (string.IsNullOrEmpty(newActionName))
            {
                LogWarning("Attempted to set empty action name");
                return;
            }
            
            actionName = newActionName;
            UpdateDisplay();
            LogDebug($"Action name updated to: {actionName}");
        }
        
        /// <summary>
        /// Updates the binding index at runtime.
        /// </summary>
        /// <param name="newBindingIndex">New binding index</param>
        public void SetBindingIndex(int newBindingIndex)
        {
            bindingIndex = Mathf.Max(0, newBindingIndex);
            UpdateDisplay();
            LogDebug($"Binding index updated to: {bindingIndex}");
        }
        
        /// <summary>
        /// Gets whether this component is currently in rebinding mode.
        /// </summary>
        /// <returns>True if rebinding is active</returns>
        public bool IsRebinding()
        {
            return _isRebinding;
        }
        
        /// <summary>
        /// Forces an update of the display text.
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateDisplay();
        }
        
        #endregion
        
        #region Private Methods
        
        private void InitializeComponents()
        {
            // Get components if not assigned
            if (displayText == null)
                displayText = GetComponentInChildren<Text>();
                
            if (rebindButton == null)
                rebindButton = GetComponent<Button>();
            
            // Validate required components
            if (rebindButton == null)
            {
                LogError("EnhancedInputRebindUI requires a Button component!");
                return;
            }
            
            // Setup button callback
            rebindButton.onClick.RemoveListener(StartRebind);
            rebindButton.onClick.AddListener(StartRebind);
            
            LogDebug("Components initialized");
        }
        
        private void Initialize()
        {
            try
            {
                // Get the rebind manager instance
                _rebindManager = EnhancedInputRebindManager.Instance;
                
                if (_rebindManager == null)
                {
                    LogError("EnhancedInputRebindManager instance not found!");
                    return;
                }
                
                _isInitialized = true;
                UpdateDisplay();
                
                LogDebug("EnhancedInputRebindUI initialized successfully");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to initialize: {ex.Message}");
                _isInitialized = false;
            }
        }
        
        private void SubscribeToEvents()
        {
            if (_rebindManager == null) return;
            
            try
            {
                _rebindManager.OnRebindStarted += OnRebindStarted;
                _rebindManager.OnRebindCompleted += OnRebindCompleted;
                _rebindManager.OnRebindCancelled += OnRebindCancelled;
                _rebindManager.OnRebindFailed += OnRebindFailed;
                
                // Subscribe to configuration changes
                InputEvents.OnConfigurationChanged += OnConfigurationChanged;
                
                LogDebug("Subscribed to events");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to subscribe to events: {ex.Message}");
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            try
            {
                if (_rebindManager != null)
                {
                    _rebindManager.OnRebindStarted -= OnRebindStarted;
                    _rebindManager.OnRebindCompleted -= OnRebindCompleted;
                    _rebindManager.OnRebindCancelled -= OnRebindCancelled;
                    _rebindManager.OnRebindFailed -= OnRebindFailed;
                }
                
                InputEvents.OnConfigurationChanged -= OnConfigurationChanged;
                
                LogDebug("Unsubscribed from events");
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to unsubscribe from events: {ex.Message}");
            }
        }
        
        private void UpdateDisplay()
        {
            if (!_isInitialized || displayText == null) return;
            
            try
            {
                if (string.IsNullOrEmpty(actionName))
                {
                    UpdateDisplayText(invalidActionText);
                    SetInteractable(false);
                    return;
                }
                
                if (!_rebindManager.CanRebindAction(actionName))
                {
                    UpdateDisplayText(invalidActionText);
                    SetInteractable(false);
                    return;
                }
                
                // Get the current binding display string
                var bindingDisplay = _rebindManager.GetBindingDisplayString(actionName, bindingIndex);
                UpdateDisplayText(string.IsNullOrEmpty(bindingDisplay) ? defaultText : bindingDisplay);
                SetInteractable(true);
            }
            catch (System.Exception ex)
            {
                LogError($"Error updating display: {ex.Message}");
                UpdateDisplayText(invalidActionText);
                SetInteractable(false);
            }
        }
        
        private void UpdateDisplayText(string text)
        {
            if (displayText != null)
            {
                displayText.text = text;
            }
        }
        
        private void SetInteractable(bool interactable)
        {
            if (rebindButton != null)
            {
                rebindButton.interactable = interactable && !_isRebinding;
            }
        }
        
        private void SetRebindingState(bool rebinding)
        {
            _isRebinding = rebinding;
            
            if (_isRebinding)
            {
                UpdateDisplayText(rebindingText);
            }
            else
            {
                UpdateDisplay();
            }
            
            SetInteractable(!_isRebinding);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnRebindStarted(string eventActionName)
        {
            if (eventActionName != actionName) return;
            
            LogDebug($"Rebind started for action '{eventActionName}'");
            SetRebindingState(true);
        }
        
        private void OnRebindCompleted(string eventActionName)
        {
            if (eventActionName != actionName) return;
            
            LogDebug($"Rebind completed for action '{eventActionName}'");
            SetRebindingState(false);
        }
        
        private void OnRebindCancelled(string eventActionName)
        {
            if (eventActionName != actionName) return;
            
            LogDebug($"Rebind cancelled for action '{eventActionName}'");
            SetRebindingState(false);
        }
        
        private void OnRebindFailed(string eventActionName, string error)
        {
            if (eventActionName != actionName) return;
            
            LogError($"Rebind failed for action '{eventActionName}': {error}");
            SetRebindingState(false);
        }
        
        private void OnConfigurationChanged(InputConfigurationChangedEventArgs eventArgs)
        {
            LogDebug("Input configuration changed - updating display");
            UpdateDisplay();
        }
        
        #endregion
        
        #region Logging
        
        private void LogDebug(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"[EnhancedInputRebindUI] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[EnhancedInputRebindUI] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[EnhancedInputRebindUI] {message}");
        }
        
        #endregion
        
        #region Editor Support
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            bindingIndex = Mathf.Max(0, bindingIndex);
            
            // Update display in editor
            if (Application.isPlaying && _isInitialized)
            {
                UpdateDisplay();
            }
        }
        #endif
        
        #endregion
    }
}
