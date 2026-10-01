using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ZS.UI.Navigation.Transitions;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// Top-level navigation of a <see cref="UIRoot"/>: the screen stack
    /// (push/replace/pop) plus modals stacked above it, and the back action.
    /// Modals are independent from screen transitions.
    /// </summary>
    public sealed class Navigator : ViewStackNavigator
    {
        private readonly List<ModalEntry> _modals = new();

        internal Navigator(INavigationHost host, ConcurrentNavigationPolicy policy) : base(host, policy)
        {
        }

        /// <summary>Open modals, from bottom to top.</summary>
        public IReadOnlyList<ViewController> Modals
        {
            get
            {
                var controllers = new List<ViewController>(_modals.Count);
                foreach (var entry in _modals)
                    controllers.Add(entry.Instance.Controller);
                return controllers;
            }
        }

        public bool HasModal => _modals.Count > 0;

        /// <summary>
        /// Opens <paramref name="definition"/> as a modal. Completes when it closes.
        /// Opening a LayerSource modal that is already open does nothing.
        /// </summary>
        public Awaitable ShowModal(ViewDefinition definition, object args = null)
        {
            var source = new AwaitableCompletionSource();
            if (!OpenModal(definition, args, _ => source.SetResult()))
                source.SetResult();
            return source.Awaitable;
        }

        /// <summary>
        /// Opens a modal driven by a <see cref="ModalController{TResult}"/> and
        /// returns its result (<c>default</c> when closed without one).
        /// </summary>
        public Awaitable<TResult> ShowModal<TResult>(ViewDefinition definition, object args = null)
        {
            var source = new AwaitableCompletionSource<TResult>();
            if (!OpenModal(definition, args, result => source.SetResult(result is TResult typed ? typed : default)))
                source.SetResult(default);
            return source.Awaitable;
        }

        public void CloseModal(ViewController modal)
        {
            CloseModal(modal, null);
        }

        public void CloseModal<TResult>(ViewController modal, TResult result)
        {
            CloseModal(modal, (object)result);
        }

        /// <summary>
        /// Back action (Esc/Android back): top modal, then pages of the current
        /// screen, then the screen itself, then a pop. Returns false when nothing
        /// handled it (already at the root screen).
        /// </summary>
        public bool HandleBack()
        {
            if (_modals.Count > 0)
            {
                var top = _modals[^1];
                if (!top.Closing && !top.Instance.Controller.OnBack() && top.Instance.Definition.CloseOnBack)
                    CloseModal(top.Instance.Controller);
                return true;
            }

            if (IsTransitioning) return true;

            var current = CurrentInstance;
            if (current == null) return false;
            if (current.Context.TryHandlePageBack()) return true;
            if (current.Controller.OnBack()) return true;

            if (Depth > 1)
            {
                Pop();
                return true;
            }

            return false;
        }

        internal override ViewInstance AcquireInstance(ViewDefinition definition)
        {
            return Host.Acquire(definition);
        }

        internal override void ReleaseInstance(ViewInstance instance)
        {
            instance.OwnerStack = null;
            Host.Release(instance);
        }

        internal void CloseAllModals()
        {
            for (var i = _modals.Count - 1; i >= 0; i--)
                Host.Release(_modals[i].Instance);
            _modals.Clear();
        }

        private bool OpenModal(ViewDefinition definition, object args, Action<object> onClosed)
        {
            if (definition.MountMode == ViewMountMode.LayerSource &&
                _modals.Exists(entry => entry.Instance.Definition == definition))
                return false;

            var instance = Host.Acquire(definition);
            if (instance == null) return false;

            var entry = new ModalEntry(instance, onClosed);
            _modals.Add(entry);
            instance.IsModal = true;

            instance.Controller.OnEnter(args);
            if (definition.UseBackdrop)
                instance.AttachBackdrop(evt => OnBackdropPointerUp(entry, evt));
            instance.BringToFront();

            _ = RunModalTransition(entry, true);
            return true;
        }

        private void CloseModal(ViewController modal, object result)
        {
            var entry = _modals.Find(candidate => candidate.Instance.Controller == modal);
            if (entry == null || entry.Closing) return;

            entry.Closing = true;
            entry.Result = result;
            modal.OnExit();
            _ = RunModalTransition(entry, false);
        }

        private void OnBackdropPointerUp(ModalEntry entry, PointerUpEvent evt)
        {
            if (evt.target == entry.Instance.Backdrop && entry.Instance.Definition.CloseOnBackdropClick)
                CloseModal(entry.Instance.Controller);
        }

        private async Awaitable RunModalTransition(ModalEntry entry, bool opening)
        {
            var scope = opening
                ? new TransitionScope(Host, null, entry.Instance)
                : new TransitionScope(Host, entry.Instance, null);

            try
            {
                var transition = entry.Instance.Definition.Transition;
                if (transition != null)
                    await transition.Run(scope);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                scope.Complete();
            }

            if (opening)
            {
                entry.Instance.Controller.OnEntered();
                return;
            }

            _modals.Remove(entry);
            entry.Instance.DetachBackdrop();
            entry.Instance.IsModal = false;
            entry.Instance.Controller.OnExited();
            Host.Release(entry.Instance);
            entry.OnClosed(entry.Result);
        }

        private sealed class ModalEntry
        {
            public ModalEntry(ViewInstance instance, Action<object> onClosed)
            {
                Instance = instance;
                OnClosed = onClosed;
            }

            public ViewInstance Instance { get; }
            public Action<object> OnClosed { get; }
            public bool Closing { get; set; }
            public object Result { get; set; }
        }
    }
}
