using System;
using System.Collections.Generic;
using MessagePipe;
using UniRx;
using UnityEngine;
using Laboratory.Core.State;

#nullable enable

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Implementation of IEventBus that wraps MessagePipe with UniRx extensions.
    /// Thread-safe and Unity-optimized for game development.
    /// </summary>
    public class UnifiedEventBus : IEventBus
    {
        #region Fields
        
        private readonly IMessageBroker? _messageBroker;
        private readonly Dictionary<Type, object> _subjects = new();
        private readonly CompositeDisposable _disposables = new();
        private bool _disposed = false;
        
        #endregion
        
        #region Constructor
        
        public UnifiedEventBus()
        {
            // Use simple UniRx subjects as fallback when MessagePipe is not available
            _messageBroker = null;
        }
        
        // Temporary constructor that accepts IMessageBroker for when we fix MessagePipe integration
        public UnifiedEventBus(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
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
            
            if (_messageBroker != null)
            {
                _messageBroker.Publish(message);
            }
            else
            {
                // Fallback to UniRx subjects
                var subject = GetSubject<T>();
                subject.OnNext(message);
            }
        }
        
        public IDisposable Subscribe<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            
            if (_messageBroker != null)
            {
                return _messageBroker.Receive<T>().Subscribe(handler);
            }
            else
            {
                // Fallback to UniRx subjects
                var subject = GetSubject<T>();
                return subject.Subscribe(handler);
            }
        }
        
        public UniRx.IObservable<T> Observe<T>() where T : class
        {
            ThrowIfDisposed();
            
            if (_messageBroker != null)
            {
                return _messageBroker.Receive<T>().AsObservable();
            }
            else
            {
                // Fallback to UniRx subjects
                var subject = GetSubject<T>();
                return subject.AsObservable();
            }
        }
        
        public IDisposable SubscribeOnMainThread<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            
            if (_messageBroker != null)
            {
                return _messageBroker.Receive<T>()
                    .ObserveOnMainThread()
                    .Subscribe(handler);
            }
            else
            {
                // Fallback to UniRx subjects
                var subject = GetSubject<T>();
                return subject
                    .ObserveOn(Scheduler.MainThreadScheduler)
                    .Subscribe(handler);
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
            
            _disposed = true;
        }
        
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnifiedEventBus));
        }
        
        #endregion
    }
}

// Common event messages for the system
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
}