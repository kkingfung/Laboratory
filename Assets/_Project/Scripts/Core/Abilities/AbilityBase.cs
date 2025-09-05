using System;
using UnityEngine;

namespace Laboratory.Core.Abilities
{
    /// <summary>
    /// Base class for all abilities in the game
    /// </summary>
    public abstract class AbilityBase : ScriptableObject
    {
        [Header("Ability Info")]
        public string abilityName;
        public string description;
        public Sprite icon;
        
        [Header("Ability Properties")]
        public float cooldown = 1f;
        public float manaCost = 10f;
        public float castTime = 0f;
        public AbilityTargetType targetType = AbilityTargetType.Self;
        
        public abstract void Execute(GameObject caster, Vector3 targetPosition = default, GameObject target = null);
        public abstract bool CanExecute(GameObject caster);
    }

    public enum AbilityTargetType
    {
        Self,
        Enemy,
        Ally,
        Ground,
        Area
    }
}
