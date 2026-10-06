using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Talents
{
    /// <summary>
    /// A synergy tag (talentos_e_oferta, "Oferta"): a theme such as Offense, Critical or Sustain that talents
    /// carry. The offer draw weighs a talent up when its tags appear in the talents the player already took
    /// (see <see cref="TalentOfferSettings"/>), and the choice screen highlights the options that "combine with
    /// the build" using the tags they share.
    /// </summary>
    [CreateAssetMenu(fileName = "SynergyTag", menuName = "GridBattle/Talents/Synergy Tag", order = 1)]
    public sealed class SynergyTagDefinition : DisplayableDefinition
    {
        [Header("Presentation")]
        [SerializeField]
        [Tooltip("Accent color the choice screen uses for this tag.")]
        private Color color = Color.white;

        public Color Color => color;
    }
}
