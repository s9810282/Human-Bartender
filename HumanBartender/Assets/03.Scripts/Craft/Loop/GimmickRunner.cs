using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 기믹 큐를 앞에서부터 하나씩 실행한다.
///
/// 무슨 기믹인지에 따라 프리팹을 띄우고, 끝나면 결과를 받아 제조 기록에 붙이고, 다음으로 넘긴다.
/// 각 기믹이 안에서 무엇을 하는지는 모른다. 그래서 기믹이 늘어나도 이 클래스는 그대로다.
///
/// 한 잔 전체의 시계도 여기서 돌린다. 시계는 기믹이 아니라 제조에 속한 것이라, 기믹이 바뀌어도
/// 멈추거나 초기화되지 않고 이어져야 하기 때문이다.
/// </summary>
public class GimmickRunner : MonoBehaviour
{
    [Serializable]
    public struct GimmickPrefabEntry
    {
        public ECraftGimmick type;
        public GameObject prefab;
    }

    [Tooltip("기믹 종류별 프리팹. 필업은 비워 둬도 된다 — 따르기와 같은 화면을 쓰므로 따르기 것을 대신 쓴다.")]
    [SerializeField] GimmickPrefabEntry[] prefabs;

    [Tooltip("띄운 기믹을 놓을 자리. 비어 있으면 이 오브젝트 아래에 만든다.")]
    [SerializeField] Transform gimmickRoot;

    [Tooltip("기믹이 바뀌어도 남아 있는 공통 표시. 비워두면 표시 없이 기믹만 돈다.")]
    [SerializeField] CraftGimmickHud hud;

    [Tooltip("기믹만 비추는 카메라. 바에서 멀리 떨어진 자리를 찍으므로 바의 배경과 손님이 함께 담기지 않는다.")]
    [SerializeField] Camera gimmickCamera;

    [Tooltip("기믹 화면이 자리잡는 그리기 순서의 바닥. 바 UI가 쓰는 값보다 확실히 높아야 한다. " +
             "막은 이 값 바로 아래, 기믹은 이 값 위, 공통 표시는 그보다 더 위에 놓인다.")]
    [SerializeField] int sortingBase = 1000;

    [Tooltip("공통 표시를 기믹보다 얼마나 위에 둘지. 기믹 안에서 쓰는 순서보다 커야 한다.")]
    [SerializeField] int hudSortingOffset = 500;

    [Inject] IObjectResolver resolver;

    CraftTimer runningTimer;

    /// <summary>제조 중 잠시 꺼 둔 바 화면의 UI. 끝나면 그대로 되돌린다.</summary>
    readonly List<Canvas> hiddenBarCanvases = new();

    /// <summary>지금 돌고 있는 기믹. 시계를 재울지 굴릴지 이 기믹에게 묻는다.</summary>
    ICraftGimmick currentGimmick;

    /// <summary>기믹 하나가 끝날 때마다 발생한다. 진행 표시를 갱신하는 쪽에서 쓴다.</summary>
    public event Action<GimmickStep, GimmickResult> GimmickFinished;

    void Update()
    {
        // 플레이어가 실제로 조작할 수 있는 동안만 시간을 센다. 기믹 사이의 전환, 성공 연출,
        // 프리팹을 띄우는 사이의 빈 시간은 손쓸 수 없는 구간이라 빼는 게 맞다.
        if (currentGimmick == null || !currentGimmick.IsManualInputActive) return;

        runningTimer?.Tick(Time.deltaTime);
    }

    /// <summary>
    /// 큐를 끝까지 실행한다. 마지막 기믹의 결과까지 기록한 뒤 제조를 확정하고 돌아온다.
    /// </summary>
    public async UniTask RunAsync(CraftSession session, GimmickQueue queue,
                                  CraftRunDisplay display, CancellationToken token)
    {
        if (session == null || queue == null) return;

        session.BeginGimmicks();
        runningTimer = session.Timer;

        ShowCraftScreen(true);
        hud?.BeginCraft(session.Timer, display.TimeLimitSec, display.HideTime);

        try
        {
            for (int i = 0; i < queue.Count; i++)
            {
                GimmickStep step = queue.Steps[i];

                Debug.Log($"[GimmickRunner] {i + 1}/{queue.Count} 시작 — {step}");

                GimmickResult result = await PlayStepAsync(step, session.Timer, token);

                Debug.Log($"[GimmickRunner] {i + 1}/{queue.Count} 종료 — {step}" +
                          (result == null ? " (결과 없음)" : $" / 종료방식 {result.EndType}"));

                if (result != null)
                {
                    session.Actual.Record(result);
                    GimmickFinished?.Invoke(step, result);
                }

                session.AdvanceGimmick();
            }
        }
        finally
        {
            // 도중에 취소되더라도 시계는 멈춰야 한다. 계속 돌면 다음 제조의 시간까지 얹힌다.
            runningTimer = null;
            hud?.EndCraft();
            ShowCraftScreen(false);
        }

        session.Complete();
    }

    /// <summary>기믹 프리팹을 띄우고 끝날 때까지 기다린 뒤 치운다.</summary>
    async UniTask<GimmickResult> PlayStepAsync(GimmickStep step, CraftTimer timer, CancellationToken token)
    {
        GameObject prefab = ResolvePrefab(step.Type);

        if (prefab == null)
        {
            Debug.LogError($"[GimmickRunner] '{step.Type}' 기믹의 프리팹이 없습니다. 이 스텝을 건너뜁니다: {step}");
            return null;
        }

        GameObject instance = Instantiate(prefab, gimmickRoot != null ? gimmickRoot : transform);

        LiftAboveBarUi(instance);

        try
        {
            var gimmick = instance.GetComponentInChildren<ICraftGimmick>();

            if (gimmick == null)
            {
                Debug.LogError($"[GimmickRunner] '{prefab.name}'에 ICraftGimmick 구현이 없습니다.");
                return null;
            }

            // 기믹 안에서도 데이터나 사운드 같은 걸 주입받을 수 있어야 한다.
            resolver?.Inject(gimmick);

            // 진행 표시를 이 기믹에 연결한다. 기믹이 시작되기 전에 붙여야 첫 프레임부터 값이 보인다.
            hud?.BindGimmick(step, gimmick);

            currentGimmick = gimmick;

            return await gimmick.PlayAsync(step, timer, token);
        }
        finally
        {
            // 이 기믹이 사라진 뒤에도 시계가 돌면, 다음 기믹을 띄우는 사이의 시간이 끼어든다.
            currentGimmick = null;

            hud?.UnbindGimmick();

            if (instance != null) Destroy(instance);
        }
    }

    /// <summary>
    /// 제조 화면을 켜고 끈다.
    ///
    /// 가리는 일이 둘로 나뉜다. 배경과 손님 같은 월드는 기믹 카메라가 단색으로 지우고 그 위에 그린다.
    /// 화면에 겹쳐 그리는 UI는 카메라와 무관하게 맨 위에 나오므로 카메라로는 지워지지 않는다 —
    /// 그건 덮지 않고 아예 꺼 버린다.
    ///
    /// 덮지 않는 이유가 있다. 겹쳐 그리는 막으로 바 UI를 가리면 그 막이 기믹의 월드 부분까지
    /// 함께 덮어 버린다. 병이나 고리처럼 UI가 아닌 기믹은 그렇게 하면 보이지 않는다.
    /// </summary>
    void ShowCraftScreen(bool visible)
    {
        if (visible)
        {
            HideBarCanvases();
            MatchMainCameraProjection();
        }
        else
        {
            RestoreBarCanvases();
        }

        if (gimmickCamera != null) gimmickCamera.gameObject.SetActive(visible);
        else Debug.LogError("[GimmickRunner] 기믹 카메라가 없습니다. 바 화면이 뒤에 그대로 보입니다.");
    }

    /// <summary>
    /// 바 화면의 UI를 잠시 끈다. 제조 루프가 가진 것(공통 표시, 띄운 기믹)은 건드리지 않는다.
    ///
    /// 어떤 UI가 있는지 미리 알아 두지 않고 그때그때 찾는다. 바에 UI가 늘어나도 여기를 고칠 일이
    /// 없고, 무엇을 꺼야 하는지 목록으로 관리하다 빠뜨리는 일도 생기지 않는다.
    /// </summary>
    void HideBarCanvases()
    {
        // 되돌리기 전에 또 부르면 껐던 목록을 잃어버려 바 UI가 영영 꺼진 채로 남는다.
        if (hiddenBarCanvases.Count > 0)
        {
            Debug.LogWarning("[GimmickRunner] 바 UI를 이미 꺼 둔 상태에서 또 껐습니다. 먼저 되돌립니다.");
            RestoreBarCanvases();
        }

        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var canvas in canvases)
        {
            if (!canvas.isRootCanvas || !canvas.enabled) continue;

            // 겹쳐 그리는 UI만 문제가 된다. 카메라에 붙거나 월드에 놓인 UI는 기믹 카메라가 알아서 지운다.
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;

            // 제조 루프가 가진 것은 남긴다.
            if (canvas.transform.IsChildOf(transform)) continue;

            canvas.enabled = false;
            hiddenBarCanvases.Add(canvas);
        }

        Debug.Log($"[GimmickRunner] 바 UI {hiddenBarCanvases.Count}개를 껐습니다: " +
                  string.Join(", ", hiddenBarCanvases.ConvertAll(c => c.name)));
    }

    /// <summary>꺼 뒀던 바 UI를 되돌린다. 그 사이 사라진 것은 건너뛴다.</summary>
    void RestoreBarCanvases()
    {
        int restored = 0;

        foreach (var canvas in hiddenBarCanvases)
        {
            if (canvas == null) continue;

            canvas.enabled = true;
            restored++;
        }

        if (hiddenBarCanvases.Count > 0)
            Debug.Log($"[GimmickRunner] 바 UI {restored}/{hiddenBarCanvases.Count}개를 되돌렸습니다.");

        hiddenBarCanvases.Clear();
    }

    /// <summary>
    /// 기믹 카메라의 화각을 메인 카메라와 맞춘다.
    ///
    /// 기믹은 바에서 멀리 떨어진 자리에 놓고 그곳만 따로 찍는다. 레이어를 나누거나 메인 카메라의
    /// 설정을 건드리지 않고도 바와 완전히 분리되는 방법이라, 기믹이 늘어도 손댈 곳이 없다.
    /// 다만 화면에 담기는 크기는 같아야 원래 만든 대로 보이므로 투영값만 그때그때 복사한다.
    /// </summary>
    void MatchMainCameraProjection()
    {
        Camera main = Camera.main;
        if (main == null) return;

        gimmickCamera.orthographic = main.orthographic;
        gimmickCamera.orthographicSize = main.orthographicSize;
        gimmickCamera.fieldOfView = main.fieldOfView;
        gimmickCamera.nearClipPlane = main.nearClipPlane;
        gimmickCamera.farClipPlane = main.farClipPlane;
    }

    /// <summary>
    /// 띄운 기믹의 캔버스를 바 UI 위로 올린다.
    ///
    /// 프리팹마다 자기 기준으로 그리기 순서를 잡아 뒀는데, 그 값이 바 화면이 쓰는 범위와 겹친다.
    /// 프리팹을 일일이 고치는 대신 여기서 통째로 밀어 올린다 — 기믹 안에서의 앞뒤 관계는 그대로 두고
    /// 전체만 옮기는 것이라, 프리팹이 새로 늘어도 이 코드가 알아서 처리한다.
    /// </summary>
    void LiftAboveBarUi(GameObject instance)
    {
        var canvases = instance.GetComponentsInChildren<Canvas>(true);

        foreach (var canvas in canvases)
        {
            // 자식 캔버스는 부모를 따라가므로 건드리지 않는다. 건드리면 부모 안에서의 순서가 뒤집힌다.
            if (!canvas.isRootCanvas) continue;

            canvas.overrideSorting = true;
            canvas.sortingOrder += sortingBase;

            // 카메라에 붙는 캔버스는 메인이 아니라 무대 카메라를 봐야 한다. 프리팹에 남아 있는
            // 참조는 씬 밖 오브젝트라 어차피 끊겨 있어서, 여기서 다시 이어 준다.
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && gimmickCamera != null)
                canvas.worldCamera = gimmickCamera;
        }

        if (hud != null) hud.SetSortingOrder(sortingBase + hudSortingOffset);
    }

    /// <summary>
    /// 기믹 종류에 맞는 프리팹을 찾는다.
    /// 필업은 전용 프리팹이 없으면 따르기 것을 쓴다 — 둘은 같은 화면을 쓰기 때문이다.
    /// </summary>
    GameObject ResolvePrefab(ECraftGimmick type)
    {
        GameObject found = FindPrefab(type);
        if (found != null) return found;

        if (type == ECraftGimmick.FillUp) return FindPrefab(ECraftGimmick.Pour);

        return null;
    }

    GameObject FindPrefab(ECraftGimmick type)
    {
        if (prefabs == null) return null;

        foreach (var entry in prefabs)
        {
            if (entry.type == type && entry.prefab != null) return entry.prefab;
        }

        return null;
    }
}
