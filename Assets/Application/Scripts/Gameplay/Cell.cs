using System;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Managers;
using JetBrains.Annotations;
using LitMotion;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace GridBattle.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer))]
    [ExecuteAlways]
    public class Cell : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Settings")]
        [SerializeField]
        private SpriteRenderer selectionRenderer;

        [SerializeField]
        private CellContentHealthBar cellContentHealthBar;

        [SerializeField]
        private TextMeshPro damageText;

        [Header("Damage Text Animation")]
        [SerializeField]
        private float damageTextArcHeight = 0.15f;

        [SerializeField]
        private float damageTextDuration = 0.4f;

        public static readonly Vector2Int Size = new Vector2Int(32, 48);

        [CanBeNull]
        private GridEntity _content = null;

        public bool Selected { get; set; } = false;

        private void Awake()
        {
            Assert.IsNotNull(selectionRenderer, "Selection renderer is not set!");
            Assert.IsNotNull(cellContentHealthBar, "Cell content health bar is not set!");
            Assert.IsNotNull(damageText, "Damage text is not set!");
            damageText.gameObject.SetActive(false);
        }

        private void Update()
        {
            selectionRenderer.enabled = Selected;
        }

        private void HandleContentHpChanged(DamageReceiveData data)
        {
            cellContentHealthBar?.UpdateHp(data.CurrentHp, data.MaxHp);

            var damageTextInstance = Instantiate(damageText, damageText.transform.parent);
            damageTextInstance.text = data.Damage.ToString();
            damageTextInstance.gameObject.SetActive(true);

            var start = new Vector3(0, 0.23f, 0);
            var end = new Vector3(Random.Range(-0.25f, 0.25f), 0.4f, 0);
            var apex = new Vector3((start.x + end.x) / 2, Mathf.Max(start.y, end.y) + damageTextArcHeight, 0);
            LMotion.Create(0f, 1f, damageTextDuration)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(() => Destroy(damageTextInstance.gameObject))
                .Bind(t => damageTextInstance.transform.localPosition =
                    (1 - t) * (1 - t) * start + 2 * (1 - t) * t * apex + t * t * end);
        }

        public void SetContent(GridEntity entity)
        {
            RemoveContent();

            Assert.IsNotNull(entity, "Entity is null!");
            _content = entity;
            entity.transform.SetParent(transform);
            entity.transform.localPosition = new Vector3(0, 8, 0) / GameConfigManager.Ppu;

            if (_content.TryGetComponent(out IDamageReceiver receiver))
            {
                receiver.OnHpChanged += HandleContentHpChanged;
                cellContentHealthBar?.Show();
            }
            else
            {
                cellContentHealthBar?.Hide();
            }
        }

        public void RemoveContent()
        {
            if (_content == null) return;
            if (_content.TryGetComponent(out IDamageReceiver receiver))
            {
                receiver.OnHpChanged -= HandleContentHpChanged;
                cellContentHealthBar?.Hide();
            }

            _content = null;
        }

        public bool HasContent =>
            _content != null;

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

            if (_content == null) return;
            if (_content.TryGetComponent(out IDamageReceiver receiver))
            {
                receiver.ReceiveDamage(10);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }
    }
}