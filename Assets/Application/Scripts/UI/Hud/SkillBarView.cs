using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Meta;
using UnityEngine.UIElements;

namespace GridBattle.UI.Hud
{
    /// <summary>
    /// The skill bar (GDD 4.2): one square button per skill slot (<see cref="SkillSettings.MaxSkillSlots"/>)
    /// showing the player's skills in order, the remaining cooldown over the icon, the selected skill
    /// highlighted and a disabled look when it cannot be selected (not the player's turn, cooldown,
    /// silenced). A tap selects the skill for aiming, and tapping it again cancels (the controller decides).
    /// </summary>
    public sealed class SkillBarView : SlotBarView
    {
        public SkillBarView(VisualElement container) : base(container, false)
        {
            Watch<SkillListChangedEvent>(e =>
            {
                if (e.Owner is not PlayerCharacter) return;

                FindPlayer();
                Refresh();
            });
            Watch<SkillCooldownsChangedEvent>(e => RefreshIfPlayer(e.Owner));
            Watch<SkillSelectionChangedEvent>(e => RefreshIfPlayer(e.Owner));
            Watch<StateAppliedEvent>(e => RefreshIfPlayer(e.Character));
            Watch<StateRemovedEvent>(e => RefreshIfPlayer(e.Character));
            Watch<TurnChangedEvent>(e =>
            {
                if (e.IsPlayerTurn)
                    NotifyFirstSkill();
            });
            Refresh();
        }

        protected override int GetSlotCount() => SkillSettings.Current.MaxSkillSlots;

        protected override void RefreshSlot(int index, HudSlot slot)
        {
            var skill = GetSkill(index);
            if (skill == null)
            {
                slot.Set(null, string.Empty, false, false, true);
                return;
            }

            var remaining = Player.Cooldowns.GetRemaining(skill);
            var selected = Controller != null && Controller.SelectedSkill == skill;
            var disabled = IsInactive || !SkillTargeting.CanSelect(Player, skill);
            slot.Set(skill.Icon, remaining > 0 ? remaining.ToString() : string.Empty, selected, disabled && !selected, false);
        }

        protected override void OnSlotTapped(int index)
        {
            var skill = GetSkill(index);
            if (skill != null && Controller != null)
                Controller.SelectSkill(skill);
        }

        private SkillDefinition GetSkill(int index)
        {
            if (Player == null) return null;

            var skills = Player.Skills;
            return index < skills.Count ? skills[index] : null;
        }

        /// <summary>
        /// The "how to use a skill" tip (interface 4.4) shows the first time the player starts a turn with a skill on
        /// the bar; the tutorial service shows each tip only once.
        /// </summary>
        private void NotifyFirstSkill()
        {
            if (GetSkill(0) != null)
                TutorialService.Notify(ETutorialTrigger.FirstSkill);
        }

        private void RefreshIfPlayer(Character character)
        {
            if (character is PlayerCharacter)
                Refresh();
        }
    }
}
