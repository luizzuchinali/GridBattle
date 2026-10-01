using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace ZS.UI.Editor
{
    /// <summary>
    /// ViewDefinition inspector: default fields plus live validation and
    /// shortcuts to the UXML and the controller script.
    /// </summary>
    [CustomEditor(typeof(ViewDefinition))]
    public class ViewDefinitionEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var definition = (ViewDefinition)target;
            var root = new VisualElement();

            var problems = new VisualElement();
            root.Add(problems);
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8 } };
            buttons.Add(new Button(() => OpenUxml(definition)) { text = "Open UXML" });
            buttons.Add(new Button(() => OpenController(definition)) { text = "Open Controller" });
            root.Add(buttons);

            RefreshProblems(problems, definition);
            root.TrackSerializedObjectValue(serializedObject, _ => RefreshProblems(problems, definition));
            return root;
        }

        private static void RefreshProblems(VisualElement container, ViewDefinition definition)
        {
            container.Clear();
            foreach (var problem in definition.Validate())
                container.Add(new HelpBox(problem, HelpBoxMessageType.Warning));
        }

        private static void OpenUxml(ViewDefinition definition)
        {
            if (definition.VisualTree != null)
                AssetDatabase.OpenAsset(definition.VisualTree);
        }

        private static void OpenController(ViewDefinition definition)
        {
            var type = definition.ControllerType;
            if (type == null) return;

            var script = AssetDatabase.FindAssets($"t:MonoScript {type.Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                .FirstOrDefault(candidate => candidate != null && candidate.text.Contains($"class {type.Name}"));
            if (script != null)
                AssetDatabase.OpenAsset(script);
        }
    }
}
