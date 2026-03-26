using Cysharp.Threading.Tasks;
using LiquidSimulation;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class ShakingManager : MonoBehaviour, IMiniGameController
{
    [SerializeField] Text countText;
    [SerializeField] CraftStationData craftStation;
    [SerializeField] ShakerInteraction shaker;
    [SerializeField] StirringInteraction stir;

    [SerializeField] string sceneName = "Shake";

    private UniTaskCompletionSource tcs;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InitGame(UniTaskCompletionSource tcs)
    {
        this.tcs = tcs;
        craftStation.craftingResult.acionCount = 0; // 카운트 초기화
        if (countText != null) countText.text = "0";
    }


    public void AddActionCount()
    {
        craftStation.craftingResult.acionCount++;
        countText.text = craftStation.craftingResult.acionCount.ToString();
    }

    public void EndShake()
    {
        craftStation.craftingResult.isResult = true;

        if (shaker != null)
            craftStation.craftingResult.mixedColor = shaker.GetFullyMixedColor();
        else
            craftStation.craftingResult.mixedColor = stir.GetFullyMixedColor();

        if (tcs != null)
        {
            Logger.Log("shakeManager tcs not null");
            tcs.TrySetResult();
        }
    }
}
