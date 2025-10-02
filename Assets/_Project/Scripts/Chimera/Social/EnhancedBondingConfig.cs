using UnityEngine;

namespace Laboratory.Chimera.Social
{
    /// <summary>
    /// Configuration ScriptableObject for the Enhanced Bonding System.
    /// Controls all aspects of creature bonding, emotional inheritance, and generational memory.
    /// </summary>
    [CreateAssetMenu(fileName = "EnhancedBondingConfig", menuName = "Chimera/Social/Enhanced Bonding Config")]
    public class EnhancedBondingConfig : ScriptableObject
    {
        [Header("Bonding Mechanics")]
        [Tooltip("Maximum distance for creatures to detect bonding opportunities")]
        [Range(1f, 20f)]
        public float BondingDetectionRange = 5f;

        [Tooltip("Range within which creatures can strengthen bonds through proximity")]
        [Range(0.5f, 10f)]
        public float ProximityBondingRange = 3f;

        [Tooltip("Distance threshold where separation begins to weaken bonds")]
        [Range(5f, 50f)]
        public float SeparationDistanceThreshold = 15f;

        [Tooltip("Minimum compatibility score required to form a bond")]
        [Range(0f, 1f)]
        public float MinimumBondCompatibility = 0.3f;

        [Tooltip("Initial strength when a new bond is formed")]
        [Range(0.1f, 0.8f)]
        public float InitialBondStrength = 0.2f;

        [Tooltip("Minimum bond strength before bond is removed")]
        [Range(0.01f, 0.3f)]
        public float MinimumBondStrength = 0.05f;

        [Header("Bond Growth & Decay")]
        [Tooltip("Base rate at which bonds grow stronger per second")]
        [Range(0.001f, 0.1f)]
        public float BaseBondGrowthRate = 0.01f;

        [Tooltip("Rate at which bonds decay when creatures are separated")]
        [Range(0.001f, 0.05f)]
        public float SeparationDecayRate = 0.005f;

        [Tooltip("Multiplier for parent bond growth rate")]
        [Range(1f, 5f)]
        public float ParentBondMultiplier = 2.5f;

        [Tooltip("Multiplier for mate bond growth rate")]
        [Range(1f, 3f)]
        public float MateBondMultiplier = 1.8f;

        [Tooltip("Multiplier for offspring bond growth rate")]
        [Range(1f, 4f)]
        public float OffspringBondMultiplier = 2.2f;

        [Tooltip("Multiplier for companion bond growth rate")]
        [Range(0.5f, 2f)]
        public float CompanionBondMultiplier = 1.0f;

        [Header("Generational Memory")]
        [Tooltip("Maximum number of memories each creature can store")]
        [Range(5, 50)]
        public int MaxMemoriesPerCreature = 20;

        [Tooltip("Maximum number of generations memories can persist")]
        [Range(1, 10)]
        public int MaxGenerationalDepth = 5;

        [Tooltip("Rate at which memories decay over time")]
        [Range(0.0001f, 0.01f)]
        public float MemoryDecayRate = 0.001f;

        [Tooltip("Strength of memory influence on current bonds")]
        [Range(0.1f, 2f)]
        public float MemoryInfluenceStrength = 0.5f;

        [Tooltip("Rate at which offspring inherit parent memories")]
        [Range(0.1f, 1f)]
        public float ParentMemoryInheritanceRate = 0.6f;

        [Tooltip("Minimum strength for inherited memories to persist")]
        [Range(0.01f, 0.5f)]
        public float MinimumInheritedMemoryStrength = 0.1f;

        [Header("Mate Compatibility")]
        [Tooltip("Maximum age difference for compatible mates")]
        [Range(1f, 20f)]
        public float MaxMateAgeDifference = 10f;

        [Tooltip("Genetic diversity bonus for mate selection")]
        [Range(0f, 1f)]
        public float GeneticDiversityBonus = 0.3f;

        [Tooltip("Personality compatibility weight in mate selection")]
        [Range(0f, 1f)]
        public float PersonalityCompatibilityWeight = 0.4f;

        [Header("Bonding Milestones")]
        [Tooltip("Time threshold for lifelong bond achievement (hours)")]
        [Range(10f, 1000f)]
        public float LifelongBondTimeThreshold = 168f; // 1 week

        [Tooltip("Bond strength required for deep bond milestone")]
        [Range(0.7f, 1f)]
        public float DeepBondThreshold = 0.9f;

        [Tooltip("Bond strength required for soulmate milestone")]
        [Range(0.6f, 1f)]
        public float SoulmateThreshold = 0.8f;

        [Header("Social Behaviors")]
        [Tooltip("Enable emotional contagion between bonded creatures")]
        public bool EnableEmotionalContagion = true;

        [Tooltip("Enable protective behaviors for bonded creatures")]
        public bool EnableProtectiveBehaviors = true;

        [Tooltip("Enable grief responses when bonds are broken")]
        public bool EnableGriefResponse = true;

        [Tooltip("Enable bonding ceremonies for milestone achievements")]
        public bool EnableBondingCeremonies = true;

        [Header("Memory Categories")]
        [Tooltip("Weight for positive bonding memories")]
        [Range(0.5f, 2f)]
        public float PositiveMemoryWeight = 1.2f;

        [Tooltip("Weight for traumatic bonding memories")]
        [Range(0.5f, 3f)]
        public float TraumaMemoryWeight = 1.8f;

        [Tooltip("Weight for protective bonding memories")]
        [Range(0.5f, 2f)]
        public float ProtectiveMemoryWeight = 1.5f;

        [Tooltip("Weight for loss/grief memories")]
        [Range(0.5f, 2.5f)]
        public float GriefMemoryWeight = 2.0f;

        [Header("Advanced Features")]
        [Tooltip("Enable cross-species bonding")]
        public bool EnableCrossSpeciesBonding = true;

        [Tooltip("Enable adoption mechanics")]
        public bool EnableAdoption = true;

        [Tooltip("Enable pack/family group formation")]
        public bool EnableGroupFormation = true;

        [Tooltip("Maximum size for family groups")]
        [Range(2, 20)]
        public int MaxFamilyGroupSize = 8;

        [Header("Performance Settings")]
        [Tooltip("Maximum number of bond calculations per frame")]
        [Range(10, 200)]
        public int MaxBondCalculationsPerFrame = 50;

        [Tooltip("Memory update frequency in seconds")]
        [Range(1f, 60f)]
        public float MemoryUpdateFrequency = 10f;

        [Tooltip("Bond strength update frequency in seconds")]
        [Range(0.1f, 10f)]
        public float BondUpdateFrequency = 1f;

        [Header("Visual & Audio")]
        [Tooltip("Enable visual bond indicators")]
        public bool ShowBondVisuals = true;

        [Tooltip("Enable audio cues for bonding events")]
        public bool EnableBondingAudio = true;

        [Tooltip("Bond strength visualization intensity")]
        [Range(0.1f, 2f)]
        public float BondVisualizationIntensity = 1.0f;

        /// <summary>
        /// Calculates the memory weight for a specific bond type
        /// </summary>
        public float GetMemoryWeight(BondType bondType, MemoryType memoryType)
        {
            float baseWeight = GetBaseBondWeight(bondType);
            float typeMultiplier = GetMemoryTypeMultiplier(memoryType);

            return baseWeight * typeMultiplier;
        }

        /// <summary>
        /// Gets the base emotional weight for different bond types
        /// </summary>
        public float GetBaseBondWeight(BondType bondType)
        {
            switch (bondType)
            {
                case BondType.Parent: return 1.0f;
                case BondType.Offspring: return 0.9f;
                case BondType.Mate: return 0.8f;
                case BondType.Companion: return 0.6f;
                case BondType.Mentor: return 0.7f;
                case BondType.Student: return 0.5f;
                case BondType.Rival: return 0.4f;
                default: return 0.5f;
            }
        }

        /// <summary>
        /// Gets the multiplier for different memory types
        /// </summary>
        public float GetMemoryTypeMultiplier(MemoryType memoryType)
        {
            switch (memoryType)
            {
                case MemoryType.Positive: return PositiveMemoryWeight;
                case MemoryType.Trauma: return TraumaMemoryWeight;
                case MemoryType.Protective: return ProtectiveMemoryWeight;
                case MemoryType.Grief: return GriefMemoryWeight;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Determines if two bond types are compatible for inheritance
        /// </summary>
        public bool AreBondTypesCompatible(BondType sourceType, BondType targetType)
        {
            // Family bonds are always compatible
            if (IsFamilyBond(sourceType) && IsFamilyBond(targetType))
                return true;

            // Same type bonds are compatible
            if (sourceType == targetType)
                return true;

            // Specific compatibility rules
            return (sourceType, targetType) switch
            {
                (BondType.Mentor, BondType.Student) => true,
                (BondType.Student, BondType.Mentor) => true,
                (BondType.Companion, BondType.Mate) => true,
                (BondType.Mate, BondType.Companion) => true,
                _ => false
            };
        }

        /// <summary>
        /// Checks if a bond type represents a family relationship
        /// </summary>
        public bool IsFamilyBond(BondType bondType)
        {
            return bondType == BondType.Parent || bondType == BondType.Offspring;
        }

        /// <summary>
        /// Calculates the ideal memory decay rate based on emotional intensity
        /// </summary>
        public float CalculateMemoryDecayRate(float emotionalIntensity, MemoryType memoryType)
        {
            float baseDecay = MemoryDecayRate;
            float intensityFactor = Mathf.Lerp(2f, 0.5f, emotionalIntensity); // Strong memories decay slower
            float typeModifier = GetMemoryTypeDecayModifier(memoryType);

            return baseDecay * intensityFactor * typeModifier;
        }

        private float GetMemoryTypeDecayModifier(MemoryType memoryType)
        {
            return memoryType switch
            {
                MemoryType.Trauma => 0.3f,    // Trauma memories persist longer
                MemoryType.Grief => 0.4f,     // Grief memories decay slowly
                MemoryType.Protective => 0.6f, // Protective memories are important
                MemoryType.Positive => 1.0f,   // Normal decay for positive memories
                _ => 1.0f
            };
        }

        private void OnValidate()
        {
            // Ensure logical constraints
            ProximityBondingRange = Mathf.Min(ProximityBondingRange, BondingDetectionRange);
            MinimumBondStrength = Mathf.Min(MinimumBondStrength, InitialBondStrength);
            MinimumInheritedMemoryStrength = Mathf.Min(MinimumInheritedMemoryStrength, MinimumBondStrength);

            // Ensure performance constraints
            MaxBondCalculationsPerFrame = Mathf.Clamp(MaxBondCalculationsPerFrame, 1, 500);
            MaxMemoriesPerCreature = Mathf.Clamp(MaxMemoriesPerCreature, 1, 100);
        }
    }

    /// <summary>
    /// Types of memories that can be formed during bonding
    /// </summary>
    public enum MemoryType
    {
        Positive,   // Happy bonding moments
        Trauma,     // Traumatic events affecting bonds
        Protective, // Protective behaviors and care
        Grief,      // Loss and separation memories
        Neutral     // General interaction memories
    }
}