using UnityEngine.UIElements;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// Container for the pages of a view. Add it to a view's UXML
    /// (<c>&lt;zsnav:PageHost name="tabs"/&gt;</c>) and navigate it with
    /// <c>Context.GetPageNavigator("tabs")</c>.
    /// </summary>
    [UxmlElement]
    public partial class PageHost : VisualElement
    {
        public const string UssClassName = "zs-page-host";

        public PageHost()
        {
            AddToClassList(UssClassName);
        }
    }
}
