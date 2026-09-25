using GridBattle.Gameplay.Entities;
using GridBattle.Managers;
using JetBrains.Annotations;
using LitMotion;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace GridBattle.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer))]
    [ExecuteAlways]
    public class Cell : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Settings")]
        [SerializeField]
        private SpriteRenderer selectionRenderer;

        public static readonly Vector2Int Size = new Vector2Int(32, 48);

        [CanBeNull]
        private GridEntity _childEntity = null;

        public bool Selected { get; set; } = false;

        private void Awake()
        {
            Assert.IsNotNull(selectionRenderer, "Selection renderer is not set!");
        }

        private void Update()
        {
            selectionRenderer.enabled = Selected;
        }

        public void SetChildEntity(GridEntity entity)
        {
            Assert.IsNotNull(entity, "Entity is null!");
            _childEntity = entity;
            entity.transform.SetParent(transform);
            entity.transform.localPosition = new Vector3(0, 8, 0) / GameConfigManager.Ppu;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var end = Vector3.one - Vector3.one * 4 / GameConfigManager.Ppu;
            var start = Vector3.one;
            LSequence.Create()
                .Append(LMotion.Create(start, end, 0.1f)
                    .WithEase(Ease.InOutBounce)
                    .Bind(x => transform.localScale = x))
                .Append(LMotion
                    .Create(end, start, 0.1f)
                    .WithEase(Ease.InOutBounce)
                    .Bind(x => transform.localScale = x))
                .Run();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }
    }
}