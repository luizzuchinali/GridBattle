using System;
using UnityEngine;

namespace GridBattle.Gameplay.States
{
    /// <summary>
    /// A state plus how it is applied (permanent or for N turns of the holder,
    /// and how many stacks). Used by talents, skills, terrain, consumables and AI.
    /// </summary>
    [Serializable]
    public struct StateGrant
    {
        [SerializeField]
        private StateDefinition state;

        [SerializeField]
        private bool permanent;

        [SerializeField]
        [Min(1)]
        [Tooltip("Turns of the holder (ignored when permanent).")]
        private int duration;

        [SerializeField]
        [Min(1)]
        private int stacks;

        public StateGrant(StateDefinition state, int duration, int stacks = 1, bool permanent = false)
        {
            this.state = state;
            this.duration = duration;
            this.stacks = stacks;
            this.permanent = permanent;
        }

        public StateDefinition State => state;
        public bool IsPermanent => permanent;
        public int Duration => permanent ? StateInstance.Permanent : Mathf.Max(1, duration);
        public int Stacks => Mathf.Max(1, stacks);
        public bool IsValid => state != null;
    }
}
