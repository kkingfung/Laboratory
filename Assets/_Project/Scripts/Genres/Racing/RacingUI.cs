using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.Genres.Racing
{
    /// <summary>
    /// Racing UI displaying lap count, position, boost, and race time
    /// </summary>
    public class RacingUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Text lapText;
        [SerializeField] private Text positionText;
        [SerializeField] private Text speedText;
        [SerializeField] private Slider boostMeter;
        [SerializeField] private Text boostText;
        [SerializeField] private Text countdownText;
        [SerializeField] private Text raceTimeText;
        [SerializeField] private GameObject finishPanel;
        [SerializeField] private Text finishPositionText;
        [SerializeField] private Text finishTimeText;

        [Header("References")]
        [SerializeField] private RacingGameMode gameMode;
        [SerializeField] private RacingVehicleController playerVehicle;

        private void Start()
        {
            // Find references if not assigned
            if (gameMode == null)
            {
                gameMode = FindFirstObjectByType<RacingGameMode>();
            }

            if (playerVehicle == null)
            {
                playerVehicle = FindFirstObjectByType<RacingVehicleController>();
            }

            // Subscribe to events
            if (gameMode != null)
            {
                gameMode.OnRaceStarted += HandleRaceStarted;
                gameMode.OnVehicleFinished += HandleVehicleFinished;
                gameMode.OnLapCompleted += HandleLapCompleted;
            }

            // Hide finish panel
            if (finishPanel != null)
            {
                finishPanel.SetActive(false);
            }

            // Initialize countdown text
            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            UpdateLapDisplay();
            UpdatePositionDisplay();
            UpdateSpeedDisplay();
            UpdateBoostDisplay();
            UpdateCountdownDisplay();
            UpdateRaceTimeDisplay();
        }

        /// <summary>
        /// Update lap counter display
        /// </summary>
        private void UpdateLapDisplay()
        {
            if (lapText == null || gameMode == null || playerVehicle == null) return;

            VehicleProgress progress = gameMode.GetVehicleProgress(playerVehicle);
            if (progress != null)
            {
                int totalCheckpoints = gameMode.GetTotalCheckpoints();
                lapText.text = $"Lap {progress.CurrentLap + 1}/{totalCheckpoints}";
            }
        }

        /// <summary>
        /// Update position display
        /// </summary>
        private void UpdatePositionDisplay()
        {
            if (positionText == null || gameMode == null || playerVehicle == null) return;

            int position = gameMode.GetVehicleRank(playerVehicle);
            string suffix = GetPositionSuffix(position);
            positionText.text = $"{position}{suffix}";

            // Color coding
            if (position == 1)
            {
                positionText.color = Color.yellow;
            }
            else if (position <= 3)
            {
                positionText.color = Color.green;
            }
            else
            {
                positionText.color = Color.white;
            }
        }

        /// <summary>
        /// Update speed display
        /// </summary>
        private void UpdateSpeedDisplay()
        {
            if (speedText == null || playerVehicle == null) return;

            float speed = playerVehicle.GetCurrentSpeed();
            speedText.text = $"{speed:F0} km/h";
        }

        /// <summary>
        /// Update boost meter and text
        /// </summary>
        private void UpdateBoostDisplay()
        {
            if (playerVehicle == null) return;

            float boostCharge = playerVehicle.GetBoostCharge();

            if (boostMeter != null)
            {
                boostMeter.value = boostCharge;
            }

            if (boostText != null)
            {
                if (boostCharge >= 1f)
                {
                    boostText.text = "BOOST READY!";
                    boostText.color = Color.cyan;
                }
                else
                {
                    boostText.text = $"Boost: {boostCharge * 100f:F0}%";
                    boostText.color = Color.white;
                }
            }
        }

        /// <summary>
        /// Update countdown display
        /// </summary>
        private void UpdateCountdownDisplay()
        {
            if (countdownText == null || gameMode == null) return;

            RaceState state = gameMode.GetCurrentState();

            if (state == RaceState.Countdown)
            {
                countdownText.gameObject.SetActive(true);
                float timer = gameMode.GetCountdownTimer();
                int countdown = Mathf.CeilToInt(timer);

                if (countdown > 0)
                {
                    countdownText.text = countdown.ToString();
                    countdownText.color = Color.red;
                }
                else
                {
                    countdownText.text = "GO!";
                    countdownText.color = Color.green;
                }
            }
            else if (state == RaceState.Racing)
            {
                // Hide countdown after "GO!" disappears
                if (countdownText.gameObject.activeSelf && gameMode.GetRaceTime() > 1f)
                {
                    countdownText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Update race time display
        /// </summary>
        private void UpdateRaceTimeDisplay()
        {
            if (raceTimeText == null || gameMode == null) return;

            RaceState state = gameMode.GetCurrentState();
            if (state == RaceState.Racing || state == RaceState.Finished)
            {
                float time = gameMode.GetRaceTime();
                raceTimeText.text = FormatTime(time);
            }
        }

        /// <summary>
        /// Format time as MM:SS.mmm
        /// </summary>
        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int milliseconds = Mathf.FloorToInt((time * 1000f) % 1000f);
            return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
        }

        /// <summary>
        /// Get position suffix (1st, 2nd, 3rd, 4th, etc.)
        /// </summary>
        private string GetPositionSuffix(int position)
        {
            if (position % 100 >= 11 && position % 100 <= 13)
            {
                return "th";
            }

            switch (position % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }

        // Event handlers
        private void HandleRaceStarted()
        {
            Debug.Log("[RacingUI] Race started!");
        }

        private void HandleLapCompleted(RacingVehicleController vehicle, int lap)
        {
            if (vehicle == playerVehicle)
            {
                Debug.Log($"[RacingUI] Lap {lap} completed!");
            }
        }

        private void HandleVehicleFinished(RacingVehicleController vehicle, int position)
        {
            if (vehicle == playerVehicle && finishPanel != null)
            {
                // Show finish panel
                finishPanel.SetActive(true);

                if (finishPositionText != null)
                {
                    string suffix = GetPositionSuffix(position);
                    finishPositionText.text = $"You finished {position}{suffix}!";
                }

                if (finishTimeText != null)
                {
                    VehicleProgress progress = gameMode.GetVehicleProgress(playerVehicle);
                    if (progress != null)
                    {
                        finishTimeText.text = $"Time: {FormatTime(progress.FinishTime)}";
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (gameMode != null)
            {
                gameMode.OnRaceStarted -= HandleRaceStarted;
                gameMode.OnVehicleFinished -= HandleVehicleFinished;
                gameMode.OnLapCompleted -= HandleLapCompleted;
            }
        }
    }
}
