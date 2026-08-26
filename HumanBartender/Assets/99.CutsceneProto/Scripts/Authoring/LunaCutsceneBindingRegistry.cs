using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    [Serializable]
    public sealed class LunaCutsceneBindingEntry
    {
        public string id;
        public GameObject target;
    }

    public sealed class LunaCutsceneBindingRegistry : MonoBehaviour
    {
        [SerializeField] private LunaCutsceneBindingEntry[] bindings = Array.Empty<LunaCutsceneBindingEntry>();
        private readonly Dictionary<string, GameObject> lookup = new(StringComparer.Ordinal);

        public IReadOnlyList<LunaCutsceneBindingEntry> Bindings => bindings;

        private void Awake()
        {
            RebuildLookup();
        }

        public void Configure(LunaCutsceneBindingEntry[] entries)
        {
            bindings = entries ?? Array.Empty<LunaCutsceneBindingEntry>();
            RebuildLookup();
        }

        public void RebuildFromChildren()
        {
            LunaCutsceneBindingId[] ids = GetComponentsInChildren<LunaCutsceneBindingId>(true);
            var entries = new List<LunaCutsceneBindingEntry>(ids.Length);
            foreach (LunaCutsceneBindingId id in ids)
            {
                if (id == null || string.IsNullOrWhiteSpace(id.BindingId))
                    continue;
                entries.Add(new LunaCutsceneBindingEntry { id = id.BindingId, target = id.gameObject });
            }
            bindings = entries.ToArray();
            RebuildLookup();
        }

        public void RebuildLookup()
        {
            lookup.Clear();
            if (bindings == null)
                return;

            foreach (LunaCutsceneBindingEntry entry in bindings)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.target == null)
                    continue;
                if (lookup.ContainsKey(entry.id))
                {
                    Debug.LogError($"[LunaCutscene] 중복 binding id: {entry.id}", this);
                    continue;
                }
                lookup.Add(entry.id, entry.target);
            }
        }

        public bool TryGet(string id, out GameObject target)
        {
            if (lookup.Count == 0)
                RebuildLookup();
            return lookup.TryGetValue(id, out target) && target != null;
        }

        public bool Contains(string id)
        {
            return TryGet(id, out _);
        }
    }
}
