using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// Play 씬 진입 시 1부(TycoonFlow)와 2부(StoryFlow)를 순서대로 실행하는 중간관리자.
///
/// 2부는 원래 VisualNovelFlow가 맡았는데 그쪽은 옛 DayDataSO를 본다. 그 진입점은 그대로 두고
/// 여기서 부르는 대상만 script/bar/dayN.json을 읽는 StoryFlow로 바꿨다.
/// 각 Flow는 이 컨트롤러가 호출할 때만 시작되며, 완료(RunAsync 반환) 시 다음 국면으로 넘어간다.
/// </summary>
public class PlayPhaseController : MonoBehaviour
{
    [SerializeField] TycoonFlow tycoonFlow;

    [Tooltip("2부 대본 국면. script/bar/dayN.json을 실행한다.")]
    [SerializeField] StoryFlow storyFlow;

    // ── 국면별 화면 ────────────────────────────────────────────────
    //
    // 어느 국면에 무엇이 보이는지는 국면을 넘기는 이곳이 정한다. 각 Flow가 상대편 화면을 끄게 하면
    // 1부는 2부 화면을, 2부는 1부 화면을 알아야 해서 둘이 서로를 붙들게 된다.

    [Header("국면별 화면")]
    [Tooltip("1부에만 보이는 것. 손님 자리(Tycoon Resource), 코스터 트레이 캔버스, 제조 슬라이드 패널 캔버스.")]
    [SerializeField] GameObject[] tycoonOnlyObjects;

    [Tooltip("2부에만 보이는 것. 좌석 인물(Story Resource).")]
    [SerializeField] GameObject[] storyOnlyObjects;

    public EPlayPhase CurrentPhase { get; private set; } = EPlayPhase.Tycoon;

    private void Start()
    {
        RunDayAsync().Forget();
    }

    public void OpenBar()
    {
        RunDayAsync().Forget();
    }

    private async UniTask RunDayAsync()
    {
        CurrentPhase = EPlayPhase.Tycoon;
        ShowPhase(tycoon: true);
        await tycoonFlow.RunAsync();

        CurrentPhase = EPlayPhase.Dialogue;
        ShowPhase(tycoon: false);
        await storyFlow.RunAsync();
    }

    /// <summary>
    /// 지금 국면의 화면만 남긴다.
    ///
    /// 1부 화면을 2부에서 끄는 것이 핵심이다 — 코스터 트레이와 제조 패널은 Screen Space 캔버스라
    /// 그냥 두면 대사 위에 그대로 얹히고, 클릭도 먼저 먹는다.
    /// </summary>
    void ShowPhase(bool tycoon)
    {
        SetActive(tycoonOnlyObjects, tycoon);
        SetActive(storyOnlyObjects, !tycoon);
    }

    static void SetActive(GameObject[] objects, bool on)
    {
        if (objects == null) return;

        foreach (var target in objects)
        {
            if (target != null) target.SetActive(on);
        }
    }
}
