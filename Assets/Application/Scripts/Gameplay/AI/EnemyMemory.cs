using System.Collections.Generic;
using GridBattle.Gameplay.Run;
using JetBrains.Annotations;

namespace GridBattle.Gameplay.AI
{
    /// <summary>
    /// Per-enemy AI memory: action cooldowns and free counters, keyed by strings
    /// (usually the action's id plus an optional suffix, see <see cref="Key"/>).
    /// Owned by the enemy's controller, so the shared <see cref="EnemyAction"/>
    /// assets stay stateless. Cooldowns count down in turns of this enemy: the
    /// controller calls <see cref="Tick"/> at the end of each of the enemy's own
    /// turns, skipping the cooldowns started during that turn, so a cooldown of N
    /// means N turns of the enemy between uses (the same rule as skill cooldowns,
    /// GDD: "número de ações entre usos"). Can be captured and restored for saves.
    /// </summary>
    public sealed class EnemyMemory
    {
        private const string CooldownPrefix = "cd:";
        private const string CounterPrefix = "n:";

        private readonly Dictionary<string, int> _cooldowns = new();
        private readonly Dictionary<string, int> _counters = new();
        private readonly HashSet<string> _startedThisTurn = new();

        /// <summary>
        /// Memory key of an action: its stable id (the asset name while the id is
        /// not generated yet) plus an optional <paramref name="suffix"/> for actions
        /// that keep several independent values.
        /// </summary>
        public static string Key(EnemyAction action, [CanBeNull] string suffix = null)
        {
            var id = string.IsNullOrEmpty(action.Id) ? action.name : action.Id;
            return string.IsNullOrEmpty(suffix) ? id : $"{id}:{suffix}";
        }

        /// <summary>Whether the cooldown of <paramref name="key"/> is over (or was never started).</summary>
        public bool IsReady(string key) => !_cooldowns.TryGetValue(key, out var remaining) || remaining <= 0;

        /// <summary>Turns of this enemy left before <paramref name="key"/> is ready again.</summary>
        public int Remaining(string key) => _cooldowns.TryGetValue(key, out var remaining) ? remaining : 0;

        /// <summary>
        /// Starts a cooldown of <paramref name="turns"/> turns of this enemy between
        /// uses: used on turn N, the action is usable again on turn
        /// N + <paramref name="turns"/> + 1 (0 = no cooldown, usable every turn).
        /// </summary>
        public void StartCooldown(string key, int turns)
        {
            if (turns <= 0)
            {
                _cooldowns.Remove(key);
                return;
            }

            _cooldowns[key] = turns;
            _startedThisTurn.Add(key);
        }

        /// <summary>Free counter (does not tick down on its own).</summary>
        public int Get(string key) => _counters.TryGetValue(key, out var value) ? value : 0;

        public void Set(string key, int value)
        {
            if (value == 0)
                _counters.Remove(key);
            else
                _counters[key] = value;
        }

        /// <summary>
        /// End of this enemy's own turn: every running cooldown goes down by one,
        /// except those started during this turn.
        /// </summary>
        public void Tick()
        {
            if (_cooldowns.Count == 0)
            {
                _startedThisTurn.Clear();
                return;
            }

            var keys = new List<string>(_cooldowns.Keys);
            foreach (var key in keys)
            {
                if (_startedThisTurn.Contains(key)) continue;

                var remaining = _cooldowns[key] - 1;
                if (remaining <= 0)
                    _cooldowns.Remove(key);
                else
                    _cooldowns[key] = remaining;
            }

            _startedThisTurn.Clear();
        }

        public void Clear()
        {
            _cooldowns.Clear();
            _counters.Clear();
            _startedThisTurn.Clear();
        }

        /// <summary>Snapshot for saves (cooldowns and counters, ordered by key).</summary>
        public List<CounterState> Capture()
        {
            var result = new List<CounterState>();
            AddSorted(result, _cooldowns, CooldownPrefix);
            AddSorted(result, _counters, CounterPrefix);
            return result;
        }

        /// <summary>Replaces the memory with a snapshot made by <see cref="Capture"/>.</summary>
        public void Restore([CanBeNull] IEnumerable<CounterState> state)
        {
            Clear();
            if (state == null) return;

            foreach (var entry in state)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Key)) continue;

                if (entry.Key.StartsWith(CooldownPrefix))
                    _cooldowns[entry.Key.Substring(CooldownPrefix.Length)] = entry.Value;
                else if (entry.Key.StartsWith(CounterPrefix))
                    _counters[entry.Key.Substring(CounterPrefix.Length)] = entry.Value;
            }
        }

        private static void AddSorted(List<CounterState> into, Dictionary<string, int> source, string prefix)
        {
            var keys = new List<string>(source.Keys);
            keys.Sort(string.CompareOrdinal);
            foreach (var key in keys)
                into.Add(new CounterState { Key = prefix + key, Value = source[key] });
        }
    }
}
