using System.Collections.Generic;
using GridBattle.Gameplay.Entities.Skills;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    public class CharacterConfig : ScriptableObject
    {
        [SerializeField]
        private int maxHp = 100;

        [SerializeField]
        private int walkDistance = 1;

        [SerializeField]
        private int attackDistance = 1;

        [SerializeField]
        private int basicAttackDamage = 10;

        [SerializeField]
        private List<SkillDefinition> skills = new();

        public int MaxHp => maxHp;
        public int WalkDistance => walkDistance;
        public int AttackDistance => attackDistance;
        public int BasicAttackDamage => basicAttackDamage;
        public IReadOnlyList<SkillDefinition> Skills => skills;
    }
}