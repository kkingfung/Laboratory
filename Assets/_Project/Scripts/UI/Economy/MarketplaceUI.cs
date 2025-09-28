using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Economy;
using Laboratory.Core.Utilities;
using Laboratory.Chimera.Core;

namespace Laboratory.UI.Economy
{
    /// <summary>
    /// Comprehensive marketplace UI system providing interfaces for creature trading,
    /// auction participation, market analysis, and social economy features.
    /// Supports real-time updates, filtering, sorting, and detailed creature inspection.
    /// </summary>
    public class MarketplaceUI : MonoBehaviour
    {
        [Header("Core UI Components")]
        [SerializeField] private Canvas marketplaceCanvas;
        [SerializeField] private GameObject marketplacePanel;
        [SerializeField] private Button toggleMarketplaceButton;

        [Header("Main Navigation")]
        [SerializeField] private Button browseListingsButton;
        [SerializeField] private Button auctionsButton;
        [SerializeField] private Button myListingsButton;
        [SerializeField] private Button marketAnalysisButton;
        [SerializeField] private Button socialFeaturesButton;

        [Header("Browse Listings Panel")]
        [SerializeField] private GameObject browsePanel;
        [SerializeField] private Transform listingsContainer;
        [SerializeField] private GameObject listingItemPrefab;
        [SerializeField] private ScrollRect listingsScrollRect;

        [Header("Auctions Panel")]
        [SerializeField] private GameObject auctionsPanel;
        [SerializeField] private Transform auctionsContainer;
        [SerializeField] private GameObject auctionItemPrefab;
        [SerializeField] private ScrollRect auctionsScrollRect;

        [Header("Market Analysis Panel")]
        [SerializeField] private GameObject analysisPanel;
        [SerializeField] private TextMeshProUGUI marketStatsText;
        [SerializeField] private Transform speciesAnalysisContainer;
        [SerializeField] private GameObject speciesAnalysisPrefab;

        [Header("Filtering and Search")]
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private TMP_Dropdown speciesFilter;
        [SerializeField] private TMP_Dropdown priceRangeFilter;
        [SerializeField] private TMP_Dropdown rarityFilter;
        [SerializeField] private TMP_Dropdown sortByDropdown;
        [SerializeField] private Toggle ascendingToggle;

        [Header("Creature Details Panel")]
        [SerializeField] private GameObject creatureDetailsPanel;
        [SerializeField] private CreatureDetailsUI creatureDetailsUI;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button bidButton;
        [SerializeField] private Button makeOfferButton;
        [SerializeField] private Button addToWatchlistButton;

        [Header("Create Listing Panel")]
        [SerializeField] private GameObject createListingPanel;
        [SerializeField] private TMP_Dropdown ownedCreaturesDropdown;
        [SerializeField] private TMP_InputField askingPriceInput;
        [SerializeField] private TMP_Dropdown listingTypeDropdown;
        [SerializeField] private TMP_InputField listingDescriptionInput;
        [SerializeField] private Button createListingButton;

        [Header("Social Features Panel")]
        [SerializeField] private GameObject socialPanel;
        [SerializeField] private Transform guildsContainer;
        [SerializeField] private Transform collaborativeProjectsContainer;
        [SerializeField] private GameObject guildItemPrefab;
        [SerializeField] private GameObject projectItemPrefab;

        [Header("Notification System")]
        [SerializeField] private Transform notificationContainer;
        [SerializeField] private GameObject notificationPrefab;

        [Header("Update Settings")]
        [SerializeField] private float uiUpdateInterval = 1f;
        [SerializeField] private bool enableRealTimeUpdates = true;

        // Core system references
        private BreedingMarketplace marketplace;
        private List<ListingItemUI> currentListingUIs = new List<ListingItemUI>();
        private List<AuctionItemUI> currentAuctionUIs = new List<AuctionItemUI>();

        // UI state
        private MarketplaceTab currentTab = MarketplaceTab.BrowseListings;
        private MarketplaceFilters currentFilters = new MarketplaceFilters();
        private CreatureGenome selectedCreature;
        private float lastUIUpdate = 0f;

        // Pagination
        private int currentPage = 0;
        private int itemsPerPage = 20;
        private int totalPages = 0;

        private void Awake()
        {
            InitializeUI();
        }

        private void Start()
        {
            ConnectToMarketplace();
            SetupEventListeners();
            RefreshUI();
        }

        private void Update()
        {
            if (enableRealTimeUpdates && Time.time - lastUIUpdate >= uiUpdateInterval)
            {
                UpdateCurrentTab();
                lastUIUpdate = Time.time;
            }
        }

        private void InitializeUI()
        {
            if (marketplaceCanvas == null)
                marketplaceCanvas = GetComponentInParent<Canvas>();

            // Initialize UI state
            if (marketplacePanel != null)
                marketplacePanel.SetActive(false);

            // Setup navigation buttons
            SetupNavigationButtons();

            // Setup filters and search
            SetupFiltersAndSearch();

            // Initialize all panels to inactive
            SetAllPanelsInactive();
        }

        private void ConnectToMarketplace()
        {
            marketplace = BreedingMarketplace.Instance;
            if (marketplace == null)
            {
                DebugManager.LogWarning("BreedingMarketplace not found - UI will not function properly");
                return;
            }
        }

        private void SetupEventListeners()
        {
            // Navigation buttons
            if (toggleMarketplaceButton != null)
                toggleMarketplaceButton.onClick.AddListener(ToggleMarketplacePanel);

            if (browseListingsButton != null)
                browseListingsButton.onClick.AddListener(() => SwitchTab(MarketplaceTab.BrowseListings));

            if (auctionsButton != null)
                auctionsButton.onClick.AddListener(() => SwitchTab(MarketplaceTab.Auctions));

            if (myListingsButton != null)
                myListingsButton.onClick.AddListener(() => SwitchTab(MarketplaceTab.MyListings));

            if (marketAnalysisButton != null)
                marketAnalysisButton.onClick.AddListener(() => SwitchTab(MarketplaceTab.MarketAnalysis));

            if (socialFeaturesButton != null)
                socialFeaturesButton.onClick.AddListener(() => SwitchTab(MarketplaceTab.SocialFeatures));

            // Filter and search
            if (searchInput != null)
                searchInput.onValueChanged.AddListener(OnSearchChanged);

            if (speciesFilter != null)
                speciesFilter.onValueChanged.AddListener(OnFiltersChanged);

            if (sortByDropdown != null)
                sortByDropdown.onValueChanged.AddListener(OnSortChanged);

            // Creature interaction buttons
            if (purchaseButton != null)
                purchaseButton.onClick.AddListener(PurchaseSelectedCreature);

            if (bidButton != null)
                bidButton.onClick.AddListener(ShowBidDialog);

            if (makeOfferButton != null)
                makeOfferButton.onClick.AddListener(ShowOfferDialog);

            if (createListingButton != null)
                createListingButton.onClick.AddListener(CreateNewListing);

            // Marketplace events
            if (marketplace != null)
            {
                marketplace.OnListingCreated += HandleListingCreated;
                marketplace.OnTransactionCompleted += HandleTransactionCompleted;
                marketplace.OnAuctionStarted += HandleAuctionStarted;
                marketplace.OnAuctionEnded += HandleAuctionEnded;
            }
        }

        /// <summary>
        /// Toggles the marketplace panel visibility
        /// </summary>
        public void ToggleMarketplacePanel()
        {
            bool isActive = marketplacePanel.activeSelf;
            marketplacePanel.SetActive(!isActive);

            if (!isActive)
            {
                RefreshUI();
                SwitchTab(MarketplaceTab.BrowseListings);
            }
        }

        /// <summary>
        /// Switches to a different marketplace tab
        /// </summary>
        public void SwitchTab(MarketplaceTab tab)
        {
            currentTab = tab;
            SetAllPanelsInactive();

            switch (tab)
            {
                case MarketplaceTab.BrowseListings:
                    if (browsePanel != null)
                    {
                        browsePanel.SetActive(true);
                        RefreshListings();
                    }
                    break;

                case MarketplaceTab.Auctions:
                    if (auctionsPanel != null)
                    {
                        auctionsPanel.SetActive(true);
                        RefreshAuctions();
                    }
                    break;

                case MarketplaceTab.MyListings:
                    if (browsePanel != null)
                    {
                        browsePanel.SetActive(true);
                        RefreshMyListings();
                    }
                    break;

                case MarketplaceTab.MarketAnalysis:
                    if (analysisPanel != null)
                    {
                        analysisPanel.SetActive(true);
                        RefreshMarketAnalysis();
                    }
                    break;

                case MarketplaceTab.SocialFeatures:
                    if (socialPanel != null)
                    {
                        socialPanel.SetActive(true);
                        RefreshSocialFeatures();
                    }
                    break;
            }

            UpdateNavigationButtonStates();
        }

        /// <summary>
        /// Refreshes all UI elements
        /// </summary>
        public void RefreshUI()
        {
            if (marketplace == null) return;

            UpdateCurrentTab();
        }

        /// <summary>
        /// Updates the currently active tab
        /// </summary>
        public void UpdateCurrentTab()
        {
            switch (currentTab)
            {
                case MarketplaceTab.BrowseListings:
                    RefreshListings();
                    break;
                case MarketplaceTab.Auctions:
                    RefreshAuctions();
                    break;
                case MarketplaceTab.MyListings:
                    RefreshMyListings();
                    break;
                case MarketplaceTab.MarketAnalysis:
                    RefreshMarketAnalysis();
                    break;
                case MarketplaceTab.SocialFeatures:
                    RefreshSocialFeatures();
                    break;
            }
        }

        private void RefreshListings()
        {
            if (marketplace == null || listingsContainer == null) return;

            // Clear existing listings
            ClearListingItems();

            // Get filtered and sorted listings
            var listings = GetFilteredListings();
            var paginatedListings = ApplyPagination(listings);

            // Create UI items for listings
            foreach (var listing in paginatedListings)
            {
                CreateListingItem(listing);
            }

            // Update pagination info
            UpdatePaginationInfo(listings.Count);
        }

        private void RefreshAuctions()
        {
            if (marketplace == null || auctionsContainer == null) return;

            // Clear existing auction items
            ClearAuctionItems();

            // Get active auctions (this would require a method in marketplace)
            // var auctions = marketplace.GetActiveAuctions();
            // foreach (var auction in auctions)
            // {
            //     CreateAuctionItem(auction);
            // }
        }

        private void RefreshMyListings()
        {
            if (marketplace == null || listingsContainer == null) return;

            // Show only player's own listings
            currentFilters.sellerIdFilter = GetCurrentPlayerId();
            RefreshListings();
        }

        private void RefreshMarketAnalysis()
        {
            if (marketplace == null || marketStatsText == null) return;

            var stats = marketplace.GetMarketStatistics();
            marketStatsText.text = GenerateMarketStatsText(stats);

            // Update species analysis
            RefreshSpeciesAnalysis();
        }

        private void RefreshSocialFeatures()
        {
            if (marketplace == null) return;

            // Refresh guilds and collaborative projects
            RefreshGuilds();
            RefreshCollaborativeProjects();
        }

        private void CreateListingItem(MarketplaceListing listing)
        {
            if (listingItemPrefab == null || listingsContainer == null) return;

            GameObject itemObj = Instantiate(listingItemPrefab, listingsContainer);
            ListingItemUI itemUI = itemObj.GetComponent<ListingItemUI>();

            if (itemUI != null)
            {
                itemUI.Initialize(listing, this);
                currentListingUIs.Add(itemUI);
            }
        }

        private void CreateAuctionItem(AuctionData auction)
        {
            if (auctionItemPrefab == null || auctionsContainer == null) return;

            GameObject itemObj = Instantiate(auctionItemPrefab, auctionsContainer);
            AuctionItemUI itemUI = itemObj.GetComponent<AuctionItemUI>();

            if (itemUI != null)
            {
                itemUI.Initialize(auction, this);
                currentAuctionUIs.Add(itemUI);
            }
        }

        /// <summary>
        /// Shows detailed information about a creature
        /// </summary>
        public void ShowCreatureDetails(CreatureGenome creature, MarketplaceListing listing = null)
        {
            selectedCreature = creature;

            if (creatureDetailsPanel != null)
            {
                creatureDetailsPanel.SetActive(true);

                if (creatureDetailsUI != null)
                {
                    creatureDetailsUI.DisplayCreature(creature);
                }

                // Update interaction buttons based on listing type
                UpdateCreatureInteractionButtons(listing);
            }
        }

        /// <summary>
        /// Hides the creature details panel
        /// </summary>
        public void HideCreatureDetails()
        {
            if (creatureDetailsPanel != null)
            {
                creatureDetailsPanel.SetActive(false);
            }

            selectedCreature = null;
        }

        private void UpdateCreatureInteractionButtons(MarketplaceListing listing)
        {
            if (listing == null) return;

            // Update button visibility and text based on listing type
            if (purchaseButton != null)
            {
                purchaseButton.gameObject.SetActive(listing.listingType == ListingType.FixedPrice);
                var buttonText = purchaseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = $"Buy for {listing.askingPrice:C}";
            }

            if (bidButton != null)
            {
                bidButton.gameObject.SetActive(listing.listingType == ListingType.Auction);
            }

            if (makeOfferButton != null)
            {
                makeOfferButton.gameObject.SetActive(listing.acceptOffers);
            }
        }

        private void PurchaseSelectedCreature()
        {
            if (selectedCreature == null || marketplace == null) return;

            // Find the listing for this creature
            // This would require a method to get listing by creature
            // var listing = marketplace.GetListingByCreature(selectedCreature);
            // if (listing != null)
            // {
            //     bool success = marketplace.PurchaseCreature(GetCurrentPlayerId(), listing.listingId);
            //     if (success)
            //     {
            //         ShowNotification("Purchase successful!", NotificationType.Success);
            //         HideCreatureDetails();
            //         RefreshUI();
            //     }
            //     else
            //     {
            //         ShowNotification("Purchase failed!", NotificationType.Error);
            //     }
            // }
        }

        private void ShowBidDialog()
        {
            // Implementation for bid dialog
            // This would show a dialog for entering bid amount
        }

        private void ShowOfferDialog()
        {
            // Implementation for offer dialog
            // This would show a dialog for making an offer
        }

        private void CreateNewListing()
        {
            // Implementation for creating new listing
            // This would validate inputs and call marketplace.CreateListing()
        }

        private List<MarketplaceListing> GetFilteredListings()
        {
            // This would get listings from marketplace and apply filters
            // var allListings = marketplace.GetAllActiveListings();
            // return FilterListings(allListings, currentFilters);
            return new List<MarketplaceListing>(); // Placeholder
        }

        private List<MarketplaceListing> ApplyPagination(List<MarketplaceListing> listings)
        {
            totalPages = Mathf.CeilToInt((float)listings.Count / itemsPerPage);
            int startIndex = currentPage * itemsPerPage;
            return listings.Skip(startIndex).Take(itemsPerPage).ToList();
        }

        private void SetAllPanelsInactive()
        {
            if (browsePanel != null) browsePanel.SetActive(false);
            if (auctionsPanel != null) auctionsPanel.SetActive(false);
            if (analysisPanel != null) analysisPanel.SetActive(false);
            if (socialPanel != null) socialPanel.SetActive(false);
            if (createListingPanel != null) createListingPanel.SetActive(false);
            if (creatureDetailsPanel != null) creatureDetailsPanel.SetActive(false);
        }

        private void ClearListingItems()
        {
            foreach (var itemUI in currentListingUIs)
            {
                if (itemUI != null && itemUI.gameObject != null)
                    Destroy(itemUI.gameObject);
            }
            currentListingUIs.Clear();
        }

        private void ClearAuctionItems()
        {
            foreach (var itemUI in currentAuctionUIs)
            {
                if (itemUI != null && itemUI.gameObject != null)
                    Destroy(itemUI.gameObject);
            }
            currentAuctionUIs.Clear();
        }

        // Event handlers
        private void HandleListingCreated(MarketplaceListing listing)
        {
            if (currentTab == MarketplaceTab.BrowseListings)
            {
                RefreshListings();
            }

            ShowNotification($"New listing: {listing.creature.species} for {listing.askingPrice:C}", NotificationType.Info);
        }

        private void HandleTransactionCompleted(MarketTransaction transaction)
        {
            ShowNotification($"Transaction completed: {transaction.creature.species} sold for {transaction.price:C}", NotificationType.Success);
            RefreshUI();
        }

        private void HandleAuctionStarted(AuctionData auction)
        {
            ShowNotification($"New auction: {auction.creature.species} starting at {auction.startingBid:C}", NotificationType.Info);
            if (currentTab == MarketplaceTab.Auctions)
            {
                RefreshAuctions();
            }
        }

        private void HandleAuctionEnded(AuctionData auction)
        {
            string message = auction.status == AuctionStatus.Sold
                ? $"Auction ended: {auction.creature.species} sold for {auction.currentBid:C}"
                : $"Auction ended: {auction.creature.species} - no sale";

            ShowNotification(message, NotificationType.Info);
            RefreshUI();
        }

        private void OnSearchChanged(string searchText)
        {
            currentFilters.searchText = searchText;
            RefreshListings();
        }

        private void OnFiltersChanged(int value)
        {
            // Update filters based on dropdown changes
            RefreshListings();
        }

        private void OnSortChanged(int value)
        {
            // Update sort order and refresh
            RefreshListings();
        }

        // Helper methods
        private string GetCurrentPlayerId()
        {
            // This would get the current player's ID
            return "player123"; // Placeholder
        }

        private void ShowNotification(string message, NotificationType type)
        {
            if (notificationContainer == null || notificationPrefab == null) return;

            GameObject notificationObj = Instantiate(notificationPrefab, notificationContainer);
            MarketplaceNotificationUI notification = notificationObj.GetComponent<MarketplaceNotificationUI>();

            if (notification != null)
            {
                notification.Initialize(message, type);
            }
        }

        private string GenerateMarketStatsText(MarketStatistics stats)
        {
            return $"Market Overview:\n" +
                   $"Active Listings: {stats.totalActiveListings}\n" +
                   $"Active Auctions: {stats.totalActiveAuctions}\n" +
                   $"Total Volume: {stats.totalMarketVolume:C}\n" +
                   $"Daily Volume: {stats.dailyTransactionVolume:C}\n" +
                   $"Average Transaction: {stats.averageTransactionValue:C}\n" +
                   $"Active Guilds: {stats.activeGuilds}\n" +
                   $"Collaborative Projects: {stats.activeCollaborativeProjects}";
        }

        private void SetupNavigationButtons()
        {
            // Setup navigation button styling and states
        }

        private void SetupFiltersAndSearch()
        {
            // Initialize filter dropdowns with appropriate options
            if (speciesFilter != null)
            {
                var speciesOptions = System.Enum.GetNames(typeof(CreatureSpecies)).ToList();
                speciesOptions.Insert(0, "All Species");
                speciesFilter.ClearOptions();
                speciesFilter.AddOptions(speciesOptions);
            }

            if (sortByDropdown != null)
            {
                var sortOptions = new List<string> { "Price: Low to High", "Price: High to Low", "Newest First", "Ending Soon", "Most Popular" };
                sortByDropdown.ClearOptions();
                sortByDropdown.AddOptions(sortOptions);
            }
        }

        private void UpdateNavigationButtonStates()
        {
            // Update button colors/states to show current tab
        }

        private void UpdatePaginationInfo(int totalItems)
        {
            // Update pagination UI if implemented
        }

        private void RefreshSpeciesAnalysis()
        {
            // Implementation for species-specific market analysis
        }

        private void RefreshGuilds()
        {
            // Implementation for guild display
        }

        private void RefreshCollaborativeProjects()
        {
            // Implementation for collaborative project display
        }

        private void OnDestroy()
        {
            // Cleanup event listeners
            if (marketplace != null)
            {
                marketplace.OnListingCreated -= HandleListingCreated;
                marketplace.OnTransactionCompleted -= HandleTransactionCompleted;
                marketplace.OnAuctionStarted -= HandleAuctionStarted;
                marketplace.OnAuctionEnded -= HandleAuctionEnded;
            }
        }
    }

    // Supporting UI component classes
    public class ListingItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI creatureNameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI sellerText;
        [SerializeField] private Image creatureIcon;
        [SerializeField] private Button viewDetailsButton;

        private MarketplaceListing listing;
        private MarketplaceUI parentUI;

        public void Initialize(MarketplaceListing listing, MarketplaceUI parentUI)
        {
            this.listing = listing;
            this.parentUI = parentUI;

            if (creatureNameText != null)
                creatureNameText.text = listing.creature.species.ToString();

            if (priceText != null)
                priceText.text = listing.askingPrice.ToString("C");

            if (sellerText != null)
                sellerText.text = $"Seller: {listing.sellerName}";

            if (viewDetailsButton != null)
                viewDetailsButton.onClick.AddListener(() => parentUI.ShowCreatureDetails(listing.creature, listing));

            // Set creature icon based on species
            SetCreatureIcon(listing.creature.species);
        }

        private void SetCreatureIcon(CreatureSpecies species)
        {
            if (creatureIcon == null) return;

            // Set icon color based on species
            Color iconColor = species switch
            {
                CreatureSpecies.ForestBeast => Color.green,
                CreatureSpecies.DesertScorpion => Color.yellow,
                CreatureSpecies.ArcticWolf => Color.cyan,
                _ => Color.white
            };

            creatureIcon.color = iconColor;
        }
    }

    public class AuctionItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI creatureNameText;
        [SerializeField] private TextMeshProUGUI currentBidText;
        [SerializeField] private TextMeshProUGUI timeRemainingText;
        [SerializeField] private Button bidButton;

        public void Initialize(AuctionData auction, MarketplaceUI parentUI)
        {
            if (creatureNameText != null)
                creatureNameText.text = auction.creature.species.ToString();

            if (currentBidText != null)
                currentBidText.text = $"Current Bid: {auction.currentBid:C}";

            if (timeRemainingText != null)
            {
                float timeRemaining = auction.TimeRemaining;
                timeRemainingText.text = $"Time: {FormatTimeRemaining(timeRemaining)}";
            }

            if (bidButton != null)
                bidButton.onClick.AddListener(() => parentUI.ShowCreatureDetails(auction.creature));
        }

        private string FormatTimeRemaining(float seconds)
        {
            if (seconds <= 0) return "Ended";

            int hours = Mathf.FloorToInt(seconds / 3600);
            int minutes = Mathf.FloorToInt((seconds % 3600) / 60);

            if (hours > 0)
                return $"{hours}h {minutes}m";
            else
                return $"{minutes}m";
        }
    }

    public class CreatureDetailsUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI creatureNameText;
        [SerializeField] private TextMeshProUGUI geneticsText;
        [SerializeField] private TextMeshProUGUI traitsText;
        [SerializeField] private Image creatureImage;

        public void DisplayCreature(CreatureGenome creature)
        {
            if (creatureNameText != null)
                creatureNameText.text = creature.species.ToString();

            if (geneticsText != null)
                geneticsText.text = $"Generation: {creature.generation}\nFitness: {creature.fitness:F2}\nRarity: {creature.rarity:F2}";

            if (traitsText != null)
                traitsText.text = GenerateTraitsText(creature);

            // Set creature image/icon
            if (creatureImage != null)
            {
                // This would load the appropriate creature image
            }
        }

        private string GenerateTraitsText(CreatureGenome creature)
        {
            // Generate a formatted string of creature traits
            return "Traits: Strength, Agility, Intelligence"; // Placeholder
        }
    }

    public class MarketplaceNotificationUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private float displayDuration = 3f;

        public void Initialize(string message, NotificationType type)
        {
            if (messageText != null)
                messageText.text = message;

            if (backgroundImage != null)
            {
                Color backgroundColor = type switch
                {
                    NotificationType.Success => Color.green,
                    NotificationType.Error => Color.red,
                    NotificationType.Warning => Color.yellow,
                    NotificationType.Info => Color.blue,
                    _ => Color.white
                };

                backgroundImage.color = backgroundColor;
            }

            // Auto-destroy after duration
            Destroy(gameObject, displayDuration);
        }
    }

    // Supporting data structures
    [System.Serializable]
    public class MarketplaceFilters
    {
        public string searchText = "";
        public CreatureSpecies speciesFilter = CreatureSpecies.None;
        public Vector2 priceRange = new Vector2(0f, float.MaxValue);
        public float minRarity = 0f;
        public float maxRarity = 1f;
        public string sellerIdFilter = "";
        public bool showOnlyAvailable = true;
    }

    public enum MarketplaceTab
    {
        BrowseListings,
        Auctions,
        MyListings,
        MarketAnalysis,
        SocialFeatures
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    // Additional creature species enum (would be moved to appropriate location)
    public enum CreatureSpecies
    {
        None,
        ForestBeast,
        DesertScorpion,
        ArcticWolf,
        VolcanicDragon,
        DeepSeaLeviathan
    }
}