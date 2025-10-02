using System;
using UnityEngine;
using Unity.Collections;
using Laboratory.Chimera.Discovery.Core;
using Laboratory.Chimera.Genetics.Core;

namespace Laboratory.Chimera.Social.Core
{
    /// <summary>
    /// Utility class for generating social share content
    /// Separated from SocialShareData to maintain ECS compliance
    /// </summary>
    public static class SocialShareUtility
    {
        /// <summary>
        /// Generate shareable content from a discovery
        /// </summary>
        public static SocialShareData CreateFromDiscovery(DiscoveryEvent discovery, string playerName, Unity.Entities.Entity creature = default)
        {
            var shareData = new SocialShareData
            {
                ShareID = new FixedString64Bytes(GenerateShareID()),
                PlayerName = new FixedString64Bytes(playerName),
                ShareTitle = new FixedString128Bytes(GenerateShareTitle(discovery)),
                ShareDescription = new FixedString512Bytes(GenerateShareDescription(discovery)),
                ShareTimestamp = (uint)Time.time,
                Type = DetermineShareType(discovery),
                SharedCreature = creature,
                SharedDiscovery = discovery,
                GeneticData = discovery.DiscoveredGenetics,
                LikeCount = 0,
                CommentCount = 0,
                ShareCount = 0,
                PopularityScore = CalculateInitialPopularity(discovery),
                IsFeatured = discovery.Rarity >= DiscoveryRarity.Legendary,
                IsVerified = discovery.IsWorldFirst,
                PrimaryColor = GetDiscoveryColor(discovery.Rarity),
                SecondaryColor = GetTypeColor(discovery.Type),
                HighlightMarkers = discovery.SpecialMarkers,
                VisualAppeal = CalculateVisualAppeal(discovery.DiscoveredGenetics, discovery.SpecialMarkers)
            };

            return shareData;
        }

        /// <summary>
        /// Generate shareable content from a creature
        /// </summary>
        public static SocialShareData CreateFromCreature(Unity.Entities.Entity creature, VisualGeneticData genetics, string playerName, string customTitle = "", string customDescription = "")
        {
            var shareData = new SocialShareData
            {
                ShareID = new FixedString64Bytes(GenerateShareID()),
                PlayerName = new FixedString64Bytes(playerName),
                ShareTitle = new FixedString128Bytes(string.IsNullOrEmpty(customTitle) ? GenerateCreatureTitle(genetics) : customTitle),
                ShareDescription = new FixedString512Bytes(string.IsNullOrEmpty(customDescription) ? GenerateCreatureDescription(genetics) : customDescription),
                ShareTimestamp = (uint)Time.time,
                Type = ShareType.CreatureShowcase,
                SharedCreature = creature,
                GeneticData = genetics,
                LikeCount = 0,
                CommentCount = 0,
                ShareCount = 0,
                PopularityScore = CalculateCreaturePopularity(genetics),
                IsFeatured = IsCreatureFeatured(genetics),
                IsVerified = false,
                PrimaryColor = GetGeneticPrimaryColor(genetics),
                SecondaryColor = GetGeneticSecondaryColor(genetics),
                HighlightMarkers = genetics.SpecialMarkers,
                VisualAppeal = CalculateVisualAppeal(genetics, genetics.SpecialMarkers)
            };

            return shareData;
        }

        // Helper methods for content generation
        private static string GenerateShareID()
        {
            return System.Guid.NewGuid().ToString("N")[..12]; // 12 character ID
        }

        private static string GenerateShareTitle(DiscoveryEvent discovery)
        {
            string rarityPrefix = discovery.Rarity switch
            {
                DiscoveryRarity.Mythical => "🌟 MYTHICAL",
                DiscoveryRarity.Legendary => "⭐ LEGENDARY",
                DiscoveryRarity.Epic => "💫 EPIC",
                DiscoveryRarity.Rare => "✨ RARE",
                _ => ""
            };

            string typeText = discovery.Type switch
            {
                DiscoveryType.NewSpecies => "New Species Discovered!",
                DiscoveryType.PerfectGenetics => "Perfect Genetics Achieved!",
                DiscoveryType.LegendaryLineage => "Legendary Bloodline Created!",
                DiscoveryType.RareMutation => "Rare Mutation Found!",
                DiscoveryType.SpecialMarker => "Special Markers Activated!",
                _ => "Genetic Breakthrough!"
            };

            return discovery.IsWorldFirst ? $"🏆 WORLD FIRST {typeText}" : $"{rarityPrefix} {typeText}";
        }

        private static string GenerateShareDescription(DiscoveryEvent discovery)
        {
            var description = $"Just discovered: {discovery.DiscoveryName}! ";

            description += discovery.Type switch
            {
                DiscoveryType.NewSpecies => "Through careful breeding, I've created an entirely new species with unique traits.",
                DiscoveryType.PerfectGenetics => "After countless generations, perfect genetic harmony has been achieved.",
                DiscoveryType.LegendaryLineage => "My breeding program has reached legendary status with this incredible lineage.",
                DiscoveryType.RareMutation => "A spontaneous mutation has created something truly special.",
                DiscoveryType.SpecialMarker => "Ancient genetic markers have awakened in this extraordinary creature.",
                _ => "This genetic combination has never been seen before."
            };

            if (discovery.SignificanceScore > 500)
                description += " This discovery will change everything we know about genetics!";
            else if (discovery.SignificanceScore > 100)
                description += " A major breakthrough for the breeding community!";

            return description;
        }

        private static string GenerateCreatureTitle(VisualGeneticData genetics)
        {
            string descriptor = GetGeneticDescriptor(genetics);
            string markerText = genetics.SpecialMarkers != GeneticMarkerFlags.None ? "Special" : "Beautiful";
            return $"My {markerText} {descriptor} Creature";
        }

        private static string GenerateCreatureDescription(VisualGeneticData genetics)
        {
            int totalStats = genetics.Strength + genetics.Vitality + genetics.Agility + genetics.Intelligence + genetics.Adaptability + genetics.Social;

            string description = $"Check out my creature with {totalStats} total stats! ";

            if (genetics.SpecialMarkers != GeneticMarkerFlags.None)
                description += "It has rare genetic markers that make it truly unique. ";

            byte maxTrait = Math.Max(genetics.Strength, Math.Max(genetics.Vitality, Math.Max(genetics.Agility, Math.Max(genetics.Intelligence, Math.Max(genetics.Adaptability, genetics.Social)))));

            if (maxTrait >= 90)
                description += "The genetics are absolutely incredible!";
            else if (maxTrait >= 70)
                description += "Such impressive traits!";
            else
                description += "I'm so proud of this breeding achievement!";

            return description;
        }

        private static ShareType DetermineShareType(DiscoveryEvent discovery)
        {
            return discovery.Type switch
            {
                DiscoveryType.NewSpecies => ShareType.NewSpecies,
                DiscoveryType.PerfectGenetics => ShareType.PerfectGenetics,
                DiscoveryType.LegendaryLineage => ShareType.LegendaryLineage,
                DiscoveryType.RareMutation => ShareType.RareMutation,
                _ => ShareType.Discovery
            };
        }

        private static float CalculateInitialPopularity(DiscoveryEvent discovery)
        {
            float baseScore = (int)discovery.Rarity * 20;
            if (discovery.IsWorldFirst) baseScore *= 3f;
            if (discovery.IsFirstTimeDiscovery) baseScore *= 1.5f;
            return baseScore + discovery.SignificanceScore * 0.1f;
        }

        private static float CalculateCreaturePopularity(VisualGeneticData genetics)
        {
            int totalStats = genetics.Strength + genetics.Vitality + genetics.Agility + genetics.Intelligence + genetics.Adaptability + genetics.Social;
            float baseScore = totalStats * 0.5f;

            if (genetics.SpecialMarkers != GeneticMarkerFlags.None)
                baseScore += Unity.Mathematics.math.countbits((uint)genetics.SpecialMarkers) * 25f;

            return baseScore;
        }

        private static bool IsCreatureFeatured(VisualGeneticData genetics)
        {
            int totalStats = genetics.Strength + genetics.Vitality + genetics.Agility + genetics.Intelligence + genetics.Adaptability + genetics.Social;
            return totalStats > 450 || genetics.SpecialMarkers != GeneticMarkerFlags.None;
        }

        private static BlittableColor GetDiscoveryColor(DiscoveryRarity rarity)
        {
            return rarity switch
            {
                DiscoveryRarity.Mythical => new BlittableColor(255, 214, 0), // Gold
                DiscoveryRarity.Legendary => new BlittableColor(255, 128, 0), // Orange
                DiscoveryRarity.Epic => new BlittableColor(153, 102, 255), // Purple
                DiscoveryRarity.Rare => new BlittableColor(0, 128, 255), // Blue
                DiscoveryRarity.Uncommon => new BlittableColor(0, 255, 0), // Green
                _ => new BlittableColor(255, 255, 255) // White
            };
        }

        private static BlittableColor GetTypeColor(DiscoveryType type)
        {
            return type switch
            {
                DiscoveryType.NewSpecies => new BlittableColor(255, 51, 51),
                DiscoveryType.PerfectGenetics => new BlittableColor(51, 255, 51),
                DiscoveryType.LegendaryLineage => new BlittableColor(255, 214, 0),
                DiscoveryType.RareMutation => new BlittableColor(153, 102, 255),
                DiscoveryType.SpecialMarker => new BlittableColor(255, 128, 0),
                _ => new BlittableColor(128, 128, 128) // Gray
            };
        }

        private static BlittableColor GetGeneticPrimaryColor(VisualGeneticData genetics)
        {
            // Based on highest trait
            byte maxTrait = genetics.Strength;
            BlittableColor color = new BlittableColor(255, 0, 0); // Strength - Red

            if (genetics.Vitality > maxTrait)
            {
                maxTrait = genetics.Vitality;
                color = new BlittableColor(0, 255, 0); // Vitality - Green
            }
            if (genetics.Agility > maxTrait)
            {
                maxTrait = genetics.Agility;
                color = new BlittableColor(0, 255, 255); // Agility - Cyan
            }
            if (genetics.Intelligence > maxTrait)
            {
                maxTrait = genetics.Intelligence;
                color = new BlittableColor(0, 0, 255); // Intelligence - Blue
            }
            if (genetics.Adaptability > maxTrait)
            {
                maxTrait = genetics.Adaptability;
                color = new BlittableColor(255, 0, 255); // Adaptability - Magenta
            }
            if (genetics.Social > maxTrait)
            {
                color = new BlittableColor(255, 255, 0); // Social - Yellow
            }

            return color;
        }

        private static BlittableColor GetGeneticSecondaryColor(VisualGeneticData genetics)
        {
            return genetics.PrimaryHelixColor;
        }

        private static float CalculateVisualAppeal(VisualGeneticData genetics, GeneticMarkerFlags markers)
        {
            float baseAppeal = VisualGeneticUtility.GetRarityScore(genetics);

            if (markers != GeneticMarkerFlags.None)
                baseAppeal += Unity.Mathematics.math.countbits((uint)markers) * 0.1f;

            return Mathf.Clamp01(baseAppeal);
        }

        private static string GetGeneticDescriptor(VisualGeneticData genetics)
        {
            byte maxTrait = Math.Max(genetics.Strength, Math.Max(genetics.Vitality, Math.Max(genetics.Agility, Math.Max(genetics.Intelligence, Math.Max(genetics.Adaptability, genetics.Social)))));

            if (genetics.Strength == maxTrait) return "Mighty";
            if (genetics.Vitality == maxTrait) return "Resilient";
            if (genetics.Agility == maxTrait) return "Swift";
            if (genetics.Intelligence == maxTrait) return "Brilliant";
            if (genetics.Adaptability == maxTrait) return "Adaptive";
            if (genetics.Social == maxTrait) return "Charismatic";

            return "Balanced";
        }

        /// <summary>
        /// Update engagement metrics
        /// </summary>
        public static SocialShareData UpdateEngagement(SocialShareData shareData, int likes, int comments, int shares)
        {
            shareData.LikeCount = likes;
            shareData.CommentCount = comments;
            shareData.ShareCount = shares;
            shareData.PopularityScore = CalculatePopularityScore(likes, comments, shares, shareData.ShareTimestamp);
            return shareData;
        }

        /// <summary>
        /// Check if share is trending
        /// </summary>
        public static bool IsTrending(SocialShareData shareData)
        {
            float hoursOld = (Time.time - shareData.ShareTimestamp) / 3600f;
            float engagementRate = (shareData.LikeCount + shareData.CommentCount * 2 + shareData.ShareCount * 3) / Mathf.Max(hoursOld, 1f);
            return engagementRate > 10f; // Trending threshold
        }

        private static float CalculatePopularityScore(int likes, int comments, int shares, uint timestamp)
        {
            float baseScore = likes + (comments * 2) + (shares * 3);
            float hoursOld = (Time.time - timestamp) / 3600f;
            return baseScore / Mathf.Max(hoursOld, 1f); // Time-weighted popularity
        }
    }
}