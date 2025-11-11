using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.UI.Components;
using Laboratory.Core.Events;
using Laboratory.Chimera.Core;
using Laboratory.Core.Enums;
using Laboratory.Core.Infrastructure;
using Laboratory.Shared.Types;
using BreedingHistoryEntry = Laboratory.Chimera.UI.Components.BreedingHistoryEntry;

namespace Laboratory.Chimera.UI
{
    /// <summary>
    /// Comprehensive breeding history tracking and display system.
    /// Tracks all breeding operations, success rates, lineage trees, and provides
    /// detailed analytics for player breeding strategies.
    /// </summary>
    public class BreedingHistoryManager : MonoBehaviour
    {
        [Header("History UI")]
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private Transform historyContainer;
        [SerializeField] private GameObject historyEntryPrefab;
        [SerializeField] private ScrollRect historyScrollView;
        [SerializeField] private TextMeshProUGUI historyStatsText;
        
        [Header("Filtering & Sorting")]
        [SerializeField] private TMP_InputField searchHistoryInput;
        [SerializeField] private Dropdown historyFilterDropdown;
        [SerializeField] private Dropdown historySortDropdown;
        [SerializeField] private Toggle showSuccessOnlyToggle;
        [SerializeField] private Toggle showFavoritesOnlyToggle;
        
        [Header("Analytics Display")]
        [SerializeField] private TextMeshProUGUI totalBreedingsText;
        [SerializeField] private TextMeshProUGUI successRateText;
        [SerializeField] private TextMeshProUGUI averageGenerationText;
        [SerializeField] private TextMeshProUGUI bestPurityText;
        [SerializeField] private TextMeshProUGUI favoriteLineageText;
        [SerializeField] private Transform analyticsChartsContainer;
        
        [Header("Lineage Tree")]
        [SerializeField] private GameObject lineagePanel;
        [SerializeField] private Transform lineageTreeContainer;
        [SerializeField] private GameObject lineageNodePrefab;
        [SerializeField] private Button showLineageButton;
        
        [Header("Settings")]
        [SerializeField] private int maxHistoryEntries = 1000;
        [SerializeField] private bool autoSaveHistory = true;
        [SerializeField] private float autoSaveInterval = 60f;
        [SerializeField] private KeyCode toggleHistoryKey = KeyCode.H;
        
        // History Data
        private List<BreedingHistoryEntry> breedingHistory = new List<BreedingHistoryEntry>();
        private List<BreedingHistoryEntry> filteredHistory = new List<BreedingHistoryEntry>();
        private BreedingHistoryEntry selectedEntry;
        
        // Analytics Data
        private BreedingAnalytics currentAnalytics;
        
        // UI State
        private bool isHistoryVisible = false;
        private string currentSearchTerm = "";
        private HistoryFilterType currentFilter = HistoryFilterType.All;
        private HistorySortType currentSort = HistorySortType.DateNewest;
        
        // Events
        private IEventBus eventBus;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeHistoryUI();
            InitializeEventBus();
            LoadBreedingHistory();
            
            if (autoSaveHistory)
                InvokeRepeating(nameof(AutoSaveHistory), autoSaveInterval, autoSaveInterval);
                
            // Listen for breeding events
            SubscribeToBreedingEvents();
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        private void OnDestroy()
        {
            SaveBreedingHistory();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeHistoryUI()
        {
            SetupHistoryEventHandlers();
            HideAllPanels();
            UpdateHistoryDisplay();
        }
        
        private void SetupHistoryEventHandlers()
        {
            if (searchHistoryInput != null)
                searchHistoryInput.onValueChanged.AddListener(OnHistorySearchChanged);
                
            if (historyFilterDropdown != null)
                historyFilterDropdown.onValueChanged.AddListener(OnHistoryFilterChanged);
                
            if (historySortDropdown != null)
                historySortDropdown.onValueChanged.AddListener(OnHistorySortChanged);
                
            if (showSuccessOnlyToggle != null)
                showSuccessOnlyToggle.onValueChanged.AddListener(OnShowSuccessOnlyChanged);
                
            if (showFavoritesOnlyToggle != null)
                showFavoritesOnlyToggle.onValueChanged.AddListener(OnShowFavoritesOnlyChanged);
                
            if (showLineageButton != null)
                showLineageButton.onClick.AddListener(ShowLineageTree);
        }
        
        private void InitializeEventBus()
        {
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                eventBus = serviceContainer.ResolveService<IEventBus>();
            }
        }
        
        private void SubscribeToBreedingEvents()
        {
            // Subscribe to breeding completion events
            if (eventBus != null)
            {
                // eventBus.Subscribe<BreedingCompleteEvent>(OnBreedingCompleted);
                // eventBus.Subscribe<BreedingFailedEvent>(OnBreedingFailed);
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(toggleHistoryKey))
            {
                ToggleHistoryUI();
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Show the breeding history UI
        /// </summary>
        public void ShowHistoryUI()
        {
            if (historyPanel != null)
                historyPanel.SetActive(true);
            isHistoryVisible = true;
            
            RefreshHistoryDisplay();
            UpdateAnalytics();
            
            UnityEngine.Debug.Log("📚 Breeding history UI opened");
        }
        
        /// <summary>
        /// Hide the breeding history UI
        /// </summary>
        public void HideHistoryUI()
        {
            if (historyPanel != null)
                historyPanel.SetActive(false);
            isHistoryVisible = false;
            
            UnityEngine.Debug.Log("📚 Breeding history UI closed");
        }
        
        /// <summary>
        /// Toggle history UI visibility
        /// </summary>
        public void ToggleHistoryUI()
        {
            if (isHistoryVisible)
                HideHistoryUI();
            else
                ShowHistoryUI();
        }
        
        /// <summary>
        /// Record a successful breeding operation
        /// </summary>
        public void RecordBreedingSuccess(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2, 
            CreatureInstanceComponent offspring, float breedingTime)
        {
            var entry = new BreedingHistoryEntry
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Now,
                Parent1Name = parent1.name,
                Parent2Name = parent2.name,
                Parent1Id = parent1.CreatureData?.UniqueId ?? string.Empty,
                Parent1Guid = Guid.TryParse(parent1.CreatureData?.UniqueId, out var p1Id) ? p1Id : Guid.Empty,
                Parent2Id = parent2.CreatureData?.UniqueId ?? string.Empty,
                Parent2Guid = Guid.TryParse(parent2.CreatureData?.UniqueId, out var p2Id) ? p2Id : Guid.Empty,
                OffspringName = offspring.name,
                OffspringId = offspring.CreatureData?.UniqueId ?? string.Empty,
                OffspringGuid = Guid.TryParse(offspring.CreatureData?.UniqueId, out var oId) ? oId : Guid.Empty,
                WasSuccessful = true,
                BreedingTimeSeconds = breedingTime,
                OffspringGeneration = offspring.CreatureData?.GeneticProfile?.Generation ?? 1,
                OffspringPurity = offspring.CreatureData?.GeneticProfile?.GetGeneticPurity() ?? 0f,
                MutationCount = offspring.CreatureData?.GeneticProfile?.Mutations.Count ?? 0,
                BiomeType = GetCurrentBiome(),
                IsFavorite = false
            };
            
            // Extract detailed genetic info
            if (offspring.CreatureData?.GeneticProfile != null)
            {
                entry.OffspringTraits = offspring.CreatureData.GeneticProfile.GetTraitSummary(5);
                entry.HasRareTraits = offspring.CreatureData.GeneticProfile.Genes
                    .Any(g => g.isActive && g.value.HasValue && g.value.Value > 0.9f);
                entry.HasMagicalTraits = offspring.CreatureData.GeneticProfile.Genes
                    .Any(g => g.traitType.GetCategory() == TraitCategory.Special && g.isActive);
            }
            
            AddHistoryEntry(entry);
            
            UnityEngine.Debug.Log($"📝 Recorded successful breeding: {parent1.name} + {parent2.name} = {offspring.name}");
        }
        
        /// <summary>
        /// Record a failed breeding operation
        /// </summary>
        public void RecordBreedingFailure(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2, 
            string failureReason, float breedingTime)
        {
            var entry = new BreedingHistoryEntry
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Now,
                Parent1Name = parent1.name,
                Parent2Name = parent2.name,
                Parent1Id = parent1.CreatureData?.UniqueId ?? string.Empty,
                Parent1Guid = Guid.TryParse(parent1.CreatureData?.UniqueId, out var p1Id) ? p1Id : Guid.Empty,
                Parent2Id = parent2.CreatureData?.UniqueId ?? string.Empty,
                Parent2Guid = Guid.TryParse(parent2.CreatureData?.UniqueId, out var p2Id) ? p2Id : Guid.Empty,
                WasSuccessful = false,
                FailureReason = failureReason,
                BreedingTimeSeconds = breedingTime,
                BiomeType = GetCurrentBiome(),
                IsFavorite = false
            };
            
            AddHistoryEntry(entry);
            
            UnityEngine.Debug.Log($"❌ Recorded failed breeding: {parent1.name} + {parent2.name} - {failureReason}");
        }
        
        /// <summary>
        /// Get breeding success rate for specific creature
        /// </summary>
        public float GetCreatureSuccessRate(Guid creatureId)
        {
            var creatureBreedings = breedingHistory.Where(h =>
                h.Parent1Guid == creatureId || h.Parent2Guid == creatureId).ToList();

            if (creatureBreedings.Count == 0) return 0f;

            int successful = creatureBreedings.Count(b => b.WasSuccessful);
            return (float)successful / creatureBreedings.Count;
        }
        
        /// <summary>
        /// Get recommended breeding partners for a creature
        /// </summary>
        public List<Guid> GetRecommendedPartners(Guid creatureId)
        {
            var successfulBreedings = breedingHistory.Where(h =>
                h.WasSuccessful && (h.Parent1Guid == creatureId || h.Parent2Guid == creatureId)).ToList();

            var partnerIds = new List<Guid>();
            foreach (var breeding in successfulBreedings)
            {
                Guid partnerId = (breeding.Parent1Guid == creatureId) ? breeding.Parent2Guid : breeding.Parent1Guid;
                if (!partnerIds.Contains(partnerId))
                {
                    partnerIds.Add(partnerId);
                }
            }
            
            return partnerIds.OrderBy(id => GetCreatureSuccessRate(id)).Take(5).ToList();
        }
        
        #endregion
        
        #region History Management
        
        private void AddHistoryEntry(BreedingHistoryEntry entry)
        {
            breedingHistory.Insert(0, entry); // Add to beginning for newest-first order
            
            // Limit history size
            if (breedingHistory.Count > maxHistoryEntries)
            {
                breedingHistory.RemoveAt(breedingHistory.Count - 1);
            }
            
            if (isHistoryVisible)
            {
                RefreshHistoryDisplay();
                UpdateAnalytics();
            }
        }

        private void UpdateHistoryDisplay()
        {
            RefreshHistoryDisplay();
        }

        private void RefreshHistoryDisplay()
        {
            ApplyHistoryFiltersAndSort();
            PopulateHistoryEntries();
            UpdateHistoryStats();
        }
        
        private void ApplyHistoryFiltersAndSort()
        {
            // Start with all history
            filteredHistory = new List<BreedingHistoryEntry>(breedingHistory);
            
            // Apply search filter
            if (!string.IsNullOrEmpty(currentSearchTerm))
            {
                filteredHistory = filteredHistory.Where(h => 
                    h.Parent1Name.ToLower().Contains(currentSearchTerm.ToLower()) ||
                    h.Parent2Name.ToLower().Contains(currentSearchTerm.ToLower()) ||
                    h.OffspringName.ToLower().Contains(currentSearchTerm.ToLower())
                ).ToList();
            }
            
            // Apply category filter
            filteredHistory = ApplyHistoryFilter(filteredHistory);
            
            // Apply toggle filters
            if (showSuccessOnlyToggle?.isOn == true)
            {
                filteredHistory = filteredHistory.Where(h => h.WasSuccessful).ToList();
            }
            
            if (showFavoritesOnlyToggle?.isOn == true)
            {
                filteredHistory = filteredHistory.Where(h => h.IsFavorite).ToList();
            }
            
            // Apply sorting
            filteredHistory = ApplyHistorySort(filteredHistory);
        }
        
        private List<BreedingHistoryEntry> ApplyHistoryFilter(List<BreedingHistoryEntry> entries)
        {
            switch (currentFilter)
            {
                case HistoryFilterType.Successful:
                    return entries.Where(e => e.WasSuccessful).ToList();
                case HistoryFilterType.Failed:
                    return entries.Where(e => !e.WasSuccessful).ToList();
                case HistoryFilterType.HighGeneration:
                    return entries.Where(e => e.OffspringGeneration >= 3).ToList();
                case HistoryFilterType.HighPurity:
                    return entries.Where(e => e.OffspringPurity > 0.8f).ToList();
                case HistoryFilterType.WithMutations:
                    return entries.Where(e => e.MutationCount > 0).ToList();
                case HistoryFilterType.RareTraits:
                    return entries.Where(e => e.HasRareTraits).ToList();
                case HistoryFilterType.MagicalTraits:
                    return entries.Where(e => e.HasMagicalTraits).ToList();
                default:
                    return entries;
            }
        }
        
        private List<BreedingHistoryEntry> ApplyHistorySort(List<BreedingHistoryEntry> entries)
        {
            switch (currentSort)
            {
                case HistorySortType.DateNewest:
                    return entries.OrderByDescending(e => e.Date).ToList();
                case HistorySortType.DateOldest:
                    return entries.OrderBy(e => e.Date).ToList();
                case HistorySortType.Generation:
                    return entries.OrderByDescending(e => e.OffspringGeneration).ToList();
                case HistorySortType.Purity:
                    return entries.OrderByDescending(e => e.OffspringPurity).ToList();
                case HistorySortType.BreedingTime:
                    return entries.OrderBy(e => e.BreedingTimeSeconds).ToList();
                case HistorySortType.SuccessFirst:
                    return entries.OrderByDescending(e => e.WasSuccessful).ThenByDescending(e => e.Date).ToList();
                default:
                    return entries;
            }
        }
        
        private void PopulateHistoryEntries()
        {
            if (historyContainer == null || historyEntryPrefab == null) return;
            
            // Clear existing entries
            for (int i = historyContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(historyContainer.GetChild(i).gameObject);
            }
            
            // Create entries for filtered history
            foreach (var entry in filteredHistory)
            {
                CreateHistoryEntryDisplay(entry);
            }
        }
        
        private void CreateHistoryEntryDisplay(BreedingHistoryEntry entry)
        {
            var entryObject = Instantiate(historyEntryPrefab, historyContainer);
            entryObject.name = $"HistoryEntry_{entry.Id}";
            
            var entryComponent = entryObject.GetComponent<BreedingHistoryEntryDisplay>();
            if (entryComponent != null)
            {
                entryComponent.SetupEntry(entry, this);
            }
            else
            {
                // Fallback - basic display
                SetupBasicHistoryEntry(entryObject, entry);
            }
        }
        
        private void SetupBasicHistoryEntry(GameObject entryObject, BreedingHistoryEntry entry)
        {
            var text = entryObject.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                string status = entry.WasSuccessful ? "✅" : "❌";
                string result = entry.WasSuccessful ? entry.OffspringName : entry.FailureReason;
                
                text.text = $"{status} {entry.Date:MM/dd HH:mm} - {entry.Parent1Name} + {entry.Parent2Name} = {result}";
                
                if (entry.WasSuccessful)
                {
                    text.text += $"\nGen {entry.OffspringGeneration}, Purity {entry.OffspringPurity:P0}";
                }
            }
        }
        
        #endregion
        
        #region Analytics
        
        private void UpdateAnalytics()
        {
            currentAnalytics = CalculateBreedingAnalytics();
            DisplayAnalytics(currentAnalytics);
        }
        
        private BreedingAnalytics CalculateBreedingAnalytics()
        {
            var analytics = new BreedingAnalytics();
            
            if (breedingHistory.Count == 0)
            {
                return analytics; // Return empty analytics
            }
            
            // Basic counts
            analytics.TotalBreedings = breedingHistory.Count;
            analytics.SuccessfulBreedings = breedingHistory.Count(h => h.WasSuccessful);
            analytics.FailedBreedings = analytics.TotalBreedings - analytics.SuccessfulBreedings;
            analytics.SuccessRate = (float)analytics.SuccessfulBreedings / analytics.TotalBreedings;
            
            // Generation and purity stats
            var successfulBreedings = breedingHistory.Where(h => h.WasSuccessful).ToList();
            if (successfulBreedings.Count > 0)
            {
                analytics.AverageGeneration = successfulBreedings.Average(h => h.OffspringGeneration);
                analytics.AveragePurity = successfulBreedings.Average(h => h.OffspringPurity);
                analytics.HighestGeneration = successfulBreedings.Max(h => h.OffspringGeneration);
                analytics.BestPurity = successfulBreedings.Max(h => h.OffspringPurity);
                analytics.AverageBreedingTime = successfulBreedings.Average(h => h.BreedingTimeSeconds);
            }
            
            // Special trait counts
            analytics.RareTraitBreedings = breedingHistory.Count(h => h.HasRareTraits);
            analytics.MagicalTraitBreedings = breedingHistory.Count(h => h.HasMagicalTraits);
            analytics.MutationBreedings = breedingHistory.Count(h => h.MutationCount > 0);
            
            // Time-based analytics
            analytics.BreedingsToday = breedingHistory.Count(h => h.Date.Date == DateTime.Today);
            analytics.BreedingsThisWeek = breedingHistory.Count(h => 
                h.Date >= DateTime.Today.AddDays(-7));
            
            // Most successful lineage
            var lineageGroups = successfulBreedings.GroupBy(h => $"{h.Parent1Name}+{h.Parent2Name}");
            if (lineageGroups.Any())
            {
                var bestLineage = lineageGroups.OrderByDescending(g => g.Count()).First();
                analytics.MostSuccessfulPairing = bestLineage.Key;
                analytics.MostSuccessfulPairingCount = bestLineage.Count();
            }
            
            return analytics;
        }
        
        private void DisplayAnalytics(BreedingAnalytics analytics)
        {
            if (totalBreedingsText != null)
                totalBreedingsText.text = analytics.TotalBreedings.ToString();
                
            if (successRateText != null)
                successRateText.text = analytics.SuccessRate.ToString("P1");
                
            if (averageGenerationText != null)
                averageGenerationText.text = analytics.AverageGeneration.ToString("F1");
                
            if (bestPurityText != null)
                bestPurityText.text = analytics.BestPurity.ToString("P0");
                
            if (favoriteLineageText != null)
                favoriteLineageText.text = analytics.MostSuccessfulPairing ?? "None";
                
            if (historyStatsText != null)
            {
                historyStatsText.text = $"Today: {analytics.BreedingsToday}\n" +
                                       $"This Week: {analytics.BreedingsThisWeek}\n" +
                                       $"Rare Traits: {analytics.RareTraitBreedings}\n" +
                                       $"Magical: {analytics.MagicalTraitBreedings}\n" +
                                       $"Mutations: {analytics.MutationBreedings}";
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnHistorySearchChanged(string searchTerm)
        {
            currentSearchTerm = searchTerm;
            RefreshHistoryDisplay();
        }
        
        private void OnHistoryFilterChanged(int filterIndex)
        {
            currentFilter = (HistoryFilterType)filterIndex;
            RefreshHistoryDisplay();
        }
        
        private void OnHistorySortChanged(int sortIndex)
        {
            currentSort = (HistorySortType)sortIndex;
            RefreshHistoryDisplay();
        }
        
        private void OnShowSuccessOnlyChanged(bool showSuccessOnly)
        {
            RefreshHistoryDisplay();
        }
        
        private void OnShowFavoritesOnlyChanged(bool showFavoritesOnly)
        {
            RefreshHistoryDisplay();
        }
        
        #endregion
        
        #region Lineage Tree
        
        private void ShowLineageTree()
        {
            if (selectedEntry == null)
            {
                UnityEngine.Debug.LogWarning("No history entry selected for lineage tree");
                return;
            }
            
            if (lineagePanel != null)
                lineagePanel.SetActive(true);
                
            GenerateLineageTree(selectedEntry);
            
            UnityEngine.Debug.Log($"🌳 Showing lineage tree for {selectedEntry.OffspringName}");
        }
        
        private void GenerateLineageTree(BreedingHistoryEntry rootEntry)
        {
            if (lineageTreeContainer == null) return;
            
            // Clear existing tree
            for (int i = lineageTreeContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(lineageTreeContainer.GetChild(i).gameObject);
            }
            
            // Build tree starting from root
            CreateLineageNode(rootEntry, lineageTreeContainer, 0);
            
            // Find and add parent entries
            var parentEntries = FindParentEntries(rootEntry.Parent1Guid, rootEntry.Parent2Guid);
            foreach (var parentEntry in parentEntries)
            {
                CreateLineageNode(parentEntry, lineageTreeContainer, 1);
            }
        }
        
        private List<BreedingHistoryEntry> FindParentEntries(Guid parent1Id, Guid parent2Id)
        {
            return breedingHistory.Where(h =>
                h.OffspringGuid == parent1Id || h.OffspringGuid == parent2Id).ToList();
        }
        
        private void CreateLineageNode(BreedingHistoryEntry entry, Transform parent, int depth)
        {
            if (lineageNodePrefab == null) return;
            
            var nodeObject = Instantiate(lineageNodePrefab, parent);
            nodeObject.name = $"LineageNode_{entry.OffspringName}";
            
            // Position based on depth
            var rectTransform = nodeObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(depth * 200f, 0f);
            }
            
            // Set up node display
            var text = nodeObject.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{entry.OffspringName}\nGen {entry.OffspringGeneration}";
            }
        }
        
        #endregion
        
        #region Data Persistence
        
        private void LoadBreedingHistory()
        {
            // Load breeding history from persistent data
            UnityEngine.Debug.Log("📂 Loading breeding history...");
            
            // For now, just create some sample data
            CreateSampleHistoryData();
        }
        
        private void SaveBreedingHistory()
        {
            if (!autoSaveHistory) return;
            
            UnityEngine.Debug.Log("💾 Saving breeding history...");
            
            // Save history data to persistent storage
            // Implementation would depend on your save system
        }
        
        private void AutoSaveHistory()
        {
            SaveBreedingHistory();
        }
        
        private void CreateSampleHistoryData()
        {
            // Create some sample breeding history for testing
            for (int i = 0; i < 10; i++)
            {
                var sampleEntry = new BreedingHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    Date = DateTime.Now.AddHours(-i * 2),
                    Parent1Name = $"Parent1_{i}",
                    Parent2Name = $"Parent2_{i}",
                    OffspringName = $"Offspring_{i}",
                    WasSuccessful = UnityEngine.Random.value > 0.3f,
                    OffspringGeneration = UnityEngine.Random.Range(1, 5),
                    OffspringPurity = UnityEngine.Random.Range(0.4f, 0.95f),
                    MutationCount = UnityEngine.Random.Range(0, 3),
                    BreedingTimeSeconds = UnityEngine.Random.Range(15f, 120f),
                    BiomeType = (BiomeType)UnityEngine.Random.Range(0, 5),
                    HasRareTraits = UnityEngine.Random.value > 0.7f,
                    HasMagicalTraits = UnityEngine.Random.value > 0.8f,
                    IsFavorite = false
                };
                
                if (!sampleEntry.WasSuccessful)
                {
                    sampleEntry.FailureReason = "Incompatible genetics";
                }
                
                breedingHistory.Add(sampleEntry);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        private void HideAllPanels()
        {
            if (historyPanel != null) historyPanel.SetActive(false);
            if (lineagePanel != null) lineagePanel.SetActive(false);
        }
        
        private void UpdateHistoryStats()
        {
            // Update the filtered history count display
            UnityEngine.Debug.Log($"📊 Showing {filteredHistory.Count} of {breedingHistory.Count} breeding records");
        }
        
        private BiomeType GetCurrentBiome()
        {
            // Get current biome from environment or settings
            return BiomeType.Temperate; // Default
        }
        
        /// <summary>
        /// Select a history entry for detailed view
        /// </summary>
        public void SelectHistoryEntry(BreedingHistoryEntry entry)
        {
            selectedEntry = entry;
            
            // Update UI to show selection
            UnityEngine.Debug.Log($"📋 Selected history entry: {entry.OffspringName}");
        }
        
        /// <summary>
        /// Toggle favorite status of a history entry
        /// </summary>
        public void ToggleHistoryEntryFavorite(BreedingHistoryEntry entry)
        {
            entry.IsFavorite = !entry.IsFavorite;
            
            if (isHistoryVisible)
            {
                RefreshHistoryDisplay();
            }
            
            UnityEngine.Debug.Log($"⭐ History entry {entry.OffspringName} favorite: {entry.IsFavorite}");
        }

        // Missing methods that are referenced
        public void RecordBreedingAttempt(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2, float breedingTime, bool success)
        {
            UnityEngine.Debug.Log($"Recording breeding attempt: {parent1.name} + {parent2.name}, Success: {success}");
            // Placeholder implementation - would record the actual breeding attempt
            if (success)
            {
                // Would call RecordBreeding with actual offspring
                UnityEngine.Debug.Log("Breeding was successful - offspring would be recorded");
            }
            else
            {
                // Would call RecordFailedBreeding
                UnityEngine.Debug.Log("Breeding failed - failure would be recorded");
            }
        }

        #endregion
    }
    
    #region Supporting Classes and Enums
    
    
    [System.Serializable]
    public class BreedingAnalytics
    {
        public int TotalBreedings;
        public int SuccessfulBreedings;
        public int FailedBreedings;
        public float SuccessRate;
        public double AverageGeneration;
        public double AveragePurity;
        public int HighestGeneration;
        public float BestPurity;
        public double AverageBreedingTime;
        public int RareTraitBreedings;
        public int MagicalTraitBreedings;
        public int MutationBreedings;
        public int BreedingsToday;
        public int BreedingsThisWeek;
        public string MostSuccessfulPairing;
        public int MostSuccessfulPairingCount;
    }
    
    public enum HistoryFilterType
    {
        All,
        Successful,
        Failed,
        HighGeneration,
        HighPurity,
        WithMutations,
        RareTraits,
        MagicalTraits
    }
    
    public enum HistorySortType
    {
        DateNewest,
        DateOldest,
        Generation,
        Purity,
        BreedingTime,
        SuccessFirst
    }

    #endregion
}