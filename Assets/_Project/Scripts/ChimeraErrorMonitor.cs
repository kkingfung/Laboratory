using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectChimera.ErrorHandling
{
    /// <summary>
    /// Advanced error monitoring and prevention system for Project Chimera
    /// Catches errors before they crash our monster breeding paradise!
    /// </summary>
    public class ChimeraErrorMonitor : MonoBehaviour
    {
        [Header("🛡️ Error Monitoring Settings")]
        [SerializeField] private bool enableErrorCapture = true;
        [SerializeField] private bool logToFile = true;
        [SerializeField] private bool showErrorNotifications = true;
        [SerializeField] private int maxStoredErrors = 100;
        
        [Header("📊 Performance Monitoring")]
        [SerializeField] private bool monitorFrameRate = true;
        [SerializeField] private float fpsWarningThreshold = 30f;
        [SerializeField] private bool monitorMemory = true;
        [SerializeField] private long memoryWarningThresholdMB = 512;

        private static ChimeraErrorMonitor _instance;
        public static ChimeraErrorMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ChimeraErrorMonitor>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[ChimeraErrorMonitor]");
                        _instance = go.AddComponent<ChimeraErrorMonitor>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Error tracking
        private List<ErrorReport> storedErrors = new List<ErrorReport>();
        private Dictionary<string, int> errorFrequency = new Dictionary<string, int>();
        private string logFilePath;
        
        // Performance tracking
        private float[] frameTimeBuffer = new float[60]; // Last 60 frames
        private int frameBufferIndex = 0;
        private float lastMemoryCheck = 0f;
        private const float memoryCheckInterval = 5f;

        // Events
        public static event Action<ErrorReport> OnErrorCaptured;
        public static event Action<string> OnPerformanceWarning;

        #region Unity Lifecycle

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeErrorMonitoring();
        }

        void Update()
        {
            if (monitorFrameRate)
            {
                MonitorFrameRate();
            }

            if (monitorMemory && Time.time - lastMemoryCheck > memoryCheckInterval)
            {
                MonitorMemoryUsage();
                lastMemoryCheck = Time.time;
            }
        }

        void OnDestroy()
        {
            if (this == _instance)
            {
                DisableErrorCapture();
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        private void InitializeErrorMonitoring()
        {
            try
            {
                Debug.Log("🛡️ Initializing Chimera Error Monitor...");
                
                // Set up log file
                if (logToFile)
                {
                    string logDirectory = Path.Combine(Application.persistentDataPath, "ChimeraLogs");
                    if (!Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }
                    
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    logFilePath = Path.Combine(logDirectory, $"chimera_errors_{timestamp}.log");
                    
                    WriteToLogFile($"=== Project Chimera Error Log Started at {DateTime.Now} ===");
                }
                
                // Enable error capture
                if (enableErrorCapture)
                {
                    EnableErrorCapture();
                }
                
                Debug.Log("✅ Error Monitor initialized successfully!");
                Debug.Log($"📁 Log file: {logFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to initialize error monitor: {e.Message}");
            }
        }

        private void EnableErrorCapture()
        {
            try
            {
                Application.logMessageReceived += HandleLog;
                Debug.Log("🎯 Error capture enabled");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to enable error capture: {e.Message}");
            }
        }

        private void DisableErrorCapture()
        {
            try
            {
                Application.logMessageReceived -= HandleLog;
                
                if (logToFile && !string.IsNullOrEmpty(logFilePath))
                {
                    WriteToLogFile($"=== Error Log Ended at {DateTime.Now} ===");
                }
                
                Debug.Log("🛑 Error capture disabled");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to disable error capture: {e.Message}");
            }
        }

        #endregion

        #region Error Handling

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            try
            {
                if (type == LogType.Error || type == LogType.Exception)
                {
                    ProcessError(logString, stackTrace, type);
                }
                else if (type == LogType.Warning)
                {
                    ProcessWarning(logString, stackTrace);
                }
            }
            catch (Exception e)
            {
                // Don't log this error as it could cause infinite recursion
                Debug.unityLogger.Log(LogType.Error, $"Error monitor failed: {e.Message}");
            }
        }

        private void ProcessError(string message, string stackTrace, LogType type)
        {
            var errorReport = new ErrorReport
            {
                message = message,
                stackTrace = stackTrace,
                type = type,
                timestamp = DateTime.Now,
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                frameCount = Time.frameCount,
                timeScale = Time.timeScale
            };

            // Store error
            StoreError(errorReport);
            
            // Update frequency tracking
            UpdateErrorFrequency(message);
            
            // Log to file
            if (logToFile)
            {
                WriteErrorToFile(errorReport);
            }
            
            // Show notification if enabled
            if (showErrorNotifications)
            {
                ShowErrorNotification(errorReport);
            }
            
            // Fire event
            OnErrorCaptured?.Invoke(errorReport);
            
            // Check for critical patterns
            CheckForCriticalErrors(errorReport);
        }

        private void ProcessWarning(string message, string stackTrace)
        {
            // Track warnings for patterns that might indicate upcoming errors
            if (IsWarningCritical(message))
            {
                Debug.Log($"⚠️ Critical warning detected: {message}");
                
                if (logToFile)
                {
                    WriteToLogFile($"[WARNING] {DateTime.Now:HH:mm:ss} - {message}");
                }
            }
        }

        private bool IsWarningCritical(string message)
        {
            string[] criticalPatterns = {
                "memory",
                "leak",
                "null reference",
                "missing",
                "failed to",
                "cannot find",
                "deprecated"
            };

            return criticalPatterns.Any(pattern => 
                message.ToLower().Contains(pattern));
        }

        #endregion

        #region Error Storage & Analysis

        private void StoreError(ErrorReport error)
        {
            storedErrors.Add(error);
            
            // Keep only the most recent errors
            if (storedErrors.Count > maxStoredErrors)
            {
                storedErrors.RemoveAt(0);
            }
        }

        private void UpdateErrorFrequency(string errorMessage)
        {
            // Simplify the error message for frequency counting
            string simplifiedMessage = SimplifyErrorMessage(errorMessage);
            
            if (errorFrequency.ContainsKey(simplifiedMessage))
            {
                errorFrequency[simplifiedMessage]++;
            }
            else
            {
                errorFrequency[simplifiedMessage] = 1;
            }
            
            // Warning for frequent errors
            if (errorFrequency[simplifiedMessage] > 5)
            {
                Debug.LogWarning($"🚨 Frequent error detected ({errorFrequency[simplifiedMessage]} times): {simplifiedMessage}");
            }
        }

        private string SimplifyErrorMessage(string message)
        {
            // Remove specific numbers, paths, and variable names to group similar errors
            string simplified = message;
            
            // Remove specific object names in parentheses
            simplified = System.Text.RegularExpressions.Regex.Replace(simplified, @"\([^)]*\)", "");
            
            // Remove file paths
            simplified = System.Text.RegularExpressions.Regex.Replace(simplified, @"[A-Za-z]:\\[^\s]*", "[PATH]");
            
            // Remove specific numbers
            simplified = System.Text.RegularExpressions.Regex.Replace(simplified, @"\b\d+\b", "[NUM]");
            
            return simplified.Trim();
        }

        private void CheckForCriticalErrors(ErrorReport error)
        {
            // Check for errors that might indicate system failure
            string[] criticalErrorPatterns = {
                "NullReferenceException",
                "IndexOutOfRangeException", 
                "ArgumentException",
                "InvalidOperationException",
                "OutOfMemoryException",
                "StackOverflowException"
            };

            if (criticalErrorPatterns.Any(pattern => error.message.Contains(pattern)))
            {
                HandleCriticalError(error);
            }
        }

        private void HandleCriticalError(ErrorReport error)
        {
            Debug.LogError($"💥 CRITICAL ERROR DETECTED: {error.message}");
            
            // Try to provide helpful suggestions
            string suggestion = GetErrorSuggestion(error.message);
            if (!string.IsNullOrEmpty(suggestion))
            {
                Debug.Log($"💡 Suggestion: {suggestion}");
            }
            
            if (logToFile)
            {
                WriteToLogFile($"[CRITICAL] {error.timestamp:HH:mm:ss} - {error.message}");
                WriteToLogFile($"[SUGGESTION] {suggestion}");
            }
        }

        private string GetErrorSuggestion(string errorMessage)
        {
            if (errorMessage.Contains("NullReferenceException"))
            {
                return "Check for null objects before accessing them. Use null-conditional operators (?.) or null checks.";
            }
            if (errorMessage.Contains("IndexOutOfRangeException"))
            {
                return "Verify array/list bounds before accessing elements. Check if index < length.";
            }
            if (errorMessage.Contains("ArgumentException"))
            {
                return "Validate method parameters before calling functions. Check parameter types and values.";
            }
            if (errorMessage.Contains("missing") && errorMessage.Contains("component"))
            {
                return "Make sure required components are attached to GameObjects. Use GetComponent safely.";
            }
            if (errorMessage.Contains("Sentis"))
            {
                return "Install Unity Sentis package from Package Manager for AI/ML functionality.";
            }
            
            return "Review the stack trace and check recent code changes.";
        }

        #endregion

        #region Performance Monitoring

        private void MonitorFrameRate()
        {
            frameTimeBuffer[frameBufferIndex] = Time.unscaledDeltaTime;
            frameBufferIndex = (frameBufferIndex + 1) % frameTimeBuffer.Length;
            
            // Calculate average FPS over the buffer
            float averageFrameTime = frameTimeBuffer.Average();
            float averageFPS = 1f / averageFrameTime;
            
            if (averageFPS < fpsWarningThreshold)
            {
                string warning = $"Low FPS detected: {averageFPS:F1} (threshold: {fpsWarningThreshold})";
                OnPerformanceWarning?.Invoke(warning);
                
                if (logToFile)
                {
                    WriteToLogFile($"[PERFORMANCE] {DateTime.Now:HH:mm:ss} - {warning}");
                }
            }
        }

        private void MonitorMemoryUsage()
        {
            long memoryUsage = GC.GetTotalMemory(false) / (1024 * 1024); // Convert to MB
            
            if (memoryUsage > memoryWarningThresholdMB)
            {
                string warning = $"High memory usage: {memoryUsage}MB (threshold: {memoryWarningThresholdMB}MB)";
                OnPerformanceWarning?.Invoke(warning);
                
                Debug.LogWarning($"🐏 {warning}");
                
                if (logToFile)
                {
                    WriteToLogFile($"[MEMORY] {DateTime.Now:HH:mm:ss} - {warning}");
                }
                
                // Suggest garbage collection
                Debug.Log("💡 Consider calling GC.Collect() or reviewing object lifecycle");
            }
        }

        #endregion

        #region File Logging

        private void WriteErrorToFile(ErrorReport error)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"[{error.type}] {error.timestamp:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Scene: {error.scene}");
                sb.AppendLine($"Frame: {error.frameCount}");
                sb.AppendLine($"Message: {error.message}");
                
                if (!string.IsNullOrEmpty(error.stackTrace))
                {
                    sb.AppendLine("Stack Trace:");
                    sb.AppendLine(error.stackTrace);
                }
                
                sb.AppendLine(new string('-', 50));
                
                WriteToLogFile(sb.ToString());
            }
            catch (Exception e)
            {
                Debug.unityLogger.Log(LogType.Error, $"Failed to write error to file: {e.Message}");
            }
        }

        private void WriteToLogFile(string content)
        {
            if (string.IsNullOrEmpty(logFilePath)) return;
            
            try
            {
                File.AppendAllText(logFilePath, content + Environment.NewLine);
            }
            catch (Exception e)
            {
                Debug.unityLogger.Log(LogType.Error, $"Failed to write to log file: {e.Message}");
            }
        }

        #endregion

        #region UI & Notifications

        private void ShowErrorNotification(ErrorReport error)
        {
            // In a real implementation, this could show a UI notification
            // For now, we'll use a distinctive console message
            Debug.Log($"🔔 ERROR NOTIFICATION: {error.message.Substring(0, Mathf.Min(100, error.message.Length))}...");
        }

        #endregion

        #region Public API

        public List<ErrorReport> GetStoredErrors()
        {
            return new List<ErrorReport>(storedErrors);
        }

        public Dictionary<string, int> GetErrorFrequency()
        {
            return new Dictionary<string, int>(errorFrequency);
        }

        public void ClearErrorHistory()
        {
            storedErrors.Clear();
            errorFrequency.Clear();
            Debug.Log("🧹 Cleared error history");
        }

        public void ExportErrorReport()
        {
            try
            {
                string reportPath = Path.Combine(Application.persistentDataPath, $"error_report_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                
                StringBuilder report = new StringBuilder();
                report.AppendLine("=== Project Chimera Error Report ===");
                report.AppendLine($"Generated: {DateTime.Now}");
                report.AppendLine($"Unity Version: {Application.unityVersion}");
                report.AppendLine($"Platform: {Application.platform}");
                report.AppendLine();
                
                report.AppendLine("=== Error Frequency ===");
                foreach (var kvp in errorFrequency.OrderByDescending(x => x.Value))
                {
                    report.AppendLine($"{kvp.Value}x - {kvp.Key}");
                }
                report.AppendLine();
                
                report.AppendLine("=== Recent Errors ===");
                foreach (var error in storedErrors.TakeLast(20))
                {
                    report.AppendLine($"[{error.timestamp:HH:mm:ss}] {error.message}");
                }
                
                File.WriteAllText(reportPath, report.ToString());
                Debug.Log($"📊 Error report exported to: {reportPath}");
                
                #if UNITY_EDITOR
                EditorUtility.RevealInFinder(reportPath);
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to export error report: {e.Message}");
            }
        }

        #endregion

        #region Editor Menu Items

        #if UNITY_EDITOR
        [MenuItem("🧪 Laboratory/Project Chimera/Error Monitor/Show Recent Errors")]
        public static void ShowRecentErrors()
        {
            if (Instance != null)
            {
                var errors = Instance.GetStoredErrors();
                Debug.Log($"📋 Recent errors ({errors.Count}):");
                
                foreach (var error in errors.TakeLast(10))
                {
                    Debug.Log($"  [{error.timestamp:HH:mm:ss}] {error.type}: {error.message}");
                }
            }
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Error Monitor/Export Error Report")]
        public static void ExportErrorReportMenu()
        {
            if (Instance != null)
            {
                Instance.ExportErrorReport();
            }
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Error Monitor/Clear Error History")]
        public static void ClearErrorHistoryMenu()
        {
            if (Instance != null)
            {
                Instance.ClearErrorHistory();
            }
        }

        [MenuItem("🧪 Laboratory/Project Chimera/Error Monitor/Open Log Folder")]
        public static void OpenLogFolder()
        {
            string logPath = Path.Combine(Application.persistentDataPath, "ChimeraLogs");
            if (Directory.Exists(logPath))
            {
                EditorUtility.RevealInFinder(logPath);
            }
            else
            {
                Debug.LogWarning("⚠️ Log folder doesn't exist yet");
            }
        }
        #endif

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class ErrorReport
    {
        public string message;
        public string stackTrace;
        public LogType type;
        public DateTime timestamp;
        public string scene;
        public int frameCount;
        public float timeScale;
    }

    #endregion
}
