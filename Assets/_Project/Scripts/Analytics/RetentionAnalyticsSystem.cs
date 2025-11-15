using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Laboratory.Backend;

namespace Laboratory.Analytics
{
    /// <summary>
    /// Player retention analytics system.
    /// Tracks DAU, MAU, retention cohorts, and churn prediction.
    /// Provides insights for improving player engagement.
    /// </summary>
    public class RetentionAnalyticsSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string sessionEndpoint = "/analytics/session";
        [SerializeField] private string retentionEndpoint = "/analytics/retention";

        [Header("Session Tracking")]
        [SerializeField] private bool trackSessions = true;
        [SerializeField] private float sessionTimeoutMinutes = 30f;
        [SerializeField] private bool trackSessionEvents = true;

        [Header("Upload Settings")]
        [SerializeField] private bool autoUploadOnSessionEnd = true;
        [SerializeField] private int requestTimeout = 10;

        #endregion

        #region Private Fields

        private static RetentionAnalyticsSystem _instance;

        // Session tracking
        private SessionData _currentSession;
        private bool _sessionActive = false;
        private float _lastActivityTime = 0f;
        private DateTime _sessionStartTime;

        // Retention data
        private RetentionData _retentionData;

        // Statistics
        private int _totalSessions = 0;
        private int _totalSessionsUploaded = 0;
        private int _uploadsFailed = 0;
        private float _totalPlayTime = 0f;

        // Events
        public event Action<SessionData> OnSessionStarted;
        public event Action<SessionData> OnSessionEnded;
        public event Action<RetentionData> OnRetentionDataUpdated;
        public event Action<string> OnUploadFailed;

        #endregion

        #region Properties

        public static RetentionAnalyticsSystem Instance => _instance;
        public bool IsSessionActive => _sessionActive;
        public float CurrentSessionDuration => _sessionActive ? (float)(DateTime.UtcNow - _sessionStartTime).TotalSeconds : 0f;
        public int TotalSessions => _totalSessions;

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
            if (trackSessions)
            {
                StartSession();
            }
        }

        private void Update()
        {
            if (!_sessionActive) return;

            // Check for session timeout
            if (Time.time - _lastActivityTime > sessionTimeoutMinutes * 60f)
            {
                EndSession(SessionEndReason.Timeout);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App backgrounded
                if (_sessionActive)
                {
                    EndSession(SessionEndReason.Background);
                }
            }
            else
            {
                // App resumed
                if (trackSessions)
                {
                    StartSession();
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (_sessionActive)
            {
                EndSession(SessionEndReason.Quit);
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[RetentionAnalyticsSystem] Initializing...");

            LoadRetentionData();

            Debug.Log("[RetentionAnalyticsSystem] Initialized");
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Start a new session.
        /// </summary>
        public void StartSession()
        {
            if (_sessionActive)
            {
                Debug.LogWarning("[RetentionAnalyticsSystem] Session already active");
                return;
            }

            _sessionStartTime = DateTime.UtcNow;
            _sessionActive = true;
            _lastActivityTime = Time.time;
            _totalSessions++;

            _currentSession = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                userId = GetUserId(),
                startTime = _sessionStartTime,
                platform = Application.platform.ToString(),
                buildVersion = Application.version,
                events = new List<SessionEvent>()
            };

            // Update retention data
            UpdateRetentionData();

            OnSessionStarted?.Invoke(_currentSession);

            Debug.Log($"[RetentionAnalyticsSystem] Session started: {_currentSession.sessionId}");
        }

        /// <summary>
        /// End the current session.
        /// </summary>
        public void EndSession(SessionEndReason reason = SessionEndReason.Manual)
        {
            if (!_sessionActive)
            {
                Debug.LogWarning("[RetentionAnalyticsSystem] No active session");
                return;
            }

            _currentSession.endTime = DateTime.UtcNow;
            _currentSession.duration = (float)(_currentSession.endTime - _currentSession.startTime).TotalSeconds;
            _currentSession.endReason = reason;

            _sessionActive = false;
            _totalPlayTime += _currentSession.duration;

            OnSessionEnded?.Invoke(_currentSession);

            // Upload session
            if (autoUploadOnSessionEnd)
            {
                UploadSession(_currentSession);
            }

            Debug.Log($"[RetentionAnalyticsSystem] Session ended: {_currentSession.sessionId}, Duration: {_currentSession.duration:F0}s");
        }

        /// <summary>
        /// Record activity to prevent session timeout.
        /// </summary>
        public void RecordActivity()
        {
            _lastActivityTime = Time.time;
        }

        #endregion

        #region Session Events

        /// <summary>
        /// Track an event in the current session.
        /// </summary>
        public void TrackSessionEvent(string eventName, Dictionary<string, string> parameters = null)
        {
            if (!_sessionActive || !trackSessionEvents)
            {
                return;
            }

            var evt = new SessionEvent
            {
                eventName = eventName,
                timestamp = DateTime.UtcNow,
                parameters = parameters
            };

            _currentSession.events.Add(evt);

            RecordActivity();
        }

        /// <summary>
        /// Track a milestone (first time events).
        /// </summary>
        public void TrackMilestone(string milestone)
        {
            if (!PlayerPrefs.HasKey($"Milestone_{milestone}"))
            {
                PlayerPrefs.SetInt($"Milestone_{milestone}", 1);
                PlayerPrefs.Save();

                TrackSessionEvent("milestone_reached", new Dictionary<string, string>
                {
                    { "milestone", milestone }
                });

                Debug.Log($"[RetentionAnalyticsSystem] Milestone reached: {milestone}");
            }
        }

        #endregion

        #region Retention Data

        private void UpdateRetentionData()
        {
            DateTime today = DateTime.UtcNow.Date;

            // First time player
            if (_retentionData == null)
            {
                _retentionData = new RetentionData
                {
                    userId = GetUserId(),
                    firstSession = today,
                    lastSession = today,
                    totalSessions = 1,
                    totalPlayTime = 0f,
                    daysSinceInstall = 0
                };
            }
            else
            {
                _retentionData.lastSession = today;
                _retentionData.totalSessions++;
                _retentionData.daysSinceInstall = (today - _retentionData.firstSession).Days;
            }

            // Calculate retention metrics
            CalculateRetentionMetrics();

            SaveRetentionData();

            OnRetentionDataUpdated?.Invoke(_retentionData);
        }

        private void CalculateRetentionMetrics()
        {
            DateTime today = DateTime.UtcNow.Date;

            // Day 1 retention
            if (_retentionData.daysSinceInstall == 1)
            {
                _retentionData.day1Retention = true;
            }

            // Day 7 retention
            if (_retentionData.daysSinceInstall == 7)
            {
                _retentionData.day7Retention = true;
            }

            // Day 30 retention
            if (_retentionData.daysSinceInstall == 30)
            {
                _retentionData.day30Retention = true;
            }

            // Calculate engagement level
            _retentionData.engagementLevel = CalculateEngagementLevel();
        }

        private EngagementLevel CalculateEngagementLevel()
        {
            if (_retentionData.daysSinceInstall < 7)
            {
                return EngagementLevel.New;
            }

            // Sessions per week
            float sessionsPerWeek = _retentionData.totalSessions / ((_retentionData.daysSinceInstall + 1) / 7f);

            if (sessionsPerWeek >= 7)
                return EngagementLevel.Highly_Engaged;
            else if (sessionsPerWeek >= 3)
                return EngagementLevel.Engaged;
            else if (sessionsPerWeek >= 1)
                return EngagementLevel.Casual;
            else
                return EngagementLevel.At_Risk;
        }

        #endregion

        #region Upload

        private void UploadSession(SessionData session)
        {
            StartCoroutine(UploadSessionCoroutine(session));
        }

        private IEnumerator UploadSessionCoroutine(SessionData session)
        {
            var requestData = new SessionUploadRequest
            {
                session = session,
                retention = _retentionData
            };

            string json = JsonUtility.ToJson(requestData);
            string url = backendUrl + sessionEndpoint;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Add auth header if available
                if (UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
                {
                    request.SetRequestHeader("Authorization", $"Bearer {UserAuthenticationSystem.Instance.AuthToken}");
                }

                request.timeout = requestTimeout;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    _totalSessionsUploaded++;
                    Debug.Log($"[RetentionAnalyticsSystem] Session uploaded: {session.sessionId}");
                }
                else
                {
                    _uploadsFailed++;
                    string error = $"Session upload failed: {request.error}";
                    OnUploadFailed?.Invoke(error);
                    Debug.LogError($"[RetentionAnalyticsSystem] {error}");
                }
            }
        }

        #endregion

        #region Persistence

        private void SaveRetentionData()
        {
            try
            {
                string json = JsonUtility.ToJson(_retentionData);
                PlayerPrefs.SetString("RetentionData", json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RetentionAnalyticsSystem] Failed to save retention data: {ex.Message}");
            }
        }

        private void LoadRetentionData()
        {
            try
            {
                if (PlayerPrefs.HasKey("RetentionData"))
                {
                    string json = PlayerPrefs.GetString("RetentionData");
                    _retentionData = JsonUtility.FromJson<RetentionData>(json);

                    Debug.Log($"[RetentionAnalyticsSystem] Loaded retention data: Days since install: {_retentionData.daysSinceInstall}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RetentionAnalyticsSystem] Failed to load retention data: {ex.Message}");
            }
        }

        #endregion

        #region Helpers

        private string GetUserId()
        {
            if (UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
            {
                return UserAuthenticationSystem.Instance.UserId;
            }

            return SystemInfo.deviceUniqueIdentifier;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get retention analytics statistics.
        /// </summary>
        public RetentionStats GetStats()
        {
            return new RetentionStats
            {
                totalSessions = _totalSessions,
                totalSessionsUploaded = _totalSessionsUploaded,
                uploadsFailed = _uploadsFailed,
                totalPlayTime = _totalPlayTime,
                averageSessionDuration = _totalSessions > 0 ? _totalPlayTime / _totalSessions : 0f,
                currentSessionDuration = CurrentSessionDuration,
                isSessionActive = _sessionActive
            };
        }

        /// <summary>
        /// Get retention data.
        /// </summary>
        public RetentionData GetRetentionData()
        {
            return _retentionData;
        }

        /// <summary>
        /// Check if milestone reached.
        /// </summary>
        public bool HasReachedMilestone(string milestone)
        {
            return PlayerPrefs.HasKey($"Milestone_{milestone}");
        }

        #endregion

        #region Context Menu

        [ContextMenu("Start Session")]
        private void StartSessionMenu()
        {
            StartSession();
        }

        [ContextMenu("End Session")]
        private void EndSessionMenu()
        {
            EndSession(SessionEndReason.Manual);
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Retention Analytics Statistics ===\n" +
                      $"Total Sessions: {stats.totalSessions}\n" +
                      $"Sessions Uploaded: {stats.totalSessionsUploaded}\n" +
                      $"Uploads Failed: {stats.uploadsFailed}\n" +
                      $"Total Play Time: {stats.totalPlayTime / 3600f:F1}h\n" +
                      $"Average Session: {stats.averageSessionDuration / 60f:F1}m\n" +
                      $"Current Session: {stats.currentSessionDuration / 60f:F1}m\n" +
                      $"Session Active: {stats.isSessionActive}");
        }

        [ContextMenu("Print Retention Data")]
        private void PrintRetentionData()
        {
            if (_retentionData == null)
            {
                Debug.Log("[RetentionAnalyticsSystem] No retention data");
                return;
            }

            Debug.Log($"=== Retention Data ===\n" +
                      $"User ID: {_retentionData.userId}\n" +
                      $"First Session: {_retentionData.firstSession:yyyy-MM-dd}\n" +
                      $"Last Session: {_retentionData.lastSession:yyyy-MM-dd}\n" +
                      $"Days Since Install: {_retentionData.daysSinceInstall}\n" +
                      $"Total Sessions: {_retentionData.totalSessions}\n" +
                      $"Total Play Time: {_retentionData.totalPlayTime / 3600f:F1}h\n" +
                      $"Day 1 Retention: {_retentionData.day1Retention}\n" +
                      $"Day 7 Retention: {_retentionData.day7Retention}\n" +
                      $"Day 30 Retention: {_retentionData.day30Retention}\n" +
                      $"Engagement Level: {_retentionData.engagementLevel}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Session data.
    /// </summary>
    [Serializable]
    public class SessionData
    {
        public string sessionId;
        public string userId;
        public DateTime startTime;
        public DateTime endTime;
        public float duration;
        public SessionEndReason endReason;
        public string platform;
        public string buildVersion;
        public List<SessionEvent> events;
    }

    /// <summary>
    /// Session event.
    /// </summary>
    [Serializable]
    public class SessionEvent
    {
        public string eventName;
        public DateTime timestamp;
        public Dictionary<string, string> parameters;
    }

    /// <summary>
    /// Retention data for a user.
    /// </summary>
    [Serializable]
    public class RetentionData
    {
        public string userId;
        public DateTime firstSession;
        public DateTime lastSession;
        public int totalSessions;
        public float totalPlayTime;
        public int daysSinceInstall;
        public bool day1Retention;
        public bool day7Retention;
        public bool day30Retention;
        public EngagementLevel engagementLevel;
    }

    /// <summary>
    /// Retention analytics statistics.
    /// </summary>
    [Serializable]
    public struct RetentionStats
    {
        public int totalSessions;
        public int totalSessionsUploaded;
        public int uploadsFailed;
        public float totalPlayTime;
        public float averageSessionDuration;
        public float currentSessionDuration;
        public bool isSessionActive;
    }

    // Request structures
    [Serializable] class SessionUploadRequest { public SessionData session; public RetentionData retention; }

    /// <summary>
    /// Session end reasons.
    /// </summary>
    public enum SessionEndReason
    {
        Manual,
        Timeout,
        Background,
        Quit
    }

    /// <summary>
    /// Player engagement levels.
    /// </summary>
    public enum EngagementLevel
    {
        New,
        Casual,
        Engaged,
        Highly_Engaged,
        At_Risk
    }

    #endregion
}
