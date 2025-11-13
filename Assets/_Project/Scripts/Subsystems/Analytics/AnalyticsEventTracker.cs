using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProjectChimera.Core;

namespace Laboratory.Analytics
{
    /// <summary>
    /// Analytics event tracking system for gameplay metrics and player behavior.
    /// Tracks custom events, user properties, and gameplay sessions.
    /// Provides local storage and export capabilities for analysis.
    /// </summary>
    public class AnalyticsEventTracker : MonoBehaviour
    {
        #region Configuration

        [Header("Tracking Settings")]
        [SerializeField] private bool enableTracking = true;
        [SerializeField] private bool logEvents = false;
        [SerializeField] private int maxEventsInMemory = 10000;
        [SerializeField] private bool enableSessionTracking = true;

        [Header("Auto-Tracking")]
        [SerializeField] private bool trackSceneLoads = true;
        [SerializeField] private bool trackApplicationEvents = true;
        [SerializeField] private bool trackPerformanceMetrics = false;

        [Header("Persistence")]
        [SerializeField] private bool saveEventsLocally = true;
        [SerializeField] private string saveDirectory = "AnalyticsData";
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes

        #endregion

        #region Private Fields

        private static AnalyticsEventTracker _instance;

        // Event storage
        private readonly List<AnalyticsEvent> _events = new List<AnalyticsEvent>();
        private readonly Dictionary<string, int> _eventCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, object> _userProperties = new Dictionary<string, object>();

        // Session tracking
        private AnalyticsSession _currentSession;
        private readonly List<AnalyticsSession> _sessions = new List<AnalyticsSession>();

        // Auto-save
        private float _lastSaveTime;

        // Statistics
        private int _totalEventsTracked;
        private int _uniqueEventTypes;

        #endregion

        #region Properties

        public static AnalyticsEventTracker Instance => _instance;
        public bool IsTrackingEnabled => enableTracking;
        public int EventCount => _events.Count;
        public string CurrentSessionId => _currentSession?.sessionId;

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

        private void Update()
        {
            if (!enableTracking) return;

            // Auto-save
            if (saveEventsLocally && Time.time - _lastSaveTime >= autoSaveInterval)
            {
                SaveEvents();
                _lastSaveTime = Time.time;
            }

            // Track performance metrics
            if (trackPerformanceMetrics)
            {
                TrackPerformanceMetrics();
            }
        }

        private void OnApplicationQuit()
        {
            EndSession();

            if (saveEventsLocally)
            {
                SaveEvents();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (trackApplicationEvents)
            {
                TrackEvent(pauseStatus ? "application_pause" : "application_resume");
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[AnalyticsEventTracker] Initializing...");

            // Start session tracking
            if (enableSessionTracking)
            {
                StartSession();
            }

            // Subscribe to scene loading
            if (trackSceneLoads)
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            }

            // Track application start
            if (trackApplicationEvents)
            {
                TrackEvent("application_start");
            }

            Debug.Log("[AnalyticsEventTracker] Initialized");
        }

        #endregion

        #region Session Tracking

        private void StartSession()
        {
            _currentSession = new AnalyticsSession
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = DateTime.UtcNow,
                platform = Application.platform.ToString(),
                version = Application.version,
                deviceModel = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem
            };

            _sessions.Add(_currentSession);

            if (logEvents)
            {
                Debug.Log($"[AnalyticsEventTracker] Session started: {_currentSession.sessionId}");
            }
        }

        private void EndSession()
        {
            if (_currentSession == null) return;

            _currentSession.endTime = DateTime.UtcNow;
            _currentSession.duration = (float)(_currentSession.endTime - _currentSession.startTime).TotalSeconds;
            _currentSession.eventCount = _currentSession.events.Count;

            if (logEvents)
            {
                Debug.Log($"[AnalyticsEventTracker] Session ended: {_currentSession.sessionId} ({_currentSession.duration:F1}s, {_currentSession.eventCount} events)");
            }

            _currentSession = null;
        }

        #endregion

        #region Event Tracking

        /// <summary>
        /// Track a simple event with just a name.
        /// </summary>
        public void TrackEvent(string eventName)
        {
            TrackEvent(eventName, null);
        }

        /// <summary>
        /// Track an event with custom parameters.
        /// </summary>
        public void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (!enableTracking) return;

            var analyticsEvent = new AnalyticsEvent
            {
                eventId = Guid.NewGuid().ToString(),
                eventName = eventName,
                timestamp = DateTime.UtcNow,
                sessionId = _currentSession?.sessionId,
                parameters = parameters ?? new Dictionary<string, object>()
            };

            // Add to storage
            _events.Add(analyticsEvent);
            _currentSession?.events.Add(analyticsEvent);

            // Update counts
            if (!_eventCounts.ContainsKey(eventName))
            {
                _eventCounts[eventName] = 0;
                _uniqueEventTypes++;
            }
            _eventCounts[eventName]++;
            _totalEventsTracked++;

            // Trim if exceeding max
            if (_events.Count > maxEventsInMemory)
            {
                _events.RemoveAt(0);
            }

            if (logEvents)
            {
                Debug.Log($"[AnalyticsEventTracker] Event: {eventName} {FormatParameters(parameters)}");
            }
        }

        /// <summary>
        /// Track a timed event (automatically calculates duration).
        /// </summary>
        public TimedEvent StartTimedEvent(string eventName)
        {
            return new TimedEvent(this, eventName);
        }

        /// <summary>
        /// Track a gameplay action.
        /// </summary>
        public void TrackGameplayAction(string action, string category = null, object value = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "action", action }
            };

            if (category != null)
                parameters["category"] = category;

            if (value != null)
                parameters["value"] = value;

            TrackEvent("gameplay_action", parameters);
        }

        /// <summary>
        /// Track a level or quest completion.
        /// </summary>
        public void TrackCompletion(string type, string name, float duration, bool success)
        {
            TrackEvent($"{type}_complete", new Dictionary<string, object>
            {
                { "name", name },
                { "duration", duration },
                { "success", success }
            });
        }

        /// <summary>
        /// Track an economy transaction.
        /// </summary>
        public void TrackTransaction(string itemName, int quantity, string currency, float amount)
        {
            TrackEvent("transaction", new Dictionary<string, object>
            {
                { "item", itemName },
                { "quantity", quantity },
                { "currency", currency },
                { "amount", amount }
            });
        }

        /// <summary>
        /// Track a social interaction.
        /// </summary>
        public void TrackSocialInteraction(string interactionType, string targetPlayer = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "interaction_type", interactionType }
            };

            if (targetPlayer != null)
                parameters["target_player"] = targetPlayer;

            TrackEvent("social_interaction", parameters);
        }

        /// <summary>
        /// Track an error or exception.
        /// </summary>
        public void TrackError(string errorType, string message, string stackTrace = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "error_type", errorType },
                { "message", message }
            };

            if (stackTrace != null)
                parameters["stack_trace"] = stackTrace;

            TrackEvent("error", parameters);
        }

        #endregion

        #region User Properties

        /// <summary>
        /// Set a user property.
        /// </summary>
        public void SetUserProperty(string propertyName, object value)
        {
            _userProperties[propertyName] = value;

            if (logEvents)
            {
                Debug.Log($"[AnalyticsEventTracker] User property: {propertyName} = {value}");
            }
        }

        /// <summary>
        /// Set multiple user properties.
        /// </summary>
        public void SetUserProperties(Dictionary<string, object> properties)
        {
            foreach (var kvp in properties)
            {
                _userProperties[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Get a user property.
        /// </summary>
        public object GetUserProperty(string propertyName)
        {
            return _userProperties.TryGetValue(propertyName, out var value) ? value : null;
        }

        #endregion

        #region Auto-Tracking

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            TrackEvent("scene_loaded", new Dictionary<string, object>
            {
                { "scene_name", scene.name },
                { "scene_index", scene.buildIndex },
                { "load_mode", mode.ToString() }
            });
        }

        private float _lastPerformanceTrack;
        private void TrackPerformanceMetrics()
        {
            if (Time.time - _lastPerformanceTrack < GameConstants.PERFORMANCE_TRACK_INTERVAL) return; // Track every minute
            _lastPerformanceTrack = Time.time;

            TrackEvent("performance_metrics", new Dictionary<string, object>
            {
                { "fps", (int)(1f / Time.deltaTime) },
                { "memory_mb", GC.GetTotalMemory(false) / 1024f / 1024f },
                { "frame_time_ms", Time.deltaTime * 1000f }
            });
        }

        #endregion

        #region Querying

        /// <summary>
        /// Get events by name.
        /// </summary>
        public List<AnalyticsEvent> GetEventsByName(string eventName)
        {
            return _events.Where(e => e.eventName == eventName).ToList();
        }

        /// <summary>
        /// Get events in time range.
        /// </summary>
        public List<AnalyticsEvent> GetEventsByTimeRange(DateTime startTime, DateTime endTime)
        {
            return _events.Where(e => e.timestamp >= startTime && e.timestamp <= endTime).ToList();
        }

        /// <summary>
        /// Get event count by name.
        /// </summary>
        public int GetEventCount(string eventName)
        {
            return _eventCounts.TryGetValue(eventName, out var count) ? count : 0;
        }

        /// <summary>
        /// Get all event names and counts.
        /// </summary>
        public Dictionary<string, int> GetAllEventCounts()
        {
            return new Dictionary<string, int>(_eventCounts);
        }

        #endregion

        #region Persistence

        private void SaveEvents()
        {
            try
            {
                string directory = System.IO.Path.Combine(Application.persistentDataPath, saveDirectory);
                System.IO.Directory.CreateDirectory(directory);

                // Save events
                var eventsData = new AnalyticsData
                {
                    events = _events,
                    sessions = _sessions,
                    userProperties = _userProperties,
                    saveTime = DateTime.UtcNow
                };

                string filename = $"analytics_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                string path = System.IO.Path.Combine(directory, filename);

                string json = JsonUtility.ToJson(eventsData, true);
                System.IO.File.WriteAllText(path, json);

                _lastSaveTime = Time.time;

                if (logEvents)
                {
                    Debug.Log($"[AnalyticsEventTracker] Saved {_events.Count} events to: {path}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsEventTracker] Failed to save events: {ex.Message}");
            }
        }

        /// <summary>
        /// Export events to CSV format.
        /// </summary>
        public string ExportToCSV()
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("EventID,EventName,Timestamp,SessionID,Parameters");

            // Rows
            foreach (var evt in _events)
            {
                sb.Append($"{evt.eventId},");
                sb.Append($"{evt.eventName},");
                sb.Append($"{evt.timestamp:yyyy-MM-dd HH:mm:ss},");
                sb.Append($"{evt.sessionId},");
                sb.Append($"\"{FormatParameters(evt.parameters)}\"");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export session data to CSV format.
        /// </summary>
        public string ExportSessionsToCSV()
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("SessionID,StartTime,EndTime,Duration,EventCount,Platform,Version");

            // Rows
            foreach (var session in _sessions)
            {
                sb.Append($"{session.sessionId},");
                sb.Append($"{session.startTime:yyyy-MM-dd HH:mm:ss},");
                sb.Append($"{session.endTime:yyyy-MM-dd HH:mm:ss},");
                sb.Append($"{session.duration:F2},");
                sb.Append($"{session.eventCount},");
                sb.Append($"{session.platform},");
                sb.Append($"{session.version}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Get analytics statistics.
        /// </summary>
        public AnalyticsStats GetStats()
        {
            return new AnalyticsStats
            {
                totalEventsTracked = _totalEventsTracked,
                uniqueEventTypes = _uniqueEventTypes,
                eventsInMemory = _events.Count,
                sessionCount = _sessions.Count,
                currentSessionDuration = _currentSession != null
                    ? (float)(DateTime.UtcNow - _currentSession.startTime).TotalSeconds
                    : 0f,
                isTrackingEnabled = enableTracking
            };
        }

        /// <summary>
        /// Generate a summary report.
        /// </summary>
        public string GenerateReport()
        {
            var stats = GetStats();
            var sb = new StringBuilder();

            sb.AppendLine("=== Analytics Summary Report ===");
            sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine($"Total Events Tracked: {stats.totalEventsTracked}");
            sb.AppendLine($"Unique Event Types: {stats.uniqueEventTypes}");
            sb.AppendLine($"Events in Memory: {stats.eventsInMemory}");
            sb.AppendLine($"Sessions: {stats.sessionCount}");
            sb.AppendLine($"Current Session Duration: {stats.currentSessionDuration:F1}s");
            sb.AppendLine();

            sb.AppendLine("Top Events:");
            var topEvents = _eventCounts.OrderByDescending(kvp => kvp.Value).Take(10);
            foreach (var kvp in topEvents)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }
            sb.AppendLine();

            sb.AppendLine("User Properties:");
            foreach (var kvp in _userProperties)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
            }

            return sb.ToString();
        }

        #endregion

        #region Helper Methods

        private string FormatParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return "";

            return string.Join(", ", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }

        #endregion

        #region Public API

        /// <summary>
        /// Clear all tracked events.
        /// </summary>
        public void ClearEvents()
        {
            _events.Clear();
            _eventCounts.Clear();
            _uniqueEventTypes = 0;

            Debug.Log("[AnalyticsEventTracker] Cleared all events");
        }

        /// <summary>
        /// Clear all sessions.
        /// </summary>
        public void ClearSessions()
        {
            _sessions.Clear();
            Debug.Log("[AnalyticsEventTracker] Cleared all sessions");
        }

        #endregion

        #region Context Menu

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            Debug.Log(GenerateReport());
        }

        [ContextMenu("Save Events Now")]
        private void SaveEventsNow()
        {
            SaveEvents();
        }

        [ContextMenu("Export Events to CSV")]
        private void ExportEventsToCSV()
        {
            string csv = ExportToCSV();
            string path = System.IO.Path.Combine(Application.persistentDataPath, $"analytics_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            System.IO.File.WriteAllText(path, csv);
            Debug.Log($"[AnalyticsEventTracker] Exported events to: {path}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// An analytics event.
    /// </summary>
    [Serializable]
    public class AnalyticsEvent
    {
        public string eventId;
        public string eventName;
        public DateTime timestamp;
        public string sessionId;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
    }

    /// <summary>
    /// An analytics session.
    /// </summary>
    [Serializable]
    public class AnalyticsSession
    {
        public string sessionId;
        public DateTime startTime;
        public DateTime endTime;
        public float duration;
        public string platform;
        public string version;
        public string deviceModel;
        public string operatingSystem;
        public int eventCount;
        public List<AnalyticsEvent> events = new List<AnalyticsEvent>();
    }

    /// <summary>
    /// Container for analytics data persistence.
    /// </summary>
    [Serializable]
    public class AnalyticsData
    {
        public List<AnalyticsEvent> events;
        public List<AnalyticsSession> sessions;
        public Dictionary<string, object> userProperties;
        public DateTime saveTime;
    }

    /// <summary>
    /// Analytics statistics.
    /// </summary>
    [Serializable]
    public struct AnalyticsStats
    {
        public int totalEventsTracked;
        public int uniqueEventTypes;
        public int eventsInMemory;
        public int sessionCount;
        public float currentSessionDuration;
        public bool isTrackingEnabled;
    }

    /// <summary>
    /// Helper class for timed events.
    /// </summary>
    public class TimedEvent : IDisposable
    {
        private readonly AnalyticsEventTracker _tracker;
        private readonly string _eventName;
        private readonly DateTime _startTime;

        public TimedEvent(AnalyticsEventTracker tracker, string eventName)
        {
            _tracker = tracker;
            _eventName = eventName;
            _startTime = DateTime.UtcNow;
        }

        public void Dispose()
        {
            float duration = (float)(DateTime.UtcNow - _startTime).TotalSeconds;
            _tracker.TrackEvent(_eventName, new Dictionary<string, object>
            {
                { "duration", duration }
            });
        }
    }

    #endregion
}
