using System.Collections.Generic;
using GridBattle.Gameplay;
using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Managers
{
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Playable characters. The class chosen in the menu is resolved through each config's CharacterClass.")]
        private List<PlayerCharacterConfig> playableCharacters = new();

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
            var playerConfig = playableCharacters.Find(config => config != null && config.CharacterClass == character);
            if (playerConfig == null)
            {
                Debug.LogError($"No PlayerCharacterConfig with class {character} in {name}.", this);
                return;
            }

            FindAnyObjectByType<GridController>().InitializeGrid(playerConfig);
        }
    }
}
