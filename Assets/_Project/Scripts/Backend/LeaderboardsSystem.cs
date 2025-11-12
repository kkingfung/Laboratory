using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Backend
{
    /// <summary>
    /// Leaderboards system for competitive rankings.
    /// Supports global, friend, and custom leaderboards.
    /// Handles score submission, fetching, and ranking.
    /// </summary>
    public class LeaderboardsSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string submitEndpoint = "/leaderboards/submit";
        [SerializeField] private string fetchEndpoint = "/leaderboards/fetch";
        [SerializeField] private string rankEndpoint = "/leaderboards/rank";

        [Header("Fetch Settings")]
        [SerializeField] private int defaultPageSize = 50;
        [SerializeField] private bool cacheLeaderboards = true;
        [SerializeField] private float cacheDuration = 300f; // 5 minutes

        [Header("Submission Settings")]
        [SerializeField] private bool allowOfflineScores = true;
        [SerializeField] private int maxOfflineScores = 10;
        [SerializeField] private bool validateScores = true;

        #endregion

        #region Private Fields

        private static LeaderboardsSystem _instance;

        // Leaderboard cache
        private readonly Dictionary<string, LeaderboardCache> _cachedLeaderboards = new Dictionary<string, LeaderboardCache>();

        // Offline scores queue
        private readonly Queue<ScoreSubmission> _offlineScores = new Queue<ScoreSubmission>();

        // User ranking cache
        private readonly Dictionary<string, int> _userRankings = new Dictionary<string, int>();

        // State
        private bool _isSubmitting = false;
        private bool _isFetching = false;

        // Statistics
        private int _totalSubmissions = 0;
        private int _failedSubmissions = 0;
        private int _totalFetches = 0;
        private int _cacheHits = 0;

        // Events
        public event Action<string, long> OnScoreSubmitted;
        public event Action<string> OnScoreSubmissionFailed;
        public event Action<string, LeaderboardEntry[]> OnLeaderboardFetched;
        public event Action<string> OnLeaderboardFetchFailed;
        public event Action<string, int> OnRankUpdated;

        #endregion

        #region Properties

        public static LeaderboardsSystem Instance => _instance;
        public bool IsSubmitting => _isSubmitting;
        public bool IsFetching => _isFetching;
        public int OfflineScoreCount => _offlineScores.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Try to submit offline scores when authenticated
            if (UserAuthenticationSystem.Instance != null)
            {
                UserAuthenticationSystem.Instance.OnLoginSuccess += (session) => SubmitOfflineScores();
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[LeaderboardsSystem] Initializing...");

            // Load offline scores
            if (allowOfflineScores)
            {
                LoadOfflineScores();
            }

            Debug.Log($"[LeaderboardsSystem] Initialized with {_offlineScores.Count} offline scores");
        }

        #endregion

        #region Score Submission

        /// <summary>
        /// Submit a score to a leaderboard.
        /// </summary>
        public void SubmitScore(string leaderboardId, long score, Dictionary<string, string> metadata = null, Action onSuccess = null, Action<string> onError = null)
        {
            // Validate score if enabled
            if (validateScores && score < 0)
            {
                string error = "Invalid score: must be non-negative";
                OnScoreSubmissionFailed?.Invoke(error);
                onError?.Invoke(error);
                return;
            }

            var submission = new ScoreSubmission
            {
                leaderboardId = leaderboardId,
                score = score,
                timestamp = DateTime.UtcNow,
                metadata = metadata
            };

            // Check if user is authenticated
            if (UserAuthenticationSystem.Instance == null || !UserAuthenticationSystem.Instance.IsAuthenticated)
            {
                // Queue for offline submission
                if (allowOfflineScores)
                {
                    QueueOfflineScore(submission);
                    onSuccess?.Invoke();
                }
                else
                {
                    string error = "User not authenticated";
                    OnScoreSubmissionFailed?.Invoke(error);
                    onError?.Invoke(error);
                }
                return;
            }

            StartCoroutine(SubmitScoreCoroutine(submission, onSuccess, onError));
        }

        private IEnumerator SubmitScoreCoroutine(ScoreSubmission submission, Action onSuccess, Action<string> onError)
        {
            _isSubmitting = true;
            _totalSubmissions++;

            string url = backendUrl + submitEndpoint;

            var requestData = new ScoreSubmitRequest
            {
                leaderboardId = submission.leaderboardId,
                userId = UserAuthenticationSystem.Instance.UserId,
                score = submission.score,
                timestamp = submission.timestamp,
                metadata = submission.metadata
            };

            string json = JsonUtility.ToJson(requestData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {UserAuthenticationSystem.Instance.AuthToken}");
                request.timeout = 10;

                yield return request.SendWebRequest();

                _isSubmitting = false;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<ScoreSubmitResponse>(request.downloadHandler.text);

                        // Update user rank cache
                        _userRankings[submission.leaderboardId] = response.rank;

                        // Invalidate leaderboard cache
                        if (_cachedLeaderboards.ContainsKey(submission.leaderboardId))
                        {
                            _cachedLeaderboards.Remove(submission.leaderboardId);
                        }

                        OnScoreSubmitted?.Invoke(submission.leaderboardId, submission.score);
                        OnRankUpdated?.Invoke(submission.leaderboardId, response.rank);
                        onSuccess?.Invoke();

                        Debug.Log($"[LeaderboardsSystem] Score submitted: {submission.leaderboardId} = {submission.score} (Rank: {response.rank})");
                    }
                    catch (Exception ex)
                    {
                        _failedSubmissions++;
                        string error = $"Failed to parse submit response: {ex.Message}";
                        OnScoreSubmissionFailed?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    _failedSubmissions++;
                    string error = $"Score submission failed: {request.error}";

                    // Queue for retry if offline
                    if (allowOfflineScores)
                    {
                        QueueOfflineScore(submission);
                    }

                    OnScoreSubmissionFailed?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[LeaderboardsSystem] {error}");
                }
            }
        }

        /// <summary>
        /// Submit multiple scores at once (batch).
        /// </summary>
        public void SubmitScores(Dictionary<string, long> scores, Action onSuccess = null, Action<string> onError = null)
        {
            int remaining = scores.Count;
            bool anyFailed = false;

            foreach (var kvp in scores)
            {
                SubmitScore(kvp.Key, kvp.Value,
                    onSuccess: () =>
                    {
                        remaining--;
                        if (remaining == 0 && !anyFailed)
                        {
                            onSuccess?.Invoke();
                        }
                    },
                    onError: (error) =>
                    {
                        anyFailed = true;
                        remaining--;
                        if (remaining == 0)
                        {
                            onError?.Invoke(error);
                        }
                    });
            }
        }

        #endregion

        #region Fetching

        /// <summary>
        /// Fetch leaderboard entries.
        /// </summary>
        public void FetchLeaderboard(string leaderboardId, LeaderboardScope scope = LeaderboardScope.Global, int pageSize = 0, int offset = 0, Action<LeaderboardEntry[]> onSuccess = null, Action<string> onError = null)
        {
            if (pageSize <= 0)
                pageSize = defaultPageSize;

            // Check cache
            if (cacheLeaderboards && _cachedLeaderboards.TryGetValue(leaderboardId, out var cache))
            {
                if (Time.time - cache.timestamp < cacheDuration)
                {
                    _cacheHits++;
                    onSuccess?.Invoke(cache.entries);
                    OnLeaderboardFetched?.Invoke(leaderboardId, cache.entries);
                    return;
                }
            }

            StartCoroutine(FetchLeaderboardCoroutine(leaderboardId, scope, pageSize, offset, onSuccess, onError));
        }

        private IEnumerator FetchLeaderboardCoroutine(string leaderboardId, LeaderboardScope scope, int pageSize, int offset, Action<LeaderboardEntry[]> onSuccess, Action<string> onError)
        {
            _isFetching = true;
            _totalFetches++;

            string url = $"{backendUrl}{fetchEndpoint}?leaderboardId={leaderboardId}&scope={scope.ToString().ToLower()}&pageSize={pageSize}&offset={offset}";

            // Add user ID for friend scope
            if (scope == LeaderboardScope.Friends && UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
            {
                url += $"&userId={UserAuthenticationSystem.Instance.UserId}";
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Add auth header if available
                if (UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
                {
                    request.SetRequestHeader("Authorization", $"Bearer {UserAuthenticationSystem.Instance.AuthToken}");
                }

                request.timeout = 10;

                yield return request.SendWebRequest();

                _isFetching = false;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<LeaderboardFetchResponse>(request.downloadHandler.text);

                        // Cache results
                        if (cacheLeaderboards)
                        {
                            _cachedLeaderboards[leaderboardId] = new LeaderboardCache
                            {
                                entries = response.entries,
                                timestamp = Time.time
                            };
                        }

                        OnLeaderboardFetched?.Invoke(leaderboardId, response.entries);
                        onSuccess?.Invoke(response.entries);

                        Debug.Log($"[LeaderboardsSystem] Fetched {response.entries.Length} entries for {leaderboardId}");
                    }
                    catch (Exception ex)
                    {
                        string error = $"Failed to parse leaderboard response: {ex.Message}";
                        OnLeaderboardFetchFailed?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Leaderboard fetch failed: {request.error}";
                    OnLeaderboardFetchFailed?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[LeaderboardsSystem] {error}");
                }
            }
        }

        /// <summary>
        /// Fetch user's rank on a leaderboard.
        /// </summary>
        public void FetchUserRank(string leaderboardId, Action<int> onSuccess = null, Action<string> onError = null)
        {
            // Check cache
            if (_userRankings.TryGetValue(leaderboardId, out int cachedRank))
            {
                onSuccess?.Invoke(cachedRank);
                return;
            }

            StartCoroutine(FetchUserRankCoroutine(leaderboardId, onSuccess, onError));
        }

        private IEnumerator FetchUserRankCoroutine(string leaderboardId, Action<int> onSuccess, Action<string> onError)
        {
            if (UserAuthenticationSystem.Instance == null || !UserAuthenticationSystem.Instance.IsAuthenticated)
            {
                onError?.Invoke("User not authenticated");
                yield break;
            }

            string url = $"{backendUrl}{rankEndpoint}?leaderboardId={leaderboardId}&userId={UserAuthenticationSystem.Instance.UserId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {UserAuthenticationSystem.Instance.AuthToken}");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<RankResponse>(request.downloadHandler.text);

                        // Cache rank
                        _userRankings[leaderboardId] = response.rank;

                        OnRankUpdated?.Invoke(leaderboardId, response.rank);
                        onSuccess?.Invoke(response.rank);
                    }
                    catch (Exception ex)
                    {
                        onError?.Invoke($"Failed to parse rank response: {ex.Message}");
                    }
                }
                else
                {
                    onError?.Invoke($"Rank fetch failed: {request.error}");
                }
            }
        }

        #endregion

        #region Offline Scores

        private void QueueOfflineScore(ScoreSubmission submission)
        {
            if (_offlineScores.Count >= maxOfflineScores)
            {
                _offlineScores.Dequeue();
            }

            _offlineScores.Enqueue(submission);
            SaveOfflineScores();

            Debug.Log($"[LeaderboardsSystem] Queued offline score: {submission.leaderboardId} = {submission.score}");
        }

        private void SubmitOfflineScores()
        {
            if (_offlineScores.Count == 0) return;

            Debug.Log($"[LeaderboardsSystem] Submitting {_offlineScores.Count} offline scores...");

            int count = _offlineScores.Count;

            for (int i = 0; i < count; i++)
            {
                var submission = _offlineScores.Dequeue();
                StartCoroutine(SubmitScoreCoroutine(submission, null, null));
            }

            SaveOfflineScores();
        }

        private void SaveOfflineScores()
        {
            try
            {
                var data = new OfflineScoresData
                {
                    scores = _offlineScores.ToArray()
                };

                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString("Leaderboards_OfflineScores", json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardsSystem] Failed to save offline scores: {ex.Message}");
            }
        }

        private void LoadOfflineScores()
        {
            try
            {
                if (PlayerPrefs.HasKey("Leaderboards_OfflineScores"))
                {
                    string json = PlayerPrefs.GetString("Leaderboards_OfflineScores");
                    var data = JsonUtility.FromJson<OfflineScoresData>(json);

                    _offlineScores.Clear();

                    foreach (var score in data.scores)
                    {
                        _offlineScores.Enqueue(score);
                    }

                    Debug.Log($"[LeaderboardsSystem] Loaded {_offlineScores.Count} offline scores");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeaderboardsSystem] Failed to load offline scores: {ex.Message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get leaderboard statistics.
        /// </summary>
        public LeaderboardStats GetStats()
        {
            return new LeaderboardStats
            {
                totalSubmissions = _totalSubmissions,
                failedSubmissions = _failedSubmissions,
                totalFetches = _totalFetches,
                cacheHits = _cacheHits,
                offlineScoreCount = _offlineScores.Count,
                cachedLeaderboardCount = _cachedLeaderboards.Count
            };
        }

        /// <summary>
        /// Clear leaderboard cache.
        /// </summary>
        public void ClearCache()
        {
            _cachedLeaderboards.Clear();
            _userRankings.Clear();

            Debug.Log("[LeaderboardsSystem] Cache cleared");
        }

        /// <summary>
        /// Get cached rank for a leaderboard.
        /// </summary>
        public int GetCachedRank(string leaderboardId)
        {
            return _userRankings.TryGetValue(leaderboardId, out int rank) ? rank : -1;
        }

        #endregion

        #region Context Menu

        [ContextMenu("Submit Test Score")]
        private void SubmitTestScore()
        {
            SubmitScore("test_leaderboard", UnityEngine.Random.Range(1000, 10000));
        }

        [ContextMenu("Fetch Test Leaderboard")]
        private void FetchTestLeaderboard()
        {
            FetchLeaderboard("test_leaderboard", LeaderboardScope.Global, 10);
        }

        [ContextMenu("Submit Offline Scores")]
        private void SubmitOfflineScoresMenu()
        {
            SubmitOfflineScores();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Leaderboard Statistics ===\n" +
                      $"Total Submissions: {stats.totalSubmissions}\n" +
                      $"Failed Submissions: {stats.failedSubmissions}\n" +
                      $"Total Fetches: {stats.totalFetches}\n" +
                      $"Cache Hits: {stats.cacheHits}\n" +
                      $"Offline Scores: {stats.offlineScoreCount}\n" +
                      $"Cached Leaderboards: {stats.cachedLeaderboardCount}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Score submission data.
    /// </summary>
    [Serializable]
    public class ScoreSubmission
    {
        public string leaderboardId;
        public long score;
        public DateTime timestamp;
        public Dictionary<string, string> metadata;
    }

    /// <summary>
    /// Leaderboard entry.
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string userId;
        public string username;
        public long score;
        public DateTime timestamp;
    }

    /// <summary>
    /// Leaderboard cache.
    /// </summary>
    [Serializable]
    public class LeaderboardCache
    {
        public LeaderboardEntry[] entries;
        public float timestamp;
    }

    /// <summary>
    /// Offline scores data.
    /// </summary>
    [Serializable]
    public class OfflineScoresData
    {
        public ScoreSubmission[] scores;
    }

    /// <summary>
    /// Leaderboard statistics.
    /// </summary>
    [Serializable]
    public struct LeaderboardStats
    {
        public int totalSubmissions;
        public int failedSubmissions;
        public int totalFetches;
        public int cacheHits;
        public int offlineScoreCount;
        public int cachedLeaderboardCount;
    }

    // Request/Response structures
    [Serializable] class ScoreSubmitRequest { public string leaderboardId; public string userId; public long score; public DateTime timestamp; public Dictionary<string, string> metadata; }
    [Serializable] class ScoreSubmitResponse { public int rank; public bool isNewRecord; }
    [Serializable] class LeaderboardFetchResponse { public LeaderboardEntry[] entries; public int totalCount; }
    [Serializable] class RankResponse { public int rank; public long score; }

    /// <summary>
    /// Leaderboard scope types.
    /// </summary>
    public enum LeaderboardScope
    {
        Global,
        Friends,
        Region
    }

    #endregion
}
