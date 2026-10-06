using System.Collections.Generic;
using GridBattle.Gameplay;
using GridBattle.Gameplay.Entities;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Managers
{
    /// <summary>
    /// Entry point of the game state: the list of playable characters and the start of a run. The run itself
    /// (map, nodes, battles, saving) lives in the <see cref="RunManager"/> on the same GameObject.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Playable characters. The class chosen in the menu is resolved through each config's CharacterClass.")]
        private List<PlayerCharacterConfig> playableCharacters = new();

        private static GameStateManager _instance;
        public static GameStateManager Instance => _instance;

        /// <summary>The playable character configs, in menu order.</summary>
        public IReadOnlyList<PlayerCharacterConfig> PlayableCharacters => playableCharacters;

        /// <summary>The run manager (added to this GameObject if the scene does not have it yet).</summary>
        public RunManager Runs
        {
            get
            {
                var runs = GetComponent<RunManager>();
                if (runs == null)
                    runs = gameObject.AddComponent<RunManager>();
                return runs;
            }
        }

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

        /// <summary>The playable config of a class, or null.</summary>
        [CanBeNull]
        public PlayerCharacterConfig FindPlayableCharacter(ECharacter character)
        {
            return playableCharacters.Find(config => config != null && config.CharacterClass == character);
        }

        /// <summary>
        /// Starts a new run with the class chosen in the menu (see <see cref="RunManager.StartNewRun"/>): generates
        /// the map and opens it. Nothing happens (an error or warning is logged) for an unknown or locked class.
        /// </summary>
        public void StartRun(ECharacter character)
        {
            var playerConfig = FindPlayableCharacter(character);
            if (playerConfig == null)
            {
                Debug.LogError($"No PlayerCharacterConfig with class {character} in {name}.", this);
                return;
            }

            Runs.StartNewRun(playerConfig);
        }
    }
}
