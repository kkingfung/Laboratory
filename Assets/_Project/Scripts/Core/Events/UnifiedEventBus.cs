using System;
using MessagePipe;
using UniRx;
using UnityEngine;

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
        
        private readonly IMessageBroker _messageBroker;
        private readonly CompositeDisposable _disposables = new();
        private bool _disposed = false;
        
        #endregion
        
        #region Constructor
        
        public UnifiedEventBus(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
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
            
            _messageBroker.Publish(message);
        }
        
        public IDisposable Subscribe<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            
            return _messageBroker.Receive<T>().Subscribe(handler);
        }
        
        public IObservable<T> Observe<T>() where T : class
        {
            ThrowIfDisposed();
            return _messageBroker.Receive<T>().AsObservable();
        }
        
        public IDisposable SubscribeOnMainThread<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            
            return _messageBroker.Receive<T>()
                .ObserveOnMainThread()
                .Subscribe(handler);
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposables?.Dispose();
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
        public GameStateManager.GameState FromState { get; }
        public GameStateManager.GameState ToState { get; }
        public object? Context { get; }
        
        public GameStateChangeRequestedEvent(
            GameStateManager.GameState fromState, 
            GameStateManager.GameState toState, 
            object? context = null)
        {
            FromState = fromState;
            ToState = toState;
            Context = context;
        }
    }
    
    public class GameStateChangedEvent
    {
        public GameStateManager.GameState PreviousState { get; }
        public GameStateManager.GameState CurrentState { get; }
        public DateTime ChangedAt { get; }
        
        public GameStateChangedEvent(GameStateManager.GameState previousState, GameStateManager.GameState currentState)
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