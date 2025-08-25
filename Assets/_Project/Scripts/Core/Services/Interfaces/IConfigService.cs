using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Service interface for configuration loading and management.
    /// Provides unified config loading with validation and caching support.
    /// </summary>
    public interface IConfigService : IDisposable
    {
        /// <summary>Loads a JSON configuration file from StreamingAssets.</summary>
        /// <typeparam name="T">The type to deserialize the config into</typeparam>
        /// <param name="relativePath">Path relative to StreamingAssets folder</param>
        /// <returns>The loaded and deserialized config or null if loading failed</returns>
        UniTask<T?> LoadJsonConfigAsync<T>(string relativePath) where T : class;
        
        /// <summary>Loads a ScriptableObject configuration from Resources.</summary>
        /// <typeparam name="T">The ScriptableObject type to load</typeparam>
        /// <param name="resourcePath">Path in Resources folder</param>
        /// <returns>The loaded ScriptableObject config or null if loading failed</returns>
        UniTask<T?> LoadScriptableObjectConfigAsync<T>(string resourcePath) where T : ScriptableObject;
        
        /// <summary>Gets a previously loaded config from cache.</summary>
        /// <typeparam name="T">The type of config to retrieve</typeparam>
        /// <param name="key">Config cache key</param>
        /// <returns>The cached config or null if not found</returns>
        T? GetCachedConfig<T>(string key) where T : class;
        
        /// <summary>Preloads all essential configuration files at startup.</summary>
        /// <param name="progress">Progress reporter for loading operations</param>
        /// <param name="cancellation">Cancellation token</param>
        /// <returns>Task that completes when essential configs are loaded</returns>
        UniTask PreloadEssentialConfigsAsync(IProgress<float>? progress = null, CancellationToken cancellation = default);
        
        /// <summary>Validates a configuration object against defined rules.</summary>
        /// <typeparam name="T">The type of config to validate</typeparam>
        /// <param name="config">The config object to validate</param>
        /// <returns>True if config is valid, false otherwise</returns>
        bool ValidateConfig<T>(T config) where T : class;
        
        /// <summary>Clears all cached configurations.</summary>
        void ClearCache();
    }
}
