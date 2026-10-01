using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay.Controllers
{
    /// <summary>
    /// Base class for character controllers. Controllers are routers: they decide
    /// WHICH action to take (player input or AI) and execute it through Character
    /// and GridController; the action logic does not live here.
    /// </summary>
    public abstract class CharacterControllerBase<TCharacter> : MonoBehaviour where TCharacter : Character
    {
        protected TCharacter Owner { get; private set; }
        protected GridController Grid { get; private set; }

        protected virtual void Awake()
        {
            Owner = GetComponent<TCharacter>();
            Grid = FindAnyObjectByType<GridController>();
        }
    }
}
