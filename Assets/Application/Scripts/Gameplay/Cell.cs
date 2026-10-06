using System;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Simulation;
using GridBattle.Gameplay.Terrain;
using GridBattle.Managers;
using JetBrains.Annotations;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Random = UnityEngine.Random;

namespace GridBattle.Gameplay
{
    public enum ECellHighlightType
    {
        Attack,
        Walk,

        /// <summary>A cell the selected skill can be aimed at.</summary>
        SkillRange
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class Cell : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
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
        [Tooltip("Highlight of the cells the selected skill can be aimed at.")]
        private Color skillRangeHighlightColor = new(0.75f, 0.45f, 1f, 0.51f);

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

        [Header("Skill Area Feedback")]
        [SerializeField]
        [Tooltip("Color the cell flashes when a skill hits its area.")]
        private Color skillAreaFlashColor = new(1f, 0.9f, 0.4f, 0.85f);

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Seconds the skill flash takes to fade out.")]
        private float skillFlashDuration = 0.25f;

        [Header("Terrain")]
        [SerializeField]
        [Tooltip("Child renderer that draws the terrain overlay sprite (under characters and highlights).")]
        private SpriteRenderer terrainRenderer;

        [SerializeField]
        [HideInInspector]
        private TerrainDefinition terrain;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("Seconds the terrain pulse (a hazard or bonus cell triggering) takes to fade out.")]
        private float terrainPulseDuration = 0.3f;

        [SerializeField]
        [Tooltip("Color the cell floor and overlay flash toward when the terrain triggers.")]
        private Color terrainPulseColor = Color.white;

        private static int _lastRiseFrame = -1;
        private static int _riseCount;

        private bool _wasHighlighted;
        private float _highlightStart;

        public static readonly Vector2Int Size = new(32, 46);

        [CanBeNull]
        private GridEntity _content;

        private MotionHandle _scaleHandle;
        private MotionHandle _flashHandle;
        private float _flashAmount;
        private MotionHandle _terrainPulseHandle;
        private float _terrainPulse;
        private SpriteRenderer _floorRenderer;

        // Long tap tracking (entity details, GDD 4.1).
        private bool _pressActive;
        private bool _longPressFired;
        private float _pressStartTime;

        public bool Selected { get; set; } = false;
        public bool Highlighted { get; set; } = false;
        public ECellHighlightType HighlightType { get; set; } = ECellHighlightType.Walk;

        public Vector2Int GridPosition { get; set; }

        /// <summary>
        /// Terrain of the cell (null = plain floor). Setting it updates the cell's
        /// look; the grid (GridController.SetTerrain) is the one that decides
        /// whether a terrain can be placed here.
        /// </summary>
        [CanBeNull]
        public TerrainDefinition Terrain
        {
            get => terrain;
            set
            {
                terrain = value;
                ApplyTerrainVisuals();
            }
        }

        /// <summary>Whether terrain blocks this cell (nobody occupies or crosses it).</summary>
        public bool IsBlocked => terrain != null && terrain.BlocksMovement;

        private SpriteRenderer FloorRenderer
        {
            get
            {
                if (_floorRenderer == null)
                    _floorRenderer = GetComponent<SpriteRenderer>();
                return _floorRenderer;
            }
        }

        private void Awake()
        {
            Assert.IsNotNull(selectionRenderer, "Selection renderer is not set!");
            Assert.IsNotNull(cellContentHealthBar, "Cell content health bar is not set!");
            Assert.IsNotNull(damageText, "Damage text is not set!");
            damageText.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<SkillUsedEvent>(OnSkillUsed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SkillUsedEvent>(OnSkillUsed);
            _pressActive = false;
            _longPressFired = false;
            if (_flashHandle.IsActive())
                _flashHandle.Cancel();
            _flashAmount = 0f;
            if (_terrainPulseHandle.IsActive())
                _terrainPulseHandle.Cancel();
            if (_terrainPulse > 0f)
            {
                _terrainPulse = 0f;
                ApplyTerrainVisuals();
            }
        }

        private void Update()
        {
            selectionRenderer.enabled = Selected;
            UpdateHighlight();
            UpdateLongPress();
        }

        /// <summary>
        /// A touch held on the cell for the configured time asks for the entity details instead of
        /// walking or attacking (the tap that ends it is suppressed in <see cref="OnPointerClick"/>).
        /// </summary>
        private void UpdateLongPress()
        {
            if (!_pressActive || _longPressFired) return;
            if (Time.unscaledTime - _pressStartTime < GameplayInputSettings.Current.LongPressSeconds) return;

            _pressActive = false;
            _longPressFired = true;
            RaiseLongPress();
        }

        /// <summary>
        /// Asks for the details of what is on this cell (<see cref="CellLongPressEvent"/>). Ignored
        /// while the game input is blocked (e.g. the details window is already open).
        /// </summary>
        public void RaiseLongPress()
        {
            if (GameplayInput.IsBlocked) return;

            EventBus.Raise(new CellLongPressEvent(this));
        }

        /// <summary>Flashes when a skill's area covers this cell (visual only).</summary>
        private void OnSkillUsed(SkillUsedEvent e)
        {
            foreach (var cell in e.AreaCells)
            {
                if (cell != GridPosition) continue;

                PlaySkillFlash();
                return;
            }
        }

        /// <summary>
        /// Visual only: highlights appearing in the same frame fade in one after
        /// another. The Highlighted flag itself (rules) is untouched.
        /// </summary>
        private void UpdateHighlight()
        {
            // A skill flash (visual only) takes over the highlight renderer while it fades.
            if (_flashAmount > 0f)
            {
                _wasHighlighted = false;
                var flash = skillAreaFlashColor;
                flash.a *= _flashAmount;
                highlightRenderer.color = flash;
                highlightRenderer.enabled = true;
                return;
            }

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
            var color = HighlightType switch
            {
                ECellHighlightType.Walk => walkHighlightColor,
                ECellHighlightType.SkillRange => skillRangeHighlightColor,
                _ => attackHighlightColor,
            };
            color.a *= alpha;
            highlightRenderer.color = color;
            highlightRenderer.enabled = Time.time >= _highlightStart;
        }

        private void HandleContentHpChanged(DamageReceiveData data)
        {
            cellContentHealthBar.UpdateHp(data.CurrentHp, data.MaxHp);

            if (data.Damage > 0)
                ShowFloatingText(data.Damage.ToString());
            else if (data.Healed > 0)
                ShowFloatingText($"+{data.Healed}");
        }

        private void ShowFloatingText(string text)
        {
            if (SimMode.IsActive) return;

            var damageTextInstance = Instantiate(damageText, damageText.transform.parent);
            damageTextInstance.text = text;
            damageTextInstance.gameObject.SetActive(true);

            var start = new Vector3(0, 0.23f, 0);
            var end = new Vector3(Random.Range(-0.25f, 0.25f), 0.4f, 0);
            var apex = new Vector3((start.x + end.x) / 2, Mathf.Max(start.y, end.y) + damageTextArcHeight, 0);
            LMotion.Create(0f, 1f, damageTextDuration)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(() => Destroy(damageTextInstance.gameObject))
                .Bind(t => damageTextInstance.transform.localPosition =
                    (1 - t) * (1 - t) * start + 2 * (1 - t) * t * apex + t * t * end)
                .AddTo(damageTextInstance.gameObject); // the board can be cleared (map) while the text still flies
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

        /// <summary>
        /// Brief fading flash with the skill area color, for cells hit by a skill.
        /// Visual only: rules never read it.
        /// </summary>
        public void PlaySkillFlash()
        {
            if (SimMode.IsActive) return;

            if (_flashHandle.IsActive())
                _flashHandle.Cancel();

            _flashAmount = 1f;
            _flashHandle = LMotion.Create(1f, 0f, skillFlashDuration)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(() => _flashAmount = 0f)
                .Bind(value => _flashAmount = value)
                .AddTo(gameObject);
        }

        /// <summary>
        /// Brief flash of the cell floor and terrain overlay when the terrain
        /// triggers on a character (visual only: rules never read it).
        /// </summary>
        public void PlayTerrainPulse()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || SimMode.IsActive) return;

            if (_terrainPulseHandle.IsActive())
                _terrainPulseHandle.Cancel();

            _terrainPulse = 1f;
            ApplyTerrainVisuals();
            _terrainPulseHandle = LMotion.Create(1f, 0f, terrainPulseDuration)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(() =>
                {
                    _terrainPulse = 0f;
                    ApplyTerrainVisuals();
                })
                .Bind(value =>
                {
                    _terrainPulse = value;
                    ApplyTerrainVisuals();
                })
                .AddTo(gameObject);
        }

        /// <summary>Shows the terrain: floor tint plus the overlay sprite (hidden when there is none).</summary>
        private void ApplyTerrainVisuals()
        {
            var floorColor = terrain != null ? terrain.FloorTint : Color.white;
            var overlayColor = terrain != null ? terrain.OverlayColor : Color.white;
            if (_terrainPulse > 0f)
            {
                floorColor = Color.Lerp(floorColor, terrainPulseColor, _terrainPulse);
                overlayColor = Color.Lerp(overlayColor, terrainPulseColor, _terrainPulse);
            }

            var floor = FloorRenderer;
            if (floor != null)
                floor.color = floorColor;

            if (terrainRenderer == null) return;

            var overlay = terrain != null ? terrain.OverlaySprite : null;
            terrainRenderer.sprite = overlay;
            terrainRenderer.color = overlayColor;
            terrainRenderer.enabled = overlay != null;
        }

        private void PlayScalePulse(float pixels, float duration, Ease inEase, Ease outEase)
        {
            if (SimMode.IsActive) return;

            if (_scaleHandle.IsActive())
                _scaleHandle.Cancel();

            var start = Vector3.one;
            var end = Vector3.one - Vector3.one * pixels / GameConfigManager.Ppu;
            _scaleHandle = LSequence.Create()
                .Append(LMotion.Create(start, end, duration).WithEase(inEase)
                    .Bind(x => SetPulseScale(x)))
                .Append(LMotion.Create(end, start, duration).WithEase(outEase)
                    .Bind(x => SetPulseScale(x)))
                .Run();
        }

        /// <summary>Scale step of a pulse; ignored if the cell was destroyed meanwhile (the grid was rebuilt).</summary>
        private void SetPulseScale(Vector3 scale)
        {
            if (this != null)
                transform.localScale = scale;
        }

        /// <summary>
        /// Left click / short tap: walk or attack (<see cref="CellTapEvent"/>). Right click: details.
        /// The release of a long tap is swallowed, so it does not also walk or attack.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_longPressFired)
            {
                _longPressFired = false;
                return;
            }

            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    EventBus.Raise(new CellTapEvent
                    {
                        Cell = this
                    });
                    break;
                case PointerEventData.InputButton.Right:
                    RaiseLongPress();
                    break;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _longPressFired = false;

            // Only touches long press; the mouse opens the details with the right button.
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (eventData is ExtendedPointerEventData { pointerType: UIPointerType.MouseOrPen }) return;

            _pressActive = true;
            _pressStartTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressActive = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Sliding off the cell cancels the press (no click will follow).
            _pressActive = false;
            _longPressFired = false;
        }
    }
}
