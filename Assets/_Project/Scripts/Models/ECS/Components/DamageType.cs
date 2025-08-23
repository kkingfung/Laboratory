// This file provides backward compatibility for the old ECS DamageType.
// The unified DamageType is now located at Laboratory.Core.Health.DamageType
//
// Migration Guide:
// Change your using statements from:
//   using Laboratory.Models.ECS.Components;
// To:
//   using Laboratory.Core.Health;
//
// The enum values remain the same, but the new implementation has better
// organization and is integrated with the unified health system.

// Type alias to maintain compatibility during migration
using DamageTypeAlias = Laboratory.Core.Health.DamageType;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Backward compatibility alias for the unified DamageType.
    /// Please update your code to use Laboratory.Core.Health.DamageType directly.
    /// </summary>
    [System.Obsolete("Use Laboratory.Core.Health.DamageType instead.")]
    public enum DamageType
    {
        /// <summary>Standard physical damage.</summary>
        Physical = DamageTypeAlias.Normal,
        
        /// <summary>Magical damage type.</summary>
        Magical = DamageTypeAlias.Normal,
        
        /// <summary>Fire elemental damage.</summary>
        Fire = DamageTypeAlias.Fire,
        
        /// <summary>Ice elemental damage.</summary>
        Ice = DamageTypeAlias.Ice,
        
        /// <summary>Lightning elemental damage.</summary>
        Lightning = DamageTypeAlias.Lightning,
        
        /// <summary>Poison damage over time.</summary>
        Poison = DamageTypeAlias.Poison,
        
        /// <summary>Healing (negative damage).</summary>
        Healing = DamageTypeAlias.Healing
    }
}
