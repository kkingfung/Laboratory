// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneController : MonoBehaviour
{
    [Header("UI Components")]
    public Button startButton;
    public Button optionsButton;
    public Button quitButton;

    [Header("Scene Settings")]
    public string gameSceneName = "GameScene"; // Replace with the name of your game scene
    public string optionsSceneName = "OptionsScene"; // Replace with the name of your options scene

    private void Start()
    {
        // Assign button listeners
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OpenOptions);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void StartGame()
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

    private void OpenOptions()
    {
        if (!string.IsNullOrEmpty(optionsSceneName))
        {
            SceneManager.LoadScene(optionsSceneName);
        }
        else
        {
            Debug.LogError("Options scene name is not set.");
        }
    }

    private void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}