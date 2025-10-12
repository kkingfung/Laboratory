using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Subsystems.Genetics
{
    /// <summary>
    /// Manages genetic data storage, retrieval, and search operations for Project Chimera.
    /// Provides high-performance access to genetic profiles, breeding history, and trait databases.
    /// Supports both in-memory caching and persistent storage.
    /// </summary>
    public class GeneticDatabaseManager : MonoBehaviour, IGeneticDatabase
    {
        [Header("Configuration")]
        [SerializeField] private bool enablePersistentStorage = true;
        [SerializeField] private bool enableCaching = true;
        [SerializeField] private int maxCacheSize = 1000;

        private GeneticsSubsystemConfig _config;

        // In-memory storage
        private readonly Dictionary<string, GeneticProfile> _profileCache = new();
        private readonly Dictionary<string, BreedingHistory> _breedingHistories = new();
        private readonly Dictionary<string, List<string>> _traitIndex = new(); // trait -> list of profile IDs
        private readonly Dictionary<string, DateTime> _cacheTimestamps = new();

        // Statistics
        private int _totalProfiles = 0;
        private int _cacheHits = 0;
        private int _cacheMisses = 0;

        // Properties
        public bool IsInitialized { get; private set; }
        public int TotalProfiles => _totalProfiles;
        public int CachedProfiles => _profileCache.Count;
        public float CacheHitRatio => _cacheHits + _cacheMisses > 0 ? (float)_cacheHits / (_cacheHits + _cacheMisses) : 0f;

        #region Initialization

        public async Task InitializeAsync(GeneticsSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            enableCaching = _config.PerformanceConfig.EnableProfileCaching;
            maxCacheSize = _config.PerformanceConfig.MaxCachedProfiles;

            // Initialize trait index
            InitializeTraitIndex();

            // Load existing data if persistent storage is enabled
            if (enablePersistentStorage)
            {
                await LoadPersistedData();
            }

            // Start cache cleanup coroutine
            if (enableCaching)
            {
                StartCoroutine(CacheCleanupRoutine());
            }

            IsInitialized = true;
            await Task.CompletedTask;

            Debug.Log($"[GeneticDatabaseManager] Initialized successfully. " +
                     $"Cached profiles: {_profileCache.Count}, " +
                     $"Indexed traits: {_traitIndex.Count}");
        }

        #endregion

        #region Profile Storage and Retrieval

        /// <summary>
        /// Stores a genetic profile
        /// </summary>
        public async Task<bool> StoreProfileAsync(string id, GeneticProfile profile)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("GeneticDatabaseManager not initialized");
            }

            if (string.IsNullOrEmpty(id) || profile == null)
            {
                Debug.LogError("[GeneticDatabaseManager] Invalid parameters for profile storage");
                return false;
            }

            try
            {
                // Store in cache
                if (enableCaching)
                {
                    StoreInCache(id, profile);
                }

                // Update trait index
                UpdateTraitIndex(id, profile);

                // Persist to storage if enabled
                if (enablePersistentStorage)
                {
                    await PersistProfile(id, profile);
                }

                // Update statistics
                if (!_profileCache.ContainsKey(id))
                {
                    _totalProfiles++;
                }

                Debug.Log($"[GeneticDatabaseManager] Stored profile {id} " +
                         $"(generation: {profile.Generation}, traits: {profile.Genes.Count})");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GeneticDatabaseManager] Failed to store profile {id}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieves a genetic profile
        /// </summary>
        public async Task<GeneticProfile> GetProfileAsync(string id)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("GeneticDatabaseManager not initialized");
            }

            if (string.IsNullOrEmpty(id))
                return null;

            try
            {
                // Check cache first
                if (enableCaching && _profileCache.TryGetValue(id, out var cachedProfile))
                {
                    _cacheHits++;
                    UpdateCacheTimestamp(id);
                    return cachedProfile;
                }

                _cacheMisses++;

                // Load from persistent storage if enabled
                if (enablePersistentStorage)
                {
                    var profile = await LoadPersistedProfile(id);
                    if (profile != null)
                    {
                        // Store in cache for future access
                        if (enableCaching)
                        {
                            StoreInCache(id, profile);
                        }
                        return profile;
                    }
                }

                Debug.LogWarning($"[GeneticDatabaseManager] Profile {id} not found");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GeneticDatabaseManager] Failed to retrieve profile {id}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Searches profiles by traits
        /// </summary>
        public async Task<List<string>> SearchProfilesByTraitsAsync(List<string> traitNames)
        {
            if (!IsInitialized || traitNames == null || traitNames.Count == 0)
                return new List<string>();

            try
            {
                var results = new HashSet<string>();

                // For first trait, get all profiles
                if (_traitIndex.TryGetValue(traitNames[0], out var firstTraitProfiles))
                {
                    results.UnionWith(firstTraitProfiles);
                }

                // For subsequent traits, intersect with existing results
                for (int i = 1; i < traitNames.Count; i++)
                {
                    if (_traitIndex.TryGetValue(traitNames[i], out var traitProfiles))
                    {
                        results.IntersectWith(traitProfiles);
                    }
                    else
                    {
                        // If any trait has no profiles, no results possible
                        results.Clear();
                        break;
                    }
                }

                var resultList = results.ToList();

                Debug.Log($"[GeneticDatabaseManager] Search for traits [{string.Join(", ", traitNames)}] " +
                         $"returned {resultList.Count} profiles");

                await Task.CompletedTask;
                return resultList;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GeneticDatabaseManager] Trait search failed: {ex.Message}");
                return new List<string>();
            }
        }

        #endregion

        #region Breeding History

        /// <summary>
        /// Gets breeding history for a profile
        /// </summary>
        public async Task<BreedingHistory> GetBreedingHistoryAsync(string profileId)
        {
            if (!IsInitialized || string.IsNullOrEmpty(profileId))
                return null;

            try
            {
                if (_breedingHistories.TryGetValue(profileId, out var history))
                {
                    await Task.CompletedTask;
                    return history;
                }

                // Create empty history if none exists
                var newHistory = new BreedingHistory
                {
                    profileId = profileId,
                    parentIds = new List<string>(),
                    offspringIds = new List<string>(),
                    breedingRecords = new List<BreedingRecord>(),
                    generation = 1,
                    averageCompatibility = 0f
                };

                _breedingHistories[profileId] = newHistory;
                await Task.CompletedTask;
                return newHistory;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GeneticDatabaseManager] Failed to get breeding history for {profileId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Records a breeding event
        /// </summary>
        public async Task RecordBreedingAsync(GeneticBreedingResult result)
        {
            if (!IsInitialized || result == null || !result.isSuccessful)
                return;

            try
            {
                // Update parent histories
                await UpdateParentBreedingHistory(result.parent1Id, result.parent2Id, result);
                await UpdateParentBreedingHistory(result.parent2Id, result.parent1Id, result);

                // Create offspring history
                if (!string.IsNullOrEmpty(result.offspringId))
                {
                    await CreateOffspringBreedingHistory(result);
                }

                Debug.Log($"[GeneticDatabaseManager] Recorded breeding: {result.parent1Id} x {result.parent2Id} = {result.offspringId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GeneticDatabaseManager] Failed to record breeding: {ex.Message}");
            }
        }

        #endregion

        #region Advanced Search

        /// <summary>
        /// Searches profiles by genetic similarity
        /// </summary>
        public async Task<List<string>> SearchByGeneticSimilarityAsync(GeneticProfile targetProfile, float minSimilarity = 0.7f)
        {
            if (!IsInitialized || targetProfile == null)
                return new List<string>();

            var results = new List<string>();

            foreach (var kvp in _profileCache)
            {
                var similarity = targetProfile.GetGeneticSimilarity(kvp.Value);
                if (similarity >= minSimilarity)
                {
                    results.Add(kvp.Key);
                }
            }

            await Task.CompletedTask;
            return results;
        }

        /// <summary>
        /// Searches profiles by generation range
        /// </summary>
        public async Task<List<string>> SearchByGenerationAsync(int minGeneration, int maxGeneration = int.MaxValue)
        {
            if (!IsInitialized)
                return new List<string>();

            var results = new List<string>();

            foreach (var kvp in _profileCache)
            {
                if (kvp.Value.Generation >= minGeneration && kvp.Value.Generation <= maxGeneration)
                {
                    results.Add(kvp.Key);
                }
            }

            await Task.CompletedTask;
            return results;
        }

        /// <summary>
        /// Searches profiles by mutation count
        /// </summary>
        public async Task<List<string>> SearchByMutationCountAsync(int minMutations, int maxMutations = int.MaxValue)
        {
            if (!IsInitialized)
                return new List<string>();

            var results = new List<string>();

            foreach (var kvp in _profileCache)
            {
                var mutationCount = kvp.Value.Mutations.Count;
                if (mutationCount >= minMutations && mutationCount <= maxMutations)
                {
                    results.Add(kvp.Key);
                }
            }

            await Task.CompletedTask;
            return results;
        }

        #endregion

        #region Cache Management

        private void StoreInCache(string id, GeneticProfile profile)
        {
            // Remove oldest entries if cache is full
            if (_profileCache.Count >= maxCacheSize)
            {
                CleanupOldestCacheEntries(maxCacheSize / 4); // Remove 25% of cache
            }

            _profileCache[id] = profile;
            _cacheTimestamps[id] = DateTime.UtcNow;
        }

        private void UpdateCacheTimestamp(string id)
        {
            _cacheTimestamps[id] = DateTime.UtcNow;
        }

        private void CleanupOldestCacheEntries(int entriesToRemove)
        {
            var oldestEntries = _cacheTimestamps
                .OrderBy(kvp => kvp.Value)
                .Take(entriesToRemove)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var id in oldestEntries)
            {
                _profileCache.Remove(id);
                _cacheTimestamps.Remove(id);
            }

            Debug.Log($"[GeneticDatabaseManager] Cleaned up {oldestEntries.Count} old cache entries");
        }

        private System.Collections.IEnumerator CacheCleanupRoutine()
        {
            var cleanupInterval = _config.PerformanceConfig.MemoryCleanupInterval;

            while (true)
            {
                yield return new WaitForSeconds(cleanupInterval);

                if (_profileCache.Count > 0)
                {
                    var cacheTimeout = _config.PerformanceConfig.CacheTimeout;
                    var cutoffTime = DateTime.UtcNow.AddSeconds(-cacheTimeout);

                    var expiredEntries = _cacheTimestamps
                        .Where(kvp => kvp.Value < cutoffTime)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var id in expiredEntries)
                    {
                        _profileCache.Remove(id);
                        _cacheTimestamps.Remove(id);
                    }

                    if (expiredEntries.Count > 0)
                    {
                        Debug.Log($"[GeneticDatabaseManager] Cleaned up {expiredEntries.Count} expired cache entries");
                    }
                }
            }
        }

        #endregion

        #region Trait Indexing

        private void InitializeTraitIndex()
        {
            // Initialize with common trait names
            var commonTraits = new[]
            {
                "Strength", "Agility", "Intelligence", "Constitution", "Charisma",
                "Size", "Speed", "Color", "Pattern", "Aggression", "Curiosity", "Loyalty"
            };

            foreach (var trait in commonTraits)
            {
                _traitIndex[trait] = new List<string>();
            }
        }

        private void UpdateTraitIndex(string profileId, GeneticProfile profile)
        {
            // Remove profile from all trait lists first
            foreach (var traitList in _traitIndex.Values)
            {
                traitList.Remove(profileId);
            }

            // Add profile to relevant trait lists
            foreach (var gene in profile.Genes.Where(g => g.isActive))
            {
                if (!_traitIndex.ContainsKey(gene.traitName))
                {
                    _traitIndex[gene.traitName] = new List<string>();
                }

                if (!_traitIndex[gene.traitName].Contains(profileId))
                {
                    _traitIndex[gene.traitName].Add(profileId);
                }
            }
        }

        #endregion

        #region Breeding History Management

        private async Task UpdateParentBreedingHistory(string parentId, string partnerId, GeneticBreedingResult result)
        {
            var history = await GetBreedingHistoryAsync(parentId);
            if (history == null)
                return;

            // Add partner to breeding records
            var breedingRecord = new BreedingRecord
            {
                partnerId = partnerId,
                offspringId = result.offspringId,
                compatibility = result.compatibility,
                timestamp = result.breedingTime,
                inheritedTraits = result.inheritedTraits,
                wasSuccessful = result.isSuccessful
            };

            history.breedingRecords.Add(breedingRecord);

            // Add offspring ID
            if (!string.IsNullOrEmpty(result.offspringId) && !history.offspringIds.Contains(result.offspringId))
            {
                history.offspringIds.Add(result.offspringId);
            }

            // Update average compatibility
            var compatibilities = history.breedingRecords.Where(r => r.wasSuccessful).Select(r => r.compatibility);
            if (compatibilities.Any())
            {
                history.averageCompatibility = compatibilities.Average();
            }
        }

        private async Task CreateOffspringBreedingHistory(GeneticBreedingResult result)
        {
            var offspringHistory = new BreedingHistory
            {
                profileId = result.offspringId,
                parentIds = new List<string> { result.parent1Id, result.parent2Id },
                offspringIds = new List<string>(),
                breedingRecords = new List<BreedingRecord>(),
                generation = result.offspring.Generation,
                averageCompatibility = 0f
            };

            _breedingHistories[result.offspringId] = offspringHistory;
            await Task.CompletedTask;
        }

        #endregion

        #region Persistent Storage (Placeholder)

        private async Task LoadPersistedData()
        {
            // Placeholder for loading from persistent storage
            // In a real implementation, this would load from files, databases, etc.
            Debug.Log("[GeneticDatabaseManager] Loading persisted data...");
            await Task.CompletedTask;
        }

        private async Task PersistProfile(string id, GeneticProfile profile)
        {
            // Placeholder for persisting to storage
            // In a real implementation, this would save to files, databases, etc.
            await Task.CompletedTask;
        }

        private async Task<GeneticProfile> LoadPersistedProfile(string id)
        {
            // Placeholder for loading from persistent storage
            // In a real implementation, this would load from files, databases, etc.
            await Task.CompletedTask;
            return null;
        }

        #endregion

        #region Statistics and Debugging

        /// <summary>
        /// Gets database statistics
        /// </summary>
        public DatabaseStatistics GetStatistics()
        {
            return new DatabaseStatistics
            {
                totalProfiles = _totalProfiles,
                cachedProfiles = _profileCache.Count,
                indexedTraits = _traitIndex.Count,
                cacheHitRatio = CacheHitRatio,
                cacheHits = _cacheHits,
                cacheMisses = _cacheMisses,
                breedingHistories = _breedingHistories.Count
            };
        }

        /// <summary>
        /// Gets the most common traits
        /// </summary>
        public Dictionary<string, int> GetTraitPopularity()
        {
            return _traitIndex.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        }

        /// <summary>
        /// Clears all cached data (useful for testing)
        /// </summary>
        [ContextMenu("Clear Cache")]
        public void ClearCache()
        {
            _profileCache.Clear();
            _cacheTimestamps.Clear();
            _cacheHits = 0;
            _cacheMisses = 0;

            Debug.Log("[GeneticDatabaseManager] Cache cleared");
        }

        /// <summary>
        /// Rebuilds trait index from cached profiles
        /// </summary>
        [ContextMenu("Rebuild Trait Index")]
        public void RebuildTraitIndex()
        {
            _traitIndex.Clear();
            InitializeTraitIndex();

            foreach (var kvp in _profileCache)
            {
                UpdateTraitIndex(kvp.Key, kvp.Value);
            }

            Debug.Log($"[GeneticDatabaseManager] Rebuilt trait index with {_traitIndex.Count} traits");
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Save any pending data if needed
            if (enablePersistentStorage && _profileCache.Count > 0)
            {
                Debug.Log($"[GeneticDatabaseManager] Shutting down with {_profileCache.Count} cached profiles");
            }
        }

        #endregion
    }

    /// <summary>
    /// Database statistics for monitoring and debugging
    /// </summary>
    public class DatabaseStatistics
    {
        public int totalProfiles;
        public int cachedProfiles;
        public int indexedTraits;
        public float cacheHitRatio;
        public int cacheHits;
        public int cacheMisses;
        public int breedingHistories;
    }
}