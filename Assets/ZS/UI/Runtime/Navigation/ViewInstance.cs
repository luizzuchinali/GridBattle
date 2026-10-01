using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// A live view: its controller and the elements it owns (or adopted) in a
    /// container (layer root or page host).
    /// </summary>
    internal sealed class ViewInstance
    {
        private readonly List<VisualElement> _elements = new();
        private bool _ownsElements;
        private EventCallback<PointerUpEvent> _backdropCallback;

        private ViewInstance(ViewDefinition definition, INavigationHost host, UILayer layer)
        {
            Definition = definition;
            Layer = layer;
            Context = new NavigationContext(host, this);
            Controller = definition.CreateController();
            Controller.Attach(this);
        }

        public ViewDefinition Definition { get; }
        public ViewController Controller { get; }
        public NavigationContext Context { get; }
        public UILayer Layer { get; }

        /// <summary>Stack (screens or pages) that currently holds this view.</summary>
        public ViewStackNavigator OwnerStack { get; set; }

        public bool IsModal { get; set; }
        public bool IsStatic { get; set; }
        public bool IsVisible { get; private set; }
        public bool IsDestroyed { get; private set; }
        public VisualElement Container { get; private set; }
        public VisualElement Backdrop { get; private set; }
        public IReadOnlyList<VisualElement> Elements => _elements;

        public static ViewInstance Create(ViewDefinition definition, INavigationHost host, UILayer layer)
        {
            var instance = new ViewInstance(definition, host, layer);
            instance.Controller.OnCreate();
            return instance;
        }

        /// <summary>
        /// Clones the view's UXML into <paramref name="container"/> (replacing
        /// elements cloned before) and binds it.
        /// </summary>
        public void Instantiate(VisualElement container)
        {
            RemoveOwnedElements();
            Container = container;
            _ownsElements = true;

            if (Definition.VisualTree != null)
            {
                Definition.VisualTree.CloneTree(container, out var firstIndex, out var count);
                for (var i = 0; i < count; i++)
                    _elements.Add(container[firstIndex + i]);
            }
            else
            {
                var root = new VisualElement();
                container.Add(root);
                _elements.Add(root);
            }

            Bind();
        }

        /// <summary>
        /// Binds to elements that already exist in the container (LayerSource
        /// views: the panel's own content).
        /// </summary>
        public void Adopt(VisualElement container, IEnumerable<VisualElement> elements)
        {
            _elements.Clear();
            _ownsElements = false;
            Container = container;
            _elements.AddRange(elements);
            Bind();
        }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            ApplyVisibility();
        }

        public void BringToFront()
        {
            Backdrop?.BringToFront();
            foreach (var element in _elements)
                element.BringToFront();
        }

        /// <summary>
        /// Adds a full-container backdrop right behind the view (modals).
        /// </summary>
        public void AttachBackdrop(EventCallback<PointerUpEvent> onPointerUp)
        {
            _backdropCallback = onPointerUp;
            CreateBackdrop();
        }

        public void DetachBackdrop()
        {
            _backdropCallback = null;
            RemoveBackdrop();
        }

        public void Destroy()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;

            Context.DisposePages();
            Controller.OnDestroy();
            DetachBackdrop();
            RemoveOwnedElements();
        }

        private void Bind()
        {
            foreach (var sheet in Definition.StyleSheets)
            {
                if (sheet == null) continue;
                foreach (var element in _elements)
                {
                    if (!element.styleSheets.Contains(sheet))
                        element.styleSheets.Add(sheet);
                }
            }

            Controller.Root = _elements.Count > 0 ? _elements[0] : null;

            if (_backdropCallback != null)
                CreateBackdrop();

            ApplyVisibility();
            Context.RetargetPages();
            Controller.OnBind(Controller.Root);
        }

        private void ApplyVisibility()
        {
            foreach (var element in _elements)
                element.SetDisplayed(IsVisible);
            Backdrop?.SetDisplayed(IsVisible);
        }

        private void CreateBackdrop()
        {
            RemoveBackdrop();
            if (Container == null || _elements.Count == 0) return;

            var backdrop = new VisualElement { name = "zs-modal-backdrop", pickingMode = PickingMode.Position };
            backdrop.AddToClassList("zs-modal-backdrop");
            backdrop.style.position = Position.Absolute;
            backdrop.style.left = 0;
            backdrop.style.top = 0;
            backdrop.style.right = 0;
            backdrop.style.bottom = 0;
            backdrop.style.backgroundColor = Definition.BackdropColor;
            backdrop.RegisterCallback(_backdropCallback);

            Container.Insert(Container.IndexOf(_elements[0]), backdrop);
            Backdrop = backdrop;
            backdrop.SetDisplayed(IsVisible);
        }

        private void RemoveBackdrop()
        {
            Backdrop?.RemoveFromHierarchy();
            Backdrop = null;
        }

        private void RemoveOwnedElements()
        {
            if (_ownsElements)
            {
                foreach (var element in _elements)
                    element.RemoveFromHierarchy();
            }

            _elements.Clear();
        }
    }
}
