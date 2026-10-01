using System;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Events;
using GridBattle.Managers;
using JetBrains.Annotations;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace GridBattle.Gameplay
{
    public enum ECellHighlightType
    {
        Attack,
        Walk
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class Cell : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Settings")]
        [SerializeField]
        private SpriteRenderer selectionRenderer;

        [SerializeField]
        private CellContentHealthBar cellContentHealthBar;

        [SerializeField]
        private SpriteRenderer highlightRenderer;

        [SerializeField]
        private Color walkHighlightColor;

        [SerializeField]
        private Color attackHighlightColor;

        [SerializeField]
        private TextMeshPro damageText;

        [SerializeField]
        private Vector3 contentPosition;

        [Header("Damage Text Animation")]
        [SerializeField]
        private float damageTextArcHeight = 0.15f;

        [SerializeField]
        private float damageTextDuration = 0.4f;

        [Header("Highlight Animation")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Delay between consecutive highlights appearing together, in seconds.")]
        private float highlightStagger = 0.025f;

        [SerializeField]
        [Min(0.01f)]
        private float highlightFadeDuration = 0.06f;

        private static int _lastRiseFrame = -1;
        private static int _riseCount;

        private bool _wasHighlighted;
        private float _highlightStart;

        public static readonly Vector2Int Size = new(32, 46);

        [CanBeNull]
        private GridEntity _content;

        private MotionHandle _scaleHandle;

        public bool Selected { get; set; } = false;
        public bool Highlighted { get; set; } = false;
        public ECellHighlightType HighlightType { get; set; } = ECellHighlightType.Walk;

        public Vector2Int GridPosition { get; set; }

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
            UpdateHighlight();
        }

        /// <summary>
        /// Visual only: highlights appearing in the same frame fade in one after
        /// another. The Highlighted flag itself (rules) is untouched.
        /// </summary>
        private void UpdateHighlight()
        {
            if (!Highlighted)
            {
                _wasHighlighted = false;
                highlightRenderer.enabled = false;
                return;
            }

            if (!_wasHighlighted)
            {
                _wasHighlighted = true;
                if (Time.frameCount != _lastRiseFrame)
                {
                    _lastRiseFrame = Time.frameCount;
                    _riseCount = 0;
                }

                _highlightStart = Time.time + _riseCount++ * highlightStagger;
            }

            var alpha = Mathf.Clamp01((Time.time - _highlightStart) / highlightFadeDuration);
            var color = HighlightType == ECellHighlightType.Walk ? walkHighlightColor : attackHighlightColor;
            color.a *= alpha;
            highlightRenderer.color = color;
            highlightRenderer.enabled = Time.time >= _highlightStart;
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
            if (_content != null)
                throw new InvalidOperationException("Cell already has content");

            Assert.IsNotNull(entity, "Entity is null!");
            _content = entity;
            entity.transform.localScale = Vector3.one;
            entity.transform.SetParent(transform);
            entity.transform.localPosition = contentPosition;
            entity.CurrentGridPos = GridPosition;

            if (_content is IDamageReceiver receiver)
            {
                receiver.OnHpChanged += HandleContentHpChanged;
                var hpInfo = receiver.GetHpInfo();
                cellContentHealthBar.UpdateHp(hpInfo.CurrentHp, hpInfo.MaxHp);
                cellContentHealthBar?.Show();
            }
            else
            {
                cellContentHealthBar?.Hide();
            }
        }

        [CanBeNull]
        public GridEntity GetContent() => _content;

        public void RemoveContent()
        {
            if (_content == null) return;
            if (_content is IDamageReceiver receiver)
            {
                receiver.OnHpChanged -= HandleContentHpChanged;
            }

            _content = null;
            cellContentHealthBar?.Hide();
        }

        public bool HasContent => _content != null;

        /// <summary>
        /// Short scale pulse on arrival of an entity, sized in pixels (never a
        /// fractional factor of the sprite).
        /// </summary>
        public void PlayArrivalPulse(float pixels, float duration)
        {
            PlayScalePulse(pixels, duration, Ease.OutQuad, Ease.InQuad);
        }

        /// <summary>
        /// Feedback for a tap that did not become an action; called by whoever
        /// rejected the tap.
        /// </summary>
        public void PlayRejectFeedback()
        {
            PlayScalePulse(4f, 0.1f, Ease.InOutBounce, Ease.InOutBounce);
        }

        private void PlayScalePulse(float pixels, float duration, Ease inEase, Ease outEase)
        {
            if (_scaleHandle.IsActive())
                _scaleHandle.Cancel();

            var start = Vector3.one;
            var end = Vector3.one - Vector3.one * pixels / GameConfigManager.Ppu;
            _scaleHandle = LSequence.Create()
                .Append(LMotion.Create(start, end, duration).WithEase(inEase)
                    .Bind(x => transform.localScale = x))
                .Append(LMotion.Create(end, start, duration).WithEase(outEase)
                    .Bind(x => transform.localScale = x))
                .Run();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            EventBus.Raise(new CellTapEvent
            {
                Cell = this
            });
        }

        public void OnPointerDown(PointerEventData eventData)
        {
        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }
    }
}