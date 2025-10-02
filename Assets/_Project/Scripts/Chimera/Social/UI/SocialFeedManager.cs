using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Unity.Entities;
using Unity.Collections;
using Laboratory.Chimera.Social.Core;
using Laboratory.Chimera.Discovery.Core;
using Laboratory.Chimera.Genetics.Core;

namespace Laboratory.Chimera.Social.UI
{
    /// <summary>
    /// Social feed manager displaying community discoveries and creatures
    /// Creates an engaging Instagram-like feed for genetic breakthroughs
    /// </summary>
    public class SocialFeedManager : MonoBehaviour
    {
        [Header("Feed UI")]
        [SerializeField] private ScrollRect _feedScrollRect;
        [SerializeField] private Transform _feedContainer;
        [SerializeField] private GameObject _shareCardPrefab;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _createPostButton;

        [Header("Feed Filters")]
        [SerializeField] private TMP_Dropdown _feedFilter;
        [SerializeField] private Button _trendingButton;
        [SerializeField] private Button _followingButton;
        [SerializeField] private Button _featuredButton;
        [SerializeField] private TMP_InputField _searchField;

        [Header("Quick Share")]
        [SerializeField] private GameObject _quickSharePanel;
        [SerializeField] private TMP_InputField _shareTitle;
        [SerializeField] private TMP_InputField _shareDescription;
        [SerializeField] private Button _shareCreatureButton;
        [SerializeField] private Button _shareDiscoveryButton;
        [SerializeField] private Button _cancelShareButton;

        [Header("Statistics")]
        [SerializeField] private TextMeshProUGUI _totalSharesText;
        [SerializeField] private TextMeshProUGUI _trendingCountText;
        [SerializeField] private TextMeshProUGUI _communityStatsText;

        [Header("Loading & Animation")]
        [SerializeField] private GameObject _loadingIndicator;
        [SerializeField] private float _cardAnimationDelay = 0.1f;
        [SerializeField] private AnimationCurve _cardAppearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private List<SocialShareData> _allShares = new List<SocialShareData>();
        private List<SocialShareData> _currentFeed = new List<SocialShareData>();
        private List<SocialShareCard> _activeCards = new List<SocialShareCard>();
        private FeedMode _currentFeedMode = FeedMode.All;
        private bool _isLoading = false;

        private static SocialFeedManager _instance;
        public static SocialFeedManager Instance => _instance;

        // Mock data for demonstration
        private List<string> _mockPlayerNames = new List<string>
        {
            "GeneticMaster", "BreedingExpert", "DragonLord", "CreatureCollector", "BioEngineer",
            "EvolutionGuru", "GeneWeaver", "SpeciesCreator", "MutationHunter", "DNAWizard"
        };

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupUI();
        }

        private void Start()
        {
            GenerateMockData();
            RefreshFeed();
        }

        /// <summary>
        /// Setup UI event handlers
        /// </summary>
        private void SetupUI()
        {
            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(RefreshFeed);

            if (_createPostButton != null)
                _createPostButton.onClick.AddListener(OpenQuickShare);

            if (_trendingButton != null)
                _trendingButton.onClick.AddListener(() => SetFeedMode(FeedMode.Trending));

            if (_followingButton != null)
                _followingButton.onClick.AddListener(() => SetFeedMode(FeedMode.Following));

            if (_featuredButton != null)
                _featuredButton.onClick.AddListener(() => SetFeedMode(FeedMode.Featured));

            if (_searchField != null)
                _searchField.onValueChanged.AddListener(OnSearchChanged);

            if (_shareCreatureButton != null)
                _shareCreatureButton.onClick.AddListener(ShareCreature);

            if (_shareDiscoveryButton != null)
                _shareDiscoveryButton.onClick.AddListener(ShareLastDiscovery);

            if (_cancelShareButton != null)
                _cancelShareButton.onClick.AddListener(CloseQuickShare);

            // Setup feed filter dropdown
            if (_feedFilter != null)
            {
                _feedFilter.ClearOptions();
                _feedFilter.AddOptions(new List<string> { "All Posts", "Discoveries", "Creatures", "Achievements", "New Species" });
                _feedFilter.onValueChanged.AddListener(OnFeedFilterChanged);
            }

            // Start with quick share closed
            if (_quickSharePanel != null)
                _quickSharePanel.SetActive(false);
        }

        /// <summary>
        /// Add a new share to the feed
        /// </summary>
        public void AddShare(SocialShareData shareData)
        {
            _allShares.Insert(0, shareData); // Add to beginning for chronological order

            if (ShouldShowInCurrentFeed(shareData))
            {
                _currentFeed.Insert(0, shareData);
                StartCoroutine(AnimateNewShare(shareData));
            }

            UpdateStatistics();
        }

        /// <summary>
        /// Refresh the entire feed
        /// </summary>
        public void RefreshFeed()
        {
            if (_isLoading) return;
            StartCoroutine(RefreshFeedCoroutine());
        }

        /// <summary>
        /// Refresh feed coroutine with loading animation
        /// </summary>
        private IEnumerator RefreshFeedCoroutine()
        {
            _isLoading = true;

            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(true);

            // Simulate network delay
            yield return new WaitForSeconds(0.5f);

            ClearFeed();
            ApplyCurrentFilter();
            yield return StartCoroutine(CreateFeedCards());
            UpdateStatistics();

            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(false);

            _isLoading = false;
        }

        /// <summary>
        /// Apply current feed mode and filters
        /// </summary>
        private void ApplyCurrentFilter()
        {
            _currentFeed.Clear();
            _currentFeed.AddRange(_allShares);

            // Apply feed mode filter
            _currentFeed = _currentFeedMode switch
            {
                FeedMode.Trending => _currentFeed.Where(s => s.IsTrending()).ToList(),
                FeedMode.Following => _currentFeed.Where(s => IsFollowing(s.PlayerName.ToString())).ToList(),
                FeedMode.Featured => _currentFeed.Where(s => s.IsFeatured).ToList(),
                _ => _currentFeed
            };

            // Apply dropdown filter
            if (_feedFilter != null && _feedFilter.value > 0)
            {
                _currentFeed = _feedFilter.value switch
                {
                    1 => _currentFeed.Where(s => s.Type == ShareType.Discovery || s.Type == ShareType.NewSpecies || s.Type == ShareType.RareMutation).ToList(),
                    2 => _currentFeed.Where(s => s.Type == ShareType.CreatureShowcase).ToList(),
                    3 => _currentFeed.Where(s => s.Type == ShareType.Achievement || s.Type == ShareType.PerfectGenetics || s.Type == ShareType.LegendaryLineage).ToList(),
                    4 => _currentFeed.Where(s => s.Type == ShareType.NewSpecies).ToList(),
                    _ => _currentFeed
                };
            }

            // Apply search filter
            if (_searchField != null && !string.IsNullOrEmpty(_searchField.text))
            {
                string searchTerm = _searchField.text.ToLower();
                _currentFeed = _currentFeed.Where(s =>
                    s.ShareTitle.ToString().ToLower().Contains(searchTerm) ||
                    s.ShareDescription.ToString().ToLower().Contains(searchTerm) ||
                    s.PlayerName.ToString().ToLower().Contains(searchTerm)
                ).ToList();
            }

            // Sort by engagement and recency
            _currentFeed = _currentFeed.OrderByDescending(s => s.PopularityScore + (Time.time - s.ShareTimestamp) * 0.1f).ToList();
        }

        /// <summary>
        /// Create feed cards for current feed
        /// </summary>
        private IEnumerator CreateFeedCards()
        {
            for (int i = 0; i < _currentFeed.Count; i++)
            {
                CreateShareCard(_currentFeed[i], i);

                // Stagger card creation for smooth animation
                if (i % 3 == 0) // Every 3 cards
                    yield return new WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// Create a single share card
        /// </summary>
        private void CreateShareCard(SocialShareData shareData, int index)
        {
            if (_shareCardPrefab == null || _feedContainer == null) return;

            GameObject cardGO = Instantiate(_shareCardPrefab, _feedContainer);
            SocialShareCard card = cardGO.GetComponent<SocialShareCard>();

            if (card != null)
            {
                card.SetupCard(shareData);
                _activeCards.Add(card);

                // Animate card appearance
                StartCoroutine(AnimateCardAppearance(card, index));
            }
        }

        /// <summary>
        /// Animate card appearance with staggered timing
        /// </summary>
        private IEnumerator AnimateCardAppearance(SocialShareCard card, int index)
        {
            if (card == null) yield break;

            // Wait for staggered appearance
            yield return new WaitForSeconds(index * _cardAnimationDelay);

            // Animate the card
            yield return StartCoroutine(card.AnimateAppearance(_cardAppearCurve));
        }

        /// <summary>
        /// Animate new share appearing at top of feed
        /// </summary>
        private IEnumerator AnimateNewShare(SocialShareData shareData)
        {
            // Create new card at top
            CreateShareCard(shareData, 0);

            // Shift existing cards down slightly
            for (int i = 1; i < _activeCards.Count; i++)
            {
                if (_activeCards[i] != null)
                {
                    StartCoroutine(_activeCards[i].AnimateShiftDown());
                }
            }

            yield return null;
        }

        /// <summary>
        /// Clear all feed cards
        /// </summary>
        private void ClearFeed()
        {
            _activeCards.Clear();

            if (_feedContainer != null)
            {
                foreach (Transform child in _feedContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Update statistics display
        /// </summary>
        private void UpdateStatistics()
        {
            if (_totalSharesText != null)
                _totalSharesText.text = $"Community Shares: {_allShares.Count:N0}";

            if (_trendingCountText != null)
            {
                int trendingCount = _allShares.Count(s => s.IsTrending());
                _trendingCountText.text = $"Trending: {trendingCount}";
            }

            if (_communityStatsText != null)
            {
                int totalLikes = _allShares.Sum(s => s.LikeCount);
                int totalComments = _allShares.Sum(s => s.CommentCount);
                _communityStatsText.text = $"💖 {totalLikes:N0} | 💬 {totalComments:N0}";
            }
        }

        /// <summary>
        /// Open quick share panel
        /// </summary>
        public void OpenQuickShare()
        {
            if (_quickSharePanel != null)
            {
                _quickSharePanel.SetActive(true);

                // Clear previous text
                if (_shareTitle != null) _shareTitle.text = "";
                if (_shareDescription != null) _shareDescription.text = "";
            }
        }

        /// <summary>
        /// Close quick share panel
        /// </summary>
        public void CloseQuickShare()
        {
            if (_quickSharePanel != null)
                _quickSharePanel.SetActive(false);
        }

        /// <summary>
        /// Share current creature
        /// </summary>
        public void ShareCreature()
        {
            // This would get the player's selected creature
            // For now, create mock data
            var mockGenetics = GenerateMockGenetics();
            string title = _shareTitle?.text ?? "Check out my amazing creature!";
            string description = _shareDescription?.text ?? "I'm so proud of this breeding achievement!";

            var shareData = SocialShareData.CreateFromCreature(
                default, // Would be actual creature entity
                mockGenetics,
                "Player", // Would be actual player name
                title,
                description
            );

            AddShare(shareData);
            CloseQuickShare();

            // Show confirmation
            Debug.Log("Creature shared to community feed!");
        }

        /// <summary>
        /// Share last discovery
        /// </summary>
        public void ShareLastDiscovery()
        {
            // This would get the player's last discovery
            // For now, create mock discovery
            var mockDiscovery = GenerateMockDiscovery();

            var shareData = SocialShareData.CreateFromDiscovery(
                mockDiscovery,
                "Player" // Would be actual player name
            );

            AddShare(shareData);
            CloseQuickShare();

            // Show confirmation
            Debug.Log("Discovery shared to community feed!");
        }

        /// <summary>
        /// Set feed mode (All, Trending, Following, Featured)
        /// </summary>
        private void SetFeedMode(FeedMode mode)
        {
            _currentFeedMode = mode;
            RefreshFeed();

            // Update button states
            UpdateFeedModeButtons();
        }

        /// <summary>
        /// Update feed mode button visual states
        /// </summary>
        private void UpdateFeedModeButtons()
        {
            // This would update button colors/states to show active mode
            // Implementation would depend on specific UI design
        }

        /// <summary>
        /// Check if share should show in current feed
        /// </summary>
        private bool ShouldShowInCurrentFeed(SocialShareData shareData)
        {
            return _currentFeedMode switch
            {
                FeedMode.Trending => shareData.IsTrending(),
                FeedMode.Following => IsFollowing(shareData.PlayerName.ToString()),
                FeedMode.Featured => shareData.IsFeatured,
                _ => true
            };
        }

        /// <summary>
        /// Check if following a player (mock implementation)
        /// </summary>
        private bool IsFollowing(string playerName)
        {
            // Mock: follow random selection of players
            return playerName.GetHashCode() % 3 == 0;
        }

        /// <summary>
        /// Event handlers
        /// </summary>
        private void OnSearchChanged(string searchText)
        {
            RefreshFeed();
        }

        private void OnFeedFilterChanged(int filterIndex)
        {
            RefreshFeed();
        }

        /// <summary>
        /// Generate mock data for demonstration
        /// </summary>
        private void GenerateMockData()
        {
            // Generate some mock shares to populate the feed
            for (int i = 0; i < 15; i++)
            {
                if (Random.value > 0.6f)
                {
                    // Mock discovery share
                    var mockDiscovery = GenerateMockDiscovery();
                    var shareData = SocialShareData.CreateFromDiscovery(
                        mockDiscovery,
                        _mockPlayerNames[Random.Range(0, _mockPlayerNames.Count)]
                    );

                    // Add some mock engagement
                    shareData.UpdateEngagement(
                        Random.Range(0, 50),
                        Random.Range(0, 10),
                        Random.Range(0, 5)
                    );

                    _allShares.Add(shareData);
                }
                else
                {
                    // Mock creature share
                    var mockGenetics = GenerateMockGenetics();
                    var shareData = SocialShareData.CreateFromCreature(
                        default,
                        mockGenetics,
                        _mockPlayerNames[Random.Range(0, _mockPlayerNames.Count)],
                        GenerateMockCreatureTitle(),
                        GenerateMockCreatureDescription()
                    );

                    // Add some mock engagement
                    shareData.UpdateEngagement(
                        Random.Range(0, 30),
                        Random.Range(0, 8),
                        Random.Range(0, 3)
                    );

                    _allShares.Add(shareData);
                }
            }

            // Sort by timestamp (newest first)
            _allShares = _allShares.OrderByDescending(s => s.ShareTimestamp).ToList();
        }

        private DiscoveryEvent GenerateMockDiscovery()
        {
            var rarities = System.Enum.GetValues(typeof(DiscoveryRarity));
            var types = System.Enum.GetValues(typeof(DiscoveryType));

            var rarity = (DiscoveryRarity)rarities.GetValue(Random.Range(0, rarities.Length));
            var type = (DiscoveryType)types.GetValue(Random.Range(0, types.Length));

            return new DiscoveryEvent
            {
                Type = type,
                Rarity = rarity,
                DiscoveredGenetics = GenerateMockGenetics(),
                SignificanceScore = Random.Range(50f, 500f),
                IsWorldFirst = Random.value > 0.9f,
                IsFirstTimeDiscovery = Random.value > 0.7f,
                SpecialMarkers = (GeneticMarkerFlags)Random.Range(0, 8),
                DiscoveryName = new FixedString64Bytes(GenerateMockDiscoveryName(type, rarity))
            };
        }

        private VisualGeneticData GenerateMockGenetics()
        {
            return new VisualGeneticData
            {
                Strength = (byte)Random.Range(20, 100),
                Vitality = (byte)Random.Range(20, 100),
                Agility = (byte)Random.Range(20, 100),
                Intelligence = (byte)Random.Range(20, 100),
                Adaptability = (byte)Random.Range(20, 100),
                Social = (byte)Random.Range(20, 100),
                SpecialMarkers = (GeneticMarkerFlags)Random.Range(0, 8)
            };
        }

        private string GenerateMockDiscoveryName(DiscoveryType type, DiscoveryRarity rarity)
        {
            string[] prefixes = { "Azure", "Crimson", "Golden", "Shadow", "Crystal", "Storm", "Flame", "Frost" };
            string[] suffixes = { "Drakon", "Chimera", "Phoenix", "Leviathan", "Griffin", "Basilisk", "Wyrm", "Hydra" };

            return $"{prefixes[Random.Range(0, prefixes.Length)]} {suffixes[Random.Range(0, suffixes.Length)]}";
        }

        private string GenerateMockCreatureTitle()
        {
            string[] adjectives = { "Magnificent", "Rare", "Beautiful", "Powerful", "Unique", "Stunning", "Perfect", "Amazing" };
            string[] creatures = { "Drakon", "Chimera", "Phoenix", "Griffin", "Basilisk", "Wyrm", "Leviathan", "Hydra" };

            return $"My {adjectives[Random.Range(0, adjectives.Length)]} {creatures[Random.Range(0, creatures.Length)]}";
        }

        private string GenerateMockCreatureDescription()
        {
            string[] descriptions = {
                "After months of careful breeding, this beauty finally emerged!",
                "Check out these incredible genetics - I'm so proud!",
                "This creature has exceeded all my expectations!",
                "The perfect combination of power and grace!",
                "Rare genetic markers make this one truly special!",
                "My breeding program's greatest achievement!",
                "Look at those stats! Absolutely incredible!",
                "This bloodline is going places!"
            };

            return descriptions[Random.Range(0, descriptions.Length)];
        }

        /// <summary>
        /// Public API for external systems
        /// </summary>
        public static void ShareDiscovery(DiscoveryEvent discovery, string playerName)
        {
            if (Instance != null)
            {
                var shareData = SocialShareData.CreateFromDiscovery(discovery, playerName);
                Instance.AddShare(shareData);
            }
        }

        public static void ShareCreature(Entity creature, VisualGeneticData genetics, string playerName, string title = "", string description = "")
        {
            if (Instance != null)
            {
                var shareData = SocialShareData.CreateFromCreature(creature, genetics, playerName, title, description);
                Instance.AddShare(shareData);
            }
        }

        public static void OpenFeed()
        {
            if (Instance != null)
            {
                Instance.gameObject.SetActive(true);
                Instance.RefreshFeed();
            }
        }
    }

    /// <summary>
    /// Feed viewing modes
    /// </summary>
    public enum FeedMode
    {
        All,
        Trending,
        Following,
        Featured
    }
}