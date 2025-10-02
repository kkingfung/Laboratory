using System;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Laboratory.Chimera.Discovery.Core;
using Laboratory.Chimera.Genetics.Core;

namespace Laboratory.Chimera.Social.Core
{
    /// <summary>
    /// Data structure for sharing discoveries and creatures with the community
    /// Creates beautiful shareable content with rich genetic information
    /// </summary>
    [System.Serializable]
    public struct SocialShareData : IComponentData
    {
        // Share metadata
        public FixedString64Bytes ShareID;
        public FixedString64Bytes PlayerName;
        public FixedString128Bytes ShareTitle;
        public FixedString512Bytes ShareDescription;
        public uint ShareTimestamp;
        public ShareType Type;

        // Content data
        public Entity SharedCreature;
        public DiscoveryEvent SharedDiscovery;
        public VisualGeneticData GeneticData;
        public BreedingLineage LineageData;

        // Engagement metrics
        public int LikeCount;
        public int CommentCount;
        public int ShareCount;
        public float PopularityScore;
        public bool IsFeatured;
        public bool IsVerified;

        // Visual data for share cards
        public Laboratory.Chimera.Genetics.Core.BlittableColor PrimaryColor;
        public Laboratory.Chimera.Genetics.Core.BlittableColor SecondaryColor;
        public GeneticMarkerFlags HighlightMarkers;
        public float VisualAppeal;

        /// <summary>
        /// Update engagement metrics for this share
        /// </summary>
        public void UpdateEngagement(int likes, int comments, int shares)
        {
            LikeCount = likes;
            CommentCount = comments;
            ShareCount = shares;
            PopularityScore = likes + (comments * 2) + (shares * 5);
        }

        /// <summary>
        /// Check if this share is currently trending
        /// </summary>
        public bool IsTrending()
        {
            return PopularityScore > 100f && IsFeatured;
        }

        /// <summary>
        /// Create share data from creature
        /// </summary>
        public static SocialShareData CreateFromCreature(Entity creature, VisualGeneticData genetics, string playerName)
        {
            return new SocialShareData
            {
                ShareID = new FixedString64Bytes(System.Guid.NewGuid().ToString("N")[..12]),
                PlayerName = new FixedString64Bytes(playerName),
                ShareTitle = new FixedString128Bytes("My Amazing Creature!"),
                ShareDescription = new FixedString512Bytes("Check out this incredible genetic combination I've created!"),
                ShareTimestamp = (uint)UnityEngine.Time.time,
                Type = ShareType.CreatureShowcase,
                SharedCreature = creature,
                GeneticData = genetics,
                PrimaryColor = genetics.PrimaryHelixColor,
                SecondaryColor = genetics.SecondaryHelixColor,
                HighlightMarkers = genetics.SpecialMarkers,
                VisualAppeal = genetics.SpecialMarkers.CountFlags() / 8f
            };
        }

        /// <summary>
        /// Create share data from discovery
        /// </summary>
        public static SocialShareData CreateFromDiscovery(DiscoveryEvent discovery, string playerName)
        {
            return new SocialShareData
            {
                ShareID = new FixedString64Bytes(System.Guid.NewGuid().ToString("N")[..12]),
                PlayerName = new FixedString64Bytes(playerName),
                ShareTitle = new FixedString128Bytes($"New Discovery: {discovery.DiscoveryName}"),
                ShareDescription = new FixedString512Bytes(discovery.DiscoveryDescription),
                ShareTimestamp = discovery.DiscoveryTimestamp,
                Type = ShareType.Discovery,
                SharedDiscovery = discovery,
                GeneticData = discovery.DiscoveredGenetics,
                PrimaryColor = discovery.DiscoveredGenetics.PrimaryHelixColor,
                SecondaryColor = discovery.DiscoveredGenetics.SecondaryHelixColor,
                HighlightMarkers = discovery.SpecialMarkers,
                VisualAppeal = discovery.SignificanceScore / 1000f,
                IsVerified = discovery.IsWorldFirst,
                IsFeatured = discovery.Rarity >= DiscoveryRarity.Epic
            };
        }
    }

    /// <summary>
    /// Types of social shares
    /// </summary>
    public enum ShareType : byte
    {
        Discovery,           // General discovery
        CreatureShowcase,    // Showing off a creature
        NewSpecies,         // New species discovery
        PerfectGenetics,    // Perfect genetic achievement
        LegendaryLineage,   // Legendary breeding line
        RareMutation,       // Rare mutation discovery
        BreedingSuccess,    // Successful breeding outcome
        Achievement         // General achievement
    }

    /// <summary>
    /// Social interaction data
    /// </summary>
    [System.Serializable]
    public struct SocialInteraction : IComponentData
    {
        public FixedString64Bytes InteractionID;
        public FixedString64Bytes ShareID;
        public FixedString64Bytes PlayerName;
        public InteractionType Type;
        public uint Timestamp;
        public FixedString128Bytes Content; // For comments
    }

    /// <summary>
    /// Types of social interactions
    /// </summary>
    public enum InteractionType : byte
    {
        Like,
        Comment,
        Share,
        Follow,
        Bookmark
    }

}