using System;
using UnityEditor;
using UnityEngine;

public abstract class TweenAction : MonoBehaviour
{
    private bool isTweening = false;
    protected float elapsedTime = 0.0f; // Changed to protected for access in derived classes

    /// <summary>
    /// Start the tween action.
    /// </summary>
    public void StartTween()
    {
        isTweening = true;
        elapsedTime = 0.0f;
    }

    /// <summary>
    /// Stop the tween action.
    /// </summary>
    public void StopTween()
    {
        isTweening = false;
    }

    private void Update()
    {
        if (!isTweening) return;

        elapsedTime += Time.deltaTime;
        PerformTween(Time.deltaTime);
    }

    private void OnDestroy()
    {
        StopTween();
    }

    /// <summary>
    /// Abstract method to define the specific tweening behavior.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    protected abstract void PerformTween(float deltaTime);
}
