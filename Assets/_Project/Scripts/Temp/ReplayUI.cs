using UnityEngine;
using UnityEngine.UI;

public class ReplayUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Slider frameSlider;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;

    [Header("Replay System")]
    [SerializeField] private ReplayManager replayManager;
    [SerializeField] private ActorPlayback mainPlayer; // optional for camera follow

    private bool isScrubbing = false;

    private void Start()
    {
        // Button events
        playButton.onClick.AddListener(() => replayManager.StartPlayback());
        pauseButton.onClick.AddListener(() => PausePlayback());
        stopButton.onClick.AddListener(() => replayManager.StopPlayback());
        saveButton.onClick.AddListener(() => replayManager.StopRecording()); // save current recording
        loadButton.onClick.AddListener(() => replayManager.StartPlayback());

        // Slider events
        frameSlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void Update()
    {
        if (!isScrubbing && mainPlayer != null && mainPlayer.enabled)
        {
            // Update slider according to playback
            if (mainPlayer.FramesLength > 0)
                frameSlider.value = (float)mainPlayer.CurrentFrame / (mainPlayer.FramesLength - 1);
        }
    }

    private void PausePlayback()
    {
        if (mainPlayer != null)
            mainPlayer.Pause();
    }

    private void OnSliderChanged(float value)
    {
        if (mainPlayer == null) return;

        isScrubbing = true;
        int frameIndex = Mathf.RoundToInt(value * (mainPlayer.FramesLength - 1));
        mainPlayer.GoToFrame(frameIndex);
        isScrubbing = false;
    }
}
