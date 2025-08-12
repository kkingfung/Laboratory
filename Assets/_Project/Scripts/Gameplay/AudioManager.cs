using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; } = null!;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource = null!;
    [SerializeField] private AudioSource sfxSource = null!;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer = null!;

    [Header("UI Clips")]
    [SerializeField] private List<AudioClip> uiClips = new();

    public enum UISound
    {
        ButtonClick,
        ButtonHover,
        Confirmation,
        Error,
    }

    private Dictionary<UISound, AudioClip> uiClipMap = new();

    private const string MixerParamMaster = "MasterVolume";
    private const string MixerParamMusic = "MusicVolume";
    private const string MixerParamSFX = "SFXVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < Mathf.Min(uiClips.Count, Enum.GetValues(typeof(UISound)).Length); i++)
        {
            uiClipMap[(UISound)i] = uiClips[i];
        }
    }

    #region UI Sound Playback

    public void PlayUISound(UISound sound)
    {
        if (uiClipMap.TryGetValue(sound, out var clip))
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"UI Sound {sound} not found!");
        }
    }

    #endregion

    #region Volume Controls (0 to 1 range)

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat(MixerParamMaster, LinearToDecibel(volume));
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat(MixerParamMusic, LinearToDecibel(volume));
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat(MixerParamSFX, LinearToDecibel(volume));
    }

    public float GetMasterVolume()
    {
        audioMixer.GetFloat(MixerParamMaster, out float value);
        return DecibelToLinear(value);
    }

    public float GetMusicVolume()
    {
        audioMixer.GetFloat(MixerParamMusic, out float value);
        return DecibelToLinear(value);
    }

    public float GetSFXVolume()
    {
        audioMixer.GetFloat(MixerParamSFX, out float value);
        return DecibelToLinear(value);
    }

    private float LinearToDecibel(float linear)
    {
        return linear <= 0f ? -80f : 20f * Mathf.Log10(linear);
    }

    private float DecibelToLinear(float db)
    {
        return Mathf.Pow(10f, db / 20f);
    }

    #endregion
}
