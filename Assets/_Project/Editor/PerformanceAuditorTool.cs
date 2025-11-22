using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Advanced Performance Auditor Tool for Unity projects
    /// Automatically scans codebase for performance anti-patterns and provides fixes
    /// </summary>
    public class PerformanceAuditorTool : EditorWindow
    {
        #region Fields

        private Vector2 scrollPosition;
        private List<PerformanceIssue> detectedIssues = new List<PerformanceIssue>();
        private bool isScanning = false;
        private int totalFilesScanned = 0;
        private int issuesFound = 0;

        // Performance pattern detection
        private readonly string[] updateMethodPatterns = {
            @"void\s+Update\s*\(\s*\)",
            @"void\s+FixedUpdate\s*\(\s*\)",
            @"void\s+LateUpdate\s*\(\s*\)"
        };

        private readonly PerformancePattern[] performancePatterns = {
            new PerformancePattern("Physics calls in Update", @"Physics\.\w+\(.*\)", Severity.High, "Cache physics results or reduce frequency"),
            new PerformancePattern("FindObjectOfType in Update", @"FindObjectOfType.*\(", Severity.Critical, "Use singleton pattern or cache references"),
            new PerformancePattern("GameObject.Find in Update", @"GameObject\.Find.*\(", Severity.Critical, "Cache GameObject references in Awake/Start"),
            new PerformancePattern("GetComponent in Update", @"GetComponent.*\(", Severity.Medium, "Cache component references"),
            new PerformancePattern("LINQ in Update", @"\.\s*(Where|Select|OrderBy|ToList)\s*\(", Severity.High, "Use for loops or pre-computed collections"),
            new PerformancePattern("String concatenation", @""".*""\s*\+\s*", Severity.Medium, "Use StringBuilder or string interpolation"),
            new PerformancePattern("Boxing in ToString", @"\.ToString\(\)", Severity.Low, "Consider caching toString results"),
            new PerformancePattern("Instantiate without pooling", @"Instantiate\s*\((?!.*pool)", Severity.Medium, "Implement object pooling for frequently spawned objects"),
            new PerformancePattern("Allocating collections in Update", @"new\s+(List|Dictionary|Array)", Severity.High, "Pre-allocate collections outside Update methods")
        };

        #endregion

        #region Unity Editor Window

        [MenuItem("🧪 Laboratory/Performance/Performance Auditor")]
        public static void ShowWindow()
        {
            GetWindow<PerformanceAuditorTool>("Performance Auditor");
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("🚀 Performance Auditor", "Automated performance issue detection");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Performance Auditor Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawScanSection();
            EditorGUILayout.Space();
            DrawResultsSection();
        }

        #endregion

        #region GUI Sections

        private void DrawScanSection()
        {
            EditorGUILayout.LabelField("📊 Scan Options", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(isScanning);
            {
                if (GUILayout.Button("🔍 Scan All Scripts", GUILayout.Height(40)))
                {
                    ScanAllScripts();
                }

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("🎯 Quick Scan (Update methods only)"))
                    {
                        ScanUpdateMethods();
                    }

                    if (GUILayout.Button("🧹 Clear Results"))
                    {
                        ClearResults();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();

            if (isScanning)
            {
                EditorGUILayout.LabelField($"⏳ Scanning... {totalFilesScanned} files processed", GUI.skin.box);
                EditorUtility.DisplayProgressBar("Performance Scan", $"Processing files...", 0.5f);
            }
        }

        private void DrawResultsSection()
        {
            if (detectedIssues.Count == 0)
            {
                EditorGUILayout.LabelField("✅ No performance issues detected or scan not run yet.", GUI.skin.box);
                return;
            }

            EditorGUILayout.LabelField($"⚠️ Found {detectedIssues.Count} Performance Issues", EditorStyles.boldLabel);

            // Summary by severity
            var criticalCount = detectedIssues.Count(i => i.Severity == Severity.Critical);
            var highCount = detectedIssues.Count(i => i.Severity == Severity.High);
            var mediumCount = detectedIssues.Count(i => i.Severity == Severity.Medium);
            var lowCount = detectedIssues.Count(i => i.Severity == Severity.Low);

            EditorGUILayout.LabelField($"🔴 Critical: {criticalCount} | 🟠 High: {highCount} | 🟡 Medium: {mediumCount} | 🟢 Low: {lowCount}");
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                foreach (var issue in detectedIssues.OrderBy(i => i.Severity))
                {
                    DrawIssue(issue);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawIssue(PerformanceIssue issue)
        {
            var bgColor = GetSeverityColor(issue.Severity);
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                GUI.backgroundColor = originalColor;

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"{GetSeverityIcon(issue.Severity)} {issue.Pattern.Name}", EditorStyles.boldLabel);

                    if (GUILayout.Button("📂 Open File", GUILayout.Width(80)))
                    {
                        OpenScriptAtLine(issue.FilePath, issue.LineNumber);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"File: {Path.GetFileName(issue.FilePath)}:{issue.LineNumber}");
                EditorGUILayout.LabelField($"Code: {issue.CodeSnippet}", GUI.skin.label);
                EditorGUILayout.LabelField($"💡 Fix: {issue.Pattern.Suggestion}", GUI.skin.label);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(2);
        }

        #endregion

        #region Scanning Logic

        private void ScanAllScripts()
        {
            isScanning = true;
            detectedIssues.Clear();
            totalFilesScanned = 0;

            try
            {
                var scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\Editor\\") || f.Contains("Laboratory\\Assets\\Editor\\")) // Include our editor tools
                    .ToArray();

                foreach (var file in scriptFiles)
                {
                    ScanFile(file);
                    totalFilesScanned++;
                }

                issuesFound = detectedIssues.Count;
                Debug.Log($"🔍 Performance scan completed: {issuesFound} issues found in {totalFilesScanned} files");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Performance scan failed: {e.Message}");
            }
            finally
            {
                isScanning = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private void ScanUpdateMethods()
        {
            isScanning = true;
            detectedIssues.Clear();

            try
            {
                var scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains("\\Editor\\"))
                    .ToArray();

                foreach (var file in scriptFiles)
                {
                    ScanFileForUpdateMethods(file);
                    totalFilesScanned++;
                }

                Debug.Log($"🎯 Quick scan completed: {detectedIssues.Count} issues found in Update methods");
            }
            finally
            {
                isScanning = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private void ScanFile(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                foreach (var pattern in performancePatterns)
                {
                    if (Regex.IsMatch(line, pattern.Regex, RegexOptions.IgnoreCase))
                    {
                        detectedIssues.Add(new PerformanceIssue
                        {
                            Pattern = pattern,
                            FilePath = filePath,
                            LineNumber = i + 1,
                            CodeSnippet = line.Length > 80 ? line.Substring(0, 80) + "..." : line,
                            Severity = pattern.Severity
                        });
                    }
                }
            }
        }

        private void ScanFileForUpdateMethods(string filePath)
        {
            var content = File.ReadAllText(filePath);

            // Find Update methods
            foreach (var updatePattern in updateMethodPatterns)
            {
                var matches = Regex.Matches(content, updatePattern + @"[\s\S]*?(?=^\s*})", RegexOptions.Multiline);

                foreach (Match match in matches)
                {
                    var updateMethodContent = match.Value;
                    var lines = updateMethodContent.Split('\n');
                    var startLine = content.Substring(0, match.Index).Count(c => c == '\n');

                    // Scan within Update method
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        foreach (var pattern in performancePatterns)
                        {
                            if (Regex.IsMatch(line, pattern.Regex, RegexOptions.IgnoreCase))
                            {
                                detectedIssues.Add(new PerformanceIssue
                                {
                                    Pattern = pattern,
                                    FilePath = filePath,
                                    LineNumber = startLine + i + 1,
                                    CodeSnippet = line.Length > 80 ? line.Substring(0, 80) + "..." : line,
                                    Severity = pattern.Severity
                                });
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        private void ClearResults()
        {
            detectedIssues.Clear();
            totalFilesScanned = 0;
            issuesFound = 0;
            Repaint();
        }

        private void OpenScriptAtLine(string filePath, int lineNumber)
        {
            var relativePath = "Assets" + filePath.Substring(Application.dataPath.Length);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(relativePath);

            if (script != null)
            {
                AssetDatabase.OpenAsset(script, lineNumber);
            }
        }

        private Color GetSeverityColor(Severity severity)
        {
            return severity switch
            {
                Severity.Critical => new Color(1f, 0.3f, 0.3f, 0.3f),
                Severity.High => new Color(1f, 0.6f, 0.3f, 0.3f),
                Severity.Medium => new Color(1f, 1f, 0.3f, 0.3f),
                Severity.Low => new Color(0.3f, 1f, 0.3f, 0.3f),
                _ => Color.white
            };
        }

        private string GetSeverityIcon(Severity severity)
        {
            return severity switch
            {
                Severity.Critical => "🔴",
                Severity.High => "🟠",
                Severity.Medium => "🟡",
                Severity.Low => "🟢",
                _ => "⚪"
            };
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class PerformancePattern
    {
        public string Name;
        public string Regex;
        public Severity Severity;
        public string Suggestion;

        public PerformancePattern(string name, string regex, Severity severity, string suggestion)
        {
            Name = name;
            Regex = regex;
            Severity = severity;
            Suggestion = suggestion;
        }
    }

    [System.Serializable]
    public class PerformanceIssue
    {
        public PerformancePattern Pattern;
        public string FilePath;
        public int LineNumber;
        public string CodeSnippet;
        public Severity Severity;
    }

    public enum Severity
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    #endregion
}