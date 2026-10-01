using ZS.UI.Navigation.Transitions;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// Per-request navigation options.
    /// </summary>
    public readonly struct NavigationOptions
    {
        public NavigationOptions(ViewTransition transition, bool immediate = false)
        {
            Transition = transition;
            Immediate = immediate;
        }

        /// <summary>Overrides the definition's transition.</summary>
        public ViewTransition Transition { get; }

        /// <summary>Skips any transition.</summary>
        public bool Immediate { get; }

        /// <summary>Navigates without a transition.</summary>
        public static NavigationOptions Instant => new(null, true);

        internal ViewTransition Resolve(ViewDefinition definition)
        {
            if (Immediate) return null;
            return Transition != null ? Transition : definition?.Transition;
        }
    }
}
