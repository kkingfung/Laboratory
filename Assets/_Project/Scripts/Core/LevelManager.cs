using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Laboratory.Core;
using Laboratory.Core.Management;

namespace Laboratory.Core
{
    /// <summary>
    /// Level manager that handles level progression, objectives, and environment setup
    /// for the 3D action game. Coordinates spawning, objectives, and level completion.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Level Settings")]
        [SerializeField] private string levelName = "Level 1";
        [SerializeField] private float levelTimeLimit = 300f; // 5 minutes
        [SerializeField] private bool hasTimeLimit = false;

        [Header("Objectives")]
        [SerializeField] private LevelObjective[] objectives;
        [SerializeField] private bool requireAllObjectives = true;

        [Header("Spawning")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private EnemySpawnPoint[] enemySpawnPoints;
        [SerializeField] private GameObject[] pickupPrefabs;
        [SerializeField] private Transform[] pickupSpawnPoints;

        [Header("Environment")]
        [SerializeField] private GameObject[] environmentObjects;
        [SerializeField] private Light[] dynamicLights;
        [SerializeField] private AudioSource ambientAudio;
        [SerializeField] private AudioClip levelMusic;

        // State
        private float levelStartTime;
        private bool levelCompleted = false;
        private bool levelFailed = false;
        private List<GameObject> spawnedEnemies = new List<GameObject>();
        private List<GameObject> spawnedPickups = new List<GameObject>();

        // Events
        public static System.Action<string> OnLevelStart;
        public static System.Action<string, bool> OnLevelComplete; // level name, success
        public static System.Action<string> OnObjectiveUpdate;

        [System.Serializable]
        public class LevelObjective
        {
            public string description;
            public ObjectiveType type;
            public int targetCount;
            public bool isCompleted;
            
            [HideInInspector] public int currentCount;

            public enum ObjectiveType
            {
                KillAllEnemies,
                KillSpecificCount,
                CollectItems,
                ReachLocation,
                Survive,
                ActivateSwitches
            }
        }

        [System.Serializable]
        public class EnemySpawnPoint
        {
            public Transform spawnTransform;
            public GameObject enemyPrefab;
            public float spawnDelay;
            public bool spawnOnStart = true;
        }

        #region Unity Lifecycle

        private void Start()
        {
            levelStartTime = Time.time;
            InitializeLevel();
        }

        private void Update()
        {
            if (levelCompleted || levelFailed) return;

            CheckTimeLimit();
            CheckObjectives();
        }

        #endregion

        #region Level Initialization

        private void InitializeLevel()
        {
            Debug.Log($"[LevelManager] Starting level: {levelName}");

            // Set up player spawn
            SetupPlayerSpawn();

            // Spawn enemies
            SpawnEnemies();

            // Spawn pickups
            SpawnPickups();

            // Initialize environment
            SetupEnvironment();

            // Play level music
            PlayLevelMusic();

            // Notify level start
            OnLevelStart?.Invoke(levelName);

            // Subscribe to game events
            SubscribeToEvents();
        }

        private void SetupPlayerSpawn()
        {
            if (playerSpawnPoint)
            {
                // Find player controller by component name to avoid assembly dependency
                var player = FindFirstObjectByType(System.Type.GetType("Laboratory.Subsystems.Player.PlayerController"));
                if (player != null)
                {
                    var playerTransform = ((MonoBehaviour)player).transform;
                    playerTransform.position = playerSpawnPoint.position;
                    playerTransform.rotation = playerSpawnPoint.rotation;
                }
            }
        }

        private void SpawnEnemies()
        {
            foreach (var spawnPoint in enemySpawnPoints)
            {
                if (spawnPoint.spawnOnStart)
                {
                    if (spawnPoint.spawnDelay > 0)
                    {
                        StartCoroutine(SpawnEnemyDelayed(spawnPoint));
                    }
                    else
                    {
                        SpawnEnemyAtPoint(spawnPoint);
                    }
                }
            }
        }

        private System.Collections.IEnumerator SpawnEnemyDelayed(EnemySpawnPoint spawnPoint)
        {
            yield return new UnityEngine.WaitForSeconds(spawnPoint.spawnDelay);
            SpawnEnemyAtPoint(spawnPoint);
        }

        private void SpawnEnemyAtPoint(EnemySpawnPoint spawnPoint)
        {
            if (spawnPoint.enemyPrefab && spawnPoint.spawnTransform)
            {
                GameObject enemy = Instantiate(spawnPoint.enemyPrefab, 
                    spawnPoint.spawnTransform.position, 
                    spawnPoint.spawnTransform.rotation);
                
                spawnedEnemies.Add(enemy);

                // Subscribe to enemy death using reflection to avoid assembly dependency
                var enemyControllerType = System.Type.GetType("Laboratory.Subsystems.EnemyAI.EnemyController");
                if (enemyControllerType != null)
                {
                    var enemyController = enemy.GetComponent(enemyControllerType);
                    if (enemyController != null)
                    {
                        // Use reflection to subscribe to OnDeath event
                        var onDeathEvent = enemyControllerType.GetEvent("OnDeath");
                        if (onDeathEvent != null)
                        {
                            var handler = System.Delegate.CreateDelegate(onDeathEvent.EventHandlerType, this, "HandleEnemyDeath");
                            onDeathEvent.AddEventHandler(enemyController, handler);
                        }
                    }
                }
            }
        }

        private void SpawnPickups()
        {
            foreach (var spawnPoint in pickupSpawnPoints)
            {
                if (pickupPrefabs.Length > 0)
                {
                    GameObject randomPickup = pickupPrefabs[Random.Range(0, pickupPrefabs.Length)];
                    GameObject pickup = Instantiate(randomPickup, spawnPoint.position, spawnPoint.rotation);
                    spawnedPickups.Add(pickup);
                }
            }
        }

        private void SetupEnvironment()
        {
            // Activate environment objects
            foreach (var envObject in environmentObjects)
            {
                if (envObject)
                    envObject.SetActive(true);
            }

            // Setup dynamic lighting
            foreach (var light in dynamicLights)
            {
                if (light)
                    light.enabled = true;
            }
        }

        private void PlayLevelMusic()
        {
            if (ambientAudio && levelMusic)
            {
                ambientAudio.clip = levelMusic;
                ambientAudio.loop = true;
                ambientAudio.Play();
            }
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            // Subscribe to game manager events if needed
        }

        private void OnEnemyKilled(GameObject enemy)
        {
            spawnedEnemies.Remove(enemy);
            
            // Update kill objectives
            foreach (var objective in objectives)
            {
                if (objective.type == LevelObjective.ObjectiveType.KillAllEnemies ||
                    objective.type == LevelObjective.ObjectiveType.KillSpecificCount)
                {
                    objective.currentCount++;
                    OnObjectiveUpdate?.Invoke($"{objective.description}: {objective.currentCount}/{objective.targetCount}");
                }
            }
        }

        // Reflection-based event handler
        private void HandleEnemyDeath()
        {
            // This method will be called via reflection when an enemy dies
            foreach (var objective in objectives)
            {
                if (objective.type == LevelObjective.ObjectiveType.KillAllEnemies ||
                    objective.type == LevelObjective.ObjectiveType.KillSpecificCount)
                {
                    objective.currentCount++;
                    OnObjectiveUpdate?.Invoke($"{objective.description}: {objective.currentCount}/{objective.targetCount}");
                }
            }
        }

        #endregion

        #region Objective System

        private void CheckObjectives()
        {
            bool allCompleted = true;
            int completedCount = 0;

            foreach (var objective in objectives)
            {
                if (!objective.isCompleted)
                {
                    bool completed = CheckIndividualObjective(objective);
                    if (completed)
                    {
                        objective.isCompleted = true;
                        OnObjectiveUpdate?.Invoke($"COMPLETED: {objective.description}");
                    }
                }

                if (objective.isCompleted)
                    completedCount++;
                else if (requireAllObjectives)
                    allCompleted = false;
            }

            // Check win condition
            if (requireAllObjectives && allCompleted)
            {
                CompleteLevel(true);
            }
            else if (!requireAllObjectives && completedCount > 0)
            {
                CompleteLevel(true);
            }
        }

        private bool CheckIndividualObjective(LevelObjective objective)
        {
            switch (objective.type)
            {
                case LevelObjective.ObjectiveType.KillAllEnemies:
                    return spawnedEnemies.Count == 0;

                case LevelObjective.ObjectiveType.KillSpecificCount:
                    return objective.currentCount >= objective.targetCount;

                case LevelObjective.ObjectiveType.CollectItems:
                    // This would need to be implemented with pickup tracking
                    return objective.currentCount >= objective.targetCount;

                case LevelObjective.ObjectiveType.Survive:
                    return Time.time - levelStartTime >= objective.targetCount;

                default:
                    return false;
            }
        }

        public void UpdateObjective(LevelObjective.ObjectiveType type, int amount = 1)
        {
            foreach (var objective in objectives)
            {
                if (objective.type == type && !objective.isCompleted)
                {
                    objective.currentCount += amount;
                    OnObjectiveUpdate?.Invoke($"{objective.description}: {objective.currentCount}/{objective.targetCount}");
                }
            }
        }

        #endregion

        #region Time Management

        private void CheckTimeLimit()
        {
            if (!hasTimeLimit) return;

            float timeRemaining = levelTimeLimit - (Time.time - levelStartTime);
            
            if (timeRemaining <= 0)
            {
                CompleteLevel(false);
            }
        }

        public float GetTimeRemaining()
        {
            if (!hasTimeLimit) return -1;
            return levelTimeLimit - (Time.time - levelStartTime);
        }

        #endregion

        #region Level Completion

        public void CompleteLevel(bool success)
        {
            if (levelCompleted || levelFailed) return;

            if (success)
            {
                levelCompleted = true;
                Debug.Log($"[LevelManager] Level completed: {levelName}");
            }
            else
            {
                levelFailed = true;
                Debug.Log($"[LevelManager] Level failed: {levelName}");
            }

            OnLevelComplete?.Invoke(levelName, success);

            // Notify game manager
            if (GameManager.Instance)
            {
                if (success)
                    GameManager.Instance.LoadNextLevel();
                else
                    GameManager.Instance.EndGame(false);
            }
        }

        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        #endregion

        #region Public Methods

        public void SpawnEnemyWave(int waveIndex)
        {
            if (waveIndex < enemySpawnPoints.Length)
            {
                SpawnEnemyAtPoint(enemySpawnPoints[waveIndex]);
            }
        }

        public void ActivateEnvironmentObject(int index)
        {
            if (index < environmentObjects.Length && environmentObjects[index])
            {
                environmentObjects[index].SetActive(true);
            }
        }

        public void DeactivateEnvironmentObject(int index)
        {
            if (index < environmentObjects.Length && environmentObjects[index])
            {
                environmentObjects[index].SetActive(false);
            }
        }

        public LevelObjective[] GetObjectives()
        {
            return objectives;
        }

        public int GetEnemyCount()
        {
            return spawnedEnemies.Count;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw player spawn point
            if (playerSpawnPoint)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(playerSpawnPoint.position, Vector3.one);
                Gizmos.DrawRay(playerSpawnPoint.position, playerSpawnPoint.forward * 2f);
            }

            // Draw enemy spawn points
            foreach (var spawnPoint in enemySpawnPoints)
            {
                if (spawnPoint.spawnTransform)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(spawnPoint.spawnTransform.position, 0.5f);
                }
            }

            // Draw pickup spawn points
            foreach (var spawnPoint in pickupSpawnPoints)
            {
                if (spawnPoint)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(spawnPoint.position, Vector3.one * 0.5f);
                }
            }
        }

        #endregion
    }
}