using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Laboratory.Core.Enums;
using Laboratory.Gameplay;

namespace Laboratory.Subsystems.Gameplay
{
    /// <summary>
    /// Orchestrates gameplay flow across all 47 genres
    /// Coordinates genre transitions, activity sessions, and system integration
    /// </summary>
    public class GameplayOrchestrator : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameplayConfig config;

        [Header("References")]
        [SerializeField] private GenreManager genreManager;
        [SerializeField] private DifficultyScalingSystem difficultySystem;

        // Session state
        private bool _isSessionActive = false;
        private float _sessionStartTime;
        private ActivityGenreCategory _currentGenre;

        // Activity queue
        private Queue<PendingActivity> _activityQueue = new Queue<PendingActivity>();
        private PendingActivity _currentActivity;

        // Statistics
        private int _totalActivitiesCompleted;
        private int _totalActivitiesFailed;
        private float _totalPlayTime;

        // Events
        public event System.Action OnSessionStarted;
        public event System.Action OnSessionEnded;
        public event System.Action<ActivityGenreCategory> OnActivityStarted;
        public event System.Action<ActivityGenreCategory, bool> OnActivityCompleted;

        private void Awake()
        {
            if (genreManager == null)
            {
                genreManager = GetComponent<GenreManager>();
            }

            if (difficultySystem == null)
            {
                difficultySystem = FindFirstObjectByType<DifficultyScalingSystem>();
            }
        }

        private void Update()
        {
            if (_isSessionActive)
            {
                UpdateSession();
            }
        }

        /// <summary>
        /// Update active session
        /// </summary>
        private void UpdateSession()
        {
            float sessionDuration = Time.time - _sessionStartTime;

            // Check for session timeout
            if (config != null && sessionDuration > config.SessionTimeout)
            {
                EndSession("Session timeout");
            }

            // Process activity queue
            if (_currentActivity == null && _activityQueue.Count > 0)
            {
                StartNextActivity();
            }
        }

        /// <summary>
        /// Start a gameplay session
        /// </summary>
        public void StartSession(ActivityGenreCategory? genre = null)
        {
            if (_isSessionActive)
            {
                Debug.LogWarning("[GameplayOrchestrator] Session already active");
                return;
            }

            _isSessionActive = true;
            _sessionStartTime = Time.time;

            ActivityGenreCategory startGenre = genre ?? (config != null ? config.DefaultGenre : ActivityGenreCategory.Action);
            _currentGenre = startGenre;

            if (genreManager != null)
            {
                genreManager.ActivateGenre(startGenre);
            }

            OnSessionStarted?.Invoke();
            Debug.Log($"[GameplayOrchestrator] Session started with genre: {startGenre}");
        }

        /// <summary>
        /// End current gameplay session
        /// </summary>
        public void EndSession(string reason = "User requested")
        {
            if (!_isSessionActive) return;

            _totalPlayTime += Time.time - _sessionStartTime;

            if (_currentActivity != null)
            {
                CompleteActivity(false);
            }

            _activityQueue.Clear();

            if (genreManager != null)
            {
                genreManager.DeactivateCurrentGenre();
            }

            _isSessionActive = false;
            OnSessionEnded?.Invoke();

            Debug.Log($"[GameplayOrchestrator] Session ended. Reason: {reason}. Duration: {(Time.time - _sessionStartTime):F1}s");
        }

        /// <summary>
        /// Queue a new activity
        /// </summary>
        public void QueueActivity(ActivityGenreCategory genre, string activityId, float difficulty = 1f)
        {
            if (!_isSessionActive)
            {
                Debug.LogWarning("[GameplayOrchestrator] Cannot queue activity - no active session");
                return;
            }

            PendingActivity activity = new PendingActivity
            {
                Genre = genre,
                ActivityId = activityId,
                Difficulty = difficulty,
                QueuedTime = Time.time
            };

            _activityQueue.Enqueue(activity);
            Debug.Log($"[GameplayOrchestrator] Queued activity: {activityId} ({genre})");
        }

        /// <summary>
        /// Start next activity from queue
        /// </summary>
        private void StartNextActivity()
        {
            if (_activityQueue.Count == 0) return;

            _currentActivity = _activityQueue.Dequeue();

            // Switch genre if needed
            if (_currentActivity.Genre != _currentGenre)
            {
                StartCoroutine(TransitionToGenre(_currentActivity.Genre));
            }
            else
            {
                BeginActivity();
            }
        }

        /// <summary>
        /// Transition to a new genre
        /// </summary>
        private IEnumerator TransitionToGenre(ActivityGenreCategory newGenre)
        {
            Debug.Log($"[GameplayOrchestrator] Transitioning from {_currentGenre} to {newGenre}");

            float transitionDelay = config != null ? config.ActivityTransitionDelay : 2f;
            yield return new WaitForSeconds(transitionDelay);

            _currentGenre = newGenre;

            if (genreManager != null)
            {
                genreManager.ActivateGenre(newGenre);
            }

            BeginActivity();
        }

        /// <summary>
        /// Begin current activity
        /// </summary>
        private void BeginActivity()
        {
            if (_currentActivity == null) return;

            _currentActivity.StartTime = Time.time;
            OnActivityStarted?.Invoke(_currentActivity.Genre);

            Debug.Log($"[GameplayOrchestrator] Started activity: {_currentActivity.ActivityId}");
        }

        /// <summary>
        /// Complete current activity
        /// </summary>
        public void CompleteActivity(bool success)
        {
            if (_currentActivity == null) return;

            float duration = Time.time - _currentActivity.StartTime;

            if (success)
            {
                _totalActivitiesCompleted++;
            }
            else
            {
                _totalActivitiesFailed++;
            }

            OnActivityCompleted?.Invoke(_currentActivity.Genre, success);

            Debug.Log($"[GameplayOrchestrator] Completed activity: {_currentActivity.ActivityId}. Success: {success}. Duration: {duration:F1}s");

            _currentActivity = null;
        }

        /// <summary>
        /// Switch to a different genre
        /// </summary>
        public void SwitchGenre(ActivityGenreCategory newGenre)
        {
            if (!_isSessionActive)
            {
                Debug.LogWarning("[GameplayOrchestrator] Cannot switch genre - no active session");
                return;
            }

            if (config != null && !config.AllowActivityInterruption && _currentActivity != null)
            {
                Debug.LogWarning("[GameplayOrchestrator] Cannot interrupt current activity");
                return;
            }

            if (_currentActivity != null)
            {
                CompleteActivity(false);
            }

            StartCoroutine(TransitionToGenre(newGenre));
        }

        /// <summary>
        /// Get session statistics
        /// </summary>
        public SessionStats GetSessionStats()
        {
            return new SessionStats
            {
                IsActive = _isSessionActive,
                CurrentGenre = _currentGenre,
                SessionDuration = _isSessionActive ? Time.time - _sessionStartTime : 0f,
                TotalActivitiesCompleted = _totalActivitiesCompleted,
                TotalActivitiesFailed = _totalActivitiesFailed,
                TotalPlayTime = _totalPlayTime,
                SuccessRate = _totalActivitiesCompleted > 0 ?
                    (float)_totalActivitiesCompleted / (_totalActivitiesCompleted + _totalActivitiesFailed) : 0f
            };
        }

        /// <summary>
        /// Check if session is active
        /// </summary>
        public bool IsSessionActive()
        {
            return _isSessionActive;
        }

        /// <summary>
        /// Get current activity
        /// </summary>
        public PendingActivity GetCurrentActivity()
        {
            return _currentActivity;
        }

        /// <summary>
        /// Pending activity data
        /// </summary>
        public class PendingActivity
        {
            public ActivityGenreCategory Genre;
            public string ActivityId;
            public float Difficulty;
            public float QueuedTime;
            public float StartTime;
        }

        /// <summary>
        /// Session statistics
        /// </summary>
        public struct SessionStats
        {
            public bool IsActive;
            public ActivityGenreCategory CurrentGenre;
            public float SessionDuration;
            public int TotalActivitiesCompleted;
            public int TotalActivitiesFailed;
            public float TotalPlayTime;
            public float SuccessRate;
        }
    }
}
