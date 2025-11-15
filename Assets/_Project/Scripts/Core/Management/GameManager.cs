using UnityEngine;
using UnityEngine.SceneManagement;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Core.State;
using Laboratory.Core.Performance;

namespace Laboratory.Core.Management
{
    /// <summary>
    /// Central game manager that coordinates all major game systems and handles
    /// game state transitions, scoring, and overall game flow for 3D action game.
    /// </summary>
    public class GameManager : OptimizedMonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private int maxLives = 3;
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private bool enableDebugMode = false;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip gameOverSound;
        [SerializeField] private AudioClip victorySound;
        [SerializeField] private AudioClip levelCompleteSound;

        // Services
        private IEventBus eventBus;
        private IGameStateService gameStateService;

        // Game State
        private int currentScore = 0;
        private int playerLives;
        private float gameTime = 0f;
        private bool isGameActive = false;
        private int enemiesRemaining = 0;

        // Events
        public static System.Action<int> OnScoreChanged;
        public static System.Action<int> OnLivesChanged;
        public static System.Action OnGameStart;
        public static System.Action OnGameEnd;
        public static System.Action OnLevelComplete;
        public static System.Action<string> OnGameStateChanged;

        // Singleton
        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<GameManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        #region Properties

        public int CurrentScore => currentScore;
        public int PlayerLives => playerLives;
        public float GameTime => gameTime;
        public bool IsGameActive => isGameActive;
        public int EnemiesRemaining => enemiesRemaining;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeServices();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            playerLives = maxLives;
            // Set to low frequency since GameManager doesn't need frequent updates
            updateFrequency = OptimizedUpdateManager.UpdateFrequency.LowFrequency;
        }

        protected override void Start()
        {
            // Subscribe to player events
            SubscribeToEvents();
            
            // Initialize game state
            StartGame();
        }

        public override void OnOptimizedUpdate(float deltaTime)
        {
            if (isGameActive)
            {
                gameTime += deltaTime;
                CheckWinCondition();
            }

            HandleDebugInput();
        }

        protected override void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        private void InitializeServices()
        {
            // Use the simple ServiceContainer for dependency injection
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                eventBus = serviceContainer.ResolveService<IEventBus>();
                if (eventBus != null && enableDebugMode)
                    Debug.Log("[GameManager] EventBus service resolved");
                else if (enableDebugMode)
                    Debug.LogWarning("[GameManager] EventBus service not available");

                gameStateService = serviceContainer.ResolveService<IGameStateService>();
                if (gameStateService != null && enableDebugMode)
                    Debug.Log("[GameManager] GameStateService resolved");
                else if (enableDebugMode)
                    Debug.LogWarning("[GameManager] GameStateService not available");
            }
            else if (enableDebugMode)
            {
                Debug.LogWarning("[GameManager] ServiceContainer not available - services unavailable");
            }

            audioSource = GetComponent<AudioSource>();

            if (enableDebugMode)
            {
                Debug.Log("[GameManager] Services initialized");
            }
        }

        private void SubscribeToEvents()
        {
            // Subscribe to player death events using reflection to avoid assembly dependency
            var playerControllerType = System.Type.GetType("Laboratory.Subsystems.Player.PlayerController");
            if (playerControllerType != null)
            {
                var players = FindObjectsByType(playerControllerType, FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    var onDeathEvent = playerControllerType.GetEvent("OnDeath");
                    if (onDeathEvent != null)
                    {
                        var handler = System.Delegate.CreateDelegate(onDeathEvent.EventHandlerType, this, "HandlePlayerDeath");
                        onDeathEvent.AddEventHandler(player, handler);
                    }
                }
            }

            // Subscribe to enemy death events using reflection
            var enemyControllerType = System.Type.GetType("Laboratory.Subsystems.EnemyAI.EnemyController");
            if (enemyControllerType != null)
            {
                var enemies = FindObjectsByType(enemyControllerType, FindObjectsSortMode.None);
                foreach (var enemy in enemies)
                {
                    var onDeathEvent = enemyControllerType.GetEvent("OnDeath");
                    if (onDeathEvent != null)
                    {
                        var handler = System.Delegate.CreateDelegate(onDeathEvent.EventHandlerType, this, "HandleEnemyDeath");
                        onDeathEvent.AddEventHandler(enemy, handler);
                    }
                }

                // Count initial enemies
                enemiesRemaining = enemies.Length;
            }
        }

        private void UnsubscribeFromEvents()
        {
            // Unsubscribe from player death events using reflection
            var playerControllerType = System.Type.GetType("Laboratory.Subsystems.Player.PlayerController");
            if (playerControllerType != null)
            {
                var players = FindObjectsByType(playerControllerType, FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player != null)
                    {
                        var onDeathEvent = playerControllerType.GetEvent("OnDeath");
                        if (onDeathEvent != null)
                        {
                            var handler = System.Delegate.CreateDelegate(onDeathEvent.EventHandlerType, this, "HandlePlayerDeath");
                            onDeathEvent.RemoveEventHandler(player, handler);
                        }
                    }
                }
            }

            // Unsubscribe from enemy death events using reflection
            var enemyControllerType = System.Type.GetType("Laboratory.Subsystems.EnemyAI.EnemyController");
            if (enemyControllerType != null)
            {
                var enemies = FindObjectsByType(enemyControllerType, FindObjectsSortMode.None);
                foreach (var enemy in enemies)
                {
                    if (enemy != null)
                    {
                        var onDeathEvent = enemyControllerType.GetEvent("OnDeath");
                        if (onDeathEvent != null)
                        {
                            var handler = System.Delegate.CreateDelegate(onDeathEvent.EventHandlerType, this, "HandleEnemyDeath");
                            onDeathEvent.RemoveEventHandler(enemy, handler);
                        }
                    }
                }
            }
        }

        #endregion

        #region Game State Management

        public void StartGame()
        {
            isGameActive = true;
            gameTime = 0f;
            currentScore = 0;
            playerLives = maxLives;

            OnGameStart?.Invoke();
            OnScoreChanged?.Invoke(currentScore);
            OnLivesChanged?.Invoke(playerLives);
            OnGameStateChanged?.Invoke("Game Started");

            if (enableDebugMode)
            {
                Debug.Log("[GameManager] Game Started");
            }
        }

        public void EndGame(bool victory = false)
        {
            isGameActive = false;

            if (victory)
            {
                PlaySound(victorySound);
                OnGameStateChanged?.Invoke("Victory!");
            }
            else
            {
                PlaySound(gameOverSound);
                OnGameStateChanged?.Invoke("Game Over");
            }

            OnGameEnd?.Invoke();

            if (enableDebugMode)
            {
                Debug.Log($"[GameManager] Game Ended - Victory: {victory}");
            }

            // Restart after delay
            Invoke(nameof(RestartGame), 3f);
        }

        public void PauseGame()
        {
            Time.timeScale = 0f;
            OnGameStateChanged?.Invoke("Paused");
        }

        public void ResumeGame()
        {
            Time.timeScale = 1f;
            OnGameStateChanged?.Invoke("Resumed");
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadNextLevel()
        {
            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                // No more levels, show victory screen
                EndGame(true);
            }
        }

        #endregion

        #region Event Handlers

        private void HandlePlayerDeath()
        {
            playerLives--;
            OnLivesChanged?.Invoke(playerLives);

            if (playerLives <= 0)
            {
                EndGame(false);
            }
            else
            {
                // Respawn player after delay
                Invoke(nameof(RespawnPlayer), respawnDelay);
                OnGameStateChanged?.Invoke($"Lives Remaining: {playerLives}");
            }

            if (enableDebugMode)
            {
                Debug.Log($"[GameManager] Player died. Lives remaining: {playerLives}");
            }
        }

        private void HandleEnemyDeath()
        {
            enemiesRemaining--;
            AddScore(100); // Base score for enemy kill

            if (enableDebugMode)
            {
                Debug.Log($"[GameManager] Enemy killed. Enemies remaining: {enemiesRemaining}");
            }
        }

        #endregion

        #region Scoring

        public void AddScore(int points)
        {
            currentScore += points;
            OnScoreChanged?.Invoke(currentScore);

            if (enableDebugMode)
            {
                Debug.Log($"[GameManager] Score added: {points}. Total: {currentScore}");
            }
        }

        #endregion

        #region Player Management

        private void RespawnPlayer()
        {
            // Find spawn point
            GameObject spawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawn");
            Vector3 spawnPosition = spawnPoint ? spawnPoint.transform.position : Vector3.zero;

            // Respawn player using reflection to avoid assembly dependency
            var playerControllerType = System.Type.GetType("Laboratory.Subsystems.Player.PlayerController");
            if (playerControllerType != null)
            {
                var player = FindFirstObjectByType(playerControllerType);
                if (player != null)
                {
                    var playerMonoBehaviour = (MonoBehaviour)player;
                    playerMonoBehaviour.transform.position = spawnPosition;
                    
                    // Try to call Heal method using reflection
                    var healMethod = playerControllerType.GetMethod("Heal");
                    var maxHealthProperty = playerControllerType.GetProperty("MaxHealth");
                    if (healMethod != null && maxHealthProperty != null)
                    {
                        var maxHealth = maxHealthProperty.GetValue(player);
                        healMethod.Invoke(player, new object[] { maxHealth });
                    }
                    
                    playerMonoBehaviour.enabled = true;
                    OnGameStateChanged?.Invoke("Player Respawned");
                }
            }

            if (enableDebugMode)
            {
                Debug.Log("[GameManager] Player respawned");
            }
        }

        #endregion

        #region Win Conditions

        private void CheckWinCondition()
        {
            if (enemiesRemaining <= 0)
            {
                PlaySound(levelCompleteSound);
                OnLevelComplete?.Invoke();
                
                // Load next level or end game
                Invoke(nameof(LoadNextLevel), 2f);
            }
        }

        #endregion

        #region Debug

        private void HandleDebugInput()
        {
            if (!enableDebugMode) return;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                AddScore(1000);
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Heal player using reflection
                var playerControllerType = System.Type.GetType("Laboratory.Subsystems.Player.PlayerController");
                if (playerControllerType != null)
                {
                    var player = FindFirstObjectByType(playerControllerType);
                    if (player != null)
                    {
                        var healMethod = playerControllerType.GetMethod("Heal");
                        var maxHealthProperty = playerControllerType.GetProperty("MaxHealth");
                        if (healMethod != null && maxHealthProperty != null)
                        {
                            var maxHealth = maxHealthProperty.GetValue(player);
                            healMethod.Invoke(player, new object[] { maxHealth });
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                EndGame(true);
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                RestartGame();
            }
        }

        #endregion

        #region Utility

        private void PlaySound(AudioClip clip)
        {
            if (audioSource && clip)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion
    }
}