using System;
using UnityEngine;

namespace Laboratory.Core.Timing
{
    /// <summary>
    /// Progress timer for loading operations and progress tracking.
    /// Supports both automatic progression and manual progress setting.
    /// </summary>
    public class ProgressTimer : ITimer, IDisposable
    {
        #region Fields

        private float _duration;
        private float _elapsed;
        private bool _isActive;
        private bool _autoProgress;
        private bool _autoRegister;

        #endregion

        #region Properties

        /// <summary>Timer duration in seconds.</summary>
        public float Duration => _duration;

        /// <summary>Remaining time in seconds.</summary>
        public float Remaining => Mathf.Max(_duration - _elapsed, 0f);

        /// <summary>Elapsed time since timer started.</summary>
        public float Elapsed => _elapsed;

        /// <summary>True if timer is currently active.</summary>
        public bool IsActive => _isActive && _elapsed < _duration;

        /// <summary>Progress from 0 (start) to 1 (complete).</summary>
        public float Progress => _duration > 0f ? Mathf.Clamp01(_elapsed / _duration) : 1f;

        #endregion

        #region Events

        /// <summary>Event fired when progress reaches 1.0.</summary>
        public event Action OnCompleted;

        /// <summary>Event fired each tick/progress update with current progress.</summary>
        public event Action<float> OnTick;

        /// <summary>Event fired when progress changes with new progress value.</summary>
        public event Action<float> OnProgressChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the progress timer.
        /// </summary>
        /// <param name="duration">Duration for automatic progression (0 for manual-only).</param>
        /// <param name="autoProgress">Whether timer automatically progresses with time.</param>
        /// <param name="autoRegister">Whether to auto-register with TimerService if available.</param>
        public ProgressTimer(float duration = 0f, bool autoProgress = true, bool autoRegister = true)
        {
            if (duration < 0f) throw new ArgumentException("Duration must be >= 0");
            _duration = duration;
            _elapsed = 0f;
            _isActive = false;
            _autoProgress = autoProgress && duration > 0f;
            _autoRegister = autoRegister;

            if (_autoRegister && TimerService.Instance != null)
            {
                TimerService.Instance.RegisterTimer(this);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the progress timer.
        /// </summary>
        public void Start()
        {
            _elapsed = 0f;
            _isActive = true;
        }

        /// <summary>
        /// Attempts to start timer only if not active. Returns true if started.
        /// </summary>
        public bool TryStart()
        {
            if (_isActive) return false;
            Start();
            return true;
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            _isActive = false;
        }

        /// <summary>
        /// Resets timer to initial state.
        /// </summary>
        public void Reset()
        {
            _elapsed = 0f;
            _isActive = false;
        }

        /// <summary>
        /// Updates progress timer by deltaTime seconds (if auto-progress enabled).
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last tick.</param>
        public void Tick(float deltaTime)
        {
            if (!_isActive || !_autoProgress) return;

            float oldProgress = Progress;
            _elapsed += deltaTime;
            
            if (_elapsed >= _duration)
            {
                _elapsed = _duration;
                _isActive = false;
            }

            float newProgress = Progress;
            OnTick?.Invoke(newProgress);

            if (Mathf.Abs(newProgress - oldProgress) > 0.001f)
            {
                OnProgressChanged?.Invoke(newProgress);
            }

            // Fire completion event when progress reaches 1.0
            if (newProgress >= 1f && oldProgress < 1f)
            {
                OnCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Manually sets progress (0 to 1). Useful for loading operations.
        /// </summary>
        /// <param name="progress">Progress value from 0 to 1.</param>
        public void SetProgress(float progress)
        {
            float oldProgress = Progress;
            progress = Mathf.Clamp01(progress);
            
            if (_duration > 0f)
            {
                _elapsed = progress * _duration;
            }
            else
            {
                // For manual-only timers, we track progress directly
                _elapsed = progress;
            }

            OnTick?.Invoke(progress);

            if (Mathf.Abs(progress - oldProgress) > 0.001f)
            {
                OnProgressChanged?.Invoke(progress);
            }

            // Fire completion event when progress reaches 1.0
            if (progress >= 1f && oldProgress < 1f)
            {
                _isActive = false;
                OnCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Sets the duration for automatic progression.
        /// </summary>
        /// <param name="duration">New duration in seconds.</param>
        public void SetDuration(float duration)
        {
            if (duration < 0f) throw new ArgumentException("Duration must be >= 0");
            _duration = duration;
            _autoProgress = duration > 0f;
        }

        /// <summary>
        /// Releases resources and unregisters from TimerService.
        /// </summary>
        public void Dispose()
        {
            if (_autoRegister && TimerService.Instance != null)
            {
                TimerService.Instance.UnregisterTimer(this);
            }
            
            OnCompleted = null;
            OnTick = null;
            OnProgressChanged = null;
        }

        #endregion
    }
}