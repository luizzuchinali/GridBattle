using GridBattle.Gameplay.Meta;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised by TutorialService when a contextual tip should be shown (tips enabled and the tip
    /// not seen yet). The UI shows it and calls TutorialService.MarkSeen(tip) so it never repeats.
    /// </summary>
    public class TutorialTipRequestedEvent
    {
        public TutorialTipDefinition Tip { get; }

        public TutorialTipRequestedEvent(TutorialTipDefinition tip)
        {
            Tip = tip;
        }
    }
}
