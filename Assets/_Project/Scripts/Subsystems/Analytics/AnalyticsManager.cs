using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Text;
using System.Linq;
using Laboratory.Core.Events;
using Laboratory.Core.Infrastructure;
using Laboratory.Subsystems.Combat.CoreAbilities;
using Laboratory.Subsystems.Spawning;

namespace Laboratory.Subsystems.Analytics
{
    /// <summary>
    /// Advanced analytics and telemetry system for tracking player behavior, performance metrics,
    /// and game events. Supports local storage, remote analytics, and real-time monitoring.
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        [Header("Analytics Configuration")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool enableLocalLogging = true;
        [SerializeField] private bool enableRemoteAnalytics = false;
        
        [Header("Data Collection")]
        [SerializeField] private float sessionUpdateInterval = 30f;
        [SerializeField] private int maxEventsPerBatch = 100;
        [SerializeField] private float batchSendInterval = 60f;
        
        [Header("Privacy Settings")]
        [SerializeField] private bool respectPlayerPrivacy = true;
        [SerializeField] private bool anonymizeUserData = true;
        
        [Header("Storage Settings")]
        [SerializeField] private string analyticsFileName = "analytics_data.json";
        [SerializeField] private int maxLogFileSize = 10 * 1024 * 1024; // 10MB
        
        // Analytics data
        private AnalyticsSession currentSession;
        private Queue<AnalyticsEvent> eventQueue = new Queue<AnalyticsEvent>();
        private Dictionary<string, object> sessionParameters = new Dictionary<string, object>();
        private Dictionary<string, int> eventCounters = new Dictionary<string, int>();
        
        // File management
        private string analyticsFilePath;
        private float lastSessionUpdate = 0f;
        private float lastBatchSend = 0f;
        private bool isInitialized = false;
        
        private void Start()
        {
            InitializeAnalytics();
            StartSession();
            
            if (enableAnalytics)
            {
                StartCoroutine(AnalyticsUpdateCoroutine());
            }
        }

        private void InitializeAnalytics()
        {
            analyticsFilePath = Path.Combine(Application.persistentDataPath, analyticsFileName);
            
            // Subscribe to common game events
            if (ServiceContainer.Instance != null)
            {
                var eventBus = ServiceContainer.Instance.ResolveService<IEventBus>();
                if (eventBus != null)
                {
                    eventBus.Subscribe<AttackPerformedEvent>(OnAttackPerformed);
                    eventBus.Subscribe<ObjectSpawnedEvent>(OnObjectSpawned);
                    StartPerformanceTracking();
                }
            }
            
            LoadPersistentAnalyticsData();
            isInitialized = true;
        }

        private void StartSession()
        {
            currentSession = new AnalyticsSession
            {
                SessionId = System.Guid.NewGuid().ToString(),
                StartTime = System.DateTime.UtcNow,
                UserId = GetAnonymizedUserId(),
                Platform = Application.platform.ToString(),
                UnityVersion = Application.unityVersion,
                ApplicationVersion = Application.version,
                DeviceInfo = GetDeviceInfo()
            };
            
            TrackEvent("session_start", new Dictionary<string, object>
            {
                {"session_id", currentSession.SessionId},
                {"platform", currentSession.Platform},
                {"device_info", currentSession.DeviceInfo}
            });
        }

        private IEnumerator AnalyticsUpdateCoroutine()
        {
            while (enableAnalytics)
            {
                // Update session data
                if (Time.time - lastSessionUpdate >= sessionUpdateInterval)
                {
                    UpdateSessionData();
                    lastSessionUpdate = Time.time;
                }
                
                // Send batch data
                if (Time.time - lastBatchSend >= batchSendInterval)
                {
                    if (enableRemoteAnalytics)
                    {
                        SendBatchData();
                    }
                    lastBatchSend = Time.time;
                }
                
                // Save local data
                if (enableLocalLogging && eventQueue.Count > 0)
                {
                    SaveAnalyticsDataToFile();
                }
                
                yield return new WaitForSeconds(1f);
            }
        }

        /// <summary>
        /// Track a custom analytics event
        /// </summary>
        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!enableAnalytics) return;
            
            var analyticsEvent = new PlayerActionEvent
            {
                actionType = eventName,
                timestamp = System.DateTime.UtcNow,
                sessionId = currentSession?.SessionId ?? "unknown",
                parameters = parameters ?? new Dictionary<string, object>()
            };
            
            // Add common parameters
            analyticsEvent.parameters["session_time"] = GetSessionDuration();
            analyticsEvent.parameters["user_id"] = currentSession?.UserId ?? "anonymous";
            
            eventQueue.Enqueue(analyticsEvent);
            
            // Update event counters
            if (eventCounters.ContainsKey(eventName))
            {
                eventCounters[eventName]++;
            }
            else
            {
                eventCounters[eventName] = 1;
            }
            
            // Limit queue size
            if (eventQueue.Count > maxEventsPerBatch * 2)
            {
                eventQueue.Dequeue();
            }
        }

        /// <summary>
        /// Track player progression event
        /// </summary>
        public void TrackProgression(string progressionType, string level, int score = 0)
        {
            TrackEvent("player_progression", new Dictionary<string, object>
            {
                {"progression_type", progressionType},
                {"level", level},
                {"score", score},
                {"timestamp", System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}
            });
        }

        /// <summary>
        /// Track performance metrics
        /// </summary>
        public void TrackPerformance(string metricName, float value, Dictionary<string, object> context = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"metric_name", metricName},
                {"value", value},
                {"timestamp", System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}
            };
            
            if (context != null)
            {
                foreach (var kvp in context)
                {
                    parameters[$"context_{kvp.Key}"] = kvp.Value;
                }
            }
            
            TrackEvent("performance_metric", parameters);
        }

        /// <summary>
        /// Track user behavior event
        /// </summary>
        public void TrackUserBehavior(string action, string target, Dictionary<string, object> properties = null)
        {
            var parameters = new Dictionary<string, object>
            {
                {"action", action},
                {"target", target}
            };
            
            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
            }
            
            TrackEvent("user_behavior", parameters);
        }

        /// <summary>
        /// Set a session parameter that will be included with all events
        /// </summary>
        public void SetSessionParameter(string key, object value)
        {
            sessionParameters[key] = value;
        }

        private void UpdateSessionData()
        {
            if (currentSession == null) return;
            
            currentSession.LastUpdateTime = System.DateTime.UtcNow;
            currentSession.SessionDuration = GetSessionDuration();
            currentSession.EventCount = eventCounters.Values.Sum();
            
            // Track session update
            TrackEvent("session_update", new Dictionary<string, object>
            {
                {"duration", currentSession.SessionDuration},
                {"event_count", currentSession.EventCount}
            });
        }

        private void SendBatchData()
        {
            if (eventQueue.Count == 0) return;
            
            var batchEvents = new List<AnalyticsEvent>();
            int batchSize = Mathf.Min(maxEventsPerBatch, eventQueue.Count);
            
            for (int i = 0; i < batchSize; i++)
            {
                batchEvents.Add(eventQueue.Dequeue());
            }
            
            var batchData = new AnalyticsBatch
            {
                BatchId = System.Guid.NewGuid().ToString(),
                SessionId = currentSession?.SessionId ?? "unknown",
                Timestamp = System.DateTime.UtcNow,
                Events = batchEvents,
                SessionParameters = new Dictionary<string, object>(sessionParameters)
            };
            
            StartCoroutine(SendBatchToRemoteService(batchData));
        }

        private IEnumerator SendBatchToRemoteService(AnalyticsBatch batch)
        {
            // Placeholder for remote analytics service integration
            // This would typically send to services like Unity Analytics, Firebase, etc.
            
            // Simulate network request
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log($"Analytics batch sent: {batch.Events.Count} events");
            
            // In a real implementation, you would use UnityWebRequest or similar
            // to send the data to your analytics endpoint
        }

        private void SaveAnalyticsDataToFile()
        {
            try
            {
                var dataToSave = new List<AnalyticsEvent>();
                
                // Save up to maxEventsPerBatch events
                int saveCount = Mathf.Min(maxEventsPerBatch, eventQueue.Count);
                for (int i = 0; i < saveCount; i++)
                {
                    if (eventQueue.Count > 0)
                    {
                        dataToSave.Add(eventQueue.Dequeue());
                    }
                }
                
                if (dataToSave.Count > 0)
                {
                    string jsonData = JsonUtility.ToJson(new SerializableList<AnalyticsEvent> { items = dataToSave }, true);
                    
                    // Check file size and rotate if necessary
                    if (File.Exists(analyticsFilePath))
                    {
                        var fileInfo = new FileInfo(analyticsFilePath);
                        if (fileInfo.Length > maxLogFileSize)
                        {
                            RotateLogFile();
                        }
                    }
                    
                    File.AppendAllText(analyticsFilePath, jsonData + "\n", Encoding.UTF8);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save analytics data: {ex.Message}");
            }
        }

        private void RotateLogFile()
        {
            string backupPath = analyticsFilePath.Replace(".json", $"_backup_{System.DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            File.Move(analyticsFilePath, backupPath);
        }

        private void LoadPersistentAnalyticsData()
        {
            try
            {
                if (File.Exists(analyticsFilePath))
                {
                    // Load previous analytics data if needed for continuation
                    // This could be used to resume sessions or maintain counters
                    string jsonData = File.ReadAllText(analyticsFilePath, Encoding.UTF8);
                    // Process loaded data as needed
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load analytics data: {ex.Message}");
            }
        }

        private string GetAnonymizedUserId()
        {
            if (!respectPlayerPrivacy)
            {
                return SystemInfo.deviceUniqueIdentifier;
            }
            
            // Create anonymous hash-based ID
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            return anonymizeUserData ? HashString(deviceId).Substring(0, 16) : deviceId;
        }

        private string HashString(string input)
        {
            // Simple hash function for Unity (without System.Security.Cryptography)
            int hash = 5381;
            for (int i = 0; i < input.Length; i++)
            {
                hash = ((hash << 5) + hash) + input[i];
            }
            return hash.ToString("X");
        }

        private DeviceInfo GetDeviceInfo()
        {
            return new DeviceInfo
            {
                DeviceModel = SystemInfo.deviceModel,
                DeviceType = SystemInfo.deviceType.ToString(),
                OperatingSystem = SystemInfo.operatingSystem,
                ProcessorType = SystemInfo.processorType,
                SystemMemorySize = SystemInfo.systemMemorySize,
                GraphicsDeviceName = SystemInfo.graphicsDeviceName,
                GraphicsMemorySize = SystemInfo.graphicsMemorySize,
                ScreenResolution = $"{Screen.width}x{Screen.height}",
                ScreenDPI = Screen.dpi
            };
        }

        private double GetSessionDuration()
        {
            if (currentSession == null) return 0;
            return (System.DateTime.UtcNow - currentSession.StartTime).TotalSeconds;
        }

        #region Event Handlers

        private void StartPerformanceTracking()
        {
            StartCoroutine(TrackPerformanceMetrics());
        }

        private System.Collections.IEnumerator TrackPerformanceMetrics()
        {
            while (isInitialized)
            {
                TrackPerformance("fps", 1f / Time.deltaTime, new Dictionary<string, object>
                {
                    {"frame_time", Time.deltaTime * 1000f},
                    {"memory_usage", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024f * 1024f)},
                    {"unity_frame_count", Time.frameCount}
                });

                yield return new WaitForSeconds(1f);
            }
        }

        private void OnAttackPerformed(AttackPerformedEvent eventArgs)
        {
            TrackUserBehavior("attack", eventArgs.Target?.name ?? "unknown", new Dictionary<string, object>
            {
                {"damage", eventArgs.Damage},
                {"attack_type", eventArgs.AttackType},
                {"attacker", eventArgs.Attacker?.name ?? "unknown"}
            });
        }

        private void OnObjectSpawned(ObjectSpawnedEvent eventArgs)
        {
            TrackEvent("object_spawned", new Dictionary<string, object>
            {
                {"object_name", eventArgs.SpawnedObject?.name ?? "unknown"},
                {"spawn_point", eventArgs.SpawnPoint?.SpawnTag ?? "unknown"},
                {"spawn_position", eventArgs.SpawnPoint?.transform.position.ToString() ?? "unknown"}
            });
        }

        #endregion

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                TrackEvent("application_pause");
            }
            else
            {
                TrackEvent("application_resume");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            TrackEvent(hasFocus ? "application_focus" : "application_unfocus");
        }

        private void OnDestroy()
        {
            if (currentSession != null)
            {
                TrackEvent("session_end", new Dictionary<string, object>
                {
                    {"session_duration", GetSessionDuration()},
                    {"total_events", eventCounters.Values.Sum()}
                });
                
                // Save any remaining data
                if (enableLocalLogging && eventQueue.Count > 0)
                {
                    SaveAnalyticsDataToFile();
                }
            }
            
        }

        /// <summary>
        /// Get analytics summary for debugging
        /// </summary>
        public AnalyticsSummary GetAnalyticsSummary()
        {
            return new AnalyticsSummary
            {
                SessionId = currentSession?.SessionId ?? "none",
                SessionDuration = GetSessionDuration(),
                TotalEvents = eventCounters.Values.Sum(),
                EventCounts = new Dictionary<string, int>(eventCounters),
                QueuedEvents = eventQueue.Count,
                IsAnalyticsEnabled = enableAnalytics
            };
        }

        /// <summary>
        /// Clear all analytics data (for privacy compliance)
        /// </summary>
        public void ClearAllData()
        {
            eventQueue.Clear();
            eventCounters.Clear();
            sessionParameters.Clear();
            
            if (File.Exists(analyticsFilePath))
            {
                File.Delete(analyticsFilePath);
            }
            
            TrackEvent("analytics_data_cleared");
        }

        /// <summary>
        /// Enable or disable analytics
        /// </summary>
        public void SetAnalyticsEnabled(bool enabled)
        {
            enableAnalytics = enabled;
            
            if (!enabled)
            {
                TrackEvent("analytics_disabled");
            }
            else
            {
                TrackEvent("analytics_enabled");
            }
        }
    }

    #region Data Structures

    [System.Serializable]
    public class AnalyticsSession
    {
        public string SessionId;
        public System.DateTime StartTime;
        public System.DateTime LastUpdateTime;
        public string UserId;
        public string Platform;
        public string UnityVersion;
        public string ApplicationVersion;
        public DeviceInfo DeviceInfo;
        public double SessionDuration;
        public int EventCount;
    }


    [System.Serializable]
    public class AnalyticsBatch
    {
        public string BatchId;
        public string SessionId;
        public System.DateTime Timestamp;
        public List<AnalyticsEvent> Events;
        public Dictionary<string, object> SessionParameters;
    }

    [System.Serializable]
    public class DeviceInfo
    {
        public string DeviceModel;
        public string DeviceType;
        public string OperatingSystem;
        public string ProcessorType;
        public int SystemMemorySize;
        public string GraphicsDeviceName;
        public int GraphicsMemorySize;
        public string ScreenResolution;
        public float ScreenDPI;
    }

    [System.Serializable]
    public class AnalyticsSummary
    {
        public string SessionId;
        public double SessionDuration;
        public int TotalEvents;
        public Dictionary<string, int> EventCounts;
        public int QueuedEvents;
        public bool IsAnalyticsEnabled;
    }

    [System.Serializable]
    public class SerializableList<T>
    {
        public List<T> items;
    }

    #endregion
}
