using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static async UniTaskVoid LoadSceneAsync(string sceneName)
    {
        await SceneManager.LoadSceneAsync(sceneName).ToUniTask();
    }
}
