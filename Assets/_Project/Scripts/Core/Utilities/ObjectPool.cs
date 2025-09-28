using System;
using System.Collections.Concurrent;

namespace Laboratory.Core.Utilities
{
    /// <summary>
    /// High-performance generic object pool for reducing garbage collection pressure
    /// Thread-safe implementation using ConcurrentQueue for multi-threaded scenarios
    /// Used by string optimization and other performance-critical systems
    /// </summary>
    /// <typeparam name="T">Type of objects to pool (must be reference type)</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly ConcurrentQueue<T> _objects = new();
        private readonly Func<T> _factory;
        private readonly Action<T> _reset;
        private readonly int _maxSize;

        /// <summary>
        /// Creates a new object pool with specified factory and reset functions
        /// </summary>
        /// <param name="factory">Function to create new instances when pool is empty</param>
        /// <param name="reset">Optional function to reset objects when returned to pool</param>
        /// <param name="maxSize">Maximum number of objects to keep in pool (0 = unlimited)</param>
        public ObjectPool(Func<T> factory, Action<T> reset = null, int maxSize = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset;
            _maxSize = maxSize;
        }

        /// <summary>
        /// Gets an object from the pool or creates a new one if pool is empty
        /// Thread-safe operation
        /// </summary>
        /// <returns>Object ready for use</returns>
        public T Get()
        {
            if (_objects.TryDequeue(out T obj))
            {
                return obj;
            }
            return _factory();
        }

        /// <summary>
        /// Returns an object to the pool for reuse
        /// Thread-safe operation
        /// </summary>
        /// <param name="obj">Object to return to pool</param>
        public void Return(T obj)
        {
            if (obj == null)
                return;

            // Apply reset function if provided
            _reset?.Invoke(obj);

            // Only add to pool if under size limit
            if (_maxSize <= 0 || _objects.Count < _maxSize)
            {
                _objects.Enqueue(obj);
            }
            // If pool is full, object will be garbage collected
        }

        /// <summary>
        /// Gets the current number of objects in the pool
        /// Note: This is an approximate count due to concurrent access
        /// </summary>
        public int Count => _objects.Count;

        /// <summary>
        /// Clears all objects from the pool
        /// Useful for cleanup or memory management
        /// </summary>
        public void Clear()
        {
            while (_objects.TryDequeue(out _))
            {
                // Drain the queue
            }
        }
    }

    /// <summary>
    /// Static object pool factory for common types
    /// Provides pre-configured pools for frequently used objects
    /// </summary>
    public static class ObjectPools
    {
        /// <summary>
        /// Shared StringBuilder pool for string optimization
        /// Configured with reasonable default capacity and reset behavior
        /// </summary>
        public static readonly ObjectPool<System.Text.StringBuilder> StringBuilder =
            new ObjectPool<System.Text.StringBuilder>(
                factory: () => new System.Text.StringBuilder(256),
                reset: sb => sb.Clear(),
                maxSize: 10
            );

        /// <summary>
        /// Shared List pool for temporary collections
        /// Helps reduce allocations when working with temporary lists
        /// </summary>
        public static readonly ObjectPool<System.Collections.Generic.List<object>> ObjectList =
            new ObjectPool<System.Collections.Generic.List<object>>(
                factory: () => new System.Collections.Generic.List<object>(16),
                reset: list => list.Clear(),
                maxSize: 5
            );

        /// <summary>
        /// Creates a typed list pool for specific types
        /// Use when you need frequent temporary lists of a specific type
        /// </summary>
        /// <typeparam name="T">Type of list elements</typeparam>
        /// <param name="initialCapacity">Initial capacity for new lists</param>
        /// <param name="maxPoolSize">Maximum number of lists to keep in pool</param>
        /// <returns>Configured object pool for lists of type T</returns>
        public static ObjectPool<System.Collections.Generic.List<T>> CreateListPool<T>(int initialCapacity = 16, int maxPoolSize = 5)
        {
            return new ObjectPool<System.Collections.Generic.List<T>>(
                factory: () => new System.Collections.Generic.List<T>(initialCapacity),
                reset: list => list.Clear(),
                maxSize: maxPoolSize
            );
        }
    }
}