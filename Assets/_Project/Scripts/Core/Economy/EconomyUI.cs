using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Laboratory.Core.MonsterTown;
using Laboratory.Core.Equipment;
using Laboratory.Core.Equipment.Types;
using EquipmentRarity = Laboratory.Core.Equipment.Types.EquipmentRarity;

namespace Laboratory.Core.Economy
{
    /// <summary>
    /// Economy UI Manager - Handles all economic interface elements
    /// Provides wallet display, marketplace, trading, and currency exchange
    /// </summary>
    public class EconomyUI : MonoBehaviour
    {
        [Header("💰 Wallet Display")]
        [SerializeField] private GameObject walletPanel;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text gemsText;
        [SerializeField] private Text tokensText;
        [SerializeField] private Text samplesText;
        [SerializeField] private Text materialsText;
        [SerializeField] private Text energyText;

        [Header("🏪 Marketplace")]
        [SerializeField] private GameObject marketplacePanel;
        [SerializeField] private Transform marketListingsParent;
        [SerializeField] private GameObject marketListingPrefab;
        [SerializeField] private Dropdown categoryFilter;
        [SerializeField] private Dropdown rarityFilter;
        [SerializeField] private Button refreshMarketButton;

        [Header("💱 Currency Exchange")]
        [SerializeField] private GameObject exchangePanel;
        [SerializeField] private Dropdown fromCurrencyDropdown;
        [SerializeField] private Dropdown toCurrencyDropdown;
        [SerializeField] private InputField exchangeAmountInput;
        [SerializeField] private Text exchangeRateText;
        [SerializeField] private Text exchangeResultText;
        [SerializeField] private Button exchangeButton;

        [Header("📊 Market Stats")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private Text totalPlayersText;
        [SerializeField] private Text globalValueText;
        [SerializeField] private Text activeListingsText;
        [SerializeField] private Transform exchangeRatesParent;
        [SerializeField] private GameObject exchangeRateItemPrefab;

        [Header("🛒 Item Listing")]
        [SerializeField] private GameObject listItemPanel;
        [SerializeField] private InputField itemNameInput;
        [SerializeField] private Dropdown itemCategoryDropdown;
        [SerializeField] private Dropdown itemRarityDropdown;
        [SerializeField] private InputField priceCoinsInput;
        [SerializeField] private InputField priceGemsInput;
        [SerializeField] private InputField quantityInput;
        [SerializeField] private Button listItemButton;

        // Runtime data
        private EconomyManager _economyManager;
        private string _currentPlayerId = "Player1"; // Default player ID
        private List<MarketListingUI> _marketListingUIs = new();
        private PlayerWallet _currentWallet;

        #region Initialization

        public void InitializeEconomyUI(EconomyManager economyManager, string playerId = "Player1")
        {
            _economyManager = economyManager;
            _currentPlayerId = playerId;

            SetupEventListeners();
            InitializeDropdowns();

            // Get or create player wallet
            _currentWallet = _economyManager.GetPlayerWallet(_currentPlayerId);

            RefreshAllUI();

            // Subscribe to economy events
            _economyManager.OnWalletUpdated += OnWalletUpdated;
            _economyManager.OnItemListed += OnItemListed;
            _economyManager.OnItemSold += OnItemSold;
            _economyManager.OnExchangeRateChanged += OnExchangeRateChanged;

            Debug.Log("💰 Economy UI initialized");
        }

        private void SetupEventListeners()
        {
            if (refreshMarketButton != null)
                refreshMarketButton.onClick.AddListener(RefreshMarketplace);

            if (exchangeButton != null)
                exchangeButton.onClick.AddListener(PerformCurrencyExchange);

            if (listItemButton != null)
                listItemButton.onClick.AddListener(ListItemForSale);

            if (categoryFilter != null)
                categoryFilter.onValueChanged.AddListener(OnCategoryFilterChanged);

            if (rarityFilter != null)
                rarityFilter.onValueChanged.AddListener(OnRarityFilterChanged);

            if (exchangeAmountInput != null)
                exchangeAmountInput.onValueChanged.AddListener(UpdateExchangePreview);

            if (fromCurrencyDropdown != null)
                fromCurrencyDropdown.onValueChanged.AddListener(OnExchangeCurrencyChanged);

            if (toCurrencyDropdown != null)
                toCurrencyDropdown.onValueChanged.AddListener(OnExchangeCurrencyChanged);
        }

        private void InitializeDropdowns()
        {
            // Initialize category filter
            if (categoryFilter != null)
            {
                categoryFilter.options.Clear();
                categoryFilter.options.Add(new Dropdown.OptionData("All Categories"));
                categoryFilter.options.Add(new Dropdown.OptionData("Equipment"));
                categoryFilter.options.Add(new Dropdown.OptionData("Monsters"));
                categoryFilter.options.Add(new Dropdown.OptionData("Materials"));
                categoryFilter.options.Add(new Dropdown.OptionData("Food"));
                categoryFilter.RefreshShownValue();
            }

            // Initialize rarity filter
            if (rarityFilter != null)
            {
                rarityFilter.options.Clear();
                rarityFilter.options.Add(new Dropdown.OptionData("All Rarities"));
                foreach (EquipmentRarity rarity in Enum.GetValues(typeof(EquipmentRarity)))
                {
                    rarityFilter.options.Add(new Dropdown.OptionData(rarity.ToString()));
                }
                rarityFilter.RefreshShownValue();
            }

            // Initialize currency dropdowns
            var currencyOptions = new List<Dropdown.OptionData>();
            foreach (CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
            {
                currencyOptions.Add(new Dropdown.OptionData(currency.ToString()));
            }

            if (fromCurrencyDropdown != null)
            {
                fromCurrencyDropdown.options = new List<Dropdown.OptionData>(currencyOptions);
                fromCurrencyDropdown.RefreshShownValue();
            }

            if (toCurrencyDropdown != null)
            {
                toCurrencyDropdown.options = new List<Dropdown.OptionData>(currencyOptions);
                toCurrencyDropdown.value = 1; // Default to second currency
                toCurrencyDropdown.RefreshShownValue();
            }

            // Initialize item category dropdown
            if (itemCategoryDropdown != null)
            {
                itemCategoryDropdown.options.Clear();
                itemCategoryDropdown.options.Add(new Dropdown.OptionData("Equipment"));
                itemCategoryDropdown.options.Add(new Dropdown.OptionData("Monsters"));
                itemCategoryDropdown.options.Add(new Dropdown.OptionData("Materials"));
                itemCategoryDropdown.options.Add(new Dropdown.OptionData("Food"));
                itemCategoryDropdown.RefreshShownValue();
            }

            // Initialize item rarity dropdown
            if (itemRarityDropdown != null)
            {
                itemRarityDropdown.options.Clear();
                foreach (EquipmentRarity rarity in Enum.GetValues(typeof(EquipmentRarity)))
                {
                    itemRarityDropdown.options.Add(new Dropdown.OptionData(rarity.ToString()));
                }
                itemRarityDropdown.RefreshShownValue();
            }
        }

        #endregion

        #region UI Display Management

        public void ShowWallet()
        {
            if (walletPanel != null)
                walletPanel.SetActive(true);

            RefreshWalletDisplay();
        }

        public void HideWallet()
        {
            if (walletPanel != null)
                walletPanel.SetActive(false);
        }

        public void ShowMarketplace()
        {
            if (marketplacePanel != null)
                marketplacePanel.SetActive(true);

            RefreshMarketplace();
        }

        public void HideMarketplace()
        {
            if (marketplacePanel != null)
                marketplacePanel.SetActive(false);
        }

        public void ShowCurrencyExchange()
        {
            if (exchangePanel != null)
                exchangePanel.SetActive(true);

            RefreshExchangeRates();
        }

        public void HideCurrencyExchange()
        {
            if (exchangePanel != null)
                exchangePanel.SetActive(false);
        }

        public void ShowMarketStats()
        {
            if (statsPanel != null)
                statsPanel.SetActive(true);

            RefreshMarketStats();
        }

        public void HideMarketStats()
        {
            if (statsPanel != null)
                statsPanel.SetActive(false);
        }

        public void ShowListItemPanel()
        {
            if (listItemPanel != null)
                listItemPanel.SetActive(true);
        }

        public void HideListItemPanel()
        {
            if (listItemPanel != null)
                listItemPanel.SetActive(false);
        }

        private void RefreshAllUI()
        {
            RefreshWalletDisplay();
            RefreshMarketplace();
            RefreshExchangeRates();
            RefreshMarketStats();
        }

        #endregion

        #region Wallet Display

        private void RefreshWalletDisplay()
        {
            if (_currentWallet == null) return;

            var currency = _currentWallet.Currency;

            if (coinsText != null)
                coinsText.text = currency.coins.ToString("N0");

            if (gemsText != null)
                gemsText.text = currency.gems.ToString("N0");

            if (tokensText != null)
                tokensText.text = currency.activityTokens.ToString("N0");

            if (samplesText != null)
                samplesText.text = currency.geneticSamples.ToString("N0");

            if (materialsText != null)
                materialsText.text = currency.materials.ToString("N0");

            if (energyText != null)
                energyText.text = currency.energy.ToString("N0");
        }

        #endregion

        #region Marketplace Display

        private void RefreshMarketplace()
        {
            // Clear existing listings
            foreach (var listingUI in _marketListingUIs)
            {
                if (listingUI != null && listingUI.gameObject != null)
                    Destroy(listingUI.gameObject);
            }
            _marketListingUIs.Clear();

            if (_economyManager == null || marketListingsParent == null) return;

            // Get filtered listings
            string selectedCategory = GetSelectedCategory();
            EquipmentRarity? selectedRarity = GetSelectedRarity();

            var listings = _economyManager.GetMarketplaceListings(selectedCategory, selectedRarity);

            // Create UI for each listing
            foreach (var listing in listings.Take(20)) // Limit to 20 for performance
            {
                CreateMarketListingUI(listing);
            }
        }

        private void CreateMarketListingUI(MarketListing listing)
        {
            if (marketListingPrefab == null) return;

            var listingObject = Instantiate(marketListingPrefab, marketListingsParent);
            var listingUI = listingObject.GetComponent<MarketListingUI>();

            if (listingUI == null)
                listingUI = listingObject.AddComponent<MarketListingUI>();

            listingUI.InitializeListing(listing, this, _economyManager);
            _marketListingUIs.Add(listingUI);
        }

        private string GetSelectedCategory()
        {
            if (categoryFilter == null || categoryFilter.value == 0) return null;
            return categoryFilter.options[categoryFilter.value].text;
        }

        private EquipmentRarity? GetSelectedRarity()
        {
            if (rarityFilter == null || rarityFilter.value == 0) return null;
            var rarityText = rarityFilter.options[rarityFilter.value].text;
            return Enum.Parse<EquipmentRarity>(rarityText);
        }

        #endregion

        #region Currency Exchange

        private void RefreshExchangeRates()
        {
            UpdateExchangePreview("");

            // Update exchange rates display
            if (exchangeRatesParent != null && exchangeRateItemPrefab != null)
            {
                // Clear existing rate displays
                for (int i = 0; i < exchangeRatesParent.childCount; i++)
                {
                    Destroy(exchangeRatesParent.GetChild(i).gameObject);
                }

                var rates = _economyManager.GetCurrentExchangeRates();
                foreach (var rate in rates)
                {
                    if (rate.Key == CurrencyType.Coins) continue; // Skip base currency

                    var rateObject = Instantiate(exchangeRateItemPrefab, exchangeRatesParent);
                    var rateText = rateObject.GetComponent<Text>();
                    if (rateText != null)
                    {
                        rateText.text = $"1 {rate.Key} = {rate.Value:F4} Coins";
                    }
                }
            }
        }

        private void UpdateExchangePreview(string inputValue)
        {
            if (_economyManager == null) return;

            if (!float.TryParse(exchangeAmountInput.text, out float amount) || amount <= 0)
            {
                if (exchangeRateText != null)
                    exchangeRateText.text = "Enter amount to see exchange rate";
                if (exchangeResultText != null)
                    exchangeResultText.text = "";
                return;
            }

            var fromCurrency = GetSelectedFromCurrency();
            var toCurrency = GetSelectedToCurrency();

            if (fromCurrency == toCurrency)
            {
                if (exchangeRateText != null)
                    exchangeRateText.text = "Select different currencies";
                return;
            }

            var exchangeRate = _economyManager.CalculateExchangeRate(fromCurrency, toCurrency);
            var result = amount * exchangeRate;

            if (exchangeRateText != null)
                exchangeRateText.text = $"Rate: 1 {fromCurrency} = {exchangeRate:F4} {toCurrency}";

            if (exchangeResultText != null)
                exchangeResultText.text = $"You will receive: {result:F2} {toCurrency}";
        }

        private void PerformCurrencyExchange()
        {
            if (!float.TryParse(exchangeAmountInput.text, out float amount) || amount <= 0)
            {
                Debug.LogWarning("Invalid exchange amount");
                return;
            }

            var fromCurrency = GetSelectedFromCurrency();
            var toCurrency = GetSelectedToCurrency();

            bool success = _economyManager.ExchangeCurrency(_currentPlayerId, fromCurrency, toCurrency, amount);

            if (success)
            {
                // Clear input and refresh UI
                if (exchangeAmountInput != null)
                    exchangeAmountInput.text = "";

                RefreshWalletDisplay();
                UpdateExchangePreview("");

                Debug.Log($"Successfully exchanged {amount} {fromCurrency} for {toCurrency}");
            }
            else
            {
                Debug.LogWarning("Currency exchange failed");
            }
        }

        private CurrencyType GetSelectedFromCurrency()
        {
            if (fromCurrencyDropdown == null) return CurrencyType.Coins;
            var currencyText = fromCurrencyDropdown.options[fromCurrencyDropdown.value].text;
            return Enum.Parse<CurrencyType>(currencyText);
        }

        private CurrencyType GetSelectedToCurrency()
        {
            if (toCurrencyDropdown == null) return CurrencyType.Gems;
            var currencyText = toCurrencyDropdown.options[toCurrencyDropdown.value].text;
            return Enum.Parse<CurrencyType>(currencyText);
        }

        #endregion

        #region Item Listing

        private void ListItemForSale()
        {
            var itemName = itemNameInput?.text;
            if (string.IsNullOrEmpty(itemName))
            {
                Debug.LogWarning("Item name is required");
                return;
            }

            if (!int.TryParse(priceCoinsInput?.text, out int coins)) coins = 0;
            if (!int.TryParse(priceGemsInput?.text, out int gems)) gems = 0;
            if (!int.TryParse(quantityInput?.text, out int quantity) || quantity <= 0) quantity = 1;

            if (coins <= 0 && gems <= 0)
            {
                Debug.LogWarning("Price must be greater than 0");
                return;
            }

            var category = itemCategoryDropdown?.options[itemCategoryDropdown.value].text ?? "Equipment";
            var rarityText = itemRarityDropdown?.options[itemRarityDropdown.value].text ?? "Common";
            var rarity = Enum.Parse<EquipmentRarity>(rarityText);

            var item = new MarketItem
            {
                Id = Guid.NewGuid().ToString(),
                Name = itemName,
                Description = $"Player-listed {itemName}",
                Category = category,
                Rarity = rarity
            };

            var price = new TownResources { coins = coins, gems = gems };

            bool success = _economyManager.ListItemForSale(_currentPlayerId, item, price, quantity);

            if (success)
            {
                // Clear form
                if (itemNameInput != null) itemNameInput.text = "";
                if (priceCoinsInput != null) priceCoinsInput.text = "";
                if (priceGemsInput != null) priceGemsInput.text = "";
                if (quantityInput != null) quantityInput.text = "1";

                RefreshMarketplace();
                RefreshWalletDisplay();

                Debug.Log($"Successfully listed {itemName} for sale");
            }
        }

        #endregion

        #region Market Statistics

        private void RefreshMarketStats()
        {
            if (_economyManager == null) return;

            var stats = _economyManager.GetEconomicStatistics();

            if (totalPlayersText != null)
                totalPlayersText.text = stats.TotalPlayersWithWallets.ToString();

            if (globalValueText != null)
                globalValueText.text = stats.GlobalEconomyValue.ToString("N0");

            if (activeListingsText != null)
                activeListingsText.text = stats.ActiveMarketListings.ToString();
        }

        #endregion

        #region Event Handlers

        private void OnWalletUpdated(PlayerWallet wallet)
        {
            if (wallet.PlayerId == _currentPlayerId)
            {
                _currentWallet = wallet;
                RefreshWalletDisplay();
            }
        }

        private void OnItemListed(MarketListing listing)
        {
            RefreshMarketplace();
        }

        private void OnItemSold(MarketListing listing)
        {
            RefreshMarketplace();
        }

        private void OnExchangeRateChanged(CurrencyType currency, float newRate)
        {
            RefreshExchangeRates();
        }

        private void OnCategoryFilterChanged(int value)
        {
            RefreshMarketplace();
        }

        private void OnRarityFilterChanged(int value)
        {
            RefreshMarketplace();
        }

        private void OnExchangeCurrencyChanged(int value)
        {
            UpdateExchangePreview("");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Switch to a different player's wallet
        /// </summary>
        public void SwitchPlayer(string playerId)
        {
            _currentPlayerId = playerId;
            _currentWallet = _economyManager.GetPlayerWallet(playerId);
            RefreshWalletDisplay();
        }

        /// <summary>
        /// Add currency to current player (for testing/admin)
        /// </summary>
        public void AddTestCurrency(TownResources amount)
        {
            _economyManager.AddCurrencyToWallet(_currentPlayerId, amount, "Test/Admin");
        }

        #endregion

        private void OnDestroy()
        {
            if (_economyManager != null)
            {
                _economyManager.OnWalletUpdated -= OnWalletUpdated;
                _economyManager.OnItemListed -= OnItemListed;
                _economyManager.OnItemSold -= OnItemSold;
                _economyManager.OnExchangeRateChanged -= OnExchangeRateChanged;
            }
        }
    }

    #region Market Listing UI Component

    /// <summary>
    /// Individual market listing UI component
    /// </summary>
    public class MarketListingUI : MonoBehaviour
    {
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemCategoryText;
        [SerializeField] private Text itemPriceText;
        [SerializeField] private Text itemQuantityText;
        [SerializeField] private Text sellerText;
        [SerializeField] private Image rarityBackground;
        [SerializeField] private Button purchaseButton;

        private MarketListing _listing;
        private EconomyUI _parentUI;
        private EconomyManager _economyManager;

        public void InitializeListing(MarketListing listing, EconomyUI parentUI, EconomyManager economyManager)
        {
            _listing = listing;
            _parentUI = parentUI;
            _economyManager = economyManager;

            if (itemNameText != null)
                itemNameText.text = listing.Item.Name;

            if (itemCategoryText != null)
                itemCategoryText.text = listing.Item.Category;

            if (itemPriceText != null)
                itemPriceText.text = FormatPrice(listing.Price);

            if (itemQuantityText != null)
                itemQuantityText.text = $"Qty: {listing.Quantity}";

            if (sellerText != null)
                sellerText.text = $"Seller: {listing.SellerId}";

            if (rarityBackground != null)
                rarityBackground.color = GetRarityColor(listing.Item.Rarity);

            if (purchaseButton != null)
                purchaseButton.onClick.AddListener(PurchaseItem);
        }

        private string FormatPrice(TownResources price)
        {
            var parts = new List<string>();

            if (price.coins > 0) parts.Add($"{price.coins} Coins");
            if (price.gems > 0) parts.Add($"{price.gems} Gems");
            if (price.activityTokens > 0) parts.Add($"{price.activityTokens} Tokens");

            return string.Join(", ", parts);
        }

        private Color GetRarityColor(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => Color.white,
                EquipmentRarity.Uncommon => Color.green,
                EquipmentRarity.Rare => Color.blue,
                EquipmentRarity.Epic => new Color(0.5f, 0f, 1f),
                EquipmentRarity.Legendary => Color.yellow,
                _ => Color.gray
            };
        }

        private void PurchaseItem()
        {
            // For this demo, always purchase quantity 1
            bool success = _economyManager.PurchaseMarketItem(_parentUI._currentPlayerId, _listing.Id, 1);

            if (success)
            {
                Debug.Log($"Successfully purchased {_listing.Item.Name}");
            }
            else
            {
                Debug.LogWarning($"Failed to purchase {_listing.Item.Name}");
            }
        }
    }

    #endregion
}