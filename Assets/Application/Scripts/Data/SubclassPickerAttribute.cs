using UnityEngine;

namespace GridBattle.Data
{
    /// <summary>
    /// Use on a [SerializeReference] field (or list) to pick the concrete
    /// subclass in the Inspector. Lets designers compose effects (state effects,
    /// skill effects, consumable effects...) without code.
    /// </summary>
    public sealed class SubclassPickerAttribute : PropertyAttribute
    {
    }
}
