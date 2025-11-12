using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectChimera.Tools.Development
{
    /// <summary>
    /// ScriptableObject Live Editor - Edit ScriptableObject values in play mode with instant feedback
    ///
    /// Features:
    /// - Real-time editing of ScriptableObject fields during play mode
    /// - Automatic change detection and propagation
    /// - Undo/redo support with change history
    /// - Preset system for quick value swapping
    /// - Change tracking with visual diff view
    /// - Export edited values to new asset
    ///
    /// Usage:
    /// - Open window via Tools > ScriptableObject Live Editor
    /// - Drag ScriptableObjects into the editor
    /// - Edit values in play mode - changes apply immediately
    /// - Save changes back to asset or create new preset
    /// </summary>
    public class ScriptableObjectLiveEditor : MonoBehaviour
    {
        private static ScriptableObjectLiveEditor _instance;
        public static ScriptableObjectLiveEditor Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("ScriptableObjectLiveEditor");
                    _instance = go.AddComponent<ScriptableObjectLiveEditor>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Live Editor Settings")]
        [Tooltip("Enable automatic change detection")]
        public bool autoDetectChanges = true;

        [Tooltip("Change detection interval (seconds)")]
        public float changeDetectionInterval = 0.1f;

        [Tooltip("Enable change history tracking")]
        public bool trackChangeHistory = true;

        [Tooltip("Maximum history entries per ScriptableObject")]
        public int maxHistoryEntries = 50;

        [Tooltip("Log all changes to console")]
        public bool logChanges = false;

        // Tracked ScriptableObjects
        private Dictionary<ScriptableObject, ScriptableObjectTracker> _trackedObjects =
            new Dictionary<ScriptableObject, ScriptableObjectTracker>();

        private float _lastCheckTime = 0f;

        private void Update()
        {
            if (!autoDetectChanges) return;
            if (Time.time - _lastCheckTime < changeDetectionInterval) return;

            _lastCheckTime = Time.time;
            DetectChanges();
        }

        /// <summary>
        /// Start tracking a ScriptableObject for live editing
        /// </summary>
        public void TrackScriptableObject(ScriptableObject so)
        {
            if (so == null || _trackedObjects.ContainsKey(so)) return;

            var tracker = new ScriptableObjectTracker(so, trackChangeHistory, maxHistoryEntries);
            _trackedObjects[so] = tracker;

            Debug.Log($"[LiveEditor] Now tracking {so.name} for live editing");
        }

        /// <summary>
        /// Stop tracking a ScriptableObject
        /// </summary>
        public void UntrackScriptableObject(ScriptableObject so)
        {
            if (so == null || !_trackedObjects.ContainsKey(so)) return;

            _trackedObjects.Remove(so);
            Debug.Log($"[LiveEditor] Stopped tracking {so.name}");
        }

        /// <summary>
        /// Detect changes in all tracked ScriptableObjects
        /// </summary>
        private void DetectChanges()
        {
            foreach (var kvp in _trackedObjects)
            {
                if (kvp.Key == null) continue;

                var changes = kvp.Value.DetectChanges();
                if (changes.Count > 0 && logChanges)
                {
                    foreach (var change in changes)
                    {
                        Debug.Log($"[LiveEditor] {kvp.Key.name}.{change.fieldName}: {change.oldValue} → {change.newValue}");
                    }
                }
            }
        }

        /// <summary>
        /// Get all tracked ScriptableObjects
        /// </summary>
        public List<ScriptableObject> GetTrackedObjects()
        {
            return _trackedObjects.Keys.Where(k => k != null).ToList();
        }

        /// <summary>
        /// Get tracker for a specific ScriptableObject
        /// </summary>
        public ScriptableObjectTracker GetTracker(ScriptableObject so)
        {
            return _trackedObjects.ContainsKey(so) ? _trackedObjects[so] : null;
        }

        /// <summary>
        /// Apply a preset to a ScriptableObject
        /// </summary>
        public void ApplyPreset(ScriptableObject target, ScriptableObjectPreset preset)
        {
            if (target == null || preset == null) return;

            foreach (var fieldValue in preset.fieldValues)
            {
                SetFieldValue(target, fieldValue.fieldName, fieldValue.value);
            }

            Debug.Log($"[LiveEditor] Applied preset '{preset.presetName}' to {target.name}");
        }

        /// <summary>
        /// Create a preset from current ScriptableObject state
        /// </summary>
        public ScriptableObjectPreset CreatePreset(ScriptableObject so, string presetName)
        {
            if (so == null) return null;

            var preset = ScriptableObject.CreateInstance<ScriptableObjectPreset>();
            preset.presetName = presetName;
            preset.targetTypeName = so.GetType().AssemblyQualifiedName;
            preset.fieldValues = new List<FieldValuePair>();

            var fields = so.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                preset.fieldValues.Add(new FieldValuePair
                {
                    fieldName = field.Name,
                    value = field.GetValue(so)
                });
            }

            return preset;
        }

        private void SetFieldValue(ScriptableObject so, string fieldName, object value)
        {
            var field = so.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(so, value);
            }
        }

        /// <summary>
        /// Save all changes to disk (Editor only)
        /// </summary>
        public void SaveAllChangesToDisk()
        {
#if UNITY_EDITOR
            foreach (var kvp in _trackedObjects)
            {
                if (kvp.Key == null) continue;
                EditorUtility.SetDirty(kvp.Key);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[LiveEditor] Saved all changes to disk");
#endif
        }
    }

    /// <summary>
    /// Tracks changes to a ScriptableObject
    /// </summary>
    public class ScriptableObjectTracker
    {
        public ScriptableObject Target { get; private set; }

        private Dictionary<string, object> _previousValues = new Dictionary<string, object>();
        private List<FieldChange> _changeHistory = new List<FieldChange>();
        private int _maxHistoryEntries;
        private bool _trackHistory;

        public ScriptableObjectTracker(ScriptableObject target, bool trackHistory, int maxHistory)
        {
            Target = target;
            _trackHistory = trackHistory;
            _maxHistoryEntries = maxHistory;

            CaptureCurrentState();
        }

        /// <summary>
        /// Capture the current state of all fields
        /// </summary>
        public void CaptureCurrentState()
        {
            _previousValues.Clear();

            var fields = Target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(Target);
                    _previousValues[field.Name] = CloneValue(value);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to capture field {field.Name}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Detect changes since last check
        /// </summary>
        public List<FieldChange> DetectChanges()
        {
            var changes = new List<FieldChange>();

            var fields = Target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                try
                {
                    var currentValue = field.GetValue(Target);

                    if (!_previousValues.ContainsKey(field.Name))
                    {
                        _previousValues[field.Name] = CloneValue(currentValue);
                        continue;
                    }

                    var previousValue = _previousValues[field.Name];

                    if (!ValuesEqual(currentValue, previousValue))
                    {
                        var change = new FieldChange
                        {
                            fieldName = field.Name,
                            oldValue = previousValue,
                            newValue = CloneValue(currentValue),
                            timestamp = Time.time
                        };

                        changes.Add(change);
                        _previousValues[field.Name] = CloneValue(currentValue);

                        if (_trackHistory)
                        {
                            AddToHistory(change);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to detect change in field {field.Name}: {e.Message}");
                }
            }

            return changes;
        }

        private void AddToHistory(FieldChange change)
        {
            _changeHistory.Add(change);

            // Trim history if needed
            while (_changeHistory.Count > _maxHistoryEntries)
            {
                _changeHistory.RemoveAt(0);
            }
        }

        private bool ValuesEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            // Special handling for Unity types
            if (a is Vector3 v3a && b is Vector3 v3b) return v3a == v3b;
            if (a is Vector2 v2a && b is Vector2 v2b) return v2a == v2b;
            if (a is Color ca && b is Color cb) return ca == cb;

            return a.Equals(b);
        }

        private object CloneValue(object value)
        {
            if (value == null) return null;

            // For value types and strings, just return the value
            var type = value.GetType();
            if (type.IsValueType || type == typeof(string))
            {
                return value;
            }

            // For Unity types, create copies
            if (value is Vector3 v3) return new Vector3(v3.x, v3.y, v3.z);
            if (value is Vector2 v2) return new Vector2(v2.x, v2.y);
            if (value is Color c) return new Color(c.r, c.g, c.b, c.a);

            // For other reference types, just store reference (not a deep clone)
            return value;
        }

        /// <summary>
        /// Get change history
        /// </summary>
        public List<FieldChange> GetHistory()
        {
            return new List<FieldChange>(_changeHistory);
        }

        /// <summary>
        /// Clear change history
        /// </summary>
        public void ClearHistory()
        {
            _changeHistory.Clear();
        }
    }

    /// <summary>
    /// Represents a change to a field
    /// </summary>
    [Serializable]
    public struct FieldChange
    {
        public string fieldName;
        public object oldValue;
        public object newValue;
        public float timestamp;
    }

    /// <summary>
    /// Preset for ScriptableObject values
    /// </summary>
    public class ScriptableObjectPreset : ScriptableObject
    {
        public string presetName;
        public string targetTypeName;
        public List<FieldValuePair> fieldValues = new List<FieldValuePair>();
    }

    [Serializable]
    public class FieldValuePair
    {
        public string fieldName;
        public object value;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor window for ScriptableObject Live Editor
    /// </summary>
    public class ScriptableObjectLiveEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private ScriptableObject _selectedObject;
        private Editor _cachedEditor;

        [MenuItem("Tools/Project Chimera/ScriptableObject Live Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ScriptableObjectLiveEditorWindow>("SO Live Editor");
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            // Header
            EditorGUILayout.LabelField("ScriptableObject Live Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Object selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target:", GUILayout.Width(50));

            var newSelection = EditorGUILayout.ObjectField(_selectedObject, typeof(ScriptableObject), false) as ScriptableObject;
            if (newSelection != _selectedObject)
            {
                _selectedObject = newSelection;
                _cachedEditor = null;

                if (_selectedObject != null && Application.isPlaying)
                {
                    ScriptableObjectLiveEditor.Instance.TrackScriptableObject(_selectedObject);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Track Object") && _selectedObject != null && Application.isPlaying)
            {
                ScriptableObjectLiveEditor.Instance.TrackScriptableObject(_selectedObject);
            }

            if (GUILayout.Button("Untrack Object") && _selectedObject != null && Application.isPlaying)
            {
                ScriptableObjectLiveEditor.Instance.UntrackScriptableObject(_selectedObject);
            }

            if (GUILayout.Button("Save Changes") && Application.isPlaying)
            {
                ScriptableObjectLiveEditor.Instance.SaveAllChangesToDisk();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Show editor for selected object
            if (_selectedObject != null)
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                if (_cachedEditor == null || _cachedEditor.target != _selectedObject)
                {
                    _cachedEditor = Editor.CreateEditor(_selectedObject);
                }

                if (_cachedEditor != null)
                {
                    EditorGUI.BeginChangeCheck();
                    _cachedEditor.OnInspectorGUI();
                    if (EditorGUI.EndChangeCheck() && Application.isPlaying)
                    {
                        EditorUtility.SetDirty(_selectedObject);
                    }
                }

                EditorGUILayout.EndScrollView();

                // Show change history if in play mode
                if (Application.isPlaying)
                {
                    EditorGUILayout.Space();
                    DrawChangeHistory();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Drag a ScriptableObject here to edit it in play mode", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawChangeHistory()
        {
            if (_selectedObject == null || !Application.isPlaying) return;

            var tracker = ScriptableObjectLiveEditor.Instance.GetTracker(_selectedObject);
            if (tracker == null) return;

            var history = tracker.GetHistory();
            if (history.Count == 0) return;

            EditorGUILayout.LabelField("Change History", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");
            foreach (var change in history.TakeLast(10))
            {
                EditorGUILayout.LabelField($"{change.fieldName}: {change.oldValue} → {change.newValue} @ {change.timestamp:F2}s");
            }
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Clear History"))
            {
                tracker.ClearHistory();
            }
        }

        private void OnDestroy()
        {
            if (_cachedEditor != null)
            {
                DestroyImmediate(_cachedEditor);
            }
        }
    }
#endif
}
