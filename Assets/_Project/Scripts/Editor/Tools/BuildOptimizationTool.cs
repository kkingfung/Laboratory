using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Build optimization tool for reducing build size and improving loading times
    /// Analyzes assets, removes unused resources, optimizes settings, and generates reports
    /// </summary>
    public class BuildOptimizationTool : EditorWindow, IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private Vector2 _scrollPosition;
        private BuildOptimizationReport _lastReport;
        private bool _showDetailedReport = false;

        // Optimization settings
        private bool _removeUnusedAssets = true;
        private bool _compressTextures = true;
        private bool _stripDebugSymbols = true;
        private bool _enableCodeStripping = true;
        private bool _optimizeAudioFiles = true;
        private bool _optimizeMeshes = true;
        private bool _generateReport = true;

        // Analysis results
        private long _estimatedSavings = 0;
        private Dictionary<string, long> _assetSizes = new Dictionary<string, long>();

        public int callbackOrder => 0;

        [MenuItem("Chimera/Build/Optimization Tool", false, 500)]
        private static void ShowWindow()
        {
            var window = GetWindow<BuildOptimizationTool>("Build Optimization");
            window.minSize = new Vector2(600, 700);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            DrawHeader();
            DrawOptimizationSettings();
            DrawAnalysisTools();

            if (_lastReport != null)
            {
                DrawReport();
            }
        }

        #region Header

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("⚡ Build Optimization Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Optimize build size and loading times", MessageType.Info);
            EditorGUILayout.Space(5);
        }

        #endregion

        #region Optimization Settings

        private void DrawOptimizationSettings()
        {
            EditorGUILayout.LabelField("Optimization Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _removeUnusedAssets = EditorGUILayout.Toggle("Remove Unused Assets", _removeUnusedAssets);
            _compressTextures = EditorGUILayout.Toggle("Compress Textures", _compressTextures);
            _stripDebugSymbols = EditorGUILayout.Toggle("Strip Debug Symbols", _stripDebugSymbols);
            _enableCodeStripping = EditorGUILayout.Toggle("Enable Code Stripping", _enableCodeStripping);
            _optimizeAudioFiles = EditorGUILayout.Toggle("Optimize Audio Files", _optimizeAudioFiles);
            _optimizeMeshes = EditorGUILayout.Toggle("Optimize Meshes", _optimizeMeshes);
            _generateReport = EditorGUILayout.Toggle("Generate Build Report", _generateReport);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
        }

        #endregion

        #region Analysis Tools

        private void DrawAnalysisTools()
        {
            EditorGUILayout.LabelField("Analysis & Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Analyze Project", GUILayout.Height(30)))
            {
                AnalyzeProject();
            }

            if (GUILayout.Button("Optimize Now", GUILayout.Height(30)))
            {
                OptimizeProject();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Find Unused Assets", GUILayout.Height(30)))
            {
                FindUnusedAssets();
            }

            if (GUILayout.Button("Compress All Textures", GUILayout.Height(30)))
            {
                CompressTextures();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply Player Settings", GUILayout.Height(30)))
            {
                ApplyOptimalPlayerSettings();
            }

            if (GUILayout.Button("Export Report", GUILayout.Height(30)))
            {
                ExportReport();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Estimated savings display
            if (_estimatedSavings > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Estimated Savings:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{FormatFileSize(_estimatedSavings)}");
                EditorGUILayout.EndVertical();
            }
        }

        #endregion

        #region Report Display

        private void DrawReport()
        {
            EditorGUILayout.LabelField("Optimization Report", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Summary
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Build Time: {_lastReport.buildTime:F2}s");
            EditorGUILayout.LabelField($"Total Size: {FormatFileSize(_lastReport.totalSize)}");
            EditorGUILayout.LabelField($"Warnings: {_lastReport.warnings.Count}");
            EditorGUILayout.LabelField($"Errors: {_lastReport.errors.Count}");

            EditorGUILayout.Space(5);

            // Asset breakdown
            _showDetailedReport = EditorGUILayout.Foldout(_showDetailedReport, "Detailed Breakdown");
            if (_showDetailedReport)
            {
                DrawAssetBreakdown();
            }

            // Warnings
            if (_lastReport.warnings.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Warnings:", EditorStyles.boldLabel);

                foreach (var warning in _lastReport.warnings.Take(10))
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }

                if (_lastReport.warnings.Count > 10)
                {
                    EditorGUILayout.LabelField($"... and {_lastReport.warnings.Count - 10} more warnings");
                }
            }

            // Recommendations
            DrawRecommendations();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private void DrawAssetBreakdown()
        {
            EditorGUI.indentLevel++;

            if (_lastReport.assetSizes.Count > 0)
            {
                var sortedAssets = _lastReport.assetSizes.OrderByDescending(kvp => kvp.Value).Take(20);

                EditorGUILayout.LabelField("Top 20 Largest Assets:", EditorStyles.miniLabel);

                foreach (var asset in sortedAssets)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Path.GetFileName(asset.Key), GUILayout.Width(300));
                    EditorGUILayout.LabelField(FormatFileSize(asset.Value), GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawRecommendations()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Recommendations:", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (_lastReport.totalSize > 500 * 1024 * 1024) // > 500MB
            {
                EditorGUILayout.HelpBox("Build size is large (>500MB). Consider asset bundle streaming.", MessageType.Warning);
            }

            if (_lastReport.buildTime > 300) // > 5 minutes
            {
                EditorGUILayout.HelpBox("Build time is slow (>5min). Enable incremental building.", MessageType.Warning);
            }

            if (_lastReport.warnings.Any(w => w.Contains("unused")))
            {
                EditorGUILayout.HelpBox("Unused assets detected. Run 'Remove Unused Assets'.", MessageType.Info);
            }

            if (!_enableCodeStripping)
            {
                EditorGUILayout.HelpBox("Code stripping disabled. Enable for smaller builds.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Optimization Methods

        private void AnalyzeProject()
        {
            Debug.Log("[BuildOptimization] Analyzing project...");

            _assetSizes.Clear();
            _estimatedSavings = 0;

            // Analyze textures
            var textures = AssetDatabase.FindAssets("t:Texture2D");
            foreach (var guid in textures)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                if (texture != null)
                {
                    long size = GetAssetSize(path);
                    _assetSizes[path] = size;

                    // Estimate savings from compression
                    if (_compressTextures && !IsTextureCompressed(texture))
                    {
                        _estimatedSavings += size / 2; // Estimate 50% reduction
                    }
                }
            }

            // Analyze audio
            var audioClips = AssetDatabase.FindAssets("t:AudioClip");
            foreach (var guid in audioClips)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                long size = GetAssetSize(path);
                _assetSizes[path] = size;

                if (_optimizeAudioFiles)
                {
                    _estimatedSavings += size / 3; // Estimate 33% reduction
                }
            }

            Debug.Log($"[BuildOptimization] Analysis complete. Estimated savings: {FormatFileSize(_estimatedSavings)}");

            EditorUtility.DisplayDialog("Analysis Complete",
                $"Found {_assetSizes.Count} assets\n" +
                $"Estimated savings: {FormatFileSize(_estimatedSavings)}",
                "OK");
        }

        private void OptimizeProject()
        {
            if (!EditorUtility.DisplayDialog("Optimize Project",
                "This will apply optimizations to your project. Create a backup first?\n\n" +
                "Optimizations:\n" +
                $"- Remove unused assets: {_removeUnusedAssets}\n" +
                $"- Compress textures: {_compressTextures}\n" +
                $"- Optimize audio: {_optimizeAudioFiles}\n" +
                $"- Optimize meshes: {_optimizeMeshes}",
                "Proceed", "Cancel"))
            {
                return;
            }

            Debug.Log("[BuildOptimization] Starting optimization...");

            int optimizedCount = 0;

            if (_compressTextures)
            {
                optimizedCount += CompressTextures();
            }

            if (_optimizeAudioFiles)
            {
                optimizedCount += OptimizeAudio();
            }

            if (_optimizeMeshes)
            {
                optimizedCount += OptimizeMeshes();
            }

            if (_stripDebugSymbols || _enableCodeStripping)
            {
                ApplyOptimalPlayerSettings();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BuildOptimization] Optimization complete. Optimized {optimizedCount} assets.");

            EditorUtility.DisplayDialog("Optimization Complete",
                $"Optimized {optimizedCount} assets\n\n" +
                "Build your project to see the final size reduction.",
                "OK");
        }

        private void FindUnusedAssets()
        {
            Debug.Log("[BuildOptimization] Searching for unused assets...");

            var allAssets = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/") && !p.Contains("Scripts"))
                .ToList();

            var usedAssets = new HashSet<string>();

            // Get all assets in scenes
            var scenes = EditorBuildSettings.scenes;
            foreach (var scene in scenes)
            {
                if (scene.enabled)
                {
                    var dependencies = AssetDatabase.GetDependencies(scene.path, true);
                    foreach (var dep in dependencies)
                    {
                        usedAssets.Add(dep);
                    }
                }
            }

            // Find unused
            var unusedAssets = allAssets.Where(a => !usedAssets.Contains(a)).ToList();

            Debug.Log($"[BuildOptimization] Found {unusedAssets.Count} potentially unused assets");

            if (unusedAssets.Count > 0)
            {
                string message = $"Found {unusedAssets.Count} potentially unused assets\n\n" +
                                "Review them in the console. Delete carefully!";

                foreach (var asset in unusedAssets.Take(20))
                {
                    Debug.LogWarning($"Potentially unused: {asset}");
                }

                EditorUtility.DisplayDialog("Unused Assets Found", message, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Unused Assets", "All assets appear to be used.", "OK");
            }
        }

        private int CompressTextures()
        {
            var textures = AssetDatabase.FindAssets("t:Texture2D");
            int compressedCount = 0;

            foreach (var guid in textures)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null && !IsTextureCompressed(importer))
                {
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.SaveAndReimport();
                    compressedCount++;
                }
            }

            Debug.Log($"[BuildOptimization] Compressed {compressedCount} textures");
            return compressedCount;
        }

        private int OptimizeAudio()
        {
            var audioClips = AssetDatabase.FindAssets("t:AudioClip");
            int optimizedCount = 0;

            foreach (var guid in audioClips)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;

                if (importer != null)
                {
                    var defaultSettings = importer.defaultSampleSettings;

                    if (defaultSettings.compressionFormat != AudioCompressionFormat.Vorbis)
                    {
                        defaultSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                        defaultSettings.quality = 0.7f;
                        defaultSettings.loadType = AudioClipLoadType.CompressedInMemory;

                        importer.defaultSampleSettings = defaultSettings;
                        importer.SaveAndReimport();
                        optimizedCount++;
                    }
                }
            }

            Debug.Log($"[BuildOptimization] Optimized {optimizedCount} audio files");
            return optimizedCount;
        }

        private int OptimizeMeshes()
        {
            var meshes = AssetDatabase.FindAssets("t:Mesh");
            int optimizedCount = 0;

            foreach (var guid in meshes)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;

                if (importer != null)
                {
                    bool changed = false;

                    if (!importer.optimizeMeshPolygons)
                    {
                        importer.optimizeMeshPolygons = true;
                        changed = true;
                    }

                    if (!importer.optimizeMeshVertices)
                    {
                        importer.optimizeMeshVertices = true;
                        changed = true;
                    }

                    if (changed)
                    {
                        importer.SaveAndReimport();
                        optimizedCount++;
                    }
                }
            }

            Debug.Log($"[BuildOptimization] Optimized {optimizedCount} meshes");
            return optimizedCount;
        }

        private void ApplyOptimalPlayerSettings()
        {
            Debug.Log("[BuildOptimization] Applying optimal player settings...");

            if (_stripDebugSymbols)
            {
                PlayerSettings.stripEngineCode = true;
            }

            if (_enableCodeStripping)
            {
                PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.High);
            }

            // Disable unused physics
            Physics.autoSyncTransforms = false;

            // Optimize graphics
            PlayerSettings.bakeCollisionMeshes = true;
            PlayerSettings.stripUnusedMeshComponents = true;

            Debug.Log("[BuildOptimization] Player settings optimized");
        }

        private void ExportReport()
        {
            if (_lastReport == null)
            {
                EditorUtility.DisplayDialog("No Report", "Run a build first to generate a report.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export Build Report", "", "build_report.txt", "txt");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== BUILD OPTIMIZATION REPORT ===");
            sb.AppendLine($"Generated: {System.DateTime.Now}");
            sb.AppendLine();

            sb.AppendLine($"Build Time: {_lastReport.buildTime:F2}s");
            sb.AppendLine($"Total Size: {FormatFileSize(_lastReport.totalSize)}");
            sb.AppendLine($"Warnings: {_lastReport.warnings.Count}");
            sb.AppendLine($"Errors: {_lastReport.errors.Count}");
            sb.AppendLine();

            sb.AppendLine("TOP 20 LARGEST ASSETS:");
            foreach (var asset in _lastReport.assetSizes.OrderByDescending(kvp => kvp.Value).Take(20))
            {
                sb.AppendLine($"  {Path.GetFileName(asset.Key)}: {FormatFileSize(asset.Value)}");
            }

            File.WriteAllText(path, sb.ToString());

            Debug.Log($"[BuildOptimization] Report exported to: {path}");
            EditorUtility.DisplayDialog("Export Complete", $"Report exported to:\n{path}", "OK");
        }

        #endregion

        #region Build Callbacks

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[BuildOptimization] Preprocessing build...");

            if (_generateReport)
            {
                _lastReport = new BuildOptimizationReport
                {
                    buildStartTime = System.DateTime.Now
                };
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            Debug.Log("[BuildOptimization] Postprocessing build...");

            if (_generateReport && _lastReport != null)
            {
                _lastReport.buildTime = (float)(System.DateTime.Now - _lastReport.buildStartTime).TotalSeconds;
                _lastReport.totalSize = report.summary.totalSize;

                foreach (var file in report.files)
                {
                    _lastReport.assetSizes[file.path] = (long)file.size;
                }

                foreach (var step in report.steps)
                {
                    foreach (var message in step.messages)
                    {
                        if (message.type == LogType.Warning)
                        {
                            _lastReport.warnings.Add(message.content);
                        }
                        else if (message.type == LogType.Error)
                        {
                            _lastReport.errors.Add(message.content);
                        }
                    }
                }

                Debug.Log($"[BuildOptimization] Build complete. Size: {FormatFileSize(_lastReport.totalSize)}, Time: {_lastReport.buildTime:F2}s");

                Repaint();
            }
        }

        #endregion

        #region Helper Methods

        private bool IsTextureCompressed(Texture2D texture)
        {
            return texture.format != TextureFormat.RGBA32 && texture.format != TextureFormat.RGB24;
        }

        private bool IsTextureCompressed(TextureImporter importer)
        {
            return importer.textureCompression != TextureImporterCompression.Uncompressed;
        }

        private long GetAssetSize(string path)
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }
            return 0;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:F2} {sizes[order]}";
        }

        #endregion

        #region Data Structures

        private class BuildOptimizationReport
        {
            public System.DateTime buildStartTime;
            public float buildTime;
            public long totalSize;
            public Dictionary<string, long> assetSizes = new Dictionary<string, long>();
            public List<string> warnings = new List<string>();
            public List<string> errors = new List<string>();
        }

        #endregion
    }
}
