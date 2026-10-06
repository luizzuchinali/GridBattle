using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.States;
using UnityEngine;

namespace GridBattle.Gameplay.Run
{
    /// <summary>
    /// Moves the run's player between <see cref="PlayerRunState"/> (what persists between nodes) and the
    /// <see cref="PlayerCharacter"/> that plays a battle: applied when a battle starts or is restored, captured
    /// when it ends. Talent effects go through <see cref="RunPlayerHooks"/>.
    /// </summary>
    public static class PlayerRunStateApplier
    {
        /// <summary>
        /// Gives a freshly spawned player the run's progress: level and XP, skills, consumables (the inventory
        /// writes straight into <see cref="PlayerRunState.ConsumableIds"/>) and, for a new battle, the carried
        /// states and cooldowns. Follow it with <see cref="FinishNewBattle"/> (new battle) or
        /// <see cref="RestoreSnapshotState"/> + <see cref="FinishRestoredBattle"/> (battle restored from a snapshot,
        /// which already holds the states and cooldowns).
        /// </summary>
        public static void Apply(PlayerCharacter player, PlayerRunState run, bool restoredFromSnapshot)
        {
            player.SetProgress(run.Level, run.Xp);
            player.SetSkills(ResolveSkills(run, player));
            player.SetInventory(new ConsumableInventory(run.ConsumableIds));

            if (!restoredFromSnapshot)
            {
                StateSnapshots.Restore(player, run.States);
                if (!RunSettings.Current.ResetSkillCooldownsBetweenBattles)
                    player.Cooldowns.Restore(run.SkillCooldowns);
            }
        }

        /// <summary>
        /// Restores the player's exact battle state from a snapshot entity: replaces the states (the config's
        /// initial states included), the cooldowns and the HP. Call after <see cref="Apply"/>.
        /// </summary>
        public static void RestoreSnapshotState(PlayerCharacter player, EntitySnapshot snapshot)
        {
            player.States.Clear();
            StateSnapshots.Restore(player, snapshot.States);
            player.Cooldowns.Restore(snapshot.SkillCooldowns);
            player.RestoreMoved(snapshot.MovedLastTurn);
        }

        /// <summary>Applies the talent hooks and then the persistent HP (a new battle).</summary>
        public static void FinishNewBattle(PlayerCharacter player, PlayerRunState run)
        {
            RunPlayerHooks.NotifyPlayerSpawned(player, run, false);
            player.SetHp(run.Hp > 0 ? run.Hp : player.MaxHp);
        }

        /// <summary>Applies the talent hooks and then the HP saved in the snapshot (a restored battle).</summary>
        public static void FinishRestoredBattle(PlayerCharacter player, PlayerRunState run, EntitySnapshot snapshot)
        {
            RunPlayerHooks.NotifyPlayerSpawned(player, run, true);
            player.SetHp(snapshot.Hp);
        }

        /// <summary>
        /// Copies what persists from the player at the end of a won battle into the run: HP (at least 1), level
        /// and XP, the states that travel to the next battle and, if the settings ask for it, the skill
        /// cooldowns.
        /// </summary>
        public static void Capture(PlayerCharacter player, PlayerRunState run, RunSettings settings)
        {
            run.Hp = Mathf.Max(1, player.Current);
            run.Level = player.Level;
            run.Xp = player.CurrentXp;
            run.States = CaptureCarriedStates(player, run, settings);
            run.SkillCooldowns = settings.ResetSkillCooldownsBetweenBattles
                ? new List<CounterState>()
                : player.Cooldowns.Capture();
        }

        /// <summary>
        /// The states that travel to the next battle: not those of the class config nor those re-applied by the
        /// talent module, and (by default) only the permanent ones.
        /// </summary>
        public static List<StateSnapshot> CaptureCarriedStates(PlayerCharacter player, PlayerRunState run,
            RunSettings settings)
        {
            var keepTemporary = settings.TemporaryStates == ETemporaryStatePolicy.KeepTemporary;
            return StateSnapshots.Capture(player, state =>
            {
                if (!keepTemporary && !state.IsPermanent) return false;
                if (IsInitialState(player.Config, state)) return false;
                return !IsTalentState(run, state);
            });
        }

        private static bool IsInitialState(CharacterConfig config, StateInstance state)
        {
            if (config == null || state.SourceId != null) return false;

            foreach (var grant in config.InitialStates)
            {
                if (grant.IsValid && grant.State == state.Definition)
                    return true;
            }

            return false;
        }

        private static bool IsTalentState(PlayerRunState run, StateInstance state)
        {
            if (!string.IsNullOrEmpty(state.SourceId))
            {
                foreach (var talent in run.Talents)
                {
                    if (talent.TalentId == state.SourceId)
                        return true;
                }
            }

            return RunPlayerHooks.OwnsState(run, state);
        }

        private static List<SkillDefinition> ResolveSkills(PlayerRunState run, PlayerCharacter player)
        {
            var result = new List<SkillDefinition>();
            var database = GameDatabase.Instance;
            if (database != null)
            {
                foreach (var id in run.SkillIds)
                {
                    var skill = database.Get<SkillDefinition>(id);
                    if (skill != null && !result.Contains(skill))
                        result.Add(skill);
                }
            }

            if (result.Count == 0 && player.Config != null)
            {
                // A run without saved skills starts with the class's own.
                foreach (var skill in player.Config.Skills)
                {
                    if (skill != null)
                        result.Add(skill);
                }
            }

            return result;
        }
    }
}
