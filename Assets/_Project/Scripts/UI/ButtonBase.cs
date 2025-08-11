using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public abstract class ButtonBase : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private Button targetButton; // The UI Button component
    [SerializeField] private AudioClip clickSound; // Sound effect for click
    [SerializeField] private float longPressDuration = 1.0f; // Duration to trigger a long press

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource; // AudioSource to play the sound effect

    private bool isPressing = false;
    private float pressTime = 0.0f;

    protected virtual void Start()
    {
        if (targetButton == null)
        {
            Debug.LogError("UIButton: Target Button is not assigned.");
            return;
        }

        // Assign the click event
        targetButton.onClick.AddListener(OnClick);
    }

    private void Update()
    {
        if (isPressing)
        {
            pressTime += Time.deltaTime;

            // Trigger long press action if the duration is met
            if (pressTime >= longPressDuration)
            {
                isPressing = false; // Stop tracking the press
                OnLongPress();
            }
        }
    }

    private void OnDestroy()
    {
        if (targetButton != null)
        {
            targetButton.onClick.RemoveListener(OnClick);
        }
    }

    /// <summary>
    /// Called when the button is clicked.
    /// </summary>
    private void OnClick()
    {
        PlayClickSound();
        PerformClickAction();
    }

    /// <summary>
    /// Called when the button is pressed down.
    /// </summary>
    public void OnPointerDown()
    {
        isPressing = true;
        pressTime = 0.0f;
    }

    /// <summary>
    /// Called when the button is released.
    /// </summary>
    public void OnPointerUp()
    {
        isPressing = false;
        pressTime = 0.0f;
    }

    /// <summary>
    /// Play the click sound effect.
    /// </summary>
    private void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    /// <summary>
    /// Abstract method to define the click action.
    /// </summary>
    protected abstract void PerformClickAction();

    /// <summary>
    /// Abstract method to define the long press action.
    /// </summary>
    protected abstract void OnLongPress();
}