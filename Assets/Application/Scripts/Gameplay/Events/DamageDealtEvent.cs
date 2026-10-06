using GridBattle.Gameplay.Combat;

namespace GridBattle.Gameplay.Events
{
    /// <summary>
    /// Raised for every hit resolved by CombatResolver (damage text, audio, metrics).
    /// </summary>
    public class DamageDealtEvent
    {
        public HitResult Hit { get; }

        public DamageDealtEvent(HitResult hit)
        {
            Hit = hit;
        }
    }
}
