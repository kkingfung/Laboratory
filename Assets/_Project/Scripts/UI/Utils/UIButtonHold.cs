using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Laboratory.UI.Utils
{
    /// <summary>
    /// Provides hold-to-activate functionality for UI buttons with configurable duration.
    /// Supports both mouse/touch input and Input System actions.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        #region Fields

        [Header("Hold Configuration")]
        [Tooltip("Duration in seconds to consider as a hold.")]
        [SerializeField] private float holdDuration = 1.0f;

        [Header("Input System (Optional)")]
        [Tooltip("Optional Input Action reference for hold input.")]
        [SerializeField] private InputActionReference holdActionRef;

        private bool _isPointerDown = false;
        private float _pointerDownTime;
        private bool _holdTriggered = false;

        #endregion

        #region Events

        /// <summary>Called once when hold starts (on pointer down).</summary>
        public event Action HoldStarted;

        /// <summary>Called once when hold completes (held for holdDuration).</summary>
        public event Action HoldCompleted;

        /// <summary>Called if hold is canceled before completion.</summary>
        public event Action HoldCanceled;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Enable Input System actions if configured.
        /// </summary>
        private void OnEnable()
        {
            if (holdActionRef != null)
            {
                holdActionRef.action.started += OnHoldActionStarted;
                holdActionRef.action.canceled += OnHoldActionCanceled;
                holdActionRef.action.performed += OnHoldActionPerformed;
                holdActionRef.action.Enable();
            }
        }

        /// <summary>
        /// Disable Input System actions and reset hold state.
        /// </summary>
        private void OnDisable()
        {
            if (holdActionRef != null)
            {
                holdActionRef.action.started -= OnHoldActionStarted;
                holdActionRef.action.canceled -= OnHoldActionCanceled;
                holdActionRef.action.performed -= OnHoldActionPerformed;
                holdActionRef.action.Disable();
            }
            ResetHold();
        }

        /// <summary>
        /// Check for hold completion during pointer down state.
        /// </summary>
        private void Update()
        {
            if (_isPointerDown && !_holdTriggered)
            {
                if (Time.unscaledTime - _pointerDownTime >= holdDuration)
                {
                    _holdTriggered = true;
                    HoldCompleted?.Invoke();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Handles pointer down events to start hold detection.
        /// </summary>
        /// <param name="eventData">Pointer event data</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            _isPointerDown = true;
            _pointerDownTime = Time.unscaledTime;
            _holdTriggered = false;
            HoldStarted?.Invoke();
        }

        /// <summary>
        /// Handles pointer up events to cancel hold if not completed.
        /// </summary>
        /// <param name="eventData">Pointer event data</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isPointerDown && !_holdTriggered)
            {
                HoldCanceled?.Invoke();
            }
            ResetHold();
        }

        /// <summary>
        /// Handles pointer exit events to cancel hold if not completed.
        /// </summary>
        /// <param name="eventData">Pointer event data</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isPointerDown && !_holdTriggered)
            {
                HoldCanceled?.Invoke();
            }
            ResetHold();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Resets the hold state to initial values.
        /// </summary>
        private void ResetHold()
        {
            _isPointerDown = false;
            _holdTriggered = false;
        }

        /// <summary>
        /// Handles Input System hold action started events.
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnHoldActionStarted(InputAction.CallbackContext context)
        {
            HoldStarted?.Invoke();
        }

        /// <summary>
        /// Handles Input System hold action canceled events.
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnHoldActionCanceled(InputAction.CallbackContext context)
        {
            HoldCanceled?.Invoke();
        }

        /// <summary>
        /// Handles Input System hold action performed events.
        /// </summary>
        /// <param name="context">Input action context</param>
        private void OnHoldActionPerformed(InputAction.CallbackContext context)
        {
            HoldCompleted?.Invoke();
        }

        #endregion
    }
}
