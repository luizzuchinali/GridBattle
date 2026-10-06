using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    /// <summary>
    /// Global skill rules (GDD 6 / classes_e_skills). Open questions of the design
    /// documents are fields here, with neutral defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillSettings", menuName = "GridBattle/Settings/Skill Settings", order = 0)]
    public sealed class SkillSettings : ScriptableObject, IGameSettings
    {
        [Header("Skill bar")]
        [SerializeField]
        [Min(1)]
        [Tooltip("Maximum number of active skills a character carries (one per skill bar button).")]
        private int maxSkillSlots = 6;

        [SerializeField]
        [Tooltip("Open question (classes_e_skills, skill slots): the class's starting skills take one of the slots. " +
                 "Off = only skills unlocked by talents use slots.")]
        private bool startingSkillsUseSlots = true;

        [Header("Cooldown")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Open question (estados_e_atributos, cooldown reduction): lowest cooldown a skill with " +
                 "cooldown above 0 can reach after the Cooldown Reduction attribute. 1 = at least one action in between.")]
        private int minimumCooldown = 1;

        public int MaxSkillSlots => Mathf.Max(1, maxSkillSlots);
        public bool StartingSkillsUseSlots => startingSkillsUseSlots;
        public int MinimumCooldown => Mathf.Max(0, minimumCooldown);

        /// <summary>The settings asset registered in <see cref="GameSettings"/> (defaults if missing).</summary>
        public static SkillSettings Current => GameSettings.Get<SkillSettings>();
    }
}
