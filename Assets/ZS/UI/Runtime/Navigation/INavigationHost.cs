using System;
using System.Threading;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// What navigators need from their host (the <see cref="UIRoot"/> at runtime,
    /// a fake in tests): creating and discarding screen/modal instances.
    /// </summary>
    internal interface INavigationHost
    {
        Navigator Navigator { get; }

        /// <summary>The UIRoot, or null when hosted outside a scene (tests).</summary>
        UIRoot Root { get; }

        IServiceProvider Services { get; }

        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Instance for a screen/modal definition: the existing one for
        /// LayerSource views, a new mounted one otherwise.
        /// </summary>
        ViewInstance Acquire(ViewDefinition definition);

        /// <summary>
        /// The instance is no longer used by a navigator: destroyed unless it is a
        /// LayerSource/static view (which live as long as the host).
        /// </summary>
        void Release(ViewInstance instance);
    }
}
