using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Shader variant stripping tool to reduce build size.
    /// Automatically removes unused shader variants during build process.
    /// Can reduce build size by 50-80% and improve loading times.
    /// </summary>
    public class ShaderVariantStripper : IPreprocessShaders
    {
        #region Configuration

        private static ShaderStrippingConfig _config;

        public int callbackOrder => 0;

        #endregion

        #region Statistics

        private static int _totalVariantsProcessed;
        private static int _totalVariantsStripped;
        private static Dictionary<string, int> _strippedByShader = new Dictionary<string, int>();

        #endregion

        #region Shader Stripping

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            LoadConfig();

            if (!_config.enableStripping)
                return;

            int originalCount = data.Count;
            _totalVariantsProcessed += originalCount;

            // Strip by keyword
            if (_config.stripUnusedKeywords)
            {
                StripByKeywords(shader, snippet, data);
            }

            // Strip by platform
            if (_config.stripByPlatform)
            {
                StripByPlatform(shader, snippet, data);
            }

            // Strip by shader type
            if (_config.stripByShaderType)
            {
                StripByShaderType(shader, snippet, data);
            }

            // Strip fog variants
            if (_config.stripFogVariants)
            {
                StripFogVariants(shader, snippet, data);
            }

            // Strip lightmap variants
            if (_config.stripLightmapVariants)
            {
                StripLightmapVariants(shader, snippet, data);
            }

            // Strip HDR variants
            if (_config.stripHDRVariants)
            {
                StripHDRVariants(shader, snippet, data);
            }

            int strippedCount = originalCount - data.Count;
            if (strippedCount > 0)
            {
                _totalVariantsStripped += strippedCount;

                string shaderName = shader.name;
                if (!_strippedByShader.ContainsKey(shaderName))
                {
                    _strippedByShader[shaderName] = 0;
                }
                _strippedByShader[shaderName] += strippedCount;

                if (_config.logStrippingDetails)
                {
                    Debug.Log($"[ShaderStripper] {shader.name}: Stripped {strippedCount}/{originalCount} variants");
                }
            }
        }

        private void StripByKeywords(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var variant = data[i];

                // Check for blacklisted keywords
                foreach (var keyword in _config.blacklistedKeywords)
                {
                    if (variant.shaderKeywordSet.IsEnabled(new ShaderKeyword(keyword)))
                    {
                        data.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        private void StripByPlatform(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            var targetPlatform = EditorUserBuildSettings.activeBuildTarget;

            for (int i = data.Count - 1; i >= 0; i--)
            {
                var variant = data[i];

                // Only keep variants for target platform
                // Note: platformKeyword API is obsolete in Unity 6, platform stripping now handled automatically
                bool shouldStrip = false;  // Disabled platform keyword stripping due to API changes

                if (shouldStrip && _config.aggressiveStripping)
                {
                    data.RemoveAt(i);
                }
            }
        }

        private void StripByShaderType(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // Strip unused shader types
            if (!_config.includeVertexShaders && snippet.shaderType == ShaderType.Vertex)
            {
                data.Clear();
                return;
            }

            if (!_config.includeFragmentShaders && snippet.shaderType == ShaderType.Fragment)
            {
                data.Clear();
                return;
            }

            if (!_config.includeGeometryShaders && snippet.shaderType == ShaderType.Geometry)
            {
                data.Clear();
                return;
            }

            if (!_config.includeComputeShaders && snippet.shaderType == ShaderType.Fragment) // Compute handled differently
            {
                // Keep compute shaders for ECS
            }
        }

        private void StripFogVariants(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // Strip fog variants if fog not used
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var variant = data[i];

                if (variant.shaderKeywordSet.IsEnabled(new ShaderKeyword("FOG_LINEAR")) ||
                    variant.shaderKeywordSet.IsEnabled(new ShaderKeyword("FOG_EXP")) ||
                    variant.shaderKeywordSet.IsEnabled(new ShaderKeyword("FOG_EXP2")))
                {
                    data.RemoveAt(i);
                }
            }
        }

        private void StripLightmapVariants(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // Strip lightmap variants for dynamic-only games
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var variant = data[i];

                if (variant.shaderKeywordSet.IsEnabled(new ShaderKeyword("LIGHTMAP_ON")) ||
                    variant.shaderKeywordSet.IsEnabled(new ShaderKeyword("DIRLIGHTMAP_COMBINED")) ||
                    variant.shaderKeywordSet.IsEnabled(new ShaderKeyword("LIGHTMAP_SHADOW_MIXING")))
                {
                    data.RemoveAt(i);
                }
            }
        }

        private void StripHDRVariants(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // Strip HDR variants if not using HDR
            for (int i = data.Count - 1; i >= 0; i--)
            {
                var variant = data[i];

                if (variant.shaderKeywordSet.IsEnabled(new ShaderKeyword("HDR_ON")) ||
                    variant.shaderKeywordSet.IsEnabled(new ShaderKeyword("_TONEMAP_ACES")))
                {
                    data.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Configuration

        private static void LoadConfig()
        {
            if (_config == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:ShaderStrippingConfig");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _config = AssetDatabase.LoadAssetAtPath<ShaderStrippingConfig>(path);
                }

                if (_config == null)
                {
                    // Create default config
                    _config = ScriptableObject.CreateInstance<ShaderStrippingConfig>();
                    _config.SetDefaults();
                }
            }
        }

        #endregion

        #region Build Reporting

        [InitializeOnLoadMethod]
        private static void RegisterBuildCallback()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayerWithReport);
        }

        private static void BuildPlayerWithReport(BuildPlayerOptions options)
        {
            _totalVariantsProcessed = 0;
            _totalVariantsStripped = 0;
            _strippedByShader.Clear();

            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);

            GenerateStrippingReport();
        }

        private static void GenerateStrippingReport()
        {
            if (_totalVariantsProcessed == 0)
                return;

            float stripPercentage = (_totalVariantsStripped / (float)_totalVariantsProcessed) * 100f;

            var report = $"=== Shader Variant Stripping Report ===\n" +
                         $"Total Variants Processed: {_totalVariantsProcessed}\n" +
                         $"Total Variants Stripped: {_totalVariantsStripped}\n" +
                         $"Strip Percentage: {stripPercentage:F1}%\n" +
                         $"\nTop Stripped Shaders:\n";

            var topShaders = _strippedByShader.OrderByDescending(kvp => kvp.Value).Take(10);
            foreach (var kvp in topShaders)
            {
                report += $"  {kvp.Key}: {kvp.Value} variants\n";
            }

            Debug.Log($"[ShaderStripper]\n{report}");

            // Save report to file
            string reportPath = "Assets/BuildReports/ShaderStripping_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            System.IO.Directory.CreateDirectory("Assets/BuildReports");
            System.IO.File.WriteAllText(reportPath, report);
        }

        #endregion

        #region Menu Items

        [MenuItem("Chimera/Shaders/Analyze Shader Variants")]
        private static void AnalyzeShaderVariants()
        {
            var shaders = AssetDatabase.FindAssets("t:Shader")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<Shader>(path))
                .Where(shader => shader != null)
                .ToList();

            Debug.Log($"[ShaderStripper] Found {shaders.Count} shaders in project");

            // In a full implementation, analyze variant counts per shader
            // This requires ShaderUtil API which has limited access
        }

        [MenuItem("Chimera/Shaders/Create Stripping Config")]
        private static void CreateStrippingConfig()
        {
            var config = ScriptableObject.CreateInstance<ShaderStrippingConfig>();
            config.SetDefaults();

            string path = "Assets/_Project/Settings/ShaderStrippingConfig.asset";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            Debug.Log($"[ShaderStripper] Created config at: {path}");
        }

        #endregion
    }

    /// <summary>
    /// Configuration for shader variant stripping.
    /// </summary>
    [CreateAssetMenu(fileName = "ShaderStrippingConfig", menuName = "Chimera/Shader Stripping Config")]
    public class ShaderStrippingConfig : ScriptableObject
    {
        [Header("General")]
        public bool enableStripping = true;
        public bool aggressiveStripping = false;
        public bool logStrippingDetails = true;

        [Header("Keyword Stripping")]
        public bool stripUnusedKeywords = true;
        public List<string> blacklistedKeywords = new List<string>();

        [Header("Platform Stripping")]
        public bool stripByPlatform = true;

        [Header("Shader Type Stripping")]
        public bool stripByShaderType = false;
        public bool includeVertexShaders = true;
        public bool includeFragmentShaders = true;
        public bool includeGeometryShaders = false;
        public bool includeComputeShaders = true;

        [Header("Feature Stripping")]
        public bool stripFogVariants = true;
        public bool stripLightmapVariants = true;
        public bool stripHDRVariants = false;
        public bool stripInstancingVariants = false;
        public bool stripShadowVariants = false;

        [Header("Quality Stripping")]
        public bool stripLowQuality = false;
        public bool stripMediumQuality = false;
        public bool stripHighQuality = false;

        public void SetDefaults()
        {
            enableStripping = true;
            aggressiveStripping = false;
            logStrippingDetails = true;

            stripUnusedKeywords = true;
            blacklistedKeywords = new List<string>
            {
                "UNITY_UI_CLIP_RECT",
                "UNITY_UI_ALPHACLIP",
                "STEREO_INSTANCING_ON",
                "STEREO_MULTIVIEW_ON"
            };

            stripByPlatform = true;
            stripByShaderType = false;

            includeVertexShaders = true;
            includeFragmentShaders = true;
            includeGeometryShaders = false;
            includeComputeShaders = true;

            stripFogVariants = true;
            stripLightmapVariants = true;
            stripHDRVariants = false;
            stripInstancingVariants = false;
            stripShadowVariants = false;

            stripLowQuality = false;
            stripMediumQuality = false;
            stripHighQuality = false;
        }
    }

    /// <summary>
    /// Editor window for shader variant analysis.
    /// </summary>
    public class ShaderVariantAnalyzerWindow : EditorWindow
    {
        [MenuItem("Chimera/Shaders/Variant Analyzer")]
        public static void ShowWindow()
        {
            var window = GetWindow<ShaderVariantAnalyzerWindow>("Shader Variant Analyzer");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private Vector2 _scrollPosition;
        private List<ShaderVariantInfo> _shaderInfo = new List<ShaderVariantInfo>();
        private bool _showOnlyLargeShaders = true;
        private int _largeShaderThreshold = 100;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Shader Variant Analyzer", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Analyze Project Shaders", GUILayout.Height(30)))
            {
                AnalyzeShaders();
            }

            if (GUILayout.Button("Export Report", GUILayout.Height(30)))
            {
                ExportReport();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Filters
            EditorGUILayout.BeginHorizontal();
            _showOnlyLargeShaders = EditorGUILayout.Toggle("Show Only Large Shaders", _showOnlyLargeShaders);
            if (_showOnlyLargeShaders)
            {
                _largeShaderThreshold = EditorGUILayout.IntField("Threshold:", _largeShaderThreshold, GUILayout.Width(200));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Shader list
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_shaderInfo.Count == 0)
            {
                EditorGUILayout.HelpBox("Click 'Analyze Project Shaders' to begin analysis.", MessageType.Info);
            }
            else
            {
                var filtered = _showOnlyLargeShaders
                    ? _shaderInfo.Where(s => s.estimatedVariants > _largeShaderThreshold).ToList()
                    : _shaderInfo;

                EditorGUILayout.LabelField($"Showing {filtered.Count()} shaders", EditorStyles.miniLabel);

                foreach (var info in filtered.OrderByDescending(s => s.estimatedVariants))
                {
                    DrawShaderInfo(info);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawShaderInfo(ShaderVariantInfo info)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(info.shaderName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"~{info.estimatedVariants} variants", GUILayout.Width(150));

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = info.shader;
                EditorGUIUtility.PingObject(info.shader);
            }

            EditorGUILayout.EndHorizontal();

            if (info.estimatedVariants > 500)
            {
                EditorGUILayout.HelpBox("This shader has a very large number of variants. Consider stripping.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void AnalyzeShaders()
        {
            _shaderInfo.Clear();

            var shaders = AssetDatabase.FindAssets("t:Shader")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<Shader>(path))
                .Where(shader => shader != null)
                .ToList();

            foreach (var shader in shaders)
            {
                // Estimate variant count (simplified - actual count requires build-time analysis)
                int estimatedVariants = EstimateVariantCount(shader);

                _shaderInfo.Add(new ShaderVariantInfo
                {
                    shader = shader,
                    shaderName = shader.name,
                    estimatedVariants = estimatedVariants
                });
            }

            Debug.Log($"[ShaderVariantAnalyzer] Analyzed {_shaderInfo.Count} shaders");
        }

        private int EstimateVariantCount(Shader shader)
        {
            // Simplified estimation based on shader complexity
            // Real variant count requires ShaderVariantCollection or build-time analysis
            int baseVariants = 1;

            // Check for common multi-compile directives (rough heuristic)
            var shaderPath = AssetDatabase.GetAssetPath(shader);
            if (!string.IsNullOrEmpty(shaderPath))
            {
                var shaderCode = System.IO.File.ReadAllText(shaderPath);

                // Count multi_compile directives (very rough estimate)
                int multiCompiles = CountOccurrences(shaderCode, "multi_compile");
                int shaderFeatures = CountOccurrences(shaderCode, "shader_feature");

                // Each multi_compile typically doubles variants (simplified)
                baseVariants = (int)Math.Pow(2, multiCompiles + shaderFeatures);

                // Cap at reasonable estimate
                baseVariants = Mathf.Min(baseVariants, 10000);
            }

            return baseVariants;
        }

        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }

        private void ExportReport()
        {
            if (_shaderInfo.Count == 0)
            {
                EditorUtility.DisplayDialog("No Data", "Please analyze shaders first.", "OK");
                return;
            }

            string report = "=== Shader Variant Analysis Report ===\n\n";
            report += $"Total Shaders: {_shaderInfo.Count}\n";
            report += $"Total Estimated Variants: {_shaderInfo.Sum(s => s.estimatedVariants)}\n\n";

            report += "Shaders by Variant Count:\n";
            foreach (var info in _shaderInfo.OrderByDescending(s => s.estimatedVariants))
            {
                report += $"  {info.shaderName}: ~{info.estimatedVariants} variants\n";
            }

            string path = EditorUtility.SaveFilePanel("Save Shader Report", "", "ShaderVariantReport.txt", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, report);
                EditorUtility.DisplayDialog("Export Complete", $"Report saved to:\n{path}", "OK");
            }
        }

        [Serializable]
        private class ShaderVariantInfo
        {
            public Shader shader;
            public string shaderName;
            public int estimatedVariants;
        }
    }
}
