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
            var hpInternalTransform = hpInternalSpriteRenderer.transform;
            hpInternalTransform.localScale = new Vector3(
                Mathf.Max(0f, (float)newHp / maxHp * hpMaxScale),
                hpInternalTransform.localScale.y, hpInternalTransform.localScale.z
            );
        }
    }
}