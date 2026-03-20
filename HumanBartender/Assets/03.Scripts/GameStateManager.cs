using UnityEngine;
using UnityEngine.Rendering;

public enum GameState
{
    None,
    Play,
    Effect,
    Loading,
    Trigger,
}


public class GameStateManager
{
    private static GameStateManager instance;
    public static GameStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameStateManager();
                instance.gameState = GameState.None;
            }
            return instance;
        }
    }



    #region Field

    GameState gameState = GameState.Play;
    bool isStart = false;

    #endregion

    #region Property
    public GameState CurrentGameState { get => gameState; set => gameState = value; }
    public bool IsStart { get => isStart; set => isStart = value; }

    #endregion
}
