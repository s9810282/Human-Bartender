using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ProjectLuna.CutscenePrototype.Editor.Authoring
{
    /// <summary>
    /// 본편 외부 거리 카메라와 같은 픽셀 기준을 컷씬 제작 카메라에 적용한다.
    /// 컷씬은 Timeline이 실제 CameraRig를 직접 움직이므로 Cinemachine 의존성은 추가하지 않는다.
    /// </summary>
    public static class LunaCutscenePixelCameraUtility
    {
        public const int AssetsPpu = 100;
        public const int ReferenceWidth = 480;
        public const int ReferenceHeight = 270;
        public const float ReferenceOrthographicSize = ReferenceHeight / (2f * AssetsPpu);

        public static PixelPerfectCamera ApplyProjectPreset(Camera camera)
        {
            if (camera == null)
                throw new InvalidOperationException("픽셀 카메라 설정을 적용할 Camera가 필요합니다.");

            camera.orthographic = true;
            camera.orthographicSize = ReferenceOrthographicSize;

            PixelPerfectCamera pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfect == null)
            {
                pixelPerfect = Application.isBatchMode
                    ? camera.gameObject.AddComponent<PixelPerfectCamera>()
                    : Undo.AddComponent<PixelPerfectCamera>(camera.gameObject);
            }

            pixelPerfect.assetsPPU = AssetsPpu;
            pixelPerfect.refResolutionX = ReferenceWidth;
            pixelPerfect.refResolutionY = ReferenceHeight;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.None;

            // URP 17의 PixelPerfectFilterMode는 공개 setter가 없으므로 직렬화 필드를 설정한다.
            SerializedObject serialized = new(pixelPerfect);
            SerializedProperty filter = serialized.FindProperty("m_FilterMode");
            if (filter != null)
                filter.enumValueIndex = (int)PixelPerfectCamera.PixelPerfectFilterMode.Point;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(pixelPerfect);
            if (camera.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
            return pixelPerfect;
        }

        public static bool MatchesProjectPreset(Camera camera, out string reason)
        {
            if (camera == null)
            {
                reason = "Camera가 없습니다.";
                return false;
            }
            if (!camera.orthographic)
            {
                reason = "Camera가 Orthographic이 아닙니다.";
                return false;
            }

            PixelPerfectCamera pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfect == null)
            {
                reason = "URP Pixel Perfect Camera가 없습니다.";
                return false;
            }
            if (pixelPerfect.assetsPPU != AssetsPpu)
            {
                reason = $"Assets PPU가 {pixelPerfect.assetsPPU}입니다. 프로젝트 기준은 {AssetsPpu}입니다.";
                return false;
            }
            if (pixelPerfect.refResolutionX != ReferenceWidth || pixelPerfect.refResolutionY != ReferenceHeight)
            {
                reason = $"Reference Resolution이 {pixelPerfect.refResolutionX}×{pixelPerfect.refResolutionY}입니다. 프로젝트 기준은 {ReferenceWidth}×{ReferenceHeight}입니다.";
                return false;
            }
            if (pixelPerfect.cropFrame != PixelPerfectCamera.CropFrame.StretchFill)
            {
                reason = "Crop Frame이 Stretch Fill이 아닙니다.";
                return false;
            }
            if (pixelPerfect.gridSnapping != PixelPerfectCamera.GridSnapping.None)
            {
                reason = "Grid Snapping이 외부 거리 기준(None)과 다릅니다.";
                return false;
            }

            SerializedObject serialized = new(pixelPerfect);
            SerializedProperty filter = serialized.FindProperty("m_FilterMode");
            if (filter != null && filter.enumValueIndex != (int)PixelPerfectCamera.PixelPerfectFilterMode.Point)
            {
                reason = "Filter Mode가 Point가 아닙니다.";
                return false;
            }

            reason = $"PPU {AssetsPpu} · {ReferenceWidth}×{ReferenceHeight} · Stretch Fill · Point";
            return true;
        }

        [MenuItem("Project L.U.N.A/Cutscene Authoring/Apply Pixel Camera Preset")]
        public static void ApplyToCurrentScene()
        {
            Camera camera = FindInScene<Camera>(SceneManager.GetActiveScene());
            ApplyProjectPreset(camera);
            Debug.Log($"[LunaCutsceneAuthoring] PIXEL_CAMERA_OK — PPU {AssetsPpu}, {ReferenceWidth}×{ReferenceHeight}, Stretch Fill, Point", camera);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
        }
    }
}
