using UnityEngine;
using System.Collections.Generic;
using System;
using Laboratory.Chimera.Core;

namespace Laboratory.Economy
{
    /// <summary>
    /// Comprehensive data structures for the breeding marketplace system including
    /// listings, auctions, transactions, social features, and market analytics.
    /// All classes are serializable for networking and persistence.
    /// </summary>

    [System.Serializable]
    public class MarketplaceConfig
    {
        [Header("Core Settings")]
        public float baseTransactionFee = 5f;
        public int maxListingsPerPlayer = 10;
        public float defaultListingDuration = 7f; // days
        public float maxAuctionDuration = 3f; // days

        [Header("Economic Settings")]
        public float marketVolatilityFactor = 0.1f;
        public bool enableDynamicPricing = true;
        public float baseCreatureValue = 100f;
        public float priceFluctuation = 0.05f;

        [Header("Social Features")]
        public bool enableReputationSystem = true;
        public bool enableBreedingGuilds = true;
        public int maxGuildMembers = 50;
        public float reputationDecayRate = 0.01f;

        [Header("Quality Controls")]
        public float minimumListingPrice = 10f;
        public float maximumListingPrice = 100000f;
        public int minimumSellerLevel = 5;
        public bool requireCreatureOwnership = true;
    }

    [System.Serializable]
    public class MarketplaceListing
    {
        [Header("Core Information")]
        public string listingId;
        public string sellerId;
        public string sellerName;
        public CreatureGenome creature;

        [Header("Pricing")]
        public float askingPrice;
        public float estimatedValue;
        public bool acceptOffers = true;
        public float minimumOffer = 0f;

        [Header("Listing Details")]
        public ListingType listingType = ListingType.FixedPrice;
        public ListingStatus status = ListingStatus.Active;
        public float creationTime;
        public float expirationTime;
        public string description;
        public List<string> tags = new List<string>();

        [Header("Bloodline Information")]
        public BloodlineInfo bloodlineInfo;
        public bool isFeaturedListing = false;
        public int viewCount = 0;
        public int favoriteCount = 0;

        [Header("Transaction History")]
        public List<OfferData> offers = new List<OfferData>();
        public List<string> interestedBuyers = new List<string>();

        public bool IsExpired => Time.time >= expirationTime;
        public bool IsActive => status == ListingStatus.Active && !IsExpired;
        public float TimeRemaining => Mathf.Max(0f, expirationTime - Time.time);
    }

    [System.Serializable]
    public class AuctionData
    {
        [Header("Core Information")]
        public string auctionId;
        public string sellerId;
        public string sellerName;
        public CreatureGenome creature;

        [Header("Auction Settings")]
        public float startingBid;
        public float reservePrice;
        public float currentBid;
        public string currentBidderId;
        public string currentBidderName;

        [Header("Timing")]
        public float startTime;
        public float endTime;
        public AuctionStatus status = AuctionStatus.Active;
        public bool hasReserveBeenMet = false;

        [Header("Bidding History")]
        public List<BidData> bids = new List<BidData>();
        public int totalBids => bids.Count;
        public int uniqueBidders => bids.Select(b => b.bidderId).Distinct().Count();

        [Header("Analytics")]
        public float estimatedValue;
        public int viewCount = 0;
        public int watchlistCount = 0;

        public bool IsActive => status == AuctionStatus.Active && Time.time < endTime;
        public float TimeRemaining => Mathf.Max(0f, endTime - Time.time);
        public bool HasBids => bids.Count > 0;
    }

    [System.Serializable]
    public class BidData
    {
        public string bidderId;
        public string bidderName;
        public float amount;
        public float timestamp;
        public bool isAutoBid = false;
        public float maxAutoBidAmount = 0f;

        public BidData() { }

        public BidData(string bidderId, string bidderName, float amount)
        {
            this.bidderId = bidderId;
            this.bidderName = bidderName;
            this.amount = amount;
            this.timestamp = Time.time;
        }
    }

    [System.Serializable]
    public class OfferData
    {
        public string offerId;
        public string buyerId;
        public string buyerName;
        public float offerAmount;
        public float timestamp;
        public OfferStatus status = OfferStatus.Pending;
        public string message;
        public float expirationTime;

        public bool IsExpired => Time.time >= expirationTime;
        public bool IsPending => status == OfferStatus.Pending && !IsExpired;
    }

    [System.Serializable]
    public class MarketTransaction
    {
        [Header("Core Information")]
        public string transactionId;
        public string buyerId;
        public string buyerName;
        public string sellerId;
        public string sellerName;

        [Header("Transaction Details")]
        public CreatureGenome creature;
        public float price;
        public float transactionFee;
        public float netSellerProceeds => price - transactionFee;
        public TransactionType transactionType;

        [Header("Metadata")]
        public float timestamp;
        public string marketConditions;
        public Dictionary<string, object> additionalData = new Dictionary<string, object>();

        [Header("Analytics")]
        public bool wasRareFind = false;
        public float marketPriceAtTime = 0f;
        public float priceDeviationFromMarket = 0f;
    }

    [System.Serializable]
    public class BloodlineInfo
    {
        public Guid lineageId;
        public string bloodlineName;
        public int generation;
        public float prestigeScore;
        public List<string> notableAchievements = new List<string>();
        public Dictionary<string, float> traitAverages = new Dictionary<string, float>();
        public int totalOffspring;
        public float averageSalePrice;
        public bool isLegendaryLineage = false;
    }

    [System.Serializable]
    public class BloodlineData
    {
        public Guid lineageId;
        public string founderCreatureId;
        public string bloodlineName;
        public List<string> memberCreatureIds = new List<string>();
        public Dictionary<string, float> averageTraits = new Dictionary<string, float>();
        public List<BloodlineAchievement> achievements = new List<BloodlineAchievement>();
        public float totalMarketValue = 0f;
        public int totalTransactions = 0;
        public float averageTransactionValue = 0f;
        public float prestigeScore = 0f;
        public bool isLegendaryStatus = false;
    }

    [System.Serializable]
    public class PlayerMarketData
    {
        [Header("Core Statistics")]
        public string playerId;
        public string playerName;
        public int totalListings = 0;
        public int totalSales = 0;
        public int totalPurchases = 0;

        [Header("Financial Data")]
        public float totalSalesVolume = 0f;
        public float totalPurchaseVolume = 0f;
        public float averageSalePrice = 0f;
        public float averagePurchasePrice = 0f;
        public float totalTransactionFees = 0f;

        [Header("Performance Metrics")]
        public float salesSuccessRate = 0f; // Percentage of listings that sold
        public float averageTimeToSale = 0f; // Average days to sell
        public float reputationScore = 100f;
        public PlayerMarketTier marketTier = PlayerMarketTier.Novice;

        [Header("Specializations")]
        public List<CreatureSpecies> specializedSpecies = new List<CreatureSpecies>();
        public Dictionary<BiomeType, int> biomeExpertise = new Dictionary<BiomeType, int>();
        public List<string> marketAchievements = new List<string>();

        [Header("Social Data")]
        public List<string> favoriteBreederIds = new List<string>();
        public List<string> blockedPlayerIds = new List<string>();
        public int positiveReviews = 0;
        public int negativeReviews = 0;
    }

    [System.Serializable]
    public class PlayerReputation
    {
        public string playerId;
        public float overallScore = 100f;
        public float sellerRating = 100f;
        public float buyerRating = 100f;
        public int totalReviews = 0;
        public int positiveReviews = 0;
        public int negativeReviews = 0;
        public List<ReputationEvent> recentEvents = new List<ReputationEvent>();
        public Dictionary<string, float> categoryScores = new Dictionary<string, float>();
        public bool isTrustedTrader = false;
        public float lastUpdated;
    }

    [System.Serializable]
    public class BreedingGuild
    {
        [Header("Guild Information")]
        public string guildId;
        public string guildName;
        public string description;
        public string founderId;
        public float creationTime;

        [Header("Membership")]
        public List<string> memberIds = new List<string>();
        public Dictionary<string, GuildRole> memberRoles = new Dictionary<string, GuildRole>();
        public int maxMembers = 50;

        [Header("Guild Stats")]
        public int totalBreedingProjects = 0;
        public int completedProjects = 0;
        public float totalMarketVolume = 0f;
        public List<string> guildAchievements = new List<string>();

        [Header("Guild Features")]
        public bool isPublic = true;
        public bool requiresApproval = true;
        public List<CreatureSpecies> specializedSpecies = new List<CreatureSpecies>();
        public Dictionary<string, object> guildResources = new Dictionary<string, object>();

        public bool CanAcceptNewMembers => memberIds.Count < maxMembers;
        public bool IsMember(string playerId) => memberIds.Contains(playerId);
    }

    [System.Serializable]
    public class CollaborativeProject
    {
        [Header("Project Information")]
        public string projectId;
        public string initiatorId;
        public CollaborativeProjectData projectData;

        [Header("Participants")]
        public List<string> participants = new List<string>();
        public Dictionary<string, ProjectContribution> contributions = new Dictionary<string, ProjectContribution>();
        public int maxParticipants => projectData.maximumParticipants;

        [Header("Project Status")]
        public ProjectStatus status = ProjectStatus.Recruiting;
        public float creationTime;
        public float targetCompletion;
        public float actualCompletion = 0f;
        public float progressPercentage = 0f;

        [Header("Results")]
        public List<CreatureGenome> resultingOffspring = new List<CreatureGenome>();
        public Dictionary<string, float> participantRewards = new Dictionary<string, float>();
        public ProjectOutcome outcome;

        public bool CanAcceptParticipants => participants.Count < maxParticipants && status == ProjectStatus.Recruiting;
        public bool IsParticipant(string playerId) => participants.Contains(playerId);
        public float TimeRemaining => Mathf.Max(0f, targetCompletion - Time.time);
    }

    [System.Serializable]
    public class CollaborativeProjectData
    {
        [Header("Project Details")]
        public string title;
        public string description;
        public ProjectType projectType;
        public ProjectDifficulty difficulty;

        [Header("Requirements")]
        public int minimumParticipants = 2;
        public int maximumParticipants = 6;
        public float estimatedDurationDays = 7f;
        public List<CreatureSpecies> requiredSpecies = new List<CreatureSpecies>();
        public List<BiomeType> requiredBiomes = new List<BiomeType>();

        [Header("Goals")]
        public List<ProjectGoal> goals = new List<ProjectGoal>();
        public CreatureGenome targetOutcome;
        public Dictionary<string, float> targetTraits = new Dictionary<string, float>();

        [Header("Rewards")]
        public float experienceReward = 500f;
        public Dictionary<string, object> rewards = new Dictionary<string, object>();
        public bool shareResults = true;
    }

    [System.Serializable]
    public class ProjectContribution
    {
        public string contributorId;
        public ContributionType contributionType;
        public List<CreatureGenome> contributedCreatures = new List<CreatureGenome>();
        public Dictionary<string, float> resourceContributions = new Dictionary<string, float>();
        public float timeContributed = 0f;
        public float valueContributed = 0f;
        public bool isLeadContributor = false;
    }

    [System.Serializable]
    public class CreatureValuationWeights
    {
        [Header("Genetic Factors")]
        [Range(0f, 2f)] public float fitnessWeight = 1f;
        [Range(0f, 2f)] public float rarityWeight = 1.5f;
        [Range(0f, 2f)] public float generationWeight = 0.8f;
        [Range(0f, 2f)] public float traitQualityWeight = 1.2f;

        [Header("Market Factors")]
        [Range(0f, 2f)] public float demandWeight = 1.3f;
        [Range(0f, 2f)] public float bloodlinePrestigeWeight = 1.1f;
        [Range(0f, 2f)] public float speciesPopularityWeight = 0.9f;
        [Range(0f, 2f)] public float marketTrendWeight = 0.7f;

        [Header("Social Factors")]
        [Range(0f, 2f)] public float sellerReputationWeight = 0.5f;
        [Range(0f, 2f)] public float achievementWeight = 0.6f;
        [Range(0f, 2f)] public float uniquenessWeight = 1.4f;
    }

    [System.Serializable]
    public class MarketAnalytics
    {
        [Header("Transaction Data")]
        public int totalTransactions = 0;
        public float totalVolume = 0f;
        public float dailyVolume = 0f;
        public float averageTransactionValue = 0f;

        [Header("Market Activity")]
        public int activeListings = 0;
        public int activeAuctions = 0;
        public int completedListings = 0;
        public int completedAuctions = 0;

        [Header("Trends")]
        public MarketTrend overallTrend = MarketTrend.Stable;
        public Dictionary<CreatureSpecies, MarketTrend> speciesTrends = new Dictionary<CreatureSpecies, MarketTrend>();
        public Dictionary<CreatureSpecies, float> averagePrices = new Dictionary<CreatureSpecies, float>();

        [Header("Analytics Events")]
        public Queue<AnalyticsEvent> recentEvents = new Queue<AnalyticsEvent>();

        public void TrackListingCreated(MarketplaceListing listing)
        {
            activeListings++;
            var analyticsEvent = new AnalyticsEvent
            {
                eventType = "ListingCreated",
                timestamp = Time.time,
                data = new Dictionary<string, object>
                {
                    ["listingId"] = listing.listingId,
                    ["species"] = listing.creature.species.ToString(),
                    ["price"] = listing.askingPrice,
                    ["sellerId"] = listing.sellerId
                }
            };
            recentEvents.Enqueue(analyticsEvent);

            // Keep only recent events
            while (recentEvents.Count > 1000)
            {
                recentEvents.Dequeue();
            }
        }

        public void TrackTransaction(MarketTransaction transaction)
        {
            totalTransactions++;
            totalVolume += transaction.price;
            dailyVolume += transaction.price;
            averageTransactionValue = totalVolume / totalTransactions;

            var analyticsEvent = new AnalyticsEvent
            {
                eventType = "TransactionCompleted",
                timestamp = Time.time,
                data = new Dictionary<string, object>
                {
                    ["transactionId"] = transaction.transactionId,
                    ["species"] = transaction.creature.species.ToString(),
                    ["price"] = transaction.price,
                    ["buyerId"] = transaction.buyerId,
                    ["sellerId"] = transaction.sellerId
                }
            };
            recentEvents.Enqueue(analyticsEvent);
        }

        public void TrackAuctionCreated(AuctionData auction)
        {
            activeAuctions++;
            var analyticsEvent = new AnalyticsEvent
            {
                eventType = "AuctionCreated",
                timestamp = Time.time,
                data = new Dictionary<string, object>
                {
                    ["auctionId"] = auction.auctionId,
                    ["species"] = auction.creature.species.ToString(),
                    ["startingBid"] = auction.startingBid,
                    ["reservePrice"] = auction.reservePrice,
                    ["sellerId"] = auction.sellerId
                }
            };
            recentEvents.Enqueue(analyticsEvent);
        }

        public void TrackBidPlaced(AuctionData auction, BidData bid)
        {
            var analyticsEvent = new AnalyticsEvent
            {
                eventType = "BidPlaced",
                timestamp = Time.time,
                data = new Dictionary<string, object>
                {
                    ["auctionId"] = auction.auctionId,
                    ["bidAmount"] = bid.amount,
                    ["bidderId"] = bid.bidderId
                }
            };
            recentEvents.Enqueue(analyticsEvent);
        }
    }

    [System.Serializable]
    public class MarketStatistics
    {
        public int totalActiveListings;
        public int totalActiveAuctions;
        public float totalMarketVolume;
        public float dailyTransactionVolume;
        public int totalTransactions;
        public float averageTransactionValue;
        public int activeGuilds;
        public int activeCollaborativeProjects;
        public List<CreatureSpecies> topSellingSpecies;
        public Dictionary<CreatureSpecies, MarketTrend> marketTrends;
        public Dictionary<CreatureSpecies, float> averagePriceBySpecies;
    }

    [System.Serializable]
    public class SpeciesMarketAnalysis
    {
        public CreatureSpecies species;
        public List<MarketplaceListing> currentListings;
        public List<MarketTransaction> recentSales;
        public PriceHistory priceHistory;
        public float averagePrice;
        public Vector2 priceRange; // x = min, y = max
        public MarketTrend marketTrend;
        public float demandLevel; // 0-1 scale
        public float supplyLevel; // 0-1 scale
    }

    [System.Serializable]
    public class PriceHistory
    {
        public CreatureSpecies species;
        public List<PriceDataPoint> dataPoints = new List<PriceDataPoint>();
        public float averagePrice;
        public float lowestPrice;
        public float highestPrice;
        public MarketTrend trend;
        public float volatility;

        public void AddDataPoint(float price, float timestamp)
        {
            dataPoints.Add(new PriceDataPoint { price = price, timestamp = timestamp });

            // Keep only recent data points (e.g., last 30 days)
            float cutoffTime = timestamp - (30 * 24 * 3600); // 30 days ago
            dataPoints.RemoveAll(dp => dp.timestamp < cutoffTime);

            // Recalculate statistics
            RecalculateStatistics();
        }

        private void RecalculateStatistics()
        {
            if (dataPoints.Count == 0) return;

            var prices = dataPoints.Select(dp => dp.price).ToList();
            averagePrice = prices.Average();
            lowestPrice = prices.Min();
            highestPrice = prices.Max();

            // Calculate trend and volatility
            CalculateTrend();
            CalculateVolatility();
        }

        private void CalculateTrend()
        {
            if (dataPoints.Count < 2)
            {
                trend = MarketTrend.Stable;
                return;
            }

            // Simple trend calculation based on recent vs older data
            var recent = dataPoints.TakeLast(dataPoints.Count / 3).Select(dp => dp.price).Average();
            var older = dataPoints.Take(dataPoints.Count / 3).Select(dp => dp.price).Average();

            float changePercentage = (recent - older) / older * 100f;

            trend = changePercentage switch
            {
                > 10f => MarketTrend.StronglyRising,
                > 5f => MarketTrend.Rising,
                > -5f => MarketTrend.Stable,
                > -10f => MarketTrend.Declining,
                _ => MarketTrend.StronglyDeclining
            };
        }

        private void CalculateVolatility()
        {
            if (dataPoints.Count < 2)
            {
                volatility = 0f;
                return;
            }

            var prices = dataPoints.Select(dp => dp.price).ToList();
            float variance = prices.Select(p => Mathf.Pow(p - averagePrice, 2)).Average();
            volatility = Mathf.Sqrt(variance) / averagePrice; // Coefficient of variation
        }
    }

    [System.Serializable]
    public class PriceDataPoint
    {
        public float price;
        public float timestamp;
    }

    // Enums for marketplace system
    public enum ListingType
    {
        FixedPrice,
        BestOffer,
        Auction,
        Trade
    }

    public enum ListingStatus
    {
        Active,
        Sold,
        Expired,
        Cancelled,
        UnderReview
    }

    public enum AuctionStatus
    {
        Active,
        Ended,
        Sold,
        Cancelled,
        NoSale
    }

    public enum OfferStatus
    {
        Pending,
        Accepted,
        Rejected,
        Expired,
        Withdrawn
    }

    public enum TransactionType
    {
        DirectSale,
        Auction,
        PrivateOffer,
        Trade,
        GuildTransaction,
        CollaborativeProject
    }

    public enum MarketTrend
    {
        StronglyDeclining,
        Declining,
        Stable,
        Rising,
        StronglyRising
    }

    public enum PlayerMarketTier
    {
        Novice,
        Apprentice,
        Skilled,
        Expert,
        Master,
        Legendary
    }

    public enum GuildRole
    {
        Member,
        Specialist,
        Officer,
        Leader,
        Founder
    }

    public enum ProjectStatus
    {
        Planning,
        Recruiting,
        Active,
        Completed,
        Failed,
        Cancelled
    }

    public enum ProjectType
    {
        TraitImprovement,
        NewBreedDevelopment,
        LegendaryQuest,
        ResearchProject,
        ConservationEffort,
        CompetitionPrep
    }

    public enum ProjectDifficulty
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert,
        Legendary
    }

    public enum ContributionType
    {
        CreatureContribution,
        ResourceContribution,
        KnowledgeSharing,
        FacilityAccess,
        TimeInvestment,
        LeadershipRole
    }

    public enum ProjectOutcome
    {
        GreatSuccess,
        Success,
        PartialSuccess,
        Failure,
        Cancelled
    }

    public enum ReputationAction
    {
        Purchase,
        Sale,
        AuctionWin,
        AuctionSale,
        ProjectParticipation,
        HelpfulReview,
        NegativeExperience
    }

    // Supporting data structures
    [System.Serializable]
    public class ReputationEvent
    {
        public string eventType;
        public float impactValue;
        public float timestamp;
        public string description;
        public string relatedPlayerId;
    }

    [System.Serializable]
    public class BloodlineAchievement
    {
        public string achievementName;
        public string description;
        public float timestamp;
        public string achievedByCreatureId;
        public float prestigeValue;
    }

    [System.Serializable]
    public class ProjectGoal
    {
        public string goalDescription;
        public bool isCompleted = false;
        public float progressPercentage = 0f;
        public Dictionary<string, object> requirements = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class AnalyticsEvent
    {
        public string eventType;
        public float timestamp;
        public Dictionary<string, object> data = new Dictionary<string, object>();
    }

    /// <summary>
    /// Utility class for marketplace calculations and validation
    /// </summary>
    public static class MarketplaceUtilities
    {
        public static float CalculateSellerRating(PlayerMarketData marketData)
        {
            if (marketData.totalSales == 0) return 100f;

            float baseRating = 100f;
            float successRateBonus = (marketData.salesSuccessRate - 0.5f) * 40f; // +/-20 points
            float volumeBonus = Mathf.Log10(marketData.totalSalesVolume + 1) * 5f; // Volume bonus
            float timeBonus = marketData.averageTimeToSale < 3f ? 10f : 0f; // Quick sales bonus

            return Mathf.Clamp(baseRating + successRateBonus + volumeBonus + timeBonus, 0f, 150f);
        }

        public static PlayerMarketTier DetermineMarketTier(PlayerMarketData marketData)
        {
            float totalVolume = marketData.totalSalesVolume + marketData.totalPurchaseVolume;
            int totalTransactions = marketData.totalSales + marketData.totalPurchases;

            return (totalVolume, totalTransactions) switch
            {
                ( >= 100000f, >= 100) => PlayerMarketTier.Legendary,
                ( >= 50000f, >= 50) => PlayerMarketTier.Master,
                ( >= 10000f, >= 25) => PlayerMarketTier.Expert,
                ( >= 5000f, >= 10) => PlayerMarketTier.Skilled,
                ( >= 1000f, >= 5) => PlayerMarketTier.Apprentice,
                _ => PlayerMarketTier.Novice
            };
        }

        public static bool IsListingSuspicious(MarketplaceListing listing, float marketAverage)
        {
            float priceDeviation = Mathf.Abs(listing.askingPrice - marketAverage) / marketAverage;
            return priceDeviation > 0.5f; // More than 50% deviation might be suspicious
        }

        public static float CalculateMarketConfidence(List<MarketTransaction> recentTransactions)
        {
            if (recentTransactions.Count < 5) return 0.5f;

            // Calculate price volatility
            var prices = recentTransactions.Select(t => t.price).ToList();
            float average = prices.Average();
            float variance = prices.Select(p => Mathf.Pow(p - average, 2)).Average();
            float volatility = Mathf.Sqrt(variance) / average;

            // Lower volatility = higher confidence
            return Mathf.Clamp01(1f - volatility);
        }

        public static string GenerateMarketSummary(MarketStatistics stats)
        {
            return $"Market Overview:\n" +
                   $"• {stats.totalActiveListings} active listings\n" +
                   $"• {stats.totalActiveAuctions} active auctions\n" +
                   $"• {stats.totalMarketVolume:C} total volume\n" +
                   $"• {stats.averageTransactionValue:C} avg transaction\n" +
                   $"• {stats.activeGuilds} active guilds\n" +
                   $"• {stats.activeCollaborativeProjects} collaborative projects";
        }
    }
}