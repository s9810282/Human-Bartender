using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    [ExecuteAlways]
    public sealed class LunaCutsceneAnchor : MonoBehaviour
    {
        [SerializeField] private string anchorId = "anchor_new";
        [SerializeField] private Color guideColor = new(0.15f, 0.95f, 0.9f, 0.9f);

        public string AnchorId => anchorId;
        public Color GuideColor => guideColor;

        public void Configure(string id)
        {
            anchorId = string.IsNullOrWhiteSpace(id) ? "anchor_new" : id.Trim();
        }
    }
}
