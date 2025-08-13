using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// FIXME: tidyup after 8/29
public class MatchmakingUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private InputField joinCodeInput;
    [SerializeField] private Button startButton;
    [SerializeField] private Button cancelButton;

    [Header("Status Messages")]
    [SerializeField] private string searchingMessage = "Searching for match...";
    [SerializeField] private string foundMessage = "Match found! Preparing...";
    [SerializeField] private string failedMessage = "Matchmaking failed. Please try again.";

    public event Action? OnCancelMatchmaking;
    public event Action? OnMatchFound;

    private void Awake()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelMatchmaking);

        Hide();
    }

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
    /// Sets the status text.
    /// </summary>
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

    private void CancelMatchmaking()
    {
        OnCancelMatchmaking?.Invoke();
        Hide();
    }

    private void OnEnable()
    {
        startButton.onClick.AddListener(OnStartClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);

        MatchmakingManager.Instance.OnMatchmakingStateChanged += OnMatchmakingStateChanged;

        UpdateUI(MatchmakingManager.Instance.CurrentState);
    }

    private void OnDisable()
    {
        startButton.onClick.RemoveListener(OnStartClicked);
        cancelButton.onClick.RemoveListener(OnCancelClicked);

        if (MatchmakingManager.Instance != null)
            MatchmakingManager.Instance.OnMatchmakingStateChanged -= OnMatchmakingStateChanged;
    }

 private void OnStartClicked()
    {
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

    private void OnCancelClicked()
    {
        MatchmakingManager.Instance.CancelMatchmaking();
    }

    private void OnMatchmakingStateChanged(MatchmakingState state)
    {
        UpdateUI(state);
    }
    
      private void UpdateUI(MatchmakingState state)
    {
        switch (state)
        {
            case MatchmakingState.Idle:
                statusText.text = "Ready to find match";
                startButton.interactable = true;
                cancelButton.interactable = false;
                break;
            case MatchmakingState.Searching:
                statusText.text = "Searching for match...";
                startButton.interactable = false;
                cancelButton.interactable = true;
                break;
            case MatchmakingState.MatchFound:
                statusText.text = "Match found! Starting game...";
                startButton.interactable = false;
                cancelButton.interactable = false;
                break;
            case MatchmakingState.Failed:
                statusText.text = "Matchmaking failed. Try again.";
                startButton.interactable = true;
                cancelButton.interactable = false;
                break;
        }
    }
}
