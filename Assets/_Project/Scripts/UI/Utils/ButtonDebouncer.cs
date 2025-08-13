using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
// FIXME: tidyup after 8/29
[RequireComponent(typeof(Button))]
public class ButtonDebouncer : MonoBehaviour
{
    [Tooltip("Minimum time in seconds between consecutive button clicks.")]
    [SerializeField] private float debounceTime = 0.5f;

    [Tooltip("Optional UnityEvent invoked when a click is ignored due to debounce.")]
    public UnityEvent? onDebouncedClick;

    private Button button;
    private float lastClickTime = -Mathf.Infinity;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (Time.unscaledTime - lastClickTime < debounceTime)
        {
            // Ignore click - debounced
            onDebouncedClick?.Invoke();
            return;
        }

        lastClickTime = Time.unscaledTime;
        // Let the button proceed with normal click events
        // Note: If you want to intercept before other listeners,
        // consider adding this script earlier or controlling invocation order.
    }
}
