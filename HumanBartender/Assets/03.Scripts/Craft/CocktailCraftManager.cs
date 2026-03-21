using UnityEngine;

public class CocktailCraftManager : MonoBehaviour
{
    [SerializeField] CraftDataSO dataSO;
    [SerializeField] CraftEventData curCraftEventData;
    [SerializeField] GameObject ingredientPanel;

    [SerializeField] GameObject shakingObj;

    [SerializeField] StringEvent dialogueEvent;

    
    public void Start()
    {
        
    }

    public CraftEventData GetCraftDataByID(string id)
    {
        foreach(var item in dataSO.craftData.craft_events)
        {
            if(item.id == id)
                return item;
        }
        
        return default;
    }


    /// <summary>
    /// Command 에서 호출 되는 함수
    /// </summary>
    /// <param name="craftEventData"></param>
    public void StartCraft(CraftEventData craftEventData)
    {
        if (craftEventData.auto_open_recipe_ui)
            ingredientPanel.gameObject.SetActive(true);

        curCraftEventData = craftEventData;
        TutorialData tutoData = craftEventData.tutorial;
        if (tutoData.enabled)
        {
            TutorialStepData[] steps = tutoData.steps;
            
            for(int i = 0; i < steps.Length; i++)
            {
                TutorialStep(steps[i]);
            }
        }
    }

    public void TutorialStep(TutorialStepData data)
    {
        switch (data.type)
        {
            case "highlight":

                Logger.Log("highLight를 어디에 넣으라는 거야" + data.target);
                Logger.Log("text는 또 어디 띄우라는거임" + data.text);

                break;
        }
    }


    public void StartBuild()
    {
        SceneTransitionManager.Instance.LoadScene("Build");
    }
    public void StartShake()
    {
        //SceneTransitionManager.Instance.LoadScene("Shake");
        shakingObj.gameObject.SetActive(true);
    }
    public void StartStur()
    {
        SceneTransitionManager.Instance.LoadScene("Stur");
    }

    public void EndShake()
    {
        Logger.Log("Shake 끝남 판정");
        Logger.Log("결과 판정 추후 진행 : 디폴트 B.");
        Logger.Log($"다이얼로그 재진입 {curCraftEventData.reactions.B}");
        Logger.Log("컷씬 재생");

        GameStateManager.Instance.CurrentGameState = GameState.Play;
        dialogueEvent?.Raise(curCraftEventData.reactions.B);

        shakingObj.gameObject.SetActive(false);
        ingredientPanel.gameObject.SetActive(false);
        curCraftEventData = default;
    }
}
