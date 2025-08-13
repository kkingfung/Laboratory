using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
// FIXME: tidyup after 8/29
[RequireComponent(typeof(Button))]
public class UIButtonHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Tooltip("Duration in seconds to consider as a hold.")]
    [SerializeField] private float holdDuration = 1.0f;

    /// <summary>Called once when hold starts (on pointer down).</summary>
    public event Action? HoldStarted;

    /// <summary>Called once when hold completes (held for holdDuration).</summary>
    public event Action? HoldCompleted;

    /// <summary>Called if hold is canceled before completion.</summary>
    public event Action? HoldCanceled;

    private bool isPointerDown = false;
    private float pointerDownTime;
    private bool holdTriggered = false;

    [SerializeField] private InputActionReference holdActionRef = null!;
    
    private void OnEnable()
    {
        if (holdActionRef != null)
        {
            holdActionRef.action.started += HoldStarted;
            holdActionRef.action.canceled += HoldCanceled;
            holdActionRef.action.performed += HoldCompleted; // optional
            holdActionRef.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (holdActionRef != null)
        {
            holdActionRef.action.started -= HoldStarted;
            holdActionRef.action.canceled -= HoldCanceled;
            holdActionRef.action.performed -= HoldCompleted;
            holdActionRef.action.Disable();
        }
        ResetHold();
    }

    private void Update()
    {
        if (isPointerDown && !holdTriggered)
        {
            if (Time.unscaledTime - pointerDownTime >= holdDuration)
            {
                holdTriggered = true;
                HoldCompleted?.Invoke();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        pointerDownTime = Time.unscaledTime;
        holdTriggered = false;
        HoldStarted?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPointerDown && !holdTriggered)
        {
            HoldCanceled?.Invoke();
        }
        isPointerDown = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPointerDown && !holdTriggered)
        {
            HoldCanceled?.Invoke();
        }
        isPointerDown = false;
    }
}
