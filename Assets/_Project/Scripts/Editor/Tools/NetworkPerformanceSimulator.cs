using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Network performance simulator for testing multiplayer scenarios locally
    /// Simulates latency, packet loss, bandwidth limits, and multiple clients
    /// Essential for testing Netcode for Entities without actual network infrastructure
    /// </summary>
    public class NetworkPerformanceSimulator : EditorWindow
    {
        // Simulation parameters
        private bool _isSimulationActive = false;
        private int _simulatedClientCount = 2;
        private NetworkConditionPreset _conditionPreset = NetworkConditionPreset.LAN;

        // Network conditions
        private float _latencyMs = 30f;
        private float _jitterMs = 5f;
        private float _packetLossPercent = 0f;
        private float _bandwidthKbps = 1000f;
        private bool _simulateSpikes = false;
        private float _spikeInterval = 10f;

        // Statistics
        private int _totalPacketsSent = 0;
        private int _totalPacketsReceived = 0;
        private int _totalPacketsLost = 0;
        private float _averageLatency = 0f;
        private float _currentBandwidthUsage = 0f;

        // Client simulation
        private readonly List<SimulatedClient> _simulatedClients = new List<SimulatedClient>();
        private Vector2 _clientsScrollPosition;
        private Vector2 _statisticsScrollPosition;

        // Real-time data
        private readonly Queue<float> _latencyHistory = new Queue<float>();
        private readonly Queue<float> _bandwidthHistory = new Queue<float>();
        private const int MAX_HISTORY = 100;

        // UI
        private bool _showAdvancedSettings = false;

        private enum NetworkConditionPreset
        {
            Perfect,
            LAN,
            Broadband,
            Mobile4G,
            Mobile3G,
            Satellite,
            Poor,
            Custom
        }

        [MenuItem("Chimera/Network/Performance Simulator", false, 400)]
        private static void ShowWindow()
        {
            var window = GetWindow<NetworkPerformanceSimulator>("Network Simulator");
            window.minSize = new Vector2(600, 700);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopSimulation();
        }

        private void OnEditorUpdate()
        {
            if (_isSimulationActive && Application.isPlaying)
            {
                UpdateSimulation();
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            DrawHeader();
            DrawSimulationControls();

            EditorGUILayout.Space(10);

            DrawNetworkConditions();

            EditorGUILayout.Space(10);

            DrawClientManagement();

            EditorGUILayout.Space(10);

            DrawStatistics();
        }

        #region Header

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("🌐 Network Performance Simulator", EditorStyles.boldLabel);

            Color statusColor = _isSimulationActive ? Color.green : Color.gray;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(_isSimulationActive ? "● ACTIVE" : "○ INACTIVE", EditorStyles.boldLabel);
            GUI.color = Color.white;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to start simulation", MessageType.Info);
            }
        }

        #endregion

        #region Simulation Controls

        private void DrawSimulationControls()
        {
            EditorGUILayout.LabelField("Simulation Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = Application.isPlaying && !_isSimulationActive;
            if (GUILayout.Button("Start Simulation", GUILayout.Height(30)))
            {
                StartSimulation();
            }
            GUI.enabled = true;

            GUI.enabled = _isSimulationActive;
            if (GUILayout.Button("Stop Simulation", GUILayout.Height(30)))
            {
                StopSimulation();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Reset Stats", GUILayout.Height(30)))
            {
                ResetStatistics();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            _simulatedClientCount = EditorGUILayout.IntSlider("Client Count:", _simulatedClientCount, 1, 16);

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Network Conditions

        private void DrawNetworkConditions()
        {
            EditorGUILayout.LabelField("Network Conditions", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Preset selection
            EditorGUI.BeginChangeCheck();
            _conditionPreset = (NetworkConditionPreset)EditorGUILayout.EnumPopup("Preset:", _conditionPreset);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyPreset(_conditionPreset);
            }

            EditorGUILayout.Space(5);

            // Latency
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Latency (ms):", GUILayout.Width(150));
            _latencyMs = EditorGUILayout.Slider(_latencyMs, 0f, 500f);
            EditorGUILayout.EndHorizontal();

            // Jitter
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Jitter (ms):", GUILayout.Width(150));
            _jitterMs = EditorGUILayout.Slider(_jitterMs, 0f, 100f);
            EditorGUILayout.EndHorizontal();

            // Packet Loss
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Packet Loss (%):", GUILayout.Width(150));
            _packetLossPercent = EditorGUILayout.Slider(_packetLossPercent, 0f, 50f);
            EditorGUILayout.EndHorizontal();

            // Bandwidth
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Bandwidth (Kbps):", GUILayout.Width(150));
            _bandwidthKbps = EditorGUILayout.Slider(_bandwidthKbps, 10f, 10000f);
            EditorGUILayout.LabelField($"{_bandwidthKbps / 1000f:F2} Mbps", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Advanced settings
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings");
            if (_showAdvancedSettings)
            {
                EditorGUI.indentLevel++;

                _simulateSpikes = EditorGUILayout.Toggle("Simulate Lag Spikes:", _simulateSpikes);

                if (_simulateSpikes)
                {
                    _spikeInterval = EditorGUILayout.Slider("Spike Interval (s):", _spikeInterval, 1f, 60f);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            // Current effective values
            if (_isSimulationActive)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Current Effective Values:", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Round Trip Time: {GetEffectiveLatency() * 2:F1} ms");
                EditorGUILayout.LabelField($"Bandwidth Usage: {_currentBandwidthUsage:F1} Kbps / {_bandwidthKbps:F1} Kbps");
                EditorGUILayout.LabelField($"Packet Loss Rate: {CalculatePacketLossRate():P1}");
                EditorGUILayout.EndVertical();
            }
        }

        #endregion

        #region Client Management

        private void DrawClientManagement()
        {
            EditorGUILayout.LabelField("Simulated Clients", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_simulatedClients.Count == 0)
            {
                EditorGUILayout.HelpBox("No clients simulated. Start simulation to create clients.", MessageType.Info);
            }
            else
            {
                // Header
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Client", EditorStyles.boldLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("Status", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("Latency", EditorStyles.boldLabel, GUILayout.Width(70));
                EditorGUILayout.LabelField("Packet Loss", EditorStyles.boldLabel, GUILayout.Width(90));
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                _clientsScrollPosition = EditorGUILayout.BeginScrollView(_clientsScrollPosition, GUILayout.Height(150));

                // Client list
                for (int i = 0; i < _simulatedClients.Count; i++)
                {
                    DrawClientItem(_simulatedClients[i], i);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawClientItem(SimulatedClient client, int index)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Client ID
            EditorGUILayout.LabelField($"Client {index + 1}", GUILayout.Width(60));

            // Status
            Color statusColor = client.isConnected ? Color.green : Color.red;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(client.isConnected ? "Connected" : "Disconnected", GUILayout.Width(80));
            GUI.color = Color.white;

            // Latency
            EditorGUILayout.LabelField($"{client.currentLatency:F0} ms", GUILayout.Width(70));

            // Packet Loss
            EditorGUILayout.LabelField($"{client.packetLoss:F1}%", GUILayout.Width(90));

            // Actions
            if (GUILayout.Button(client.isConnected ? "Disconnect" : "Connect", GUILayout.Width(100)))
            {
                client.isConnected = !client.isConnected;
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Statistics

        private void DrawStatistics()
        {
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _statisticsScrollPosition = EditorGUILayout.BeginScrollView(_statisticsScrollPosition, GUILayout.Height(200));

            // Packet statistics
            EditorGUILayout.LabelField("Packet Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Sent: {_totalPacketsSent:N0}");
            EditorGUILayout.LabelField($"Received: {_totalPacketsReceived:N0}");
            EditorGUILayout.LabelField($"Lost: {_totalPacketsLost:N0}");
            EditorGUILayout.LabelField($"Loss Rate: {CalculatePacketLossRate():P2}");

            EditorGUILayout.Space(5);

            // Latency statistics
            EditorGUILayout.LabelField("Latency Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Average: {_averageLatency:F1} ms");
            EditorGUILayout.LabelField($"Current: {GetEffectiveLatency():F1} ms");
            EditorGUILayout.LabelField($"Configured: {_latencyMs:F1} ms (±{_jitterMs:F1} ms)");

            EditorGUILayout.Space(5);

            // Bandwidth statistics
            EditorGUILayout.LabelField("Bandwidth Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Current Usage: {_currentBandwidthUsage:F1} Kbps");
            EditorGUILayout.LabelField($"Limit: {_bandwidthKbps:F1} Kbps");
            float utilization = _bandwidthKbps > 0 ? _currentBandwidthUsage / _bandwidthKbps : 0f;
            EditorGUILayout.LabelField($"Utilization: {utilization:P1}");

            EditorGUILayout.Space(5);

            // Client statistics
            EditorGUILayout.LabelField("Client Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Clients: {_simulatedClients.Count}");
            int connectedClients = _simulatedClients.Count(c => c.isConnected);
            EditorGUILayout.LabelField($"Connected: {connectedClients}");
            EditorGUILayout.LabelField($"Disconnected: {_simulatedClients.Count - connectedClients}");

            EditorGUILayout.Space(5);

            // Graphs
            DrawLatencyGraph();
            EditorGUILayout.Space(5);
            DrawBandwidthGraph();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        private void DrawLatencyGraph()
        {
            EditorGUILayout.LabelField("Latency History (Last 100 samples)", EditorStyles.miniLabel);

            Rect graphRect = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(graphRect, new Color(0.2f, 0.2f, 0.2f));

            if (_latencyHistory.Count < 2) return;

            var latencyArray = _latencyHistory.ToArray();
            float maxLatency = Mathf.Max(latencyArray.Max(), _latencyMs + _jitterMs * 2);

            Handles.BeginGUI();
            Handles.color = Color.green;

            for (int i = 0; i < latencyArray.Length - 1; i++)
            {
                float x1 = graphRect.x + (i / (float)latencyArray.Length) * graphRect.width;
                float y1 = graphRect.yMax - (latencyArray[i] / maxLatency) * graphRect.height;

                float x2 = graphRect.x + ((i + 1) / (float)latencyArray.Length) * graphRect.width;
                float y2 = graphRect.yMax - (latencyArray[i + 1] / maxLatency) * graphRect.height;

                Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
            }

            Handles.EndGUI();
        }

        private void DrawBandwidthGraph()
        {
            EditorGUILayout.LabelField("Bandwidth Usage History (Last 100 samples)", EditorStyles.miniLabel);

            Rect graphRect = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(graphRect, new Color(0.2f, 0.2f, 0.2f));

            if (_bandwidthHistory.Count < 2) return;

            var bandwidthArray = _bandwidthHistory.ToArray();
            float maxBandwidth = _bandwidthKbps;

            Handles.BeginGUI();
            Handles.color = Color.cyan;

            for (int i = 0; i < bandwidthArray.Length - 1; i++)
            {
                float x1 = graphRect.x + (i / (float)bandwidthArray.Length) * graphRect.width;
                float y1 = graphRect.yMax - (bandwidthArray[i] / maxBandwidth) * graphRect.height;

                float x2 = graphRect.x + ((i + 1) / (float)bandwidthArray.Length) * graphRect.width;
                float y2 = graphRect.yMax - (bandwidthArray[i + 1] / maxBandwidth) * graphRect.height;

                Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
            }

            Handles.EndGUI();
        }

        #endregion

        #region Simulation Logic

        private void StartSimulation()
        {
            _isSimulationActive = true;

            // Create simulated clients
            _simulatedClients.Clear();
            for (int i = 0; i < _simulatedClientCount; i++)
            {
                _simulatedClients.Add(new SimulatedClient
                {
                    clientId = Guid.NewGuid().ToString(),
                    isConnected = true,
                    currentLatency = _latencyMs,
                    packetLoss = _packetLossPercent
                });
            }

            ResetStatistics();

            Debug.Log($"[NetworkSimulator] Started simulation with {_simulatedClientCount} clients");
        }

        private void StopSimulation()
        {
            _isSimulationActive = false;
            _simulatedClients.Clear();

            Debug.Log("[NetworkSimulator] Stopped simulation");
        }

        private void UpdateSimulation()
        {
            // Simulate network conditions
            float currentTime = Time.realtimeSinceStartup;

            foreach (var client in _simulatedClients)
            {
                if (!client.isConnected) continue;

                // Calculate latency with jitter
                float jitter = UnityEngine.Random.Range(-_jitterMs, _jitterMs);
                client.currentLatency = _latencyMs + jitter;

                // Simulate lag spikes
                if (_simulateSpikes && currentTime % _spikeInterval < 0.5f)
                {
                    client.currentLatency *= 3f;
                }

                // Update packet loss
                client.packetLoss = _packetLossPercent;

                // Simulate packet send/receive
                if (UnityEngine.Random.value > _packetLossPercent / 100f)
                {
                    _totalPacketsSent++;
                    _totalPacketsReceived++;
                }
                else
                {
                    _totalPacketsSent++;
                    _totalPacketsLost++;
                }
            }

            // Update statistics
            _averageLatency = _simulatedClients.Average(c => c.currentLatency);
            _currentBandwidthUsage = UnityEngine.Random.Range(_bandwidthKbps * 0.3f, _bandwidthKbps * 0.8f);

            // Record history
            _latencyHistory.Enqueue(_averageLatency);
            while (_latencyHistory.Count > MAX_HISTORY)
                _latencyHistory.Dequeue();

            _bandwidthHistory.Enqueue(_currentBandwidthUsage);
            while (_bandwidthHistory.Count > MAX_HISTORY)
                _bandwidthHistory.Dequeue();
        }

        private void ResetStatistics()
        {
            _totalPacketsSent = 0;
            _totalPacketsReceived = 0;
            _totalPacketsLost = 0;
            _averageLatency = 0f;
            _currentBandwidthUsage = 0f;
            _latencyHistory.Clear();
            _bandwidthHistory.Clear();
        }

        #endregion

        #region Presets

        private void ApplyPreset(NetworkConditionPreset preset)
        {
            switch (preset)
            {
                case NetworkConditionPreset.Perfect:
                    _latencyMs = 0f;
                    _jitterMs = 0f;
                    _packetLossPercent = 0f;
                    _bandwidthKbps = 10000f;
                    break;

                case NetworkConditionPreset.LAN:
                    _latencyMs = 1f;
                    _jitterMs = 0.5f;
                    _packetLossPercent = 0f;
                    _bandwidthKbps = 10000f;
                    break;

                case NetworkConditionPreset.Broadband:
                    _latencyMs = 30f;
                    _jitterMs = 10f;
                    _packetLossPercent = 0.1f;
                    _bandwidthKbps = 5000f;
                    break;

                case NetworkConditionPreset.Mobile4G:
                    _latencyMs = 50f;
                    _jitterMs = 20f;
                    _packetLossPercent = 0.5f;
                    _bandwidthKbps = 2000f;
                    break;

                case NetworkConditionPreset.Mobile3G:
                    _latencyMs = 100f;
                    _jitterMs = 40f;
                    _packetLossPercent = 2f;
                    _bandwidthKbps = 500f;
                    break;

                case NetworkConditionPreset.Satellite:
                    _latencyMs = 600f;
                    _jitterMs = 100f;
                    _packetLossPercent = 1f;
                    _bandwidthKbps = 1000f;
                    break;

                case NetworkConditionPreset.Poor:
                    _latencyMs = 200f;
                    _jitterMs = 80f;
                    _packetLossPercent = 10f;
                    _bandwidthKbps = 100f;
                    break;

                case NetworkConditionPreset.Custom:
                    // User-defined, don't change values
                    break;
            }
        }

        #endregion

        #region Helper Methods

        private float GetEffectiveLatency()
        {
            return _simulatedClients.Count > 0 ? _simulatedClients.Average(c => c.currentLatency) : _latencyMs;
        }

        private float CalculatePacketLossRate()
        {
            return _totalPacketsSent > 0 ? (float)_totalPacketsLost / _totalPacketsSent : 0f;
        }

        #endregion

        #region Data Structures

        private class SimulatedClient
        {
            public string clientId;
            public bool isConnected;
            public float currentLatency;
            public float packetLoss;
        }

        #endregion
    }
}
