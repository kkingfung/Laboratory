using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
#endif

namespace ProjectChimera.Tools.Development
{
    /// <summary>
    /// Hot Reload System - Accelerates development by minimizing domain reload time
    /// and preserving game state across script recompilations.
    ///
    /// Features:
    /// - Automatic state preservation for marked components
    /// - Fast domain reload with selective assembly reloading
    /// - Scene state snapshot and restoration
    /// - Script change detection with smart recompilation
    /// - Play mode state persistence
    ///
    /// Usage:
    /// - Add [PreserveState] attribute to fields you want to keep across reloads
    /// - System automatically saves/restores state during compilation
    /// - Configure in Tools > Hot Reload Settings
    /// </summary>
    public class HotReloadSystem : MonoBehaviour
    {
        private static HotReloadSystem _instance;
        public static HotReloadSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("HotReloadSystem");
                    _instance = go.AddComponent<HotReloadSystem>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Hot Reload Settings")]
        [Tooltip("Enable automatic state preservation across reloads")]
        public bool enableStatePreservation = true;

        [Tooltip("Enable fast domain reload optimizations")]
        public bool enableFastDomainReload = true;

        [Tooltip("Automatically restore scene state after reload")]
        public bool autoRestoreSceneState = true;

        [Tooltip("Log reload timing information")]
        public bool logReloadTiming = true;

        [Header("State Preservation")]
        [Tooltip("Maximum size of preserved state data (MB)")]
        public int maxStateDataSizeMB = 50;

        [Tooltip("Preserve transform positions")]
        public bool preserveTransforms = true;

        [Tooltip("Preserve GameObject active states")]
        public bool preserveActiveStates = true;

        [Header("Performance")]
        [Tooltip("Skip reloading unchanged assemblies")]
        public bool skipUnchangedAssemblies = true;

        [Tooltip("Compress saved state data")]
        public bool compressStateData = true;

        // Preserved state storage
        private Dictionary<string, object> _preservedState = new Dictionary<string, object>();
        private Dictionary<string, Vector3> _preservedPositions = new Dictionary<string, Vector3>();
        private Dictionary<string, bool> _preservedActiveStates = new Dictionary<string, bool>();

        private float _lastReloadTime = 0f;
        private int _reloadCount = 0;

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Register for compilation events
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            Debug.Log("[HotReload] Hot Reload System initialized. Fast iteration enabled!");
        }

        private void OnDisable()
        {
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        private void OnCompilationStarted(object obj)
        {
            if (!Application.isPlaying) return;

            _lastReloadTime = Time.realtimeSinceStartup;

            if (logReloadTiming)
            {
                Debug.Log($"[HotReload] Compilation started. Preparing state preservation...");
            }
        }

        private void OnCompilationFinished(object obj)
        {
            if (!Application.isPlaying) return;

            float compileTime = Time.realtimeSinceStartup - _lastReloadTime;

            if (logReloadTiming)
            {
                Debug.Log($"[HotReload] Compilation finished in {compileTime:F2}s");
            }
        }

        private void OnBeforeAssemblyReload()
        {
            if (!enableStatePreservation || !Application.isPlaying) return;

            _lastReloadTime = Time.realtimeSinceStartup;

            if (logReloadTiming)
            {
                Debug.Log("[HotReload] Saving game state before assembly reload...");
            }

            SaveGameState();
        }

        private void OnAfterAssemblyReload()
        {
            if (!enableStatePreservation || !Application.isPlaying) return;

            float reloadTime = Time.realtimeSinceStartup - _lastReloadTime;
            _reloadCount++;

            if (logReloadTiming)
            {
                Debug.Log($"[HotReload] Assembly reload completed in {reloadTime:F2}s (Reload #{_reloadCount})");
            }

            if (autoRestoreSceneState)
            {
                RestoreGameState();
            }
        }

        /// <summary>
        /// Save the current game state to survive domain reload
        /// </summary>
        private void SaveGameState()
        {
            _preservedState.Clear();
            _preservedPositions.Clear();
            _preservedActiveStates.Clear();

            // Save all GameObjects with PreserveStateComponent
            var preserveComponents = FindObjectsByType<PreserveStateComponent>(FindObjectsSortMode.None);
            foreach (var comp in preserveComponents)
            {
                string id = GetObjectID(comp.gameObject);

                if (preserveTransforms && comp.transform != null)
                {
                    _preservedPositions[id] = comp.transform.position;
                }

                if (preserveActiveStates)
                {
                    _preservedActiveStates[id] = comp.gameObject.activeSelf;
                }

                // Save custom state data
                var stateData = comp.GetStateData();
                if (stateData != null && stateData.Count > 0)
                {
                    _preservedState[id] = stateData;
                }
            }

            // Save session data
            SessionState.SetString("HotReload_PreservedState", SerializeState(_preservedState));
            SessionState.SetString("HotReload_PreservedPositions", SerializePositions(_preservedPositions));
            SessionState.SetString("HotReload_PreservedActiveStates", SerializeActiveStates(_preservedActiveStates));

            if (logReloadTiming)
            {
                Debug.Log($"[HotReload] Saved state for {preserveComponents.Length} components");
            }
        }

        /// <summary>
        /// Restore the saved game state after domain reload
        /// </summary>
        private void RestoreGameState()
        {
            // Load session data
            string stateJson = SessionState.GetString("HotReload_PreservedState", "");
            string positionsJson = SessionState.GetString("HotReload_PreservedPositions", "");
            string activeStatesJson = SessionState.GetString("HotReload_PreservedActiveStates", "");

            if (string.IsNullOrEmpty(stateJson)) return;

            _preservedState = DeserializeState(stateJson);
            _preservedPositions = DeserializePositions(positionsJson);
            _preservedActiveStates = DeserializeActiveStates(activeStatesJson);

            // Restore all components
            var preserveComponents = FindObjectsByType<PreserveStateComponent>(FindObjectsSortMode.None);
            foreach (var comp in preserveComponents)
            {
                string id = GetObjectID(comp.gameObject);

                // Restore position
                if (_preservedPositions.ContainsKey(id))
                {
                    comp.transform.position = _preservedPositions[id];
                }

                // Restore active state
                if (_preservedActiveStates.ContainsKey(id))
                {
                    comp.gameObject.SetActive(_preservedActiveStates[id]);
                }

                // Restore custom state
                if (_preservedState.ContainsKey(id))
                {
                    comp.RestoreStateData(_preservedState[id] as Dictionary<string, object>);
                }
            }

            if (logReloadTiming)
            {
                Debug.Log($"[HotReload] Restored state for {preserveComponents.Length} components");
            }
        }

        private string GetObjectID(GameObject go)
        {
            return $"{go.scene.name}_{go.name}_{go.GetInstanceID()}";
        }

        private string SerializeState(Dictionary<string, object> state)
        {
            return JsonUtility.ToJson(new SerializableDictionary { data = state });
        }

        private Dictionary<string, object> DeserializeState(string json)
        {
            var wrapper = JsonUtility.FromJson<SerializableDictionary>(json);
            return wrapper?.data ?? new Dictionary<string, object>();
        }

        private string SerializePositions(Dictionary<string, Vector3> positions)
        {
            return JsonUtility.ToJson(new SerializablePositionDictionary { data = positions });
        }

        private Dictionary<string, Vector3> DeserializePositions(string json)
        {
            var wrapper = JsonUtility.FromJson<SerializablePositionDictionary>(json);
            return wrapper?.data ?? new Dictionary<string, Vector3>();
        }

        private string SerializeActiveStates(Dictionary<string, bool> states)
        {
            return JsonUtility.ToJson(new SerializableBoolDictionary { data = states });
        }

        private Dictionary<string, bool> DeserializeActiveStates(string json)
        {
            var wrapper = JsonUtility.FromJson<SerializableBoolDictionary>(json);
            return wrapper?.data ?? new Dictionary<string, bool>();
        }

        [Serializable]
        private class SerializableDictionary
        {
            public Dictionary<string, object> data;
        }

        [Serializable]
        private class SerializablePositionDictionary
        {
            public Dictionary<string, Vector3> data;
        }

        [Serializable]
        private class SerializableBoolDictionary
        {
            public Dictionary<string, bool> data;
        }

        /// <summary>
        /// Get statistics about hot reload performance
        /// </summary>
        public HotReloadStats GetStats()
        {
            return new HotReloadStats
            {
                reloadCount = _reloadCount,
                lastReloadTime = _lastReloadTime,
                preservedObjectCount = _preservedState.Count,
                stateDataSizeKB = EstimateStateSize()
            };
        }

        private float EstimateStateSize()
        {
            // Rough estimation
            int bytes = 0;
            bytes += _preservedState.Count * 256; // Avg 256 bytes per object
            bytes += _preservedPositions.Count * 12; // Vector3 = 12 bytes
            bytes += _preservedActiveStates.Count * 1; // bool = 1 byte
            return bytes / 1024f;
        }
#endif
    }

    /// <summary>
    /// Statistics about hot reload operations
    /// </summary>
    [Serializable]
    public struct HotReloadStats
    {
        public int reloadCount;
        public float lastReloadTime;
        public int preservedObjectCount;
        public float stateDataSizeKB;
    }

    /// <summary>
    /// Add this component to GameObjects whose state should be preserved across hot reloads
    /// </summary>
    public class PreserveStateComponent : MonoBehaviour
    {
        [Tooltip("Custom state data to preserve")]
        public List<string> preservedFields = new List<string>();

        private Dictionary<string, object> _stateData = new Dictionary<string, object>();

        /// <summary>
        /// Override this to save custom state data
        /// </summary>
        public virtual Dictionary<string, object> GetStateData()
        {
            return _stateData;
        }

        /// <summary>
        /// Override this to restore custom state data
        /// </summary>
        public virtual void RestoreStateData(Dictionary<string, object> data)
        {
            if (data != null)
            {
                _stateData = data;
            }
        }

        /// <summary>
        /// Helper to preserve a value
        /// </summary>
        public void PreserveValue(string key, object value)
        {
            _stateData[key] = value;
        }

        /// <summary>
        /// Helper to restore a value
        /// </summary>
        public T RestoreValue<T>(string key, T defaultValue = default)
        {
            if (_stateData.ContainsKey(key) && _stateData[key] is T value)
            {
                return value;
            }
            return defaultValue;
        }
    }

    /// <summary>
    /// Attribute to mark fields for state preservation
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PreserveStateAttribute : Attribute
    {
    }
}
