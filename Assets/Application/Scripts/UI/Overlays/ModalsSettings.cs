using GridBattle.Data;
using UnityEngine;
using ZS.UI.Navigation;

namespace GridBattle.UI.Overlays
{
    /// <summary>
    /// Settings of the modal windows of the interface (talent choice, glossary, options, tutorial tips, give up
    /// confirmation): the views the flow opens and the open questions of the interface documents (GDD 4.1, 4.3,
    /// 4.4), each with a neutral default.
    /// </summary>
    [CreateAssetMenu(fileName = "ModalsSettings", menuName = "GridBattle/Settings/Modals Settings", order = 2)]
    public sealed class ModalsSettings : ScriptableObject, IGameSettings
    {
        [Header("Views")]
        [SerializeField]
        [Tooltip("Yes/no confirmation modal (skip a talent, give up the run).")]
        private ViewDefinition confirmView;

        [SerializeField]
        [Tooltip("Talent choice: opened by TalentOfferOpenedEvent, answers through TalentService.")]
        private ViewDefinition talentChoiceView;

        [SerializeField]
        [Tooltip("In-game glossary (classes and enemies): opened by GlossaryRequestedEvent.")]
        private ViewDefinition glossaryView;

        [SerializeField]
        [Tooltip("Options (language, volumes, tips): opened by OptionsRequestedEvent.")]
        private ViewDefinition optionsView;

        [SerializeField]
        [Tooltip("Small window of a contextual tutorial tip: opened by TutorialTipRequestedEvent.")]
        private ViewDefinition tutorialTipView;

        [Header("Talent choice (interface 4.1)")]
        [SerializeField]
        [Tooltip("Skipping an offer asks for confirmation first (the level's talent is lost).")]
        private bool confirmSkip = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Seconds the acquisition effect (the chosen card flashes, the window fades) lasts before the window " +
                 "closes. The game resumes at once; this is only visual. 0 = close immediately.")]
        private float acquireEffectSeconds = 0.35f;

        [SerializeField]
        [Min(0)]
        [Tooltip("Milliseconds between one card and the next fading in when an offer is shown, rerolled or a talent is banned. 0 = all at once.")]
        private int cardStaggerMs = 70;

        [Header("Glossary (interface 4.3)")]
        [SerializeField]
        [Tooltip("Open question: whether the glossary can be opened while a talent choice is open (the game is paused). " +
                 "Off = the request is ignored during the talent pause.")]
        private bool allowGlossaryDuringTalentChoice;

        [Header("Pause menu (interface 4.3)")]
        [SerializeField]
        [Tooltip("Giving up asks for confirmation first.")]
        private bool confirmGiveUp = true;

        [Header("Tutorial tips (interface 4.4)")]
        [SerializeField]
        [Tooltip("A tip may open on top of the talent choice (the first level up tip explains that very window). " +
                 "Off = tips wait until the talent choice closes.")]
        private bool showTipsOverTalentChoice = true;

        [Header("Icons (placeholders)")]
        [SerializeField]
        [Tooltip("Badge of a talent option that combines with the build.")]
        private Sprite buildMatchIcon;

        [SerializeField]
        [Tooltip("Badge of a talent option that unlocks a skill without an icon of its own.")]
        private Sprite newSkillIcon;

        public ViewDefinition ConfirmView => confirmView;
        public ViewDefinition TalentChoiceView => talentChoiceView;
        public ViewDefinition GlossaryView => glossaryView;
        public ViewDefinition OptionsView => optionsView;
        public ViewDefinition TutorialTipView => tutorialTipView;
        public bool ConfirmSkip => confirmSkip;
        public float AcquireEffectSeconds => acquireEffectSeconds;
        public int CardStaggerMs => cardStaggerMs;
        public bool AllowGlossaryDuringTalentChoice => allowGlossaryDuringTalentChoice;
        public bool ConfirmGiveUp => confirmGiveUp;
        public bool ShowTipsOverTalentChoice => showTipsOverTalentChoice;
        public Sprite BuildMatchIcon => buildMatchIcon;
        public Sprite NewSkillIcon => newSkillIcon;

        /// <summary>The settings asset registered in <see cref="GameSettings"/> (defaults if missing).</summary>
        public static ModalsSettings Current => GameSettings.Get<ModalsSettings>();
    }
}
