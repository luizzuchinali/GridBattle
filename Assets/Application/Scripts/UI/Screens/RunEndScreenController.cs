using GridBattle.Gameplay.Run;
using UnityEngine.Scripting;

namespace GridBattle.UI.Screens
{
    /// <summary>End of a run that was lost or given up (interface 4.3).</summary>
    [Preserve]
    public sealed class RunEndScreenController : RunSummaryScreenController
    {
        protected override string GetTitleKey(RunSummary summary) =>
            summary.Reason == ERunEndReason.GivenUp ? "run_end.given_up" : "run_end.defeat";

        protected override string GetSubtitleKey(RunSummary summary) =>
            summary.Reason == ERunEndReason.GivenUp ? "run_end.given_up.sub" : "run_end.defeat.sub";
    }
}
