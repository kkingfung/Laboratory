using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using UnityEngine;
using Newtonsoft.Json;

#nullable enable

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Implementation of IConfigService that handles configuration loading and caching.
    /// </summary>
    public class ConfigService : IConfigService, IDisposable
    {
        private readonly Dictionary<string, object> _cache = new();
        private readonly IEventBus _eventBus;
        private bool _disposed = false;

        public ConfigService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async UniTask<T?> LoadJsonConfigAsync<T>(string relativePath) where T : class
        {
            ThrowIfDisposed();

            var cacheKey = $"json:{relativePath}";
            if (_cache.TryGetValue(cacheKey, out var cached) && cached is T cachedConfig)
            {
                return cachedConfig;
            }

            try
            {
                var fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);
                
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"Config file not found: {fullPath}");
                    return null;
                }

                var json = await File.ReadAllTextAsync(fullPath);
                var config = JsonConvert.DeserializeObject<T>(json);
                
                if (config != null && ValidateConfig(config))
                {
                    _cache[cacheKey] = config;
                    return config;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load JSON config '{relativePath}': {ex.Message}");
            }

            return null;
        }

        public async UniTask<T?> LoadScriptableObjectConfigAsync<T>(string resourcePath) where T : ScriptableObject
        {
            ThrowIfDisposed();

            var cacheKey = $"so:{resourcePath}";
            if (_cache.TryGetValue(cacheKey, out var cached) && cached is T cachedConfig)
            {
                return cachedConfig;
            }

            try
            {
                var request = Resources.LoadAsync<T>(resourcePath);
                await request;
                
                var config = request.asset as T;
                if (config != null)
                {
                    _cache[cacheKey] = config;
                    return config;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load ScriptableObject config '{resourcePath}': {ex.Message}");
            }

            return null;
        }

        public T? GetCachedConfig<T>(string key) where T : class
        {
            ThrowIfDisposed();
            return _cache.TryGetValue(key, out var config) && config is T cachedConfig ? cachedConfig : null;
        }

        public async UniTask PreloadEssentialConfigsAsync(IProgress<float>? progress = null, CancellationToken cancellation = default)
        {
            ThrowIfDisposed();

            var essentialConfigs = new[]
            {
                ("game_settings.json", typeof(object)),
                ("input_bindings.json", typeof(object)),
                ("audio_settings.json", typeof(object)),
                ("graphics_settings.json", typeof(object))
            };

            progress?.Report(0f);
            _eventBus.Publish(new LoadingStartedEvent("EssentialConfigs", "Loading essential configurations"));

            for (int i = 0; i < essentialConfigs.Length; i++)
            {
                cancellation.ThrowIfCancellationRequested();
                
                try
                {
                    await LoadJsonConfigAsync<object>(essentialConfigs[i].Item1);
                    progress?.Report((float)(i + 1) / essentialConfigs.Length);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to preload essential config '{essentialConfigs[i].Item1}': {ex.Message}");
                }
            }

            _eventBus.Publish(new LoadingCompletedEvent("EssentialConfigs", true));
        }

        public bool ValidateConfig<T>(T config) where T : class
        {
            ThrowIfDisposed();

            if (config == null)
                return false;

            // Basic validation - can be extended with specific rules
            try
            {
                // Check if config has required properties (example validation)
                var type = typeof(T);
                var properties = type.GetProperties();
                
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(config);
                    
                    // Check for required attributes or validation rules
                    if (prop.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false).Length > 0)
                    {
                        if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                        {
                            Debug.LogError($"Config validation failed: Required property '{prop.Name}' is null or empty");
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Config validation error: {ex.Message}");
                return false;
            }
        }

        public void ClearCache()
        {
            ThrowIfDisposed();
            _cache.Clear();
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
                throw new ObjectDisposedException(nameof(ConfigService));
        }
    }
}
