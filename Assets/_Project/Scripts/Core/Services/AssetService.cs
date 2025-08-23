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

#nullable enable

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Implementation of IAssetService that handles asset loading from multiple sources.
    /// </summary>
    public class AssetService : IAssetService, IDisposable
    {
        private readonly Dictionary<string, UnityEngine.Object> _cache = new();
        private readonly Dictionary<string, AsyncOperationHandle> _addressableHandles = new();
        private readonly IEventBus _eventBus;
        private bool _disposed = false;

        public AssetService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async UniTask<T?> LoadAssetAsync<T>(string key, AssetSource source = AssetSource.Auto) where T : UnityEngine.Object
        {
            ThrowIfDisposed();

            if (_cache.TryGetValue(key, out var cached) && cached is T cachedAsset)
            {
                return cachedAsset;
            }

            T? asset = null;
            
            try
            {
                asset = source switch
                {
                    AssetSource.Resources => await LoadFromResourcesAsync<T>(key),
                    AssetSource.Addressables => await LoadFromAddressablesAsync<T>(key),
                    AssetSource.StreamingAssets => await LoadFromStreamingAssetsAsync<T>(key),
                    AssetSource.Auto => await LoadAutoAsync<T>(key),
                    _ => throw new ArgumentException($"Unknown asset source: {source}")
                };

                if (asset != null)
                {
                    _cache[key] = asset;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load asset '{key}' from {source}: {ex.Message}");
            }

            return asset;
        }

        public async UniTask LoadAssetsAsync(IEnumerable<string> keys, AssetSource source = AssetSource.Auto)
        {
            var tasks = new List<UniTask>();
            foreach (var key in keys)
            {
                tasks.Add(LoadAssetAsync<UnityEngine.Object>(key, source).AsUniTask());
            }
            await UniTask.WhenAll(tasks);
        }

        public async UniTask PreloadCoreAssetsAsync(IProgress<float>? progress = null, CancellationToken cancellation = default)
        {
            ThrowIfDisposed();

            var coreAssets = new[]
            {
                "UI/MainMenuPrefab",
                "UI/LoadingScreenPrefab", 
                "UI/HUDPrefab",
                "Audio/UIClickSound",
                "Textures/DefaultTexture"
            };

            progress?.Report(0f);
            
            for (int i = 0; i < coreAssets.Length; i++)
            {
                cancellation.ThrowIfCancellationRequested();
                
                try
                {
                    await LoadAssetAsync<UnityEngine.Object>(coreAssets[i]);
                    progress?.Report((float)(i + 1) / coreAssets.Length);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to preload core asset '{coreAssets[i]}': {ex.Message}");
                }
            }

            _eventBus.Publish(new LoadingCompletedEvent("CoreAssets", true));
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
                if (_addressableHandles.TryGetValue(key, out var handle))
                {
                    Addressables.Release(handle);
                    _addressableHandles.Remove(key);
                }
                
                _cache.Remove(key);
            }
        }

        public void ClearCache()
        {
            ThrowIfDisposed();
            
            foreach (var handle in _addressableHandles.Values)
            {
                Addressables.Release(handle);
            }
            
            _addressableHandles.Clear();
            _cache.Clear();
        }

        public AssetCacheStats GetCacheStats()
        {
            ThrowIfDisposed();
            
            long memoryUsage = 0;
            int resourcesAssets = 0;
            int addressableAssets = 0;

            foreach (var asset in _cache.Values)
            {
                if (asset != null)
                {
                    // Rough memory usage estimation
                    if (asset is Texture2D tex)
                        memoryUsage += tex.width * tex.height * 4; // Assume RGBA32
                    else if (asset is AudioClip clip)
                        memoryUsage += clip.samples * clip.channels * 4;
                    else
                        memoryUsage += 1024; // Default estimate
                }
            }

            resourcesAssets = _cache.Count - _addressableHandles.Count;
            addressableAssets = _addressableHandles.Count;

            return new AssetCacheStats
            {
                TotalAssets = _cache.Count,
                TotalMemoryUsage = memoryUsage,
                ResourcesAssets = resourcesAssets,
                AddressableAssets = addressableAssets
            };
        }

        private async UniTask<T?> LoadFromResourcesAsync<T>(string key) where T : UnityEngine.Object
        {
            var request = Resources.LoadAsync<T>(key);
            await request;
            return request.asset as T;
        }

        private async UniTask<T?> LoadFromAddressablesAsync<T>(string key) where T : UnityEngine.Object
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                _addressableHandles[key] = handle;
                var asset = await handle;
                return asset;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async UniTask<T?> LoadFromStreamingAssetsAsync<T>(string key) where T : UnityEngine.Object
        {
            // StreamingAssets loading for specific types (e.g., text files, configs)
            if (typeof(T) == typeof(TextAsset))
            {
                var path = Path.Combine(Application.streamingAssetsPath, key);
                if (File.Exists(path))
                {
                    var text = await File.ReadAllTextAsync(path);
                    var textAsset = new TextAsset(text);
                    return textAsset as T;
                }
            }
            return null;
        }

        private async UniTask<T?> LoadAutoAsync<T>(string key) where T : UnityEngine.Object
        {
            // Try Addressables first, then Resources
            var asset = await LoadFromAddressablesAsync<T>(key);
            if (asset == null)
            {
                asset = await LoadFromResourcesAsync<T>(key);
            }
            return asset;
        }

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
    }
}
