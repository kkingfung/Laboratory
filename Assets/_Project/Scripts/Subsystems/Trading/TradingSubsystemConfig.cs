using UnityEngine;
using System.Collections.Generic;
using System;

namespace Laboratory.Subsystems.Trading
{
    /// <summary>
    /// Configuration ScriptableObject for the Trading & Economy Subsystem.
    /// Controls marketplace behavior, currency management, and educational trading features.
    /// </summary>
    [CreateAssetMenu(fileName = "TradingSubsystemConfig", menuName = "Project Chimera/Subsystems/Trading Config")]
    public class TradingSubsystemConfig : ScriptableObject
    {
        [Header("Core Settings")]
        [Tooltip("Background processing interval in milliseconds")]
        [Range(1000, 10000)]
        public int backgroundProcessingIntervalMs = 5000;

        [Tooltip("Enable debug logging for trading operations")]
        public bool enableDebugLogging = false;

        [Tooltip("Maximum concurrent trades per player")]
        [Range(1, 50)]
        public int maxConcurrentTradesPerPlayer = 10;

        [Tooltip("Trade offer expiry time in hours")]
        [Range(1f, 168f)]
        public float defaultTradeExpiryHours = 48f;

        [Header("Marketplace Settings")]
        [Tooltip("Enable public marketplace")]
        public bool enablePublicMarketplace = true;

        [Tooltip("Enable auction system")]
        public bool enableAuctions = true;

        [Tooltip("Enable direct trading between players")]
        public bool enableDirectTrading = true;

        [Tooltip("Marketplace commission percentage")]
        [Range(0f, 10f)]
        public float marketplaceCommissionPercent = 2.5f;

        [Tooltip("Featured trade cost in discovery tokens")]
        [Range(1, 1000)]
        public int featuredTradeCost = 100;

        [Tooltip("Maximum featured trades displayed")]
        [Range(1, 20)]
        public int maxFeaturedTrades = 8;

        [Header("Currency Configuration")]
        [Tooltip("Starting currency amounts for new players")]
        public List<CurrencyStartingAmount> startingCurrencies = new List<CurrencyStartingAmount>
        {
            new CurrencyStartingAmount { currencyType = CurrencyType.ResearchPoints, amount = 1000 },
            new CurrencyStartingAmount { currencyType = CurrencyType.DiscoveryTokens, amount = 100 },
            new CurrencyStartingAmount { currencyType = CurrencyType.CommunityCredits, amount = 50 }
        };

        [Tooltip("Daily currency earning limits")]
        public List<CurrencyDailyLimit> dailyEarningLimits = new List<CurrencyDailyLimit>
        {
            new CurrencyDailyLimit { currencyType = CurrencyType.ResearchPoints, limit = 5000 },
            new CurrencyDailyLimit { currencyType = CurrencyType.DiscoveryTokens, limit = 500 },
            new CurrencyDailyLimit { currencyType = CurrencyType.CommunityCredits, limit = 1000 }
        };

        [Tooltip("Maximum wallet balances")]
        public List<CurrencyMaxBalance> maxBalances = new List<CurrencyMaxBalance>
        {
            new CurrencyMaxBalance { currencyType = CurrencyType.ResearchPoints, maxBalance = 100000 },
            new CurrencyMaxBalance { currencyType = CurrencyType.DiscoveryTokens, maxBalance = 50000 },
            new CurrencyMaxBalance { currencyType = CurrencyType.CommunityCredits, maxBalance = 25000 }
        };

        [Header("Educational Features")]
        [Tooltip("Enable educational trading mode")]
        public bool enableEducationalMode = true;

        [Tooltip("Require educator approval for student trades")]
        public bool requireEducatorApproval = true;

        [Tooltip("Student daily spending limits")]
        public List<CurrencyDailyLimit> studentSpendingLimits = new List<CurrencyDailyLimit>
        {
            new CurrencyDailyLimit { currencyType = CurrencyType.ResearchPoints, limit = 500 },
            new CurrencyDailyLimit { currencyType = CurrencyType.DiscoveryTokens, limit = 50 },
            new CurrencyDailyLimit { currencyType = CurrencyType.CommunityCredits, limit = 100 }
        };

        [Tooltip("Educational trade value limits")]
        [Range(1, 10000)]
        public int maxEducationalTradeValue = 1000;

        [Tooltip("Classroom trading session duration in minutes")]
        [Range(15, 480)]
        public int classroomSessionDurationMinutes = 60;

        [Header("Research Consortium")]
        [Tooltip("Enable research consortium funding")]
        public bool enableResearchConsortium = true;

        [Tooltip("Minimum consortium contribution")]
        [Range(100, 10000)]
        public int minimumConsortiumContribution = 500;

        [Tooltip("Consortium funding bonus multiplier")]
        [Range(1f, 5f)]
        public float consortiumBonusMultiplier = 2f;

        [Tooltip("Research project funding threshold")]
        [Range(1000, 100000)]
        public int researchProjectThreshold = 10000;

        [Header("Conservation Economy")]
        [Tooltip("Enable conservation trading")]
        public bool enableConservationTrading = true;

        [Tooltip("Conservation credit earning rate")]
        [Range(0.1f, 10f)]
        public float conservationCreditRate = 1f;

        [Tooltip("Endangered species trade restrictions")]
        public bool restrictEndangeredSpeciesTrade = true;

        [Tooltip("Conservation project minimum funding")]
        [Range(500, 50000)]
        public int conservationProjectMinFunding = 2500;

        [Header("Reputation System")]
        [Tooltip("Enable reputation system")]
        public bool enableReputationSystem = true;

        [Tooltip("Initial reputation score for new players")]
        [Range(0f, 100f)]
        public float initialReputationScore = 50f;

        [Tooltip("Minimum reputation for featured trades")]
        [Range(0f, 100f)]
        public float minReputationForFeatured = 75f;

        [Tooltip("Reputation penalty for cancelled trades")]
        [Range(0f, 10f)]
        public float cancelledTradePenalty = 2f;

        [Tooltip("Reputation bonus for completed trades")]
        [Range(0f, 5f)]
        public float completedTradeBonus = 1f;

        [Tooltip("Review weight based on reviewer reputation")]
        public bool useReputationWeightedReviews = true;

        [Header("Market Analytics")]
        [Tooltip("Enable market trend analysis")]
        public bool enableMarketAnalytics = true;

        [Tooltip("Price history retention period in days")]
        [Range(7, 365)]
        public int priceHistoryRetentionDays = 90;

        [Tooltip("Market trend update interval in minutes")]
        [Range(5, 60)]
        public int marketTrendUpdateIntervalMinutes = 15;

        [Tooltip("Minimum trades for trend analysis")]
        [Range(5, 100)]
        public int minimumTradesForTrends = 20;

        [Header("Trade Validation")]
        [Tooltip("Enable item authenticity verification")]
        public bool enableItemAuthentication = true;

        [Tooltip("Enable genetic hash verification")]
        public bool enableGeneticHashVerification = true;

        [Tooltip("Maximum trade age for verification in hours")]
        [Range(1f, 72f)]
        public float maxTradeAgeForVerificationHours = 24f;

        [Tooltip("Require breeding documentation for rare items")]
        public bool requireBreedingDocumentation = true;

        [Header("Performance")]
        [Tooltip("Maximum search results per query")]
        [Range(10, 200)]
        public int maxSearchResults = 100;

        [Tooltip("Trade processing batch size")]
        [Range(1, 50)]
        public int tradeProcessingBatchSize = 10;

        [Tooltip("Market data cache duration in minutes")]
        [Range(1, 60)]
        public int marketDataCacheDurationMinutes = 5;

        [Tooltip("Enable trade result caching")]
        public bool enableTradeResultCaching = true;

        [Header("Notifications")]
        [Tooltip("Enable trade notifications")]
        public bool enableTradeNotifications = true;

        [Tooltip("Enable price alert notifications")]
        public bool enablePriceAlerts = true;

        [Tooltip("Enable market trend notifications")]
        public bool enableMarketTrendNotifications = true;

        [Tooltip("Notification cleanup interval in hours")]
        [Range(1f, 168f)]
        public float notificationCleanupIntervalHours = 24f;

        [Tooltip("Maximum notifications per player")]
        [Range(10, 500)]
        public int maxNotificationsPerPlayer = 100;

        [Header("Security")]
        [Tooltip("Enable fraud detection")]
        public bool enableFraudDetection = true;

        [Tooltip("Maximum trades per hour per player")]
        [Range(1, 100)]
        public int maxTradesPerHourPerPlayer = 20;

        [Tooltip("Suspicious activity threshold")]
        [Range(1f, 10f)]
        public float suspiciousActivityThreshold = 5f;

        [Tooltip("Enable trade dispute system")]
        public bool enableTradeDisputes = true;

        [Tooltip("Trade dispute resolution time in hours")]
        [Range(24f, 168f)]
        public float tradeDisputeResolutionHours = 48f;

        #region Validation

        private void OnValidate()
        {
            // Ensure reasonable values
            backgroundProcessingIntervalMs = Mathf.Max(1000, backgroundProcessingIntervalMs);
            maxConcurrentTradesPerPlayer = Mathf.Max(1, maxConcurrentTradesPerPlayer);
            defaultTradeExpiryHours = Mathf.Max(1f, defaultTradeExpiryHours);
            marketplaceCommissionPercent = Mathf.Clamp(marketplaceCommissionPercent, 0f, 10f);

            // Ensure starting currencies have defaults
            if (startingCurrencies.Count == 0)
            {
                startingCurrencies.AddRange(CreateDefaultStartingCurrencies());
            }

            // Ensure daily limits have defaults
            if (dailyEarningLimits.Count == 0)
            {
                dailyEarningLimits.AddRange(CreateDefaultDailyLimits());
            }

            // Ensure max balances have defaults
            if (maxBalances.Count == 0)
            {
                maxBalances.AddRange(CreateDefaultMaxBalances());
            }

            // Ensure student limits have defaults
            if (studentSpendingLimits.Count == 0)
            {
                studentSpendingLimits.AddRange(CreateDefaultStudentLimits());
            }

            // Validate reputation values
            initialReputationScore = Mathf.Clamp(initialReputationScore, 0f, 100f);
            minReputationForFeatured = Mathf.Clamp(minReputationForFeatured, 0f, 100f);

            // Validate performance settings
            maxSearchResults = Mathf.Max(10, maxSearchResults);
            tradeProcessingBatchSize = Mathf.Max(1, tradeProcessingBatchSize);
            maxNotificationsPerPlayer = Mathf.Max(10, maxNotificationsPerPlayer);
        }

        private List<CurrencyStartingAmount> CreateDefaultStartingCurrencies()
        {
            return new List<CurrencyStartingAmount>
            {
                new CurrencyStartingAmount { currencyType = CurrencyType.ResearchPoints, amount = 1000 },
                new CurrencyStartingAmount { currencyType = CurrencyType.DiscoveryTokens, amount = 100 },
                new CurrencyStartingAmount { currencyType = CurrencyType.CommunityCredits, amount = 50 },
                new CurrencyStartingAmount { currencyType = CurrencyType.EducationalPoints, amount = 500 },
                new CurrencyStartingAmount { currencyType = CurrencyType.ConservationFunds, amount = 200 }
            };
        }

        private List<CurrencyDailyLimit> CreateDefaultDailyLimits()
        {
            return new List<CurrencyDailyLimit>
            {
                new CurrencyDailyLimit { currencyType = CurrencyType.ResearchPoints, limit = 5000 },
                new CurrencyDailyLimit { currencyType = CurrencyType.DiscoveryTokens, limit = 500 },
                new CurrencyDailyLimit { currencyType = CurrencyType.CommunityCredits, limit = 1000 },
                new CurrencyDailyLimit { currencyType = CurrencyType.EducationalPoints, limit = 2000 },
                new CurrencyDailyLimit { currencyType = CurrencyType.ConservationFunds, limit = 1500 }
            };
        }

        private List<CurrencyMaxBalance> CreateDefaultMaxBalances()
        {
            return new List<CurrencyMaxBalance>
            {
                new CurrencyMaxBalance { currencyType = CurrencyType.ResearchPoints, maxBalance = 100000 },
                new CurrencyMaxBalance { currencyType = CurrencyType.DiscoveryTokens, maxBalance = 50000 },
                new CurrencyMaxBalance { currencyType = CurrencyType.CommunityCredits, maxBalance = 25000 },
                new CurrencyMaxBalance { currencyType = CurrencyType.EducationalPoints, maxBalance = 75000 },
                new CurrencyMaxBalance { currencyType = CurrencyType.ConservationFunds, maxBalance = 40000 }
            };
        }

        private List<CurrencyDailyLimit> CreateDefaultStudentLimits()
        {
            return new List<CurrencyDailyLimit>
            {
                new CurrencyDailyLimit { currencyType = CurrencyType.ResearchPoints, limit = 500 },
                new CurrencyDailyLimit { currencyType = CurrencyType.DiscoveryTokens, limit = 50 },
                new CurrencyDailyLimit { currencyType = CurrencyType.CommunityCredits, limit = 100 },
                new CurrencyDailyLimit { currencyType = CurrencyType.EducationalPoints, limit = 1000 },
                new CurrencyDailyLimit { currencyType = CurrencyType.ConservationFunds, limit = 200 }
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets starting currency amount for specific currency type
        /// </summary>
        public long GetStartingCurrency(CurrencyType currencyType)
        {
            var currency = startingCurrencies.Find(c => c.currencyType == currencyType);
            return currency?.amount ?? 0;
        }

        /// <summary>
        /// Gets daily earning limit for specific currency type
        /// </summary>
        public long GetDailyEarningLimit(CurrencyType currencyType)
        {
            var limit = dailyEarningLimits.Find(l => l.currencyType == currencyType);
            return limit?.limit ?? 1000;
        }

        /// <summary>
        /// Gets maximum balance for specific currency type
        /// </summary>
        public long GetMaxBalance(CurrencyType currencyType)
        {
            var balance = maxBalances.Find(b => b.currencyType == currencyType);
            return balance?.maxBalance ?? 10000;
        }

        /// <summary>
        /// Gets student spending limit for specific currency type
        /// </summary>
        public long GetStudentSpendingLimit(CurrencyType currencyType)
        {
            var limit = studentSpendingLimits.Find(l => l.currencyType == currencyType);
            return limit?.limit ?? 100;
        }

        /// <summary>
        /// Calculates marketplace commission for trade value
        /// </summary>
        public long CalculateMarketplaceCommission(long tradeValue)
        {
            return (long)(tradeValue * (marketplaceCommissionPercent / 100f));
        }

        /// <summary>
        /// Checks if player can create featured trade
        /// </summary>
        public bool CanCreateFeaturedTrade(float playerReputation, long playerTokens)
        {
            return playerReputation >= minReputationForFeatured && playerTokens >= featuredTradeCost;
        }

        /// <summary>
        /// Calculates consortium funding bonus
        /// </summary>
        public long CalculateConsortiumBonus(long contribution)
        {
            if (contribution < minimumConsortiumContribution)
                return 0;

            return (long)(contribution * (consortiumBonusMultiplier - 1f));
        }

        /// <summary>
        /// Checks if trade requires special verification
        /// </summary>
        public bool RequiresSpecialVerification(TradeableItem item)
        {
            if (!enableItemAuthentication)
                return false;

            return item.rarity >= ItemRarity.Epic ||
                   item.itemType == TradeableItemType.GeneticMaterial ||
                   item.itemType == TradeableItemType.Species;
        }

        /// <summary>
        /// Gets trade expiry time
        /// </summary>
        public DateTime GetTradeExpiryTime()
        {
            return DateTime.Now.AddHours(defaultTradeExpiryHours);
        }

        /// <summary>
        /// Checks if player is within trading limits
        /// </summary>
        public bool IsWithinTradingLimits(int currentTrades, DateTime lastTradeTime)
        {
            if (currentTrades >= maxConcurrentTradesPerPlayer)
                return false;

            var hoursSinceLastTrade = (DateTime.Now - lastTradeTime).TotalHours;
            if (hoursSinceLastTrade < 1.0 / maxTradesPerHourPerPlayer)
                return false;

            return true;
        }

        /// <summary>
        /// Validates educational trade constraints
        /// </summary>
        public bool ValidateEducationalTrade(TradeOffer offer, bool isStudent)
        {
            if (!enableEducationalMode)
                return true;

            if (isStudent)
            {
                // Check value limits
                var totalValue = offer.offeredCurrency?.amount ?? 0 + offer.requestedCurrency?.amount ?? 0;
                if (totalValue > maxEducationalTradeValue)
                    return false;

                // Check spending limits
                if (offer.offeredCurrency != null)
                {
                    var spendingLimit = GetStudentSpendingLimit(offer.offeredCurrency.currencyType);
                    if (offer.offeredCurrency.amount > spendingLimit)
                        return false;
                }
            }

            return true;
        }

        #endregion
    }

    [System.Serializable]
    public class CurrencyStartingAmount
    {
        public CurrencyType currencyType;
        public long amount;
    }

    [System.Serializable]
    public class CurrencyDailyLimit
    {
        public CurrencyType currencyType;
        public long limit;
    }

    [System.Serializable]
    public class CurrencyMaxBalance
    {
        public CurrencyType currencyType;
        public long maxBalance;
    }
}