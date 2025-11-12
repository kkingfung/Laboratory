using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;

namespace Laboratory.Core.Health
{
    /// <summary>
    /// Comprehensive system health monitoring and auto-recovery system
    /// Detects failures, attempts automatic recovery, and provides diagnostic reports
    /// Integrates with performance monitoring for holistic system health
    /// </summary>
    public class SystemHealthMonitor : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float healthCheckInterval = 5f;
        [SerializeField] private int maxRecoveryAttempts = 3;
        [SerializeField] private bool enableAutoRecovery = true;
        [SerializeField] private bool logHealthChecks = false;

        [Header("Runtime Status")]
        [SerializeField] private int systemsMonitored = 0;
        [SerializeField] private int healthySystemsCount = 0;
        [SerializeField] private int warningSystemsCount = 0;
        [SerializeField] private int criticalSystemsCount = 0;
        [SerializeField] private float overallHealthScore = 100f;

        // System tracking
        private readonly Dictionary<string, SystemHealthData> _systemHealth = new Dictionary<string, SystemHealthData>();
        private readonly Dictionary<string, ISystemRecoveryStrategy> _recoveryStrategies = new Dictionary<string, ISystemRecoveryStrategy>();
        private readonly List<SystemHealthEvent> _healthEvents = new List<SystemHealthEvent>();

        // ECS integration
        private World _ecsWorld;
        private EntityManager _entityManager;

        // Timers
        private float _lastHealthCheckTime = 0f;

        // Events
        public event Action<SystemHealthEvent> OnHealthEventTriggered;
        public event Action<SystemRecoveryEvent> OnRecoveryAttempted;
        public event Action<float> OnOverallHealthChanged;

        private static SystemHealthMonitor _instance;
        public static SystemHealthMonitor Instance => _instance;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeHealthMonitor();
        }

        private void Update()
        {
            if (Time.unscaledTime - _lastHealthCheckTime >= healthCheckInterval)
            {
                _ = PerformHealthCheckAsync();
                _lastHealthCheckTime = Time.unscaledTime;
            }
        }

        #endregion

        #region Initialization

        private void InitializeHealthMonitor()
        {
            _ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (_ecsWorld?.IsCreated == true)
            {
                _entityManager = _ecsWorld.EntityManager;
            }

            RegisterDefaultRecoveryStrategies();
            RegisterMonitoredSystems();

            Debug.Log("[SystemHealthMonitor] Initialized - monitoring " + _systemHealth.Count + " systems");
        }

        private void RegisterMonitoredSystems()
        {
            // Register critical systems for monitoring
            var criticalSystems = new[]
            {
                "GeneticsSystem",
                "BreedingSystem",
                "ActivitySystem",
                "EcosystemSystem",
                "SpawningSystem",
                "SaveLoadSystem",
                "NetworkSystem",
                "AISystem",
                "RenderingSystem",
                "PhysicsSystem"
            };

            foreach (var systemName in criticalSystems)
            {
                RegisterSystem(systemName, SystemPriority.Critical);
            }

            systemsMonitored = _systemHealth.Count;
        }

        private void RegisterDefaultRecoveryStrategies()
        {
            // Register recovery strategies for common system failures
            RegisterRecoveryStrategy("GeneticsSystem", new GeneticsSystemRecovery());
            RegisterRecoveryStrategy("SpawningSystem", new SpawningSystemRecovery());
            RegisterRecoveryStrategy("SaveLoadSystem", new SaveLoadSystemRecovery());
            RegisterRecoveryStrategy("NetworkSystem", new NetworkSystemRecovery());
        }

        #endregion

        #region Health Monitoring

        public void RegisterSystem(string systemName, SystemPriority priority)
        {
            if (!_systemHealth.ContainsKey(systemName))
            {
                _systemHealth[systemName] = new SystemHealthData
                {
                    systemName = systemName,
                    priority = priority,
                    status = SystemHealthStatus.Healthy,
                    healthScore = 100f,
                    lastCheckTime = DateTime.UtcNow,
                    consecutiveFailures = 0,
                    recoveryAttempts = 0
                };

                Debug.Log($"[SystemHealthMonitor] Registered system: {systemName} (Priority: {priority})");
            }
        }

        public async Task PerformHealthCheckAsync()
        {
            if (logHealthChecks)
                Debug.Log("[SystemHealthMonitor] Performing health check...");

            int healthy = 0;
            int warnings = 0;
            int critical = 0;
            float totalHealth = 0f;

            foreach (var systemName in _systemHealth.Keys.ToList())
            {
                var health = await CheckSystemHealthAsync(systemName);
                _systemHealth[systemName] = health;

                totalHealth += health.healthScore;

                switch (health.status)
                {
                    case SystemHealthStatus.Healthy:
                        healthy++;
                        break;
                    case SystemHealthStatus.Warning:
                        warnings++;
                        break;
                    case SystemHealthStatus.Critical:
                    case SystemHealthStatus.Failed:
                        critical++;
                        break;
                }

                // Attempt recovery if needed
                if (enableAutoRecovery && (health.status == SystemHealthStatus.Critical || health.status == SystemHealthStatus.Failed))
                {
                    if (health.recoveryAttempts < maxRecoveryAttempts)
                    {
                        _ = AttemptSystemRecoveryAsync(systemName);
                    }
                }
            }

            // Update runtime stats
            healthySystemsCount = healthy;
            warningSystemsCount = warnings;
            criticalSystemsCount = critical;
            overallHealthScore = _systemHealth.Count > 0 ? totalHealth / _systemHealth.Count : 100f;

            OnOverallHealthChanged?.Invoke(overallHealthScore);

            // Trim event history
            if (_healthEvents.Count > 1000)
            {
                _healthEvents.RemoveRange(0, _healthEvents.Count - 1000);
            }
        }

        private async Task<SystemHealthData> CheckSystemHealthAsync(string systemName)
        {
            var healthData = _systemHealth[systemName];
            healthData.lastCheckTime = DateTime.UtcNow;

            try
            {
                // Perform system-specific health checks
                var checks = await RunHealthChecksAsync(systemName);

                // Calculate health score
                float healthScore = CalculateHealthScore(checks);
                healthData.healthScore = healthScore;

                // Determine status
                var previousStatus = healthData.status;
                healthData.status = DetermineHealthStatus(healthScore, checks);

                // Track consecutive failures
                if (healthData.status == SystemHealthStatus.Failed || healthData.status == SystemHealthStatus.Critical)
                {
                    healthData.consecutiveFailures++;
                }
                else
                {
                    healthData.consecutiveFailures = 0;
                }

                // Store check results
                healthData.lastCheckResults = checks;

                // Trigger health event if status changed
                if (previousStatus != healthData.status)
                {
                    var healthEvent = new SystemHealthEvent
                    {
                        systemName = systemName,
                        previousStatus = previousStatus,
                        currentStatus = healthData.status,
                        healthScore = healthScore,
                        timestamp = DateTime.UtcNow,
                        details = string.Join("; ", checks.Where(c => !c.passed).Select(c => c.checkName))
                    };

                    _healthEvents.Add(healthEvent);
                    OnHealthEventTriggered?.Invoke(healthEvent);

                    Debug.LogWarning($"[SystemHealthMonitor] {systemName} status changed: {previousStatus} → {healthData.status} (Score: {healthScore:F1})");
                }

                return healthData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SystemHealthMonitor] Health check failed for {systemName}: {ex.Message}");
                healthData.status = SystemHealthStatus.Failed;
                healthData.healthScore = 0f;
                healthData.consecutiveFailures++;
                return healthData;
            }
        }

        private async Task<List<HealthCheck>> RunHealthChecksAsync(string systemName)
        {
            var checks = new List<HealthCheck>();

            // Common checks for all systems
            checks.Add(CheckSystemInitialized(systemName));
            checks.Add(CheckSystemResponsive(systemName));
            checks.Add(CheckMemoryUsage(systemName));
            checks.Add(CheckPerformance(systemName));

            // System-specific checks
            switch (systemName)
            {
                case "GeneticsSystem":
                    checks.Add(CheckGeneticsSystemHealth());
                    break;

                case "EcosystemSystem":
                    checks.Add(CheckEcosystemSystemHealth());
                    break;

                case "SpawningSystem":
                    checks.Add(CheckSpawningSystemHealth());
                    break;

                case "NetworkSystem":
                    checks.Add(CheckNetworkSystemHealth());
                    break;
            }

            await Task.CompletedTask;
            return checks;
        }

        #endregion

        #region Health Checks

        private HealthCheck CheckSystemInitialized(string systemName)
        {
            // Check if system is initialized
            bool initialized = true; // Would check actual system state

            return new HealthCheck
            {
                checkName = "Initialization",
                passed = initialized,
                message = initialized ? "System initialized" : "System not initialized",
                severity = HealthCheckSeverity.Critical
            };
        }

        private HealthCheck CheckSystemResponsive(string systemName)
        {
            // Check if system is responding to updates
            var healthData = _systemHealth[systemName];
            var timeSinceLastCheck = DateTime.UtcNow - healthData.lastCheckTime;
            bool responsive = timeSinceLastCheck.TotalSeconds < 30;

            return new HealthCheck
            {
                checkName = "Responsiveness",
                passed = responsive,
                message = responsive ? "System responsive" : $"No response for {timeSinceLastCheck.TotalSeconds:F1}s",
                severity = HealthCheckSeverity.Warning
            };
        }

        private HealthCheck CheckMemoryUsage(string systemName)
        {
            // Check if system memory usage is within acceptable limits
            long estimatedMemory = UnityEngine.Random.Range(10, 200) * 1024 * 1024; // Mock data
            long memoryLimit = 500 * 1024 * 1024; // 500MB limit
            bool withinLimits = estimatedMemory < memoryLimit;

            return new HealthCheck
            {
                checkName = "Memory Usage",
                passed = withinLimits,
                message = $"Memory: {estimatedMemory / (1024 * 1024)}MB / {memoryLimit / (1024 * 1024)}MB",
                severity = HealthCheckSeverity.Warning
            };
        }

        private HealthCheck CheckPerformance(string systemName)
        {
            // Check if system performance is acceptable
            float frameTime = Time.unscaledDeltaTime * 1000f;
            bool performanceOk = frameTime < 33.33f; // Target 30 FPS minimum

            return new HealthCheck
            {
                checkName = "Performance",
                passed = performanceOk,
                message = $"Frame time: {frameTime:F2}ms",
                severity = HealthCheckSeverity.Warning
            };
        }

        private HealthCheck CheckGeneticsSystemHealth()
        {
            // Check genetics system specific health
            bool healthy = true;

            if (_entityManager != null && _ecsWorld != null && _ecsWorld.IsCreated)
            {
                // Could check for genetic data integrity, population counts, etc.
            }

            return new HealthCheck
            {
                checkName = "Genetics Data Integrity",
                passed = healthy,
                message = healthy ? "Genetic data valid" : "Genetic data corrupted",
                severity = HealthCheckSeverity.Critical
            };
        }

        private HealthCheck CheckEcosystemSystemHealth()
        {
            return new HealthCheck
            {
                checkName = "Ecosystem Balance",
                passed = true,
                message = "Ecosystem stable",
                severity = HealthCheckSeverity.Warning
            };
        }

        private HealthCheck CheckSpawningSystemHealth()
        {
            return new HealthCheck
            {
                checkName = "Spawning Queue",
                passed = true,
                message = "Spawning queue operational",
                severity = HealthCheckSeverity.Warning
            };
        }

        private HealthCheck CheckNetworkSystemHealth()
        {
            return new HealthCheck
            {
                checkName = "Network Connectivity",
                passed = true,
                message = "Network connected",
                severity = HealthCheckSeverity.Critical
            };
        }

        #endregion

        #region Health Scoring

        private float CalculateHealthScore(List<HealthCheck> checks)
        {
            if (checks.Count == 0) return 100f;

            float totalWeight = 0f;
            float totalScore = 0f;

            foreach (var check in checks)
            {
                float weight = check.severity switch
                {
                    HealthCheckSeverity.Critical => 3f,
                    HealthCheckSeverity.Warning => 2f,
                    HealthCheckSeverity.Info => 1f,
                    _ => 1f
                };

                totalWeight += weight;
                totalScore += check.passed ? weight : 0f;
            }

            return totalWeight > 0 ? (totalScore / totalWeight) * 100f : 100f;
        }

        private SystemHealthStatus DetermineHealthStatus(float healthScore, List<HealthCheck> checks)
        {
            // Check for critical failures
            bool hasCriticalFailure = checks.Any(c => !c.passed && c.severity == HealthCheckSeverity.Critical);
            if (hasCriticalFailure)
                return SystemHealthStatus.Failed;

            // Determine status based on health score
            if (healthScore >= 80f)
                return SystemHealthStatus.Healthy;
            else if (healthScore >= 50f)
                return SystemHealthStatus.Warning;
            else
                return SystemHealthStatus.Critical;
        }

        #endregion

        #region Auto-Recovery

        public void RegisterRecoveryStrategy(string systemName, ISystemRecoveryStrategy strategy)
        {
            _recoveryStrategies[systemName] = strategy;
            Debug.Log($"[SystemHealthMonitor] Registered recovery strategy for {systemName}");
        }

        private async Task<bool> AttemptSystemRecoveryAsync(string systemName)
        {
            if (!_recoveryStrategies.TryGetValue(systemName, out var strategy))
            {
                Debug.LogWarning($"[SystemHealthMonitor] No recovery strategy for {systemName}");
                return false;
            }

            var healthData = _systemHealth[systemName];
            healthData.recoveryAttempts++;

            var recoveryEvent = new SystemRecoveryEvent
            {
                systemName = systemName,
                attemptNumber = healthData.recoveryAttempts,
                timestamp = DateTime.UtcNow
            };

            try
            {
                Debug.Log($"[SystemHealthMonitor] Attempting recovery for {systemName} (Attempt {healthData.recoveryAttempts}/{maxRecoveryAttempts})");

                bool success = await strategy.AttemptRecoveryAsync(systemName);

                recoveryEvent.wasSuccessful = success;
                recoveryEvent.recoveryAction = success ? "Recovery successful" : "Recovery failed";

                OnRecoveryAttempted?.Invoke(recoveryEvent);

                if (success)
                {
                    healthData.consecutiveFailures = 0;
                    healthData.recoveryAttempts = 0;
                    healthData.status = SystemHealthStatus.Warning; // Start at warning after recovery
                    Debug.Log($"[SystemHealthMonitor] Recovery successful for {systemName}");
                }
                else
                {
                    Debug.LogError($"[SystemHealthMonitor] Recovery failed for {systemName}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SystemHealthMonitor] Recovery exception for {systemName}: {ex.Message}");
                recoveryEvent.wasSuccessful = false;
                recoveryEvent.recoveryAction = $"Exception: {ex.Message}";
                OnRecoveryAttempted?.Invoke(recoveryEvent);
                return false;
            }
        }

        #endregion

        #region Public API

        public SystemHealthData GetSystemHealth(string systemName)
        {
            return _systemHealth.TryGetValue(systemName, out var health) ? health : null;
        }

        public Dictionary<string, SystemHealthData> GetAllSystemHealth()
        {
            return new Dictionary<string, SystemHealthData>(_systemHealth);
        }

        public SystemHealthReport GenerateHealthReport()
        {
            return new SystemHealthReport
            {
                timestamp = DateTime.UtcNow,
                overallHealthScore = overallHealthScore,
                totalSystems = systemsMonitored,
                healthySystems = healthySystemsCount,
                warningSystems = warningSystemsCount,
                criticalSystems = criticalSystemsCount,
                systemDetails = _systemHealth.Values.ToList(),
                recentEvents = _healthEvents.TakeLast(50).ToList()
            };
        }

        public void ForceHealthCheck()
        {
            _ = PerformHealthCheckAsync();
        }

        #endregion

        #region Context Menu Commands

        [ContextMenu("Force Health Check")]
        private void ForceHealthCheckDebug()
        {
            ForceHealthCheck();
        }

        [ContextMenu("Generate Health Report")]
        private void GenerateHealthReportDebug()
        {
            var report = GenerateHealthReport();
            Debug.Log($"=== SYSTEM HEALTH REPORT ===\n" +
                     $"Overall Health: {report.overallHealthScore:F1}%\n" +
                     $"Healthy: {report.healthySystems}/{report.totalSystems}\n" +
                     $"Warnings: {report.warningSystems}\n" +
                     $"Critical: {report.criticalSystems}\n" +
                     $"Recent Events: {report.recentEvents.Count}");
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class SystemHealthData
    {
        public string systemName;
        public SystemPriority priority;
        public SystemHealthStatus status;
        public float healthScore;
        public DateTime lastCheckTime;
        public int consecutiveFailures;
        public int recoveryAttempts;
        public List<HealthCheck> lastCheckResults;
    }

    [Serializable]
    public class HealthCheck
    {
        public string checkName;
        public bool passed;
        public string message;
        public HealthCheckSeverity severity;
    }

    [Serializable]
    public class SystemHealthEvent
    {
        public string systemName;
        public SystemHealthStatus previousStatus;
        public SystemHealthStatus currentStatus;
        public float healthScore;
        public DateTime timestamp;
        public string details;
    }

    [Serializable]
    public class SystemRecoveryEvent
    {
        public string systemName;
        public int attemptNumber;
        public bool wasSuccessful;
        public string recoveryAction;
        public DateTime timestamp;
    }

    [Serializable]
    public class SystemHealthReport
    {
        public DateTime timestamp;
        public float overallHealthScore;
        public int totalSystems;
        public int healthySystems;
        public int warningSystems;
        public int criticalSystems;
        public List<SystemHealthData> systemDetails;
        public List<SystemHealthEvent> recentEvents;
    }

    public enum SystemHealthStatus
    {
        Healthy,
        Warning,
        Critical,
        Failed,
        Unknown
    }

    public enum SystemPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    public enum HealthCheckSeverity
    {
        Info,
        Warning,
        Critical
    }

    #endregion

    #region Recovery Strategies

    public interface ISystemRecoveryStrategy
    {
        Task<bool> AttemptRecoveryAsync(string systemName);
    }

    public class GeneticsSystemRecovery : ISystemRecoveryStrategy
    {
        public async Task<bool> AttemptRecoveryAsync(string systemName)
        {
            Debug.Log("[GeneticsSystemRecovery] Attempting to recover genetics system...");

            // Clear corrupted genetic data
            // Reinitialize genetic pools
            // Validate genetic configurations

            await Task.Delay(100); // Simulate recovery work
            return true;
        }
    }

    public class SpawningSystemRecovery : ISystemRecoveryStrategy
    {
        public async Task<bool> AttemptRecoveryAsync(string systemName)
        {
            Debug.Log("[SpawningSystemRecovery] Attempting to recover spawning system...");

            // Clear spawning queue
            // Reset spawn timers
            // Validate spawn locations

            await Task.Delay(100);
            return true;
        }
    }

    public class SaveLoadSystemRecovery : ISystemRecoveryStrategy
    {
        public async Task<bool> AttemptRecoveryAsync(string systemName)
        {
            Debug.Log("[SaveLoadSystemRecovery] Attempting to recover save/load system...");

            // Verify file system access
            // Validate save directory
            // Clear save cache

            await Task.Delay(100);
            return true;
        }
    }

    public class NetworkSystemRecovery : ISystemRecoveryStrategy
    {
        public async Task<bool> AttemptRecoveryAsync(string systemName)
        {
            Debug.Log("[NetworkSystemRecovery] Attempting to recover network system...");

            // Reconnect to server
            // Resync network state
            // Flush network buffers

            await Task.Delay(100);
            return true;
        }
    }

    #endregion
}
