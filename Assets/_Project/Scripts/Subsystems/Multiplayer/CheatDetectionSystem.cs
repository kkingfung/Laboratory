using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace Laboratory.Multiplayer
{
    /// <summary>
    /// Cheat detection system for multiplayer gameplay.
    /// Detects common cheating patterns: speed hacks, teleportation, impossible stats, modified data.
    /// Server-authoritative with client-side validation for performance.
    /// </summary>
    public class CheatDetectionSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Detection Settings")]
        [SerializeField] private bool enableCheatDetection = true;
        [SerializeField] private bool logDetections = true;
        [SerializeField] private float detectionCheckInterval = 1f;

        [Header("Speed Detection")]
        [SerializeField] private bool detectSpeedHacks = true;
        [SerializeField] private float maxAllowedSpeed = 15f;
        [SerializeField] private float speedViolationThreshold = 2f; // 2x max speed
        [SerializeField] private int speedViolationsBeforeBan = 5;

        [Header("Teleport Detection")]
        [SerializeField] private bool detectTeleportation = true;
        [SerializeField] private float maxDistancePerFrame = 50f;
        [SerializeField] private int teleportViolationsBeforeBan = 3;

        [Header("Stats Validation")]
        [SerializeField] private bool validateStats = true;
        [SerializeField] private float maxStatValue = 1000f;
        [SerializeField] private int statViolationsBeforeBan = 2;

        [Header("Input Validation")]
        [SerializeField] private bool detectImpossibleInputs = true;
        [SerializeField] private int maxActionsPerSecond = 20;
        [SerializeField] private int inputViolationsBeforeBan = 10;

        [Header("Actions")]
        [SerializeField] private CheatResponseAction responseAction = CheatResponseAction.LogAndWarn;
        [SerializeField] private bool autoKickCheaters = false;
        [SerializeField] private bool autoBanCheaters = false;

        #endregion

        #region Private Fields

        private static CheatDetectionSystem _instance;

        // Player tracking
        private readonly Dictionary<ulong, PlayerCheatData> _playerData = new Dictionary<ulong, PlayerCheatData>();
        private readonly Dictionary<ulong, List<CheatViolation>> _violations = new Dictionary<ulong, List<CheatViolation>>();
        private readonly HashSet<ulong> _bannedPlayers = new HashSet<ulong>();
        private readonly HashSet<ulong> _whitelistedPlayers = new HashSet<ulong>();

        // Detection state
        private float _lastCheckTime;

        // Statistics
        private int _totalViolationsDetected;
        private int _totalPlayersBanned;
        private int _totalPlayersKicked;

        // Events
        public event Action<ulong, CheatViolation> OnViolationDetected;
        public event Action<ulong, string> OnPlayerBanned;
        public event Action<ulong, string> OnPlayerKicked;

        #endregion

        #region Properties

        public static CheatDetectionSystem Instance => _instance;
        public bool IsEnabled => enableCheatDetection;
        public int TotalViolations => _totalViolationsDetected;
        public int BannedPlayerCount => _bannedPlayers.Count;

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
            if (!enableCheatDetection) return;

            if (Time.time - _lastCheckTime >= detectionCheckInterval)
            {
                _lastCheckTime = Time.time;
                PerformDetectionChecks();
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[CheatDetectionSystem] Initializing...");
            LoadBanList();
            Debug.Log("[CheatDetectionSystem] Initialized");
        }

        #endregion

        #region Player Tracking

        /// <summary>
        /// Register a player for cheat detection tracking.
        /// </summary>
        public void RegisterPlayer(ulong playerId, Vector3 initialPosition)
        {
            if (_playerData.ContainsKey(playerId))
            {
                Debug.LogWarning($"[CheatDetectionSystem] Player {playerId} already registered");
                return;
            }

            _playerData[playerId] = new PlayerCheatData
            {
                playerId = playerId,
                lastPosition = initialPosition,
                lastUpdateTime = Time.time,
                registrationTime = Time.time
            };

            _violations[playerId] = new List<CheatViolation>();

            if (logDetections)
            {
                Debug.Log($"[CheatDetectionSystem] Registered player: {playerId}");
            }
        }

        /// <summary>
        /// Unregister a player (on disconnect).
        /// </summary>
        public void UnregisterPlayer(ulong playerId)
        {
            _playerData.Remove(playerId);
            // Keep violations for history
        }

        /// <summary>
        /// Update player position for movement validation.
        /// </summary>
        public void UpdatePlayerPosition(ulong playerId, Vector3 newPosition)
        {
            if (!enableCheatDetection) return;
            if (!_playerData.TryGetValue(playerId, out var data)) return;

            float deltaTime = Time.time - data.lastUpdateTime;
            if (deltaTime <= 0) return;

            // Calculate movement
            float distance = Vector3.Distance(data.lastPosition, newPosition);
            float speed = distance / deltaTime;

            // Update data
            data.lastPosition = newPosition;
            data.lastUpdateTime = Time.time;
            data.movementHistory.Add(new MovementSnapshot
            {
                position = newPosition,
                timestamp = Time.time,
                speed = speed,
                distance = distance
            });

            // Trim history
            if (data.movementHistory.Count > 100)
            {
                data.movementHistory.RemoveAt(0);
            }

            // Check for speed hacks
            if (detectSpeedHacks && speed > maxAllowedSpeed * speedViolationThreshold)
            {
                ReportViolation(playerId, CheatType.SpeedHack, $"Speed: {speed:F2} (max: {maxAllowedSpeed})");
            }

            // Check for teleportation
            if (detectTeleportation && distance > maxDistancePerFrame)
            {
                ReportViolation(playerId, CheatType.Teleportation, $"Distance: {distance:F2} in {deltaTime:F2}s");
            }
        }

        /// <summary>
        /// Update player stats for validation.
        /// </summary>
        public void UpdatePlayerStats(ulong playerId, Dictionary<string, float> stats)
        {
            if (!enableCheatDetection) return;
            if (!validateStats) return;
            if (!_playerData.TryGetValue(playerId, out var data)) return;

            foreach (var kvp in stats)
            {
                // Check for impossible stat values
                if (kvp.Value > maxStatValue)
                {
                    ReportViolation(playerId, CheatType.InvalidStats, $"{kvp.Key}: {kvp.Value} (max: {maxStatValue})");
                }

                // Check for stat modifications (rapid changes)
                if (data.lastStats.TryGetValue(kvp.Key, out float lastValue))
                {
                    float change = kvp.Value - lastValue;
                    float maxChange = GetMaxStatChange(kvp.Key);

                    if (Math.Abs(change) > maxChange)
                    {
                        ReportViolation(playerId, CheatType.StatModification, $"{kvp.Key} changed by {change} (max: {maxChange})");
                    }
                }

                data.lastStats[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Record a player action for input validation.
        /// </summary>
        public void RecordPlayerAction(ulong playerId, string actionType)
        {
            if (!enableCheatDetection) return;
            if (!detectImpossibleInputs) return;
            if (!_playerData.TryGetValue(playerId, out var data)) return;

            data.actionHistory.Add(new PlayerAction
            {
                actionType = actionType,
                timestamp = Time.time
            });

            // Trim old actions (keep last 5 seconds)
            float cutoffTime = Time.time - 5f;
            data.actionHistory.RemoveAll(a => a.timestamp < cutoffTime);

            // Check for impossible input rates
            int recentActions = data.actionHistory.Count;
            if (recentActions > maxActionsPerSecond * 5) // 5 seconds window
            {
                ReportViolation(playerId, CheatType.InputHacking, $"{recentActions} actions in 5s (max: {maxActionsPerSecond * 5})");
            }
        }

        #endregion

        #region Detection Checks

        private void PerformDetectionChecks()
        {
            foreach (var kvp in _playerData)
            {
                ulong playerId = kvp.Key;
                var data = kvp.Value;

                // Check for inactivity anomalies
                float inactiveTime = Time.time - data.lastUpdateTime;
                if (inactiveTime > 60f && data.actionHistory.Count > 0)
                {
                    // Player inactive but actions recorded - possible bot
                    ReportViolation(playerId, CheatType.Botting, $"Inactive for {inactiveTime:F1}s but actions recorded");
                }

                // Check movement patterns for bot-like behavior
                if (data.movementHistory.Count > 10)
                {
                    DetectBotMovement(playerId, data);
                }
            }
        }

        private void DetectBotMovement(ulong playerId, PlayerCheatData data)
        {
            // Check for perfectly linear movement (bot characteristic)
            if (data.movementHistory.Count < 5) return;

            var recent = data.movementHistory.Skip(data.movementHistory.Count - 5).ToList();

            // Calculate if movement is suspiciously linear
            bool perfectlyLinear = true;
            for (int i = 1; i < recent.Count - 1; i++)
            {
                Vector3 dir1 = (recent[i].position - recent[i - 1].position).normalized;
                Vector3 dir2 = (recent[i + 1].position - recent[i].position).normalized;

                float dot = Vector3.Dot(dir1, dir2);
                if (dot < 0.99f) // Allow 1% deviation
                {
                    perfectlyLinear = false;
                    break;
                }
            }

            if (perfectlyLinear && recent.All(m => Math.Abs(m.speed - recent[0].speed) < 0.1f))
            {
                data.linearMovementCount++;
                if (data.linearMovementCount > 20)
                {
                    ReportViolation(playerId, CheatType.Botting, "Perfectly linear movement detected");
                    data.linearMovementCount = 0; // Reset to avoid spam
                }
            }
            else
            {
                data.linearMovementCount = 0;
            }
        }

        private float GetMaxStatChange(string statName)
        {
            // Define max allowed stat changes per check interval
            // In production, load from configuration
            return statName switch
            {
                "Health" => 100f,
                "Stamina" => 50f,
                "Mana" => 50f,
                "Experience" => 1000f,
                _ => 10f
            };
        }

        #endregion

        #region Violation Handling

        private void ReportViolation(ulong playerId, CheatType cheatType, string details)
        {
            // Check whitelist
            if (_whitelistedPlayers.Contains(playerId))
                return;

            var violation = new CheatViolation
            {
                playerId = playerId,
                cheatType = cheatType,
                timestamp = DateTime.UtcNow,
                details = details,
                severity = GetViolationSeverity(cheatType)
            };

            if (!_violations.ContainsKey(playerId))
            {
                _violations[playerId] = new List<CheatViolation>();
            }

            _violations[playerId].Add(violation);
            _totalViolationsDetected++;

            OnViolationDetected?.Invoke(playerId, violation);

            if (logDetections)
            {
                Debug.LogWarning($"[CheatDetectionSystem] VIOLATION: Player {playerId} - {cheatType} - {details}");
            }

            // Take action based on severity and count
            HandleViolation(playerId, cheatType, violation);
        }

        private void HandleViolation(ulong playerId, CheatType cheatType, CheatViolation violation)
        {
            var violations = _violations[playerId];
            int typeCount = violations.Count(v => v.cheatType == cheatType);

            // Check ban thresholds
            bool shouldBan = cheatType switch
            {
                CheatType.SpeedHack => typeCount >= speedViolationsBeforeBan,
                CheatType.Teleportation => typeCount >= teleportViolationsBeforeBan,
                CheatType.InvalidStats => typeCount >= statViolationsBeforeBan,
                CheatType.StatModification => typeCount >= statViolationsBeforeBan,
                CheatType.InputHacking => typeCount >= inputViolationsBeforeBan,
                CheatType.Botting => typeCount >= 3,
                CheatType.DataTampering => typeCount >= 1, // Instant ban
                _ => false
            };

            if (shouldBan)
            {
                if (responseAction == CheatResponseAction.Ban || autoBanCheaters)
                {
                    BanPlayer(playerId, $"{cheatType} - {typeCount} violations");
                }
                else if (responseAction == CheatResponseAction.Kick || autoKickCheaters)
                {
                    KickPlayer(playerId, $"{cheatType} violations");
                }
                else if (responseAction == CheatResponseAction.LogAndWarn)
                {
                    Debug.LogError($"[CheatDetectionSystem] Player {playerId} should be banned for {cheatType} but auto-ban disabled");
                }
            }
        }

        private ViolationSeverity GetViolationSeverity(CheatType cheatType)
        {
            return cheatType switch
            {
                CheatType.DataTampering => ViolationSeverity.Critical,
                CheatType.StatModification => ViolationSeverity.High,
                CheatType.Teleportation => ViolationSeverity.High,
                CheatType.SpeedHack => ViolationSeverity.Medium,
                CheatType.InvalidStats => ViolationSeverity.Medium,
                CheatType.InputHacking => ViolationSeverity.Low,
                CheatType.Botting => ViolationSeverity.Medium,
                _ => ViolationSeverity.Low
            };
        }

        #endregion

        #region Player Actions

        /// <summary>
        /// Kick a player from the server.
        /// </summary>
        public void KickPlayer(ulong playerId, string reason)
        {
            OnPlayerKicked?.Invoke(playerId, reason);
            _totalPlayersKicked++;

            if (logDetections)
            {
                Debug.LogWarning($"[CheatDetectionSystem] KICKED player {playerId}: {reason}");
            }

            // In production, actually disconnect the player via networking
        }

        /// <summary>
        /// Ban a player permanently.
        /// </summary>
        public void BanPlayer(ulong playerId, string reason)
        {
            _bannedPlayers.Add(playerId);
            OnPlayerBanned?.Invoke(playerId, reason);
            _totalPlayersBanned++;

            if (logDetections)
            {
                Debug.LogError($"[CheatDetectionSystem] BANNED player {playerId}: {reason}");
            }

            SaveBanList();

            // In production, disconnect and prevent reconnection
            KickPlayer(playerId, $"Banned: {reason}");
        }

        /// <summary>
        /// Unban a player.
        /// </summary>
        public void UnbanPlayer(ulong playerId)
        {
            if (_bannedPlayers.Remove(playerId))
            {
                SaveBanList();
                Debug.Log($"[CheatDetectionSystem] Unbanned player: {playerId}");
            }
        }

        /// <summary>
        /// Add a player to whitelist (disable cheat detection).
        /// </summary>
        public void WhitelistPlayer(ulong playerId)
        {
            _whitelistedPlayers.Add(playerId);
            Debug.Log($"[CheatDetectionSystem] Whitelisted player: {playerId}");
        }

        /// <summary>
        /// Check if a player is banned.
        /// </summary>
        public bool IsPlayerBanned(ulong playerId)
        {
            return _bannedPlayers.Contains(playerId);
        }

        #endregion

        #region Persistence

        private void SaveBanList()
        {
            try
            {
                var banData = new BanListData
                {
                    bannedPlayers = _bannedPlayers.ToList(),
                    saveTime = DateTime.UtcNow
                };

                string json = JsonUtility.ToJson(banData, true);
                string path = System.IO.Path.Combine(Application.persistentDataPath, "banlist.json");
                System.IO.File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CheatDetectionSystem] Failed to save ban list: {ex.Message}");
            }
        }

        private void LoadBanList()
        {
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, "banlist.json");
                if (System.IO.File.Exists(path))
                {
                    string json = System.IO.File.ReadAllText(path);
                    var banData = JsonUtility.FromJson<BanListData>(json);

                    _bannedPlayers.Clear();
                    foreach (var id in banData.bannedPlayers)
                    {
                        _bannedPlayers.Add(id);
                    }

                    Debug.Log($"[CheatDetectionSystem] Loaded {_bannedPlayers.Count} banned players");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CheatDetectionSystem] Failed to load ban list: {ex.Message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get violations for a specific player.
        /// </summary>
        public List<CheatViolation> GetPlayerViolations(ulong playerId)
        {
            if (_violations.TryGetValue(playerId, out var violations))
            {
                return new List<CheatViolation>(violations);
            }
            return new List<CheatViolation>();
        }

        /// <summary>
        /// Get statistics for the cheat detection system.
        /// </summary>
        public CheatDetectionStats GetStats()
        {
            return new CheatDetectionStats
            {
                totalViolations = _totalViolationsDetected,
                totalPlayersBanned = _totalPlayersBanned,
                totalPlayersKicked = _totalPlayersKicked,
                trackedPlayers = _playerData.Count,
                bannedPlayers = _bannedPlayers.Count,
                isEnabled = enableCheatDetection
            };
        }

        /// <summary>
        /// Clear all violations for a player.
        /// </summary>
        public void ClearPlayerViolations(ulong playerId)
        {
            if (_violations.ContainsKey(playerId))
            {
                _violations[playerId].Clear();
                Debug.Log($"[CheatDetectionSystem] Cleared violations for player: {playerId}");
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Cheat Detection Statistics ===\n" +
                      $"Total Violations: {stats.totalViolations}\n" +
                      $"Players Banned: {stats.totalPlayersBanned}\n" +
                      $"Players Kicked: {stats.totalPlayersKicked}\n" +
                      $"Tracked Players: {stats.trackedPlayers}\n" +
                      $"Banned Players: {stats.bannedPlayers}\n" +
                      $"Enabled: {stats.isEnabled}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Cheat detection data for a player.
    /// </summary>
    [Serializable]
    public class PlayerCheatData
    {
        public ulong playerId;
        public Vector3 lastPosition;
        public float lastUpdateTime;
        public float registrationTime;
        public Dictionary<string, float> lastStats = new Dictionary<string, float>();
        public List<MovementSnapshot> movementHistory = new List<MovementSnapshot>();
        public List<PlayerAction> actionHistory = new List<PlayerAction>();
        public int linearMovementCount;
    }

    /// <summary>
    /// A snapshot of player movement.
    /// </summary>
    [Serializable]
    public struct MovementSnapshot
    {
        public Vector3 position;
        public float timestamp;
        public float speed;
        public float distance;
    }

    /// <summary>
    /// A player action record.
    /// </summary>
    [Serializable]
    public struct PlayerAction
    {
        public string actionType;
        public float timestamp;
    }

    /// <summary>
    /// A cheat violation record.
    /// </summary>
    [Serializable]
    public class CheatViolation
    {
        public ulong playerId;
        public CheatType cheatType;
        public DateTime timestamp;
        public string details;
        public ViolationSeverity severity;
    }

    /// <summary>
    /// Ban list persistence data.
    /// </summary>
    [Serializable]
    public class BanListData
    {
        public List<ulong> bannedPlayers = new List<ulong>();
        public DateTime saveTime;
    }

    /// <summary>
    /// Statistics for cheat detection.
    /// </summary>
    [Serializable]
    public struct CheatDetectionStats
    {
        public int totalViolations;
        public int totalPlayersBanned;
        public int totalPlayersKicked;
        public int trackedPlayers;
        public int bannedPlayers;
        public bool isEnabled;
    }

    /// <summary>
    /// Types of cheats that can be detected.
    /// </summary>
    public enum CheatType
    {
        SpeedHack,
        Teleportation,
        InvalidStats,
        StatModification,
        InputHacking,
        Botting,
        DataTampering
    }

    /// <summary>
    /// Severity levels for violations.
    /// </summary>
    public enum ViolationSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Actions to take when cheat is detected.
    /// </summary>
    public enum CheatResponseAction
    {
        LogOnly,
        LogAndWarn,
        Kick,
        Ban
    }

    #endregion
}
