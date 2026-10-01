using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.UIElements;

namespace ZS.UI.Navigation.Transitions
{
    /// <summary>
    /// One view change handed to a <see cref="ViewTransition"/>: the outgoing
    /// view (<see cref="From"/>), the incoming one (<see cref="To"/>) and the
    /// operations that show/hide them. Either side can be null (first screen,
    /// opening or closing a modal).
    /// </summary>
    public sealed class TransitionScope
    {
        private readonly ViewInstance _from;
        private readonly ViewInstance _to;
        private bool _fromHidden;
        private bool _toShown;

        internal TransitionScope(INavigationHost host, ViewInstance from, ViewInstance to)
        {
            Root = host.Root;
            CancellationToken = host.CancellationToken;
            _from = from;
            _to = to;
        }

        /// <summary>UIRoot running the transition (null outside a scene).</summary>
        public UIRoot Root { get; }

        /// <summary>Canceled when the UIRoot is destroyed.</summary>
        public CancellationToken CancellationToken { get; }

        public ViewController From => _from?.Controller;
        public ViewController To => _to?.Controller;

        public IReadOnlyList<VisualElement> FromElements => _from != null ? _from.Elements : Array.Empty<VisualElement>();
        public IReadOnlyList<VisualElement> ToElements => _to != null ? _to.Elements : Array.Empty<VisualElement>();

        public void ShowTo()
        {
            if (_toShown) return;
            _toShown = true;
            _to?.SetVisible(true);
        }

        public void HideFrom()
        {
            if (_fromHidden) return;
            _fromHidden = true;
            if (_from != null && _from != _to)
                _from.SetVisible(false);
        }

        /// <summary>Hides the outgoing view and shows the incoming one.</summary>
        public void Swap()
        {
            HideFrom();
            ShowTo();
        }

        internal void Complete()
        {
            Swap();
        }
    }
}
