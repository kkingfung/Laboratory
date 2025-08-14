using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Laboratory.UI.Utils
{
    /// <summary>
    /// Prevents rapid consecutive button clicks by implementing a debounce mechanism.
    /// Automatically attaches to Button components and adds a minimum delay between valid clicks.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonDebouncer : MonoBehaviour
    {
        #region Fields

        [Header("Debounce Configuration")]
        [Tooltip("Button to apply debounce to.")]
        [SerializeField] private Button button = null!;

        [Tooltip("Minimum time in seconds between consecutive button clicks.")]
        [SerializeField] private float debounceTime = 0.5f;

        [Tooltip("Optional UnityEvent invoked when a click is ignored due to debounce.")]
        public UnityEvent onDebouncedClick;

        private float _lastClickTime = -Mathf.Infinity;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize the button reference and add click listener.
        /// </summary>
        private void Awake()
        {
            if (button == null)
            {
                Debug.LogError($"{nameof(ButtonDebouncer)} requires a Button assigned in the Inspector.", this);
                enabled = false;
                return;
            }

            button.onClick.AddListener(OnButtonClicked);
        }

        /// <summary>
        /// Clean up event listeners to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnButtonClicked);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles button click events with debounce logic.
        /// Ignores clicks that occur within the debounce time window.
        /// </summary>
        private void OnButtonClicked()
        {
            if (Time.unscaledTime - _lastClickTime < debounceTime)
            {
                // Ignore click - debounced
                onDebouncedClick?.Invoke();
                return;
            }

            _lastClickTime = Time.unscaledTime;
            // Let the button proceed with normal click events
            // Note: If you want to intercept before other listeners,
            // consider adding this script earlier or controlling invocation order.
        }

        #endregion
    }
}
