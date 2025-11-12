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
    /// Live events system for time-limited content and competitions.
    /// Manages event scheduling, progress tracking, and rewards.
    /// Supports recurring events, challenges, and seasonal content.
    /// </summary>
    public class LiveEventsSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string eventsEndpoint = "/events/active";
        [SerializeField] private string progressEndpoint = "/events/progress";
        [SerializeField] private string claimEndpoint = "/events/claim";

        [Header("Fetch Settings")]
        [SerializeField] private bool fetchOnStart = true;
        [SerializeField] private bool autoRefresh = true;
        [SerializeField] private float refreshInterval = 600f; // 10 minutes
        [SerializeField] private int requestTimeout = 10;

        [Header("Notifications")]
        [SerializeField] private bool notifyOnNewEvent = true;
        [SerializeField] private bool notifyOnEventEnding = true;
        [SerializeField] private float eventEndingWarning = 3600f; // 1 hour

        #endregion

        #region Private Fields

        private static LiveEventsSystem _instance;

        // Active events
        private readonly Dictionary<string, LiveEvent> _activeEvents = new Dictionary<string, LiveEvent>();
        private readonly Dictionary<string, EventProgress> _eventProgress = new Dictionary<string, EventProgress>();

        // State
        private bool _isFetching = false;
        private float _lastFetchTime = 0f;

        // Statistics
        private int _totalEventsFetched = 0;
        private int _totalProgressUpdates = 0;
        private int _totalRewardsClaimed = 0;

        // Events
        public event Action<LiveEvent> OnEventStarted;
        public event Action<LiveEvent> OnEventEnding;
        public event Action<LiveEvent> OnEventEnded;
        public event Action<string, EventProgress> OnProgressUpdated;
        public event Action<string, EventReward[]> OnRewardsClaimed;
        public event Action<string> OnEventError;

        #endregion

        #region Properties

        public static LiveEventsSystem Instance => _instance;
        public int ActiveEventCount => _activeEvents.Count;
        public bool IsFetching => _isFetching;
        public LiveEvent[] ActiveEvents => _activeEvents.Values.ToArray();

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
            if (fetchOnStart)
            {
                FetchActiveEvents();
            }
        }

        private void Update()
        {
            if (!autoRefresh || _isFetching) return;

            // Auto-refresh events
            if (Time.time - _lastFetchTime >= refreshInterval)
            {
                FetchActiveEvents();
            }

            // Check for ending events
            if (notifyOnEventEnding)
            {
                CheckEndingEvents();
            }

            // Remove expired events
            RemoveExpiredEvents();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[LiveEventsSystem] Initializing...");
            Debug.Log("[LiveEventsSystem] Initialized");
        }

        #endregion

        #region Event Fetching

        /// <summary>
        /// Fetch active events from backend.
        /// </summary>
        public void FetchActiveEvents(Action onSuccess = null, Action<string> onError = null)
        {
            if (_isFetching)
            {
                Debug.LogWarning("[LiveEventsSystem] Fetch already in progress");
                return;
            }

            StartCoroutine(FetchEventsCoroutine(onSuccess, onError));
        }

        private IEnumerator FetchEventsCoroutine(Action onSuccess, Action<string> onError)
        {
            _isFetching = true;

            string url = backendUrl + eventsEndpoint;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Add auth header if available
                if (UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
                {
                    request.SetRequestHeader("Authorization", $"Bearer {UserAuthenticationSystem.Instance.AuthToken}");
                }

                request.timeout = requestTimeout;

                yield return request.SendWebRequest();

                _isFetching = false;
                _lastFetchTime = Time.time;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<EventsResponse>(request.downloadHandler.text);

                        ProcessEvents(response.events);

                        _totalEventsFetched += response.events.Length;

                        onSuccess?.Invoke();

                        Debug.Log($"[LiveEventsSystem] Fetched {response.events.Length} active events");
                    }
                    catch (Exception ex)
                    {
                        string error = $"Failed to parse events response: {ex.Message}";
                        OnEventError?.Invoke(error);
                        onError?.Invoke(error);
                        Debug.LogError($"[LiveEventsSystem] {error}");
                    }
                }
                else
                {
                    string error = $"Events fetch failed: {request.error}";
                    OnEventError?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[LiveEventsSystem] {error}");
                }
            }
        }

        private void ProcessEvents(LiveEvent[] events)
        {
            foreach (var evt in events)
            {
                bool isNew = !_activeEvents.ContainsKey(evt.eventId);

                _activeEvents[evt.eventId] = evt;

                if (isNew)
                {
                    OnEventStarted?.Invoke(evt);

                    if (notifyOnNewEvent)
                    {
                        Debug.Log($"[LiveEventsSystem] New event started: {evt.name}");
                    }
                }
            }
        }

        #endregion

        #region Progress Tracking

        /// <summary>
        /// Update progress for an event.
        /// </summary>
        public void UpdateProgress(string eventId, string objectiveId, int value, Action onSuccess = null, Action<string> onError = null)
        {
            if (!_activeEvents.ContainsKey(eventId))
            {
                Debug.LogWarning($"[LiveEventsSystem] Event not found: {eventId}");
                return;
            }

            StartCoroutine(UpdateProgressCoroutine(eventId, objectiveId, value, onSuccess, onError));
        }

        private IEnumerator UpdateProgressCoroutine(string eventId, string objectiveId, int value, Action onSuccess, Action<string> onError)
        {
            _totalProgressUpdates++;

            string url = backendUrl + progressEndpoint;

            var requestData = new ProgressUpdateRequest
            {
                eventId = eventId,
                objectiveId = objectiveId,
                value = value,
                timestamp = DateTime.UtcNow
            };

            string json = JsonUtility.ToJson(requestData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                if (UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
                {
                    request.SetRequestHeader("Authorization", $"Bearer {UserAuthenticationSystem.Instance.AuthToken}");
                }

                request.timeout = requestTimeout;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<ProgressUpdateResponse>(request.downloadHandler.text);

                        // Update local progress
                        _eventProgress[eventId] = response.progress;

                        OnProgressUpdated?.Invoke(eventId, response.progress);
                        onSuccess?.Invoke();

                        Debug.Log($"[LiveEventsSystem] Progress updated: {eventId} - {objectiveId} = {value}");
                    }
                    catch (Exception ex)
                    {
                        string error = $"Failed to parse progress response: {ex.Message}";
                        OnEventError?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Progress update failed: {request.error}";
                    OnEventError?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[LiveEventsSystem] {error}");
                }
            }
        }

        /// <summary>
        /// Increment progress for an event objective.
        /// </summary>
        public void IncrementProgress(string eventId, string objectiveId, int increment = 1, Action onSuccess = null, Action<string> onError = null)
        {
            // Get current progress
            int currentValue = 0;

            if (_eventProgress.TryGetValue(eventId, out var progress))
            {
                var objective = progress.objectives.FirstOrDefault(o => o.objectiveId == objectiveId);
                if (objective != null)
                {
                    currentValue = objective.currentValue;
                }
            }

            UpdateProgress(eventId, objectiveId, currentValue + increment, onSuccess, onError);
        }

        #endregion

        #region Rewards

        /// <summary>
        /// Claim rewards for completed event objectives.
        /// </summary>
        public void ClaimRewards(string eventId, Action<EventReward[]> onSuccess = null, Action<string> onError = null)
        {
            if (!_activeEvents.ContainsKey(eventId))
            {
                Debug.LogWarning($"[LiveEventsSystem] Event not found: {eventId}");
                return;
            }

            StartCoroutine(ClaimRewardsCoroutine(eventId, onSuccess, onError));
        }

        private IEnumerator ClaimRewardsCoroutine(string eventId, Action<EventReward[]> onSuccess, Action<string> onError)
        {
            string url = backendUrl + claimEndpoint;

            var requestData = new ClaimRewardsRequest
            {
                eventId = eventId,
                timestamp = DateTime.UtcNow
            };

            string json = JsonUtility.ToJson(requestData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                if (UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
                {
                    request.SetRequestHeader("Authorization", $"Bearer {UserAuthenticationSystem.Instance.AuthToken}");
                }

                request.timeout = requestTimeout;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<ClaimRewardsResponse>(request.downloadHandler.text);

                        _totalRewardsClaimed += response.rewards.Length;

                        OnRewardsClaimed?.Invoke(eventId, response.rewards);
                        onSuccess?.Invoke(response.rewards);

                        Debug.Log($"[LiveEventsSystem] Claimed {response.rewards.Length} rewards from event: {eventId}");
                    }
                    catch (Exception ex)
                    {
                        string error = $"Failed to parse claim response: {ex.Message}";
                        OnEventError?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Claim rewards failed: {request.error}";
                    OnEventError?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[LiveEventsSystem] {error}");
                }
            }
        }

        #endregion

        #region Event Management

        private void CheckEndingEvents()
        {
            foreach (var kvp in _activeEvents)
            {
                var evt = kvp.Value;
                double secondsRemaining = (evt.endTime - DateTime.UtcNow).TotalSeconds;

                if (secondsRemaining > 0 && secondsRemaining <= eventEndingWarning)
                {
                    OnEventEnding?.Invoke(evt);
                }
            }
        }

        private void RemoveExpiredEvents()
        {
            var expiredIds = new List<string>();

            foreach (var kvp in _activeEvents)
            {
                if (DateTime.UtcNow >= kvp.Value.endTime)
                {
                    expiredIds.Add(kvp.Key);
                    OnEventEnded?.Invoke(kvp.Value);
                    Debug.Log($"[LiveEventsSystem] Event ended: {kvp.Value.name}");
                }
            }

            foreach (var id in expiredIds)
            {
                _activeEvents.Remove(id);
                _eventProgress.Remove(id);
            }
        }

        #endregion

        #region Query API

        /// <summary>
        /// Get an event by ID.
        /// </summary>
        public LiveEvent GetEvent(string eventId)
        {
            return _activeEvents.TryGetValue(eventId, out var evt) ? evt : null;
        }

        /// <summary>
        /// Get progress for an event.
        /// </summary>
        public EventProgress GetProgress(string eventId)
        {
            return _eventProgress.TryGetValue(eventId, out var progress) ? progress : null;
        }

        /// <summary>
        /// Get events by type.
        /// </summary>
        public LiveEvent[] GetEventsByType(EventType type)
        {
            return _activeEvents.Values.Where(e => e.eventType == type).ToArray();
        }

        /// <summary>
        /// Check if an event is active.
        /// </summary>
        public bool IsEventActive(string eventId)
        {
            return _activeEvents.ContainsKey(eventId);
        }

        /// <summary>
        /// Get time remaining for an event.
        /// </summary>
        public TimeSpan GetTimeRemaining(string eventId)
        {
            if (_activeEvents.TryGetValue(eventId, out var evt))
            {
                var remaining = evt.endTime - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }

            return TimeSpan.Zero;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get live events statistics.
        /// </summary>
        public LiveEventsStats GetStats()
        {
            return new LiveEventsStats
            {
                activeEventCount = _activeEvents.Count,
                totalEventsFetched = _totalEventsFetched,
                totalProgressUpdates = _totalProgressUpdates,
                totalRewardsClaimed = _totalRewardsClaimed,
                secondsSinceLastFetch = Time.time - _lastFetchTime
            };
        }

        /// <summary>
        /// Force refresh active events.
        /// </summary>
        public void ForceRefresh()
        {
            if (_isFetching)
            {
                Debug.LogWarning("[LiveEventsSystem] Already fetching");
                return;
            }

            FetchActiveEvents();
        }

        #endregion

        #region Context Menu

        [ContextMenu("Fetch Active Events")]
        private void FetchActiveEventsMenu()
        {
            FetchActiveEvents();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Live Events Statistics ===\n" +
                      $"Active Events: {stats.activeEventCount}\n" +
                      $"Total Events Fetched: {stats.totalEventsFetched}\n" +
                      $"Progress Updates: {stats.totalProgressUpdates}\n" +
                      $"Rewards Claimed: {stats.totalRewardsClaimed}\n" +
                      $"Seconds Since Last Fetch: {stats.secondsSinceLastFetch:F0}s");
        }

        [ContextMenu("Print Active Events")]
        private void PrintActiveEvents()
        {
            if (_activeEvents.Count == 0)
            {
                Debug.Log("[LiveEventsSystem] No active events");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== Active Events ===");

            foreach (var evt in _activeEvents.Values)
            {
                var remaining = GetTimeRemaining(evt.eventId);
                sb.AppendLine($"  {evt.name} ({evt.eventType})");
                sb.AppendLine($"    ID: {evt.eventId}");
                sb.AppendLine($"    Time Remaining: {remaining.TotalHours:F1}h");
            }

            Debug.Log(sb.ToString());
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Live event data.
    /// </summary>
    [Serializable]
    public class LiveEvent
    {
        public string eventId;
        public string name;
        public string description;
        public EventType eventType;
        public DateTime startTime;
        public DateTime endTime;
        public EventObjective[] objectives;
        public EventReward[] rewards;
        public Dictionary<string, string> metadata;
    }

    /// <summary>
    /// Event objective.
    /// </summary>
    [Serializable]
    public class EventObjective
    {
        public string objectiveId;
        public string description;
        public int targetValue;
        public int currentValue;
        public bool isCompleted;
    }

    /// <summary>
    /// Event progress.
    /// </summary>
    [Serializable]
    public class EventProgress
    {
        public string eventId;
        public EventObjective[] objectives;
        public int completedObjectives;
        public float completionPercentage;
    }

    /// <summary>
    /// Event reward.
    /// </summary>
    [Serializable]
    public class EventReward
    {
        public string rewardId;
        public string rewardType;
        public int quantity;
        public bool isClaimed;
    }

    /// <summary>
    /// Live events statistics.
    /// </summary>
    [Serializable]
    public struct LiveEventsStats
    {
        public int activeEventCount;
        public int totalEventsFetched;
        public int totalProgressUpdates;
        public int totalRewardsClaimed;
        public float secondsSinceLastFetch;
    }

    // Request/Response structures
    [Serializable] class EventsResponse { public LiveEvent[] events; }
    [Serializable] class ProgressUpdateRequest { public string eventId; public string objectiveId; public int value; public DateTime timestamp; }
    [Serializable] class ProgressUpdateResponse { public EventProgress progress; }
    [Serializable] class ClaimRewardsRequest { public string eventId; public DateTime timestamp; }
    [Serializable] class ClaimRewardsResponse { public EventReward[] rewards; }

    /// <summary>
    /// Event types.
    /// </summary>
    public enum EventType
    {
        Challenge,
        Tournament,
        Seasonal,
        Raid,
        Community
    }

    #endregion
}
