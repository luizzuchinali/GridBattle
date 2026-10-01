namespace ZS.UI.Navigation
{
    /// <summary>
    /// What a stack navigator does with a request received while a transition is
    /// still running.
    /// </summary>
    public enum ConcurrentNavigationPolicy
    {
        /// <summary>The request is dropped.</summary>
        Ignore,

        /// <summary>The request runs after the current transition finishes.</summary>
        Queue
    }
}
