using System.Collections.Generic;
using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Consumables;
using GridBattle.Gameplay.Entities.Skills;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Progression;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    public class PlayerCharacter : Character
    {
        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; }

        private ConsumableInventory _inventory;

        // The screen changes as soon as the player dies.
        protected override bool PlaysDeathEffect => false;

        public PlayerCharacterConfig PlayerConfig => Config as PlayerCharacterConfig;

        /// <summary>
        /// PlayerCharacter-only information: the class chosen in the menu.
        /// Enemies have no class.
        /// </summary>
        public ECharacter Class => PlayerConfig != null ? PlayerConfig.CharacterClass : default;

        /// <summary>
        /// XP threshold for the next level, following the global curve of
        /// <see cref="ProgressionSettings"/> (the same for every class).
        /// </summary>
        public int XpToNextLevel => ProgressionSettings.Current.GetXpToNextLevel(Level);

        /// <summary>
        /// The level cap (<see cref="ProgressionSettings.MaxLevel"/>) was reached: XP gives no more levels, so
        /// the XP bar stays full.
        /// </summary>
        public bool IsMaxLevel => ProgressionSettings.Current.IsMaxLevel(Level);

        /// <summary>
        /// Skill bar slots taken by the current skills. The class's starting skills
        /// only count when <see cref="SkillSettings.StartingSkillsUseSlots"/> is on.
        /// </summary>
        public int UsedSkillSlots
        {
            get
            {
                var settings = SkillSettings.Current;
                var used = 0;
                foreach (var skill in Skills)
                {
                    if (settings.StartingSkillsUseSlots || !IsStartingSkill(skill))
                        used++;
                }

                return used;
            }
        }

        /// <summary>Skill bar slots still free (a new skill can be unlocked while above 0).</summary>
        public int FreeSkillSlots => Mathf.Max(0, SkillSettings.Current.MaxSkillSlots - UsedSkillSlots);

        /// <summary>
        /// The consumables the player carries (GDD 2.8). The run replaces it with its own
        /// (<see cref="SetInventory"/>, over <c>RunState.Player.ConsumableIds</c>); a character spawned
        /// outside a run starts with <c>ConsumableSettings.DebugStartingConsumables</c>.
        /// </summary>
        public ConsumableInventory Inventory => _inventory ??= new ConsumableInventory();

        /// <summary>
        /// Consumables used in the current turn (reset when the player's turn starts). Using one
        /// does not consume the turn's action, but only <c>ConsumableSettings.MaxUsesPerTurn</c> fit
        /// in a turn.
        /// </summary>
        public int ConsumableUsesThisTurn { get; private set; }

        /// <summary>Whether a consumable was used in the current turn (the flag the run save stores).</summary>
        public bool ConsumableUsedThisTurn => ConsumableUsesThisTurn > 0;

        protected override void Awake()
        {
            base.Awake();
            EventBus.Subscribe<GlobalTurnStartedEvent>(OnGlobalTurnStarted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GlobalTurnStartedEvent>(OnGlobalTurnStarted);
        }

        private void OnGlobalTurnStarted(GlobalTurnStartedEvent e)
        {
            ConsumableUsesThisTurn = 0;
        }

        /// <summary>
        /// Replaces the carried consumables (the run's inventory when a battle starts or a run is
        /// loaded). Raises <see cref="ConsumableInventoryChangedEvent"/> so the item bar redraws.
        /// </summary>
        public void SetInventory(ConsumableInventory inventory)
        {
            _inventory = inventory ?? new ConsumableInventory();
            EventBus.Raise(new ConsumableInventoryChangedEvent(_inventory));
        }

        /// <summary>Counts one consumable use in this turn (called by <see cref="ConsumableExecutor"/>).</summary>
        public void RegisterConsumableUse()
        {
            ConsumableUsesThisTurn++;
        }

        /// <summary>
        /// Restores the per-turn use counter from a save (<c>BattleSnapshot.ConsumableUsedThisTurn</c>).
        /// Call it after the grid was initialized: starting the turn resets the counter.
        /// </summary>
        public void RestoreConsumableUses(int uses)
        {
            ConsumableUsesThisTurn = Mathf.Max(0, uses);
        }

        public override void Initialize(CharacterConfig characterConfig, CharacterScaling scaling)
        {
            base.Initialize(characterConfig, scaling);

            // New character = fresh consumables: the debug set until a run assigns its own inventory.
            ConsumableUsesThisTurn = 0;
            SetInventory(CreateDebugInventory());

            // New character = new progression. Notifies the UI so it doesn't keep the
            // XP bar state from a previous run.
            EventBus.Raise(new PlayerXpChangedEvent(Level, CurrentXp, XpToNextLevel));

            // Same for the skill bar: the new character starts with its class skills.
            EventBus.Raise(new SkillListChangedEvent(this));
        }

        private static ConsumableInventory CreateDebugInventory()
        {
            var settings = ConsumableSettings.Current;
            var ids = new List<string>();
            foreach (var consumable in settings.DebugStartingConsumables)
            {
                if (consumable != null && !string.IsNullOrEmpty(consumable.Id) && ids.Count < settings.Slots)
                    ids.Add(consumable.Id);
            }

            return new ConsumableInventory(ids, settings.Slots);
        }

        private bool IsStartingSkill(SkillDefinition skill)
        {
            if (Config == null) return false;

            foreach (var starting in Config.Skills)
            {
                if (starting == skill) return true;
            }

            return false;
        }

        /// <summary>
        /// Sets level and XP directly: the run gives a new battle's player the progress it carries
        /// (<c>RunState.Player</c>), without leveling up again. Raises <see cref="PlayerXpChangedEvent"/>.
        /// </summary>
        public void SetProgress(int level, int xp)
        {
            var settings = ProgressionSettings.Current;
            Level = Mathf.Clamp(level, 1, settings.MaxLevel);
            CurrentXp = Mathf.Max(0, xp);
            if (IsMaxLevel)
                CurrentXp = Mathf.Min(CurrentXp, XpToNextLevel);
            EventBus.Raise(new PlayerXpChangedEvent(Level, CurrentXp, XpToNextLevel));
        }

        /// <summary>
        /// XP gain. Called by XpRewardSystem as each XP packet is delivered
        /// (not directly when the enemy dies). Each time the XP crosses a threshold the level goes up
        /// (the XP above it carries over unless <see cref="ProgressionSettings.CarryOverLeftoverXp"/> is off);
        /// at the level cap the XP gives nothing more. Raises <see cref="PlayerXpChangedEvent"/> for the UI and
        /// then, for each level gained, heals <see cref="ProgressionSettings.LevelUpHealFraction"/> of the max HP
        /// and raises one <see cref="PlayerLeveledUpEvent"/> (the talent module opens one offer for each), so the
        /// XP bar already shows the new level when the choice opens.
        /// </summary>
        public void GainXp(int amount)
        {
            if (IsDead || amount <= 0) return;

            var settings = ProgressionSettings.Current;
            var firstLevel = Level;
            if (!IsMaxLevel)
            {
                CurrentXp += amount;
                while (!IsMaxLevel && CurrentXp >= XpToNextLevel)
                {
                    CurrentXp = settings.CarryOverLeftoverXp ? CurrentXp - XpToNextLevel : 0;
                    Level++;
                }

                // At the cap the bar stays full; the rest of the XP is lost.
                if (IsMaxLevel)
                    CurrentXp = Mathf.Min(CurrentXp, XpToNextLevel);
            }

            EventBus.Raise(new PlayerXpChangedEvent(Level, CurrentXp, XpToNextLevel));

            for (var level = firstLevel + 1; level <= Level; level++)
            {
                if (settings.LevelUpHealFraction > 0f)
                    CombatResolver.HealFraction(this, settings.LevelUpHealFraction);
                EventBus.Raise(new PlayerLeveledUpEvent(this, level));
            }
        }
    }
}
