using Cysharp.Threading.Tasks;
using Mono.Cecil;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShakingManagerNew : MonoBehaviour, IMiniGameController
{
    [Header("Data")]
    [SerializeField] CraftStationData data;
    [SerializeField] CategoryColorData colorData;
    [SerializeField] CocktailDataSO cocktailDataSO;
    [SerializeField] NodePatternData nodePatternData;

    [Header("Manager")]
    [SerializeField] ShakeLineCreator shakeLineCreator;
    [SerializeField] ShakingStrikeNode shakingStrikeNode;
    [SerializeField] ShakingCatergoryNodeCreator nodeCreator;
    [SerializeField] AudioSource bgmSource;

    [Header("Dot")]
    [SerializeField] List<Image> dots;

    [SerializeField] float judgeRange = 1;

    [SerializeField] Camera canvasCamera;


    Vector3[] dotPositions;

    void Start()
    {
        data.targetCocktailData = cocktailDataSO.allCocktails[data.targetCocktailId];

        shakeLineCreator.CreateLine();

        for (int i = 0; i < dots.Count; i++)
        {
            shakeLineCreator.SetLinePosition(i , GetDotWorldPosition(i));
        }


        dotPositions = new Vector3[dots.Count];
        for (int i = 0; i < dotPositions.Length; i++)
        {
            dotPositions[i] = GetDotWorldPosition(i);
        }


        Color[] colors = new Color[data.targetCocktailData.Keywords.Length];
        for(int i = 0; i < colors.Length; i++)
        {
            int n = colorData.categorys.
                FindIndex(a => a.Contains(data.targetCocktailData.Keywords[i]));

            colors[i] = colorData.colors[n];
        }

        nodeCreator.Init();
        nodeCreator.InitToStart(
            dotPositions, 
            colors, 
            nodePatternData.patternDatas[Random.Range(0, nodePatternData.patternDatas.Count)]);
    }

    
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

        bgmSource.PlayScheduled(AudioSettings.dspTime + 0.1f);
        shakingStrikeNode.InitToStart(dotPositions, 60);
    }

    public void ClickEvent()
    {
        Logger.Log("Click Event");
        CategoryNode node =  nodeCreator.GetNearestNode(shakingStrikeNode.transform.position, judgeRange);

        if (node != null)
        {
            Logger.Log("Judge");
            nodeCreator.CreateEffectNode(node.transform.position, node.curColor);
        }
        else
        {
            Logger.Log("Judge Fail");
            nodeCreator.CreateEffectNode(shakingStrikeNode.transform.position, Color.white);
        }
    }







    public Vector3 GetDotWorldPosition(int index)
    {
        Vector3 local = dots[index].rectTransform.position;

        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint
            (canvasCamera, local);
        screenPos.z = 10f;
        Vector3 worldPos = canvasCamera.ScreenToWorldPoint(screenPos);


        return worldPos;
    }
}
