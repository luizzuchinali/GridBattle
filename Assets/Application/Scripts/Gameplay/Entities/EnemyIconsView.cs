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
    /// whatever the art resolution. While the enemy prepares a telegraphed skill, the skill icon and the turns
    /// left until it fires (1 = on its next turn) show over its head (created the first time they are needed), and so
    /// does its place in the acting order of the enemy phase (<see cref="SetActingOrder"/>).
    /// </summary>
    public class EnemyIconsView : MonoBehaviour
    {
        [Header("Role icon")]
        [SerializeField]
        private SpriteRenderer roleIcon;

        [SerializeField]
        [Tooltip("Offset of the role icon center from the entity pivot (feet), in pixels.")]
        private Vector2 roleIconOffset = new(-12f, 30f);

        [Header("State icons")]
        [SerializeField]
        [Tooltip("One renderer per slot; permanent states fill the slots in the order they were applied.")]
        private List<SpriteRenderer> stateIcons = new();

        [SerializeField]
        [Tooltip("Offset of each state icon center from the entity pivot (feet), in pixels. One per slot.")]
        private List<Vector2> stateIconOffsets = new() { new Vector2(12f, 30f), new Vector2(12f, 22f) };

        [Header("Telegraphed skill")]
        [SerializeField]
        [Tooltip("Offset of the icon of the skill being prepared, from the entity pivot (feet), in pixels.")]
        private Vector2 telegraphIconOffset = new(-4f, 40f);

        [SerializeField]
        [Tooltip("Offset of the number of turns left until the prepared skill fires, in pixels.")]
        private Vector2 telegraphTurnsOffset = new(6f, 40f);

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("Font size of the number of turns (world text, like the damage numbers).")]
        private float telegraphTurnsFontSize = 1.5f;

        [SerializeField]
        private Color telegraphTurnsColor = new(1f, 0.85f, 0.1f, 1f);

        [Header("Acting order")]
        [SerializeField]
        [Tooltip("Offset of the enemy's place in the acting order (1 = acts first), from the entity pivot (feet), in pixels.")]
        private Vector2 actingOrderOffset = new(-14f, 40f);

        [Header("Size")]
        [SerializeField]
        [Min(1)]
        [Tooltip("Target on-screen size of the icons in pixels. Sprites are only scaled by whole numbers " +
                 "(pixel perfect): an 8x8 sprite is shown at 8, 16, 24... Use icons drawn at the size you want.")]
        private int iconSizePixels = 8;

        private const string FontResource = "Fonts & Materials/ThaleahFat_TTF SDF";

        private Enemy _enemy;
        private SpriteRenderer _telegraphIcon;
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
                return;
            }

            if (_actingOrder == null)
                _actingOrder = CreateText("ActingOrder", Forecast.ForecastSettings.Current.OrderFontSize,
                    Forecast.ForecastSettings.Current.OrderColor);

            var ppu = GameConfigManager.Ppu;
            _actingOrder.transform.localPosition = new Vector3(actingOrderOffset.x / ppu, actingOrderOffset.y / ppu, 0f);
            _actingOrder.text = _order.ToString();
            _actingOrder.gameObject.SetActive(true);
        }

        /// <summary>The icon and turns left of the telegraphed skill being prepared (hidden when there is none).</summary>
        private void RefreshTelegraph()
        {
            var telegraph = _enemy != null && !_enemy.IsDead ? _enemy.Telegraph : null;
            if (telegraph == null || SimMode.IsActive)
            {
                Hide(_telegraphIcon);
                if (_telegraphTurns != null)
                    _telegraphTurns.gameObject.SetActive(false);
                return;
            }

            EnsureTelegraphViews();
            Show(_telegraphIcon, telegraph.Skill.Icon, telegraphIconOffset);

            if (_telegraphTurns == null) return;

            var ppu = GameConfigManager.Ppu;
            _telegraphTurns.transform.localPosition =
                new Vector3(telegraphTurnsOffset.x / ppu, telegraphTurnsOffset.y / ppu, 0f);
            _telegraphTurns.text = telegraph.TurnsLeft.ToString();
            _telegraphTurns.gameObject.SetActive(true);
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
                _telegraphIcon.sortingOrder = template.sortingOrder;
            }

            if (_telegraphTurns == null)
                _telegraphTurns = CreateText("TelegraphTurns", telegraphTurnsFontSize, telegraphTurnsColor);
        }

        /// <summary>A small world text next to the icons (same parent, layer and order as them).</summary>
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
            text.sortingOrder = template != null ? template.sortingOrder : 100;
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
