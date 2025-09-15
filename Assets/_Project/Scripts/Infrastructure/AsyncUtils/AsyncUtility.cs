using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#nullable enable

namespace Laboratory.Infrastructure.AsyncUtils
{
    /// <summary>
    /// Utility methods for async operations in Unity.
    /// Provides helpers for common async patterns and Unity-specific async operations.
    /// </summary>
    public static class AsyncUtility
    {
        /// <summary>
        /// Runs an async operation on the main thread and returns a Task.
        /// </summary>
        public static async Task RunOnMainThread(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            
            var tcs = new TaskCompletionSource<bool>();
            
            if (Application.isPlaying)
            {
                // Queue the action to run on the main thread
                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    try
                    {
                        action();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
            }
            else
            {
                // If not playing, run immediately
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }
            
            await tcs.Task;
        }
        
        /// <summary>
        /// Creates a task that completes after the specified delay on the main thread.
        /// </summary>
        public static async Task DelayMainThread(float seconds)
        {
            if (seconds <= 0f) return;
            
            var tcs = new TaskCompletionSource<bool>();
            var startTime = Time.realtimeSinceStartup;
            
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                CheckDelayComplete(tcs, startTime, seconds);
            });
            
            await tcs.Task;
        }
        
        /// <summary>
        /// Creates a cancellable delay task.
        /// </summary>
        public static async Task Delay(float seconds, CancellationToken cancellationToken = default)
        {
            if (seconds <= 0f) return;
            
            var delayMs = (int)(seconds * 1000f);
            await Task.Delay(delayMs, cancellationToken);
        }
        
        /// <summary>
        /// Safely ignores a task without causing unobserved task exceptions.
        /// </summary>
        public static void Forget(this Task task)
        {
            if (task == null) return;
            
            task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    Debug.LogError($"Unhandled exception in forgotten task: {t.Exception}");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        
        /// <summary>
        /// Wraps a task with timeout functionality.
        /// </summary>
        public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource();
            var timeoutTask = Task.Delay(timeout, cts.Token);
            
            var completedTask = await Task.WhenAny(task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException($"Task timed out after {timeout.TotalSeconds} seconds");
            }
            
            cts.Cancel(); // Cancel the timeout task
            return await task;
        }
        
        private static void CheckDelayComplete(TaskCompletionSource<bool> tcs, float startTime, float duration)
        {
            if (Time.realtimeSinceStartup - startTime >= duration)
            {
                tcs.SetResult(true);
            }
            else
            {
                // Schedule another check next frame
                UnityMainThreadDispatcher.Enqueue(() =>
                {
                    CheckDelayComplete(tcs, startTime, duration);
                });
            }
        }
    }
    
    /// <summary>
    /// Simple main thread dispatcher for Unity.
    /// Allows execution of actions on the main thread from background threads.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher? _instance;
        private static readonly Queue<Action> _executionQueue = new();
        
        /// <summary>
        /// Gets the singleton instance, creating it if necessary.
        /// </summary>
        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    CreateInstance();
                }
                return _instance!;
            }
        }
        
        /// <summary>
        /// Enqueues an action to be executed on the main thread.
        /// </summary>
        public static void Enqueue(Action action)
        {
            if (action == null) return;
            
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }
        
        private void Update()
        {
            // Execute all queued actions on the main thread
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    var action = _executionQueue.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error executing main thread action: {ex}");
                    }
                }
            }
        }
        
        private static void CreateInstance()
        {
            var go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
