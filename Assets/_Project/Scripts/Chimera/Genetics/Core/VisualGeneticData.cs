using System;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace Laboratory.Chimera.Genetics.Core
{
    /// <summary>
    /// Enhanced genetic data structure for visual DNA display system
    /// Stores both genetic information and visual representation data
    /// </summary>
    [System.Serializable]
    public struct VisualGeneticData : IComponentData
    {
        // Core genetic traits (0-100 scale for easy visualization)
        public byte Strength;
        public byte Vitality;
        public byte Agility;
        public byte Intelligence;
        public byte Adaptability;
        public byte Social;

        // Genetic alleles (dominant/recessive pairs)
        public TraitAllele StrengthAlleles;
        public TraitAllele VitalityAlleles;
        public TraitAllele AgilityAlleles;
        public TraitAllele IntelligenceAlleles;
        public TraitAllele AdaptabilityAlleles;
        public TraitAllele SocialAlleles;

        // Visual DNA properties
        public uint VisualSeed; // For procedural DNA helix generation
        public byte GenerationCount;
        public byte InbreedingCoefficient;
        public byte MutationCount;

        // DNA Visualization Colors (RGB values 0-255)
        public BlittableColor PrimaryHelixColor;
        public BlittableColor SecondaryHelixColor;
        public BlittableColor BaseSequenceColor;

        // Special genetic markers for visual effects
        public GeneticMarkerFlags SpecialMarkers;

    }

    /// <summary>
    /// Trait allele pair (dominant/recessive)
    /// </summary>
    [System.Serializable]
    public struct TraitAllele
    {
        public byte DominantValue;  // 0-100
        public byte RecessiveValue; // 0-100
        public bool IsDominantExpressed; // Which allele is currently expressed

    }

    /// <summary>
    /// Special genetic markers that trigger visual effects
    /// </summary>
    [Flags]
    public enum GeneticMarkerFlags : uint
    {
        None = 0,
        Bioluminescent = 1 << 0,    // Creature glows
        CamouflageGene = 1 << 1,    // Adaptive coloring
        PackLeader = 1 << 2,        // Leadership traits
        SeasonalAdaptation = 1 << 3, // Changes with seasons
        HybridVigor = 1 << 4,       // Crossbreed advantages
        RareLineage = 1 << 5,       // Ancient bloodline
        MutationCarrier = 1 << 6,   // Carries unique mutations
        ElementalAffinity = 1 << 7, // Environmental specialization
    }

    /// <summary>
    /// Utility class for working with genetic data (moved from component structs for ECS compliance)
    /// </summary>
    public static class VisualGeneticUtility
    {
        /// <summary>
        /// Get trait value by index for easy iteration
        /// </summary>
        public static byte GetTraitValue(VisualGeneticData data, int traitIndex)
        {
            return traitIndex switch
            {
                0 => data.Strength,
                1 => data.Vitality,
                2 => data.Agility,
                3 => data.Intelligence,
                4 => data.Adaptability,
                5 => data.Social,
                _ => 0
            };
        }

        /// <summary>
        /// Get trait alleles by index
        /// </summary>
        public static TraitAllele GetTraitAlleles(VisualGeneticData data, int traitIndex)
        {
            return traitIndex switch
            {
                0 => data.StrengthAlleles,
                1 => data.VitalityAlleles,
                2 => data.AgilityAlleles,
                3 => data.IntelligenceAlleles,
                4 => data.AdaptabilityAlleles,
                5 => data.SocialAlleles,
                _ => default
            };
        }

        /// <summary>
        /// Calculate genetic rarity score for visual effects
        /// </summary>
        public static float GetRarityScore(VisualGeneticData data)
        {
            float rarityScore = 0f;

            // High values in multiple traits increase rarity
            int highTraits = 0;
            if (data.Strength > 80) highTraits++;
            if (data.Vitality > 80) highTraits++;
            if (data.Agility > 80) highTraits++;
            if (data.Intelligence > 80) highTraits++;
            if (data.Adaptability > 80) highTraits++;
            if (data.Social > 80) highTraits++;

            rarityScore += highTraits * 0.15f;

            // Special markers add significant rarity
            rarityScore += data.SpecialMarkers.CountFlags() * 0.2f;

            // High generation count reduces rarity (inbreeding)
            rarityScore -= data.InbreedingCoefficient * 0.01f;

            // Mutations add rarity
            rarityScore += data.MutationCount * 0.1f;

            return Mathf.Clamp01(rarityScore);
        }

        /// <summary>
        /// Get the expressed trait value
        /// </summary>
        public static byte GetExpressedValue(TraitAllele allele)
        {
            return allele.IsDominantExpressed ? allele.DominantValue : allele.RecessiveValue;
        }

        /// <summary>
        /// Get the hidden (non-expressed) value
        /// </summary>
        public static byte GetHiddenValue(TraitAllele allele)
        {
            return allele.IsDominantExpressed ? allele.RecessiveValue : allele.DominantValue;
        }
    }

    /// <summary>
    /// Blittable color struct for ECS compatibility (replaces Color32)
    /// </summary>
    [System.Serializable]
    public struct BlittableColor
    {
        public byte r, g, b, a;

        public BlittableColor(byte red, byte green, byte blue, byte alpha = 255)
        {
            r = red;
            g = green;
            b = blue;
            a = alpha;
        }

        public Color32 ToColor32()
        {
            return new Color32(r, g, b, a);
        }

        public Color ToColor()
        {
            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        public static BlittableColor FromColor32(Color32 color)
        {
            return new BlittableColor(color.r, color.g, color.b, color.a);
        }

        public static BlittableColor FromColor(Color color)
        {
            return new BlittableColor(
                (byte)(color.r * 255),
                (byte)(color.g * 255),
                (byte)(color.b * 255),
                (byte)(color.a * 255)
            );
        }
    }

    /// <summary>
    /// Extension methods for genetic marker flags
    /// </summary>
    public static class GeneticMarkerExtensions
    {
        public static int CountFlags(this GeneticMarkerFlags flags)
        {
            int count = 0;
            uint value = (uint)flags;
            while (value != 0)
            {
                count += (int)(value & 1);
                value >>= 1;
            }
            return count;
        }

        public static bool HasMarker(this GeneticMarkerFlags flags, GeneticMarkerFlags marker)
        {
            return (flags & marker) == marker;
        }
    }
}