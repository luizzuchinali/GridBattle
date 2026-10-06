using System.Collections.Generic;
using GridBattle.Core.Randomness;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Run;
using GridBattle.Managers;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Talents
{
    /// <summary>
    /// What the talent choice screen (and anything else that needs the build) calls: the open offer, the
    /// player's tools and the counts. The talent module is event driven: a level up
    /// (<see cref="PlayerLeveledUpEvent"/>) or a talent node (<see cref="TalentNodeEnteredEvent"/>) opens an
    /// offer, <see cref="TalentOfferOpenedEvent"/> tells the UI, and the UI answers with <see cref="Choose"/>,
    /// <see cref="Reroll"/>, <see cref="Ban"/> or <see cref="Skip"/> (each raises its own events and resumes
    /// the game when the last pending offer is resolved).
    /// <para>
    /// The service works on the run in progress (<see cref="RunManager.CurrentRun"/>). Outside a run (a battle
    /// started from the editor with <c>GridController.InitializeGrid</c>) a level up still opens offers, drawn
    /// from the class pool over a throwaway run: nothing is saved and the profile and metrics are not touched.
    /// </para>
    /// </summary>
    public static class TalentService
    {
        private static TalentSession _session;

        // ------------------------------------------------------------------ the open offer

        /// <summary>The session of the current run (or of the debug battle), or null when there is none.</summary>
        [CanBeNull]
        public static TalentSession Session => ResolveSession();

        /// <summary>An offer is open: the game is paused until it is resolved.</summary>
        public static bool HasOffer => Session != null && Session.HasOffer;

        /// <summary>The open offer, or null.</summary>
        [CanBeNull]
        public static TalentOffer CurrentOffer => Session?.CurrentOffer;

        /// <summary>Offers waiting, the open one included.</summary>
        public static int PendingCount => Session != null ? Session.PendingCount : 0;

        /// <summary>Rerolls the run has left (0 without a run).</summary>
        public static int RerollsLeft => Session != null ? Session.RerollsLeft : 0;

        /// <summary>Bans the run has left (0 without a run).</summary>
        public static int BansLeft => Session != null ? Session.BansLeft : 0;

        /// <summary>Skips the run has left (0 without a run).</summary>
        public static int SkipsLeft => Session != null ? Session.SkipsLeft : 0;

        // ------------------------------------------------------------------ the player's tools

        /// <summary>
        /// Takes the option at <paramref name="index"/> of the open offer. The talent takes effect at once.
        /// Returns false when no offer is open or the index is invalid.
        /// </summary>
        public static bool Choose(int index) => Session != null && Session.Choose(index);

        /// <summary>Rerolls the open offer (uses a reroll). Returns false when none is left or nothing new can be shown.</summary>
        public static bool Reroll() => Session != null && Session.Reroll();

        /// <summary>Bans the option at <paramref name="index"/> (uses a ban) and replaces it. Returns false when none is left.</summary>
        public static bool Ban(int index) => Session != null && Session.Ban(index);

        /// <summary>Skips the open offer (uses a skip); that level's talent is lost. Returns false when none is left.</summary>
        public static bool Skip() => Session != null && Session.Skip();

        // ------------------------------------------------------------------ the build

        /// <summary>The talents the run's player has taken, in pick order (empty without a run).</summary>
        public static IReadOnlyList<TalentRankState> GetBuild()
        {
            var session = Session;
            return session != null ? session.Run.Player.Talents : new List<TalentRankState>();
        }

        /// <summary>Rank of a talent in the current run (0 = not taken).</summary>
        public static int GetRank(TalentDefinition talent) =>
            Session != null ? TalentRules.GetRank(Session.Run.Player, talent) : 0;

        /// <summary>
        /// Every talent a class can be offered during a run (its pool plus the shared pool, restricted by the
        /// talents' allowed classes): what the glossary lists for the class.
        /// </summary>
        public static List<TalentDefinition> GetPool(PlayerCharacterConfig playerClass) =>
            TalentRules.GetPool(playerClass);

        // ------------------------------------------------------------------ plumbing (called by TalentBootstrap)

        /// <summary>
        /// The session for the active run, created on demand; a throwaway debug session is kept as it is and a
        /// session of a run that is over is dropped.
        /// </summary>
        [CanBeNull]
        private static TalentSession ResolveSession()
        {
            var manager = RunManager.Instance;
            if (manager != null && manager.IsRunActive && manager.CurrentRun != null && manager.PlayerClass != null)
            {
                var run = manager.CurrentRun;
                if (_session == null || _session.Run != run || !_session.IsPersistent)
                {
                    DropSession();
                    _session = new TalentSession(run, manager.PlayerClass, true) { LivePlayerProvider = FindBattlePlayer };
                }

                return _session;
            }

            if (_session != null && !_session.IsPersistent)
                return _session;

            DropSession();
            return null;
        }

        /// <summary>The player of the battle in progress (talents apply to it at once); none between battles.</summary>
        private static PlayerCharacter FindBattlePlayer()
        {
            var manager = RunManager.Instance;
            if (manager == null || !manager.IsBattleActive) return null;

            return Object.FindAnyObjectByType<PlayerCharacter>();
        }

        /// <summary>The player of a battle that is not part of a run.</summary>
        private static PlayerCharacter FindAnyPlayer() => Object.FindAnyObjectByType<PlayerCharacter>();

        private static TalentSession EnsureDebugSession(PlayerCharacter player)
        {
            if (_session != null && !_session.IsPersistent) return _session;

            DropSession();
            var run = new RunState
            {
                Random = new RunRandom(RunRandom.CreateSeed()),
                ClassId = player.PlayerConfig != null ? player.PlayerConfig.Id : null,
                Phase = ERunPhase.Battle,
            };
            run.Player.Level = player.Level;
            _session = new TalentSession(run, player.PlayerConfig, false) { LivePlayerProvider = FindAnyPlayer };
            return _session;
        }

        internal static void OnPlayerLeveledUp(PlayerLeveledUpEvent e)
        {
            if (!Application.isPlaying || e.Player == null) return;

            var session = ResolveSession();
            if (session == null)
                session = EnsureDebugSession(e.Player);
            if (!session.IsPersistent)
                session.Run.Player.Level = e.Player.Level;

            // The final boss is dead: the run ends right after the last XP lands, a talent would be pointless.
            if (session.IsPersistent && IsFinalBossDefeated() && !TalentOfferSettings.Current.OfferAfterFinalBossKill)
                return;

            session.EnqueueLevelUp(e.Level);
        }

        private static bool IsFinalBossDefeated()
        {
            var manager = RunManager.Instance;
            if (manager == null || !manager.IsBattleActive) return false;

            var node = manager.CurrentNode;
            return node != null && node.Type == EMapNodeType.Boss && !BattleController.AnyEnemyAlive();
        }

        internal static void OnTalentNodeEntered(TalentNodeEnteredEvent e)
        {
            if (!Application.isPlaying) return;

            var session = ResolveSession();
            if (session != null && session.IsPersistent)
                session.OpenNodeOffer();
        }

        internal static void OnRunStarted(RunStartedEvent e)
        {
            DropSession();
            ResolveSession();
        }

        internal static void OnRunEnded(RunEndedEvent e) => DropSession();

        internal static void OnGridInitialized(GridInitializedEvent e)
        {
            if (_session == null) return;

            // A new battle: a debug session starts over, a run's session only forgets what was on screen.
            if (_session.IsPersistent)
                _session.Cancel();
            else
                DropSession();
        }

        /// <summary>Opens the offers a resumed run left pending (the map or a restored battle just opened).</summary>
        internal static void OnResumePoint()
        {
            if (!Application.isPlaying) return;

            var session = ResolveSession();
            if (session != null && session.IsPersistent)
                session.ResumePending();
        }

        internal static void ResetStatics() => DropSession();

        private static void DropSession()
        {
            if (_session == null) return;

            _session.Cancel();
            _session = null;
        }
    }
}
