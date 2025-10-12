using System;
using System.Collections;
using UnityEngine;
using Laboratory.Core.Input.Interfaces;
using Laboratory.Core.Input.Events;
using Laboratory.Infrastructure.Core;

namespace Laboratory.Core.Input
{
    /// <summary>
    /// Unified input handler that consolidates all input functionality.
    /// Replaces ActionInputHandler and EnhancedActionInputHandler with a single, comprehensive implementation.
    /// Integrates with the unified input system and provides click, long press, and hold behaviors.
    /// </summary>
    public class EnhancedInputHandler : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Input Configuration")]
        [Tooltip("Name of the input action to monitor")]
        [SerializeField] private string actionName = "ActionOrCraft";
        
        [Tooltip("Time threshold in seconds before a long press starts")]
        [SerializeField] private float longPressThreshold = 0.5f;
        
        [Tooltip("How often long press triggers while holding in seconds")]
        [SerializeField] private float longPressRepeatRate = 0.1f;
        
        [Tooltip("Whether to use input buffering for this action")]
        [SerializeField] private bool useInputBuffering = true;

        [Header("Debug")]
        [SerializeField] private bool debugLogging = false;

        #endregion

        #region Private Fields

        private IInputService _inputService;
        private Coroutine _longPressCoroutine;
        private bool _isPressed = false;
        private float _pressStartTime;
        private bool _isInitialized = false;

        #endregion

        #region Events

        /// <summary>Fired when a click action is detected (short press)</summary>
        public event Action OnClick;
        
        /// <summary>Fired when a long press action starts</summary>
        public event Action OnLongPressStart;
        
        /// <summary>Fired repeatedly while long press is held</summary>
        public event Action OnLongPressHold;
        
        /// <summary>Fired when any press starts (immediate response)</summary>
        public event Action OnPressStart;
        
        /// <summary>Fired when any press ends</summary>
        public event Action OnPressEnd;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                SubscribeToEvents();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            StopLongPressCoroutine();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            StopLongPressCoroutine();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Changes the action name being monitored at runtime.
        /// </summary>
        /// <param name="newActionName">New action name to monitor</param>
        public void SetActionName(string newActionName)
        {
            if (string.IsNullOrEmpty(newActionName))
            {
                LogWarning("Attempted to set empty action name");
                return;
            }

            UnsubscribeFromEvents();
            actionName = newActionName;
            
            if (_isInitialized)
            {
                SubscribeToEvents();
            }
            
            LogDebug($"Action name changed to: {actionName}");
        }

        /// <summary>
        /// Updates the long press threshold at runtime.
        /// </summary>
        /// <param name="threshold">New threshold in seconds</param>
        public void SetLongPressThreshold(float threshold)
        {
            longPressThreshold = Mathf.Max(0.1f, threshold);
            LogDebug($"Long press threshold set to: {longPressThreshold}s");
        }

        /// <summary>
        /// Updates the long press repeat rate at runtime.
        /// </summary>
        /// <param name="repeatRate">New repeat rate in seconds</param>
        public void SetLongPressRepeatRate(float repeatRate)
        {
            longPressRepeatRate = Mathf.Max(0.05f, repeatRate);
            LogDebug($"Long press repeat rate set to: {longPressRepeatRate}s");
        }

        /// <summary>
        /// Gets whether the action is currently being pressed.
        /// </summary>
        /// <returns>True if the action is pressed</returns>
        public bool IsPressed()
        {
            return _inputService?.IsActionPressed(actionName) ?? false;
        }

        /// <summary>
        /// Gets whether the action was performed this frame.
        /// </summary>
        /// <returns>True if the action was performed this frame</returns>
        public bool WasPerformed()
        {
            return _inputService?.WasActionPerformed(actionName) ?? false;
        }

        /// <summary>
        /// Gets the current press duration if the action is being held.
        /// </summary>
        /// <returns>Press duration in seconds, or 0 if not pressed</returns>
        public float GetPressDuration()
        {
            return _isPressed ? (Time.unscaledTime - _pressStartTime) : 0f;
        }

        /// <summary>
        /// Manually trigger a click event (useful for testing or external triggers).
        /// </summary>
        public void TriggerClick()
        {
            OnClick?.Invoke();
            LogDebug($"Click manually triggered for action '{actionName}'");
        }

        /// <summary>
        /// Manually trigger a long press start event.
        /// </summary>
        public void TriggerLongPressStart()
        {
            OnLongPressStart?.Invoke();
            LogDebug($"Long press start manually triggered for action '{actionName}'");
        }

        /// <summary>
        /// Gets configuration information for this input handler.
        /// </summary>
        /// <returns>Configuration data structure</returns>
        public InputHandlerConfig GetConfiguration()
        {
            return new InputHandlerConfig
            {
                ActionName = actionName,
                LongPressThreshold = longPressThreshold,
                LongPressRepeatRate = longPressRepeatRate,
                UseInputBuffering = useInputBuffering,
                IsInitialized = _isInitialized,
                IsPressed = _isPressed
            };
        }

        #endregion

        #region Private Methods

        private void Initialize()
        {
            try
            {
                // Get input service from DI container
                var serviceContainer = ServiceContainer.Instance;
                if (serviceContainer != null)
                {
                    _inputService = serviceContainer.ResolveService<IInputService>();
                }

                if (_inputService == null)
                {
                    LogError("Failed to get IInputService from service container");
                    return;
                }

                if (!_inputService.HasAction(actionName))
                {
                    LogError($"Action '{actionName}' not found in input system");
                    return;
                }

                _isInitialized = true;
                SubscribeToEvents();
                
                LogDebug($"EnhancedInputHandler initialized for action: {actionName}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize: {ex.Message}");
            }
        }

        private void SubscribeToEvents()
        {
            if (_inputService == null || !_isInitialized) return;

            try
            {
                // Subscribe to unified input events
                InputEvents.OnActionPressed += OnActionPressed;
                InputEvents.OnActionReleased += OnActionReleased;
                
                LogDebug("Subscribed to input events");
            }
            catch (Exception ex)
            {
                LogError($"Failed to subscribe to events: {ex.Message}");
            }
        }

        private void UnsubscribeFromEvents()
        {
            try
            {
                InputEvents.OnActionPressed -= OnActionPressed;
                InputEvents.OnActionReleased -= OnActionReleased;
                
                LogDebug("Unsubscribed from input events");
            }
            catch (Exception ex)
            {
                LogError($"Failed to unsubscribe from events: {ex.Message}");
            }
        }

        private void OnActionPressed(ActionInputEventArgs eventArgs)
        {
            if (eventArgs.ActionName != actionName || !eventArgs.IsPressed) return;

            _isPressed = true;
            _pressStartTime = Time.unscaledTime;

            OnPressStart?.Invoke();
            LogDebug($"Action '{actionName}' pressed");

            // Start long press detection
            StopLongPressCoroutine();
            _longPressCoroutine = StartCoroutine(LongPressDetectionCoroutine());
        }

        private void OnActionReleased(ActionInputEventArgs eventArgs)
        {
            if (eventArgs.ActionName != actionName || eventArgs.IsPressed) return;

            var wasPressed = _isPressed;
            var pressDuration = GetPressDuration();
            
            _isPressed = false;

            OnPressEnd?.Invoke();
            LogDebug($"Action '{actionName}' released (duration: {pressDuration:F3}s)");

            // Stop long press detection
            StopLongPressCoroutine();

            // If released before long press threshold, it's a click
            if (wasPressed && pressDuration < longPressThreshold)
            {
                OnClick?.Invoke();
                LogDebug($"Click detected for action '{actionName}'");
            }
        }

        private IEnumerator LongPressDetectionCoroutine()
        {
            // Wait for long press threshold
            yield return new WaitForSecondsRealtime(longPressThreshold);

            if (_isPressed)
            {
                OnLongPressStart?.Invoke();
                LogDebug($"Long press started for action '{actionName}'");

                // Continue triggering hold events
                while (_isPressed)
                {
                    OnLongPressHold?.Invoke();
                    yield return new WaitForSecondsRealtime(longPressRepeatRate);
                }
            }

            _longPressCoroutine = null;
        }

        private void StopLongPressCoroutine()
        {
            if (_longPressCoroutine != null)
            {
                StopCoroutine(_longPressCoroutine);
                _longPressCoroutine = null;
            }
        }

        #endregion

        #region Logging

        private void LogDebug(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"[EnhancedInputHandler] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[EnhancedInputHandler] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[EnhancedInputHandler] {message}");
        }

        #endregion

        #region Editor Support

        #if UNITY_EDITOR
        private void OnValidate()
        {
            longPressThreshold = Mathf.Max(0.1f, longPressThreshold);
            longPressRepeatRate = Mathf.Max(0.05f, longPressRepeatRate);
        }

        [ContextMenu("Test Click")]
        private void TestClick()
        {
            TriggerClick();
        }

        [ContextMenu("Test Long Press Start")]
        private void TestLongPressStart()
        {
            TriggerLongPressStart();
        }
        #endif

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Configuration data for input handler.
    /// </summary>
    [System.Serializable]
    public struct InputHandlerConfig
    {
        public string ActionName;
        public float LongPressThreshold;
        public float LongPressRepeatRate;
        public bool UseInputBuffering;
        public bool IsInitialized;
        public bool IsPressed;
    }

    #endregion
}