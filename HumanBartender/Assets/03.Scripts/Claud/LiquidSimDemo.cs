using UnityEngine;

namespace LiquidSimulation
{
    /// <summary>
    /// 데모 씬 (v5) - 빈 씬에 이것만 추가하면 동작
    /// 
    /// 1~5: 잔에 액체    6~9: 쉐이커에 액체
    /// 좌클릭 드래그: 휘젓기    우클릭 드래그: 쉐이커 잡고 흔들기
    /// S 누르기: 쉐이커 직접 흔들기    P: 따르기    R: 리셋
    /// </summary>
    public class LiquidSimDemo : MonoBehaviour
    {
        [SerializeField] private int glassW = 32;
        [SerializeField] private int glassH = 48;
        [SerializeField] private int shakerW = 24;
        [SerializeField] private int shakerH = 56;

        private LiquidContainer glassCon;
        private LiquidContainer shakerCon;
        private LiquidRenderer glassRen;
        private LiquidRenderer shakerRen;
        private PouringSystem pour;
        private LiquidData[] liquids;

        private void Start()
        {
            MakeLiquids();
            MakeGlass();
            MakeShaker();
            MakePour();
            Debug.Log("=== Liquid Sim Demo v10 ===");
            Debug.Log("1-5:Glass液 6-9:Shaker液 | S:Shake R:Reset");
            Debug.Log("Q:Cube W:Sphere E:HalfMoon T:Crushed Y:Long (잔에 얼음)");
            Debug.Log("Shift+Q/W/T: 쉐이커에 얼음 | LMB:Stir RMB:GrabShaker");
            Debug.Log("★ P:따르기 연결/해제 | ←→:쉐이커 기울기 | ↑:기울기 리셋");
        }

        private void MakeLiquids()
        {
            liquids = new LiquidData[]
            {
                Liq("Grenadine",   LiquidType.Grenadine,   new Color32(180,20,40,255),  1.8f),
                Liq("Kahlua",      LiquidType.Kahlua,      new Color32(60,30,15,255),   1.4f),
                Liq("BlueCuracao", LiquidType.BlueCuracao,  new Color32(30,100,220,255), 1.1f),
                Liq("OJ",         LiquidType.OrangeJuice,  new Color32(255,160,40,255), 1.05f),
                Liq("Vodka",      LiquidType.Vodka,        new Color32(220,225,240,180),0.8f),
            };
        }

        private LiquidData Liq(string n, LiquidType t, Color32 c, float d)
        {
            var data = ScriptableObject.CreateInstance<LiquidData>();
            data.displayName = n;
            data.liquidType = t;
            data.color = c;
            data.density = d;
            data.viscosity = 0.3f;
            return data;
        }

        private void MakeGlass()
        {
            // 부모 오브젝트
            var obj = new GameObject("Glass");
            obj.transform.position = new Vector3(-1.5f, -1f, 0f);

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(glassW / 16f, glassH / 16f);
            col.offset = new Vector2(0f, glassH / 32f);

            // 자식: 렌더러
            var liq = new GameObject("LiquidSurface");
            liq.transform.SetParent(obj.transform);
            liq.transform.localPosition = Vector3.zero;

            glassRen = liq.AddComponent<LiquidRenderer>();
            glassRen.Initialize(glassW, glassH, 16);

            // 마스크: 사다리꼴
            var g = glassRen.Grid;
            for (int x = 0; x < glassW; x++)
                for (int y = 0; y < glassH; y++)
                    g.ContainerMask[x, y] = false;

            for (int y = 0; y < glassH; y++)
            {
                float t = (float)y / glassH;
                float ratio = Mathf.Lerp(0.5f, 0.9f, t);
                int hw = Mathf.RoundToInt(glassW * ratio * 0.5f);
                int cx = glassW / 2;
                for (int x = cx - hw; x < cx + hw; x++)
                {
                    if (x >= 0 && x < glassW)
                        g.ContainerMask[x, y] = true;
                }
            }
            g.BuildRowCache();
            glassRen.RefreshMaskCache();

            // 컨테이너
            glassCon = obj.AddComponent<LiquidContainer>();
            glassCon.Setup(glassRen, ContainerType.Glass, 800);

            obj.AddComponent<StirringInteraction>();

            Debug.Log("[Glass] " + g.Width + "x" + g.Height + " OK");
        }

        private void MakeShaker()
        {
            var obj = new GameObject("Shaker");
            obj.transform.position = new Vector3(2f, -1f, 0f);

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(shakerW / 16f, shakerH / 16f);
            col.offset = new Vector2(0f, shakerH / 32f);

            var liq = new GameObject("LiquidSurface");
            liq.transform.SetParent(obj.transform);
            liq.transform.localPosition = Vector3.zero;

            shakerRen = liq.AddComponent<LiquidRenderer>();
            shakerRen.Initialize(shakerW, shakerH, 16);

            // 마스크: 통 형태
            var g = shakerRen.Grid;
            for (int x = 0; x < shakerW; x++)
                for (int y = 0; y < shakerH; y++)
                    g.ContainerMask[x, y] = false;

            for (int y = 0; y < shakerH; y++)
            {
                float t = (float)y / shakerH;
                float ratio;
                if (t < 0.15f) ratio = Mathf.Lerp(0.5f, 0.85f, t / 0.15f);
                else if (t < 0.85f) ratio = 0.85f;
                else ratio = Mathf.Lerp(0.85f, 0.35f, (t - 0.85f) / 0.15f);

                int hw = Mathf.RoundToInt(shakerW * ratio * 0.5f);
                int cx = shakerW / 2;
                for (int x = cx - hw; x < cx + hw; x++)
                {
                    if (x >= 0 && x < shakerW)
                        g.ContainerMask[x, y] = true;
                }
            }
            g.BuildRowCache();
            shakerRen.RefreshMaskCache();

            shakerCon = obj.AddComponent<LiquidContainer>();
            shakerCon.Setup(shakerRen, ContainerType.Shaker, 600);

            obj.AddComponent<ShakerInteraction>();

            Debug.Log("[Shaker] " + g.Width + "x" + g.Height + " OK");
        }

        private void MakePour()
        {
            var obj = new GameObject("PouringSystem");
            pour = obj.AddComponent<PouringSystem>();
        }

        private void Update()
        {
            // 1~5: 잔에 추가
            for (int i = 0; i < liquids.Length; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    glassRen.Grid.AddLiquid(glassW / 2, 60, liquids[i]);
                    Debug.Log("[Glass] +" + liquids[i].displayName +
                        " total=" + glassRen.Grid.GetTotalLiquidCount());
                }
            }

            // 6~9: 쉐이커에 추가
            for (int i = 0; i < Mathf.Min(4, liquids.Length); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha6 + i))
                {
                    shakerRen.Grid.AddLiquid(shakerW / 2, 60, liquids[i]);
                    Debug.Log("[Shaker] +" + liquids[i].displayName +
                        " total=" + shakerRen.Grid.GetTotalLiquidCount());
                }
            }

            // ★ 얼음 추가
            // Shift 없음 → 잔에, Shift 있음 → 쉐이커에
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            LiquidGrid iceTarget = shift ? shakerRen.Grid : glassRen.Grid;
            string iceLabel = shift ? "[Shaker]" : "[Glass]";
            int iceW = shift ? shakerW : glassW;

            if (Input.GetKeyDown(KeyCode.Q))
            { iceTarget.AddIce(IceShape.Cube); Debug.Log(iceLabel + " +Ice Cube"); }
            if (Input.GetKeyDown(KeyCode.W))
            { iceTarget.AddIce(IceShape.Sphere); Debug.Log(iceLabel + " +Ice Sphere"); }
            if (Input.GetKeyDown(KeyCode.E))
            { iceTarget.AddIce(IceShape.HalfMoon); Debug.Log(iceLabel + " +Ice HalfMoon"); }
            if (Input.GetKeyDown(KeyCode.T))
            {
                int count = Random.Range(3, 6);
                for (int i = 0; i < count; i++)
                {
                    float rx = iceW * 0.3f + Random.Range(0f, iceW * 0.4f);
                    iceTarget.AddIce(IceShape.Crushed, rx);
                }
                Debug.Log(iceLabel + " +Crushed Ice x" + count);
            }
            if (Input.GetKeyDown(KeyCode.Y))
            { iceTarget.AddIce(IceShape.Long); Debug.Log(iceLabel + " +Ice Long"); }

            // P: 따르기 연결/해제
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (pour.IsPouring)
                {
                    pour.StopPouring();
                    Debug.Log("[Pour] Disconnected - 기울기 복원은 ↑키");
                }
                else
                {
                    pour.StartPouring(shakerCon, glassCon);
                    Debug.Log("[Pour] Connected - ←→키로 쉐이커 기울이기");
                }
            }

            // ★ ←→: 쉐이커 수동 기울기
            // 에디터에서 직접 rotation을 돌려도 동일하게 동작
            float tiltInput = 0f;
            if (Input.GetKey(KeyCode.LeftArrow))  tiltInput = 60f;   // CCW
            if (Input.GetKey(KeyCode.RightArrow)) tiltInput = -60f;  // CW

            if (Mathf.Abs(tiltInput) > 0.1f)
            {
                Vector3 euler = shakerCon.transform.eulerAngles;
                euler.z += tiltInput * Time.deltaTime;
                // -90 ~ 90 범위 제한
                float z = euler.z;
                if (z > 180f) z -= 360f;
                z = Mathf.Clamp(z, -90f, 90f);
                shakerCon.transform.rotation = Quaternion.Euler(0, 0, z);
            }

            // ★ ↑: 기울기 리셋 (서서히 복원)
            if (Input.GetKey(KeyCode.UpArrow))
            {
                float z = shakerCon.transform.eulerAngles.z;
                if (z > 180f) z -= 360f;
                z = Mathf.MoveTowards(z, 0f, 80f * Time.deltaTime);
                shakerCon.transform.rotation = Quaternion.Euler(0, 0, z);
            }

            // R: 리셋
            if (Input.GetKeyDown(KeyCode.R))
            {
                pour.StopPouring();
                Clear(glassRen.Grid);
                Clear(shakerRen.Grid);
                // 회전도 리셋
                shakerCon.transform.rotation = Quaternion.identity;
                Debug.Log("Reset!");
            }
        }

        private void Clear(LiquidGrid g)
        {
            if (g == null) return;
            for (int x = 0; x < g.Width; x++)
                for (int y = 0; y < g.Height; y++)
                    g.Cells[x, y] = LiquidCell.Empty;
            g.ClearIce(); // ★ 얼음도 제거
        }
    }
}
