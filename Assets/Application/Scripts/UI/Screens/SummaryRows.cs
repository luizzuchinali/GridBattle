using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI.Screens
{
    /// <summary>
    /// Builds the rows of the information lists (end-of-run summary, consumable offer): section titles, rows with
    /// an optional icon and value, and paragraphs. Styles are the <c>info-*</c> classes of Components.uss.
    /// </summary>
    public static class SummaryRows
    {
        public static Label AddSection(VisualElement parent, string title)
        {
            var label = new Label(title) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("info-section-title");
            parent.Add(label);
            return label;
        }

        public static Label AddText(VisualElement parent, string text, string modifier = null)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("info-text");
            if (!string.IsNullOrEmpty(modifier))
                label.AddToClassList(modifier);
            parent.Add(label);
            return label;
        }

        public static VisualElement AddRow(VisualElement parent, string label, string value, Sprite icon = null,
            string modifier = null)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("info-row");
            if (!string.IsNullOrEmpty(modifier))
                row.AddToClassList(modifier);

            if (icon != null)
            {
                var image = new Image { sprite = icon, pickingMode = PickingMode.Ignore };
                image.AddToClassList("info-icon");
                row.Add(image);
            }

            var name = new Label(label) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("info-row__label");
            row.Add(name);

            if (!string.IsNullOrEmpty(value))
            {
                var text = new Label(value) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("info-row__value");
                row.Add(text);
            }

            parent.Add(row);
            return row;
        }
    }
}
