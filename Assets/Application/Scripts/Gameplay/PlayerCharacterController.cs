using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Events;
using UnityEngine;

namespace GridBattle.Gameplay
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterController : CharacterController
    {
        private PlayerCharacter _playerCharacter;
        private GridController _gridController;

        private void Awake()
        {
            EventBus.Subscribe<CellTapEvent>(OnCellTap);

            _playerCharacter = GetComponent<PlayerCharacter>();
            _gridController = FindAnyObjectByType<GridController>();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CellTapEvent>(OnCellTap);
        }

        private void OnCellTap(CellTapEvent @event)
        {
            if (!@event.Cell.HasContent)
            {
                if (!_playerCharacter.CanWalk(@event.Cell.GridPosition)) return;

                _gridController.Move(_playerCharacter.CurrentGridPos, @event.Cell.GridPosition);
                _playerCharacter.CurrentGridPos = @event.Cell.GridPosition;
            }
            else
            {
                var target = @event.Cell.GetContent();
                if (target is PlayerCharacter) return;
                if (!_playerCharacter.IsInAttackRange(@event.Cell.GridPosition)) return;
                if(target is IDamageReceiver receiver)
                    _playerCharacter.Attack(receiver);
            }
        }
    }
}