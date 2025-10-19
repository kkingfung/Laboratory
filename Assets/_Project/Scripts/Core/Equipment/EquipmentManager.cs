using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.MonsterTown;
using Laboratory.Core.Configuration;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.Equipment.Types;
using EquipmentItem = Laboratory.Core.MonsterTown.Equipment;
using ActivityType = Laboratory.Core.Activities.Types.ActivityType;

namespace Laboratory.Core.Equipment
{
    /// <summary>
    /// Equipment Manager - Handles all equipment mechanics for monsters
    ///
    /// Key Features:
    /// - Equipment bonuses directly affect monster performance in activities
    /// - 5 rarity tiers from Common to Legendary
    /// - Activity-specific bonuses (Racing gear helps in racing, etc.)
    /// - Set bonuses for wearing complete equipment sets
    /// - Equipment progression through upgrades and crafting
    /// - Designer-configurable through ScriptableObjects
    /// </summary>
    public class EquipmentManager : MonoBehaviour
    {
        [Header("🎒 Equipment Configuration")]
        [SerializeField] private EquipmentDatabase equipmentDatabase;
        [SerializeField] private bool enableSetBonuses = true;
        [SerializeField] private bool allowMultipleOfSameType = false;

        [Header("⚡ Performance Settings")]
        [SerializeField] private bool cacheEquipmentCalculations = true;
        [SerializeField] private float cacheUpdateFrequency = 5f;

        // Equipment storage and management
        private Dictionary<string, EquipmentItem> _globalEquipment = new();
        private Dictionary<string, List<EquipmentItem>> _monsterEquipment = new();
        private Dictionary<string, EquipmentPerformanceCache> _performanceCache = new();

        #region Equipment Database Management

        public void InitializeEquipmentSystem(EquipmentDatabase database)
        {
            equipmentDatabase = database;
            LoadEquipmentDatabase();

            if (cacheEquipmentCalculations)
            {
                InvokeRepeating(nameof(UpdatePerformanceCache), cacheUpdateFrequency, cacheUpdateFrequency);
            }

            Debug.Log($"⚔️ Equipment System initialized with {_globalEquipment.Count} items");
        }

        private void LoadEquipmentDatabase()
        {
            if (equipmentDatabase == null)
            {
                Debug.LogError("EquipmentManager: No equipment database assigned!");
                return;
            }

            _globalEquipment.Clear();

            foreach (var equipmentConfig in equipmentDatabase.AllEquipment)
            {
                var equipment = CreateEquipmentFromConfig(equipmentConfig);
                _globalEquipment[equipment.ItemId] = equipment;
            }
        }

        private EquipmentItem CreateEquipmentFromConfig(EquipmentConfig config)
        {
            return new EquipmentItem
            {
                ItemId = config.ItemId,
                Name = config.Name,
                Description = config.Description,
                Type = config.Type,
                Rarity = config.Rarity,
                StatBonuses = new Dictionary<StatType, float>(config.StatBonuses),
                ActivityBonuses = new List<ActivityType>(config.ActivityBonuses),
                Level = 1,
                IsEquipped = false
            };
        }

        #endregion

        #region Monster Equipment Management

        /// <summary>
        /// Equip an item to a monster
        /// </summary>
        public bool EquipItem(Monster monster, string itemId)
        {
            if (!_globalEquipment.TryGetValue(itemId, out var equipment))
            {
                Debug.LogError($"Equipment not found: {itemId}");
                return false;
            }

            if (!CanEquipItem(monster, equipment))
            {
                Debug.LogWarning($"Cannot equip {itemId} to {monster.Name}");
                return false;
            }

            // Initialize monster equipment list if needed
            if (!_monsterEquipment.ContainsKey(monster.UniqueId))
            {
                _monsterEquipment[monster.UniqueId] = new List<EquipmentItem>();
            }

            var monsterEquipment = _monsterEquipment[monster.UniqueId];

            // Remove existing equipment of same type if not allowing multiples
            if (!allowMultipleOfSameType)
            {
                var existingItem = monsterEquipment.FirstOrDefault(e => e.Type == equipment.Type);
                if (existingItem != null)
                {
                    UnequipItem(monster, existingItem.ItemId);
                }
            }

            // Clone equipment for monster (so we can have individual levels/upgrades)
            var monsterEquipmentCopy = CloneEquipment(equipment);
            monsterEquipmentCopy.IsEquipped = true;

            monsterEquipment.Add(monsterEquipmentCopy);

            // Update monster's equipment list
            monster.Equipment = monsterEquipment;

            // Invalidate performance cache
            InvalidatePerformanceCache(monster.UniqueId);

            Debug.Log($"🎒 {monster.Name} equipped {equipment.Name}");
            return true;
        }

        /// <summary>
        /// Unequip an item from a monster
        /// </summary>
        public bool UnequipItem(Monster monster, string itemId)
        {
            if (!_monsterEquipment.TryGetValue(monster.UniqueId, out var monsterEquipment))
                return false;

            var equipment = monsterEquipment.FirstOrDefault(e => e.ItemId == itemId);
            if (equipment == null)
                return false;

            equipment.IsEquipped = false;
            monsterEquipment.Remove(equipment);

            // Update monster's equipment list
            monster.Equipment = monsterEquipment;

            // Invalidate performance cache
            InvalidatePerformanceCache(monster.UniqueId);

            Debug.Log($"🎒 {monster.Name} unequipped {equipment.Name}");
            return true;
        }

        /// <summary>
        /// Check if a monster can equip a specific item
        /// </summary>
        public bool CanEquipItem(Monster monster, EquipmentItem equipment)
        {
            // Level requirement
            if (monster.Level < equipment.Level)
                return false;

            // Check if monster already has max equipment
            if (!_monsterEquipment.TryGetValue(monster.UniqueId, out var monsterEquipment))
                return true;

            var maxEquipmentSlots = 8; // Could be configurable
            if (monsterEquipment.Count >= maxEquipmentSlots && allowMultipleOfSameType)
                return false;

            return true;
        }

        #endregion

        #region Performance Calculation

        /// <summary>
        /// Calculate equipment performance bonuses for a monster in a specific activity
        /// </summary>
        public MonsterPerformance CalculateEquipmentBonuses(Monster monster, ActivityType activityType)
        {
            var cacheKey = $"{monster.UniqueId}_{activityType}";

            if (cacheEquipmentCalculations && _performanceCache.TryGetValue(cacheKey, out var cached))
            {
                if (Time.time - cached.LastUpdateTime < cacheUpdateFrequency)
                {
                    return cached.Performance;
                }
            }

            var performance = CalculateEquipmentBonusesInternal(monster, activityType);

            if (cacheEquipmentCalculations)
            {
                _performanceCache[cacheKey] = new EquipmentPerformanceCache
                {
                    Performance = performance,
                    LastUpdateTime = Time.time
                };
            }

            return performance;
        }

        private MonsterPerformance CalculateEquipmentBonusesInternal(Monster monster, ActivityType activityType)
        {
            var bonuses = new MonsterPerformance();

            if (!_monsterEquipment.TryGetValue(monster.UniqueId, out var equipment))
                return bonuses;

            // Calculate base equipment bonuses
            float totalActivityBonus = 0f;
            int equipmentCount = 0;

            foreach (var item in equipment.Where(e => e.IsEquipped))
            {
                // Activity-specific bonuses
                totalActivityBonus += item.GetActivityBonus(activityType);
                equipmentCount++;

                // Add stat bonuses (convert to performance values)
                foreach (var statBonus in item.StatBonuses)
                {
                    ApplyStatBonusToPerformance(ref bonuses, statBonus.Key, statBonus.Value * GetRarityMultiplier(item.Rarity));
                }
            }

            // Set equipment bonus
            bonuses.EquipmentBonus = totalActivityBonus;

            // Calculate set bonuses if enabled
            if (enableSetBonuses)
            {
                var setBonuses = CalculateSetBonuses(equipment, activityType);
                bonuses.EquipmentBonus += setBonuses;
            }

            // Apply equipment count bonus (having more equipment provides small general bonus)
            bonuses.EquipmentBonus += equipmentCount * 0.02f; // 2% per item

            return bonuses;
        }

        private void ApplyStatBonusToPerformance(ref MonsterPerformance performance, StatType statType, float bonusValue)
        {
            switch (statType)
            {
                case StatType.Strength:
                    performance.AttackPower += bonusValue / 100f;
                    break;
                case StatType.Agility:
                    performance.Agility += bonusValue / 100f;
                    performance.Handling += bonusValue / 150f;
                    break;
                case StatType.Vitality:
                    performance.Endurance += bonusValue / 100f;
                    performance.Defense += bonusValue / 100f;
                    break;
                case StatType.Speed:
                    performance.Speed += bonusValue / 100f;
                    break;
                case StatType.Intelligence:
                    performance.Intelligence += bonusValue / 100f;
                    performance.Creativity += bonusValue / 150f;
                    break;
                case StatType.Social:
                    performance.Creativity += bonusValue / 100f;
                    break;
            }
        }

        private float GetRarityMultiplier(EquipmentRarity rarity)
        {
            return rarity switch
            {
                EquipmentRarity.Common => 1.0f,
                EquipmentRarity.Uncommon => 1.2f,
                EquipmentRarity.Rare => 1.5f,
                EquipmentRarity.Epic => 2.0f,
                EquipmentRarity.Legendary => 3.0f,
                _ => 1.0f
            };
        }

        #endregion

        #region Set Bonuses

        /// <summary>
        /// Calculate set bonuses when wearing multiple pieces of the same equipment set
        /// </summary>
        private float CalculateSetBonuses(List<EquipmentItem> equipment, ActivityType activityType)
        {
            // Group equipment by set (using rarity as set identifier for now)
            var sets = equipment.GroupBy(e => e.Rarity).Where(g => g.Count() >= 2);

            float totalSetBonus = 0f;

            foreach (var set in sets)
            {
                int setCount = set.Count();
                var setRarity = set.Key;

                // Set bonus scales with number of pieces and rarity
                float setBonus = (setCount - 1) * 0.1f * GetRarityMultiplier(setRarity) * 0.5f;
                totalSetBonus += setBonus;

                // Special set bonuses for certain activities
                if (setCount >= 3)
                {
                    totalSetBonus += GetSpecialSetBonus(setRarity, activityType);
                }
            }

            return totalSetBonus;
        }

        private float GetSpecialSetBonus(EquipmentRarity rarity, ActivityType activityType)
        {
            // Special bonuses for wearing 3+ pieces of the same rarity
            return rarity switch
            {
                EquipmentRarity.Rare when activityType == ActivityType.Racing => 0.15f,
                EquipmentRarity.Epic when activityType == ActivityType.Combat => 0.20f,
                EquipmentRarity.Legendary => 0.25f, // Universal legendary set bonus
                _ => 0.05f
            };
        }

        #endregion

        #region Equipment Creation and Management

        /// <summary>
        /// Create new equipment instance (for rewards, crafting, etc.)
        /// </summary>
        public EquipmentItem CreateEquipment(string baseItemId, int level = 1, EquipmentRarity? overrideRarity = null)
        {
            if (!_globalEquipment.TryGetValue(baseItemId, out var baseEquipment))
            {
                Debug.LogError($"Cannot create equipment: {baseItemId} not found in database");
                return null;
            }

            var newEquipment = CloneEquipment(baseEquipment);
            newEquipment.Level = level;

            if (overrideRarity.HasValue)
            {
                newEquipment.Rarity = overrideRarity.Value;
            }

            // Scale bonuses with level
            var scaledBonuses = new Dictionary<StatType, float>();
            foreach (var bonus in newEquipment.StatBonuses)
            {
                scaledBonuses[bonus.Key] = bonus.Value * (1f + (level - 1) * 0.1f);
            }
            newEquipment.StatBonuses = scaledBonuses;

            return newEquipment;
        }

        /// <summary>
        /// Upgrade equipment level
        /// </summary>
        public bool UpgradeEquipment(Monster monster, string itemId, int levelsToAdd = 1)
        {
            if (!_monsterEquipment.TryGetValue(monster.UniqueId, out var equipment))
                return false;

            var item = equipment.FirstOrDefault(e => e.ItemId == itemId);
            if (item == null)
                return false;

            item.Level += levelsToAdd;

            // Recalculate stat bonuses
            foreach (var statType in item.StatBonuses.Keys.ToList())
            {
                var baseValue = item.StatBonuses[statType] / (1f + (item.Level - levelsToAdd - 1) * 0.1f);
                item.StatBonuses[statType] = baseValue * (1f + (item.Level - 1) * 0.1f);
            }

            InvalidatePerformanceCache(monster.UniqueId);

            Debug.Log($"⬆️ {item.Name} upgraded to level {item.Level}");
            return true;
        }

        #endregion

        #region Utility Methods

        private EquipmentItem CloneEquipment(EquipmentItem original)
        {
            return new EquipmentItem
            {
                ItemId = original.ItemId,
                Name = original.Name,
                Description = original.Description,
                Type = original.Type,
                Rarity = original.Rarity,
                StatBonuses = new Dictionary<StatType, float>(original.StatBonuses),
                ActivityBonuses = new List<ActivityType>(original.ActivityBonuses),
                Level = original.Level,
                IsEquipped = false
            };
        }

        private void InvalidatePerformanceCache(string monsterId)
        {
            var keysToRemove = _performanceCache.Keys.Where(k => k.StartsWith(monsterId)).ToList();
            foreach (var key in keysToRemove)
            {
                _performanceCache.Remove(key);
            }
        }

        private void UpdatePerformanceCache()
        {
            // Remove old cache entries
            var expiredKeys = _performanceCache.Where(kvp => Time.time - kvp.Value.LastUpdateTime > cacheUpdateFrequency * 2)
                                              .Select(kvp => kvp.Key).ToList();

            foreach (var key in expiredKeys)
            {
                _performanceCache.Remove(key);
            }
        }

        /// <summary>
        /// Get all equipment owned by a monster
        /// </summary>
        public List<EquipmentItem> GetMonsterEquipment(Monster monster)
        {
            return _monsterEquipment.TryGetValue(monster.UniqueId, out var equipment)
                ? new List<EquipmentItem>(equipment)
                : new List<EquipmentItem>();
        }

        /// <summary>
        /// Get equipment by item ID from global database
        /// </summary>
        public EquipmentItem GetEquipmentById(string itemId)
        {
            return _globalEquipment.TryGetValue(itemId, out var equipment) ? equipment : null;
        }

        #endregion
    }

    #region Supporting Data Structures

    /// <summary>
    /// Performance cache for equipment calculations
    /// </summary>
    [Serializable]
    public class EquipmentPerformanceCache
    {
        public MonsterPerformance Performance;
        public float LastUpdateTime;
    }

    /// <summary>
    /// Equipment configuration for ScriptableObject database
    /// </summary>
    [Serializable]
    public class EquipmentConfig
    {
        [Header("Basic Info")]
        public string ItemId;
        public string Name;
        [TextArea(3, 5)]
        public string Description;
        public EquipmentType Type;
        public EquipmentRarity Rarity;

        [Header("Stat Bonuses")]
        public Dictionary<StatType, float> StatBonuses = new();

        [Header("Activity Bonuses")]
        public List<ActivityType> ActivityBonuses = new();

        [Header("Visual")]
        public Sprite Icon;
        public GameObject VisualPrefab;
    }

    #endregion
}