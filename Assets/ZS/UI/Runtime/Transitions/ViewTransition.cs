using UnityEngine;

namespace ZS.UI.Navigation.Transitions
{
    /// <summary>
    /// Animates a view change. Implementations decide when the swap happens
    /// (<see cref="TransitionScope.ShowTo"/>, <see cref="TransitionScope.HideFrom"/>
    /// or <see cref="TransitionScope.Swap"/>); whatever is not done by the end of
    /// <see cref="Run"/> is completed by the navigator. Assets are shared, so keep
    /// per-run state in local variables.
    /// </summary>
    public abstract class ViewTransition : ScriptableObject
    {
        public abstract Awaitable Run(TransitionScope scope);
    }
}
