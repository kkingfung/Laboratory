using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// UI component for the main menu interface.
    /// Handles navigation to game scenes, settings, and application exit.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        #region Fields

        [Header("UI References")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Animation Settings")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize UI components and setup event handlers.
        /// </summary>
        private void Awake()
        {
            SetupButtonHandlers();
            InitializeUI();
        }

        /// <summary>
        /// Perform startup fade-in animation.
        /// </summary>
        private void Start()
        {
            if (canvasGroup != null)
                StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, fadeDuration));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Setup button event handlers.
        /// </summary>
        private void SetupButtonHandlers()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        /// <summary>
        /// Initialize UI to default state.
        /// </summary>
        private void InitializeUI()
        {
            mainMenuPanel.SetActive(true);

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Handle play button click - start game scene loading.
        /// </summary>
        private void OnPlayClicked()
        {
            StartCoroutine(LoadSceneAsync("GameScene"));
        }

        /// <summary>
        /// Handle settings button click - open settings menu.
        /// </summary>
        private void OnSettingsClicked()
        {
            // TODO: Open settings menu UI
            Debug.Log("Settings button clicked - implement settings menu navigation");
        }

        /// <summary>
        /// Handle quit button click - exit application.
        /// </summary>
        private void OnQuitClicked()
        {
            Debug.Log("Quit button clicked");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Load a scene asynchronously with fade out transition.
        /// </summary>
        /// <param name="sceneName">Name of scene to load</param>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            // Fade out current UI
            if (canvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, fadeDuration));

            // Start loading the scene
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            // Wait for scene to finish loading
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Fade canvas group between alpha values over specified duration.
        /// </summary>
        /// <param name="cg">Canvas group to fade</param>
        /// <param name="from">Starting alpha value</param>
        /// <param name="to">Target alpha value</param>
        /// <param name="duration">Fade duration in seconds</param>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float elapsed = 0f;
            cg.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            cg.alpha = to;
        }

        #endregion
    }
}
