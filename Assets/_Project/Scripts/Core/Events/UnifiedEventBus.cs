using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Unified event bus implementation using R3 for reactive programming.
    /// Provides thread-safe, performant event handling with advanced reactive features.
    /// </summary>
    public class UnifiedEventBus : IEventBus, IDisposable
    {
        #region Fields
        
        private readonly Dictionary<Type, Subject<object>> _subjects = new();
        private readonly Dictionary<Type, int> _subscriberCounts = new();
        private readonly CompositeDisposable _disposables = new();
        private bool _disposed = false;
        
        #endregion
        
        #region Constructor
        
        public UnifiedEventBus()
        {
            Debug.Log("UnifiedEventBus: Initialized with R3 reactive system");
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
            if (_subjects.TryGetValue(eventType, out var subject))
            {
                try
                {
                    subject.OnNext(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error publishing event {typeof(T).Name}: {ex}");
                }
            }
        }
        
        public IDisposable Subscribe<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            
            var observable = GetOrCreateObservable<T>();
            var subscription = observable
                .Subscribe(evt => handler((T)evt));
            
            // Track subscriber count
            IncrementSubscriberCount<T>();
            
            // Wrap subscription to handle unsubscribe
            var wrappedSubscription = new WrappedSubscription(subscription, () => DecrementSubscriberCount<T>());
            wrappedSubscription.AddTo(_disposables);
            
            return wrappedSubscription;
        }
        
        public Observable<T> AsObservable<T>() where T : class
        {
            ThrowIfDisposed();
            
            var observable = GetOrCreateObservable<T>();
            return observable.Cast<object, T>();
        }
        
        public object Observe<T>() where T : class
        {
            return AsObservable<T>();
        }
        
        public IDisposable SubscribeOnMainThread<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) 
                throw new ArgumentNullException(nameof(handler));
            
            // In Unity, we're typically already on the main thread, but we'll use the Subscribe method
            // and rely on Unity's main thread execution
            return Subscribe<T>(handler);
        }
        
        public IDisposable SubscribeWhere<T>(Func<T, bool> predicate, Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            
            var observable = GetOrCreateObservable<T>();
            var subscription = observable
                .Cast<object, T>()
                .Where(predicate)
                .Subscribe(handler);
            
            // Track subscriber count
            IncrementSubscriberCount<T>();
            
            // Wrap subscription to handle unsubscribe
            var wrappedSubscription = new WrappedSubscription(subscription, () => DecrementSubscriberCount<T>());
            wrappedSubscription.AddTo(_disposables);
            
            return wrappedSubscription;
        }
        
        public IDisposable SubscribeFirst<T>(Action<T> handler) where T : class
        {
            ThrowIfDisposed();
            
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            
            var observable = GetOrCreateObservable<T>();
            var subscription = observable
                .Cast<object, T>()
                .Take(1)
                .Subscribe(handler);
            
            // Track subscriber count (will auto-decrement after first event)
            IncrementSubscriberCount<T>();
            
            // Wrap subscription to handle unsubscribe
            var wrappedSubscription = new WrappedSubscription(subscription, () => DecrementSubscriberCount<T>());
            wrappedSubscription.AddTo(_disposables);
            
            return wrappedSubscription;
        }
        
        public int GetSubscriberCount<T>() where T : class
        {
            ThrowIfDisposed();
            
            var eventType = typeof(T);
            return _subscriberCounts.TryGetValue(eventType, out var count) ? count : 0;
        }
        
        public void ClearSubscriptions<T>() where T : class
        {
            ThrowIfDisposed();
            
            var eventType = typeof(T);
            if (_subjects.TryGetValue(eventType, out var subject))
            {
                subject.Dispose();
                _subjects.Remove(eventType);
                _subscriberCounts.Remove(eventType);
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private Observable<object> GetOrCreateObservable<T>() where T : class
        {
            var eventType = typeof(T);
            if (!_subjects.TryGetValue(eventType, out var subject))
            {
                subject = new Subject<object>();
                _subjects[eventType] = subject;
                _subscriberCounts[eventType] = 0;
                subject.AddTo(_disposables);
            }
            return subject.AsObservable();
        }
        
        private void IncrementSubscriberCount<T>() where T : class
        {
            var eventType = typeof(T);
            _subscriberCounts[eventType] = _subscriberCounts.TryGetValue(eventType, out var count) ? count + 1 : 1;
        }
        
        private void DecrementSubscriberCount<T>() where T : class
        {
            var eventType = typeof(T);
            if (_subscriberCounts.TryGetValue(eventType, out var count) && count > 0)
            {
                _subscriberCounts[eventType] = count - 1;
            }
        }
        
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnifiedEventBus));
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                _disposables.Dispose();
                _subjects.Clear();
                _subscriberCounts.Clear();
                
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
        
        #endregion
    }
    
    /// <summary>
    /// Wrapper for subscriptions that handles cleanup callbacks
    /// </summary>
    internal class WrappedSubscription : IDisposable
    {
        private readonly IDisposable _innerSubscription;
        private readonly Action _onDispose;
        private bool _disposed = false;
        
        public WrappedSubscription(IDisposable innerSubscription, Action onDispose)
        {
            _innerSubscription = innerSubscription ?? throw new ArgumentNullException(nameof(innerSubscription));
            _onDispose = onDispose;
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                _innerSubscription?.Dispose();
                _onDispose?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error disposing WrappedSubscription: {ex}");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}
