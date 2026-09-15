using GridBattle.UI.Events;
using UnityEngine;

namespace GridBattle.Managers
{
    public class GameStateManager : MonoBehaviour
    {
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

        public static void StartRun(ECharacter character)
        {
            Debug.Log($"Character choosen {character}");
        }
    }
}