using UnityEngine;
using System;
using Laboratory.Core;

namespace Laboratory.Systems
{
    /// <summary>
    /// Interface for personality management within Systems assembly
    /// This allows Systems to work without depending on Laboratory.AI.Personality
    /// </summary>
    public interface IPersonalityManager
    {
        event Action<uint, uint, SocialInteractionType> OnSocialInteraction;
        event Action<uint, MoodState> OnMoodChanged;
        int ActivePersonalityCount { get; }
    }

    /// <summary>
    /// Default implementation that can be used when personality system is not available
    /// </summary>
    public class DefaultPersonalityManager : MonoBehaviour, IPersonalityManager
    {
        private static DefaultPersonalityManager instance;
        public static DefaultPersonalityManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("DefaultPersonalityManager");
                    instance = go.AddComponent<DefaultPersonalityManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public event Action<uint, uint, SocialInteractionType> OnSocialInteraction;
        public event Action<uint, MoodState> OnMoodChanged;

        public int ActivePersonalityCount => 0; // Default implementation returns 0

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void TriggerSocialInteraction(uint creatureA, uint creatureB, SocialInteractionType interactionType)
        {
            OnSocialInteraction?.Invoke(creatureA, creatureB, interactionType);
        }

        public void TriggerMoodChange(uint creatureId, MoodState newMood)
        {
            OnMoodChanged?.Invoke(creatureId, newMood);
        }
    }

    /// <summary>
    /// Static accessor that automatically uses the real personality manager if available,
    /// or falls back to default implementation
    /// </summary>
    public static class PersonalityManager
    {
        public static IPersonalityManager GetInstance()
        {
            // Try to get the real personality manager first
            var realManager = GameObject.FindFirstObjectByType<MonoBehaviour>()?.GetComponent("CreaturePersonalityManager") as IPersonalityManager;
            if (realManager != null)
                return realManager;

            // Fall back to default implementation
            return DefaultPersonalityManager.Instance;
        }
    }
}