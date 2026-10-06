using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    /// <summary>
    /// Visual part of the character: applies the sprite and animations defined in
    /// the config to the prefab template. Future visual reactions (damage, attack,
    /// death) belong to this component, not to Character.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public class CharacterView : MonoBehaviour
    {
        public void Apply(CharacterConfig config)
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = config.Sprite;
            spriteRenderer.color = config.Tint;
            GetComponent<Animator>().runtimeAnimatorController = config.AnimatorController;
        }
    }
}
