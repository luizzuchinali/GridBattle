using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using ZS.UI.Navigation.Transitions;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// Describes a navigable view (screen, modal or page): its UXML, the C#
    /// controller that drives it, the layer it lives in and how it transitions.
    /// </summary>
    [CreateAssetMenu(fileName = "ViewDefinition", menuName = "ZS/UI/View Definition", order = 0)]
    public class ViewDefinition : ScriptableObject
    {
        [Header("Content")]
        [SerializeField]
        [Tooltip("UXML of the view. Without it the view gets an empty root element to be built in code.")]
        private VisualTreeAsset visualTree;

        [SerializeField]
        [Tooltip("Extra style sheets added to the view root elements.")]
        private List<StyleSheet> styleSheets = new();

        [SerializeField]
        [ViewControllerType]
        [Tooltip("ViewController subclass created for each instance of this view.")]
        private string controllerType;

        [Header("Hosting")]
        [SerializeField]
        [Tooltip("Layer the view is shown in. Ignored for pages (they live in a PageHost).")]
        private UILayerDefinition layer;

        [SerializeField]
        private ViewMountMode mountMode = ViewMountMode.Instantiate;

        [SerializeField]
        [Tooltip("Transition used when navigating to this view (and when popping/closing it). Empty = immediate.")]
        private ViewTransition transition;

        [Header("Modal")]
        [SerializeField]
        [Tooltip("Adds a full-layer backdrop behind the modal that blocks input to the views below.")]
        private bool useBackdrop = true;

        [SerializeField]
        private Color backdropColor = new(0f, 0f, 0f, 0.5f);

        [SerializeField]
        private bool closeOnBackdropClick = true;

        [SerializeField]
        [Tooltip("Whether the back action (Navigator.HandleBack) closes the modal.")]
        private bool closeOnBack = true;

        [NonSerialized]
        private Type _resolvedControllerType;

        [NonSerialized]
        private string _resolvedFrom;

        public VisualTreeAsset VisualTree => visualTree;
        public IReadOnlyList<StyleSheet> StyleSheets => styleSheets;
        public string ControllerTypeName => controllerType;
        public UILayerDefinition Layer => layer;
        public ViewMountMode MountMode => mountMode;
        public ViewTransition Transition => transition;
        public bool UseBackdrop => useBackdrop;
        public Color BackdropColor => backdropColor;
        public bool CloseOnBackdropClick => closeOnBackdropClick;
        public bool CloseOnBack => closeOnBack;

        /// <summary>
        /// Resolved controller type, or null if none is set or it cannot be found.
        /// </summary>
        public Type ControllerType
        {
            get
            {
                if (_resolvedFrom != controllerType)
                {
                    _resolvedFrom = controllerType;
                    _resolvedControllerType = string.IsNullOrEmpty(controllerType) ? null : Type.GetType(controllerType);
                }

                return _resolvedControllerType;
            }
        }

        internal ViewController CreateController()
        {
            var type = ControllerType;
            if (type == null)
            {
                if (!string.IsNullOrEmpty(controllerType))
                    Debug.LogError($"View '{name}': controller type '{controllerType}' not found.", this);
                return new EmptyViewController();
            }

            return (ViewController)Activator.CreateInstance(type);
        }

        /// <summary>
        /// Configuration problems, for editor validation. Empty when valid.
        /// </summary>
        public IEnumerable<string> Validate()
        {
            if (!string.IsNullOrEmpty(controllerType))
            {
                var type = ControllerType;
                if (type == null)
                    yield return $"Controller type '{controllerType}' not found.";
                else if (!typeof(ViewController).IsAssignableFrom(type) || type.IsAbstract)
                    yield return $"'{type.Name}' is not a concrete ViewController.";
            }

            if (mountMode == ViewMountMode.Instantiate && visualTree == null)
                yield return "No UXML: the view starts with an empty root element.";

            if (mountMode == ViewMountMode.LayerSource && layer == null)
                yield return "LayerSource views need a layer.";
        }

        /// <summary>
        /// Serialized form of a controller type ("Namespace.Type, Assembly").
        /// </summary>
        public static string ToTypeName(Type type)
        {
            return type == null ? null : $"{type.FullName}, {type.Assembly.GetName().Name}";
        }

        internal void SetModalOptions(bool backdrop, bool closeOnBackdrop, bool closeWithBack)
        {
            useBackdrop = backdrop;
            closeOnBackdropClick = closeOnBackdrop;
            closeOnBack = closeWithBack;
        }

        internal void SetForTests(Type controller, UILayerDefinition targetLayer, ViewMountMode mode = ViewMountMode.Instantiate,
            ViewTransition viewTransition = null)
        {
            controllerType = ToTypeName(controller);
            layer = targetLayer;
            mountMode = mode;
            transition = viewTransition;
        }
    }
}
