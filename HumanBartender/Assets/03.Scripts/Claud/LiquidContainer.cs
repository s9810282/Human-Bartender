using UnityEngine;

namespace LiquidSimulation
{
    /// <summary>
    /// 액체 용기 (v10 - 에디터 배치용)
    /// 
    /// ★ 사용법:
    /// 1. 빈 오브젝트 생성 (이름: "Glass", "Shaker" 등)
    /// 2. 자식 오브젝트 생성 (이름: "Liquid")
    /// 3. 자식에 LiquidRenderer 추가 → 크기 설정
    /// 4. 부모에 LiquidContainer 추가
    /// 5. 인스펙터에서 설정:
    ///    - Container Type: Glass / Shaker / Bottle
    ///    - Mask Source: Preset(자동 생성) 또는 Texture(텍스처 지정)
    ///    - Max Capacity: 최대 셀 수
    ///    - Initial Liquids: 시작 시 채울 액체들
    /// 6. 필요한 인터랙션 컴포넌트 추가 (StirringInteraction, ShakerInteraction)
    /// 7. 플레이!
    /// </summary>
    public class LiquidContainer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LiquidRenderer liquidRenderer;

        [Header("Container")]
        [SerializeField] private ContainerType containerType = ContainerType.Glass;
        [SerializeField] private int maxCapacity = 500;

        [Header("Mask")]
        [SerializeField] private MaskSource maskSource = MaskSource.Preset;
        [SerializeField] private Texture2D maskTexture;  // maskSource=Texture일 때

        [Header("Preset Shape Parameters")]
        [Tooltip("바닥 너비 비율 (0~1). 1이면 직사각형, 0.3이면 아래가 좁은 사다리꼴")]
        [Range(0.1f, 1f)]
        [SerializeField] private float bottomWidthRatio = 0.5f;
        [Tooltip("입구 너비 비율 (0~1). 1이면 꽉 찬 너비")]
        [Range(0.1f, 1f)]
        [SerializeField] private float topWidthRatio = 0.9f;
        [Tooltip("쉐이커/병 입구 좁아지는 비율 (0~1). 0이면 안 좁아짐")]
        [Range(0f, 0.8f)]
        [SerializeField] private float neckNarrowRatio = 0f;
        [Tooltip("목이 시작되는 높이 비율 (0~1)")]
        [Range(0.5f, 1f)]
        [SerializeField] private float neckStartRatio = 0.85f;

        [Header("Pour / Tilt")]
        [SerializeField] private float minTiltToPour = 35f;

        [Header("Simulation")]
        [SerializeField] private int ticksPerSecond = 25;

        [Header("Pour Points")]
        [SerializeField] private Transform pourPoint;
        [SerializeField] private Transform receivePoint;

        [Header("Initial Liquids (시작 시 자동 추가)")]
        [SerializeField] private InitialLiquid[] initialLiquids;

        private float simTimer = 0f;

        public LiquidGrid Grid => liquidRenderer?.Grid;
        public LiquidRenderer Renderer => liquidRenderer;
        public ContainerType Type => containerType;

        // ============================================================
        // 초기화
        // ============================================================

        private bool maskReady = false;

        private void Start()
        {
            // 렌더러 자동 탐색
            if (liquidRenderer == null)
                liquidRenderer = GetComponentInChildren<LiquidRenderer>();

            if (liquidRenderer == null)
            {
                Debug.LogError($"[{name}] LiquidRenderer not found! 자식 오브젝트에 추가해주세요.");
                return;
            }
        }

        /// <summary>
        /// ★ Update에서 Grid가 준비되면 마스크 설정 (초기화 순서 안전)
        /// </summary>
        private void EnsureMaskReady()
        {
            if (maskReady) return;
            if (Grid == null) return; // 렌더러 아직 초기화 안 됨 → 다음 프레임에 재시도

            SetupMask();
            maskReady = true;
        }

        /// <summary>
        /// ★ 마스크 설정 - 프리셋 또는 텍스처 기반
        /// </summary>
        private void SetupMask()
        {
            if (Grid == null)
            {
                Debug.LogError($"[{name}] Grid is null. LiquidRenderer가 초기화되지 않았습니다.");
                return;
            }

            int w = liquidRenderer.GridWidth;
            int h = liquidRenderer.GridHeight;

            if (maskSource == MaskSource.Texture && maskTexture != null)
            {
                // 텍스처 기반 마스크
                Grid.SetMaskFromTexture(maskTexture);
            }
            else
            {
                // ★ 프리셋 기반 마스크 자동 생성
                GeneratePresetMask(w, h);
                Grid.BuildRowCache();
            }

            liquidRenderer.RefreshMaskCache();

            // ★ 초기 액체 추가
            if (initialLiquids != null)
            {
                foreach (var il in initialLiquids)
                {
                    if (il.liquidData != null && il.amount > 0)
                        Grid.AddLiquid(w / 2, il.amount, il.liquidData);
                }
            }

            Debug.Log($"[{name}] Setup complete: {containerType}, {w}x{h}, " +
                      $"mask={maskSource}, liquids={Grid.GetTotalLiquidCount()}");
        }

        /// <summary>
        /// ★ ContainerType과 파라미터로 마스크 자동 생성
        /// </summary>
        private void GeneratePresetMask(int w, int h)
        {
            // 전체 false로 초기화
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    Grid.ContainerMask[x, y] = false;

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;  // 0(바닥) ~ 1(꼭대기)
                float widthRatio;

                if (t < neckStartRatio)
                {
                    // 본체: bottomWidthRatio → topWidthRatio 선형 보간
                    float bodyT = t / neckStartRatio;
                    widthRatio = Mathf.Lerp(bottomWidthRatio, topWidthRatio, bodyT);
                }
                else
                {
                    // 목: topWidthRatio → (topWidthRatio * (1 - neckNarrowRatio))
                    float neckT = (t - neckStartRatio) / (1f - neckStartRatio);
                    float neckWidth = topWidthRatio * (1f - neckNarrowRatio);
                    widthRatio = Mathf.Lerp(topWidthRatio, neckWidth, neckT);
                }

                int halfW = Mathf.RoundToInt(w * widthRatio * 0.5f);
                int cx = w / 2;
                for (int x = cx - halfW; x < cx + halfW; x++)
                {
                    if (x >= 0 && x < w)
                        Grid.ContainerMask[x, y] = true;
                }
            }
        }

        /// <summary>
        /// 코드에서 초기화할 때 사용 (LiquidSimDemo 호환)
        /// 이 경우 마스크도 코드에서 직접 설정하므로 자동 마스크 생성 건너뜀
        /// </summary>
        public void Setup(LiquidRenderer renderer, ContainerType type, int capacity)
        {
            liquidRenderer = renderer;
            containerType = type;
            maxCapacity = capacity;
            maskReady = true; // ★ 코드에서 직접 설정 → 자동 마스크 건너뜀
        }

        // ============================================================
        // 업데이트
        // ============================================================

        private void Update()
        {
            if (Grid == null) return;

            // ★ 첫 프레임: 마스크 초기화 (렌더러 초기화 후)
            EnsureMaskReady();

            // ★ transform.rotation.z → TiltAngle 자동 동기화
            SyncRotationToTilt();

            // 시뮬레이션 틱
            float interval = 1f / ticksPerSecond;
            simTimer += Time.deltaTime;
            while (simTimer >= interval)
            {
                simTimer -= interval;
                Grid.SimulationStep();
            }
        }

        private void SyncRotationToTilt()
        {
            float zRot = transform.eulerAngles.z;
            if (zRot > 180f) zRot -= 360f;
            Grid.TiltAngle = zRot;
        }

        // ============================================================
        // 공개 인터페이스
        // ============================================================

        public void AddLiquid(LiquidData data, int amount)
        {
            if (Grid == null || data == null) return;
            int actual = Mathf.Min(amount, maxCapacity - Grid.GetTotalLiquidCount());
            if (actual <= 0) return;
            Grid.AddLiquid(Grid.Width / 2, actual, data);
        }

        public void Stir(Vector2 worldPos, float strength)
        {
            if (Grid == null || liquidRenderer == null) return;
            Vector2Int gp = liquidRenderer.WorldToGrid(worldPos);
            Grid.ApplyVortex(new Vector2(gp.x, gp.y), strength, Grid.Width * 0.35f);
        }

        public void Shake(float intensity)
        {
            if (Grid == null) return;
            Grid.ShakeAll(intensity);
        }

        public LiquidCell RemoveFromTop()
        {
            if (Grid == null) return LiquidCell.Empty;
            return Grid.RemoveLiquidFromTop(Grid.Width / 2);
        }

        public LiquidCell RemoveFromRim(out int rimX, out int rimY)
        {
            if (Grid == null) { rimX = 0; rimY = 0; return LiquidCell.Empty; }
            float tilt = Grid.TiltAngle;
            bool fromLeft = tilt > 0;
            return Grid.RemoveLiquidFromRim(fromLeft, out rimX, out rimY);
        }

        public bool IsTiltedEnoughToPour()
        {
            return Grid != null && Mathf.Abs(Grid.TiltAngle) >= minTiltToPour;
        }

        public float GetTiltAngle()
        {
            return Grid != null ? Grid.TiltAngle : 0f;
        }

        public float GetFillRatio()
        {
            if (Grid == null) return 0f;
            return (float)Grid.GetTotalLiquidCount() / maxCapacity;
        }

        public Vector3 GetPourPosition()
            => pourPoint != null ? pourPoint.position : transform.position + Vector3.up * 2f;

        public Vector3 GetReceivePosition()
            => receivePoint != null ? receivePoint.position : transform.position + Vector3.up * 1.5f;
    }

    // ============================================================
    // 지원 타입
    // ============================================================

    public enum ContainerType
    { Glass, Shaker, Bottle, MixingGlass, ShotGlass }

    public enum MaskSource
    { Preset, Texture }

    /// <summary>
    /// 인스펙터에서 시작 시 채울 액체를 설정
    /// </summary>
    [System.Serializable]
    public class InitialLiquid
    {
        public LiquidData liquidData;
        public int amount = 60;
    }
}
