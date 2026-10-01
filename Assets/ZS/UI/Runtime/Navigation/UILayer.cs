using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// A rendering layer: one UI Toolkit panel (PanelRenderer or UIDocument on the
    /// same GameObject) with its own sorting order. Views of the layer are shown
    /// inside its root element. Bound to a <see cref="UILayerDefinition"/> so view
    /// definitions can target it.
    /// </summary>
    [DisallowMultipleComponent]
    public class UILayer : MonoBehaviour
    {
        [SerializeField]
        private UILayerDefinition definition;

        [SerializeField]
        [Tooltip("Optional LayerSource view whose content is this panel's own UXML.")]
        private ViewDefinition sourceView;

        private IPanelHost _host;

        public UILayerDefinition Definition => definition;
        public ViewDefinition SourceView => sourceView;

        /// <summary>Root element of the panel (null until the panel loads).</summary>
        public VisualElement Root { get; private set; }

        /// <summary>Incremented on every UI reload.</summary>
        public int Version { get; private set; }

        public int SortingOrder
        {
            get => EnsureHost()?.SortingOrder ?? 0;
            set
            {
                var host = EnsureHost();
                if (host != null)
                    host.SortingOrder = value;
            }
        }

        /// <summary>Raised when the panel root is (re)built.</summary>
        public event Action<UILayer> RootChanged;

        private void Awake()
        {
            EnsureHost();
        }

        private void OnEnable()
        {
            EnsureHost()?.Attach(OnRootLoaded);
        }

        private void OnDisable()
        {
            _host?.Detach();
        }

        private void LateUpdate()
        {
            _host?.Poll();
        }

        private IPanelHost EnsureHost()
        {
            if (_host != null) return _host;

            if (TryGetComponent(out PanelRenderer panelRenderer))
                _host = new PanelRendererHost(panelRenderer);
            else if (TryGetComponent(out UIDocument document))
                _host = new UIDocumentHost(document);
            else
                Debug.LogError($"UILayer '{name}' needs a PanelRenderer or UIDocument.", this);

            return _host;
        }

        private void OnRootLoaded(VisualElement root, int version)
        {
            if (root == Root && version == Version) return;

            Root = root;
            Version = version;
            RootChanged?.Invoke(this);
        }

        private interface IPanelHost
        {
            int SortingOrder { get; set; }
            void Attach(Action<VisualElement, int> onLoaded);
            void Detach();
            void Poll();
        }

        private sealed class PanelRendererHost : IPanelHost
        {
            private readonly PanelRenderer _renderer;
            private Action<VisualElement, int> _onLoaded;

            public PanelRendererHost(PanelRenderer renderer)
            {
                _renderer = renderer;
            }

            public int SortingOrder
            {
                get => _renderer.sortingOrder;
                set => _renderer.sortingOrder = value;
            }

            public void Attach(Action<VisualElement, int> onLoaded)
            {
                _onLoaded = onLoaded;
                _renderer.RegisterUIReloadCallback(OnReload);
            }

            public void Detach()
            {
                _renderer.UnregisterUIReloadCallback(OnReload);
                _onLoaded = null;
            }

            public void Poll()
            {
            }

            private void OnReload(PanelRenderer panelRenderer, VisualElement root, int version)
            {
                _onLoaded?.Invoke(root, version);
            }
        }

        /// <summary>
        /// UIDocument has no reload callback: the root is read on attach and
        /// checked every frame for replacement.
        /// </summary>
        private sealed class UIDocumentHost : IPanelHost
        {
            private readonly UIDocument _document;
            private Action<VisualElement, int> _onLoaded;
            private VisualElement _lastRoot;
            private int _version;

            public UIDocumentHost(UIDocument document)
            {
                _document = document;
            }

            public int SortingOrder
            {
                get => (int)_document.sortingOrder;
                set => _document.sortingOrder = value;
            }

            public void Attach(Action<VisualElement, int> onLoaded)
            {
                _onLoaded = onLoaded;
                _lastRoot = null;
                Poll();
            }

            public void Detach()
            {
                _onLoaded = null;
            }

            public void Poll()
            {
                var root = _document.rootVisualElement;
                if (_onLoaded == null || root == null || root == _lastRoot) return;

                _lastRoot = root;
                _onLoaded(root, ++_version);
            }
        }
    }
}
