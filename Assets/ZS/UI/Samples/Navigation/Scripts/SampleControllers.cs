using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using ZS.UI.Navigation;

namespace ZS.UI.Samples
{
    /// <summary>
    /// References to the sample's view definitions, shared by the controllers
    /// through the UIRoot's service provider.
    /// </summary>
    public sealed class SampleViews : System.IServiceProvider
    {
        public SampleViews(SampleBootstrap bootstrap)
        {
            Bootstrap = bootstrap;
        }

        public SampleBootstrap Bootstrap { get; }

        public object GetService(System.Type serviceType)
        {
            return serviceType == typeof(SampleViews) ? this : null;
        }
    }

    /// <summary>Home: push with arguments, modal with result, screen with pages.</summary>
    [Preserve]
    public sealed class HomeController : ViewController
    {
        private int _detailsCount;

        protected override void OnBind(VisualElement root)
        {
            var views = Context.GetService<SampleViews>().Bootstrap;
            var result = root.Q<Label>("result");

            root.Q<Button>("details").clicked += () => Context.Navigator.Push(views.Details, ++_detailsCount);
            root.Q<Button>("settings").clicked += () => Context.Navigator.Push(views.Settings);
            root.Q<Button>("confirm").clicked += async () =>
            {
                var accepted = await Context.Navigator.ShowModal<bool>(views.Confirm, "Delete everything?");
                result.text = accepted ? "Confirmed" : "Cancelled";
            };
        }
    }

    /// <summary>Shows the navigation argument; Back pops it.</summary>
    [Preserve]
    public sealed class DetailsController : ViewController
    {
        private Label _title;

        protected override void OnBind(VisualElement root)
        {
            _title = root.Q<Label>("title");
            root.Q<Button>("back").clicked += Close;
        }

        protected override void OnEnter(object args)
        {
            if (args != null)
                _title.text = $"Details #{args}";
        }
    }

    /// <summary>Tabs implemented with a PageHost.</summary>
    [Preserve]
    public sealed class SettingsController : ViewController
    {
        protected override void OnBind(VisualElement root)
        {
            var views = Context.GetService<SampleViews>().Bootstrap;
            var pages = Context.GetPageNavigator("pages");

            root.Q<Button>("general").clicked += () => pages.Replace(views.GeneralPage);
            root.Q<Button>("audio").clicked += () => pages.Replace(views.AudioPage);
            root.Q<Button>("back").clicked += Close;

            if (pages.Depth == 0)
                pages.Replace(views.GeneralPage, null, NavigationOptions.Instant);
        }
    }

    /// <summary>Yes/No modal returning a bool.</summary>
    [Preserve]
    public sealed class ConfirmController : ModalController<bool>
    {
        private Label _message;

        protected override void OnBind(VisualElement root)
        {
            _message = root.Q<Label>("message");
            root.Q<Button>("yes").clicked += () => Close(true);
            root.Q<Button>("no").clicked += () => Close(false);
        }

        protected override void OnEnter(object args)
        {
            _message.text = args as string ?? "Are you sure?";
        }
    }
}
