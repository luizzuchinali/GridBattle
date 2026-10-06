using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Simulation;
using JetBrains.Annotations;

namespace GridBattle.Gameplay.Meta
{
    /// <summary>
    /// Contextual tutorial tips (interface 4.4). Gameplay modules call <see cref="Notify"/> when a
    /// situation happens; if tips are enabled and the matching tip was never seen, a
    /// <see cref="TutorialTipRequestedEvent"/> is raised. The UI shows the tip and calls
    /// <see cref="MarkSeen"/> so it is not shown again (the profile remembers it between runs).
    /// A tip is requested at most once per session until it is marked as seen.
    /// </summary>
    public static class TutorialService
    {
        private static readonly HashSet<string> RequestedThisSession = new();

        /// <summary>
        /// Requests the tip for <paramref name="trigger"/>. Returns whether a request was raised
        /// (false: tips off, tip already seen or requested, or no tip asset for the trigger).
        /// </summary>
        public static bool Notify(ETutorialTrigger trigger)
        {
            if (SimMode.IsActive) return false;

            if (!ProfileService.TipsEnabled) return false;

            var tip = GetTip(trigger);
            if (tip == null || string.IsNullOrEmpty(tip.Id)) return false;
            if (ProfileService.HasSeenTip(tip.Id)) return false;
            if (!RequestedThisSession.Add(tip.Id)) return false;

            EventBus.Raise(new TutorialTipRequestedEvent(tip));
            return true;
        }

        /// <summary>The tip asset of a trigger, or null if there is none.</summary>
        [CanBeNull]
        public static TutorialTipDefinition GetTip(ETutorialTrigger trigger)
        {
            var database = GameDatabase.Instance;
            if (database == null) return null;

            foreach (var tip in database.GetAll<TutorialTipDefinition>())
            {
                if (tip.Trigger == trigger)
                    return tip;
            }

            return null;
        }

        /// <summary>The tip was shown: it will not be requested again.</summary>
        public static void MarkSeen(TutorialTipDefinition tip)
        {
            if (tip == null) return;

            ProfileService.MarkTipSeen(tip.Id);
        }

        public static bool HasSeen(TutorialTipDefinition tip) => tip != null && ProfileService.HasSeenTip(tip.Id);

        /// <summary>Forgets which tips were requested in this session (a tip not marked as seen can be requested again).</summary>
        public static void ResetSession() => RequestedThisSession.Clear();
    }
}
