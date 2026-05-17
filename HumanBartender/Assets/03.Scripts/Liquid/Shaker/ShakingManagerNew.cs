using Cysharp.Threading.Tasks;
using Mono.Cecil;
using UnityEngine;
using UnityEngine.UI;

public class ShakingManagerNew : MonoBehaviour, IMiniGameController
{
    [Header("Data")]
    [SerializeField] CraftStationData data;

    [Header("Manager")]
    [SerializeField] ShakeLineCreator shakeLineCreator;
    [SerializeField] ShakingStrikeNode shakingStrikeNode;
    [SerializeField] ShakingCatergoryNodeCreator nodeCreator;

    [Header("Dot")]
    [SerializeField] Image dotMiddle;
    [SerializeField] Image dotTop;
    [SerializeField] Image dotBottom;

    [SerializeField] int dotCount = 3;
    [SerializeField] float judgeRange = 1;

    [SerializeField] Camera canvasCamera;


    void Start()
    {
        shakeLineCreator.CreateLine();

        for (int i = 0; i < dotCount; i++)
        {
            shakeLineCreator.SetLinePosition(i , GetDotWorldPosition(i));
        }

        shakingStrikeNode.InitToStart(new Vector3[] 
        {   GetDotWorldPosition(0), 
            GetDotWorldPosition(1), 
            GetDotWorldPosition(2) });

        nodeCreator.InitToStart(
            GetDotWorldPosition(0),
            GetDotWorldPosition(1),
            GetDotWorldPosition(2));
    }

    // Update is called once per frame
    void Update()
    {
        shakingStrikeNode.Handle();
        nodeCreator.Handle();
    }





    public void InitGame(UniTaskCompletionSource tcs)
    {
        
    }

    public void CompleteMade()
    {
        
    }

    public void OnNextButton()
    {
        
    }

    public void Serve()
    {
        
    }

    public void Retry()
    {
        
    }






    public void StartGame()
    {
        Logger.Log("Start Game");
    }

    public void ClickEvent()
    {
        Logger.Log("Click Event");
        CategoryNode node =  nodeCreator.GetNearestNode(shakingStrikeNode.transform.position, judgeRange);

        if (node != null)
        {
            Logger.Log("Judge");
        }
        else
        {
            Logger.Log("Judge Fail");
        }
    }







    public Vector3 GetDotWorldPosition(int index)
    {
        Vector3 local;

        switch (index)
        {
            case 0: local = dotTop.rectTransform.position; break;
            case 1: local = dotMiddle.rectTransform.position; break;
            case 2: local = dotBottom.rectTransform.position; break;

            default: return (Vector2)transform.position;
        }


        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint
            (canvasCamera, local);
        screenPos.z = 10f;
        Vector3 worldPos = canvasCamera.ScreenToWorldPoint(screenPos);


        return worldPos;
    }
}
