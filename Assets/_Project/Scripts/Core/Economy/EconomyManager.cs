using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.MonsterTown;
using Laboratory.Core.Equipment;
using Laboratory.Core.Events;

namespace Laboratory.Core.Economy
{
    /// <summary>
    /// Economy Manager - Handles all economic systems for Monster Town
    ///
    /// Key Features:
    /// - Multi-currency system (Coins, Gems, Activity Tokens, Genetic Samples)
    /// - Dynamic market pricing based on supply/demand
    /// - Trading between players and NPCs
    /// - Equipment marketplace with rarity-based pricing
    /// - Monster trading and breeding contracts
    /// - Economic progression tied to town development
    /// - Real-time price fluctuations for engaging gameplay
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        [Header("💰 Currency Configuration")]
        [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private CurrencyExchangeRates exchangeRates;
        [SerializeField] private bool enableDynamicPricing = true;
        [SerializeField] private float priceUpdateFrequency = 30f;

        [Header("🏪 Marketplace Settings")]
        [SerializeField] private int maxMarketplaceListings = 100;
        [SerializeField] private float listingFeePercentage = 0.05f;
        [SerializeField] private float marketplaceTaxRate = 0.02f;
        [SerializeField] private bool enablePlayerTrading = true;

        [Header("📈 Economy Simulation")]
        [SerializeField] private bool enableSupplyDemandSimulation = true;
        [SerializeField] private float marketVolatility = 0.1f;
        [SerializeField] private int simulatedTraders = 50;

        // Economy state
        private TownResources _globalEconomy;
        private Dictionary<string, PlayerWallet> _playerWallets = new();
        private Dictionary<CurrencyType, float> _currentExchangeRates = new();
        private Dictionary<string, MarketListing> _activeListings = new();
        private Dictionary<string, PriceHistory> _priceHistories = new();
        private List<EconomicTransaction> _transactionHistory = new();

        // Market simulation
        private Dictionary<string, MarketDemand> _marketDemand = new();
        private Dictionary<string, MarketSupply> _marketSupply = new();
        private Queue<MarketEvent> _pendingMarketEvents = new();

        // Events
        public event Action<CurrencyType, float> OnExchangeRateChanged;
        public event Action<EconomicTransaction> OnTransactionCompleted;
        public event Action<MarketListing> OnItemListed;
        public event Action<MarketListing> OnItemSold;
        public event Action<PlayerWallet> OnWalletUpdated;

        #region Initialization

        public void InitializeEconomy(EconomyConfig config, TownResources startingEconomy)
        {
            economyConfig = config;
            _globalEconomy = startingEconomy;

            InitializeExchangeRates();
            InitializeMarketDemand();
            InitializePriceHistories();

            if (enableDynamicPricing)
            {
                InvokeRepeating(nameof(UpdateMarketPrices), priceUpdateFrequency, priceUpdateFrequency);
            }

            if (enableSupplyDemandSimulation)
            {
                InvokeRepeating(nameof(SimulateMarketActivity), 5f, 10f);
            }

            Debug.Log($"💰 Economy Manager initialized with {_globalEconomy.GetTotalValue()} total value");
        }

        private void InitializeExchangeRates()
        {
            _currentExchangeRates[CurrencyType.Coins] = 1f; // Base currency
            _currentExchangeRates[CurrencyType.Gems] = exchangeRates.coinsToGemsRate;
            _currentExchangeRates[CurrencyType.ActivityTokens] = exchangeRates.coinsToTokensRate;
            _currentExchangeRates[CurrencyType.GeneticSamples] = exchangeRates.coinsToSamplesRate;
            _currentExchangeRates[CurrencyType.Materials] = exchangeRates.coinsToMaterialsRate;
            _currentExchangeRates[CurrencyType.Energy] = exchangeRates.coinsToEnergyRate;
        }

        private void InitializeMarketDemand()
        {
            // Initialize demand for different item categories
            var itemCategories = new[]
            {
                "Equipment", "Monsters", "GeneticSamples", "Food", "Materials", "Buildings"
            };

            foreach (var category in itemCategories)
            {
                _marketDemand[category] = new MarketDemand
                {
                    baseLevel = 1f,
                    currentLevel = UnityEngine.Random.Range(0.7f, 1.3f),
                    trend = UnityEngine.Random.Range(-0.1f, 0.1f)
                };
            }
        }

        private void InitializePriceHistories()
        {
            // Create price history tracking for key items
            var trackedItems = new[]
            {
                "SpeedBoots", "CombatArmor", "ThinkingCap", "RhythmGloves",
                "CommonMonster", "RareMonster", "EpicMonster", "LegendaryMonster"
            };

            foreach (var item in trackedItems)
            {
                _priceHistories[item] = new PriceHistory();
            }
        }

        #endregion

        #region Player Wallet Management

        /// <summary>
        /// Create a new wallet for a player
        /// </summary>
        public PlayerWallet CreatePlayerWallet(string playerId, TownResources startingCurrency = null)
        {
            if (_playerWallets.ContainsKey(playerId))
            {
                Debug.LogWarning($"Wallet already exists for player {playerId}");
                return _playerWallets[playerId];
            }

            var wallet = new PlayerWallet
            {
                PlayerId = playerId,
                Currency = startingCurrency ?? economyConfig.startingPlayerCurrency,
                LastUpdateTime = DateTime.UtcNow,
                TransactionHistory = new List<string>()
            };

            _playerWallets[playerId] = wallet;
            OnWalletUpdated?.Invoke(wallet);

            Debug.Log($"💳 Created wallet for player {playerId} with {wallet.Currency.GetTotalValue()} total value");
            return wallet;
        }

        /// <summary>
        /// Get a player's wallet
        /// </summary>
        public PlayerWallet GetPlayerWallet(string playerId)
        {
            if (!_playerWallets.TryGetValue(playerId, out var wallet))
            {
                wallet = CreatePlayerWallet(playerId);
            }
            return wallet;
        }

        /// <summary>
        /// Add currency to a player's wallet
        /// </summary>
        public bool AddCurrencyToWallet(string playerId, TownResources amount, string reason = "")
        {
            var wallet = GetPlayerWallet(playerId);
            wallet.Currency += amount;
            wallet.LastUpdateTime = DateTime.UtcNow;

            // Record transaction
            var transaction = new EconomicTransaction
            {
                Id = Guid.NewGuid().ToString(),
                PlayerId = playerId,
                Type = TransactionType.Income,
                Amount = amount,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            };

            RecordTransaction(transaction);
            OnWalletUpdated?.Invoke(wallet);

            Debug.Log($"💰 Added {amount} to {playerId}'s wallet. Reason: {reason}");
            return true;
        }

        /// <summary>
        /// Deduct currency from a player's wallet
        /// </summary>
        public bool DeductCurrencyFromWallet(string playerId, TownResources amount, string reason = "")
        {
            var wallet = GetPlayerWallet(playerId);

            if (!wallet.Currency.CanAfford(amount))
            {
                Debug.LogWarning($"Player {playerId} cannot afford {amount}");
                return false;
            }

            wallet.Currency -= amount;
            wallet.LastUpdateTime = DateTime.UtcNow;

            // Record transaction
            var transaction = new EconomicTransaction
            {
                Id = Guid.NewGuid().ToString(),
                PlayerId = playerId,
                Type = TransactionType.Expense,
                Amount = amount,
                Reason = reason,
                Timestamp = DateTime.UtcNow
            };

            RecordTransaction(transaction);
            OnWalletUpdated?.Invoke(wallet);

            Debug.Log($"💸 Deducted {amount} from {playerId}'s wallet. Reason: {reason}");
            return true;
        }

        #endregion

        #region Currency Exchange

        /// <summary>
        /// Exchange one currency type for another
        /// </summary>
        public bool ExchangeCurrency(string playerId, CurrencyType fromCurrency, CurrencyType toCurrency, float amount)
        {
            var wallet = GetPlayerWallet(playerId);
            var exchangeRate = CalculateExchangeRate(fromCurrency, toCurrency);
            var exchangeAmount = amount * exchangeRate;

            // Check if player has enough source currency
            if (!HasSufficientCurrency(wallet, fromCurrency, amount))
            {
                Debug.LogWarning($"Player {playerId} has insufficient {fromCurrency} for exchange");
                return false;
            }

            // Apply exchange fee
            var fee = amount * economyConfig.exchangeFeePercentage;
            var netAmount = amount - fee;
            var receivedAmount = netAmount * exchangeRate;

            // Perform exchange
            DeductCurrencyFromWallet(playerId, CreateCurrencyAmount(fromCurrency, amount), $"Currency exchange: {fromCurrency} to {toCurrency}");
            AddCurrencyToWallet(playerId, CreateCurrencyAmount(toCurrency, receivedAmount), $"Currency exchange: {fromCurrency} to {toCurrency}");

            Debug.Log($"💱 {playerId} exchanged {amount} {fromCurrency} for {receivedAmount} {toCurrency} (rate: {exchangeRate:F4})");
            return true;
        }

        /// <summary>
        /// Calculate current exchange rate between two currencies
        /// </summary>
        public float CalculateExchangeRate(CurrencyType fromCurrency, CurrencyType toCurrency)
        {
            if (fromCurrency == toCurrency) return 1f;

            var fromRate = _currentExchangeRates[fromCurrency];
            var toRate = _currentExchangeRates[toCurrency];

            return fromRate / toRate;
        }

        /// <summary>
        /// Get current exchange rates for all currencies
        /// </summary>
        public Dictionary<CurrencyType, float> GetCurrentExchangeRates()
        {
            return new Dictionary<CurrencyType, float>(_currentExchangeRates);
        }

        #endregion

        #region Marketplace System

        /// <summary>
        /// List an item for sale in the marketplace
        /// </summary>
        public bool ListItemForSale(string sellerId, MarketItem item, TownResources price, int quantity = 1)
        {
            if (_activeListings.Count >= maxMarketplaceListings)
            {
                Debug.LogWarning("Marketplace is full, cannot list more items");
                return false;
            }

            // Calculate listing fee
            var listingFee = price * listingFeePercentage;
            if (!DeductCurrencyFromWallet(sellerId, listingFee, "Marketplace listing fee"))
            {
                return false;
            }

            var listing = new MarketListing
            {
                Id = Guid.NewGuid().ToString(),
                SellerId = sellerId,
                Item = item,
                Price = price,
                Quantity = quantity,
                ListedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _activeListings[listing.Id] = listing;
            OnItemListed?.Invoke(listing);

            Debug.Log($"🏪 {sellerId} listed {item.Name} for {price}");
            return true;
        }

        /// <summary>
        /// Purchase an item from the marketplace
        /// </summary>
        public bool PurchaseMarketItem(string buyerId, string listingId, int quantity = 1)
        {
            if (!_activeListings.TryGetValue(listingId, out var listing))
            {
                Debug.LogWarning($"Listing {listingId} not found");
                return false;
            }

            if (listing.Quantity < quantity)
            {
                Debug.LogWarning($"Insufficient quantity available. Requested: {quantity}, Available: {listing.Quantity}");
                return false;
            }

            var totalPrice = listing.Price * quantity;
            var tax = totalPrice * marketplaceTaxRate;
            var sellerReceives = totalPrice - tax;

            // Process purchase
            if (!DeductCurrencyFromWallet(buyerId, totalPrice, $"Marketplace purchase: {listing.Item.Name}"))
            {
                return false;
            }

            // Pay seller
            AddCurrencyToWallet(listing.SellerId, sellerReceives, $"Marketplace sale: {listing.Item.Name}");

            // Add tax to global economy
            _globalEconomy += tax;

            // Update listing
            listing.Quantity -= quantity;
            if (listing.Quantity <= 0)
            {
                _activeListings.Remove(listingId);
            }

            // Update market data
            UpdateMarketSupplyDemand(listing.Item.Category, false, quantity);

            OnItemSold?.Invoke(listing);

            Debug.Log($"🛒 {buyerId} purchased {quantity}x {listing.Item.Name} from {listing.SellerId} for {totalPrice}");
            return true;
        }

        /// <summary>
        /// Get all active marketplace listings
        /// </summary>
        public List<MarketListing> GetMarketplaceListings(string category = null, EquipmentRarity? rarity = null)
        {
            var listings = _activeListings.Values.ToList();

            if (!string.IsNullOrEmpty(category))
            {
                listings = listings.Where(l => l.Item.Category == category).ToList();
            }

            if (rarity.HasValue)
            {
                listings = listings.Where(l => l.Item.Rarity == rarity.Value).ToList();
            }

            return listings.OrderBy(l => l.Price.GetTotalValue()).ToList();
        }

        #endregion

        #region Market Simulation

        private void UpdateMarketPrices()
        {
            foreach (var currencyType in Enum.GetValues(typeof(CurrencyType)).Cast<CurrencyType>())
            {
                if (currencyType == CurrencyType.Coins) continue; // Base currency

                var oldRate = _currentExchangeRates[currencyType];
                var demandFactor = CalculateCurrencyDemand(currencyType);
                var volatility = UnityEngine.Random.Range(-marketVolatility, marketVolatility);

                var newRate = oldRate * (1f + demandFactor + volatility);
                newRate = Mathf.Clamp(newRate, oldRate * 0.8f, oldRate * 1.2f); // Limit extreme changes

                _currentExchangeRates[currencyType] = newRate;

                if (Mathf.Abs(newRate - oldRate) > oldRate * 0.05f) // 5% change threshold
                {
                    OnExchangeRateChanged?.Invoke(currencyType, newRate);
                }
            }
        }

        private void SimulateMarketActivity()
        {
            // Simulate NPC trading activity
            for (int i = 0; i < simulatedTraders; i++)
            {
                SimulateNPCTransaction();
            }

            // Process pending market events
            while (_pendingMarketEvents.Count > 0)
            {
                var marketEvent = _pendingMarketEvents.Dequeue();
                ProcessMarketEvent(marketEvent);
            }
        }

        private void SimulateNPCTransaction()
        {
            // Randomly choose a transaction type
            var transactionTypes = new[] { "buy_equipment", "sell_equipment", "exchange_currency", "list_item" };
            var transactionType = transactionTypes[UnityEngine.Random.Range(0, transactionTypes.Length)];

            switch (transactionType)
            {
                case "buy_equipment":
                    SimulateNPCPurchase();
                    break;
                case "exchange_currency":
                    SimulateNPCCurrencyExchange();
                    break;
                case "list_item":
                    SimulateNPCListing();
                    break;
            }
        }

        private void SimulateNPCPurchase()
        {
            var availableListings = _activeListings.Values.Where(l => UnityEngine.Random.value < 0.1f).ToList();
            if (availableListings.Count > 0)
            {
                var listing = availableListings[UnityEngine.Random.Range(0, availableListings.Count)];
                // Simulate NPC purchase without affecting player economies
                UpdateMarketSupplyDemand(listing.Item.Category, false, 1);
            }
        }

        private void SimulateNPCCurrencyExchange()
        {
            var currencies = Enum.GetValues(typeof(CurrencyType)).Cast<CurrencyType>().ToArray();
            var fromCurrency = currencies[UnityEngine.Random.Range(0, currencies.Length)];
            var toCurrency = currencies[UnityEngine.Random.Range(0, currencies.Length)];

            if (fromCurrency != toCurrency)
            {
                // Affect exchange rates slightly
                var impact = UnityEngine.Random.Range(0.001f, 0.005f);
                _currentExchangeRates[toCurrency] += impact;
            }
        }

        private void SimulateNPCListing()
        {
            var categories = new[] { "Equipment", "Monsters", "Materials", "Food" };
            var category = categories[UnityEngine.Random.Range(0, categories.Length)];

            UpdateMarketSupplyDemand(category, true, 1);
        }

        private void UpdateMarketSupplyDemand(string category, bool isSupply, int quantity)
        {
            if (!_marketDemand.ContainsKey(category))
            {
                _marketDemand[category] = new MarketDemand();
            }

            if (!_marketSupply.ContainsKey(category))
            {
                _marketSupply[category] = new MarketSupply();
            }

            if (isSupply)
            {
                _marketSupply[category].currentLevel += quantity * 0.1f;
            }
            else
            {
                _marketDemand[category].currentLevel += quantity * 0.1f;
            }
        }

        private float CalculateCurrencyDemand(CurrencyType currencyType)
        {
            // Base demand calculation - simplified
            return UnityEngine.Random.Range(-0.02f, 0.02f);
        }

        private void ProcessMarketEvent(MarketEvent marketEvent)
        {
            // Process special market events like economic booms, crashes, etc.
            Debug.Log($"📈 Processing market event: {marketEvent.Type}");
        }

        #endregion

        #region Pricing System

        /// <summary>
        /// Calculate dynamic price for an item based on market conditions
        /// </summary>
        public TownResources CalculateItemPrice(MarketItem item, int quantity = 1)
        {
            var basePrice = GetBaseItemPrice(item);
            var marketMultiplier = CalculateMarketPriceMultiplier(item.Category);
            var rarityMultiplier = GetRarityPriceMultiplier(item.Rarity);
            var quantityMultiplier = CalculateQuantityDiscount(quantity);

            var finalPrice = basePrice * marketMultiplier * rarityMultiplier * quantityMultiplier;
            return finalPrice;
        }

        private TownResources GetBaseItemPrice(MarketItem item)
        {
            return item.Category switch
            {
                "Equipment" => new TownResources { coins = 100 },
                "Monsters" => new TownResources { coins = 500, gems = 5 },
                "GeneticSamples" => new TownResources { coins = 50, activityTokens = 2 },
                "Materials" => new TownResources { coins = 25 },
                "Food" => new TownResources { coins = 10 },
                _ => new TownResources { coins = 50 }
            };
        }

        private float CalculateMarketPriceMultiplier(string category)
        {
            if (!_marketDemand.TryGetValue(category, out var demand) ||
                !_marketSupply.TryGetValue(category, out var supply))
            {
                return 1f;
            }

            var ratio = demand.currentLevel / Mathf.Max(supply.currentLevel, 0.1f);
            return Mathf.Clamp(ratio, 0.5f, 2f);
        }

        private float GetRarityPriceMultiplier(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => 1f,
                EquipmentRarity.Uncommon => 2f,
                EquipmentRarity.Rare => 5f,
                EquipmentRarity.Epic => 12f,
                EquipmentRarity.Legendary => 30f,
                _ => 1f
            };
        }

        private float CalculateQuantityDiscount(int quantity)
        {
            // Bulk discount
            if (quantity >= 10) return 0.9f;
            if (quantity >= 5) return 0.95f;
            return 1f;
        }

        #endregion

        #region Utility Methods

        private bool HasSufficientCurrency(PlayerWallet wallet, CurrencyType currencyType, float amount)
        {
            return currencyType switch
            {
                CurrencyType.Coins => wallet.Currency.coins >= amount,
                CurrencyType.Gems => wallet.Currency.gems >= amount,
                CurrencyType.ActivityTokens => wallet.Currency.activityTokens >= amount,
                CurrencyType.GeneticSamples => wallet.Currency.geneticSamples >= amount,
                CurrencyType.Materials => wallet.Currency.materials >= amount,
                CurrencyType.Energy => wallet.Currency.energy >= amount,
                _ => false
            };
        }

        private TownResources CreateCurrencyAmount(CurrencyType currencyType, float amount)
        {
            var resources = new TownResources();
            switch (currencyType)
            {
                case CurrencyType.Coins: resources.coins = (int)amount; break;
                case CurrencyType.Gems: resources.gems = (int)amount; break;
                case CurrencyType.ActivityTokens: resources.activityTokens = (int)amount; break;
                case CurrencyType.GeneticSamples: resources.geneticSamples = (int)amount; break;
                case CurrencyType.Materials: resources.materials = (int)amount; break;
                case CurrencyType.Energy: resources.energy = (int)amount; break;
            }
            return resources;
        }

        private void RecordTransaction(EconomicTransaction transaction)
        {
            _transactionHistory.Add(transaction);

            // Limit transaction history size
            if (_transactionHistory.Count > 1000)
            {
                _transactionHistory.RemoveAt(0);
            }

            OnTransactionCompleted?.Invoke(transaction);
        }

        /// <summary>
        /// Get economic statistics for analysis
        /// </summary>
        public EconomicStatistics GetEconomicStatistics()
        {
            return new EconomicStatistics
            {
                TotalPlayersWithWallets = _playerWallets.Count,
                GlobalEconomyValue = _globalEconomy.GetTotalValue(),
                ActiveMarketListings = _activeListings.Count,
                RecentTransactions = _transactionHistory.Where(t => t.Timestamp > DateTime.UtcNow.AddHours(-24)).Count(),
                AverageWealthPerPlayer = _playerWallets.Values.Any() ? _playerWallets.Values.Average(w => w.Currency.GetTotalValue()) : 0f,
                CurrentExchangeRates = new Dictionary<CurrencyType, float>(_currentExchangeRates)
            };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Economy configuration ScriptableObject
    /// </summary>
    [Serializable]
    public class EconomyConfig
    {
        [Header("Starting Resources")]
        public TownResources startingPlayerCurrency;
        public TownResources globalEconomyPool;

        [Header("Economy Rules")]
        [Range(0f, 1f)] public float exchangeFeePercentage = 0.05f;
        [Range(0f, 1f)] public float marketplaceTaxRate = 0.02f;
        public bool enableInflation = true;
        public float baseInflationRate = 0.001f;
    }

    /// <summary>
    /// Currency exchange rates configuration
    /// </summary>
    [Serializable]
    public class CurrencyExchangeRates
    {
        [Header("Exchange Rates (Coins as base)")]
        public float coinsToGemsRate = 0.1f;
        public float coinsToTokensRate = 0.5f;
        public float coinsToSamplesRate = 2f;
        public float coinsToMaterialsRate = 1f;
        public float coinsToEnergyRate = 3f;
    }

    /// <summary>
    /// Player wallet data
    /// </summary>
    [Serializable]
    public class PlayerWallet
    {
        public string PlayerId;
        public TownResources Currency;
        public DateTime LastUpdateTime;
        public List<string> TransactionHistory;

        public float GetTotalValue() => Currency.GetTotalValue();
    }

    /// <summary>
    /// Market item representation
    /// </summary>
    [Serializable]
    public class MarketItem
    {
        public string Id;
        public string Name;
        public string Description;
        public string Category;
        public EquipmentRarity Rarity;
        public Sprite Icon;
        public Dictionary<string, object> Properties;
    }

    /// <summary>
    /// Marketplace listing
    /// </summary>
    [Serializable]
    public class MarketListing
    {
        public string Id;
        public string SellerId;
        public MarketItem Item;
        public TownResources Price;
        public int Quantity;
        public DateTime ListedAt;
        public DateTime ExpiresAt;
        public bool IsFeatured;
    }

    /// <summary>
    /// Economic transaction record
    /// </summary>
    [Serializable]
    public class EconomicTransaction
    {
        public string Id;
        public string PlayerId;
        public TransactionType Type;
        public TownResources Amount;
        public string Reason;
        public DateTime Timestamp;
        public Dictionary<string, object> Metadata;
    }

    /// <summary>
    /// Market demand tracking
    /// </summary>
    [Serializable]
    public class MarketDemand
    {
        public float baseLevel = 1f;
        public float currentLevel = 1f;
        public float trend = 0f;
        public float volatility = 0.1f;
    }

    /// <summary>
    /// Market supply tracking
    /// </summary>
    [Serializable]
    public class MarketSupply
    {
        public float baseLevel = 1f;
        public float currentLevel = 1f;
        public float productionRate = 0.1f;
        public float consumptionRate = 0.1f;
    }

    /// <summary>
    /// Price history for items
    /// </summary>
    [Serializable]
    public class PriceHistory
    {
        public List<PricePoint> History = new();
        public float CurrentPrice;
        public float LowestPrice;
        public float HighestPrice;
        public DateTime LastUpdated;

        public void AddPricePoint(float price)
        {
            History.Add(new PricePoint { Price = price, Timestamp = DateTime.UtcNow });
            CurrentPrice = price;

            if (LowestPrice == 0 || price < LowestPrice) LowestPrice = price;
            if (price > HighestPrice) HighestPrice = price;

            LastUpdated = DateTime.UtcNow;

            // Keep only last 100 points
            if (History.Count > 100)
            {
                History.RemoveAt(0);
            }
        }
    }

    [Serializable]
    public struct PricePoint
    {
        public float Price;
        public DateTime Timestamp;
    }

    /// <summary>
    /// Market event for simulation
    /// </summary>
    [Serializable]
    public class MarketEvent
    {
        public string Type;
        public string Description;
        public float ImpactMagnitude;
        public string[] AffectedCategories;
        public DateTime EventTime;
    }

    /// <summary>
    /// Economic statistics
    /// </summary>
    [Serializable]
    public class EconomicStatistics
    {
        public int TotalPlayersWithWallets;
        public float GlobalEconomyValue;
        public int ActiveMarketListings;
        public int RecentTransactions;
        public float AverageWealthPerPlayer;
        public Dictionary<CurrencyType, float> CurrentExchangeRates;
    }

    /// <summary>
    /// Currency types
    /// </summary>
    public enum CurrencyType
    {
        Coins,
        Gems,
        ActivityTokens,
        GeneticSamples,
        Materials,
        Energy
    }

    /// <summary>
    /// Transaction types
    /// </summary>
    public enum TransactionType
    {
        Income,
        Expense,
        Exchange,
        Trade,
        Fee,
        Tax,
        Reward,
        Penalty
    }

    #endregion
}