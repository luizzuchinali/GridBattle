using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay
{
    [RequireComponent(typeof(Enemy))]
    public class EnemyController : CharacterController
    {
        private Enemy _enemy;
        private GridController _gridController;
        private PlayerCharacter _player;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _gridController = FindAnyObjectByType<GridController>();
        }

        public void Act()
        {
            if (_enemy.IsDead) return;

            CachePlayer();
            if (_player == null) return;

            if (TrySpecialAction()) return;
            if (TryAttackPlayer()) return;
            TryWalkTowardPlayer();
        }

        /// <summary>
        /// Ação específica do tipo de inimigo: consome as skills definidas no
        /// config do character (a lógica vive nas SkillDefinitions, não aqui).
        /// Sobrescrever nas subclasses para comportamentos manuais. Retornar
        /// true indica que a ação do turno foi consumida.
        /// </summary>
        protected virtual bool TrySpecialAction()
        {
            foreach (var skill in _enemy.Skills)
            {
                if (_enemy.TryUseSkill(_gridController, skill, _player.CurrentGridPos))
                    return true;
            }

            return false;
        }

        private bool TryAttackPlayer()
        {
            if (!GridRules.IsAttackTarget(_gridController, _enemy, _player.CurrentGridPos))
                return false;

            if (_player is IDamageReceiver receiver)
                _enemy.Attack(receiver);
            return true;
        }

        private void TryWalkTowardPlayer()
        {
            var step = FindBestStep(_player.CurrentGridPos);
            if (step == null) return;

            _gridController.MoveEntity(_enemy, step.Value);
        }

        private Vector2Int? FindBestStep(Vector2Int playerPos)
        {
            Vector2Int? bestStep = null;
            var bestDistance = Vector2Int.Distance(_enemy.CurrentGridPos, playerPos);
            var range = _enemy.WalkDistance;

            for (var x = -range; x <= range; x++)
            {
                for (var y = -range; y <= range; y++)
                {
                    if (x == 0 && y == 0) continue;

                    var candidate = _enemy.CurrentGridPos + new Vector2Int(x, y);
                    if (!GridRules.CanWalkTo(_gridController, _enemy, candidate)) continue;

                    var distance = Vector2Int.Distance(candidate, playerPos);
                    if (distance >= bestDistance) continue;

                    bestDistance = distance;
                    bestStep = candidate;
                }
            }

            return bestStep;
        }

        private void CachePlayer()
        {
            if (_player != null) return;
            _player = FindAnyObjectByType<PlayerCharacter>();
        }
    }
}