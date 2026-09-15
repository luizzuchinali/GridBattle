namespace GridBattle.UI.Events
{
    public enum ECharacter
    {
        Warrior,
        Mage,
        Rogue
    }
    
    public class CharacterChoosenEvent
    {
        public ECharacter Character { get; private set; }

        public CharacterChoosenEvent(ECharacter character)
        {
            Character = character;
        }
    }
}