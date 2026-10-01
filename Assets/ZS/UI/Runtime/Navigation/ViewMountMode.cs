namespace ZS.UI.Navigation
{
    public enum ViewMountMode
    {
        /// <summary>
        /// The view's UXML is cloned into its layer (or page host) every time the
        /// view is created. Supports multiple instances, pages and dynamic modals.
        /// </summary>
        Instantiate,

        /// <summary>
        /// The view is the layer's own panel content (the UXML set on the layer's
        /// PanelRenderer). One instance per layer, created with the
        /// UIRoot. Keeps the exact element hierarchy, so it suits views animated by
        /// an Animator and gives edit-time preview of the panel.
        /// </summary>
        LayerSource
    }
}
