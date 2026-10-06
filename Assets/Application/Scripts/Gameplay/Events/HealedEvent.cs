using GridBattle.Gameplay.Entities;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised when a character recovers HP.
    /// </summary>
    public class HealedEvent
    {
        public Character Character { get; }
        public int Amount { get; }

        public HealedEvent(Character character, int amount)
        {
            Character = character;
            Amount = amount;
        }
    }
}
