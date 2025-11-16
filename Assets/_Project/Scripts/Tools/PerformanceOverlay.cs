using UnityEngine;
using UnityEngine.UI;
using Laboratory.Core.Performance;
using Laboratory.Core.Utilities;

namespace Laboratory.Tools
{
    /// <summary>
    /// Performance monitoring overlay that displays real-time performance metrics
    /// including frames per second (FPS), frame time, and memory usage.
    /// Automatically updates the UI with smoothed performance data.
    /// </summary>
    public class PerformanceOverlay : OptimizedMonoBehaviour
    {
        #region Serialized Fields

        /// <summary>
        /// UI Text component for displaying the current FPS value.
        /// </summary>
        [SerializeField] 
        [Tooltip("Text component that displays the frames per second")]
        private Text _fpsText = null!;

        /// <summary>
        /// UI Text component for displaying the frame time in milliseconds.
        /// </summary>
        [SerializeField] 
        [Tooltip("Text component that displays the frame time in milliseconds")]
        private Text _frameTimeText = null!;

        /// <summary>
        /// UI Text component for displaying the memory usage.
        /// </summary>
        [SerializeField] 
        [Tooltip("Text component that displays the current memory usage")]
        private Text _memoryText = null!;

        #endregion

        #region Configuration Fields

        /// <summary>
        /// Smoothing factor for FPS calculations (lower values = more smoothing).
        /// </summary>
        [SerializeField] 
        [Range(0.01f, 1.0f)]
        [Tooltip("Smoothing factor for FPS calculations (0.01 = very smooth, 1.0 = no smoothing)")]
        private float _smoothingFactor = 0.1f;

        /// <summary>
        /// Update frequency for memory calculations (in seconds).
        /// </summary>
        [SerializeField] 
        [Range(0.1f, 5.0f)]
        [Tooltip("How often to update memory usage display (in seconds)")]
        private float _memoryUpdateInterval = 1.0f;

        #endregion

        #region Private Fields

        /// <summary>
        /// Smoothed delta time used for FPS calculations.
        /// </summary>
        private float _deltaTime = 0.0f;

        /// <summary>
        /// Timer for controlling memory update frequency.
        /// </summary>
        private float _memoryUpdateTimer = 0.0f;

        /// <summary>
        /// Cached memory value to avoid frequent GC calls.
        /// </summary>
        private long _cachedMemory = 0;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Validates component references and initializes the overlay.
        /// </summary>
        protected override void Start()
        {
            base.Start(); // Register for optimized updates

            ValidateComponents();
            InitializeDisplay();

            // Performance overlay doesn't need frequent updates
            updateFrequency = OptimizedUpdateManager.UpdateFrequency.LowFrequency;
        }

        /// <summary>
        /// Updates performance metrics and refreshes the display.
        /// </summary>
        public override void OnOptimizedUpdate(float deltaTime)
        {
            UpdatePerformanceMetrics();
            RefreshDisplay();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Toggles the visibility of the performance overlay.
        /// </summary>
        public void ToggleVisibility()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        /// <summary>
        /// Resets all performance tracking values.
        /// </summary>
        public void ResetMetrics()
        {
            _deltaTime = 0.0f;
            _memoryUpdateTimer = 0.0f;
            _cachedMemory = 0;
        }

        /// <summary>
        /// Updates the smoothing factor for FPS calculations.
        /// </summary>
        /// <param name="smoothingFactor">New smoothing factor (0.01 to 1.0).</param>
        public void SetSmoothingFactor(float smoothingFactor)
        {
            _smoothingFactor = Mathf.Clamp(smoothingFactor, 0.01f, 1.0f);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates that all required UI components are assigned.
        /// </summary>
        private void ValidateComponents()
        {
            if (_fpsText == null)
                Debug.LogError($"FPS Text component is not assigned on {name}");
            
            if (_frameTimeText == null)
                Debug.LogError($"Frame Time Text component is not assigned on {name}");
            
            if (_memoryText == null)
                Debug.LogError($"Memory Text component is not assigned on {name}");
        }

        /// <summary>
        /// Initializes the display with default values.
        /// </summary>
        private void InitializeDisplay()
        {
            if (_fpsText != null)
                _fpsText.text = "FPS: --";
            
            if (_frameTimeText != null)
                _frameTimeText.text = "Frame Time: -- ms";
            
            if (_memoryText != null)
                _memoryText.text = "Memory: -- MB";
        }

        /// <summary>
        /// Updates all performance tracking metrics.
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            UpdateFrameTimeMetrics();
            UpdateMemoryMetrics();
        }

        /// <summary>
        /// Updates frame time and FPS calculations using smoothed delta time.
        /// </summary>
        private void UpdateFrameTimeMetrics()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * _smoothingFactor;
        }

        /// <summary>
        /// Updates memory usage metrics at the specified interval.
        /// </summary>
        private void UpdateMemoryMetrics()
        {
            _memoryUpdateTimer += Time.unscaledDeltaTime;
            
            if (_memoryUpdateTimer >= _memoryUpdateInterval)
            {
                _cachedMemory = System.GC.GetTotalMemory(false);
                _memoryUpdateTimer = 0.0f;
            }
        }

        /// <summary>
        /// Refreshes the UI display with current performance values.
        /// </summary>
        private void RefreshDisplay()
        {
            UpdateFpsDisplay();
            UpdateFrameTimeDisplay();
            UpdateMemoryDisplay();
        }

        /// <summary>
        /// Updates the FPS display text with the current calculated value.
        /// </summary>
        private void UpdateFpsDisplay()
        {
            if (_fpsText == null || _deltaTime <= 0) return;

            float fps = 1.0f / _deltaTime;
            _fpsText.text = StringOptimizer.FormatOptimized("FPS: {0:F1}", fps);
        }

        /// <summary>
        /// Updates the frame time display text with the current value in milliseconds.
        /// </summary>
        private void UpdateFrameTimeDisplay()
        {
            if (_frameTimeText == null) return;

            float frameMs = _deltaTime * 1000.0f;
            _frameTimeText.text = StringOptimizer.FormatOptimized("Frame Time: {0:F2} ms", frameMs);
        }

        /// <summary>
        /// Updates the memory display text with the current usage in megabytes.
        /// </summary>
        private void UpdateMemoryDisplay()
        {
            if (_memoryText == null) return;

            float memoryMB = _cachedMemory / (1024f * 1024f);
            _memoryText.text = StringOptimizer.FormatOptimized("Memory: {0:F2} MB", memoryMB);
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        /// <summary>
        /// Validates component assignments in the editor.
        /// </summary>
        private void OnValidate()
        {
            _smoothingFactor = Mathf.Clamp(_smoothingFactor, 0.01f, 1.0f);
            _memoryUpdateInterval = Mathf.Clamp(_memoryUpdateInterval, 0.1f, 5.0f);
        }
#endif

        #endregion
    }
}
