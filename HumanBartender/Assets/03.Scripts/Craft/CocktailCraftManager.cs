using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;
using VContainer;

public class CocktailCraftManager : MonoBehaviour
{
    [SerializeField] CraftDataSO dataSO;
    [SerializeField] CraftEventData curCraftEventData;
    [SerializeField] IngredientPanel ingredientPanel;

    [SerializeField] StringEvent dialogueEvent;
    [Inject] LifetimeScope currentInGameScope;

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
        {
            ingredientPanel.ResetPanel();
            ingredientPanel.gameObject.SetActive(true);
        }
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


    public void ProcessCraftResult()
    {
        EndCraft();
    }

    /// <summary>
    /// 연출 있다는데 그건 그때 넣어야되는거고
    /// </summary>
    public void StartBuild()
    {
        SceneTransitionManager.Instance.LoadScene("Build", LoadSceneMode.Additive);

    }
    public void StartShake()
    {
        GameStateManager.Instance.CurrentGameState = GameState.MiniGame;
        
        using (LifetimeScope.EnqueueParent(currentInGameScope))
        {
            SceneTransitionManager.Instance.LoadScene("Shake", LoadSceneMode.Additive);
        }

        ingredientPanel.ResetPanel();
        ingredientPanel.gameObject.SetActive(false);
    }
    public void StartStur()
    {
        GameStateManager.Instance.CurrentGameState = GameState.MiniGame;

        using (LifetimeScope.EnqueueParent(currentInGameScope))
        {
            SceneTransitionManager.Instance.LoadScene("Stur", LoadSceneMode.Additive);
        }

        ingredientPanel.ResetPanel();
        ingredientPanel.gameObject.SetActive(false);
    }


    /// <summary>
    /// Craft는 종료 후 다시 Main으로 돌아왔을 때 호출할 필드이긴한데...
    /// </summary>
    public void EndCraft()
    {
        Logger.Log("Shake 끝남 판정");
        Logger.Log("결과 판정 추후 진행 : 디폴트 B.");
        Logger.Log($"다이얼로그 재진입 {curCraftEventData.reactions.B}");
        Logger.Log("컷씬 재생");

        GameStateManager.Instance.CurrentGameState = GameState.Play;
        dialogueEvent?.Raise(curCraftEventData.reactions.B);

        ingredientPanel.gameObject.SetActive(false);
        curCraftEventData = default;
    }
}
