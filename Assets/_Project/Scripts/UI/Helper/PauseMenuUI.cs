using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// UI component for the pause menu system.
    /// Handles game pause/resume, settings navigation, and gamepad input support.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        #region Fields

        [Header("UI References")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button[] menuButtons;

        [Header("Navigation Settings")]
        [SerializeField] private float navigateDelay = 0.2f;

        private int _currentButtonIndex = 0;
        private float _lastNavigateTime = 0f;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the game is currently paused.
        /// </summary>
        public static bool IsPaused { get; private set; } = false;

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when pause state changes.
        /// </summary>
        public event Action<bool> OnPauseStateChanged;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize UI components and setup event handlers.
        /// </summary>
        private void Awake()
        {
            SetupUI();
            SetupButtonHandlers();
        }

        /// <summary>
        /// Handle pause input and gamepad navigation.
        /// </summary>
        private void Update()
        {
            HandlePauseInput();
            
            if (IsPaused)
                HandleGamepadNavigation();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Pause the game and show pause menu.
        /// </summary>
        public void PauseGame()
        {
            if (IsPaused) return;

            SetPauseState(true);
            pauseMenuPanel.SetActive(true);
            OnPauseStateChanged?.Invoke(true);
            
            // TODO: Disable player input systems here
        }

        /// <summary>
        /// Resume the game and hide pause menu.
        /// </summary>
        public void ResumeGame()
        {
            if (!IsPaused) return;

            SetPauseState(false);
            pauseMenuPanel.SetActive(false);
            OnPauseStateChanged?.Invoke(false);
            
            // TODO: Re-enable player input systems here
        }

        #endregion

        #region Private Methods - Setup

        /// <summary>
        /// Setup initial UI state.
        /// </summary>
        private void SetupUI()
        {
            pauseMenuPanel.SetActive(false);
        }

        /// <summary>
        /// Setup button event handlers.
        /// </summary>
        private void SetupButtonHandlers()
        {
            resumeButton.onClick.AddListener(ResumeGame);
            settingsButton.onClick.AddListener(OpenSettings);
            quitButton.onClick.AddListener(QuitGame);
        }

        #endregion

        #region Private Methods - Input Handling

        /// <summary>
        /// Handle pause/resume input from keyboard and gamepad.
        /// </summary>
        private void HandlePauseInput()
        {
            bool pausePressed = Keyboard.current?.escapeKey.wasPressedThisFrame == true ||
                               Gamepad.current?.startButton.wasPressedThisFrame == true;

            if (pausePressed)
            {
                if (IsPaused) 
                    ResumeGame();
                else 
                    PauseGame();
            }
        }

        /// <summary>
        /// Handle gamepad navigation in pause menu.
        /// </summary>
        private void HandleGamepadNavigation()
        {
            if (Gamepad.current == null) return;

            HandleDirectionalNavigation();
            HandleConfirmInput();
        }

        /// <summary>
        /// Handle directional pad navigation.
        /// </summary>
        private void HandleDirectionalNavigation()
        {
            var navigate = Gamepad.current.dpad.ReadValue();

            if (navigate.y > 0.5f)
            {
                MoveSelection(-1);
            }
            else if (navigate.y < -0.5f)
            {
                MoveSelection(1);
            }
        }

        /// <summary>
        /// Handle confirm button input.
        /// </summary>
        private void HandleConfirmInput()
        {
            if (Gamepad.current.aButton.wasPressedThisFrame)
            {
                menuButtons[_currentButtonIndex].onClick.Invoke();
            }
        }

        #endregion

        #region Private Methods - Navigation

        /// <summary>
        /// Move selection between menu buttons.
        /// </summary>
        /// <param name="direction">Direction to move (-1 for up, 1 for down)</param>
        private void MoveSelection(int direction)
        {
            if (Time.unscaledTime - _lastNavigateTime < navigateDelay) return;

            _lastNavigateTime = Time.unscaledTime;
            UpdateSelectedButtonIndex(direction);
            SelectButton(_currentButtonIndex);
        }

        /// <summary>
        /// Update the currently selected button index.
        /// </summary>
        /// <param name="direction">Direction to move</param>
        private void UpdateSelectedButtonIndex(int direction)
        {
            _currentButtonIndex += direction;
            
            if (_currentButtonIndex < 0) 
                _currentButtonIndex = menuButtons.Length - 1;
            else if (_currentButtonIndex >= menuButtons.Length) 
                _currentButtonIndex = 0;
        }

        /// <summary>
        /// Select button at specified index.
        /// </summary>
        /// <param name="index">Button index to select</param>
        private void SelectButton(int index)
        {
            var button = menuButtons[index];
            if (button != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }

        #endregion

        #region Private Methods - Menu Actions

        /// <summary>
        /// Open settings menu.
        /// </summary>
        private void OpenSettings()
        {
            // TODO: Open settings menu UI panel
            Debug.Log("Open Settings menu - implement settings navigation");
        }

        /// <summary>
        /// Quit the game or return to main menu.
        /// </summary>
        private void QuitGame()
        {
            SetPauseState(false);
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Private Methods - Utilities

        /// <summary>
        /// Set the pause state and time scale.
        /// </summary>
        /// <param name="paused">Whether game should be paused</param>
        private void SetPauseState(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        #endregion
    }
}
