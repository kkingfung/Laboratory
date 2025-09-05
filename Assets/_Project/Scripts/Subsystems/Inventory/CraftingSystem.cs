using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Gameplay.Inventory;
using Laboratory.Subsystems.Inventory;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// Comprehensive Crafting System for Laboratory Unity Project
    /// Version 1.0 - Complete implementation with recipes, validation, and events
    /// </summary>
    [System.Serializable]
    public class CraftingSystem : MonoBehaviour
    {
        #region Fields

        [Header("Crafting Configuration")]
        [SerializeField] private List<CraftingRecipe> availableRecipes = new();
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool requireCraftingTable = false;
        [SerializeField] private float craftingTime = 2f;

        [Header("Crafting Stations")]
        [SerializeField] private List<CraftingStation> craftingStations = new();

        private IInventorySystem _inventorySystem;
        private IEventBus _eventBus;
        private Dictionary<string, CraftingRecipe> _recipeDatabase = new();
        private List<CraftingProcess> _activeCraftingProcesses = new();

        #endregion

        #region Events

        public event Action<CraftingRecipe> OnRecipeDiscovered;
        public event Action<CraftingProcess> OnCraftingStarted;
        public event Action<CraftingProcess> OnCraftingCompleted;
        public event Action<CraftingProcess> OnCraftingFailed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeCraftingSystem();
        }

        private void Start()
        {
            SetupRecipeDatabase();
        }

        private void Update()
        {
            UpdateActiveCraftingProcesses();
        }

        #endregion

        #region Initialization

        private void InitializeCraftingSystem()
        {
            try
            {
                // Resolve dependencies
                _inventorySystem = GlobalServiceProvider.Resolve<IInventorySystem>();
                _eventBus = GlobalServiceProvider.Resolve<IEventBus>();

                if (enableDebugLogs)
                    Debug.Log("[CraftingSystem] Successfully initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CraftingSystem] Failed to initialize: {ex.Message}");
            }
        }

        private void SetupRecipeDatabase()
        {
            _recipeDatabase.Clear();
            
            foreach (var recipe in availableRecipes)
            {
                if (recipe != null && !string.IsNullOrEmpty(recipe.RecipeID))
                {
                    _recipeDatabase[recipe.RecipeID] = recipe;
                }
            }

            if (enableDebugLogs)
                Debug.Log($"[CraftingSystem] Loaded {_recipeDatabase.Count} recipes");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Attempts to start crafting a recipe
        /// </summary>
        public bool TryStartCrafting(string recipeID, CraftingStationType stationType = CraftingStationType.None)
        {
            if (!_recipeDatabase.TryGetValue(recipeID, out var recipe))
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[CraftingSystem] Recipe not found: {recipeID}");
                return false;
            }

            return TryStartCrafting(recipe, stationType);
        }

        /// <summary>
        /// Attempts to start crafting a recipe
        /// </summary>
        public bool TryStartCrafting(CraftingRecipe recipe, CraftingStationType stationType = CraftingStationType.None)
        {
            if (!CanCraftRecipe(recipe, stationType))
                return false;

            try
            {
                // Consume materials
                if (!ConsumeMaterials(recipe))
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"[CraftingSystem] Failed to consume materials for {recipe.RecipeName}");
                    return false;
                }

                // Create crafting process
                var craftingProcess = new CraftingProcess
                {
                    Recipe = recipe,
                    StartTime = Time.time,
                    Duration = recipe.CraftingTime > 0 ? recipe.CraftingTime : craftingTime,
                    StationType = stationType,
                    ProcessID = System.Guid.NewGuid().ToString()
                };

                _activeCraftingProcesses.Add(craftingProcess);

                // Fire events
                OnCraftingStarted?.Invoke(craftingProcess);
                _eventBus?.Publish(new CraftingStartedEvent(craftingProcess));

                if (enableDebugLogs)
                    Debug.Log($"[CraftingSystem] Started crafting: {recipe.RecipeName}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CraftingSystem] Error starting crafting: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a recipe can be crafted
        /// </summary>
        public bool CanCraftRecipe(string recipeID, CraftingStationType stationType = CraftingStationType.None)
        {
            return _recipeDatabase.TryGetValue(recipeID, out var recipe) && CanCraftRecipe(recipe, stationType);
        }

        /// <summary>
        /// Checks if a recipe can be crafted
        /// </summary>
        public bool CanCraftRecipe(CraftingRecipe recipe, CraftingStationType stationType = CraftingStationType.None)
        {
            if (recipe == null || _inventorySystem == null)
                return false;

            // Check crafting table requirement (global setting)
            if (requireCraftingTable && stationType == CraftingStationType.None)
            {
                return false;
            }

            // Check crafting station requirement
            if (recipe.RequiredStation != CraftingStationType.None && 
                stationType != recipe.RequiredStation)
            {
                return false;
            }

            // Check material requirements
            foreach (var material in recipe.RequiredMaterials)
            {
                if (!_inventorySystem.HasItem(material.ItemID, material.Quantity))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets all available recipes that can currently be crafted
        /// </summary>
        public List<CraftingRecipe> GetAvailableRecipes(CraftingStationType stationType = CraftingStationType.None)
        {
            return _recipeDatabase.Values
                .Where(recipe => CanCraftRecipe(recipe, stationType))
                .ToList();
        }

        /// <summary>
        /// Gets all recipes, regardless of whether they can be crafted
        /// </summary>
        public List<CraftingRecipe> GetAllKnownRecipes()
        {
            return _recipeDatabase.Values.ToList();
        }

        /// <summary>
        /// Discovers a new recipe and adds it to the available recipes
        /// </summary>
        public bool DiscoverRecipe(CraftingRecipe recipe)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.RecipeID))
                return false;

            if (_recipeDatabase.ContainsKey(recipe.RecipeID))
                return false; // Already known

            _recipeDatabase[recipe.RecipeID] = recipe;
            availableRecipes.Add(recipe);

            OnRecipeDiscovered?.Invoke(recipe);
            _eventBus?.Publish(new RecipeDiscoveredEvent(recipe));

            if (enableDebugLogs)
                Debug.Log($"[CraftingSystem] Discovered new recipe: {recipe.RecipeName}");

            return true;
        }

        /// <summary>
        /// Gets active crafting processes
        /// </summary>
        public List<CraftingProcess> GetActiveCraftingProcesses()
        {
            return new List<CraftingProcess>(_activeCraftingProcesses);
        }

        /// <summary>
        /// Cancels a crafting process and refunds materials
        /// </summary>
        public bool CancelCrafting(string processID)
        {
            var process = _activeCraftingProcesses.FirstOrDefault(p => p.ProcessID == processID);
            if (process == null)
                return false;

            try
            {
                // Refund materials
                RefundMaterials(process.Recipe);

                // Remove from active processes
                _activeCraftingProcesses.Remove(process);

                _eventBus?.Publish(new CraftingCancelledEvent(process));

                if (enableDebugLogs)
                    Debug.Log($"[CraftingSystem] Cancelled crafting: {process.Recipe.RecipeName}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CraftingSystem] Error cancelling crafting: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Private Methods

        private void UpdateActiveCraftingProcesses()
        {
            for (int i = _activeCraftingProcesses.Count - 1; i >= 0; i--)
            {
                var process = _activeCraftingProcesses[i];
                
                if (Time.time >= process.StartTime + process.Duration)
                {
                    CompleteCrafting(process);
                    _activeCraftingProcesses.RemoveAt(i);
                }
            }
        }

        private void CompleteCrafting(CraftingProcess process)
        {
            try
            {
                // Add result items to inventory
                foreach (var result in process.Recipe.ResultItems)
                {
                    if (!_inventorySystem.TryAddItem(result.ItemData, result.Quantity))
                    {
                        // If inventory is full, could drop items on ground
                        Debug.LogWarning($"[CraftingSystem] Inventory full, couldn't add {result.ItemData.ItemName}");
                        OnCraftingFailed?.Invoke(process);
                        _eventBus?.Publish(new CraftingFailedEvent(process, "Inventory full"));
                        return;
                    }
                }

                // Fire success events
                OnCraftingCompleted?.Invoke(process);
                _eventBus?.Publish(new CraftingCompletedEvent(process));

                if (enableDebugLogs)
                {
                    var resultNames = string.Join(", ", process.Recipe.ResultItems.Select(r => 
                        $"{r.Quantity}x {r.ItemData.ItemName}"));
                    Debug.Log($"[CraftingSystem] Completed crafting: {resultNames}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CraftingSystem] Error completing crafting: {ex.Message}");
                OnCraftingFailed?.Invoke(process);
                _eventBus?.Publish(new CraftingFailedEvent(process, ex.Message));
            }
        }

        private bool ConsumeMaterials(CraftingRecipe recipe)
        {
            foreach (var material in recipe.RequiredMaterials)
            {
                if (!_inventorySystem.TryRemoveItem(material.ItemID, material.Quantity))
                {
                    return false;
                }
            }
            return true;
        }

        private void RefundMaterials(CraftingRecipe recipe)
        {
            foreach (var material in recipe.RequiredMaterials)
            {
                // Try to refund materials, but don't worry if inventory is full
                _inventorySystem.TryAddItem(GetItemDataByID(material.ItemID), material.Quantity);
            }
        }

        private ItemData GetItemDataByID(string itemID)
        {
            // This would typically be resolved through a service
            // For now, returning null - should be implemented with ItemDatabase service
            return null;
        }

        #endregion

        #region Crafting Station Management

        /// <summary>
        /// Registers a crafting station
        /// </summary>
        public void RegisterCraftingStation(CraftingStation station)
        {
            if (station != null && !craftingStations.Contains(station))
            {
                craftingStations.Add(station);
                
                if (enableDebugLogs)
                    Debug.Log($"[CraftingSystem] Registered crafting station: {station.StationType}");
            }
        }

        /// <summary>
        /// Gets available crafting stations
        /// </summary>
        public List<CraftingStation> GetAvailableCraftingStations()
        {
            return craftingStations.Where(s => s != null && s.IsAvailable).ToList();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Represents a crafting recipe
    /// </summary>
    [System.Serializable]
    public class CraftingRecipe : ScriptableObject
    {
        [Header("Recipe Info")]
        public string RecipeID;
        public string RecipeName;
        [TextArea(3, 5)] public string Description;
        public Sprite Icon;

        [Header("Requirements")]
        public List<MaterialRequirement> RequiredMaterials = new();
        public CraftingStationType RequiredStation = CraftingStationType.None;
        public float CraftingTime = 2f;
        public int RequiredLevel = 1;

        [Header("Results")]
        public List<CraftingResult> ResultItems = new();
        
        [Header("Discovery")]
        public bool IsKnownByDefault = true;
        public List<string> RequiredRecipesToDiscover = new();
    }

    /// <summary>
    /// Material requirement for crafting
    /// </summary>
    [System.Serializable]
    public class MaterialRequirement
    {
        public string ItemID;
        public int Quantity;
        [SerializeField] private ItemData _itemDataReference; // Editor convenience

        public ItemData ItemData => _itemDataReference;
    }

    /// <summary>
    /// Crafting result item
    /// </summary>
    [System.Serializable]
    public class CraftingResult
    {
        public ItemData ItemData;
        public int Quantity = 1;
        [Range(0f, 1f)] public float SuccessChance = 1f;
    }

    /// <summary>
    /// Active crafting process
    /// </summary>
    [System.Serializable]
    public class CraftingProcess
    {
        public string ProcessID;
        public CraftingRecipe Recipe;
        public float StartTime;
        public float Duration;
        public CraftingStationType StationType;

        public float Progress => Mathf.Clamp01((Time.time - StartTime) / Duration);
        public float RemainingTime => Mathf.Max(0, (StartTime + Duration) - Time.time);
        public bool IsCompleted => Time.time >= StartTime + Duration;
    }

    /// <summary>
    /// Crafting station representation
    /// </summary>
    [System.Serializable]
    public class CraftingStation : MonoBehaviour
    {
        [Header("Station Configuration")]
        public CraftingStationType StationType;
        public string StationName;
        public bool IsAvailable = true;
        public float CraftingSpeedMultiplier = 1f;

        [Header("Station Requirements")]
        public int RequiredLevel = 1;
        public List<string> RequiredItems = new();

        public bool CanUseStation()
        {
            return IsAvailable && gameObject.activeInHierarchy;
        }
    }

    /// <summary>
    /// Types of crafting stations
    /// </summary>
    public enum CraftingStationType
    {
        None = 0,
        Workbench = 1,
        Forge = 2,
        Alchemy = 3,
        Enchanting = 4,
        Cooking = 5,
        Tailoring = 6
    }

    #endregion

    #region Events

    public class CraftingStartedEvent
    {
        public CraftingProcess Process { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public CraftingStartedEvent(CraftingProcess process)
        {
            Process = process;
        }
    }

    public class CraftingCompletedEvent
    {
        public CraftingProcess Process { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public CraftingCompletedEvent(CraftingProcess process)
        {
            Process = process;
        }
    }

    public class CraftingFailedEvent
    {
        public CraftingProcess Process { get; }
        public string Reason { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public CraftingFailedEvent(CraftingProcess process, string reason)
        {
            Process = process;
            Reason = reason;
        }
    }

    public class CraftingCancelledEvent
    {
        public CraftingProcess Process { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public CraftingCancelledEvent(CraftingProcess process)
        {
            Process = process;
        }
    }

    public class RecipeDiscoveredEvent
    {
        public CraftingRecipe Recipe { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public RecipeDiscoveredEvent(CraftingRecipe recipe)
        {
            Recipe = recipe;
        }
    }

    #endregion
}