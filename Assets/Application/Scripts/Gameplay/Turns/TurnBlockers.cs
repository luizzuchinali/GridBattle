using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace GridBattle.Gameplay.Turns
{
    /// <summary>
    /// Things that must finish before the turn flow continues: XP orbs that will
    /// cause a level up, the talent choice (the game is paused while it is open),
    /// the pause menu... The TurnManager and BattleController wait for
    /// <see cref="WaitAsync"/> at each step of the turn. Acquire a blocker and
    /// dispose the returned handle to release it.
    /// </summary>
    public static class TurnBlockers
    {
        private static readonly List<Handle> Active = new();

        public static bool IsBlocked => Active.Count > 0;

        /// <summary>Raised when the first blocker is acquired or the last one released.</summary>
        public static event Action<bool> BlockedChanged;

        /// <param name="reason">Debug label (shown in logs and the inspector tools).</param>
        public static IDisposable Acquire(string reason)
        {
            var handle = new Handle(reason);
            Active.Add(handle);
            if (Active.Count == 1)
                BlockedChanged?.Invoke(true);
            return handle;
        }

        public static async Awaitable WaitAsync(CancellationToken cancellationToken)
        {
            while (IsBlocked)
                await Awaitable.NextFrameAsync(cancellationToken);
        }

        /// <summary>Releases every blocker (a new run or battle starts from a clean state).</summary>
        public static void Clear()
        {
            if (Active.Count == 0) return;

            foreach (var handle in Active)
                handle.Released = true;
            Active.Clear();
            BlockedChanged?.Invoke(false);
        }

        public static IEnumerable<string> Reasons
        {
            get
            {
                foreach (var handle in Active)
                    yield return handle.Reason;
            }
        }

        private static void Release(Handle handle)
        {
            if (!Active.Remove(handle)) return;
            if (Active.Count == 0)
                BlockedChanged?.Invoke(false);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active.Clear();
            BlockedChanged = null;
        }

        private sealed class Handle : IDisposable
        {
            public Handle(string reason)
            {
                Reason = reason;
            }

            public string Reason { get; }
            public bool Released { get; set; }

            public void Dispose()
            {
                if (Released) return;
                Released = true;
                Release(this);
            }
        }
    }
}
