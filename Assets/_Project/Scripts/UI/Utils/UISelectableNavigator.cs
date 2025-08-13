using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
// FIXME: tidyup after 8/29
public class UISelectableNavigator : MonoBehaviour
{
    [Tooltip("List of Selectable UI elements in navigation order.")]
    [SerializeField] private List<Selectable> selectables = new List<Selectable>();

    [Tooltip("Enable wrap-around navigation (from last to first and vice versa).")]
    [SerializeField] private bool wrapAround = true;

    [Tooltip("Input axis or keys to navigate UI (horizontal).")]
    [SerializeField] private string horizontalAxis = "Horizontal";

    [Tooltip("Input axis or keys to navigate UI (vertical).")]
    [SerializeField] private string verticalAxis = "Vertical";

    [Tooltip("Time delay between navigation inputs to prevent overscrolling.")]
    [SerializeField] private float inputDelay = 0.2f;

    private int currentIndex = 0;
    private float inputTimer = 0f;

    private void Start()
    {
        if (selectables.Count == 0)
        {
            Debug.LogWarning("UISelectableNavigator: No selectables assigned.");
            return;
        }

        // Set first selectable as selected on start
        SetSelected(currentIndex);
    }

    private void Update()
    {
        if (selectables.Count == 0) return;

        inputTimer -= Time.unscaledDeltaTime;

        if (inputTimer > 0f) return;

        float verticalInput = Input.GetAxisRaw(verticalAxis);
        float horizontalInput = Input.GetAxisRaw(horizontalAxis);

        if (Mathf.Abs(verticalInput) > 0.5f)
        {
            Navigate(verticalInput > 0 ? -1 : 1);
        }
        else if (Mathf.Abs(horizontalInput) > 0.5f)
        {
            Navigate(horizontalInput > 0 ? 1 : -1);
        }
    }

    private void Navigate(int direction)
    {
        currentIndex += direction;

        if (wrapAround)
        {
            if (currentIndex < 0) currentIndex = selectables.Count - 1;
            else if (currentIndex >= selectables.Count) currentIndex = 0;
        }
        else
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, selectables.Count - 1);
        }

        SetSelected(currentIndex);

        inputTimer = inputDelay;
    }

    private void SetSelected(int index)
    {
        Selectable selectable = selectables[index];
        if (selectable != null && selectable.IsInteractable() && selectable.gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
