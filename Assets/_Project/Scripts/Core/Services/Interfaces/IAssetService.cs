using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Service interface for asset loading and management.
    /// Provides unified asset loading from multiple sources with caching support.
    /// </summary>
    public interface IAssetService : IDisposable
    {
        /// <summary>Loads a single asset asynchronously and caches it.</summary>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <param name="key">Asset key/path</param>
        /// <param name="source">Asset source (Auto, Resources, Addressables, StreamingAssets)</param>
        /// <returns>The loaded asset or null if loading failed</returns>
        UniTask<T?> LoadAssetAsync<T>(string key, AssetSource source = AssetSource.Auto) where T : UnityEngine.Object;
        
        /// <summary>Loads multiple assets in parallel.</summary>
        /// <param name="keys">Collection of asset keys to load</param>
        /// <param name="source">Asset source to load from</param>
        /// <returns>Task that completes when all assets are loaded</returns>
        UniTask LoadAssetsAsync(IEnumerable<string> keys, AssetSource source = AssetSource.Auto);
        
        /// <summary>Preloads core game assets needed at startup.</summary>
        /// <param name="progress">Progress reporter for loading operations</param>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>Task that completes when core assets are preloaded</returns>
        UniTask PreloadCoreAssetsAsync(IProgress<float>? progress = null, CancellationToken cancellation = default);
        
        /// <summary>Gets a cached asset by key without loading.</summary>
        /// <typeparam name="T">The type of asset to retrieve</typeparam>
        /// <param name="key">Asset key</param>
        /// <returns>The cached asset or null if not found</returns>
        T? GetCachedAsset<T>(string key) where T : UnityEngine.Object;
        
        /// <summary>Checks if an asset is currently cached.</summary>
        /// <param name="key">Asset key to check</param>
        /// <returns>True if asset is cached, false otherwise</returns>
        bool IsAssetCached(string key);
        
        /// <summary>Unloads and removes an asset from cache.</summary>
        /// <param name="key">Asset key to unload</param>
        void UnloadAsset(string key);
        
        /// <summary>Clears all cached assets.</summary>
        void ClearCache();
        
        /// <summary>Gets cache statistics.</summary>
        /// <returns>Current cache statistics</returns>
        AssetCacheStats GetCacheStats();
    }

    /// <summary>
    /// Asset source types for loading.
    /// </summary>
    public enum AssetSource
    {
        /// <summary>Try Addressables first, then Resources.</summary>
        Auto,
        /// <summary>Unity Resources folder.</summary>
        Resources,
        /// <summary>Addressable Asset System.</summary>
        Addressables,
        /// <summary>StreamingAssets folder.</summary>
        StreamingAssets
    }

    /// <summary>
    /// Asset cache statistics for monitoring and debugging.
    /// </summary>
    public struct AssetCacheStats
    {
        /// <summary>Total number of cached assets.</summary>
        public int TotalAssets { get; set; }
        
        /// <summary>Estimated total memory usage in bytes.</summary>
        public long TotalMemoryUsage { get; set; }
        
        /// <summary>Number of assets loaded from Resources.</summary>
        public int ResourcesAssets { get; set; }
        
        /// <summary>Number of assets loaded from Addressables.</summary>
        public int AddressableAssets { get; set; }
    }
}
