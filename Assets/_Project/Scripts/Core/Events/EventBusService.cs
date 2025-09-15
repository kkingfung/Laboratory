using UnityEngine;

#nullable enable

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Service that provides global access to the unified event bus.
    /// Implements a simple singleton pattern for event bus management.
    /// </summary>
    public static class EventBusService
    {
        #region Fields
        
        private static IEventBus? _instance;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the global instance of the unified event bus.
        /// Creates a new instance if one doesn't exist.
        /// </summary>
        public static IEventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UnifiedEventBus();
                    Debug.Log("EventBusService: Created new UnifiedEventBus instance");
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initializes the event bus service with a specific implementation.
        /// Useful for testing or when you want to provide a custom event bus.
        /// </summary>
        /// <param name="eventBus">The event bus implementation to use</param>
        public static void Initialize(IEventBus eventBus)
        {
            if (_instance != null)
            {
                Debug.LogWarning("EventBusService: Replacing existing event bus instance");
                _instance.Dispose();
            }
            
            _instance = eventBus;
            Debug.Log($"EventBusService: Initialized with {eventBus.GetType().Name}");
        }
        
        /// <summary>
        /// Disposes the current event bus instance and clears the reference.
        /// Call this during application shutdown or when resetting the system.
        /// </summary>
        public static void Shutdown()
        {
            if (_instance != null)
            {
                _instance.Dispose();
                _instance = null;
                Debug.Log("EventBusService: Shutdown complete");
            }
        }
        
        /// <summary>
        /// Checks if the event bus has been initialized.
        /// </summary>
        /// <returns>True if the event bus is ready to use</returns>
        public static bool IsInitialized => _instance != null;
        
        #endregion
    }
}
