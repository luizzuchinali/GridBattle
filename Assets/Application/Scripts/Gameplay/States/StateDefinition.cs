using System.Collections.Generic;
using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.States
{
    public enum EStateKind
    {
        Buff,
        Debuff,
        Neutral
    }

    /// <summary>What happens when a state is applied to a character that already has it.</summary>
    public enum EStackPolicy
    {
        /// <summary>Keeps one instance; the duration becomes the longest of the two.</summary>
        RefreshDuration,

        /// <summary>Keeps one instance; the durations are added.</summary>
        AddDuration,

        /// <summary>Adds stacks (up to Max Stacks) and refreshes the duration. Effects scale with stacks.</summary>
        AddStacks,

        /// <summary>The new application is ignored.</summary>
        IgnoreIfPresent
    }

    /// <summary>
    /// A named state (GDD 3.1): grants one or more effects to the character that
    /// holds it (attribute changes, damage or healing over time, thorns, life
    /// steal, shield, restrictions, run modifiers...). The duration is not part of
    /// the state: whoever applies it (talent, skill, terrain, enemy) decides.
    /// </summary>
    [CreateAssetMenu(fileName = "State", menuName = "GridBattle/States/State Definition", order = 0)]
    public class StateDefinition : DisplayableDefinition
    {
        [Header("Rules")]
        [SerializeField]
        private EStateKind kind = EStateKind.Buff;

        [SerializeField]
        [Tooltip("Reapplication of a state the character already has (GDD 3.1 open question).")]
        private EStackPolicy stackPolicy = EStackPolicy.RefreshDuration;

        [SerializeField]
        [Min(1)]
        [Tooltip("Maximum stacks (Add Stacks policy only).")]
        private int maxStacks = 1;

        [SerializeField]
        [Tooltip("Shown in the entity details window.")]
        private bool showInDetails = true;

        [Header("Effects")]
        [SerializeReference]
        [SubclassPicker]
        private List<StateEffect> effects = new();

        public EStateKind Kind => kind;
        public EStackPolicy StackPolicy => stackPolicy;
        public int MaxStacks => Mathf.Max(1, maxStacks);
        public bool ShowInDetails => showInDetails;
        public IReadOnlyList<StateEffect> Effects => effects;
    }
}
