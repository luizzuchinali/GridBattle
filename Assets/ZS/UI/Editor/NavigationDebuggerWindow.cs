using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace ZS.UI.Editor
{
    /// <summary>
    /// Play Mode view of every active UIRoot: screen stack, modals, pages,
    /// static/layer views and transition state.
    /// </summary>
    public class NavigationDebuggerWindow : EditorWindow
    {
        private Label _output;

        [MenuItem("Window/ZS/UI Navigation Debugger")]
        public static void Open()
        {
            GetWindow<NavigationDebuggerWindow>("UI Navigation");
        }

        public void CreateGUI()
        {
            var scroll = new ScrollView();
            _output = new Label { enableRichText = true, style = { whiteSpace = WhiteSpace.Normal, paddingLeft = 6, paddingTop = 6 } };
            scroll.Add(_output);
            rootVisualElement.Add(scroll);
            rootVisualElement.schedule.Execute(Refresh).Every(250);
            Refresh();
        }

        private void Refresh()
        {
            if (_output == null) return;

            if (!Application.isPlaying)
            {
                _output.text = "Enter Play Mode to inspect UIRoots.";
                return;
            }

            var text = new StringBuilder();
            foreach (var root in UIRoot.ActiveRoots)
                Describe(root, text);
            _output.text = text.Length > 0 ? text.ToString() : "No active UIRoot.";
        }

        private static void Describe(UIRoot root, StringBuilder text)
        {
            var navigator = root.Navigator;
            text.AppendLine($"<b>{root.name}</b>{(navigator.IsTransitioning ? "  <color=orange>(transitioning)</color>" : "")}");

            text.AppendLine("  <b>Screens</b> (bottom → top)");
            var stack = navigator.Stack;
            for (var i = 0; i < stack.Count; i++)
                DescribeView(stack[i], text, "    ", i == stack.Count - 1 ? "▶ " : "  ");

            text.AppendLine("  <b>Modals</b>");
            foreach (var modal in navigator.Modals)
                DescribeView(modal, text, "    ", "• ");

            text.AppendLine("  <b>Layers</b>");
            foreach (var layer in root.Layers)
            {
                if (layer == null) continue;
                text.AppendLine($"    {layer.name}  sorting={layer.SortingOrder}  loaded={layer.Root != null}");
            }

            text.AppendLine();
        }

        private static void DescribeView(ViewController view, StringBuilder text, string indent, string bullet)
        {
            text.AppendLine($"{indent}{bullet}{view.Definition.name} <color=grey>[{view.GetType().Name}]</color>{(view.IsVisible ? "" : " <color=grey>(hidden)</color>")}");
            foreach (var pages in view.Context.PageNavigators)
            {
                text.AppendLine($"{indent}    pages '{pages.Name}':");
                var stack = pages.Stack;
                for (var i = 0; i < stack.Count; i++)
                    DescribeView(stack[i], text, indent + "      ", i == stack.Count - 1 ? "▶ " : "  ");
            }
        }
    }
}
