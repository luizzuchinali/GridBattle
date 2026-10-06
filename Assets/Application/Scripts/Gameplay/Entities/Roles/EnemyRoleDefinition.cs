using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Entities.Roles
{
    /// <summary>
    /// The role of an enemy (GDD Mechanic 4): melee, swarm, ranged, support,
    /// summoner or controller. The role is shown over the enemy and in the node
    /// preview, and carries the numbers the battle generator needs to compose
    /// battles by role. How the enemy actually behaves comes from its
    /// <see cref="AI.EnemyBehavior"/>, not from the role.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyRole", menuName = "GridBattle/AI/Enemy Role", order = 20)]
    public class EnemyRoleDefinition : DisplayableDefinition
    {
        [Header("Battle composition")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Role factor of the threat formula (balanceamento_e_geracao.md): threat = damage x HP / 50 x movement factor x range factor x role factor. Open question: neutral default 1 for every role.")]
        private float threatFactor = 1f;

        [SerializeField]
        [Tooltip("Whether enemies of this role fight at the front (melee, swarm). Used by the battle composition rules, e.g. never a battle without a frontline.")]
        private bool countsAsFrontline;

        public float ThreatFactor => threatFactor;
        public bool CountsAsFrontline => countsAsFrontline;
    }
}
