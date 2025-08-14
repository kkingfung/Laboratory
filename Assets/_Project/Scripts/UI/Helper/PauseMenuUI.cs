using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// FIXME: tidyup after 8/29
public class PauseMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button[] menuButtons = null!;

    public static bool IsPaused { get; private set; } = false;

    public event Action<bool>? _OnPauseStateChanged;

    private float _lastNavigateTime = 0f;
    private float _navigateDelay = 0.2f;

    private void Awake()
    {
        pauseMenuPanel.SetActive(false);

        resumeButton.onClick.AddListener(ResumeGame);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(QuitGame);
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame || Gamepad.current?.startButton.wasPressedThisFrame == true)
        {
            if (IsPaused) ResumeGame();
            else PauseGame();
        }

        if (!IsPaused) return;

        HandleGamepadNavigation();
    }

    public void PauseGame()
    {
        if (IsPaused) return;

        Time.timeScale = 0f;
        pauseMenuPanel.SetActive(true);
        IsPaused = true;

        _OnPauseStateChanged?.Invoke(true);

        // Optionally disable player input here
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;

        Time.timeScale = 1f;
        pauseMenuPanel.SetActive(false);
        IsPaused = false;

        _OnPauseStateChanged?.Invoke(false);

        // Optionally re-enable player input here
    }

    private void HandleGamepadNavigation()
    {
        if (Gamepad.current == null) return;

        var navigate = Gamepad.current.dpad.ReadValue();

        if (navigate.y > 0.5f)
        {
            MoveSelection(-1);
        }
        else if (navigate.y < -0.5f)
        {
            MoveSelection(1);
        }

        if (Gamepad.current.aButton.wasPressedThisFrame)
        {
            menuButtons[_currentButtonIndex].onClick.Invoke();
        }
    }

     private void MoveSelection(int direction)
    {
        if (Time.unscaledTime - _lastNavigateTime < _navigateDelay) return;

        _lastNavigateTime = Time.unscaledTime;

        _currentButtonIndex += direction;
        if (_currentButtonIndex < 0) _currentButtonIndex = menuButtons.Length - 1;
        else if (_currentButtonIndex >= menuButtons.Length) _currentButtonIndex = 0;

        SelectButton(_currentButtonIndex);
    }

    private void SelectButton(int index)
    {
        var button = menuButtons[index];
        if (button != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    private int _currentButtonIndex = 0;
    private void OpenSettings()
    {
        // TODO: Open settings menu UI panel
        Debug.Log("Open Settings menu");
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
