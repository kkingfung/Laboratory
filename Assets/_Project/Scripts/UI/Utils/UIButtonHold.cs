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

    private bool _isPointerDown = false;
    private float _pointerDownTime;
    private bool _holdTriggered = false;

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
        if (_isPointerDown && !_holdTriggered)
        {
            if (Time.unscaledTime - _pointerDownTime >= holdDuration)
            {
                _holdTriggered = true;
                HoldCompleted?.Invoke();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPointerDown = true;
        _pointerDownTime = Time.unscaledTime;
        _holdTriggered = false;
        HoldStarted?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_isPointerDown && !_holdTriggered)
        {
            HoldCanceled?.Invoke();
        }
        _isPointerDown = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isPointerDown && !_holdTriggered)
        {
            HoldCanceled?.Invoke();
        }
        _isPointerDown = false;
    }
}
