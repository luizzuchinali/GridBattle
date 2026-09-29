using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GridEntity : MonoBehaviour
    {
        public Vector2Int CurrentGridPos { get; set; }
    }
}