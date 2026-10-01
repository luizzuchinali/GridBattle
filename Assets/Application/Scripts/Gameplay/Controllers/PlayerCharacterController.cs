using System.Collections.Generic;
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
        private static readonly Dictionary<Vector2Int, ECellHighlightType> NoHighlights = new();

        private bool _isMyTurn = true;

        protected override void Awake()
        {
            base.Awake();
            EventBus.Subscribe<CellTapEvent>(OnCellTap);
            EventBus.Subscribe<TurnChangedEvent>(OnTurnChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CellTapEvent>(OnCellTap);
            EventBus.Unsubscribe<TurnChangedEvent>(OnTurnChanged);
        }

        private void Update()
        {
            if (!_isMyTurn) return;

            Grid.HighlightCells(GridRules.GetHighlightInfos(Grid, Owner));
        }

        /// <summary>
        /// The player only acts, and only sees highlights, on its own turn.
        /// </summary>
        private void OnTurnChanged(TurnChangedEvent e)
        {
            _isMyTurn = e.IsPlayerTurn;
            if (!_isMyTurn)
                Grid.HighlightCells(NoHighlights);
        }

        private void OnCellTap(CellTapEvent @event)
        {
            if (!_isMyTurn)
            {
                @event.Cell.PlayRejectFeedback();
                return;
            }

            if (!@event.Cell.HasContent)
            {
                if (!GridRules.CanWalkTo(Grid, Owner, @event.Cell.GridPosition))
                {
                    @event.Cell.PlayRejectFeedback();
                    return;
                }

                Grid.MoveEntity(Owner, @event.Cell.GridPosition);
            }
            else
            {
                if (!GridRules.IsAttackTarget(Grid, Owner, @event.Cell.GridPosition))
                {
                    @event.Cell.PlayRejectFeedback();
                    return;
                }

                var target = @event.Cell.GetContent();
                if (target is IDamageReceiver receiver)
                {
                    Grid.PlayAttackAnimation(Owner, @event.Cell.GridPosition);
                    Owner.Attack(receiver);
                }
            }

            EventBus.Raise<PlayerActionEvent>();
        }

        /// <summary>
        /// Entry point for using a skill (to be called by the UI). Validation and
        /// effect live in SkillDefinition / PlayerCharacter, not here.
        /// </summary>
        public bool TryUseSkill(SkillDefinition skill, Vector2Int targetPos)
        {
            if (!_isMyTurn) return false;
            if (!Owner.TryUseSkill(Grid, skill, targetPos)) return false;

            EventBus.Raise<PlayerActionEvent>();
            return true;
        }
    }
}
