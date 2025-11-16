using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace Laboratory.Subsystems.Leaderboard
{
    /// <summary>
    /// Global leaderboard system with persistent storage, multiple leaderboard types,
    /// seasonal support, and backend integration for cross-platform competitive features.
    /// </summary>
    public class GlobalLeaderboardSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Configuration")]
        [SerializeField] private string apiEndpoint = "https://api.chimeraos.example.com/leaderboards";
        [SerializeField] private bool useLocalCache = true;
        [SerializeField] private float cacheRefreshInterval = 300f; // 5 minutes

        [Header("Leaderboard Settings")]
        [SerializeField] private int maxEntriesPerBoard = 1000;
        [SerializeField] private int topEntriesCache = 100;
        [SerializeField] private bool enableSeasonalLeaderboards = true;

        [Header("Activity Types")]
        [SerializeField] private LeaderboardActivityType[] supportedActivities = new[]
        {
            LeaderboardActivityType.Racing,
            LeaderboardActivityType.Combat,
            LeaderboardActivityType.Puzzle,
            LeaderboardActivityType.Strategy,
            LeaderboardActivityType.Music,
            LeaderboardActivityType.Adventure,
            LeaderboardActivityType.Platforming,
            LeaderboardActivityType.Crafting
        };

        #endregion

        #region State

        private Dictionary<string, LeaderboardCache> _leaderboardCaches = new();
        private GlobalLeaderboardBackend _backend;
        private bool _isInitialized = false;
        private float _lastCacheRefresh = 0f;

        #endregion

        #region Initialization

        private void Awake()
        {
            _backend = new GlobalLeaderboardBackend(apiEndpoint);
        }

        private async void Start()
        {
            await InitializeAsync();
        }

        private void Update()
        {
            if (_isInitialized && useLocalCache)
            {
                // Refresh cache periodically
                if (Time.time - _lastCacheRefresh > cacheRefreshInterval)
                {
                    _ = RefreshAllCachesAsync();
                }
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                Debug.Log("[GlobalLeaderboard] Initializing...");

                // Initialize backend
                await _backend.InitializeAsync();

                // Create caches for each supported activity
                foreach (var activity in supportedActivities)
                {
                    // Global all-time leaderboard
                    var globalKey = GetLeaderboardKey(activity, LeaderboardTimeframe.AllTime);
                    _leaderboardCaches[globalKey] = new LeaderboardCache(activity, LeaderboardTimeframe.AllTime);

                    // Seasonal leaderboards
                    if (enableSeasonalLeaderboards)
                    {
                        var weeklyKey = GetLeaderboardKey(activity, LeaderboardTimeframe.Weekly);
                        _leaderboardCaches[weeklyKey] = new LeaderboardCache(activity, LeaderboardTimeframe.Weekly);

                        var monthlyKey = GetLeaderboardKey(activity, LeaderboardTimeframe.Monthly);
                        _leaderboardCaches[monthlyKey] = new LeaderboardCache(activity, LeaderboardTimeframe.Monthly);

                        var seasonalKey = GetLeaderboardKey(activity, LeaderboardTimeframe.Seasonal);
                        _leaderboardCaches[seasonalKey] = new LeaderboardCache(activity, LeaderboardTimeframe.Seasonal);
                    }
                }

                // Load initial cache data
                await RefreshAllCachesAsync();

                _isInitialized = true;
                Debug.Log($"[GlobalLeaderboard] Initialized successfully - {_leaderboardCaches.Count} leaderboards active");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GlobalLeaderboard] Initialization failed: {ex.Message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Submit a score to the global leaderboard
        /// </summary>
        public async Task<LeaderboardSubmitResult> SubmitScoreAsync(
            string playerId,
            string playerName,
            LeaderboardActivityType activity,
            float score,
            Dictionary<string, object> metadata = null)
        {
            if (!_isInitialized)
            {
                return new LeaderboardSubmitResult { success = false, error = "System not initialized" };
            }

            try
            {
                var entry = new LeaderboardEntry
                {
                    playerId = playerId,
                    playerName = playerName,
                    activity = activity,
                    score = score,
                    timestamp = DateTime.UtcNow,
                    metadata = metadata ?? new Dictionary<string, object>()
                };

                // Submit to backend
                var result = await _backend.SubmitScoreAsync(entry);

                if (result.success)
                {
                    // Update caches
                    UpdateLocalCache(entry, LeaderboardTimeframe.AllTime);

                    if (enableSeasonalLeaderboards)
                    {
                        UpdateLocalCache(entry, LeaderboardTimeframe.Weekly);
                        UpdateLocalCache(entry, LeaderboardTimeframe.Monthly);
                        UpdateLocalCache(entry, LeaderboardTimeframe.Seasonal);
                    }

                    Debug.Log($"[GlobalLeaderboard] Score submitted for {playerName}: {score} in {activity}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GlobalLeaderboard] Submit score failed: {ex.Message}");
                return new LeaderboardSubmitResult { success = false, error = ex.Message };
            }
        }

        /// <summary>
        /// Get leaderboard rankings for a specific activity and timeframe
        /// </summary>
        public async Task<LeaderboardEntry[]> GetLeaderboardAsync(
            LeaderboardActivityType activity,
            LeaderboardTimeframe timeframe,
            int count = 100,
            int offset = 0)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[GlobalLeaderboard] System not initialized");
                return new LeaderboardEntry[0];
            }

            try
            {
                var key = GetLeaderboardKey(activity, timeframe);

                if (_leaderboardCaches.TryGetValue(key, out var cache))
                {
                    // Return from cache if available
                    if (cache.IsValid())
                    {
                        return cache.GetEntries(count, offset);
                    }
                }

                // Fetch from backend
                var entries = await _backend.GetLeaderboardAsync(activity, timeframe, count, offset);

                // Update cache
                if (_leaderboardCaches.ContainsKey(key))
                {
                    _leaderboardCaches[key].UpdateEntries(entries);
                }

                return entries;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GlobalLeaderboard] Get leaderboard failed: {ex.Message}");
                return new LeaderboardEntry[0];
            }
        }

        /// <summary>
        /// Get player rank in a specific leaderboard
        /// </summary>
        public async Task<PlayerRankInfo> GetPlayerRankAsync(
            string playerId,
            LeaderboardActivityType activity,
            LeaderboardTimeframe timeframe)
        {
            if (!_isInitialized)
            {
                return new PlayerRankInfo { playerId = playerId, rank = -1 };
            }

            try
            {
                return await _backend.GetPlayerRankAsync(playerId, activity, timeframe);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GlobalLeaderboard] Get player rank failed: {ex.Message}");
                return new PlayerRankInfo { playerId = playerId, rank = -1 };
            }
        }

        /// <summary>
        /// Get leaderboard entries around a specific player
        /// </summary>
        public async Task<LeaderboardEntry[]> GetEntriesAroundPlayerAsync(
            string playerId,
            LeaderboardActivityType activity,
            LeaderboardTimeframe timeframe,
            int radius = 5)
        {
            if (!_isInitialized)
            {
                return new LeaderboardEntry[0];
            }

            try
            {
                return await _backend.GetEntriesAroundPlayerAsync(playerId, activity, timeframe, radius);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GlobalLeaderboard] Get entries around player failed: {ex.Message}");
                return new LeaderboardEntry[0];
            }
        }

        /// <summary>
        /// Get all leaderboards for a specific player
        /// </summary>
        public async Task<PlayerLeaderboardStats> GetPlayerStatsAsync(string playerId)
        {
            if (!_isInitialized)
            {
                return new PlayerLeaderboardStats { playerId = playerId };
            }

            try
            {
                return await _backend.GetPlayerStatsAsync(playerId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GlobalLeaderboard] Get player stats failed: {ex.Message}");
                return new PlayerLeaderboardStats { playerId = playerId };
            }
        }

        #endregion

        #region Cache Management

        private void UpdateLocalCache(LeaderboardEntry entry, LeaderboardTimeframe timeframe)
        {
            var key = GetLeaderboardKey(entry.activity, timeframe);

            if (_leaderboardCaches.TryGetValue(key, out var cache))
            {
                cache.AddOrUpdateEntry(entry);
            }
        }

        private async Task RefreshAllCachesAsync()
        {
            _lastCacheRefresh = Time.time;

            foreach (var kvp in _leaderboardCaches)
            {
                try
                {
                    var cache = kvp.Value;
                    var entries = await _backend.GetLeaderboardAsync(
                        cache.Activity,
                        cache.Timeframe,
                        topEntriesCache,
                        0);

                    cache.UpdateEntries(entries);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GlobalLeaderboard] Failed to refresh cache {kvp.Key}: {ex.Message}");
                }
            }

            Debug.Log($"[GlobalLeaderboard] Cache refresh completed");
        }

        private string GetLeaderboardKey(LeaderboardActivityType activity, LeaderboardTimeframe timeframe)
        {
            return $"{activity}_{timeframe}";
        }

        #endregion
    }

    #region Leaderboard Backend

    /// <summary>
    /// Backend communication layer for global leaderboards
    /// </summary>
    public class GlobalLeaderboardBackend
    {
        private string _apiEndpoint;
        private string _authToken;

        public GlobalLeaderboardBackend(string apiEndpoint)
        {
            _apiEndpoint = apiEndpoint;
        }

        public async Task InitializeAsync()
        {
            // Authenticate with backend
            await AuthenticateAsync();
        }

        public async Task<LeaderboardSubmitResult> SubmitScoreAsync(LeaderboardEntry entry)
        {
            var url = $"{_apiEndpoint}/submit";
            var payload = JsonConvert.SerializeObject(entry);

            // Mock implementation - replace with actual HTTP request
            await Task.Delay(50);

            Debug.Log($"[LeaderboardBackend] Submit: {url} - Player: {entry.playerName}, Score: {entry.score}");

            return new LeaderboardSubmitResult
            {
                success = true,
                rank = UnityEngine.Random.Range(1, 1000),
                isNewRecord = UnityEngine.Random.value > 0.8f
            };

            // Production implementation:
            /*
            using (var request = UnityWebRequest.Post(url, payload, "application/json"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return JsonConvert.DeserializeObject<LeaderboardSubmitResult>(request.downloadHandler.text);
                }
                else
                {
                    return new LeaderboardSubmitResult { success = false, error = request.error };
                }
            }
            */
        }

        public async Task<LeaderboardEntry[]> GetLeaderboardAsync(
            LeaderboardActivityType activity,
            LeaderboardTimeframe timeframe,
            int count,
            int offset)
        {
            var url = $"{_apiEndpoint}/get?activity={activity}&timeframe={timeframe}&count={count}&offset={offset}";

            // Mock implementation
            await Task.Delay(100);

            Debug.Log($"[LeaderboardBackend] Get: {url}");

            // Return mock data
            return GenerateMockLeaderboard(activity, count);

            // Production implementation:
            /*
            using (var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return JsonConvert.DeserializeObject<LeaderboardEntry[]>(request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"Failed to get leaderboard: {request.error}");
                    return new LeaderboardEntry[0];
                }
            }
            */
        }

        public async Task<PlayerRankInfo> GetPlayerRankAsync(
            string playerId,
            LeaderboardActivityType activity,
            LeaderboardTimeframe timeframe)
        {
            var url = $"{_apiEndpoint}/rank?playerId={playerId}&activity={activity}&timeframe={timeframe}";

            // Mock implementation
            await Task.Delay(50);

            return new PlayerRankInfo
            {
                playerId = playerId,
                rank = UnityEngine.Random.Range(1, 10000),
                score = UnityEngine.Random.Range(1000, 100000),
                percentile = UnityEngine.Random.Range(1f, 99f)
            };
        }

        public async Task<LeaderboardEntry[]> GetEntriesAroundPlayerAsync(
            string playerId,
            LeaderboardActivityType activity,
            LeaderboardTimeframe timeframe,
            int radius)
        {
            var url = $"{_apiEndpoint}/around?playerId={playerId}&activity={activity}&timeframe={timeframe}&radius={radius}";

            // Mock implementation
            await Task.Delay(100);

            return GenerateMockLeaderboard(activity, radius * 2 + 1);
        }

        public async Task<PlayerLeaderboardStats> GetPlayerStatsAsync(string playerId)
        {
            var url = $"{_apiEndpoint}/player-stats?playerId={playerId}";

            // Mock implementation
            await Task.Delay(100);

            var stats = new PlayerLeaderboardStats
            {
                playerId = playerId,
                ranks = new Dictionary<LeaderboardActivityType, PlayerRankInfo>()
            };

            foreach (LeaderboardActivityType activity in Enum.GetValues(typeof(LeaderboardActivityType)))
            {
                if (activity == LeaderboardActivityType.None) continue;

                stats.ranks[activity] = new PlayerRankInfo
                {
                    playerId = playerId,
                    rank = UnityEngine.Random.Range(1, 10000),
                    score = UnityEngine.Random.Range(1000, 100000)
                };
            }

            return stats;
        }

        private async Task AuthenticateAsync()
        {
            // Mock authentication
            _authToken = "mock_token_" + Guid.NewGuid().ToString();
            await Task.CompletedTask;
        }

        private LeaderboardEntry[] GenerateMockLeaderboard(LeaderboardActivityType activity, int count)
        {
            var entries = new List<LeaderboardEntry>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(new LeaderboardEntry
                {
                    playerId = $"player_{i}",
                    playerName = $"Player {i + 1}",
                    activity = activity,
                    score = 100000 - (i * 100),
                    rank = i + 1,
                    timestamp = DateTime.UtcNow.AddDays(-i),
                    metadata = new Dictionary<string, object>()
                });
            }

            return entries.ToArray();
        }
    }

    #endregion

    #region Cache System

    /// <summary>
    /// Local cache for leaderboard entries
    /// </summary>
    public class LeaderboardCache
    {
        private List<LeaderboardEntry> _entries = new();
        private DateTime _lastUpdate;
        private float _cacheValiditySeconds = 300f;

        public LeaderboardActivityType Activity { get; private set; }
        public LeaderboardTimeframe Timeframe { get; private set; }

        public LeaderboardCache(LeaderboardActivityType activity, LeaderboardTimeframe timeframe)
        {
            Activity = activity;
            Timeframe = timeframe;
            _lastUpdate = DateTime.UtcNow;
        }

        public bool IsValid()
        {
            return (DateTime.UtcNow - _lastUpdate).TotalSeconds < _cacheValiditySeconds;
        }

        public LeaderboardEntry[] GetEntries(int count, int offset)
        {
            return _entries.Skip(offset).Take(count).ToArray();
        }

        public void UpdateEntries(LeaderboardEntry[] entries)
        {
            _entries = entries.ToList();
            _lastUpdate = DateTime.UtcNow;
            SortEntries();
        }

        public void AddOrUpdateEntry(LeaderboardEntry entry)
        {
            var existing = _entries.FirstOrDefault(e => e.playerId == entry.playerId);

            if (existing != null)
            {
                // Update if new score is better
                if (entry.score > existing.score)
                {
                    _entries.Remove(existing);
                    _entries.Add(entry);
                    SortEntries();
                }
            }
            else
            {
                _entries.Add(entry);
                SortEntries();
            }

            // Recalculate ranks
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].rank = i + 1;
            }
        }

        private void SortEntries()
        {
            _entries = _entries.OrderByDescending(e => e.score).ToList();
        }
    }

    #endregion

    #region Data Structures

    [Serializable]
    public enum LeaderboardActivityType
    {
        None = 0,
        Racing = 1,
        Combat = 2,
        Puzzle = 3,
        Strategy = 4,
        Music = 5,
        Adventure = 6,
        Platforming = 7,
        Crafting = 8,
        Overall = 99
    }

    [Serializable]
    public enum LeaderboardTimeframe
    {
        AllTime,
        Weekly,
        Monthly,
        Seasonal,
        Daily
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public string playerId;
        public string playerName;
        public LeaderboardActivityType activity;
        public float score;
        public int rank;
        public DateTime timestamp;
        public Dictionary<string, object> metadata;
    }

    [Serializable]
    public class LeaderboardSubmitResult
    {
        public bool success;
        public int rank;
        public bool isNewRecord;
        public string error;
    }

    [Serializable]
    public class PlayerRankInfo
    {
        public string playerId;
        public int rank;
        public float score;
        public float percentile;
    }

    [Serializable]
    public class PlayerLeaderboardStats
    {
        public string playerId;
        public Dictionary<LeaderboardActivityType, PlayerRankInfo> ranks;
    }

    #endregion
}
