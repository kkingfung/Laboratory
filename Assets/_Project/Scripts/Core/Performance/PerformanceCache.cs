using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Laboratory.Core.Performance
{
    /// <summary>
    /// Performance optimization utilities for caching commonly accessed properties.
    /// Eliminates repeated property access and allocations in hot paths.
    /// Use these patterns to optimize Update loops and frequently called methods.
    /// </summary>
    public static class PerformanceCache
    {
        #region Camera Caching

        private static Camera _cachedMainCamera;
        private static float _lastCameraCacheTime;
        private const float CAMERA_CACHE_TIMEOUT = 1f;

        /// <summary>
        /// Cached Camera.main access (eliminates FindGameObjectWithTag every call)
        /// </summary>
        public static Camera MainCamera
        {
            get
            {
                // Re-cache if null or expired
                if (_cachedMainCamera == null || Time.realtimeSinceStartup - _lastCameraCacheTime > CAMERA_CACHE_TIMEOUT)
                {
                    _cachedMainCamera = Camera.main;
                    _lastCameraCacheTime = Time.realtimeSinceStartup;
                }
                return _cachedMainCamera;
            }
        }

        /// <summary>
        /// Invalidate cached camera (call when camera changes)
        /// </summary>
        public static void InvalidateCamera()
        {
            _cachedMainCamera = null;
        }

        #endregion

        #region WaitForSeconds Caching

        private static readonly Dictionary<float, WaitForSeconds> _cachedWaits = new();

        /// <summary>
        /// Get a cached WaitForSeconds to avoid allocations in coroutines
        /// </summary>
        public static WaitForSeconds GetWait(float seconds)
        {
            if (!_cachedWaits.TryGetValue(seconds, out var wait))
            {
                wait = new WaitForSeconds(seconds);
                _cachedWaits[seconds] = wait;
            }
            return wait;
        }

        // Common wait times pre-cached
        public static readonly WaitForSeconds Wait01 = new WaitForSeconds(0.1f);
        public static readonly WaitForSeconds Wait02 = new WaitForSeconds(0.2f);
        public static readonly WaitForSeconds Wait05 = new WaitForSeconds(0.5f);
        public static readonly WaitForSeconds Wait1 = new WaitForSeconds(1f);
        public static readonly WaitForSeconds Wait2 = new WaitForSeconds(2f);
        public static readonly WaitForSeconds Wait5 = new WaitForSeconds(5f);
        public static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
        public static readonly WaitForFixedUpdate WaitForFixedUpdate = new WaitForFixedUpdate();

        #endregion

        #region StringBuilder Pool

        private static readonly Stack<StringBuilder> _stringBuilderPool = new();
        private const int MAX_BUILDER_POOL_SIZE = 10;

        /// <summary>
        /// Get a StringBuilder from the pool (eliminates allocations for string building)
        /// </summary>
        public static StringBuilder GetStringBuilder()
        {
            if (_stringBuilderPool.Count > 0)
            {
                var sb = _stringBuilderPool.Pop();
                sb.Clear();
                return sb;
            }
            return new StringBuilder(256);
        }

        /// <summary>
        /// Return a StringBuilder to the pool
        /// </summary>
        public static void ReturnStringBuilder(StringBuilder sb)
        {
            if (sb != null && _stringBuilderPool.Count < MAX_BUILDER_POOL_SIZE)
            {
                sb.Clear();
                _stringBuilderPool.Push(sb);
            }
        }

        /// <summary>
        /// Build a string using a pooled StringBuilder
        /// </summary>
        public static string BuildString(Action<StringBuilder> buildAction)
        {
            var sb = GetStringBuilder();
            try
            {
                buildAction(sb);
                return sb.ToString();
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }

        #endregion

        #region Component Caching

        /// <summary>
        /// Cache component references to avoid repeated GetComponent calls
        /// </summary>
        public class ComponentCache<T> where T : Component
        {
            private readonly Dictionary<GameObject, T> _cache = new();

            public T Get(GameObject gameObject)
            {
                if (!_cache.TryGetValue(gameObject, out var component))
                {
                    component = gameObject.GetComponent<T>();
                    if (component != null)
                    {
                        _cache[gameObject] = component;
                    }
                }
                return component;
            }

            public void Clear()
            {
                _cache.Clear();
            }

            public void Remove(GameObject gameObject)
            {
                _cache.Remove(gameObject);
            }
        }

        #endregion

        #region Transform Caching Helpers

        /// <summary>
        /// Cached transform position accessor to reduce property calls
        /// </summary>
        public struct TransformCache
        {
            public Transform transform;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 localScale;

            public TransformCache(Transform t)
            {
                transform = t;
                position = t.position;
                rotation = t.rotation;
                localScale = t.localScale;
            }

            /// <summary>
            /// Update cached values from transform
            /// </summary>
            public void Refresh()
            {
                if (transform != null)
                {
                    position = transform.position;
                    rotation = transform.rotation;
                    localScale = transform.localScale;
                }
            }

            /// <summary>
            /// Apply cached values to transform
            /// </summary>
            public void Apply()
            {
                if (transform != null)
                {
                    transform.SetPositionAndRotation(position, rotation);
                    transform.localScale = localScale;
                }
            }

            /// <summary>
            /// Apply only position
            /// </summary>
            public void ApplyPosition()
            {
                if (transform != null)
                {
                    transform.position = position;
                }
            }
        }

        #endregion

        #region Material Property Block Caching

        private static MaterialPropertyBlock _cachedPropertyBlock;

        /// <summary>
        /// Get a cached MaterialPropertyBlock to avoid allocations
        /// Use instead of accessing renderer.material which creates instances
        /// </summary>
        public static MaterialPropertyBlock GetPropertyBlock()
        {
            if (_cachedPropertyBlock == null)
            {
                _cachedPropertyBlock = new MaterialPropertyBlock();
            }
            return _cachedPropertyBlock;
        }

        /// <summary>
        /// Set a color on a renderer using MaterialPropertyBlock (no material instance created)
        /// </summary>
        public static void SetRendererColor(Renderer renderer, Color color, string propertyName = "_Color")
        {
            var block = GetPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(propertyName, color);
            renderer.SetPropertyBlock(block);
        }

        /// <summary>
        /// Set a float on a renderer using MaterialPropertyBlock
        /// </summary>
        public static void SetRendererFloat(Renderer renderer, float value, string propertyName)
        {
            var block = GetPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetFloat(propertyName, value);
            renderer.SetPropertyBlock(block);
        }

        #endregion

        #region Layer Mask Caching

        private static readonly Dictionary<string, int> _layerMaskCache = new();

        /// <summary>
        /// Get cached layer mask value to avoid LayerMask.NameToLayer calls
        /// </summary>
        public static int GetLayerMask(string layerName)
        {
            if (!_layerMaskCache.TryGetValue(layerName, out var layerMask))
            {
                layerMask = LayerMask.NameToLayer(layerName);
                _layerMaskCache[layerName] = layerMask;
            }
            return layerMask;
        }

        #endregion

        #region Color Caching

        // Pre-allocated common colors to avoid allocations
        public static readonly Color Red = Color.red;
        public static readonly Color Green = Color.green;
        public static readonly Color Blue = Color.blue;
        public static readonly Color Yellow = Color.yellow;
        public static readonly Color White = Color.white;
        public static readonly Color Black = Color.black;
        public static readonly Color Clear = Color.clear;

        #endregion

        #region Cleanup

        /// <summary>
        /// Clear all caches (call on scene unload or when needed)
        /// </summary>
        public static void ClearAllCaches()
        {
            _cachedMainCamera = null;
            _cachedWaits.Clear();
            _stringBuilderPool.Clear();
            _cachedPropertyBlock = null;
            _layerMaskCache.Clear();
        }

        #endregion
    }

    /// <summary>
    /// MonoBehaviour extension methods for performance optimization
    /// </summary>
    public static class PerformanceExtensions
    {
        /// <summary>
        /// Cached GetComponent that stores result for repeated access
        /// </summary>
        public static T GetCachedComponent<T>(this GameObject gameObject, ref T cachedComponent) where T : Component
        {
            if (cachedComponent == null)
            {
                cachedComponent = gameObject.GetComponent<T>();
            }
            return cachedComponent;
        }

        /// <summary>
        /// Cached transform property
        /// </summary>
        public static Transform GetCachedTransform(this Component component, ref Transform cachedTransform)
        {
            if (cachedTransform == null)
            {
                cachedTransform = component.transform;
            }
            return cachedTransform;
        }
    }
}
