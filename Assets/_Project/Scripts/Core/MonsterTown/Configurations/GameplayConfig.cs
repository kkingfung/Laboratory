using System;
using UnityEngine;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Genre-specific gameplay configuration
    /// </summary>
    [Serializable]
    public struct GenreGameplayConfig
    {
        public GameplayMechanic[] mechanics;
        public float baseSuccessRate;
        public float skillCeiling;
        public int maxAttempts;
        public bool allowRetries;
    }

    /// <summary>
    /// Stat weight for performance calculation
    /// </summary>
    [Serializable]
    public struct StatWeight
    {
        public StatType statType;
        [Range(0f, 1f)] public float weight;
        public string description;
    }
}