using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Laboratory.Subsystems.Trading
{
    #region Core Trading Data

    [Serializable]
    public class TradeOffer
    {
        public string offerId;
        public string sellerId;
        public string buyerId;
        public TradeOfferType offerType;
        public TradeOfferStatus status;
        public DateTime createdTime;
        public DateTime expiryTime;
        public TradeableItem offeredItem;
        public TradeableItem requestedItem;
        public CurrencyAmount offeredCurrency;
        public CurrencyAmount requestedCurrency;
        public List<TradeCondition> conditions = new();
        public TradeMetadata metadata = new();
        public bool isPublic = true;
        public string description;
        public List<string> tags = new();
    }

    [Serializable]
    public class TradeableItem
    {
        public TradeableItemType itemType;
        public string itemId;
        public string itemName;
        public int quantity;
        public ItemRarity rarity;
        public Dictionary<string, object> itemData = new();
        public ItemCondition condition;
        public DateTime acquisitionDate;
        public string originalOwnerId;
        public List<string> previousOwners = new();
        public bool isBound = false;
        public ItemMetadata metadata = new();
    }

    [Serializable]
    public class CurrencyAmount
    {
        public CurrencyType currencyType;
        public long amount;
        public string displayString;
    }

    [Serializable]
    public class TradeCondition
    {
        public TradeConditionType conditionType;
        public string description;
        public Dictionary<string, object> parameters = new();
        public bool isMet = false;
        public DateTime evaluationTime;
    }

    [Serializable]
    public class TradeMetadata
    {
        public float estimatedValue;
        public int viewCount;
        public int favoriteCount;
        public List<string> interestedBuyers = new();
        public TradeReputationData reputation = new();
        public bool isFeatured = false;
        public DateTime lastUpdated;
    }

    [Serializable]
    public class ItemMetadata
    {
        public float marketValue;
        public int tradingHistory;
        public float popularityScore;
        public List<TradeHistoryEntry> priceHistory = new();
        public ItemCertification certification;
        public bool isAuthenticated = true;
    }

    [Serializable]
    public class TradeHistoryEntry
    {
        public DateTime tradeDate;
        public long salePrice;
        public CurrencyType currency;
        public string sellerId;
        public string buyerId;
        public TradeOfferType tradeType;
    }

    [Serializable]
    public class ItemCertification
    {
        public bool isOriginal;
        public string breederId;
        public DateTime creationDate;
        public string geneticHash;
        public List<string> verificationStamps = new();
        public CertificationLevel level;
    }

    public enum TradeOfferType
    {
        DirectSale,
        Auction,
        Exchange,
        Loan,
        Rental,
        Consortium,
        Educational,
        Collaboration
    }

    public enum TradeOfferStatus
    {
        Draft,
        Active,
        Pending,
        Accepted,
        Completed,
        Cancelled,
        Expired,
        Disputed
    }

    public enum TradeableItemType
    {
        Creature,
        GeneticMaterial,
        ResearchData,
        BreedingRights,
        Species,
        Equipment,
        Resource,
        Cosmetic,
        License
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic,
        Unique
    }

    public enum ItemCondition
    {
        Perfect,
        Excellent,
        Good,
        Fair,
        Poor,
        Damaged
    }

    public enum CurrencyType
    {
        ResearchPoints,
        DiscoveryTokens,
        CommunityCredits,
        EducationalPoints,
        ConservationFunds,
        ReputationPoints
    }

    public enum TradeConditionType
    {
        MinimumLevel,
        RequiredAchievement,
        ResearchContribution,
        TimeRestriction,
        GeographicRestriction,
        EducationalStatus,
        CommunityStanding
    }

    public enum CertificationLevel
    {
        Basic,
        Verified,
        Premium,
        Authenticated,
        Legacy
    }

    #endregion

    #region Market Data

    [Serializable]
    public class MarketData
    {
        public Dictionary<string, ItemMarketInfo> itemPrices = new();
        public MarketTrends trends = new();
        public DateTime lastUpdate;
        public MarketVolume volume = new();
        public List<FeaturedTrade> featuredTrades = new();
        public MarketHealth health = new();
    }

    [Serializable]
    public class ItemMarketInfo
    {
        public string itemId;
        public TradeableItemType itemType;
        public long averagePrice;
        public long highestPrice;
        public long lowestPrice;
        public int totalTrades;
        public float priceChangePercentage;
        public PriceTrend trend;
        public DateTime lastTradeDate;
        public List<PricePoint> priceHistory = new();
    }

    [Serializable]
    public class MarketTrends
    {
        public Dictionary<TradeableItemType, float> demandByType = new();
        public Dictionary<ItemRarity, float> valueByRarity = new();
        public List<string> trendingItems = new();
        public List<string> emergingItems = new();
        public MarketSentiment sentiment;
        public float overallGrowth;
    }

    [Serializable]
    public class MarketVolume
    {
        public int dailyTrades;
        public int weeklyTrades;
        public int monthlyTrades;
        public long dailyValue;
        public long weeklyValue;
        public long monthlyValue;
        public int activeTrades;
        public int uniqueTraders;
    }

    [Serializable]
    public class FeaturedTrade
    {
        public string tradeId;
        public string title;
        public string description;
        public TradeableItem item;
        public string sellerId;
        public DateTime featuredUntil;
        public int priority;
    }

    [Serializable]
    public class MarketHealth
    {
        public float liquidityScore;
        public float stabilityScore;
        public float diversityScore;
        public float activityScore;
        public float overallHealthScore;
        public List<string> healthIndicators = new();
    }

    [Serializable]
    public class PricePoint
    {
        public DateTime timestamp;
        public long price;
        public int volume;
        public CurrencyType currency;
    }

    public enum PriceTrend
    {
        Rising,
        Falling,
        Stable,
        Volatile,
        Unknown
    }

    public enum MarketSentiment
    {
        Bullish,
        Bearish,
        Neutral,
        Uncertain
    }

    #endregion

    #region Player Trading Data

    [Serializable]
    public class PlayerTradingProfile
    {
        public string playerId;
        public TradeReputationData reputation = new();
        public TradingStatistics statistics = new();
        public List<string> activeOffers = new();
        public List<string> watchlist = new();
        public List<string> blacklist = new();
        public TradingPreferences preferences = new();
        public PlayerWallet wallet = new();
        public List<TradeNotification> notifications = new();
        public DateTime lastActivity;
    }

    [Serializable]
    public class TradeReputationData
    {
        public float overallRating;
        public int totalRatings;
        public int positiveRatings;
        public int neutralRatings;
        public int negativeRatings;
        public List<TradeReview> reviews = new();
        public ReputationBadges badges = new();
        public int trustScore;
        public bool isVerified;
    }

    [Serializable]
    public class TradingStatistics
    {
        public int totalTrades;
        public int successfulTrades;
        public int cancelledTrades;
        public int disputedTrades;
        public long totalValueTraded;
        public DateTime firstTradeDate;
        public DateTime lastTradeDate;
        public Dictionary<TradeableItemType, int> tradesByType = new();
        public float averageTradeValue;
        public float successRate;
    }

    [Serializable]
    public class TradingPreferences
    {
        public bool allowDirectMessages;
        public bool autoAcceptFromFriends;
        public List<TradeableItemType> interestedItemTypes = new();
        public List<TradeOfferType> preferredTradeTypes = new();
        public bool enablePriceAlerts;
        public bool enableOutbidNotifications;
        public CurrencyType preferredCurrency;
        public NotificationSettings notifications = new();
    }

    [Serializable]
    public class PlayerWallet
    {
        public Dictionary<CurrencyType, long> currencies = new();
        public List<WalletTransaction> transactionHistory = new();
        public DateTime lastUpdate;
        public WalletLimits limits = new();
        public bool isSecure = true;
    }

    [Serializable]
    public class TradeReview
    {
        public string reviewId;
        public string reviewerId;
        public string tradedWithId;
        public string tradeId;
        public int rating; // 1-5 stars
        public string comment;
        public DateTime reviewDate;
        public bool isVerified;
        public List<string> tags = new();
    }

    [Serializable]
    public class ReputationBadges
    {
        public bool trustworthyTrader;
        public bool fastShipper;
        public bool fairPricer;
        public bool helpfulCommunicator;
        public bool authenticItemSeller;
        public bool conservationSupporter;
        public bool educationalContributor;
        public Dictionary<string, DateTime> badgeEarned = new();
    }

    [Serializable]
    public class TradeNotification
    {
        public string notificationId;
        public NotificationType notificationType;
        public string title;
        public string message;
        public DateTime timestamp;
        public bool isRead;
        public string relatedTradeId;
        public Dictionary<string, object> data = new();
    }

    [Serializable]
    public class NotificationSettings
    {
        public bool enableTradeCompleted;
        public bool enableOfferReceived;
        public bool enablePriceAlerts;
        public bool enableWatchlistUpdates;
        public bool enableMarketTrends;
        public bool enableReputationChanges;
        public bool enableSystemMessages;
    }

    [Serializable]
    public class WalletTransaction
    {
        public string transactionId;
        public TransactionType transactionType;
        public CurrencyType currency;
        public long amount;
        public DateTime timestamp;
        public string description;
        public string relatedTradeId;
        public long balanceAfter;
    }

    [Serializable]
    public class WalletLimits
    {
        public Dictionary<CurrencyType, long> dailySpendLimits = new();
        public Dictionary<CurrencyType, long> maxBalances = new();
        public Dictionary<CurrencyType, long> dailySpent = new();
        public DateTime limitsResetDate;
        public bool hasEducationalLimits;
    }

    public enum NotificationType
    {
        TradeCompleted,
        OfferReceived,
        OfferAccepted,
        OfferCancelled,
        PriceAlert,
        WatchlistUpdate,
        ReputationChange,
        SystemMessage,
        MarketTrend
    }

    public enum TransactionType
    {
        TradePayment,
        TradeReceived,
        CurrencyEarned,
        CurrencySpent,
        SystemAward,
        Refund,
        Fee,
        Bonus
    }

    #endregion

    #region Service Interfaces

    /// <summary>
    /// Core trading marketplace service
    /// </summary>
    public interface ITradingMarketplaceService
    {
        Task<bool> InitializeAsync();
        Task<TradeOffer> CreateTradeOfferAsync(TradeOffer offer);
        Task<bool> CancelTradeOfferAsync(string offerId, string playerId);
        Task<bool> AcceptTradeOfferAsync(string offerId, string buyerId);
        Task<List<TradeOffer>> SearchTradesAsync(TradeSearchCriteria criteria);
        Task<TradeOffer> GetTradeOfferAsync(string offerId);
        Task<bool> UpdateTradeOfferAsync(TradeOffer offer);
    }

    /// <summary>
    /// Player currency and wallet management
    /// </summary>
    public interface ICurrencyService
    {
        Task<bool> InitializeAsync();
        Task<bool> AwardCurrencyAsync(string playerId, CurrencyType currencyType, long amount, string reason);
        Task<bool> SpendCurrencyAsync(string playerId, CurrencyType currencyType, long amount, string reason);
        Task<long> GetCurrencyBalanceAsync(string playerId, CurrencyType currencyType);
        Task<PlayerWallet> GetPlayerWalletAsync(string playerId);
        Task<bool> TransferCurrencyAsync(string fromPlayerId, string toPlayerId, CurrencyType currencyType, long amount);
    }

    /// <summary>
    /// Market data and pricing analytics
    /// </summary>
    public interface IMarketDataService
    {
        Task<bool> InitializeAsync();
        Task<MarketData> GetMarketDataAsync();
        Task<ItemMarketInfo> GetItemMarketInfoAsync(string itemId, TradeableItemType itemType);
        Task UpdateMarketTrendsAsync();
        Task<List<FeaturedTrade>> GetFeaturedTradesAsync();
        Task<float> EstimateItemValueAsync(TradeableItem item);
    }

    /// <summary>
    /// Trading reputation and review system
    /// </summary>
    public interface IReputationService
    {
        Task<bool> InitializeAsync();
        Task<bool> SubmitTradeReviewAsync(TradeReview review);
        Task<TradeReputationData> GetPlayerReputationAsync(string playerId);
        Task UpdateReputationAsync(string playerId, TradeOffer completedTrade);
        Task<bool> VerifyPlayerAsync(string playerId);
        Task AwardReputationBadgeAsync(string playerId, string badgeType);
    }

    /// <summary>
    /// Educational trading features and compliance
    /// </summary>
    public interface IEducationalTradingService
    {
        Task<bool> InitializeAsync();
        Task<bool> CreateEducationalTradeAsync(TradeOffer offer, string educatorId, string classroomId);
        Task<List<TradeOffer>> GetClassroomTradesAsync(string classroomId);
        Task<bool> ApproveEducationalTradeAsync(string tradeId, string educatorId);
        Task<TradingStatistics> GetClassroomTradingStatsAsync(string classroomId);
        Task<bool> SetTradingLimitsAsync(string studentId, WalletLimits limits);
    }

    #endregion

    #region Search and Filtering

    [Serializable]
    public class TradeSearchCriteria
    {
        public List<TradeableItemType> itemTypes = new();
        public List<ItemRarity> rarities = new();
        public List<TradeOfferType> tradeTypes = new();
        public CurrencyType preferredCurrency;
        public long minPrice = 0;
        public long maxPrice = long.MaxValue;
        public string searchTerm;
        public List<string> tags = new();
        public SortOrder sortOrder = SortOrder.CreatedTimeDesc;
        public int maxResults = 50;
        public int pageOffset = 0;
        public bool includeExpired = false;
        public bool onlyVerifiedSellers = false;
        public bool onlyFeatured = false;
        public string excludeSellerId;
    }

    public enum SortOrder
    {
        CreatedTimeAsc,
        CreatedTimeDesc,
        PriceAsc,
        PriceDesc,
        PopularityDesc,
        RarityDesc,
        NameAsc,
        NameDesc,
        ReputationDesc
    }

    #endregion

    #region Events and Notifications

    [Serializable]
    public class TradingEvent
    {
        public TradingEventType eventType;
        public DateTime timestamp;
        public string playerId;
        public string tradeId;
        public Dictionary<string, object> eventData = new();
    }

    public enum TradingEventType
    {
        TradeCreated,
        TradeAccepted,
        TradeCompleted,
        TradeCancelled,
        TradeExpired,
        OfferReceived,
        ReputationChanged,
        CurrencyEarned,
        CurrencySpent,
        MarketTrendAlert,
        PriceAlert,
        WatchlistUpdated
    }

    [Serializable]
    public class TradingEventData
    {
        public string eventId;
        public TradingEventType eventType;
        public DateTime timestamp;
        public Dictionary<string, object> parameters = new();
        public string triggeredBy;
        public List<string> affectedPlayers = new();
    }

    #endregion

    #region Trade Transactions

    [Serializable]
    public class TradeTransaction
    {
        public string transactionId;
        public string sellerId;
        public string buyerId;
        public TradeTransactionType transactionType;
        public TransactionStatus status;
        public DateTime createdTime;
        public DateTime completedTime;
        public Dictionary<string, object> tradeData = new();
        public CurrencyAmount transactionValue;
        public float transactionFee;
        public string notes;
        public List<string> participants = new();
    }

    public enum TradeTransactionType
    {
        GeneticTrade,
        ResearchCollaboration,
        ResourceTrade,
        ConsortiumContribution,
        CurrencyExchange,
        ItemTrade
    }

    public enum TransactionStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Disputed
    }

    [Serializable]
    public class ResearchConsortium
    {
        public string consortiumId;
        public string consortiumName;
        public string description;
        public List<string> memberIds = new();
        public string leaderId;
        public DateTime createdTime;
        public ResearchConsortiumStatus status;
        public Dictionary<string, object> researchData = new();
        public Dictionary<string, float> memberContributions = new();
        public float totalInvestment;
        public List<string> completedProjects = new();
        public List<string> activeProjects = new();
    }

    public enum ResearchConsortiumStatus
    {
        Active,
        Recruiting,
        ResearchPhase,
        CompletionPhase,
        Dissolved,
        Archived
    }

    [Serializable]
    public class EconomyMetrics
    {
        public int totalPlayers;
        public int activeOffers;
        public int totalTransactions;
        public int successfulTransactions;
        public int failedTransactions;
        public float totalTransactionValue;
        public float averageWealthPerPlayer;
        public float currencyInflation;
        public DateTime lastUpdated;
    }

    [Serializable]
    public class ConsortiumEvent
    {
        public string eventId;
        public string consortiumId;
        public ConsortiumEventType eventType;
        public DateTime timestamp;
        public string description;
        public Dictionary<string, object> eventData = new();
        public List<string> participantIds = new();
    }

    [Serializable]
    public class EconomyEvent
    {
        public string eventId;
        public EconomyEventType eventType;
        public DateTime timestamp;
        public string description;
        public Dictionary<string, object> eventData = new();
        public float economicImpact;
        public List<string> affectedPlayers = new();
    }

    public enum ConsortiumEventType
    {
        Created,
        MemberJoined,
        MemberLeft,
        ProjectStarted,
        ProjectCompleted,
        FundingReceived,
        Dissolved
    }

    public enum EconomyEventType
    {
        MarketCrash,
        MarketBoom,
        CurrencyInflation,
        CurrencyDeflation,
        TradeVolumeSpike,
        TradeVolumeDrop,
        NewTradeableDiscovered,
        PriceAlert
    }

    [Serializable]
    public class TradeOfferRequest
    {
        public TradeableItem offeredItem;
        public TradeableItem requestedItem;
        public CurrencyAmount offeredCurrency;
        public CurrencyAmount requestedCurrency;
        public List<TradeCondition> conditions = new();
        public string description;
        public List<string> tags = new();
        public DateTime expiryTime;
        public bool isPublic = true;
    }

    [Serializable]
    public class ConsortiumRequest
    {
        public string requestId;
        public string requesterId;
        public string consortiumId;
        public ConsortiumRequestType requestType;
        public string description;
        public Dictionary<string, object> requestData = new();
        public float contributionAmount;
        public DateTime requestTime;
        public DateTime deadline;
        public ConsortiumRequestStatus status;
    }

    public enum ConsortiumRequestType
    {
        Join,
        Leave,
        Contribute,
        StartProject,
        VoteOnProposal,
        RequestFunding
    }

    public enum ConsortiumRequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Completed,
        Cancelled
    }

    [Serializable]
    public class MarketplaceFilter
    {
        public List<TradeableItemType> itemTypes = new();
        public List<ItemRarity> rarities = new();
        public CurrencyAmount minPrice;
        public CurrencyAmount maxPrice;
        public List<string> tags = new();
        public string searchTerm;
        public MarketplaceSortBy sortBy = MarketplaceSortBy.TimeCreated;
        public bool sortDescending = true;
    }

    public enum MarketplaceSortBy
    {
        TimeCreated,
        Price,
        Rarity,
        PopularityScore,
        TimeRemaining
    }

    #endregion

    #region Service Interfaces

    /// <summary>
    /// Genetic marketplace service interface
    /// </summary>
    public interface IGeneticMarketplaceService
    {
        Task<bool> InitializeAsync();
        Task<List<TradeOffer>> GetActiveOffersAsync(TradeableItemType itemType);
        Task<bool> CreateOfferAsync(TradeOffer offer);
        Task<bool> CancelOfferAsync(string offerId);
        Task<TradeOffer> GetOfferAsync(string offerId);
        float CalculateMarketValue(TradeableItem item);
    }

    /// <summary>
    /// Research consortium service interface
    /// </summary>
    public interface IResearchConsortiumService
    {
        Task<bool> InitializeAsync();
        Task<ResearchConsortium> CreateConsortiumAsync(string name, string leaderId);
        Task<bool> JoinConsortiumAsync(string consortiumId, string memberId);
        Task<bool> LeaveConsortiumAsync(string consortiumId, string memberId);
        Task<List<ResearchConsortium>> GetActiveConsortiumsAsync();
        Task<bool> ContributeToConsortiumAsync(string consortiumId, string memberId, float amount);
    }

    /// <summary>
    /// Educational licensing service interface
    /// </summary>
    public interface IEducationalLicensingService
    {
        Task<bool> InitializeAsync();
        Task<bool> CreateLicenseAsync(string itemId, string educatorId, float price);
        Task<bool> PurchaseLicenseAsync(string licenseId, string buyerId);
        Task<List<TradeOffer>> GetEducationalOffersAsync();
        bool IsEducationalContext(string userId);
        float CalculateEducationalDiscount(string userId);
    }

    /// <summary>
    /// Economy management service interface
    /// </summary>
    public interface IEconomyManagementService
    {
        Task<bool> InitializeAsync();
        EconomyMetrics GetEconomyMetrics();
        Task<bool> ProcessInflationAsync();
        Task<bool> AdjustCurrencySupplyAsync(float adjustment);
        Task<bool> TriggerEconomyEventAsync(EconomyEvent economyEvent);
        float CalculateInflationRate();
        Task<bool> MonitorEconomyHealthAsync();
    }

    #endregion
}