using System;
using System.Collections.Generic;
using System.Linq;
using GridBattle.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.Editor.Drawers
{
    /// <summary>
    /// Inspector for [SerializeReference, SubclassPicker] fields: a dropdown with
    /// every concrete, serializable subclass of the field type, followed by the
    /// fields of the chosen subclass.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassPickerAttribute))]
    public sealed class SubclassPickerDrawer : PropertyDrawer
    {
        private const string NoneLabel = "(none)";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                root.Add(new HelpBox($"{property.displayName}: SubclassPicker needs [SerializeReference].",
                    HelpBoxMessageType.Error));
                return root;
            }

            var baseType = GetFieldType();
            var types = TypeCache.GetTypesDerivedFrom(baseType)
                .Append(baseType)
                .Where(t => !t.IsAbstract && !t.IsGenericType && t.IsSerializable
                            && !typeof(UnityEngine.Object).IsAssignableFrom(t)
                            && t.GetConstructor(Type.EmptyTypes) != null)
                .Distinct()
                .OrderBy(t => t.Name)
                .ToList();

            var choices = new List<string> { NoneLabel };
            choices.AddRange(types.Select(Nicify));

            var dropdown = new DropdownField(property.displayName, choices, CurrentIndex(property, types));
            dropdown.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            root.Add(dropdown);

            var fields = new VisualElement { style = { marginLeft = 12 } };
            root.Add(fields);
            DrawChildren(property, fields);

            dropdown.RegisterValueChangedCallback(evt =>
            {
                var index = choices.IndexOf(evt.newValue) - 1;
                property.serializedObject.Update();
                property.managedReferenceValue = index >= 0 ? Activator.CreateInstance(types[index]) : null;
                property.serializedObject.ApplyModifiedProperties();
                DrawChildren(property, fields);
            });

            return root;
        }

        private Type GetFieldType()
        {
            var type = fieldInfo.FieldType;
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return type.GetGenericArguments()[0];
            return type;
        }

        private static int CurrentIndex(SerializedProperty property, List<Type> types)
        {
            var current = property.managedReferenceValue?.GetType();
            return current == null ? 0 : types.IndexOf(current) + 1;
        }

        private static void DrawChildren(SerializedProperty property, VisualElement container)
        {
            container.Clear();
            var copy = property.Copy();
            var end = copy.GetEndProperty();
            if (!copy.NextVisible(true)) return;

            while (!SerializedProperty.EqualContents(copy, end))
            {
                var field = new PropertyField(copy.Copy());
                field.Bind(property.serializedObject);
                container.Add(field);
                if (!copy.NextVisible(false)) break;
            }
        }

        private static string Nicify(Type type) => ObjectNames.NicifyVariableName(type.Name);
    }
}
