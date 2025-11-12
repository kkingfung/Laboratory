using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;

namespace Laboratory.Subsystems.Monitoring
{
    /// <summary>
    /// Concrete implementation of log aggregation service
    /// Handles log collection, filtering, storage, and export
    /// </summary>
    public class LogAggregationService : ILogAggregationService
    {
        #region Fields

        private readonly DebugSubsystemConfig _config;
        private List<DebugLogEntry> _logEntries;
        private Dictionary<DebugLogType, List<DebugLogEntry>> _logsByType;
        private DebugLogType _minimumLogLevel;
        private string _logFilePath;
        private StringBuilder _logBuffer;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public LogAggregationService(DebugSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region ILogAggregationService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _logEntries = new List<DebugLogEntry>();
                _logsByType = new Dictionary<DebugLogType, List<DebugLogEntry>>();
                _minimumLogLevel = DebugLogType.Info;
                _logBuffer = new StringBuilder();

                // Initialize log type collections
                foreach (DebugLogType logType in Enum.GetValues(typeof(DebugLogType)))
                {
                    _logsByType[logType] = new List<DebugLogEntry>();
                }

                // Setup log file path
                if (_config.enableLogFileOutput)
                {
                    SetupLogFilePath();
                }

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log("[LogAggregationService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[LogAggregationService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void AddLogEntry(DebugLogEntry entry)
        {
            if (!_isInitialized || entry == null)
                return;

            // Check log level filtering
            if (!ShouldLogEntry(entry))
                return;

            // Add to main collection
            _logEntries.Add(entry);

            // Add to type-specific collection
            var debugLogType = ConvertToDebugLogType(entry.logType);
            if (_logsByType.ContainsKey(debugLogType))
            {
                _logsByType[debugLogType].Add(entry);
            }

            // Keep collection size manageable
            TrimLogCollections();

            // Add to buffer for file output
            if (_config.enableLogFileOutput)
            {
                AddToLogBuffer(entry);
            }
        }

        public List<DebugLogEntry> GetLogEntries(DebugLogType logType, int count = 100)
        {
            if (!_isInitialized)
                return new List<DebugLogEntry>();

            if (!_logsByType.TryGetValue(logType, out var entries))
                return new List<DebugLogEntry>();

            var result = new List<DebugLogEntry>();
            var startIndex = Math.Max(0, entries.Count - count);

            for (int i = startIndex; i < entries.Count; i++)
            {
                result.Add(entries[i]);
            }

            return result;
        }

        public List<DebugLogEntry> GetLogEntriesInTimeRange(DateTime start, DateTime end)
        {
            if (!_isInitialized)
                return new List<DebugLogEntry>();

            return _logEntries
                .Where(entry => entry.timestamp >= start && entry.timestamp <= end)
                .ToList();
        }

        public void ClearLogs()
        {
            if (!_isInitialized)
                return;

            _logEntries.Clear();

            foreach (var collection in _logsByType.Values)
            {
                collection.Clear();
            }

            _logBuffer.Clear();

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log("[LogAggregationService] Logs cleared");
        }

        public void FlushLogs()
        {
            if (!_isInitialized || !_config.enableLogFileOutput)
                return;

            try
            {
                if (_logBuffer.Length > 0 && !string.IsNullOrEmpty(_logFilePath))
                {
                    File.AppendAllText(_logFilePath, _logBuffer.ToString());
                    _logBuffer.Clear();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[LogAggregationService] Failed to flush logs: {ex.Message}");
            }
        }

        public void ExportLogs(string filePath)
        {
            if (!_isInitialized || string.IsNullOrEmpty(filePath))
                return;

            try
            {
                var exportContent = new StringBuilder();

                // Add header
                exportContent.AppendLine("=== Laboratory Debug Logs Export ===");
                exportContent.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                exportContent.AppendLine($"Total Entries: {_logEntries.Count}");
                exportContent.AppendLine();

                // Add log entries
                foreach (var entry in _logEntries)
                {
                    exportContent.AppendLine(FormatLogEntry(entry));
                }

                File.WriteAllText(filePath, exportContent.ToString());

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log($"[LogAggregationService] Logs exported to: {filePath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[LogAggregationService] Failed to export logs: {ex.Message}");
            }
        }

        public void SetLogLevel(DebugLogType minimumLevel)
        {
            if (!_isInitialized)
                return;

            _minimumLogLevel = minimumLevel;

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[LogAggregationService] Set minimum log level to: {minimumLevel}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets log statistics
        /// </summary>
        public LogStatistics GetLogStatistics()
        {
            if (!_isInitialized)
                return new LogStatistics();

            var stats = new LogStatistics
            {
                totalEntries = _logEntries.Count,
                entriesByType = new Dictionary<DebugLogType, int>()
            };

            foreach (var kvp in _logsByType)
            {
                stats.entriesByType[kvp.Key] = kvp.Value.Count;
            }

            if (_logEntries.Count > 0)
            {
                stats.oldestEntry = _logEntries[0].timestamp;
                stats.newestEntry = _logEntries[_logEntries.Count - 1].timestamp;
            }

            return stats;
        }

        /// <summary>
        /// Searches logs by text content
        /// </summary>
        public List<DebugLogEntry> SearchLogs(string searchTerm, bool caseSensitive = false)
        {
            if (!_isInitialized || string.IsNullOrEmpty(searchTerm))
                return new List<DebugLogEntry>();

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            return _logEntries
                .Where(entry => entry.message.IndexOf(searchTerm, comparison) >= 0 ||
                               (!string.IsNullOrEmpty(entry.category) && entry.category.IndexOf(searchTerm, comparison) >= 0))
                .ToList();
        }

        #endregion

        #region Private Methods

        private void SetupLogFilePath()
        {
            try
            {
                var logDirectory = Path.Combine(Application.persistentDataPath, _config.logOutputDirectory);

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                var fileName = $"debug_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                _logFilePath = Path.Combine(logDirectory, fileName);

                // Write initial header
                var header = new StringBuilder();
                header.AppendLine("=== Laboratory Debug Log ===");
                header.AppendLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                header.AppendLine($"Unity Version: {Application.unityVersion}");
                header.AppendLine($"Platform: {Application.platform}");
                header.AppendLine($"Product Name: {Application.productName}");
                header.AppendLine();

                File.WriteAllText(_logFilePath, header.ToString());
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[LogAggregationService] Failed to setup log file: {ex.Message}");
                _logFilePath = null;
            }
        }

        private bool ShouldLogEntry(DebugLogEntry entry)
        {
            // Check minimum log level
            var debugLogType = ConvertToDebugLogType(entry.logType);
            if (GetLogLevelPriority(debugLogType) < GetLogLevelPriority(_minimumLogLevel))
                return false;

            return true;
        }

        private int GetLogLevelPriority(DebugLogType logType)
        {
            return logType switch
            {
                DebugLogType.Verbose => 0,
                DebugLogType.Debug => 1,
                DebugLogType.Info => 2,
                DebugLogType.Warning => 3,
                DebugLogType.Error => 4,
                DebugLogType.Performance => 2,
                DebugLogType.Network => 2,
                DebugLogType.AI => 2,
                DebugLogType.Genetics => 2,
                DebugLogType.System => 3,
                _ => 2
            };
        }

        private void TrimLogCollections()
        {
            // Trim main collection
            while (_logEntries.Count > _config.maxLogEntries)
            {
                var removedEntry = _logEntries[0];
                _logEntries.RemoveAt(0);

                // Also remove from type-specific collection
                var debugLogType = ConvertToDebugLogType(removedEntry.logType);
                if (_logsByType.ContainsKey(debugLogType))
                {
                    _logsByType[debugLogType].Remove(removedEntry);
                }
            }
        }

        private void AddToLogBuffer(DebugLogEntry entry)
        {
            _logBuffer.AppendLine(FormatLogEntry(entry));

            // Auto-flush if buffer gets too large
            if (_logBuffer.Length > 10000) // 10KB
            {
                FlushLogs();
            }
        }

        private string FormatLogEntry(DebugLogEntry entry)
        {
            var formatted = new StringBuilder();

            formatted.Append($"[{entry.timestamp:yyyy-MM-dd HH:mm:ss.fff}]");
            formatted.Append($" [{entry.logType}]");

            if (!string.IsNullOrEmpty(entry.category))
                formatted.Append($" [{entry.category}]");

            if (entry.frameNumber > 0)
                formatted.Append($" [Frame:{entry.frameNumber}]");

            formatted.Append($" {entry.message}");

            if (!string.IsNullOrEmpty(entry.stackTrace))
            {
                formatted.AppendLine();
                formatted.Append($"Stack Trace: {entry.stackTrace}");
            }

            return formatted.ToString();
        }

        /// <summary>
        /// Converts Unity LogType to our custom DebugLogType
        /// </summary>
        private DebugLogType ConvertToDebugLogType(UnityEngine.LogType unityLogType)
        {
            return unityLogType switch
            {
                UnityEngine.LogType.Log => DebugLogType.Info,
                UnityEngine.LogType.Warning => DebugLogType.Warning,
                UnityEngine.LogType.Error => DebugLogType.Error,
                UnityEngine.LogType.Exception => DebugLogType.Error,
                UnityEngine.LogType.Assert => DebugLogType.Debug,
                _ => DebugLogType.Debug
            };
        }

        #endregion

        #region Helper Classes

        public class LogStatistics
        {
            public int totalEntries;
            public Dictionary<DebugLogType, int> entriesByType = new();
            public DateTime oldestEntry;
            public DateTime newestEntry;
        }

        #endregion
    }
}