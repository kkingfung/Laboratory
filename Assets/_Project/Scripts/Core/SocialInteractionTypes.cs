using UnityEngine;

namespace Laboratory.Core
{
    /// <summary>
    /// Shared enumeration for social interaction types between creatures
    /// </summary>
    public enum SocialInteractionType
    {
        Neutral,
        Cooperation,
        Conflict,
        Play,
        Mating,
        Territorial,
        Feeding,
        Grooming,
        Protection,
        Competition
    }

    /// <summary>
    /// Shared data structure for social interaction events
    /// </summary>
    [System.Serializable]
    public struct SocialInteractionData
    {
        public uint creatureA;
        public uint creatureB;
        public SocialInteractionType interactionType;
        public float intensity;
        public float duration;
        public Vector3 location;
        public float timestamp;
    }

    /// <summary>
    /// Shared data structure for creature mood state
    /// </summary>
    [System.Serializable]
    public struct MoodState
    {
        public float happiness;
        public float excitement;
        public float stress;
        public float confidence;
        public float curiosity;
        public float socialNeed;
        public float timestamp;
    }
}