using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectLuna.WebAnimatic.Editor
{
    /// <summary>
    /// 웹 애니매틱 내보내기 번들(Data/)로부터 라이브러리 에셋과 재생 씬을 생성한다.
    /// 엔진의 기존 시트 텍스처(Resources/Cutscenes/Lab, 01.Sprites/Ch/luna)를 그대로 참조하며,
    /// 프레임 절단은 웹과 동일한 rect 메타를 쓴다.
    /// </summary>
    public static class WebAnimaticImporter
    {
        const string Root = "Assets/99.CutsceneProto/WebAnimatic";
        const string LibraryPath = Root + "/WebAnimaticLibrary.asset";
        const string ScenePath = Root + "/WebAnimatic.unity";

        static readonly (string id, string label)[] SceneList =
        {
            ("s1", "S#1 벡터그룹 습격"),
            ("s2", "S#2 도망을 가는 이들"),
            ("s3", "S#3 마지막 대화 + 낙하"),
            ("s5", "S#5 루나의 꿈 (회상)"),
            ("s99", "S#99 효과 테스트"),
        };

        [MenuItem("Window/Project L.U.N.A/Web Animatic/Build All")]
        public static void BuildAll()
        {
            PrepareStageTextures();
            WebAnimaticLibrary lib = BuildLibrary();
            BuildScene(lib);
            Debug.Log("[WebAnimatic] Build All 완료");
        }

        static void PrepareStageTextures()
        {
            if (Directory.Exists(Root + "/Data/sheets"))
                foreach (string path in Directory.GetFiles(Root + "/Data/sheets", "*.png"))
                {
                    string p = path.Replace('\\', '/');
                    var si = (TextureImporter)AssetImporter.GetAtPath(p);
                    if (si == null) { AssetDatabase.ImportAsset(p); si = (TextureImporter)AssetImporter.GetAtPath(p); }
                    si.textureType = TextureImporterType.Default;
                    si.filterMode = FilterMode.Point;
                    si.textureCompression = TextureImporterCompression.Uncompressed;
                    si.mipmapEnabled = false;
                    si.maxTextureSize = 4096;
                    si.SaveAndReimport();
                }
            foreach (string path in Directory.GetFiles(Root + "/Data/stages", "*.png"))
            {
                string p = path.Replace('\\', '/');
                var imp = (TextureImporter)AssetImporter.GetAtPath(p);
                if (imp == null) { AssetDatabase.ImportAsset(p); imp = (TextureImporter)AssetImporter.GetAtPath(p); }
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100f;
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = 8192;
                TextureImporterSettings ts = new();
                imp.ReadTextureSettings(ts);
                ts.spriteAlignment = (int)SpriteAlignment.Custom;
                ts.spritePivot = new Vector2(0f, 1f);   // 상단 좌측 = 웹 (0,0)
                imp.SetTextureSettings(ts);
                imp.SaveAndReimport();
            }
        }

        static WebAnimaticLibrary BuildLibrary()
        {
            JObject meta = JObject.Parse(File.ReadAllText(Root + "/Data/meta/sheets.json"));
            JObject stagesMeta = JObject.Parse(File.ReadAllText(Root + "/Data/meta/stages.json"));

            WebAnimaticLibrary lib = AssetDatabase.LoadAssetAtPath<WebAnimaticLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<WebAnimaticLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }

            // 애니메이션 표 + 사용 시트 수집
            var anims = new List<WebAnimaticLibrary.AnimEntry>();
            var usedSheets = new HashSet<string> { "lab_effect-Sheet" };
            foreach (var kv in (JObject)meta["anims"]!)
            {
                JObject a = (JObject)kv.Value!;
                anims.Add(new WebAnimaticLibrary.AnimEntry
                {
                    id = kv.Key,
                    sheet = a.Value<string>("sheet"),
                    from = a.Value<int>("from"), to = a.Value<int>("to"),
                    fps = a.Value<float>("fps"), loop = a.Value<bool>("loop"),
                    hasAnchor = a["ax"]!.Type != JTokenType.Null,
                    ax = a.Value<float?>("ax") ?? 0f, ay = a.Value<float?>("ay") ?? 0f,
                });
                usedSheets.Add(a.Value<string>("sheet"));
            }
            lib.anims = anims.ToArray();

            // 시트 — 엔진 텍스처 탐색 + 프레임 메타
            var sheetEntries = new List<WebAnimaticLibrary.SheetEntry>();
            var problems = new List<string>();
            foreach (string id in usedSheets)
            {
                JObject sh = (JObject)meta["sheets"]![id]!;
                Texture2D tex = FindSheetTexture(id);
                if (tex == null) { problems.Add($"시트 텍스처 없음: {id}"); continue; }
                var frames = new List<WebAnimaticLibrary.FrameMeta>();
                var heights = new List<int>();
                int maxX = 0, maxY = 0;
                foreach (JObject f in sh["frames"]!)
                {
                    var fm = new WebAnimaticLibrary.FrameMeta
                    {
                        sx = f.Value<int>("sx"), sy = f.Value<int>("sy"),
                        sw = f.Value<int>("sw"), sh = f.Value<int>("sh"),
                        ox = f.Value<int>("ox"), oy = f.Value<int>("oy"),
                        cw = f.Value<int>("cw"), ch = f.Value<int>("ch"),
                    };
                    frames.Add(fm);
                    heights.Add(fm.sh);
                    maxX = Mathf.Max(maxX, fm.sx + fm.sw);
                    maxY = Mathf.Max(maxY, fm.sy + fm.sh);
                }
                if (maxX > tex.width || maxY > tex.height)
                    problems.Add($"시트 크기 불일치: {id} (메타 {maxX}x{maxY} > 텍스처 {tex.width}x{tex.height}, 경로 {AssetDatabase.GetAssetPath(tex)})");
                heights.Sort();
                sheetEntries.Add(new WebAnimaticLibrary.SheetEntry
                {
                    id = id, texture = tex, frames = frames.ToArray(),
                    contentH = heights[heights.Count / 2],
                });
            }
            lib.sheets = sheetEntries.ToArray();

            // 무대
            var stageEntries = new List<WebAnimaticLibrary.StageEntry>();
            foreach (var kv in stagesMeta)
            {
                JObject m = (JObject)kv.Value!;
                var variants = new List<WebAnimaticLibrary.StageVariant>();
                foreach (string png in Directory.GetFiles(Root + "/Data/stages", kv.Key + "*.png"))
                {
                    string p = png.Replace('\\', '/');
                    string vid = Path.GetFileNameWithoutExtension(p);
                    Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                    if (sp == null) problems.Add($"무대 스프라이트 로드 실패: {p}");
                    variants.Add(new WebAnimaticLibrary.StageVariant { id = vid, sprite = sp });
                }
                stageEntries.Add(new WebAnimaticLibrary.StageEntry
                {
                    id = kv.Key,
                    width = m.Value<int>("W"), height = m.Value<int>("H"), floor = m.Value<int>("FLOOR"),
                    variants = variants.ToArray(),
                });
            }
            lib.stages = stageEntries.ToArray();

            // 씬 JSON
            lib.scenes = SceneList.Select(s => new WebAnimaticLibrary.SceneEntry
            {
                id = s.id,
                label = s.label,
                json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{Root}/Data/scenes/{s.id}.json"),
            }).ToArray();
            foreach (var s in lib.scenes)
                if (s.json == null) problems.Add($"씬 JSON 없음: {s.id}");

            lib.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/06.Fonts/NeoDunggeunmo SDF.asset");
            if (lib.font == null) problems.Add("폰트 없음: NeoDunggeunmo SDF");

            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();

            if (problems.Count > 0)
                Debug.LogError("[WebAnimatic] 문제:\n" + string.Join("\n", problems));
            else
                Debug.Log($"[WebAnimatic] 라이브러리 갱신 — 시트 {sheetEntries.Count} / 애니 {anims.Count} / 무대 {stageEntries.Count} / 씬 {lib.scenes.Length}");
            return lib;
        }

        static Texture2D FindSheetTexture(string id)
        {
            // 엔진에 이미 들어 있는 동일 이름 텍스처를 찾는다 (Lab 폴더 우선)
            string[] preferred =
            {
                // 엔진 사본이 다운스케일된 시트는 웹 원본을 로컬로 우선 사용
                $"{Root}/Data/sheets/{id}.png",
                $"Assets/Resources/Cutscenes/Lab/{id}.png",
                $"Assets/01.Sprites/Ch/luna/{id}.png",
            };
            foreach (string p in preferred)
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (t != null) return t;
            }
            foreach (string guid in AssetDatabase.FindAssets($"{id} t:Texture2D"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(p) == id)
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
            return null;
        }

        static void BuildScene(WebAnimaticLibrary lib)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("WebAnimatic");
            WebAnimaticPlayer player = root.AddComponent<WebAnimaticPlayer>();
            player.library = lib;
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
    }
}
