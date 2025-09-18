using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Subsystems.LiveOps
{
    /// <summary>
    /// Manages live operations including events, daily rewards, and remote configuration
    /// </summary>
    public class LiveOpsManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool enableDebugLogging = false;
        
        private Dictionary<string, object> remoteConfig = new Dictionary<string, object>();
        private List<LiveOpsEvent> activeEvents = new List<LiveOpsEvent>();
        
        private void Awake()
        {
            if (enableDebugLogging)
                Debug.Log("LiveOpsManager: Initialized");
        }
        
        /// <summary>
        /// Gets a remote configuration value with a default fallback
        /// </summary>
        public T GetRemoteConfigValue<T>(string key, T defaultValue)
        {
            if (remoteConfig.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)value;
                }
                catch (InvalidCastException)
                {
                    Debug.LogWarning($"LiveOpsManager: Could not cast remote config value for key '{key}' to type {typeof(T)}");
                    return defaultValue;
                }
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Gets a string remote configuration value
        /// </summary>
        public string GetRemoteConfigValue(string key, string defaultValue)
        {
            return GetRemoteConfigValue<string>(key, defaultValue);
        }
        
        /// <summary>
        /// Gets all currently active events
        /// </summary>
        public List<LiveOpsEvent> GetActiveEvents()
        {
            var currentTime = DateTime.Now;
            return activeEvents.FindAll(evt => evt.IsActive(currentTime));
        }
        
        /// <summary>
        /// Gets a specific event by ID
        /// </summary>
        public LiveOpsEvent GetEvent(string eventId)
        {
            return activeEvents.Find(evt => evt.Id == eventId);
        }
        
        /// <summary>
        /// Claims the daily reward if available
        /// </summary>
        public bool ClaimDailyReward()
        {
            const string lastClaimKey = "LiveOps_LastDailyReward";
            var lastClaimDate = PlayerPrefs.GetString(lastClaimKey, "");
            
            if (string.IsNullOrEmpty(lastClaimDate))
            {
                // First time claiming
                PlayerPrefs.SetString(lastClaimKey, DateTime.Now.ToBinary().ToString());
                PlayerPrefs.Save();
                
                if (enableDebugLogging)
                    Debug.Log("LiveOpsManager: Daily reward claimed for the first time");
                
                return true;
            }
            
            if (DateTime.TryParse(lastClaimDate, out var lastClaim))
            {
                var timeSinceLastClaim = DateTime.Now - lastClaim;
                if (timeSinceLastClaim.TotalHours >= 24)
                {
                    PlayerPrefs.SetString(lastClaimKey, DateTime.Now.ToBinary().ToString());
                    PlayerPrefs.Save();
                    
                    if (enableDebugLogging)
                        Debug.Log("LiveOpsManager: Daily reward claimed");
                    
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Tracks an analytics event
        /// </summary>
        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (enableDebugLogging)
            {
                var paramStr = parameters != null ? string.Join(", ", parameters) : "none";
                Debug.Log($"LiveOpsManager: Tracked event '{eventName}' with parameters: {paramStr}");
            }
            
            // In a real implementation, this would send to analytics service
        }
        
        /// <summary>
        /// Sets remote configuration values (typically called during initialization)
        /// </summary>
        public void SetRemoteConfigValues(Dictionary<string, object> config)
        {
            remoteConfig = config ?? new Dictionary<string, object>();
            
            if (enableDebugLogging)
                Debug.Log($"LiveOpsManager: Remote config updated with {remoteConfig.Count} values");
        }
        
        /// <summary>
        /// Sets the active events list
        /// </summary>
        public void SetActiveEvents(List<LiveOpsEvent> events)
        {
            activeEvents = events ?? new List<LiveOpsEvent>();
            
            if (enableDebugLogging)
                Debug.Log($"LiveOpsManager: Active events updated with {activeEvents.Count} events");
        }
    }
    
    /// <summary>
    /// Represents a live operations event
    /// </summary>
    [System.Serializable]
    public class LiveOpsEvent
    {
        public string Id;
        public string Name;
        public string Description;
        public DateTime StartTime;
        public DateTime EndTime;
        
        /// <summary>
        /// Checks if the event is currently active
        /// </summary>
        public bool IsActive(DateTime currentTime)
        {
            return currentTime >= StartTime && currentTime <= EndTime;
        }
        
        /// <summary>
        /// Gets the time remaining for this event
        /// </summary>
        public TimeSpan GetTimeRemaining(DateTime currentTime)
        {
            if (!IsActive(currentTime))
                return TimeSpan.Zero;
            
            return EndTime - currentTime;
        }
    }
    
    // Event classes for the event system
    
    /// <summary>
    /// Event fired when LiveOps system is initialized
    /// </summary>
    public class LiveOpsInitializedEvent
    {
        public DateTime InitializationTime { get; private set; }
        
        public LiveOpsInitializedEvent()
        {
            InitializationTime = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Event fired when daily reward becomes available
    /// </summary>
    public class DailyRewardAvailableEvent
    {
        public int DaysSinceLastClaim { get; private set; }
        
        public DailyRewardAvailableEvent(int daysSinceLastClaim)
        {
            DaysSinceLastClaim = daysSinceLastClaim;
        }
    }
    
    /// <summary>
    /// Event fired when daily reward is claimed
    /// </summary>
    public class DailyRewardClaimedEvent
    {
        public int ConsecutiveDays { get; private set; }
        public DateTime ClaimTime { get; private set; }
        
        public DailyRewardClaimedEvent(int consecutiveDays)
        {
            ConsecutiveDays = consecutiveDays;
            ClaimTime = DateTime.Now;
        }
    }
}