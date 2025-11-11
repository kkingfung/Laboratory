using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.Debug
{
    /// <summary>
    /// Concrete implementation of debug visualization service
    /// Handles rendering debug information, gizmos, and visual debugging aids
    /// </summary>
    public class DebugVisualizationService : IDebugVisualizationService
    {
        #region Fields

        private readonly DebugSubsystemConfig _config;
        private List<DebugVisualizationData> _activeVisualizations;
        private List<DebugVisualizationData> _persistentVisualizations;
        private Dictionary<VisualizationType, Action<DebugVisualizationData>> _visualizationRenderers;
        private bool _isInitialized;
        private bool _visualizationEnabled;

        #endregion

        #region Constructor

        public DebugVisualizationService(DebugSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IDebugVisualizationService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _activeVisualizations = new List<DebugVisualizationData>();
                _persistentVisualizations = new List<DebugVisualizationData>();
                _visualizationRenderers = new Dictionary<VisualizationType, Action<DebugVisualizationData>>();
                _visualizationEnabled = _config.showDebugGizmos;

                // Register default visualization renderers
                RegisterDefaultRenderers();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log("[DebugVisualizationService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DebugVisualizationService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void DrawVisualization(DebugVisualizationData data)
        {
            if (!_isInitialized || !_visualizationEnabled || data == null)
                return;

            // Set timestamp if not set
            if (data.duration <= 0f)
                data.duration = 1f;

            // Add to appropriate collection
            if (data.isPersistent)
            {
                _persistentVisualizations.Add(data);
            }
            else
            {
                _activeVisualizations.Add(data);
            }
        }

        public void UpdateVisualizations()
        {
            if (!_isInitialized || !_visualizationEnabled)
                return;

            var currentTime = Time.time;

            // Update active visualizations
            for (int i = _activeVisualizations.Count - 1; i >= 0; i--)
            {
                var visualization = _activeVisualizations[i];

                // Check if visualization has expired
                if (currentTime - visualization.duration > 1f) // Simple duration check
                {
                    _activeVisualizations.RemoveAt(i);
                    continue;
                }

                // Render visualization
                RenderVisualization(visualization);
            }

            // Render persistent visualizations
            foreach (var visualization in _persistentVisualizations)
            {
                RenderVisualization(visualization);
            }
        }

        public void ClearVisualizations()
        {
            if (!_isInitialized)
                return;

            _activeVisualizations.Clear();
            _persistentVisualizations.Clear();

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log("[DebugVisualizationService] Visualizations cleared");
        }

        public void SetVisualizationEnabled(bool enabled)
        {
            if (!_isInitialized)
                return;

            _visualizationEnabled = enabled;

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[DebugVisualizationService] Visualization {(enabled ? "enabled" : "disabled")}");
        }

        public void RegisterVisualizationRenderer(VisualizationType type, Action<DebugVisualizationData> renderer)
        {
            if (!_isInitialized || renderer == null)
                return;

            _visualizationRenderers[type] = renderer;

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[DebugVisualizationService] Registered custom renderer for: {type}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Draws a debug point at the specified position
        /// </summary>
        public void DrawPoint(Vector3 position, Color color, float duration = 1f, bool persistent = false)
        {
            var data = new DebugVisualizationData
            {
                visualizationName = "DebugPoint",
                visualizationType = VisualizationType.Point,
                worldPosition = position,
                color = color,
                duration = duration,
                isPersistent = persistent
            };

            data.data["radius"] = 0.1f;
            DrawVisualization(data);
        }

        /// <summary>
        /// Draws a debug line between two points
        /// </summary>
        public void DrawLine(Vector3 from, Vector3 to, Color color, float duration = 1f, bool persistent = false)
        {
            var data = new DebugVisualizationData
            {
                visualizationName = "DebugLine",
                visualizationType = VisualizationType.Line,
                worldPosition = from,
                color = color,
                duration = duration,
                isPersistent = persistent
            };

            data.data["endPosition"] = to;
            DrawVisualization(data);
        }

        /// <summary>
        /// Draws a debug sphere at the specified position
        /// </summary>
        public void DrawSphere(Vector3 position, float radius, Color color, float duration = 1f, bool persistent = false)
        {
            var data = new DebugVisualizationData
            {
                visualizationName = "DebugSphere",
                visualizationType = VisualizationType.Sphere,
                worldPosition = position,
                color = color,
                duration = duration,
                isPersistent = persistent
            };

            data.data["radius"] = radius;
            DrawVisualization(data);
        }

        /// <summary>
        /// Draws debug text at the specified position
        /// </summary>
        public void DrawText(Vector3 position, string text, Color color, float duration = 1f, bool persistent = false)
        {
            var data = new DebugVisualizationData
            {
                visualizationName = "DebugText",
                visualizationType = VisualizationType.Text,
                worldPosition = position,
                color = color,
                duration = duration,
                isPersistent = persistent
            };

            data.data["text"] = text;
            data.data["fontSize"] = 12;
            DrawVisualization(data);
        }

        /// <summary>
        /// Draws a debug arrow from one position to another
        /// </summary>
        public void DrawArrow(Vector3 from, Vector3 to, Color color, float duration = 1f, bool persistent = false)
        {
            var data = new DebugVisualizationData
            {
                visualizationName = "DebugArrow",
                visualizationType = VisualizationType.Arrow,
                worldPosition = from,
                color = color,
                duration = duration,
                isPersistent = persistent
            };

            data.data["direction"] = (to - from).normalized;
            data.data["length"] = Vector3.Distance(from, to);
            data.data["arrowHeadSize"] = 0.2f;
            DrawVisualization(data);
        }

        #endregion

        #region Private Methods

        private void RegisterDefaultRenderers()
        {
            _visualizationRenderers[VisualizationType.Point] = RenderPoint;
            _visualizationRenderers[VisualizationType.Line] = RenderLine;
            _visualizationRenderers[VisualizationType.Sphere] = RenderSphere;
            _visualizationRenderers[VisualizationType.Cube] = RenderCube;
            _visualizationRenderers[VisualizationType.Arrow] = RenderArrow;
            _visualizationRenderers[VisualizationType.Text] = RenderText;
            _visualizationRenderers[VisualizationType.Wireframe] = RenderWireframe;
        }

        private void RenderVisualization(DebugVisualizationData data)
        {
            if (_visualizationRenderers.TryGetValue(data.visualizationType, out var renderer))
            {
                try
                {
                    renderer(data);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[DebugVisualizationService] Error rendering {data.visualizationType}: {ex.Message}");
                }
            }
        }

        private void RenderPoint(DebugVisualizationData data)
        {
            var radius = data.data.TryGetValue("radius", out var r) ? (float)r : 0.1f;

            // Draw as a small sphere using Debug.DrawRay to simulate
            var position = data.worldPosition;

            // Draw rays in multiple directions to create a point visualization
            UnityEngine.Debug.DrawRay(position, Vector3.up * radius, data.color);
            UnityEngine.Debug.DrawRay(position, Vector3.down * radius, data.color);
            UnityEngine.Debug.DrawRay(position, Vector3.left * radius, data.color);
            UnityEngine.Debug.DrawRay(position, Vector3.right * radius, data.color);
            UnityEngine.Debug.DrawRay(position, Vector3.forward * radius, data.color);
            UnityEngine.Debug.DrawRay(position, Vector3.back * radius, data.color);
        }

        private void RenderLine(DebugVisualizationData data)
        {
            var endPosition = data.data.TryGetValue("endPosition", out var end) ? (Vector3)end : data.worldPosition + Vector3.forward;
            UnityEngine.Debug.DrawLine(data.worldPosition, endPosition, data.color);
        }

        private void RenderSphere(DebugVisualizationData data)
        {
            var radius = data.data.TryGetValue("radius", out var r) ? (float)r : 1f;
            var center = data.worldPosition;

            // Draw wireframe sphere using multiple circles
            DrawWireCircle(center, Vector3.up, radius, data.color, 16);
            DrawWireCircle(center, Vector3.right, radius, data.color, 16);
            DrawWireCircle(center, Vector3.forward, radius, data.color, 16);
        }

        private void RenderCube(DebugVisualizationData data)
        {
            var size = data.data.TryGetValue("size", out var s) ? (float)s : 1f;
            var center = data.worldPosition;
            var halfSize = size * 0.5f;

            // Draw wireframe cube
            var vertices = new Vector3[8];
            vertices[0] = center + new Vector3(-halfSize, -halfSize, -halfSize);
            vertices[1] = center + new Vector3(halfSize, -halfSize, -halfSize);
            vertices[2] = center + new Vector3(halfSize, halfSize, -halfSize);
            vertices[3] = center + new Vector3(-halfSize, halfSize, -halfSize);
            vertices[4] = center + new Vector3(-halfSize, -halfSize, halfSize);
            vertices[5] = center + new Vector3(halfSize, -halfSize, halfSize);
            vertices[6] = center + new Vector3(halfSize, halfSize, halfSize);
            vertices[7] = center + new Vector3(-halfSize, halfSize, halfSize);

            // Draw edges
            // Bottom face
            UnityEngine.Debug.DrawLine(vertices[0], vertices[1], data.color);
            UnityEngine.Debug.DrawLine(vertices[1], vertices[2], data.color);
            UnityEngine.Debug.DrawLine(vertices[2], vertices[3], data.color);
            UnityEngine.Debug.DrawLine(vertices[3], vertices[0], data.color);

            // Top face
            UnityEngine.Debug.DrawLine(vertices[4], vertices[5], data.color);
            UnityEngine.Debug.DrawLine(vertices[5], vertices[6], data.color);
            UnityEngine.Debug.DrawLine(vertices[6], vertices[7], data.color);
            UnityEngine.Debug.DrawLine(vertices[7], vertices[4], data.color);

            // Vertical edges
            UnityEngine.Debug.DrawLine(vertices[0], vertices[4], data.color);
            UnityEngine.Debug.DrawLine(vertices[1], vertices[5], data.color);
            UnityEngine.Debug.DrawLine(vertices[2], vertices[6], data.color);
            UnityEngine.Debug.DrawLine(vertices[3], vertices[7], data.color);
        }

        private void RenderArrow(DebugVisualizationData data)
        {
            var direction = data.data.TryGetValue("direction", out var dir) ? (Vector3)dir : Vector3.forward;
            var length = data.data.TryGetValue("length", out var len) ? (float)len : 1f;
            var arrowHeadSize = data.data.TryGetValue("arrowHeadSize", out var head) ? (float)head : 0.2f;

            var from = data.worldPosition;
            var to = from + direction * length;

            // Draw main line
            UnityEngine.Debug.DrawLine(from, to, data.color);

            // Draw arrow head
            var right = Vector3.Cross(direction, Vector3.up).normalized * arrowHeadSize;
            var up = Vector3.Cross(right, direction).normalized * arrowHeadSize;

            UnityEngine.Debug.DrawLine(to, to - direction * arrowHeadSize + right, data.color);
            UnityEngine.Debug.DrawLine(to, to - direction * arrowHeadSize - right, data.color);
            UnityEngine.Debug.DrawLine(to, to - direction * arrowHeadSize + up, data.color);
            UnityEngine.Debug.DrawLine(to, to - direction * arrowHeadSize - up, data.color);
        }

        private void RenderText(DebugVisualizationData data)
        {
            // Text rendering would require a more complex implementation
            // For now, we'll just indicate text position with a small cross
            var position = data.worldPosition;
            var size = 0.1f;

            UnityEngine.Debug.DrawLine(position - Vector3.right * size, position + Vector3.right * size, data.color);
            UnityEngine.Debug.DrawLine(position - Vector3.up * size, position + Vector3.up * size, data.color);

            // In a full implementation, this would render actual text using GUI or TextMesh
        }

        private void RenderWireframe(DebugVisualizationData data)
        {
            // Generic wireframe rendering - defaults to cube
            RenderCube(data);
        }

        private void DrawWireCircle(Vector3 center, Vector3 normal, float radius, Color color, int segments)
        {
            var angle = 0f;
            var angleStep = 360f / segments;
            var right = Vector3.Cross(normal, Vector3.up).normalized;
            if (right == Vector3.zero)
                right = Vector3.Cross(normal, Vector3.forward).normalized;
            var up = Vector3.Cross(normal, right).normalized;

            var prevPoint = center + right * radius;

            for (int i = 1; i <= segments; i++)
            {
                angle = i * angleStep * Mathf.Deg2Rad;
                var x = Mathf.Cos(angle) * radius;
                var y = Mathf.Sin(angle) * radius;
                var currentPoint = center + right * x + up * y;

                UnityEngine.Debug.DrawLine(prevPoint, currentPoint, color);
                prevPoint = currentPoint;
            }
        }

        #endregion
    }
}