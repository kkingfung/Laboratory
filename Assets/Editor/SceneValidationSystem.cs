using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Advanced Scene Validation & Quality Assurance System
    /// FEATURES: Automated scene checks, missing reference detection, performance validation
    /// PURPOSE: Ensure scene quality and catch issues before they reach production
    /// </summary>
    public class SceneValidationSystem : EditorWindow
    {
        #region Fields

        private Vector2 scrollPosition;
        private List<ValidationResult> validationResults = new List<ValidationResult>();
        private List<ValidationRule> activeRules = new List<ValidationRule>();
        private bool isValidating = false;
        private int totalChecks = 0;
        private int completedChecks = 0;

        // Validation categories
        private bool validateMissingReferences = true;
        private bool validatePerformance = true;
        private bool validateNaming = true;
        private bool validateComponents = true;
        private bool validateLighting = true;
        private bool validateAudio = true;

        // Performance thresholds
        private int maxRenderers = 1000;
        private int maxLights = 8;
        private int maxAudioSources = 20;
        private float maxDrawCalls = 500;

        // Naming conventions
        private bool enforceNamingConventions = true;
        private string[] forbiddenObjectNames = { "GameObject", "Cube", "Sphere", "Capsule" };

        #endregion

        #region Unity Editor Window

        [MenuItem("🧪 Laboratory/Quality/Scene Validation")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneValidationSystem>("Scene Validator");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("🔍 Scene Validator", "Scene quality assurance and validation");
            InitializeValidationRules();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Scene Validation System", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawValidationControls();
            EditorGUILayout.Space();

            DrawValidationSettings();
            EditorGUILayout.Space();

            if (isValidating)
            {
                DrawValidationProgress();
            }
            else if (validationResults.Count > 0)
            {
                DrawValidationResults();
            }
            else
            {
                EditorGUILayout.HelpBox("Run validation to check current scene quality", MessageType.Info);
            }
        }

        #endregion

        #region GUI Sections

        private void DrawValidationControls()
        {
            EditorGUILayout.LabelField("🔧 Validation Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("🔍 Validate Current Scene", GUILayout.Height(30)))
                {
                    ValidateCurrentScene();
                }

                if (GUILayout.Button("🌍 Validate All Scenes", GUILayout.Height(30)))
                {
                    ValidateAllScenes();
                }

                if (GUILayout.Button("🧹 Clear Results", GUILayout.Height(30)))
                {
                    ClearResults();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("📊 Generate Report"))
                {
                    GenerateValidationReport();
                }

                if (GUILayout.Button("🛠️ Auto-Fix Issues"))
                {
                    AutoFixIssues();
                }

                if (GUILayout.Button("⚙️ Export Settings"))
                {
                    ExportValidationSettings();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawValidationSettings()
        {
            EditorGUILayout.LabelField("⚙️ Validation Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                EditorGUILayout.LabelField("Validation Categories:", EditorStyles.boldLabel);

                validateMissingReferences = EditorGUILayout.Toggle("Missing References", validateMissingReferences);
                validatePerformance = EditorGUILayout.Toggle("Performance Issues", validatePerformance);
                validateNaming = EditorGUILayout.Toggle("Naming Conventions", validateNaming);
                validateComponents = EditorGUILayout.Toggle("Component Validation", validateComponents);
                validateLighting = EditorGUILayout.Toggle("Lighting Setup", validateLighting);
                validateAudio = EditorGUILayout.Toggle("Audio Configuration", validateAudio);
            }
            EditorGUILayout.EndVertical();

            if (validatePerformance)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    EditorGUILayout.LabelField("Performance Thresholds:", EditorStyles.boldLabel);
                    maxRenderers = EditorGUILayout.IntField("Max Renderers:", maxRenderers);
                    maxLights = EditorGUILayout.IntField("Max Lights:", maxLights);
                    maxAudioSources = EditorGUILayout.IntField("Max Audio Sources:", maxAudioSources);
                    maxDrawCalls = EditorGUILayout.FloatField("Max Draw Calls:", maxDrawCalls);
                }
                EditorGUILayout.EndVertical();
            }

            if (validateNaming)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    EditorGUILayout.LabelField("Naming Conventions:", EditorStyles.boldLabel);
                    enforceNamingConventions = EditorGUILayout.Toggle("Enforce Conventions", enforceNamingConventions);

                    if (enforceNamingConventions)
                    {
                        EditorGUILayout.LabelField("Forbidden Names:");
                        for (int i = 0; i < forbiddenObjectNames.Length; i++)
                        {
                            forbiddenObjectNames[i] = EditorGUILayout.TextField($"  {i + 1}:", forbiddenObjectNames[i]);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawValidationProgress()
        {
            EditorGUILayout.LabelField("🔄 Validation Progress", EditorStyles.boldLabel);

            var progress = totalChecks > 0 ? (float)completedChecks / totalChecks : 0f;
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"{completedChecks}/{totalChecks} checks");

            EditorGUILayout.LabelField($"Validating scene...", GUI.skin.box);
        }

        private void DrawValidationResults()
        {
            EditorGUILayout.LabelField("📋 Validation Results", EditorStyles.boldLabel);

            var errorCount = validationResults.Count(r => r.severity == ValidationSeverity.Error);
            var warningCount = validationResults.Count(r => r.severity == ValidationSeverity.Warning);
            var infoCount = validationResults.Count(r => r.severity == ValidationSeverity.Info);

            EditorGUILayout.BeginHorizontal();
            {
                if (errorCount > 0)
                {
                    EditorGUILayout.LabelField($"🔴 Errors: {errorCount}", GUILayout.Width(100));
                }
                if (warningCount > 0)
                {
                    EditorGUILayout.LabelField($"🟡 Warnings: {warningCount}", GUILayout.Width(120));
                }
                if (infoCount > 0)
                {
                    EditorGUILayout.LabelField($"🔵 Info: {infoCount}", GUILayout.Width(100));
                }

                GUILayout.FlexibleSpace();

                if (errorCount == 0 && warningCount == 0)
                {
                    EditorGUILayout.LabelField("✅ Scene validation passed!", EditorStyles.boldLabel);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            {
                foreach (var result in validationResults.OrderByDescending(r => r.severity))
                {
                    DrawValidationResult(result);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawValidationResult(ValidationResult result)
        {
            var resultColor = GetSeverityColor(result.severity);
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = resultColor;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                GUI.backgroundColor = originalColor;

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"{GetSeverityIcon(result.severity)} {result.category}", EditorStyles.boldLabel);

                    if (result.target != null && GUILayout.Button("🎯 Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = result.target;
                        EditorGUIUtility.PingObject(result.target);
                    }

                    if (result.canAutoFix && GUILayout.Button("🛠️ Fix", GUILayout.Width(50)))
                    {
                        AutoFixResult(result);
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField(result.message);

                if (!string.IsNullOrEmpty(result.suggestion))
                {
                    EditorGUILayout.LabelField($"💡 Suggestion: {result.suggestion}");
                }

                if (result.target != null)
                {
                    EditorGUILayout.LabelField($"🎯 Object: {GetObjectPath(result.target)}");
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(2);
        }

        #endregion

        #region Validation Logic

        private void InitializeValidationRules()
        {
            activeRules.Clear();

            // Missing reference rules
            activeRules.Add(new ValidationRule("Missing Script References", ValidateMissingScripts));
            activeRules.Add(new ValidationRule("Missing Component References", ValidateMissingComponentReferences));

            // Performance rules
            activeRules.Add(new ValidationRule("Renderer Performance", ValidateRendererPerformance));
            activeRules.Add(new ValidationRule("Lighting Performance", ValidateLightingPerformance));
            activeRules.Add(new ValidationRule("Audio Performance", ValidateAudioPerformance));

            // Naming convention rules
            activeRules.Add(new ValidationRule("Object Naming", ValidateObjectNaming));
            activeRules.Add(new ValidationRule("Asset Naming", ValidateAssetNaming));

            // Component validation rules
            activeRules.Add(new ValidationRule("Collider Validation", ValidateColliders));
            activeRules.Add(new ValidationRule("Rigidbody Validation", ValidateRigidbodies));
            activeRules.Add(new ValidationRule("Canvas Validation", ValidateCanvases));

            // Scene setup rules
            activeRules.Add(new ValidationRule("Camera Setup", ValidateCameraSetup));
            activeRules.Add(new ValidationRule("Lighting Setup", ValidateLightingSetup));
        }

        private void ValidateCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            ValidateScene(scene);
        }

        private void ValidateAllScenes()
        {
            var scenePaths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path);

            foreach (var scenePath in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                ValidateScene(scene);
            }
        }

        private void ValidateScene(Scene scene)
        {
            isValidating = true;
            validationResults.Clear();
            completedChecks = 0;
            totalChecks = activeRules.Count;

            try
            {
                foreach (var rule in activeRules)
                {
                    if (ShouldRunRule(rule))
                    {
                        rule.validate();
                        completedChecks++;
                        Repaint();
                    }
                }

                Debug.Log($"Scene validation completed: {validationResults.Count} issues found in {scene.name}");
            }
            finally
            {
                isValidating = false;
                Repaint();
            }
        }

        private bool ShouldRunRule(ValidationRule rule)
        {
            return rule.ruleName switch
            {
                "Missing Script References" or "Missing Component References" => validateMissingReferences,
                "Renderer Performance" or "Lighting Performance" or "Audio Performance" => validatePerformance,
                "Object Naming" or "Asset Naming" => validateNaming,
                "Collider Validation" or "Rigidbody Validation" or "Canvas Validation" => validateComponents,
                "Camera Setup" or "Lighting Setup" => validateLighting,
                _ => true
            };
        }

        #endregion

        #region Validation Rules

        private void ValidateMissingScripts()
        {
            var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var go in allGameObjects)
            {
                var components = go.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        validationResults.Add(new ValidationResult
                        {
                            category = "Missing Scripts",
                            severity = ValidationSeverity.Error,
                            message = $"Missing script reference on GameObject '{go.name}'",
                            suggestion = "Remove the missing script component or reassign the script",
                            target = go,
                            canAutoFix = true
                        });
                    }
                }
            }
        }

        private void ValidateMissingComponentReferences()
        {
            var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            foreach (var component in allComponents)
            {
                if (component == null) continue;

                var serializedObject = new SerializedObject(component);
                var property = serializedObject.GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue == null &&
                        property.hasChildren == false)
                    {
                        validationResults.Add(new ValidationResult
                        {
                            category = "Missing References",
                            severity = ValidationSeverity.Warning,
                            message = $"Null reference '{property.displayName}' in component '{component.GetType().Name}' on '{component.gameObject.name}'",
                            suggestion = "Assign a valid reference or make the field optional",
                            target = component.gameObject
                        });
                    }
                }
            }
        }

        private void ValidateRendererPerformance()
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            if (renderers.Length > maxRenderers)
            {
                validationResults.Add(new ValidationResult
                {
                    category = "Performance",
                    severity = ValidationSeverity.Warning,
                    message = $"High renderer count: {renderers.Length} (threshold: {maxRenderers})",
                    suggestion = "Consider LOD groups, object pooling, or static batching"
                });
            }

            // Check for disabled renderers
            var disabledRenderers = renderers.Where(r => !r.enabled).ToList();
            if (disabledRenderers.Count > 10)
            {
                validationResults.Add(new ValidationResult
                {
                    category = "Performance",
                    severity = ValidationSeverity.Info,
                    message = $"{disabledRenderers.Count} disabled renderers found",
                    suggestion = "Consider removing unused renderer components"
                });
            }
        }

        private void ValidateLightingPerformance()
        {
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

            if (lights.Length > maxLights)
            {
                validationResults.Add(new ValidationResult
                {
                    category = "Lighting",
                    severity = ValidationSeverity.Warning,
                    message = $"High light count: {lights.Length} (threshold: {maxLights})",
                    suggestion = "Use baked lighting or light probes for static lights"
                });
            }

            // Check for real-time lights
            var realtimeLights = lights.Where(l => l.lightmapBakeType == LightmapBakeType.Realtime).ToList();
            if (realtimeLights.Count > 4)
            {
                validationResults.Add(new ValidationResult
                {
                    category = "Lighting",
                    severity = ValidationSeverity.Warning,
                    message = $"{realtimeLights.Count} realtime lights may impact performance",
                    suggestion = "Consider baking some lights to reduce runtime cost"
                });
            }
        }

        private void ValidateAudioPerformance()
        {
            var audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

            if (audioSources.Length > maxAudioSources)
            {
                validationResults.Add(new ValidationResult
                {
                    category = "Audio",
                    severity = ValidationSeverity.Warning,
                    message = $"High AudioSource count: {audioSources.Length} (threshold: {maxAudioSources})",
                    suggestion = "Use audio pooling or reduce concurrent audio sources"
                });
            }
        }

        private void ValidateObjectNaming()
        {
            if (!enforceNamingConventions) return;

            var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var go in allGameObjects)
            {
                if (forbiddenObjectNames.Contains(go.name))
                {
                    validationResults.Add(new ValidationResult
                    {
                        category = "Naming",
                        severity = ValidationSeverity.Warning,
                        message = $"GameObject has generic name: '{go.name}'",
                        suggestion = "Use descriptive names that indicate the object's purpose",
                        target = go,
                        canAutoFix = false
                    });
                }

                // Check for naming patterns
                if (Regex.IsMatch(go.name, @"^.*\(\d+\)$"))
                {
                    validationResults.Add(new ValidationResult
                    {
                        category = "Naming",
                        severity = ValidationSeverity.Info,
                        message = $"GameObject has duplicate name pattern: '{go.name}'",
                        suggestion = "Consider using unique, descriptive names",
                        target = go
                    });
                }
            }
        }

        private void ValidateAssetNaming()
        {
            // This would validate asset naming conventions in the project
            // Implementation depends on specific naming requirements
        }

        private void ValidateColliders()
        {
            var colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);

            foreach (var collider in colliders)
            {
                if (collider.isTrigger && collider.GetComponent<Rigidbody>() == null)
                {
                    var hasEventHandlers = collider.GetComponent<MonoBehaviour>() != null;
                    if (!hasEventHandlers)
                    {
                        validationResults.Add(new ValidationResult
                        {
                            category = "Components",
                            severity = ValidationSeverity.Info,
                            message = $"Trigger collider '{collider.name}' has no event handlers",
                            suggestion = "Add trigger event handling script or remove trigger",
                            target = collider.gameObject
                        });
                    }
                }
            }
        }

        private void ValidateRigidbodies()
        {
            var rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);

            foreach (var rb in rigidbodies)
            {
                if (rb.isKinematic && rb.GetComponent<Collider>() == null)
                {
                    validationResults.Add(new ValidationResult
                    {
                        category = "Physics",
                        severity = ValidationSeverity.Warning,
                        message = $"Kinematic Rigidbody '{rb.name}' has no collider",
                        suggestion = "Add a collider component or remove the Rigidbody",
                        target = rb.gameObject
                    });
                }
            }
        }

        private void ValidateCanvases()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

            foreach (var canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                {
                    validationResults.Add(new ValidationResult
                    {
                        category = "UI",
                        severity = ValidationSeverity.Warning,
                        message = $"WorldSpace Canvas '{canvas.name}' has no camera assigned",
                        suggestion = "Assign a camera to the World Camera field",
                        target = canvas.gameObject
                    });
                }
            }
        }

        private void ValidateCameraSetup()
        {
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

            if (cameras.Length == 0)
            {
                validationResults.Add(new ValidationResult
                {
                    category = "Scene Setup",
                    severity = ValidationSeverity.Error,
                    message = "No cameras found in scene",
                    suggestion = "Add at least one camera to the scene"
                });
            }
            else if (cameras.Count(c => c.enabled) > 1)
            {
                validationResults.Add(new ValidationResult
                {
                    category = "Scene Setup",
                    severity = ValidationSeverity.Info,
                    message = $"Multiple active cameras found: {cameras.Count(c => c.enabled)}",
                    suggestion = "Ensure camera priorities are set correctly"
                });
            }
        }

        private void ValidateLightingSetup()
        {
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

            if (lights.Length == 0)
            {
                validationResults.Add(new ValidationResult
                {
                    category = "Lighting",
                    severity = ValidationSeverity.Warning,
                    message = "No lights found in scene",
                    suggestion = "Add lighting or configure skybox/environment lighting"
                });
            }
        }

        #endregion

        #region Helper Methods

        private void ClearResults()
        {
            validationResults.Clear();
            Repaint();
        }

        private void AutoFixIssues()
        {
            var fixableIssues = validationResults.Where(r => r.canAutoFix).ToList();

            foreach (var issue in fixableIssues)
            {
                AutoFixResult(issue);
            }

            // Re-validate after fixes
            ValidateCurrentScene();
        }

        private void AutoFixResult(ValidationResult result)
        {
            switch (result.category)
            {
                case "Missing Scripts":
                    RemoveMissingScripts(result.target as GameObject);
                    break;
                default:
                    Debug.LogWarning($"Auto-fix not implemented for: {result.category}");
                    break;
            }
        }

        private void RemoveMissingScripts(GameObject target)
        {
            if (target == null) return;

            var components = target.GetComponents<Component>();
            for (int i = components.Length - 1; i >= 0; i--)
            {
                if (components[i] == null)
                {
                    DestroyImmediate(components[i], true);
                }
            }

            EditorUtility.SetDirty(target);
        }

        private void GenerateValidationReport()
        {
            if (validationResults.Count == 0)
            {
                Debug.LogWarning("No validation results to report");
                return;
            }

            var report = "=== Scene Validation Report ===\n\n";
            report += $"Scene: {SceneManager.GetActiveScene().name}\n";
            report += $"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            report += $"Total Issues: {validationResults.Count}\n\n";

            var groupedResults = validationResults.GroupBy(r => r.category);
            foreach (var group in groupedResults)
            {
                report += $"{group.Key}:\n";
                foreach (var result in group)
                {
                    report += $"  {GetSeverityIcon(result.severity)} {result.message}\n";
                }
                report += "\n";
            }

            Debug.Log(report);

            var filePath = $"Scene_Validation_Report_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            System.IO.File.WriteAllText(filePath, report);
            Debug.Log($"Validation report saved to: {filePath}");
        }

        private void ExportValidationSettings()
        {
            var settings = new ValidationSettings
            {
                validateMissingReferences = this.validateMissingReferences,
                validatePerformance = this.validatePerformance,
                validateNaming = this.validateNaming,
                validateComponents = this.validateComponents,
                validateLighting = this.validateLighting,
                validateAudio = this.validateAudio,
                maxRenderers = this.maxRenderers,
                maxLights = this.maxLights,
                maxAudioSources = this.maxAudioSources,
                forbiddenObjectNames = this.forbiddenObjectNames
            };

            var json = JsonUtility.ToJson(settings, true);
            var path = EditorUtility.SaveFilePanel("Export Validation Settings", "", "ValidationSettings", "json");

            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"Validation settings exported to: {path}");
            }
        }

        private Color GetSeverityColor(ValidationSeverity severity)
        {
            return severity switch
            {
                ValidationSeverity.Error => new Color(1f, 0.3f, 0.3f, 0.3f),
                ValidationSeverity.Warning => new Color(1f, 0.8f, 0.3f, 0.3f),
                ValidationSeverity.Info => new Color(0.3f, 0.8f, 1f, 0.3f),
                _ => Color.white
            };
        }

        private string GetSeverityIcon(ValidationSeverity severity)
        {
            return severity switch
            {
                ValidationSeverity.Error => "🔴",
                ValidationSeverity.Warning => "🟡",
                ValidationSeverity.Info => "🔵",
                _ => "⚪"
            };
        }

        private string GetObjectPath(Object target)
        {
            if (target is GameObject go)
            {
                var path = go.name;
                var parent = go.transform.parent;
                while (parent != null)
                {
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
                return path;
            }
            return target.name;
        }

        #endregion

        #region Data Structures

        [System.Serializable]
        public struct ValidationResult
        {
            public string category;
            public ValidationSeverity severity;
            public string message;
            public string suggestion;
            public Object target;
            public bool canAutoFix;
        }

        [System.Serializable]
        public struct ValidationRule
        {
            public string ruleName;
            public System.Action validate;

            public ValidationRule(string name, System.Action validateAction)
            {
                ruleName = name;
                validate = validateAction;
            }
        }

        [System.Serializable]
        public struct ValidationSettings
        {
            public bool validateMissingReferences;
            public bool validatePerformance;
            public bool validateNaming;
            public bool validateComponents;
            public bool validateLighting;
            public bool validateAudio;
            public int maxRenderers;
            public int maxLights;
            public int maxAudioSources;
            public string[] forbiddenObjectNames;
        }

        public enum ValidationSeverity
        {
            Info = 0,
            Warning = 1,
            Error = 2
        }

        #endregion
    }
}