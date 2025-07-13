// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.UI;

public class OptionSettingsSceneController : MonoBehaviour
{
    [Header("UI Components")]
    public Slider volumeSlider;
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;
    public Button backButton;

    [Header("Settings")]
    public string mainMenuSceneName = "MainMenuScene";

    private Resolution[] resolutions;

    private void Start()
    {
        // Initialize UI components
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        if (resolutionDropdown != null)
        {
            resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            var options = new System.Collections.Generic.List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);

                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();

            resolutionDropdown.onValueChanged.AddListener(SetResolution);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(GoToMainMenu);
        }
    }

    private void SetVolume(float value)
    {
        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();
        AudioListener.volume = value;
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    private void SetResolution(int resolutionIndex)
    {
        if (resolutions != null && resolutionIndex >= 0 && resolutionIndex < resolutions.Length)
        {
            Resolution resolution = resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }
    }

    private void GoToMainMenu()
    {
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("Main menu scene name is not set.");
        }
    }
}