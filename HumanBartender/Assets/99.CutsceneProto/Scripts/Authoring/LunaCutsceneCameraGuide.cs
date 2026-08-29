using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class LunaCutsceneCameraGuide : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float aspectWidth = 16f;
        [SerializeField, Min(1f)] private float aspectHeight = 9f;
        [SerializeField, Range(0f, 0.25f)] private float letterboxRatio = 0.12f;
        [SerializeField] private bool showFrame = true;
        [SerializeField] private bool showLetterbox = true;

        public float Aspect => aspectWidth / Mathf.Max(1f, aspectHeight);
        public float LetterboxRatio => letterboxRatio;
        public bool ShowFrame => showFrame;
        public bool ShowLetterbox => showLetterbox;

        public void ConfigureLetterbox(float ratio)
        {
            letterboxRatio = Mathf.Clamp(ratio, 0f, 0.25f);
        }
    }
}
