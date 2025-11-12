using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Backend
{
    /// <summary>
    /// Crash and error reporting system for production debugging.
    /// Captures exceptions, logs, and system information.
    /// Reports to backend for analysis and alerting.
    /// </summary>
    public class CrashReportingSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string crashEndpoint = "/crashes";
        [SerializeField] private string errorEndpoint = "/errors";

        [Header("Reporting Settings")]
        [SerializeField] private bool enableReporting = true;
        [SerializeField] private bool reportInEditor = false;
        [SerializeField] private bool captureScreenshotOnCrash = false;
        [SerializeField] private int maxReportsPerSession = 50;

        [Header("Error Levels")]
        [SerializeField] private bool reportExceptions = true;
        [SerializeField] private bool reportErrors = true;
        [SerializeField] private bool reportAsserts = true;
        [SerializeField] private bool reportWarnings = false;

        [Header("Data Collection")]
        [SerializeField] private bool includeSystemInfo = true;
        [SerializeField] private bool includeStackTrace = true;
        [SerializeField] private bool includeLogHistory = true;
        [SerializeField] private int logHistorySize = 50;

        [Header("Privacy")]
        [SerializeField] private bool anonymizeUserId = false;
        [SerializeField] private bool includeUserData = true;

        #endregion

        #region Private Fields

        private static CrashReportingSystem _instance;

        // Report tracking
        private int _reportsSent = 0;
        private Queue<string> _logHistory = new Queue<string>();
        private bool _isReporting = false;

        // Statistics
        private int _totalExceptions = 0;
        private int _totalErrors = 0;
        private int _totalWarnings = 0;
        private int _reportsFailed = 0;

        // Events
        public event Action<CrashReport> OnCrashReported;
        public event Action<ErrorReport> OnErrorReported;
        public event Action<string> OnReportFailed;

        #endregion

        #region Properties

        public static CrashReportingSystem Instance => _instance;
        public bool IsEnabled => enableReporting;
        public int ReportsSent => _reportsSent;

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

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void OnApplicationQuit()
        {
            // Final report on quit if needed
            if (_isReporting)
            {
                Debug.Log("[CrashReportingSystem] Waiting for pending reports...");
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[CrashReportingSystem] Initializing...");

            // Don't report in editor unless enabled
            if (Application.isEditor && !reportInEditor)
            {
                Debug.Log("[CrashReportingSystem] Disabled in editor");
                enableReporting = false;
                return;
            }

            Debug.Log("[CrashReportingSystem] Initialized");
        }

        #endregion

        #region Log Handling

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!enableReporting) return;

            // Add to history
            if (includeLogHistory)
            {
                _logHistory.Enqueue($"[{type}] {logString}");

                if (_logHistory.Count > logHistorySize)
                {
                    _logHistory.Dequeue();
                }
            }

            // Check if we should report this log type
            bool shouldReport = false;

            switch (type)
            {
                case LogType.Exception:
                    _totalExceptions++;
                    shouldReport = reportExceptions;
                    break;

                case LogType.Error:
                    _totalErrors++;
                    shouldReport = reportErrors;
                    break;

                case LogType.Assert:
                    shouldReport = reportAsserts;
                    break;

                case LogType.Warning:
                    _totalWarnings++;
                    shouldReport = reportWarnings;
                    break;

                case LogType.Log:
                    shouldReport = false;
                    break;
            }

            // Check report limit
            if (_reportsSent >= maxReportsPerSession)
            {
                return;
            }

            if (shouldReport)
            {
                if (type == LogType.Exception)
                {
                    ReportCrash(logString, stackTrace);
                }
                else
                {
                    ReportError(logString, stackTrace, type);
                }
            }
        }

        #endregion

        #region Crash Reporting

        /// <summary>
        /// Report a crash (exception).
        /// </summary>
        public void ReportCrash(string message, string stackTrace, Dictionary<string, string> metadata = null)
        {
            if (!enableReporting || _reportsSent >= maxReportsPerSession) return;

            var report = CreateCrashReport(message, stackTrace, metadata);

            StartCoroutine(SendCrashReport(report));
        }

        private CrashReport CreateCrashReport(string message, string stackTrace, Dictionary<string, string> metadata)
        {
            var report = new CrashReport
            {
                timestamp = DateTime.UtcNow,
                message = message,
                stackTrace = includeStackTrace ? stackTrace : null,
                sessionId = GetSessionId(),
                userId = GetUserId(),
                buildVersion = Application.version,
                platform = Application.platform.ToString(),
                systemInfo = includeSystemInfo ? CollectSystemInfo() : null,
                logHistory = includeLogHistory ? _logHistory.ToArray() : null,
                metadata = metadata
            };

            // Screenshot
            if (captureScreenshotOnCrash)
            {
                report.screenshot = CaptureScreenshot();
            }

            return report;
        }

        private IEnumerator SendCrashReport(CrashReport report)
        {
            _isReporting = true;

            string url = backendUrl + crashEndpoint;
            string json = JsonUtility.ToJson(report);

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

                request.timeout = 10;

                yield return request.SendWebRequest();

                _isReporting = false;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    _reportsSent++;
                    OnCrashReported?.Invoke(report);
                    Debug.Log($"[CrashReportingSystem] Crash report sent: {report.message}");
                }
                else
                {
                    _reportsFailed++;
                    OnReportFailed?.Invoke($"Failed to send crash report: {request.error}");
                    Debug.LogError($"[CrashReportingSystem] Failed to send crash report: {request.error}");
                }
            }
        }

        #endregion

        #region Error Reporting

        /// <summary>
        /// Report a non-fatal error.
        /// </summary>
        public void ReportError(string message, string stackTrace, LogType logType, Dictionary<string, string> metadata = null)
        {
            if (!enableReporting || _reportsSent >= maxReportsPerSession) return;

            var report = CreateErrorReport(message, stackTrace, logType, metadata);

            StartCoroutine(SendErrorReport(report));
        }

        private ErrorReport CreateErrorReport(string message, string stackTrace, LogType logType, Dictionary<string, string> metadata)
        {
            var report = new ErrorReport
            {
                timestamp = DateTime.UtcNow,
                message = message,
                stackTrace = includeStackTrace ? stackTrace : null,
                logType = logType.ToString(),
                sessionId = GetSessionId(),
                userId = GetUserId(),
                buildVersion = Application.version,
                platform = Application.platform.ToString(),
                metadata = metadata
            };

            return report;
        }

        private IEnumerator SendErrorReport(ErrorReport report)
        {
            _isReporting = true;

            string url = backendUrl + errorEndpoint;
            string json = JsonUtility.ToJson(report);

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

                request.timeout = 10;

                yield return request.SendWebRequest();

                _isReporting = false;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    _reportsSent++;
                    OnErrorReported?.Invoke(report);
                    Debug.Log($"[CrashReportingSystem] Error report sent: {report.message}");
                }
                else
                {
                    _reportsFailed++;
                    OnReportFailed?.Invoke($"Failed to send error report: {request.error}");
                }
            }
        }

        #endregion

        #region Data Collection

        private SystemInfoData CollectSystemInfo()
        {
            return new SystemInfoData
            {
                deviceModel = SystemInfo.deviceModel,
                deviceName = SystemInfo.deviceName,
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                systemMemorySize = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsMemorySize = SystemInfo.graphicsMemorySize,
                graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion,
                screenResolution = $"{Screen.width}x{Screen.height}",
                screenDpi = Screen.dpi,
                targetFrameRate = Application.targetFrameRate,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()]
            };
        }

        private string CaptureScreenshot()
        {
            try
            {
                // Capture screenshot as base64
                var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                byte[] bytes = screenshot.EncodeToPNG();
                Destroy(screenshot);

                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CrashReportingSystem] Failed to capture screenshot: {ex.Message}");
                return null;
            }
        }

        private string GetSessionId()
        {
            // Use Unity's session ID or generate one
            if (!PlayerPrefs.HasKey("SessionId"))
            {
                PlayerPrefs.SetString("SessionId", Guid.NewGuid().ToString());
                PlayerPrefs.Save();
            }

            return PlayerPrefs.GetString("SessionId");
        }

        private string GetUserId()
        {
            if (!includeUserData)
                return "anonymous";

            // Use authenticated user ID if available
            if (UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
            {
                string userId = UserAuthenticationSystem.Instance.UserId;

                if (anonymizeUserId)
                {
                    // Hash the user ID for privacy
                    return HashUserId(userId);
                }

                return userId;
            }

            // Fallback to device ID
            string deviceId = SystemInfo.deviceUniqueIdentifier;

            if (anonymizeUserId)
            {
                return HashUserId(deviceId);
            }

            return deviceId;
        }

        private string HashUserId(string userId)
        {
            // Simple hash for anonymization
            int hash = userId.GetHashCode();
            return $"user_{hash:X8}";
        }

        #endregion

        #region Manual Reporting

        /// <summary>
        /// Manually report a custom event.
        /// </summary>
        public void ReportCustomEvent(string eventName, string message, Dictionary<string, string> metadata = null)
        {
            if (!enableReporting) return;

            var customMetadata = new Dictionary<string, string>(metadata ?? new Dictionary<string, string>());
            customMetadata["eventName"] = eventName;

            ReportError(message, "", LogType.Log, customMetadata);
        }

        /// <summary>
        /// Manually report a breadcrumb (for debugging).
        /// </summary>
        public void RecordBreadcrumb(string breadcrumb)
        {
            if (includeLogHistory)
            {
                _logHistory.Enqueue($"[BREADCRUMB] {breadcrumb}");

                if (_logHistory.Count > logHistorySize)
                {
                    _logHistory.Dequeue();
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get crash reporting statistics.
        /// </summary>
        public CrashReportingStats GetStats()
        {
            return new CrashReportingStats
            {
                reportsSent = _reportsSent,
                reportsFailed = _reportsFailed,
                totalExceptions = _totalExceptions,
                totalErrors = _totalErrors,
                totalWarnings = _totalWarnings,
                isEnabled = enableReporting,
                maxReportsPerSession = maxReportsPerSession
            };
        }

        /// <summary>
        /// Enable or disable crash reporting.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            enableReporting = enabled;
            Debug.Log($"[CrashReportingSystem] Reporting {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Set user identifier for crash reports.
        /// </summary>
        public void SetUserId(string userId)
        {
            PlayerPrefs.SetString("CrashReporting_UserId", userId);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Clear all crash reporting data.
        /// </summary>
        public void ClearData()
        {
            _logHistory.Clear();
            _reportsSent = 0;
            _reportsFailed = 0;
            _totalExceptions = 0;
            _totalErrors = 0;
            _totalWarnings = 0;

            Debug.Log("[CrashReportingSystem] Data cleared");
        }

        #endregion

        #region Context Menu

        [ContextMenu("Test Crash Report")]
        private void TestCrashReport()
        {
            try
            {
                throw new Exception("Test crash from context menu");
            }
            catch (Exception ex)
            {
                ReportCrash(ex.Message, ex.StackTrace);
            }
        }

        [ContextMenu("Test Error Report")]
        private void TestErrorReport()
        {
            ReportError("Test error from context menu", "Fake stack trace", LogType.Error);
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Crash Reporting Statistics ===\n" +
                      $"Enabled: {stats.isEnabled}\n" +
                      $"Reports Sent: {stats.reportsSent}/{stats.maxReportsPerSession}\n" +
                      $"Reports Failed: {stats.reportsFailed}\n" +
                      $"Total Exceptions: {stats.totalExceptions}\n" +
                      $"Total Errors: {stats.totalErrors}\n" +
                      $"Total Warnings: {stats.totalWarnings}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Crash report data.
    /// </summary>
    [Serializable]
    public class CrashReport
    {
        public DateTime timestamp;
        public string message;
        public string stackTrace;
        public string sessionId;
        public string userId;
        public string buildVersion;
        public string platform;
        public SystemInfoData systemInfo;
        public string[] logHistory;
        public Dictionary<string, string> metadata;
        public string screenshot;
    }

    /// <summary>
    /// Error report data.
    /// </summary>
    [Serializable]
    public class ErrorReport
    {
        public DateTime timestamp;
        public string message;
        public string stackTrace;
        public string logType;
        public string sessionId;
        public string userId;
        public string buildVersion;
        public string platform;
        public Dictionary<string, string> metadata;
    }

    /// <summary>
    /// System information data.
    /// </summary>
    [Serializable]
    public class SystemInfoData
    {
        public string deviceModel;
        public string deviceName;
        public string operatingSystem;
        public string processorType;
        public int processorCount;
        public int systemMemorySize;
        public string graphicsDeviceName;
        public int graphicsMemorySize;
        public string graphicsDeviceVersion;
        public string screenResolution;
        public float screenDpi;
        public int targetFrameRate;
        public string quality;
    }

    /// <summary>
    /// Crash reporting statistics.
    /// </summary>
    [Serializable]
    public struct CrashReportingStats
    {
        public int reportsSent;
        public int reportsFailed;
        public int totalExceptions;
        public int totalErrors;
        public int totalWarnings;
        public bool isEnabled;
        public int maxReportsPerSession;
    }

    #endregion
}
