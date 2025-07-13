// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

public class SceneManagerController : MonoBehaviour
{
    public static SceneManagerController Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            Debug.LogError("Cannot load scenes directly in multiplayer mode. Use NetworkManager for scene transitions.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneAsync(string sceneName)
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            Debug.LogError("Cannot load scenes directly in multiplayer mode. Use NetworkManager for scene transitions.");
            return;
        }
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    private System.Collections.IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // Multiplayer-specific scene transition using NetworkManager
    public void LoadMultiplayerScene(string sceneName)
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("Only the server can initiate scene transitions in multiplayer mode.");
            return;
        }

        NetworkManager.singleton.ServerChangeScene(sceneName);
    }
}