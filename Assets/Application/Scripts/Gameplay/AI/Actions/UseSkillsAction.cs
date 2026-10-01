using UnityEngine;

namespace GridBattle.Gameplay.AI.Actions
{
    /// <summary>
    /// Tries the enemy config's skills, in order, against the target position.
    /// Each skill's logic lives in its own SkillDefinition.
    /// </summary>
    [CreateAssetMenu(fileName = "UseSkills", menuName = "GridBattle/AI/Actions/Use Skills", order = 0)]
    public class UseSkillsAction : EnemyAction
    {
        public override bool TryExecute(in EnemyTurnContext context)
        {
            foreach (var skill in context.Self.Skills)
            {
                if (context.Self.TryUseSkill(context.Grid, skill, context.Target.CurrentGridPos))
                    return true;
            }

            return false;
        }
    }
}
