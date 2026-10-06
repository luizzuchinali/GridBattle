using System;
using System.Collections.Generic;
using GridBattle.Core.Randomness;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.States;
using GridBattle.Gameplay.Turns;
using GridBattle.Managers;
using GridBattle.Managers.Audio;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Talents
{
    /// <summary>
    /// The talent offers of one run (or of a debug battle outside a run): the queue of pending offers kept in
    /// <see cref="RunState.PendingOffers"/>, the open offer, and the player's tools (choose, reroll, ban, skip).
    /// <see cref="TalentService"/> owns the session of the current run and is what the UI calls; the session
    /// itself has no static state, so tests can drive one over any <see cref="RunState"/>.
    /// <para>
    /// While an offer is open the game is paused (a <c>TurnBlockers</c> handle, "Talent choice", and
    /// <c>AudioManager.SetTalentPause</c>); it resumes when the last pending offer is resolved. A persistent
    /// session (a real run) also saves the run at talent nodes, reports to the profile and the metrics and, for a
    /// talent node, completes the node through <c>RunManager.CompleteTalentNode</c>. A non-persistent one (a
    /// debug battle) does none of that: offers draw from the class pool and nothing is written to disk.
    /// </para>
    /// </summary>
    public sealed class TalentSession : IDisposable
    {
        private readonly RunState _run;
        private readonly PlayerCharacterConfig _class;
        private readonly bool _persistent;
        private IDisposable _blocker;
        private TalentOffer _current;

        /// <param name="run">The run whose talents and offers this session manages (needs a <see cref="RunState.Random"/>).</param>
        /// <param name="playerClass">Class of the run (its pool, its starting skills).</param>
        /// <param name="persistent">A real run: saves, profile, metrics and the talent node completion apply.</param>
        public TalentSession(RunState run, PlayerCharacterConfig playerClass, bool persistent)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _class = playerClass;
            _persistent = persistent;
            LivePlayerProvider = DefaultLivePlayer;
        }

        /// <summary>The run (or the throwaway run of a debug battle) the session works on.</summary>
        public RunState Run => _run;

        public PlayerCharacterConfig PlayerClass => _class;

        /// <summary>Whether this is a real run (saved, reported) or a debug battle.</summary>
        public bool IsPersistent => _persistent;

        /// <summary>
        /// The settings the session reads (the registered asset by default; tests can replace it).
        /// </summary>
        public TalentOfferSettings Settings { get; set; }

        /// <summary>
        /// Returns the player character of the battle in progress, or null when there is none (the talent node,
        /// the map, tests). Talents are applied to it at once, and its states give the run modifiers.
        /// </summary>
        [NotNull]
        public Func<PlayerCharacter> LivePlayerProvider { get; set; }

        /// <summary>Raised after a talent node's offer was resolved (a persistent session also completes the node itself).</summary>
        public event Action NodeOfferCompleted;

        // ------------------------------------------------------------------ state

        private PlayerRunState Player => _run.Player;
        private TalentOfferSettings Config => Settings != null ? Settings : TalentOfferSettings.Current;

        /// <summary>An offer is open (the game is paused).</summary>
        public bool HasOffer => _current != null;

        /// <summary>The open offer, or null.</summary>
        [CanBeNull]
        public TalentOffer CurrentOffer => _current;

        /// <summary>Offers waiting, the open one included.</summary>
        public int PendingCount => _run.PendingOffers.Count;

        /// <summary>The player's run modifier of a kind: from the live character's states in battle, else from the talents.</summary>
        public int GetRunModifier(ERunModifier modifier)
        {
            var live = GetLivePlayer();
            return live != null ? live.States.GetRunModifier(modifier) : TalentRules.GetRunModifier(Player, modifier);
        }

        /// <summary>Talents an offer shows now (base + the states' modifier).</summary>
        public int OptionCount => Config.GetOptionCount(GetRunModifier(ERunModifier.TalentOptions));

        /// <summary>Rerolls the run has left (base + modifier - used).</summary>
        public int RerollsLeft => Math.Max(0, Config.BaseRerolls + GetRunModifier(ERunModifier.Rerolls) - Player.RerollsUsed);

        /// <summary>Bans the run has left.</summary>
        public int BansLeft => Math.Max(0, Config.BaseBans + GetRunModifier(ERunModifier.Bans) - Player.BansUsed);

        /// <summary>Skips the run has left.</summary>
        public int SkipsLeft => Math.Max(0, Config.BaseSkips + GetRunModifier(ERunModifier.Skips) - Player.SkipsUsed);

        // ------------------------------------------------------------------ opening offers

        /// <summary>
        /// A level was reached: queues its offer and, if none is open, opens it. Levels gained together queue in
        /// order and open one after the other. The options are drawn when the offer opens, so a talent taken at
        /// the previous level counts for the next one.
        /// </summary>
        public void EnqueueLevelUp(int level)
        {
            _run.PendingOffers.Add(new TalentOfferState { Source = ETalentOfferSource.LevelUp, Level = level });
            SaveRun();
            if (_current == null)
            {
                OpenNext();
                return;
            }

            // An offer is open: tell the screen that one more waits behind it.
            _current = BuildOffer(_run.PendingOffers[0]);
            EventBus.Raise(new TalentOfferChangedEvent(_current, ETalentOfferChange.Queued));
        }

        /// <summary>
        /// The talent node's offer: opens the saved one when the run was resumed with it pending (same options),
        /// otherwise creates it. Completes the node when it is resolved.
        /// </summary>
        public void OpenNodeOffer()
        {
            if (_current != null) return;

            var exists = false;
            foreach (var pending in _run.PendingOffers)
            {
                if (pending.Source == ETalentOfferSource.TalentNode)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                _run.PendingOffers.Add(new TalentOfferState
                {
                    Source = ETalentOfferSource.TalentNode,
                    Level = Player.Level,
                });
                SaveRun();
            }

            OpenNext();
        }

        /// <summary>Opens the oldest pending offer, if none is open (leftovers of a resumed run).</summary>
        public void ResumePending()
        {
            if (_current == null && _run.PendingOffers.Count > 0)
                OpenNext();
        }

        // ------------------------------------------------------------------ the player's tools

        /// <summary>
        /// Takes the option at <paramref name="index"/>: the talent takes effect at once (rank + 1, its states on
        /// the live player, its skill unlocked), the offer is resolved, and the next pending offer opens or the
        /// game resumes. Returns false for an invalid index or when no offer is open.
        /// </summary>
        public bool Choose(int index)
        {
            if (_current == null || index < 0 || index >= _current.Options.Count) return false;

            var offer = _current;
            var option = offer.Options[index];
            var state = _run.PendingOffers[0];

            var rank = GrantTalent(option.Talent);
            AudioManager.Play(ESfx.TalentChosen);

            if (_persistent)
            {
                ProfileService.RegisterTalentChosen(_class, option.Talent.Id);
                MetricsRecorder.Record("talent_chosen", new
                {
                    source = state.Source.ToString(),
                    level = offer.Level,
                    talent = option.Talent.Id,
                    name = option.Talent.name,
                    rank,
                    matchedBuild = option.MatchesBuild,
                    rerolls = offer.RerollIndex,
                    offered = Names(offer.Options),
                });
                for (var i = 0; i < offer.Options.Count; i++)
                {
                    if (i != index)
                        RecordDeclined(offer.Options[i].Talent, "chosen");
                }
            }

            Resolve(offer, state, ETalentOfferResult.Chosen, option.Talent, rank);
            return true;
        }

        /// <summary>
        /// Draws the options again (uses one reroll; the reroll index is part of the random key, so the new
        /// options are reproducible). Returns false when the run has no rerolls left or there is nothing new to show.
        /// </summary>
        public bool Reroll()
        {
            if (_current == null || RerollsLeft <= 0) return false;

            var state = _run.PendingOffers[0];
            var level = GetOfferLevel(state);
            var key = GetOfferKey(state);
            var count = OptionCount;
            var config = Config;

            var next = state.RerollIndex + 1;
            List<string> drawn;
            if (config.RerollExcludesCurrentOptions)
            {
                // Nothing outside the current options: the reroll would show the same talents again.
                if (TalentOfferGenerator.GetCandidates(Player, _class, level, config, state.OptionIds).Count == 0)
                    return false;

                drawn = TalentOfferGenerator.GenerateAvoiding(Player, _class, level, count, config, _run.Random, key,
                    next, state.OptionIds);
            }
            else
            {
                drawn = TalentOfferGenerator.Generate(Player, _class, level, count, config, _run.Random, key, next);
            }

            if (drawn.Count == 0) return false;

            Player.RerollsUsed++;
            state.RerollIndex = next;
            state.OptionIds = drawn;
            AudioManager.Play(ESfx.Reroll);

            _current = BuildOffer(state);
            if (_persistent)
            {
                MetricsRecorder.Record("talent_rerolled", new
                {
                    source = state.Source.ToString(),
                    level = _current.Level,
                    rerollIndex = state.RerollIndex,
                    options = Names(_current.Options),
                });
            }

            ReportOffered(_current);
            SaveRun();
            EventBus.Raise(new TalentOfferChangedEvent(_current, ETalentOfferChange.Rerolled));
            return true;
        }

        /// <summary>
        /// Bans the option at <paramref name="index"/> (uses one ban): the talent leaves the run's pool for good
        /// and a new draw (from the talents not on screen) takes its place. If nothing can replace it the option
        /// is simply removed, and an offer left empty closes by itself. Returns false when the run has no bans
        /// left or the index is invalid.
        /// </summary>
        public bool Ban(int index)
        {
            if (_current == null || BansLeft <= 0 || index < 0 || index >= _current.Options.Count) return false;

            var state = _run.PendingOffers[0];
            var banned = _current.Options[index].Talent;

            Player.BannedTalentIds.Add(banned.Id);
            Player.BansUsed++;
            AudioManager.Play(ESfx.Ban);

            var replacement = TalentOfferGenerator.DrawReplacement(Player, _class, GetOfferLevel(state), Config,
                _run.Random, GetOfferKey(state), state.RerollIndex, Player.BansUsed, state.OptionIds);
            if (replacement != null)
                state.OptionIds[index] = replacement;
            else
                state.OptionIds.RemoveAt(index);

            if (_persistent)
            {
                MetricsRecorder.Record("talent_banned", new
                {
                    source = state.Source.ToString(),
                    level = GetOfferLevel(state),
                    talent = banned.Id,
                    name = banned.name,
                    replacedBy = replacement,
                });
            }

            if (state.OptionIds.Count == 0)
            {
                // Nothing left to offer: the offer closes by itself.
                var empty = BuildOffer(state);
                SaveRun();
                Resolve(empty, state, ETalentOfferResult.NoOptions, null, 0);
                return true;
            }

            _current = BuildOffer(state);
            if (replacement != null)
                ReportOffered(_current, replacement);
            SaveRun();
            EventBus.Raise(new TalentOfferChangedEvent(_current, ETalentOfferChange.Banned, index));
            return true;
        }

        /// <summary>
        /// Skips the offer (uses one skip): no talent is taken and that level's choice is lost. Returns false
        /// when the run has no skips left or no offer is open.
        /// </summary>
        public bool Skip()
        {
            if (_current == null || SkipsLeft <= 0) return false;

            var offer = _current;
            var state = _run.PendingOffers[0];
            Player.SkipsUsed++;
            AudioManager.Play(ESfx.Skip);

            if (_persistent)
            {
                MetricsRecorder.Record("talent_skipped", new
                {
                    source = state.Source.ToString(),
                    level = offer.Level,
                    offered = Names(offer.Options),
                });
                foreach (var option in offer.Options)
                    RecordDeclined(option.Talent, "skipped");
            }

            Resolve(offer, state, ETalentOfferResult.Skipped, null, 0);
            return true;
        }

        // ------------------------------------------------------------------ taking a talent

        /// <summary>
        /// Gives the run one more rank of <paramref name="talent"/> and puts it in effect: the talent is stored
        /// in <see cref="PlayerRunState.Talents"/>, its states go on the live player (so it counts for the rest
        /// of the turn), its skill is unlocked, and a Max HP bonus restores the same amount of HP when the
        /// settings say so. Used by <see cref="Choose"/>; also available to debug tools. Returns the new rank (the
        /// current one, with no effect, when the talent is already at its maximum rank).
        /// </summary>
        public int GrantTalent(TalentDefinition talent)
        {
            // Already at the maximum rank: nothing to add.
            var currentRank = TalentRules.GetRank(Player, talent);
            if (currentRank >= talent.MaxRank)
                return currentRank;

            var baseMaxHp = _class != null ? _class.MaxHp : 1;
            var maxHpBefore = TalentRules.GetMaxHp(Player, baseMaxHp);

            var rank = TalentRules.AddRank(Player, talent);
            var unlockedSkill = false;
            if (talent.IsSkillTalent)
            {
                EnsureStartingSkillIds();
                if (!Player.SkillIds.Contains(talent.UnlockedSkill.Id))
                {
                    Player.SkillIds.Add(talent.UnlockedSkill.Id);
                    unlockedSkill = true;
                }
            }

            var live = GetLivePlayer();
            if (live != null)
            {
                TalentApplier.ApplyStates(live, talent, rank);
                if (unlockedSkill)
                    live.SetSkills(TalentRules.GetCurrentSkills(Player, _class));
            }

            var maxHpGain = TalentRules.GetMaxHp(Player, baseMaxHp) - maxHpBefore;
            if (maxHpGain > 0 && Config.MaxHpGainHealsSameAmount)
                HealPlayer(live, maxHpGain);

            EventBus.Raise(new TalentAcquiredEvent(talent, rank, unlockedSkill));
            return rank;
        }

        /// <summary>The run keeps an explicit skill list as soon as a talent adds to it, starting from the class's skills.</summary>
        private void EnsureStartingSkillIds()
        {
            if (Player.SkillIds.Count > 0 || _class == null) return;

            foreach (var skill in _class.Skills)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.Id) && !Player.SkillIds.Contains(skill.Id))
                    Player.SkillIds.Add(skill.Id);
            }
        }

        private void HealPlayer([CanBeNull] PlayerCharacter live, int amount)
        {
            if (live != null)
            {
                live.RestoreHp(amount);
                return;
            }

            if (_persistent && RunManager.Instance != null)
            {
                RunManager.Instance.ChangePlayerHp(amount);
                return;
            }

            var maxHp = TalentRules.GetMaxHp(Player, _class != null ? _class.MaxHp : 1);
            Player.Hp = Mathf.Clamp(Player.Hp + amount, 1, maxHp);
        }

        // ------------------------------------------------------------------ opening and closing

        private void OpenNext()
        {
            while (_run.PendingOffers.Count > 0)
            {
                var state = _run.PendingOffers[0];
                if (state.OptionIds.Count == 0)
                {
                    state.OptionIds = TalentOfferGenerator.Generate(Player, _class, GetOfferLevel(state), OptionCount,
                        Config, _run.Random, GetOfferKey(state), state.RerollIndex);

                    if (state.OptionIds.Count == 0)
                    {
                        // The pool is exhausted: nothing to choose, the level passes.
                        var empty = BuildOffer(state);
                        _run.PendingOffers.RemoveAt(0);
                        if (_persistent)
                        {
                            MetricsRecorder.Record("talent_offer_empty", new
                            {
                                source = state.Source.ToString(),
                                level = empty.Level,
                            });
                        }

                        SaveRun();
                        EventBus.Raise(new TalentOfferResolvedEvent(empty, ETalentOfferResult.NoOptions, null, 0,
                            _run.PendingOffers.Count > 0));
                        if (state.Source == ETalentOfferSource.TalentNode && _run.PendingOffers.Count == 0)
                        {
                            Release();
                            CompleteNode();
                            return;
                        }

                        continue;
                    }

                    SaveRun();
                }

                _current = BuildOffer(state);
                Acquire();
                AudioManager.Play(ESfx.TalentOfferOpen);
                if (_persistent)
                {
                    if (state.Source == ETalentOfferSource.LevelUp)
                        TutorialService.Notify(ETutorialTrigger.FirstLevelUp);
                    MetricsRecorder.Record("talent_offered", new
                    {
                        source = state.Source.ToString(),
                        level = _current.Level,
                        options = Names(_current.Options),
                        rerollsLeft = _current.RerollsLeft,
                        bansLeft = _current.BansLeft,
                        skipsLeft = _current.SkipsLeft,
                    });
                }

                ReportOffered(_current);
                EventBus.Raise(new TalentOfferOpenedEvent(_current));
                return;
            }

            Release();
        }

        private void Resolve(TalentOffer offer, TalentOfferState state, ETalentOfferResult result,
            [CanBeNull] TalentDefinition chosen, int rank)
        {
            _run.PendingOffers.Remove(state);
            _current = null;
            SaveRun();

            EventBus.Raise(new TalentOfferResolvedEvent(offer, result, chosen, rank, _run.PendingOffers.Count > 0));

            OpenNext();
            if (state.Source == ETalentOfferSource.TalentNode && _current == null && _run.PendingOffers.Count == 0)
                CompleteNode();
        }

        private void CompleteNode()
        {
            if (_persistent && RunManager.Instance != null)
                RunManager.Instance.CompleteTalentNode();
            NodeOfferCompleted?.Invoke();
        }

        private void Acquire()
        {
            if (_blocker != null) return;

            _blocker = TurnBlockers.Acquire("Talent choice");
            AudioManager.SetTalentPause(true);
        }

        private void Release()
        {
            _current = null;
            if (_blocker == null) return;

            _blocker.Dispose();
            _blocker = null;
            AudioManager.SetTalentPause(false);
        }

        /// <summary>
        /// Drops the open offer from the screen state and releases the pause, without touching the run's data
        /// (the run ended, a new battle began, or the app is closing the session). Pending offers stay in the run.
        /// </summary>
        public void Cancel()
        {
            Release();
        }

        public void Dispose() => Cancel();

        // ------------------------------------------------------------------ building offers

        private TalentOffer BuildOffer(TalentOfferState state)
        {
            var tagCounts = TalentRules.GetTakenTagCounts(Player);
            var options = new List<TalentOption>();
            foreach (var id in state.OptionIds)
            {
                var talent = TalentRules.Resolve(id);
                if (talent == null) continue;

                options.Add(new TalentOption(talent, TalentRules.GetRank(Player, talent),
                    TalentRules.GetSharedTags(talent, tagCounts)));
            }

            var index = _run.PendingOffers.IndexOf(state);
            var queuedAfter = index >= 0 ? _run.PendingOffers.Count - 1 - index : 0;
            return new TalentOffer(state.Source, GetOfferLevel(state), state.RerollIndex, options, RerollsLeft,
                BansLeft, SkipsLeft, queuedAfter);
        }

        /// <summary>
        /// Level whose talents the offer may contain: the level reached for a level-up, the player's level plus
        /// <see cref="TalentOfferSettings.TalentNodeLevelBonus"/> for the talent node (it works like an extra level).
        /// </summary>
        private int GetOfferLevel(TalentOfferState state) =>
            state.Source == ETalentOfferSource.LevelUp
                ? state.Level
                : Math.Max(1, Player.Level) + Config.TalentNodeLevelBonus;

        private long GetOfferKey(TalentOfferState state) =>
            state.Source == ETalentOfferSource.LevelUp
                ? TalentOfferGenerator.GetLevelKey(state.Level)
                : TalentOfferGenerator.GetNodeKey(_run.Map != null ? _run.Map.CurrentNodeId : 0);

        // ------------------------------------------------------------------ helpers

        [CanBeNull]
        private PlayerCharacter GetLivePlayer()
        {
            var live = LivePlayerProvider();
            return live != null && !live.IsDead ? live : null;
        }

        private static PlayerCharacter DefaultLivePlayer() => UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();

        private void SaveRun()
        {
            if (!_persistent || RunManager.Instance == null || _run.Phase == ERunPhase.Battle) return;

            RunManager.Instance.SaveNow();
        }

        private void ReportOffered(TalentOffer offer, [CanBeNull] string onlyId = null)
        {
            if (!_persistent) return;

            foreach (var option in offer.Options)
            {
                if (onlyId == null || option.Talent.Id == onlyId)
                    ProfileService.RegisterTalentOffered(_class, option.Talent.Id);
            }
        }

        private static void RecordDeclined(TalentDefinition talent, string reason)
        {
            MetricsRecorder.Record("talent_declined", new { talent = talent.Id, name = talent.name, reason });
        }

        private static List<string> Names(IReadOnlyList<TalentOption> options)
        {
            var names = new List<string>(options.Count);
            foreach (var option in options)
                names.Add(option.Talent.name);
            return names;
        }
    }
}
