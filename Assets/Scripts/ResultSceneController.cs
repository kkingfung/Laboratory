// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultSceneController : MonoBehaviour
{
    [Header("UI Components")]
    public Text resultText;
    public Button replayButton;
    public Button mainMenuButton;

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene"; // Replace with the name of your game scene
    public string mainMenuSceneName = "MainMenuScene"; // Replace with the name of your main menu scene

    private void Start()
    {
        // Assign button listeners
        if (replayButton != null)
            replayButton.onClick.AddListener(ReplayGame);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);

        DisplayResult();
    }

    private void DisplayResult()
    {
        // Logic to display the result (e.g., win/lose, score, etc.)
        if (resultText != null)
        {
            // Example: Fetch result data from a game manager or similar system
            string result = PlayerPrefs.GetString("GameResult", "Result Unavailable");
            resultText.text = result;
        }
    }

    private void ReplayGame()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Game scene name is not set.");
        }
    }

    private void GoToMainMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("Main menu scene name is not set.");
        }
    }
}