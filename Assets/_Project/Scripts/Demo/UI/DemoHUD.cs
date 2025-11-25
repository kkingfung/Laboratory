using UnityEngine;
using UnityEngine.UI;
using Laboratory.Subsystems.Spawning;
using Laboratory.Subsystems.Gameplay;

namespace Laboratory.Demo.UI
{
    /// <summary>
    /// Demo HUD displaying performance metrics and controls
    /// Shows FPS, spawn counts, and gameplay stats
    /// </summary>
    public class DemoHUD : MonoBehaviour
    {
        [Header("UI Text Elements")]
        [SerializeField] private Text fpsText;
        [SerializeField] private Text spawnStatsText;
        [SerializeField] private Text gameplayStatsText;
        [SerializeField] private Text controlsText;
        [SerializeField] private Text performanceText;

        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 0.5f;

        // References
        private DemoCreatureSpawner _spawner;
        private SpawningSubsystemManager _spawningSubsystem;
        private GameplaySubsystemManager _gameplaySubsystem;

        // FPS tracking
        private float _fpsTimer = 0f;
        private int _frameCount = 0;
        private float _currentFPS = 0f;

        // Update timer
        private float _updateTimer = 0f;

        private void Start()
        {
            _spawner = FindFirstObjectByType<DemoCreatureSpawner>();
            _spawningSubsystem = SpawningSubsystemManager.Instance;
            _gameplaySubsystem = GameplaySubsystemManager.Instance;

            // Display controls
            if (controlsText != null)
            {
                controlsText.text = GetControlsText();
            }
        }

        private void Update()
        {
            // Update FPS
            UpdateFPS();

            // Update stats periodically
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= updateInterval)
            {
                UpdateStats();
                _updateTimer = 0f;
            }
        }

        /// <summary>
        /// Update FPS counter
        /// </summary>
        private void UpdateFPS()
        {
            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;

            if (_fpsTimer >= 1f)
            {
                _currentFPS = _frameCount / _fpsTimer;
                _frameCount = 0;
                _fpsTimer = 0f;

                if (fpsText != null)
                {
                    Color fpsColor = _currentFPS >= 60f ? Color.green : (_currentFPS >= 30f ? Color.yellow : Color.red);
                    fpsText.text = $"FPS: {_currentFPS:F0}";
                    fpsText.color = fpsColor;
                }
            }
        }

        /// <summary>
        /// Update all stats displays
        /// </summary>
        private void UpdateStats()
        {
            UpdateSpawnStats();
            UpdateGameplayStats();
            UpdatePerformanceStats();
        }

        /// <summary>
        /// Update spawn statistics
        /// </summary>
        private void UpdateSpawnStats()
        {
            if (spawnStatsText == null) return;

            if (_spawner != null)
            {
                SpawnStats stats = _spawner.GetStats();
                spawnStatsText.text = $"<b>Spawning</b>\n" +
                    $"Active: {stats.ActiveCount}/{stats.MaxCreatures}\n" +
                    $"Total Spawned: {stats.TotalSpawned}\n" +
                    $"Total Recycled: {stats.TotalRecycled}";
            }
            else if (_spawningSubsystem != null)
            {
                int activeCount = _spawningSubsystem.GetTotalActiveObjects();
                spawnStatsText.text = $"<b>Spawning</b>\n" +
                    $"Active Objects: {activeCount}";
            }
        }

        /// <summary>
        /// Update gameplay statistics
        /// </summary>
        private void UpdateGameplayStats()
        {
            if (gameplayStatsText == null || _gameplaySubsystem == null) return;

            var stats = _gameplaySubsystem.GetSessionStats();

            if (stats.IsActive)
            {
                gameplayStatsText.text = $"<b>Gameplay</b>\n" +
                    $"Genre: {stats.CurrentGenre}\n" +
                    $"Session: {stats.SessionDuration:F0}s\n" +
                    $"Completed: {stats.TotalActivitiesCompleted}\n" +
                    $"Failed: {stats.TotalActivitiesFailed}\n" +
                    $"Success Rate: {stats.SuccessRate * 100f:F0}%";
            }
            else
            {
                gameplayStatsText.text = "<b>Gameplay</b>\nNo active session";
            }
        }

        /// <summary>
        /// Update performance statistics
        /// </summary>
        private void UpdatePerformanceStats()
        {
            if (performanceText == null) return;

            float memoryUsage = System.GC.GetTotalMemory(false) / 1024f / 1024f;

            performanceText.text = $"<b>Performance</b>\n" +
                $"Memory: {memoryUsage:F1} MB\n" +
                $"Time Scale: {Time.timeScale:F2}";
        }

        /// <summary>
        /// Get controls help text
        /// </summary>
        private string GetControlsText()
        {
            return "<b>Controls</b>\n" +
                "WASD - Move\n" +
                "Mouse - Look\n" +
                "Space - Jump\n" +
                "Shift - Sprint\n" +
                "Tab - Settings Menu\n" +
                "G - Spawn 1 Creature\n" +
                "H - Recycle All\n" +
                "J - Spawn Wave (10)\n" +
                "Esc - Toggle Cursor";
        }

        /// <summary>
        /// Toggle HUD visibility
        /// </summary>
        public void ToggleHUD()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        /// <summary>
        /// Set HUD visibility
        /// </summary>
        public void SetHUDVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
