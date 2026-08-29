using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    public enum LunaCutscenePresentationEase
    {
        SmoothStep,
        EaseOutCubic,
        Linear
    }

    /// <summary>
    /// Reusable cutscene presentation settings for letterbox, pixel zoom and end fade.
    /// This is intentionally independent from Timeline so every cutscene can share one preset.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Project L.U.N.A/Cutscene/Presentation Preset",
        fileName = "CutscenePresentationPreset")]
    public sealed class LunaCutscenePresentationPreset : ScriptableObject
    {
        [Header("Opening Letterbox")]
        public bool useLetterbox = true;
        [Range(0.10f, 0.15f)] public float letterboxHeightRatio = 0.12f;
        [Min(0.01f)] public float entranceDuration = 0.7f;
        public LunaCutscenePresentationEase entranceEase = LunaCutscenePresentationEase.SmoothStep;
        public Color letterboxColor = Color.black;

        [Header("Pixel Perfect Camera Zoom")]
        public bool usePixelPerfectZoom = true;
        [Range(0f, 0.15f)] public float zoomInRatio = 0.0666667f;

        [Header("Ending")]
        [Tooltip("Keep the letterbox in place and absorb it into the full-screen fade.")]
        public bool fadeOutOnEnd = true;
        [Min(0.01f)] public float exitFadeDuration = 0.55f;

        public float EvaluateEntrance(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            return entranceEase switch
            {
                LunaCutscenePresentationEase.Linear => t,
                LunaCutscenePresentationEase.EaseOutCubic => 1f - Mathf.Pow(1f - t, 3f),
                _ => t * t * (3f - 2f * t)
            };
        }

        private void OnValidate()
        {
            letterboxHeightRatio = Mathf.Clamp(letterboxHeightRatio, 0.10f, 0.15f);
            entranceDuration = Mathf.Max(0.01f, entranceDuration);
            zoomInRatio = Mathf.Clamp(zoomInRatio, 0f, 0.15f);
            exitFadeDuration = Mathf.Max(0.01f, exitFadeDuration);
        }
    }
}
