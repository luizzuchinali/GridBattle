using UnityEngine;
using ZS.UI.Navigation;

namespace ZS.UI.Samples
{
    /// <summary>
    /// Wires the sample: exposes the view definitions to the controllers through
    /// the UIRoot service provider. Runs before UIRoot so services exist when the
    /// first screen binds.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(UIRoot))]
    public class SampleBootstrap : MonoBehaviour
    {
        [SerializeField] private ViewDefinition details;
        [SerializeField] private ViewDefinition settings;
        [SerializeField] private ViewDefinition confirm;
        [SerializeField] private ViewDefinition generalPage;
        [SerializeField] private ViewDefinition audioPage;

        public ViewDefinition Details => details;
        public ViewDefinition Settings => settings;
        public ViewDefinition Confirm => confirm;
        public ViewDefinition GeneralPage => generalPage;
        public ViewDefinition AudioPage => audioPage;

        private void Awake()
        {
            GetComponent<UIRoot>().Services = new SampleViews(this);
        }
    }
}
