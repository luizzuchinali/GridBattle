namespace GridBattle.Gameplay.States
{
    /// <summary>
    /// A state active on a character. <see cref="Remaining"/> counts turns of the
    /// holder (GDD 3.1): it goes down at the end of each of the holder's turns and
    /// the state ends at zero. Negative = permanent.
    /// </summary>
    public sealed class StateInstance
    {
        public const int Permanent = -1;

        public StateInstance(StateDefinition definition, int remaining, int stacks, string sourceId)
        {
            Definition = definition;
            Remaining = remaining;
            Stacks = stacks;
            SourceId = sourceId;
        }

        public StateDefinition Definition { get; }

        /// <summary>Remaining turns of the holder; <see cref="Permanent"/> if it never expires.</summary>
        public int Remaining { get; internal set; }

        public int Stacks { get; internal set; }

        /// <summary>
        /// Who granted it when instances must stay separate (e.g. a talent id);
        /// null for states that stack with each other by definition.
        /// </summary>
        public string SourceId { get; }

        /// <summary>Remaining shield points (shield effects only).</summary>
        public int Shield { get; set; }

        public bool IsPermanent => Remaining < 0;

        internal bool Removed { get; set; }
    }
}
