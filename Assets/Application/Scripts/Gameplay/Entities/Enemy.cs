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
    /// enemy (<see cref="AdvanceTelegraph"/>, called by the controller at the start of its action). Dying, a state
    /// that prevents skills or (when the skill says so) a push or pull cancels it.
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
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CharacterDisplacedEvent>(OnDisplaced);
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

            var cells = SkillTargeting.GetAreaCells(grid, this, skill, targetPos);
            _telegraph = new SkillTelegraph(this, skill, targetPos, cells, skill.WindupTurns);
            EventBus.Raise(new SkillTelegraphChangedEvent(this, _telegraph, ETelegraphChange.Prepared));
            TutorialService.Notify(ETutorialTrigger.FirstTelegraph);
            return true;
        }

        /// <summary>
        /// Start of this enemy's action (called by its controller): a prepared skill waits one turn less and fires
        /// when the wait is over. Returns whether the enemy is still preparing or just fired (both use the action of
        /// the turn); false when there is nothing prepared.
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
            if (telegraph.Skill.ExecuteOnCells(this, grid, telegraph.TargetPos, telegraph.Cells))
                Cooldowns.Trigger(telegraph.Skill);
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

            if (_telegraph != null && _telegraph.Skill.CancelledByDisplacement)
                CancelTelegraph();
        }
    }
}
