using System;
using UnityEngine;

namespace Laboratory.Core.Diagnostics
{
    /// <summary>
    /// Diagnostic severity levels
    /// </summary>
    public enum DiagnosticLevel : byte
    {
        /// <summary>Informational message</summary>
        Info = 0,
        /// <summary>Warning message</summary>
        Warning = 1,
        /// <summary>Error message</summary>
        Error = 2,
        /// <summary>Critical error message</summary>
        Critical = 3,
        /// <summary>Debug-only message</summary>
        Debug = 4
    }

    /// <summary>
    /// Performance diagnostic categories
    /// </summary>
    public enum DiagnosticCategory : byte
    {
        /// <summary>General performance</summary>
        Performance = 0,
        /// <summary>Memory usage</summary>
        Memory = 1,
        /// <summary>Rendering performance</summary>
        Rendering = 2,
        /// <summary>AI system performance</summary>
        AI = 3,
        /// <summary>Network performance</summary>
        Network = 4,
        /// <summary>Physics performance</summary>
        Physics = 5,
        /// <summary>Audio performance</summary>
        Audio = 6
    }

    /// <summary>
    /// Diagnostic message structure
    /// </summary>
    [System.Serializable]
    public struct DiagnosticMessage
    {
        /// <summary>Message content</summary>
        public string Message;

        /// <summary>Severity level</summary>
        public DiagnosticLevel Level;

        /// <summary>Category of diagnostic</summary>
        public DiagnosticCategory Category;

        /// <summary>Timestamp when message was created</summary>
        public DateTime Timestamp;

        /// <summary>Source of the diagnostic</summary>
        public string Source;

        public DiagnosticMessage(string message, DiagnosticLevel level, DiagnosticCategory category, string source = "")
        {
            Message = message;
            Level = level;
            Category = category;
            Timestamp = DateTime.Now;
            Source = source;
        }
    }

    /// <summary>
    /// Simple diagnostics logger for performance monitoring
    /// </summary>
    public static class DiagnosticsLogger
    {
        /// <summary>Log a diagnostic message</summary>
        public static void Log(string message, DiagnosticLevel level = DiagnosticLevel.Info, DiagnosticCategory category = DiagnosticCategory.Performance, string source = "")
        {
            var diagnostic = new DiagnosticMessage(message, level, category, source);

            switch (level)
            {
                case DiagnosticLevel.Info:
                case DiagnosticLevel.Debug:
                    Debug.Log($"[{category}] {message}");
                    break;
                case DiagnosticLevel.Warning:
                    Debug.LogWarning($"[{category}] {message}");
                    break;
                case DiagnosticLevel.Error:
                case DiagnosticLevel.Critical:
                    Debug.LogError($"[{category}] {message}");
                    break;
            }
        }

        /// <summary>Log performance timing</summary>
        public static void LogTiming(string operation, float timeMs, string source = "")
        {
            Log($"{operation} took {timeMs:F2}ms", DiagnosticLevel.Info, DiagnosticCategory.Performance, source);
        }

        /// <summary>Log memory usage</summary>
        public static void LogMemory(string context, long bytesUsed, string source = "")
        {
            Log($"{context}: {bytesUsed / 1024}KB used", DiagnosticLevel.Info, DiagnosticCategory.Memory, source);
        }
    }
}