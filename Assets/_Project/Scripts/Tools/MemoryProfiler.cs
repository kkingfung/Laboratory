using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Laboratory.Tools
{
    /// <summary>
    /// Advanced memory profiling system with leak detection and allocation tracking.
    /// Provides detailed memory analysis, snapshot comparison, and automatic leak detection.
    /// Complements Unity Profiler with game-specific memory tracking.
    /// </summary>
    public class MemoryProfiler : MonoBehaviour
    {
        #region Configuration

        [Header("Profiling Settings")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private bool enableLeakDetection = true;
        [SerializeField] private float snapshotInterval = 5f;
        [SerializeField] private int maxSnapshots = 20;
        [SerializeField] private bool logMemoryWarnings = true;

        [Header("Leak Detection")]
        [SerializeField] private float leakThresholdMB = 10f;
        [SerializeField] private int minSnapshotsForLeakDetection = 3;
        [SerializeField] private float leakGrowthRateThreshold = 1.5f; // MB per snapshot

        [Header("Memory Thresholds")]
        [SerializeField] private long warningThresholdMB = 500;
        [SerializeField] private long criticalThresholdMB = 800;

        #endregion

        #region Private Fields

        private static MemoryProfiler _instance;
        private readonly List<MemorySnapshot> _snapshots = new List<MemorySnapshot>();
        private readonly Dictionary<string, AllocationTracker> _allocationTrackers = new Dictionary<string, AllocationTracker>();
        private readonly List<MemoryLeak> _detectedLeaks = new List<MemoryLeak>();

        private float _lastSnapshotTime;
        private MemorySnapshot _currentSnapshot;
        private MemorySnapshot _previousSnapshot;

        // Unity-specific memory trackers
        private ProfilerRecorder _totalReservedMemoryRecorder;
        private ProfilerRecorder _totalUsedMemoryRecorder;
        private ProfilerRecorder _gcReservedMemoryRecorder;
        private ProfilerRecorder _gcUsedMemoryRecorder;
        private ProfilerRecorder _gcAllocationFrameRecorder;
        private ProfilerRecorder _textureMemoryRecorder;
        private ProfilerRecorder _meshMemoryRecorder;
        private ProfilerRecorder _audioMemoryRecorder;

        // Statistics
        private long _totalAllocations;
        private long _totalDeallocations;
        private long _peakMemoryUsage;
        private int _leaksDetected;

        #endregion

        #region Properties

        public static MemoryProfiler Instance => _instance;
        public bool IsProfilingEnabled => enableProfiling;
        public int SnapshotCount => _snapshots.Count;
        public int DetectedLeaksCount => _detectedLeaks.Count;
        public long CurrentMemoryMB => GC.GetTotalMemory(false) / 1024 / 1024;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!enableProfiling) return;

            // Take periodic snapshots
            if (Time.unscaledTime - _lastSnapshotTime >= snapshotInterval)
            {
                TakeSnapshot();
                _lastSnapshotTime = Time.unscaledTime;
            }

            // Check memory thresholds
            CheckMemoryThresholds();
        }

        private void OnDestroy()
        {
            DisposeRecorders();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[MemoryProfiler] Initializing...");

            // Initialize ProfilerRecorders
            _totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
            _totalUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            _gcReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            _gcUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
            _gcAllocationFrameRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocation In Frame");
            _textureMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory");
            _meshMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Mesh Memory");
            _audioMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Audio Memory");

            // Take initial snapshot
            TakeSnapshot();

            Debug.Log("[MemoryProfiler] Initialized");
        }

        private void DisposeRecorders()
        {
            _totalReservedMemoryRecorder.Dispose();
            _totalUsedMemoryRecorder.Dispose();
            _gcReservedMemoryRecorder.Dispose();
            _gcUsedMemoryRecorder.Dispose();
            _gcAllocationFrameRecorder.Dispose();
            _textureMemoryRecorder.Dispose();
            _meshMemoryRecorder.Dispose();
            _audioMemoryRecorder.Dispose();
        }

        #endregion

        #region Snapshot Management

        /// <summary>
        /// Take a memory snapshot for analysis.
        /// </summary>
        public MemorySnapshot TakeSnapshot()
        {
            _previousSnapshot = _currentSnapshot;

            _currentSnapshot = new MemorySnapshot
            {
                timestamp = DateTime.UtcNow,
                frameCount = Time.frameCount,

                // Unity memory
                totalReservedMemory = GetRecorderValue(_totalReservedMemoryRecorder),
                totalUsedMemory = GetRecorderValue(_totalUsedMemoryRecorder),
                gcReservedMemory = GetRecorderValue(_gcReservedMemoryRecorder),
                gcUsedMemory = GetRecorderValue(_gcUsedMemoryRecorder),
                gcAllocationFrame = GetRecorderValue(_gcAllocationFrameRecorder),

                // Asset memory
                textureMemory = GetRecorderValue(_textureMemoryRecorder),
                meshMemory = GetRecorderValue(_meshMemoryRecorder),
                audioMemory = GetRecorderValue(_audioMemoryRecorder),

                // Managed memory
                managedHeapSize = GC.GetTotalMemory(false),
                monoHeapSize = Profiler.GetMonoHeapSizeLong(),
                monoUsedSize = Profiler.GetMonoUsedSizeLong(),

                // Unity objects
                totalObjectCount = FindObjectsOfType<UnityEngine.Object>().Length,
                gameObjectCount = FindObjectsOfType<GameObject>().Length
            };

            // Calculate deltas if we have a previous snapshot
            if (_previousSnapshot != null)
            {
                _currentSnapshot.totalMemoryDelta = _currentSnapshot.totalUsedMemory - _previousSnapshot.totalUsedMemory;
                _currentSnapshot.gcMemoryDelta = _currentSnapshot.gcUsedMemory - _previousSnapshot.gcUsedMemory;
                _currentSnapshot.managedMemoryDelta = _currentSnapshot.managedHeapSize - _previousSnapshot.managedHeapSize;
            }

            // Add to history
            _snapshots.Add(_currentSnapshot);

            // Trim old snapshots
            while (_snapshots.Count > maxSnapshots)
            {
                _snapshots.RemoveAt(0);
            }

            // Update peak memory
            if (_currentSnapshot.totalUsedMemory > _peakMemoryUsage)
            {
                _peakMemoryUsage = _currentSnapshot.totalUsedMemory;
            }

            // Detect leaks
            if (enableLeakDetection && _snapshots.Count >= minSnapshotsForLeakDetection)
            {
                DetectMemoryLeaks();
            }

            return _currentSnapshot;
        }

        /// <summary>
        /// Get a specific snapshot by index.
        /// </summary>
        public MemorySnapshot GetSnapshot(int index)
        {
            if (index < 0 || index >= _snapshots.Count)
                return null;

            return _snapshots[index];
        }

        /// <summary>
        /// Get all snapshots.
        /// </summary>
        public List<MemorySnapshot> GetAllSnapshots()
        {
            return new List<MemorySnapshot>(_snapshots);
        }

        /// <summary>
        /// Clear all snapshots.
        /// </summary>
        public void ClearSnapshots()
        {
            _snapshots.Clear();
            _currentSnapshot = null;
            _previousSnapshot = null;
            Debug.Log("[MemoryProfiler] Snapshots cleared");
        }

        #endregion

        #region Leak Detection

        private void DetectMemoryLeaks()
        {
            if (_snapshots.Count < minSnapshotsForLeakDetection) return;

            // Analyze recent snapshots for consistent growth
            var recentSnapshots = _snapshots.Skip(_snapshots.Count - minSnapshotsForLeakDetection).ToList();

            // Check total memory growth
            CheckForLeak("Total Memory", recentSnapshots, s => s.totalUsedMemory);

            // Check GC memory growth
            CheckForLeak("GC Memory", recentSnapshots, s => s.gcUsedMemory);

            // Check managed heap growth
            CheckForLeak("Managed Heap", recentSnapshots, s => s.managedHeapSize);

            // Check texture memory growth
            CheckForLeak("Texture Memory", recentSnapshots, s => s.textureMemory);

            // Check mesh memory growth
            CheckForLeak("Mesh Memory", recentSnapshots, s => s.meshMemory);

            // Check GameObject count growth (potential object leak)
            CheckForLeak("GameObject Count", recentSnapshots, s => s.gameObjectCount, isObjectCount: true);
        }

        private void CheckForLeak(string category, List<MemorySnapshot> snapshots, Func<MemorySnapshot, long> valueSelector, bool isObjectCount = false)
        {
            if (snapshots.Count < 2) return;

            // Calculate growth rate
            long firstValue = valueSelector(snapshots.First());
            long lastValue = valueSelector(snapshots.Last());
            long growth = lastValue - firstValue;

            // Convert to MB for memory, keep as count for objects
            float growthValue = isObjectCount ? growth : growth / 1024f / 1024f;
            float timeSpan = (float)(snapshots.Last().timestamp - snapshots.First().timestamp).TotalSeconds;
            float growthRate = timeSpan > 0 ? growthValue / timeSpan : 0;

            // Check if growth exceeds threshold
            float threshold = isObjectCount ? 100 : leakGrowthRateThreshold; // 100 objects/sec for object count
            if (Math.Abs(growthRate) > threshold)
            {
                // Check if this is consistent growth (not just a spike)
                bool consistentGrowth = true;
                for (int i = 1; i < snapshots.Count; i++)
                {
                    long prevValue = valueSelector(snapshots[i - 1]);
                    long currValue = valueSelector(snapshots[i]);
                    if (currValue < prevValue)
                    {
                        consistentGrowth = false;
                        break;
                    }
                }

                if (consistentGrowth)
                {
                    // Potential leak detected
                    var leak = new MemoryLeak
                    {
                        category = category,
                        detectedAt = DateTime.UtcNow,
                        initialValue = firstValue,
                        currentValue = lastValue,
                        growthRate = growthRate,
                        snapshotCount = snapshots.Count,
                        isObjectCountLeak = isObjectCount
                    };

                    // Check if we already reported this leak recently
                    var existingLeak = _detectedLeaks.FirstOrDefault(l =>
                        l.category == category &&
                        (DateTime.UtcNow - l.detectedAt).TotalSeconds < snapshotInterval * minSnapshotsForLeakDetection);

                    if (existingLeak == null)
                    {
                        _detectedLeaks.Add(leak);
                        _leaksDetected++;

                        if (logMemoryWarnings)
                        {
                            string unit = isObjectCount ? "objects/sec" : "MB/sec";
                            Debug.LogWarning($"[MemoryProfiler] POTENTIAL MEMORY LEAK DETECTED\n" +
                                           $"Category: {category}\n" +
                                           $"Growth: {growthValue:F2} {(isObjectCount ? "objects" : "MB")} over {timeSpan:F1}s\n" +
                                           $"Growth Rate: {growthRate:F2} {unit}\n" +
                                           $"Initial: {FormatMemoryOrCount(firstValue, isObjectCount)}\n" +
                                           $"Current: {FormatMemoryOrCount(lastValue, isObjectCount)}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get all detected memory leaks.
        /// </summary>
        public List<MemoryLeak> GetDetectedLeaks()
        {
            return new List<MemoryLeak>(_detectedLeaks);
        }

        /// <summary>
        /// Clear detected leaks.
        /// </summary>
        public void ClearDetectedLeaks()
        {
            _detectedLeaks.Clear();
            Debug.Log("[MemoryProfiler] Detected leaks cleared");
        }

        #endregion

        #region Allocation Tracking

        /// <summary>
        /// Start tracking allocations for a specific category.
        /// </summary>
        public void StartTrackingAllocations(string category)
        {
            if (!_allocationTrackers.ContainsKey(category))
            {
                _allocationTrackers[category] = new AllocationTracker
                {
                    category = category,
                    startTime = DateTime.UtcNow,
                    startMemory = GC.GetTotalMemory(false)
                };

                Debug.Log($"[MemoryProfiler] Started tracking allocations: {category}");
            }
        }

        /// <summary>
        /// Stop tracking allocations and get results.
        /// </summary>
        public AllocationTracker StopTrackingAllocations(string category)
        {
            if (_allocationTrackers.TryGetValue(category, out var tracker))
            {
                tracker.endTime = DateTime.UtcNow;
                tracker.endMemory = GC.GetTotalMemory(false);
                tracker.totalAllocated = tracker.endMemory - tracker.startMemory;
                tracker.duration = (float)(tracker.endTime - tracker.startTime).TotalSeconds;

                _allocationTrackers.Remove(category);

                _totalAllocations += tracker.totalAllocated;

                Debug.Log($"[MemoryProfiler] Stopped tracking allocations: {category}\n" +
                         $"Duration: {tracker.duration:F2}s\n" +
                         $"Allocated: {tracker.totalAllocated / 1024f / 1024f:F2} MB\n" +
                         $"Rate: {(tracker.totalAllocated / tracker.duration) / 1024f / 1024f:F2} MB/s");

                return tracker;
            }

            return null;
        }

        #endregion

        #region Memory Thresholds

        private void CheckMemoryThresholds()
        {
            long currentMemory = CurrentMemoryMB;

            if (currentMemory >= criticalThresholdMB)
            {
                if (logMemoryWarnings)
                {
                    Debug.LogError($"[MemoryProfiler] CRITICAL: Memory usage at {currentMemory} MB (threshold: {criticalThresholdMB} MB)");
                }
            }
            else if (currentMemory >= warningThresholdMB)
            {
                if (logMemoryWarnings)
                {
                    Debug.LogWarning($"[MemoryProfiler] WARNING: Memory usage at {currentMemory} MB (threshold: {warningThresholdMB} MB)");
                }
            }
        }

        #endregion

        #region Analysis

        /// <summary>
        /// Compare two snapshots and get the differences.
        /// </summary>
        public MemoryDiff CompareSnapshots(MemorySnapshot snapshot1, MemorySnapshot snapshot2)
        {
            if (snapshot1 == null || snapshot2 == null)
                return null;

            return new MemoryDiff
            {
                snapshot1 = snapshot1,
                snapshot2 = snapshot2,
                totalMemoryChange = snapshot2.totalUsedMemory - snapshot1.totalUsedMemory,
                gcMemoryChange = snapshot2.gcUsedMemory - snapshot1.gcUsedMemory,
                managedMemoryChange = snapshot2.managedHeapSize - snapshot1.managedHeapSize,
                textureMemoryChange = snapshot2.textureMemory - snapshot1.textureMemory,
                meshMemoryChange = snapshot2.meshMemory - snapshot1.meshMemory,
                audioMemoryChange = snapshot2.audioMemory - snapshot1.audioMemory,
                gameObjectCountChange = snapshot2.gameObjectCount - snapshot1.gameObjectCount,
                timeSpan = (float)(snapshot2.timestamp - snapshot1.timestamp).TotalSeconds
            };
        }

        /// <summary>
        /// Generate a detailed memory report.
        /// </summary>
        public string GenerateMemoryReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== MEMORY PROFILER REPORT ===");
            sb.AppendLine($"Generated: {DateTime.UtcNow}");
            sb.AppendLine();

            if (_currentSnapshot != null)
            {
                sb.AppendLine("Current Memory Status:");
                sb.AppendLine($"  Total Reserved: {FormatBytes(_currentSnapshot.totalReservedMemory)}");
                sb.AppendLine($"  Total Used: {FormatBytes(_currentSnapshot.totalUsedMemory)}");
                sb.AppendLine($"  GC Reserved: {FormatBytes(_currentSnapshot.gcReservedMemory)}");
                sb.AppendLine($"  GC Used: {FormatBytes(_currentSnapshot.gcUsedMemory)}");
                sb.AppendLine($"  Managed Heap: {FormatBytes(_currentSnapshot.managedHeapSize)}");
                sb.AppendLine($"  Mono Heap: {FormatBytes(_currentSnapshot.monoHeapSize)}");
                sb.AppendLine($"  Mono Used: {FormatBytes(_currentSnapshot.monoUsedSize)}");
                sb.AppendLine();

                sb.AppendLine("Asset Memory:");
                sb.AppendLine($"  Textures: {FormatBytes(_currentSnapshot.textureMemory)}");
                sb.AppendLine($"  Meshes: {FormatBytes(_currentSnapshot.meshMemory)}");
                sb.AppendLine($"  Audio: {FormatBytes(_currentSnapshot.audioMemory)}");
                sb.AppendLine();

                sb.AppendLine("Object Counts:");
                sb.AppendLine($"  Total Objects: {_currentSnapshot.totalObjectCount}");
                sb.AppendLine($"  GameObjects: {_currentSnapshot.gameObjectCount}");
                sb.AppendLine();
            }

            sb.AppendLine("Statistics:");
            sb.AppendLine($"  Peak Memory: {FormatBytes(_peakMemoryUsage)}");
            sb.AppendLine($"  Snapshots Taken: {_snapshots.Count}");
            sb.AppendLine($"  Leaks Detected: {_leaksDetected}");
            sb.AppendLine($"  Total Allocations Tracked: {FormatBytes(_totalAllocations)}");
            sb.AppendLine();

            if (_detectedLeaks.Count > 0)
            {
                sb.AppendLine("Detected Leaks:");
                foreach (var leak in _detectedLeaks)
                {
                    sb.AppendLine($"  [{leak.category}]");
                    sb.AppendLine($"    Detected: {leak.detectedAt}");
                    sb.AppendLine($"    Growth: {FormatMemoryOrCount(leak.currentValue - leak.initialValue, leak.isObjectCountLeak)}");
                    sb.AppendLine($"    Rate: {leak.growthRate:F2} {(leak.isObjectCountLeak ? "objects/sec" : "MB/sec")}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion

        #region Helper Methods

        private long GetRecorderValue(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue : 0;
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F2} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / 1024f / 1024f:F2} MB";
            else
                return $"{bytes / 1024f / 1024f / 1024f:F2} GB";
        }

        private string FormatMemoryOrCount(long value, bool isCount)
        {
            return isCount ? $"{value} objects" : FormatBytes(value);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current memory profiling statistics.
        /// </summary>
        public MemoryProfilerStats GetStats()
        {
            return new MemoryProfilerStats
            {
                currentMemoryMB = CurrentMemoryMB,
                peakMemoryMB = _peakMemoryUsage / 1024 / 1024,
                snapshotCount = _snapshots.Count,
                detectedLeaksCount = _detectedLeaks.Count,
                totalAllocationsTracked = _totalAllocations,
                isProfilingEnabled = enableProfiling,
                isLeakDetectionEnabled = enableLeakDetection
            };
        }

        /// <summary>
        /// Force a garbage collection and take a snapshot.
        /// </summary>
        public MemorySnapshot ForceGCAndSnapshot()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return TakeSnapshot();
        }

        #endregion

        #region Context Menu

        [ContextMenu("Take Snapshot")]
        private void TakeSnapshotMenu()
        {
            TakeSnapshot();
        }

        [ContextMenu("Force GC and Snapshot")]
        private void ForceGCAndSnapshotMenu()
        {
            ForceGCAndSnapshot();
        }

        [ContextMenu("Print Memory Report")]
        private void PrintMemoryReportMenu()
        {
            Debug.Log(GenerateMemoryReport());
        }

        [ContextMenu("Clear All Data")]
        private void ClearAllDataMenu()
        {
            ClearSnapshots();
            ClearDetectedLeaks();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// A snapshot of memory state at a specific point in time.
    /// </summary>
    [Serializable]
    public class MemorySnapshot
    {
        public DateTime timestamp;
        public int frameCount;

        // Unity memory
        public long totalReservedMemory;
        public long totalUsedMemory;
        public long gcReservedMemory;
        public long gcUsedMemory;
        public long gcAllocationFrame;

        // Asset memory
        public long textureMemory;
        public long meshMemory;
        public long audioMemory;

        // Managed memory
        public long managedHeapSize;
        public long monoHeapSize;
        public long monoUsedSize;

        // Object counts
        public int totalObjectCount;
        public int gameObjectCount;

        // Deltas (from previous snapshot)
        public long totalMemoryDelta;
        public long gcMemoryDelta;
        public long managedMemoryDelta;
    }

    /// <summary>
    /// Information about a detected memory leak.
    /// </summary>
    [Serializable]
    public class MemoryLeak
    {
        public string category;
        public DateTime detectedAt;
        public long initialValue;
        public long currentValue;
        public float growthRate;
        public int snapshotCount;
        public bool isObjectCountLeak;
    }

    /// <summary>
    /// Tracks allocations for a specific category or operation.
    /// </summary>
    [Serializable]
    public class AllocationTracker
    {
        public string category;
        public DateTime startTime;
        public DateTime endTime;
        public long startMemory;
        public long endMemory;
        public long totalAllocated;
        public float duration;
    }

    /// <summary>
    /// Comparison between two memory snapshots.
    /// </summary>
    [Serializable]
    public class MemoryDiff
    {
        public MemorySnapshot snapshot1;
        public MemorySnapshot snapshot2;
        public long totalMemoryChange;
        public long gcMemoryChange;
        public long managedMemoryChange;
        public long textureMemoryChange;
        public long meshMemoryChange;
        public long audioMemoryChange;
        public int gameObjectCountChange;
        public float timeSpan;
    }

    /// <summary>
    /// Memory profiler statistics.
    /// </summary>
    [Serializable]
    public struct MemoryProfilerStats
    {
        public long currentMemoryMB;
        public long peakMemoryMB;
        public int snapshotCount;
        public int detectedLeaksCount;
        public long totalAllocationsTracked;
        public bool isProfilingEnabled;
        public bool isLeakDetectionEnabled;
    }

    #endregion
}
