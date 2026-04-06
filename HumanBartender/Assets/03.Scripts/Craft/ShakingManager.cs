using Cysharp.Threading.Tasks;
using LiquidSimulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class ShakingManager : MonoBehaviour, IMiniGameController
{
    [SerializeField] Canvas gameCanvas;
    [SerializeField] Canvas buttonCanvas;

    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] CraftStationData craftStation;
    [SerializeField] ShakerInteraction shaker;

    //일단 하드코딩하기.
    [Header("Craft Anim")]
    [SerializeField] Animator characterAnim;
    [SerializeField] int curAnimIndex = 0;
    [SerializeField] int animMaxnIndex = 4;

    [Header("Craft Event")]
    [SerializeField] VoidEvent craftServe;
    [SerializeField] VoidEvent craftRetry;

    [SerializeField] string sceneName = "Shake";

    private UniTaskCompletionSource tcs;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameCanvas.worldCamera = Camera.main;
        buttonCanvas.worldCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayShakeAnim(int num)
    {
        Logger.Log("Shake Anim");

        curAnimIndex = (curAnimIndex) % 4 + 1;
        characterAnim.Play("Shaking" + curAnimIndex);
    }

    public void UpdateActionCount()
    {
        craftStation.craftingResult.acionCount++;
        countText.text = "횟수 : " + craftStation.craftingResult.acionCount.ToString() + "회";
    }

    public void InitGame(UniTaskCompletionSource tcs)
    {
        this.tcs = tcs;
        craftStation.craftingResult.acionCount = 0; // 카운트 초기화
        if (countText != null) countText.text = "0";
    }
    public void CompleteMade()
    {
        craftStation.craftingResult.isResult = true;

        if (shaker != null)
            craftStation.craftingResult.mixedColor = shaker.GetFullyMixedColor();
     
        if (tcs != null)
        {
            Logger.Log("shakeManager tcs not null");
            tcs.TrySetResult();
        }
    }
    public void OnNextButton()
    {
        buttonCanvas.gameObject.SetActive(true);
    }
    public void Serve()
    {
        craftServe?.Raise(new Void());
    }

    public void Retry()
    {
        craftRetry?.Raise(new Void());
    }
}
