using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

#nullable enable

namespace Laboratory.Core.Services
{
    #region Asset Service
    
    /// <summary>
    /// Implementation of IAssetService with improved caching, progress reporting, and error handling.
    /// </summary>
    public class AssetService : IAssetService, IDisposable
    {
        #region Fields

        private readonly IEventBus _eventBus;
        private readonly Dictionary<string, UnityEngine.Object> _cache = new();
        private readonly Dictionary<string, AsyncOperationHandle> _addressableHandles = new();
        private bool _disposed = false;

        #endregion

        #region Constructor

        public AssetService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        #endregion

        #region IAssetService Implementation

        public async UniTask<T?> LoadAssetAsync<T>(string key, AssetSource source = AssetSource.Auto) where T : UnityEngine.Object
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Asset key cannot be null or empty", nameof(key));

            // Check cache first
            if (_cache.TryGetValue(key, out var cached) && cached is T cachedAsset)
            {
                return cachedAsset;
            }

            _eventBus.Publish(new LoadingStartedEvent($"Asset:{key}", $"Loading asset: {key}"));

            try
            {
                T? asset = source switch
                {
                    AssetSource.Resources => await LoadFromResourcesAsync<T>(key),
                    AssetSource.Addressables => await LoadFromAddressablesAsync<T>(key),
                    AssetSource.StreamingAssets => await LoadFromStreamingAssetsAsync<T>(key),
                    AssetSource.Auto => await LoadFromAutoAsync<T>(key),
                    _ => throw new ArgumentOutOfRangeException(nameof(source))
                };

                if (asset != null)
                {
                    _cache[key] = asset;
                    _eventBus.Publish(new LoadingCompletedEvent($"Asset:{key}", true));
                }
                else
                {
                    _eventBus.Publish(new LoadingCompletedEvent($"Asset:{key}", false, "Asset not found"));
                }

                return asset;
            }
            catch (Exception ex)
            {
                _eventBus.Publish(new LoadingCompletedEvent($"Asset:{key}", false, ex.Message));
                Debug.LogError($"AssetService: Failed to load asset '{key}': {ex}");
                return null;
            }
        }

        public async UniTask LoadAssetsAsync(IEnumerable<string> keys, AssetSource source = AssetSource.Auto)
        {
            ThrowIfDisposed();
            
            var tasks = new List<UniTask>();
            foreach (var key in keys)
            {
                tasks.Add(LoadAssetAsync<UnityEngine.Object>(key, source));
            }
            
            await UniTask.WhenAll(tasks);
        }

        public async UniTask PreloadCoreAssetsAsync(IProgress<float>? progress = null, CancellationToken cancellation = default)
        {
            ThrowIfDisposed();
            
            // Define core assets that should be preloaded at startup
            var coreAssets = new[]
            {
                ("UI/LoadingSpinner", AssetSource.Resources),
                ("UI/ErrorDialog", AssetSource.Resources),
                ("Audio/UIClick", AssetSource.Resources),
                ("Prefabs/NetworkPlayer", AssetSource.Resources),
                // Add more core assets as needed
            };

            var totalAssets = coreAssets.Length;
            var completedAssets = 0;

            foreach (var (assetKey, assetSource) in coreAssets)
            {
                cancellation.ThrowIfCancellationRequested();
                
                await LoadAssetAsync<UnityEngine.Object>(assetKey, assetSource);
                
                completedAssets++;
                progress?.Report((float)completedAssets / totalAssets);
            }

            Debug.Log($"AssetService: Preloaded {completedAssets} core assets");
        }

        public T? GetCachedAsset<T>(string key) where T : UnityEngine.Object
        {
            ThrowIfDisposed();
            
            return _cache.TryGetValue(key, out var asset) && asset is T cachedAsset ? cachedAsset : null;
        }

        public bool IsAssetCached(string key)
        {
            ThrowIfDisposed();
            return _cache.ContainsKey(key);
        }

        public void UnloadAsset(string key)
        {
            ThrowIfDisposed();
            
            if (_cache.TryGetValue(key, out var asset))
            {
                _cache.Remove(key);
                
                // Release Addressable handles
                if (_addressableHandles.TryGetValue(key, out var handle))
                {
                    Addressables.Release(handle);
                    _addressableHandles.Remove(key);
                }
                
                // Don't destroy Resources assets as Unity manages them
            }
        }

        public void ClearCache()
        {
            ThrowIfDisposed();
            
            // Release all Addressable handles
            foreach (var handle in _addressableHandles.Values)
            {
                Addressables.Release(handle);
            }
            
            _addressableHandles.Clear();
            _cache.Clear();
            
            Debug.Log("AssetService: Cache cleared");
        }

        public AssetCacheStats GetCacheStats()
        {
            ThrowIfDisposed();
            
            var stats = new AssetCacheStats
            {
                TotalAssets = _cache.Count,
                AddressableAssets = _addressableHandles.Count
            };
            
            // Calculate approximate memory usage and categorize assets
            foreach (var kvp in _cache)
            {
                var asset = kvp.Value;
                
                // Rough memory estimation (this is approximate)
                if (asset is Texture2D texture)
                    stats.TotalMemoryUsage += texture.width * texture.height * 4; // RGBA
                else if (asset is AudioClip audio)
                    stats.TotalMemoryUsage += audio.samples * audio.channels * 4; // 32-bit float
                else if (asset is Mesh mesh)
                    stats.TotalMemoryUsage += mesh.vertexCount * 32; // Rough estimate
                
                if (!_addressableHandles.ContainsKey(kvp.Key))
                    stats.ResourcesAssets++;
            }
            
            return stats;
        }

        #endregion

        #region Private Methods

        private async UniTask<T?> LoadFromResourcesAsync<T>(string key) where T : UnityEngine.Object
        {
            var request = Resources.LoadAsync<T>(key);
            await request;
            return request.asset as T;
        }

        private async UniTask<T?> LoadFromAddressablesAsync<T>(string key) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _addressableHandles[key] = handle;
                return handle.Result;
            }
            
            return null;
        }

        private async UniTask<T?> LoadFromStreamingAssetsAsync<T>(string key) where T : UnityEngine.Object
        {
            // StreamingAssets loading depends on the asset type
            // This is a placeholder - implement based on your specific needs
            await UniTask.Yield();
            Debug.LogWarning($"AssetService: StreamingAssets loading not implemented for {typeof(T).Name}");
            return null;
        }

        private async UniTask<T?> LoadFromAutoAsync<T>(string key) where T : UnityEngine.Object
        {
            // Try Addressables first, then Resources
            var asset = await LoadFromAddressablesAsync<T>(key);
            if (asset != null) return asset;
            
            return await LoadFromResourcesAsync<T>(key);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            
            ClearCache();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AssetService));
        }

        #endregion
    }

    #endregion
}