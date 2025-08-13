using UnityEngine;
using UnityEngine.UI;
using TMPro;
// FIXME: tidyup after 8/29
public class SettingsMenuUI : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Graphics Settings")]
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("Controls Settings")]
    [SerializeField] private Slider sensitivitySlider;

    [Header("Buttons")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;

    private void Awake()
    {
        LoadSettings();

        applyButton.onClick.AddListener(ApplySettings);
        resetButton.onClick.AddListener(ResetSettings);
    }

    private void LoadSettings()
    {
        masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.7f);

        qualityDropdown.value = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());

        sensitivitySlider.value = PlayerPrefs.GetFloat("ControlSensitivity", 1f);
    }

    public void ApplySettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);

        PlayerPrefs.SetInt("GraphicsQuality", qualityDropdown.value);
        QualitySettings.SetQualityLevel(qualityDropdown.value);

        PlayerPrefs.SetFloat("ControlSensitivity", sensitivitySlider.value);

        PlayerPrefs.Save();

        // Optionally notify audio manager or input manager here
    }

    public void ResetSettings()
    {
        masterVolumeSlider.value = 1f;
        musicVolumeSlider.value = 0.7f;
        sfxVolumeSlider.value = 0.7f;

        qualityDropdown.value = QualitySettings.GetQualityLevel();

        sensitivitySlider.value = 1f;

        ApplySettings();
    }
}
