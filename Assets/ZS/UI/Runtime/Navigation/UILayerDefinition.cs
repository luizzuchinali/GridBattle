using UnityEngine;

namespace ZS.UI.Navigation
{
    /// <summary>
    /// Identity of a UI layer. Views reference a layer definition (an asset), and
    /// each scene binds it to a <see cref="UILayer"/> (a panel). This keeps view
    /// definitions independent from scenes.
    /// </summary>
    [CreateAssetMenu(fileName = "UILayer", menuName = "ZS/UI/Layer Definition", order = 1)]
    public class UILayerDefinition : ScriptableObject
    {
        [SerializeField]
        [TextArea]
        private string description;

        public string Description => description;
    }
}
