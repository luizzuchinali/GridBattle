using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Entities.Skills;
using UnityEngine;

namespace GridBattle.Gameplay.Consumables
{
    /// <summary>How the player aims a consumable.</summary>
    public enum EConsumableTargeting
    {
        /// <summary>Used immediately on the user, with no target.</summary>
        Self,

        /// <summary>Needs a target cell (like a skill): the item is aimed within its range and hits an area around it.</summary>
        Cell
    }

    /// <summary>
    /// A consumable (GDD 2.8): a single-use item obtained at the consumable node,
    /// carried in the item bar and used only in battle, without consuming the
    /// turn's action. Fully data-driven: targeting, range, area and who is
    /// affected are Inspector fields, and what the item does is composed from
    /// <see cref="ConsumableEffect"/>s (heal, damage, states). Assets are shared and
    /// never hold runtime state (the carried items live in <see cref="ConsumableInventory"/>).
    /// </summary>
    [CreateAssetMenu(fileName = "Consumable", menuName = "GridBattle/Consumables/Consumable Definition", order = 0)]
    public class ConsumableDefinition : DisplayableDefinition
    {
        public const int MinAreaSize = SkillDefinition.MinAreaSize;
        public const int MaxAreaSize = SkillDefinition.MaxAreaSize;

        [Header("Targeting")]
        [SerializeField]
        private EConsumableTargeting targeting = EConsumableTargeting.Self;

        [SerializeField]
        [Min(0)]
        [Tooltip("Cell targeting: maximum distance (skill range metric, Manhattan by default) from the user to the " +
                 "target cell. 0 = centered on the user's own cell.")]
        private int range = 3;

        [SerializeField]
        [Tooltip("Cell targeting: the target cell can be anywhere on the grid (ignores Range).")]
        private bool unlimitedRange;

        [Header("Area of effect (Cell targeting)")]
        [SerializeField]
        private ESkillAreaShape areaShape = ESkillAreaShape.Circle;

        [SerializeField]
        [Range(MinAreaSize, MaxAreaSize)]
        [Tooltip("Size of the area (odd number, 1 to 9). 1 = only the target cell.")]
        private int areaSize = 1;

        [SerializeField]
        [Tooltip("Cell targeting: which characters inside the area the effects reach. Items that target the user " +
                 "always affect only the user.")]
        private ESkillTargetFilter affects = ESkillTargetFilter.Enemies;

        [Header("Effects")]
        [SerializeReference]
        [SubclassPicker]
        [Tooltip("What the item does, applied in order. The item can only be used when at least one effect " +
                 "would change something (e.g. damage needs a character in the area).")]
        private List<ConsumableEffect> effects = new();

        [Header("Obtaining")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Relative chance of being granted by the consumable node (0 = never drawn). Only matters for " +
                 "items listed in the Consumable Settings pool.")]
        private float dropWeight = 1f;

        [SerializeField]
        [Tooltip("Open question (consumiveis 2.8): whether the item appears in the in-game glossary.")]
        private bool showInGlossary;

        public EConsumableTargeting Targeting => targeting;
        public int Range => range;
        public bool UnlimitedRange => unlimitedRange;
        public ESkillAreaShape AreaShape => areaShape;
        public int AreaSize => areaSize;
        public ESkillTargetFilter Affects => affects;
        public IReadOnlyList<ConsumableEffect> Effects => effects;
        public float DropWeight => dropWeight;
        public bool ShowInGlossary => showInGlossary;

        /// <summary>True when the player picks a target cell (<see cref="EConsumableTargeting.Cell"/>).</summary>
        public bool NeedsTarget => targeting == EConsumableTargeting.Cell;

        /// <summary>True for Cell items centered on the user (range 0, not unlimited).</summary>
        public bool IsSelfCentered => !unlimitedRange && range == 0;

        /// <summary>
        /// Whether at least one effect would change something for this resolved use
        /// (see <see cref="ConsumableEffect.IsUseful"/>). Items without effects are never usable.
        /// </summary>
        public virtual bool IsUseful(in ConsumableContext context)
        {
            foreach (var effect in effects)
            {
                if (effect != null && effect.IsUseful(context))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Applies every effect, in order. Validation (turn, limit, target) is done by
        /// <see cref="ConsumableRules"/> before; removing the item and the events are
        /// done by <see cref="ConsumableExecutor"/>. Special items can subclass and override.
        /// </summary>
        public virtual void Execute(in ConsumableContext context)
        {
            foreach (var effect in effects)
            {
                if (effect != null)
                    effect.Apply(context);
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            areaSize = Mathf.Clamp(areaSize, MinAreaSize, MaxAreaSize);
            if (areaSize % 2 == 0)
                areaSize++;
        }
#endif
    }
}
