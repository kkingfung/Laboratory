using UnityEngine;
using UnityEngine.UI;
using Laboratory.Core.Replay;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// User interface controller for replay system functionality.
    /// Manages playback controls, timeline scrubbing, and replay session management.
    /// </summary>
    public class ReplayUI : MonoBehaviour
    {
        #region Fields - UI Components
        
        [Header("Playback Controls")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button stopButton;
        
        [Header("Timeline Controls")]
        [SerializeField] private Slider frameSlider;
        
        [Header("Session Management")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        
        #endregion
        
        #region Fields - System References
        
        [Header("Replay System")]
        [SerializeField] private Laboratory.Core.Replay.ReplayManager replayManager;
        [SerializeField] private ActorPlayback mainPlayer; // Optional for camera follow
        
        #endregion
        
        #region Fields - State
        
        /// <summary>
        /// Indicates whether the user is currently manually scrubbing through the timeline
        /// </summary>
        private bool isScrubbing = false;
        
        /// <summary>
        /// Tracks the last known frame count to detect changes in playback content
        /// </summary>
        private int lastFrameCount = 0;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Initialize UI event handlers and validate configuration
        /// </summary>
        private void Start()
        {
            ValidateConfiguration();
            SetupEventHandlers();
            InitializeUI();
        }
        
        /// <summary>
        /// Update timeline slider based on current playback progress
        /// </summary>
        private void Update()
        {
            UpdateTimelineSlider();
        }
        
        /// <summary>
        /// Clean up event handlers when component is destroyed
        /// </summary>
        private void OnDestroy()
        {
            CleanupEventHandlers();
        }
        
        #endregion
        
        #region Public Methods - Playback Control
        
        /// <summary>
        /// Starts replay playback through the replay manager
        /// </summary>
        public void StartPlayback()
        {
            if (replayManager != null)
            {
                replayManager.StartPlayback();
                UpdateButtonStates(true);
            }
        }
        
        /// <summary>
        /// Pauses the current replay playback
        /// </summary>
        public void PausePlayback()
        {
            if (mainPlayer != null)
            {
                mainPlayer.Pause();
            }
        }
        
        /// <summary>
        /// Stops the current replay playback
        /// </summary>
        public void StopPlayback()
        {
            if (replayManager != null)
            {
                replayManager.StopPlayback();
                UpdateButtonStates(false);
                ResetTimelineSlider();
            }
        }
        
        /// <summary>
        /// Saves the current recording session
        /// </summary>
        public void SaveRecording()
        {
            if (replayManager != null)
            {
                replayManager.StopRecording();
            }
        }
        
        /// <summary>
        /// Loads and starts playback of a saved replay session
        /// </summary>
        public void LoadAndPlayReplay()
        {
            if (replayManager != null)
            {
                replayManager.StartPlayback();
                UpdateButtonStates(true);
            }
        }
        
        #endregion
        
        #region Private Methods - UI Management
        
        /// <summary>
        /// Sets up all UI event handlers for buttons and sliders
        /// </summary>
        private void SetupEventHandlers()
        {
            // Playback control handlers
            if (playButton != null)
                playButton.onClick.AddListener(StartPlayback);
            
            if (pauseButton != null)
                pauseButton.onClick.AddListener(PausePlayback);
            
            if (stopButton != null)
                stopButton.onClick.AddListener(StopPlayback);
            
            // Session management handlers
            if (saveButton != null)
                saveButton.onClick.AddListener(SaveRecording);
            
            if (loadButton != null)
                loadButton.onClick.AddListener(LoadAndPlayReplay);
            
            // Timeline control handlers
            if (frameSlider != null)
                frameSlider.onValueChanged.AddListener(OnTimelineSliderChanged);
        }
        
        /// <summary>
        /// Removes all UI event handlers to prevent memory leaks
        /// </summary>
        private void CleanupEventHandlers()
        {
            if (playButton != null)
                playButton.onClick.RemoveListener(StartPlayback);
            
            if (pauseButton != null)
                pauseButton.onClick.RemoveListener(PausePlayback);
            
            if (stopButton != null)
                stopButton.onClick.RemoveListener(StopPlayback);
            
            if (saveButton != null)
                saveButton.onClick.RemoveListener(SaveRecording);
            
            if (loadButton != null)
                loadButton.onClick.RemoveListener(LoadAndPlayReplay);
            
            if (frameSlider != null)
                frameSlider.onValueChanged.RemoveListener(OnTimelineSliderChanged);
        }
        
        /// <summary>
        /// Initializes UI elements to their default states
        /// </summary>
        private void InitializeUI()
        {
            UpdateButtonStates(false);
            ResetTimelineSlider();
        }
        
        /// <summary>
        /// Updates the enabled state of playback control buttons
        /// </summary>
        /// <param name="isPlaying">Whether playback is currently active</param>
        private void UpdateButtonStates(bool isPlaying)
        {
            if (playButton != null)
                playButton.interactable = !isPlaying;
            
            if (pauseButton != null)
                pauseButton.interactable = isPlaying;
            
            if (stopButton != null)
                stopButton.interactable = isPlaying;
        }
        
        /// <summary>
        /// Resets the timeline slider to its initial position
        /// </summary>
        private void ResetTimelineSlider()
        {
            if (frameSlider != null)
            {
                frameSlider.value = 0f;
                frameSlider.interactable = false;
            }
        }
        
        #endregion
        
        #region Private Methods - Timeline Management
        
        /// <summary>
        /// Updates the timeline slider based on current playback progress
        /// </summary>
        private void UpdateTimelineSlider()
        {
            if (isScrubbing || mainPlayer == null || frameSlider == null)
                return;
            
            if (!mainPlayer.enabled || mainPlayer.FramesLength <= 0)
            {
                ResetTimelineSlider();
                return;
            }
            
            // Enable slider if it's not already enabled and we have frames
            if (!frameSlider.interactable)
            {
                frameSlider.interactable = true;
            }
            
            // Update slider value based on current frame
            float normalizedProgress = (float)mainPlayer.CurrentFrame / (mainPlayer.FramesLength - 1);
            frameSlider.value = Mathf.Clamp01(normalizedProgress);
            
            // Cache frame count for change detection
            lastFrameCount = mainPlayer.FramesLength;
        }
        
        /// <summary>
        /// Handles timeline slider value changes for manual scrubbing
        /// </summary>
        /// <param name="value">Normalized slider value (0-1)</param>
        private void OnTimelineSliderChanged(float value)
        {
            if (mainPlayer == null || mainPlayer.FramesLength <= 0)
                return;
            
            isScrubbing = true;
            
            int targetFrame = Mathf.RoundToInt(value * (mainPlayer.FramesLength - 1));
            targetFrame = Mathf.Clamp(targetFrame, 0, mainPlayer.FramesLength - 1);
            
            mainPlayer.GoToFrame(targetFrame);
            
            // Reset scrubbing flag after a short delay to allow for smooth scrubbing
            Invoke(nameof(EndScrubbing), 0.1f);
        }
        
        /// <summary>
        /// Ends the scrubbing state after a delay
        /// </summary>
        private void EndScrubbing()
        {
            isScrubbing = false;
        }
        
        #endregion
        
        #region Private Methods - Validation
        
        /// <summary>
        /// Validates that all required UI components and system references are properly configured
        /// </summary>
        private void ValidateConfiguration()
        {
            // Validate required UI components
            if (playButton == null)
                Debug.LogWarning($"[{name}] Play button not assigned");
            
            if (pauseButton == null)
                Debug.LogWarning($"[{name}] Pause button not assigned");
            
            if (stopButton == null)
                Debug.LogWarning($"[{name}] Stop button not assigned");
            
            if (frameSlider == null)
                Debug.LogWarning($"[{name}] Frame slider not assigned");
            
            if (saveButton == null)
                Debug.LogWarning($"[{name}] Save button not assigned");
            
            if (loadButton == null)
                Debug.LogWarning($"[{name}] Load button not assigned");
            
            // Validate system references
            if (replayManager == null)
            {
                Debug.LogError($"[{name}] Replay Manager not assigned - UI will not function");
            }
            
            if (mainPlayer == null)
            {
                Debug.LogWarning($"[{name}] Main player not assigned - timeline scrubbing will not work");
            }
        }
        
        #endregion
        
        #region Public Properties
        
        /// <summary>
        /// Gets whether the UI is currently in scrubbing mode
        /// </summary>
        public bool IsScrubbing => isScrubbing;
        
        /// <summary>
        /// Gets the assigned replay manager reference
        /// </summary>
        public Laboratory.Core.Replay.ReplayManager ReplayManager => replayManager;
        
        /// <summary>
        /// Gets the assigned main player reference
        /// </summary>
        public ActorPlayback MainPlayer => mainPlayer;
        
        #endregion
    }
}
