using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
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

        private void Update()
        {
            _gridController.HighlightCells(GridRules.GetHighlightInfos(_gridController, _playerCharacter));
        }

        private void OnCellTap(CellTapEvent @event)
        {
            if (!@event.Cell.HasContent)
            {
                if (!GridRules.CanWalkTo(_gridController, _playerCharacter, @event.Cell.GridPosition)) return;

                _gridController.Move(_playerCharacter.CurrentGridPos, @event.Cell.GridPosition);
                _playerCharacter.CurrentGridPos = @event.Cell.GridPosition;
            }
            else
            {
                if (!GridRules.IsAttackTarget(_gridController, _playerCharacter, @event.Cell.GridPosition)) return;

                var target = @event.Cell.GetContent();
                if (target is IDamageReceiver receiver)
                    _playerCharacter.Attack(receiver);
            }
        }
    }
}