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

// Helper extension for converting Action to IDisposable
static class ActionExtensions
{
    public static System.IDisposable AsDisposable(this System.Action action)
    {
        return new ActionDisposable(action);
    }
    
    private class ActionDisposable : System.IDisposable
    {
        private System.Action? _action;
        
        public ActionDisposable(System.Action action)
        {
            _action = action;
        }
        
        public void Dispose()
        {
            _action?.Invoke();
            _action = null;
        }
    }
}

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Implementation of IAssetService that handles asset loading from multiple sources.
    /// </summary>
    public class AssetService : IAssetService, IDisposable
    {
        private readonly Dictionary<string, UnityEngine.Object> _cache = new();
        private readonly Dictionary<string, object> _addressableHandles = new();
        private readonly Dictionary<string, System.IDisposable> _handleDisposables = new();
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

            // Load core assets from a configurable list rather than hardcoded
            var coreAssets = GetCoreAssetList();

            progress?.Report(0f);
            
            var loadedCount = 0;
            
            for (int i = 0; i < coreAssets.Length; i++)
            {
                cancellation.ThrowIfCancellationRequested();
                
                try
                {
                    var asset = await LoadAssetAsync<UnityEngine.Object>(coreAssets[i]);
                    if (asset != null)
                    {
                        loadedCount++;
                        Debug.Log($"Successfully preloaded core asset: {coreAssets[i]}");
                    }
                    else
                    {
                        Debug.LogWarning($"Core asset '{coreAssets[i]}' not found - this is normal if the asset doesn't exist yet");
                    }
                    
                    progress?.Report((float)(i + 1) / coreAssets.Length);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Unable to preload core asset '{coreAssets[i]}': {ex.Message}");
                }
            }

            Debug.Log($"Preloaded {loadedCount}/{coreAssets.Length} core assets");
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
                if (_handleDisposables.TryGetValue(key, out var disposable))
                {
                    disposable?.Dispose();
                    _handleDisposables.Remove(key);
                }
                
                _addressableHandles.Remove(key);
                _cache.Remove(key);
            }
        }

        public void ClearCache()
        {
            ThrowIfDisposed();
            
            foreach (var disposable in _handleDisposables.Values)
            {
                disposable?.Dispose();
            }
            
            _handleDisposables.Clear();
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
                // Check if the key exists first to avoid InvalidKeyException
                var locationsHandle = Addressables.LoadResourceLocationsAsync(key);
                await locationsHandle;
                
                if (locationsHandle.Result == null || locationsHandle.Result.Count == 0)
                {
                    // Key doesn't exist, release the handle and return null gracefully
                    Addressables.Release(locationsHandle);
                    return null;
                }
                
                Addressables.Release(locationsHandle);
                
                // Now load the actual asset since we know the key exists
                var handle = Addressables.LoadAssetAsync<T>(key);
                await handle;
                T asset = handle.Result;
                
                if (asset != null)
                {
                    // Store a simple cleanup action
                    _addressableHandles[key] = key; // Just store the key for tracking
                    _handleDisposables[key] = new System.Action(() => 
                    {
                        // Use the asset reference to release
                        if (_cache.TryGetValue(key, out var cachedAsset) && cachedAsset != null)
                        {
                            Addressables.Release(cachedAsset);
                        }
                    }).AsDisposable();
                }
                
                return asset;
            }
            catch (System.Exception ex)
            {
                // Only log as warning instead of error for missing assets
                Debug.LogWarning($"Addressable asset '{key}' not found or failed to load: {ex.Message}");
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
        
        /// <summary>
        /// Gets the list of core assets to preload. Override this for custom asset lists.
        /// </summary>
        protected virtual string[] GetCoreAssetList()
        {
            // Return an empty list by default - no hardcoded assets
            // In the future, this could be loaded from a configuration file
            return new string[0];
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
