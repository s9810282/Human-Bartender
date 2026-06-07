using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

public enum GameState
{
    None,
    Play,
    Effect,
    Loading,
    MiniGame,
}

[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum EGameFlow
{
    Bar,

    [EnumMember(Value = "commute_in")]
    CommuteIn,

    [EnumMember(Value = "commute_out")]
    CommuteOut,

    Home,
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
                instance.Init();
            }
            return instance;
        }
    }



    #region Field

    GameState gameState = GameState.Play;
    EGameFlow gameFlow = EGameFlow.Bar;
    bool isDialogInitStart = false;
    bool isOutsideLoad = false;
    int currentDay = 0;

    #endregion

    #region Property
    public GameState CurrentGameState { get => gameState; set => gameState = value; }
    public EGameFlow GameFlow { get => gameFlow; set => gameFlow = value; }
    public bool IsDialogInitStart { get => isDialogInitStart; set => isDialogInitStart = value; }
    public int CurrentDay { get => currentDay; set => currentDay = value; }
    #endregion

    private void Init()
    {
        gameFlow = EGameFlow.Bar;
        currentDay = 0;

        isOutsideLoad = false;
    }
}
