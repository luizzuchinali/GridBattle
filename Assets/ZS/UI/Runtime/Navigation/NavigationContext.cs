using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// What a <see cref="ViewController"/> can reach: the navigator, its layer,
    /// services and its page navigators.
    /// </summary>
    public sealed class NavigationContext
    {
        private readonly INavigationHost _host;
        private readonly ViewInstance _instance;
        private readonly Dictionary<string, PageNavigator> _pages = new();

        internal NavigationContext(INavigationHost host, ViewInstance instance)
        {
            _host = host;
            _instance = instance;
        }

        public Navigator Navigator => _host.Navigator;

        /// <summary>The UIRoot hosting the view (null outside a scene, e.g. tests).</summary>
        public UIRoot Root => _host.Root;

        /// <summary>Layer the view is shown in (null for pages).</summary>
        public UILayer Layer => _instance.Layer;

        public ViewDefinition Definition => _instance.Definition;

        public IServiceProvider Services => _host.Services;

        public IReadOnlyCollection<PageNavigator> PageNavigators => _pages.Values;

        public T GetService<T>() where T : class
        {
            return Services?.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Navigator for the <see cref="PageHost"/> named <paramref name="pageHostName"/>
        /// inside this view. Created on first use; after a UI reload it moves its
        /// pages to the new host element automatically.
        /// </summary>
        public PageNavigator GetPageNavigator(string pageHostName)
        {
            if (_pages.TryGetValue(pageHostName, out var navigator))
                return navigator;

            var hostElement = FindPageHost(pageHostName);
            if (hostElement == null)
                throw new InvalidOperationException($"View '{Definition.name}' has no PageHost named '{pageHostName}'.");

            navigator = new PageNavigator(_host, pageHostName, hostElement, Navigator.Policy);
            _pages.Add(pageHostName, navigator);
            return navigator;
        }

        internal void CloseView()
        {
            if (_instance.IsModal)
            {
                Navigator.CloseModal(_instance.Controller);
                return;
            }

            var stack = _instance.OwnerStack;
            if (stack != null && stack.CurrentInstance == _instance)
            {
                stack.Pop();
                return;
            }

            Debug.LogWarning($"View '{Definition.name}' cannot close itself: it is not a modal nor the top of a stack.");
        }

        internal void RetargetPages()
        {
            foreach (var navigator in _pages.Values)
            {
                var hostElement = FindPageHost(navigator.Name);
                if (hostElement != null && hostElement != navigator.HostElement)
                    navigator.Retarget(hostElement);
            }
        }

        internal void DisposePages()
        {
            foreach (var navigator in _pages.Values)
                navigator.Dispose();
            _pages.Clear();
        }

        /// <summary>
        /// Back action inside this view's pages (deepest navigator first).
        /// </summary>
        internal bool TryHandlePageBack()
        {
            foreach (var navigator in _pages.Values)
            {
                var current = navigator.CurrentInstance;
                if (current == null) continue;

                if (current.Context.TryHandlePageBack()) return true;
                if (current.Controller.OnBack()) return true;
                if (navigator.IsTransitioning) return true;

                if (navigator.Depth > 1)
                {
                    navigator.Pop();
                    return true;
                }
            }

            return false;
        }

        private PageHost FindPageHost(string pageHostName)
        {
            return _instance.Controller.Root?.Q<PageHost>(pageHostName);
        }
    }
}
