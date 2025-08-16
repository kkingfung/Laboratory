using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Laboratory.Gameplay.Lobby;

namespace Laboratory.UI.Helper
{
    using MatchmakingState = MatchmakingManager.MatchmakingState;

    /// <summary>
    /// UI component for managing matchmaking interface and status display.
    /// Handles matchmaking start/cancel, join codes, and state transitions.
    /// </summary>
    public class MatchmakingUI : MonoBehaviour
    {
        #region Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private InputField joinCodeInput;
        [SerializeField] private Button startButton;
        [SerializeField] private Button cancelButton;

        [Header("Status Messages")]
        [SerializeField] private string searchingMessage = "Searching for match...";
        [SerializeField] private string foundMessage = "Match found! Preparing...";
        [SerializeField] private string failedMessage = "Matchmaking failed. Please try again.";

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when matchmaking is cancelled.
        /// </summary>
        public event Action OnCancelMatchmaking;

        /// <summary>
        /// Event triggered when a match is found.
        /// </summary>
        public event Action OnMatchFound;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize UI components and setup initial state.
        /// </summary>
        private void Awake()
        {
            SetupButtonHandlers();
            Hide();
        }

        /// <summary>
        /// Subscribe to events when enabled.
        /// </summary>
        private void OnEnable()
        {
            SubscribeToMatchmakingEvents();

            if (MatchmakingManager.Instance != null)
                UpdateUI(MatchmakingManager.Instance.CurrentState);
        }

        /// <summary>
        /// Unsubscribe from events when disabled.
        /// </summary>
        private void OnDisable()
        {
            UnsubscribeFromMatchmakingEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show matchmaking UI with "searching" status.
        /// </summary>
        public void ShowSearching()
        {
            Show();
            SetStatus(searchingMessage);
        }

        /// <summary>
        /// Show matchmaking UI with "found" status.
        /// </summary>
        public void ShowMatchFound()
        {
            Show();
            SetStatus(foundMessage);
            OnMatchFound?.Invoke();
        }

        /// <summary>
        /// Show matchmaking UI with "failed" status.
        /// </summary>
        public void ShowFailed()
        {
            Show();
            SetStatus(failedMessage);
        }

        /// <summary>
        /// Sets the status text display.
        /// </summary>
        /// <param name="message">Status message to display</param>
        public void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        /// <summary>
        /// Show the matchmaking UI panel.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the matchmaking UI panel.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Setup button event handlers.
        /// </summary>
        private void SetupButtonHandlers()
        {
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelMatchmaking);
        }

        /// <summary>
        /// Subscribe to matchmaking manager events.
        /// </summary>
        private void SubscribeToMatchmakingEvents()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);

            if (MatchmakingManager.Instance != null)
                MatchmakingManager.Instance.OnMatchmakingStateChanged += OnMatchmakingStateChanged;
        }

        /// <summary>
        /// Unsubscribe from matchmaking manager events.
        /// </summary>
        private void UnsubscribeFromMatchmakingEvents()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);

            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(OnCancelClicked);

            if (MatchmakingManager.Instance != null)
                MatchmakingManager.Instance.OnMatchmakingStateChanged -= OnMatchmakingStateChanged;
        }

        /// <summary>
        /// Handle start button click - begin matchmaking or join with code.
        /// </summary>
        private void OnStartClicked()
        {
            if (MatchmakingManager.Instance == null) return;

            string joinCode = joinCodeInput.text.Trim();
            if (!string.IsNullOrEmpty(joinCode))
            {
                MatchmakingManager.Instance.JoinMatch(joinCode);
            }
            else
            {
                MatchmakingManager.Instance.StartMatchmaking();
            }
        }

        /// <summary>
        /// Handle cancel button click - cancel matchmaking process.
        /// </summary>
        private void OnCancelClicked()
        {
            if (MatchmakingManager.Instance != null)
                MatchmakingManager.Instance.CancelMatchmaking();
        }

        /// <summary>
        /// Handle matchmaking state change events.
        /// </summary>
        /// <param name="state">New matchmaking state</param>
        private void OnMatchmakingStateChanged(MatchmakingState state)
        {
            UpdateUI(state);
        }

        /// <summary>
        /// Update UI based on current matchmaking state.
        /// </summary>
        /// <param name="state">Current matchmaking state</param>
        private void UpdateUI(MatchmakingState state)
        {
            switch (state)
            {
                case MatchmakingState.Idle:
                    SetIdleState();
                    break;

                case MatchmakingState.Searching:
                    SetSearchingState();
                    break;

                case MatchmakingState.MatchFound:
                    SetMatchFoundState();
                    break;

                case MatchmakingState.Failed:
                    SetFailedState();
                    break;
            }
        }

        /// <summary>
        /// Set UI to idle state.
        /// </summary>
        private void SetIdleState()
        {
            statusText.text = "Ready to find match";
            startButton.interactable = true;
            cancelButton.interactable = false;
        }

        /// <summary>
        /// Set UI to searching state.
        /// </summary>
        private void SetSearchingState()
        {
            statusText.text = searchingMessage;
            startButton.interactable = false;
            cancelButton.interactable = true;
        }

        /// <summary>
        /// Set UI to match found state.
        /// </summary>
        private void SetMatchFoundState()
        {
            statusText.text = foundMessage;
            startButton.interactable = false;
            cancelButton.interactable = false;
        }

        /// <summary>
        /// Set UI to failed state.
        /// </summary>
        private void SetFailedState()
        {
            statusText.text = failedMessage;
            startButton.interactable = true;
            cancelButton.interactable = false;
        }

        /// <summary>
        /// Cancel matchmaking and hide UI.
        /// </summary>
        private void CancelMatchmaking()
        {
            OnCancelMatchmaking?.Invoke();
            Hide();
        }

        #endregion
    }
}
