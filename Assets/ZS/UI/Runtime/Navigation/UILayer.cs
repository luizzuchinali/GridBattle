using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// A rendering layer: the PanelRenderer on the same GameObject, with its own
    /// sorting order. Views of the layer are shown inside its root element.
    /// Bound to a <see cref="UILayerDefinition"/> so view definitions can target
    /// it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PanelRenderer))]
    public class UILayer : MonoBehaviour
    {
        [SerializeField]
        private UILayerDefinition definition;

        [SerializeField]
        [Tooltip("Optional LayerSource view whose content is this panel's own UXML.")]
        private ViewDefinition sourceView;

        private PanelRenderer _panelRenderer;

        public UILayerDefinition Definition => definition;
        public ViewDefinition SourceView => sourceView;

        public PanelRenderer PanelRenderer => _panelRenderer != null ? _panelRenderer : _panelRenderer = GetComponent<PanelRenderer>();

        /// <summary>Root element of the panel (null until the panel loads).</summary>
        public VisualElement Root { get; private set; }

        /// <summary>Panel version, incremented by the PanelRenderer on every UI reload.</summary>
        public int Version { get; private set; }

        public int SortingOrder
        {
            get => PanelRenderer.sortingOrder;
            set => PanelRenderer.sortingOrder = value;
        }

        /// <summary>Raised when the panel root is (re)built.</summary>
        public event Action<UILayer> RootChanged;

        private void OnEnable()
        {
            // Invoked right away when the panel is already loaded.
            PanelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            PanelRenderer.UnregisterUIReloadCallback(OnUIReload);
        }

        private void OnUIReload(PanelRenderer panelRenderer, VisualElement root, int version)
        {
            if (root == Root && version == Version) return;

            Root = root;
            Version = version;
            RootChanged?.Invoke(this);
        }
    }
}
