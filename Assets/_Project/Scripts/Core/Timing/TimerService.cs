using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Infrastructure;

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
                var serviceContainer = ServiceContainer.Instance;
                if (serviceContainer != null)
                {
                    try
                    {
                        serviceContainer.RegisterService<TimerService>(this);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Could not register TimerService with container: {ex.Message}");
                    }
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
                    try
                    {
                        timer.Tick(deltaTime);
                        
                        // Mark inactive timers for removal
                        if (!timer.IsActive)
                        {
                            _timersToRemove.Add(timer);
                        }
                    }
                    catch (System.ObjectDisposedException)
                    {
                        // Timer was disposed externally, mark for removal
                        _timersToRemove.Add(timer);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"TimerService: Error updating timer: {ex.Message}");
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

        #region Example Usage (Context Menu Items)

        [ContextMenu("Create Example Weapon Cooldown")]
        private void CreateExampleWeaponCooldown()
        {
            var weaponCooldown = new CooldownTimer(3f);
            weaponCooldown.OnCompleted += () => Debug.Log("⚔️ Weapon ready to fire!");
            weaponCooldown.OnTick += (elapsed) => Debug.Log($"⏱️ Weapon cooldown: {weaponCooldown.Progress:P1}");
            
            weaponCooldown.Start();
            Debug.Log("Started example weapon cooldown (3 seconds)");
        }

        [ContextMenu("Create Example Match Timer")]
        private void CreateExampleMatchTimer()
        {
            var matchTimer = new CountdownTimer(60f);
            matchTimer.OnCompleted += () => Debug.Log("🏁 Match ended! Time's up!");
            matchTimer.OnTick += (remaining) => {
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                if (seconds % 10 == 0 && seconds > 0) // Log every 10 seconds
                    Debug.Log($"🕒 Match time remaining: {minutes:00}:{seconds:00}");
            };
            
            matchTimer.Start();
            Debug.Log("Started example match timer (60 seconds)");
        }

        [ContextMenu("Create Example Loading Progress")]
        private void CreateExampleLoadingProgress()
        {
            var loadingProgress = new ProgressTimer(duration: 5f, autoProgress: true);
            loadingProgress.OnProgressChanged += (progress) => {
                if (progress % 0.25f < 0.1f) // Log at 25%, 50%, 75%, 100%
                    Debug.Log($"📎 Loading progress: {progress:P0}");
            };
            loadingProgress.OnCompleted += () => Debug.Log("✅ Loading completed!");
            
            loadingProgress.Start();
            Debug.Log("Started example loading progress (5 seconds)");
        }

        [ContextMenu("Test Timer Cleanup")]
        private void TestTimerCleanup()
        {
            Debug.Log($"Before cleanup: {GetActiveTimerCount()} active timers");
            
            // Create some temporary timers that will auto-complete
            for (int i = 0; i < 3; i++)
            {
                var tempTimer = new CooldownTimer(0.5f + i * 0.5f);
                tempTimer.OnCompleted += () => Debug.Log("✅ Temporary timer completed");
                tempTimer.Start();
            }
            
            Debug.Log($"After creating temporary timers: {GetActiveTimerCount()} active timers");
            Debug.Log("Timers will auto-cleanup when they complete...");
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