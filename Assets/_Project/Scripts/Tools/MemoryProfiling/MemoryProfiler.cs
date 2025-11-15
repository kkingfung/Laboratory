using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Tools
{
    /// <summary>
    /// Memory snapshot containing allocation information at a specific point in time.
    /// </summary>
    [Serializable]
    public class MemorySnapshot
    {
        public DateTime Timestamp { get; set; }
        public long TotalMemory { get; set; }
        public long GCMemory { get; set; }
        public long ManagedMemory { get; set; }
        public long TextureMemory { get; set; }
        public long MeshMemory { get; set; }
        public long AudioMemory { get; set; }
        public Dictionary<string, long> CategoryBreakdown { get; set; } = new Dictionary<string, long>();
    }

    /// <summary>
    /// Information about a detected memory leak.
    /// </summary>
    [Serializable]
    public class MemoryLeak
    {
        public string ObjectType { get; set; }
        public int InstanceCount { get; set; }
        public long TotalSize { get; set; }
        public float GrowthRate { get; set; }
        public DateTime FirstDetected { get; set; }
        public DateTime LastUpdated { get; set; }
        public string StackTrace { get; set; }
    }

    /// <summary>
    /// Difference between two memory snapshots.
    /// </summary>
    [Serializable]
    public class MemoryDiff
    {
        public MemorySnapshot Before { get; set; }
        public MemorySnapshot After { get; set; }
        public long TotalDifference { get; set; }
        public long GCDifference { get; set; }
        public long ManagedDifference { get; set; }
        public Dictionary<string, long> CategoryDifferences { get; set; } = new Dictionary<string, long>();
    }

    /// <summary>
    /// Statistics about memory profiling system.
    /// </summary>
    [Serializable]
    public class MemoryProfilerStats
    {
        public int TotalSnapshots { get; set; }
        public int DetectedLeaks { get; set; }
        public long AverageMemoryUsage { get; set; }
        public long PeakMemoryUsage { get; set; }
        public float AllocationRate { get; set; }
        public DateTime LastSnapshot { get; set; }
    }

    /// <summary>
    /// Memory profiler for tracking allocations, detecting leaks, and analyzing usage patterns.
    /// </summary>
    public class MemoryProfiler
    {
        #region Singleton

        private static MemoryProfiler _instance;
        public static MemoryProfiler Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MemoryProfiler();
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly List<MemorySnapshot> _snapshots = new List<MemorySnapshot>();
        private readonly List<MemoryLeak> _detectedLeaks = new List<MemoryLeak>();
        private readonly Dictionary<string, List<long>> _allocationTrackers = new Dictionary<string, List<long>>();
        private MemoryProfilerStats _cachedStats;
        private DateTime _lastStatsUpdate = DateTime.MinValue;
        private readonly TimeSpan _statsUpdateInterval = TimeSpan.FromSeconds(1);
        private bool _isTracking;

        #endregion

        #region Public Methods

        /// <summary>
        /// Takes a memory snapshot.
        /// </summary>
        public MemorySnapshot TakeSnapshot()
        {
            var snapshot = new MemorySnapshot
            {
                Timestamp = DateTime.Now,
                TotalMemory = GC.GetTotalMemory(false),
                GCMemory = GC.GetTotalMemory(true),
                ManagedMemory = System.GC.GetTotalMemory(false),
                TextureMemory = UnityEngine.Profiling.Profiler.GetAllocatedMemory("Texture2D"),
                MeshMemory = UnityEngine.Profiling.Profiler.GetAllocatedMemory("Mesh"),
                AudioMemory = UnityEngine.Profiling.Profiler.GetAllocatedMemory("AudioClip")
            };

            // Add category breakdown
            snapshot.CategoryBreakdown["Managed"] = snapshot.ManagedMemory;
            snapshot.CategoryBreakdown["Texture"] = snapshot.TextureMemory;
            snapshot.CategoryBreakdown["Mesh"] = snapshot.MeshMemory;
            snapshot.CategoryBreakdown["Audio"] = snapshot.AudioMemory;

            _snapshots.Add(snapshot);
            InvalidateStats();

            return snapshot;
        }

        /// <summary>
        /// Gets all memory snapshots.
        /// </summary>
        public IEnumerable<MemorySnapshot> GetSnapshots()
        {
            return _snapshots;
        }

        /// <summary>
        /// Compares two memory snapshots.
        /// </summary>
        public MemoryDiff CompareSnapshots(MemorySnapshot before, MemorySnapshot after)
        {
            var diff = new MemoryDiff
            {
                Before = before,
                After = after,
                TotalDifference = after.TotalMemory - before.TotalMemory,
                GCDifference = after.GCMemory - before.GCMemory,
                ManagedDifference = after.ManagedMemory - before.ManagedMemory
            };

            // Calculate category differences
            foreach (var category in before.CategoryBreakdown.Keys.Union(after.CategoryBreakdown.Keys))
            {
                var beforeValue = before.CategoryBreakdown.GetValueOrDefault(category, 0);
                var afterValue = after.CategoryBreakdown.GetValueOrDefault(category, 0);
                diff.CategoryDifferences[category] = afterValue - beforeValue;
            }

            return diff;
        }

        /// <summary>
        /// Gets detected memory leaks.
        /// </summary>
        public IEnumerable<MemoryLeak> GetDetectedLeaks()
        {
            return _detectedLeaks;
        }

        /// <summary>
        /// Starts tracking allocations for a specific category.
        /// </summary>
        public void StartTracking(string trackerName)
        {
            if (!_allocationTrackers.ContainsKey(trackerName))
            {
                _allocationTrackers[trackerName] = new List<long>();
            }
            _isTracking = true;
        }

        /// <summary>
        /// Stops tracking allocations.
        /// </summary>
        public void StopTracking()
        {
            _isTracking = false;
        }

        /// <summary>
        /// Gets profiler statistics.
        /// </summary>
        public MemoryProfilerStats GetStats()
        {
            if (_cachedStats == null || DateTime.Now - _lastStatsUpdate > _statsUpdateInterval)
            {
                UpdateStats();
            }

            return _cachedStats;
        }

        /// <summary>
        /// Clears all snapshots and tracking data.
        /// </summary>
        public void Clear()
        {
            _snapshots.Clear();
            _detectedLeaks.Clear();
            _allocationTrackers.Clear();
            InvalidateStats();
        }

        #endregion

        #region Private Methods

        private void UpdateStats()
        {
            _cachedStats = new MemoryProfilerStats
            {
                TotalSnapshots = _snapshots.Count,
                DetectedLeaks = _detectedLeaks.Count,
                LastSnapshot = _snapshots.LastOrDefault()?.Timestamp ?? DateTime.MinValue
            };

            if (_snapshots.Count > 0)
            {
                _cachedStats.AverageMemoryUsage = (long)_snapshots.Average(s => s.TotalMemory);
                _cachedStats.PeakMemoryUsage = _snapshots.Max(s => s.TotalMemory);
            }

            _lastStatsUpdate = DateTime.Now;
        }

        private void InvalidateStats()
        {
            _lastStatsUpdate = DateTime.MinValue;
        }

        #endregion
    }
}