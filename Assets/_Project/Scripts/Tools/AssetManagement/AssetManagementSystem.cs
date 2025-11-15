using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Tools
{
    /// <summary>
    /// System for tracking and managing project assets including dependencies and memory usage.
    /// </summary>
    public class AssetManagementSystem
    {
        #region Singleton

        private static AssetManagementSystem _instance;
        public static AssetManagementSystem Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AssetManagementSystem();
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly Dictionary<string, AssetInfo> _trackedAssets = new Dictionary<string, AssetInfo>();
        private AssetManagementStats _cachedStats;
        private DateTime _lastStatsUpdate = DateTime.MinValue;
        private readonly TimeSpan _statsUpdateInterval = TimeSpan.FromSeconds(1);

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets information about a specific asset.
        /// </summary>
        public AssetInfo GetAssetInfo(string assetPath)
        {
            _trackedAssets.TryGetValue(assetPath, out var info);
            return info;
        }

        /// <summary>
        /// Gets all tracked assets.
        /// </summary>
        public IEnumerable<AssetInfo> GetAllAssets()
        {
            return _trackedAssets.Values;
        }

        /// <summary>
        /// Gets assets filtered by type.
        /// </summary>
        public IEnumerable<AssetInfo> GetAssetsByType(Type type)
        {
            return _trackedAssets.Values.Where(a => a.AssetType == type);
        }

        /// <summary>
        /// Gets assets that depend on the specified asset.
        /// </summary>
        public IEnumerable<AssetInfo> GetDependents(string assetPath)
        {
            return _trackedAssets.Values.Where(a => a.Dependencies.Contains(assetPath));
        }

        /// <summary>
        /// Refreshes asset tracking information.
        /// </summary>
        public void RefreshAssets()
        {
            // Implementation would scan project for assets and update tracking info
            Debug.Log("AssetManagementSystem: Refreshing asset tracking...");

            // For now, create some dummy data
            _trackedAssets.Clear();

            // This would be replaced with actual asset scanning logic
            var dummyAsset = new AssetInfo
            {
                AssetPath = "Assets/Example.prefab",
                AssetGuid = System.Guid.NewGuid().ToString(),
                AssetType = typeof(GameObject),
                FileSize = 1024,
                LastModified = DateTime.Now,
                IsLoaded = true,
                MemoryUsage = 512,
                ReferenceCount = 3
            };

            _trackedAssets[dummyAsset.AssetPath] = dummyAsset;

            InvalidateStats();
        }

        /// <summary>
        /// Gets current system statistics.
        /// </summary>
        public AssetManagementStats GetStats()
        {
            if (_cachedStats == null || DateTime.Now - _lastStatsUpdate > _statsUpdateInterval)
            {
                UpdateStats();
            }

            return _cachedStats;
        }

        /// <summary>
        /// Reloads a specific asset.
        /// </summary>
        public void ReloadAsset(string assetPath)
        {
            Debug.Log($"AssetManagementSystem: Reloading asset {assetPath}");

            if (_trackedAssets.TryGetValue(assetPath, out var asset))
            {
                asset.LastModified = DateTime.Now;
                InvalidateStats();
            }
        }

        #endregion

        #region Private Methods

        private void UpdateStats()
        {
            var assets = _trackedAssets.Values;

            _cachedStats = new AssetManagementStats
            {
                TotalAssets = assets.Count(),
                LoadedAssets = assets.Count(a => a.IsLoaded),
                TotalFileSize = assets.Sum(a => a.FileSize),
                TotalMemoryUsage = assets.Sum(a => a.MemoryUsage),
                OrphanedAssets = assets.Count(a => a.ReferenceCount == 0),
                DuplicateAssets = 0, // Would need analysis to detect duplicates
                LastRefresh = DateTime.Now
            };

            _lastStatsUpdate = DateTime.Now;
        }

        private void InvalidateStats()
        {
            _lastStatsUpdate = DateTime.MinValue;
        }

        #endregion
    }
}