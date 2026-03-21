using UnityEngine;
using UnityEngine.UI;

public class UIIngredientPanel : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text countText;
    [SerializeField] Image ingredientImage;
    [SerializeField] Button panelButton;

    int count = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        count = 0;
        countText.text = "0";   
    }

    public void SetImage(Sprite sprite)
    {
        ingredientImage.sprite = sprite;
    }

    public void SetNameText(string text)
    { 
        nameText.text = text;
    }
    public void ResetCount()
    {
        count = 0;
        SetCountText();
    }
    public void SetCountText()
    {
        countText.text = count.ToString();
    }
    public void IncreaseCount()
    {
        count++;
        SetCountText();
    }

    public Button GetButton()
    {
        return panelButton;
    }
}
