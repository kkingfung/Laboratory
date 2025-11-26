using UnityEngine;
using Laboratory.Core.Enums;
using Laboratory.Chimera.Progression;

namespace Laboratory.Subsystems.Gameplay
{
    /// <summary>
    /// Subsystem manager for Gameplay system
    /// Coordinates all gameplay-related systems for 47 genres
    /// Follows Project Chimera architecture pattern
    /// </summary>
    public class GameplaySubsystemManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameplayConfig config;

        [Header("Components")]
        [SerializeField] private GenreManager genreManager;
        [SerializeField] private GameplayOrchestrator orchestrator;

        // Singleton
        private static GameplaySubsystemManager _instance;
        public static GameplaySubsystemManager Instance => _instance;

        // Services
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeComponents();
        }

        private void Start()
        {
            InitializeSubsystem();
        }

        /// <summary>
        /// Initialize required components
        /// </summary>
        private void InitializeComponents()
        {
            if (genreManager == null)
            {
                genreManager = GetComponentInChildren<GenreManager>();
                if (genreManager == null)
                {
                    GameObject genreObj = new GameObject("GenreManager");
                    genreObj.transform.SetParent(transform);
                    genreManager = genreObj.AddComponent<GenreManager>();
                }
            }

            if (orchestrator == null)
            {
                orchestrator = GetComponentInChildren<GameplayOrchestrator>();
                if (orchestrator == null)
                {
                    GameObject orchestratorObj = new GameObject("GameplayOrchestrator");
                    orchestratorObj.transform.SetParent(transform);
                    orchestrator = orchestratorObj.AddComponent<GameplayOrchestrator>();
                }
            }
        }

        /// <summary>
        /// Initialize gameplay subsystem
        /// </summary>
        private void InitializeSubsystem()
        {
            if (_isInitialized) return;

            if (config == null)
            {
                Debug.LogWarning("[GameplaySubsystem] No configuration assigned!");
            }

            _isInitialized = true;
            Debug.Log("[GameplaySubsystem] Initialized");
        }

        /// <summary>
        /// Start a gameplay session
        /// </summary>
        public void StartSession(ActivityGenreCategory? genre = null)
        {
            if (orchestrator != null)
            {
                orchestrator.StartSession(genre);
            }
            else
            {
                Debug.LogError("[GameplaySubsystem] Orchestrator not found!");
            }
        }

        /// <summary>
        /// End current session
        /// </summary>
        public void EndSession(string reason = "User requested")
        {
            if (orchestrator != null)
            {
                orchestrator.EndSession(reason);
            }
        }

        /// <summary>
        /// Queue an activity
        /// </summary>
        public void QueueActivity(ActivityGenreCategory genre, string activityId, float difficulty = 1f)
        {
            if (orchestrator != null)
            {
                orchestrator.QueueActivity(genre, activityId, difficulty);
            }
            else
            {
                Debug.LogError("[GameplaySubsystem] Orchestrator not found!");
            }
        }

        /// <summary>
        /// Switch to a different genre
        /// </summary>
        public void SwitchGenre(ActivityGenreCategory newGenre)
        {
            if (orchestrator != null)
            {
                orchestrator.SwitchGenre(newGenre);
            }
            else
            {
                Debug.LogError("[GameplaySubsystem] Orchestrator not found!");
            }
        }

        /// <summary>
        /// Complete current activity
        /// </summary>
        public void CompleteActivity(bool success)
        {
            if (orchestrator != null)
            {
                orchestrator.CompleteActivity(success);
            }
        }

        /// <summary>
        /// Get genre manager
        /// </summary>
        public GenreManager GetGenreManager()
        {
            return genreManager;
        }

        /// <summary>
        /// Get gameplay orchestrator
        /// </summary>
        public GameplayOrchestrator GetOrchestrator()
        {
            return orchestrator;
        }

        /// <summary>
        /// Get configuration
        /// </summary>
        public GameplayConfig GetConfig()
        {
            return config;
        }

        /// <summary>
        /// Get session statistics
        /// </summary>
        public GameplayOrchestrator.SessionStats GetSessionStats()
        {
            if (orchestrator != null)
            {
                return orchestrator.GetSessionStats();
            }

            return new GameplayOrchestrator.SessionStats();
        }

        /// <summary>
        /// Check if session is active
        /// </summary>
        public bool IsSessionActive()
        {
            return orchestrator != null && orchestrator.IsSessionActive();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
