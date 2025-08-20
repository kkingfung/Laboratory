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
    #region Config Service

    /// <summary>
    /// Service interface for configuration loading and management.
    /// Replaces ConfigLoader with caching and validation.
    /// </summary>
    public interface IConfigService
    {
        /// <summary>Loads a JSON config from StreamingAssets.</summary>
        UniTask<T?> LoadJsonConfigAsync<T>(string relativePath) where T : class;
        
        /// <summary>Loads a ScriptableObject config from Resources.</summary>
        UniTask<T?> LoadScriptableObjectConfigAsync<T>(string resourcePath) where T : ScriptableObject;
        
        /// <summary>Gets a cached config by key.</summary>
        T? GetCachedConfig<T>(string key) where T : class;
        
        /// <summary>Preloads all essential configs at startup.</summary>
        UniTask PreloadEssentialConfigsAsync(IProgress<float>? progress = null, CancellationToken cancellation = default);
        
        /// <summary>Validates a config object against defined rules.</summary>
        bool ValidateConfig<T>(T config) where T : class;
        
        /// <summary>Clears all cached configs.</summary>
        void ClearCache();
    }

    #endregion
}