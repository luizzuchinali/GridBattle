using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Meta
{
    /// <summary>
    /// A short contextual tip (interface 4.4): the name is the title and the description is the
    /// text (one or two sentences). Shown once, the first time its <see cref="Trigger"/> happens;
    /// the profile remembers which tips were seen.
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialTip", menuName = "GridBattle/Meta/Tutorial Tip", order = 0)]
    public sealed class TutorialTipDefinition : DisplayableDefinition
    {
        [Header("Tutorial")]
        [SerializeField]
        [Tooltip("Situation that shows this tip the first time it happens. Use one tip per trigger.")]
        private ETutorialTrigger trigger;

        public ETutorialTrigger Trigger => trigger;
    }
}
