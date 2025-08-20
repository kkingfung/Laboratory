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
    /// Service interface for asset loading and management.
    /// Replaces the AssetPreloader with a more comprehensive service.
    /// </summary>
    public interface IAssetService
    {
        /// <summary>Preloads a single asset and caches it.</summary>
        UniTask<T?> LoadAssetAsync<T>(string key, AssetSource source = AssetSource.Auto) where T : UnityEngine.Object;
        
        /// <summary>Preloads multiple assets in parallel.</summary>
        UniTask LoadAssetsAsync(IEnumerable<string> keys, AssetSource source = AssetSource.Auto);
        
        /// <summary>Preloads core game assets needed at startup.</summary>
        UniTask PreloadCoreAssetsAsync(IProgress<float>? progress = null, CancellationToken cancellation = default);
        
        /// <summary>Gets a cached asset by key.</summary>
        T? GetCachedAsset<T>(string key) where T : UnityEngine.Object;
        
        /// <summary>Checks if an asset is cached.</summary>
        bool IsAssetCached(string key);
        
        /// <summary>Unloads and removes an asset from cache.</summary>
        void UnloadAsset(string key);
        
        /// <summary>Clears all cached assets.</summary>
        void ClearCache();
        
        /// <summary>Gets cache statistics.</summary>
        AssetCacheStats GetCacheStats();
    }

    /// <summary>
    /// Asset source types for loading.
    /// </summary>
    public enum AssetSource
    {
        Auto,           // Try Addressables first, then Resources
        Resources,      // Unity Resources folder
        Addressables,   // Addressable Asset System
        StreamingAssets // StreamingAssets folder
    }

    /// <summary>
    /// Asset cache statistics.
    /// </summary>
    public struct AssetCacheStats
    {
        public int TotalAssets { get; set; }
        public long TotalMemoryUsage { get; set; }
        public int ResourcesAssets { get; set; }
        public int AddressableAssets { get; set; }
    }

    #endregion
}
