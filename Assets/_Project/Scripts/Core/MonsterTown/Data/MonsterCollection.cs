using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Events;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Monster Collection - manages a player's collection of monsters
    /// Handles storage, organization, and collection statistics
    /// </summary>
    [System.Serializable]
    public class MonsterCollection
    {
        [Header("Collection Configuration")]
        [SerializeField] private int maxCapacity = 100;
        [SerializeField] private bool allowDuplicates = true;

        // Collection data
        private Dictionary<string, MonsterInstance> monsters = new();
        private Dictionary<string, int> speciesCount = new();
        private Dictionary<string, List<string>> speciesGroups = new();

        // Collection statistics
        private CollectionStats stats = new();

        #region Public API

        /// <summary>
        /// Get all monsters in collection as enumerable
        /// </summary>
        public IEnumerable<MonsterInstance> Monsters => monsters.Values;

        /// <summary>
        /// Add monster to collection
        /// </summary>
        public bool AddMonster(MonsterInstance monster)
        {
            if (monster == null)
            {
                Debug.LogWarning("Cannot add null monster to collection");
                return false;
            }

            if (monsters.Count >= maxCapacity)
            {
                Debug.LogWarning("Monster collection is at capacity");
                return false;
            }

            if (monsters.ContainsKey(monster.UniqueId))
            {
                Debug.LogWarning($"Monster {monster.UniqueId} already in collection");
                return false;
            }

            // Check for duplicates if not allowed
            if (!allowDuplicates)
            {
                if (speciesCount.ContainsKey(monster.Species) && speciesCount[monster.Species] > 0)
                {
                    Debug.LogWarning($"Duplicate monster of species {monster.Species} not allowed");
                    return false;
                }
            }

            // Add to main collection
            monsters[monster.UniqueId] = monster;

            // Update species tracking
            if (!speciesCount.ContainsKey(monster.Species))
            {
                speciesCount[monster.Species] = 0;
                speciesGroups[monster.Species] = new List<string>();
            }

            speciesCount[monster.Species]++;
            speciesGroups[monster.Species].Add(monster.UniqueId);

            // Update statistics
            UpdateCollectionStats();

            Debug.Log($"📚 Added {monster.Name} to collection. Total: {monsters.Count}/{maxCapacity}");
            return true;
        }

        /// <summary>
        /// Remove monster from collection
        /// </summary>
        public bool RemoveMonster(string monsterId)
        {
            if (!monsters.TryGetValue(monsterId, out var monster))
            {
                Debug.LogWarning($"Monster {monsterId} not found in collection");
                return false;
            }

            // Remove from main collection
            monsters.Remove(monsterId);

            // Update species tracking
            if (speciesCount.ContainsKey(monster.Species))
            {
                speciesCount[monster.Species]--;
                if (speciesCount[monster.Species] <= 0)
                {
                    speciesCount.Remove(monster.Species);
                    speciesGroups.Remove(monster.Species);
                }
                else
                {
                    speciesGroups[monster.Species].Remove(monsterId);
                }
            }

            // Update statistics
            UpdateCollectionStats();

            Debug.Log($"📚 Removed {monster.Name} from collection. Total: {monsters.Count}/{maxCapacity}");
            return true;
        }

        /// <summary>
        /// Get monster by ID
        /// </summary>
        public MonsterInstance GetMonster(string monsterId)
        {
            return monsters.TryGetValue(monsterId, out var monster) ? monster : null;
        }

        /// <summary>
        /// Get all monsters in collection
        /// </summary>
        public List<MonsterInstance> GetAllMonsters()
        {
            return new List<MonsterInstance>(monsters.Values);
        }

        /// <summary>
        /// Get monsters by species
        /// </summary>
        public List<MonsterInstance> GetMonstersBySpecies(string species)
        {
            if (!speciesGroups.TryGetValue(species, out var monsterIds))
                return new List<MonsterInstance>();

            return monsterIds.Select(id => monsters[id]).ToList();
        }

        /// <summary>
        /// Get monsters by generation
        /// </summary>
        public List<MonsterInstance> GetMonstersByGeneration(int generation)
        {
            return monsters.Values.Where(m => m.Generation == generation).ToList();
        }

        /// <summary>
        /// Get monsters by level range
        /// </summary>
        public List<MonsterInstance> GetMonstersByLevelRange(int minLevel, int maxLevel)
        {
            return monsters.Values.Where(m => m.Level >= minLevel && m.Level <= maxLevel).ToList();
        }

        /// <summary>
        /// Search monsters by name
        /// </summary>
        public List<MonsterInstance> SearchMonstersByName(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();
            return monsters.Values.Where(m => m.Name.ToLower().Contains(searchTerm)).ToList();
        }

        /// <summary>
        /// Get monsters sorted by criteria
        /// </summary>
        public List<MonsterInstance> GetMonstersSorted(SortCriteria criteria, bool ascending = true)
        {
            var sorted = monsters.Values.AsEnumerable();

            sorted = criteria switch
            {
                SortCriteria.Name => ascending ? sorted.OrderBy(m => m.Name) : sorted.OrderByDescending(m => m.Name),
                SortCriteria.Level => ascending ? sorted.OrderBy(m => m.Level) : sorted.OrderByDescending(m => m.Level),
                SortCriteria.Species => ascending ? sorted.OrderBy(m => m.Species) : sorted.OrderByDescending(m => m.Species),
                SortCriteria.Age => ascending ? sorted.OrderBy(m => m.BirthTime) : sorted.OrderByDescending(m => m.BirthTime),
                SortCriteria.Happiness => ascending ? sorted.OrderBy(m => m.Happiness) : sorted.OrderByDescending(m => m.Happiness),
                SortCriteria.Genetics => ascending ? sorted.OrderBy(m => m.Genetics.GetOverallFitness()) : sorted.OrderByDescending(m => m.Genetics.GetOverallFitness()),
                _ => sorted.OrderBy(m => m.Name)
            };

            return sorted.ToList();
        }

        /// <summary>
        /// Get collection statistics
        /// </summary>
        public CollectionStats GetCollectionStats()
        {
            return stats;
        }

        /// <summary>
        /// Get species diversity information
        /// </summary>
        public List<SpeciesInfo> GetSpeciesDiversity()
        {
            var speciesInfo = new List<SpeciesInfo>();

            foreach (var kvp in speciesCount)
            {
                var species = kvp.Key;
                var count = kvp.Value;
                var monsters = GetMonstersBySpecies(species);

                speciesInfo.Add(new SpeciesInfo
                {
                    Name = species,
                    Count = count,
                    AverageLevel = (float)monsters.Average(m => m.Level),
                    AverageHappiness = monsters.Average(m => m.Happiness),
                    AverageGenetics = monsters.Average(m => m.Genetics.GetOverallFitness()),
                    HighestLevel = monsters.Max(m => m.Level),
                    OldestMonster = monsters.OrderBy(m => m.BirthTime).First()
                });
            }

            return speciesInfo.OrderByDescending(s => s.Count).ToList();
        }

        /// <summary>
        /// Get top monsters by criteria
        /// </summary>
        public List<MonsterInstance> GetTopMonsters(SortCriteria criteria, int count = 5)
        {
            var sorted = GetMonstersSorted(criteria, false); // Descending order
            return sorted.Take(count).ToList();
        }

        /// <summary>
        /// Check collection achievements
        /// </summary>
        public List<CollectionAchievement> CheckAchievements()
        {
            var achievements = new List<CollectionAchievement>();

            // Total monsters achievement
            if (monsters.Count >= 10)
                achievements.Add(new CollectionAchievement { Name = "Collector", Description = "Own 10+ monsters" });
            if (monsters.Count >= 50)
                achievements.Add(new CollectionAchievement { Name = "Master Collector", Description = "Own 50+ monsters" });

            // Species diversity achievement
            if (speciesCount.Count >= 5)
                achievements.Add(new CollectionAchievement { Name = "Diverse Collection", Description = "Own 5+ different species" });

            // High-level monsters achievement
            var highLevelCount = monsters.Values.Count(m => m.Level >= 20);
            if (highLevelCount >= 5)
                achievements.Add(new CollectionAchievement { Name = "Elite Trainer", Description = "Own 5+ high-level monsters" });

            // Multi-generation achievement
            var maxGeneration = monsters.Values.Any() ? monsters.Values.Max(m => m.Generation) : 0;
            if (maxGeneration >= 3)
                achievements.Add(new CollectionAchievement { Name = "Dynasty Builder", Description = "Reach 3rd generation monsters" });

            return achievements;
        }

        /// <summary>
        /// Get collection capacity information
        /// </summary>
        public CollectionCapacityInfo GetCapacityInfo()
        {
            return new CollectionCapacityInfo
            {
                Current = monsters.Count,
                Maximum = maxCapacity,
                Percentage = (float)monsters.Count / maxCapacity,
                Remaining = maxCapacity - monsters.Count
            };
        }

        /// <summary>
        /// Export collection data for backup/transfer
        /// </summary>
        public CollectionExportData ExportCollection()
        {
            return new CollectionExportData
            {
                Monsters = GetAllMonsters(),
                ExportDate = DateTime.Now,
                TotalCount = monsters.Count,
                SpeciesCount = speciesCount.Count,
                Statistics = stats
            };
        }

        #endregion

        #region Private Methods

        private void UpdateCollectionStats()
        {
            if (monsters.Count == 0)
            {
                stats = new CollectionStats();
                return;
            }

            var allMonsters = monsters.Values.ToList();

            stats.TotalMonsters = allMonsters.Count;
            stats.UniqueSpecies = speciesCount.Count;
            stats.AverageLevel = (float)allMonsters.Average(m => m.Level);
            stats.AverageHappiness = allMonsters.Average(m => m.Happiness);
            stats.AverageGenetics = allMonsters.Average(m => m.Genetics.GetOverallFitness());
            stats.HighestLevel = allMonsters.Max(m => m.Level);
            stats.LowestLevel = allMonsters.Min(m => m.Level);
            stats.OldestMonster = allMonsters.OrderBy(m => m.BirthTime).First().Name;
            stats.NewestMonster = allMonsters.OrderByDescending(m => m.BirthTime).First().Name;
            stats.MostCommonSpecies = speciesCount.OrderByDescending(kvp => kvp.Value).First().Key;
            stats.MaxGeneration = allMonsters.Max(m => m.Generation);
            stats.LastUpdated = DateTime.Now;
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class CollectionStats
    {
        public int TotalMonsters;
        public int UniqueSpecies;
        public float AverageLevel;
        public float AverageHappiness;
        public float AverageGenetics;
        public int HighestLevel;
        public int LowestLevel;
        public string OldestMonster;
        public string NewestMonster;
        public string MostCommonSpecies;
        public int MaxGeneration;
        public DateTime LastUpdated;
    }

    [System.Serializable]
    public class SpeciesInfo
    {
        public string Name;
        public int Count;
        public float AverageLevel;
        public float AverageHappiness;
        public float AverageGenetics;
        public int HighestLevel;
        public MonsterInstance OldestMonster;
    }

    [System.Serializable]
    public class CollectionAchievement
    {
        public string Name;
        public string Description;
        public DateTime UnlockedDate = DateTime.Now;
    }

    [System.Serializable]
    public class CollectionCapacityInfo
    {
        public int Current;
        public int Maximum;
        public float Percentage;
        public int Remaining;
    }

    [System.Serializable]
    public class CollectionExportData
    {
        public List<MonsterInstance> Monsters;
        public DateTime ExportDate;
        public int TotalCount;
        public int SpeciesCount;
        public CollectionStats Statistics;
    }

    public enum SortCriteria
    {
        Name,
        Level,
        Species,
        Age,
        Happiness,
        Genetics
    }

    #endregion
}