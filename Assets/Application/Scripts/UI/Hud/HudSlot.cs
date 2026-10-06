using UnityEngine;
using UnityEngine.UIElements;

namespace GridBattle.UI.Hud
{
    /// <summary>
    /// One 32x32 button of the skill or item bar: icon, a badge over it (the remaining cooldown)
    /// and the selected, disabled and empty looks. The only place that knows the slot's USS classes.
    /// </summary>
    public sealed class HudSlot
    {
        public const string SlotClass = "slot-button";
        public const string RoundClass = "slot-button--round";
        public const string SelectedClass = "slot-button--selected";
        public const string DisabledClass = "slot-button--disabled";
        public const string EmptyClass = "slot-button--empty";
        public const string BadgeClass = "slot-button__badge";

        private readonly Label _badge;

        public HudSlot(bool round)
        {
            Button = new Button { focusable = false };
            Button.AddToClassList(SlotClass);
            if (round)
                Button.AddToClassList(RoundClass);

            _badge = new Label { pickingMode = PickingMode.Ignore };
            _badge.AddToClassList(BadgeClass);
            Button.Add(_badge);
        }

        public Button Button { get; }

        /// <summary>The text over the icon (empty when there is none).</summary>
        public string Badge => _badge.text;

        public bool IsSelected => Button.ClassListContains(SelectedClass);
        public bool IsDisabled => Button.ClassListContains(DisabledClass);
        public bool IsEmpty => Button.ClassListContains(EmptyClass);

        /// <summary>Shows the whole state of the slot at once.</summary>
        public void Set(Sprite icon, string badge, bool selected, bool disabled, bool empty)
        {
            Button.iconImage = icon != null ? UnityEngine.UIElements.Background.FromSprite(icon) : default;
            _badge.text = badge ?? string.Empty;
            Button.EnableInClassList(SelectedClass, selected);
            Button.EnableInClassList(DisabledClass, disabled);
            Button.EnableInClassList(EmptyClass, empty);
        }
    }
}
