using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.Bootstrap
{
    /// <summary>
    /// Interface for startup tasks that need to be executed during game initialization.
    /// Provides ordering, dependency management, and progress reporting.
    /// </summary>
    public interface IStartupTask
    {
        /// <summary>
        /// Task execution priority (lower numbers execute first).
        /// Use ranges: 0-99 (Core), 100-199 (Services), 200-299 (Game Systems), 300+ (UI/Content)
        /// </summary>
        int Priority { get; }
        
        /// <summary>Human-readable name for logging and debugging.</summary>
        string Name { get; }
        
        /// <summary>Optional dependencies that must complete before this task can run.</summary>
        IReadOnlyList<Type> Dependencies { get; }
        
        /// <summary>Estimated duration for progress reporting (optional).</summary>
        TimeSpan EstimatedDuration { get; }
        
        /// <summary>Executes the startup task with progress reporting and cancellation support.</summary>
        UniTask ExecuteAsync(ServiceContainer services, IProgress<float>? progress, CancellationToken cancellation);
    }

    /// <summary>
    /// Base implementation of IStartupTask with common functionality.
    /// </summary>
    public abstract class StartupTaskBase : IStartupTask
    {
        public abstract int Priority { get; }
        public abstract string Name { get; }
        public virtual IReadOnlyList<Type> Dependencies => Array.Empty<Type>();
        public virtual TimeSpan EstimatedDuration => TimeSpan.FromSeconds(1);
        
        public abstract UniTask ExecuteAsync(ServiceContainer services, IProgress<float>? progress, CancellationToken cancellation);
        
        protected void ReportProgress(IProgress<float>? progress, float value)
        {
            progress?.Report(Mathf.Clamp01(value));
        }
        
        protected void LogInfo(string message)
        {
            Debug.Log($"[{Name}] {message}");
        }
        
        protected void LogError(string message, Exception? exception = null)
        {
            if (exception != null)
                Debug.LogError($"[{Name}] {message}: {exception}");
            else
                Debug.LogError($"[{Name}] {message}");
        }
    }

    /// <summary>
    /// Orchestrates the execution of startup tasks with proper ordering, dependency resolution,
    /// progress reporting, and error handling. Replaces manual initialization in GameBootstrap.
    /// </summary>
    public class StartupOrchestrator
    {
        #region Fields
        
        private readonly List<IStartupTask> _tasks = new();
        private readonly Dictionary<Type, IStartupTask> _tasksByType = new();
        private readonly Dictionary<Type, TaskExecutionInfo> _executionInfo = new();
        private IEventBus? _eventBus;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Adds a startup task to the orchestration pipeline.
        /// </summary>
        public void AddTask<T>() where T : IStartupTask, new()
        {
            AddTask(new T());
        }
        
        /// <summary>
        /// Adds a startup task instance to the orchestration pipeline.
        /// </summary>
        public void AddTask(IStartupTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
                
            _tasks.Add(task);
            _tasksByType[task.GetType()] = task;
        }
        
        /// <summary>
        /// Adds multiple startup tasks of specified types.
        /// </summary>
        public void AddTasks(params Type[] taskTypes)
        {
            foreach (var taskType in taskTypes)
            {
                if (!typeof(IStartupTask).IsAssignableFrom(taskType))
                    throw new ArgumentException($"Type {taskType.Name} does not implement IStartupTask");
                    
                if (Activator.CreateInstance(taskType) is IStartupTask task)
                {
                    AddTask(task);
                }
                else
                {
                    throw new InvalidOperationException($"Failed to create instance of {taskType.Name}");
                }
            }
        }
        
        /// <summary>
        /// Executes all registered startup tasks with proper ordering and dependency resolution.
        /// </summary>
        public async UniTask InitializeAsync(ServiceContainer services, IProgress<float>? overallProgress = null, CancellationToken cancellation = default)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
                
            Debug.Log($"StartupOrchestrator: Beginning initialization with {_tasks.Count} tasks");
            
            // Get event bus for progress reporting (optional)
            _eventBus = services.ResolveService<IEventBus>();
            
            try
            {
                var orderedTasks = ResolveTaskOrder();
                await ExecuteTasksInOrder(orderedTasks, services, overallProgress, cancellation);
                
                Debug.Log("StartupOrchestrator: All startup tasks completed successfully");
                _eventBus?.Publish(new SystemInitializedEvent("StartupOrchestrator"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"StartupOrchestrator: Initialization failed: {ex}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets information about task execution status and timing.
        /// </summary>
        public IReadOnlyDictionary<Type, TaskExecutionInfo> GetExecutionInfo()
        {
            return _executionInfo;
        }
        
        #endregion
        
        #region Private Methods
        
        private List<IStartupTask> ResolveTaskOrder()
        {
            var ordered = new List<IStartupTask>();
            var visited = new HashSet<Type>();
            var visiting = new HashSet<Type>();
            
            // Sort by priority first
            var tasksByPriority = _tasks.OrderBy(t => t.Priority).ToList();
            
            foreach (var task in tasksByPriority)
            {
                if (!visited.Contains(task.GetType()))
                {
                    ResolveTaskDependencies(task, ordered, visited, visiting);
                }
            }
            
            return ordered;
        }
        
        private void ResolveTaskDependencies(IStartupTask task, List<IStartupTask> ordered, HashSet<Type> visited, HashSet<Type> visiting)
        {
            var taskType = task.GetType();
            
            if (visiting.Contains(taskType))
            {
                throw new InvalidOperationException($"Circular dependency detected involving task {task.Name}");
            }
            
            if (visited.Contains(taskType))
            {
                return; // Already processed
            }
            
            visiting.Add(taskType);
            
            // Process dependencies first
            foreach (var dependencyType in task.Dependencies)
            {
                if (_tasksByType.TryGetValue(dependencyType, out var dependencyTask))
                {
                    ResolveTaskDependencies(dependencyTask, ordered, visited, visiting);
                }
                else
                {
                    Debug.LogWarning($"StartupOrchestrator: Dependency {dependencyType.Name} for task {task.Name} is not registered");
                }
            }
            
            visiting.Remove(taskType);
            visited.Add(taskType);
            ordered.Add(task);
        }
        
        private async UniTask ExecuteTasksInOrder(List<IStartupTask> orderedTasks, ServiceContainer services, IProgress<float>? overallProgress, CancellationToken cancellation)
        {
            var totalTasks = orderedTasks.Count;
            var completedTasks = 0;
            
            foreach (var task in orderedTasks)
            {
                var taskType = task.GetType();
                var executionInfo = new TaskExecutionInfo
                {
                    StartTime = DateTime.UtcNow,
                    Status = TaskExecutionStatus.Running
                };
                _executionInfo[taskType] = executionInfo;
                
                try
                {
                    Debug.Log($"StartupOrchestrator: Executing task '{task.Name}' (Priority: {task.Priority})");
                    
                    // Publish task start event
                    _eventBus?.Publish(new LoadingStartedEvent($"StartupTask:{task.Name}", $"Initializing {task.Name}"));
                    
                    // Create progress reporter for this task
                    var taskProgress = new Progress<float>(progress =>
                    {
                        var overallTaskProgress = (completedTasks + progress) / totalTasks;
                        overallProgress?.Report(overallTaskProgress);
                        
                        _eventBus?.Publish(new LoadingProgressEvent($"StartupTask:{task.Name}", progress));
                    });
                    
                    await task.ExecuteAsync(services, taskProgress, cancellation);
                    
                    executionInfo.EndTime = DateTime.UtcNow;
                    executionInfo.Status = TaskExecutionStatus.Completed;
                    executionInfo.Duration = executionInfo.EndTime.Value - executionInfo.StartTime;
                    
                    // Publish task completion event
                    _eventBus?.Publish(new LoadingCompletedEvent($"StartupTask:{task.Name}", true));
                    
                    Debug.Log($"StartupOrchestrator: Task '{task.Name}' completed in {executionInfo.Duration.TotalMilliseconds:F2}ms");
                }
                catch (OperationCanceledException)
                {
                    executionInfo.Status = TaskExecutionStatus.Cancelled;
                    executionInfo.EndTime = DateTime.UtcNow;
                    Debug.LogWarning($"StartupOrchestrator: Task '{task.Name}' was cancelled");
                    throw;
                }
                catch (Exception ex)
                {
                    executionInfo.Status = TaskExecutionStatus.Failed;
                    executionInfo.EndTime = DateTime.UtcNow;
                    executionInfo.Error = ex;
                    
                    _eventBus?.Publish(new LoadingCompletedEvent($"StartupTask:{task.Name}", false, ex.Message));
                    
                    Debug.LogError($"StartupOrchestrator: Task '{task.Name}' failed: {ex}");
                    throw new StartupTaskException($"Startup task '{task.Name}' failed", ex);
                }
                
                completedTasks++;
                overallProgress?.Report((float)completedTasks / totalTasks);
            }
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Information about task execution status and timing.
    /// </summary>
    public class TaskExecutionInfo
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public TaskExecutionStatus Status { get; set; }
        public Exception? Error { get; set; }
    }
    
    /// <summary>
    /// Task execution status enumeration.
    /// </summary>
    public enum TaskExecutionStatus
    {
        NotStarted,
        Running,
        Completed,
        Failed,
        Cancelled
    }
    
    /// <summary>
    /// Exception thrown when a startup task fails.
    /// </summary>
    public class StartupTaskException : Exception
    {
        public StartupTaskException(string message) : base(message) { }
        public StartupTaskException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    #endregion
}
