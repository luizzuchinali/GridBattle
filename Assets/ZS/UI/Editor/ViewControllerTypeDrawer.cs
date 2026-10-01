using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace ZS.UI.Editor
{
    /// <summary>
    /// Draws a <see cref="ViewControllerTypeAttribute"/> string as a dropdown of
    /// every concrete <see cref="ViewController"/> in the project.
    /// </summary>
    [CustomPropertyDrawer(typeof(ViewControllerTypeAttribute))]
    public class ViewControllerTypeDrawer : PropertyDrawer
    {
        private const string NoneLabel = "(None)";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var (labels, values) = Options();
            var current = values.IndexOf(property.stringValue);
            if (current < 0 && !string.IsNullOrEmpty(property.stringValue))
            {
                labels.Add($"Missing: {property.stringValue}");
                values.Add(property.stringValue);
                current = values.Count - 1;
            }

            var dropdown = new DropdownField(property.displayName, labels, Mathf.Max(0, current));
            dropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            dropdown.tooltip = property.tooltip;
            dropdown.RegisterValueChangedCallback(_ =>
            {
                property.stringValue = values[dropdown.index];
                property.serializedObject.ApplyModifiedProperties();
            });
            return dropdown;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var (labels, values) = Options();
            var current = Mathf.Max(0, values.IndexOf(property.stringValue));
            var selected = EditorGUI.Popup(position, label.text, current, labels.ToArray());
            if (selected != current)
                property.stringValue = values[selected];
        }

        private static (List<string> labels, List<string> values) Options()
        {
            var types = TypeCache.GetTypesDerivedFrom<ViewController>()
                .Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition &&
                               type.GetConstructor(System.Type.EmptyTypes) != null)
                .OrderBy(type => type.FullName)
                .ToList();

            var labels = new List<string> { NoneLabel };
            var values = new List<string> { string.Empty };
            foreach (var type in types)
            {
                labels.Add(type.FullName?.Replace('.', '/'));
                values.Add(ViewDefinition.ToTypeName(type));
            }

            return (labels, values);
        }
    }
}
