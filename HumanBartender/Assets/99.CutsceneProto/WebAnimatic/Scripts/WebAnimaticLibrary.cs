using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectLuna.WebAnimatic
{
    /// <summary>
    /// 웹 애니매틱 재생에 필요한 에셋 묶음.
    /// 시트 프레임 절단은 유니티 슬라이스가 아니라 웹과 동일한 rect/앵커 메타를 쓴다
    /// (웹 애니매틱과 픽셀 단위로 같은 결과를 내기 위해서다).
    /// </summary>
    [CreateAssetMenu(menuName = "Project L.U.N.A/Web Animatic Library")]
    public class WebAnimaticLibrary : ScriptableObject
    {
        [Serializable]
        public class FrameMeta
        {
            public int sx, sy, sw, sh;   // 시트 안 위치·크기 (웹 좌표: y는 위에서 아래)
            public int ox, oy, cw, ch;   // 셀 안 오프셋·셀 크기
        }

        [Serializable]
        public class SheetEntry
        {
            public string id;
            public Texture2D texture;
            public FrameMeta[] frames;
            public float contentH;       // 말풍선 앵커용 — 프레임 높이 중앙값
        }

        [Serializable]
        public class AnimEntry
        {
            public string id;
            public string sheet;
            public int from, to;
            public float fps;
            public bool loop;
            public bool hasAnchor;       // false = frame 정렬(프레임별 자기 발끝)
            public float ax, ay;
        }

        [Serializable]
        public class StageVariant
        {
            public string id;            // 예: "lobby_door0"
            public Sprite sprite;
        }

        [Serializable]
        public class StageEntry
        {
            public string id;            // lobby | corridor | disposal | shaft
            public int width, height, floor;
            public StageVariant[] variants;
        }

        [Serializable]
        public class SceneEntry
        {
            public string id;            // s1 | s2 | s3 | s5 | s99
            public string label;
            public TextAsset json;
        }

        public SheetEntry[] sheets;
        public AnimEntry[] anims;
        public StageEntry[] stages;
        public SceneEntry[] scenes;
        public TMPro.TMP_FontAsset font;

        Dictionary<string, SheetEntry> _sheetMap;
        Dictionary<string, AnimEntry> _animMap;
        Dictionary<string, StageEntry> _stageMap;
        Dictionary<string, SceneEntry> _sceneMap;
        Dictionary<(string sheet, int frame, int anchorKey), Sprite> _spriteCache;

        public SheetEntry Sheet(string id)
        {
            _sheetMap ??= Build(sheets, s => s.id);
            return _sheetMap.TryGetValue(id, out var v) ? v : null;
        }

        public AnimEntry Anim(string id)
        {
            _animMap ??= Build(anims, a => a.id);
            return _animMap.TryGetValue(id, out var v) ? v : null;
        }

        public StageEntry Stage(string id)
        {
            _stageMap ??= Build(stages, s => s.id);
            return _stageMap.TryGetValue(id, out var v) ? v : null;
        }

        public SceneEntry Scene(string id)
        {
            _sceneMap ??= Build(scenes, s => s.id);
            return !string.IsNullOrEmpty(id) && _sceneMap.TryGetValue(id, out SceneEntry value)
                ? value
                : null;
        }

        public int SceneIndex(string id)
        {
            if (scenes == null || string.IsNullOrEmpty(id))
                return -1;

            for (int i = 0; i < scenes.Length; i++)
                if (scenes[i] != null && string.Equals(scenes[i].id, id, StringComparison.Ordinal))
                    return i;
            return -1;
        }

        public Sprite StageSprite(string variantId)
        {
            foreach (StageEntry st in stages)
                foreach (StageVariant v in st.variants)
                    if (v.id == variantId) return v.sprite;
            return null;
        }

        static Dictionary<string, T> Build<T>(T[] arr, Func<T, string> key)
        {
            Dictionary<string, T> d = new();
            if (arr != null) foreach (T e in arr) d[key(e)] = e;
            return d;
        }

        /// <summary>프레임 스프라이트 — 앵커(ax,ay)가 피벗이 되도록 생성해 캐시한다.</summary>
        public Sprite Frame(string sheetId, int frame, float ax, float ay)
        {
            _spriteCache ??= new Dictionary<(string, int, int), Sprite>();
            int anchorKey = Mathf.RoundToInt(ax * 8f) * 100000 + Mathf.RoundToInt(ay * 8f);
            var cacheKey = (sheetId, frame, anchorKey);
            if (_spriteCache.TryGetValue(cacheKey, out Sprite cached) && cached != null) return cached;

            SheetEntry sh = Sheet(sheetId);
            if (sh == null || sh.texture == null || frame < 0 || frame >= sh.frames.Length) return null;
            FrameMeta f = sh.frames[frame];
            // 웹 y(위→아래) → 텍스처 rect(y 원점 아래)
            Rect rect = new(f.sx, sh.texture.height - f.sy - f.sh, f.sw, f.sh);
            Vector2 pivot = new((ax - f.ox) / f.sw, 1f - (ay - f.oy) / f.sh);
            Sprite sp = Sprite.Create(sh.texture, rect, pivot, 100f, 0, SpriteMeshType.FullRect);
            sp.name = $"{sheetId}#{frame}";
            _spriteCache[cacheKey] = sp;
            return sp;
        }
    }
}
