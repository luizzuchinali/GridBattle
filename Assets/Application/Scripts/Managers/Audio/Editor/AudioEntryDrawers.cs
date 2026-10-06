using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GridBattle.Managers.Audio.Editor
{
    /// <summary>
    /// Draws the entries of the AudioLibrary lists as foldouts titled with the
    /// effect or context they configure (instead of "Element 17"); the enum key
    /// itself is not editable here, the library keeps one entry per value.
    /// </summary>
    public abstract class EnumKeyedEntryDrawer : PropertyDrawer
    {
        /// <summary>Name of the serialized enum field that identifies the entry.</summary>
        protected abstract string KeyField { get; }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded) return height;

            foreach (var child in Children(property))
                height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, TitleOf(property, label), true);
            if (!property.isExpanded) return;

            EditorGUI.indentLevel++;
            var y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
            foreach (var child in Children(property))
            {
                var height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true);
                y += height + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.indentLevel--;
        }

        private GUIContent TitleOf(SerializedProperty property, GUIContent fallback)
        {
            var key = property.FindPropertyRelative(KeyField);
            if (key == null || key.propertyType != SerializedPropertyType.Enum) return fallback;

            var names = key.enumDisplayNames;
            return key.enumValueIndex >= 0 && key.enumValueIndex < names.Length
                ? new GUIContent(names[key.enumValueIndex])
                : fallback;
        }

        /// <summary>The visible child fields of the entry, without the key.</summary>
        private IEnumerable<SerializedProperty> Children(SerializedProperty property)
        {
            var iterator = property.Copy();
            var end = iterator.GetEndProperty();
            var enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                if (iterator.name != KeyField)
                    yield return iterator.Copy();
            }
        }
    }

    [CustomPropertyDrawer(typeof(SfxCue))]
    public sealed class SfxCueDrawer : EnumKeyedEntryDrawer
    {
        protected override string KeyField => "sfx";
    }

    [CustomPropertyDrawer(typeof(MusicTrack))]
    public sealed class MusicTrackDrawer : EnumKeyedEntryDrawer
    {
        protected override string KeyField => "context";
    }
}
