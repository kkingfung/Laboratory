using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Laboratory.Shared.Types;
using Laboratory.Subsystems.AIDirector;
using Laboratory.Subsystems.Analytics;

namespace Laboratory.Systems.Analytics.Services
{
    /// <summary>
    /// Handles saving and loading of analytics data to/from persistent storage.
    /// Extracted from PlayerAnalyticsTracker for single responsibility.
    /// </summary>
    public class AnalyticsDataPersistence
    {
        // Events
        public System.Action<PlayerProfile> OnPlayerProfileLoaded;
        public System.Action OnPlayerProfileSaved;
        public System.Action<List<AnalyticsSessionData>> OnSessionHistoryLoaded;

        // Persistence configuration
        private string _saveDirectory;
        private bool _anonymizePlayerData;
        private bool _enableDataExport;

        // File paths
        private string ProfileFilePath => Path.Combine(_saveDirectory, "player_profile.json");
        private string SessionHistoryFilePath => Path.Combine(_saveDirectory, "session_history.json");

        public AnalyticsDataPersistence(bool anonymizePlayerData = true, bool enableDataExport = false)
        {
            _anonymizePlayerData = anonymizePlayerData;
            _enableDataExport = enableDataExport;

            // Set save directory
            _saveDirectory = Path.Combine(Application.persistentDataPath, "Analytics");

            // Ensure directory exists
            if (!Directory.Exists(_saveDirectory))
            {
                Directory.CreateDirectory(_saveDirectory);
            }
        }

        /// <summary>
        /// Saves player profile to disk
        /// </summary>
        public void SavePlayerProfile(PlayerProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("[AnalyticsDataPersistence] Cannot save null profile");
                return;
            }

            try
            {
                // Anonymize if needed
                if (_anonymizePlayerData)
                {
                    profile = AnonymizeProfile(profile);
                }

                string json = JsonUtility.ToJson(profile, true);
                File.WriteAllText(ProfileFilePath, json);

                OnPlayerProfileSaved?.Invoke();
                Debug.Log($"[AnalyticsDataPersistence] Player profile saved to: {ProfileFilePath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnalyticsDataPersistence] Failed to save player profile: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads player profile from disk
        /// </summary>
        public PlayerProfile LoadPlayerProfile()
        {
            if (!File.Exists(ProfileFilePath))
            {
                Debug.Log("[AnalyticsDataPersistence] No saved profile found, creating new profile");
                return CreateNewProfile();
            }

            try
            {
                string json = File.ReadAllText(ProfileFilePath);
                PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(json);

                OnPlayerProfileLoaded?.Invoke(profile);
                Debug.Log($"[AnalyticsDataPersistence] Player profile loaded from: {ProfileFilePath}");

                return profile;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnalyticsDataPersistence] Failed to load player profile: {ex.Message}");
                return CreateNewProfile();
            }
        }

        /// <summary>
        /// Saves session history to disk
        /// </summary>
        public void SaveSessionHistory(List<AnalyticsSessionData> sessionHistory)
        {
            if (sessionHistory == null || sessionHistory.Count == 0)
            {
                Debug.LogWarning("[AnalyticsDataPersistence] No session history to save");
                return;
            }

            try
            {
                var wrapper = new SessionHistoryWrapper { sessions = sessionHistory };
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(SessionHistoryFilePath, json);

                Debug.Log($"[AnalyticsDataPersistence] Session history saved: {sessionHistory.Count} sessions");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnalyticsDataPersistence] Failed to save session history: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads session history from disk
        /// </summary>
        public List<AnalyticsSessionData> LoadSessionHistory()
        {
            if (!File.Exists(SessionHistoryFilePath))
            {
                Debug.Log("[AnalyticsDataPersistence] No session history found");
                return new List<AnalyticsSessionData>();
            }

            try
            {
                string json = File.ReadAllText(SessionHistoryFilePath);
                var wrapper = JsonUtility.FromJson<SessionHistoryWrapper>(json);

                OnSessionHistoryLoaded?.Invoke(wrapper.sessions);
                Debug.Log($"[AnalyticsDataPersistence] Session history loaded: {wrapper.sessions.Count} sessions");

                return wrapper.sessions;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnalyticsDataPersistence] Failed to load session history: {ex.Message}");
                return new List<AnalyticsSessionData>();
            }
        }

        /// <summary>
        /// Exports analytics data to JSON file
        /// </summary>
        public void ExportAnalyticsData(PlayerProfile profile, List<AnalyticsSessionData> sessionHistory)
        {
            if (!_enableDataExport)
            {
                Debug.LogWarning("[AnalyticsDataPersistence] Data export is disabled");
                return;
            }

            try
            {
                var exportData = new AnalyticsExportData
                {
                    profile = profile,
                    sessionHistory = sessionHistory,
                    exportTimestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
                };

                string exportPath = Path.Combine(_saveDirectory, $"analytics_export_{exportData.exportTimestamp}.json");
                string json = JsonUtility.ToJson(exportData, true);
                File.WriteAllText(exportPath, json);

                Debug.Log($"[AnalyticsDataPersistence] Analytics data exported to: {exportPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnalyticsDataPersistence] Failed to export analytics data: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes all analytics data
        /// </summary>
        public void DeleteAllData()
        {
            try
            {
                if (File.Exists(ProfileFilePath))
                {
                    File.Delete(ProfileFilePath);
                }

                if (File.Exists(SessionHistoryFilePath))
                {
                    File.Delete(SessionHistoryFilePath);
                }

                Debug.Log("[AnalyticsDataPersistence] All analytics data deleted");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnalyticsDataPersistence] Failed to delete analytics data: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new player profile
        /// </summary>
        private PlayerProfile CreateNewProfile()
        {
            return new PlayerProfile
            {
                playerId = GeneratePlayerId(),
                creationDate = System.DateTime.Now.ToString("yyyy-MM-dd"),
                totalPlayTime = 0f,
                totalSessions = 0
            };
        }

        /// <summary>
        /// Anonymizes player profile data
        /// </summary>
        private PlayerProfile AnonymizeProfile(PlayerProfile profile)
        {
            // Create a copy
            var anonymized = profile;

            // Remove or hash personally identifiable information
            anonymized.playerId = HashPlayerId(profile.playerId);

            return anonymized;
        }

        /// <summary>
        /// Generates a unique player ID
        /// </summary>
        private string GeneratePlayerId()
        {
            return System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Hashes a player ID for anonymization
        /// </summary>
        private string HashPlayerId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return "anonymous";

            // Simple hash for anonymization
            int hash = playerId.GetHashCode();
            return $"player_{Mathf.Abs(hash)}";
        }

        /// <summary>
        /// Gets persistence statistics
        /// </summary>
        public PersistenceStats GetPersistenceStats()
        {
            return new PersistenceStats
            {
                profileExists = File.Exists(ProfileFilePath),
                sessionHistoryExists = File.Exists(SessionHistoryFilePath),
                saveDirectory = _saveDirectory,
                anonymizationEnabled = _anonymizePlayerData,
                exportEnabled = _enableDataExport
            };
        }
    }

    /// <summary>
    /// Wrapper for session history serialization
    /// </summary>
    [System.Serializable]
    public class SessionHistoryWrapper
    {
        public List<AnalyticsSessionData> sessions;
    }

    /// <summary>
    /// Analytics export data container
    /// </summary>
    [System.Serializable]
    public class AnalyticsExportData
    {
        public PlayerProfile profile;
        public List<AnalyticsSessionData> sessionHistory;
        public string exportTimestamp;
    }

    /// <summary>
    /// Persistence statistics summary
    /// </summary>
    public struct PersistenceStats
    {
        public bool profileExists;
        public bool sessionHistoryExists;
        public string saveDirectory;
        public bool anonymizationEnabled;
        public bool exportEnabled;
    }
}
