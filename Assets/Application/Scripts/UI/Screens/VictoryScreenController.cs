using GridBattle.Gameplay.Run;
using UnityEngine.Scripting;

namespace GridBattle.UI.Screens
{
    /// <summary>Victory: the final boss was defeated (interface 4.3). Same summary as the end of a lost run.</summary>
    [Preserve]
    public sealed class VictoryScreenController : RunSummaryScreenController
    {
        protected override string GetTitleKey(RunSummary summary) => "victory.title";

        protected override string GetSubtitleKey(RunSummary summary) => "victory.sub";
    }
}
