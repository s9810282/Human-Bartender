using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사선 분할선 위에 놓이는 진행 게이지. 판정 하나가 끝날 때마다 칸 하나가 채워진다 —
/// 성공은 라임, 실패는 적색이라 지나온 시도의 성패가 한눈에 남는다.
///
/// 칸은 아래에서 위로 채워진다(프로토타입의 column-reverse). 에디터 셋업이 칸을 미리 만들어 두므로
/// 여기서는 색만 바꾼다.
/// </summary>
public class StirGaugeView : MonoBehaviour
{
    [Tooltip("아래에서 위 순서로 넣는다. 개수가 곧 표시 가능한 판정 수다.")]
    [SerializeField] Image[] segments;

    [Header("Color")]
    [SerializeField] Color idleColor = new Color(1f, 1f, 1f, 0.025f);
    [SerializeField] Color successColor = new Color(0.87f, 1f, 0.42f);
    [SerializeField] Color failColor = new Color(1f, 0.39f, 0.47f);

    void Awake() => ResetAll();

    public void ResetAll()
    {
        if (segments == null) return;

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] != null) segments[i].color = idleColor;
        }
    }

    /// <summary>index번째 칸을 결과 색으로 채운다. 칸 수보다 판정이 많으면 넘치는 건 그냥 무시한다.</summary>
    public void SetResult(int index, bool success)
    {
        if (segments == null || index < 0 || index >= segments.Length) return;
        if (segments[index] == null) return;

        segments[index].color = success ? successColor : failColor;
    }
}
