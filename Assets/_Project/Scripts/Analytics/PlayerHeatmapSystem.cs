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
    /// Player heatmap system for spatial analytics.
    /// Tracks player positions, deaths, interactions, and combat zones.
    /// Visualizes data for level design optimization.
    /// </summary>
    public class PlayerHeatmapSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string heatmapEndpoint = "/analytics/heatmap";

        [Header("Tracking Settings")]
        [SerializeField] private bool enableTracking = true;
        [SerializeField] private float trackingInterval = 1f; // Track position every second
        [SerializeField] private float gridCellSize = 1f; // 1 meter cells
        [SerializeField] private int maxBufferSize = 1000;

        [Header("Event Types")]
        [SerializeField] private bool trackPositions = true;
        [SerializeField] private bool trackDeaths = true;
        [SerializeField] private bool trackKills = true;
        [SerializeField] private bool trackInteractions = true;
        [SerializeField] private bool trackCombat = true;

        [Header("Upload Settings")]
        [SerializeField] private bool autoUpload = true;
        [SerializeField] private float uploadInterval = 300f; // 5 minutes
        [SerializeField] private int uploadBatchSize = 100;

        #endregion

        #region Private Fields

        private static PlayerHeatmapSystem _instance;

        // Data buffers
        private readonly List<HeatmapEvent> _eventBuffer = new List<HeatmapEvent>();
        private readonly Dictionary<Vector3Int, HeatmapCell> _localHeatmap = new Dictionary<Vector3Int, HeatmapCell>();

        // Tracking state
        private float _lastTrackTime = 0f;
        private float _lastUploadTime = 0f;
        private bool _isUploading = false;

        // Statistics
        private int _totalEventsTracked = 0;
        private int _totalEventsUploaded = 0;
        private int _uploadsFailed = 0;

        // Events
        public event Action<HeatmapEvent> OnEventTracked;
        public event Action<int> OnEventsUploaded;
        public event Action<string> OnUploadFailed;

        #endregion

        #region Properties

        public static PlayerHeatmapSystem Instance => _instance;
        public bool IsTracking => enableTracking;
        public int BufferedEventCount => _eventBuffer.Count;
        public int LocalHeatmapSize => _localHeatmap.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[PlayerHeatmapSystem] Initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!enableTracking) return;

            // Track player position at intervals
            if (trackPositions && Time.time - _lastTrackTime >= trackingInterval)
            {
                TrackPlayerPosition();
                _lastTrackTime = Time.time;
            }

            // Auto-upload at intervals
            if (autoUpload && Time.time - _lastUploadTime >= uploadInterval)
            {
                UploadEvents();
            }
        }

        private void OnApplicationQuit()
        {
            // Upload remaining events on quit
            if (_eventBuffer.Count > 0 && !_isUploading)
            {
                UploadEvents();
            }
        }

        #endregion

        #region Position Tracking

        private void TrackPlayerPosition()
        {
            // Find player transform (customize based on your game)
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            TrackEvent(HeatmapEventType.Position, player.transform.position);
        }

        #endregion

        #region Event Tracking

        /// <summary>
        /// Track a heatmap event.
        /// </summary>
        public void TrackEvent(HeatmapEventType eventType, Vector3 position, string metadata = null)
        {
            if (!enableTracking) return;

            // Check event type filters
            bool shouldTrack = eventType switch
            {
                HeatmapEventType.Position => trackPositions,
                HeatmapEventType.Death => trackDeaths,
                HeatmapEventType.Kill => trackKills,
                HeatmapEventType.Interaction => trackInteractions,
                HeatmapEventType.Combat => trackCombat,
                _ => true
            };

            if (!shouldTrack) return;

            var evt = new HeatmapEvent
            {
                eventType = eventType,
                position = position,
                timestamp = DateTime.UtcNow,
                metadata = metadata,
                sessionId = GetSessionId(),
                userId = GetUserId()
            };

            _eventBuffer.Add(evt);
            _totalEventsTracked++;

            // Update local heatmap
            UpdateLocalHeatmap(position, eventType);

            // Trim buffer if too large
            if (_eventBuffer.Count > maxBufferSize)
            {
                _eventBuffer.RemoveRange(0, _eventBuffer.Count - maxBufferSize);
            }

            OnEventTracked?.Invoke(evt);
        }

        /// <summary>
        /// Track player death.
        /// </summary>
        public void TrackDeath(Vector3 position, string causeOfDeath = null)
        {
            TrackEvent(HeatmapEventType.Death, position, causeOfDeath);
        }

        /// <summary>
        /// Track player kill.
        /// </summary>
        public void TrackKill(Vector3 position, string enemyType = null)
        {
            TrackEvent(HeatmapEventType.Kill, position, enemyType);
        }

        /// <summary>
        /// Track player interaction.
        /// </summary>
        public void TrackInteraction(Vector3 position, string objectType = null)
        {
            TrackEvent(HeatmapEventType.Interaction, position, objectType);
        }

        /// <summary>
        /// Track combat event.
        /// </summary>
        public void TrackCombat(Vector3 position, string combatType = null)
        {
            TrackEvent(HeatmapEventType.Combat, position, combatType);
        }

        #endregion

        #region Local Heatmap

        private void UpdateLocalHeatmap(Vector3 position, HeatmapEventType eventType)
        {
            Vector3Int gridCell = WorldToGrid(position);

            if (!_localHeatmap.TryGetValue(gridCell, out var cell))
            {
                cell = new HeatmapCell
                {
                    gridPosition = gridCell,
                    worldPosition = GridToWorld(gridCell)
                };
                _localHeatmap[gridCell] = cell;
            }

            cell.eventCounts[(int)eventType]++;
            cell.totalEvents++;
        }

        private Vector3Int WorldToGrid(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPosition.x / gridCellSize),
                Mathf.FloorToInt(worldPosition.y / gridCellSize),
                Mathf.FloorToInt(worldPosition.z / gridCellSize)
            );
        }

        private Vector3 GridToWorld(Vector3Int gridPosition)
        {
            return new Vector3(
                gridPosition.x * gridCellSize + gridCellSize * 0.5f,
                gridPosition.y * gridCellSize + gridCellSize * 0.5f,
                gridPosition.z * gridCellSize + gridCellSize * 0.5f
            );
        }

        #endregion

        #region Upload

        /// <summary>
        /// Upload buffered events to backend.
        /// </summary>
        public void UploadEvents(Action onSuccess = null, Action<string> onError = null)
        {
            if (_eventBuffer.Count == 0)
            {
                Debug.Log("[PlayerHeatmapSystem] No events to upload");
                onSuccess?.Invoke();
                return;
            }

            if (_isUploading)
            {
                Debug.LogWarning("[PlayerHeatmapSystem] Upload already in progress");
                return;
            }

            StartCoroutine(UploadEventsCoroutine(onSuccess, onError));
        }

        private IEnumerator UploadEventsCoroutine(Action onSuccess, Action<string> onError)
        {
            _isUploading = true;

            // Take events from buffer
            int count = Mathf.Min(_eventBuffer.Count, uploadBatchSize);
            var eventsToUpload = _eventBuffer.GetRange(0, count);

            var requestData = new HeatmapUploadRequest
            {
                events = eventsToUpload.ToArray()
            };

            string json = JsonUtility.ToJson(requestData);
            string url = backendUrl + heatmapEndpoint;

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

                request.timeout = 30;

                yield return request.SendWebRequest();

                _isUploading = false;
                _lastUploadTime = Time.time;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Remove uploaded events from buffer
                    _eventBuffer.RemoveRange(0, count);

                    _totalEventsUploaded += count;

                    OnEventsUploaded?.Invoke(count);
                    onSuccess?.Invoke();

                    Debug.Log($"[PlayerHeatmapSystem] Uploaded {count} events");
                }
                else
                {
                    _uploadsFailed++;
                    string error = $"Upload failed: {request.error}";
                    OnUploadFailed?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[PlayerHeatmapSystem] {error}");
                }
            }
        }

        #endregion

        #region Visualization

        /// <summary>
        /// Get heatmap cells for visualization.
        /// </summary>
        public HeatmapCell[] GetHeatmapCells(HeatmapEventType? eventType = null)
        {
            if (eventType.HasValue)
            {
                return _localHeatmap.Values
                    .Where(c => c.eventCounts[(int)eventType.Value] > 0)
                    .ToArray();
            }

            return _localHeatmap.Values.ToArray();
        }

        /// <summary>
        /// Get hottest cells (most activity).
        /// </summary>
        public HeatmapCell[] GetHottestCells(int count, HeatmapEventType? eventType = null)
        {
            var cells = GetHeatmapCells(eventType);

            return cells
                .OrderByDescending(c => c.totalEvents)
                .Take(count)
                .ToArray();
        }

        /// <summary>
        /// Get cells in area.
        /// </summary>
        public HeatmapCell[] GetCellsInArea(Vector3 center, float radius)
        {
            return _localHeatmap.Values
                .Where(c => Vector3.Distance(c.worldPosition, center) <= radius)
                .ToArray();
        }

        /// <summary>
        /// Clear local heatmap data.
        /// </summary>
        public void ClearLocalHeatmap()
        {
            _localHeatmap.Clear();
            Debug.Log("[PlayerHeatmapSystem] Local heatmap cleared");
        }

        #endregion

        #region Helpers

        private string GetSessionId()
        {
            if (!PlayerPrefs.HasKey("SessionId"))
            {
                PlayerPrefs.SetString("SessionId", Guid.NewGuid().ToString());
                PlayerPrefs.Save();
            }

            return PlayerPrefs.GetString("SessionId");
        }

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
        /// Get heatmap statistics.
        /// </summary>
        public HeatmapStats GetStats()
        {
            return new HeatmapStats
            {
                totalEventsTracked = _totalEventsTracked,
                totalEventsUploaded = _totalEventsUploaded,
                uploadsFailed = _uploadsFailed,
                bufferedEventCount = _eventBuffer.Count,
                localHeatmapSize = _localHeatmap.Count,
                gridCellSize = gridCellSize
            };
        }

        /// <summary>
        /// Clear event buffer.
        /// </summary>
        public void ClearBuffer()
        {
            _eventBuffer.Clear();
            Debug.Log("[PlayerHeatmapSystem] Event buffer cleared");
        }

        /// <summary>
        /// Enable/disable tracking.
        /// </summary>
        public void SetTracking(bool enabled)
        {
            enableTracking = enabled;
            Debug.Log($"[PlayerHeatmapSystem] Tracking {(enabled ? "enabled" : "disabled")}");
        }

        #endregion

        #region Context Menu

        [ContextMenu("Upload Events")]
        private void UploadEventsMenu()
        {
            UploadEvents();
        }

        [ContextMenu("Clear Buffer")]
        private void ClearBufferMenu()
        {
            ClearBuffer();
        }

        [ContextMenu("Clear Local Heatmap")]
        private void ClearLocalHeatmapMenu()
        {
            ClearLocalHeatmap();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Player Heatmap Statistics ===\n" +
                      $"Total Events Tracked: {stats.totalEventsTracked}\n" +
                      $"Total Events Uploaded: {stats.totalEventsUploaded}\n" +
                      $"Uploads Failed: {stats.uploadsFailed}\n" +
                      $"Buffered Events: {stats.bufferedEventCount}\n" +
                      $"Local Heatmap Size: {stats.localHeatmapSize} cells\n" +
                      $"Grid Cell Size: {stats.gridCellSize}m");
        }

        [ContextMenu("Print Hottest Cells")]
        private void PrintHottestCells()
        {
            var hottestCells = GetHottestCells(10);

            if (hottestCells.Length == 0)
            {
                Debug.Log("[PlayerHeatmapSystem] No heatmap data");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== Hottest Cells (Top 10) ===");

            foreach (var cell in hottestCells)
            {
                sb.AppendLine($"  Position: {cell.worldPosition}, Events: {cell.totalEvents}");
                sb.AppendLine($"    Deaths: {cell.eventCounts[(int)HeatmapEventType.Death]}");
                sb.AppendLine($"    Kills: {cell.eventCounts[(int)HeatmapEventType.Kill]}");
                sb.AppendLine($"    Interactions: {cell.eventCounts[(int)HeatmapEventType.Interaction]}");
            }

            Debug.Log(sb.ToString());
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!enableTracking || _localHeatmap.Count == 0) return;

            // Draw hottest cells
            var hottestCells = GetHottestCells(50);
            float maxEvents = hottestCells.Length > 0 ? hottestCells[0].totalEvents : 1;

            foreach (var cell in hottestCells)
            {
                float intensity = cell.totalEvents / maxEvents;

                // Red = high activity, yellow = medium, green = low
                Gizmos.color = Color.Lerp(Color.green, Color.red, intensity);
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);

                Gizmos.DrawCube(cell.worldPosition, Vector3.one * gridCellSize * 0.9f);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Heatmap event.
    /// </summary>
    [Serializable]
    public class HeatmapEvent
    {
        public HeatmapEventType eventType;
        public Vector3 position;
        public DateTime timestamp;
        public string metadata;
        public string sessionId;
        public string userId;
    }

    /// <summary>
    /// Heatmap cell for local visualization.
    /// </summary>
    [Serializable]
    public class HeatmapCell
    {
        public Vector3Int gridPosition;
        public Vector3 worldPosition;
        public int totalEvents;
        public int[] eventCounts = new int[Enum.GetValues(typeof(HeatmapEventType)).Length];
    }

    /// <summary>
    /// Heatmap statistics.
    /// </summary>
    [Serializable]
    public struct HeatmapStats
    {
        public int totalEventsTracked;
        public int totalEventsUploaded;
        public int uploadsFailed;
        public int bufferedEventCount;
        public int localHeatmapSize;
        public float gridCellSize;
    }

    // Request structures
    [Serializable] class HeatmapUploadRequest { public HeatmapEvent[] events; }

    /// <summary>
    /// Heatmap event types.
    /// </summary>
    public enum HeatmapEventType
    {
        Position = 0,
        Death = 1,
        Kill = 2,
        Interaction = 3,
        Combat = 4
    }

    #endregion
}
