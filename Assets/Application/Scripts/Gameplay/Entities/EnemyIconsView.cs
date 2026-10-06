using System.Collections.Generic;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Managers;
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
    /// whatever the art resolution.
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

        [Header("Size")]
        [SerializeField]
        [Min(1)]
        [Tooltip("Target on-screen size of the icons in pixels. Sprites are only scaled by whole numbers " +
                 "(pixel perfect): an 8x8 sprite is shown at 8, 16, 24... Use icons drawn at the size you want.")]
        private int iconSizePixels = 8;

        private Enemy _enemy;

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
            _enemy = null;
        }

        private void OnHpChanged(DamageReceiveData data)
        {
            if (_enemy != null && _enemy.IsDead)
                Refresh();
        }

        private void Refresh()
        {
            if (_enemy == null || _enemy.IsDead)
            {
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
