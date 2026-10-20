using GridBattle.Gameplay.Entities.Roles;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.Run;
using GridBattle.Gameplay.States;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    /// <summary>
    /// Enemy: attributes, behavior (AI) and reward come from the
    /// <see cref="Entities.EnemyConfig"/>. Creating a new enemy means creating an
    /// EnemyConfig asset, not a prefab.
    /// <para>
    /// Telegraphed skills (<see cref="SkillDefinition.Telegraphed"/>): using one prepares it instead
    /// (<see cref="Telegraph"/>, the cells of its area are fixed and marked), and it fires on a later turn of the
    /// enemy (<see cref="AdvanceTelegraph"/>, called by the controller at the start of its turn). Dying, a state
    /// that prevents skills or (when the skill says so) a push or pull cancels it; otherwise a push, pull or swap
    /// moves the marked cells with the enemy (<see cref="SkillDefinition.TelegraphFollowsCaster"/>).
    /// </para>
    /// </summary>
    public class Enemy : Character
    {
        private int? _xpRewardOverride;
        private SkillTelegraph _telegraph;

        /// <summary>The telegraphed skill this enemy is preparing, or null.</summary>
        [CanBeNull]
        public SkillTelegraph Telegraph => _telegraph;

        /// <summary>Whether this enemy is preparing a telegraphed skill.</summary>
        public bool IsWindingUp => _telegraph != null;

        public EnemyConfig EnemyConfig => Config as EnemyConfig;

        /// <summary>Role of this enemy (from its config), or null.</summary>
        [CanBeNull]
        public EnemyRoleDefinition Role => EnemyConfig != null ? EnemyConfig.Role : null;

        /// <summary>
        /// XP granted when this enemy dies: the config value unless the battle
        /// generator set another one (e.g. scaled by depth, or 0 for summoned
        /// enemies).
        /// </summary>
        public int XpReward => _xpRewardOverride ?? (EnemyConfig != null ? EnemyConfig.XpReward : 0);

        /// <summary>Whether another enemy created this one during the battle (not part of the generated encounter).</summary>
        public bool IsSummoned { get; private set; }

        /// <summary>
        /// The enemy that summoned this one, if it is known. Null for a summoned enemy
        /// restored from a save (only <see cref="IsSummoned"/> is saved) or whose
        /// summoner died and was destroyed.
        /// </summary>
        [CanBeNull]
        public Enemy Summoner { get; private set; }

        /// <summary>
        /// The enemy's place in the battle, which decides when it acts in the enemy phase (see
        /// <see cref="Turns.ActingOrder"/>): given by the grid when it is spawned, saved with the battle.
        /// </summary>
        public int ActOrder { get; private set; }

        public void SetActOrder(int order) => ActOrder = Mathf.Max(0, order);

        public void SetXpReward(int xpReward) => _xpRewardOverride = Mathf.Max(0, xpReward);

        /// <summary>
        /// Marks this enemy as summoned during the battle. Pass the summoner when it
        /// is known (null when restoring a save).
        /// </summary>
        public void MarkSummoned([CanBeNull] Enemy summoner = null)
        {
            IsSummoned = true;
            Summoner = summoner;
        }

        /// <summary>
        /// Applies the config like any character, then refreshes the role and state
        /// icons shown over the enemy.
        /// </summary>
        public override void Initialize(CharacterConfig characterConfig, CharacterScaling scaling)
        {
            _telegraph = null;
            base.Initialize(characterConfig, scaling);

            if (TryGetComponent(out EnemyIconsView icons))
                icons.Bind(this);
        }

        protected override void Awake()
        {
            base.Awake();
            States.Changed += OnStatesChangedForTelegraph;
            EventBus.Subscribe<CharacterDisplacedEvent>(OnDisplaced);
            EventBus.Subscribe<CharactersSwappedEvent>(OnSwapped);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CharacterDisplacedEvent>(OnDisplaced);
            EventBus.Unsubscribe<CharactersSwappedEvent>(OnSwapped);
        }

        /// <summary>
        /// A telegraphed skill is prepared instead of used: the same checks as any use (alive, not silenced, cooldown
        /// ready, valid target), then the area is fixed on the cells it covers now and the action of the turn is
        /// used. Other skills are used at once, as for any character.
        /// </summary>
        public override bool TryUseSkill(GridController grid, SkillDefinition skill, Vector2Int targetPos)
        {
            if (skill == null || !skill.Telegraphed) return base.TryUseSkill(grid, skill, targetPos);

            if (IsDead || _telegraph != null) return false;
            if (States.IsRestricted(EBehaviorRestriction.PreventSkills)) return false;
            if (!skill.CanUse(this, grid, targetPos)) return false;

            var effective = EffectiveSkill.Resolve(this, skill);
            var cells = SkillTargeting.GetAreaCells(grid, this, effective, targetPos);
            var player = PlayerCharacter.Active;
            var aimedAtPlayer = player != null &&
                                SkillTargeting.GetAffectedCharacters(grid, this, effective, targetPos).Contains(player);
            _telegraph = new SkillTelegraph(this, skill, targetPos, cells, skill.WindupTurns, aimedAtPlayer);
            EventBus.Raise(new SkillTelegraphChangedEvent(this, _telegraph, ETelegraphChange.Prepared));
            TutorialService.Notify(ETutorialTrigger.FirstTelegraph);
            return true;
        }

        /// <summary>
        /// Start of this enemy's turn (called by its controller): a prepared skill waits one turn less and fires
        /// when the wait is over (<see cref="TelegraphResolvedEvent"/> follows the hit). Returns whether the enemy is
        /// still preparing or just fired; false when there is nothing prepared.
        /// </summary>
        public bool AdvanceTelegraph(GridController grid)
        {
            if (_telegraph == null || IsDead) return false;

            // A skill that cannot be interrupted still fires while a state forbids skills; one that can was
            // cancelled when the state arrived (this only catches a state restored from a save).
            if (_telegraph.Skill.InterruptibleByStates && States.IsRestricted(EBehaviorRestriction.PreventSkills))
            {
                CancelTelegraph();
                return true;
            }

            if (!_telegraph.Advance())
            {
                EventBus.Raise(new SkillTelegraphChangedEvent(this, _telegraph, ETelegraphChange.Waited));
                return true;
            }

            var telegraph = _telegraph;
            _telegraph = null;
            EventBus.Raise(new SkillTelegraphChangedEvent(this, telegraph, ETelegraphChange.Fired));

            // Who stands on the marked cells now: the same resolution the execution makes, taken before the damage.
            var hit = SkillTargeting.CreateContext(grid, this, EffectiveSkill.Resolve(this, telegraph.Skill),
                telegraph.TargetPos, telegraph.Cells).Affected;
            if (telegraph.Skill.ExecuteOnCells(this, grid, telegraph.TargetPos, telegraph.Cells))
                Cooldowns.Trigger(telegraph.Skill);
            EventBus.Raise(new TelegraphResolvedEvent(this, telegraph, hit));
            return true;
        }

        /// <summary>Drops the prepared skill (its cooldown starts as if it had been used). No-op without one.</summary>
        public void CancelTelegraph()
        {
            if (_telegraph == null) return;

            var telegraph = _telegraph;
            _telegraph = null;
            if (!IsDead)
                Cooldowns.Trigger(telegraph.Skill);
            EventBus.Raise(new SkillTelegraphChangedEvent(this, telegraph, ETelegraphChange.Cancelled));
        }

        /// <summary>Saved form of the prepared skill, or null.</summary>
        [CanBeNull]
        public TelegraphState CaptureTelegraph() => _telegraph?.Capture();

        /// <summary>Restores the prepared skill of a battle save (after the states and cooldowns).</summary>
        public void RestoreTelegraph([CanBeNull] TelegraphState state)
        {
            _telegraph = SkillTelegraph.Restore(this, state);
            if (_telegraph != null)
                EventBus.Raise(new SkillTelegraphChangedEvent(this, _telegraph, ETelegraphChange.Restored));
        }

        protected override void Die()
        {
            if (_telegraph != null)
            {
                var telegraph = _telegraph;
                _telegraph = null;
                EventBus.Raise(new SkillTelegraphChangedEvent(this, telegraph, ETelegraphChange.Cancelled));
            }

            base.Die();
        }

        /// <summary>A state that prevents skills interrupts the preparation (when the skill allows it).</summary>
        private void OnStatesChangedForTelegraph()
        {
            if (_telegraph == null || !_telegraph.Skill.InterruptibleByStates) return;

            if (States.IsRestricted(EBehaviorRestriction.PreventSkills))
                CancelTelegraph();
        }

        private void OnDisplaced(CharacterDisplacedEvent e)
        {
            if (e.Target != this || e.CellsMoved <= 0) return;

            OnForcedMove(e.To - e.From);
        }

        private void OnSwapped(CharactersSwappedEvent e)
        {
            if (e.First == this)
                OnForcedMove(e.SecondFrom - e.FirstFrom);
            else if (e.Second == this)
                OnForcedMove(e.FirstFrom - e.SecondFrom);
        }

        /// <summary>
        /// Another character moved this enemy (push, pull, swap): the prepared skill is cancelled when the skill says
        /// so, or its marked cells follow the enemy, keeping their shape and direction.
        /// </summary>
        private void OnForcedMove(Vector2Int delta)
        {
            if (_telegraph == null || delta == Vector2Int.zero || IsDead) return;

            if (_telegraph.Skill.CancelledByDisplacement)
            {
                CancelTelegraph();
                return;
            }

            if (!_telegraph.Skill.TelegraphFollowsCaster) return;

            _telegraph.Shift(delta);
            EventBus.Raise(new SkillTelegraphChangedEvent(this, _telegraph, ETelegraphChange.Moved));
        }
    }
}
