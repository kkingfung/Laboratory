using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Equipment Inventory - manages a player's equipment collection
    /// Handles storage, organization, and equipment management
    /// </summary>
    [System.Serializable]
    public class EquipmentInventory
    {
        [Header("Inventory Configuration")]
        [SerializeField] private int maxCapacity = 200;
        [SerializeField] private bool autoStackSimilar = true;
        [SerializeField] private bool autoSellJunk = false;

        // Inventory data
        private Dictionary<string, EquipmentStack> equipmentStacks = new();
        private Dictionary<EquipmentType, List<string>> typeGroups = new();
        private Dictionary<EquipmentRarity, List<string>> rarityGroups = new();

        // Inventory statistics
        private InventoryStats stats = new();

        #region Public API

        /// <summary>
        /// Add equipment to inventory
        /// </summary>
        public bool AddEquipment(Equipment equipment, int quantity = 1)
        {
            if (equipment == null)
            {
                Debug.LogWarning("Cannot add null equipment to inventory");
                return false;
            }

            if (GetTotalItemCount() + quantity > maxCapacity)
            {
                Debug.LogWarning("Equipment inventory is at capacity");
                return false;
            }

            string stackKey = GetStackKey(equipment);

            // Check if we can stack with existing items
            if (autoStackSimilar && equipmentStacks.TryGetValue(stackKey, out var existingStack))
            {
                if (CanStack(existingStack.Equipment, equipment))
                {
                    existingStack.Quantity += quantity;
                    UpdateInventoryStats();
                    Debug.Log($"🎒 Stacked {quantity}x {equipment.Name}. Total: {existingStack.Quantity}");
                    return true;
                }
            }

            // Create new stack
            var newStack = new EquipmentStack
            {
                Equipment = equipment,
                Quantity = quantity,
                AcquiredDate = DateTime.Now
            };

            equipmentStacks[stackKey] = newStack;

            // Update groupings
            UpdateTypeGrouping(equipment, stackKey);
            UpdateRarityGrouping(equipment, stackKey);

            UpdateInventoryStats();

            // Auto-sell common items if enabled
            if (autoSellJunk && equipment.Rarity == EquipmentRarity.Common)
            {
                int sellValue = CalculateSellValue(equipment) * quantity;
                RemoveEquipment(stackKey, quantity);
                Debug.Log($"💰 Auto-sold {quantity}x {equipment.Name} for {sellValue} coins");
                return true;
            }

            Debug.Log($"🎒 Added {quantity}x {equipment.Name} to inventory");
            return true;
        }

        /// <summary>
        /// Remove equipment from inventory
        /// </summary>
        public bool RemoveEquipment(string equipmentId, int quantity = 1)
        {
            var stack = GetEquipmentStack(equipmentId);
            if (stack == null)
            {
                Debug.LogWarning($"Equipment {equipmentId} not found in inventory");
                return false;
            }

            if (stack.Quantity < quantity)
            {
                Debug.LogWarning($"Not enough {stack.Equipment.Name} in inventory. Have: {stack.Quantity}, Need: {quantity}");
                return false;
            }

            stack.Quantity -= quantity;

            if (stack.Quantity <= 0)
            {
                // Remove empty stack
                string stackKey = GetStackKey(stack.Equipment);
                equipmentStacks.Remove(stackKey);

                // Update groupings
                RemoveFromTypeGrouping(stack.Equipment, stackKey);
                RemoveFromRarityGrouping(stack.Equipment, stackKey);
            }

            UpdateInventoryStats();

            Debug.Log($"🎒 Removed {quantity}x {stack.Equipment.Name} from inventory");
            return true;
        }

        /// <summary>
        /// Get equipment stack by ID
        /// </summary>
        public EquipmentStack GetEquipmentStack(string equipmentId)
        {
            return equipmentStacks.Values.FirstOrDefault(stack => stack.Equipment.ItemId == equipmentId);
        }

        /// <summary>
        /// Get all equipment stacks
        /// </summary>
        public List<EquipmentStack> GetAllEquipment()
        {
            return new List<EquipmentStack>(equipmentStacks.Values);
        }

        /// <summary>
        /// Get equipment by type
        /// </summary>
        public List<EquipmentStack> GetEquipmentByType(EquipmentType type)
        {
            if (!typeGroups.TryGetValue(type, out var stackKeys))
                return new List<EquipmentStack>();

            return stackKeys.Select(key => equipmentStacks[key]).ToList();
        }

        /// <summary>
        /// Get equipment by rarity
        /// </summary>
        public List<EquipmentStack> GetEquipmentByRarity(EquipmentRarity rarity)
        {
            if (!rarityGroups.TryGetValue(rarity, out var stackKeys))
                return new List<EquipmentStack>();

            return stackKeys.Select(key => equipmentStacks[key]).ToList();
        }

        /// <summary>
        /// Get equipment sorted by criteria
        /// </summary>
        public List<EquipmentStack> GetEquipmentSorted(EquipmentSortCriteria criteria, bool ascending = true)
        {
            var sorted = equipmentStacks.Values.AsEnumerable();

            sorted = criteria switch
            {
                EquipmentSortCriteria.Name => ascending
                    ? sorted.OrderBy(s => s.Equipment.Name)
                    : sorted.OrderByDescending(s => s.Equipment.Name),
                EquipmentSortCriteria.Type => ascending
                    ? sorted.OrderBy(s => s.Equipment.Type)
                    : sorted.OrderByDescending(s => s.Equipment.Type),
                EquipmentSortCriteria.Rarity => ascending
                    ? sorted.OrderBy(s => s.Equipment.Rarity)
                    : sorted.OrderByDescending(s => s.Equipment.Rarity),
                EquipmentSortCriteria.Level => ascending
                    ? sorted.OrderBy(s => s.Equipment.Level)
                    : sorted.OrderByDescending(s => s.Equipment.Level),
                EquipmentSortCriteria.Quantity => ascending
                    ? sorted.OrderBy(s => s.Quantity)
                    : sorted.OrderByDescending(s => s.Quantity),
                EquipmentSortCriteria.AcquiredDate => ascending
                    ? sorted.OrderBy(s => s.AcquiredDate)
                    : sorted.OrderByDescending(s => s.AcquiredDate),
                _ => sorted.OrderBy(s => s.Equipment.Name)
            };

            return sorted.ToList();
        }

        /// <summary>
        /// Search equipment by name
        /// </summary>
        public List<EquipmentStack> SearchEquipment(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();
            return equipmentStacks.Values
                .Where(stack => stack.Equipment.Name.ToLower().Contains(searchTerm) ||
                               stack.Equipment.Description.ToLower().Contains(searchTerm))
                .ToList();
        }

        /// <summary>
        /// Get equipment that can be equipped by monster
        /// </summary>
        public List<EquipmentStack> GetEquippableFor(MonsterInstance monster)
        {
            return equipmentStacks.Values
                .Where(stack => CanMonsterEquip(monster, stack.Equipment))
                .ToList();
        }

        /// <summary>
        /// Get best equipment for monster and slot
        /// </summary>
        public EquipmentStack GetBestEquipmentFor(MonsterInstance monster, EquipmentType type)
        {
            var candidates = GetEquipmentByType(type)
                .Where(stack => CanMonsterEquip(monster, stack.Equipment))
                .ToList();

            if (!candidates.Any())
                return null;

            // Score equipment based on monster's needs
            return candidates
                .OrderByDescending(stack => ScoreEquipmentForMonster(monster, stack.Equipment))
                .First();
        }

        /// <summary>
        /// Auto-organize inventory
        /// </summary>
        public void OrganizeInventory()
        {
            // Group similar items together
            var reorganized = new Dictionary<string, EquipmentStack>();

            foreach (var stack in equipmentStacks.Values.OrderBy(s => s.Equipment.Type).ThenBy(s => s.Equipment.Rarity))
            {
                string newKey = GetStackKey(stack.Equipment);
                reorganized[newKey] = stack;
            }

            equipmentStacks = reorganized;

            // Rebuild groupings
            RebuildGroupings();

            Debug.Log("🗂️ Inventory organized");
        }

        /// <summary>
        /// Get inventory statistics
        /// </summary>
        public InventoryStats GetInventoryStats()
        {
            return stats;
        }

        /// <summary>
        /// Get capacity information
        /// </summary>
        public InventoryCapacityInfo GetCapacityInfo()
        {
            int totalItems = GetTotalItemCount();
            return new InventoryCapacityInfo
            {
                Current = totalItems,
                Maximum = maxCapacity,
                Percentage = (float)totalItems / maxCapacity,
                Remaining = maxCapacity - totalItems
            };
        }

        /// <summary>
        /// Sell junk equipment automatically
        /// </summary>
        public int SellJunkEquipment()
        {
            var junkItems = equipmentStacks.Values
                .Where(stack => IsJunkEquipment(stack.Equipment))
                .ToList();

            int coinsSold = 0;
            foreach (var junk in junkItems)
            {
                coinsSold += CalculateSellValue(junk.Equipment) * junk.Quantity;
                string stackKey = GetStackKey(junk.Equipment);
                equipmentStacks.Remove(stackKey);
            }

            if (junkItems.Count > 0)
            {
                RebuildGroupings();
                UpdateInventoryStats();
                Debug.Log($"💰 Sold {junkItems.Count} junk items for {coinsSold} coins");
            }

            return coinsSold;
        }

        #endregion

        #region Private Methods

        private string GetStackKey(Equipment equipment)
        {
            // Create unique key for stacking similar items
            return $"{equipment.Name}_{equipment.Type}_{equipment.Rarity}_{equipment.Level}";
        }

        private bool CanStack(Equipment existing, Equipment newItem)
        {
            return existing.Name == newItem.Name &&
                   existing.Type == newItem.Type &&
                   existing.Rarity == newItem.Rarity &&
                   existing.Level == newItem.Level;
        }

        private int GetTotalItemCount()
        {
            return equipmentStacks.Values.Sum(stack => stack.Quantity);
        }

        private void UpdateTypeGrouping(Equipment equipment, string stackKey)
        {
            if (!typeGroups.ContainsKey(equipment.Type))
            {
                typeGroups[equipment.Type] = new List<string>();
            }

            if (!typeGroups[equipment.Type].Contains(stackKey))
            {
                typeGroups[equipment.Type].Add(stackKey);
            }
        }

        private void UpdateRarityGrouping(Equipment equipment, string stackKey)
        {
            if (!rarityGroups.ContainsKey(equipment.Rarity))
            {
                rarityGroups[equipment.Rarity] = new List<string>();
            }

            if (!rarityGroups[equipment.Rarity].Contains(stackKey))
            {
                rarityGroups[equipment.Rarity].Add(stackKey);
            }
        }

        private void RemoveFromTypeGrouping(Equipment equipment, string stackKey)
        {
            if (typeGroups.TryGetValue(equipment.Type, out var typeList))
            {
                typeList.Remove(stackKey);
                if (typeList.Count == 0)
                {
                    typeGroups.Remove(equipment.Type);
                }
            }
        }

        private void RemoveFromRarityGrouping(Equipment equipment, string stackKey)
        {
            if (rarityGroups.TryGetValue(equipment.Rarity, out var rarityList))
            {
                rarityList.Remove(stackKey);
                if (rarityList.Count == 0)
                {
                    rarityGroups.Remove(equipment.Rarity);
                }
            }
        }

        private void RebuildGroupings()
        {
            typeGroups.Clear();
            rarityGroups.Clear();

            foreach (var kvp in equipmentStacks)
            {
                string stackKey = kvp.Key;
                var equipment = kvp.Value.Equipment;

                UpdateTypeGrouping(equipment, stackKey);
                UpdateRarityGrouping(equipment, stackKey);
            }
        }

        private void UpdateInventoryStats()
        {
            if (equipmentStacks.Count == 0)
            {
                stats = new InventoryStats();
                return;
            }

            var allStacks = equipmentStacks.Values.ToList();
            var allEquipment = allStacks.SelectMany(s => Enumerable.Repeat(s.Equipment, s.Quantity)).ToList();

            stats.TotalItems = GetTotalItemCount();
            stats.UniqueItems = equipmentStacks.Count;
            stats.TypeCounts = typeGroups.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Sum(key => equipmentStacks[key].Quantity));
            stats.RarityCounts = rarityGroups.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Sum(key => equipmentStacks[key].Quantity));
            stats.AverageLevel = (float)allEquipment.Average(e => e.Level);
            stats.HighestLevel = allEquipment.Max(e => e.Level);
            stats.MostCommonType = stats.TypeCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
            stats.MostCommonRarity = stats.RarityCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
            stats.LastUpdated = DateTime.Now;
        }

        private bool CanMonsterEquip(MonsterInstance monster, Equipment equipment)
        {
            // Basic level requirement
            if (monster.Level < equipment.Level)
                return false;

            // Could add more complex requirements here
            return true;
        }

        private float ScoreEquipmentForMonster(MonsterInstance monster, Equipment equipment)
        {
            float score = 0f;

            // Base score from equipment level
            score += equipment.Level * 10f;

            // Rarity bonus
            score += (int)equipment.Rarity * 5f;

            // Activity compatibility bonus
            foreach (var activityType in equipment.ActivityBonuses)
            {
                score += equipment.GetActivityBonus(activityType) * 20f;
            }

            // Stat synergy bonus based on monster's genetics
            foreach (var statBonus in equipment.StatBonuses)
            {
                float geneticValue = GetGeneticValue(monster, statBonus.Key);
                score += statBonus.Value * geneticValue * 2f;
            }

            return score;
        }

        private float GetGeneticValue(MonsterInstance monster, StatType statType)
        {
            return statType switch
            {
                StatType.Strength => monster.Genetics.GetTraitValue("Strength"),
                StatType.Agility => monster.Genetics.GetTraitValue("Agility"),
                StatType.Intelligence => monster.Genetics.GetTraitValue("Intelligence"),
                StatType.Vitality => monster.Genetics.GetTraitValue("Vitality"),
                StatType.Social => monster.Genetics.GetTraitValue("Social"),
                _ => 0.5f
            };
        }

        private bool IsJunkEquipment(Equipment equipment)
        {
            // Define junk criteria
            return equipment.Rarity == EquipmentRarity.Common &&
                   equipment.Level < 5 &&
                   equipment.StatBonuses.Values.All(v => v < 10f);
        }

        private int CalculateSellValue(Equipment equipment)
        {
            int baseValue = equipment.Level * (int)equipment.Rarity * 5;
            return Mathf.Max(1, baseValue);
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class EquipmentStack
    {
        public Equipment Equipment;
        public int Quantity;
        public DateTime AcquiredDate;
    }

    [System.Serializable]
    public class InventoryStats
    {
        public int TotalItems;
        public int UniqueItems;
        public Dictionary<EquipmentType, int> TypeCounts = new();
        public Dictionary<EquipmentRarity, int> RarityCounts = new();
        public float AverageLevel;
        public int HighestLevel;
        public EquipmentType MostCommonType;
        public EquipmentRarity MostCommonRarity;
        public DateTime LastUpdated;
    }

    [System.Serializable]
    public class InventoryCapacityInfo
    {
        public int Current;
        public int Maximum;
        public float Percentage;
        public int Remaining;
    }

    public enum EquipmentSortCriteria
    {
        Name,
        Type,
        Rarity,
        Level,
        Quantity,
        AcquiredDate
    }

    #endregion
}