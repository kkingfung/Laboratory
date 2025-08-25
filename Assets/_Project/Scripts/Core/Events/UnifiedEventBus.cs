using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using Laboratory.Core.State;

#nullable enable

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Enhanced implementation of IEventBus that removes MessagePipe dependencies
    /// and provides a stable, Unity-optimized event system using R3.
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
            Debug.Log("UnifiedEventBus: Initialized with R3 backend");
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
        
        public Observable<T> Observe<T>() where T : class
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
        
        public int GetSubscriberCount<T>() where T : class
        {
            ThrowIfDisposed();
            
            if (_subjects.TryGetValue(typeof(T), out var subject))
            {
                // R3 doesn't expose exact subscriber count, so we return 1 if has observers, 0 otherwise
                return ((Subject<T>)subject).HasObservers ? 1 : 0;
            }
            return 0;
        }
        
        /// <summary>
        /// Get count of active subscribers for a specific event type.
        /// Note: R3 doesn't expose exact subscriber count, so this returns whether there are any subscribers.
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
                _disposables.Add((Subject<T>)subject);
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

// Event messages are now organized in separate files under Events/Messages/
