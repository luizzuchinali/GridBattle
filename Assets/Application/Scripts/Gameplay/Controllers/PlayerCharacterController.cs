using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Entities.Interfaces;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Rules;
using UnityEngine;

namespace GridBattle.Gameplay.Controllers
{
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterController : CharacterControllerBase<PlayerCharacter>
    {
        protected override void Awake()
        {
            base.Awake();
            EventBus.Subscribe<CellTapEvent>(OnCellTap);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CellTapEvent>(OnCellTap);
        }

        private void Update()
        {
            Grid.HighlightCells(GridRules.GetHighlightInfos(Grid, Owner));
        }

        private void OnCellTap(CellTapEvent @event)
        {
            if (!@event.Cell.HasContent)
            {
                if (!GridRules.CanWalkTo(Grid, Owner, @event.Cell.GridPosition)) return;

                Grid.MoveEntity(Owner, @event.Cell.GridPosition);
            }
            else
            {
                if (!GridRules.IsAttackTarget(Grid, Owner, @event.Cell.GridPosition)) return;

                var target = @event.Cell.GetContent();
                if (target is IDamageReceiver receiver)
                    Owner.Attack(receiver);
            }

            EventBus.Raise<PlayerActionEvent>();
        }

        /// <summary>
        /// Entry point for using a skill (to be called by the UI). Validation and
        /// effect live in SkillDefinition / PlayerCharacter, not here.
        /// </summary>
        public bool TryUseSkill(SkillDefinition skill, Vector2Int targetPos)
        {
            if (!Owner.TryUseSkill(Grid, skill, targetPos)) return false;

            EventBus.Raise<PlayerActionEvent>();
            return true;
        }
    }
}
