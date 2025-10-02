using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Marketplace
{
    /// <summary>
    /// Genetic Marketplace and Economics System for Project Chimera.
    /// Creates a thriving economy around genetic discoveries, creature trading,
    /// breeding services, and knowledge commerce.
    ///
    /// Features:
    /// - Creature Trading: Buy/sell creatures with verified genetic profiles
    /// - Genetic Material Exchange: Trade genetic samples and breeding rights
    /// - Breeding Services: Commission professional breeders for specific traits
    /// - Knowledge Commerce: Buy/sell genetic research and breeding techniques
    /// - Market Intelligence: Track genetic trends and price fluctuations
    /// - Economic Events: Market crashes, genetic booms, rarity discoveries
    /// </summary>
    public class GeneticMarketplaceSystem : MonoBehaviour
    {
        [Header("System Configuration")]
        [SerializeField] private GeneticMarketplaceConfig config;
        [SerializeField] private bool enableCreatureTrading = true;
        [SerializeField] private bool enableGeneticMaterialTrading = true;
        [SerializeField] private bool enableBreedingServices = true;
        [SerializeField] private bool enableKnowledgeCommerce = true;

        [Header("Market Dynamics")]
        [SerializeField] private float priceVolatility = 0.1f;
        [SerializeField] private float marketUpdateInterval = 300f; // 5 minutes
        [SerializeField] private int maxListingsPerPlayer = 10;
        [SerializeField] private float transactionFeePercentage = 0.05f; // 5% fee

        [Header("Economic Events")]
        [SerializeField] private float economicEventRate = 0.01f; // 1% chance per day

        // Market data and listings
        private Dictionary<string, MarketListing> activeListings = new Dictionary<string, MarketListing>();
        private Dictionary<string, List<TransactionRecord>> transactionHistory = new Dictionary<string, List<TransactionRecord>>();
        private Dictionary<string, MarketPriceData> priceDatabase = new Dictionary<string, MarketPriceData>();

        // Economic state
        private Dictionary<string, float> traitDemandIndex = new Dictionary<string, float>();
        private List<EconomicEvent> activeEconomicEvents = new List<EconomicEvent>();
        private float globalMarketHealth = 1f;

        // Player economics
        private Dictionary<string, PlayerEconomicProfile> playerProfiles = new Dictionary<string, PlayerEconomicProfile>();
        private Dictionary<string, List<string>> playerWatchlists = new Dictionary<string, List<string>>();

        // Market intelligence
        private Dictionary<string, TrendAnalysis> marketTrends = new Dictionary<string, TrendAnalysis>();
        private List<MarketAlert> marketAlerts = new List<MarketAlert>();

        // Events
        public static event Action<MarketListing> OnListingCreated;
        public static event Action<TransactionRecord> OnTransactionCompleted;
        public static event Action<EconomicEvent> OnEconomicEventTriggered;
        public static event Action<MarketAlert> OnMarketAlert;
        public static event Action<string, float> OnPriceUpdate; // Item ID, New Price

        void Start()
        {
            InitializeMarketplace();
            InvokeRepeating(nameof(UpdateMarketPrices), marketUpdateInterval, marketUpdateInterval);
            InvokeRepeating(nameof(ProcessEconomicEvents), 3600f, 86400f); // Check daily for events
            InvokeRepeating(nameof(UpdateMarketTrends), 1800f, 1800f); // Update trends every 30 min
        }

        #region Initialization

        private void InitializeMarketplace()
        {
            LoadMarketData();
            InitializeBasePrices();
            SeedMarketWithListings();

            UnityEngine.Debug.Log("Genetic Marketplace System initialized - The economy is open for business!");
        }

        private void InitializeBasePrices()
        {
            // Initialize base prices for common genetic traits
            var commonTraits = new[]
            {
                "Strength", "Agility", "Intelligence", "Vitality", "Charisma",
                "Fire Resistance", "Water Breathing", "Night Vision", "Enhanced Speed"
            };

            foreach (var trait in commonTraits)
            {
                priceDatabase[trait] = new MarketPriceData
                {
                    itemId = trait,
                    currentPrice = UnityEngine.Random.Range(100f, 500f),
                    basePrice = 200f,
                    historicalPrices = new List<PricePoint>(),
                    volume = 0,
                    lastUpdate = Time.time
                };

                traitDemandIndex[trait] = 1f; // Neutral demand
            }
        }

        private void SeedMarketWithListings()
        {
            // Create some initial market listings for gameplay
            for (int i = 0; i < 10; i++)
            {
                CreateSeedListing();
            }
        }

        private void CreateSeedListing()
        {
            var listingTypes = Enum.GetValues(typeof(ListingType)).Cast<ListingType>().ToArray();
            var listingType = listingTypes[UnityEngine.Random.Range(0, listingTypes.Length)];

            var listing = new MarketListing
            {
                id = Guid.NewGuid().ToString(),
                sellerId = $"NPC_Seller_{UnityEngine.Random.Range(1000, 9999)}",
                listingType = listingType,
                title = GenerateListingTitle(listingType),
                description = GenerateListingDescription(listingType),
                price = UnityEngine.Random.Range(50f, 1000f),
                currency = CurrencyType.GeneticCredits,
                listingTime = Time.time,
                expirationTime = Time.time + (86400f * UnityEngine.Random.Range(7, 30)), // 7-30 days
                status = ListingStatus.Active,
                featured = UnityEngine.Random.value < 0.1f // 10% chance featured
            };

            // Add type-specific data
            switch (listingType)
            {
                case ListingType.Creature:
                    listing.creatureData = GenerateMarketCreature();
                    break;
                case ListingType.GeneticMaterial:
                    listing.geneticSample = GenerateGeneticSample();
                    break;
                case ListingType.BreedingService:
                    listing.serviceData = GenerateBreedingService();
                    break;
                case ListingType.Knowledge:
                    listing.knowledgeData = GenerateKnowledgeItem();
                    break;
            }

            activeListings[listing.id] = listing;
        }

        #endregion

        #region Creature Trading

        /// <summary>
        /// Lists a creature for sale on the marketplace
        /// </summary>
        public MarketListing ListCreatureForSale(string sellerId, GeneticProfile creature, float price,
            string title, string description, int durationDays = 14)
        {
            if (!enableCreatureTrading) return null;

            var playerProfile = EnsurePlayerProfile(sellerId);
            if (playerProfile.activeListings >= maxListingsPerPlayer)
            {
                UnityEngine.Debug.Log($"Player {sellerId} has reached maximum listing limit");
                return null;
            }

            var listing = new MarketListing
            {
                id = Guid.NewGuid().ToString(),
                sellerId = sellerId,
                listingType = ListingType.Creature,
                title = title,
                description = description,
                price = price,
                currency = CurrencyType.GeneticCredits,
                listingTime = Time.time,
                expirationTime = Time.time + (durationDays * 86400f),
                status = ListingStatus.Active,
                creatureData = new CreatureMarketData
                {
                    geneticProfile = creature,
                    generation = creature.Generation,
                    rarityScore = CalculateCreatureRarity(creature),
                    healthStatus = UnityEngine.Random.Range(0.8f, 1f),
                    ageInDays = UnityEngine.Random.Range(30, 365),
                    breedingHistory = GenerateBreedingHistory(creature)
                }
            };

            // Calculate market-suggested price
            var suggestedPrice = CalculateCreatureMarketValue(creature);
            listing.marketSuggestedPrice = suggestedPrice;

            activeListings[listing.id] = listing;
            playerProfile.activeListings++;

            OnListingCreated?.Invoke(listing);
            UnityEngine.Debug.Log($"Creature listed for sale: {title} by {GetPlayerName(sellerId)} for {price} GC");

            return listing;
        }

        /// <summary>
        /// Purchases a creature from the marketplace
        /// </summary>
        public TransactionRecord PurchaseCreature(string buyerId, string listingId)
        {
            if (!activeListings.ContainsKey(listingId))
            {
                UnityEngine.Debug.Log("Listing not found or no longer active");
                return null;
            }

            var listing = activeListings[listingId];
            if (listing.status != ListingStatus.Active || listing.listingType != ListingType.Creature)
            {
                UnityEngine.Debug.Log("Invalid listing for creature purchase");
                return null;
            }

            var buyerProfile = EnsurePlayerProfile(buyerId);
            var sellerProfile = EnsurePlayerProfile(listing.sellerId);

            // Check if buyer has sufficient funds
            if (buyerProfile.balance < listing.price)
            {
                UnityEngine.Debug.Log("Insufficient funds for purchase");
                return null;
            }

            // Process transaction
            var transaction = ProcessTransaction(buyerId, listing);

            // Update player balances
            var fee = listing.price * transactionFeePercentage;
            buyerProfile.balance -= listing.price;
            sellerProfile.balance += (listing.price - fee);

            // Update transaction history
            UpdateTransactionHistory(listing.creatureData.geneticProfile.GetTraitNames(), transaction);

            // Update market prices based on transaction
            UpdatePricesFromTransaction(listing);

            // Remove listing
            listing.status = ListingStatus.Sold;
            activeListings.Remove(listingId);
            sellerProfile.activeListings--;

            OnTransactionCompleted?.Invoke(transaction);
            UnityEngine.Debug.Log($"Creature purchased: {listing.title} by {GetPlayerName(buyerId)}");

            return transaction;
        }

        #endregion

        #region Genetic Material Trading

        /// <summary>
        /// Lists genetic material for sale
        /// </summary>
        public MarketListing ListGeneticMaterial(string sellerId, GeneticSample sample, float price,
            string title, string description, int durationDays = 7)
        {
            if (!enableGeneticMaterialTrading) return null;

            var listing = new MarketListing
            {
                id = Guid.NewGuid().ToString(),
                sellerId = sellerId,
                listingType = ListingType.GeneticMaterial,
                title = title,
                description = description,
                price = price,
                currency = CurrencyType.GeneticCredits,
                listingTime = Time.time,
                expirationTime = Time.time + (durationDays * 86400f),
                status = ListingStatus.Active,
                geneticSample = sample
            };

            listing.marketSuggestedPrice = CalculateGeneticMaterialValue(sample);

            activeListings[listing.id] = listing;
            EnsurePlayerProfile(sellerId).activeListings++;

            OnListingCreated?.Invoke(listing);
            UnityEngine.Debug.Log($"Genetic material listed: {title} for {price} GC");

            return listing;
        }

        /// <summary>
        /// Creates a breeding service listing
        /// </summary>
        public MarketListing OfferBreedingService(string providerId, BreedingServiceData serviceData,
            float price, string title, string description)
        {
            if (!enableBreedingServices) return null;

            var listing = new MarketListing
            {
                id = Guid.NewGuid().ToString(),
                sellerId = providerId,
                listingType = ListingType.BreedingService,
                title = title,
                description = description,
                price = price,
                currency = CurrencyType.GeneticCredits,
                listingTime = Time.time,
                expirationTime = Time.time + (serviceData.durationDays * 86400f),
                status = ListingStatus.Active,
                serviceData = serviceData
            };

            activeListings[listing.id] = listing;
            EnsurePlayerProfile(providerId).activeListings++;

            OnListingCreated?.Invoke(listing);
            UnityEngine.Debug.Log($"Breeding service offered: {title} by {GetPlayerName(providerId)}");

            return listing;
        }

        #endregion

        #region Knowledge Commerce

        /// <summary>
        /// Lists knowledge/research for sale
        /// </summary>
        public MarketListing SellKnowledge(string sellerId, KnowledgeData knowledge, float price,
            string title, string description)
        {
            if (!enableKnowledgeCommerce) return null;

            var listing = new MarketListing
            {
                id = Guid.NewGuid().ToString(),
                sellerId = sellerId,
                listingType = ListingType.Knowledge,
                title = title,
                description = description,
                price = price,
                currency = CurrencyType.ResearchPoints,
                listingTime = Time.time,
                expirationTime = Time.time + (30 * 86400f), // 30 days
                status = ListingStatus.Active,
                knowledgeData = knowledge
            };

            listing.marketSuggestedPrice = CalculateKnowledgeValue(knowledge);

            activeListings[listing.id] = listing;
            EnsurePlayerProfile(sellerId).activeListings++;

            OnListingCreated?.Invoke(listing);
            UnityEngine.Debug.Log($"Knowledge listed: {title} for {price} RP");

            return listing;
        }

        #endregion

        #region Market Intelligence

        /// <summary>
        /// Searches marketplace listings with filters
        /// </summary>
        public MarketListing[] SearchMarketplace(MarketSearchCriteria criteria)
        {
            var results = activeListings.Values.Where(listing => listing.status == ListingStatus.Active);

            // Apply filters
            if (criteria.listingType.HasValue)
                results = results.Where(l => l.listingType == criteria.listingType.Value);

            if (criteria.maxPrice.HasValue)
                results = results.Where(l => l.price <= criteria.maxPrice.Value);

            if (criteria.minPrice.HasValue)
                results = results.Where(l => l.price >= criteria.minPrice.Value);

            if (!string.IsNullOrEmpty(criteria.searchTerm))
            {
                var term = criteria.searchTerm.ToLower();
                results = results.Where(l =>
                    l.title.ToLower().Contains(term) ||
                    l.description.ToLower().Contains(term));
            }

            if (criteria.requiredTraits != null && criteria.requiredTraits.Length > 0)
            {
                results = results.Where(l => HasRequiredTraits(l, criteria.requiredTraits));
            }

            // Sort results
            results = criteria.sortBy switch
            {
                MarketSortBy.PriceAscending => results.OrderBy(l => l.price),
                MarketSortBy.PriceDescending => results.OrderByDescending(l => l.price),
                MarketSortBy.DateNewest => results.OrderByDescending(l => l.listingTime),
                MarketSortBy.DateOldest => results.OrderBy(l => l.listingTime),
                MarketSortBy.Relevance => results.OrderByDescending(l => CalculateRelevanceScore(l, criteria)),
                _ => results.OrderByDescending(l => l.listingTime)
            };

            return results.Take(criteria.maxResults).ToArray();
        }

        /// <summary>
        /// Gets price history for a specific genetic trait
        /// </summary>
        public PriceHistoryData GetPriceHistory(string traitName, int daysBack = 30)
        {
            if (!priceDatabase.ContainsKey(traitName))
                return null;

            var priceData = priceDatabase[traitName];
            var cutoffTime = Time.time - (daysBack * 86400f);

            var relevantPrices = priceData.historicalPrices
                .Where(p => p.timestamp >= cutoffTime)
                .OrderBy(p => p.timestamp)
                .ToArray();

            return new PriceHistoryData
            {
                traitName = traitName,
                currentPrice = priceData.currentPrice,
                pricePoints = relevantPrices,
                averagePrice = relevantPrices.Length > 0 ? relevantPrices.Average(p => p.price) : priceData.currentPrice,
                priceChange = CalculatePriceChange(relevantPrices),
                volume = priceData.volume
            };
        }

        /// <summary>
        /// Gets current market trends and analysis
        /// </summary>
        public MarketIntelligenceReport GetMarketIntelligence()
        {
            return new MarketIntelligenceReport
            {
                globalMarketHealth = globalMarketHealth,
                activeListings = activeListings.Count,
                totalTransactionVolume = CalculateDailyVolume(),
                trendingTraits = GetTrendingTraits(),
                marketAlerts = marketAlerts.ToArray(),
                economicEvents = activeEconomicEvents.ToArray(),
                priceMovers = GetTopPriceMovers(),
                demandIndex = traitDemandIndex.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        #endregion

        #region Price Calculations

        private float CalculateCreatureMarketValue(GeneticProfile creature)
        {
            float baseValue = 100f;

            // Rarity multiplier
            var rarity = CalculateCreatureRarity(creature);
            baseValue *= (1f + rarity);

            // Generation bonus
            baseValue += creature.Generation * 10f;

            // Trait value
            foreach (var gene in creature.Genes.Where(g => g.isActive && g.value.HasValue))
            {
                var traitValue = GetTraitMarketValue(gene.traitName);
                var geneContribution = traitValue * gene.value.Value;
                baseValue += geneContribution;

                // Mutation bonus
                if (gene.isMutation)
                    baseValue += geneContribution * 0.5f;
            }

            // Genetic purity affects value
            baseValue *= creature.GetGeneticPurity();

            // Market demand modifier
            var demandModifier = CalculateDemandModifier(creature);
            baseValue *= demandModifier;

            return Mathf.Round(baseValue);
        }

        private float CalculateGeneticMaterialValue(GeneticSample sample)
        {
            float baseValue = 50f;

            // Quality affects value
            baseValue *= sample.purity;

            // Trait rarity
            var traitValue = GetTraitMarketValue(sample.traitName);
            baseValue += traitValue * 0.3f;

            // Market demand
            var demand = traitDemandIndex.ContainsKey(sample.traitName) ? traitDemandIndex[sample.traitName] : 1f;
            baseValue *= demand;

            return Mathf.Round(baseValue);
        }

        private float CalculateKnowledgeValue(KnowledgeData knowledge)
        {
            float baseValue = 25f;

            // Quality and completeness
            baseValue *= knowledge.completeness;

            // Rarity of knowledge
            baseValue *= knowledge.rarity;

            // Research complexity
            baseValue += knowledge.researchHours * 0.1f;

            return Mathf.Round(baseValue);
        }

        private float GetTraitMarketValue(string traitName)
        {
            if (priceDatabase.ContainsKey(traitName))
                return priceDatabase[traitName].currentPrice;

            // Default value for unknown traits
            return 100f;
        }

        private float CalculateCreatureRarity(GeneticProfile creature)
        {
            float rarity = 0f;

            // Mutations increase rarity
            rarity += creature.Mutations.Count() * 0.2f;

            // High-value traits increase rarity
            var highValueTraits = creature.Genes.Count(g => g.value.HasValue && g.value.Value > 0.8f);
            rarity += highValueTraits * 0.1f;

            // Generation affects rarity
            rarity += creature.Generation * 0.05f;

            return Mathf.Clamp01(rarity);
        }

        private float CalculateDemandModifier(GeneticProfile creature)
        {
            float modifier = 1f;

            foreach (var gene in creature.Genes.Where(g => g.isActive))
            {
                if (traitDemandIndex.ContainsKey(gene.traitName))
                {
                    modifier *= traitDemandIndex[gene.traitName];
                }
            }

            return Mathf.Clamp(modifier, 0.5f, 3f); // Limit extreme price swings
        }

        #endregion

        #region Market Dynamics

        private void UpdateMarketPrices()
        {
            foreach (var kvp in priceDatabase.ToArray())
            {
                var priceData = kvp.Value;

                // Apply volatility
                var priceChange = (UnityEngine.Random.value - 0.5f) * 2f * priceVolatility;

                // Apply demand influence
                var demand = traitDemandIndex.ContainsKey(kvp.Key) ? traitDemandIndex[kvp.Key] : 1f;
                priceChange *= demand;

                // Apply global market health
                priceChange *= globalMarketHealth;

                var newPrice = Mathf.Max(10f, priceData.currentPrice * (1f + priceChange));

                // Record price history
                priceData.historicalPrices.Add(new PricePoint
                {
                    price = newPrice,
                    timestamp = Time.time,
                    volume = UnityEngine.Random.Range(0, 10)
                });

                // Limit history size
                if (priceData.historicalPrices.Count > 1000)
                    priceData.historicalPrices.RemoveAt(0);

                priceData.currentPrice = newPrice;
                priceData.lastUpdate = Time.time;

                OnPriceUpdate?.Invoke(kvp.Key, newPrice);
            }
        }

        private void ProcessEconomicEvents()
        {
            // Check for new economic events
            if (UnityEngine.Random.value < economicEventRate)
            {
                TriggerRandomEconomicEvent();
            }

            // Update existing events
            for (int i = activeEconomicEvents.Count - 1; i >= 0; i--)
            {
                var economicEvent = activeEconomicEvents[i];
                if (Time.time > economicEvent.endTime)
                {
                    EndEconomicEvent(economicEvent);
                    activeEconomicEvents.RemoveAt(i);
                }
            }
        }

        private void TriggerRandomEconomicEvent()
        {
            var eventTypes = Enum.GetValues(typeof(EconomicEventType)).Cast<EconomicEventType>().ToArray();
            var eventType = eventTypes[UnityEngine.Random.Range(0, eventTypes.Length)];

            var economicEvent = new EconomicEvent
            {
                id = Guid.NewGuid().ToString(),
                type = eventType,
                startTime = Time.time,
                duration = UnityEngine.Random.Range(86400f, 604800f), // 1-7 days
                intensity = UnityEngine.Random.Range(0.3f, 0.8f),
                affectedTraits = SelectAffectedTraits(eventType),
                description = GenerateEventDescription(eventType)
            };

            economicEvent.endTime = economicEvent.startTime + economicEvent.duration;

            ApplyEconomicEventEffects(economicEvent);
            activeEconomicEvents.Add(economicEvent);

            OnEconomicEventTriggered?.Invoke(economicEvent);
            UnityEngine.Debug.Log($"Economic event triggered: {eventType} affecting {economicEvent.affectedTraits.Length} traits");
        }

        private void ApplyEconomicEventEffects(EconomicEvent economicEvent)
        {
            foreach (var trait in economicEvent.affectedTraits)
            {
                var modifier = economicEvent.type switch
                {
                    EconomicEventType.GeneticBoom => 1f + economicEvent.intensity,
                    EconomicEventType.MarketCrash => 1f - economicEvent.intensity,
                    EconomicEventType.RarityDiscovery => 1f + (economicEvent.intensity * 0.5f),
                    EconomicEventType.SupplyShortage => 1f + (economicEvent.intensity * 0.7f),
                    EconomicEventType.DemandSpike => 1f + (economicEvent.intensity * 0.6f),
                    _ => 1f
                };

                if (traitDemandIndex.ContainsKey(trait))
                    traitDemandIndex[trait] *= modifier;
                else
                    traitDemandIndex[trait] = modifier;
            }

            // Create market alert
            var alert = new MarketAlert
            {
                id = Guid.NewGuid().ToString(),
                type = MarketAlertType.EconomicEvent,
                message = $"{economicEvent.type}: {economicEvent.description}",
                timestamp = Time.time,
                severity = economicEvent.intensity > 0.6f ? AlertSeverity.High : AlertSeverity.Medium,
                affectedItems = economicEvent.affectedTraits.ToList()
            };

            marketAlerts.Add(alert);
            OnMarketAlert?.Invoke(alert);
        }

        private void EndEconomicEvent(EconomicEvent economicEvent)
        {
            // Gradually return affected traits to normal demand
            foreach (var trait in economicEvent.affectedTraits)
            {
                if (traitDemandIndex.ContainsKey(trait))
                {
                    traitDemandIndex[trait] = Mathf.Lerp(traitDemandIndex[trait], 1f, 0.5f);
                }
            }

            UnityEngine.Debug.Log($"Economic event ended: {economicEvent.type}");
        }

        #endregion

        #region Helper Methods

        private TransactionRecord ProcessTransaction(string buyerId, MarketListing listing)
        {
            return new TransactionRecord
            {
                id = Guid.NewGuid().ToString(),
                buyerId = buyerId,
                sellerId = listing.sellerId,
                listingId = listing.id,
                listingType = listing.listingType,
                price = listing.price,
                currency = listing.currency,
                transactionTime = Time.time,
                fee = listing.price * transactionFeePercentage,
                itemData = GetItemDataFromListing(listing)
            };
        }

        private void UpdateTransactionHistory(List<string> traits, TransactionRecord transaction)
        {
            foreach (var trait in traits)
            {
                if (!transactionHistory.ContainsKey(trait))
                    transactionHistory[trait] = new List<TransactionRecord>();

                transactionHistory[trait].Add(transaction);

                // Limit history size
                if (transactionHistory[trait].Count > 100)
                    transactionHistory[trait].RemoveAt(0);
            }
        }

        private void UpdatePricesFromTransaction(MarketListing listing)
        {
            if (listing.listingType == ListingType.Creature && listing.creatureData != null)
            {
                var traits = listing.creatureData.geneticProfile.GetTraitNames();
                foreach (var trait in traits)
                {
                    if (priceDatabase.ContainsKey(trait))
                    {
                        // Transaction affects market price slightly
                        var priceData = priceDatabase[trait];
                        var influence = 0.05f; // 5% influence
                        var targetPrice = listing.price / traits.Count; // Distribute price among traits

                        priceData.currentPrice = Mathf.Lerp(priceData.currentPrice, targetPrice, influence);
                        priceData.volume++;
                    }
                }
            }
        }

        private PlayerEconomicProfile EnsurePlayerProfile(string playerId)
        {
            if (!playerProfiles.ContainsKey(playerId))
            {
                playerProfiles[playerId] = new PlayerEconomicProfile
                {
                    playerId = playerId,
                    balance = config.startingBalance,
                    researchPoints = config.startingResearchPoints,
                    transactionCount = 0,
                    totalSpent = 0f,
                    totalEarned = 0f,
                    reputation = 0f,
                    activeListings = 0,
                    joinDate = Time.time
                };
            }

            return playerProfiles[playerId];
        }

        private string GetPlayerName(string playerId)
        {
            return $"Player_{playerId[..Math.Min(8, playerId.Length)]}";
        }

        private bool HasRequiredTraits(MarketListing listing, string[] requiredTraits)
        {
            if (listing.listingType != ListingType.Creature || listing.creatureData?.geneticProfile == null)
                return false;

            var creatureTraits = listing.creatureData.geneticProfile.GetTraitNames();
            return requiredTraits.All(required => creatureTraits.Any(trait => trait.Contains(required)));
        }

        private float CalculateRelevanceScore(MarketListing listing, MarketSearchCriteria criteria)
        {
            float score = 0f;

            // Price relevance
            if (criteria.maxPrice.HasValue)
            {
                var priceRatio = listing.price / criteria.maxPrice.Value;
                score += (1f - priceRatio) * 0.3f;
            }

            // Trait relevance
            if (criteria.requiredTraits != null && listing.listingType == ListingType.Creature)
            {
                var matchCount = criteria.requiredTraits.Count(trait =>
                    listing.creatureData?.geneticProfile?.GetTraitNames().Any(t => t.Contains(trait)) == true);
                score += (float)matchCount / criteria.requiredTraits.Length * 0.4f;
            }

            // Recency bonus
            var daysSinceListing = (Time.time - listing.listingTime) / 86400f;
            score += Mathf.Max(0f, (30f - daysSinceListing) / 30f) * 0.3f;

            return score;
        }

        private void UpdateMarketTrends()
        {
            foreach (var trait in priceDatabase.Keys)
            {
                var trend = AnalyzeTrend(trait);
                marketTrends[trait] = trend;

                // Generate alerts for significant trends
                if (trend.significance > 0.7f)
                {
                    GenerateTrendAlert(trait, trend);
                }
            }
        }

        private TrendAnalysis AnalyzeTrend(string trait)
        {
            if (!priceDatabase.ContainsKey(trait))
                return new TrendAnalysis { trait = trait, direction = TrendDirection.Stable };

            var priceData = priceDatabase[trait];
            var recentPrices = priceData.historicalPrices
                .Where(p => p.timestamp >= Time.time - 86400f) // Last 24 hours
                .OrderBy(p => p.timestamp)
                .ToArray();

            if (recentPrices.Length < 2)
                return new TrendAnalysis { trait = trait, direction = TrendDirection.Stable };

            var startPrice = recentPrices[0].price;
            var endPrice = recentPrices[recentPrices.Length - 1].price;
            var change = (endPrice - startPrice) / startPrice;

            var direction = Math.Abs(change) < 0.05f ? TrendDirection.Stable :
                          change > 0 ? TrendDirection.Rising : TrendDirection.Falling;

            return new TrendAnalysis
            {
                trait = trait,
                direction = direction,
                strength = Math.Abs(change),
                significance = CalculateTrendSignificance(recentPrices),
                volume = recentPrices.Sum(p => p.volume),
                priceChange = change
            };
        }

        private float CalculateTrendSignificance(PricePoint[] prices)
        {
            if (prices.Length < 3) return 0f;

            // Calculate how consistent the trend is
            var changes = new List<float>();
            for (int i = 1; i < prices.Length; i++)
            {
                var change = (prices[i].price - prices[i-1].price) / prices[i-1].price;
                changes.Add(change);
            }

            // Consistency is measured by how many changes go in the same direction
            var positiveChanges = changes.Count(c => c > 0);
            var consistency = Math.Max(positiveChanges, changes.Count - positiveChanges) / (float)changes.Count;

            return consistency;
        }

        private void GenerateTrendAlert(string trait, TrendAnalysis trend)
        {
            var alert = new MarketAlert
            {
                id = Guid.NewGuid().ToString(),
                type = MarketAlertType.PriceTrend,
                message = $"{trait} price is {trend.direction.ToString().ToLower()} with {trend.strength:P1} change",
                timestamp = Time.time,
                severity = trend.strength > 0.2f ? AlertSeverity.High : AlertSeverity.Medium,
                affectedItems = new List<string> { trait }
            };

            marketAlerts.Add(alert);
            OnMarketAlert?.Invoke(alert);
        }

        // Additional helper methods for data generation
        private string GenerateListingTitle(ListingType type)
        {
            return type switch
            {
                ListingType.Creature => $"Rare {GetRandomSpeciesName()} - Generation {UnityEngine.Random.Range(2, 10)}",
                ListingType.GeneticMaterial => $"{GetRandomTraitName()} Genetic Sample",
                ListingType.BreedingService => $"Professional Breeding Service - {GetRandomSpecialization()}",
                ListingType.Knowledge => $"{GetRandomResearchTopic()} Research Data",
                _ => "Market Listing"
            };
        }

        private string GenerateListingDescription(ListingType type)
        {
            return type switch
            {
                ListingType.Creature => "Exceptional creature with verified genetic lineage and health certification.",
                ListingType.GeneticMaterial => "High-quality genetic material suitable for breeding programs.",
                ListingType.BreedingService => "Expert breeding services with guaranteed genetic outcomes.",
                ListingType.Knowledge => "Comprehensive research data with detailed analysis and documentation.",
                _ => "Quality marketplace item"
            };
        }

        private CreatureMarketData GenerateMarketCreature()
        {
            return new CreatureMarketData
            {
                geneticProfile = GeneticProfile.CreateRandom(new Laboratory.Chimera.Creatures.CreatureStats()),
                generation = UnityEngine.Random.Range(2, 15),
                rarityScore = UnityEngine.Random.Range(0.1f, 0.9f),
                healthStatus = UnityEngine.Random.Range(0.7f, 1f),
                ageInDays = UnityEngine.Random.Range(30, 500),
                breedingHistory = new List<string> { "Unknown lineage", "Certified healthy" }
            };
        }

        private GeneticSample GenerateGeneticSample()
        {
            return new GeneticSample
            {
                traitName = GetRandomTraitName(),
                purity = UnityEngine.Random.Range(0.6f, 0.95f),
                viability = UnityEngine.Random.Range(0.5f, 0.9f),
                extractionDate = Time.time - UnityEngine.Random.Range(0, 86400f * 7),
                sourceCreatureId = Guid.NewGuid().ToString()
            };
        }

        private BreedingServiceData GenerateBreedingService()
        {
            return new BreedingServiceData
            {
                serviceType = "Custom Trait Breeding",
                guaranteedTraits = new[] { GetRandomTraitName(), GetRandomTraitName() },
                successRate = UnityEngine.Random.Range(0.6f, 0.95f),
                durationDays = UnityEngine.Random.Range(14, 60),
                providerReputation = UnityEngine.Random.Range(50f, 200f)
            };
        }

        private KnowledgeData GenerateKnowledgeItem()
        {
            return new KnowledgeData
            {
                title = GetRandomResearchTopic(),
                knowledgeType = "Breeding Technique",
                completeness = UnityEngine.Random.Range(0.7f, 1f),
                rarity = UnityEngine.Random.Range(0.3f, 0.8f),
                researchHours = UnityEngine.Random.Range(10, 100),
                verification = "Peer Reviewed"
            };
        }

        private string GetRandomSpeciesName()
        {
            var names = new[] { "Flame Drake", "Crystal Wyrm", "Shadow Wolf", "Storm Eagle", "Frost Bear" };
            return names[UnityEngine.Random.Range(0, names.Length)];
        }

        private string GetRandomTraitName()
        {
            var traits = new[] { "Fire Resistance", "Lightning Speed", "Iron Hide", "Night Vision", "Regeneration" };
            return traits[UnityEngine.Random.Range(0, traits.Length)];
        }

        private string GetRandomSpecialization()
        {
            var specs = new[] { "Combat Genetics", "Elemental Traits", "Speed Enhancement", "Defensive Builds" };
            return specs[UnityEngine.Random.Range(0, specs.Length)];
        }

        private string GetRandomResearchTopic()
        {
            var topics = new[] { "Advanced Breeding", "Genetic Stability", "Trait Combination", "Mutation Analysis" };
            return topics[UnityEngine.Random.Range(0, topics.Length)];
        }

        private string[] SelectAffectedTraits(EconomicEventType eventType)
        {
            var allTraits = priceDatabase.Keys.ToArray();
            var count = UnityEngine.Random.Range(2, 6);
            return allTraits.OrderBy(x => UnityEngine.Random.value).Take(count).ToArray();
        }

        private string GenerateEventDescription(EconomicEventType eventType)
        {
            return eventType switch
            {
                EconomicEventType.GeneticBoom => "A breakthrough discovery has increased demand for specific genetic traits",
                EconomicEventType.MarketCrash => "Economic uncertainty has reduced overall market confidence",
                EconomicEventType.RarityDiscovery => "New rare genetic combinations have been discovered",
                EconomicEventType.SupplyShortage => "Limited availability of genetic material has driven up prices",
                EconomicEventType.DemandSpike => "Increased breeding activity has created high demand",
                _ => "Market conditions are fluctuating"
            };
        }

        private object GetItemDataFromListing(MarketListing listing)
        {
            return listing.listingType switch
            {
                ListingType.Creature => listing.creatureData,
                ListingType.GeneticMaterial => listing.geneticSample,
                ListingType.BreedingService => listing.serviceData,
                ListingType.Knowledge => listing.knowledgeData,
                _ => null
            };
        }

        private List<string> GenerateBreedingHistory(GeneticProfile creature)
        {
            return new List<string>
            {
                $"Generation {creature.Generation} lineage",
                "Health certified",
                "Genetic authenticity verified"
            };
        }

        private float CalculateDailyVolume()
        {
            return transactionHistory.Values
                .SelectMany(transactions => transactions)
                .Where(t => t.transactionTime >= Time.time - 86400f)
                .Sum(t => t.price);
        }

        private string[] GetTrendingTraits()
        {
            return marketTrends
                .Where(kvp => kvp.Value.direction != TrendDirection.Stable)
                .OrderByDescending(kvp => kvp.Value.significance)
                .Take(5)
                .Select(kvp => kvp.Key)
                .ToArray();
        }

        private Dictionary<string, float> GetTopPriceMovers()
        {
            return marketTrends
                .OrderByDescending(kvp => Math.Abs(kvp.Value.priceChange))
                .Take(10)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.priceChange);
        }

        private float CalculatePriceChange(PricePoint[] prices)
        {
            if (prices.Length < 2) return 0f;

            var startPrice = prices[0].price;
            var endPrice = prices[prices.Length - 1].price;
            return (endPrice - startPrice) / startPrice;
        }

        private void LoadMarketData()
        {
            // Load saved market data from persistent storage
        }

        private void SaveMarketData()
        {
            // Save market data to persistent storage
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveMarketData();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets all active listings of a specific type
        /// </summary>
        public MarketListing[] GetActiveListings(ListingType? type = null)
        {
            var listings = activeListings.Values.Where(l => l.status == ListingStatus.Active);

            if (type.HasValue)
                listings = listings.Where(l => l.listingType == type.Value);

            return listings.OrderByDescending(l => l.listingTime).ToArray();
        }

        /// <summary>
        /// Gets player's economic profile
        /// </summary>
        public PlayerEconomicProfile GetPlayerProfile(string playerId)
        {
            return EnsurePlayerProfile(playerId);
        }

        /// <summary>
        /// Gets current market prices for all traits
        /// </summary>
        public Dictionary<string, float> GetCurrentPrices()
        {
            return priceDatabase.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.currentPrice);
        }

        /// <summary>
        /// Gets player's active listings
        /// </summary>
        public MarketListing[] GetPlayerListings(string playerId)
        {
            return activeListings.Values
                .Where(l => l.sellerId == playerId && l.status == ListingStatus.Active)
                .OrderByDescending(l => l.listingTime)
                .ToArray();
        }

        /// <summary>
        /// Cancels a listing
        /// </summary>
        public bool CancelListing(string listingId, string playerId)
        {
            if (!activeListings.ContainsKey(listingId)) return false;

            var listing = activeListings[listingId];
            if (listing.sellerId != playerId) return false;

            listing.status = ListingStatus.Cancelled;
            activeListings.Remove(listingId);

            var playerProfile = EnsurePlayerProfile(playerId);
            playerProfile.activeListings--;

            return true;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Represents a marketplace listing
    /// </summary>
    [Serializable]
    public struct MarketListing
    {
        public string id;
        public string sellerId;
        public ListingType listingType;
        public string title;
        public string description;
        public float price;
        public float marketSuggestedPrice;
        public CurrencyType currency;
        public float listingTime;
        public float expirationTime;
        public ListingStatus status;
        public bool featured;

        // Type-specific data
        public CreatureMarketData creatureData;
        public GeneticSample geneticSample;
        public BreedingServiceData serviceData;
        public KnowledgeData knowledgeData;
    }

    /// <summary>
    /// Types of marketplace listings
    /// </summary>
    public enum ListingType
    {
        Creature,
        GeneticMaterial,
        BreedingService,
        Knowledge
    }

    /// <summary>
    /// Status of marketplace listings
    /// </summary>
    public enum ListingStatus
    {
        Active,
        Sold,
        Expired,
        Cancelled
    }

    /// <summary>
    /// Currency types in the marketplace
    /// </summary>
    public enum CurrencyType
    {
        GeneticCredits,
        ResearchPoints,
        ConservationTokens
    }

    /// <summary>
    /// Market data for creatures
    /// </summary>
    [Serializable]
    public struct CreatureMarketData
    {
        public GeneticProfile geneticProfile;
        public int generation;
        public float rarityScore;
        public float healthStatus;
        public int ageInDays;
        public List<string> breedingHistory;
    }

    /// <summary>
    /// Genetic sample for trading
    /// </summary>
    [Serializable]
    public struct GeneticSample
    {
        public string traitName;
        public float purity;
        public float viability;
        public float extractionDate;
        public string sourceCreatureId;
    }

    /// <summary>
    /// Breeding service data
    /// </summary>
    [Serializable]
    public struct BreedingServiceData
    {
        public string serviceType;
        public string[] guaranteedTraits;
        public float successRate;
        public int durationDays;
        public float providerReputation;
    }

    /// <summary>
    /// Knowledge item for trading
    /// </summary>
    [Serializable]
    public struct KnowledgeData
    {
        public string title;
        public string knowledgeType;
        public float completeness;
        public float rarity;
        public int researchHours;
        public string verification;
    }

    /// <summary>
    /// Transaction record
    /// </summary>
    [Serializable]
    public struct TransactionRecord
    {
        public string id;
        public string buyerId;
        public string sellerId;
        public string listingId;
        public ListingType listingType;
        public float price;
        public CurrencyType currency;
        public float transactionTime;
        public float fee;
        public object itemData;
    }

    /// <summary>
    /// Player economic profile
    /// </summary>
    [Serializable]
    public struct PlayerEconomicProfile
    {
        public string playerId;
        public float balance;
        public float researchPoints;
        public int transactionCount;
        public float totalSpent;
        public float totalEarned;
        public float reputation;
        public int activeListings;
        public float joinDate;

        public string economicRank => reputation switch
        {
            >= 1000f => "Market Titan",
            >= 500f => "Trade Baron",
            >= 200f => "Merchant",
            >= 100f => "Trader",
            >= 50f => "Vendor",
            _ => "Novice"
        };
    }

    /// <summary>
    /// Market search criteria
    /// </summary>
    [Serializable]
    public struct MarketSearchCriteria
    {
        public ListingType? listingType;
        public float? minPrice;
        public float? maxPrice;
        public string searchTerm;
        public string[] requiredTraits;
        public MarketSortBy sortBy;
        public int maxResults;
    }

    /// <summary>
    /// Market sorting options
    /// </summary>
    public enum MarketSortBy
    {
        DateNewest,
        DateOldest,
        PriceAscending,
        PriceDescending,
        Relevance
    }

    /// <summary>
    /// Economic event affecting market
    /// </summary>
    [Serializable]
    public struct EconomicEvent
    {
        public string id;
        public EconomicEventType type;
        public float startTime;
        public float endTime;
        public float duration;
        public float intensity;
        public string[] affectedTraits;
        public string description;
    }

    /// <summary>
    /// Types of economic events
    /// </summary>
    public enum EconomicEventType
    {
        GeneticBoom,
        MarketCrash,
        RarityDiscovery,
        SupplyShortage,
        DemandSpike
    }

    /// <summary>
    /// Market price data
    /// </summary>
    [Serializable]
    public struct MarketPriceData
    {
        public string itemId;
        public float currentPrice;
        public float basePrice;
        public List<PricePoint> historicalPrices;
        public int volume;
        public float lastUpdate;
    }

    /// <summary>
    /// Price point in history
    /// </summary>
    [Serializable]
    public struct PricePoint
    {
        public float price;
        public float timestamp;
        public int volume;
    }

    /// <summary>
    /// Price history data
    /// </summary>
    [Serializable]
    public struct PriceHistoryData
    {
        public string traitName;
        public float currentPrice;
        public PricePoint[] pricePoints;
        public float averagePrice;
        public float priceChange;
        public int volume;
    }

    /// <summary>
    /// Market trend analysis
    /// </summary>
    [Serializable]
    public struct TrendAnalysis
    {
        public string trait;
        public TrendDirection direction;
        public float strength;
        public float significance;
        public int volume;
        public float priceChange;
    }

    /// <summary>
    /// Trend direction
    /// </summary>
    public enum TrendDirection
    {
        Rising,
        Falling,
        Stable
    }

    /// <summary>
    /// Market alert
    /// </summary>
    [Serializable]
    public struct MarketAlert
    {
        public string id;
        public MarketAlertType type;
        public string message;
        public float timestamp;
        public AlertSeverity severity;
        public List<string> affectedItems;
    }

    /// <summary>
    /// Types of market alerts
    /// </summary>
    public enum MarketAlertType
    {
        PriceTrend,
        EconomicEvent,
        ListingExpiring,
        TransactionCompleted
    }

    /// <summary>
    /// Alert severity levels
    /// </summary>
    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Comprehensive market intelligence report
    /// </summary>
    [Serializable]
    public struct MarketIntelligenceReport
    {
        public float globalMarketHealth;
        public int activeListings;
        public float totalTransactionVolume;
        public string[] trendingTraits;
        public MarketAlert[] marketAlerts;
        public EconomicEvent[] economicEvents;
        public Dictionary<string, float> priceMovers;
        public Dictionary<string, float> demandIndex;
    }

    #endregion
}