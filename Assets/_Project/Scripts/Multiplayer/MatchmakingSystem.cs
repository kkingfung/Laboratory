using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Multiplayer
{
    /// <summary>
    /// Matchmaking system for multiplayer games.
    /// Supports skill-based matchmaking (SBMM), party systems, and quick play.
    /// Handles queue management and server allocation.
    /// </summary>
    public class MatchmakingSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string queueEndpoint = "/matchmaking/queue";
        [SerializeField] private string cancelEndpoint = "/matchmaking/cancel";
        [SerializeField] private string statusEndpoint = "/matchmaking/status";

        [Header("Matchmaking Settings")]
        [SerializeField] private MatchmakingMode defaultMode = MatchmakingMode.QuickPlay;
        [SerializeField] private bool useSkillBasedMatchmaking = true;
        [SerializeField] private float maxSkillDifference = 200f;
        [SerializeField] private float queueTimeoutSeconds = 300f; // 5 minutes

        [Header("Polling Settings")]
        [SerializeField] private float statusPollInterval = 2f;
        [SerializeField] private int maxPollAttempts = 150; // 5 minutes at 2s intervals

        #endregion

        #region Private Fields

        private static MatchmakingSystem _instance;

        // Queue state
        private bool _inQueue = false;
        private MatchmakingRequest _currentRequest;
        private DateTime _queueStartTime;
        private int _pollAttempts = 0;

        // Match state
        private MatchInfo _currentMatch;

        // Statistics
        private int _totalQueues = 0;
        private int _totalMatches = 0;
        private int _queueCancellations = 0;
        private float _averageQueueTime = 0f;

        // Events
        public event Action OnQueueStarted;
        public event Action<MatchInfo> OnMatchFound;
        public event Action<string> OnQueueCancelled;
        public event Action<string> OnMatchmakingFailed;
        public event Action<float> OnQueueTimeUpdated;

        #endregion

        #region Properties

        public static MatchmakingSystem Instance => _instance;
        public bool IsInQueue => _inQueue;
        public float QueueTime => _inQueue ? (float)(DateTime.UtcNow - _queueStartTime).TotalSeconds : 0f;
        public MatchInfo CurrentMatch => _currentMatch;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[MatchmakingSystem] Initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            if (_inQueue)
            {
                CancelQueue();
            }
        }

        #endregion

        #region Queue Management

        /// <summary>
        /// Join matchmaking queue.
        /// </summary>
        public void JoinQueue(MatchmakingMode mode = MatchmakingMode.QuickPlay, string[] partyMembers = null, Action<MatchInfo> onSuccess = null, Action<string> onError = null)
        {
            if (_inQueue)
            {
                Debug.LogWarning("[MatchmakingSystem] Already in queue");
                onError?.Invoke("Already in queue");
                return;
            }

            if (Backend.UserAuthenticationSystem.Instance == null || !Backend.UserAuthenticationSystem.Instance.IsAuthenticated)
            {
                string error = "User not authenticated";
                OnMatchmakingFailed?.Invoke(error);
                onError?.Invoke(error);
                return;
            }

            _currentRequest = new MatchmakingRequest
            {
                userId = Backend.UserAuthenticationSystem.Instance.UserId,
                mode = mode,
                partyMembers = partyMembers ?? new string[0],
                skillRating = GetPlayerSkillRating(),
                timestamp = DateTime.UtcNow,
                useSkillBasedMatchmaking = this.useSkillBasedMatchmaking,
                maxSkillDifference = this.maxSkillDifference
            };

            _queueStartTime = DateTime.UtcNow;
            _inQueue = true;
            _pollAttempts = 0;
            _totalQueues++;

            OnQueueStarted?.Invoke();

            StartCoroutine(JoinQueueCoroutine(onSuccess, onError));
        }

        private IEnumerator JoinQueueCoroutine(Action<MatchInfo> onSuccess, Action<string> onError)
        {
            string url = backendUrl + queueEndpoint;
            string json = JsonUtility.ToJson(_currentRequest);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {Backend.UserAuthenticationSystem.Instance.AuthToken}");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<QueueResponse>(request.downloadHandler.text);

                        Debug.Log($"[MatchmakingSystem] Joined queue: {response.queueId}");

                        // Start polling for match
                        StartCoroutine(PollMatchStatus(response.queueId, onSuccess, onError));
                    }
                    catch (Exception ex)
                    {
                        _inQueue = false;
                        string error = $"Failed to parse queue response: {ex.Message}";
                        OnMatchmakingFailed?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    _inQueue = false;
                    string error = $"Failed to join queue: {request.error}";
                    OnMatchmakingFailed?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[MatchmakingSystem] {error}");
                }
            }
        }

        /// <summary>
        /// Cancel matchmaking queue.
        /// </summary>
        public void CancelQueue()
        {
            if (!_inQueue)
            {
                Debug.LogWarning("[MatchmakingSystem] Not in queue");
                return;
            }

            StartCoroutine(CancelQueueCoroutine());
        }

        private IEnumerator CancelQueueCoroutine()
        {
            string url = backendUrl + cancelEndpoint;

            var requestData = new CancelRequest
            {
                userId = Backend.UserAuthenticationSystem.Instance.UserId,
                timestamp = DateTime.UtcNow
            };

            string json = JsonUtility.ToJson(requestData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {Backend.UserAuthenticationSystem.Instance.AuthToken}");
                request.timeout = 5;

                yield return request.SendWebRequest();

                _inQueue = false;
                _queueCancellations++;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    OnQueueCancelled?.Invoke("Queue cancelled by user");
                    Debug.Log("[MatchmakingSystem] Queue cancelled");
                }
                else
                {
                    Debug.LogWarning($"[MatchmakingSystem] Cancel request failed: {request.error}");
                }
            }
        }

        #endregion

        #region Match Polling

        private IEnumerator PollMatchStatus(string queueId, Action<MatchInfo> onSuccess, Action<string> onError)
        {
            while (_inQueue && _pollAttempts < maxPollAttempts)
            {
                yield return new WaitForSeconds(statusPollInterval);

                _pollAttempts++;

                // Update queue time
                OnQueueTimeUpdated?.Invoke(QueueTime);

                // Check timeout
                if (QueueTime >= queueTimeoutSeconds)
                {
                    _inQueue = false;
                    string error = "Matchmaking timeout";
                    OnMatchmakingFailed?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[MatchmakingSystem] {error}");
                    yield break;
                }

                // Poll status
                yield return StartCoroutine(CheckMatchStatusCoroutine(queueId, onSuccess, onError));

                if (!_inQueue)
                {
                    // Match found or cancelled
                    yield break;
                }
            }

            // Max polls reached
            if (_inQueue)
            {
                _inQueue = false;
                string error = "Matchmaking failed: max poll attempts reached";
                OnMatchmakingFailed?.Invoke(error);
                onError?.Invoke(error);
            }
        }

        private IEnumerator CheckMatchStatusCoroutine(string queueId, Action<MatchInfo> onSuccess, Action<string> onError)
        {
            string url = $"{backendUrl}{statusEndpoint}?queueId={queueId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {Backend.UserAuthenticationSystem.Instance.AuthToken}");
                request.timeout = 5;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<StatusResponse>(request.downloadHandler.text);

                        if (response.matchFound)
                        {
                            _inQueue = false;
                            _totalMatches++;

                            // Update average queue time
                            float queueTime = QueueTime;
                            _averageQueueTime = (_averageQueueTime * (_totalMatches - 1) + queueTime) / _totalMatches;

                            _currentMatch = response.match;

                            OnMatchFound?.Invoke(_currentMatch);
                            onSuccess?.Invoke(_currentMatch);

                            Debug.Log($"[MatchmakingSystem] Match found after {queueTime:F1}s: {_currentMatch.serverId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[MatchmakingSystem] Failed to parse status response: {ex.Message}");
                    }
                }
            }
        }

        #endregion

        #region Match Connection

        /// <summary>
        /// Connect to the matched server.
        /// </summary>
        public void ConnectToMatch(Action onSuccess = null, Action<string> onError = null)
        {
            if (_currentMatch == null)
            {
                onError?.Invoke("No active match");
                return;
            }

            Debug.Log($"[MatchmakingSystem] Connecting to match: {_currentMatch.serverId} at {_currentMatch.serverAddress}:{_currentMatch.serverPort}");

            // Implement actual connection logic here (depends on networking solution)
            // For now, just invoke success callback
            onSuccess?.Invoke();
        }

        /// <summary>
        /// Leave the current match.
        /// </summary>
        public void LeaveMatch()
        {
            if (_currentMatch == null)
            {
                Debug.LogWarning("[MatchmakingSystem] No active match");
                return;
            }

            Debug.Log($"[MatchmakingSystem] Leaving match: {_currentMatch.matchId}");

            _currentMatch = null;
        }

        #endregion

        #region Skill Rating

        private int GetPlayerSkillRating()
        {
            // Get player skill rating from PlayerPrefs or backend
            // For now, return a default value
            return PlayerPrefs.GetInt("SkillRating", 1000);
        }

        /// <summary>
        /// Update player skill rating.
        /// </summary>
        public void UpdateSkillRating(int newRating)
        {
            PlayerPrefs.SetInt("SkillRating", newRating);
            PlayerPrefs.Save();

            Debug.Log($"[MatchmakingSystem] Skill rating updated: {newRating}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get matchmaking statistics.
        /// </summary>
        public MatchmakingStats GetStats()
        {
            return new MatchmakingStats
            {
                totalQueues = _totalQueues,
                totalMatches = _totalMatches,
                queueCancellations = _queueCancellations,
                averageQueueTime = _averageQueueTime,
                currentQueueTime = QueueTime,
                isInQueue = _inQueue
            };
        }

        /// <summary>
        /// Quick play (join any available match).
        /// </summary>
        public void QuickPlay(Action<MatchInfo> onSuccess = null, Action<string> onError = null)
        {
            JoinQueue(MatchmakingMode.QuickPlay, null, onSuccess, onError);
        }

        /// <summary>
        /// Ranked play (skill-based matchmaking).
        /// </summary>
        public void RankedPlay(Action<MatchInfo> onSuccess = null, Action<string> onError = null)
        {
            JoinQueue(MatchmakingMode.Ranked, null, onSuccess, onError);
        }

        /// <summary>
        /// Party play (join with friends).
        /// </summary>
        public void PartyPlay(string[] partyMembers, Action<MatchInfo> onSuccess = null, Action<string> onError = null)
        {
            JoinQueue(MatchmakingMode.Party, partyMembers, onSuccess, onError);
        }

        #endregion

        #region Context Menu

        [ContextMenu("Join Quick Play")]
        private void JoinQuickPlayMenu()
        {
            QuickPlay();
        }

        [ContextMenu("Join Ranked")]
        private void JoinRankedMenu()
        {
            RankedPlay();
        }

        [ContextMenu("Cancel Queue")]
        private void CancelQueueMenu()
        {
            CancelQueue();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Matchmaking Statistics ===\n" +
                      $"Total Queues: {stats.totalQueues}\n" +
                      $"Total Matches: {stats.totalMatches}\n" +
                      $"Cancellations: {stats.queueCancellations}\n" +
                      $"Average Queue Time: {stats.averageQueueTime:F1}s\n" +
                      $"Current Queue Time: {stats.currentQueueTime:F1}s\n" +
                      $"In Queue: {stats.isInQueue}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Matchmaking request data.
    /// </summary>
    [Serializable]
    public class MatchmakingRequest
    {
        public string userId;
        public MatchmakingMode mode;
        public string[] partyMembers;
        public int skillRating;
        public DateTime timestamp;
        public bool useSkillBasedMatchmaking;
        public float maxSkillDifference;
    }

    /// <summary>
    /// Match information.
    /// </summary>
    [Serializable]
    public class MatchInfo
    {
        public string matchId;
        public string serverId;
        public string serverAddress;
        public int serverPort;
        public string[] playerIds;
        public MatchmakingMode mode;
        public int averageSkillRating;
        public DateTime createdAt;
    }

    /// <summary>
    /// Matchmaking statistics.
    /// </summary>
    [Serializable]
    public struct MatchmakingStats
    {
        public int totalQueues;
        public int totalMatches;
        public int queueCancellations;
        public float averageQueueTime;
        public float currentQueueTime;
        public bool isInQueue;
    }

    // Request/Response structures
    [Serializable] class QueueResponse { public string queueId; public int estimatedWaitTime; }
    [Serializable] class StatusResponse { public bool matchFound; public MatchInfo match; }
    [Serializable] class CancelRequest { public string userId; public DateTime timestamp; }

    /// <summary>
    /// Matchmaking modes.
    /// </summary>
    public enum MatchmakingMode
    {
        QuickPlay,
        Ranked,
        Party,
        Custom
    }

    #endregion
}
