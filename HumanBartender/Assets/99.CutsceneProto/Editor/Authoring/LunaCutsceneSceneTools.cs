using System;
using System.Collections.Generic;
using System.Linq;
using ProjectLuna.CutscenePrototype.Authoring;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ProjectLuna.CutscenePrototype.Editor.Authoring
{
    [InitializeOnLoad]
    public static class LunaCutsceneSceneTools
    {
        private const string GuideKey = "LUNA.CutsceneAuthoring.ShowSceneGuides";

        public static bool ShowGuides
        {
            get => EditorPrefs.GetBool(GuideKey, true);
            set
            {
                EditorPrefs.SetBool(GuideKey, value);
                SceneView.RepaintAll();
            }
        }

        static LunaCutsceneSceneTools()
        {
            SceneView.duringSceneGui += DrawMovementPaths;
        }

        private static void DrawMovementPaths(SceneView view)
        {
            if (!ShowGuides)
                return;
            PlayableDirector director = TimelineEditor.inspectedDirector;
            if (director == null || director.playableAsset is not TimelineAsset timeline)
                return;

            TimelineClip selected = TimelineEditor.selectedClip;
            IEnumerable<TimelineClip> clips = selected?.asset is LunaActorMoveClip
                ? new[] { selected }
                : LunaCutsceneAuthoringBuilder.EnumerateTracks(timeline)
                    .OfType<LunaActorMoveTrack>()
                    .SelectMany(track => track.GetClips());

            foreach (TimelineClip clip in clips)
            {
                if (clip.asset is not LunaActorMoveClip move)
                    continue;
                Transform from = move.ResolveFrom(director);
                Transform to = move.ResolveTo(director);
                if (from == null || to == null)
                    continue;

                Handles.color = selected == clip
                    ? new Color(1f, 0.78f, 0.2f, 1f)
                    : new Color(0.15f, 0.9f, 0.85f, 0.75f);
                Handles.DrawAAPolyLine(4f, from.position, to.position);
                Vector3 direction = to.position - from.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    float size = HandleUtility.GetHandleSize(to.position) * 0.18f;
                    Handles.ArrowHandleCap(0, to.position, Quaternion.LookRotation(direction), size, EventType.Repaint);
                }
                Handles.Label((from.position + to.position) * 0.5f + Vector3.up * 0.12f,
                    $"{clip.displayName}  {clip.duration:0.00}s");
            }
        }
    }

    [CustomEditor(typeof(LunaCutsceneAnchor))]
    public sealed class LunaCutsceneAnchorEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            LunaCutsceneAnchor anchor = (LunaCutsceneAnchor)target;
            EditorGUI.BeginChangeCheck();
            Vector3 position = Handles.PositionHandle(anchor.transform.position, Quaternion.identity);
            if (!EditorGUI.EndChangeCheck())
                return;
            Undo.RecordObject(anchor.transform, "Move L.U.N.A Cutscene Anchor");
            anchor.transform.position = position;
        }

        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawAnchorGizmo(LunaCutsceneAnchor anchor, GizmoType gizmoType)
        {
            if (anchor == null || !LunaCutsceneSceneTools.ShowGuides)
                return;
            Gizmos.color = anchor.GuideColor;
            Gizmos.DrawWireSphere(anchor.transform.position, 0.12f);
            Gizmos.DrawLine(anchor.transform.position + Vector3.left * 0.22f, anchor.transform.position + Vector3.right * 0.22f);
            Gizmos.DrawLine(anchor.transform.position + Vector3.down * 0.22f, anchor.transform.position + Vector3.up * 0.22f);
            Handles.Label(anchor.transform.position + Vector3.up * 0.22f, anchor.AnchorId);
        }
    }

    [CustomEditor(typeof(LunaCutsceneCameraGuide))]
    public sealed class LunaCutsceneCameraGuideEditor : UnityEditor.Editor
    {
        [DrawGizmo(GizmoType.Active | GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawCameraGuide(LunaCutsceneCameraGuide guide, GizmoType gizmoType)
        {
            if (guide == null || !LunaCutsceneSceneTools.ShowGuides || !guide.ShowFrame)
                return;
            Camera camera = guide.GetComponent<Camera>();
            if (camera == null || !camera.orthographic)
                return;

            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * guide.Aspect;
            Vector3 center = camera.transform.position + camera.transform.forward * 10f;
            Vector3 right = camera.transform.right * halfWidth;
            Vector3 up = camera.transform.up * halfHeight;
            Vector3 topLeft = center - right + up;
            Vector3 topRight = center + right + up;
            Vector3 bottomRight = center + right - up;
            Vector3 bottomLeft = center - right - up;

            Gizmos.color = new Color(0.2f, 0.95f, 0.92f, 0.9f);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);

            if (!guide.ShowLetterbox)
                return;
            float inset = halfHeight * 2f * guide.LetterboxRatio;
            Gizmos.color = new Color(1f, 0.55f, 0.16f, 0.8f);
            Gizmos.DrawLine(center - right + camera.transform.up * (halfHeight - inset), center + right + camera.transform.up * (halfHeight - inset));
            Gizmos.DrawLine(center - right - camera.transform.up * (halfHeight - inset), center + right - camera.transform.up * (halfHeight - inset));
        }
    }
}
