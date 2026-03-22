using LiquidSimulation;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class ShakingManager : MonoBehaviour
{
    [SerializeField] Text countText;

    [Inject] private CocktailCraftManager mainCraftManager;
    [SerializeField] CraftStationData craftStation;
    [SerializeField] ShakerInteraction shaker;
    [SerializeField] StirringInteraction stir;

    [SerializeField] string sceneName = "Shake";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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


        mainCraftManager.EndCraft();

        SceneTransitionManager.Instance.UnLoadScene(sceneName);
        
    }
}
