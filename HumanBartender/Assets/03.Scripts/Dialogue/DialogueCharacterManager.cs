using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueCharacterManager : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] TextMeshProUGUI nameTMPText;      // 이름 텍스트

    [SerializeField] private SpriteRenderer portraitImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetCharacter(string characterName, string expression)
    {
        portraitImage.gameObject.SetActive(true);

        //TODO : 데이터 부ㅡㄹ러오기

    }
    public void OffCharacter()
    {
        portraitImage.gameObject.SetActive(false);
    }
    

    public void SetNameColor(Color32 color)
    {
        nameText.color = color;
        nameTMPText.color = color;
    }
}
