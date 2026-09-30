using UnityEngine;

namespace GridBattle.Gameplay.Entities.Skills
{
    public class SkillDefinition : ScriptableObject
    {
        [SerializeField]
        private string skillName;

        [SerializeField]
        [TextArea]
        private string description;

        public string SkillName => skillName;
        public string Description => description;

        /// <summary>
        /// Indica se o caster pode usar esta skill no alvo, dadas as regras do
        /// grid. Implementar nas subclasses de cada skill.
        /// </summary>
        public virtual bool CanUse(Character caster, GridController grid, Vector2Int targetPos)
        {
            return false;
        }

        /// <summary>
        /// Executa o efeito da skill. Retornar true significa que o turno foi
        /// consumido.
        /// </summary>
        public virtual bool Execute(Character caster, GridController grid, Vector2Int targetPos)
        {
            return false;
        }
    }
}