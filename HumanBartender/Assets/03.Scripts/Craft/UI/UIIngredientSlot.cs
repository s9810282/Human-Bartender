using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIIngredientSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] Image ingredientImage;

    IngredientData data;
    public event Action<IngredientData> OnLeftClick;
    public event Action<IngredientData> OnRightClick;

    int count = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        count = 0;
        countText.text = "0";   
    }

    public void SetData(IngredientData d)
    {
        data = d;
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
    public void IncreaseCount(int v)
    {
        count += v;

        if (count < 0) count = 0;

        SetCountText();
    }

    public void OnPointerClick(PointerEventData e)
    {
        switch (e.button)
        {
            case PointerEventData.InputButton.Left:
                OnLeftClick?.Invoke(data);
                break;
            case PointerEventData.InputButton.Right:
                OnRightClick?.Invoke(data);
                break;
        }
    }
}
