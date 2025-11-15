using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Tools
{
    /// <summary>
    /// Information about a tracked asset including dependencies and usage statistics.
    /// </summary>
    [Serializable]
    public class AssetInfo
    {
        public string AssetPath { get; set; }
        public string AssetGuid { get; set; }
        public Type AssetType { get; set; }
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public List<string> Dependencies { get; set; } = new List<string>();
        public List<string> Dependents { get; set; } = new List<string>();
        public int ReferenceCount { get; set; }
        public bool IsLoaded { get; set; }
        public long MemoryUsage { get; set; }
    }

    /// <summary>
    /// Sorting modes for asset listings.
    /// </summary>
    public enum AssetSortMode
    {
        Path,
        Type,
        Size,
        LastModified,
        ReferenceCount,
        MemoryUsage
    }

    /// <summary>
    /// Statistics about the asset management system.
    /// </summary>
    [Serializable]
    public class AssetManagementStats
    {
        public int TotalAssets { get; set; }
        public int LoadedAssets { get; set; }
        public long TotalFileSize { get; set; }
        public long TotalMemoryUsage { get; set; }
        public int OrphanedAssets { get; set; }
        public int DuplicateAssets { get; set; }
        public DateTime LastRefresh { get; set; }
    }
}