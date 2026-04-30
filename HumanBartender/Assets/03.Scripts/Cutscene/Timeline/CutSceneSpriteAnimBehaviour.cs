using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// 클립 구간 동안 UI Image에 스프라이트 시트 기반 프레임 애니메이션 재생.
///
/// 스프라이트 시트 설정:
///   1. 텍스처의 Sprite Mode를 Multiple로 설정
///   2. Sprite Editor에서 슬라이스
///   3. Resources 폴더 안에 배치
///   4. spriteSheetPath에 텍스처 경로 입력 (확장자 제외)
///
/// 예시:
///   파일 위치: Resources/Cutscenes/luna_walk_sheet.png
///   spriteSheetPath: "Cutscenes/luna_walk_sheet"
///   → 서브 스프라이트 luna_walk_sheet_0, _1, _2... 가 자동 로드됨
/// </summary>
[Serializable]
public class CutSceneSpriteAnimBehaviour : PlayableBehaviour
{
    [Header("대상")]
    [Tooltip("ImageTrack에서 등록한 imagePath와 동일한 값")]
    public string imagePath;

    [Header("스프라이트 시트")]
    [Tooltip("Resources 하위 경로 (확장자 제외). 예: Cutscenes/luna_walk_sheet")]
    public string spriteSheetPath;

    [Tooltip("초당 프레임 수")]
    public float fps = 12f;

    [Tooltip("클립 내에서 반복 재생")]
    public bool loop = true;

    [Tooltip("특정 프레임 구간만 재생 (0이면 전체)")]
    public int startFrame = 0;
    [Tooltip("끝 프레임 (0이면 마지막까지)")]
    public int endFrame = 0;

    // ── 런타임 ────────────────────────────────────────────────────────
    [NonSerialized] internal CutSceneTimelineManager manager;
    [NonSerialized] private Sprite[] frames;
    [NonSerialized] private bool loaded;
    [NonSerialized] private Image targetImage;
    [NonSerialized] private int actualStartFrame;
    [NonSerialized] private int actualEndFrame;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        loaded = false;
        targetImage = null;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        manager = playerData as CutSceneTimelineManager;
        if (manager == null) return;

        // 대상 Image 찾기
        if (targetImage == null)
        {
            targetImage = manager.GetActiveImage(imagePath);

            if (targetImage == null)
            {
                Logger.Log("target Image is Null");
                return;
            }
        }

        // 스프라이트 시트에서 서브 스프라이트 로드 (한 번만)
        if (!loaded)
        {
            loaded = true;

            // LoadAll<Sprite>는 해당 텍스처의 모든 서브 스프라이트를 반환
            Sprite[] allSprites = Resources.LoadAll<Sprite>($"Cutscenes/{spriteSheetPath}");

            if (allSprites == null || allSprites.Length == 0)
            {
                Debug.LogWarning(
                    $"[SpriteAnim] 스프라이트를 찾을 수 없음: {spriteSheetPath}\n" +
                    "확인사항: Sprite Mode가 Multiple인지, Resources 폴더 안에 있는지");
                return;
            }

            // 이름순 정렬 (슬라이스 순서 보장)
            Array.Sort(allSprites, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            Logger.Log(allSprites.Length);

            // 프레임 범위 계산
            actualStartFrame = Mathf.Clamp(startFrame, 0, allSprites.Length - 1);
            actualEndFrame = (endFrame <= 0 || endFrame >= allSprites.Length)
                ? allSprites.Length - 1
                : endFrame;

            // 범위만큼 잘라서 저장
            int count = actualEndFrame - actualStartFrame + 1;
            frames = new Sprite[count];
            Array.Copy(allSprites, actualStartFrame, frames, 0, count);
        }

        if (frames == null || frames.Length == 0) return;

        // 현재 시간 → 프레임 인덱스
        float elapsed = (float)playable.GetTime();
        int frameIndex = Mathf.FloorToInt(elapsed * fps);

        if (loop)
        {
            frameIndex = frameIndex % frames.Length;
        }
        else
        {
            frameIndex = Mathf.Min(frameIndex, frames.Length - 1);
        }

        //Logger.Log(frameIndex);
        targetImage.sprite = frames[frameIndex];
        targetImage.SetNativeSize();
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        targetImage = null;
    }
}
