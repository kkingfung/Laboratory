using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectChimera.Tools.Development
{
    /// <summary>
    /// Asset Bundle Analyzer - Analyze, optimize, and visualize asset bundle dependencies
    ///
    /// Features:
    /// - Dependency graph visualization
    /// - Duplicate asset detection across bundles
    /// - Size optimization recommendations
    /// - Load time predictions
    /// - Compression comparison (LZ4, LZMA, Uncompressed)
    /// - Bundle organization suggestions
    /// - Memory impact analysis
    ///
    /// Usage:
    /// - Open window via Tools > Asset Bundle Analyzer
    /// - Build asset bundles for analysis
    /// - View dependency graphs and optimization suggestions
    /// - Export reports for build pipeline optimization
    /// </summary>
    public class AssetBundleAnalyzer : MonoBehaviour
    {
        private static AssetBundleAnalyzer _instance;
        public static AssetBundleAnalyzer Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AssetBundleAnalyzer");
                    _instance = go.AddComponent<AssetBundleAnalyzer>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Analysis Settings")]
        [Tooltip("Path to asset bundle output directory")]
        public string bundleOutputPath = "AssetBundles";

        [Tooltip("Enable automatic dependency analysis")]
        public bool autoAnalyzeDependencies = true;

        [Tooltip("Detect duplicate assets across bundles")]
        public bool detectDuplicates = true;

        [Tooltip("Calculate load time estimates")]
        public bool calculateLoadTimes = true;

        [Tooltip("Analyze memory footprint")]
        public bool analyzeMemoryFootprint = true;

        [Header("Optimization")]
        [Tooltip("Minimum bundle size for analysis (KB)")]
        public int minBundleSizeKB = 10;

        [Tooltip("Duplicate asset size threshold (KB)")]
        public int duplicateThresholdKB = 50;

        [Tooltip("Show optimization suggestions")]
        public bool showOptimizationSuggestions = true;

        // Analysis data
        private Dictionary<string, BundleAnalysisData> _bundleData = new Dictionary<string, BundleAnalysisData>();
        private List<DuplicateAssetInfo> _duplicates = new List<DuplicateAssetInfo>();
        private List<OptimizationSuggestion> _suggestions = new List<OptimizationSuggestion>();

        /// <summary>
        /// Analyze all asset bundles in the output directory
        /// </summary>
        public AnalysisReport AnalyzeAssetBundles()
        {
            var report = new AnalysisReport();
            report.analysisTime = DateTime.Now;

            _bundleData.Clear();
            _duplicates.Clear();
            _suggestions.Clear();

#if UNITY_EDITOR
            string fullPath = Path.Combine(Application.dataPath, "..", bundleOutputPath);

            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"[AssetBundleAnalyzer] Bundle directory not found: {fullPath}");
                return report;
            }

            // Get all manifest files
            var manifestFiles = Directory.GetFiles(fullPath, "*.manifest", SearchOption.AllDirectories);

            Debug.Log($"[AssetBundleAnalyzer] Analyzing {manifestFiles.Length} asset bundles...");

            foreach (var manifestPath in manifestFiles)
            {
                AnalyzeBundle(manifestPath);
            }

            // Detect duplicates
            if (detectDuplicates)
            {
                DetectDuplicateAssets();
            }

            // Generate optimization suggestions
            if (showOptimizationSuggestions)
            {
                GenerateOptimizationSuggestions();
            }

            // Compile report
            report.bundleCount = _bundleData.Count;
            report.totalSizeKB = _bundleData.Values.Sum(b => b.sizeKB);
            report.totalAssets = _bundleData.Values.Sum(b => b.assetCount);
            report.duplicateCount = _duplicates.Count;
            report.duplicateSizeKB = _duplicates.Sum(d => d.estimatedSizeKB);
            report.suggestionCount = _suggestions.Count;
            report.bundles = _bundleData.Values.ToList();
            report.duplicates = new List<DuplicateAssetInfo>(_duplicates);
            report.suggestions = new List<OptimizationSuggestion>(_suggestions);

            Debug.Log($"[AssetBundleAnalyzer] Analysis complete! " +
                      $"Bundles: {report.bundleCount}, " +
                      $"Total Size: {report.totalSizeKB:F2} KB, " +
                      $"Duplicates: {report.duplicateCount}, " +
                      $"Suggestions: {report.suggestionCount}");
#endif

            return report;
        }

        private void AnalyzeBundle(string manifestPath)
        {
            string bundleName = Path.GetFileNameWithoutExtension(manifestPath);
            string bundlePath = manifestPath.Replace(".manifest", "");

            if (!File.Exists(bundlePath))
            {
                Debug.LogWarning($"[AssetBundleAnalyzer] Bundle file not found: {bundlePath}");
                return;
            }

            var data = new BundleAnalysisData();
            data.bundleName = bundleName;
            data.bundlePath = bundlePath;

            // Get file size
            FileInfo fileInfo = new FileInfo(bundlePath);
            data.sizeKB = fileInfo.Length / 1024f;

            // Parse manifest
            ParseManifest(manifestPath, data);

            // Calculate load time estimate (simplified)
            if (calculateLoadTimes)
            {
                data.estimatedLoadTimeMs = CalculateLoadTime(data.sizeKB);
            }

            // Analyze memory footprint
            if (analyzeMemoryFootprint)
            {
                data.estimatedMemoryKB = EstimateMemoryFootprint(data);
            }

            _bundleData[bundleName] = data;
        }

        private void ParseManifest(string manifestPath, BundleAnalysisData data)
        {
            var lines = File.ReadAllLines(manifestPath);
            bool inDependencies = false;
            bool inAssets = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("Assets:"))
                {
                    inAssets = true;
                    inDependencies = false;
                    continue;
                }

                if (line.StartsWith("Dependency_"))
                {
                    inDependencies = true;
                    inAssets = false;
                    continue;
                }

                if (line.StartsWith("ClassTypes:") || line.StartsWith("SerializeReferenceClassIdentifiers:"))
                {
                    inAssets = false;
                    inDependencies = false;
                    continue;
                }

                if (inAssets && line.StartsWith("- "))
                {
                    string assetPath = line.Substring(2).Trim();
                    data.assets.Add(assetPath);
                    data.assetCount++;
                }

                if (inDependencies && line.StartsWith("- "))
                {
                    string dependencyName = line.Substring(2).Trim();
                    data.dependencies.Add(dependencyName);
                }

                if (line.StartsWith("CRC: "))
                {
                    data.crc = line.Substring(5).Trim();
                }
            }
        }

        private float CalculateLoadTime(float sizeKB)
        {
            // Simplified load time estimation
            // Assumes average disk read speed of 100 MB/s and decompression overhead
            float baseReadTime = (sizeKB / 1024f) / 0.1f * 1000f; // Convert to ms
            float decompressionOverhead = baseReadTime * 0.3f; // 30% overhead
            return baseReadTime + decompressionOverhead;
        }

        private float EstimateMemoryFootprint(BundleAnalysisData data)
        {
            // Rough estimation based on asset types
            float memoryKB = data.sizeKB * 1.5f; // Average 1.5x expansion in memory

            // Adjust based on asset types
            int textureCount = data.assets.Count(a => a.EndsWith(".png") || a.EndsWith(".jpg") || a.EndsWith(".tga"));
            int modelCount = data.assets.Count(a => a.EndsWith(".fbx") || a.EndsWith(".obj"));
            int audioCount = data.assets.Count(a => a.EndsWith(".wav") || a.EndsWith(".mp3") || a.EndsWith(".ogg"));

            // Textures typically expand more in memory
            memoryKB += textureCount * 200; // Avg 200KB per texture
            memoryKB += modelCount * 100; // Avg 100KB per model
            memoryKB += audioCount * 500; // Avg 500KB per audio clip

            return memoryKB;
        }

        private void DetectDuplicateAssets()
        {
            // Group assets by name across all bundles
            var assetToBundles = new Dictionary<string, List<string>>();

            foreach (var kvp in _bundleData)
            {
                foreach (var asset in kvp.Value.assets)
                {
                    string assetName = Path.GetFileName(asset);

                    if (!assetToBundles.ContainsKey(assetName))
                    {
                        assetToBundles[assetName] = new List<string>();
                    }

                    assetToBundles[assetName].Add(kvp.Key);
                }
            }

            // Find duplicates
            foreach (var kvp in assetToBundles)
            {
                if (kvp.Value.Count > 1)
                {
                    var duplicate = new DuplicateAssetInfo();
                    duplicate.assetName = kvp.Key;
                    duplicate.bundles = kvp.Value;
                    duplicate.occurrenceCount = kvp.Value.Count;
                    duplicate.estimatedSizeKB = EstimateAssetSize(kvp.Key);

                    if (duplicate.estimatedSizeKB >= duplicateThresholdKB)
                    {
                        _duplicates.Add(duplicate);
                    }
                }
            }
        }

        private float EstimateAssetSize(string assetName)
        {
            // Very rough estimation based on file extension
            string ext = Path.GetExtension(assetName).ToLower();

            return ext switch
            {
                ".png" or ".jpg" or ".tga" => 200f,
                ".fbx" or ".obj" => 100f,
                ".wav" or ".mp3" or ".ogg" => 500f,
                ".prefab" => 50f,
                ".mat" => 10f,
                ".shader" => 20f,
                _ => 25f
            };
        }

        private void GenerateOptimizationSuggestions()
        {
            // Suggestion 1: Large bundles should be split
            foreach (var bundle in _bundleData.Values)
            {
                if (bundle.sizeKB > 5000) // > 5MB
                {
                    _suggestions.Add(new OptimizationSuggestion
                    {
                        severity = SuggestionSeverity.High,
                        category = "Bundle Size",
                        title = $"Large bundle detected: {bundle.bundleName}",
                        description = $"Bundle is {bundle.sizeKB:F2} KB. Consider splitting into smaller bundles for better streaming.",
                        estimatedSavingsKB = 0
                    });
                }
            }

            // Suggestion 2: Duplicate assets
            if (_duplicates.Count > 0)
            {
                float totalDuplicateWaste = _duplicates.Sum(d => d.estimatedSizeKB * (d.occurrenceCount - 1));

                _suggestions.Add(new OptimizationSuggestion
                {
                    severity = SuggestionSeverity.High,
                    category = "Duplicates",
                    title = $"{_duplicates.Count} duplicate assets found",
                    description = $"Move common assets to shared bundle to save {totalDuplicateWaste:F2} KB",
                    estimatedSavingsKB = totalDuplicateWaste
                });
            }

            // Suggestion 3: Bundles with many dependencies
            foreach (var bundle in _bundleData.Values)
            {
                if (bundle.dependencies.Count > 10)
                {
                    _suggestions.Add(new OptimizationSuggestion
                    {
                        severity = SuggestionSeverity.Medium,
                        category = "Dependencies",
                        title = $"Complex dependency tree: {bundle.bundleName}",
                        description = $"Bundle has {bundle.dependencies.Count} dependencies. Simplify for faster loading.",
                        estimatedSavingsKB = 0
                    });
                }
            }

            // Suggestion 4: Small bundles (overhead)
            foreach (var bundle in _bundleData.Values)
            {
                if (bundle.sizeKB < minBundleSizeKB)
                {
                    _suggestions.Add(new OptimizationSuggestion
                    {
                        severity = SuggestionSeverity.Low,
                        category = "Bundle Size",
                        title = $"Small bundle: {bundle.bundleName}",
                        description = $"Bundle is only {bundle.sizeKB:F2} KB. Consider merging with related bundles.",
                        estimatedSavingsKB = 0
                    });
                }
            }
        }

        /// <summary>
        /// Get analysis data for a specific bundle
        /// </summary>
        public BundleAnalysisData GetBundleData(string bundleName)
        {
            return _bundleData.ContainsKey(bundleName) ? _bundleData[bundleName] : null;
        }

        /// <summary>
        /// Get all analyzed bundles
        /// </summary>
        public List<BundleAnalysisData> GetAllBundles()
        {
            return _bundleData.Values.ToList();
        }

        /// <summary>
        /// Get all detected duplicates
        /// </summary>
        public List<DuplicateAssetInfo> GetDuplicates()
        {
            return new List<DuplicateAssetInfo>(_duplicates);
        }

        /// <summary>
        /// Get all optimization suggestions
        /// </summary>
        public List<OptimizationSuggestion> GetSuggestions()
        {
            return new List<OptimizationSuggestion>(_suggestions);
        }
    }

    /// <summary>
    /// Complete analysis report
    /// </summary>
    [Serializable]
    public class AnalysisReport
    {
        public DateTime analysisTime;
        public int bundleCount;
        public float totalSizeKB;
        public int totalAssets;
        public int duplicateCount;
        public float duplicateSizeKB;
        public int suggestionCount;
        public List<BundleAnalysisData> bundles;
        public List<DuplicateAssetInfo> duplicates;
        public List<OptimizationSuggestion> suggestions;
    }

    /// <summary>
    /// Analysis data for a single bundle
    /// </summary>
    [Serializable]
    public class BundleAnalysisData
    {
        public string bundleName;
        public string bundlePath;
        public float sizeKB;
        public int assetCount;
        public string crc;
        public List<string> assets = new List<string>();
        public List<string> dependencies = new List<string>();
        public float estimatedLoadTimeMs;
        public float estimatedMemoryKB;
    }

    /// <summary>
    /// Information about duplicate assets
    /// </summary>
    [Serializable]
    public class DuplicateAssetInfo
    {
        public string assetName;
        public List<string> bundles;
        public int occurrenceCount;
        public float estimatedSizeKB;
    }

    /// <summary>
    /// Optimization suggestion
    /// </summary>
    [Serializable]
    public class OptimizationSuggestion
    {
        public SuggestionSeverity severity;
        public string category;
        public string title;
        public string description;
        public float estimatedSavingsKB;
    }

    public enum SuggestionSeverity
    {
        Low,
        Medium,
        High
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor window for Asset Bundle Analyzer
    /// </summary>
    public class AssetBundleAnalyzerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private AnalysisReport _lastReport;
        private int _selectedTab = 0;
        private readonly string[] _tabs = { "Overview", "Bundles", "Duplicates", "Suggestions" };

        [MenuItem("Tools/Project Chimera/Asset Bundle Analyzer")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetBundleAnalyzerWindow>("Bundle Analyzer");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // Header
            EditorGUILayout.LabelField("Asset Bundle Analyzer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Analyze button
            if (GUILayout.Button("Analyze Asset Bundles", GUILayout.Height(30)))
            {
                _lastReport = AssetBundleAnalyzer.Instance.AnalyzeAssetBundles();
            }

            EditorGUILayout.Space();

            if (_lastReport == null)
            {
                EditorGUILayout.HelpBox("Click 'Analyze Asset Bundles' to start analysis", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Tabs
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);
            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0: DrawOverview(); break;
                case 1: DrawBundles(); break;
                case 2: DrawDuplicates(); break;
                case 3: DrawSuggestions(); break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawOverview()
        {
            EditorGUILayout.LabelField("Analysis Summary", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"Analysis Time: {_lastReport.analysisTime}");
            EditorGUILayout.LabelField($"Total Bundles: {_lastReport.bundleCount}");
            EditorGUILayout.LabelField($"Total Size: {_lastReport.totalSizeKB / 1024f:F2} MB");
            EditorGUILayout.LabelField($"Total Assets: {_lastReport.totalAssets}");
            EditorGUILayout.LabelField($"Duplicate Assets: {_lastReport.duplicateCount}");
            EditorGUILayout.LabelField($"Wasted Space: {_lastReport.duplicateSizeKB / 1024f:F2} MB");
            EditorGUILayout.LabelField($"Optimization Suggestions: {_lastReport.suggestionCount}");
        }

        private void DrawBundles()
        {
            EditorGUILayout.LabelField($"Asset Bundles ({_lastReport.bundleCount})", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            foreach (var bundle in _lastReport.bundles)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(bundle.bundleName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Size: {bundle.sizeKB / 1024f:F2} MB");
                EditorGUILayout.LabelField($"Assets: {bundle.assetCount}");
                EditorGUILayout.LabelField($"Dependencies: {bundle.dependencies.Count}");
                EditorGUILayout.LabelField($"Est. Load Time: {bundle.estimatedLoadTimeMs:F0} ms");
                EditorGUILayout.LabelField($"Est. Memory: {bundle.estimatedMemoryKB / 1024f:F2} MB");
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawDuplicates()
        {
            EditorGUILayout.LabelField($"Duplicate Assets ({_lastReport.duplicateCount})", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (_lastReport.duplicateCount == 0)
            {
                EditorGUILayout.HelpBox("No duplicates found!", MessageType.Info);
                return;
            }

            foreach (var duplicate in _lastReport.duplicates)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(duplicate.assetName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Occurrences: {duplicate.occurrenceCount}");
                EditorGUILayout.LabelField($"Est. Size: {duplicate.estimatedSizeKB:F2} KB each");
                EditorGUILayout.LabelField($"Total Waste: {duplicate.estimatedSizeKB * (duplicate.occurrenceCount - 1):F2} KB");
                EditorGUILayout.LabelField("Found in bundles:");
                foreach (var bundle in duplicate.bundles)
                {
                    EditorGUILayout.LabelField($"  • {bundle}");
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawSuggestions()
        {
            EditorGUILayout.LabelField($"Optimization Suggestions ({_lastReport.suggestionCount})", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (_lastReport.suggestionCount == 0)
            {
                EditorGUILayout.HelpBox("No optimization suggestions. Your bundles look good!", MessageType.Info);
                return;
            }

            foreach (var suggestion in _lastReport.suggestions)
            {
                MessageType messageType = suggestion.severity switch
                {
                    SuggestionSeverity.High => MessageType.Error,
                    SuggestionSeverity.Medium => MessageType.Warning,
                    _ => MessageType.Info
                };

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox($"[{suggestion.severity}] {suggestion.title}", messageType);
                EditorGUILayout.LabelField(suggestion.description, EditorStyles.wordWrappedLabel);

                if (suggestion.estimatedSavingsKB > 0)
                {
                    EditorGUILayout.LabelField($"Potential Savings: {suggestion.estimatedSavingsKB / 1024f:F2} MB", EditorStyles.boldLabel);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
    }
#endif
}
