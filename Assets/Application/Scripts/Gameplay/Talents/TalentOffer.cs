using System.Collections.Generic;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Run;

namespace GridBattle.Gameplay.Talents
{
    /// <summary>How a talent offer ended.</summary>
    public enum ETalentOfferResult
    {
        /// <summary>The player took one of the options.</summary>
        Chosen,

        /// <summary>The player skipped the offer: that level's talent is lost.</summary>
        Skipped,

        /// <summary>No talent could be offered (the pool is exhausted), so the offer closed by itself.</summary>
        NoOptions
    }

    /// <summary>What changed in an open offer (<see cref="Events.TalentOfferChangedEvent"/>).</summary>
    public enum ETalentOfferChange
    {
        /// <summary>The offer was rerolled: the options are new.</summary>
        Rerolled,

        /// <summary>A talent was banned: it left the run's pool and a new draw replaced it.</summary>
        Banned,

        /// <summary>Another offer was queued behind the open one (a kill that gave several levels): only <c>QueuedAfter</c> changed.</summary>
        Queued
    }

    /// <summary>One talent of an offer, with what the choice screen needs to describe it.</summary>
    public sealed class TalentOption
    {
        public TalentOption(TalentDefinition talent, int currentRank, IReadOnlyList<SynergyTagDefinition> sharedTags)
        {
            Talent = talent;
            CurrentRank = currentRank;
            SharedTags = sharedTags;
        }

        public TalentDefinition Talent { get; }

        /// <summary>Rank the player has now (0 = not taken yet).</summary>
        public int CurrentRank { get; }

        /// <summary>Rank the player would have after taking it.</summary>
        public int NextRank => CurrentRank + 1;

        public int MaxRank => Talent.MaxRank;

        /// <summary>The player already has the talent: taking it again raises its rank.</summary>
        public bool IsUpgrade => CurrentRank > 0;

        /// <summary>Tags the talent shares with the build (empty = no synergy).</summary>
        public IReadOnlyList<SynergyTagDefinition> SharedTags { get; }

        /// <summary>"Combines with the build": the talent shares at least one tag with the talents already taken.</summary>
        public bool MatchesBuild => SharedTags.Count > 0;

        /// <summary>The talent unlocks a skill.</summary>
        public bool UnlocksSkill => Talent.IsSkillTalent;

        /// <summary>The skill the talent unlocks (null when it is not a skill talent).</summary>
        public SkillDefinition Skill => Talent.UnlockedSkill;
    }

    /// <summary>
    /// A talent offer as the choice screen sees it (a snapshot: a new object is built after each reroll or
    /// ban, carried by <see cref="Events.TalentOfferChangedEvent"/>; <c>TalentService.CurrentOffer</c> always
    /// holds the latest). The saved form is <see cref="TalentOfferState"/>.
    /// </summary>
    public sealed class TalentOffer
    {
        public TalentOffer(ETalentOfferSource source, int level, int rerollIndex,
            IReadOnlyList<TalentOption> options, int rerollsLeft, int bansLeft, int skipsLeft, int queuedAfter)
        {
            Source = source;
            Level = level;
            RerollIndex = rerollIndex;
            Options = options;
            RerollsLeft = rerollsLeft;
            BansLeft = bansLeft;
            SkipsLeft = skipsLeft;
            QueuedAfter = queuedAfter;
        }

        /// <summary>A level up or a talent node.</summary>
        public ETalentOfferSource Source { get; }

        /// <summary>Level the offer is for (the level reached; the player's level at a talent node).</summary>
        public int Level { get; }

        /// <summary>Times this offer was rerolled.</summary>
        public int RerollIndex { get; }

        /// <summary>The talents offered, in screen order.</summary>
        public IReadOnlyList<TalentOption> Options { get; }

        /// <summary>Rerolls the run still has (shared by every offer).</summary>
        public int RerollsLeft { get; }

        /// <summary>Bans the run still has.</summary>
        public int BansLeft { get; }

        /// <summary>Skips the run still has.</summary>
        public int SkipsLeft { get; }

        /// <summary>More offers wait behind this one (a kill that gave several levels).</summary>
        public int QueuedAfter { get; }

        public bool CanReroll => RerollsLeft > 0;
        public bool CanBan => BansLeft > 0 && Options.Count > 0;
        public bool CanSkip => SkipsLeft > 0;
    }
}
