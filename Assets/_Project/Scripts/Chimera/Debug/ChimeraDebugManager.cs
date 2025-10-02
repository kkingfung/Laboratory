using UnityEngine;
using System;

namespace Laboratory.Chimera.Debug
{
    /// <summary>
    /// Centralized debug manager for Chimera systems
    /// Provides logging, error handling, and debug utilities
    /// </summary>
    public static class DebugManager
    {
        private static bool _isEnabled = true;
        private static bool _isVerboseMode = false;

        public static bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public static bool IsVerboseMode
        {
            get => _isVerboseMode;
            set => _isVerboseMode = value;
        }

        /// <summary>
        /// Log standard debug message
        /// </summary>
        public static void Log(string message, UnityEngine.Object context = null)
        {
            if (!_isEnabled) return;
            UnityEngine.Debug.Log($"[Chimera] {message}", context);
        }

        /// <summary>
        /// Log verbose debug message (only shown in verbose mode)
        /// </summary>
        public static void LogVerbose(string message, UnityEngine.Object context = null)
        {
            if (!_isEnabled || !_isVerboseMode) return;
            UnityEngine.Debug.Log($"[Chimera-Verbose] {message}", context);
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public static void LogWarning(string message, UnityEngine.Object context = null)
        {
            if (!_isEnabled) return;
            UnityEngine.Debug.LogWarning($"[Chimera-Warning] {message}", context);
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public static void LogError(string message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogError($"[Chimera-Error] {message}", context);
        }

        /// <summary>
        /// Log exception
        /// </summary>
        public static void LogException(Exception exception, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogException(exception, context);
        }

        /// <summary>
        /// Assert condition with custom message
        /// </summary>
        public static void Assert(bool condition, string message = "", UnityEngine.Object context = null)
        {
            if (!condition)
            {
                LogError($"Assertion failed: {message}", context);
            }
        }
    }

    /// <summary>
    /// Simplified logging utilities for backwards compatibility
    /// </summary>
    public static class Log
    {
        public static void Info(string message) => DebugManager.Log(message);
        public static void Warning(string message) => DebugManager.LogWarning(message);
        public static void Error(string message) => DebugManager.LogError(message);
        public static void Verbose(string message) => DebugManager.LogVerbose(message);
    }

    /// <summary>
    /// Error utilities for backwards compatibility
    /// </summary>
    public static class LogError
    {
        public static void Message(string message) => DebugManager.LogError(message);
    }
}