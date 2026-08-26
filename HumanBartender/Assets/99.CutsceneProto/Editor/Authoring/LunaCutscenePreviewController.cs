using System;
using System.Collections.Generic;
using ProjectLuna.CutscenePrototype.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace ProjectLuna.CutscenePrototype.Editor.Authoring
{
    [InitializeOnLoad]
    public static class LunaCutscenePreviewController
    {
        private readonly struct TransformSnapshot
        {
            public readonly Transform target;
            public readonly Vector3 localPosition;
            public readonly Quaternion localRotation;
            public readonly Vector3 localScale;

            public TransformSnapshot(Transform value)
            {
                target = value;
                localPosition = value.localPosition;
                localRotation = value.localRotation;
                localScale = value.localScale;
            }

            public void Restore()
            {
                if (target == null)
                    return;
                target.localPosition = localPosition;
                target.localRotation = localRotation;
                target.localScale = localScale;
            }
        }

        private static readonly List<TransformSnapshot> Snapshots = new();
        private static PlayableDirector activeDirector;
        private static double previousEditorTime;
        private static double loopStart;
        private static double loopEnd;
        private static float speed = 1f;
        private static bool loop;

        public static bool IsPreviewing => activeDirector != null;

        static LunaCutscenePreviewController()
        {
            AssemblyReloadEvents.beforeAssemblyReload += StopAndRestore;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void Play(PlayableDirector director, double start, double end, bool shouldLoop, float playbackSpeed)
        {
            if (director == null || director.playableAsset == null || EditorApplication.isPlaying)
                return;

            if (activeDirector != director)
            {
                StopAndRestore();
                CaptureSceneState(director);
            }

            activeDirector = director;
            loopStart = Math.Max(0d, start);
            loopEnd = Math.Max(loopStart + 0.01d, Math.Min(end, director.duration));
            loop = shouldLoop;
            speed = Mathf.Clamp(playbackSpeed, 0.1f, 4f);
            previousEditorTime = EditorApplication.timeSinceStartup;
            director.timeUpdateMode = DirectorUpdateMode.Manual;
            director.time = Math.Clamp(director.time, loopStart, loopEnd);
            director.Evaluate();
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        public static void EvaluateAt(PlayableDirector director, double time)
        {
            if (director == null || director.playableAsset == null || EditorApplication.isPlaying)
                return;
            if (activeDirector != director)
            {
                StopAndRestore();
                CaptureSceneState(director);
                activeDirector = director;
            }
            director.timeUpdateMode = DirectorUpdateMode.Manual;
            director.time = Math.Clamp(time, 0d, Math.Max(0d, director.duration));
            director.Evaluate();
            SceneView.RepaintAll();
        }

        public static void Step(PlayableDirector director, int frameDelta, float frameRate)
        {
            double frame = 1d / Math.Max(1f, frameRate);
            EvaluateAt(director, director.time + frame * frameDelta);
        }

        public static void StopAndRestore()
        {
            EditorApplication.update -= Update;
            if (activeDirector != null)
                activeDirector.Stop();
            foreach (TransformSnapshot snapshot in Snapshots)
                snapshot.Restore();
            Snapshots.Clear();
            activeDirector = null;
            SceneView.RepaintAll();
        }

        private static void CaptureSceneState(PlayableDirector director)
        {
            Snapshots.Clear();
            LunaCutsceneBindingRegistry registry = director.GetComponent<LunaCutsceneBindingRegistry>();
            if (registry == null)
                return;
            HashSet<Transform> unique = new();
            foreach (LunaCutsceneBindingEntry entry in registry.Bindings)
            {
                if (entry?.target == null || !unique.Add(entry.target.transform))
                    continue;
                Snapshots.Add(new TransformSnapshot(entry.target.transform));
            }
        }

        private static void Update()
        {
            if (activeDirector == null || EditorApplication.isPlaying)
            {
                StopAndRestore();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            double delta = Math.Min(0.1d, now - previousEditorTime) * speed;
            previousEditorTime = now;
            double next = activeDirector.time + delta;
            if (next >= loopEnd)
            {
                if (loop)
                    next = loopStart + (next - loopEnd);
                else
                {
                    activeDirector.time = loopEnd;
                    activeDirector.Evaluate();
                    EditorApplication.update -= Update;
                    SceneView.RepaintAll();
                    return;
                }
            }

            activeDirector.time = next;
            activeDirector.Evaluate();
            SceneView.RepaintAll();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                StopAndRestore();
        }
    }
}
