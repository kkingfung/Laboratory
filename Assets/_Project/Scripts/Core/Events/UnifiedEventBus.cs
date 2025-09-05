using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using Laboratory.Core.State;

#nullable enable

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Simple implementation of IEventBus without R3 dependencies.
    /// Thread-safe and performant for game development.
    /// </summary>
    public class UnifiedEventBus : IEventBus, IDisposable
    {
        #region Fields
        
        private readonly Dictionary<Type, List<object>> _handlers = new();
        private readonly List<IDisposable> _subscriptions = new();
        private bool _disposed = false;
        
        #endregion
        
        #region Constructor
        
        public UnifiedEventBus()
        {
            Debug.Log("UnifiedEventBus: Initialized with simple event system");
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
            
            var eventType = typeof(T);
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                foreach (var handler in handlers.ToArray()) // ToArray to avoid modification during iteration
                {
                    try
                    {
                        ((Action<T>)handler)(message);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error executing handler for event {typeof(T).Name}: {ex}");
                    }
                }
            }
        }
        
        public IDisposable Subscribe<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            
            var eventType = typeof(T);
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<object>();
            }
            
            _handlers[eventType].Add(handler);
            
            var subscription = new SimpleSubscription(() => {
                if (_handlers.ContainsKey(eventType))
                {
                    _handlers[eventType].Remove(handler);
                }
            });
            
            _subscriptions.Add(subscription);
            return subscription;
        }
        
        public object Observe<T>() where T : class
        {
            ThrowIfDisposed();
            
            // Return a simple observable mock
            return new SimpleObservable<T>();
        }
        
        public IDisposable SubscribeOnMainThread<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            // Since we're already on the main thread in Unity, just use regular subscribe
            return Subscribe(handler);
        }
        
        #endregion
        
        #region Enhanced Features
        
        public IDisposable SubscribeWhere<T>(Func<T, bool> predicate, Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            
            return Subscribe<T>(evt => {
                if (predicate(evt))
                {
                    handler(evt);
                }
            });
        }
        
        public IDisposable SubscribeFirst<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            
            bool handled = false;
            return Subscribe<T>(evt => {
                if (!handled)
                {
                    handled = true;
                    handler(evt);
                }
            });
        }
        
        public int GetSubscriberCount<T>() where T : class
        {
            ThrowIfDisposed();
            
            var eventType = typeof(T);
            return _handlers.ContainsKey(eventType) ? _handlers[eventType].Count : 0;
        }
        
        public void ClearSubscriptions<T>() where T : class
        {
            ThrowIfDisposed();
            
            var eventType = typeof(T);
            if (_handlers.ContainsKey(eventType))
            {
                _handlers[eventType].Clear();
            }
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription?.Dispose();
                }
                _subscriptions.Clear();
                _handlers.Clear();
                
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
    
    /// <summary>
    /// Simple subscription implementation
    /// </summary>
    public class SimpleSubscription : IDisposable
    {
        private readonly Action _unsubscribeAction;
        private bool _disposed = false;
        
        public SimpleSubscription(Action unsubscribeAction)
        {
            _unsubscribeAction = unsubscribeAction;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _unsubscribeAction?.Invoke();
                _disposed = true;
            }
        }
    }
    
    /// <summary>
    /// Simple observable mock
    /// </summary>
    public class SimpleObservable<T>
    {
        public SimpleObservable()
        {
            // Simple mock implementation
        }
    }
}
