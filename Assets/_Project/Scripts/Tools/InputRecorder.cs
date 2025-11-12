using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Laboratory.Tools
{
    /// <summary>
    /// Input recording and playback system for automated testing and demos.
    /// Records all input actions with timestamps and can replay them perfectly.
    /// Useful for regression testing, demo videos, and automated QA.
    /// </summary>
    public class InputRecorder : MonoBehaviour
    {
        #region Configuration

        [Header("Recording Settings")]
        [SerializeField] private bool enableRecording = false;
        [SerializeField] private KeyCode recordKey = KeyCode.F8;
        [SerializeField] private KeyCode playbackKey = KeyCode.F9;
        [SerializeField] private KeyCode stopKey = KeyCode.F7;

        [Header("Playback")]
        [SerializeField] private bool loopPlayback = false;
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private bool logPlaybackEvents = false;

        [Header("Storage")]
        [SerializeField] private string recordingsDirectory = "InputRecordings";
        [SerializeField] private bool autoSaveOnStop = true;

        #endregion

        #region Private Fields

        private static InputRecorder _instance;

        // Recording state
        private bool _isRecording = false;
        private bool _isPlayingBack = false;
        private InputRecording _currentRecording;
        private float _recordingStartTime;

        // Playback state
        private int _playbackIndex = 0;
        private float _playbackStartTime;

        // Input tracking
        private readonly List<RecordedInput> _recordedInputs = new List<RecordedInput>();

        // Statistics
        private int _totalRecordings = 0;
        private int _totalPlaybacks = 0;

        #endregion

        #region Properties

        public static InputRecorder Instance => _instance;
        public bool IsRecording => _isRecording;
        public bool IsPlayingBack => _isPlayingBack;
        public int RecordedInputCount => _recordedInputs.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[InputRecorder] Initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Recording controls
            if (Input.GetKeyDown(recordKey))
            {
                if (_isRecording)
                    StopRecording();
                else
                    StartRecording();
            }

            if (Input.GetKeyDown(playbackKey) && !_isRecording)
            {
                if (_isPlayingBack)
                    StopPlayback();
                else
                    StartPlayback();
            }

            if (Input.GetKeyDown(stopKey))
            {
                if (_isRecording)
                    StopRecording();
                if (_isPlayingBack)
                    StopPlayback();
            }

            // Record inputs
            if (_isRecording)
            {
                RecordFrame();
            }

            // Playback inputs
            if (_isPlayingBack)
            {
                PlaybackFrame();
            }
        }

        #endregion

        #region Recording

        /// <summary>
        /// Start recording inputs.
        /// </summary>
        public void StartRecording()
        {
            if (_isPlayingBack)
            {
                Debug.LogWarning("[InputRecorder] Cannot record during playback");
                return;
            }

            _isRecording = true;
            _recordingStartTime = Time.time;
            _recordedInputs.Clear();

            _currentRecording = new InputRecording
            {
                recordingName = $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}",
                recordedAt = DateTime.UtcNow,
                platform = Application.platform.ToString()
            };

            Debug.Log($"[InputRecorder] Started recording: {_currentRecording.recordingName}");
        }

        /// <summary>
        /// Stop recording and optionally save.
        /// </summary>
        public void StopRecording()
        {
            if (!_isRecording) return;

            _isRecording = false;

            _currentRecording.inputs = new List<RecordedInput>(_recordedInputs);
            _currentRecording.duration = Time.time - _recordingStartTime;
            _totalRecordings++;

            Debug.Log($"[InputRecorder] Stopped recording. Captured {_recordedInputs.Count} inputs over {_currentRecording.duration:F1}s");

            if (autoSaveOnStop)
            {
                SaveRecording(_currentRecording);
            }
        }

        private void RecordFrame()
        {
            float timestamp = Time.time - _recordingStartTime;

            // Record keyboard inputs
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    RecordInput(InputType.KeyDown, key.ToString(), timestamp);
                }
                else if (Input.GetKeyUp(key))
                {
                    RecordInput(InputType.KeyUp, key.ToString(), timestamp);
                }
            }

            // Record mouse buttons
            for (int i = 0; i < 3; i++)
            {
                if (Input.GetMouseButtonDown(i))
                {
                    RecordInput(InputType.MouseDown, $"Mouse{i}", timestamp, Input.mousePosition);
                }
                else if (Input.GetMouseButtonUp(i))
                {
                    RecordInput(InputType.MouseUp, $"Mouse{i}", timestamp, Input.mousePosition);
                }
            }

            // Record mouse movement (sample every frame)
            if (_recordedInputs.Count == 0 || Vector3.Distance(Input.mousePosition, _recordedInputs[_recordedInputs.Count - 1].mousePosition) > 1f)
            {
                RecordInput(InputType.MouseMove, "MouseMove", timestamp, Input.mousePosition);
            }
        }

        private void RecordInput(InputType type, string inputName, float timestamp, Vector3 mousePosition = default)
        {
            var recordedInput = new RecordedInput
            {
                type = type,
                inputName = inputName,
                timestamp = timestamp,
                mousePosition = mousePosition
            };

            _recordedInputs.Add(recordedInput);
        }

        #endregion

        #region Playback

        /// <summary>
        /// Start playing back the current recording.
        /// </summary>
        public void StartPlayback()
        {
            if (_currentRecording == null || _currentRecording.inputs == null || _currentRecording.inputs.Count == 0)
            {
                Debug.LogWarning("[InputRecorder] No recording to play back");
                return;
            }

            _isPlayingBack = true;
            _playbackIndex = 0;
            _playbackStartTime = Time.time;

            Debug.Log($"[InputRecorder] Started playback: {_currentRecording.recordingName}");
        }

        /// <summary>
        /// Start playing back a loaded recording.
        /// </summary>
        public void StartPlayback(InputRecording recording)
        {
            _currentRecording = recording;
            _recordedInputs.Clear();
            _recordedInputs.AddRange(recording.inputs);
            StartPlayback();
        }

        /// <summary>
        /// Stop playback.
        /// </summary>
        public void StopPlayback()
        {
            if (!_isPlayingBack) return;

            _isPlayingBack = false;
            _totalPlaybacks++;

            Debug.Log($"[InputRecorder] Stopped playback at index {_playbackIndex}/{_currentRecording.inputs.Count}");
        }

        private void PlaybackFrame()
        {
            float currentPlaybackTime = (Time.time - _playbackStartTime) * playbackSpeed;

            // Play all inputs that should have happened by now
            while (_playbackIndex < _currentRecording.inputs.Count)
            {
                var input = _currentRecording.inputs[_playbackIndex];

                if (input.timestamp > currentPlaybackTime)
                    break; // Wait for this input's time

                // Simulate input (note: actual input simulation requires Input System or custom handling)
                SimulateInput(input);

                _playbackIndex++;
            }

            // Check if playback complete
            if (_playbackIndex >= _currentRecording.inputs.Count)
            {
                if (loopPlayback)
                {
                    _playbackIndex = 0;
                    _playbackStartTime = Time.time;
                    Debug.Log("[InputRecorder] Looping playback");
                }
                else
                {
                    StopPlayback();
                }
            }
        }

        private void SimulateInput(RecordedInput input)
        {
            // Note: Actual input simulation requires Unity Input System or custom handling
            // This is a simplified placeholder

            if (logPlaybackEvents)
            {
                Debug.Log($"[InputRecorder] Playback: {input.type} - {input.inputName} at {input.timestamp:F2}s");
            }

            // In a real implementation, you would:
            // 1. Use Unity's Input System to inject simulated inputs
            // 2. Or trigger input-related events directly
            // 3. Or call methods that respond to inputs

            // Example: Trigger custom input events
            switch (input.type)
            {
                case InputType.KeyDown:
                    OnPlaybackKeyDown?.Invoke(input.inputName);
                    break;
                case InputType.KeyUp:
                    OnPlaybackKeyUp?.Invoke(input.inputName);
                    break;
                case InputType.MouseDown:
                    OnPlaybackMouseDown?.Invoke(input.inputName, input.mousePosition);
                    break;
                case InputType.MouseUp:
                    OnPlaybackMouseUp?.Invoke(input.inputName, input.mousePosition);
                    break;
                case InputType.MouseMove:
                    OnPlaybackMouseMove?.Invoke(input.mousePosition);
                    break;
            }
        }

        #endregion

        #region Persistence

        /// <summary>
        /// Save a recording to disk.
        /// </summary>
        public void SaveRecording(InputRecording recording)
        {
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, recordingsDirectory);
                Directory.CreateDirectory(directory);

                string filename = $"{recording.recordingName}.json";
                string path = Path.Combine(directory, filename);

                string json = JsonUtility.ToJson(recording, true);
                File.WriteAllText(path, json);

                Debug.Log($"[InputRecorder] Saved recording: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputRecorder] Failed to save recording: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a recording from disk.
        /// </summary>
        public InputRecording LoadRecording(string filename)
        {
            try
            {
                string directory = Path.Combine(Application.persistentDataPath, recordingsDirectory);
                string path = Path.Combine(directory, filename);

                if (!File.Exists(path))
                {
                    Debug.LogError($"[InputRecorder] Recording not found: {path}");
                    return null;
                }

                string json = File.ReadAllText(path);
                var recording = JsonUtility.FromJson<InputRecording>(json);

                Debug.Log($"[InputRecorder] Loaded recording: {filename} ({recording.inputs.Count} inputs, {recording.duration:F1}s)");

                return recording;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InputRecorder] Failed to load recording: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get all available recordings.
        /// </summary>
        public List<string> GetAvailableRecordings()
        {
            string directory = Path.Combine(Application.persistentDataPath, recordingsDirectory);

            if (!Directory.Exists(directory))
                return new List<string>();

            var files = Directory.GetFiles(directory, "*.json");
            var recordings = new List<string>();

            foreach (var file in files)
            {
                recordings.Add(Path.GetFileName(file));
            }

            return recordings;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get input recorder statistics.
        /// </summary>
        public InputRecorderStats GetStats()
        {
            return new InputRecorderStats
            {
                isRecording = _isRecording,
                isPlayingBack = _isPlayingBack,
                recordedInputCount = _recordedInputs.Count,
                currentRecordingDuration = _isRecording ? Time.time - _recordingStartTime : 0f,
                totalRecordings = _totalRecordings,
                totalPlaybacks = _totalPlaybacks
            };
        }

        #endregion

        #region Events

        public event Action<string> OnPlaybackKeyDown;
        public event Action<string> OnPlaybackKeyUp;
        public event Action<string, Vector3> OnPlaybackMouseDown;
        public event Action<string, Vector3> OnPlaybackMouseUp;
        public event Action<Vector3> OnPlaybackMouseMove;

        #endregion

        #region Context Menu

        [ContextMenu("Start Recording")]
        private void StartRecordingMenu()
        {
            StartRecording();
        }

        [ContextMenu("Stop Recording")]
        private void StopRecordingMenu()
        {
            StopRecording();
        }

        [ContextMenu("Start Playback")]
        private void StartPlaybackMenu()
        {
            StartPlayback();
        }

        [ContextMenu("Stop Playback")]
        private void StopPlaybackMenu()
        {
            StopPlayback();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Input Recorder Statistics ===\n" +
                      $"Is Recording: {stats.isRecording}\n" +
                      $"Is Playing Back: {stats.isPlayingBack}\n" +
                      $"Recorded Inputs: {stats.recordedInputCount}\n" +
                      $"Total Recordings: {stats.totalRecordings}\n" +
                      $"Total Playbacks: {stats.totalPlaybacks}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// A complete input recording.
    /// </summary>
    [Serializable]
    public class InputRecording
    {
        public string recordingName;
        public DateTime recordedAt;
        public float duration;
        public string platform;
        public List<RecordedInput> inputs = new List<RecordedInput>();
    }

    /// <summary>
    /// A single recorded input event.
    /// </summary>
    [Serializable]
    public struct RecordedInput
    {
        public InputType type;
        public string inputName;
        public float timestamp;
        public Vector3 mousePosition;
    }

    /// <summary>
    /// Input recorder statistics.
    /// </summary>
    [Serializable]
    public struct InputRecorderStats
    {
        public bool isRecording;
        public bool isPlayingBack;
        public int recordedInputCount;
        public float currentRecordingDuration;
        public int totalRecordings;
        public int totalPlaybacks;
    }

    /// <summary>
    /// Types of recorded inputs.
    /// </summary>
    public enum InputType
    {
        KeyDown,
        KeyUp,
        MouseDown,
        MouseUp,
        MouseMove
    }

    #endregion
}
