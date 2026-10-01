using System;
using System.Collections.Generic;
using UnityEngine;
using ZS.UI.Navigation.Transitions;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// History stack of views where only the top one is shown: screens
    /// (<see cref="Navigator"/>) and pages (<see cref="PageNavigator"/>).
    /// Operations return an <see cref="Awaitable"/> that completes when the
    /// transition finishes (immediately when there is none).
    /// </summary>
    public abstract class ViewStackNavigator
    {
        private readonly List<ViewInstance> _stack = new();
        private readonly Queue<Action> _pending = new();

        internal ViewStackNavigator(INavigationHost host, ConcurrentNavigationPolicy policy)
        {
            Host = host;
            Policy = policy;
        }

        internal INavigationHost Host { get; }

        /// <summary>What happens to requests received during a transition.</summary>
        public ConcurrentNavigationPolicy Policy { get; set; }

        public bool IsTransitioning { get; private set; }

        public int Depth => _stack.Count;

        /// <summary>View on top of the stack (the visible one), or null.</summary>
        public ViewController Current => CurrentInstance?.Controller;

        /// <summary>Views from bottom to top.</summary>
        public IReadOnlyList<ViewController> Stack
        {
            get
            {
                var controllers = new List<ViewController>(_stack.Count);
                foreach (var instance in _stack)
                    controllers.Add(instance.Controller);
                return controllers;
            }
        }

        /// <summary>Raised after each completed change: (previous, current).</summary>
        public event Action<ViewController, ViewController> Changed;

        internal ViewInstance CurrentInstance => _stack.Count > 0 ? _stack[^1] : null;
        internal IReadOnlyList<ViewInstance> Instances => _stack;

        /// <summary>Shows <paramref name="definition"/> on top, keeping the current view in history.</summary>
        public Awaitable Push(ViewDefinition definition, object args = null, NavigationOptions options = default)
        {
            return Schedule(() => DoPush(definition, args, options));
        }

        /// <summary>Replaces the current view with <paramref name="definition"/>.</summary>
        public Awaitable Replace(ViewDefinition definition, object args = null, NavigationOptions options = default)
        {
            return Schedule(() => DoReplace(definition, args, options));
        }

        /// <summary>Removes the current view and returns to the previous one.</summary>
        public Awaitable Pop(NavigationOptions options = default)
        {
            return Schedule(() => DoPop(options));
        }

        /// <summary>Pops until <paramref name="definition"/> is the current view.</summary>
        public Awaitable PopTo(ViewDefinition definition, NavigationOptions options = default)
        {
            return Schedule(() => DoPopTo(definition, options));
        }

        internal abstract ViewInstance AcquireInstance(ViewDefinition definition);

        internal abstract void ReleaseInstance(ViewInstance instance);

        /// <summary>Destroys every view in the stack without transitions.</summary>
        internal void ClearInstances()
        {
            for (var i = _stack.Count - 1; i >= 0; i--)
                ReleaseInstance(_stack[i]);
            _stack.Clear();
        }

        private Awaitable DoPush(ViewDefinition definition, object args, NavigationOptions options)
        {
            var from = CurrentInstance;
            var to = AcquireInstance(definition);
            if (to == null) return AwaitableUtility.Completed();

            _stack.Remove(to);
            _stack.Add(to);
            to.OwnerStack = this;
            return RunTransition(from, to, args, options.Resolve(definition), false);
        }

        private Awaitable DoReplace(ViewDefinition definition, object args, NavigationOptions options)
        {
            var from = CurrentInstance;
            var to = AcquireInstance(definition);
            if (to == null) return AwaitableUtility.Completed();

            if (from != null)
                _stack.RemoveAt(_stack.Count - 1);
            _stack.Remove(to);
            _stack.Add(to);
            to.OwnerStack = this;

            var releaseFrom = from != null && from != to && !_stack.Contains(from);
            return RunTransition(from, to, args, options.Resolve(definition), releaseFrom);
        }

        private Awaitable DoPop(NavigationOptions options)
        {
            var from = CurrentInstance;
            if (from == null) return AwaitableUtility.Completed();

            _stack.RemoveAt(_stack.Count - 1);
            return RunTransition(from, CurrentInstance, null, options.Resolve(from.Definition), true);
        }

        private Awaitable DoPopTo(ViewDefinition definition, NavigationOptions options)
        {
            var targetIndex = _stack.FindLastIndex(instance => instance.Definition == definition);
            if (targetIndex < 0 || targetIndex == _stack.Count - 1) return AwaitableUtility.Completed();

            var from = CurrentInstance;
            for (var i = _stack.Count - 2; i > targetIndex; i--)
            {
                ReleaseInstance(_stack[i]);
                _stack.RemoveAt(i);
            }

            _stack.RemoveAt(_stack.Count - 1);
            return RunTransition(from, CurrentInstance, null, options.Resolve(from.Definition), true);
        }

        private Awaitable Schedule(Func<Awaitable> operation)
        {
            if (!IsTransitioning)
                return operation();

            if (Policy == ConcurrentNavigationPolicy.Ignore)
                return AwaitableUtility.Completed();

            var source = new AwaitableCompletionSource();
            _pending.Enqueue(() => CompleteWhenDone(operation, source));
            return source.Awaitable;
        }

        private static async void CompleteWhenDone(Func<Awaitable> operation, AwaitableCompletionSource source)
        {
            try
            {
                await operation();
            }
            finally
            {
                source.SetResult();
            }
        }

        private async Awaitable RunTransition(ViewInstance from, ViewInstance to, object args,
            ViewTransition transition, bool releaseFrom)
        {
            IsTransitioning = true;
            var changed = from != to;

            to?.Controller.OnEnter(args);
            if (changed)
                from?.Controller.OnExit();

            var scope = new TransitionScope(Host, from, to);
            try
            {
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
                IsTransitioning = false;
            }

            if (changed)
                from?.Controller.OnExited();
            to?.Controller.OnEntered();

            if (releaseFrom && changed && from != null)
                ReleaseInstance(from);

            Changed?.Invoke(from?.Controller, to?.Controller);

            if (_pending.Count > 0 && !IsTransitioning)
                _pending.Dequeue()();
        }
    }
}
