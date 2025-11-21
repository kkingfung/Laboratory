using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Chimera.Genetics;
using Laboratory.Subsystems.Genetics;

namespace Laboratory.Subsystems.Trading
{
    /// <summary>
    /// Trading & Economy Subsystem Manager for Project Chimera.
    /// Handles genetic marketplace, trait trading, research consortium funding,
    /// and educational licensing while maintaining non-pay-to-win integrity.
    /// </summary>
    public class TradingSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        [Header("Configuration")]
        [SerializeField] private TradingSubsystemConfig config;

        [Header("Services")]
        [SerializeField] private bool enableGeneticMarketplace = true;
        [SerializeField] private bool enableResearchConsortiums = true;
        [SerializeField] private bool enableEducationalLicensing = true;
        [SerializeField] private bool enableResourceTrading = true;

        // Public Properties
        public bool IsInitialized { get; private set; }
        public string SubsystemName => "Trading";
        public float InitializationProgress { get; private set; }

        // Services
        public IGeneticMarketplaceService GeneticMarketplaceService { get; private set; }
        public IResearchConsortiumService ResearchConsortiumService { get; private set; }
        public IEducationalLicensingService EducationalLicensingService { get; private set; }
        public IEconomyManagementService EconomyManagementService { get; private set; }

        // Events
        public static event Action<TradingEvent> OnTradeCompleted;
        public static event Action<TradingEvent> OnMarketplaceEvent;
        public static event Action<ConsortiumEvent> OnConsortiumEvent;
        public static event Action<EconomyEvent> OnEconomyEvent;

        private readonly Dictionary<string, PlayerWallet> _playerWallets = new();
        private readonly Dictionary<string, TradeOffer> _activeOffers = new();
        private readonly Dictionary<string, ResearchConsortium> _activeConsortiums = new();
        private readonly Queue<TradeTransaction> _pendingTransactions = new();
        private readonly EconomyMetrics _economyMetrics = new();

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateConfiguration();
            InitializeComponents();
        }

        private void Start()
        {
            _ = InitializeAsync();
        }

        private void Update()
        {
            ProcessPendingTransactions();
            UpdateEconomyMetrics();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void ValidateConfiguration()
        {
            if (config == null)
            {
                Debug.LogError($"[{SubsystemName}] Configuration is missing! Please assign a TradingSubsystemConfig.");
                return;
            }

            if (config.currencyTypes == null || config.currencyTypes.Count == 0)
            {
                Debug.LogWarning($"[{SubsystemName}] No currency types configured. Trading will be limited.");
            }

            if (config.tradeableItems == null || config.tradeableItems.Count == 0)
            {
                Debug.LogWarning($"[{SubsystemName}] No tradeable items configured. Marketplace will be empty.");
            }
        }

        private void InitializeComponents()
        {
            InitializationProgress = 0.2f;

            // Try to resolve services from service container
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                serviceContainer.TryResolve<IGeneticMarketplaceService>(out var geneticMarketplaceService);
                serviceContainer.TryResolve<IResearchConsortiumService>(out var researchConsortiumService);
                serviceContainer.TryResolve<IEducationalLicensingService>(out var educationalLicensingService);
                serviceContainer.TryResolve<IEconomyManagementService>(out var economyManagementService);

                GeneticMarketplaceService = geneticMarketplaceService;
                ResearchConsortiumService = researchConsortiumService;
                EducationalLicensingService = educationalLicensingService;
                EconomyManagementService = economyManagementService;
            }

            if (config.enableDebugLogging)
            {
                Debug.Log("[TradingSubsystem] Trading services resolved from service container");
                Debug.Log($"  GeneticMarketplace: {(GeneticMarketplaceService != null ? "Available" : "Not Available")}");
                Debug.Log($"  ResearchConsortium: {(ResearchConsortiumService != null ? "Available" : "Not Available")}");
                Debug.Log($"  EducationalLicensing: {(EducationalLicensingService != null ? "Available" : "Not Available")}");
                Debug.Log($"  EconomyManagement: {(EconomyManagementService != null ? "Available" : "Not Available")}");
            }

            InitializationProgress = 0.4f;
        }

        private async Task InitializeAsync()
        {
            try
            {
                InitializationProgress = 0.5f;

                // Initialize genetic marketplace
                if (enableGeneticMarketplace)
                {
                    await GeneticMarketplaceService.InitializeAsync();
                }
                InitializationProgress = 0.6f;

                // Initialize research consortiums
                if (enableResearchConsortiums)
                {
                    await ResearchConsortiumService.InitializeAsync();
                }
                InitializationProgress = 0.7f;

                // Initialize educational licensing
                if (enableEducationalLicensing)
                {
                    await EducationalLicensingService.InitializeAsync();
                }
                InitializationProgress = 0.8f;

                // Initialize economy management
                await EconomyManagementService.InitializeAsync();
                InitializationProgress = 0.9f;

                // Subscribe to game events
                SubscribeToGameEvents();

                // Register services
                RegisterServices();

                // Load existing economy data
                await LoadEconomyData();

                // Start background trading processing
                _ = StartTradingProcessingLoop();

                IsInitialized = true;
                InitializationProgress = 1.0f;

                Debug.Log($"[{SubsystemName}] Initialization complete. " +
                         $"Marketplace: {enableGeneticMarketplace}, " +
                         $"Consortiums: {enableResearchConsortiums}, " +
                         $"Educational: {enableEducationalLicensing}, " +
                         $"Resources: {enableResourceTrading}");

                // Notify system initialization
                EventBus.Publish(new SubsystemInitializedEvent(SubsystemName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Initialization failed: {ex.Message}");
                InitializationProgress = 0f;
            }
        }

        private void SubscribeToGameEvents()
        {
            // Subscribe to genetics events for automatic genetic value assessment
            GeneticsSubsystemManager.OnBreedingComplete += HandleBreedingComplete;
            GeneticsSubsystemManager.OnTraitDiscovered += HandleTraitDiscovered;

            // Subscribe to research events for consortium collaboration
            Laboratory.Subsystems.Research.ResearchSubsystemManager.OnPublicationCreated += HandlePublicationCreated;

            // Subscribe to analytics events for economy balancing
            Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnDiscoveryTracked += HandleDiscoveryTracked;
        }

        private void RegisterServices()
        {
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.Register<IGeneticMarketplaceService>(GeneticMarketplaceService);
                ServiceContainer.Instance.Register<IResearchConsortiumService>(ResearchConsortiumService);
                ServiceContainer.Instance.Register<IEducationalLicensingService>(EducationalLicensingService);
                ServiceContainer.Instance.Register<IEconomyManagementService>(EconomyManagementService);
                ServiceContainer.Instance.Register<TradingSubsystemManager>(this);
            }
        }

        private async Task LoadEconomyData()
        {
            // In a real implementation, this would load from persistent storage
            await Task.CompletedTask;
            Debug.Log($"[{SubsystemName}] Economy data loaded");
        }

        #endregion

        #region Core Trading Operations

        /// <summary>
        /// Creates a trade offer for genetic material or research data
        /// </summary>
        public async Task<TradeOffer> CreateTradeOfferAsync(string sellerId, TradeOfferRequest request)
        {
            if (!IsInitialized)
                return null;

            // Validate trade offer
            if (!ValidateTradeOffer(request, sellerId))
                return null;

            var offer = await CreateOfferFromRequestAsync(sellerId, request);

            if (offer != null)
            {
                _activeOffers[offer.offerId] = offer;

                // Fire marketplace event
                var marketplaceEvent = new TradingEvent
                {
                    eventType = TradingEventType.TradeCreated,
                    tradeId = offer.offerId,
                    timestamp = DateTime.Now
                };

                OnMarketplaceEvent?.Invoke(marketplaceEvent);

                Debug.Log($"[{SubsystemName}] Trade offer created: {offer.offerId}");
            }

            return offer;
        }

        /// <summary>
        /// Accepts a trade offer
        /// </summary>
        public async Task<bool> AcceptTradeOfferAsync(string buyerId, string offerId)
        {
            if (!_activeOffers.TryGetValue(offerId, out var offer))
            {
                Debug.LogWarning($"[{SubsystemName}] Trade offer not found: {offerId}");
                return false;
            }

            // Validate buyer can afford the trade
            if (!CanAffordTrade(buyerId, offer))
            {
                Debug.LogWarning($"[{SubsystemName}] Player {buyerId} cannot afford trade {offerId}");
                return false;
            }

            return await ExecuteTradeAsync(offer, buyerId);
        }

        /// <summary>
        /// Creates or joins a research consortium
        /// </summary>
        public async Task<ResearchConsortium> CreateResearchConsortiumAsync(string founderId, ConsortiumRequest request)
        {
            if (!enableResearchConsortiums)
                return null;

            return await ResearchConsortiumService?.CreateConsortiumAsync(request.requestId, request.description);
        }

        /// <summary>
        /// Joins an existing research consortium
        /// </summary>
        public async Task<bool> JoinResearchConsortiumAsync(string playerId, string consortiumId)
        {
            if (!enableResearchConsortiums)
                return false;

            return await ResearchConsortiumService.JoinConsortiumAsync(consortiumId, playerId);
        }

        /// <summary>
        /// Gets player's wallet information
        /// </summary>
        public PlayerWallet GetPlayerWallet(string playerId)
        {
            if (!_playerWallets.TryGetValue(playerId, out var wallet))
            {
                wallet = CreateDefaultWallet(playerId);
                _playerWallets[playerId] = wallet;
            }
            return wallet;
        }

        /// <summary>
        /// Awards currency to a player
        /// </summary>
        public bool AwardCurrency(string playerId, CurrencyType currencyType, long amount, string reason = null)
        {
            var wallet = GetPlayerWallet(playerId);

            if (!wallet.currencies.ContainsKey(currencyType))
                wallet.currencies[currencyType] = 0;

            wallet.currencies[currencyType] += amount;

            // Record transaction
            var transaction = new WalletTransaction
            {
                transactionId = Guid.NewGuid().ToString(),
                currency = currencyType,
                amount = amount,
                transactionType = TransactionType.SystemAward,
                timestamp = DateTime.Now
            };

            wallet.transactionHistory.Add(transaction);

            Debug.Log($"[{SubsystemName}] Awarded {amount} {currencyType} to {playerId}: {reason}");
            return true;
        }

        /// <summary>
        /// Gets active marketplace offers
        /// </summary>
        public List<TradeOffer> GetMarketplaceOffers(MarketplaceFilter filter = null)
        {
            var offers = new List<TradeOffer>(_activeOffers.Values);

            if (filter != null)
            {
                offers = FilterMarketplaceOffers(offers, filter);
            }

            return offers;
        }

        /// <summary>
        /// Gets active research consortiums
        /// </summary>
        public List<ResearchConsortium> GetActiveConsortiums()
        {
            return new List<ResearchConsortium>(_activeConsortiums.Values);
        }

        /// <summary>
        /// Calculates the market value of genetic material
        /// </summary>
        public int CalculateGeneticValue(GeneticProfile geneticProfile)
        {
            // Basic genetic value calculation
            // In a full implementation, this would consider rarity, traits, etc.
            if (geneticProfile == null)
                return 0;

            // Simple calculation based on genetic complexity and rarity
            int baseValue = 100;
            // Add value based on genetic traits if available
            // This is a placeholder implementation
            return baseValue + UnityEngine.Random.Range(0, 500);
        }

        #endregion

        #region Transaction Processing

        private void ProcessPendingTransactions()
        {
            const int maxTransactionsPerFrame = 5;
            int processedCount = 0;

            while (_pendingTransactions.Count > 0 && processedCount < maxTransactionsPerFrame)
            {
                var transaction = _pendingTransactions.Dequeue();
                ProcessTradeTransaction(transaction);
                processedCount++;
            }
        }

        private void ProcessTradeTransaction(TradeTransaction transaction)
        {
            try
            {
                switch (transaction.transactionType)
                {
                    case TradeTransactionType.GeneticTrade:
                        ProcessGeneticTrade(transaction);
                        break;

                    case TradeTransactionType.ResearchCollaboration:
                        ProcessResearchCollaboration(transaction);
                        break;

                    case TradeTransactionType.ResourceTrade:
                        ProcessResourceTrade(transaction);
                        break;

                    case TradeTransactionType.ConsortiumContribution:
                        ProcessConsortiumContribution(transaction);
                        break;
                }

                // Fire trade completed event
                var tradeEvent = new TradingEvent
                {
                    eventType = TradingEventType.TradeCompleted,
                    tradeId = transaction.transactionId,
                    timestamp = DateTime.Now
                };

                OnTradeCompleted?.Invoke(tradeEvent);

                // Update economy metrics
                UpdateTransactionMetrics(transaction);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Failed to process transaction {transaction.transactionId}: {ex.Message}");
                transaction.status = TransactionStatus.Failed;
            }
        }

        private void ProcessGeneticTrade(TradeTransaction transaction)
        {
            // Transfer genetic material between players
            // This would integrate with the genetics system
            transaction.status = TransactionStatus.Completed;
            Debug.Log($"[{SubsystemName}] Genetic trade completed: {transaction.transactionId}");
        }

        private void ProcessResearchCollaboration(TradeTransaction transaction)
        {
            // Process research collaboration payment
            transaction.status = TransactionStatus.Completed;
            Debug.Log($"[{SubsystemName}] Research collaboration processed: {transaction.transactionId}");
        }

        private void ProcessResourceTrade(TradeTransaction transaction)
        {
            // Process resource trading
            transaction.status = TransactionStatus.Completed;
            Debug.Log($"[{SubsystemName}] Resource trade completed: {transaction.transactionId}");
        }

        private void ProcessConsortiumContribution(TradeTransaction transaction)
        {
            // Process consortium contribution
            transaction.status = TransactionStatus.Completed;
            Debug.Log($"[{SubsystemName}] Consortium contribution processed: {transaction.transactionId}");

            // Fire consortium event
            var consortiumEvent = new ConsortiumEvent
            {
                consortiumId = transaction.transactionId,
                eventType = "ContributionProcessed",
                timestamp = DateTime.Now,
                data = transaction.tradeData
            };
            OnConsortiumEvent?.Invoke(consortiumEvent);
        }

        private Task<bool> ExecuteTradeAsync(TradeOffer offer, string buyerId)
        {
            var transaction = new TradeTransaction
            {
                transactionId = Guid.NewGuid().ToString(),
                sellerId = offer.sellerId,
                buyerId = buyerId,
                transactionType = DetermineTransactionType(offer),
                status = TransactionStatus.Pending,
                createdTime = DateTime.Now,
                tradeData = new Dictionary<string, object>
                {
                    ["offer"] = offer,
                    ["offerId"] = offer.offerId
                }
            };

            // Deduct payment from buyer
            if (!DeductPayment(buyerId, offer.requestedCurrency))
            {
                return Task.FromResult(false);
            }

            // Add payment to seller
            if (!AddPayment(offer.sellerId, offer.requestedCurrency))
            {
                // Refund buyer if seller payment fails
                AddPayment(buyerId, offer.requestedCurrency);
                return Task.FromResult(false);
            }

            // Queue transaction for processing
            _pendingTransactions.Enqueue(transaction);

            // Remove offer from marketplace
            _activeOffers.Remove(offer.offerId);

            return Task.FromResult(true);
        }

        #endregion

        #region Economy Management

        private void UpdateEconomyMetrics()
        {
            // Update every 10 seconds
            if (Time.unscaledTime % 10f < Time.unscaledDeltaTime)
            {
                _economyMetrics.totalPlayers = _playerWallets.Count;
                _economyMetrics.activeOffers = _activeOffers.Count;
                _economyMetrics.totalTransactions = CalculateTotalTransactions();
                _economyMetrics.averageWealthPerPlayer = CalculateAverageWealth();
                _economyMetrics.currencyInflation = CalculateCurrencyInflation();

                // Fire economy metrics update event
                var economyEvent = new EconomyEvent
                {
                    eventType = "MetricsUpdated",
                    timestamp = DateTime.Now,
                    metrics = _economyMetrics
                };
                OnEconomyEvent?.Invoke(economyEvent);
            }
        }

        private void UpdateTransactionMetrics(TradeTransaction transaction)
        {
            _economyMetrics.totalTransactionValue += GetTransactionValue(transaction);

            if (transaction.status == TransactionStatus.Completed)
            {
                _economyMetrics.successfulTransactions++;
            }
            else
            {
                _economyMetrics.failedTransactions++;
            }
        }

        private int CalculateTotalTransactions()
        {
            int total = 0;
            foreach (var wallet in _playerWallets.Values)
            {
                total += wallet.transactionHistory.Count;
            }
            return total;
        }

        private float CalculateAverageWealth()
        {
            if (_playerWallets.Count == 0)
                return 0f;

            float totalWealth = 0f;
            foreach (var wallet in _playerWallets.Values)
            {
                totalWealth += CalculateWalletValue(wallet);
            }

            return totalWealth / _playerWallets.Count;
        }

        private float CalculateCurrencyInflation()
        {
            // Simple inflation calculation based on currency supply
            // In a real implementation, this would be more sophisticated
            return 0.02f; // 2% placeholder
        }

        private float CalculateWalletValue(PlayerWallet wallet)
        {
            float value = 0f;
            foreach (var currency in wallet.currencies)
            {
                value += currency.Value * GetCurrencyExchangeRate(currency.Key);
            }
            return value;
        }

        private float GetCurrencyExchangeRate(CurrencyType currencyType)
        {
            return currencyType switch
            {
                CurrencyType.ResearchPoints => 1f,
                CurrencyType.DiscoveryTokens => 1.5f,
                CurrencyType.CommunityCredits => 2f,
                CurrencyType.EducationalPoints => 0.8f,
                _ => 1f
            };
        }

        private float GetTransactionValue(TradeTransaction transaction)
        {
            if (transaction.tradeData.TryGetValue("offer", out var offerObj) && offerObj is TradeOffer offer)
            {
                return offer.requestedCurrency.amount * GetCurrencyExchangeRate(offer.requestedCurrency.currencyType);
            }
            return 0f;
        }

        #endregion

        #region Event Handlers

        private void HandleBreedingComplete(GeneticBreedingResult result)
        {
            if (result?.offspring != null && result.isSuccessful)
            {
                // Award currency for successful breeding
                var value = CalculateGeneticValue(result.offspring);
                var award = Mathf.RoundToInt(value * 1.5f); // Default breeding success multiplier

                AwardCurrency("CurrentPlayer", CurrencyType.DiscoveryTokens, award, "Successful breeding");
            }
        }

        private void HandleTraitDiscovered(TraitDiscoveryEvent discoveryEvent)
        {
            // Award currency for trait discovery
            var baseAward = 100; // Default trait discovery reward
            if (discoveryEvent.isWorldFirst)
                baseAward *= 3; // Default world first multiplier

            AwardCurrency("CurrentPlayer", CurrencyType.ResearchPoints, baseAward, $"Trait discovery: {discoveryEvent.traitName}");
        }

        private void HandlePublicationCreated(Laboratory.Subsystems.Research.PublicationEvent publicationEvent)
        {
            // Award currency for research publication
            var award = 200; // Default publication reward
            if (publicationEvent.publication.coAuthors.Count > 0)
                award += 50; // Default collaboration bonus

            AwardCurrency(publicationEvent.publication.authorId, CurrencyType.ResearchPoints, award, "Research publication");
        }

        private void HandleDiscoveryTracked(Laboratory.Subsystems.Analytics.DiscoveryAnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent.isWorldFirst)
            {
                // Extra reward for world-first discoveries
                var award = Mathf.RoundToInt(3 * 100); // worldFirstMultiplier * traitDiscoveryReward
                AwardCurrency("CurrentPlayer", CurrencyType.DiscoveryTokens, award, $"World first: {analyticsEvent.discoveredItem}");
            }
        }

        #endregion

        #region Helper Methods

        private bool ValidateTradeOffer(TradeOfferRequest request, string sellerId)
        {
            // Validate player owns the item being traded
            if (request.offeredItem.itemType == TradeableItemType.GeneticMaterial)
            {
                // Would need to check genetics system for ownership
                return true; // Placeholder
            }

            // Validate pricing is reasonable
            if (request.requestedCurrency.amount <= 0)
            {
                Debug.LogWarning($"[{SubsystemName}] Invalid price for trade offer");
                return false;
            }

            // Check for prohibited items
            // No prohibited items list configured, allow all trades
            // In a full implementation, this would check against a prohibited items list

            return true;
        }

        private bool CanAffordTrade(string playerId, TradeOffer offer)
        {
            var wallet = GetPlayerWallet(playerId);
            return wallet.currencies.GetValueOrDefault(offer.requestedCurrency.currencyType, 0) >= offer.requestedCurrency.amount;
        }

        private bool DeductPayment(string playerId, CurrencyAmount payment)
        {
            var wallet = GetPlayerWallet(playerId);

            if (!CanAffordPayment(wallet, payment))
                return false;

            wallet.currencies[payment.currencyType] -= payment.amount;

            // Record transaction
            var transaction = new WalletTransaction
            {
                transactionId = Guid.NewGuid().ToString(),
                currency = payment.currencyType,
                amount = -payment.amount,
                transactionType = TransactionType.TradePayment,
                timestamp = DateTime.Now
            };

            wallet.transactionHistory.Add(transaction);
            return true;
        }

        private bool AddPayment(string playerId, CurrencyAmount payment)
        {
            var wallet = GetPlayerWallet(playerId);

            if (!wallet.currencies.ContainsKey(payment.currencyType))
                wallet.currencies[payment.currencyType] = 0;

            wallet.currencies[payment.currencyType] += payment.amount;

            // Record transaction
            var transaction = new WalletTransaction
            {
                transactionId = Guid.NewGuid().ToString(),
                currency = payment.currencyType,
                amount = payment.amount,
                transactionType = TransactionType.TradeReceived,
                timestamp = DateTime.Now
            };

            wallet.transactionHistory.Add(transaction);
            return true;
        }

        private bool CanAffordPayment(PlayerWallet wallet, CurrencyAmount payment)
        {
            return wallet.currencies.GetValueOrDefault(payment.currencyType, 0) >= payment.amount;
        }

        private PlayerWallet CreateDefaultWallet(string playerId)
        {
            var wallet = new PlayerWallet
            {
                lastUpdate = DateTime.Now,
                currencies = new Dictionary<CurrencyType, long>(),
                transactionHistory = new List<WalletTransaction>()
            };

            // Give starting currency
            foreach (var startingCurrency in config.startingCurrencies)
            {
                wallet.currencies[startingCurrency.currencyType] = startingCurrency.amount;
            }

            return wallet;
        }

        private TradeTransactionType DetermineTransactionType(TradeOffer offer)
        {
            return offer.offeredItem.itemType switch
            {
                TradeableItemType.GeneticMaterial => TradeTransactionType.GeneticTrade,
                TradeableItemType.ResearchData => TradeTransactionType.ResearchCollaboration,
                TradeableItemType.Resource => TradeTransactionType.ResourceTrade,
                _ => TradeTransactionType.GeneticTrade
            };
        }

        private List<TradeOffer> FilterMarketplaceOffers(List<TradeOffer> offers, MarketplaceFilter filter)
        {
            var filtered = new List<TradeOffer>();

            foreach (var offer in offers)
            {
                if (MatchesFilter(offer, filter))
                    filtered.Add(offer);
            }

            return filtered;
        }

        private bool MatchesFilter(TradeOffer offer, MarketplaceFilter filter)
        {
            if (filter.itemTypes.Count > 0 && !filter.itemTypes.Contains(offer.offeredItem.itemType))
                return false;

            if (filter.maxPrice != null && offer.requestedCurrency.amount > filter.maxPrice.amount)
                return false;

            if (!string.IsNullOrEmpty(filter.searchTerm) &&
                !offer.offeredItem.itemName.ToLower().Contains(filter.searchTerm.ToLower()) &&
                !offer.description.ToLower().Contains(filter.searchTerm.ToLower()))
                return false;

            return true;
        }

        #endregion

        #region Background Processing

        private async Task StartTradingProcessingLoop()
        {
            while (IsInitialized)
            {
                try
                {
                    await Task.Delay(config.backgroundProcessingIntervalMs);

                    // Monitor economy health
                    await EconomyManagementService?.MonitorEconomyHealthAsync();

                    // Clean up expired offers
                    CleanupExpiredOffers();

                    // Process inflation adjustments (if enabled)
                    await EconomyManagementService?.ProcessInflationAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{SubsystemName}] Background processing error: {ex.Message}");
                }
            }
        }

        private void CleanupExpiredOffers()
        {
            var expiredOffers = new List<string>();
            var now = DateTime.Now;

            foreach (var kvp in _activeOffers)
            {
                if (kvp.Value.expiryTime <= now)
                {
                    expiredOffers.Add(kvp.Key);
                }
            }

            foreach (var offerId in expiredOffers)
            {
                _activeOffers.Remove(offerId);
                Debug.Log($"[{SubsystemName}] Removed expired offer: {offerId}");
            }
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            // Unsubscribe from events
            GeneticsSubsystemManager.OnBreedingComplete -= HandleBreedingComplete;
            GeneticsSubsystemManager.OnTraitDiscovered -= HandleTraitDiscovered;

            Laboratory.Subsystems.Research.ResearchSubsystemManager.OnPublicationCreated -= HandlePublicationCreated;

            Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnDiscoveryTracked -= HandleDiscoveryTracked;

            // Clear collections
            _playerWallets.Clear();
            _activeOffers.Clear();
            _activeConsortiums.Clear();
            _pendingTransactions.Clear();

            Debug.Log($"[{SubsystemName}] Cleanup complete");
        }

        private async Task<TradeOffer> CreateOfferFromRequestAsync(string sellerId, TradeOfferRequest request)
        {
            // Create a trade offer from the request since the service isn't available
            await Task.CompletedTask; // Simulate async operation

            return new TradeOffer
            {
                offerId = Guid.NewGuid().ToString(),
                sellerId = sellerId,
                offerType = TradeOfferType.DirectSale,
                status = TradeOfferStatus.Active,
                createdTime = DateTime.Now,
                expiryTime = DateTime.Now.AddHours(config.defaultTradeExpiryHours),
                offeredItem = request.offeredItem,
                requestedItem = request.requestedItem,
                offeredCurrency = request.offeredCurrency,
                requestedCurrency = request.requestedCurrency,
                conditions = request.conditions,
                isPublic = request.isPublic
            };
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Test Trade Creation")]
        private void TestTradeCreation()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Trading subsystem not initialized");
                return;
            }

            var request = new TradeOfferRequest
            {
                description = "A test trade for debugging",
                offeredItem = new TradeableItem
                {
                    itemType = TradeableItemType.GeneticMaterial,
                    itemId = "TestTrait",
                    itemName = "Test Genetic Trait",
                    quantity = 1
                },
                requestedCurrency = new CurrencyAmount
                {
                    currencyType = CurrencyType.DiscoveryTokens,
                    amount = 100
                },
                expiryTime = DateTime.Now.AddHours(24)
            };

            _ = CreateTradeOfferAsync("TestPlayer", request);
        }

        [ContextMenu("Test Currency Award")]
        private void TestCurrencyAward()
        {
            AwardCurrency("TestPlayer", CurrencyType.ResearchPoints, 50, "Debug test");
        }

        [ContextMenu("Print Economy Metrics")]
        private void PrintEconomyMetrics()
        {
            Debug.Log($"Economy Metrics:\n" +
                     $"Total Players: {_economyMetrics.totalPlayers}\n" +
                     $"Active Offers: {_economyMetrics.activeOffers}\n" +
                     $"Total Transactions: {_economyMetrics.totalTransactions}\n" +
                     $"Average Wealth: {_economyMetrics.averageWealthPerPlayer:F1}\n" +
                     $"Currency Inflation: {_economyMetrics.currencyInflation:P2}");
        }

        #endregion
    }
}