using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Genres.Racing
{
    /// <summary>
    /// Racing game mode manager
    /// Handles race logic, lap counting, and win conditions
    /// </summary>
    public class RacingGameMode : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private RacingConfig config;

        [Header("Track")]
        [SerializeField] private List<RacingCheckpoint> checkpoints = new List<RacingCheckpoint>();
        [SerializeField] private Transform[] spawnPoints;

        [Header("Vehicles")]
        [SerializeField] private RacingVehicleController playerVehicle;
        [SerializeField] private List<RacingVehicleController> aiVehicles = new List<RacingVehicleController>();

        // Race state
        private RaceState _currentState = RaceState.NotStarted;
        private float _countdownTimer;
        private float _raceTime;

        // Vehicle tracking
        private Dictionary<RacingVehicleController, VehicleProgress> _vehicleProgress = new Dictionary<RacingVehicleController, VehicleProgress>();
        private List<RacingVehicleController> _rankedVehicles = new List<RacingVehicleController>();

        // Events
        public event System.Action OnRaceStarted;
        public event System.Action<RacingVehicleController, int> OnVehicleFinished; // vehicle, position
        public event System.Action<RacingVehicleController, int> OnLapCompleted; // vehicle, lap number
        public event System.Action OnRaceCompleted;

        private void Start()
        {
            InitializeRace();
        }

        private void Update()
        {
            UpdateRaceState();
            UpdateRankings();
        }

        /// <summary>
        /// Initialize race
        /// </summary>
        private void InitializeRace()
        {
            if (config == null)
            {
                Debug.LogError("[RacingGameMode] No config assigned!");
                return;
            }

            // Subscribe to checkpoint events
            foreach (RacingCheckpoint checkpoint in checkpoints)
            {
                if (checkpoint != null)
                {
                    checkpoint.OnVehiclePassed += HandleCheckpointPassed;
                }
            }

            // Initialize vehicle progress tracking
            if (playerVehicle != null)
            {
                _vehicleProgress[playerVehicle] = new VehicleProgress();
            }

            foreach (RacingVehicleController ai in aiVehicles)
            {
                if (ai != null)
                {
                    _vehicleProgress[ai] = new VehicleProgress();
                    ai.SetAIControlled(true);
                }
            }

            // Spawn vehicles at starting positions
            SpawnVehicles();

            Debug.Log("[RacingGameMode] Race initialized");
        }

        /// <summary>
        /// Spawn vehicles at starting grid
        /// </summary>
        private void SpawnVehicles()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[RacingGameMode] No spawn points defined!");
                return;
            }

            int spawnIndex = 0;

            // Spawn player
            if (playerVehicle != null && spawnIndex < spawnPoints.Length)
            {
                playerVehicle.ResetToPosition(spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation);
                spawnIndex++;
            }

            // Spawn AI
            foreach (RacingVehicleController ai in aiVehicles)
            {
                if (ai != null && spawnIndex < spawnPoints.Length)
                {
                    ai.ResetToPosition(spawnPoints[spawnIndex].position, spawnPoints[spawnIndex].rotation);
                    spawnIndex++;
                }
            }
        }

        /// <summary>
        /// Update race state machine
        /// </summary>
        private void UpdateRaceState()
        {
            switch (_currentState)
            {
                case RaceState.NotStarted:
                    // Waiting to start
                    break;

                case RaceState.Countdown:
                    UpdateCountdown();
                    break;

                case RaceState.Racing:
                    UpdateRacing();
                    break;

                case RaceState.Finished:
                    // Race over
                    break;
            }
        }

        /// <summary>
        /// Update countdown
        /// </summary>
        private void UpdateCountdown()
        {
            _countdownTimer -= Time.deltaTime;

            if (_countdownTimer <= 0f)
            {
                StartRace();
            }
        }

        /// <summary>
        /// Update racing
        /// </summary>
        private void UpdateRacing()
        {
            _raceTime += Time.deltaTime;

            // Update AI waypoints
            UpdateAIVehicles();

            // Check if all human players finished
            if (playerVehicle != null)
            {
                VehicleProgress progress = _vehicleProgress[playerVehicle];
                if (progress.HasFinished && _currentState == RaceState.Racing)
                {
                    // Could end race or wait for all AI to finish
                }
            }
        }

        /// <summary>
        /// Update AI vehicle waypoints
        /// </summary>
        private void UpdateAIVehicles()
        {
            foreach (RacingVehicleController ai in aiVehicles)
            {
                if (ai == null) continue;

                VehicleProgress progress = _vehicleProgress[ai];
                if (progress.HasFinished) continue;

                // Get next checkpoint
                int nextCheckpointIndex = progress.CurrentCheckpoint + 1;
                if (nextCheckpointIndex >= checkpoints.Count)
                {
                    nextCheckpointIndex = 0;
                }

                if (nextCheckpointIndex < checkpoints.Count)
                {
                    ai.SetAIWaypoint(checkpoints[nextCheckpointIndex].transform);
                }
            }
        }

        /// <summary>
        /// Update vehicle rankings
        /// </summary>
        private void UpdateRankings()
        {
            _rankedVehicles = _vehicleProgress.Keys.OrderByDescending(v =>
            {
                VehicleProgress progress = _vehicleProgress[v];
                return progress.CurrentLap * 1000 + progress.CurrentCheckpoint;
            }).ToList();
        }

        /// <summary>
        /// Start countdown
        /// </summary>
        public void StartCountdown()
        {
            if (_currentState != RaceState.NotStarted) return;

            _currentState = RaceState.Countdown;
            _countdownTimer = config != null ? config.CountdownDuration : 3f;

            Debug.Log("[RacingGameMode] Countdown started");
        }

        /// <summary>
        /// Start race
        /// </summary>
        private void StartRace()
        {
            _currentState = RaceState.Racing;
            _raceTime = 0f;

            OnRaceStarted?.Invoke();

            Debug.Log("[RacingGameMode] Race started!");
        }

        /// <summary>
        /// Handle checkpoint passed
        /// </summary>
        private void HandleCheckpointPassed(RacingVehicleController vehicle)
        {
            if (!_vehicleProgress.ContainsKey(vehicle)) return;

            VehicleProgress progress = _vehicleProgress[vehicle];

            // Get checkpoint that was passed
            RacingCheckpoint checkpoint = checkpoints.FirstOrDefault(c => c.OnVehiclePassed.GetInvocationList().Contains((System.Action<RacingVehicleController>)HandleCheckpointPassed));

            if (checkpoint == null) return;

            // Check if this is the next expected checkpoint
            int expectedCheckpoint = progress.CurrentCheckpoint + 1;
            if (expectedCheckpoint >= checkpoints.Count)
            {
                expectedCheckpoint = 0;
            }

            if (checkpoint.CheckpointIndex == expectedCheckpoint)
            {
                progress.CurrentCheckpoint = checkpoint.CheckpointIndex;

                // Check for lap completion
                if (checkpoint.IsFinishLine && progress.CurrentCheckpoint == 0)
                {
                    progress.CurrentLap++;

                    OnLapCompleted?.Invoke(vehicle, progress.CurrentLap);

                    Debug.Log($"[RacingGameMode] {vehicle.name} completed lap {progress.CurrentLap}");

                    // Check for race finish
                    if (config != null && progress.CurrentLap >= config.TotalLaps)
                    {
                        FinishVehicle(vehicle);
                    }
                }
            }
        }

        /// <summary>
        /// Finish vehicle
        /// </summary>
        private void FinishVehicle(RacingVehicleController vehicle)
        {
            if (!_vehicleProgress.ContainsKey(vehicle)) return;

            VehicleProgress progress = _vehicleProgress[vehicle];
            if (progress.HasFinished) return;

            progress.HasFinished = true;
            progress.FinishTime = _raceTime;
            progress.FinishPosition = _vehicleProgress.Values.Count(v => v.HasFinished);

            OnVehicleFinished?.Invoke(vehicle, progress.FinishPosition);

            Debug.Log($"[RacingGameMode] {vehicle.name} finished in position {progress.FinishPosition}!");

            // Check if race is complete
            if (playerVehicle != null && _vehicleProgress[playerVehicle].HasFinished)
            {
                CompleteRace();
            }
        }

        /// <summary>
        /// Complete race
        /// </summary>
        private void CompleteRace()
        {
            if (_currentState != RaceState.Racing) return;

            _currentState = RaceState.Finished;

            OnRaceCompleted?.Invoke();

            Debug.Log("[RacingGameMode] Race completed!");
        }

        /// <summary>
        /// Get vehicle ranking
        /// </summary>
        public int GetVehicleRank(RacingVehicleController vehicle)
        {
            return _rankedVehicles.IndexOf(vehicle) + 1;
        }

        /// <summary>
        /// Get vehicle progress
        /// </summary>
        public VehicleProgress GetVehicleProgress(RacingVehicleController vehicle)
        {
            return _vehicleProgress.ContainsKey(vehicle) ? _vehicleProgress[vehicle] : null;
        }

        // Getters
        public RaceState GetCurrentState() => _currentState;
        public float GetCountdownTimer() => _countdownTimer;
        public float GetRaceTime() => _raceTime;
        public int GetTotalCheckpoints() => checkpoints.Count;
    }

    /// <summary>
    /// Race state enum
    /// </summary>
    public enum RaceState
    {
        NotStarted,
        Countdown,
        Racing,
        Finished
    }

    /// <summary>
    /// Vehicle progress tracking
    /// </summary>
    public class VehicleProgress
    {
        public int CurrentLap = 0;
        public int CurrentCheckpoint = -1;
        public bool HasFinished = false;
        public float FinishTime = 0f;
        public int FinishPosition = 0;
    }
}
