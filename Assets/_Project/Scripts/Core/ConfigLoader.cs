using System;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Infrastructure
{
    /// <summary>
    /// Loads configuration data asynchronously from JSON files or ScriptableObjects.
    /// Provides generic methods to load configs as data models.
    /// </summary>
    public class ConfigLoader
    {
        #region Public Methods

        /// <summary>
        /// Load a config JSON file from StreamingAssets folder asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of config data model.</typeparam>
        /// <param name="relativePath">Path relative to StreamingAssets, e.g. "Configs/gameConfig.json"</param>
        /// <returns>Deserialized config object of type T.</returns>
        public async UniTask<T?> LoadJsonConfigAsync<T>(string relativePath) where T : class
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

            try
            {
                string json;

#if UNITY_ANDROID && !UNITY_EDITOR
                // On Android, StreamingAssets is compressed inside the APK, so use UnityWebRequest
                using (var www = UnityEngine.Networking.UnityWebRequest.Get(fullPath))
                {
                    await www.SendWebRequest();
                    if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Failed to load config JSON: {www.error}");
                        return null;
                    }
                    json = www.downloadHandler.text;
                }
#else
                // On other platforms, read file normally
                json = await File.ReadAllTextAsync(fullPath);
#endif
                // Deserialize JSON (you can switch to Newtonsoft.Json if preferred)
                var config = JsonUtility.FromJson<T>(json);

                return config;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConfigLoader: Failed to load JSON config from {relativePath} - {ex}");
                return null;
            }
        }

        /// <summary>
        /// Load a ScriptableObject config asset asynchronously from Resources folder.
        /// </summary>
        /// <typeparam name="T">ScriptableObject type</typeparam>
        /// <param name="resourcePath">Path under Resources, e.g. "Configs/GameConfig"</param>
        /// <returns>Loaded ScriptableObject instance or null if failed</returns>
        public async UniTask<T?> LoadScriptableObjectConfigAsync<T>(string resourcePath) where T : ScriptableObject
        {
            var request = Resources.LoadAsync<T>(resourcePath);
            await request;

            if (request.asset is T asset)
            {
                return asset;
            }
            else
            {
                Debug.LogError($"ConfigLoader: Failed to load ScriptableObject config at {resourcePath}");
                return null;
            }
        }

        #endregion
    }
}
