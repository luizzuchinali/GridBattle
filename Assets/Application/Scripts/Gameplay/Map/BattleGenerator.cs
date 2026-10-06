using System;
using System.Collections.Generic;
using GridBattle.Core.Randomness;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Roles;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.Terrain;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Map
{
    /// <summary>
    /// The threat of an enemy (balanceamento_e_geracao.md): <c>damage x HP / divisor x movement factor x range
    /// factor x role factor</c>, with the coefficients configured in <see cref="BattleGenerationSettings"/>.
    /// Pure and static: the generator spends a budget of threat to compose battles.
    /// </summary>
    public static class ThreatCalculator
    {
        /// <summary>Threat of one enemy with the given HP and damage multipliers (1 = the config's base stats).</summary>
        public static float Compute(EnemyConfig config, float hpMultiplier, float damageMultiplier,
            BattleGenerationSettings settings)
        {
            if (config == null) return 0f;

            var damage = config.BasicAttackDamage * damageMultiplier;
            var hp = config.MaxHp * hpMultiplier;
            var movementFactor = Mathf.Max(0f, 1f + settings.MovementCoefficient * (config.WalkDistance - 1));
            var rangeFactor = Mathf.Max(0f, 1f + settings.RangeCoefficient * (config.AttackDistance - 1));
            var roleFactor = config.Role != null ? config.Role.ThreatFactor : 1f;
            return damage * hp / settings.ThreatDivisor * movementFactor * rangeFactor * roleFactor;
        }

        /// <summary>Total threat of the enemies of a generated battle (ids resolved through the GameDatabase).</summary>
        public static float GetTotal(BattleSpec spec, BattleGenerationSettings settings)
        {
            var database = GameDatabase.Instance;
            if (spec == null || database == null) return 0f;

            var total = 0f;
            foreach (var enemy in spec.Enemies)
            {
                var config = database.Get<EnemyConfig>(enemy.EnemyId);
                var hp = settings.UseScaledThreat ? enemy.HpMultiplier : 1f;
                var damage = settings.UseScaledThreat ? enemy.DamageMultiplier : 1f;
                total += Compute(config, hp, damage, settings);
            }

            return total;
        }
    }

    /// <summary>
    /// Generates the battle of a map node (GDD Mechanic 2, balanceamento_e_geracao.md): which enemies enter, how
    /// many, where they start and the XP the battle grants. The number of enemies comes from a threat budget by
    /// depth and difficulty (stronger enemies only enter at higher depths, weaker ones in greater numbers), within
    /// the limits and composition rules of <see cref="BattleGenerationSettings"/>. Pure and deterministic: the
    /// only source of randomness is the <see cref="Rng"/> argument, so the same seed always gives the same battle.
    /// </summary>
    public static class BattleGenerator
    {
        private sealed class Candidate
        {
            public EnemyConfig Config;
            public float Weight;
            public float Threat;
            public bool Frontline;
        }

        /// <summary>
        /// Generates one battle. <paramref name="terrain"/> (from <see cref="TerrainGenerator"/>) is copied into
        /// the spec and decides where enemies can start. A boss battle ignores the budget and the difficulty: it
        /// holds the settings' boss encounter scaled for its depth.
        /// </summary>
        /// <param name="depth">Depth of the node on the map (stored in the spec).</param>
        /// <param name="balanceDepth">
        /// Depth the settings are read at (<see cref="MapGenerationSettings.GetBalanceDepth"/>): budget, scaling
        /// and XP use it as is, the enemy pool uses the nearest whole depth. Negative = <paramref name="depth"/>.
        /// </param>
        /// <param name="xpScale">Factor of the battle's XP (<see cref="MapGenerationSettings.RunLengthXpFactor"/>).</param>
        public static BattleSpec Generate(Rng rng, int depth, EBattleDifficulty difficulty, bool isBoss,
            BattleGenerationSettings settings, [CanBeNull] IReadOnlyList<TerrainCellSpec> terrain = null,
            float balanceDepth = -1f, float xpScale = 1f)
        {
            if (balanceDepth < 0f) balanceDepth = depth;

            var size = settings.GridSize;
            var spec = new BattleSpec
            {
                Depth = depth,
                Width = size.x,
                Height = size.y,
                PlayerX = settings.PlayerSpawn.x,
                PlayerY = settings.PlayerSpawn.y,
                IsBoss = isBoss,
            };
            if (terrain != null)
            {
                foreach (var cell in terrain)
                    spec.Terrain.Add(new TerrainCellSpec { X = cell.X, Y = cell.Y, TerrainId = cell.TerrainId });
            }

            var placementCells = GetPlacementCells(spec, settings);
            var effectiveDifficulty = isBoss ? EBattleDifficulty.Normal : difficulty;
            var profile = settings.GetProfile(effectiveDifficulty);
            var hpMultiplier = settings.GetHpMultiplier(balanceDepth) * profile.StrengthMultiplier;
            var damageMultiplier = settings.GetDamageMultiplier(balanceDepth) * profile.StrengthMultiplier;

            var picks = isBoss
                ? PickBoss(settings, hpMultiplier, damageMultiplier)
                : DrawEnemies(rng, balanceDepth, difficulty, spec, placementCells.Count, settings, hpMultiplier,
                    damageMultiplier);

            var placed = Place(rng, spec, picks, placementCells, settings, isBoss, hpMultiplier, damageMultiplier);
            AssignXp(spec, placed, settings, effectiveDifficulty, profile, balanceDepth, xpScale);
            return spec;
        }

        /// <summary>
        /// Cells where enemies may start: free of blocking terrain (and of hazards unless allowed), not the
        /// player's cell and at least the minimum distance away from it, in row-then-column order.
        /// </summary>
        public static List<Vector2Int> GetPlacementCells(BattleSpec spec, BattleGenerationSettings settings)
        {
            var player = new Vector2Int(spec.PlayerX, spec.PlayerY);
            var spawnable = TerrainGenerator.GetSpawnableCells(spec.Width, spec.Height, spec.Terrain,
                settings.AllowHazardCells);
            var result = new List<Vector2Int>(spawnable.Count);
            foreach (var cell in spawnable)
            {
                if (cell == player) continue;
                if (Manhattan(cell, player) < settings.MinDistanceFromPlayer) continue;
                result.Add(cell);
            }

            return result;
        }

        /// <summary>Most enemies a battle can have on a grid with <paramref name="blockedCells"/> obstacles.</summary>
        public static int GetMaxEnemyCount(BattleGenerationSettings settings, int width, int height, int blockedCells,
            int placementCells)
        {
            var byOccupancy = Mathf.FloorToInt(settings.MaxOccupancy * Mathf.Max(0, width * height - blockedCells));
            return Mathf.Max(1, Mathf.Min(Mathf.Min(settings.MaxEnemies, byOccupancy), placementCells));
        }

        // -------------------------------------------------------------------- enemy draw

        private static List<Candidate> PickBoss(BattleGenerationSettings settings, float hpMultiplier,
            float damageMultiplier)
        {
            var result = new List<Candidate>();
            foreach (var config in settings.BossEncounter)
            {
                if (config == null) continue;

                result.Add(new Candidate
                {
                    Config = config,
                    Weight = 1f,
                    Threat = ThreatCalculator.Compute(config, settings.UseScaledThreat ? hpMultiplier : 1f,
                        settings.UseScaledThreat ? damageMultiplier : 1f, settings),
                    Frontline = config.Role != null && config.Role.CountsAsFrontline,
                });
            }

            return result;
        }

        private static List<Candidate> DrawEnemies(Rng rng, float depth, EBattleDifficulty difficulty, BattleSpec spec,
            int placementCells, BattleGenerationSettings settings, float hpMultiplier, float damageMultiplier)
        {
            var band = settings.GetCompositionBand(depth);
            var candidates = GetCandidates(Mathf.RoundToInt(depth), settings, hpMultiplier, damageMultiplier, band);
            if (candidates.Count == 0) return new List<Candidate>();
            var minRoles = band != null ? band.MinDistinctRoles : 1;

            var blocked = CountBlockedCells(spec);
            var maxCount = GetMaxEnemyCount(settings, spec.Width, spec.Height, blocked, placementCells);
            var budget = settings.GetBudget(depth, difficulty);
            var tolerance = settings.BudgetTolerance;

            List<Candidate> best = null;
            var bestDeviation = float.MaxValue;
            var bestMeetsRoles = false;
            for (var attempt = 0; attempt < settings.MaxAttempts; attempt++)
            {
                // Each attempt draws from its own generator so a retry never depends on how far the last one went.
                var attemptRng = new Rng(rng.NextULong());
                var picks = DrawAttempt(attemptRng, candidates, budget, tolerance, maxCount, settings, minRoles);
                var spent = Sum(picks);
                var deviation = budget > 0f ? Mathf.Abs(spent - budget) / budget : 0f;
                var meetsRoles = CountDistinctRoles(picks) >= minRoles;

                // An attempt that reaches the minimum of roles beats one that does not; then the closest to the budget.
                if (best == null || (meetsRoles && !bestMeetsRoles) ||
                    (meetsRoles == bestMeetsRoles && deviation < bestDeviation))
                {
                    best = picks;
                    bestDeviation = deviation;
                    bestMeetsRoles = meetsRoles;
                }

                if (picks.Count >= settings.MinEnemies && deviation <= tolerance && meetsRoles)
                    return picks;
            }

            return best ?? new List<Candidate>();
        }

        private static int CountDistinctRoles(List<Candidate> picks)
        {
            var roles = new HashSet<EnemyRoleDefinition>();
            foreach (var pick in picks)
            {
                if (pick.Config.Role != null)
                    roles.Add(pick.Config.Role);
            }

            return roles.Count;
        }

        private static List<Candidate> DrawAttempt(Rng rng, List<Candidate> candidates, float budget, float tolerance,
            int maxCount, BattleGenerationSettings settings, int minRoles)
        {
            var picks = new List<Candidate>();
            var roleCounts = new Dictionary<EnemyRoleDefinition, int>();
            var spent = 0f;
            var limit = budget * (1f + tolerance);
            var minEnemies = Mathf.Min(settings.MinEnemies, maxCount);

            var needFrontline = false;
            if (settings.RequireFrontline)
            {
                foreach (var candidate in candidates)
                {
                    if (candidate.Frontline)
                    {
                        needFrontline = true;
                        break;
                    }
                }
            }

            var hasFrontline = false;
            var eligible = new List<Candidate>();
            var weights = new List<float>();
            while (picks.Count < maxCount)
            {
                if (spent >= budget && picks.Count >= minEnemies && (hasFrontline || !needFrontline)) break;

                // The frontline requirement is met by the first draw (the rest of the battle is free).
                var mustBeFrontline = needFrontline && picks.Count == 0;

                eligible.Clear();
                foreach (var candidate in candidates)
                {
                    if (mustBeFrontline && !candidate.Frontline) continue;
                    if (IsRoleFull(candidate, roleCounts, settings)) continue;

                    // Past the minimum an enemy is added only while the part of its threat that must fit (all of it,
                    // minus the overshoot allowance) still fits, so the battles average the budget instead of
                    // overshooting it; the tolerance is a hard cap.
                    if (picks.Count >= minEnemies &&
                        (spent + candidate.Threat * (1f - settings.OvershootAllowance) > budget ||
                         spent + candidate.Threat > limit))
                        continue;

                    eligible.Add(candidate);
                }

                // Below the minimum of different roles the draw is restricted to roles the battle does not have yet
                // (when any of them can still be added).
                if (picks.Count > 0 && roleCounts.Count < minRoles && eligible.Count > 0)
                {
                    var fresh = eligible.FindAll(candidate =>
                        candidate.Config.Role != null && !roleCounts.ContainsKey(candidate.Config.Role));
                    if (fresh.Count > 0)
                    {
                        eligible.Clear();
                        eligible.AddRange(fresh);
                    }
                }

                if (eligible.Count == 0)
                {
                    if (picks.Count >= minEnemies) break;

                    // Below the minimum: take the cheapest enemy that respects the rules, even over budget.
                    Candidate cheapest = null;
                    foreach (var candidate in candidates)
                    {
                        if (mustBeFrontline && !candidate.Frontline) continue;
                        if (IsRoleFull(candidate, roleCounts, settings)) continue;
                        if (cheapest == null || candidate.Threat < cheapest.Threat)
                            cheapest = candidate;
                    }

                    if (cheapest == null) break;
                    eligible.Add(cheapest);
                }

                weights.Clear();
                foreach (var candidate in eligible)
                    weights.Add(candidate.Weight);

                var index = rng.WeightedIndex(weights);
                var pick = eligible[index < 0 ? 0 : index];
                picks.Add(pick);
                spent += pick.Threat;
                hasFrontline |= pick.Frontline;
                if (pick.Config.Role != null)
                {
                    roleCounts.TryGetValue(pick.Config.Role, out var count);
                    roleCounts[pick.Config.Role] = count + 1;
                }
            }

            return picks;
        }

        private static List<Candidate> GetCandidates(int depth, BattleGenerationSettings settings, float hpMultiplier,
            float damageMultiplier, [CanBeNull] CompositionBand band)
        {
            var result = new List<Candidate>();
            foreach (var entry in settings.Pool)
            {
                if (entry == null || !entry.IsAvailableAt(depth) || entry.Enemy.IsBoss) continue;

                result.Add(MakeCandidate(entry, settings, hpMultiplier, damageMultiplier, band));
            }

            if (result.Count > 0) return result;

            // No enemy is available at this depth: fall back to every pool enemy so the battle is never empty.
            foreach (var entry in settings.Pool)
            {
                if (entry == null || entry.Enemy == null || entry.Weight <= 0f || entry.Enemy.IsBoss) continue;

                result.Add(MakeCandidate(entry, settings, hpMultiplier, damageMultiplier, band));
            }

            return result;
        }

        private static Candidate MakeCandidate(EnemyPoolEntry entry, BattleGenerationSettings settings,
            float hpMultiplier, float damageMultiplier, [CanBeNull] CompositionBand band)
        {
            return new Candidate
            {
                Config = entry.Enemy,
                Weight = entry.Weight * (band != null ? band.GetRoleWeight(entry.Enemy.Role) : 1f),
                Threat = ThreatCalculator.Compute(entry.Enemy, settings.UseScaledThreat ? hpMultiplier : 1f,
                    settings.UseScaledThreat ? damageMultiplier : 1f, settings),
                Frontline = entry.Enemy.Role != null && entry.Enemy.Role.CountsAsFrontline,
            };
        }

        private static bool IsRoleFull(Candidate candidate, Dictionary<EnemyRoleDefinition, int> roleCounts,
            BattleGenerationSettings settings)
        {
            var role = candidate.Config.Role;
            if (role == null) return false;

            var limit = settings.GetRoleLimit(role);
            if (limit < 0) return false;

            roleCounts.TryGetValue(role, out var count);
            return count >= limit;
        }

        // -------------------------------------------------------------------- placement and XP

        /// <summary>Places the picks on free cells and returns the ones that got a cell, in spec order.</summary>
        private static List<Candidate> Place(Rng rng, BattleSpec spec, List<Candidate> picks,
            List<Vector2Int> placementCells, BattleGenerationSettings settings, bool isBoss, float hpMultiplier,
            float damageMultiplier)
        {
            var player = new Vector2Int(spec.PlayerX, spec.PlayerY);
            var remaining = new List<Vector2Int>(placementCells);
            var placed = new List<Candidate>(picks.Count);

            for (var i = 0; i < picks.Count; i++)
            {
                if (remaining.Count == 0) break;

                var pick = picks[i];
                var preferFar = !pick.Frontline || (isBoss && i == 0);
                var cell = preferFar
                    ? DrawFarCell(rng, remaining, player, settings.FarCellFraction)
                    : remaining[rng.Range(0, remaining.Count)];
                remaining.Remove(cell);

                spec.Enemies.Add(new EnemySpawnSpec
                {
                    EnemyId = pick.Config.Id,
                    X = cell.x,
                    Y = cell.y,
                    HpMultiplier = hpMultiplier,
                    DamageMultiplier = damageMultiplier,
                });
                placed.Add(pick);
            }

            return placed;
        }

        /// <summary>
        /// Sets the XP of every placed enemy and the battle's total, from the settings' XP source (see
        /// <see cref="EBattleXpSource"/>), the difficulty's XP multiplier and <paramref name="xpScale"/>.
        /// </summary>
        private static void AssignXp(BattleSpec spec, List<Candidate> placed, BattleGenerationSettings settings,
            EBattleDifficulty difficulty, BattleDifficultyProfile profile, float depth, float xpScale)
        {
            var factor = profile.XpMultiplier * Mathf.Max(0f, xpScale);
            var xp = new int[placed.Count];
            switch (settings.XpSource)
            {
                case EBattleXpSource.ThreatBudget:
                {
                    var total = settings.GetXpBudget(depth, difficulty) * settings.XpPerThreat *
                                settings.GetXpDepthFactor(depth) * factor;
                    SplitByThreat(Mathf.Max(0, Mathf.RoundToInt(total)), placed, xp);
                    break;
                }
                case EBattleXpSource.EnemyThreat:
                    for (var i = 0; i < placed.Count; i++)
                        xp[i] = Mathf.RoundToInt(placed[i].Threat * settings.XpPerThreat * factor);
                    break;
                default:
                    for (var i = 0; i < placed.Count; i++)
                        xp[i] = Mathf.RoundToInt(placed[i].Config.XpReward * settings.GetXpDepthFactor(depth) * factor);
                    break;
            }

            spec.TotalXp = 0;
            for (var i = 0; i < placed.Count; i++)
            {
                var value = Mathf.Max(0, xp[i]);
                spec.Enemies[i].XpReward = value;
                spec.TotalXp += value;
            }
        }

        /// <summary>
        /// Splits <paramref name="total"/> among the enemies in proportion to their threat (equally if none has
        /// threat), rounding so the parts add up to the total: the remainder goes to the largest fractions, then
        /// to the first enemies.
        /// </summary>
        private static void SplitByThreat(int total, List<Candidate> placed, int[] result)
        {
            if (placed.Count == 0) return;

            var threat = Sum(placed);
            var fractions = new float[placed.Count];
            var given = 0;
            for (var i = 0; i < placed.Count; i++)
            {
                var share = threat > 0f ? total * placed[i].Threat / threat : (float)total / placed.Count;
                result[i] = Mathf.Min(total - given, Mathf.FloorToInt(share));
                fractions[i] = share - result[i];
                given += result[i];
            }

            var order = new List<int>(placed.Count);
            for (var i = 0; i < placed.Count; i++)
                order.Add(i);
            order.Sort((a, b) =>
            {
                var byFraction = fractions[b].CompareTo(fractions[a]);
                return byFraction != 0 ? byFraction : a.CompareTo(b);
            });

            for (var k = 0; given < total; k = (k + 1) % order.Count)
            {
                result[order[k]]++;
                given++;
            }
        }

        /// <summary>A random cell among the <paramref name="fraction"/> of the cells that are farthest from the player.</summary>
        private static Vector2Int DrawFarCell(Rng rng, List<Vector2Int> cells, Vector2Int player, float fraction)
        {
            var ordered = new List<Vector2Int>(cells);
            ordered.Sort((a, b) =>
            {
                var byDistance = Manhattan(b, player).CompareTo(Manhattan(a, player));
                if (byDistance != 0) return byDistance;
                var byY = a.y.CompareTo(b.y);
                return byY != 0 ? byY : a.x.CompareTo(b.x);
            });

            var count = Mathf.Clamp(Mathf.CeilToInt(ordered.Count * fraction), 1, ordered.Count);
            return ordered[rng.Range(0, count)];
        }

        private static int CountBlockedCells(BattleSpec spec)
        {
            var database = GameDatabase.Instance;
            if (database == null) return 0;

            var blocked = 0;
            foreach (var cell in spec.Terrain)
            {
                var definition = database.Get<TerrainDefinition>(cell.TerrainId);
                if (definition != null && definition.BlocksMovement)
                    blocked++;
            }

            return blocked;
        }

        private static float Sum(List<Candidate> picks)
        {
            var total = 0f;
            foreach (var pick in picks)
                total += pick.Threat;
            return total;
        }

        private static int Manhattan(Vector2Int a, Vector2Int b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
    }
}
