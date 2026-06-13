using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using VContainer;

public class UIDisplayOptions : MonoBehaviour
{
    [Header("Screen UI References")]
    [SerializeField] private GameObject optionUI;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown windowSizeDropdown;

    [Header("Sound UI References")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    [Inject] private IDisplaySettings _display;
    [Inject] private ISoundManager _sound;

    bool isOnOption = false;

    private void Start()
    {
        BuildDropdownOptions();
        SyncUIFromCurrentSettings();

        isOnOption = false;
    }

    private void OnEnable()
    {
        if (_sound == null) return;
        if (bgmSlider) bgmSlider.SetValueWithoutNotify(_sound.GetBGMVolume());
        if (seSlider) seSlider.SetValueWithoutNotify(_sound.GetSEVolume());
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            isOnOption = !isOnOption;
            optionUI.gameObject.SetActive(isOnOption);
        }
        else if(Input.GetKeyDown(KeyCode.O))
        {
            SceneTransitionManager.Instance.LoadScene("OutSide");
        }
    }

    
    private void BuildDropdownOptions()
    {
        windowSizeDropdown.ClearOptions();
        
        var labels = new List<string>();
        foreach (var s in _display.WindowedSizes)
            labels.Add($"{s.x} x {s.y}");

        windowSizeDropdown.AddOptions(labels);
    }

    private void SyncUIFromCurrentSettings()
    {
        bool full = PlayerPrefs.GetInt("Windowed", 1) == 1;
        int winIndex = PlayerPrefs.GetInt("WinIndex", 0);
        int fillIndex = PlayerPrefs.GetInt("FillStretch", 1);

        fullscreenToggle.SetIsOnWithoutNotify(full);
        windowSizeDropdown.SetValueWithoutNotify(Mathf.Clamp(winIndex, 0, _display.WindowedSizes.Length - 1));
        windowSizeDropdown.interactable = !full;
    }

    public void OnFullscreenToggled(bool isOn)
    {
        windowSizeDropdown.interactable = !isOn;
    }

    public void OnApplyClicked()
    {
        bool full = fullscreenToggle.isOn;
        int winIndex = windowSizeDropdown.value;

        _display.SetWindowedSize(winIndex);
        _display.SetFullscreen(full);
    }

    public void OnBgmSliderChanged(float value01) => _sound.SetBGMVolume(value01);
    public void OnSeSliderChanged(float value01) => _sound.SetSEVolume(value01);
}