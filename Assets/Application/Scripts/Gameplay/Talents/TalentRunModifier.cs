using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.States;

namespace GridBattle.Gameplay.Talents
{
    /// <summary>
    /// The talent module's hook into the run's player (<see cref="IRunPlayerModifier"/>): the states of the
    /// run's talents are applied to every player character built for a battle, the talents' Max HP modifiers
    /// count outside battles, and talent states are not carried between battles as regular states (they are
    /// re-applied each battle).
    /// </summary>
    public sealed class TalentRunModifier : IRunPlayerModifier
    {
        public void OnPlayerSpawned(PlayerCharacter player, PlayerRunState run, bool restoredFromSnapshot)
        {
            TalentApplier.SyncStates(player, run);
        }

        public int ModifyMaxHp(PlayerRunState run, PlayerCharacterConfig playerClass, int baseMaxHp) =>
            TalentRules.GetMaxHp(run, baseMaxHp);

        public bool OwnsState(PlayerRunState run, StateInstance state)
        {
            if (string.IsNullOrEmpty(state.SourceId)) return false;

            foreach (var entry in run.Talents)
            {
                if (entry.TalentId == state.SourceId)
                    return true;
            }

            return TalentRules.Resolve(state.SourceId) != null;
        }
    }
}
