using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// Entry point of the navigation system in a scene: knows the layers, creates
    /// and mounts views, owns the <see cref="Navigator"/>. Add it to a GameObject,
    /// list the scene's <see cref="UILayer"/>s and set the initial screen.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIRoot : MonoBehaviour, INavigationHost
    {
        [SerializeField]
        private List<UILayer> layers = new();

        [SerializeField]
        [Tooltip("Views always visible, outside navigation (backgrounds, persistent HUD, transition covers).")]
        private List<ViewDefinition> staticViews = new();

        [SerializeField]
        [Tooltip("Screen shown on start, without transition.")]
        private ViewDefinition initialScreen;

        [SerializeField]
        [Tooltip("What the screen navigator does with requests received during a transition.")]
        private ConcurrentNavigationPolicy navigationPolicy = ConcurrentNavigationPolicy.Ignore;

        internal static readonly List<UIRoot> ActiveRoots = new();

        private readonly List<ViewInstance> _instances = new();
        private readonly Dictionary<ViewDefinition, ViewInstance> _layerSourceInstances = new();

        public Navigator Navigator { get; private set; }

        /// <summary>
        /// Optional service provider available to controllers through
        /// <see cref="NavigationContext.Services"/>. Plug any DI container here.
        /// </summary>
        public IServiceProvider Services { get; set; }

        public IReadOnlyList<UILayer> Layers => layers;

        UIRoot INavigationHost.Root => this;
        CancellationToken INavigationHost.CancellationToken => destroyCancellationToken;

        internal IReadOnlyList<ViewInstance> Instances => _instances;

        private void Awake()
        {
            Navigator = new Navigator(this, navigationPolicy);

            foreach (var layer in layers)
            {
                if (layer != null && layer.SourceView != null)
                    CreateLayerSourceInstance(layer);
            }

            foreach (var definition in staticViews)
            {
                if (definition == null) continue;

                var instance = Acquire(definition);
                if (instance == null) continue;

                instance.IsStatic = true;
                instance.SetVisible(true);
            }

            foreach (var layer in layers)
            {
                if (layer == null) continue;

                layer.RootChanged += OnLayerRootChanged;
                if (layer.Root != null)
                    OnLayerRootChanged(layer);
            }

            if (initialScreen != null)
                Navigator.Replace(initialScreen, null, NavigationOptions.Instant);
        }

        private void OnEnable()
        {
            ActiveRoots.Add(this);
        }

        private void OnDisable()
        {
            ActiveRoots.Remove(this);
        }

        private void OnDestroy()
        {
            foreach (var layer in layers)
            {
                if (layer != null)
                    layer.RootChanged -= OnLayerRootChanged;
            }

            Navigator?.CloseAllModals();
            Navigator?.ClearInstances();

            foreach (var instance in _instances.ToList())
                instance.Destroy();
            _instances.Clear();
            _layerSourceInstances.Clear();
        }

        public UILayer GetLayer(UILayerDefinition definition)
        {
            if (definition == null) return null;
            return layers.Find(layer => layer != null && layer.Definition == definition);
        }

        /// <summary>
        /// First live controller of type <typeparamref name="T"/> (screens, modals,
        /// static views and pages), visible or not.
        /// </summary>
        public bool TryGetController<T>(out T controller) where T : ViewController
        {
            foreach (var instance in _instances)
            {
                if (TryFind(instance, out controller))
                    return true;
            }

            controller = null;
            return false;
        }

        /// <summary>Back action. See <see cref="Navigator.HandleBack"/>.</summary>
        public bool HandleBack()
        {
            return Navigator.HandleBack();
        }

        ViewInstance INavigationHost.Acquire(ViewDefinition definition)
        {
            return Acquire(definition);
        }

        void INavigationHost.Release(ViewInstance instance)
        {
            if (instance.IsStatic || _layerSourceInstances.ContainsValue(instance))
            {
                if (!instance.IsStatic)
                    instance.SetVisible(false);
                return;
            }

            instance.Destroy();
            _instances.Remove(instance);
        }

        private ViewInstance Acquire(ViewDefinition definition)
        {
            if (definition.MountMode == ViewMountMode.LayerSource)
            {
                if (_layerSourceInstances.TryGetValue(definition, out var existing))
                    return existing;

                Debug.LogError($"UIRoot '{name}': no UILayer has '{definition.name}' as its source view.", this);
                return null;
            }

            var layer = GetLayer(definition.Layer);
            if (layer == null)
            {
                Debug.LogError($"UIRoot '{name}': no UILayer for the layer of view '{definition.name}'.", this);
                return null;
            }

            var instance = ViewInstance.Create(definition, this, layer);
            _instances.Add(instance);
            if (layer.Root != null)
                instance.Instantiate(layer.Root);
            return instance;
        }

        private void CreateLayerSourceInstance(UILayer layer)
        {
            var definition = layer.SourceView;
            if (definition.MountMode != ViewMountMode.LayerSource)
                Debug.LogWarning($"View '{definition.name}' is the source view of layer '{layer.name}' but its mount mode is not LayerSource.", definition);

            if (_layerSourceInstances.ContainsKey(definition)) return;

            var instance = ViewInstance.Create(definition, this, layer);
            _layerSourceInstances.Add(definition, instance);
            _instances.Add(instance);
        }

        private void OnLayerRootChanged(UILayer layer)
        {
            var root = layer.Root;
            var instantiated = _instances
                .Where(instance => instance.Layer == layer && !_layerSourceInstances.ContainsValue(instance))
                .ToList();

            if (layer.SourceView != null && _layerSourceInstances.TryGetValue(layer.SourceView, out var source))
            {
                // The panel content minus what instantiated views (and their backdrops) added.
                var owned = new HashSet<VisualElement>(instantiated.SelectMany(instance => instance.Elements));
                owned.UnionWith(instantiated.Select(instance => instance.Backdrop).Where(backdrop => backdrop != null));
                source.Adopt(root, root.Children().Where(child => !owned.Contains(child)).ToList());
            }

            foreach (var instance in instantiated)
                instance.Instantiate(root);
        }

        private static bool TryFind<T>(ViewInstance instance, out T controller) where T : ViewController
        {
            if (instance.Controller is T match)
            {
                controller = match;
                return true;
            }

            foreach (var pages in instance.Context.PageNavigators)
            {
                foreach (var page in pages.Instances)
                {
                    if (TryFind(page, out controller))
                        return true;
                }
            }

            controller = null;
            return false;
        }
    }
}
