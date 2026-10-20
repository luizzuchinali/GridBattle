using UnityEngine;

namespace GridBattle.Gameplay.Feel
{
    /// <summary>
    /// Paints a character in one solid color for an instant (the white silhouette of a hit): a child sprite drawn
    /// right over the character with <see cref="GameFeelSettings.FlashMaterial"/>, which copies the character's
    /// current animation frame while it shows. Added on the first flash. Its timer runs on game time, so a hit stop
    /// holds the silhouette on screen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteFlash : MonoBehaviour
    {
        private SpriteRenderer _source;
        private SpriteRenderer _overlay;
        private float _remaining;

        /// <summary>
        /// Shows <paramref name="color"/> over the sprite of <paramref name="target"/> for <paramref name="duration"/>
        /// seconds. Does nothing without a flash material.
        /// </summary>
        public static void Play(Component target, Color color, float duration)
        {
            var settings = GameFeelSettings.Current;
            if (target == null || duration <= 0f || settings.FlashMaterial == null) return;
            if (!target.TryGetComponent(out SpriteRenderer _)) return;

            if (!target.TryGetComponent(out SpriteFlash flash))
                flash = target.gameObject.AddComponent<SpriteFlash>();
            flash.Show(color, duration, settings.FlashMaterial);
        }

        private void Show(Color color, float duration, Material material)
        {
            if (_source == null)
                _source = GetComponent<SpriteRenderer>();

            if (_overlay == null)
            {
                var child = new GameObject("Flash");
                child.transform.SetParent(transform, false);
                _overlay = child.AddComponent<SpriteRenderer>();
            }

            _overlay.sharedMaterial = material;
            _overlay.color = color;
            _remaining = duration;
            Sync();
            _overlay.enabled = true;
            enabled = true;
        }

        private void LateUpdate()
        {
            if (_overlay == null || _source == null)
            {
                enabled = false;
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _overlay.enabled = false;
                enabled = false;
                return;
            }

            Sync();
        }

        /// <summary>Same frame, facing and draw order as the character, one step above it.</summary>
        private void Sync()
        {
            _overlay.sprite = _source.sprite;
            _overlay.flipX = _source.flipX;
            _overlay.flipY = _source.flipY;
            _overlay.sortingLayerID = _source.sortingLayerID;
            _overlay.sortingOrder = _source.sortingOrder + 1;

            // A dying character fades: the silhouette fades with it.
            var color = _overlay.color;
            color.a = Mathf.Min(1f, _source.color.a);
            _overlay.color = color;
        }
    }
}
