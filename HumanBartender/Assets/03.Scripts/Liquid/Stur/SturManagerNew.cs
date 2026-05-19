using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SturManagerNew : MonoBehaviour, IMiniGameController 
{
    [Header("Manager")]
    [SerializeField] SturStrikeNode sturStrikeNode;
    [SerializeField] AudioSource bgmSource;

    [Header("Center")]
    [SerializeField] Image center;
    [SerializeField] float judgeRange = 1;

    [SerializeField] Camera canvasCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        sturStrikeNode.Handle();
    }


    public void CompleteMade()
    {

    }

    public void InitGame(UniTaskCompletionSource tcs)
    {

    }

    public void OnNextButton()
    {

    }

    public void Retry()
    {

    }

    public void Serve()
    {

    }

    public void StartGame()
    {
        Logger.Log("Start Game");

        bgmSource.PlayScheduled(AudioSettings.dspTime + 0.1f);
        sturStrikeNode.InitToStart(GetCenterWorldPosition(), 60, 4, 2);
    }

    public void ClickEvent()
    {
        Logger.Log("Click Event");
       
    }



    public Vector3 GetCenterWorldPosition()
    {
        Vector3 local = center.rectTransform.position;

        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint
            (canvasCamera, local);
        screenPos.z = 10f;
        Vector3 worldPos = canvasCamera.ScreenToWorldPoint(screenPos);


        return worldPos;
    }
}
