using System.Threading.Tasks;

namespace Laboratory.Core.Infrastructure
{
    /// <summary>
    /// Base interface for all subsystem managers in Project Chimera.
    /// Provides standardized initialization, lifecycle management, and monitoring.
    /// </summary>
    public interface ISubsystemManager
    {
        /// <summary>
        /// Unique name identifier for this subsystem
        /// </summary>
        string SubsystemName { get; }

        /// <summary>
        /// Whether the subsystem has completed initialization
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Current initialization progress (0.0 to 1.0)
        /// </summary>
        float InitializationProgress { get; }
    }

    /// <summary>
    /// Extended interface for subsystems that support async initialization
    /// </summary>
    public interface IAsyncSubsystemManager : ISubsystemManager
    {
        /// <summary>
        /// Asynchronously initializes the subsystem
        /// </summary>
        Task InitializeAsync();
    }

    /// <summary>
    /// Interface for subsystems that require cleanup on shutdown
    /// </summary>
    public interface ICleanupableSubsystem : ISubsystemManager
    {
        /// <summary>
        /// Performs cleanup operations before shutdown
        /// </summary>
        void Cleanup();
    }

    /// <summary>
    /// Interface for subsystems that can be paused and resumed
    /// </summary>
    public interface IPausableSubsystem : ISubsystemManager
    {
        /// <summary>
        /// Whether the subsystem is currently paused
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// Pauses subsystem operations
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes subsystem operations
        /// </summary>
        void Resume();
    }

    /// <summary>
    /// Interface for subsystems that provide performance metrics
    /// </summary>
    public interface IMonitorableSubsystem : ISubsystemManager
    {
        /// <summary>
        /// Gets current performance metrics
        /// </summary>
        SubsystemMetrics GetMetrics();
    }

    /// <summary>
    /// Performance metrics for subsystem monitoring
    /// </summary>
    public class SubsystemMetrics
    {
        public string subsystemName;
        public float cpuUsage;
        public float memoryUsage;
        public int activeOperations;
        public float averageResponseTime;
        public int errorsPerMinute;
        public System.DateTime lastUpdate;
    }
}