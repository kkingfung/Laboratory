using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Laboratory.Core.State;

#nullable enable

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Enhanced implementation of IEventBus that removes MessagePipe dependencies
    /// and provides a stable, Unity-optimized event system using UniRx.
    /// Thread-safe and performant for game development.
    /// </summary>
    public class UnifiedEventBus : IEventBus, IDisposable
    {
        #region Fields
        
        private readonly Dictionary<Type, object> _subjects = new();
        private readonly CompositeDisposable _disposables = new();
        private bool _disposed = false;
        
        #endregion
        
        #region Constructor
        
        public UnifiedEventBus()
        {
            Debug.Log("UnifiedEventBus: Initialized with UniRx backend");
        }
        
        #endregion
        
        #region IEventBus Implementation
        
        public void Publish<T>(T message) where T : class
        {
            ThrowIfDisposed();
            
            if (message == null) 
            {
                Debug.LogWarning($"Attempted to publish null message of type {typeof(T).Name}");
                return;
            }
            
            var subject = GetSubject<T>();
            try
            {
                subject.OnNext(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error publishing event {typeof(T).Name}: {ex}");
            }
        }
        
        public IDisposable Subscribe<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            
            var subject = GetSubject<T>();
            return subject
                .Subscribe(
                    onNext: handler,
                    onError: ex => Debug.LogError($"Error in event handler for {typeof(T).Name}: {ex}")
                );
        }
        
        public UniRx.IObservable<T> Observe<T>() where T : class
        {
            ThrowIfDisposed();
            
            var subject = GetSubject<T>();
            return subject.AsObservable();
        }
        
        public IDisposable SubscribeOnMainThread<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            
            var subject = GetSubject<T>();
            return subject
                .ObserveOnMainThread()
                .Subscribe(
                    onNext: handler,
                    onError: ex => Debug.LogError($"Error in main thread event handler for {typeof(T).Name}: {ex}")
                );
        }
        
        #endregion
        
        #region Enhanced Features
        
        /// <summary>
        /// Subscribe with filtering predicate.
        /// </summary>
        public IDisposable SubscribeWhere<T>(Func<T, bool> predicate, Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            
            var subject = GetSubject<T>();
            return subject
                .Where(predicate)
                .Subscribe(
                    onNext: handler,
                    onError: ex => Debug.LogError($"Error in filtered event handler for {typeof(T).Name}: {ex}")
                );
        }
        
        /// <summary>
        /// Subscribe for only the first occurrence of an event.
        /// </summary>
        public IDisposable SubscribeFirst<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            
            var subject = GetSubject<T>();
            return subject
                .First()
                .Subscribe(
                    onNext: handler,
                    onError: ex => Debug.LogError($"Error in first-only event handler for {typeof(T).Name}: {ex}")
                );
        }
        
        /// <summary>
        /// Get count of active subscribers for a specific event type.
        /// Note: UniRx doesn't expose exact subscriber count, so this returns whether there are any subscribers.
        /// </summary>
        public bool HasSubscribers<T>() where T : class
        {
            ThrowIfDisposed();
            
            if (_subjects.TryGetValue(typeof(T), out var subject))
            {
                return ((Subject<T>)subject).HasObservers;
            }
            return false;
        }
        
        /// <summary>
        /// Get count of registered event types.
        /// </summary>
        public int GetEventTypeCount()
        {
            ThrowIfDisposed();
            return _subjects.Count;
        }
        
        /// <summary>
        /// Clear all subjects and subscriptions for a specific type.
        /// </summary>
        public void ClearSubscriptions<T>() where T : class
        {
            ThrowIfDisposed();
            
            var type = typeof(T);
            if (_subjects.TryGetValue(type, out var subject))
            {
                ((Subject<T>)subject)?.Dispose();
                _subjects.Remove(type);
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private Subject<T> GetSubject<T>() where T : class
        {
            var type = typeof(T);
            if (!_subjects.TryGetValue(type, out var subject))
            {
                subject = new Subject<T>();
                _subjects[type] = subject;
                
                // Ensure subject gets disposed with the event bus
                ((Subject<T>)subject).AddTo(_disposables);
            }
            return (Subject<T>)subject;
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                _disposables?.Dispose();
                
                // Dispose individual subjects
                foreach (var subject in _subjects.Values)
                {
                    if (subject is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _subjects.Clear();
                
                Debug.Log("UnifiedEventBus: Disposed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error disposing UnifiedEventBus: {ex}");
            }
            finally
            {
                _disposed = true;
            }
        }
        
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnifiedEventBus));
        }
        
        #endregion
    }
}

// Enhanced event messages for the system
namespace Laboratory.Core.Events.Messages
{
    #region System Events
    
    public class SystemInitializedEvent
    {
        public string SystemName { get; }
        public DateTime InitializedAt { get; }
        
        public SystemInitializedEvent(string systemName)
        {
            SystemName = systemName;
            InitializedAt = DateTime.UtcNow;
        }
    }
    
    public class SystemShutdownEvent
    {
        public string SystemName { get; }
        public DateTime ShutdownAt { get; }
        
        public SystemShutdownEvent(string systemName)
        {
            SystemName = systemName;
            ShutdownAt = DateTime.UtcNow;
        }
    }
    
    #endregion
    
    #region Loading Events
    
    public class LoadingStartedEvent
    {
        public string OperationName { get; }
        public string Description { get; }
        
        public LoadingStartedEvent(string operationName, string description = "")
        {
            OperationName = operationName;
            Description = description;
        }
    }
    
    public class LoadingProgressEvent
    {
        public string OperationName { get; }
        public float Progress { get; }
        public string? StatusText { get; }
        
        public LoadingProgressEvent(string operationName, float progress, string? statusText = null)
        {
            OperationName = operationName;
            Progress = UnityEngine.Mathf.Clamp01(progress);
            StatusText = statusText;
        }
    }
    
    public class LoadingCompletedEvent
    {
        public string OperationName { get; }
        public bool Success { get; }
        public string? ErrorMessage { get; }
        
        public LoadingCompletedEvent(string operationName, bool success, string? errorMessage = null)
        {
            OperationName = operationName;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
    
    #endregion
    
    #region Game State Events
    
    public class GameStateChangeRequestedEvent
    {
        public GameState FromState { get; }
        public GameState ToState { get; }
        public object? Context { get; }
        
        public GameStateChangeRequestedEvent(
            GameState fromState, 
            GameState toState, 
            object? context = null)
        {
            FromState = fromState;
            ToState = toState;
            Context = context;
        }
    }
    
    public class GameStateChangedEvent
    {
        public GameState PreviousState { get; }
        public GameState CurrentState { get; }
        public DateTime ChangedAt { get; }
        
        public GameStateChangedEvent(GameState previousState, GameState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            ChangedAt = DateTime.UtcNow;
        }
    }
    
    #endregion
    
    #region Scene Events
    
    public class SceneChangeRequestedEvent
    {
        public string SceneName { get; }
        public UnityEngine.SceneManagement.LoadSceneMode LoadMode { get; }
        
        public SceneChangeRequestedEvent(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadMode)
        {
            SceneName = sceneName;
            LoadMode = loadMode;
        }
    }
    
    #endregion
    
    #region Health & Combat Events
    
    /// <summary>
    /// Unified damage event that replaces fragmented damage events across the system.
    /// </summary>
    public class DamageEvent
    {
        public UnityEngine.GameObject Target { get; }
        public UnityEngine.GameObject Source { get; }
        public float Amount { get; }
        public Laboratory.Core.Health.DamageType Type { get; }
        public UnityEngine.Vector3 Direction { get; }
        public ulong TargetClientId { get; }
        public ulong AttackerClientId { get; }

        public DamageEvent(UnityEngine.GameObject target, UnityEngine.GameObject source, float amount, 
            Laboratory.Core.Health.DamageType type, UnityEngine.Vector3 direction, 
            ulong targetClientId = 0, ulong attackerClientId = 0)
        {
            Target = target;
            Source = source;
            Amount = amount;
            Type = type;
            Direction = direction;
            TargetClientId = targetClientId;
            AttackerClientId = attackerClientId;
        }
    }

    /// <summary>
    /// Unified death event that replaces fragmented death events across the system.
    /// </summary>
    public class DeathEvent
    {
        public UnityEngine.GameObject Target { get; }
        public UnityEngine.GameObject Source { get; }
        public ulong VictimClientId { get; }
        public ulong KillerClientId { get; }

        public DeathEvent(UnityEngine.GameObject target, UnityEngine.GameObject source = null,
            ulong victimClientId = 0, ulong killerClientId = 0)
        {
            Target = target;
            Source = source;
            VictimClientId = victimClientId;
            KillerClientId = killerClientId;
        }
    }
    
    #endregion
}
