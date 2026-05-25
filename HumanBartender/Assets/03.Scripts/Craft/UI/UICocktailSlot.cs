using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public struct VisualUI
{
    public Image img;
    public Sprite onSprite;
    public Sprite offSprite;
}


public class UICocktailSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Data UI")]
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] Image cocktailImage;

    [Header("Visual UI")]
    [SerializeField] VisualUI[] visualUIs;

    CocktailData data;
    public event Action<CocktailData> OnClick;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void SetData(CocktailData d)
    {
        data = d;
    }

    public void SetImage(Sprite sprite)
    {
        cocktailImage.sprite = sprite;
    }
    public void SetNameText(string text)
    {
        nameText.text = text;
    }

    public void OnOff(bool isOn)
    {
        foreach (var item in visualUIs)
        {
            item.img.sprite = isOn ? item.onSprite : item.offSprite;
            cocktailImage.gameObject.SetActive(isOn);
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        switch (e.button)
        {
            case PointerEventData.InputButton.Left:
                OnClick?.Invoke(data);
                break;
        }
    }
}
