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
    /// Implementation of IConfigService with improved caching and validation.
    /// </summary>
    public class ConfigService : IConfigService, IDisposable
    {
        #region Fields

        private readonly IEventBus _eventBus;
        private readonly Dictionary<string, object> _configCache = new();
        private bool _disposed = false;

        #endregion

        #region Constructor

        public ConfigService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        #endregion

        #region IConfigService Implementation

        public async UniTask<T?> LoadJsonConfigAsync<T>(string relativePath) where T : class
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentException("Relative path cannot be null or empty", nameof(relativePath));

            // Check cache first
            if (_configCache.TryGetValue(relativePath, out var cached) && cached is T cachedConfig)
            {
                return cachedConfig;
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
            _eventBus.Publish(new LoadingStartedEvent($"Config:{relativePath}", $"Loading config: {relativePath}"));

            try
            {
                string json;

#if UNITY_ANDROID && !UNITY_EDITOR
                using var www = UnityEngine.Networking.UnityWebRequest.Get(fullPath);
                await www.SendWebRequest();
                
                if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    throw new InvalidOperationException($"Failed to load config: {www.error}");
                }
                
                json = www.downloadHandler.text;
#else
                json = await File.ReadAllTextAsync(fullPath);
#endif

                var config = JsonUtility.FromJson<T>(json);
                
                if (config != null)
                {
                    if (ValidateConfig(config))
                    {
                        _configCache[relativePath] = config;
                        _eventBus.Publish(new LoadingCompletedEvent($"Config:{relativePath}", true));
                        return config;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Config validation failed for {relativePath}");
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _eventBus.Publish(new LoadingCompletedEvent($"Config:{relativePath}", false, ex.Message));
                Debug.LogError($"ConfigService: Failed to load JSON config from {relativePath}: {ex}");
                return null;
            }
        }

        public async UniTask<T?> LoadScriptableObjectConfigAsync<T>(string resourcePath) where T : ScriptableObject
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrEmpty(resourcePath))
                throw new ArgumentException("Resource path cannot be null or empty", nameof(resourcePath));

            // Check cache first
            if (_configCache.TryGetValue(resourcePath, out var cached) && cached is T cachedConfig)
            {
                return cachedConfig;
            }

            _eventBus.Publish(new LoadingStartedEvent($"Config:{resourcePath}", $"Loading ScriptableObject config: {resourcePath}"));

            try
            {
                var request = Resources.LoadAsync<T>(resourcePath);
                await request;

                if (request.asset is T config)
                {
                    _configCache[resourcePath] = config;
                    _eventBus.Publish(new LoadingCompletedEvent($"Config:{resourcePath}", true));
                    return config;
                }
                
                _eventBus.Publish(new LoadingCompletedEvent($"Config:{resourcePath}", false, "Asset not found"));
                return null;
            }
            catch (Exception ex)
            {
                _eventBus.Publish(new LoadingCompletedEvent($"Config:{resourcePath}", false, ex.Message));
                Debug.LogError($"ConfigService: Failed to load ScriptableObject config at {resourcePath}: {ex}");
                return null;
            }
        }

        public T? GetCachedConfig<T>(string key) where T : class
        {
            ThrowIfDisposed();
            
            return _configCache.TryGetValue(key, out var config) && config is T cachedConfig ? cachedConfig : null;
        }

        public async UniTask PreloadEssentialConfigsAsync(IProgress<float>? progress = null, CancellationToken cancellation = default)
        {
            ThrowIfDisposed();
            
            // Define essential configs that should be loaded at startup
            var essentialConfigs = new[]
            {
                ("Configs/game.json", typeof(object)), // Replace with your actual config types
                ("Configs/audio.json", typeof(object)),
                ("Configs/graphics.json", typeof(object)),
                // Add more essential configs as needed
            };

            var totalConfigs = essentialConfigs.Length;
            var completedConfigs = 0;

            foreach (var (configPath, configType) in essentialConfigs)
            {
                cancellation.ThrowIfCancellationRequested();
                
                // Load config (this is a simplified example - you'd need proper type handling)
                await LoadJsonConfigAsync<object>(configPath);
                
                completedConfigs++;
                progress?.Report((float)completedConfigs / totalConfigs);
            }

            Debug.Log($"ConfigService: Preloaded {completedConfigs} essential configs");
        }

        public bool ValidateConfig<T>(T config) where T : class
        {
            if (config == null) return false;
            
            // Add custom validation logic here
            // This could include checking required fields, value ranges, etc.
            
            return true; // Placeholder - implement validation as needed
        }

        public void ClearCache()
        {
            ThrowIfDisposed();
            
            _configCache.Clear();
            Debug.Log("ConfigService: Cache cleared");
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
                throw new ObjectDisposedException(nameof(ConfigService));
        }

        #endregion
    }

    #endregion
}