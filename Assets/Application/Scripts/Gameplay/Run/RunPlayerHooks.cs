using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.States;

namespace GridBattle.Gameplay.Run
{
    /// <summary>
    /// Extension point for systems that change the run's player beyond what <see cref="PlayerRunState"/> stores
    /// by itself (the talent module: talent states, maximum HP bonuses). Implement it, register it with
    /// <see cref="RunPlayerHooks.Register"/> (e.g. in a <c>RuntimeInitializeOnLoadMethod</c>) and the run
    /// manager and the grid call it at the right moments.
    /// </summary>
    public interface IRunPlayerModifier
    {
        /// <summary>
        /// A player character was built for a battle (new battle or restored from a save) and already has the
        /// run's level, XP, skills, consumables and carried states. Apply the talents' effects here (states with
        /// the talent id as <see cref="StateInstance.SourceId"/>, extra skills...). Called before the HP is set,
        /// so maximum HP bonuses count.
        /// <para>
        /// Must be idempotent: when <paramref name="restoredFromSnapshot"/> is true the player's states were
        /// restored from the battle snapshot and talent states may already be present (keep them, they carry the
        /// exact remaining turns, stacks and shield); only apply the ones that are missing
        /// (<c>player.States.Find(definition, talentId) == null</c>).
        /// </para>
        /// </summary>
        void OnPlayerSpawned(PlayerCharacter player, PlayerRunState run, bool restoredFromSnapshot);

        /// <summary>
        /// Maximum HP of the run's player outside battles (heal node, talent node cost, HUD between battles):
        /// <paramref name="baseMaxHp"/> is the class's base; return it plus the talents' bonuses. Must agree with
        /// what <see cref="OnPlayerSpawned"/> produces in battle.
        /// </summary>
        int ModifyMaxHp(PlayerRunState run, PlayerCharacterConfig playerClass, int baseMaxHp) => baseMaxHp;

        /// <summary>
        /// Whether the state belongs to this module (e.g. it was granted by a talent) and is re-applied by
        /// <see cref="OnPlayerSpawned"/> every battle, so it must not be carried between battles as a regular state.
        /// </summary>
        bool OwnsState(PlayerRunState run, StateInstance state) => false;
    }

    /// <summary>
    /// Registry of the <see cref="IRunPlayerModifier"/>s. Static state, reset on SubsystemRegistration (domain
    /// reload is off in Play Mode): register again from a <c>RuntimeInitializeOnLoadMethod</c> with
    /// <c>RuntimeInitializeLoadType.BeforeSceneLoad</c> or from a manager's Awake.
    /// </summary>
    public static class RunPlayerHooks
    {
        private static readonly List<IRunPlayerModifier> Modifiers = new();

        /// <summary>Adds a modifier (ignored if it is already registered).</summary>
        public static void Register(IRunPlayerModifier modifier)
        {
            if (modifier != null && !Modifiers.Contains(modifier))
                Modifiers.Add(modifier);
        }

        public static void Unregister(IRunPlayerModifier modifier) => Modifiers.Remove(modifier);

        /// <summary>Calls <see cref="IRunPlayerModifier.OnPlayerSpawned"/> on every registered modifier.</summary>
        public static void NotifyPlayerSpawned(PlayerCharacter player, PlayerRunState run, bool restoredFromSnapshot)
        {
            foreach (var modifier in Modifiers.ToArray())
                modifier.OnPlayerSpawned(player, run, restoredFromSnapshot);
        }

        /// <summary>Maximum HP of the run's player outside battles: the class base through every modifier.</summary>
        public static int GetMaxHp(PlayerRunState run, PlayerCharacterConfig playerClass)
        {
            var maxHp = playerClass != null ? playerClass.MaxHp : 1;
            foreach (var modifier in Modifiers.ToArray())
                maxHp = modifier.ModifyMaxHp(run, playerClass, maxHp);

            return maxHp < 1 ? 1 : maxHp;
        }

        /// <summary>Whether any modifier owns the state (see <see cref="IRunPlayerModifier.OwnsState"/>).</summary>
        public static bool OwnsState(PlayerRunState run, StateInstance state)
        {
            foreach (var modifier in Modifiers.ToArray())
            {
                if (modifier.OwnsState(run, state))
                    return true;
            }

            return false;
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Modifiers.Clear();
    }
}
