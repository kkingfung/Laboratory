using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Laboratory.Gameplay.Audio;
// FIXME: tidyup after 8/29
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(AudioSource))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    [SerializeField] private AudioManager.UISound hoverSound = AudioManager.UISound.ButtonHover;
    [SerializeField] private AudioManager.UISound clickSound = AudioManager.UISound.ButtonClick;
    [SerializeField] private AudioManager.UISound downSound = AudioManager.UISound.ButtonDown;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitch = 1f;

    private AudioSource? _audioSource;
    private Button? _button;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _button = GetComponent<Button>();

        _button?.onClick.AddListener(PlayClickSound);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySound(hoverSound);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PlaySound(downSound);
    }

    private void PlayClickSound()
    {
        PlaySound(clickSound);
    }

    private void PlaySound(AudioClip? clip)
    {
        if (clip == null || _audioSource == null) return;

        _audioSource.pitch = pitch;
        _audioSource.volume = volume;
        _audioSource.PlayOneShot(clip);
    }

    private void OnDestroy()
    {
        _button?.onClick.RemoveListener(PlayClickSound);
    }
}
