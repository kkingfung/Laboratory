// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchSceneController : MonoBehaviour
{
    [Header("UI Components")]
    public Button backButton;
    public Button readyButton;
    public Text statusText;

    [Header("Match Settings")]
    public string lobbySceneName = "LobbyScene";

    private bool isReady = false;

    private void Start()
    {
        // Assign button listeners
        if (backButton != null)
            backButton.onClick.AddListener(LeaveMatch);

        if (readyButton != null)
            readyButton.onClick.AddListener(ToggleReady);

        UpdateStatusText();
    }

    private void LeaveMatch()
    {
        // Logic to leave the match and return to the lobby
        Debug.Log("Leaving match...");
        if (!string.IsNullOrEmpty(lobbySceneName))
        {
            SceneManager.LoadScene(lobbySceneName);
        }
        else
        {
            Debug.LogError("Lobby scene name is not set.");
        }
    }

    private void ToggleReady()
    {
        isReady = !isReady;
        UpdateStatusText();

        // Notify server or matchmaking system about ready status
        Debug.Log(isReady ? "Player is ready." : "Player is not ready.");
    }

    private void UpdateStatusText()
    {
        if (statusText != null)
        {
            statusText.text = isReady ? "Ready" : "Not Ready";
        }
    }
}