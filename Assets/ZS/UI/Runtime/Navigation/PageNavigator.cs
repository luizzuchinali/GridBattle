namespace ZS.UI.Navigation
{
    /// <summary>
    /// Navigation inside a view (tabs, wizards, sub-pages): a stack of pages
    /// instantiated in a <see cref="PageHost"/>. Obtained through
    /// <see cref="NavigationContext.GetPageNavigator"/>; destroyed with its view.
    /// Pages always use <see cref="ViewMountMode.Instantiate"/>.
    /// </summary>
    public sealed class PageNavigator : ViewStackNavigator
    {
        internal PageNavigator(INavigationHost host, string name, PageHost hostElement,
            ConcurrentNavigationPolicy policy) : base(host, policy)
        {
            Name = name;
            HostElement = hostElement;
        }

        /// <summary>Name of the PageHost element.</summary>
        public string Name { get; }

        public PageHost HostElement { get; private set; }

        internal override ViewInstance AcquireInstance(ViewDefinition definition)
        {
            var instance = ViewInstance.Create(definition, Host, null);
            instance.Instantiate(HostElement);
            return instance;
        }

        internal override void ReleaseInstance(ViewInstance instance)
        {
            instance.OwnerStack = null;
            instance.Destroy();
        }

        internal void Retarget(PageHost hostElement)
        {
            HostElement = hostElement;
            foreach (var instance in Instances)
                instance.Instantiate(hostElement);
        }

        internal void Dispose()
        {
            ClearInstances();
        }
    }
}
