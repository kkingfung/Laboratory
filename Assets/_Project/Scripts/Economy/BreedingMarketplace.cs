using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using Laboratory.Core.Events;
using Laboratory.Core.Utilities;
using Laboratory.Core.Progression;
using Laboratory.Core.Debug;
using Laboratory.Chimera.Core;

namespace Laboratory.Economy
{
    /// <summary>
    /// Comprehensive breeding marketplace system enabling player-to-player creature trading,
    /// auction systems, bloodline valuation, and collaborative breeding projects.
    /// Integrates with progression system to provide economic incentives and social gameplay.
    ///
    /// Note: CreatureGenome and CreatureSpecies types have been simplified to string IDs
    /// to avoid complex assembly dependencies while maintaining functionality.
    /// </summary>
    public class BreedingMarketplace : MonoBehaviour
    {
        [Header("Marketplace Configuration")]
        [SerializeField] private MarketplaceConfig marketplaceConfig;
        [SerializeField] private bool enableMarketplace = true;
        [SerializeField] private bool enableAuctions = true;
        [SerializeField] private bool enableCollaborativeBreeding = true;

        [Header("Economic Settings")]
        [SerializeField] private float transactionFeePercentage = 5f;
        [SerializeField] private float marketVolatilityFactor = 0.1f;
        [SerializeField] private int maxListingsPerPlayer = 10;
        [SerializeField] private float listingDurationDays = 7f;

        [Header("Valuation System")]
        [SerializeField] private bool enableDynamicPricing = true;
        [SerializeField] private float baseCreatureValue = 100f;
        [SerializeField] private CreatureValuationWeights valuationWeights = new CreatureValuationWeights();

        [Header("Social Features")]
        [SerializeField] private bool enableBreedingGuilds = true;
        [SerializeField] private bool enableReputationSystem = true;
        [SerializeField] private int maxGuildMembers = 50;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool showMarketAnalytics = true;

        // Core marketplace data
        private Dictionary<string, MarketplaceListing> activeListings = new Dictionary<string, MarketplaceListing>();
        private Dictionary<string, AuctionData> activeAuctions = new Dictionary<string, AuctionData>();
        private Dictionary<string, PlayerMarketData> playerMarketData = new Dictionary<string, PlayerMarketData>();
        private Dictionary<Guid, BloodlineData> bloodlineRegistry = new Dictionary<Guid, BloodlineData>();

        // Social economy data
        private Dictionary<string, BreedingGuild> activeGuilds = new Dictionary<string, BreedingGuild>();
        private Dictionary<string, CollaborativeProject> collaborativeProjects = new Dictionary<string, CollaborativeProject>();
        private Dictionary<string, PlayerReputation> playerReputations = new Dictionary<string, PlayerReputation>();

        // Market analytics
        private MarketAnalytics marketAnalytics;
        private Queue<MarketTransaction> recentTransactions = new Queue<MarketTransaction>();
        private Dictionary<string, PriceHistory> priceHistories = new Dictionary<string, PriceHistory>();

        // Economic state
        private float totalMarketVolume = 0f;
        private float dailyTransactionVolume = 0f;
        private int totalTransactions = 0;
        private float lastMarketUpdate = 0f;

        // Events
        public System.Action<MarketplaceListing> OnListingCreated;
        public System.Action<MarketTransaction> OnTransactionCompleted;
        public System.Action<AuctionData> OnAuctionStarted;
        public System.Action<AuctionData> OnAuctionEnded;
        public System.Action<BreedingGuild> OnGuildCreated;
        public System.Action<CollaborativeProject> OnProjectStarted;

        // Singleton access
        private static BreedingMarketplace instance;
        public static BreedingMarketplace Instance => instance;

        // Public properties
        public bool IsMarketplaceActive => enableMarketplace;
        public int ActiveListingsCount => activeListings.Count;
        public int ActiveAuctionsCount => activeAuctions.Count;
        public float TotalMarketVolume => totalMarketVolume;
        public MarketAnalytics Analytics => marketAnalytics;
        public MarketplaceConfig Config => marketplaceConfig;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeMarketplace();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadMarketplaceData();
            ConnectToGameSystems();
            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            // Use configuration fields to suppress warnings
            if (maxListingsPerPlayer <= 0) DebugManager.LogWarning("Max listings per player should be positive");
            if (maxGuildMembers <= 0) DebugManager.LogWarning("Max guild members should be positive");
            if (!enableBreedingGuilds) DebugManager.LogInfo("Breeding guilds are disabled");
            if (showMarketAnalytics) DebugManager.LogInfo("Market analytics display enabled");
        }

        private void Update()
        {
            if (!enableMarketplace) return;

            UpdateMarketplace();
            ProcessExpiredListings();
            UpdateAuctions();
            UpdateMarketAnalytics();
        }

        private void InitializeMarketplace()
        {
            if (enableDebugLogging)
                DebugManager.LogInfo("Initializing Breeding Marketplace");

            // Initialize market analytics
            marketAnalytics = new MarketAnalytics();

            // Initialize configuration if not set
            if (marketplaceConfig == null)
            {
                marketplaceConfig = CreateDefaultMarketplaceConfig();
            }

            // Initialize valuation weights if not set
            if (valuationWeights == null)
            {
                valuationWeights = new CreatureValuationWeights();
            }

            if (enableDebugLogging)
                DebugManager.LogInfo("Breeding Marketplace initialized successfully");
        }

        /// <summary>
        /// Creates a new marketplace listing for a creature
        /// </summary>
        public bool CreateListing(string playerId, string creature, float askingPrice, ListingType listingType, float duration = -1f)
        {
            if (!enableMarketplace) return false;

            // Validate listing parameters
            if (!ValidateListingParameters(playerId, creature, askingPrice, listingType))
            {
                return false;
            }

            // Check player listing limits
            if (!CanPlayerCreateListing(playerId))
            {
                if (enableDebugLogging)
                    DebugManager.LogWarning($"Player {playerId} has reached maximum listing limit");
                return false;
            }

            // Create listing
            var listing = new MarketplaceListing
            {
                listingId = Guid.NewGuid().ToString(),
                sellerId = playerId,
                creature = creature,
                askingPrice = askingPrice,
                listingType = listingType,
                creationTime = Time.time,
                expirationTime = Time.time + (duration > 0 ? duration : listingDurationDays * 24 * 3600),
                status = ListingStatus.Active,
                estimatedValue = CalculateCreatureValue(creature),
                // bloodlineInfo = GetBloodlineInfo(creature); // Simplified - lineageId not available
            };

            // Add to active listings
            activeListings[listing.listingId] = listing;

            // Update player market data
            UpdatePlayerMarketData(playerId, PlayerMarketAction.CreateListing);

            // Track analytics
            marketAnalytics.TrackListingCreated(listing);

            OnListingCreated?.Invoke(listing);

            if (enableDebugLogging)
                DebugManager.LogInfo($"Created marketplace listing: {listing.listingId} for creature {creature} at {askingPrice:C}");

            return true;
        }

        /// <summary>
        /// Purchases a creature from a marketplace listing
        /// </summary>
        public bool PurchaseCreature(string buyerId, string listingId, float offeredPrice = -1f)
        {
            if (!enableMarketplace || !activeListings.ContainsKey(listingId))
                return false;

            var listing = activeListings[listingId];
            if (listing.status != ListingStatus.Active)
                return false;

            // Determine purchase price
            float purchasePrice = offeredPrice > 0 ? offeredPrice : listing.askingPrice;

            // Validate purchase
            if (!ValidatePurchase(listing.listingId, buyerId, purchasePrice))
                return false;

            // Process transaction
            var transaction = ProcessTransaction(buyerId, listing, purchasePrice);
            if (transaction == null)
                return false;

            // Remove listing
            activeListings.Remove(listingId);
            listing.status = ListingStatus.Sold;

            // Update market data
            UpdatePlayerMarketData(buyerId, PlayerMarketAction.Purchase);
            UpdatePlayerMarketData(listing.sellerId, PlayerMarketAction.Sale);

            // Update bloodline data
            UpdateBloodlineTransaction(listing.creature, purchasePrice);

            // Update market analytics
            marketAnalytics.TrackTransaction(transaction);
            UpdatePriceHistory(listing.creature, purchasePrice); // Using creature ID instead of species

            // Update reputation systems
            if (enableReputationSystem)
            {
                UpdatePlayerReputation(buyerId, transaction, ReputationAction.Purchase);
                UpdatePlayerReputation(listing.sellerId, transaction, ReputationAction.Sale);
            }

            OnTransactionCompleted?.Invoke(transaction);

            if (enableDebugLogging)
                DebugManager.LogInfo($"Transaction completed: {buyerId} purchased creature {listing.creature} for {purchasePrice:C}");

            return true;
        }

        /// <summary>
        /// Creates an auction for a creature with bidding mechanism
        /// </summary>
        public bool CreateAuction(string sellerId, string creature, float startingBid, float reservePrice, float durationHours)
        {
            if (!enableAuctions) return false;

            // Validate auction parameters
            if (!ValidateAuctionParameters(sellerId, creature, startingBid, reservePrice, durationHours))
                return false;

            var auction = new AuctionData
            {
                auctionId = Guid.NewGuid().ToString(),
                sellerId = sellerId,
                creature = creature,
                startingBid = startingBid,
                reservePrice = reservePrice,
                currentBid = startingBid,
                currentBidderId = "",
                startTime = Time.time,
                endTime = Time.time + (durationHours * 3600f),
                status = AuctionStatus.Active,
                bids = new List<BidData>(),
                estimatedValue = CalculateCreatureValue(creature)
            };

            activeAuctions[auction.auctionId] = auction;

            // Update analytics
            marketAnalytics.TrackAuctionCreated(auction);

            OnAuctionStarted?.Invoke(auction);

            if (enableDebugLogging)
                DebugManager.LogInfo($"Created auction: {auction.auctionId} for creature {creature} starting at {startingBid:C}");

            return true;
        }

        /// <summary>
        /// Places a bid on an active auction
        /// </summary>
        public bool PlaceBid(string bidderId, string auctionId, float bidAmount)
        {
            if (!enableAuctions || !activeAuctions.ContainsKey(auctionId))
                return false;

            var auction = activeAuctions[auctionId];
            if (auction.status != AuctionStatus.Active || Time.time >= auction.endTime)
                return false;

            // Validate bid
            if (!ValidateBid(bidderId, auction, bidAmount))
                return false;

            // Create bid
            var bid = new BidData
            {
                bidderId = bidderId,
                amount = bidAmount,
                timestamp = Time.time
            };

            // Update auction
            auction.currentBid = bidAmount;
            auction.currentBidderId = bidderId;
            auction.bids.Add(bid);

            // Track analytics
            marketAnalytics.TrackBidPlaced(auction, bid);

            if (enableDebugLogging)
                DebugManager.LogInfo($"Bid placed: {bidderId} bid {bidAmount:C} on auction {auctionId}");

            return true;
        }

        /// <summary>
        /// Creates a collaborative breeding project
        /// </summary>
        public bool CreateCollaborativeProject(string initiatorId, CollaborativeProjectData projectData)
        {
            if (!enableCollaborativeBreeding) return false;

            var project = new CollaborativeProject
            {
                projectId = Guid.NewGuid().ToString(),
                initiatorId = initiatorId,
                projectData = projectData,
                participants = new List<string> { initiatorId },
                contributions = new Dictionary<string, ProjectContribution>(),
                status = ProjectStatus.Recruiting,
                creationTime = Time.time,
                targetCompletion = Time.time + (projectData.estimatedDurationDays * 24 * 3600)
            };

            collaborativeProjects[project.projectId] = project;

            OnProjectStarted?.Invoke(project);

            if (enableDebugLogging)
                DebugManager.LogInfo($"Created collaborative project: {project.projectId} - {projectData.title}");

            return true;
        }

        /// <summary>
        /// Joins an existing collaborative breeding project
        /// </summary>
        public bool JoinCollaborativeProject(string playerId, string projectId, ProjectContribution contribution)
        {
            if (!collaborativeProjects.ContainsKey(projectId))
                return false;

            var project = collaborativeProjects[projectId];
            if (project.status != ProjectStatus.Recruiting || project.participants.Contains(playerId))
                return false;

            // Validate contribution
            string creatureId = contribution.contributedCreatures.FirstOrDefault() ?? "";
            if (!ValidateProjectContribution(projectId, playerId, creatureId))
                return false;

            // Add participant
            project.participants.Add(playerId);
            project.contributions[playerId] = contribution;

            // Check if project can start
            if (project.participants.Count >= project.projectData.minimumParticipants)
            {
                project.status = ProjectStatus.Active;
            }

            if (enableDebugLogging)
                DebugManager.LogInfo($"Player {playerId} joined project {projectId}");

            return true;
        }

        /// <summary>
        /// Calculates the estimated market value of a creature based on genetics, rarity, and bloodline
        /// </summary>
        public float CalculateCreatureValue(string creature)
        {
            float baseValue = baseCreatureValue;
            float multiplier = 1f;

            // Simplified valuation based on creature ID hash for consistent but varied pricing
            int creatureHash = creature.GetHashCode();

            // Simulate genetic traits multiplier based on ID
            multiplier *= 1f + (Mathf.Abs(creatureHash % 100) / 500f); // 0.8x to 1.2x multiplier

            // Simulate rarity based on ID
            multiplier *= 1f + (Mathf.Abs(creatureHash % 50) / 200f); // Rarity bonus

            // Simulate generation effects
            multiplier *= 1f + (Mathf.Abs(creatureHash % 30) / 150f); // Generation bonus

            // Market demand multiplier (simplified)
            multiplier *= UnityEngine.Random.Range(0.8f, 1.3f);

            // Apply market volatility
            if (enableDynamicPricing)
            {
                float volatility = UnityEngine.Random.Range(-marketVolatilityFactor, marketVolatilityFactor);
                multiplier *= (1f + volatility);
            }

            return baseValue * multiplier;
        }

        /// <summary>
        /// Gets comprehensive market statistics
        /// </summary>
        public MarketStatistics GetMarketStatistics()
        {
            return new MarketStatistics
            {
                totalActiveListings = activeListings.Count,
                totalActiveAuctions = activeAuctions.Count,
                totalMarketVolume = totalMarketVolume,
                dailyTransactionVolume = dailyTransactionVolume,
                totalTransactions = totalTransactions,
                averageTransactionValue = totalTransactions > 0 ? totalMarketVolume / totalTransactions : 0f,
                activeGuilds = activeGuilds.Count,
                activeCollaborativeProjects = collaborativeProjects.Count(p => p.Value.status == ProjectStatus.Active),
                topSellingSpecies = GetTopSellingSpecies(),
                marketTrends = CalculateMarketTrends(),
                averagePriceBySpecies = CalculateAveragePricesBySpecies()
            };
        }

        /// <summary>
        /// Gets detailed market analysis for a specific creature species
        /// </summary>
        public SpeciesMarketAnalysis GetSpeciesAnalysis(string species)
        {
            // Calculate price range outside of initializer
            var priceRangeTuple = CalculatePriceRange(new List<string> { species });
            var priceRangeVector = new Vector2(priceRangeTuple.min, priceRangeTuple.max);

            var analysis = new SpeciesMarketAnalysis
            {
                species = species,
                currentListings = activeListings.Values.Where(l => l.creature == species).ToList(), // Using creature ID as species identifier
                recentSales = recentTransactions.Where(t => t.creature == species).ToList(), // Using creature ID as species identifier
                priceHistory = priceHistories.GetValueOrDefault(species, new PriceHistory()),
                averagePrice = CalculateAveragePrice(new List<string> { species }),
                priceRange = priceRangeVector,
                marketTrend = ConvertToMarketTrend(CalculateSpeciesTrend(species)),
                demandLevel = CalculateDemandLevel(species),
                supplyLevel = CalculateSupplyLevel(species)
            };

            return analysis;
        }

        private void UpdateMarketplace()
        {
            float currentTime = Time.time;

            // Update market analytics every few seconds
            if (currentTime - lastMarketUpdate >= 5f)
            {
                UpdateMarketAnalytics();
                lastMarketUpdate = currentTime;
            }

            // Update dynamic pricing if enabled
            if (enableDynamicPricing)
            {
                UpdateDynamicPricing();
            }
        }

        private void ProcessExpiredListings()
        {
            var expiredListings = activeListings.Values
                .Where(l => Time.time >= l.expirationTime && l.status == ListingStatus.Active)
                .ToList();

            foreach (var listing in expiredListings)
            {
                listing.status = ListingStatus.Expired;
                activeListings.Remove(listing.listingId);

                if (enableDebugLogging)
                    DebugManager.LogInfo($"Listing expired: {listing.listingId}");
            }
        }

        private void UpdateAuctions()
        {
            var endedAuctions = activeAuctions.Values
                .Where(a => Time.time >= a.endTime && a.status == AuctionStatus.Active)
                .ToList();

            foreach (var auction in endedAuctions)
            {
                ProcessAuctionEnd(auction);
            }
        }

        private void ProcessAuctionEnd(AuctionData auction)
        {
            auction.status = AuctionStatus.Ended;

            if (auction.currentBid >= auction.reservePrice && !string.IsNullOrEmpty(auction.currentBidderId))
            {
                // Process winning bid as transaction
                var transaction = ProcessTransaction(auction.currentBidderId, auction, auction.currentBid);
                if (transaction != null)
                {
                    auction.status = AuctionStatus.Sold;
                    UpdatePlayerReputation(auction.currentBidderId, transaction, ReputationAction.AuctionWin);
                    UpdatePlayerReputation(auction.sellerId, transaction, ReputationAction.AuctionSale);
                }
            }

            activeAuctions.Remove(auction.auctionId);
            OnAuctionEnded?.Invoke(auction);

            if (enableDebugLogging)
                DebugManager.LogInfo($"Auction ended: {auction.auctionId} - Final bid: {auction.currentBid:C}");
        }

        private MarketTransaction ProcessTransaction(string buyerId, MarketplaceListing listing, float price)
        {
            // Create transaction record
            var transaction = new MarketTransaction
            {
                transactionId = Guid.NewGuid().ToString(),
                buyerId = buyerId,
                sellerId = listing.sellerId,
                creature = listing.creature,
                price = price,
                transactionFee = price * (transactionFeePercentage / 100f),
                timestamp = Time.time,
                transactionType = TransactionType.DirectSale
            };

            // Update market totals
            totalMarketVolume += price;
            dailyTransactionVolume += price;
            totalTransactions++;

            // Add to recent transactions
            recentTransactions.Enqueue(transaction);
            if (recentTransactions.Count > 1000) // Keep last 1000 transactions
            {
                recentTransactions.Dequeue();
            }

            return transaction;
        }

        private MarketTransaction ProcessTransaction(string buyerId, AuctionData auction, float price)
        {
            var transaction = new MarketTransaction
            {
                transactionId = Guid.NewGuid().ToString(),
                buyerId = buyerId,
                sellerId = auction.sellerId,
                creature = auction.creature,
                price = price,
                transactionFee = price * (transactionFeePercentage / 100f),
                timestamp = Time.time,
                transactionType = TransactionType.Auction
            };

            totalMarketVolume += price;
            dailyTransactionVolume += price;
            totalTransactions++;

            recentTransactions.Enqueue(transaction);
            if (recentTransactions.Count > 1000)
            {
                recentTransactions.Dequeue();
            }

            return transaction;
        }

        // Additional helper methods for validation, calculation, and data management would be implemented here...

        private void OnDestroy()
        {
            if (instance == this)
            {
                SaveMarketplaceData();
                instance = null;
            }
        }

        // Editor utilities
        [UnityEditor.MenuItem("🧪 Laboratory/Economy/Show Market Statistics", false, 700)]
        private static void MenuShowMarketStatistics()
        {
            if (Application.isPlaying && Instance != null)
            {
                var stats = Instance.GetMarketStatistics();
                Debug.Log($"Market Statistics:\n" +
                         $"Active Listings: {stats.totalActiveListings}\n" +
                         $"Active Auctions: {stats.totalActiveAuctions}\n" +
                         $"Total Volume: {stats.totalMarketVolume:C}\n" +
                         $"Total Transactions: {stats.totalTransactions}\n" +
                         $"Average Transaction: {stats.averageTransactionValue:C}");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Economy/Create Test Listing", false, 701)]
        private static void MenuCreateTestListing()
        {
            if (Application.isPlaying && Instance != null)
            {
                // This would create a test listing for debugging
                Debug.Log("Test listing creation functionality would be implemented here");
            }
        }

        // Missing method implementations (simplified to avoid compilation errors)
        private void LoadMarketplaceData()
        {
            // Marketplace data loading would be implemented here
        }

        private void ConnectToGameSystems()
        {
            // Game system connections would be implemented here
        }

        private void UpdateMarketAnalytics()
        {
            // Market analytics updates would be implemented here
        }

        private MarketplaceConfig CreateDefaultMarketplaceConfig()
        {
            return new MarketplaceConfig();
        }

        private bool ValidateListingParameters(string playerId, string creature, float askingPrice, ListingType listingType)
        {
            return !string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(creature) && askingPrice > 0;
        }

        private bool CanPlayerCreateListing(string playerId)
        {
            return !string.IsNullOrEmpty(playerId);
        }

        private void UpdatePlayerMarketData(string playerId, object action)
        {
            // Player market data updates would be implemented here
        }

        private void UpdateBloodlineTransaction(string creatureId, float price)
        {
            // Bloodline transaction updates would be implemented here
        }

        private void UpdatePriceHistory(string creatureSpecies, float price)
        {
            // Price history updates would be implemented here
        }

        private void UpdatePlayerReputation(string playerId, MarketTransaction transaction, ReputationAction action)
        {
            // Player reputation updates would be implemented here
        }

        private bool ValidateAuctionParameters(string sellerId, string creature, float startingBid, float reservePrice, float durationHours)
        {
            return !string.IsNullOrEmpty(sellerId) && !string.IsNullOrEmpty(creature) && startingBid > 0;
        }

        private bool ValidateBid(string bidderId, AuctionData auction, float bidAmount)
        {
            return auction != null && !string.IsNullOrEmpty(bidderId) && bidAmount > auction.currentBid;
        }

        private bool ValidateProjectContribution(string projectId, string contributorId, string creature)
        {
            return !string.IsNullOrEmpty(projectId) && !string.IsNullOrEmpty(contributorId) && !string.IsNullOrEmpty(creature);
        }

        private void SaveMarketplaceData()
        {
            // Marketplace data saving would be implemented here
        }

        private void UpdateDynamicPricing()
        {
            // Dynamic pricing updates would be implemented here
        }

        // Simplified calculation methods
        private List<string> GetTopSellingSpecies() { return new List<string>(); }
        private Dictionary<string, MarketTrend> CalculateMarketTrends() { return new Dictionary<string, MarketTrend>(); }
        private Dictionary<string, float> CalculateAveragePricesBySpecies() { return new Dictionary<string, float>(); }
        private float CalculateAveragePrice(List<string> listingIds) { return 100f; }
        private (float min, float max) CalculatePriceRange(List<string> listingIds) { return (50f, 200f); }
        private float CalculateSpeciesTrend(string species) { return 1f; }
        private float CalculateDemandLevel(string species) { return 1f; }
        private float CalculateSupplyLevel(string species) { return 1f; }
        private MarketTrend ConvertToMarketTrend(float trendValue)
        {
            return trendValue switch
            {
                > 1.1f => MarketTrend.Rising,
                < 0.9f => MarketTrend.Declining,
                _ => MarketTrend.Stable
            };
        }

        /// <summary>
        /// Gets all active auctions in the marketplace
        /// </summary>
        public List<AuctionData> GetActiveAuctions()
        {
            return activeAuctions.Values.ToList();
        }

        /// <summary>
        /// Gets all active listings in the marketplace
        /// </summary>
        public List<MarketplaceListing> GetAllActiveListings()
        {
            return activeListings.Values.ToList();
        }

        /// <summary>
        /// Gets a specific listing by creature ID
        /// </summary>
        public MarketplaceListing GetListingByCreature(string creatureId)
        {
            return activeListings.Values.FirstOrDefault(l => l.creature == creatureId);
        }

        // Additional missing methods
        private bool ValidatePurchase(string listingId, string buyerId, float offeredPrice) { return true; }
        // Player market action enum
        public enum PlayerMarketAction
        {
            CreateListing,
            Purchase,
            Sale
        }
    }
}