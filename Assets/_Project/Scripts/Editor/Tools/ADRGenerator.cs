using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Architecture Decision Records (ADR) generator.
    /// Creates and manages architectural decision documentation.
    /// Follows the ADR template format for consistent decision tracking.
    /// </summary>
    public class ADRGenerator : EditorWindow
    {
        #region Window Setup

        [MenuItem("Chimera/Documentation/ADR Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ADRGenerator>("ADR Generator");
            window.minSize = new Vector2(600, 700);
            window.Show();
        }

        #endregion

        #region Private Fields

        private string _adrDirectory = "Documentation/ADRs";
        private int _nextAdrNumber = 1;

        // ADR fields
        private string _title = "";
        private string _context = "";
        private string _decision = "";
        private string _consequences = "";
        private ADRStatus _status = ADRStatus.Proposed;
        private string _deciders = "";
        private string _technicalStory = "";
        private string _alternatives = "";

        // UI
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private bool _stylesInitialized;

        #endregion

        #region Unity Lifecycle

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.LabelField("Architecture Decision Record Generator", _headerStyle);
            EditorGUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawConfiguration();
            EditorGUILayout.Space(20);

            DrawADRForm();
            EditorGUILayout.Space(20);

            DrawActions();

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Initialization

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 10)
            };

            _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(0, 0, 5, 5)
            };

            _stylesInitialized = true;

            // Find next ADR number
            UpdateNextAdrNumber();
        }

        private void UpdateNextAdrNumber()
        {
            string fullPath = Path.Combine(Application.dataPath, _adrDirectory);

            if (Directory.Exists(fullPath))
            {
                var files = Directory.GetFiles(fullPath, "*.md");
                _nextAdrNumber = files.Length + 1;
            }
        }

        #endregion

        #region GUI - Configuration

        private void DrawConfiguration()
        {
            EditorGUILayout.LabelField("Configuration", _sectionStyle);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField("ADR Directory:", _adrDirectory);
            EditorGUILayout.LabelField("Next ADR Number:", _nextAdrNumber.ToString("0000"));

            if (GUILayout.Button("Change Directory"))
            {
                string newPath = EditorUtility.OpenFolderPanel("Select ADR Directory", Application.dataPath, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    _adrDirectory = newPath.Replace(Application.dataPath, "").TrimStart('/');
                    UpdateNextAdrNumber();
                }
            }

            if (GUILayout.Button("Open ADR Directory"))
            {
                string fullPath = Path.Combine(Application.dataPath, _adrDirectory);
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
                EditorUtility.RevealInFinder(fullPath);
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region GUI - ADR Form

        private void DrawADRForm()
        {
            EditorGUILayout.LabelField("ADR Content", _sectionStyle);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Title
            EditorGUILayout.LabelField("Title *", EditorStyles.boldLabel);
            _title = EditorGUILayout.TextField(_title);
            EditorGUILayout.Space(5);

            // Status
            EditorGUILayout.LabelField("Status *", EditorStyles.boldLabel);
            _status = (ADRStatus)EditorGUILayout.EnumPopup(_status);
            EditorGUILayout.Space(5);

            // Deciders
            EditorGUILayout.LabelField("Deciders", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Who was involved in making this decision? (comma-separated)", MessageType.None);
            _deciders = EditorGUILayout.TextField(_deciders);
            EditorGUILayout.Space(5);

            // Technical Story
            EditorGUILayout.LabelField("Technical Story", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("What technical issue is this addressing?", MessageType.None);
            _technicalStory = EditorGUILayout.TextArea(_technicalStory, GUILayout.Height(60));
            EditorGUILayout.Space(5);

            // Context
            EditorGUILayout.LabelField("Context *", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("What is the issue we're facing? What forces are at play?", MessageType.None);
            _context = EditorGUILayout.TextArea(_context, GUILayout.Height(80));
            EditorGUILayout.Space(5);

            // Decision
            EditorGUILayout.LabelField("Decision *", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("What is the change we're proposing/making?", MessageType.None);
            _decision = EditorGUILayout.TextArea(_decision, GUILayout.Height(80));
            EditorGUILayout.Space(5);

            // Alternatives
            EditorGUILayout.LabelField("Alternatives Considered", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("What other options were considered and why were they rejected?", MessageType.None);
            _alternatives = EditorGUILayout.TextArea(_alternatives, GUILayout.Height(80));
            EditorGUILayout.Space(5);

            // Consequences
            EditorGUILayout.LabelField("Consequences *", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("What becomes easier/harder after this decision?", MessageType.None);
            _consequences = EditorGUILayout.TextArea(_consequences, GUILayout.Height(80));

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region GUI - Actions

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", _sectionStyle);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Generate ADR", GUILayout.Height(40)))
            {
                GenerateADR();
            }

            if (GUILayout.Button("Clear Form", GUILayout.Height(40)))
            {
                ClearForm();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("View Template"))
            {
                ShowTemplate();
            }

            if (GUILayout.Button("List ADRs"))
            {
                ListADRs();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region ADR Generation

        private void GenerateADR()
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_title))
            {
                EditorUtility.DisplayDialog("Validation Error", "Title is required", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(_context))
            {
                EditorUtility.DisplayDialog("Validation Error", "Context is required", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(_decision))
            {
                EditorUtility.DisplayDialog("Validation Error", "Decision is required", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(_consequences))
            {
                EditorUtility.DisplayDialog("Validation Error", "Consequences are required", "OK");
                return;
            }

            // Generate ADR content
            string adrContent = GenerateADRContent();

            // Create filename
            string filename = $"{_nextAdrNumber:0000}-{SanitizeFilename(_title)}.md";
            string fullPath = Path.Combine(Application.dataPath, _adrDirectory);
            string filePath = Path.Combine(fullPath, filename);

            // Create directory if needed
            Directory.CreateDirectory(fullPath);

            // Write file
            try
            {
                File.WriteAllText(filePath, adrContent);
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("ADR Generated",
                    $"ADR successfully created:\n{filename}",
                    "OK");

                // Clear form
                ClearForm();

                // Update next number
                _nextAdrNumber++;

                // Open file
                EditorUtility.RevealInFinder(filePath);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error",
                    $"Failed to create ADR:\n{ex.Message}",
                    "OK");
            }
        }

        private string GenerateADRContent()
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine($"# {_nextAdrNumber:0000}. {_title}");
            sb.AppendLine();

            // Date
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd}");
            sb.AppendLine();

            // Status
            sb.AppendLine($"## Status");
            sb.AppendLine();
            sb.AppendLine(_status.ToString());
            sb.AppendLine();

            // Deciders
            if (!string.IsNullOrWhiteSpace(_deciders))
            {
                sb.AppendLine($"## Deciders");
                sb.AppendLine();
                foreach (var decider in _deciders.Split(','))
                {
                    sb.AppendLine($"* {decider.Trim()}");
                }
                sb.AppendLine();
            }

            // Technical Story
            if (!string.IsNullOrWhiteSpace(_technicalStory))
            {
                sb.AppendLine($"## Technical Story");
                sb.AppendLine();
                sb.AppendLine(_technicalStory);
                sb.AppendLine();
            }

            // Context
            sb.AppendLine($"## Context and Problem Statement");
            sb.AppendLine();
            sb.AppendLine(_context);
            sb.AppendLine();

            // Alternatives
            if (!string.IsNullOrWhiteSpace(_alternatives))
            {
                sb.AppendLine($"## Considered Options");
                sb.AppendLine();
                sb.AppendLine(_alternatives);
                sb.AppendLine();
            }

            // Decision
            sb.AppendLine($"## Decision");
            sb.AppendLine();
            sb.AppendLine(_decision);
            sb.AppendLine();

            // Consequences
            sb.AppendLine($"## Consequences");
            sb.AppendLine();
            sb.AppendLine(_consequences);
            sb.AppendLine();

            // Links
            sb.AppendLine($"## Links");
            sb.AppendLine();
            sb.AppendLine("* [ADR Template](https://github.com/joelparkerhenderson/architecture-decision-record)");

            return sb.ToString();
        }

        private string SanitizeFilename(string filename)
        {
            // Replace spaces with hyphens and remove invalid characters
            filename = filename.Replace(" ", "-").ToLower();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c.ToString(), "");
            }
            return filename;
        }

        #endregion

        #region Helper Methods

        private void ClearForm()
        {
            _title = "";
            _context = "";
            _decision = "";
            _consequences = "";
            _deciders = "";
            _technicalStory = "";
            _alternatives = "";
            _status = ADRStatus.Proposed;
        }

        private void ShowTemplate()
        {
            string template = @"# [Number]. [Title]

Date: [YYYY-MM-DD]

## Status

[Proposed | Accepted | Deprecated | Superseded]

## Deciders

* [Person 1]
* [Person 2]

## Technical Story

[Short description of the technical issue]

## Context and Problem Statement

[Describe the context and problem statement, e.g., in free form using two to three sentences. You may want to articulate the problem in form of a question.]

## Considered Options

* [Option 1]
* [Option 2]
* [Option 3]

## Decision

[Chosen option]

## Consequences

### Positive

* [Positive consequence 1]
* [Positive consequence 2]

### Negative

* [Negative consequence 1]
* [Negative consequence 2]

## Links

* [Link type] [Link to ADR]";

            EditorUtility.DisplayDialog("ADR Template", template, "OK");
        }

        private void ListADRs()
        {
            string fullPath = Path.Combine(Application.dataPath, _adrDirectory);

            if (!Directory.Exists(fullPath))
            {
                EditorUtility.DisplayDialog("No ADRs", "No ADRs have been created yet.", "OK");
                return;
            }

            var files = Directory.GetFiles(fullPath, "*.md");

            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog("No ADRs", "No ADRs found in directory.", "OK");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Found {files.Length} ADRs:\n");

            foreach (var file in files)
            {
                sb.AppendLine(Path.GetFileName(file));
            }

            EditorUtility.DisplayDialog("ADR List", sb.ToString(), "OK");
        }

        #endregion

        #region Context Menu

        [MenuItem("Chimera/Documentation/Create Quick ADR")]
        private static void CreateQuickADR()
        {
            ShowWindow();
        }

        #endregion
    }

    /// <summary>
    /// ADR status types.
    /// </summary>
    public enum ADRStatus
    {
        Proposed,
        Accepted,
        Deprecated,
        Superseded
    }
}
