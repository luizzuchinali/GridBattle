using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using ZS.UI.Navigation;
using ZS.UI.Navigation.Transitions;

namespace ZS.UI.Tests
{
    /// <summary>
    /// Navigation host without a scene: every view is instantiated into one
    /// container element.
    /// </summary>
    internal sealed class FakeHost : INavigationHost
    {
        public FakeHost(ConcurrentNavigationPolicy policy = ConcurrentNavigationPolicy.Ignore)
        {
            Navigator = new Navigator(this, policy);
        }

        public VisualElement Container { get; } = new();
        public List<ViewInstance> Live { get; } = new();

        public Navigator Navigator { get; }
        public UIRoot Root => null;
        public IServiceProvider Services { get; set; }
        public CancellationToken CancellationToken => CancellationToken.None;

        public ViewInstance Acquire(ViewDefinition definition)
        {
            var instance = ViewInstance.Create(definition, this, null);
            instance.Instantiate(Container);
            Live.Add(instance);
            return instance;
        }

        public void Release(ViewInstance instance)
        {
            instance.Destroy();
            Live.Remove(instance);
        }
    }

    /// <summary>Records every lifecycle call in <see cref="Log"/>.</summary>
    internal class LogController : ViewController
    {
        public static readonly List<string> Log = new();

        private string Id => Definition.name;

        protected internal override void OnCreate() => Log.Add($"{Id}.Create");
        protected internal override void OnBind(VisualElement root) => Log.Add($"{Id}.Bind");
        protected internal override void OnEnter(object args) => Log.Add($"{Id}.Enter({args})");
        protected internal override void OnEntered() => Log.Add($"{Id}.Entered");
        protected internal override void OnExit() => Log.Add($"{Id}.Exit");
        protected internal override void OnExited() => Log.Add($"{Id}.Exited");
        protected internal override void OnDestroy() => Log.Add($"{Id}.Destroy");
    }

    /// <summary>Screen whose root holds a PageHost named "pages".</summary>
    internal sealed class PagesController : LogController
    {
        protected internal override void OnBind(VisualElement root)
        {
            base.OnBind(root);
            root.Add(new PageHost { name = "pages" });
        }
    }

    internal sealed class ConfirmController : ModalController<bool>
    {
        public void Answer(bool value) => Close(value);
    }

    internal sealed class BackConsumingController : LogController
    {
        public int BackCount;

        protected internal override bool OnBack()
        {
            BackCount++;
            return true;
        }
    }

    /// <summary>Transition that only finishes when <see cref="Finish"/> is called.</summary>
    internal sealed class ManualTransition : ViewTransition
    {
        private AwaitableCompletionSource _gate = new();

        public bool Running { get; private set; }

        public override async Awaitable Run(TransitionScope scope)
        {
            Running = true;
            await _gate.Awaitable;
            Running = false;
        }

        public void Finish()
        {
            var gate = _gate;
            _gate = new AwaitableCompletionSource();
            gate.SetResult();
        }
    }
}
