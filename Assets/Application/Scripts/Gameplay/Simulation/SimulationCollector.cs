using System;
using System.Collections.Generic;
using System.Diagnostics;
using GridBattle.Data;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Progression;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Talents;
using GridBattle.Gameplay.Terrain;
using GridBattle.Managers;
using JetBrains.Annotations;

namespace GridBattle.Gameplay.Simulation
{
    /// <summary>
    /// Listens to the game's events (the same ones the UI and the design metrics use) and turns them into the
    /// <see cref="BattleRecord"/> and <see cref="RunRecord"/> of the simulation. One collector serves a whole batch:
    /// <see cref="BeginRun"/> opens a run, the run ends by itself with <c>RunEndedEvent</c> (or by
    /// <see cref="AbortRun"/> when the simulator stops it) and the finished records pile up in
    /// <see cref="Runs"/> and <see cref="Battles"/>. A battle record closes on <c>BattleEndedEvent</c> or on the
    /// run's end, whichever the game raises first (a lost battle ends the run from inside the same event).
    /// </summary>
    public sealed class SimulationCollector : IDisposable
    {
        private readonly List<RunRecord> _runs = new();
        private readonly List<BattleRecord> _battles = new();
        private readonly Stopwatch _runClock = new();
        private readonly Stopwatch _battleClock = new();

        private RunRecord _run;
        private BattleRecord _battle;
        private string _lastHitSource = string.Empty;
        private string _abortReason;
        private int _lastOfferOptionCount;
        private bool _subscribed;
        private bool _battleClosePending;
        private bool _battleCloseVictory;
        private RunSummary _runEndPending;

        public SimulationCollector()
        {
            Subscribe();
        }

        public IReadOnlyList<RunRecord> Runs => _runs;
        public IReadOnlyList<BattleRecord> Battles => _battles;

        /// <summary>The run being recorded, or null between runs.</summary>
        [CanBeNull]
        public RunRecord CurrentRun => _run;

        /// <summary>The battle being recorded, or null between battles.</summary>
        [CanBeNull]
        public BattleRecord CurrentBattle => _battle;

        /// <summary>Opens the record of a run; call it right before <c>RunManager.StartNewRun</c>.</summary>
        public void BeginRun(string className, int runIndex, long seed)
        {
            Flush();
            _run = new RunRecord { ClassName = className, RunIndex = runIndex, Seed = seed };
            _battle = null;
            _abortReason = null;
            _lastHitSource = string.Empty;
            _runClock.Restart();
        }

        /// <summary>
        /// The simulator is stopping the run (the caller gives up the run right after): the record ends as aborted
        /// with <paramref name="reason"/>.
        /// </summary>
        public void AbortRun(string reason)
        {
            _abortReason = reason;
            if (_battle != null)
            {
                _battle.Aborted = true;
                _battle.AbortReason = reason;
            }
        }

        public void Dispose()
        {
            if (!_subscribed) return;

            _subscribed = false;
            EventBus.Unsubscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Unsubscribe<NodeEnteredEvent>(OnNodeEntered);
            EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Unsubscribe<GlobalTurnStartedEvent>(OnGlobalTurnStarted);
            EventBus.Unsubscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<HealedEvent>(OnHealed);
            EventBus.Unsubscribe<CharacterDiedEvent>(OnCharacterDied);
            EventBus.Unsubscribe<SkillUsedEvent>(OnSkillUsed);
            EventBus.Unsubscribe<ConsumableUsedEvent>(OnConsumableUsed);
            EventBus.Unsubscribe<TerrainTriggeredEvent>(OnTerrainTriggered);
            EventBus.Unsubscribe<CharacterDisplacedEvent>(OnCharacterDisplaced);
            EventBus.Unsubscribe<EnemySummonedEvent>(OnEnemySummoned);
            EventBus.Unsubscribe<TalentOfferOpenedEvent>(OnTalentOfferOpened);
            EventBus.Unsubscribe<TalentOfferChangedEvent>(OnTalentOfferChanged);
            EventBus.Unsubscribe<TalentAcquiredEvent>(OnTalentAcquired);
            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Unsubscribe<RunEndedEvent>(OnRunEnded);
        }

        private void Subscribe()
        {
            _subscribed = true;
            EventBus.Subscribe<RunStartedEvent>(OnRunStarted);
            EventBus.Subscribe<NodeEnteredEvent>(OnNodeEntered);
            EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
            EventBus.Subscribe<GlobalTurnStartedEvent>(OnGlobalTurnStarted);
            EventBus.Subscribe<PlayerActionEvent>(OnPlayerAction);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<HealedEvent>(OnHealed);
            EventBus.Subscribe<CharacterDiedEvent>(OnCharacterDied);
            EventBus.Subscribe<SkillUsedEvent>(OnSkillUsed);
            EventBus.Subscribe<ConsumableUsedEvent>(OnConsumableUsed);
            EventBus.Subscribe<TerrainTriggeredEvent>(OnTerrainTriggered);
            EventBus.Subscribe<CharacterDisplacedEvent>(OnCharacterDisplaced);
            EventBus.Subscribe<EnemySummonedEvent>(OnEnemySummoned);
            EventBus.Subscribe<TalentOfferOpenedEvent>(OnTalentOfferOpened);
            EventBus.Subscribe<TalentOfferChangedEvent>(OnTalentOfferChanged);
            EventBus.Subscribe<TalentAcquiredEvent>(OnTalentAcquired);
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
        }

        // ------------------------------------------------------------------------------------ run and node

        private void OnRunStarted(RunStartedEvent e)
        {
            if (_run == null || e.Run == null) return;

            _run.Floors = e.Run.Map.FloorCount;
            foreach (var node in e.Run.Map.Nodes)
            {
                if ((node.Type == EMapNodeType.Battle || node.Type == EMapNodeType.Boss) && node.Battle != null)
                {
                    _run.MapBattles.Add(new MapNodeInfo(node.Depth, node.Type, node.Difficulty, node.Battle.TotalXp,
                        node.Battle.Enemies.Count));
                }
            }
        }

        private void OnNodeEntered(NodeEnteredEvent e)
        {
            if (_run == null || e.Node == null) return;

            switch (e.Node.Type)
            {
                case EMapNodeType.Heal:
                    _run.HealNodes++;
                    break;
                case EMapNodeType.Talent:
                    _run.TalentNodes++;
                    break;
                case EMapNodeType.Consumable:
                    _run.ConsumableNodes++;
                    break;
            }
        }

        // ------------------------------------------------------------------------------------ battle

        private void OnBattleStarted(BattleStartedEvent e)
        {
            if (_run == null) return;

            Flush();

            // A battle left open (should not happen) is closed as aborted before the next one starts.
            if (_battle != null)
                CloseBattle(false, "interrupted");

            var runs = RunManager.Instance;
            var database = GameDatabase.Instance;
            var run = runs != null ? runs.CurrentRun : null;
            _battle = new BattleRecord
            {
                ClassName = _run.ClassName,
                RunIndex = _run.RunIndex,
                Seed = _run.Seed,
                NodeId = e.Node != null ? e.Node.Id : -1,
                Depth = e.Spec != null ? e.Spec.Depth : (e.Node != null ? e.Node.Depth : 0),
                NodeType = e.Node != null ? e.Node.Type : EMapNodeType.Battle,
                Difficulty = e.Node != null ? e.Node.Difficulty : EBattleDifficulty.Normal,
                PlannedXp = e.Spec != null ? e.Spec.TotalXp : 0,
                HpStart = run != null ? run.Player.Hp : 0,
                MaxHp = runs != null ? runs.PlayerMaxHp : 0,
                LevelStart = run != null ? run.Player.Level : 1,
            };
            _battle.XpTotalStart = run != null ? TotalXp(run.Player.Level, run.Player.Xp) : 0;
            if (e.Spec != null)
            {
                foreach (var enemy in e.Spec.Enemies)
                {
                    var config = database != null ? database.Get<EnemyConfig>(enemy.EnemyId) : null;
                    _battle.EnemyNames.Add(config != null ? config.name : enemy.EnemyId);
                    _battle.RoleNames.Add(config != null && config.Role != null ? config.Role.name : "-");
                }

                foreach (var cell in e.Spec.Terrain)
                {
                    var terrain = database != null ? database.Get<TerrainDefinition>(cell.TerrainId) : null;
                    if (terrain == null) continue;

                    switch (terrain.Kind)
                    {
                        case ETerrainKind.Obstacle:
                            _battle.ObstacleCells++;
                            break;
                        case ETerrainKind.Hazard:
                            _battle.HazardCells++;
                            break;
                        default:
                            _battle.BonusCells++;
                            break;
                    }
                }
            }

            _lastHitSource = string.Empty;
            _battleClock.Restart();
        }

        private void OnEnemySummoned(EnemySummonedEvent e)
        {
            if (_battle != null)
                _battle.Summoned++;
        }

        private void OnGlobalTurnStarted(GlobalTurnStartedEvent e)
        {
            if (_battle != null)
                _battle.GlobalTurns = e.GlobalTurn;
        }

        private void OnPlayerAction(PlayerActionEvent e)
        {
            if (_battle != null)
                _battle.Turns++;
        }

        private void OnDamageDealt(DamageDealtEvent e)
        {
            if (_battle == null) return;

            var hit = e.Hit;
            if (hit.Target is PlayerCharacter)
            {
                var source = hit.Attacker is Enemy attacker && attacker.EnemyConfig != null
                    ? attacker.EnemyConfig.name
                    : hit.Kind.ToString().ToLowerInvariant();
                _lastHitSource = source;
                if (hit.HpDamage <= 0) return;

                _battle.DamageTaken += hit.HpDamage;
                Add(_battle.DamageByEnemy, source, hit.HpDamage);
                Add(_battle.DamageByKind, hit.Kind.ToString(), hit.HpDamage);
            }
            else if (hit.Target is Enemy)
            {
                _battle.DamageDealt += hit.Damage;
                if (hit.Kind == EDamageKind.Collision && hit.Killed)
                    _battle.CollisionKills++;
            }
        }

        private void OnHealed(HealedEvent e)
        {
            if (_battle != null && e.Character is PlayerCharacter)
                _battle.HealingReceived += e.Amount;
        }

        private void OnCharacterDied(CharacterDiedEvent e)
        {
            if (_battle == null || e.Character is not Enemy enemy) return;

            Add(_battle.Kills, enemy.EnemyConfig != null ? enemy.EnemyConfig.name : enemy.name, 1);
            _battle.GainedXp += enemy.XpReward;
        }

        private void OnSkillUsed(SkillUsedEvent e)
        {
            if (_battle == null || e.Caster is not PlayerCharacter) return;

            _battle.SkillsUsed++;
            Add(_battle.SkillUses, e.Skill != null ? e.Skill.name : "?", 1);
        }

        private void OnConsumableUsed(ConsumableUsedEvent e)
        {
            if (_battle == null || e.User is not PlayerCharacter) return;

            _battle.ConsumablesUsed++;
            Add(_battle.ItemUses, e.Consumable != null ? e.Consumable.name : "?", 1);
        }

        private void OnTerrainTriggered(TerrainTriggeredEvent e)
        {
            if (_battle == null || e.Terrain == null) return;

            var isPlayer = e.Character is PlayerCharacter;
            Add(_battle.TerrainBy, e.Terrain.name + (isPlayer ? "/player" : "/enemy"), 1);
            if (!isPlayer) return;

            _battle.TerrainTriggers++;
            _battle.TerrainDamage += e.Damage;
        }

        /// <summary>Pushes and pulls: who was displaced, collisions and the damage they dealt, terrain reacting at once.</summary>
        private void OnCharacterDisplaced(CharacterDisplacedEvent e)
        {
            if (_battle == null) return;

            var targetIsPlayer = e.Target is PlayerCharacter;
            if (targetIsPlayer)
                _battle.PlayerDisplaced++;
            else
                _battle.EnemiesDisplaced++;

            if (e.Collided)
                _battle.Collisions++;

            if (targetIsPlayer)
                _battle.CollisionDamageToPlayer += e.CollisionDamage;
            else
                _battle.CollisionDamageToEnemies += e.CollisionDamage;

            if (e.BlockedBy is Enemy)
                _battle.CollisionDamageToEnemies += e.HitCharacterDamage;
            else if (e.BlockedBy is PlayerCharacter)
                _battle.CollisionDamageToPlayer += e.HitCharacterDamage;

            if (e.ForcedTerrain != null)
            {
                if (targetIsPlayer)
                    _battle.ForcedTerrainPlayer++;
                else
                    _battle.ForcedTerrainEnemies++;
            }
        }

        /// <summary>
        /// The battle's end is only noted here: events of the same moment still follow in the same call (the XP of
        /// the last kill, the damage event of the blow that killed the player), so the records close in
        /// <see cref="Flush"/>, which the runner calls before it does anything else.
        /// </summary>
        private void OnBattleEnded(BattleEndedEvent e)
        {
            if (_battle == null || _battleClosePending) return;

            _battleClosePending = true;
            _battleCloseVictory = e.Victory;
        }

        /// <summary>Closes the battle and the run whose end was announced (call it once the game has settled).</summary>
        public void Flush()
        {
            if (_battleClosePending)
            {
                _battleClosePending = false;
                if (_battle != null)
                    CloseBattle(_battleCloseVictory, null);
            }

            if (_runEndPending != null)
            {
                var summary = _runEndPending;
                _runEndPending = null;
                FinishRun(summary);
            }
        }

        private void CloseBattle(bool victory, string abortReason)
        {
            var battle = _battle;
            if (battle == null) return;

            _battle = null;
            var run = RunManager.Instance != null ? RunManager.Instance.CurrentRun : null;
            battle.Victory = victory;
            battle.HpEnd = victory && run != null ? run.Player.Hp : 0;
            battle.LevelEnd = run != null ? run.Player.Level : battle.LevelStart;
            battle.XpCredited = run != null ? TotalXp(run.Player.Level, run.Player.Xp) - battle.XpTotalStart : 0;
            battle.Milliseconds = _battleClock.Elapsed.TotalMilliseconds;
            if (abortReason != null && !battle.Aborted)
            {
                battle.Aborted = true;
                battle.AbortReason = abortReason;
            }

            _battles.Add(battle);
            if (_run != null)
            {
                _run.BattlesPlayed++;
                _run.Turns += battle.Turns;
                _run.SkillsUsed += battle.SkillsUsed;
                _run.ConsumablesUsed += battle.ConsumablesUsed;
                _run.TerrainTriggers += battle.TerrainTriggers;
                _run.EnemiesDisplaced += battle.EnemiesDisplaced;
                _run.PlayerDisplaced += battle.PlayerDisplaced;
                _run.Collisions += battle.Collisions;
                _run.CollisionDamageToEnemies += battle.CollisionDamageToEnemies;
                _run.CollisionDamageToPlayer += battle.CollisionDamageToPlayer;
                _run.CollisionKills += battle.CollisionKills;
                _run.ForcedTerrainEnemies += battle.ForcedTerrainEnemies;
                _run.ForcedTerrainPlayer += battle.ForcedTerrainPlayer;
                _run.DamageDealt += battle.DamageDealt;
                _run.DamageTaken += battle.DamageTaken;
            }
        }

        // ------------------------------------------------------------------------------------ talents

        private void OnTalentOfferOpened(TalentOfferOpenedEvent e)
        {
            CountOffered(e.Offer, -1);
            _lastOfferOptionCount = e.Offer != null ? e.Offer.Options.Count : 0;
        }

        private void OnTalentOfferChanged(TalentOfferChangedEvent e)
        {
            if (e.Offer == null) return;

            switch (e.Change)
            {
                case ETalentOfferChange.Rerolled:
                    CountOffered(e.Offer, -1);
                    break;
                case ETalentOfferChange.Banned:
                    // A replacement drawn for the banned option keeps the option count; otherwise the option left.
                    if (e.Offer.Options.Count == _lastOfferOptionCount)
                        CountOffered(e.Offer, e.BannedIndex);
                    break;
            }

            _lastOfferOptionCount = e.Offer.Options.Count;
        }

        private void CountOffered(TalentOffer offer, int onlyIndex)
        {
            if (_run == null || offer == null) return;

            for (var i = 0; i < offer.Options.Count; i++)
            {
                if (onlyIndex >= 0 && i != onlyIndex) continue;

                Add(_run.TalentsOffered, offer.Options[i].Talent.name, 1);
            }
        }

        private void OnTalentAcquired(TalentAcquiredEvent e)
        {
            if (_run == null || e.Talent == null) return;

            _run.TalentsPicked++;
            Add(_run.TalentsTaken, e.Talent.name, 1);

            var manager = RunManager.Instance;
            var playerClass = manager != null ? manager.PlayerClass : null;
            if (TalentRules.IsSharedOnly(e.Talent, playerClass))
                _run.SharedTalentPicks++;
            else
                _run.ClassTalentPicks++;
        }

        // ------------------------------------------------------------------------------------ run end

        private void OnRunEnded(RunEndedEvent e)
        {
            if (_run == null || _runEndPending != null) return;

            // The run's end comes from inside the battle's end event: both close in Flush.
            if (_battle != null && !_battleClosePending)
            {
                _battleClosePending = true;
                _battleCloseVictory = e.Victory;
            }

            _runEndPending = e.Summary;
        }

        private void FinishRun(RunSummary summary)
        {
            var run = _run;
            if (run == null) return;

            _run = null;
            var runs = RunManager.Instance;
            var state = runs != null ? runs.CurrentRun : null;
            run.Result = _abortReason != null
                ? ERunResult.Aborted
                : summary.Reason == ERunEndReason.Victory ? ERunResult.Victory
                : summary.Reason == ERunEndReason.Defeat ? ERunResult.Defeat
                : ERunResult.Aborted;
            run.AbortReason = _abortReason ?? (run.Result == ERunResult.Aborted ? "given_up" : string.Empty);
            run.EndDepth = summary.Depth;
            run.Floors = summary.FloorCount;
            run.FinalLevel = summary.Level;
            run.BattlesWon = summary.BattlesWon;
            run.DistinctTalents = summary.Talents.Count;
            if (state != null)
            {
                run.Rerolls = state.Player.RerollsUsed;
                run.Bans = state.Player.BansUsed;
                run.Skips = state.Player.SkipsUsed;
            }

            var database = GameDatabase.Instance;
            foreach (var id in summary.SkillIds)
            {
                var skill = database != null ? database.Get<SkillDefinition>(id) : null;
                run.FinalSkills.Add(skill != null ? skill.name : id);
            }

            if (run.Result == ERunResult.Defeat)
                run.KilledBy = _lastHitSource;

            run.Milliseconds = _runClock.Elapsed.TotalMilliseconds;
            _runs.Add(run);
            _abortReason = null;
        }

        /// <summary>All the XP a player of this level and XP has earned (the levels' costs plus the current XP).</summary>
        private static int TotalXp(int level, int xp)
        {
            var settings = ProgressionSettings.Current;
            var total = xp;
            for (var l = 1; l < level; l++)
                total += settings.GetXpToNextLevel(l);
            return total;
        }

        private static void Add(Dictionary<string, int> counts, string key, int amount)
        {
            counts.TryGetValue(key, out var current);
            counts[key] = current + amount;
        }
    }
}
