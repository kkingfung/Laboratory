using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using Laboratory.Core.Events;
using System.Collections;

namespace Laboratory.Chimera.UI
{
    /// <summary>
    /// Comprehensive creature collection management system.
    /// Provides filtering, sorting, detailed inspection, and batch operations for creature collections.
    /// </summary>
    public class CreatureManagementUI : MonoBehaviour
    {
        [Header("Main UI Panels")]
        [SerializeField] private GameObject managementPanel;
        [SerializeField] private GameObject creatureListPanel;
        [SerializeField] private GameObject creatureDetailPanel;
        [SerializeField] private GameObject creatureStatsPanel;
        [SerializeField] private GameObject lineagePanel;
        [SerializeField] private GameObject collectionStatsPanel;
        
        [Header("Creature List")]
        [SerializeField] private Transform creatureListContainer;
        [SerializeField] private GameObject creatureListItemPrefab;
        [SerializeField] private ScrollRect creatureListScrollRect;
        [SerializeField] private TextMeshProUGUI creatureCountText;
        [SerializeField] private Button selectAllButton;
        [SerializeField] private Button deselectAllButton;
        
        [Header("Filtering & Sorting")]
        [SerializeField] private TMP_InputField searchInputField;
        [SerializeField] private TMP_Dropdown sortDropdown;
        [SerializeField] private TMP_Dropdown filterDropdown;
        [SerializeField] private Toggle showOnlyAliveToggle;
        [SerializeField] private Toggle showOnlyAdultsToggle;
        [SerializeField] private Toggle showOnlyBreedableToggle;
        [SerializeField] private Slider minAgeSlider;
        [SerializeField] private Slider maxAgeSlider;
        [SerializeField] private TextMeshProUGUI ageRangeText;
        
        [Header("Creature Details")]
        [SerializeField] private Image creaturePortrait;
        [SerializeField] private TextMeshProUGUI creatureNameText;
        [SerializeField] private TextMeshProUGUI creatureAgeText;
        [SerializeField] private TextMeshProUGUI creatureGenerationText;
        [SerializeField] private TextMeshProUGUI creatureHealthText;
        [SerializeField] private TextMeshProUGUI creatureHappinessText;
        [SerializeField] private TextMeshProUGUI creatureBiomeText;
        [SerializeField] private Button renameCreatureButton;
        [SerializeField] private Button deleteCreatureButton;
        [SerializeField] private Button releaseCreatureButton;
        
        [Header("Genetic Information")]
        [SerializeField] private Transform traitsContainer;
        [SerializeField] private Transform mutationsContainer;
        [SerializeField] private Transform ancestryContainer;
        [SerializeField] private TextMeshProUGUI geneticPurityText;
        [SerializeField] private TextMeshProUGUI lineageIdText;
        [SerializeField] private Button viewFullGeneticsButton;
        
        [Header("Batch Operations")]
        [SerializeField] private GameObject batchOperationsPanel;
        [SerializeField] private Button batchBreedButton;
        [SerializeField] private Button batchReleaseButton;
        [SerializeField] private Button batchDeleteButton;
        [SerializeField] private Button exportSelectedButton;
        [SerializeField] private TextMeshProUGUI selectionCountText;
        
        [Header("Collection Statistics")]
        [SerializeField] private TextMeshProUGUI totalCreaturesText;
        [SerializeField] private TextMeshProUGUI totalGenerationsText;
        [SerializeField] private TextMeshProUGUI averageAgeText;
        [SerializeField] private TextMeshProUGUI rarityDistributionText;
        [SerializeField] private Button viewStatisticsButton;
        
        [Header("Settings")]
        [SerializeField] private bool autoRefreshList = true;
        [SerializeField] private float refreshInterval = 5f;
        [SerializeField] private int maxCreaturesPerPage = 50;
        [SerializeField] private KeyCode toggleUIKey = KeyCode.C;
        
        private List<CreatureInstanceComponent> allCreatures = new List<CreatureInstanceComponent>();
        private List<CreatureInstanceComponent> filteredCreatures = new List<CreatureInstanceComponent>();
        private List<CreatureInstanceComponent> selectedCreatures = new List<CreatureInstanceComponent>();
        private CreatureInstanceComponent currentlyViewedCreature;
        private Dictionary<CreatureInstanceComponent, CreatureListItem> creatureListItems = new Dictionary<CreatureInstanceComponent, CreatureListItem>();
        
        private IEventBus eventBus;
        private bool isUIVisible = false;
        private Coroutine refreshCoroutine;
        
        public enum SortMode
        {
            Name,
            Age,
            Generation,
            Health,
            Happiness,
            Rarity,
            LastSeen
        }
        
        public enum FilterMode
        {
            All,
            Alive,
            Adults,
            Breedable,
            HighRarity,
            RecentlyBorn,
            NeedsCare
        }
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeUI();
            SetupEventHandlers();
            InitializeEventBus();
            RefreshCreatureList();
            
            if (autoRefreshList)
            {
                refreshCoroutine = StartCoroutine(AutoRefreshCoroutine());
            }
            
            HideAllPanels();
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        private void OnDestroy()
        {
            if (refreshCoroutine != null)
            {
                StopCoroutine(refreshCoroutine);
            }
        }
        
        #endregion
        
        #region UI Initialization
        
        private void InitializeUI()
        {
            // Set up button listeners
            if (selectAllButton != null)
                selectAllButton.onClick.AddListener(SelectAllCreatures);
            if (deselectAllButton != null)
                deselectAllButton.onClick.AddListener(DeselectAllCreatures);
                
            if (renameCreatureButton != null)
                renameCreatureButton.onClick.AddListener(RenameCurrentCreature);
            if (deleteCreatureButton != null)
                deleteCreatureButton.onClick.AddListener(DeleteCurrentCreature);
            if (releaseCreatureButton != null)
                releaseCreatureButton.onClick.AddListener(ReleaseCurrentCreature);
                
            if (batchBreedButton != null)
                batchBreedButton.onClick.AddListener(BatchBreedSelected);
            if (batchReleaseButton != null)
                batchReleaseButton.onClick.AddListener(BatchReleaseSelected);
            if (batchDeleteButton != null)
                batchDeleteButton.onClick.AddListener(BatchDeleteSelected);
            if (exportSelectedButton != null)
                exportSelectedButton.onClick.AddListener(ExportSelectedCreatures);
                
            if (viewFullGeneticsButton != null)
                viewFullGeneticsButton.onClick.AddListener(ViewFullGenetics);
            if (viewStatisticsButton != null)
                viewStatisticsButton.onClick.AddListener(ViewCollectionStatistics);
            
            // Set up dropdown listeners
            if (sortDropdown != null)
                sortDropdown.onValueChanged.AddListener(OnSortModeChanged);
            if (filterDropdown != null)
                filterDropdown.onValueChanged.AddListener(OnFilterModeChanged);
            
            // Set up search field
            if (searchInputField != null)
                searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
            
            // Set up toggle listeners
            if (showOnlyAliveToggle != null)
                showOnlyAliveToggle.onValueChanged.AddListener(OnFilterToggleChanged);
            if (showOnlyAdultsToggle != null)
                showOnlyAdultsToggle.onValueChanged.AddListener(OnFilterToggleChanged);
            if (showOnlyBreedableToggle != null)
                showOnlyBreedableToggle.onValueChanged.AddListener(OnFilterToggleChanged);
            
            // Set up age range sliders
            if (minAgeSlider != null)
                minAgeSlider.onValueChanged.AddListener(OnAgeRangeChanged);
            if (maxAgeSlider != null)
                maxAgeSlider.onValueChanged.AddListener(OnAgeRangeChanged);
            
            // Initialize dropdowns
            InitializeSortDropdown();
            InitializeFilterDropdown();
            
            // Initialize age range
            UpdateAgeRangeDisplay();
        }
        
        private void InitializeSortDropdown()
        {
            if (sortDropdown == null) return;
            
            sortDropdown.ClearOptions();
            var sortOptions = System.Enum.GetNames(typeof(SortMode)).ToList();
            sortDropdown.AddOptions(sortOptions);
        }
        
        private void InitializeFilterDropdown()
        {
            if (filterDropdown == null) return;
            
            filterDropdown.ClearOptions();
            var filterOptions = System.Enum.GetNames(typeof(FilterMode)).ToList();
            filterDropdown.AddOptions(filterOptions);
        }
        
        private void SetupEventHandlers()
        {
            // Listen for creature lifecycle events
        }
        
        private void InitializeEventBus()
        {
            if (Laboratory.Core.DI.GlobalServiceProvider.IsInitialized)
            {
                Laboratory.Core.DI.GlobalServiceProvider.TryResolve<IEventBus>(out eventBus);
            }
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(toggleUIKey))
            {
                ToggleManagementUI();
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Show the creature management UI
        /// </summary>
        public void ShowManagementUI()
        {
            if (managementPanel != null)
                managementPanel.SetActive(true);
            isUIVisible = true;
            
            RefreshCreatureList();
            UpdateCollectionStatistics();
            
            Debug.Log("📋 Creature Management UI opened");
        }
        
        /// <summary>
        /// Hide the creature management UI
        /// </summary>
        public void HideManagementUI()
        {
            if (managementPanel != null)
                managementPanel.SetActive(false);
            isUIVisible = false;
            
            Debug.Log("📋 Creature Management UI closed");
        }
        
        /// <summary>
        /// Toggle management UI visibility
        /// </summary>
        public void ToggleManagementUI()
        {
            if (isUIVisible)
                HideManagementUI();
            else
                ShowManagementUI();
        }
        
        /// <summary>
        /// Add a creature to the management system
        /// </summary>
        public void AddCreature(CreatureInstanceComponent creature)
        {
            if (creature == null || allCreatures.Contains(creature)) return;
            
            allCreatures.Add(creature);
            
            if (isUIVisible)
            {
                RefreshCreatureList();
                UpdateCollectionStatistics();
            }
            
            Debug.Log($"➕ Added creature to management: {creature.name}");
        }
        
        /// <summary>
        /// Remove a creature from the management system
        /// </summary>
        public void RemoveCreature(CreatureInstanceComponent creature)
        {
            if (creature == null) return;
            
            allCreatures.Remove(creature);
            selectedCreatures.Remove(creature);
            
            if (creatureListItems.ContainsKey(creature))
            {
                var listItem = creatureListItems[creature];
                if (listItem != null && listItem.gameObject != null)
                    Destroy(listItem.gameObject);
                creatureListItems.Remove(creature);
            }
            
            if (currentlyViewedCreature == creature)
            {
                currentlyViewedCreature = null;
                ClearCreatureDetails();
            }
            
            if (isUIVisible)
            {
                UpdateSelectionDisplay();
                UpdateCollectionStatistics();
            }
            
            Debug.Log($"➖ Removed creature from management: {creature.name}");
        }
        
        /// <summary>
        /// Select a specific creature for detailed viewing
        /// </summary>
        public void ViewCreature(CreatureInstanceComponent creature)
        {
            currentlyViewedCreature = creature;
            DisplayCreatureDetails(creature);
            
            Debug.Log($"👁️ Viewing creature: {creature.name}");
        }
        
        #endregion
        
        #region Creature List Management
        
        private void RefreshCreatureList()
        {
            UpdateCreatureCollection();
            ApplyFiltersAndSorting();
            UpdateCreatureListDisplay();
            UpdateCollectionStatistics();
        }
        
        private void UpdateCreatureCollection()
        {
            // Find all creatures in the scene
            var sceneCreatures = FindObjectsByType<CreatureInstanceComponent>(FindObjectsSortMode.None);
            
            // Add new creatures
            foreach (var creature in sceneCreatures)
            {
                if (!allCreatures.Contains(creature))
                {
                    allCreatures.Add(creature);
                }
            }
            
            // Remove destroyed creatures
            allCreatures.RemoveAll(creature => creature == null || !creature.gameObject.activeInHierarchy);
        }
        
        private void ApplyFiltersAndSorting()
        {
            filteredCreatures = allCreatures.Where(creature => PassesFilters(creature)).ToList();
            
            // Apply sorting
            var sortMode = (SortMode)sortDropdown.value;
            filteredCreatures = SortCreatures(filteredCreatures, sortMode).ToList();
        }
        
        private bool PassesFilters(CreatureInstanceComponent creature)
        {
            if (creature?.CreatureData == null) return false;
            
            // Search filter
            if (!string.IsNullOrEmpty(searchInputField?.text))
            {
                string searchTerm = searchInputField.text.ToLower();
                if (!creature.name.ToLower().Contains(searchTerm))
                    return false;
            }
            
            // Alive filter
            if (showOnlyAliveToggle?.isOn == true)
            {
                if (creature.CreatureData.CurrentHealth <= 0)
                    return false;
            }
            
            // Adults filter
            if (showOnlyAdultsToggle?.isOn == true)
            {
                if (creature.CreatureData.AgeInDays < 30) // Assume 30 days = adult
                    return false;
            }
            
            // Breedable filter
            if (showOnlyBreedableToggle?.isOn == true)
            {
                if (!IsBreedable(creature))
                    return false;
            }
            
            // Age range filter
            float minAge = minAgeSlider?.value ?? 0f;
            float maxAge = maxAgeSlider?.value ?? 1000f;
            if (creature.CreatureData.AgeInDays < minAge || creature.CreatureData.AgeInDays > maxAge)
                return false;
            
            // Filter mode
            var filterMode = (FilterMode)filterDropdown.value;
            if (!PassesFilterMode(creature, filterMode))
                return false;
            
            return true;
        }
        
        private bool PassesFilterMode(CreatureInstanceComponent creature, FilterMode mode)
        {
            switch (mode)
            {
                case FilterMode.All:
                    return true;
                    
                case FilterMode.Alive:
                    return creature.CreatureData.CurrentHealth > 0;
                    
                case FilterMode.Adults:
                    return creature.CreatureData.AgeInDays >= 30;
                    
                case FilterMode.Breedable:
                    return IsBreedable(creature);
                    
                case FilterMode.HighRarity:
                    return GetCreatureRarity(creature) >= 0.8f;
                    
                case FilterMode.RecentlyBorn:
                    return creature.CreatureData.AgeInDays <= 7;
                    
                case FilterMode.NeedsCare:
                    return creature.CreatureData.CurrentHealth < 50 || creature.CreatureData.Happiness < 0.3f;
                    
                default:
                    return true;
            }
        }
        
        private IEnumerable<CreatureInstanceComponent> SortCreatures(IEnumerable<CreatureInstanceComponent> creatures, SortMode mode)
        {
            switch (mode)
            {
                case SortMode.Name:
                    return creatures.OrderBy(c => c.name);
                    
                case SortMode.Age:
                    return creatures.OrderByDescending(c => c.CreatureData?.AgeInDays ?? 0);
                    
                case SortMode.Generation:
                    return creatures.OrderByDescending(c => c.CreatureData?.GeneticProfile?.Generation ?? 0);
                    
                case SortMode.Health:
                    return creatures.OrderByDescending(c => c.CreatureData?.CurrentHealth ?? 0);
                    
                case SortMode.Happiness:
                    return creatures.OrderByDescending(c => c.CreatureData?.Happiness ?? 0);
                    
                case SortMode.Rarity:
                    return creatures.OrderByDescending(c => GetCreatureRarity(c));
                    
                case SortMode.LastSeen:
                    return creatures.OrderByDescending(c => Time.time); // Placeholder
                    
                default:
                    return creatures;
            }
        }
        
        private void UpdateCreatureListDisplay()
        {
            // Clear existing list items
            foreach (var item in creatureListItems.Values)
            {
                if (item != null && item.gameObject != null)
                    Destroy(item.gameObject);
            }
            creatureListItems.Clear();
            
            // Create new list items
            int displayCount = Mathf.Min(filteredCreatures.Count, maxCreaturesPerPage);
            for (int i = 0; i < displayCount; i++)
            {
                var creature = filteredCreatures[i];
                CreateCreatureListItem(creature);
            }
            
            // Update count display
            if (creatureCountText != null)
                creatureCountText.text = $"Showing {displayCount} of {filteredCreatures.Count} creatures ({allCreatures.Count} total)";
        }
        
        private void CreateCreatureListItem(CreatureInstanceComponent creature)
        {
            if (creatureListContainer == null || creatureListItemPrefab == null) return;
            
            var listItemObject = Instantiate(creatureListItemPrefab, creatureListContainer);
            var listItem = listItemObject.GetComponent<CreatureListItem>();
            
            if (listItem == null)
            {
                listItem = listItemObject.AddComponent<CreatureListItem>();
            }
            
            listItem.Initialize(creature, this);
            creatureListItems[creature] = listItem;
        }
        
        #endregion
        
        #region Selection Management
        
        public void ToggleCreatureSelection(CreatureInstanceComponent creature)
        {
            if (selectedCreatures.Contains(creature))
            {
                selectedCreatures.Remove(creature);
            }
            else
            {
                selectedCreatures.Add(creature);
            }
            
            UpdateSelectionDisplay();
            UpdateBatchOperationsUI();
        }
        
        private void SelectAllCreatures()
        {
            selectedCreatures.Clear();
            selectedCreatures.AddRange(filteredCreatures);
            
            UpdateSelectionDisplay();
            UpdateBatchOperationsUI();
            
            Debug.Log($"✅ Selected all {selectedCreatures.Count} creatures");
        }
        
        private void DeselectAllCreatures()
        {
            selectedCreatures.Clear();
            
            UpdateSelectionDisplay();
            UpdateBatchOperationsUI();
            
            Debug.Log("❌ Deselected all creatures");
        }
        
        private void UpdateSelectionDisplay()
        {
            if (selectionCountText != null)
                selectionCountText.text = $"{selectedCreatures.Count} selected";
            
            // Update visual state of list items
            foreach (var kvp in creatureListItems)
            {
                var creature = kvp.Key;
                var listItem = kvp.Value;
                
                if (listItem != null)
                {
                    listItem.SetSelected(selectedCreatures.Contains(creature));
                }
            }
        }
        
        #endregion
        
        #region Creature Details Display
        
        private void DisplayCreatureDetails(CreatureInstanceComponent creature)
        {
            if (creature?.CreatureData == null) return;
            
            var data = creature.CreatureData;
            var genetics = data.GeneticProfile;
            
            // Basic info
            if (creatureNameText != null)
                creatureNameText.text = creature.name;
            if (creatureAgeText != null)
                creatureAgeText.text = $"Age: {data.AgeInDays} days";
            if (creatureGenerationText != null)
                creatureGenerationText.text = $"Generation: {genetics?.Generation ?? 0}";
            if (creatureHealthText != null)
                creatureHealthText.text = $"Health: {data.CurrentHealth:F0}%";
            if (creatureHappinessText != null)
                creatureHappinessText.text = $"Happiness: {data.Happiness:P0}";
            
            // Genetic info
            if (genetics != null)
            {
                if (geneticPurityText != null)
                    geneticPurityText.text = $"Genetic Purity: {genetics.GetGeneticPurity():P0}";
                if (lineageIdText != null)
                    lineageIdText.text = $"Lineage: {genetics.LineageId}";
                
                DisplayTraits(genetics);
                DisplayMutations(genetics);
            }
            
            // Portrait (placeholder)
            if (creaturePortrait != null)
            {
                // Use creature's color as portrait background
                var colorGene = genetics?.Genes.FirstOrDefault(g => g.traitName.Contains("Color"));
                if (colorGene.HasValue && colorGene.Value.value.HasValue)
                {
                    creaturePortrait.color = Color.HSVToRGB(colorGene.Value.value.Value, 0.8f, 0.9f);
                }
                else
                {
                    creaturePortrait.color = Color.white;
                }
            }
        }
        
        private void DisplayTraits(GeneticProfile genetics)
        {
            if (traitsContainer == null) return;
            
            // Clear existing traits
            for (int i = traitsContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(traitsContainer.GetChild(i).gameObject);
            }
            
            // Display top traits
            var topTraits = genetics.Genes
                .Where(g => g.isActive && g.value.HasValue)
                .OrderByDescending(g => g.value.Value)
                .Take(8);
            
            foreach (var trait in topTraits)
            {
                CreateTraitDisplay(trait);
            }
        }
        
        private void CreateTraitDisplay(Gene trait)
        {
            var traitObject = new GameObject($"Trait_{trait.traitName}");
            traitObject.transform.SetParent(traitsContainer);
            
            var text = traitObject.AddComponent<TextMeshProUGUI>();
            text.text = $"{trait.traitName}: {trait.value.Value:P0}";
            text.fontSize = 12;
            text.color = GetTraitColor(trait);
            
            var layoutElement = traitObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 20;
        }
        
        private void DisplayMutations(GeneticProfile genetics)
        {
            if (mutationsContainer == null) return;
            
            // Clear existing mutations
            for (int i = mutationsContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(mutationsContainer.GetChild(i).gameObject);
            }
            
            // Display mutations
            foreach (var mutation in genetics.Mutations)
            {
                CreateMutationDisplay(mutation);
            }
        }
        
        private void CreateMutationDisplay(Mutation mutation)
        {
            var mutationObject = new GameObject($"Mutation_{mutation.affectedTrait}");
            mutationObject.transform.SetParent(mutationsContainer);
            
            var text = mutationObject.AddComponent<TextMeshProUGUI>();
            text.text = $"{mutation.mutationType}: {mutation.affectedTrait}";
            text.fontSize = 10;
            text.color = mutation.isHarmful ? Color.red : Color.green;
            
            var layoutElement = mutationObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 18;
        }
        
        private void ClearCreatureDetails()
        {
            if (creatureNameText != null) creatureNameText.text = "No creature selected";
            if (creatureAgeText != null) creatureAgeText.text = "";
            if (creatureGenerationText != null) creatureGenerationText.text = "";
            if (creatureHealthText != null) creatureHealthText.text = "";
            if (creatureHappinessText != null) creatureHappinessText.text = "";
            if (geneticPurityText != null) geneticPurityText.text = "";
            if (lineageIdText != null) lineageIdText.text = "";
            
            // Clear traits and mutations
            ClearContainer(traitsContainer);
            ClearContainer(mutationsContainer);
        }
        
        private void ClearContainer(Transform container)
        {
            if (container == null) return;
            
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(container.GetChild(i).gameObject);
            }
        }
        
        #endregion
        
        #region Batch Operations
        
        private void UpdateBatchOperationsUI()
        {
            bool hasSelection = selectedCreatures.Count > 0;
            
            if (batchBreedButton != null)
                batchBreedButton.interactable = selectedCreatures.Count >= 2;
            if (batchReleaseButton != null)
                batchReleaseButton.interactable = hasSelection;
            if (batchDeleteButton != null)
                batchDeleteButton.interactable = hasSelection;
            if (exportSelectedButton != null)
                exportSelectedButton.interactable = hasSelection;
        }
        
        private void BatchBreedSelected()
        {
            if (selectedCreatures.Count < 2)
            {
                Debug.LogWarning("Need at least 2 creatures selected for batch breeding");
                return;
            }
            
            // Open breeding UI with selected creatures
            var breedingUI = FindFirstObjectByType<AdvancedBreedingUI>();
            if (breedingUI != null)
            {
                // Set up breeding pairs from selection
                for (int i = 0; i < selectedCreatures.Count - 1; i += 2)
                {
                    breedingUI.QuickBreed(selectedCreatures[i], selectedCreatures[i + 1]);
                }
            }
            
            Debug.Log($"🧬 Started batch breeding with {selectedCreatures.Count} creatures");
        }
        
        private void BatchReleaseSelected()
        {
            Debug.Log($"🚪 Releasing {selectedCreatures.Count} creatures...");
            
            foreach (var creature in selectedCreatures.ToList())
            {
                ReleaseCreature(creature);
            }
            
            selectedCreatures.Clear();
            RefreshCreatureList();
        }
        
        private void BatchDeleteSelected()
        {
            Debug.Log($"🗑️ Deleting {selectedCreatures.Count} creatures...");
            
            foreach (var creature in selectedCreatures.ToList())
            {
                DeleteCreature(creature);
            }
            
            selectedCreatures.Clear();
            RefreshCreatureList();
        }
        
        private void ExportSelectedCreatures()
        {
            Debug.Log($"📤 Exporting {selectedCreatures.Count} creatures...");
            
            // Implement export functionality
            // This could save creature data to JSON, CSV, etc.
        }
        
        #endregion
        
        #region Individual Creature Operations
        
        private void RenameCurrentCreature()
        {
            if (currentlyViewedCreature == null) return;
            
            // Simple rename (in a real game, you'd want a proper input dialog)
            string newName = $"Creature_{System.Guid.NewGuid().ToString("N")[..8]}";
            currentlyViewedCreature.name = newName;
            
            DisplayCreatureDetails(currentlyViewedCreature);
            RefreshCreatureList();
            
            Debug.Log($"📝 Renamed creature to: {newName}");
        }
        
        private void DeleteCurrentCreature()
        {
            if (currentlyViewedCreature == null) return;
            
            DeleteCreature(currentlyViewedCreature);
            currentlyViewedCreature = null;
            ClearCreatureDetails();
            RefreshCreatureList();
        }
        
        private void ReleaseCurrentCreature()
        {
            if (currentlyViewedCreature == null) return;
            
            ReleaseCreature(currentlyViewedCreature);
            currentlyViewedCreature = null;
            ClearCreatureDetails();
            RefreshCreatureList();
        }
        
        private void DeleteCreature(CreatureInstanceComponent creature)
        {
            if (creature != null)
            {
                RemoveCreature(creature);
                Destroy(creature.gameObject);
                Debug.Log($"🗑️ Deleted creature: {creature.name}");
            }
        }
        
        private void ReleaseCreature(CreatureInstanceComponent creature)
        {
            if (creature != null)
            {
                // Mark as released (could move to wild area, change AI, etc.)
                creature.tag = "WildCreature";
                RemoveCreature(creature);
                Debug.Log($"🚪 Released creature: {creature.name}");
            }
        }
        
        #endregion
        
        #region Statistics & Analytics
        
        private void UpdateCollectionStatistics()
        {
            if (totalCreaturesText != null)
                totalCreaturesText.text = $"Total: {allCreatures.Count}";
            
            if (allCreatures.Count > 0)
            {
                var maxGeneration = allCreatures.Max(c => c.CreatureData?.GeneticProfile?.Generation ?? 0);
                if (totalGenerationsText != null)
                    totalGenerationsText.text = $"Max Gen: {maxGeneration}";
                
                var avgAge = allCreatures.Average(c => c.CreatureData?.AgeInDays ?? 0);
                if (averageAgeText != null)
                    averageAgeText.text = $"Avg Age: {avgAge:F1} days";
                
                UpdateRarityDistribution();
            }
        }
        
        private void UpdateRarityDistribution()
        {
            if (rarityDistributionText == null || allCreatures.Count == 0) return;
            
            var rarityGroups = allCreatures
                .GroupBy(c => GetRarityTier(GetCreatureRarity(c)))
                .ToDictionary(g => g.Key, g => g.Count());
            
            string distribution = string.Join(", ", rarityGroups.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            rarityDistributionText.text = $"Rarity: {distribution}";
        }
        
        private void ViewCollectionStatistics()
        {
            Debug.Log("📊 Opening detailed collection statistics...");
            // This would open a detailed statistics panel
        }
        
        private void ViewFullGenetics()
        {
            if (currentlyViewedCreature?.CreatureData?.GeneticProfile == null) return;
            
            Debug.Log("🧬 Opening full genetics viewer...");
            // This would open a detailed genetics analysis panel
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnSortModeChanged(int index)
        {
            ApplyFiltersAndSorting();
            UpdateCreatureListDisplay();
        }
        
        private void OnFilterModeChanged(int index)
        {
            ApplyFiltersAndSorting();
            UpdateCreatureListDisplay();
        }
        
        private void OnSearchTextChanged(string searchText)
        {
            ApplyFiltersAndSorting();
            UpdateCreatureListDisplay();
        }
        
        private void OnFilterToggleChanged(bool value)
        {
            ApplyFiltersAndSorting();
            UpdateCreatureListDisplay();
        }
        
        private void OnAgeRangeChanged(float value)
        {
            UpdateAgeRangeDisplay();
            ApplyFiltersAndSorting();
            UpdateCreatureListDisplay();
        }
        
        private void UpdateAgeRangeDisplay()
        {
            if (ageRangeText != null && minAgeSlider != null && maxAgeSlider != null)
            {
                ageRangeText.text = $"Age: {minAgeSlider.value:F0} - {maxAgeSlider.value:F0} days";
            }
        }
        
        #endregion
        
        #region Auto Refresh
        
        private IEnumerator AutoRefreshCoroutine()
        {
            while (autoRefreshList)
            {
                yield return new WaitForSeconds(refreshInterval);
                
                if (isUIVisible)
                {
                    RefreshCreatureList();
                }
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        private bool IsBreedable(CreatureInstanceComponent creature)
        {
            if (creature?.CreatureData == null) return false;
            
            return creature.CreatureData.AgeInDays >= 30 && // Adult
                   creature.CreatureData.CurrentHealth > 50 && // Healthy
                   creature.CreatureData.Happiness > 0.3f; // Happy enough
        }
        
        private float GetCreatureRarity(CreatureInstanceComponent creature)
        {
            if (creature?.CreatureData?.GeneticProfile == null) return 0f;
            
            var genetics = creature.CreatureData.GeneticProfile;
            
            // Calculate rarity based on multiple factors
            float rarity = 0f;
            
            // Generation bonus
            rarity += Mathf.Min(genetics.Generation * 0.1f, 0.3f);
            
            // Mutation count
            rarity += Mathf.Min(genetics.Mutations.Count() * 0.15f, 0.4f);
            
            // Rare trait combinations
            var rareTraits = genetics.Genes.Count(g => g.value.HasValue && g.value.Value > 0.9f);
            rarity += Mathf.Min(rareTraits * 0.1f, 0.3f);
            
            return Mathf.Clamp01(rarity);
        }
        
        private string GetRarityTier(float rarity)
        {
            if (rarity >= 0.8f) return "Legendary";
            if (rarity >= 0.6f) return "Epic";
            if (rarity >= 0.4f) return "Rare";
            if (rarity >= 0.2f) return "Uncommon";
            return "Common";
        }
        
        private Color GetTraitColor(Gene trait)
        {
            switch (trait.traitType)
            {
                case TraitType.Physical: return Color.green;
                case TraitType.Mental: return Color.blue;
                case TraitType.Magical: return Color.magenta;
                case TraitType.Social: return Color.yellow;
                case TraitType.Combat: return Color.red;
                default: return Color.white;
            }
        }
        
        private void HideAllPanels()
        {
            if (managementPanel != null) managementPanel.SetActive(false);
            if (creatureDetailPanel != null) creatureDetailPanel.SetActive(false);
            if (lineagePanel != null) lineagePanel.SetActive(false);
            if (collectionStatsPanel != null) collectionStatsPanel.SetActive(false);
        }
        
        #endregion
    }
}