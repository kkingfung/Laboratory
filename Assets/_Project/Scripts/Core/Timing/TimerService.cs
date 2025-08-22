using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.DI;

namespace Laboratory.Core.Timing
{
    /// <summary>
    /// Centralized service for managing all timer updates.
    /// Reduces the number of Update() methods and provides consistent timer behavior.
    /// </summary>
    public class TimerService : MonoBehaviour
    {
        #region Singleton

        private static TimerService _instance;
        public static TimerService Instance => _instance;

        #endregion

        #region Fields

        private readonly List<ITimer> _activeTimers = new();
        private readonly List<ITimer> _timersToRemove = new();

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private int activeTimerCount = 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Register with service container if available
                if (GlobalServiceProvider.Instance != null)
                {
                    GlobalServiceProvider.Instance.RegisterService<TimerService>(this);
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Update()
        {
            if (_activeTimers.Count == 0) return;

            float deltaTime = Time.deltaTime;
            _timersToRemove.Clear();

            // Update all active timers
            for (int i = 0; i < _activeTimers.Count; i++)
            {
                var timer = _activeTimers[i];
                if (timer != null)
                {
                    timer.Tick(deltaTime);
                    
                    // Mark inactive timers for removal
                    if (!timer.IsActive)
                    {
                        _timersToRemove.Add(timer);
                    }
                }
                else
                {
                    // Handle null timers
                    _timersToRemove.Add(timer);
                }
            }

            // Remove inactive/null timers
            foreach (var timer in _timersToRemove)
            {
                _activeTimers.Remove(timer);
            }

            // Update debug info
            if (showDebugInfo)
            {
                activeTimerCount = _activeTimers.Count;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a timer for automatic updates.
        /// </summary>
        /// <param name="timer">Timer to register.</param>
        public void RegisterTimer(ITimer timer)
        {
            if (timer != null && !_activeTimers.Contains(timer))
            {
                _activeTimers.Add(timer);
            }
        }

        /// <summary>
        /// Unregisters a timer from automatic updates.
        /// </summary>
        /// <param name="timer">Timer to unregister.</param>
        public void UnregisterTimer(ITimer timer)
        {
            if (timer != null)
            {
                _activeTimers.Remove(timer);
            }
        }

        /// <summary>
        /// Gets the count of currently active timers.
        /// </summary>
        /// <returns>Number of active timers.</returns>
        public int GetActiveTimerCount()
        {
            return _activeTimers.Count;
        }

        /// <summary>
        /// Clears all registered timers. Use with caution.
        /// </summary>
        public void ClearAllTimers()
        {
            _activeTimers.Clear();
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label($"Timer Service Debug");
            GUILayout.Label($"Active Timers: {_activeTimers.Count}");
            GUILayout.EndArea();
        }

        #endregion
    }
}