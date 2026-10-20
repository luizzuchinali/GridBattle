using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised when a character earns another walk in the current turn (a kill with Momentum,
    /// <c>OnKillEffect.GrantExtraWalk</c>). The player's controller gives the walk back; enemies ignore it.
    /// </summary>
    public class ExtraWalkGrantedEvent
    {
        public ExtraWalkGrantedEvent(Character character)
        {
            Character = character;
        }

        public Character Character { get; }
    }
}
