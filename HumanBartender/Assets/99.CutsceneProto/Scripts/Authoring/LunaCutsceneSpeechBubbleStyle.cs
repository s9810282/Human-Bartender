using TMPro;
using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    [CreateAssetMenu(
        fileName = "CutsceneSpeechBubbleStyle",
        menuName = "Project L.U.N.A/Cutscene/Speech Bubble Style")]
    public sealed class LunaCutsceneSpeechBubbleStyle : ScriptableObject
    {
        [Header("Font")]
        public TMP_FontAsset font;
        [Min(8f)] public float speakerFontSize = 21f;
        [Min(8f)] public float bodyFontSize = 23f;
        [Min(8f)] public float hintFontSize = 14f;
        [Min(0f)] public float lineSpacing = 2f;

        [Header("Color")]
        public Color backgroundColor = new(0.015f, 0.04f, 0.055f, 0.96f);
        public Color speakerColor = new(0.32f, 1f, 0.94f, 1f);
        public Color bodyColor = Color.white;
        public Color hintColor = new(0.55f, 0.85f, 0.88f, 1f);
        public Color tailColor = new(0.015f, 0.04f, 0.055f, 0.96f);

        [Header("Adaptive Size")]
        [Min(120f)] public float minWidth = 260f;
        [Min(120f)] public float maxWidth = 560f;
        [Min(60f)] public float minHeight = 104f;
        [Min(60f)] public float maxHeight = 250f;
        [Min(0f)] public float horizontalPadding = 24f;
        [Min(0f)] public float verticalPadding = 17f;
        [Min(0f)] public float speakerBodySpacing = 5f;
        [Min(0f)] public float bodyHintSpacing = 5f;
        [Min(10f)] public float hintHeight = 20f;

        [Header("Tracking")]
        public Vector2 bubbleScreenOffset = new(0f, 36f);
        public Vector2 unboundFallbackPosition = new(0f, 155f);
        public Vector2 tailSize = new(22f, 22f);
        [Min(0f)] public float tailEdgeInset = 24f;
        [Min(0f)] public float safeAreaPadding = 18f;

        private void OnValidate()
        {
            maxWidth = Mathf.Max(minWidth, maxWidth);
            maxHeight = Mathf.Max(minHeight, maxHeight);
            horizontalPadding = Mathf.Min(horizontalPadding, maxWidth * 0.35f);
            verticalPadding = Mathf.Min(verticalPadding, maxHeight * 0.35f);
        }
    }
}
