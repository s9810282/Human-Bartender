using UnityEngine;
using UnityEngine.UI;

public class UICocktailPanel : MonoBehaviour
{
    [SerializeField] Image cocktailImage;
    [SerializeField] Text cocktailNameText;

    [SerializeField] Button panelButton;

    void Start()
    {
        
    }

    public void SetImage(Sprite img)
    {
        cocktailImage.sprite = img;
    }
    public void SetText(string text)
    {
        cocktailNameText.text = text;
    }
    public Button GetButton()
    {
        return panelButton;
    }
}
