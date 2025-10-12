using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Discovery.Core;
using Laboratory.Chimera.Genetics.Core;
using CoreDiscoveryEvent = Laboratory.Chimera.Discovery.Core.DiscoveryEvent;
using CoreDiscoveryType = Laboratory.Chimera.Discovery.Core.DiscoveryType;

namespace Laboratory.Chimera.Discovery.UI
{
    /// <summary>
    /// Discovery journal that tracks all genetic breakthroughs
    /// Creates a beautiful portfolio of achievements and rare discoveries
    /// </summary>
    public class DiscoveryJournal : MonoBehaviour
    {
        [Header("Journal UI")]
        [SerializeField] private ScrollRect _journalScrollRect;
        [SerializeField] private Transform _entryContainer;
        [SerializeField] private GameObject _journalEntryPrefab;
        [SerializeField] private Button _journalToggleButton;
        [SerializeField] private Canvas _journalCanvas;

        [Header("Statistics")]
        [SerializeField] private TextMeshProUGUI _totalDiscoveriesText;
        [SerializeField] private TextMeshProUGUI _rarityBreakdownText;
        [SerializeField] private TextMeshProUGUI _significanceScoreText;
        [SerializeField] private Image _progressBar;

        [Header("Filtering")]
        [SerializeField] private TMP_Dropdown _rarityFilter;
        [SerializeField] private TMP_Dropdown _typeFilter;
        [SerializeField] private TMP_InputField _searchField;
        [SerializeField] private Button _sortByDateButton;
        [SerializeField] private Button _sortByRarityButton;

        [Header("Animation")]
        [SerializeField] private float _entryAnimationDelay = 0.1f;
        [SerializeField] private AnimationCurve _entryAppearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private List<CoreDiscoveryEvent> _allDiscoveries = new List<CoreDiscoveryEvent>();
        private List<CoreDiscoveryEvent> _filteredDiscoveries = new List<CoreDiscoveryEvent>();
        private List<DiscoveryJournalEntry> _entryComponents = new List<DiscoveryJournalEntry>();
        private bool _isJournalOpen = false;
        private SortMode _currentSortMode = SortMode.DateDescending;

        private static DiscoveryJournal _instance;
        public static DiscoveryJournal Instance => _instance;

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
            UpdateJournalDisplay();
        }

        /// <summary>
        /// Setup UI event handlers
        /// </summary>
        private void SetupUI()
        {
            if (_journalToggleButton != null)
                _journalToggleButton.onClick.AddListener(ToggleJournal);

            if (_rarityFilter != null)
                _rarityFilter.onValueChanged.AddListener(OnFilterChanged);

            if (_typeFilter != null)
                _typeFilter.onValueChanged.AddListener(OnFilterChanged);

            if (_searchField != null)
                _searchField.onValueChanged.AddListener(OnSearchChanged);

            if (_sortByDateButton != null)
                _sortByDateButton.onClick.AddListener(() => SetSortMode(SortMode.DateDescending));

            if (_sortByRarityButton != null)
                _sortByRarityButton.onClick.AddListener(() => SetSortMode(SortMode.RarityDescending));

            // Initialize filters
            SetupFilterDropdowns();

            // Start with journal closed
            if (_journalCanvas != null)
                _journalCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// Setup filter dropdown options
        /// </summary>
        private void SetupFilterDropdowns()
        {
            if (_rarityFilter != null)
            {
                _rarityFilter.ClearOptions();
                var rarityOptions = new List<string> { "All Rarities" };
                rarityOptions.AddRange(System.Enum.GetNames(typeof(DiscoveryRarity)));
                _rarityFilter.AddOptions(rarityOptions);
            }

            if (_typeFilter != null)
            {
                _typeFilter.ClearOptions();
                var typeOptions = new List<string> { "All Types" };
                typeOptions.AddRange(System.Enum.GetNames(typeof(CoreDiscoveryType)));
                _typeFilter.AddOptions(typeOptions);
            }
        }

        /// <summary>
        /// Add a new discovery to the journal
        /// </summary>
        public void AddDiscovery(CoreDiscoveryEvent discovery)
        {
            _allDiscoveries.Add(discovery);
            ApplyFiltersAndSort();
            UpdateStatistics();

            // If journal is open, animate in the new entry
            if (_isJournalOpen)
            {
                StartCoroutine(AnimateNewEntry(discovery));
            }
        }

        /// <summary>
        /// Toggle journal visibility
        /// </summary>
        public void ToggleJournal()
        {
            _isJournalOpen = !_isJournalOpen;

            if (_journalCanvas != null)
            {
                _journalCanvas.gameObject.SetActive(_isJournalOpen);

                if (_isJournalOpen)
                {
                    UpdateJournalDisplay();
                }
            }
        }

        /// <summary>
        /// Update the complete journal display
        /// </summary>
        private void UpdateJournalDisplay()
        {
            ClearEntries();
            ApplyFiltersAndSort();
            CreateEntries();
            UpdateStatistics();
        }

        /// <summary>
        /// Apply current filters and sorting
        /// </summary>
        private void ApplyFiltersAndSort()
        {
            _filteredDiscoveries.Clear();
            _filteredDiscoveries.AddRange(_allDiscoveries);

            // Apply rarity filter
            if (_rarityFilter != null && _rarityFilter.value > 0)
            {
                var targetRarity = (DiscoveryRarity)(_rarityFilter.value - 1);
                _filteredDiscoveries = _filteredDiscoveries.Where(d => d.Rarity == targetRarity).ToList();
            }

            // Apply type filter
            if (_typeFilter != null && _typeFilter.value > 0)
            {
                var targetType = (CoreDiscoveryType)(_typeFilter.value - 1);
                _filteredDiscoveries = _filteredDiscoveries.Where(d => d.Type == targetType).ToList();
            }

            // Apply search filter
            if (_searchField != null && !string.IsNullOrEmpty(_searchField.text))
            {
                string searchTerm = _searchField.text.ToLower();
                _filteredDiscoveries = _filteredDiscoveries.Where(d =>
                    d.DiscoveryName.ToString().ToLower().Contains(searchTerm) ||
                    d.DiscoveryDescription.ToString().ToLower().Contains(searchTerm)
                ).ToList();
            }

            // Apply sorting
            _filteredDiscoveries = _currentSortMode switch
            {
                SortMode.DateAscending => _filteredDiscoveries.OrderBy(d => d.DiscoveryTimestamp).ToList(),
                SortMode.DateDescending => _filteredDiscoveries.OrderByDescending(d => d.DiscoveryTimestamp).ToList(),
                SortMode.RarityAscending => _filteredDiscoveries.OrderBy(d => (int)d.Rarity).ToList(),
                SortMode.RarityDescending => _filteredDiscoveries.OrderByDescending(d => (int)d.Rarity).ToList(),
                SortMode.SignificanceAscending => _filteredDiscoveries.OrderBy(d => d.SignificanceScore).ToList(),
                SortMode.SignificanceDescending => _filteredDiscoveries.OrderByDescending(d => d.SignificanceScore).ToList(),
                _ => _filteredDiscoveries
            };
        }

        /// <summary>
        /// Create journal entries for filtered discoveries
        /// </summary>
        private void CreateEntries()
        {
            for (int i = 0; i < _filteredDiscoveries.Count; i++)
            {
                CreateJournalEntry(_filteredDiscoveries[i], i);
            }
        }

        /// <summary>
        /// Create a single journal entry
        /// </summary>
        private void CreateJournalEntry(CoreDiscoveryEvent discovery, int index)
        {
            if (_journalEntryPrefab == null || _entryContainer == null) return;

            GameObject entryGO = Instantiate(_journalEntryPrefab, _entryContainer);
            DiscoveryJournalEntry entry = entryGO.GetComponent<DiscoveryJournalEntry>();

            if (entry != null)
            {
                entry.SetupEntry(discovery);
                _entryComponents.Add(entry);

                // Animate entry appearance
                if (_isJournalOpen)
                {
                    StartCoroutine(AnimateEntryAppearance(entry, index));
                }
            }
        }

        /// <summary>
        /// Clear all journal entries
        /// </summary>
        private void ClearEntries()
        {
            _entryComponents.Clear();

            if (_entryContainer != null)
            {
                foreach (Transform child in _entryContainer)
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
            if (_totalDiscoveriesText != null)
                _totalDiscoveriesText.text = $"Total Discoveries: {_allDiscoveries.Count}";

            UpdateRarityBreakdown();
            UpdateSignificanceScore();
            UpdateProgressBar();
        }

        /// <summary>
        /// Update rarity breakdown text
        /// </summary>
        private void UpdateRarityBreakdown()
        {
            if (_rarityBreakdownText == null) return;

            var breakdown = _allDiscoveries.GroupBy(d => d.Rarity)
                .ToDictionary(g => g.Key, g => g.Count());

            var rarityText = new List<string>();
            foreach (DiscoveryRarity rarity in System.Enum.GetValues(typeof(DiscoveryRarity)))
            {
                int count = breakdown.GetValueOrDefault(rarity, 0);
                if (count > 0)
                {
                    rarityText.Add($"{rarity}: {count}");
                }
            }

            _rarityBreakdownText.text = string.Join(" | ", rarityText);
        }

        /// <summary>
        /// Update total significance score
        /// </summary>
        private void UpdateSignificanceScore()
        {
            if (_significanceScoreText == null) return;

            float totalSignificance = _allDiscoveries.Sum(d => d.SignificanceScore);
            _significanceScoreText.text = $"Research Score: {totalSignificance:F0}";
        }

        /// <summary>
        /// Update progress bar based on discoveries
        /// </summary>
        private void UpdateProgressBar()
        {
            if (_progressBar == null) return;

            // Progress based on rarity milestones
            int milestones = 0;
            var rarityGroups = _allDiscoveries.GroupBy(d => d.Rarity).ToDictionary(g => g.Key, g => g.Count());

            if (rarityGroups.GetValueOrDefault(DiscoveryRarity.Common, 0) >= 10) milestones++;
            if (rarityGroups.GetValueOrDefault(DiscoveryRarity.Uncommon, 0) >= 5) milestones++;
            if (rarityGroups.GetValueOrDefault(DiscoveryRarity.Rare, 0) >= 3) milestones++;
            if (rarityGroups.GetValueOrDefault(DiscoveryRarity.Epic, 0) >= 2) milestones++;
            if (rarityGroups.GetValueOrDefault(DiscoveryRarity.Legendary, 0) >= 1) milestones++;
            if (rarityGroups.GetValueOrDefault(DiscoveryRarity.Mythical, 0) >= 1) milestones++;

            _progressBar.fillAmount = milestones / 6f;
        }

        /// <summary>
        /// Animate new entry appearance
        /// </summary>
        private System.Collections.IEnumerator AnimateNewEntry(CoreDiscoveryEvent discovery)
        {
            // Find the entry for this discovery
            var entry = _entryComponents.LastOrDefault();
            if (entry != null)
            {
                yield return StartCoroutine(AnimateEntryAppearance(entry, 0));
            }
        }

        /// <summary>
        /// Animate entry appearance with staggered timing
        /// </summary>
        private System.Collections.IEnumerator AnimateEntryAppearance(DiscoveryJournalEntry entry, int index)
        {
            if (entry == null) yield break;

            // Wait for staggered appearance
            yield return new WaitForSeconds(index * _entryAnimationDelay);

            // Animate the entry
            yield return StartCoroutine(entry.AnimateAppearance(_entryAppearCurve));
        }

        /// <summary>
        /// Event handlers
        /// </summary>
        private void OnFilterChanged(int value)
        {
            UpdateJournalDisplay();
        }

        private void OnSearchChanged(string searchText)
        {
            UpdateJournalDisplay();
        }

        private void SetSortMode(SortMode sortMode)
        {
            _currentSortMode = sortMode;
            UpdateJournalDisplay();
        }

        /// <summary>
        /// Public API for external systems
        /// </summary>
        public static void RecordDiscovery(CoreDiscoveryEvent discovery)
        {
            if (Instance != null)
            {
                Instance.AddDiscovery(discovery);
            }
        }

        public static void OpenJournal()
        {
            if (Instance != null && !Instance._isJournalOpen)
            {
                Instance.ToggleJournal();
            }
        }

        public static int GetDiscoveryCount()
        {
            return Instance?._allDiscoveries.Count ?? 0;
        }

        public static float GetTotalSignificance()
        {
            return Instance?._allDiscoveries.Sum(d => d.SignificanceScore) ?? 0f;
        }
    }

    /// <summary>
    /// Sorting modes for journal entries
    /// </summary>
    public enum SortMode
    {
        DateAscending,
        DateDescending,
        RarityAscending,
        RarityDescending,
        SignificanceAscending,
        SignificanceDescending
    }
}