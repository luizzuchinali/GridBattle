using UnityEngine;
using UnityEngine.Assertions;

namespace GridBattle.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer))]
    [ExecuteAlways]
    public class Cell : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField]
        private SpriteRenderer selectionRenderer;

        public static readonly Vector2Int Size = new Vector2Int(32, 48);

        public bool Selected { get; set; } = false;

        private void Awake()
        {
            Assert.IsNotNull(selectionRenderer, "Selection renderer is not set!");
        }

        private void Update()
        {
            selectionRenderer.enabled = Selected;
        }
    }
}