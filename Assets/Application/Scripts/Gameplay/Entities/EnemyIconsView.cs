using System.Collections.Generic;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Simulation;
using GridBattle.Managers;
using TMPro;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    /// <summary>
    /// Icons shown over an enemy: its role (GDD Mechanic 4) and up to a few of
    /// its permanent states (Mechanic 6: armored, regenerating, thorny...), so the
    /// player can read what the enemy demands from the build. The icons are
    /// children of the enemy prefab; the sprites come from the enemy's
    /// <see cref="EnemyConfig"/> and its states, and update when the states change.
    /// Sizes and offsets are in screen pixels so the icons stay pixel perfect
    /// whatever the art resolution. In the top left corner of its cell, on a small dark plate, its place in the acting
    /// order of the enemy phase (<see cref="SetActingOrder"/>). While the player inspects it (<see cref="SetFocused"/>),
    /// the icon of the telegraphed skill it prepares shows over its head on a plate too, with the turns left until it
    /// fires when that is more than one. Everything stays inside the enemy's own cell except that inspected badge, so
    /// nothing covers the health bar of a neighbour (created the first time they are needed).
    /// </summary>
    public class EnemyIconsView : MonoBehaviour
    {
        [Header("Role icon")]
        [SerializeField]
        private SpriteRenderer roleIcon;

        [SerializeField]
        [Tooltip("Offset of the role icon center from the entity pivot (feet), in pixels.")]
        private Vector2 roleIconOffset = new(12f, 30f);

        [Header("State icons")]
        [SerializeField]
        [Tooltip("One renderer per slot; permanent states fill the slots in the order they were applied.")]
        private List<SpriteRenderer> stateIcons = new();

        [SerializeField]
        [Tooltip("Offset of each state icon center from the entity pivot (feet), in pixels. One per slot.")]
        private List<Vector2> stateIconOffsets = new() { new Vector2(12f, 22f), new Vector2(12f, 14f) };

        [Header("Badges: acting order and prepared strike")]
        [SerializeField]
        [Tooltip("Offset of the enemy's place in the acting order (1 = acts first), from the entity pivot (feet), in " +
                 "pixels. Keep it inside the enemy's cell.")]
        private Vector2 actingOrderOffset = new(-12f, 29f);

        [SerializeField]
        [Tooltip("Offset of the icon of the skill being prepared (shown while the enemy is inspected), from the entity " +
                 "pivot (feet), in pixels.")]
        private Vector2 telegraphIconOffset = new(0f, 40f);

        [SerializeField]
        [Tooltip("Offset of the number of turns left until the prepared skill fires, in pixels. Shown only when it " +
                 "fires in more than one turn.")]
        private Vector2 telegraphTurnsOffset = new(11f, 40f);

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Font size of the number of turns (world text, like the damage numbers).")]
        private float telegraphTurnsFontSize = 1.5f;

        [SerializeField]
        private Color telegraphTurnsColor = new(1f, 0.85f, 0.1f, 1f);

        [SerializeField]
        [Tooltip("Color of the plates behind the badges.")]
        private Color badgeColor = new(0.094f, 0.078f, 0.145f, 0.85f);

        [SerializeField]
        [Min(0)]
        [Tooltip("Pixels of plate around the pieces of a badge.")]
        private int badgePadding = 1;

        [SerializeField]
        [Tooltip("Size in pixels of one character of the badge numbers (to fit the plates around them).")]
        private Vector2Int badgeDigitSize = new(6, 8);

        [Header("Size")]
        [SerializeField]
        [Min(1)]
        [Tooltip("Target on-screen size of the icons in pixels. Sprites are only scaled by whole numbers " +
                 "(pixel perfect): an 8x8 sprite is shown at 8, 16, 24... Use icons drawn at the size you want.")]
        private int iconSizePixels = 8;

        private const string FontResource = "Fonts & Materials/ThaleahFat_TTF SDF";

        private Enemy _enemy;
        private SpriteRenderer _telegraphIcon;
        private SpriteRenderer _orderPlate;
        private SpriteRenderer _strikePlate;
        private bool _focused;
        private TextMeshPro _telegraphTurns;
        private TextMeshPro _actingOrder;
        private int _order;

        /// <summary>
        /// Starts showing the icons of <paramref name="enemy"/> (its config must be
        /// applied already) and keeps them in sync with its states and life.
        /// </summary>
        public void Bind(Enemy enemy)
        {
            Unbind();
            _enemy = enemy;
            if (_enemy == null) return;

            _enemy.States.Changed += Refresh;
            _enemy.OnHpChanged += OnHpChanged;
            EventBus.Subscribe<SkillTelegraphChangedEvent>(OnTelegraphChanged);
            Refresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (_enemy == null) return;

            _enemy.States.Changed -= Refresh;
            _enemy.OnHpChanged -= OnHpChanged;
            EventBus.Unsubscribe<SkillTelegraphChangedEvent>(OnTelegraphChanged);
            _enemy = null;
        }

        private void OnHpChanged(DamageReceiveData data)
        {
            if (_enemy != null && _enemy.IsDead)
                Refresh();
        }

        private void OnTelegraphChanged(SkillTelegraphChangedEvent e)
        {
            if (e.Caster == _enemy)
                RefreshTelegraph();
        }

        private void Refresh()
        {
            RefreshTelegraph();
            if (_enemy == null || _enemy.IsDead)
            {
                SetActingOrder(0);
                Hide(roleIcon);
                foreach (var icon in stateIcons)
                    Hide(icon);
                return;
            }

            var role = _enemy.Role;
            Show(roleIcon, role != null ? role.Icon : null, roleIconOffset);

            var slot = 0;
            foreach (var state in _enemy.States.All)
            {
                if (slot >= stateIcons.Count) break;

                var definition = state.Definition;
                if (!state.IsPermanent || !definition.ShowInDetails || definition.Icon == null) continue;

                var offset = slot < stateIconOffsets.Count ? stateIconOffsets[slot] : Vector2.zero;
                Show(stateIcons[slot], definition.Icon, offset);
                slot++;
            }

            for (; slot < stateIcons.Count; slot++)
                Hide(stateIcons[slot]);
        }

        /// <summary>
        /// Shows the enemy's place in the acting order of the enemy phase (1 = acts first; 0 hides it). Set by the
        /// forecast view (<see cref="Forecast.ForecastView"/>). Visual only.
        /// </summary>
        public void SetActingOrder(int order)
        {
            _order = SimMode.IsActive || _enemy == null || _enemy.IsDead ? 0 : Mathf.Max(0, order);
            if (_order == 0)
            {
                if (_actingOrder != null)
                    _actingOrder.gameObject.SetActive(false);
                RefreshPlates();
                return;
            }

            if (_actingOrder == null)
                _actingOrder = CreateText("ActingOrder", Forecast.ForecastSettings.Current.OrderFontSize,
                    Forecast.ForecastSettings.Current.OrderColor);

            var ppu = GameConfigManager.Ppu;
            _actingOrder.transform.localPosition = new Vector3(actingOrderOffset.x / ppu, actingOrderOffset.y / ppu, 0f);
            _actingOrder.text = _order.ToString();
            _actingOrder.gameObject.SetActive(true);
            RefreshPlates();
        }

        /// <summary>
        /// Whether the player inspects this enemy (taps it): its prepared strike's icon shows over its head. Set by the
        /// forecast view (<see cref="Forecast.ForecastView"/>). Visual only.
        /// </summary>
        public void SetFocused(bool focused)
        {
            if (_focused == focused) return;

            _focused = focused;
            RefreshTelegraph();
        }

        /// <summary>
        /// The icon and turns left of the telegraphed skill being prepared, while the enemy is inspected (hidden when it
        /// is not or there is none).
        /// </summary>
        private void RefreshTelegraph()
        {
            var telegraph = _enemy != null && !_enemy.IsDead && _focused ? _enemy.Telegraph : null;
            if (telegraph == null || SimMode.IsActive)
            {
                Hide(_telegraphIcon);
                if (_telegraphTurns != null)
                    _telegraphTurns.gameObject.SetActive(false);
                RefreshPlates();
                return;
            }

            EnsureTelegraphViews();
            Show(_telegraphIcon, telegraph.Skill.Icon, telegraphIconOffset);

            if (_telegraphTurns != null)
            {
                // "1" (fires on its next turn) is the rule for every strike: only a longer windup is worth a number.
                var ppu = GameConfigManager.Ppu;
                _telegraphTurns.transform.localPosition =
                    new Vector3(telegraphTurnsOffset.x / ppu, telegraphTurnsOffset.y / ppu, 0f);
                _telegraphTurns.text = telegraph.TurnsLeft.ToString();
                _telegraphTurns.gameObject.SetActive(telegraph.TurnsLeft > 1);
            }

            RefreshPlates();
        }

        /// <summary>
        /// The dark plates behind the badges (the acting order; the strike icon with its turns), fitted around what
        /// they show with whole pixel edges, and hidden when they show nothing.
        /// </summary>
        private void RefreshPlates()
        {
            var order = new PlateBounds();
            if (_actingOrder != null && _actingOrder.gameObject.activeSelf)
                order.Include(actingOrderOffset, DigitsSize(_actingOrder.text));

            var strike = new PlateBounds();
            if (_telegraphIcon != null && _telegraphIcon.enabled && _telegraphIcon.sprite != null)
            {
                var rect = _telegraphIcon.sprite.rect;
                var factor = Mathf.Max(1, Mathf.RoundToInt(iconSizePixels / rect.width));
                strike.Include(telegraphIconOffset, new Vector2(rect.width, rect.height) * factor);
            }

            if (_telegraphTurns != null && _telegraphTurns.gameObject.activeSelf)
                strike.Include(telegraphTurnsOffset, DigitsSize(_telegraphTurns.text));

            FitPlate(ref _orderPlate, "OrderPlate", order);
            FitPlate(ref _strikePlate, "StrikePlate", strike);
        }

        private Vector2 DigitsSize(string text) => new(badgeDigitSize.x * text.Length, badgeDigitSize.y);

        private void FitPlate(ref SpriteRenderer plate, string objectName, PlateBounds bounds)
        {
            if (bounds.IsEmpty || SimMode.IsActive)
            {
                Hide(plate);
                return;
            }

            if (plate == null)
            {
                var template = roleIcon != null ? roleIcon : stateIcons.Count > 0 ? stateIcons[0] : null;
                if (template == null) return;

                var go = new GameObject(objectName);
                go.transform.SetParent(template.transform.parent, false);
                plate = go.AddComponent<SpriteRenderer>();
                plate.sharedMaterial = template.sharedMaterial;
                plate.sortingLayerID = template.sortingLayerID;
                // Under the pieces of the badge, which draw one order above the other icons.
                plate.sortingOrder = template.sortingOrder;
            }

            var left = Mathf.FloorToInt(bounds.Min.x) - badgePadding;
            var bottom = Mathf.FloorToInt(bounds.Min.y) - badgePadding;
            var right = Mathf.CeilToInt(bounds.Max.x) + badgePadding;
            var top = Mathf.CeilToInt(bounds.Max.y) + badgePadding;
            plate.sprite = Forecast.ForecastSprites.GetPlate(right - left, top - bottom);
            plate.color = badgeColor;
            plate.transform.localScale = Vector3.one;
            var ppu = GameConfigManager.Ppu;
            plate.transform.localPosition = new Vector3((left + right) * 0.5f / ppu, (bottom + top) * 0.5f / ppu, 0f);
            plate.enabled = true;
        }

        /// <summary>The pixel box around the pieces of one badge.</summary>
        private struct PlateBounds
        {
            public Vector2 Min;
            public Vector2 Max;
            private bool _any;

            public bool IsEmpty => !_any;

            public void Include(Vector2 center, Vector2 size)
            {
                var low = center - size * 0.5f;
                var high = center + size * 0.5f;
                Min = _any ? Vector2.Min(Min, low) : low;
                Max = _any ? Vector2.Max(Max, high) : high;
                _any = true;
            }
        }

        private void EnsureTelegraphViews()
        {
            var template = roleIcon != null ? roleIcon : stateIcons.Count > 0 ? stateIcons[0] : null;
            if (_telegraphIcon == null && template != null)
            {
                var icon = new GameObject("TelegraphIcon");
                icon.transform.SetParent(template.transform.parent, false);
                _telegraphIcon = icon.AddComponent<SpriteRenderer>();
                _telegraphIcon.sharedMaterial = template.sharedMaterial;
                _telegraphIcon.sortingLayerID = template.sortingLayerID;
                _telegraphIcon.sortingOrder = template.sortingOrder + 1;
            }

            if (_telegraphTurns == null)
                _telegraphTurns = CreateText("TelegraphTurns", telegraphTurnsFontSize, telegraphTurnsColor);
        }

        /// <summary>A small world text next to the icons (same parent and layer as them, one order above).</summary>
        private TextMeshPro CreateText(string objectName, float fontSize, Color color)
        {
            var template = roleIcon != null ? roleIcon : stateIcons.Count > 0 ? stateIcons[0] : null;
            var go = new GameObject(objectName);
            go.transform.SetParent(template != null ? template.transform.parent : transform, false);
            var text = go.AddComponent<TextMeshPro>();
            var font = Resources.Load<TMP_FontAsset>(FontResource);
            if (font != null)
                text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(0.3f, 0.3f);
            text.sortingLayerID = template != null ? template.sortingLayerID : 0;
            // Over the badge plate, which shares the order of the other icons.
            text.sortingOrder = (template != null ? template.sortingOrder : 100) + 1;
            return text;
        }

        private void Show(SpriteRenderer renderer, Sprite sprite, Vector2 offsetPixels)
        {
            if (renderer == null) return;
            if (sprite == null)
            {
                Hide(renderer);
                return;
            }

            renderer.sprite = sprite;
            renderer.enabled = true;

            var ppu = GameConfigManager.Ppu;
            var iconTransform = renderer.transform;
            iconTransform.localPosition = new Vector3(offsetPixels.x / ppu, offsetPixels.y / ppu, 0f);

            // Whole-number magnification only, so every sprite pixel covers the same
            // number of screen pixels (fractional scales break pixel art).
            var factor = Mathf.Max(1, Mathf.RoundToInt(iconSizePixels / sprite.rect.width));
            var scale = factor * sprite.pixelsPerUnit / ppu;
            iconTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private static void Hide(SpriteRenderer renderer)
        {
            if (renderer != null)
                renderer.enabled = false;
        }
    }
}
