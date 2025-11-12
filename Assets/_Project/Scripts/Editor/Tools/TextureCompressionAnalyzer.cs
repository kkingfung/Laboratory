using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Texture compression analyzer for identifying optimization opportunities.
    /// Scans project textures and recommends compression settings.
    /// Can significantly reduce build size and memory usage.
    /// </summary>
    public class TextureCompressionAnalyzer : EditorWindow
    {
        #region Window Setup

        [MenuItem("Chimera/Optimization/Texture Compression Analyzer")]
        public static void ShowWindow()
        {
            var window = GetWindow<TextureCompressionAnalyzer>("Texture Compression Analyzer");
            window.minSize = new Vector2(900, 700);
            window.Show();
        }

        #endregion

        #region Private Fields

        private Vector2 _scrollPosition;
        private List<TextureAnalysis> _analyzedTextures = new List<TextureAnalysis>();
        private bool _isAnalyzing = false;
        private float _totalMemorySavingsMB = 0f;
        private int _optimizableTextures = 0;

        // Filters
        private bool _showOnlyOptimizable = true;
        private long _minSizeThresholdKB = 100;
        private TextureSortMode _sortMode = TextureSortMode.PotentialSavings;

        // Analysis settings
        private bool _checkCompression = true;
        private bool _checkMipmaps = true;
        private bool _checkReadWrite = true;
        private bool _checkMaxSize = true;

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _goodStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _errorStyle;
        private bool _stylesInitialized;

        #endregion

        #region Unity Lifecycle

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.LabelField("Texture Compression Analyzer", _headerStyle);
            EditorGUILayout.Space(10);

            DrawControls();
            EditorGUILayout.Space(10);

            DrawSummary();
            EditorGUILayout.Space(10);

            DrawTextureList();
        }

        #endregion

        #region Initialization

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            _goodStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
            };

            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.5f, 0f) }
            };

            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red },
                fontStyle = FontStyle.Bold
            };

            _stylesInitialized = true;
        }

        #endregion

        #region GUI - Controls

        private void DrawControls()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField("Analysis Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _checkCompression = EditorGUILayout.Toggle("Check Compression", _checkCompression);
            _checkMipmaps = EditorGUILayout.Toggle("Check Mipmaps", _checkMipmaps);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _checkReadWrite = EditorGUILayout.Toggle("Check Read/Write", _checkReadWrite);
            _checkMaxSize = EditorGUILayout.Toggle("Check Max Size", _checkMaxSize);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !_isAnalyzing;

            if (GUILayout.Button("Analyze All Textures", GUILayout.Height(30)))
            {
                AnalyzeAllTextures();
            }

            if (GUILayout.Button("Apply Recommended Settings", GUILayout.Height(30)))
            {
                ApplyRecommendedSettings();
            }

            if (GUILayout.Button("Export Report", GUILayout.Height(30)))
            {
                ExportReport();
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region GUI - Summary

        private void DrawSummary()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"Total Textures: {_analyzedTextures.Count}");
            EditorGUILayout.LabelField($"Optimizable Textures: {_optimizableTextures}", _optimizableTextures > 0 ? _warningStyle : _goodStyle);
            EditorGUILayout.LabelField($"Potential Memory Savings: {_totalMemorySavingsMB:F2} MB",
                _totalMemorySavingsMB > 0 ? _errorStyle : _goodStyle);

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region GUI - Texture List

        private void DrawTextureList()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Filters
            EditorGUILayout.BeginHorizontal();
            _showOnlyOptimizable = EditorGUILayout.Toggle("Show Only Optimizable", _showOnlyOptimizable);
            _minSizeThresholdKB = EditorGUILayout.LongField("Min Size (KB)", _minSizeThresholdKB, GUILayout.Width(200));
            _sortMode = (TextureSortMode)EditorGUILayout.EnumPopup("Sort By", _sortMode, GUILayout.Width(250));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var filtered = GetFilteredTextures();

            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox("No textures found. Click 'Analyze All Textures' to begin.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Showing {filtered.Count} textures", EditorStyles.miniLabel);

                foreach (var analysis in filtered)
                {
                    DrawTextureRow(analysis);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawTextureRow(TextureAnalysis analysis)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            // Texture preview
            if (analysis.texture != null)
            {
                EditorGUILayout.ObjectField(analysis.texture, typeof(Texture2D), false, GUILayout.Width(64), GUILayout.Height(64));
            }

            EditorGUILayout.BeginVertical();

            // Name and path
            EditorGUILayout.LabelField(analysis.textureName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(analysis.path, EditorStyles.miniLabel);

            // Stats
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Size: {analysis.width}x{analysis.height}", GUILayout.Width(120));
            EditorGUILayout.LabelField($"Format: {analysis.format}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"Memory: {analysis.memoryMB:F2} MB", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            // Issues
            if (analysis.issues.Count > 0)
            {
                EditorGUILayout.LabelField("Issues:", EditorStyles.boldLabel);
                foreach (var issue in analysis.issues)
                {
                    EditorGUILayout.LabelField($"• {issue}", _warningStyle);
                }

                if (analysis.potentialSavingsMB > 0)
                {
                    EditorGUILayout.LabelField($"Potential Savings: {analysis.potentialSavingsMB:F2} MB", _errorStyle);
                }
            }
            else
            {
                EditorGUILayout.LabelField("✓ Optimized", _goodStyle);
            }

            EditorGUILayout.EndVertical();

            // Actions
            EditorGUILayout.BeginVertical(GUILayout.Width(100));

            if (GUILayout.Button("Select"))
            {
                Selection.activeObject = analysis.texture;
                EditorGUIUtility.PingObject(analysis.texture);
            }

            if (analysis.issues.Count > 0 && GUILayout.Button("Fix"))
            {
                ApplyRecommendedSettings(analysis);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Analysis

        private void AnalyzeAllTextures()
        {
            _isAnalyzing = true;
            _analyzedTextures.Clear();
            _totalMemorySavingsMB = 0f;
            _optimizableTextures = 0;

            try
            {
                var textureGuids = AssetDatabase.FindAssets("t:Texture2D");
                int total = textureGuids.Length;
                int current = 0;

                foreach (var guid in textureGuids)
                {
                    current++;
                    EditorUtility.DisplayProgressBar("Analyzing Textures",
                        $"Analyzing texture {current}/{total}",
                        (float)current / total);

                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                    if (texture != null)
                    {
                        var analysis = AnalyzeTexture(texture, path);
                        _analyzedTextures.Add(analysis);

                        if (analysis.issues.Count > 0)
                        {
                            _optimizableTextures++;
                            _totalMemorySavingsMB += analysis.potentialSavingsMB;
                        }
                    }
                }

                EditorUtility.ClearProgressBar();

                Debug.Log($"[TextureCompressionAnalyzer] Analyzed {_analyzedTextures.Count} textures. " +
                          $"Found {_optimizableTextures} optimizable textures with {_totalMemorySavingsMB:F2} MB potential savings.");
            }
            finally
            {
                _isAnalyzing = false;
                EditorUtility.ClearProgressBar();
            }
        }

        private TextureAnalysis AnalyzeTexture(Texture2D texture, string path)
        {
            var analysis = new TextureAnalysis
            {
                texture = texture,
                textureName = texture.name,
                path = path,
                width = texture.width,
                height = texture.height,
                format = texture.format.ToString()
            };

            // Get import settings
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return analysis;

            // Calculate current memory usage
            analysis.memoryMB = CalculateTextureMemory(texture);

            // Check compression
            if (_checkCompression)
            {
                CheckCompression(analysis, importer);
            }

            // Check mipmaps
            if (_checkMipmaps)
            {
                CheckMipmaps(analysis, importer);
            }

            // Check read/write
            if (_checkReadWrite)
            {
                CheckReadWrite(analysis, importer);
            }

            // Check max size
            if (_checkMaxSize)
            {
                CheckMaxSize(analysis, importer, texture);
            }

            return analysis;
        }

        private void CheckCompression(TextureAnalysis analysis, TextureImporter importer)
        {
            if (importer.textureCompression == TextureImporterCompression.Uncompressed)
            {
                analysis.issues.Add("Texture is uncompressed (use Compressed or Compressed High Quality)");
                analysis.potentialSavingsMB += analysis.memoryMB * 0.75f; // Estimate 75% savings
            }
        }

        private void CheckMipmaps(TextureAnalysis analysis, TextureImporter importer)
        {
            if (!importer.mipmapEnabled && analysis.texture.width > 256)
            {
                analysis.issues.Add("Large texture without mipmaps (enable for better performance)");
            }
        }

        private void CheckReadWrite(TextureAnalysis analysis, TextureImporter importer)
        {
            if (importer.isReadable)
            {
                analysis.issues.Add("Read/Write enabled (doubles memory usage)");
                analysis.potentialSavingsMB += analysis.memoryMB * 0.5f; // Estimate 50% savings
            }
        }

        private void CheckMaxSize(TextureAnalysis analysis, TextureImporter importer, Texture2D texture)
        {
            if (importer.maxTextureSize > 2048 && texture.width <= 2048 && texture.height <= 2048)
            {
                analysis.issues.Add("Max texture size too large (consider reducing to 2048 or lower)");
                float reduction = texture.width > 1024 ? 0.25f : 0.5f;
                analysis.potentialSavingsMB += analysis.memoryMB * reduction;
            }
        }

        private float CalculateTextureMemory(Texture2D texture)
        {
            // Rough estimation
            int pixelCount = texture.width * texture.height;
            int bytesPerPixel = 4; // Assume RGBA32 worst case

            float bytes = pixelCount * bytesPerPixel;

            // Account for mipmaps (adds ~33%)
            if (texture.mipmapCount > 1)
            {
                bytes *= 1.33f;
            }

            return bytes / 1024f / 1024f; // Convert to MB
        }

        #endregion

        #region Optimization

        private void ApplyRecommendedSettings()
        {
            if (!EditorUtility.DisplayDialog("Apply Recommended Settings",
                $"This will apply recommended compression settings to {_optimizableTextures} textures.\n\nContinue?",
                "Yes", "No"))
            {
                return;
            }

            int count = 0;
            foreach (var analysis in _analyzedTextures.Where(a => a.issues.Count > 0))
            {
                ApplyRecommendedSettings(analysis);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Complete",
                $"Applied recommended settings to {count} textures.",
                "OK");

            // Re-analyze
            AnalyzeAllTextures();
        }

        private void ApplyRecommendedSettings(TextureAnalysis analysis)
        {
            var importer = AssetImporter.GetAtPath(analysis.path) as TextureImporter;
            if (importer == null) return;

            bool modified = false;

            foreach (var issue in analysis.issues)
            {
                if (issue.Contains("uncompressed"))
                {
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    modified = true;
                }

                if (issue.Contains("Read/Write"))
                {
                    importer.isReadable = false;
                    modified = true;
                }

                if (issue.Contains("mipmaps"))
                {
                    importer.mipmapEnabled = true;
                    modified = true;
                }

                if (issue.Contains("Max texture size"))
                {
                    importer.maxTextureSize = 2048;
                    modified = true;
                }
            }

            if (modified)
            {
                importer.SaveAndReimport();
                Debug.Log($"[TextureCompressionAnalyzer] Optimized: {analysis.textureName}");
            }
        }

        #endregion

        #region Filtering & Sorting

        private List<TextureAnalysis> GetFilteredTextures()
        {
            var filtered = _analyzedTextures.AsEnumerable();

            if (_showOnlyOptimizable)
            {
                filtered = filtered.Where(a => a.issues.Count > 0);
            }

            if (_minSizeThresholdKB > 0)
            {
                filtered = filtered.Where(a => a.memoryMB * 1024 >= _minSizeThresholdKB);
            }

            // Sort
            filtered = _sortMode switch
            {
                TextureSortMode.Name => filtered.OrderBy(a => a.textureName),
                TextureSortMode.Size => filtered.OrderByDescending(a => a.memoryMB),
                TextureSortMode.PotentialSavings => filtered.OrderByDescending(a => a.potentialSavingsMB),
                _ => filtered
            };

            return filtered.ToList();
        }

        #endregion

        #region Reporting

        private void ExportReport()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Texture Compression Analysis Report ===");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine($"Total Textures: {_analyzedTextures.Count}");
            sb.AppendLine($"Optimizable Textures: {_optimizableTextures}");
            sb.AppendLine($"Potential Memory Savings: {_totalMemorySavingsMB:F2} MB");
            sb.AppendLine();

            sb.AppendLine("Textures with Issues:");
            foreach (var analysis in _analyzedTextures.Where(a => a.issues.Count > 0).OrderByDescending(a => a.potentialSavingsMB))
            {
                sb.AppendLine($"\n{analysis.textureName} ({analysis.path})");
                sb.AppendLine($"  Size: {analysis.width}x{analysis.height}, Format: {analysis.format}, Memory: {analysis.memoryMB:F2} MB");
                sb.AppendLine($"  Potential Savings: {analysis.potentialSavingsMB:F2} MB");
                sb.AppendLine("  Issues:");
                foreach (var issue in analysis.issues)
                {
                    sb.AppendLine($"    - {issue}");
                }
            }

            string path = EditorUtility.SaveFilePanel("Save Report", "", "TextureCompressionReport.txt", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, sb.ToString());
                EditorUtility.DisplayDialog("Export Complete", $"Report saved to:\n{path}", "OK");
            }
        }

        #endregion

        #region Data Structures

        private class TextureAnalysis
        {
            public Texture2D texture;
            public string textureName;
            public string path;
            public int width;
            public int height;
            public string format;
            public float memoryMB;
            public List<string> issues = new List<string>();
            public float potentialSavingsMB;
        }

        private enum TextureSortMode
        {
            Name,
            Size,
            PotentialSavings
        }

        #endregion
    }
}
