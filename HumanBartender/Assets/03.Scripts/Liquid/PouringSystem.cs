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
            if (isConnected && source != null && target != null)
            {
                // ★ 소스가 충분히 기울어져 있을 때만 따라짐
                if (source.IsTiltedEnoughToPour())
                {
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
            }

            // ★ 직접 따르기 업데이트
            UpdateDirectPour();
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

        // ============================================================
        // ★ 직접 따르기 (소스 용기 없이)
        // ============================================================

        // 직접 따르기 상태
        private bool isDirectPouring = false;
        private LiquidContainer directTarget;
        private LiquidData directLiquidData;
        private int directRemaining;     // 남은 셀 수
        private int directCellsPerTick = 2;
        private float directPourTimer = 0f;
        private float directPourSpeed = 25f;

        /// <summary>
        /// ★ 소스 용기 없이 LiquidData로 직접 잔에 따르기.
        /// 화면에 Glass만 있을 때 액체를 채우는 연출용.
        /// 
        /// 사용법:
        ///   PouringSystem.Instance.StartDirectPour(rumData, glass, 80);
        /// 
        /// SO 이벤트에서 호출하려면 래퍼 스크립트에서:
        ///   public void PourRum() => PouringSystem.Instance.StartDirectPour(rumData, glass, 80);
        /// </summary>
        /// <param name="data">따를 액체 종류</param>
        /// <param name="to">받는 용기</param>
        /// <param name="amount">총 셀 수</param>
        /// <param name="speed">초당 셀 수 (기본 25)</param>
        /// <param name="perTick">틱당 셀 수 (기본 2)</param>
        public void StartDirectPour(LiquidData data, LiquidContainer to,
            int amount, float speed = 25f, int perTick = 2)
        {
            // 기존 연결이 있으면 중단
            if (isConnected) StopPouring();
            if (isDirectPouring) StopDirectPour();

            directTarget = to;
            directLiquidData = data;
            directRemaining = amount;
            directPourSpeed = speed;
            directCellsPerTick = perTick;
            directPourTimer = 0f;
            isDirectPouring = true;

            Debug.Log($"[Pour] Direct: {data.displayName} × {amount} → {to.name}");
        }

        /// <summary>
        /// 직접 따르기 중단.
        /// </summary>
        public void StopDirectPour()
        {
            isDirectPouring = false;
            directTarget = null;
            directLiquidData = null;
            directRemaining = 0;
            Debug.Log("[Pour] Direct stopped");
        }

        /// <summary>
        /// 직접 따르기 중인지
        /// </summary>
        public bool IsDirectPouring => isDirectPouring;

        /// <summary>
        /// 남은 따르기 양
        /// </summary>
        public int DirectRemaining => directRemaining;

        private void UpdateDirectPour()
        {
            if (!isDirectPouring || directTarget == null || directLiquidData == null) return;
            if (directRemaining <= 0)
            {
                StopDirectPour();
                return;
            }

            var grid = directTarget.Grid;
            if (grid == null) return;

            directPourTimer += Time.deltaTime;
            float interval = 1f / directPourSpeed;

            while (directPourTimer >= interval && directRemaining > 0)
            {
                directPourTimer -= interval;

                for (int i = 0; i < directCellsPerTick && directRemaining > 0; i++)
                {
                    // ★ 셀 생성: LiquidData에서 속성 복사
                    LiquidCell cell = new LiquidCell
                    {
                        liquidType = directLiquidData.liquidType,
                        color = directLiquidData.color,
                        density = directLiquidData.density,
                        mixRatio = 0f,
                        velocityX = Random.Range(-0.3f, 0.3f),
                        velocityY = -1.5f  // 위에서 떨어지는 느낌
                    };

                    // 잔 상단 중앙 부근에 추가
                    int addX = grid.Width / 2 + Random.Range(-2, 3);
                    addX = Mathf.Clamp(addX, 1, grid.Width - 2);

                    if (grid.AddCellAtPosition(addX, cell))
                        directRemaining--;
                }
            }
        }
    }
}
