using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using System.Text;
using System.IO;
using Laboratory.Core.Infrastructure;

namespace Laboratory.Subsystems.Monitoring
{
    /// <summary>
    /// Debug & Development Tools Subsystem Manager
    ///
    /// Provides comprehensive debugging, profiling, testing, and development tools
    /// for Project Chimera. Includes real-time system monitoring, automated testing,
    /// performance profiling, and developer utilities.
    ///
    /// Key responsibilities:
    /// - Real-time system monitoring and debugging
    /// - Automated testing framework integration
    /// - Performance profiling and bottleneck detection
    /// - Developer console and command execution
    /// - Log aggregation and analysis
    /// - Debug visualization and inspection tools
    /// - Build and deployment automation helpers
    /// </summary>
    public class DebugSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        #region Events

        public static event Action<DebugLogEntry> OnDebugLogAdded;
        public static event Action<SystemMonitorData> OnSystemMonitorUpdated;
        public static event Action<PerformanceAlert> OnPerformanceAlertTriggered;
        public static event Action<TestResult> OnTestCompleted;
        public static event Action<DebugCommand> OnDebugCommandExecuted;
        public static event Action<string> OnDeveloperNotification;

        #endregion

        #region Configuration

        [Header("Configuration")]
        [SerializeField] private DebugSubsystemConfig _config;

        public DebugSubsystemConfig Config
        {
            get => _config;
            set => _config = value;
        }

        #endregion

        #region ISubsystemManager Implementation

        public string SubsystemName => "Debug & Development Tools";
        public bool IsInitialized => _isInitialized;
        public float InitializationProgress { get; private set; } = 0f;

        #endregion

        #region Services

        private ISystemMonitoringService _systemMonitoringService;
        private IPerformanceProfilerService _performanceProfilerService;
        private IAutomatedTestingService _automatedTestingService;
        private IDeveloperConsoleService _developerConsoleService;
        private ILogAggregationService _logAggregationService;
        private IDebugVisualizationService _debugVisualizationService;

        #endregion

        #region State

        private bool _isInitialized;
        private bool _isRunning;
        private Coroutine _monitoringCoroutine;
        private Queue<DebugLogEntry> _logQueue;
        private Dictionary<string, SystemMonitor> _systemMonitors;
        private Dictionary<string, PerformanceProfilerService> _performanceProfilers;
        private List<DebugCommand> _registeredCommands;
        private Dictionary<string, object> _debugVariables;
        private StringBuilder _logBuilder;
        private DateTime _lastLogFlush;

        // Debug UI state
        private bool _showDebugUI = false;
        private Vector2 _consoleScrollPosition;
        private string _consoleInput = "";
        private List<string> _commandHistory;
        private int _commandHistoryIndex = -1;

        #endregion

        #region Initialization

        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
                return true;

            try
            {
                InitializationProgress = 0f;

                if (_config == null)
                {
                    UnityEngine.Debug.LogError("[DebugSubsystem] Configuration is null");
                    return false;
                }

                // Initialize services
                InitializationProgress = 0.1f;
                await InitializeServicesAsync();

                // Initialize data structures
                InitializationProgress = 0.3f;
                InitializeDataStructures();

                // Register built-in debug commands
                InitializationProgress = 0.5f;
                RegisterBuiltInCommands();

                // Initialize system monitors
                InitializationProgress = 0.7f;
                InitializeSystemMonitors();

                // Start background monitoring
                InitializationProgress = 0.85f;
                StartBackgroundMonitoring();

                // Setup log capture
                InitializationProgress = 0.95f;
                SetupLogCapture();

                _isInitialized = true;
                _isRunning = true;
                InitializationProgress = 1f;

                if (_config.enableDebugLogging)
                    UnityEngine.Debug.Log("[DebugSubsystem] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DebugSubsystem] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        private async Task InitializeServicesAsync()
        {
            // Initialize system monitoring service
            _systemMonitoringService = new SystemMonitoringService(_config);
            await _systemMonitoringService.InitializeAsync();

            // Initialize performance profiler service
            _performanceProfilerService = new PerformanceProfilerService(_config);
            await _performanceProfilerService.InitializeAsync();

            // Initialize automated testing service
            _automatedTestingService = new AutomatedTestingService(_config);
            await _automatedTestingService.InitializeAsync();

            // Initialize developer console service
            _developerConsoleService = new DeveloperConsoleService(_config);
            await _developerConsoleService.InitializeAsync();

            // Initialize log aggregation service
            _logAggregationService = new LogAggregationService(_config);
            await _logAggregationService.InitializeAsync();

            // Initialize debug visualization service
            _debugVisualizationService = new DebugVisualizationService(_config);
            await _debugVisualizationService.InitializeAsync();

            // Register with service container if available
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.RegisterService<ISystemMonitoringService>(_systemMonitoringService);
                ServiceContainer.Instance.RegisterService<IPerformanceProfilerService>(_performanceProfilerService);
                ServiceContainer.Instance.RegisterService<IAutomatedTestingService>(_automatedTestingService);
                ServiceContainer.Instance.RegisterService<IDeveloperConsoleService>(_developerConsoleService);
                ServiceContainer.Instance.RegisterService<ILogAggregationService>(_logAggregationService);
                ServiceContainer.Instance.RegisterService<IDebugVisualizationService>(_debugVisualizationService);
            }
        }

        private void InitializeDataStructures()
        {
            _logQueue = new Queue<DebugLogEntry>();
            _systemMonitors = new Dictionary<string, SystemMonitor>();
            _performanceProfilers = new Dictionary<string, PerformanceProfilerService>();
            _registeredCommands = new List<DebugCommand>();
            _debugVariables = new Dictionary<string, object>();
            _logBuilder = new StringBuilder();
            _commandHistory = new List<string>();
        }

        private void RegisterBuiltInCommands()
        {
            RegisterCommand(new DebugCommand
            {
                commandName = "help",
                description = "Display available debug commands",
                parameters = new List<DebugCommandParameter>(),
                executeAction = (parameters) => ShowHelpCommand(new List<object>())
            });

            RegisterCommand(new DebugCommand
            {
                commandName = "clear",
                description = "Clear debug console",
                parameters = new List<DebugCommandParameter>(),
                executeAction = (parameters) => { ClearConsoleCommand(new List<object>()); }
            });

            RegisterCommand(new DebugCommand
            {
                commandName = "fps",
                description = "Show current FPS",
                parameters = new List<DebugCommandParameter>(),
                executeAction = (parameters) => { ShowFPSCommand(new List<object>()); }
            });

            RegisterCommand(new DebugCommand
            {
                commandName = "memory",
                description = "Show memory usage",
                parameters = new List<DebugCommandParameter>(),
                executeAction = (parameters) => { ShowMemoryCommand(new List<object>()); }
            });

            RegisterCommand(new DebugCommand
            {
                commandName = "profiler",
                description = "Control performance profiler",
                parameters = new List<DebugCommandParameter>
                {
                    new DebugCommandParameter { name = "action", parameterType = typeof(string), description = "start/stop/results", isRequired = true }
                },
                executeAction = (parameters) => { ProfilerCommand(new List<object>(parameters.Values)); }
            });

            RegisterCommand(new DebugCommand
            {
                commandName = "test",
                description = "Run automated tests",
                parameters = new List<DebugCommandParameter>
                {
                    new DebugCommandParameter { name = "category", parameterType = typeof(string), description = "Test category", isRequired = false, defaultValue = "all" }
                },
                executeAction = (parameters) => { RunTestsCommand(new List<object>(parameters.Values)); }
            });

            RegisterCommand(new DebugCommand
            {
                commandName = "spawn",
                description = "Spawn test creatures",
                parameters = new List<DebugCommandParameter>
                {
                    new DebugCommandParameter { name = "count", parameterType = typeof(int), description = "Number of creatures", isRequired = true }
                },
                executeAction = (parameters) => { SpawnTestCreaturesCommand(new List<object>(parameters.Values)); }
            });

            RegisterCommand(new DebugCommand
            {
                commandName = "set",
                description = "Set debug variable",
                parameters = new List<DebugCommandParameter>
                {
                    new DebugCommandParameter { name = "variable", parameterType = typeof(string), description = "Variable name", isRequired = true },
                    new DebugCommandParameter { name = "value", parameterType = typeof(string), description = "Variable value", isRequired = true }
                },
                executeAction = (parameters) => { SetVariableCommand(new List<object>(parameters.Values)); }
            });

            RegisterCommand(new DebugCommand
            {
                commandName = "get",
                description = "Get debug variable",
                parameters = new List<DebugCommandParameter>
                {
                    new DebugCommandParameter { name = "variable", parameterType = typeof(string), description = "Variable name", isRequired = true }
                },
                executeAction = (parameters) => { GetVariableCommand(new List<object>(parameters.Values)); }
            });
        }

        private void InitializeSystemMonitors()
        {
            var systemTypes = new[]
            {
                "ECS",
                "Genetics",
                "AI",
                "Networking",
                "Audio",
                "Rendering",
                "Physics",
                "UI"
            };

            foreach (var systemType in systemTypes)
            {
                _systemMonitors[systemType] = new SystemMonitor
                {
                    systemName = systemType,
                    isActive = true,
                    updateInterval = _config.systemMonitoringInterval,
                    lastUpdate = DateTime.Now
                };
            }
        }

        private void StartBackgroundMonitoring()
        {
            _monitoringCoroutine = StartCoroutine(BackgroundMonitoringLoop());
        }

        private void SetupLogCapture()
        {
            if (_config.enableDebugLogging)
            {
                Application.logMessageReceived += OnLogMessageReceived;
            }
        }

        #endregion

        #region Background Monitoring

        private IEnumerator BackgroundMonitoringLoop()
        {
            var interval = _config.systemMonitoringInterval;

            while (_isRunning)
            {
                // Update system monitoring
                UpdateSystemMonitoring();

                // Process log queue
                ProcessLogQueue();

                // Check performance alerts
                CheckPerformanceAlerts();

                // Update debug visualizations
                UpdateDebugVisualizations();

                // Update performance profiling
                UpdatePerformanceProfiling();

                // Flush logs if needed
                FlushLogsIfNeeded();

                yield return new WaitForSeconds(interval);
            }
        }

        private void UpdateSystemMonitoring()
        {
            foreach (var monitor in _systemMonitors.Values)
            {
                if (monitor.isActive)
                {
                    UpdateSystemMonitor(monitor);
                }
            }
        }

        private void UpdateSystemMonitor(SystemMonitor monitor)
        {
            var monitorData = _systemMonitoringService?.GetSystemData(monitor.systemName);
            if (monitorData != null)
            {
                monitor.currentData = monitorData;
                monitor.lastUpdate = DateTime.Now;

                OnSystemMonitorUpdated?.Invoke(monitorData);

                // Check for performance alerts
                CheckSystemPerformanceAlerts(monitor, monitorData);
            }
        }

        private void ProcessLogQueue()
        {
            var processedCount = 0;
            var maxLogsPerFrame = _config.maxLogEntries / 100; // Use fraction of max log entries as frame limit

            while (_logQueue.Count > 0 && processedCount < maxLogsPerFrame)
            {
                var logEntry = _logQueue.Dequeue();
                ProcessLogEntry(logEntry);
                processedCount++;
            }
        }

        private void ProcessLogEntry(DebugLogEntry logEntry)
        {
            // Add to log aggregation service
            _logAggregationService?.AddLogEntry(logEntry);

            // Add to console output
            AddToConsoleOutput(logEntry);

            // Write to log file if enabled
            if (_config.enableDebugLogging) // Use existing debug logging setting
            {
                WriteToLogFile(logEntry);
            }

            OnDebugLogAdded?.Invoke(logEntry);
        }

        private void CheckPerformanceAlerts()
        {
            var performanceData = _performanceProfilerService?.GetCurrentPerformanceData();
            if (performanceData != null)
            {
                CheckPerformanceThresholds(performanceData);
            }
        }

        private void CheckSystemPerformanceAlerts(SystemMonitor monitor, SystemMonitorData data)
        {
            // Check for system-specific performance issues
            if (data.cpuUsage > 80f) // Use default threshold of 80%
            {
                TriggerPerformanceAlert(new PerformanceAlert
                {
                    alertType = PerformanceAlertType.CPUSpike,
                    systemName = monitor.systemName,
                    currentValue = data.cpuUsage,
                    thresholdValue = 80f,
                    timestamp = DateTime.Now
                });
            }

            if (data.memoryUsage > 500f) // Use default threshold of 500MB
            {
                TriggerPerformanceAlert(new PerformanceAlert
                {
                    alertType = PerformanceAlertType.HighMemoryUsage,
                    systemName = monitor.systemName,
                    currentValue = data.memoryUsage,
                    thresholdValue = 500f, // Default 500MB threshold
                    timestamp = DateTime.Now
                });
            }
        }

        private void CheckPerformanceThresholds(PerformanceData performanceData)
        {
            // Check frame rate
            if (performanceData.frameRate < 30f) // Default 30 FPS threshold
            {
                TriggerPerformanceAlert(new PerformanceAlert
                {
                    alertType = PerformanceAlertType.LowFrameRate,
                    systemName = "Rendering",
                    currentValue = performanceData.frameRate,
                    thresholdValue = 30f, // Default 30 FPS threshold
                    timestamp = DateTime.Now
                });
            }

            // Check memory usage
            if (performanceData.memoryUsedMB > 1000f) // Default 1GB threshold
            {
                TriggerPerformanceAlert(new PerformanceAlert
                {
                    alertType = PerformanceAlertType.HighMemoryUsage,
                    systemName = "Memory",
                    currentValue = performanceData.memoryUsedMB,
                    thresholdValue = 1000f, // Default 1GB threshold
                    timestamp = DateTime.Now
                });
            }
        }

        private void TriggerPerformanceAlert(PerformanceAlert alert)
        {
            OnPerformanceAlertTriggered?.Invoke(alert);

            if (_config.enableDebugLogging)
            {
                UnityEngine.Debug.LogWarning($"[DebugSubsystem] Performance Alert: {alert.alertType} in {alert.systemName} - {alert.currentValue} (threshold: {alert.thresholdValue})");
            }
        }

        private void UpdateDebugVisualizations()
        {
            if (_config.enableDebugVisualization)
            {
                _debugVisualizationService?.UpdateVisualizations();
            }
        }

        private void UpdatePerformanceProfiling()
        {
            if (_config.enablePerformanceProfiling && _performanceProfilerService is PerformanceProfilerService profilerService)
            {
                profilerService.UpdateProfiling();
            }
        }

        private void FlushLogsIfNeeded()
        {
            var timeSinceLastFlush = DateTime.Now - _lastLogFlush;
            if (timeSinceLastFlush.TotalSeconds >= 30f) // Default 30 second flush interval
            {
                FlushLogs();
                _lastLogFlush = DateTime.Now;
            }
        }

        #endregion

        #region Log Management

        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            var logEntry = new DebugLogEntry
            {
                timestamp = DateTime.Now,
                logType = type,
                message = logString,
                stackTrace = stackTrace,
                category = "Unity",
                frameNumber = Time.frameCount
            };

            _logQueue.Enqueue(logEntry);
        }

        private DebugLogType ConvertLogType(LogType unityLogType)
        {
            return unityLogType switch
            {
                LogType.Error => DebugLogType.Error,
                LogType.Assert => DebugLogType.Error,
                LogType.Warning => DebugLogType.Warning,
                LogType.Log => DebugLogType.Info,
                LogType.Exception => DebugLogType.Error,
                _ => DebugLogType.Info
            };
        }

        private LogType ConvertToUnityLogType(DebugLogType debugLogType)
        {
            return debugLogType switch
            {
                DebugLogType.Error => LogType.Error,
                DebugLogType.Warning => LogType.Warning,
                DebugLogType.Info => LogType.Log,
                DebugLogType.Debug => LogType.Log,
                DebugLogType.Verbose => LogType.Log,
                _ => LogType.Log
            };
        }

        private void AddToConsoleOutput(DebugLogEntry logEntry)
        {
            var formattedMessage = FormatLogEntry(logEntry);
            _logBuilder.AppendLine(formattedMessage);

            // Keep console output size manageable
            if (_logBuilder.Length > 50000) // Default 50K character console limit
            {
                var text = _logBuilder.ToString();
                var lines = text.Split('\n');
                var keepLines = lines.Length / 2;

                _logBuilder.Clear();
                for (int i = keepLines; i < lines.Length; i++)
                {
                    _logBuilder.AppendLine(lines[i]);
                }
            }
        }

        private string FormatLogEntry(DebugLogEntry logEntry)
        {
            var timeString = logEntry.timestamp.ToString("HH:mm:ss.fff");
            var typeString = logEntry.logType.ToString().ToUpper();
            return $"[{timeString}] [{typeString}] {logEntry.message}";
        }

        private void WriteToLogFile(DebugLogEntry logEntry)
        {
            // This would write to a persistent log file
            // Implementation would depend on platform-specific file I/O
        }

        private void FlushLogs()
        {
            _logAggregationService?.FlushLogs();

            if (_config.enableDebugLogging) // Use existing debug logging setting
            {
                // Flush file logs
            }
        }

        #endregion

        #region Command System

        /// <summary>
        /// Registers a debug command
        /// </summary>
        public void RegisterCommand(DebugCommand command)
        {
            _registeredCommands.Add(command);
            _developerConsoleService?.RegisterCommand(command);

            if (_config.enableDebugLogging)
                UnityEngine.Debug.Log($"[DebugSubsystem] Registered command: {command.commandName}");
        }

        /// <summary>
        /// Executes a debug command
        /// </summary>
        public async Task<CommandResult> ExecuteCommandAsync(string commandLine)
        {
            if (_developerConsoleService != null)
            {
                var result = _developerConsoleService.ExecuteCommand(commandLine);
                var commandResult = new CommandResult
                {
                    success = true,
                    result = result,
                    executionTime = DateTime.Now
                };

                OnDebugCommandExecuted?.Invoke(commandResult.command);
                return commandResult;
            }
            else
            {
                return new CommandResult
                {
                    success = false,
                    result = "Developer console service not available",
                    errorMessage = "Service not initialized",
                    executionTime = DateTime.Now
                };
            }
        }

        /// <summary>
        /// Gets all registered commands
        /// </summary>
        public List<DebugCommand> GetRegisteredCommands()
        {
            return new List<DebugCommand>(_registeredCommands);
        }

        #endregion

        #region Built-in Commands

        private CommandResult ShowHelpCommand(List<object> parameters)
        {
            var helpText = new StringBuilder();
            helpText.AppendLine("Available Debug Commands:");
            helpText.AppendLine("========================");

            foreach (var command in _registeredCommands)
            {
                helpText.AppendLine($"{command.commandName} - {command.description}");

                if (command.parameters.Count > 0)
                {
                    foreach (var param in command.parameters)
                    {
                        var optional = param.isRequired ? "" : " (optional)";
                        helpText.AppendLine($"  {param.name}: {param.description}{optional}");
                    }
                }

                helpText.AppendLine();
            }

            return new CommandResult
            {
                success = true,
                result = helpText.ToString(),
                executionTime = DateTime.Now
            };
        }

        private CommandResult ClearConsoleCommand(List<object> parameters)
        {
            _logBuilder.Clear();
            return new CommandResult
            {
                success = true,
                result = "Console cleared",
                executionTime = DateTime.Now
            };
        }

        private CommandResult ShowFPSCommand(List<object> parameters)
        {
            var fps = 1f / Time.unscaledDeltaTime;
            return new CommandResult
            {
                success = true,
                result = $"Current FPS: {fps:F1}",
                executionTime = DateTime.Now
            };
        }

        private CommandResult ShowMemoryCommand(List<object> parameters)
        {
            var memoryMB = GC.GetTotalMemory(false) / (1024f * 1024f);
            return new CommandResult
            {
                success = true,
                result = $"Memory Usage: {memoryMB:F1} MB",
                executionTime = DateTime.Now
            };
        }

        private CommandResult ProfilerCommand(List<object> parameters)
        {
            if (parameters.Count == 0)
            {
                return new CommandResult
                {
                    success = false,
                    errorMessage = "Usage: profiler <start|stop|results>",
                    executionTime = DateTime.Now
                };
            }

            var action = parameters[0].ToString().ToLower();

            switch (action)
            {
                case "start":
                    _performanceProfilerService?.StartProfiling();
                    return new CommandResult { success = true, result = "Profiler started", executionTime = DateTime.Now };

                case "stop":
                    _performanceProfilerService?.StopProfiling();
                    return new CommandResult { success = true, result = "Profiler stopped", executionTime = DateTime.Now };

                case "results":
                    var results = _performanceProfilerService?.GetProfilingResults();
                    return new CommandResult
                    {
                        success = true,
                        result = results?.ToString() ?? "No profiling data available",
                        executionTime = DateTime.Now
                    };

                default:
                    return new CommandResult
                    {
                        success = false,
                        errorMessage = "Invalid action. Use: start, stop, or results",
                        executionTime = DateTime.Now
                    };
            }
        }

        private CommandResult RunTestsCommand(List<object> parameters)
        {
            var category = parameters.Count > 0 ? parameters[0].ToString() : "all";

            Task.Run(async () =>
            {
                if (_automatedTestingService != null)
                {
                    var testResults = await _automatedTestingService.RunTestSuite(category);
                    foreach (var result in testResults)
                    {
                        OnTestCompleted?.Invoke(result);
                    }
                }
            });

            return new CommandResult
            {
                success = true,
                result = $"Running tests for category: {category}",
                executionTime = DateTime.Now
            };
        }

        private CommandResult SpawnTestCreaturesCommand(List<object> parameters)
        {
            if (parameters.Count == 0)
            {
                return new CommandResult
                {
                    success = false,
                    errorMessage = "Usage: spawn <count>",
                    executionTime = DateTime.Now
                };
            }

            if (int.TryParse(parameters[0].ToString(), out var count))
            {
                // This would integrate with the creature spawning system
                SpawnTestCreatures(count);

                return new CommandResult
                {
                    success = true,
                    result = $"Spawned {count} test creatures",
                    executionTime = DateTime.Now
                };
            }
            else
            {
                return new CommandResult
                {
                    success = false,
                    errorMessage = "Invalid count parameter",
                    executionTime = DateTime.Now
                };
            }
        }

        private CommandResult SetVariableCommand(List<object> parameters)
        {
            if (parameters.Count < 2)
            {
                return new CommandResult
                {
                    success = false,
                    errorMessage = "Usage: set <variable> <value>",
                    executionTime = DateTime.Now
                };
            }

            var variable = parameters[0].ToString();
            var value = parameters[1];

            _debugVariables[variable] = value;

            return new CommandResult
            {
                success = true,
                result = $"Set {variable} = {value}",
                executionTime = DateTime.Now
            };
        }

        private CommandResult GetVariableCommand(List<object> parameters)
        {
            if (parameters.Count == 0)
            {
                return new CommandResult
                {
                    success = false,
                    errorMessage = "Usage: get <variable>",
                    executionTime = DateTime.Now
                };
            }

            var variable = parameters[0].ToString();

            if (_debugVariables.TryGetValue(variable, out var value))
            {
                return new CommandResult
                {
                    success = true,
                    result = $"{variable} = {value}",
                    executionTime = DateTime.Now
                };
            }
            else
            {
                return new CommandResult
                {
                    success = false,
                    errorMessage = $"Variable '{variable}' not found",
                    executionTime = DateTime.Now
                };
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets current system monitoring data
        /// </summary>
        public Dictionary<string, SystemMonitorData> GetSystemMonitoringData()
        {
            var data = new Dictionary<string, SystemMonitorData>();

            foreach (var kvp in _systemMonitors)
            {
                if (kvp.Value.currentData != null)
                {
                    data[kvp.Key] = kvp.Value.currentData;
                }
            }

            return data;
        }

        /// <summary>
        /// Gets current performance data
        /// </summary>
        public PerformanceData GetCurrentPerformanceData()
        {
            return _performanceProfilerService?.GetCurrentPerformanceData();
        }

        /// <summary>
        /// Runs automated tests
        /// </summary>
        public async Task<TestResult> RunAutomatedTestsAsync(string category = "all")
        {
            if (_automatedTestingService != null)
            {
                var results = await _automatedTestingService.RunTestSuite(category);
                return results.Count > 0 ? results[0] : new TestResult
                {
                    testName = "No tests",
                    status = TestStatus.Skipped,
                    message = "No tests found in category: " + category
                };
            }
            else
            {
                return new TestResult
                {
                    testName = "Service unavailable",
                    status = TestStatus.Error,
                    message = "Automated testing service not available"
                };
            }
        }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        public void LogDebug(string message, string category = "Debug", DebugLogType logType = DebugLogType.Info)
        {
            var logEntry = new DebugLogEntry
            {
                timestamp = DateTime.Now,
                logType = ConvertToUnityLogType(logType),
                message = message,
                category = category,
                frameNumber = Time.frameCount
            };

            _logQueue.Enqueue(logEntry);
        }

        /// <summary>
        /// Gets debug variable value
        /// </summary>
        public T GetDebugVariable<T>(string variableName, T defaultValue = default)
        {
            if (_debugVariables.TryGetValue(variableName, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets debug variable value
        /// </summary>
        public void SetDebugVariable(string variableName, object value)
        {
            _debugVariables[variableName] = value;
        }

        #endregion

        #region Helper Methods

        private void SpawnTestCreatures(int count)
        {
            // This would integrate with the creature spawning system
            // For now, just log the action
            LogDebug($"Would spawn {count} test creatures", "CreatureSpawning");
        }

        #endregion

        #region Debug UI

        private void OnGUI()
        {
            if (!_config.enableDeveloperConsole || !_showDebugUI)
                return;

            DrawDebugUI();
        }

        private void DrawDebugUI()
        {
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;
            var consoleWidth = screenWidth * 0.6f;
            var consoleHeight = screenHeight * 0.4f;

            GUILayout.BeginArea(new Rect(10, 10, consoleWidth, consoleHeight), GUI.skin.box);

            // Console output
            _consoleScrollPosition = GUILayout.BeginScrollView(_consoleScrollPosition, GUILayout.Height(consoleHeight - 60));
            GUILayout.Label(_logBuilder.ToString(), GUI.skin.textArea);
            GUILayout.EndScrollView();

            // Console input
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("ConsoleInput");
            _consoleInput = GUILayout.TextField(_consoleInput, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Execute", GUILayout.Width(70)) ||
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "ConsoleInput"))
            {
                if (!string.IsNullOrEmpty(_consoleInput))
                {
                    ExecuteConsoleCommand(_consoleInput);
                    _commandHistory.Add(_consoleInput);
                    _consoleInput = "";
                    _commandHistoryIndex = -1;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void ExecuteConsoleCommand(string command)
        {
            LogDebug($"> {command}", "Console");

            Task.Run(async () =>
            {
                var result = await ExecuteCommandAsync(command);
                LogDebug(result.success ? result.result : result.errorMessage, "Console", result.success ? DebugLogType.Info : DebugLogType.Error);
            });
        }

        #endregion

        #region Input Handling

        private void Update()
        {
            // Toggle debug UI
            if (Input.GetKeyDown(KeyCode.F12))
            {
                _showDebugUI = !_showDebugUI;
            }

            // Handle command history navigation
            if (_showDebugUI && GUI.GetNameOfFocusedControl() == "ConsoleInput")
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    NavigateCommandHistory(-1);
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    NavigateCommandHistory(1);
                }
            }
        }

        private void NavigateCommandHistory(int direction)
        {
            if (_commandHistory.Count == 0)
                return;

            _commandHistoryIndex = Mathf.Clamp(_commandHistoryIndex + direction, -1, _commandHistory.Count - 1);

            if (_commandHistoryIndex >= 0)
            {
                _consoleInput = _commandHistory[_commandHistory.Count - 1 - _commandHistoryIndex];
            }
            else
            {
                _consoleInput = "";
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Toggle Debug UI")]
        private void DebugToggleUI()
        {
            _showDebugUI = !_showDebugUI;
        }

        [ContextMenu("Run Performance Test")]
        private async void DebugRunPerformanceTest()
        {
            await RunAutomatedTestsAsync("performance");
        }

        [ContextMenu("Log System Status")]
        private void DebugLogSystemStatus()
        {
            var data = GetSystemMonitoringData();
            foreach (var kvp in data)
            {
                UnityEngine.Debug.Log($"[DebugSubsystem] {kvp.Key}: CPU={kvp.Value.cpuUsage:F1}%, Memory={kvp.Value.memoryUsage:F1}MB");
            }
        }

        #endregion

        #region Lifecycle

        private void OnDestroy()
        {
            _isRunning = false;

            if (_monitoringCoroutine != null)
            {
                StopCoroutine(_monitoringCoroutine);
            }

            if (_config.enableDebugLogging)
            {
                Application.logMessageReceived -= OnLogMessageReceived;
            }

            FlushLogs();
        }

        #endregion
    }
}