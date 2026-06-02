using UnityEngine;

public enum EGameFlow
{
    Bar,
    Attendance,
    OffWork,
    Home,
}


public class GameManager
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameManager();
                instance.Init();
            }

            return instance;
        }
    }

    public EGameFlow GameFlow { get => gameFlow; set => gameFlow = value; }

    EGameFlow gameFlow = EGameFlow.Bar;

    private void Init()
    {
        gameFlow = EGameFlow.Bar;
    }
}
