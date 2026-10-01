using GridBattle.Gameplay.AI;
using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Controllers
{
    /// <summary>
    /// Runs the enemy's turn by delegating to the config's <see cref="EnemyBehavior"/>.
    /// The behavior (action priority and types) is data, not code.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyController : CharacterControllerBase<Enemy>
    {
        private PlayerCharacter _target;

        public void Act()
        {
            if (Owner.IsDead) return;

            if (_target == null)
                _target = FindAnyObjectByType<PlayerCharacter>();
            if (_target == null) return;

            var config = Owner.EnemyConfig;
            if (config == null || config.Behavior == null) return;

            config.Behavior.TakeTurn(new EnemyTurnContext(Owner, Grid, _target));
        }
    }
}
