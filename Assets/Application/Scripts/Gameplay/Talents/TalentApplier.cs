using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.States;

namespace GridBattle.Gameplay.Talents
{
    /// <summary>
    /// Puts the effects of talents on a live character: the talent's permanent states, applied with the talent
    /// id as <see cref="StateInstance.SourceId"/> and one stack of each granted stack per rank.
    /// </summary>
    public static class TalentApplier
    {
        /// <summary>
        /// Applies the states of <paramref name="talent"/> at <paramref name="rank"/>, replacing the ones of the
        /// previous rank (the same talent's states are removed first, so the stacks always equal the rank).
        /// </summary>
        public static void ApplyStates(Character holder, TalentDefinition talent, int rank)
        {
            if (holder == null || talent == null || talent.States.Count == 0) return;

            holder.States.RemoveBySource(talent.Id);
            foreach (var grant in talent.States)
            {
                if (!grant.IsValid) continue;

                holder.States.Apply(grant.State, StateInstance.Permanent, TalentRules.GetStacks(grant, rank), talent.Id);
            }
        }

        /// <summary>Whether the holder already has every state of the talent, permanent and with the stacks of the rank.</summary>
        public static bool AreStatesInSync(Character holder, TalentDefinition talent, int rank)
        {
            foreach (var grant in talent.States)
            {
                if (!grant.IsValid) continue;

                var instance = holder.States.Find(grant.State, talent.Id);
                if (instance == null || !instance.IsPermanent || instance.Stacks != TalentRules.GetStacks(grant, rank))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Makes the player hold the states of every talent of the run. Idempotent: states that are already
        /// right (e.g. restored from a battle snapshot, with their exact stacks) are left alone; missing or
        /// outdated ones are applied.
        /// </summary>
        public static void SyncStates(Character player, PlayerRunState run)
        {
            foreach (var entry in run.Talents)
            {
                var talent = TalentRules.Resolve(entry.TalentId);
                if (talent == null) continue;

                var rank = System.Math.Max(1, entry.Rank);
                if (!AreStatesInSync(player, talent, rank))
                    ApplyStates(player, talent, rank);
            }
        }
    }
}
