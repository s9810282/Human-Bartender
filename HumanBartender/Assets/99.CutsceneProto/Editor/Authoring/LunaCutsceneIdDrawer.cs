using System;
using System.Collections.Generic;
using System.Linq;
using ProjectLuna.CutscenePrototype.Authoring;
using UnityEditor;
using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Editor.Authoring
{
    [CustomPropertyDrawer(typeof(LunaBindingIdAttribute))]
    public sealed class LunaBindingIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            LunaCutsceneBindingRegistry registry = UnityEngine.Object.FindFirstObjectByType<LunaCutsceneBindingRegistry>();
            List<string> ids = registry == null
                ? new List<string>()
                : registry.Bindings
                    .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.id) && entry.target != null)
                    .Select(entry => entry.id)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToList();

            string current = property.stringValue ?? string.Empty;
            List<string> options = new() { "" };
            options.AddRange(ids);
            if (!string.IsNullOrWhiteSpace(current) && !options.Contains(current))
                options.Add(current);

            string[] labels = options
                .Select(value => string.IsNullOrEmpty(value)
                    ? "(대상 없음)"
                    : ids.Contains(value) ? value : $"⚠ {value} (미등록)")
                .ToArray();
            int selected = Mathf.Max(0, options.IndexOf(current));

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            int next = EditorGUI.Popup(position, label.text, selected, labels);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = options[next];
            EditorGUI.EndProperty();
        }
    }
}
