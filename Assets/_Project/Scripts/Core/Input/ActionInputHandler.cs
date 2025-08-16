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
        [SerializeField] private InputAction interactAction;
        
        [Tooltip("Time threshold in seconds before a long press starts")]
        [SerializeField] private float longPressThreshold = 0.5f;
        
        [Tooltip("How often long press triggers while holding in seconds")]
        [SerializeField] private float longPressRepeatRate = 0.1f;

        private Coroutine longPressCoroutine;

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
            interactAction.Enable();
            interactAction.performed += HandlePress;
            interactAction.canceled += HandleRelease;
        }

        /// <summary>
        /// Called when the component is disabled
        /// </summary>
        private void OnDisable()
        {
            interactAction.performed -= HandlePress;
            interactAction.canceled -= HandleRelease;
            interactAction.Disable();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles the press event from the input action
        /// </summary>
        /// <param name="context">Input callback context</param>
        private void HandlePress(InputAction.CallbackContext context)
        {
            if (longPressCoroutine != null)
                StopCoroutine(longPressCoroutine);

            longPressCoroutine = StartCoroutine(LongPressRoutine());
        }

        /// <summary>
        /// Handles the release event from the input action
        /// </summary>
        /// <param name="context">Input callback context</param>
        private void HandleRelease(InputAction.CallbackContext context)
        {
            if (longPressCoroutine != null)
            {
                StopCoroutine(longPressCoroutine);
                longPressCoroutine = null;

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
            yield return new WaitForSeconds(longPressThreshold);

            // Long press started
            OnLongPressStart?.Invoke();

            // Repeat while holding
            while (interactAction.ReadValue<float>() > 0)
            {
                OnLongPressHold?.Invoke();
                yield return new WaitForSeconds(longPressRepeatRate);
            }
        }

        #endregion
    }
}
