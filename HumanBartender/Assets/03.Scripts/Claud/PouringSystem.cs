using UnityEngine;

namespace LiquidSimulation
{
    /// <summary>
    /// ★ 따르기 시스템 (v10)
    /// 
    /// 이 시스템은 기울기를 직접 제어하지 않음!
    /// LiquidContainer가 자신의 transform.rotation을 읽어서 TiltAngle을 설정하고,
    /// 기울기에 따라 액체가 자동으로 쏠림.
    /// 
    /// PouringSystem의 역할:
    /// (1) 소스와 타겟을 연결 (StartPouring)
    /// (2) 소스가 충분히 기울어져 있으면 rim에서 셀 제거
    /// (3) 제거된 셀을 타겟에 추가
    /// (4) 연결 해제 (StopPouring)
    /// 
    /// 기울기는 사용자가 직접 제어:
    /// - 에디터에서 rotation.z 슬라이더
    /// - ShakerInteraction으로 드래그
    /// - 코드에서 transform.rotation 설정
    /// - 애니메이션 등
    /// </summary>
    public class PouringSystem : MonoBehaviour
    {
        public static PouringSystem Instance { get; private set; }

        [Header("Pour")]
        [SerializeField] private int cellsPerTick = 2;

        // 연결 상태
        private bool isConnected = false;
        private LiquidContainer source;
        private LiquidContainer target;

        // 따르기 타이밍
        private float pourTimer = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        // ============================================================
        // 공개 인터페이스
        // ============================================================

        /// <summary>
        /// 소스와 타겟 연결.
        /// 소스가 기울어지면 자동으로 따라짐.
        /// 기울기는 사용자가 직접 제어해야 함.
        /// </summary>
        public void StartPouring(LiquidContainer from, LiquidContainer to)
        {
            source = from;
            target = to;
            isConnected = true;
            pourTimer = 0f;
            Debug.Log("[Pour] Connected: " + from.name + " → " + to.name);
        }

        /// <summary>
        /// 연결 해제. 기울기 복원은 하지 않음 (사용자가 직접).
        /// </summary>
        public void StopPouring()
        {
            isConnected = false;
            source = null;
            target = null;
            Debug.Log("[Pour] Disconnected");
        }

        public bool IsPouring => isConnected;

        /// <summary>
        /// 현재 연결된 소스 (외부에서 기울기 제어용)
        /// </summary>
        public LiquidContainer Source => source;

        /// <summary>
        /// 현재 연결된 타겟
        /// </summary>
        public LiquidContainer Target => target;

        // ============================================================
        // 업데이트
        // ============================================================

        private void Update()
        {
            if (!isConnected || source == null || target == null) return;

            // ★ 소스가 충분히 기울어져 있을 때만 따라짐
            if (!source.IsTiltedEnoughToPour()) return;

            // 기울기 크기에 비례하여 따르는 속도 증가
            float absTilt = Mathf.Abs(source.GetTiltAngle());
            float speedMul = Mathf.Lerp(0.3f, 3f, Mathf.Clamp01((absTilt - 35f) / 30f));
            float interval = 1f / (25f * speedMul);

            pourTimer += Time.deltaTime;
            while (pourTimer >= interval)
            {
                pourTimer -= interval;
                TransferTick();
            }
        }

        /// <summary>
        /// ★ 소스의 rim에서 셀 제거 → 타겟에 추가
        /// </summary>
        private void TransferTick()
        {
            var srcGrid = source.Grid;
            var tgtGrid = target.Grid;
            if (srcGrid == null || tgtGrid == null) return;

            for (int i = 0; i < cellsPerTick; i++)
            {
                int rimX, rimY;
                LiquidCell cell = source.RemoveFromRim(out rimX, out rimY);

                if (cell.IsEmpty)
                    return; // 입구에 도달한 액체 없음 → 대기

                // 타겟에 추가
                float dx = target.transform.position.x - source.transform.position.x;
                int addX;
                if (dx > 0)
                    addX = Mathf.Max(1, tgtGrid.Width / 5);
                else
                    addX = Mathf.Min(tgtGrid.Width - 2, tgtGrid.Width * 4 / 5);

                cell.velocityY = -1.5f;
                cell.velocityX = (dx > 0) ? 0.3f : -0.3f;
                tgtGrid.AddCellAtPosition(addX, cell);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
