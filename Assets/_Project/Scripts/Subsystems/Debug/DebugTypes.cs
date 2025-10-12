using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Laboratory.Subsystems.Debug
{
    #region Core Debug Data Types

    [Serializable]
    public class DebugLogEntry
    {
        public DateTime timestamp;
        public LogType logType;
        public string message;
        public string stackTrace;
        public string source;
        public string category;
        public int frameNumber;
        public Dictionary<string, object> metadata = new();
    }

    [Serializable]
    public class SystemMonitorData
    {
        public string systemName;
        public DateTime timestamp;
        public SystemStatus status;
        public float cpuUsage;
        public float memoryUsage;
        public int activeOperations;
        public float averageResponseTime;
        public int errorCount;
        public Dictionary<string, float> customMetrics = new();
    }

    [Serializable]
    public class PerformanceData
    {
        public DateTime timestamp;
        public float frameRate;
        public float frameTimeMs;
        public float cpuTimeMs;
        public float gpuTimeMs;
        public float memoryUsedMB;
        public int drawCalls;
        public int triangles;
        public Dictionary<string, float> systemTimings = new();
    }

    [Serializable]
    public class TestResult
    {
        public string testName;
        public string testCategory;
        public TestStatus status;
        public DateTime startTime;
        public DateTime endTime;
        public float duration;
        public string message;
        public string errorDetails;
        public Dictionary<string, object> testData = new();
    }

    [Serializable]
    public class DebugCommand
    {
        public string commandName;
        public string description;
        public List<DebugCommandParameter> parameters = new();
        public Action<Dictionary<string, object>> executeAction;
        public string category;
        public bool requiresConfirmation;
        public DebugCommandPermission permission;
    }

    [Serializable]
    public class DebugCommandParameter
    {
        public string name;
        public Type parameterType;
        public bool isRequired;
        public object defaultValue;
        public string description;
    }

    [Serializable]
    public class SystemMonitor
    {
        public string systemName;
        public bool isActive;
        public float updateInterval;
        public DateTime lastUpdate;
        public SystemMonitorData currentData;
        public Queue<SystemMonitorData> dataHistory;
        public SystemMonitorConfig config;
    }

    [Serializable]
    public class SystemMonitorConfig
    {
        public bool enableCpuMonitoring = true;
        public bool enableMemoryMonitoring = true;
        public bool enablePerformanceMonitoring = true;
        public float updateIntervalSeconds = 1f;
        public int historySize = 100;
        public Dictionary<string, bool> customMetrics = new();
    }

    [Serializable]
    public class PerformanceAlert
    {
        public PerformanceAlertType alertType;
        public PerformanceAlertSeverity severity;
        public string message;
        public DateTime timestamp;
        public float currentValue;
        public float thresholdValue;
        public string systemName;
        public Dictionary<string, object> alertData = new();
    }

    [Serializable]
    public class DebugVisualizationData
    {
        public string visualizationName;
        public VisualizationType visualizationType;
        public Dictionary<string, object> data = new();
        public Vector3 worldPosition;
        public Color color = Color.white;
        public float duration = 1f;
        public bool isPersistent = false;
    }

    [Serializable]
    public class TestSuite
    {
        public string suiteName;
        public List<TestCase> testCases = new();
        public TestSuiteConfig config;
        public TestSuiteStatus status;
        public DateTime lastRunTime;
        public float totalDuration;
        public int passedTests;
        public int failedTests;
        public int skippedTests;
    }

    [Serializable]
    public class TestCase
    {
        public string testName;
        public string description;
        public Func<Task<TestResult>> testFunction;
        public TestCaseConfig config;
        public List<TestResult> results = new();
    }

    [Serializable]
    public class TestCaseConfig
    {
        public float timeoutSeconds = 30f;
        public int retryCount = 0;
        public bool isEnabled = true;
        public TestPriority priority = TestPriority.Normal;
        public List<string> tags = new();
    }

    [Serializable]
    public class TestSuiteConfig
    {
        public bool runInParallel = false;
        public bool stopOnFirstFailure = false;
        public float suiteTimeoutSeconds = 300f;
        public TestExecutionMode executionMode = TestExecutionMode.Sequential;
    }

    #endregion

    #region Enums

    public enum SystemStatus
    {
        Unknown,
        Healthy,
        Warning,
        Error,
        Critical,
        Disabled
    }

    public enum TestStatus
    {
        NotRun,
        Running,
        Passed,
        Failed,
        Skipped,
        Timeout,
        Error
    }

    public enum TestSuiteStatus
    {
        NotRun,
        Running,
        Completed,
        Failed,
        Aborted
    }

    public enum TestPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public enum TestExecutionMode
    {
        Sequential,
        Parallel,
        Adaptive
    }

    public enum DebugCommandPermission
    {
        Public,
        Developer,
        Admin,
        System
    }

    public enum DebugLogType
    {
        Info,
        Warning,
        Error,
        Debug,
        Verbose,
        Performance,
        Network,
        AI,
        Genetics,
        System
    }

    public enum PerformanceAlertType
    {
        LowFrameRate,
        HighFrameTime,
        FrameStutter,
        HighMemoryUsage,
        MemoryLeak,
        HighDrawCalls,
        HighTriangleCount,
        CPUSpike,
        GPUSpike,
        SystemOverload,
        QualityDegradation
    }

    public enum PerformanceAlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum VisualizationType
    {
        Point,
        Line,
        Sphere,
        Cube,
        Arrow,
        Text,
        Graph,
        Heatmap,
        Wireframe
    }

    [Serializable]
    public class CommandResult
    {
        public DebugCommand command;
        public bool success;
        public string result;
        public string errorMessage;
        public DateTime executionTime;
        public float executionDurationMs;
    }

    #endregion

    #region Service Interfaces

    /// <summary>
    /// System monitoring service for tracking subsystem health and performance
    /// </summary>
    public interface ISystemMonitoringService
    {
        Task<bool> InitializeAsync();
        void RegisterSystemMonitor(string systemName, SystemMonitorConfig config);
        void UnregisterSystemMonitor(string systemName);
        SystemMonitorData GetSystemData(string systemName);
        Dictionary<string, SystemMonitorData> GetAllSystemData();
        void UpdateSystemMetrics(string systemName, Dictionary<string, float> metrics);
        void SetSystemStatus(string systemName, SystemStatus status);
    }

    /// <summary>
    /// Performance profiling service for detailed performance analysis
    /// </summary>
    public interface IPerformanceProfilerService
    {
        Task<bool> InitializeAsync();
        void StartProfiling(string profileName = "default");
        void StopProfiling(string profileName = "default");
        PerformanceData GetProfilingResults(string profileName = "default");
        PerformanceData GetCurrentPerformanceData();
        void SetProfilingTarget(string targetSystem);
        List<PerformanceData> GetProfilingHistory(string profileName, int count = 100);
    }

    /// <summary>
    /// Automated testing service for running test suites and cases
    /// </summary>
    public interface IAutomatedTestingService
    {
        Task<bool> InitializeAsync();
        void RegisterTestSuite(TestSuite testSuite);
        void UnregisterTestSuite(string suiteName);
        Task<TestResult> RunTestCase(string testCaseName);
        Task<List<TestResult>> RunTestSuite(string suiteName);
        Task<List<TestResult>> RunAllTests();
        List<TestSuite> GetRegisteredTestSuites();
        TestResult GetLastTestResult(string testCaseName);
    }

    /// <summary>
    /// Developer console service for command execution and debugging
    /// </summary>
    public interface IDeveloperConsoleService
    {
        Task<bool> InitializeAsync();
        void RegisterCommand(DebugCommand command);
        void UnregisterCommand(string commandName);
        string ExecuteCommand(string commandText);
        List<DebugCommand> GetAvailableCommands();
        List<string> GetCommandHistory();
        void ClearCommandHistory();
        void SetVariable(string name, object value);
        object GetVariable(string name);
    }

    /// <summary>
    /// Log aggregation service for collecting and analyzing logs
    /// </summary>
    public interface ILogAggregationService
    {
        Task<bool> InitializeAsync();
        void AddLogEntry(DebugLogEntry entry);
        List<DebugLogEntry> GetLogEntries(DebugLogType logType, int count = 100);
        List<DebugLogEntry> GetLogEntriesInTimeRange(DateTime start, DateTime end);
        void ClearLogs();
        void FlushLogs();
        void ExportLogs(string filePath);
        void SetLogLevel(DebugLogType minimumLevel);
    }

    /// <summary>
    /// Debug visualization service for rendering debug information
    /// </summary>
    public interface IDebugVisualizationService
    {
        Task<bool> InitializeAsync();
        void DrawVisualization(DebugVisualizationData data);
        void UpdateVisualizations();
        void ClearVisualizations();
        void SetVisualizationEnabled(bool enabled);
        void RegisterVisualizationRenderer(VisualizationType type, Action<DebugVisualizationData> renderer);
    }

    #endregion

    #region Configuration

    [Serializable]
    public class DebugSubsystemConfig
    {
        [Header("General Settings")]
        public bool enableDebugLogging = true;
        public bool enableSystemMonitoring = true;
        public bool enablePerformanceProfiling = true;
        public bool enableAutomatedTesting = false;
        public bool enableDeveloperConsole = true;
        public bool enableDebugVisualization = true;

        [Header("Monitoring Settings")]
        public float systemMonitoringInterval = 1f;
        public int maxLogEntries = 10000;
        public int maxPerformanceHistory = 1000;
        public bool enableRealTimeMonitoring = true;

        [Header("Console Settings")]
        public int maxCommandHistory = 100;
        public bool enableCommandAutoComplete = true;
        public bool enableCommandHelp = true;
        public DebugCommandPermission defaultPermissionLevel = DebugCommandPermission.Developer;

        [Header("Testing Settings")]
        public bool runTestsOnStartup = false;
        public bool enableContinuousIntegration = false;
        public float testTimeoutSeconds = 30f;
        public bool generateTestReports = true;

        [Header("Visualization Settings")]
        public bool showDebugGizmos = true;
        public bool showPerformanceOverlay = false;
        public bool showSystemStatusOverlay = false;
        public Color debugVisualizationColor = Color.green;

        [Header("File I/O Settings")]
        public string logOutputDirectory = "Logs/";
        public string testReportDirectory = "TestReports/";
        public bool enableLogFileOutput = true;
        public bool enableCrashDumps = true;
    }

    #endregion
}