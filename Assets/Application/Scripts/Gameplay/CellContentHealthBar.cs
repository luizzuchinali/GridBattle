using GridBattle.Gameplay.Forecast;
using UnityEngine;
using UnityEngine.Assertions;

namespace GridBattle.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CellContentHealthBar : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer hpSpriteRenderer;

        [SerializeField]
        private SpriteRenderer hpInternalSpriteRenderer;

        [SerializeField]
        private int hpMaxScale;

        private int _hp;
        private int _maxHp = 1;
        private int _preview;

        private void Awake()
        {
            Assert.IsNotNull(hpSpriteRenderer);
            Assert.IsNotNull(hpInternalSpriteRenderer);
            Hide();
        }

        public void Hide()
        {
            hpSpriteRenderer.gameObject.SetActive(false);
        }

        public void Show()
        {
            hpSpriteRenderer.gameObject.SetActive(true);
        }

        public void UpdateHp(int newHp, int maxHp)
        {
            _hp = Mathf.Max(0, newHp);
            _maxHp = Mathf.Max(1, maxHp);
            ApplyFill(_hp);
        }

        /// <summary>
        /// The damage a forecast hit would deal (0 = none): the part of the bar it would take away blinks. Visual only.
        /// </summary>
        public void SetPreview(int damage)
        {
            _preview = Mathf.Max(0, damage);
            if (_preview == 0)
                ApplyFill(_hp);
        }

        private void Update()
        {
            if (_preview <= 0) return;

            // Half of each period shows the HP now, the other half what would be left.
            var period = ForecastSettings.Current.BlinkPeriod;
            var showLoss = Time.unscaledTime % period >= period * 0.5f;
            ApplyFill(showLoss ? Mathf.Max(0, _hp - _preview) : _hp);
        }

        private void ApplyFill(int hp)
        {
            var hpInternalTransform = hpInternalSpriteRenderer.transform;
            hpInternalTransform.localScale = new Vector3(
                Mathf.Max(0f, (float)hp / _maxHp * hpMaxScale),
                hpInternalTransform.localScale.y, hpInternalTransform.localScale.z
            );
        }
    }
}
