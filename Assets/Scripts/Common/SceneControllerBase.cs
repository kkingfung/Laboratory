// 2025/7/21 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class SceneController : MonoBehaviour
{
    // Stack to keep track of loaded scenes
    private Stack<string> sceneStack = new Stack<string>();

    /// <summary>
    /// Loads a new scene additively and pushes it onto the stack.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public void PushScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive).completed += (operation) =>
            {
                sceneStack.Push(sceneName);
                OnScenePushed(sceneName);
            };
        }
    }

    /// <summary>
    /// Pops the top scene from the stack and unloads it.
    /// </summary>
    public void PopScene()
    {
        if (sceneStack.Count > 0)
        {
            string topScene = sceneStack.Pop();
            SceneManager.UnloadSceneAsync(topScene).completed += (operation) =>
            {
                OnScenePopped(topScene);
            };
        }
    }

    /// <summary>
    /// Retrieves the name of the current scene on top of the stack.
    /// </summary>
    /// <returns>The name of the current scene, or null if the stack is empty.</returns>
    public string GetCurrentScene()
    {
        return sceneStack.Count > 0 ? sceneStack.Peek() : null;
    }

    /// <summary>
    /// Abstract method triggered after a scene is pushed.
    /// </summary>
    /// <param name="sceneName">The name of the scene that was pushed.</param>
    protected abstract void OnScenePushed(string sceneName);

    /// <summary>
    /// Abstract method triggered after a scene is popped.
    /// </summary>
    /// <param name="sceneName">The name of the scene that was popped.</param>
    protected abstract void OnScenePopped(string sceneName);
}