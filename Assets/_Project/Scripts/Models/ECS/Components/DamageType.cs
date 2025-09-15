using Unity.Entities;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Types of damage in the game
    /// </summary>
    public enum DamageType
    {
        Normal = 0,
        Critical = 1,
        Physical = 2,
        Fire = 3,
        Ice = 4,
        Lightning = 5,
        Poison = 6,
        Explosive = 7,
        Piercing = 8,
        Magic = 9,
        Fall = 10,
        Drowning = 11
    }
    
    /// <summary>
    /// Combat-related damage type extensions
    /// </summary>
    public static class DamageTypeExtensions
    {
        public static string GetDisplayName(this DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Normal => "Normal",
                DamageType.Critical => "Critical",
                DamageType.Physical => "Physical",
                DamageType.Fire => "Fire",
                DamageType.Ice => "Ice", 
                DamageType.Lightning => "Lightning",
                DamageType.Poison => "Poison",
                DamageType.Explosive => "Explosive",
                DamageType.Piercing => "Piercing",
                DamageType.Magic => "Magic",
                DamageType.Fall => "Fall Damage",
                DamageType.Drowning => "Drowning",
                _ => "Unknown"
            };
        }
        
        public static UnityEngine.Color GetDamageColor(this DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Normal => UnityEngine.Color.white,
                DamageType.Critical => new UnityEngine.Color(1f, 0.5f, 0f),
                DamageType.Physical => UnityEngine.Color.gray,
                DamageType.Fire => UnityEngine.Color.red,
                DamageType.Ice => UnityEngine.Color.cyan,
                DamageType.Lightning => UnityEngine.Color.yellow,
                DamageType.Poison => UnityEngine.Color.green,
                DamageType.Explosive => UnityEngine.Color.magenta,
                DamageType.Piercing => new UnityEngine.Color(0.8f, 0.8f, 0.8f),
                DamageType.Magic => new UnityEngine.Color(0.5f, 0f, 1f),
                DamageType.Fall => UnityEngine.Color.blue,
                DamageType.Drowning => new UnityEngine.Color(0f, 0.5f, 1f),
                _ => UnityEngine.Color.white
            };
        }
    }
}
