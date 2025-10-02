using System;
using UnityEngine;

namespace Laboratory.Chimera.Marketplace
{
    /// <summary>
    /// Configuration ScriptableObject for the Genetic Marketplace System.
    /// Allows designers to tune trading mechanics, market dynamics, and economic balance.
    /// </summary>
    [CreateAssetMenu(fileName = "GeneticMarketplaceConfig", menuName = "Chimera/Marketplace/Marketplace Config")]
    public class GeneticMarketplaceConfig : ScriptableObject
    {
        [Header("Trading Settings")]
        [Tooltip("Base transaction fee percentage")]
        [Range(0f, 0.2f)]
        public float baseFeePercentage = 0.05f;

        [Tooltip("Maximum simultaneous listings per player")]
        [Range(1, 50)]
        public int maxListingsPerPlayer = 10;

        [Tooltip("Maximum auction duration in hours")]
        [Range(1f, 168f)]
        public float maxAuctionDurationHours = 72f;

        [Tooltip("Minimum listing duration in hours")]
        [Range(0.5f, 24f)]
        public float minListingDurationHours = 1f;

        [Tooltip("Auto-relisting fee multiplier")]
        [Range(1f, 3f)]
        public float relistingFeeMultiplier = 1.5f;

        [Header("Price Calculation")]
        [Tooltip("Base price for common genetic material")]
        [Range(10, 1000)]
        public int baseGeneticPrice = 100;

        [Tooltip("Rarity multiplier for pricing")]
        [Range(1f, 10f)]
        public float rarityPriceMultiplier = 2f;

        [Tooltip("Quality multiplier for pricing")]
        [Range(0.5f, 5f)]
        public float qualityPriceMultiplier = 1.5f;

        [Tooltip("Age depreciation rate per day")]
        [Range(0f, 0.1f)]
        public float ageDepreciationRate = 0.02f;

        [Tooltip("Market volatility factor")]
        [Range(0f, 0.5f)]
        public float volatilityFactor = 0.1f;

        [Header("Auction Settings")]
        [Tooltip("Minimum bid increment percentage")]
        [Range(0.01f, 0.2f)]
        public float minBidIncrement = 0.05f;

        [Tooltip("Auto-extend time when bid placed in final minutes")]
        [Range(1f, 30f)]
        public float autoExtendMinutes = 5f;

        [Tooltip("Reserve price percentage of estimated value")]
        [Range(0.5f, 2f)]
        public float reservePricePercentage = 0.8f;

        [Tooltip("Buyout price multiplier over starting bid")]
        [Range(1.5f, 5f)]
        public float buyoutMultiplier = 2.5f;

        [Header("Breeding Services")]
        [Tooltip("Base stud service fee")]
        [Range(100, 10000)]
        public int baseStudFee = 500;

        [Tooltip("Success guarantee fee multiplier")]
        [Range(1.5f, 3f)]
        public float guaranteeFeeMultiplier = 2f;

        [Tooltip("Breeding consultation fee per session")]
        [Range(50, 1000)]
        public int consultationFee = 200;

        [Tooltip("Custom breeding plan fee")]
        [Range(500, 5000)]
        public int customPlanFee = 1500;

        [Header("Knowledge Commerce")]
        [Tooltip("Base research data price")]
        [Range(10, 500)]
        public int baseResearchPrice = 50;

        [Tooltip("Genetic analysis fee")]
        [Range(25, 500)]
        public int analysisServiceFee = 100;

        [Tooltip("Bloodline investigation fee")]
        [Range(100, 2000)]
        public int investigationFee = 300;

        [Tooltip("Breeding prediction accuracy bonus")]
        [Range(1f, 3f)]
        public float accuracyBonusMultiplier = 1.8f;

        [Header("Market Intelligence")]
        [Tooltip("Price tracking history days")]
        [Range(7, 365)]
        public int priceHistoryDays = 90;

        [Tooltip("Trend analysis update frequency in hours")]
        [Range(1f, 24f)]
        public float trendUpdateFrequency = 6f;

        [Tooltip("Market alert threshold percentage")]
        [Range(0.1f, 1f)]
        public float alertThreshold = 0.25f;

        [Tooltip("Premium analytics subscription cost")]
        [Range(100, 2000)]
        public int premiumAnalyticsCost = 500;

        [Header("Economic Events")]
        [Tooltip("Base frequency of economic events (days)")]
        [Range(1f, 30f)]
        public float eventBaseFrequency = 7f;

        [Tooltip("Market crash probability per week")]
        [Range(0f, 0.1f)]
        public float crashProbability = 0.02f;

        [Tooltip("Boom event duration in days")]
        [Range(1f, 14f)]
        public float boomDuration = 3f;

        [Tooltip("Shortage event impact multiplier")]
        [Range(1.5f, 5f)]
        public float shortageImpactMultiplier = 3f;

        [Header("Security & Verification")]
        [Tooltip("Genetic authenticity verification fee")]
        [Range(10, 200)]
        public int verificationFee = 50;

        [Tooltip("Escrow service fee percentage")]
        [Range(0.01f, 0.1f)]
        public float escrowFeePercentage = 0.03f;

        [Tooltip("Fraud detection sensitivity")]
        [Range(0.5f, 1f)]
        public float fraudSensitivity = 0.8f;

        [Tooltip("Insurance premium percentage")]
        [Range(0.01f, 0.15f)]
        public float insurancePremium = 0.05f;

        [Header("Performance Settings")]
        [Tooltip("Maximum transactions processed per frame")]
        [Range(1, 50)]
        public int maxTransactionsPerFrame = 10;

        [Tooltip("Price update frequency in seconds")]
        [Range(30f, 3600f)]
        public float priceUpdateFrequency = 300f;

        [Tooltip("Market data cache duration in seconds")]
        [Range(60f, 3600f)]
        public float dataCacheDuration = 600f;

        [Tooltip("Maximum search results to display")]
        [Range(10, 200)]
        public int maxSearchResults = 50;

        [Header("Social Features")]
        [Tooltip("Enable seller ratings and reviews")]
        public bool enableSellerRatings = true;

        [Tooltip("Enable trading guild system")]
        public bool enableTradingGuilds = true;

        [Tooltip("Guild membership fee")]
        [Range(100, 5000)]
        public int guildMembershipFee = 1000;

        [Tooltip("Guild transaction fee discount")]
        [Range(0f, 0.5f)]
        public float guildFeeDiscount = 0.2f;

        [Header("Market Categories")]
        public MarketCategoryConfig[] categories = new MarketCategoryConfig[]
        {
            new MarketCategoryConfig
            {
                category = MarketCategory.Breeding,
                displayName = "Live Creatures",
                feeMultiplier = 1f,
                insuranceRequired = true,
                description = "Living creatures for breeding and companionship"
            },
            new MarketCategoryConfig
            {
                category = MarketCategory.GeneticMaterial,
                displayName = "Genetic Material",
                feeMultiplier = 0.8f,
                insuranceRequired = false,
                description = "DNA samples, genetic sequences, and breeding materials"
            },
            new MarketCategoryConfig
            {
                category = MarketCategory.BreedingServices,
                displayName = "Breeding Services",
                feeMultiplier = 1.2f,
                insuranceRequired = true,
                description = "Professional breeding services and consultations"
            },
            new MarketCategoryConfig
            {
                category = MarketCategory.ResearchData,
                displayName = "Research Data",
                feeMultiplier = 0.6f,
                insuranceRequired = false,
                description = "Scientific research and genetic analysis data"
            },
            new MarketCategoryConfig
            {
                category = MarketCategory.Equipment,
                displayName = "Equipment",
                feeMultiplier = 1f,
                insuranceRequired = true,
                description = "Breeding equipment and laboratory tools"
            }
        };

        /// <summary>
        /// Calculates the total fee for a transaction
        /// </summary>
        public float CalculateTransactionFee(float price, MarketCategory category, bool hasInsurance, bool isGuildMember)
        {
            var categoryConfig = GetCategoryConfig(category);
            float fee = price * baseFeePercentage * categoryConfig.feeMultiplier;

            if (hasInsurance && categoryConfig.insuranceRequired)
                fee += price * insurancePremium;

            if (isGuildMember)
                fee *= (1f - guildFeeDiscount);

            return fee;
        }

        /// <summary>
        /// Estimates price based on creature/item properties
        /// </summary>
        public float EstimatePrice(float rarity, float quality, float age, MarketCategory category)
        {
            float basePrice = baseGeneticPrice;

            // Apply rarity multiplier
            basePrice *= Mathf.Pow(rarityPriceMultiplier, rarity);

            // Apply quality multiplier
            basePrice *= Mathf.Lerp(0.5f, qualityPriceMultiplier, quality);

            // Apply age depreciation
            basePrice *= Mathf.Max(0.1f, 1f - (age * ageDepreciationRate));

            // Apply market volatility
            float volatility = UnityEngine.Random.Range(-volatilityFactor, volatilityFactor);
            basePrice *= (1f + volatility);

            return basePrice;
        }

        /// <summary>
        /// Gets configuration for a specific market category
        /// </summary>
        public MarketCategoryConfig GetCategoryConfig(MarketCategory category)
        {
            foreach (var config in categories)
            {
                if (config.category == category)
                    return config;
            }

            // Return default if not found
            return new MarketCategoryConfig
            {
                category = category,
                displayName = category.ToString(),
                feeMultiplier = 1f,
                insuranceRequired = false,
                description = "Market category"
            };
        }

        /// <summary>
        /// Determines if an auction should auto-extend
        /// </summary>
        public bool ShouldAutoExtend(float timeRemainingMinutes, bool bidPlacedRecently)
        {
            return bidPlacedRecently && timeRemainingMinutes <= autoExtendMinutes;
        }

        /// <summary>
        /// Calculates minimum bid increment
        /// </summary>
        public float CalculateMinimumBid(float currentBid)
        {
            return currentBid * (1f + minBidIncrement);
        }

        /// <summary>
        /// Determines if a price represents a market anomaly
        /// </summary>
        public bool IsMarketAnomaly(float currentPrice, float historicalAverage)
        {
            float deviation = Mathf.Abs(currentPrice - historicalAverage) / historicalAverage;
            return deviation > alertThreshold;
        }

        void OnValidate()
        {
            // Ensure reasonable fee structures
            baseFeePercentage = Mathf.Clamp(baseFeePercentage, 0f, 0.2f);
            escrowFeePercentage = Mathf.Clamp(escrowFeePercentage, 0.01f, 0.1f);
            insurancePremium = Mathf.Clamp(insurancePremium, 0.01f, 0.15f);

            // Validate auction settings
            minListingDurationHours = Mathf.Clamp(minListingDurationHours, 0.5f, maxAuctionDurationHours);

            // Ensure pricing makes sense
            baseGeneticPrice = Mathf.Clamp(baseGeneticPrice, 10, 100000);

            // Validate categories have proper settings
            for (int i = 0; i < categories.Length; i++)
            {
                categories[i].feeMultiplier = Mathf.Clamp(categories[i].feeMultiplier, 0.1f, 5f);
            }
        }
    }

    #region Configuration Data Structures

    /// <summary>
    /// Configuration for market categories
    /// </summary>
    [Serializable]
    public struct MarketCategoryConfig
    {
        [Tooltip("The market category this configures")]
        public MarketCategory category;

        [Tooltip("Display name for this category")]
        public string displayName;

        [Tooltip("Fee multiplier for this category")]
        [Range(0.1f, 5f)]
        public float feeMultiplier;

        [Tooltip("Whether insurance is required for this category")]
        public bool insuranceRequired;

        [Tooltip("Category description")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Category icon color")]
        public Color categoryColor;

        [Tooltip("Enable special promotions for this category")]
        public bool enablePromotions;

        [Tooltip("Minimum seller reputation required")]
        [Range(0f, 1000f)]
        public float minSellerReputation;
    }

    #endregion

    /// <summary>
    /// Market categories for genetic trading
    /// </summary>
    public enum MarketCategory : byte
    {
        Common,         // Common genetic traits
        Rare,           // Rare genetic combinations
        Legendary,      // Legendary creatures/traits
        Experimental,   // Experimental genetic modifications
        Breeding,       // Breeding services and materials
        Equipment,      // Lab equipment and tools
        Research        // Research data and findings
    }
}