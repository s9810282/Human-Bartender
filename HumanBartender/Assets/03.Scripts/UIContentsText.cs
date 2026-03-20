using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIContentsText : MonoBehaviour
{
    //public TextMeshProUGUI contextTMPText;      // 이름 텍스트
    public Text contextText;

    public int defaultFont = 25;

    public void Start()
    {
        SetFontSize(defaultFont);
    }

    public void SetText(string text)
    {
        contextText.text = text;
        //contextTMPText.text = text;
    }

    public void SetFontSize(int size)
    { 
        contextText.fontSize = size;
        //contextTMPText.fontSize = size;
    }

    public void SetFontColor(Color color)
    {
        contextText.color = color;
        //contextTMPText.color = color;

    }
}
