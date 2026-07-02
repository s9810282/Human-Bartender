using UnityEngine;

/// <summary>
/// 메인(타이틀) 씬 전용 매니저.
/// TempStart()를 호출하면 Play 씬으로 전환한다.
/// </summary>
public class MainManager : MonoBehaviour
{
    void Start() { }
    void Update() { }

    /// <summary>Play 씬으로 즉시 전환한다.</summary>
    public void TempStart()
    {
        SceneTransitionManager.Instance.LoadScene("Play");
    }
}
