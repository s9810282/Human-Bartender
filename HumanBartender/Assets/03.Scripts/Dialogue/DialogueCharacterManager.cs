using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueCharacterManager : MonoBehaviour
{
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
        Logger.Log("SC");
        portraitImage.gameObject.SetActive(true);

        //TODO : 데이터 부ㅡㄹ러오기

    }
    public void OffCharacter()
    {
        portraitImage.gameObject.SetActive(false);
    }
   
}
