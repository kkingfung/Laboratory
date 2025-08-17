using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Laboratory.Core.Input
{
    /// <summary>
    /// Handles input actions for click and long press behaviors.
    /// Provides events for click, long press start, and long press hold actions.
    /// </summary>
    public class ActionInputHandler : MonoBehaviour
    {
        #region Fields

        [Header("Input Settings")]
        [Tooltip("Input action for interaction")]
        [SerializeField] private InputAction _interactAction;
        
        [Tooltip("Time threshold in seconds before a long press starts")]
        [SerializeField] private float _longPressThreshold = 0.5f;
        
        [Tooltip("How often long press triggers while holding in seconds")]
        [SerializeField] private float _longPressRepeatRate = 0.1f;

        private Coroutine _longPressCoroutine;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a click action is detected (short press)
        /// </summary>
        public event Action OnClick;
        
        /// <summary>
        /// Fired when a long press action starts
        /// </summary>
        public event Action OnLongPressStart;
        
        /// <summary>
        /// Fired repeatedly while long press is held
        /// </summary>
        public event Action OnLongPressHold;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called when the component is enabled
        /// </summary>
        private void OnEnable()
        {
            _interactAction.Enable();
            _interactAction.performed += HandlePress;
            _interactAction.canceled += HandleRelease;
        }

        /// <summary>
        /// Called when the component is disabled
        /// </summary>
        private void OnDisable()
        {
            _interactAction.performed -= HandlePress;
            _interactAction.canceled -= HandleRelease;
            _interactAction.Disable();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the press event from the input action
        /// </summary>
        /// <param name="context">Input callback context</param>
        private void HandlePress(InputAction.CallbackContext context)
        {
            if (_longPressCoroutine != null)
                StopCoroutine(_longPressCoroutine);

            _longPressCoroutine = StartCoroutine(LongPressRoutine());
        }

        /// <summary>
        /// Handles the release event from the input action
        /// </summary>
        /// <param name="context">Input callback context</param>
        private void HandleRelease(InputAction.CallbackContext context)
        {
            if (_longPressCoroutine != null)
            {
                StopCoroutine(_longPressCoroutine);
                _longPressCoroutine = null;

                // If released before long press threshold, it's a click
                OnClick?.Invoke();
            }
        }

        /// <summary>
        /// Coroutine that handles long press detection and repetition
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator LongPressRoutine()
        {
            // Wait until long press threshold
            yield return new WaitForSeconds(_longPressThreshold);

            // Long press started
            OnLongPressStart?.Invoke();

            // Repeat while holding
            while (_interactAction.ReadValue<float>() > 0)
            {
                OnLongPressHold?.Invoke();
                yield return new WaitForSeconds(_longPressRepeatRate);
            }
        }

        #endregion
    }
}
