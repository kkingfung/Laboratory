using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

public class ActionInputHandler : MonoBehaviour
{
    [Header("Input Settings")]
    public InputAction interactAction;
    public float longPressThreshold = 0.5f; // seconds before long press starts
    public float longPressRepeatRate = 0.1f; // how often long press triggers while holding

    public event Action OnClick;
    public event Action OnLongPressStart;
    public event Action OnLongPressHold;

    private Coroutine longPressCoroutine;

    private void OnEnable()
    {
        interactAction.Enable();
        interactAction.performed += HandlePress;
        interactAction.canceled += HandleRelease;
    }

    private void OnDisable()
    {
        interactAction.performed -= HandlePress;
        interactAction.canceled -= HandleRelease;
        interactAction.Disable();
    }

    private void HandlePress(InputAction.CallbackContext context)
    {
        // Start the coroutine for long press detection
        if (longPressCoroutine != null)
            StopCoroutine(longPressCoroutine);

        longPressCoroutine = StartCoroutine(LongPressRoutine());
    }

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
}
