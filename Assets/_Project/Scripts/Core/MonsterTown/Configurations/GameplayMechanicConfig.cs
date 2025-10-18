using System;
using UnityEngine;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Types of gameplay mechanics
    /// </summary>
    public enum MechanicType
    {
        BonusMultiplier,
        ResourceGeneration,
        SpecialAbility,
        StatBoost,
        EfficiencyBonus,
        RarityBonus
    }

    /// <summary>
    /// Gameplay mechanic definition
    /// </summary>
    [Serializable]
    public struct GameplayMechanic
    {
        public MechanicType mechanicType;
        public float influence;
        public string description;
    }
}