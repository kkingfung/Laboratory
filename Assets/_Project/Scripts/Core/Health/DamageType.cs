namespace Laboratory.Core.Health
{
    /// <summary>
    /// Types of damage that can be inflicted on entities.
    /// </summary>
    public enum DamageType
    {
        /// <summary>Standard physical damage.</summary>
        Normal = 0,
        
        /// <summary>Fire-based elemental damage.</summary>
        Fire = 1,
        
        /// <summary>Ice-based elemental damage.</summary>
        Ice = 2,
        
        /// <summary>Lightning-based elemental damage.</summary>
        Lightning = 3,
        
        /// <summary>Poison damage over time.</summary>
        Poison = 4,
        
        /// <summary>Explosion/blast damage.</summary>
        Explosive = 5,
        
        /// <summary>Fall damage from heights.</summary>
        Fall = 6,
        
        /// <summary>Environmental hazard damage.</summary>
        Environmental = 7,
        
        /// <summary>Critical hit with increased damage.</summary>
        Critical = 8,
        
        /// <summary>Healing (negative damage).</summary>
        Healing = 9
    }
}
