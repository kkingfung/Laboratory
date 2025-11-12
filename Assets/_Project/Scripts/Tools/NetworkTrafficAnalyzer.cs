using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Laboratory.Tools
{
    /// <summary>
    /// Network traffic analyzer for monitoring bandwidth usage and packet inspection.
    /// Tracks sent/received bytes, packet counts, and message types.
    /// Provides real-time bandwidth monitoring and historical analysis.
    /// </summary>
    public class NetworkTrafficAnalyzer : MonoBehaviour
    {
        #region Configuration

        [Header("Analysis Settings")]
        [SerializeField] private bool enableAnalysis = true;
        [SerializeField] private bool logTraffic = false;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private KeyCode toggleUI = KeyCode.F10;

        [Header("Data Collection")]
        [SerializeField] private int maxHistorySeconds = 300; // 5 minutes
        [SerializeField] private bool trackMessageTypes = true;
        [SerializeField] private bool trackPerConnection = true;

        [Header("Thresholds")]
        [SerializeField] private long warningBytesPerSecond = 1024 * 100; // 100 KB/s
        [SerializeField] private long criticalBytesPerSecond = 1024 * 500; // 500 KB/s

        #endregion

        #region Private Fields

        private static NetworkTrafficAnalyzer _instance;

        // Traffic data
        private readonly List<TrafficSample> _trafficHistory = new List<TrafficSample>();
        private readonly Dictionary<string, MessageTypeStats> _messageStats = new Dictionary<string, MessageTypeStats>();
        private readonly Dictionary<ulong, ConnectionStats> _connectionStats = new Dictionary<ulong, ConnectionStats>();

        // Current period tracking
        private long _bytesSentThisPeriod;
        private long _bytesReceivedThisPeriod;
        private int _packetsSentThisPeriod;
        private int _packetsReceivedThisPeriod;
        private float _periodStartTime;

        // Totals
        private long _totalBytesSent;
        private long _totalBytesReceived;
        private int _totalPacketsSent;
        private int _totalPacketsReceived;

        // Current stats
        private long _currentBytesSentPerSecond;
        private long _currentBytesReceivedPerSecond;
        private long _peakBytesSentPerSecond;
        private long _peakBytesReceivedPerSecond;

        // UI
        private bool _showUI = false;
        private Rect _windowRect = new Rect(10, 10, 500, 600);
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Overview", "Messages", "Connections", "History" };

        #endregion

        #region Properties

        public static NetworkTrafficAnalyzer Instance => _instance;
        public bool IsEnabled => enableAnalysis;
        public long CurrentBandwidthSent => _currentBytesSentPerSecond;
        public long CurrentBandwidthReceived => _currentBytesReceivedPerSecond;

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
            if (!enableAnalysis) return;

            // Toggle UI
            if (Input.GetKeyDown(toggleUI))
            {
                _showUI = !_showUI;
            }

            // Update bandwidth calculations
            if (Time.time - _periodStartTime >= updateInterval)
            {
                UpdateBandwidthStats();
            }
        }

        private void OnGUI()
        {
            if (!enableAnalysis || !_showUI) return;

            _windowRect = GUI.Window(1, _windowRect, DrawWindow, "Network Traffic Analyzer");
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[NetworkTrafficAnalyzer] Initialized");
            _periodStartTime = Time.time;
        }

        #endregion

        #region Traffic Tracking

        /// <summary>
        /// Record bytes sent.
        /// </summary>
        public void RecordBytesSent(long bytes, string messageType = null, ulong connectionId = 0)
        {
            if (!enableAnalysis) return;

            _bytesSentThisPeriod += bytes;
            _packetsSentThisPeriod++;
            _totalBytesSent += bytes;
            _totalPacketsSent++;

            if (trackMessageTypes && !string.IsNullOrEmpty(messageType))
            {
                RecordMessageStats(messageType, bytes, true);
            }

            if (trackPerConnection && connectionId != 0)
            {
                RecordConnectionStats(connectionId, bytes, true);
            }

            if (logTraffic)
            {
                Debug.Log($"[NetworkTraffic] Sent: {bytes} bytes{(messageType != null ? $" ({messageType})" : "")}");
            }
        }

        /// <summary>
        /// Record bytes received.
        /// </summary>
        public void RecordBytesReceived(long bytes, string messageType = null, ulong connectionId = 0)
        {
            if (!enableAnalysis) return;

            _bytesReceivedThisPeriod += bytes;
            _packetsReceivedThisPeriod++;
            _totalBytesReceived += bytes;
            _totalPacketsReceived++;

            if (trackMessageTypes && !string.IsNullOrEmpty(messageType))
            {
                RecordMessageStats(messageType, bytes, false);
            }

            if (trackPerConnection && connectionId != 0)
            {
                RecordConnectionStats(connectionId, bytes, false);
            }

            if (logTraffic)
            {
                Debug.Log($"[NetworkTraffic] Received: {bytes} bytes{(messageType != null ? $" ({messageType})" : "")}");
            }
        }

        private void RecordMessageStats(string messageType, long bytes, bool isSent)
        {
            if (!_messageStats.ContainsKey(messageType))
            {
                _messageStats[messageType] = new MessageTypeStats { messageType = messageType };
            }

            var stats = _messageStats[messageType];
            if (isSent)
            {
                stats.bytesSent += bytes;
                stats.packetsSent++;
            }
            else
            {
                stats.bytesReceived += bytes;
                stats.packetsReceived++;
            }
        }

        private void RecordConnectionStats(ulong connectionId, long bytes, bool isSent)
        {
            if (!_connectionStats.ContainsKey(connectionId))
            {
                _connectionStats[connectionId] = new ConnectionStats { connectionId = connectionId };
            }

            var stats = _connectionStats[connectionId];
            if (isSent)
            {
                stats.bytesSent += bytes;
                stats.packetsSent++;
            }
            else
            {
                stats.bytesReceived += bytes;
                stats.packetsReceived++;
            }
        }

        #endregion

        #region Bandwidth Calculation

        private void UpdateBandwidthStats()
        {
            float periodDuration = Time.time - _periodStartTime;

            // Calculate bytes per second
            _currentBytesSentPerSecond = (long)(_bytesSentThisPeriod / periodDuration);
            _currentBytesReceivedPerSecond = (long)(_bytesReceivedThisPeriod / periodDuration);

            // Update peaks
            if (_currentBytesSentPerSecond > _peakBytesSentPerSecond)
            {
                _peakBytesSentPerSecond = _currentBytesSentPerSecond;
            }

            if (_currentBytesReceivedPerSecond > _peakBytesReceivedPerSecond)
            {
                _peakBytesReceivedPerSecond = _currentBytesReceivedPerSecond;
            }

            // Record sample
            var sample = new TrafficSample
            {
                timestamp = Time.time,
                bytesSent = _currentBytesSentPerSecond,
                bytesReceived = _currentBytesReceivedPerSecond,
                packetsSent = _packetsSentThisPeriod,
                packetsReceived = _packetsReceivedThisPeriod
            };

            _trafficHistory.Add(sample);

            // Trim history
            float cutoffTime = Time.time - maxHistorySeconds;
            _trafficHistory.RemoveAll(s => s.timestamp < cutoffTime);

            // Check thresholds
            CheckThresholds();

            // Reset period counters
            _bytesSentThisPeriod = 0;
            _bytesReceivedThisPeriod = 0;
            _packetsSentThisPeriod = 0;
            _packetsReceivedThisPeriod = 0;
            _periodStartTime = Time.time;
        }

        private void CheckThresholds()
        {
            long totalBandwidth = _currentBytesSentPerSecond + _currentBytesReceivedPerSecond;

            if (totalBandwidth >= criticalBytesPerSecond)
            {
                Debug.LogError($"[NetworkTraffic] CRITICAL: Bandwidth at {FormatBytes(totalBandwidth)}/s");
            }
            else if (totalBandwidth >= warningBytesPerSecond)
            {
                Debug.LogWarning($"[NetworkTraffic] WARNING: Bandwidth at {FormatBytes(totalBandwidth)}/s");
            }
        }

        #endregion

        #region UI

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // Tabs
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);

            GUILayout.Space(10);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0: DrawOverviewTab(); break;
                case 1: DrawMessagesTab(); break;
                case 2: DrawConnectionsTab(); break;
                case 3: DrawHistoryTab(); break;
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void DrawOverviewTab()
        {
            GUILayout.Label("Current Bandwidth", GUI.skin.box);
            GUILayout.Label($"Sent: {FormatBytes(_currentBytesSentPerSecond)}/s");
            GUILayout.Label($"Received: {FormatBytes(_currentBytesReceivedPerSecond)}/s");
            GUILayout.Label($"Total: {FormatBytes(_currentBytesSentPerSecond + _currentBytesReceivedPerSecond)}/s");

            GUILayout.Space(10);

            GUILayout.Label("Peak Bandwidth", GUI.skin.box);
            GUILayout.Label($"Peak Sent: {FormatBytes(_peakBytesSentPerSecond)}/s");
            GUILayout.Label($"Peak Received: {FormatBytes(_peakBytesReceivedPerSecond)}/s");

            GUILayout.Space(10);

            GUILayout.Label("Totals", GUI.skin.box);
            GUILayout.Label($"Total Sent: {FormatBytes(_totalBytesSent)}");
            GUILayout.Label($"Total Received: {FormatBytes(_totalBytesReceived)}");
            GUILayout.Label($"Packets Sent: {_totalPacketsSent}");
            GUILayout.Label($"Packets Received: {_totalPacketsReceived}");

            GUILayout.Space(10);

            if (GUILayout.Button("Reset Statistics"))
            {
                ResetStatistics();
            }

            if (GUILayout.Button("Export Report"))
            {
                ExportReport();
            }
        }

        private void DrawMessagesTab()
        {
            GUILayout.Label($"Message Types: {_messageStats.Count}", GUI.skin.box);

            foreach (var kvp in _messageStats.OrderByDescending(m => m.Value.bytesSent + m.Value.bytesReceived))
            {
                var stats = kvp.Value;
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(stats.messageType, GUI.skin.label);
                GUILayout.Label($"  Sent: {FormatBytes(stats.bytesSent)} ({stats.packetsSent} packets)");
                GUILayout.Label($"  Received: {FormatBytes(stats.bytesReceived)} ({stats.packetsReceived} packets)");
                GUILayout.EndVertical();
            }
        }

        private void DrawConnectionsTab()
        {
            GUILayout.Label($"Active Connections: {_connectionStats.Count}", GUI.skin.box);

            foreach (var kvp in _connectionStats.OrderByDescending(c => c.Value.bytesSent + c.Value.bytesReceived))
            {
                var stats = kvp.Value;
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"Connection {stats.connectionId}", GUI.skin.label);
                GUILayout.Label($"  Sent: {FormatBytes(stats.bytesSent)} ({stats.packetsSent} packets)");
                GUILayout.Label($"  Received: {FormatBytes(stats.bytesReceived)} ({stats.packetsReceived} packets)");
                GUILayout.EndVertical();
            }
        }

        private void DrawHistoryTab()
        {
            GUILayout.Label($"Samples: {_trafficHistory.Count} ({maxHistorySeconds}s)", GUI.skin.box);

            if (_trafficHistory.Count > 0)
            {
                var recent = _trafficHistory.Skip(Math.Max(0, _trafficHistory.Count - 10)).ToList();
                foreach (var sample in recent.AsEnumerable().Reverse())
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    GUILayout.Label($"Time: {sample.timestamp:F1}s");
                    GUILayout.Label($"Sent: {FormatBytes(sample.bytesSent)}/s ({sample.packetsSent} packets)");
                    GUILayout.Label($"Received: {FormatBytes(sample.bytesReceived)}/s ({sample.packetsReceived} packets)");
                    GUILayout.EndVertical();
                }
            }
        }

        #endregion

        #region Reporting

        private void ExportReport()
        {
            var report = GenerateReport();
            string path = System.IO.Path.Combine(Application.persistentDataPath, $"network_traffic_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            System.IO.File.WriteAllText(path, report);

            Debug.Log($"[NetworkTrafficAnalyzer] Report exported to: {path}");
        }

        private string GenerateReport()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Network Traffic Report ===");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine("Current Bandwidth:");
            sb.AppendLine($"  Sent: {FormatBytes(_currentBytesSentPerSecond)}/s");
            sb.AppendLine($"  Received: {FormatBytes(_currentBytesReceivedPerSecond)}/s");
            sb.AppendLine();

            sb.AppendLine("Totals:");
            sb.AppendLine($"  Total Sent: {FormatBytes(_totalBytesSent)}");
            sb.AppendLine($"  Total Received: {FormatBytes(_totalBytesReceived)}");
            sb.AppendLine($"  Packets Sent: {_totalPacketsSent}");
            sb.AppendLine($"  Packets Received: {_totalPacketsReceived}");
            sb.AppendLine();

            sb.AppendLine("Top Message Types:");
            foreach (var kvp in _messageStats.OrderByDescending(m => m.Value.bytesSent + m.Value.bytesReceived).Take(10))
            {
                sb.AppendLine($"  {kvp.Key}: {FormatBytes(kvp.Value.bytesSent + kvp.Value.bytesReceived)}");
            }

            return sb.ToString();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get network traffic statistics.
        /// </summary>
        public NetworkTrafficStats GetStats()
        {
            return new NetworkTrafficStats
            {
                currentBytesSentPerSecond = _currentBytesSentPerSecond,
                currentBytesReceivedPerSecond = _currentBytesReceivedPerSecond,
                peakBytesSentPerSecond = _peakBytesSentPerSecond,
                peakBytesReceivedPerSecond = _peakBytesReceivedPerSecond,
                totalBytesSent = _totalBytesSent,
                totalBytesReceived = _totalBytesReceived,
                totalPacketsSent = _totalPacketsSent,
                totalPacketsReceived = _totalPacketsReceived,
                messageTypeCount = _messageStats.Count,
                connectionCount = _connectionStats.Count
            };
        }

        /// <summary>
        /// Reset all statistics.
        /// </summary>
        public void ResetStatistics()
        {
            _trafficHistory.Clear();
            _messageStats.Clear();
            _connectionStats.Clear();

            _totalBytesSent = 0;
            _totalBytesReceived = 0;
            _totalPacketsSent = 0;
            _totalPacketsReceived = 0;

            _currentBytesSentPerSecond = 0;
            _currentBytesReceivedPerSecond = 0;
            _peakBytesSentPerSecond = 0;
            _peakBytesReceivedPerSecond = 0;

            Debug.Log("[NetworkTrafficAnalyzer] Statistics reset");
        }

        #endregion

        #region Helper Methods

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F2} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / 1024f / 1024f:F2} MB";
            else
                return $"{bytes / 1024f / 1024f / 1024f:F2} GB";
        }

        #endregion

        #region Context Menu

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            Debug.Log(GenerateReport());
        }

        [ContextMenu("Reset Statistics")]
        private void ResetStatisticsMenu()
        {
            ResetStatistics();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// A traffic sample at a point in time.
    /// </summary>
    [Serializable]
    public struct TrafficSample
    {
        public float timestamp;
        public long bytesSent;
        public long bytesReceived;
        public int packetsSent;
        public int packetsReceived;
    }

    /// <summary>
    /// Statistics for a message type.
    /// </summary>
    [Serializable]
    public class MessageTypeStats
    {
        public string messageType;
        public long bytesSent;
        public long bytesReceived;
        public int packetsSent;
        public int packetsReceived;
    }

    /// <summary>
    /// Statistics for a connection.
    /// </summary>
    [Serializable]
    public class ConnectionStats
    {
        public ulong connectionId;
        public long bytesSent;
        public long bytesReceived;
        public int packetsSent;
        public int packetsReceived;
    }

    /// <summary>
    /// Network traffic statistics.
    /// </summary>
    [Serializable]
    public struct NetworkTrafficStats
    {
        public long currentBytesSentPerSecond;
        public long currentBytesReceivedPerSecond;
        public long peakBytesSentPerSecond;
        public long peakBytesReceivedPerSecond;
        public long totalBytesSent;
        public long totalBytesReceived;
        public int totalPacketsSent;
        public int totalPacketsReceived;
        public int messageTypeCount;
        public int connectionCount;
    }

    #endregion
}
