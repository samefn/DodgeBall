using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityValue;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValue;
    [SerializeField] private TMP_Text fullscreenLabel;
    [Tooltip("Only Sí / No (the PANTALLA COMPLETA image already has the text).")]
    [SerializeField] private bool compactFullscreenLabel;
    [Tooltip("Panel shown again when this one is closed.")]
    [SerializeField] private GameObject returnPanel;

    private void Awake()
    {
        sensitivitySlider.minValue = GameSettings.MinSensitivity;
        sensitivitySlider.maxValue = GameSettings.MaxSensitivity;
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnEnable()
    {
        sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
        RefreshLabels();
    }

    private void OnDisable()
    {
        GameSettings.Save();
    }

    public void ToggleFullscreen()
    {
        GameSettings.Fullscreen = !GameSettings.Fullscreen;
        SetFullscreenLabel(!Screen.fullScreen);
    }

    public void Back()
    {
        gameObject.SetActive(false);
        if (returnPanel != null)
            returnPanel.SetActive(true);
    }

    private void OnSensitivityChanged(float value)
    {
        GameSettings.MouseSensitivity = value;
        RefreshLabels();
    }

    private void OnVolumeChanged(float value)
    {
        GameSettings.Volume = value;
        RefreshLabels();
    }

    private void SetFullscreenLabel(bool fullscreen)
    {
        string value = fullscreen ? "Sí" : "No";
        fullscreenLabel.text = compactFullscreenLabel ? value.ToUpperInvariant() : $"Pantalla completa: {value}";
    }

    private void RefreshLabels()
    {
        sensitivityValue.text = $"x{GameSettings.MouseSensitivity:0.0}";
        volumeValue.text = $"{Mathf.RoundToInt(GameSettings.Volume * 100f)}%";
        SetFullscreenLabel(Screen.fullScreen);
    }
}
