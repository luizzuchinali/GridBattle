using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using UnityEngine;

namespace GridBattle.Gameplay
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterController : MonoBehaviour
    {
        private PlayerCharacter _playerCharacter;

        private void Awake()
        {
            EventBus.Subscribe<CellTapEvent>(OnCellTap);

            _playerCharacter = GetComponent<PlayerCharacter>();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CellTapEvent>(OnCellTap);
        }

        private void OnCellTap(CellTapEvent @event)
        {

        }
    }
}