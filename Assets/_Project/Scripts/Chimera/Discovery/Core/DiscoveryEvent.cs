using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Laboratory.Chimera.Genetics.Core;

namespace Laboratory.Chimera.Discovery.Core
{
    /// <summary>
    /// Core data structure for genetic discoveries
    /// Contains all information needed for celebration and tracking
    /// </summary>
    [System.Serializable]
    public struct DiscoveryEvent : IComponentData
    {
        public DiscoveryType Type;
        public DiscoveryRarity Rarity;
        public float SignificanceScore;
        public uint DiscoveryTimestamp;

        // Genetic discovery details
        public FixedString64Bytes DiscoveryName;
        public FixedString128Bytes DiscoveryDescription;
        public VisualGeneticData DiscoveredGenetics;
        public GeneticMarkerFlags SpecialMarkers;

        // Discovery context
        public Entity DiscovererPlayer;
        public Entity DiscoveredCreature;
        public float3 DiscoveryLocation;
        public BreedingLineage ParentLineage;

        // Celebration parameters
        public float CelebrationIntensity;
        [MarshalAs(UnmanagedType.U1)]
        public bool IsFirstTimeDiscovery;
        [MarshalAs(UnmanagedType.U1)]
        public bool IsWorldFirst;
        public int CommunityRank;

        // Static utility methods
        public static float CalculateSignificance(DiscoveryType type, DiscoveryRarity rarity, bool isFirstTime, bool isWorldFirst)
        {
            return DiscoveryNameGenerator.CalculateSignificance(type, rarity, isFirstTime, isWorldFirst);
        }

        public static string GenerateDiscoveryName(DiscoveryType type, VisualGeneticData genetics, GeneticMarkerFlags markers)
        {
            return DiscoveryNameGenerator.GenerateDiscoveryName(type, genetics, markers);
        }
    }

    /// <summary>
    /// Breeding lineage tracking for discovery context
    /// </summary>
    [System.Serializable]
    public struct BreedingLineage
    {
        public Entity Parent1;
        public Entity Parent2;
        public int GenerationDepth;
        [MarshalAs(UnmanagedType.U1)]
        public bool IsLinebred;
        [MarshalAs(UnmanagedType.U1)]
        public bool IsOutcrossed;
        public float InbreedingCoefficient;
    }

    /// <summary>
    /// Types of genetic discoveries
    /// </summary>
    public enum DiscoveryType : byte
    {
        NewTrait,           // Never-before-seen trait combination
        RareMutation,       // Genetic mutation occurred
        SpecialMarker,      // Special genetic marker activated
        PerfectGenetics,    // All traits at maximum values
        NewSpecies,         // Completely new genetic combination
        LegendaryLineage    // Breeding line reaches legendary status
    }

    /// <summary>
    /// Rarity levels for discoveries
    /// </summary>
    public enum DiscoveryRarity : byte
    {
        Common,      // 1 in 10 chance
        Uncommon,    // 1 in 50 chance
        Rare,        // 1 in 200 chance
        Epic,        // 1 in 1000 chance
        Legendary,   // 1 in 5000 chance
        Mythical     // 1 in 25000 chance
    }

    /// <summary>
    /// Utility class for generating discovery names and descriptions
    /// Separated from DiscoveryEvent to maintain ECS compliance
    /// </summary>
    public static class DiscoveryNameGenerator
    {
        /// <summary>
        /// Calculate significance score based on rarity and context
        /// </summary>
        public static float CalculateSignificance(DiscoveryType type, DiscoveryRarity rarity, bool isFirstTime, bool isWorldFirst)
        {
            float baseScore = rarity switch
            {
                DiscoveryRarity.Common => 10f,
                DiscoveryRarity.Uncommon => 25f,
                DiscoveryRarity.Rare => 50f,
                DiscoveryRarity.Epic => 100f,
                DiscoveryRarity.Legendary => 250f,
                DiscoveryRarity.Mythical => 500f,
                _ => 1f
            };

            float typeMultiplier = type switch
            {
                DiscoveryType.NewTrait => 1.0f,
                DiscoveryType.RareMutation => 1.5f,
                DiscoveryType.SpecialMarker => 2.0f,
                DiscoveryType.PerfectGenetics => 3.0f,
                DiscoveryType.NewSpecies => 5.0f,
                DiscoveryType.LegendaryLineage => 10.0f,
                _ => 1.0f
            };

            float contextBonus = 1.0f;
            if (isFirstTime) contextBonus += 0.5f;
            if (isWorldFirst) contextBonus += 2.0f;

            return baseScore * typeMultiplier * contextBonus;
        }
        /// <summary>
        /// Generate discovery name based on genetics and type
        /// </summary>
        public static string GenerateDiscoveryName(DiscoveryType type, VisualGeneticData genetics, GeneticMarkerFlags markers)
        {
            string prefix = type switch
            {
                DiscoveryType.NewTrait => "Enhanced",
                DiscoveryType.RareMutation => "Mutant",
                DiscoveryType.SpecialMarker => "Marked",
                DiscoveryType.PerfectGenetics => "Perfect",
                DiscoveryType.NewSpecies => "Hybrid",
                DiscoveryType.LegendaryLineage => "Legendary",
                _ => "Unknown"
            };

            string descriptor = GetGeneticDescriptor(genetics);
            string markerSuffix = GetMarkerSuffix(markers);

            return $"{prefix} {descriptor}{markerSuffix}";
        }

        private static string GetGeneticDescriptor(VisualGeneticData genetics)
        {
            byte maxTrait = Math.Max(
                Math.Max(genetics.Strength, genetics.Vitality),
                Math.Max(genetics.Agility, Math.Max(genetics.Intelligence, Math.Max(genetics.Adaptability, genetics.Social)))
            );

            if (genetics.Strength == maxTrait) return "Titan";
            if (genetics.Vitality == maxTrait) return "Eternal";
            if (genetics.Agility == maxTrait) return "Swift";
            if (genetics.Intelligence == maxTrait) return "Genius";
            if (genetics.Adaptability == maxTrait) return "Evolved";
            if (genetics.Social == maxTrait) return "Alpha";

            return "Balanced";
        }

        private static string GetMarkerSuffix(GeneticMarkerFlags markers)
        {
            if (markers.HasFlag(GeneticMarkerFlags.Bioluminescent)) return " Lumina";
            if (markers.HasFlag(GeneticMarkerFlags.ElementalAffinity)) return " Elemental";
            if (markers.HasFlag(GeneticMarkerFlags.RareLineage)) return " Prime";
            if (markers.HasFlag(GeneticMarkerFlags.HybridVigor)) return " Hybrid";
            if (markers.HasFlag(GeneticMarkerFlags.PackLeader)) return " Rex";

            return "";
        }
    }
}