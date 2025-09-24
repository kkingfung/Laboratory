using System;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// Categories of genetic traits for Project Chimera monster breeding system
    /// Defines the different types of inheritable characteristics
    /// </summary>
    [Serializable]
    public enum TraitType
    {
        /// <summary>Physical appearance traits</summary>
        Physical = 0,
        
        /// <summary>Behavioral and personality traits</summary>
        Behavioral = 1,
        
        /// <summary>Combat and defensive abilities</summary>
        Combat = 2,
        
        /// <summary>Magical and elemental affinities</summary>
        Elemental = 3,
        
        /// <summary>Mental capabilities and intelligence</summary>
        Mental = 4,
        
        /// <summary>Movement and locomotion traits</summary>
        Movement = 5,
        
        /// <summary>Special abilities and unique powers</summary>
        Special = 6,
        
        /// <summary>Health, stamina, and vitality traits</summary>
        Vitality = 7,
        
        /// <summary>Environmental adaptation traits</summary>
        Environmental = 8,
        
        /// <summary>Social and breeding traits</summary>
        Social = 9,
        
        /// <summary>Rare mutation-only traits</summary>
        Mutation = 10,
        
        /// <summary>Hidden or dormant traits</summary>
        Hidden = 11,
        
        /// <summary>Magical and supernatural traits</summary>
        Magical = 12,

        /// <summary>Sensory abilities and perception traits</summary>
        Sensory = 13,

        /// <summary>Metabolic and energy processing traits</summary>
        Metabolic = 14,

        /// <summary>Utility and tool-use traits</summary>
        Utility = 15
    }
    
    /// <summary>
    /// Helper methods for TraitType operations
    /// </summary>
    public static class TraitTypeExtensions
    {
        /// <summary>
        /// Gets the display name for a trait type
        /// </summary>
        public static string GetDisplayName(this TraitType traitType)
        {
            return traitType switch
            {
                TraitType.Physical => "Physical",
                TraitType.Behavioral => "Behavioral", 
                TraitType.Combat => "Combat",
                TraitType.Elemental => "Elemental",
                TraitType.Mental => "Mental",
                TraitType.Movement => "Movement",
                TraitType.Special => "Special",
                TraitType.Vitality => "Vitality",
                TraitType.Environmental => "Environmental",
                TraitType.Social => "Social",
                TraitType.Mutation => "Mutation",
                TraitType.Hidden => "Hidden",
                TraitType.Sensory => "Sensory",
                TraitType.Metabolic => "Metabolic",
                TraitType.Utility => "Utility",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Gets the color associated with this trait type for UI display
        /// </summary>
        public static UnityEngine.Color GetTraitColor(this TraitType traitType)
        {
            return traitType switch
            {
                TraitType.Physical => UnityEngine.Color.cyan,
                TraitType.Behavioral => UnityEngine.Color.yellow,
                TraitType.Combat => UnityEngine.Color.red,
                TraitType.Elemental => UnityEngine.Color.magenta,
                TraitType.Mental => UnityEngine.Color.blue,
                TraitType.Movement => UnityEngine.Color.green,
                TraitType.Special => UnityEngine.Color.white,
                TraitType.Vitality => new UnityEngine.Color(1f, 0.5f, 0f), // Orange
                TraitType.Environmental => new UnityEngine.Color(0.5f, 0.8f, 0.3f), // Olive
                TraitType.Social => new UnityEngine.Color(0.8f, 0.4f, 0.8f), // Pink
                TraitType.Mutation => new UnityEngine.Color(0.2f, 0.2f, 0.2f), // Dark gray
                TraitType.Hidden => new UnityEngine.Color(0.5f, 0.5f, 0.5f, 0.5f), // Translucent gray
                TraitType.Sensory => new UnityEngine.Color(0.9f, 0.8f, 0.2f), // Bright yellow
                TraitType.Metabolic => new UnityEngine.Color(0.3f, 0.9f, 0.6f), // Light green
                TraitType.Utility => new UnityEngine.Color(0.6f, 0.6f, 0.9f), // Light blue
                _ => UnityEngine.Color.gray
            };
        }
        
        /// <summary>
        /// Checks if this trait type can be inherited normally
        /// </summary>
        public static bool IsInheritable(this TraitType traitType)
        {
            return traitType switch
            {
                TraitType.Mutation => false, // Mutations are special
                TraitType.Hidden => false,   // Hidden traits have special rules
                _ => true
            };
        }
        
        /// <summary>
        /// Gets the base mutation rate for this trait type
        /// </summary>
        public static float GetBaseMutationRate(this TraitType traitType)
        {
            return traitType switch
            {
                TraitType.Physical => 0.02f,
                TraitType.Behavioral => 0.03f,
                TraitType.Combat => 0.01f,
                TraitType.Elemental => 0.025f,
                TraitType.Mental => 0.015f,
                TraitType.Movement => 0.02f,
                TraitType.Special => 0.005f,
                TraitType.Vitality => 0.01f,
                TraitType.Environmental => 0.03f,
                TraitType.Social => 0.025f,
                TraitType.Mutation => 0.1f,  // Mutations beget mutations
                TraitType.Hidden => 0.001f,  // Very rare to activate
                TraitType.Sensory => 0.02f,
                TraitType.Metabolic => 0.015f,
                TraitType.Utility => 0.025f,
                _ => 0.02f
            };
        }
        
        /// <summary>
        /// Gets all trait types that can interact with this one
        /// </summary>
        public static TraitType[] GetCompatibleTraits(this TraitType traitType)
        {
            return traitType switch
            {
                TraitType.Physical => new[] { TraitType.Movement, TraitType.Vitality, TraitType.Environmental },
                TraitType.Behavioral => new[] { TraitType.Mental, TraitType.Social, TraitType.Combat },
                TraitType.Combat => new[] { TraitType.Physical, TraitType.Behavioral, TraitType.Elemental },
                TraitType.Elemental => new[] { TraitType.Combat, TraitType.Special, TraitType.Environmental },
                TraitType.Mental => new[] { TraitType.Behavioral, TraitType.Special, TraitType.Social },
                TraitType.Movement => new[] { TraitType.Physical, TraitType.Environmental },
                TraitType.Special => new[] { TraitType.Elemental, TraitType.Mental, TraitType.Mutation },
                TraitType.Vitality => new[] { TraitType.Physical, TraitType.Environmental },
                TraitType.Environmental => new[] { TraitType.Physical, TraitType.Elemental, TraitType.Movement },
                TraitType.Social => new[] { TraitType.Behavioral, TraitType.Mental },
                TraitType.Mutation => new[] { TraitType.Special }, // Mutations can interact with anything
                TraitType.Hidden => new TraitType[0], // Hidden traits don't interact normally
                TraitType.Sensory => new[] { TraitType.Mental, TraitType.Environmental },
                TraitType.Metabolic => new[] { TraitType.Vitality, TraitType.Environmental },
                TraitType.Utility => new[] { TraitType.Mental, TraitType.Physical },
                _ => new TraitType[0]
            };
        }
    }
}
