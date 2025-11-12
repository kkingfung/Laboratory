using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Tools
{
    /// <summary>
    /// Client-side prediction debugging tool for multiplayer gameplay.
    /// Visualizes prediction errors, server corrections, and network state.
    /// Helps identify and fix desync issues in networked games.
    /// </summary>
    public class PredictionDebugger : MonoBehaviour
    {
        #region Configuration

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugging = true;
        [SerializeField] private bool visualizeErrors = true;
        [SerializeField] private bool logPredictionErrors = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F11;

        [Header("Visualization")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool drawOnScreenInfo = true;
        [SerializeField] private Color predictedColor = Color.yellow;
        [SerializeField] private Color serverColor = Color.green;
        [SerializeField] private Color errorColor = Color.red;

        [Header("Error Tracking")]
        [SerializeField] private float errorThreshold = 0.1f; // Meters
        [SerializeField] private int maxHistorySize = 100;
        [SerializeField] private bool trackErrorStatistics = true;

        #endregion

        #region Private Fields

        private static PredictionDebugger _instance;

        // Tracked entities
        private readonly Dictionary<ulong, PredictionState> _entityStates = new Dictionary<ulong, PredictionState>();

        // Error history
        private readonly List<PredictionError> _errorHistory = new List<PredictionError>();

        // Statistics
        private int _totalPredictions;
        private int _totalCorrections;
        private float _averageError;
        private float _maxError;
        private int _errorsAboveThreshold;

        // UI
        private bool _showDebugUI = true;
        private Rect _debugWindowRect = new Rect(10, 10, 400, 500);
        private Vector2 _scrollPosition;

        #endregion

        #region Properties

        public static PredictionDebugger Instance => _instance;
        public bool IsEnabled => enableDebugging;
        public int TrackedEntityCount => _entityStates.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[PredictionDebugger] Initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!enableDebugging) return;

            // Toggle UI
            if (Input.GetKeyDown(toggleKey))
            {
                _showDebugUI = !_showDebugUI;
            }

            // Update error statistics
            if (trackErrorStatistics)
            {
                UpdateErrorStatistics();
            }
        }

        private void OnGUI()
        {
            if (!enableDebugging || !drawOnScreenInfo || !_showDebugUI) return;

            _debugWindowRect = GUI.Window(0, _debugWindowRect, DrawDebugWindow, "Prediction Debugger");
        }

        private void OnDrawGizmos()
        {
            if (!enableDebugging || !drawGizmos || !visualizeErrors) return;

            foreach (var kvp in _entityStates)
            {
                DrawPredictionGizmos(kvp.Value);
            }
        }

        #endregion

        #region Entity Tracking

        /// <summary>
        /// Register an entity for prediction debugging.
        /// </summary>
        public void RegisterEntity(ulong entityId, Vector3 initialPosition)
        {
            if (_entityStates.ContainsKey(entityId)) return;

            _entityStates[entityId] = new PredictionState
            {
                entityId = entityId,
                predictedPosition = initialPosition,
                serverPosition = initialPosition,
                lastUpdateTime = Time.time
            };

            Debug.Log($"[PredictionDebugger] Registered entity: {entityId}");
        }

        /// <summary>
        /// Unregister an entity.
        /// </summary>
        public void UnregisterEntity(ulong entityId)
        {
            _entityStates.Remove(entityId);
        }

        /// <summary>
        /// Update predicted position for an entity.
        /// </summary>
        public void UpdatePredictedPosition(ulong entityId, Vector3 predictedPosition, Vector3 predictedVelocity)
        {
            if (!_entityStates.TryGetValue(entityId, out var state)) return;

            state.predictedPosition = predictedPosition;
            state.predictedVelocity = predictedVelocity;
            state.lastPredictionTime = Time.time;

            _totalPredictions++;
        }

        /// <summary>
        /// Update server-authoritative position (server correction).
        /// </summary>
        public void UpdateServerPosition(ulong entityId, Vector3 serverPosition, Vector3 serverVelocity, float serverTimestamp)
        {
            if (!_entityStates.TryGetValue(entityId, out var state)) return;

            // Store previous server position
            state.previousServerPosition = state.serverPosition;

            // Update server state
            state.serverPosition = serverPosition;
            state.serverVelocity = serverVelocity;
            state.serverTimestamp = serverTimestamp;
            state.lastUpdateTime = Time.time;

            // Calculate prediction error
            float error = Vector3.Distance(state.predictedPosition, serverPosition);
            state.predictionError = error;

            // Record error if above threshold
            if (error > errorThreshold)
            {
                RecordPredictionError(entityId, state.predictedPosition, serverPosition, error);
            }

            _totalCorrections++;
        }

        /// <summary>
        /// Record when reconciliation occurs.
        /// </summary>
        public void RecordReconciliation(ulong entityId, Vector3 correctionDelta)
        {
            if (!_entityStates.TryGetValue(entityId, out var state)) return;

            state.reconciliationCount++;
            state.lastReconciliationTime = Time.time;
            state.lastCorrectionDelta = correctionDelta;

            if (logPredictionErrors)
            {
                Debug.LogWarning($"[PredictionDebugger] Reconciliation: Entity {entityId}, Delta: {correctionDelta.magnitude:F3}m");
            }
        }

        #endregion

        #region Error Tracking

        private void RecordPredictionError(ulong entityId, Vector3 predicted, Vector3 server, float error)
        {
            var predictionError = new PredictionError
            {
                entityId = entityId,
                predictedPosition = predicted,
                serverPosition = server,
                error = error,
                timestamp = Time.time
            };

            _errorHistory.Add(predictionError);

            // Trim history
            if (_errorHistory.Count > maxHistorySize)
            {
                _errorHistory.RemoveAt(0);
            }

            _errorsAboveThreshold++;

            if (logPredictionErrors)
            {
                Debug.LogWarning($"[PredictionDebugger] Prediction error: Entity {entityId}, Error: {error:F3}m");
            }
        }

        private void UpdateErrorStatistics()
        {
            if (_errorHistory.Count == 0) return;

            // Calculate average error
            _averageError = _errorHistory.Average(e => e.error);

            // Find max error
            _maxError = _errorHistory.Max(e => e.error);
        }

        #endregion

        #region Visualization

        private void DrawPredictionGizmos(PredictionState state)
        {
            // Draw predicted position
            Gizmos.color = predictedColor;
            Gizmos.DrawWireSphere(state.predictedPosition, 0.3f);

            // Draw server position
            Gizmos.color = serverColor;
            Gizmos.DrawWireSphere(state.serverPosition, 0.2f);

            // Draw error line
            if (state.predictionError > errorThreshold)
            {
                Gizmos.color = errorColor;
                Gizmos.DrawLine(state.predictedPosition, state.serverPosition);
            }

            // Draw velocity vectors
            Gizmos.color = predictedColor;
            Gizmos.DrawLine(state.predictedPosition, state.predictedPosition + state.predictedVelocity * 0.5f);

            Gizmos.color = serverColor;
            Gizmos.DrawLine(state.serverPosition, state.serverPosition + state.serverVelocity * 0.5f);
        }

        private void DrawDebugWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // Statistics
            GUILayout.Label("Statistics", GUI.skin.box);
            GUILayout.Label($"Tracked Entities: {_entityStates.Count}");
            GUILayout.Label($"Total Predictions: {_totalPredictions}");
            GUILayout.Label($"Total Corrections: {_totalCorrections}");
            GUILayout.Label($"Errors > Threshold: {_errorsAboveThreshold}");
            GUILayout.Label($"Average Error: {_averageError:F3}m");
            GUILayout.Label($"Max Error: {_maxError:F3}m");

            GUILayout.Space(10);

            // Controls
            GUILayout.Label("Controls", GUI.skin.box);
            visualizeErrors = GUILayout.Toggle(visualizeErrors, "Visualize Errors");
            drawGizmos = GUILayout.Toggle(drawGizmos, "Draw Gizmos");
            logPredictionErrors = GUILayout.Toggle(logPredictionErrors, "Log Errors");

            GUILayout.Space(10);

            // Entity list
            GUILayout.Label("Tracked Entities", GUI.skin.box);
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));

            foreach (var kvp in _entityStates)
            {
                var state = kvp.Value;
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"Entity: {state.entityId}");
                GUILayout.Label($"  Error: {state.predictionError:F3}m");
                GUILayout.Label($"  Reconciliations: {state.reconciliationCount}");
                GUILayout.Label($"  Last Update: {Time.time - state.lastUpdateTime:F1}s ago");
                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Actions
            if (GUILayout.Button("Clear History"))
            {
                ClearHistory();
            }

            if (GUILayout.Button("Export Report"))
            {
                ExportReport();
            }

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get prediction statistics.
        /// </summary>
        public PredictionStats GetStats()
        {
            return new PredictionStats
            {
                trackedEntities = _entityStates.Count,
                totalPredictions = _totalPredictions,
                totalCorrections = _totalCorrections,
                errorsAboveThreshold = _errorsAboveThreshold,
                averageError = _averageError,
                maxError = _maxError,
                errorHistory = _errorHistory.Count
            };
        }

        /// <summary>
        /// Get errors for a specific entity.
        /// </summary>
        public List<PredictionError> GetEntityErrors(ulong entityId)
        {
            return _errorHistory.Where(e => e.entityId == entityId).ToList();
        }

        /// <summary>
        /// Clear error history.
        /// </summary>
        public void ClearHistory()
        {
            _errorHistory.Clear();
            _errorsAboveThreshold = 0;
            _averageError = 0f;
            _maxError = 0f;

            Debug.Log("[PredictionDebugger] History cleared");
        }

        /// <summary>
        /// Export debugging report.
        /// </summary>
        public void ExportReport()
        {
            var report = GenerateReport();
            string path = System.IO.Path.Combine(Application.persistentDataPath, $"prediction_debug_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            System.IO.File.WriteAllText(path, report);

            Debug.Log($"[PredictionDebugger] Report exported to: {path}");
        }

        private string GenerateReport()
        {
            var stats = GetStats();
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== Prediction Debugging Report ===");
            sb.AppendLine($"Generated: {DateTime.Now}");
            sb.AppendLine();

            sb.AppendLine("Statistics:");
            sb.AppendLine($"  Tracked Entities: {stats.trackedEntities}");
            sb.AppendLine($"  Total Predictions: {stats.totalPredictions}");
            sb.AppendLine($"  Total Corrections: {stats.totalCorrections}");
            sb.AppendLine($"  Errors Above Threshold: {stats.errorsAboveThreshold}");
            sb.AppendLine($"  Average Error: {stats.averageError:F3}m");
            sb.AppendLine($"  Max Error: {stats.maxError:F3}m");
            sb.AppendLine();

            sb.AppendLine("Entity Details:");
            foreach (var kvp in _entityStates)
            {
                var state = kvp.Value;
                sb.AppendLine($"  Entity {state.entityId}:");
                sb.AppendLine($"    Prediction Error: {state.predictionError:F3}m");
                sb.AppendLine($"    Reconciliations: {state.reconciliationCount}");
                sb.AppendLine($"    Last Update: {Time.time - state.lastUpdateTime:F1}s ago");
            }
            sb.AppendLine();

            sb.AppendLine("Recent Errors:");
            var recentErrors = _errorHistory.Skip(Math.Max(0, _errorHistory.Count - 20)).ToList();
            foreach (var error in recentErrors)
            {
                sb.AppendLine($"  Entity {error.entityId}: {error.error:F3}m at {error.timestamp:F1}s");
            }

            return sb.ToString();
        }

        #endregion

        #region Context Menu

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            Debug.Log(GenerateReport());
        }

        [ContextMenu("Clear History")]
        private void ClearHistoryMenu()
        {
            ClearHistory();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Prediction state for an entity.
    /// </summary>
    [Serializable]
    public class PredictionState
    {
        public ulong entityId;
        public Vector3 predictedPosition;
        public Vector3 predictedVelocity;
        public Vector3 serverPosition;
        public Vector3 serverVelocity;
        public Vector3 previousServerPosition;
        public float serverTimestamp;
        public float predictionError;
        public float lastPredictionTime;
        public float lastUpdateTime;
        public int reconciliationCount;
        public float lastReconciliationTime;
        public Vector3 lastCorrectionDelta;
    }

    /// <summary>
    /// A recorded prediction error.
    /// </summary>
    [Serializable]
    public struct PredictionError
    {
        public ulong entityId;
        public Vector3 predictedPosition;
        public Vector3 serverPosition;
        public float error;
        public float timestamp;
    }

    /// <summary>
    /// Prediction debugging statistics.
    /// </summary>
    [Serializable]
    public struct PredictionStats
    {
        public int trackedEntities;
        public int totalPredictions;
        public int totalCorrections;
        public int errorsAboveThreshold;
        public float averageError;
        public float maxError;
        public int errorHistory;
    }

    #endregion
}
