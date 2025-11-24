using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Laboratory.Editor
{
    /// <summary>
    /// Tool to help migrate Resources.Load code to AssetService.LoadAssetAsync.
    /// Scans C# files and provides migration suggestions.
    /// </summary>
    public class ResourcesCodeMigrationTool : EditorWindow
    {
        private class ResourcesUsage
        {
            public string FilePath;
            public int LineNumber;
            public string Line;
            public string Pattern;
            public string Suggestion;
        }

        private Vector2 _scrollPosition;
        private List<ResourcesUsage> _usages = new List<ResourcesUsage>();
        private bool _scanComplete = false;
        private bool _includeComments = false;

        [MenuItem("Tools/Chimera/Resources Code Migration Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<ResourcesCodeMigrationTool>("Resources Code Migration");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Resources.Load Code Migration Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool scans your C# scripts for Resources.Load usage and provides migration suggestions.\n" +
                "It will help you convert to AssetService.LoadAssetAsync pattern.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            _includeComments = EditorGUILayout.Toggle("Include Commented Code", _includeComments);

            if (GUILayout.Button("Scan C# Scripts for Resources.Load", GUILayout.Height(30)))
            {
                ScanForResourcesUsage();
            }

            EditorGUILayout.Space(10);

            if (_scanComplete)
            {
                EditorGUILayout.LabelField($"Found {_usages.Count} Resources.Load usages", EditorStyles.boldLabel);

                if (_usages.Count > 0)
                {
                    if (GUILayout.Button("Export Migration Report", GUILayout.Height(25)))
                    {
                        ExportMigrationReport();
                    }

                    if (GUILayout.Button("Copy All Suggestions to Clipboard", GUILayout.Height(25)))
                    {
                        CopyAllSuggestionsToClipboard();
                    }

                    EditorGUILayout.Space(5);

                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                    foreach (var usage in _usages)
                    {
                        DrawUsageBox(usage);
                    }

                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.HelpBox("No Resources.Load usage found! Your code is already migrated or uses a different loading pattern.", MessageType.Info);
                }
            }
        }

        private void DrawUsageBox(ResourcesUsage usage)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            // File and line number (clickable)
            EditorGUILayout.BeginHorizontal();
            var linkStyle = new GUIStyle(GUI.skin.label);
            linkStyle.normal.textColor = new Color(0.3f, 0.5f, 1f);
            linkStyle.hover.textColor = new Color(0.4f, 0.6f, 1f);
            if (GUILayout.Button($"{Path.GetFileName(usage.FilePath)}:{usage.LineNumber}", linkStyle))
            {
                OpenScriptAtLine(usage.FilePath, usage.LineNumber);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Pattern: {usage.Pattern}", GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            // Original code
            EditorGUILayout.LabelField("Original:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(usage.Line, GUILayout.Height(20));

            // Suggestion
            EditorGUILayout.LabelField("Suggested:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(usage.Suggestion, GUILayout.Height(20));

            if (GUILayout.Button("Copy Suggestion", GUILayout.Height(20)))
            {
                EditorGUIUtility.systemCopyBuffer = usage.Suggestion;
                Debug.Log($"Copied suggestion to clipboard: {usage.Suggestion}");
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void ScanForResourcesUsage()
        {
            _usages.Clear();

            var scriptFiles = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("/Editor/") || f.Contains("AddressablesMigration")) // Include most scripts
                .ToList();

            var patterns = new[]
            {
                new { Regex = new Regex(@"Resources\.Load\s*<\s*(\w+)\s*>\s*\(\s*""([^""]+)""\s*\)"), Type = "Resources.Load<T>" },
                new { Regex = new Regex(@"Resources\.Load\s*<\s*(\w+)\s*>\s*\(\s*([^)]+)\s*\)"), Type = "Resources.Load<T>" },
                new { Regex = new Regex(@"Resources\.LoadAll\s*<\s*(\w+)\s*>\s*\(\s*""([^""]+)""\s*\)"), Type = "Resources.LoadAll<T>" },
                new { Regex = new Regex(@"Resources\.LoadAll\s*<\s*(\w+)\s*>\s*\(\s*([^)]+)\s*\)"), Type = "Resources.LoadAll<T>" },
                new { Regex = new Regex(@"Resources\.LoadAsync\s*<\s*(\w+)\s*>\s*\(\s*""([^""]+)""\s*\)"), Type = "Resources.LoadAsync<T>" },
                new { Regex = new Regex(@"Resources\.LoadAsync\s*<\s*(\w+)\s*>\s*\(\s*([^)]+)\s*\)"), Type = "Resources.LoadAsync<T>" }
            };

            foreach (var scriptFile in scriptFiles)
            {
                try
                {
                    var lines = File.ReadAllLines(scriptFile);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];

                        // Skip comments unless requested
                        if (!_includeComments && line.TrimStart().StartsWith("//"))
                            continue;

                        foreach (var pattern in patterns)
                        {
                            var match = pattern.Regex.Match(line);
                            if (match.Success)
                            {
                                var suggestion = GenerateSuggestion(line, pattern.Type, match);
                                _usages.Add(new ResourcesUsage
                                {
                                    FilePath = scriptFile,
                                    LineNumber = i + 1,
                                    Line = line.Trim(),
                                    Pattern = pattern.Type,
                                    Suggestion = suggestion
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to scan {scriptFile}: {ex.Message}");
                }
            }

            _scanComplete = true;
            Debug.Log($"[Resources Migration] Found {_usages.Count} Resources.Load usages across {scriptFiles.Count} scripts");
        }

        private string GenerateSuggestion(string originalLine, string patternType, Match match)
        {
            var indentation = originalLine.Length - originalLine.TrimStart().Length;
            var indent = new string(' ', indentation);

            if (patternType == "Resources.Load<T>" || patternType == "Resources.LoadAsync<T>")
            {
                // Extract type and path
                var typeMatch = Regex.Match(originalLine, @"Resources\.LoadAsync?\s*<\s*(\w+)\s*>");
                var pathMatch = Regex.Match(originalLine, @"\(\s*([^)]+)\s*\)");

                if (typeMatch.Success && pathMatch.Success)
                {
                    var type = typeMatch.Groups[1].Value;
                    var path = pathMatch.Groups[1].Value;

                    // Generate async suggestion
                    var varName = originalLine.Contains("var ") ? "" : "var ";
                    return $"{indent}{varName}asset = await _assetService.LoadAssetAsync<{type}>({path});";
                }
            }
            else if (patternType == "Resources.LoadAll<T>")
            {
                // LoadAll requires different approach
                var typeMatch = Regex.Match(originalLine, @"Resources\.LoadAll\s*<\s*(\w+)\s*>");
                var pathMatch = Regex.Match(originalLine, @"\(\s*([^)]+)\s*\)");

                if (typeMatch.Success && pathMatch.Success)
                {
                    var type = typeMatch.Groups[1].Value;
                    var path = pathMatch.Groups[1].Value;

                    return $"{indent}// TODO: LoadAll pattern requires manual migration\n" +
                           $"{indent}// Option 1: Load by Addressables label - await Addressables.LoadAssetsAsync<{type}>({path}, null);\n" +
                           $"{indent}// Option 2: Define explicit list of keys and use _assetService.LoadAssetsAsync(keys);";
                }
            }

            return $"{indent}// TODO: Manual migration required for this pattern";
        }

        private void OpenScriptAtLine(string filePath, int lineNumber)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(filePath);
            if (script != null)
            {
                AssetDatabase.OpenAsset(script, lineNumber);
            }
            else
            {
                Debug.LogWarning($"Could not open script: {filePath}");
            }
        }

        private void ExportMigrationReport()
        {
            var reportPath = "Assets/_Project/Docs/RESOURCES_CODE_MIGRATION_REPORT.md";
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));

            var report = GenerateFullReport();
            File.WriteAllText(reportPath, report);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Report Exported", $"Code migration report saved to:\n{reportPath}", "OK");

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(reportPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }

            Debug.Log($"[Resources Migration] Report exported: {reportPath}");
        }

        private void CopyAllSuggestionsToClipboard()
        {
            var suggestions = string.Join("\n\n", _usages.Select(u =>
                $"// File: {u.FilePath}:{u.LineNumber}\n" +
                $"// Original: {u.Line}\n" +
                $"{u.Suggestion}"
            ));

            EditorGUIUtility.systemCopyBuffer = suggestions;
            Debug.Log($"Copied {_usages.Count} suggestions to clipboard");
            EditorUtility.DisplayDialog("Copied", $"Copied {_usages.Count} migration suggestions to clipboard", "OK");
        }

        private string GenerateFullReport()
        {
            var report = "# Resources.Load Code Migration Report\n\n";
            report += $"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            report += $"**Total Usages Found:** {_usages.Count}\n\n";

            // Group by file
            var groupedByFile = _usages.GroupBy(u => u.FilePath).OrderBy(g => g.Key);

            report += "## Summary by File\n\n";
            report += "| File | Count |\n";
            report += "|------|-------|\n";
            foreach (var fileGroup in groupedByFile)
            {
                report += $"| {Path.GetFileName(fileGroup.Key)} | {fileGroup.Count()} |\n";
            }
            report += "\n";

            // Group by pattern
            var groupedByPattern = _usages.GroupBy(u => u.Pattern);
            report += "## Summary by Pattern\n\n";
            report += "| Pattern | Count |\n";
            report += "|---------|-------|\n";
            foreach (var patternGroup in groupedByPattern)
            {
                report += $"| {patternGroup.Key} | {patternGroup.Count()} |\n";
            }
            report += "\n";

            report += "## Migration Instructions\n\n";
            report += "### Step 1: Add AssetService Dependency\n\n";
            report += "```csharp\n";
            report += "using Laboratory.Core.Services;\n";
            report += "using Cysharp.Threading.Tasks;\n\n";
            report += "public class YourClass : MonoBehaviour\n";
            report += "{\n";
            report += "    [Inject] private IAssetService _assetService; // Using VContainer\n";
            report += "    \n";
            report += "    // Or manually in Awake/Start:\n";
            report += "    // _assetService = ServiceLocator.Get<IAssetService>();\n";
            report += "}\n";
            report += "```\n\n";

            report += "### Step 2: Convert Methods to Async\n\n";
            report += "```csharp\n";
            report += "// Before:\n";
            report += "void LoadAsset()\n";
            report += "{\n";
            report += "    var prefab = Resources.Load<GameObject>(\"Prefabs/MyPrefab\");\n";
            report += "}\n\n";
            report += "// After:\n";
            report += "async UniTask LoadAsset()\n";
            report += "{\n";
            report += "    var prefab = await _assetService.LoadAssetAsync<GameObject>(\"Prefabs/MyPrefab\");\n";
            report += "}\n";
            report += "```\n\n";

            report += "### Step 3: Handle Null Checks\n\n";
            report += "```csharp\n";
            report += "var asset = await _assetService.LoadAssetAsync<GameObject>(\"MyAsset\");\n";
            report += "if (asset == null)\n";
            report += "{\n";
            report += "    Debug.LogError(\"Failed to load asset: MyAsset\");\n";
            report += "    return;\n";
            report += "}\n";
            report += "// Use asset...\n";
            report += "```\n\n";

            report += "## Detailed Migrations\n\n";

            foreach (var fileGroup in groupedByFile)
            {
                report += $"### {Path.GetFileName(fileGroup.Key)}\n\n";
                report += $"**Path:** `{fileGroup.Key}`\n\n";

                foreach (var usage in fileGroup.OrderBy(u => u.LineNumber))
                {
                    report += $"#### Line {usage.LineNumber}\n\n";
                    report += "**Original:**\n";
                    report += "```csharp\n";
                    report += usage.Line + "\n";
                    report += "```\n\n";
                    report += "**Suggested:**\n";
                    report += "```csharp\n";
                    report += usage.Suggestion + "\n";
                    report += "```\n\n";
                }
            }

            report += "## Special Cases\n\n";
            report += "### Resources.LoadAll Pattern\n\n";
            report += "Resources.LoadAll requires manual migration as there's no direct equivalent.\n\n";
            report += "**Option 1: Use Addressables Labels**\n";
            report += "```csharp\n";
            report += "// Mark assets with label \"Items\" in Addressables Groups window\n";
            report += "var items = await Addressables.LoadAssetsAsync<ItemData>(\"Items\", null);\n";
            report += "```\n\n";
            report += "**Option 2: Load Explicit List**\n";
            report += "```csharp\n";
            report += "var keys = new[] { \"Items/Sword\", \"Items/Shield\", \"Items/Potion\" };\n";
            report += "await _assetService.LoadAssetsAsync(keys);\n";
            report += "// Then retrieve from cache:\n";
            report += "var sword = _assetService.GetCachedAsset<ItemData>(\"Items/Sword\");\n";
            report += "```\n\n";

            return report;
        }
    }
}
