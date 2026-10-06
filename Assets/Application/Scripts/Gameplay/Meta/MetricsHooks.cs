using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using UnityEngine;

namespace GridBattle.Gameplay.Meta
{
    /// <summary>
    /// Records, through <see cref="MetricsRecorder"/>, the design metrics (GDD 9) that the game
    /// already raises events for:
    /// <list type="bullet">
    /// <item><c>battle_started</c>: enemy ids, player HP, class.</item>
    /// <item><c>enemy_killed</c> and <c>player_died</c> (with the killer), as they happen.</item>
    /// <item><c>battle_ended</c>: victory, turns (last global turn) and the damage dealt/taken by
    /// source (each enemy type, or the damage kind for damage over time, terrain...).</item>
    /// <item><c>class_unlocked</c>: total battles won and runs played when a class unlocks.</item>
    /// </list>
    /// (<c>run_ended</c> is recorded by <see cref="ProfileService.RegisterRunEnded"/>.)
    /// The summary is written one frame after the battle ends because the hit that killed the
    /// player is announced after the defeat.
    /// </summary>
    internal static class MetricsHooks
    {
        /// <summary>Damage exchanged with the player by one source (an enemy type, or a damage kind).</summary>
        private sealed class SourceStats
        {
            public string Id;
            public string Name;

            /// <summary>Damage this source dealt to the player (before shields).</summary>
            public int DealtToPlayer;

            /// <summary>Part of it that took HP away from the player.</summary>
            public int HpLostByPlayer;

            /// <summary>Damage this source (an enemy type) received from anyone.</summary>
            public int Taken;

            public int Kills;
        }

        private sealed class BattleMetrics
        {
            public int LastTurn;
            public bool Ended;

            /// <summary>Player state when the battle ended (the player object is destroyed right after a defeat).</summary>
            public int PlayerHp;
            public int PlayerMaxHp;
            public readonly Dictionary<string, SourceStats> Sources = new();
        }

        private static BattleMetrics _battle;

        internal static void Subscribe()
        {
            Unsubscribe();
            EventBus.Subscribe<GridInitializedEvent>(OnGridInitialized);
            EventBus.Subscribe<GlobalTurnStartedEvent>(OnGlobalTurnStarted);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Subscribe<ClassUnlockedEvent>(OnClassUnlocked);
        }

        internal static void Unsubscribe()
        {
            EventBus.Unsubscribe<GridInitializedEvent>(OnGridInitialized);
            EventBus.Unsubscribe<GlobalTurnStartedEvent>(OnGlobalTurnStarted);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Unsubscribe<ClassUnlockedEvent>(OnClassUnlocked);
            _battle = null;
        }

        private static bool Active => Application.isPlaying && MetricsRecorder.Enabled;

        private static void OnGridInitialized(GridInitializedEvent e)
        {
            if (!Active) return;

            _battle = new BattleMetrics { LastTurn = e.FirstGlobalTurn };

            var enemies = new List<object>();
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
            {
                if (enemy.Config != null)
                    enemies.Add(Describe(enemy.Config));
            }

            var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
            MetricsRecorder.Record("battle_started", new
            {
                @class = player != null && player.Config != null ? Describe(player.Config) : null,
                playerHp = player != null ? player.Current : 0,
                playerMaxHp = player != null ? player.MaxHp : 0,
                playerLevel = player != null ? player.Level : 0,
                firstTurn = e.FirstGlobalTurn,
                restored = e.FirstGlobalTurn > 1,
                enemies,
            });
        }

        private static void OnGlobalTurnStarted(GlobalTurnStartedEvent e)
        {
            if (_battle != null)
                _battle.LastTurn = e.GlobalTurn;
        }

        private static void OnDamageDealt(DamageDealtEvent e)
        {
            if (!Active) return;

            var hit = e.Hit;
            if (_battle == null)
                _battle = new BattleMetrics();

            if (hit.Target is PlayerCharacter)
            {
                var source = GetSource(hit.Attacker, hit.Kind);
                source.DealtToPlayer += hit.Damage;
                source.HpLostByPlayer += hit.HpDamage;
                if (hit.Killed)
                {
                    source.Kills++;
                    MetricsRecorder.Record("player_died", new
                    {
                        killer = new { id = source.Id, name = source.Name },
                        kind = hit.Kind.ToString(),
                        turn = _battle.LastTurn,
                        level = ((PlayerCharacter)hit.Target).Level,
                    });
                }
            }
            else if (hit.Target is Enemy enemy && enemy.Config != null)
            {
                var source = GetSource(enemy, EDamageKind.Pure);
                source.Taken += hit.Damage;
                if (hit.Killed)
                {
                    MetricsRecorder.Record("enemy_killed", new
                    {
                        enemy = Describe(enemy.Config),
                        kind = hit.Kind.ToString(),
                        byPlayer = hit.Attacker is PlayerCharacter,
                        turn = _battle.LastTurn,
                    });
                }
            }
        }

        /// <summary>GDD 9: how many runs the player needs to unlock each class.</summary>
        private static void OnClassUnlocked(ClassUnlockedEvent e)
        {
            if (!Active) return;

            var runsPlayed = 0;
            foreach (var record in ProfileService.State.Classes.Values)
                runsPlayed += record.RunsPlayed;

            MetricsRecorder.Record("class_unlocked", new
            {
                @class = Describe(e.Config),
                totalBattlesWon = ProfileService.TotalBattlesWon,
                runsPlayed,
            });
        }

        private static void OnBattleEnded(BattleEndedEvent e)
        {
            if (!Active) return;

            var battle = _battle ?? new BattleMetrics();
            if (battle.Ended) return;

            battle.Ended = true;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
            battle.PlayerHp = player != null ? Mathf.Max(0, player.Current) : 0;
            battle.PlayerMaxHp = player != null ? player.MaxHp : 0;
            _ = WriteSummaryNextFrame(battle, e.Victory);
        }

        private static async Awaitable WriteSummaryNextFrame(BattleMetrics battle, bool victory)
        {
            try
            {
                await Awaitable.NextFrameAsync();
            }
            catch (OperationCanceledException)
            {
                // Leaving Play Mode: still write what was gathered.
            }

            var sources = new List<object>();
            foreach (var source in battle.Sources.Values)
            {
                sources.Add(new
                {
                    id = source.Id,
                    name = source.Name,
                    dealtToPlayer = source.DealtToPlayer,
                    hpLostByPlayer = source.HpLostByPlayer,
                    taken = source.Taken,
                    kills = source.Kills,
                });
            }

            MetricsRecorder.Record("battle_ended", new
            {
                victory,
                turns = battle.LastTurn,
                playerHp = battle.PlayerHp,
                playerMaxHp = battle.PlayerMaxHp,
                sources,
            });
            MetricsRecorder.Flush();
        }

        /// <summary>The stats entry of an enemy type, or of a damage kind when there is no attacker.</summary>
        private static SourceStats GetSource(Character attacker, EDamageKind kind)
        {
            string id;
            string name;
            if (attacker is Enemy enemy && enemy.Config != null)
            {
                id = enemy.Config.Id;
                name = enemy.Config.name;
            }
            else if (attacker != null && attacker.Config != null)
            {
                id = attacker.Config.Id;
                name = attacker.Config.name;
            }
            else
            {
                id = kind.ToString().ToLowerInvariant();
                name = id;
            }

            if (!_battle.Sources.TryGetValue(id, out var stats))
            {
                stats = new SourceStats { Id = id, Name = name };
                _battle.Sources[id] = stats;
            }

            return stats;
        }

        private static object Describe(CharacterConfig config) => new { id = config.Id, name = config.name };
    }
}
