using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Multiplayer
{
    /// <summary>
    /// Server-side anti-cheat system for multiplayer security.
    /// Validates player actions, detects anomalies, and enforces rules.
    /// Complements client-side cheat detection with authoritative validation.
    /// </summary>
    public class ServerSideAntiCheat : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string reportEndpoint = "/anticheat/report";

        [Header("Validation Settings")]
        [SerializeField] private bool validateMovement = true;
        [SerializeField] private bool validateActions = true;
        [SerializeField] private bool validateInventory = true;
        [SerializeField] private bool validateDamage = true;

        [Header("Movement Validation")]
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float maxAcceleration = 20f;
        [SerializeField] private float maxTeleportDistance = 5f;

        [Header("Action Validation")]
        [SerializeField] private float minActionInterval = 0.1f; // 100ms between actions
        [SerializeField] private int maxActionsPerSecond = 20;

        [Header("Thresholds")]
        [SerializeField] private int violationKickThreshold = 10;
        [SerializeField] private int violationBanThreshold = 50;
        [SerializeField] private float violationDecayRate = 1f; // Per second

        #endregion

        #region Private Fields

        private static ServerSideAntiCheat _instance;

        // Player tracking
        private readonly Dictionary<string, PlayerValidationState> _playerStates = new Dictionary<string, PlayerValidationState>();

        // Violation tracking
        private readonly Dictionary<string, List<CheatViolation>> _violations = new Dictionary<string, List<CheatViolation>>();

        // Statistics
        private int _totalValidations = 0;
        private int _totalViolations = 0;
        private int _playersKicked = 0;
        private int _playersBanned = 0;

        // Events
        public event Action<string, CheatViolation> OnViolationDetected;
        public event Action<string> OnPlayerKicked;
        public event Action<string> OnPlayerBanned;

        #endregion

        #region Properties

        public static ServerSideAntiCheat Instance => _instance;
        public int TrackedPlayerCount => _playerStates.Count;
        public int TotalViolations => _totalViolations;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[ServerSideAntiCheat] Initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Decay violations over time
            DecayViolations();
        }

        #endregion

        #region Player Registration

        /// <summary>
        /// Register a player for anti-cheat validation.
        /// </summary>
        public void RegisterPlayer(string playerId, Vector3 initialPosition)
        {
            if (_playerStates.ContainsKey(playerId))
            {
                Debug.LogWarning($"[ServerSideAntiCheat] Player already registered: {playerId}");
                return;
            }

            _playerStates[playerId] = new PlayerValidationState
            {
                playerId = playerId,
                lastPosition = initialPosition,
                lastActionTime = Time.time,
                actionsThisSecond = 0,
                violationScore = 0
            };

            _violations[playerId] = new List<CheatViolation>();

            Debug.Log($"[ServerSideAntiCheat] Player registered: {playerId}");
        }

        /// <summary>
        /// Unregister a player.
        /// </summary>
        public void UnregisterPlayer(string playerId)
        {
            _playerStates.Remove(playerId);
            _violations.Remove(playerId);

            Debug.Log($"[ServerSideAntiCheat] Player unregistered: {playerId}");
        }

        #endregion

        #region Movement Validation

        /// <summary>
        /// Validate player movement.
        /// </summary>
        public bool ValidateMovement(string playerId, Vector3 newPosition, float deltaTime)
        {
            if (!validateMovement) return true;

            _totalValidations++;

            if (!_playerStates.TryGetValue(playerId, out var state))
            {
                Debug.LogWarning($"[ServerSideAntiCheat] Player not registered: {playerId}");
                return false;
            }

            Vector3 delta = newPosition - state.lastPosition;
            float distance = delta.magnitude;
            float speed = distance / deltaTime;

            // Check teleportation
            if (distance > maxTeleportDistance && deltaTime < 0.1f)
            {
                RecordViolation(playerId, ViolationType.Teleportation, $"Distance: {distance:F2}m in {deltaTime:F3}s");
                return false;
            }

            // Check speed
            if (speed > maxSpeed * 1.5f) // 50% tolerance
            {
                RecordViolation(playerId, ViolationType.SpeedHack, $"Speed: {speed:F2} (max: {maxSpeed})");
                return false;
            }

            // Check acceleration
            Vector3 velocity = delta / deltaTime;
            Vector3 acceleration = (velocity - state.lastVelocity) / deltaTime;

            if (acceleration.magnitude > maxAcceleration * 2f) // 100% tolerance
            {
                RecordViolation(playerId, ViolationType.SpeedHack, $"Acceleration: {acceleration.magnitude:F2}");
                return false;
            }

            // Update state
            state.lastPosition = newPosition;
            state.lastVelocity = velocity;
            state.lastMoveTime = Time.time;

            return true;
        }

        #endregion

        #region Action Validation

        /// <summary>
        /// Validate player action.
        /// </summary>
        public bool ValidateAction(string playerId, string actionType, Dictionary<string, object> parameters = null)
        {
            if (!validateActions) return true;

            _totalValidations++;

            if (!_playerStates.TryGetValue(playerId, out var state))
            {
                Debug.LogWarning($"[ServerSideAntiCheat] Player not registered: {playerId}");
                return false;
            }

            float currentTime = Time.time;

            // Check action interval
            float timeSinceLastAction = currentTime - state.lastActionTime;

            if (timeSinceLastAction < minActionInterval)
            {
                RecordViolation(playerId, ViolationType.ActionSpam, $"Interval: {timeSinceLastAction:F3}s");
                return false;
            }

            // Check actions per second
            if (currentTime - state.actionSecondStart >= 1f)
            {
                state.actionSecondStart = currentTime;
                state.actionsThisSecond = 0;
            }

            state.actionsThisSecond++;

            if (state.actionsThisSecond > maxActionsPerSecond)
            {
                RecordViolation(playerId, ViolationType.ActionSpam, $"Actions/sec: {state.actionsThisSecond}");
                return false;
            }

            // Update state
            state.lastActionTime = currentTime;

            return true;
        }

        #endregion

        #region Inventory Validation

        /// <summary>
        /// Validate inventory modification.
        /// </summary>
        public bool ValidateInventoryChange(string playerId, string itemId, int quantity)
        {
            if (!validateInventory) return true;

            _totalValidations++;

            // Check for impossible item quantities
            if (quantity < 0)
            {
                RecordViolation(playerId, ViolationType.InventoryManipulation, $"Negative quantity: {quantity}");
                return false;
            }

            // Check for excessive quantities (implement based on game rules)
            const int maxStackSize = 999;
            if (quantity > maxStackSize)
            {
                RecordViolation(playerId, ViolationType.InventoryManipulation, $"Excessive quantity: {quantity}");
                return false;
            }

            return true;
        }

        #endregion

        #region Damage Validation

        /// <summary>
        /// Validate damage dealt.
        /// </summary>
        public bool ValidateDamage(string attackerId, string victimId, float damage, string weaponId)
        {
            if (!validateDamage) return true;

            _totalValidations++;

            // Check for impossible damage values
            if (damage < 0)
            {
                RecordViolation(attackerId, ViolationType.DamageManipulation, $"Negative damage: {damage}");
                return false;
            }

            // Check for excessive damage (implement based on game balance)
            const float maxPossibleDamage = 1000f;
            if (damage > maxPossibleDamage)
            {
                RecordViolation(attackerId, ViolationType.DamageManipulation, $"Excessive damage: {damage}");
                return false;
            }

            // Validate weapon ownership (implement based on inventory system)
            // ...

            return true;
        }

        #endregion

        #region Violation Management

        private void RecordViolation(string playerId, ViolationType type, string details)
        {
            if (!_playerStates.TryGetValue(playerId, out var state))
                return;

            var violation = new CheatViolation
            {
                playerId = playerId,
                type = type,
                details = details,
                timestamp = DateTime.UtcNow,
                severity = GetViolationSeverity(type)
            };

            _violations[playerId].Add(violation);
            _totalViolations++;

            state.violationScore += violation.severity;

            OnViolationDetected?.Invoke(playerId, violation);

            Debug.LogWarning($"[ServerSideAntiCheat] Violation: {playerId} - {type} - {details}");

            // Report to backend
            StartCoroutine(ReportViolation(violation));

            // Check for enforcement actions
            CheckEnforcementActions(playerId, state);
        }

        private int GetViolationSeverity(ViolationType type)
        {
            return type switch
            {
                ViolationType.Teleportation => 5,
                ViolationType.SpeedHack => 3,
                ViolationType.ActionSpam => 2,
                ViolationType.InventoryManipulation => 4,
                ViolationType.DamageManipulation => 5,
                ViolationType.UnauthorizedAction => 3,
                _ => 1
            };
        }

        private void DecayViolations()
        {
            float decayAmount = violationDecayRate * Time.deltaTime;

            foreach (var state in _playerStates.Values)
            {
                if (state.violationScore > 0)
                {
                    state.violationScore = Mathf.Max(0, state.violationScore - decayAmount);
                }
            }
        }

        #endregion

        #region Enforcement

        private void CheckEnforcementActions(string playerId, PlayerValidationState state)
        {
            if (state.violationScore >= violationBanThreshold)
            {
                BanPlayer(playerId);
            }
            else if (state.violationScore >= violationKickThreshold)
            {
                KickPlayer(playerId);
            }
        }

        /// <summary>
        /// Kick a player from the server.
        /// </summary>
        public void KickPlayer(string playerId)
        {
            if (!_playerStates.ContainsKey(playerId))
                return;

            _playersKicked++;

            OnPlayerKicked?.Invoke(playerId);

            Debug.LogWarning($"[ServerSideAntiCheat] Player kicked: {playerId}");

            // Implement actual kick logic (depends on networking solution)
            UnregisterPlayer(playerId);
        }

        /// <summary>
        /// Ban a player permanently.
        /// </summary>
        public void BanPlayer(string playerId)
        {
            if (!_playerStates.ContainsKey(playerId))
                return;

            _playersBanned++;

            OnPlayerBanned?.Invoke(playerId);

            Debug.LogWarning($"[ServerSideAntiCheat] Player banned: {playerId}");

            // Implement actual ban logic (backend database)
            UnregisterPlayer(playerId);
        }

        #endregion

        #region Backend Reporting

        private IEnumerator ReportViolation(CheatViolation violation)
        {
            var requestData = new ViolationReportRequest
            {
                violation = violation
            };

            string json = JsonUtility.ToJson(requestData);
            string url = backendUrl + reportEndpoint;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 5;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[ServerSideAntiCheat] Violation report failed: {request.error}");
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get anti-cheat statistics.
        /// </summary>
        public AntiCheatStats GetStats()
        {
            return new AntiCheatStats
            {
                trackedPlayers = _playerStates.Count,
                totalValidations = _totalValidations,
                totalViolations = _totalViolations,
                playersKicked = _playersKicked,
                playersBanned = _playersBanned
            };
        }

        /// <summary>
        /// Get violations for a player.
        /// </summary>
        public CheatViolation[] GetPlayerViolations(string playerId)
        {
            if (_violations.TryGetValue(playerId, out var violations))
            {
                return violations.ToArray();
            }

            return new CheatViolation[0];
        }

        /// <summary>
        /// Get player violation score.
        /// </summary>
        public float GetPlayerViolationScore(string playerId)
        {
            if (_playerStates.TryGetValue(playerId, out var state))
            {
                return state.violationScore;
            }

            return 0f;
        }

        /// <summary>
        /// Clear player violations (admin action).
        /// </summary>
        public void ClearPlayerViolations(string playerId)
        {
            if (_playerStates.TryGetValue(playerId, out var state))
            {
                state.violationScore = 0;
                _violations[playerId].Clear();

                Debug.Log($"[ServerSideAntiCheat] Violations cleared for: {playerId}");
            }
        }

        #endregion

        #region Context Menu

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Anti-Cheat Statistics ===\n" +
                      $"Tracked Players: {stats.trackedPlayers}\n" +
                      $"Total Validations: {stats.totalValidations}\n" +
                      $"Total Violations: {stats.totalViolations}\n" +
                      $"Players Kicked: {stats.playersKicked}\n" +
                      $"Players Banned: {stats.playersBanned}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Player validation state.
    /// </summary>
    [Serializable]
    public class PlayerValidationState
    {
        public string playerId;
        public Vector3 lastPosition;
        public Vector3 lastVelocity;
        public float lastMoveTime;
        public float lastActionTime;
        public float actionSecondStart;
        public int actionsThisSecond;
        public float violationScore;
    }

    /// <summary>
    /// Cheat violation record.
    /// </summary>
    [Serializable]
    public class CheatViolation
    {
        public string playerId;
        public ViolationType type;
        public string details;
        public DateTime timestamp;
        public int severity;
    }

    /// <summary>
    /// Anti-cheat statistics.
    /// </summary>
    [Serializable]
    public struct AntiCheatStats
    {
        public int trackedPlayers;
        public int totalValidations;
        public int totalViolations;
        public int playersKicked;
        public int playersBanned;
    }

    // Request structures
    [Serializable] class ViolationReportRequest { public CheatViolation violation; }

    /// <summary>
    /// Violation types.
    /// </summary>
    public enum ViolationType
    {
        Teleportation,
        SpeedHack,
        ActionSpam,
        InventoryManipulation,
        DamageManipulation,
        UnauthorizedAction
    }

    #endregion
}
