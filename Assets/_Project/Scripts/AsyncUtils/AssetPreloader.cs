using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Infrastructure
{
    /// <summary>
    /// Async asset preloader that loads and caches assets by key.
    /// Supports Resources folder and Addressables.
    /// </summary>
    public class AssetPreloader
    {
        #region Fields

        private readonly Dictionary<string, UnityEngine.Object> _cache = new Dictionary<string, UnityEngine.Object>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Preload a single asset from Resources folder asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of asset</typeparam>
        /// <param name="resourcePath">Path under Resources folder</param>
        /// <returns>Loaded asset or null</returns>
        public async UniTask<T?> PreloadFromResourcesAsync<T>(string resourcePath) where T : UnityEngine.Object
        {
            if (_cache.TryGetValue(resourcePath, out var cached))
            {
                return cached as T;
            }

            var request = Resources.LoadAsync<T>(resourcePath);
            await request;

            if (request.asset is T asset)
            {
                _cache[resourcePath] = asset;
                return asset;
            }
            else
            {
                Debug.LogError($"AssetPreloader: Failed to load {resourcePath} from Resources.");
                return null;
            }
        }

        /// <summary>
        /// Preload a single asset from Addressables asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of asset</typeparam>
        /// <param name="address">Addressable asset key</param>
        /// <returns>Loaded asset or null</returns>
        public async UniTask<T?> PreloadFromAddressablesAsync<T>(string address) where T : UnityEngine.Object
        {
            if (_cache.TryGetValue(address, out var cached))
            {
                return cached as T;
            }

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _cache[address] = handle.Result;
                return handle.Result;
            }
            else
            {
                Debug.LogError($"AssetPreloader: Failed to load {address} from Addressables.");
                return null;
            }
        }

        /// <summary>
        /// Preload core assets needed at game start.
        /// Add your critical assets here.
        /// </summary>
        public async UniTask PreloadCoreAssets()
        {
            // Example: preload UI textures, player prefab, audio clips
            await PreloadFromResourcesAsync<Texture2D>("UI/HUD_HealthBar");
            await PreloadFromResourcesAsync<GameObject>("Prefabs/Player");

            // If using Addressables:
            // await PreloadFromAddressablesAsync<AudioClip>("Audio/BackgroundMusic");

            Debug.Log("AssetPreloader: Core assets preloaded.");
        }

        /// <summary>
        /// Try get a cached asset by key.
        /// </summary>
        public bool TryGetCachedAsset<T>(string key, out T? asset) where T : UnityEngine.Object
        {
            if (_cache.TryGetValue(key, out var cached) && cached is T casted)
            {
                asset = casted;
                return true;
            }

            asset = null;
            return false;
        }

        /// <summary>
        /// Clear all cached assets.
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        #endregion
    }
}
