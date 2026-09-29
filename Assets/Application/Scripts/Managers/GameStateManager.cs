using System.Collections.Generic;
using GridBattle.Gameplay;
using GridBattle.Gameplay.Entities;
using GridBattle.UI.Events;
using UnityEngine;

namespace GridBattle.Managers
{
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField]
        private Dictionary<ECharacter, PlayerCharacter> _playerCharacterPrefabs;

        private static GameStateManager _instance;
        public static GameStateManager Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartRun(ECharacter character)
        {
            FindAnyObjectByType<GridController>().InitializeGrid(_playerCharacterPrefabs[character]);
        }
    }
}