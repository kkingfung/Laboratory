using System;
using UnityEngine;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Reward tier configuration
    /// </summary>
    [Serializable]
    public struct RewardTier
    {
        public string tierName;
        [Range(0f, 1f)] public float minimumPerformance;
        public TownResourcesConfig rewards;
        public float experienceMultiplier;
        public string description;
    }
}