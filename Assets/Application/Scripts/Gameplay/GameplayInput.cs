using GridBattle.Gameplay.Turns;

namespace GridBattle.Gameplay
{
    /// <summary>
    /// Whether the player's game input (cell taps, skill and item buttons) is accepted right now.
    /// It is not while something holds the turn flow (<see cref="TurnBlockers"/>): the entity
    /// details window, the talent choice, the pause menu, XP orbs that will level up. Input
    /// sources and routers ask here instead of knowing who the blockers are.
    /// </summary>
    public static class GameplayInput
    {
        /// <summary>True while any turn blocker is held: game actions are ignored.</summary>
        public static bool IsBlocked => TurnBlockers.IsBlocked;
    }
}
