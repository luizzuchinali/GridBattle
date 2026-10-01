using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// Base class of the C# logic behind a view (screen, modal or page). Plain C#
    /// (not a MonoBehaviour): created by the navigation system from the
    /// <see cref="ViewDefinition"/>, so it needs a parameterless constructor.
    /// Mark subclasses with [Preserve] when building with code stripping.
    /// <para>
    /// Lifecycle: <see cref="OnCreate"/> → <see cref="OnBind"/> (again after every
    /// UI reload) → <see cref="OnEnter"/> → <see cref="OnEntered"/> →
    /// <see cref="OnExit"/> → <see cref="OnExited"/> → <see cref="OnDestroy"/>.
    /// </para>
    /// </summary>
    [RequireDerived]
    public abstract class ViewController
    {
        internal ViewInstance Instance { get; private set; }

        public ViewDefinition Definition => Instance.Definition;
        public NavigationContext Context => Instance.Context;

        /// <summary>
        /// Root element of the view (first top-level element of its UXML). Null
        /// until the view is bound to a panel.
        /// </summary>
        public VisualElement Root { get; internal set; }

        public bool IsVisible => Instance.IsVisible;

        internal void Attach(ViewInstance instance)
        {
            Instance = instance;
        }

        /// <summary>
        /// Called once, right after the controller is created (no UI yet). Good for
        /// subscribing to external events.
        /// </summary>
        protected internal virtual void OnCreate()
        {
        }

        /// <summary>
        /// Called when the view's elements are available, and again after every UI
        /// reload (new elements). Query elements and register UI callbacks here.
        /// </summary>
        protected internal virtual void OnBind(VisualElement root)
        {
        }

        /// <summary>
        /// The view is about to become the current one (before its transition).
        /// <paramref name="args"/> is the navigation argument (null when returning
        /// to the view through a pop).
        /// </summary>
        protected internal virtual void OnEnter(object args)
        {
        }

        /// <summary>The view is visible and its transition finished.</summary>
        protected internal virtual void OnEntered()
        {
        }

        /// <summary>The view is about to stop being the current one.</summary>
        protected internal virtual void OnExit()
        {
        }

        /// <summary>The view is hidden and its transition finished.</summary>
        protected internal virtual void OnExited()
        {
        }

        /// <summary>
        /// Back action while this view is the current one. Return true to consume
        /// it (otherwise the navigator applies its default: close/pop).
        /// </summary>
        protected internal virtual bool OnBack()
        {
            return false;
        }

        /// <summary>
        /// Called once when the view is discarded. Undo what <see cref="OnCreate"/>
        /// did.
        /// </summary>
        protected internal virtual void OnDestroy()
        {
        }

        /// <summary>
        /// Closes this view: closes it if it is a modal, or pops it if it is the top
        /// of its stack (screen or page).
        /// </summary>
        protected void Close()
        {
            Context.CloseView();
        }
    }

    /// <summary>
    /// Controller used when a view definition has no controller type.
    /// </summary>
    internal sealed class EmptyViewController : ViewController
    {
    }
}
