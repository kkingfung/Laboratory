using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// System for handling item crafting
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        [SerializeField] private List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();
        
        public List<CraftingRecipe> AvailableRecipes => availableRecipes;
        
        /// <summary>
        /// Attempts to craft an item using the specified recipe
        /// </summary>
        public bool TryCraftItem(CraftingRecipe recipe, IInventorySystem inventory)
        {
            if (recipe == null || inventory == null) return false;
            
            // Check if player has all required materials
            foreach (var ingredient in recipe.RequiredItems)
            {
                if (!inventory.HasItem(ingredient.Item.ItemID, ingredient.Quantity))
                {
                    return false;
                }
            }
            
            // Remove materials from inventory
            foreach (var ingredient in recipe.RequiredItems)
            {
                inventory.RemoveItem(ingredient.Item, ingredient.Quantity);
            }
            
            // Add crafted item to inventory
            inventory.AddItem(recipe.ResultItem, recipe.ResultQuantity);
            
            return true;
        }
        
        /// <summary>
        /// Gets all recipes that can be crafted with current inventory
        /// </summary>
        public List<CraftingRecipe> GetCraftableRecipes(IInventorySystem inventory)
        {
            var craftableRecipes = new List<CraftingRecipe>();
            
            foreach (var recipe in availableRecipes)
            {
                bool canCraft = true;
                foreach (var ingredient in recipe.RequiredItems)
                {
                    if (!inventory.HasItem(ingredient.Item.ItemID, ingredient.Quantity))
                    {
                        canCraft = false;
                        break;
                    }
                }
                
                if (canCraft)
                {
                    craftableRecipes.Add(recipe);
                }
            }
            
            return craftableRecipes;
        }
    }
    
    /// <summary>
    /// Represents a crafting recipe
    /// </summary>
    [System.Serializable]
    public class CraftingRecipe
    {
        [SerializeField] private string recipeName;
        [SerializeField] private List<InventorySlot> requiredItems = new List<InventorySlot>();
        [SerializeField] private ItemData resultItem;
        [SerializeField] private int resultQuantity = 1;
        [SerializeField] private float craftingTime = 1f;
        
        public string RecipeName => recipeName;
        public List<InventorySlot> RequiredItems => requiredItems;
        public ItemData ResultItem => resultItem;
        public int ResultQuantity => resultQuantity;
        public float CraftingTime => craftingTime;
    }
    
    /// <summary>
    /// Save system for inventory data
    /// </summary>
    public class InventorySaveSystem : MonoBehaviour
    {
        [SerializeField] private string saveFileName = "inventory.json";
        
        /// <summary>
        /// Saves inventory data to file
        /// </summary>
        public void SaveInventory(IInventorySystem inventory)
        {
            var saveData = new InventorySaveData
            {
                items = inventory.GetAllItems()
            };
            
            string jsonData = JsonUtility.ToJson(saveData, true);
            string filePath = Application.persistentDataPath + "/" + saveFileName;
            System.IO.File.WriteAllText(filePath, jsonData);
        }
        
        /// <summary>
        /// Loads inventory data from file
        /// </summary>
        public void LoadInventory(IInventorySystem inventory)
        {
            string filePath = Application.persistentDataPath + "/" + saveFileName;
            
            if (System.IO.File.Exists(filePath))
            {
                string jsonData = System.IO.File.ReadAllText(filePath);
                var saveData = JsonUtility.FromJson<InventorySaveData>(jsonData);
                
                inventory.ClearInventory();
                foreach (var slot in saveData.items)
                {
                    if (!slot.IsEmpty)
                    {
                        inventory.AddItem(slot.Item, slot.Quantity);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Data structure for saving inventory
    /// </summary>
    [System.Serializable]
    public class InventorySaveData
    {
        public List<InventorySlot> items = new List<InventorySlot>();
    }
}
