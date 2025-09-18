using UnityEngine;

namespace Laboratory.Tools
{
    /// <summary>
    /// Utility tools for development and debugging
    /// </summary>
    public static class DebugTools
    {
        /// <summary>
        /// Draws a debug ray in the scene view
        /// </summary>
        public static void DrawDebugRay(Vector3 origin, Vector3 direction, Color color, float duration = 0.0f)
        {
            Debug.DrawRay(origin, direction, color, duration);
        }

        /// <summary>
        /// Logs a formatted debug message
        /// </summary>
        public static void LogFormatted(string category, string message, LogType logType = LogType.Log)
        {
            string formattedMessage = $"[{category}] {message}";
            
            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError(formattedMessage);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                default:
                    Debug.Log(formattedMessage);
                    break;
            }
        }

        /// <summary>
        /// Draws a wireframe sphere in the scene view
        /// </summary>
        public static void DrawWireSphere(Vector3 center, float radius, Color color, float duration = 0.0f)
        {
            // Simple wire sphere implementation using Debug.DrawLine
            int segments = 16;
            float angleStep = 360f / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle1 = Mathf.Deg2Rad * (i * angleStep);
                float angle2 = Mathf.Deg2Rad * ((i + 1) * angleStep);
                
                Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
                Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
                
                Debug.DrawLine(point1, point2, color, duration);
            }
        }
    }

    /// <summary>
    /// Performance profiling tools
    /// </summary>
    public static class ProfilerTools
    {
        /// <summary>
        /// Simple timer for measuring performance
        /// </summary>
        public class SimpleTimer
        {
            private float startTime;
            private string label;

            public SimpleTimer(string label)
            {
                this.label = label;
                startTime = Time.realtimeSinceStartup;
            }

            public void Stop()
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                Debug.Log($"[Timer] {label}: {elapsed * 1000:F2}ms");
            }
        }

        /// <summary>
        /// Creates a new timer with the given label
        /// </summary>
        public static SimpleTimer StartTimer(string label)
        {
            return new SimpleTimer(label);
        }
    }
}