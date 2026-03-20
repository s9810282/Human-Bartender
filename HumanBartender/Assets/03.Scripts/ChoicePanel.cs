using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoicePanel : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI panelTMPText;
    [SerializeField] Text panelText;

    public void SetPanelText(string text)
    {
        panelText.text = text;
    }

    public void SetActive(bool isOn)
    {
        panel.SetActive(isOn);
    }

    public void ResetPanel()
    {
        panelText.text = "";
        panel.SetActive(false);
    }
}
