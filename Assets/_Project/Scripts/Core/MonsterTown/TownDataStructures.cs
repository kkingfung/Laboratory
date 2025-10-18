using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Complete data structures for Monster Town system.
    /// Fills in missing types and ensures all references compile correctly.
    /// </summary>

    #region Monster and Creature Integration

    /// <summary>
    /// Bridge class for integrating existing Chimera creatures with Monster Town
    /// </summary>
    [Serializable]
    public class Monster
    {
        public string UniqueId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Monster";
        public int Level { get; set; } = 1;
        public float Happiness { get; set; } = 0.8f;
        public GeneticProfile GeneticProfile { get; set; }
        public MonsterStats Stats { get; set; } = new MonsterStats();
        public Dictionary<ActivityType, float> ActivityExperience { get; set; } = new();
        public List<Equipment> Equipment { get; set; } = new();
        public TownLocation CurrentLocation { get; set; } = TownLocation.TownCenter;
        public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Check if monster meets activity requirements
        /// </summary>
        public bool MeetsRequirements(ActivityRequirements requirements)
        {
            if (Level < requirements.MinLevel) return false;
            if (Happiness < requirements.MinHappiness) return false;

            // Check required stats
            if (requirements.RequiredStats != null)
            {
                foreach (var statName in requirements.RequiredStats)
                {
                    var statValue = GetStatValue(statName);
                    if (statValue < 30f) // Minimum threshold
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Apply cross-activity bonus from other activities
        /// </summary>
        public void ApplyCrossActivityBonus(ActivityType targetActivity, float bonusAmount)
        {
            if (!ActivityExperience.ContainsKey(targetActivity))
                ActivityExperience[targetActivity] = 0f;

            ActivityExperience[targetActivity] += bonusAmount;

            // Cap at reasonable maximum
            ActivityExperience[targetActivity] = Mathf.Min(ActivityExperience[targetActivity], 1000f);
        }

        /// <summary>
        /// Get stat value by name for requirement checking
        /// </summary>
        private float GetStatValue(string statName)
        {
            return statName.ToLower() switch
            {
                "strength" => Stats.strength,
                "agility" => Stats.agility,
                "vitality" => Stats.vitality,
                "intelligence" => Stats.intelligence,
                "social" => Stats.social,
                "adaptability" => Stats.adaptability,
                "speed" => Stats.speed,
                "charisma" => Stats.charisma,
                _ => 50f // Default value
            };
        }
    }

    #endregion

    #region Performance and Activity Data

    /// <summary>
    /// Monster performance metrics for activities
    /// </summary>
    [Serializable]
    public struct MonsterPerformance
    {
        [Header("Core Performance")]
        public float Speed;
        public float Endurance;
        public float Handling;
        public float AttackPower;
        public float Defense;
        public float Agility;
        public float Intelligence;
        public float Creativity;

        [Header("Activity Bonuses")]
        public float EquipmentBonus;
        public float ExperienceBonus;
        public float HappinessBonus;

        /// <summary>
        /// Calculate total performance for an activity
        /// </summary>
        public float CalculateTotal()
        {
            var basePerformance = (Speed + Endurance + AttackPower + Defense + Agility + Intelligence) / 6f;
            return Mathf.Clamp01(basePerformance + EquipmentBonus + ExperienceBonus + HappinessBonus);
        }

        /// <summary>
        /// Create performance from monster stats
        /// </summary>
        public static MonsterPerformance FromMonsterStats(MonsterStats stats)
        {
            return new MonsterPerformance
            {
                Speed = stats.speed / 100f,
                Endurance = stats.vitality / 100f,
                Handling = stats.agility / 100f,
                AttackPower = stats.strength / 100f,
                Defense = stats.vitality / 100f,
                Agility = stats.agility / 100f,
                Intelligence = stats.intelligence / 100f,
                Creativity = stats.social / 100f,
                EquipmentBonus = 0f,
                ExperienceBonus = 0f,
                HappinessBonus = 0f
            };
        }
    }


    #endregion

    #region Equipment and Rewards

    /// <summary>
    /// Equipment item that monsters can use
    /// </summary>
    [Serializable]
    public class Equipment
    {
        public string ItemId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public EquipmentType Type { get; set; }
        public EquipmentRarity Rarity { get; set; }
        public Dictionary<StatType, float> StatBonuses { get; set; } = new();
        public List<ActivityType> ActivityBonuses { get; set; } = new();
        public int Level { get; set; } = 1;
        public bool IsEquipped { get; set; } = false;

        /// <summary>
        /// Calculate bonus for specific activity
        /// </summary>
        public float GetActivityBonus(ActivityType activityType)
        {
            if (ActivityBonuses.Contains(activityType))
            {
                return 0.1f * Level * GetRarityMultiplier();
            }
            return 0f;
        }

        private float GetRarityMultiplier()
        {
            return Rarity switch
            {
                EquipmentRarity.Common => 1f,
                EquipmentRarity.Uncommon => 1.2f,
                EquipmentRarity.Rare => 1.5f,
                EquipmentRarity.Epic => 2f,
                EquipmentRarity.Legendary => 3f,
                _ => 1f
            };
        }
    }

    /// <summary>
    /// Reward given for completing activities
    /// </summary>
    [Serializable]
    public struct Reward
    {
        public RewardType Type;
        public int Amount;
        public string ItemId;
        public string Description;

        public override string ToString()
        {
            return Type switch
            {
                RewardType.Coins => $"{Amount} Coins",
                RewardType.Gems => $"{Amount} Gems",
                RewardType.ActivityTokens => $"{Amount} Activity Tokens",
                RewardType.Equipment => $"Equipment: {ItemId}",
                RewardType.Experience => $"{Amount} Experience",
                _ => $"{Type}: {Amount}"
            };
        }
    }

    #endregion

    #region Activity System Enums

    /// <summary>
    /// All available activity types covering every game genre
    /// </summary>
    public enum ActivityType
    {
        // Core Activities (matching existing implementation)
        Racing,
        Combat,
        Puzzle,
        Strategy,
        Adventure,
        Platforming,
        Music,
        Crafting,
        Exploration,
        Social,

        // Extended Activities (new implementations needed)
        Sports,
        Stealth,
        Rhythm,
        CardGame,
        BoardGame,
        Simulation,
        Detective,
        Survival,
        Building,
        Farming
    }

    /// <summary>
    /// Equipment types for different activities
    /// </summary>
    public enum EquipmentType
    {
        Weapon,
        Armor,
        Accessory,
        Tool,
        Vehicle,
        Instrument,
        Gadget,
        Boost
    }

    /// <summary>
    /// Equipment rarity levels
    /// </summary>
    public enum EquipmentRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// Types of rewards
    /// </summary>
    public enum RewardType
    {
        Coins,
        Gems,
        ActivityTokens,
        GeneticSamples,
        Materials,
        Energy,
        Equipment,
        Experience
    }

    /// <summary>
    /// Monster stat types for equipment bonuses
    /// </summary>
    public enum StatType
    {
        Strength,
        Agility,
        Vitality,
        Intelligence,
        Social,
        Adaptability,
        Speed,
        Charisma
    }

    #endregion

    #region Activity Requirements and Configuration

    /// <summary>
    /// Requirements for participating in activities
    /// </summary>
    [Serializable]
    public struct ActivityRequirements
    {
        public int MinLevel;
        public string[] RequiredStats;
        public string[] RequiredEquipment;
        public float MinHappiness;
        public int EnergyCost;

        public static ActivityRequirements GetDefault()
        {
            return new ActivityRequirements
            {
                MinLevel = 1,
                RequiredStats = new string[0],
                RequiredEquipment = new string[0],
                MinHappiness = 0.3f,
                EnergyCost = 10
            };
        }
    }

    /// <summary>
    /// Configuration for individual activities
    /// </summary>
    [Serializable]
    public struct ActivityConfig
    {
        public ActivityType Type;
        public string Name;
        public string Description;
        public ActivityRequirements Requirements;
        public float BaseRewardMultiplier;
        public bool IsUnlocked;
        public float DifficultyLevel;
        public int MaxParticipants;
        public float Duration; // In seconds

        public static ActivityConfig CreateDefault(ActivityType activityType)
        {
            return new ActivityConfig
            {
                Type = activityType,
                Name = activityType.ToString(),
                Description = $"Engage in {activityType.ToString().ToLower()} activities",
                Requirements = ActivityRequirements.GetDefault(),
                BaseRewardMultiplier = 1f,
                IsUnlocked = true,
                DifficultyLevel = 1f,
                MaxParticipants = 1,
                Duration = 30f
            };
        }
    }

    #endregion

    #region Statistics and Progression

    /// <summary>
    /// Monster statistics for different aspects
    /// </summary>
    [Serializable]
    public struct MonsterStats
    {
        [Header("Physical Stats")]
        public float strength;
        public float agility;
        public float vitality;
        public float speed;

        [Header("Mental Stats")]
        public float intelligence;
        public float adaptability;

        [Header("Social Stats")]
        public float social;
        public float charisma;

        /// <summary>
        /// Get total stat points
        /// </summary>
        public float GetTotalStats()
        {
            return strength + agility + vitality + speed + intelligence + adaptability + social + charisma;
        }

        /// <summary>
        /// Get average stat value
        /// </summary>
        public float GetAverageStats()
        {
            return GetTotalStats() / 8f;
        }

        /// <summary>
        /// Create balanced stats
        /// </summary>
        public static MonsterStats CreateBalanced(float baseValue = 50f)
        {
            return new MonsterStats
            {
                strength = baseValue,
                agility = baseValue,
                vitality = baseValue,
                speed = baseValue,
                intelligence = baseValue,
                adaptability = baseValue,
                social = baseValue,
                charisma = baseValue
            };
        }

        /// <summary>
        /// Create random stats within range
        /// </summary>
        public static MonsterStats CreateRandom(float min = 30f, float max = 70f)
        {
            return new MonsterStats
            {
                strength = UnityEngine.Random.Range(min, max),
                agility = UnityEngine.Random.Range(min, max),
                vitality = UnityEngine.Random.Range(min, max),
                speed = UnityEngine.Random.Range(min, max),
                intelligence = UnityEngine.Random.Range(min, max),
                adaptability = UnityEngine.Random.Range(min, max),
                social = UnityEngine.Random.Range(min, max),
                charisma = UnityEngine.Random.Range(min, max)
            };
        }
    }

    /// <summary>
    /// Town location tracking for monsters
    /// </summary>
    public enum TownLocation
    {
        TownCenter,
        BreedingCenter,
        TrainingGrounds,
        ActivityCenter,
        Habitat,
        Hospital,
        Laboratory,
        Shop,
        Adventure
    }

    #endregion

    #region Save/Load Data Structures

    /// <summary>
    /// Complete save data for Monster Town
    /// </summary>
    [Serializable]
    public struct MonsterTownSaveData
    {
        public string townName;
        public int townLevel;
        public TownResources resources;
        public List<MonsterSaveData> monsters;
        public List<BuildingSaveData> buildings;
        public Dictionary<ActivityType, float> activityProgress;
        public DateTime lastSaveTime;
        public float totalPlayTime;

        public static MonsterTownSaveData CreateDefault()
        {
            return new MonsterTownSaveData
            {
                townName = "New Monster Town",
                townLevel = 1,
                resources = TownResources.GetDefault(),
                monsters = new List<MonsterSaveData>(),
                buildings = new List<BuildingSaveData>(),
                activityProgress = new Dictionary<ActivityType, float>(),
                lastSaveTime = DateTime.UtcNow,
                totalPlayTime = 0f
            };
        }
    }

    /// <summary>
    /// Individual monster save data
    /// </summary>
    [Serializable]
    public struct MonsterSaveData
    {
        public string uniqueId;
        public string name;
        public int level;
        public float happiness;
        public MonsterStats stats;
        public Dictionary<ActivityType, float> activityExperience;
        public List<string> equipmentIds;
        public TownLocation currentLocation;

        public static MonsterSaveData FromMonster(Monster monster)
        {
            return new MonsterSaveData
            {
                uniqueId = monster.UniqueId,
                name = monster.Name,
                level = monster.Level,
                happiness = monster.Happiness,
                stats = monster.Stats,
                activityExperience = monster.ActivityExperience,
                equipmentIds = monster.Equipment.ConvertAll(e => e.ItemId),
                currentLocation = monster.CurrentLocation
            };
        }
    }

    /// <summary>
    /// Building save data
    /// </summary>
    [Serializable]
    public struct BuildingSaveData
    {
        public BuildingType buildingType;
        public Vector3 position;
        public int level;
        public bool isConstructed;
        public float health;
        public DateTime constructionTime;

        public static BuildingSaveData Create(BuildingType type, Vector3 pos, int lvl = 1)
        {
            return new BuildingSaveData
            {
                buildingType = type,
                position = pos,
                level = lvl,
                isConstructed = true,
                health = 100f,
                constructionTime = DateTime.UtcNow
            };
        }
    }

    #endregion
}