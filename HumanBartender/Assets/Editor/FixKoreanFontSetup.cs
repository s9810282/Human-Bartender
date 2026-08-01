// Editor/FixKoreanFontSetup.cs
// 메뉴: Tools > Tycoon > Fix Korean Font On Scene Text

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 현재 열려있는 씬의 모든 TextMeshProUGUI(비활성 오브젝트 포함)에 폰트를 NeoDunggeunmo SDF로 일괄 적용하고,
/// CraftMenuPanel의 font 필드도 함께 채운다. 기본 TMP 폰트(LiberationSans)가 한글을 지원하지 않아, 코드로
/// 미리 생성해둔 UI 텍스트(예: 좌측 슬라이드 패널의 버튼 라벨)가 폰트 지원 추가 전에 만들어졌을 때 이 메뉴로
/// 한 번에 맞춰줄 수 있다. CraftMenuPanel의 칵테일 목록 행(CocktailRow)은 Play 모드에서 Populate()가 호출될
/// 때 코드로 즉석 생성되므로 씬 스캔으로는 못 잡는다 — font 필드만 채워두면 다음 실행부터 반영된다.
/// 결과를 확인한 뒤 직접 Ctrl+S로 저장해야 한다.
/// </summary>
public static class FixKoreanFontSetup
{
    const string FontAssetPath = "Assets/06.Fonts/NeoDunggeunmo SDF.asset";

    [MenuItem("Tools/Tycoon/Fix Korean Font On Scene Text")]
    public static void Run()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
        {
            Debug.LogError($"[FixKoreanFontSetup] {FontAssetPath}에서 폰트를 찾지 못했습니다.");
            return;
        }

        var texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        int changed = 0;

        foreach (var text in texts)
        {
            // 프리팹 에셋 자체(씬에 없는 것)는 건드리지 않고, 현재 씬에 배치된 인스턴스만 대상으로 한다.
            if (!text.gameObject.scene.IsValid()) continue;
            if (text.font == font) continue;

            Undo.RecordObject(text, "Fix Korean Font");
            text.font = font;
            EditorUtility.SetDirty(text);
            changed++;
        }

        int patchedPanels = 0;
        foreach (var panel in Object.FindObjectsByType<CraftMenuPanel>(FindObjectsSortMode.None))
        {
            var so = new SerializedObject(panel);
            var fontProp = so.FindProperty("font");
            if (fontProp.objectReferenceValue == font) continue;

            fontProp.objectReferenceValue = font;
            so.ApplyModifiedProperties();
            patchedPanels++;
        }

        if (changed > 0 || patchedPanels > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[FixKoreanFontSetup] 완료. 텍스트 {changed}개 폰트 적용, CraftMenuPanel {patchedPanels}개 font 필드 연결(칵테일 목록 행은 런타임 생성이라 이걸로 반영됨). Ctrl+S로 씬을 저장하세요.");
    }
}
