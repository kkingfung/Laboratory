using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages ragdoll-related sounds such as hits, collisions, and landings.
/// Can be called by PartialRagdollController or RagdollImpactEffect.
/// </summary>
public class RagdollSoundManager : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Clips played when bones are hit.")]
    [SerializeField] private List<AudioClip> hitClips;

    [Tooltip("Clips played when bones collide with environment.")]
    [SerializeField] private List<AudioClip> collisionClips;

    [Tooltip("Clips played when character lands.")]
    [SerializeField] private List<AudioClip> landingClips;

    [Header("Settings")]
    [Tooltip("Audio source used to play ragdoll sounds.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Volume for ragdoll sounds.")]
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    /// <summary>
    /// Play a random hit sound.
    /// </summary>
    public void PlayHitSound()
    {
        PlayRandomClip(hitClips);
    }

    /// <summary>
    /// Play a random collision sound.
    /// </summary>
    public void PlayCollisionSound()
    {
        PlayRandomClip(collisionClips);
    }

    /// <summary>
    /// Play a random landing sound.
    /// </summary>
    public void PlayLandingSound()
    {
        PlayRandomClip(landingClips);
    }

    private void PlayRandomClip(List<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0 || audioSource == null) return;

        int index = Random.Range(0, clips.Count);
        AudioClip clip = clips[index];
        audioSource.PlayOneShot(clip, volume);
    }
}
