using GridBattle.Gameplay.Combat;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Meta;
using GridBattle.Gameplay.States;

namespace GridBattle.Managers.Audio
{
    /// <summary>
    /// Maps gameplay events to sounds, so gameplay code never calls the audio
    /// system. Only what the existing events can tell is covered here; effects
    /// that depend on other modules (skills, UI, map, talents, terrain,
    /// consumables, run result) are triggered by those modules through
    /// <see cref="AudioManager.Play(ESfx)"/>.
    /// </summary>
    public sealed class AudioEventHooks
    {
        private bool _subscribed;
        private bool _battleActive;
        private int _lastLevel;

        public void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;

            EventBus.Subscribe<EntityMoveStartedEvent>(OnMoveStarted);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Subscribe<HealedEvent>(OnHealed);
            EventBus.Subscribe<StateAppliedEvent>(OnStateApplied);
            EventBus.Subscribe<StateRemovedEvent>(OnStateRemoved);
            EventBus.Subscribe<CharacterDiedEvent>(OnCharacterDied);
            EventBus.Subscribe<XpRewardDroppedEvent>(OnXpRewardDropped);
            EventBus.Subscribe<PlayerXpChangedEvent>(OnPlayerXpChanged);
            EventBus.Subscribe<GridInitializedEvent>(OnGridInitialized);
            EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Subscribe<OptionsChangedEvent>(OnOptionsChanged);

            // Volumes saved in the player profile.
            AudioManager.MusicVolume = ProfileService.MusicVolume;
            AudioManager.SfxVolume = ProfileService.SfxVolume;
        }

        public void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;

            EventBus.Unsubscribe<EntityMoveStartedEvent>(OnMoveStarted);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
            EventBus.Unsubscribe<HealedEvent>(OnHealed);
            EventBus.Unsubscribe<StateAppliedEvent>(OnStateApplied);
            EventBus.Unsubscribe<StateRemovedEvent>(OnStateRemoved);
            EventBus.Unsubscribe<CharacterDiedEvent>(OnCharacterDied);
            EventBus.Unsubscribe<XpRewardDroppedEvent>(OnXpRewardDropped);
            EventBus.Unsubscribe<PlayerXpChangedEvent>(OnPlayerXpChanged);
            EventBus.Unsubscribe<GridInitializedEvent>(OnGridInitialized);
            EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
            EventBus.Unsubscribe<OptionsChangedEvent>(OnOptionsChanged);
        }

        private static void OnOptionsChanged(OptionsChangedEvent e)
        {
            AudioManager.MusicVolume = e.MusicVolume;
            AudioManager.SfxVolume = e.SfxVolume;
        }

        private static void OnMoveStarted(EntityMoveStartedEvent e)
        {
            AudioManager.Play(ESfx.Walk);
        }

        private static void OnDamageDealt(DamageDealtEvent e)
        {
            var hit = e.Hit;

            // Skills play their own cast sound (SkillCast) from the skill module.
            if (hit.Kind == EDamageKind.BasicAttack)
                AudioManager.Play(ESfx.BasicAttack);

            if (hit.Target == null) return;

            if (hit.IsCrit)
                AudioManager.Play(ESfx.CriticalHit);

            // A killing blow is followed by the death sound, which replaces the hurt one.
            var library = AudioManager.Library;
            var playHurt = !hit.Killed || (library != null && library.HurtSoundOnKill);
            if (playHurt)
                AudioManager.Play(hit.Target is PlayerCharacter ? ESfx.PlayerHurt : ESfx.EnemyHurt);
        }

        private static void OnHealed(HealedEvent e)
        {
            if (e.Amount > 0)
                AudioManager.Play(ESfx.Heal);
        }

        private static void OnStateApplied(StateAppliedEvent e)
        {
            if (ShouldPlayStateSound(e.Character, e.State))
                AudioManager.Play(ESfx.StateApplied);
        }

        private static void OnStateRemoved(StateRemovedEvent e)
        {
            if (ShouldPlayStateSound(e.Character, e.State))
                AudioManager.Play(ESfx.StateExpired);
        }

        /// <summary>
        /// Only states shown in the details window make a sound (hidden bookkeeping
        /// states stay silent), and so do not states cleared because their holder died.
        /// Permanent states (talents) are applied silently at the start of every battle
        /// unless the library says otherwise.
        /// </summary>
        private static bool ShouldPlayStateSound(Character holder, StateInstance state)
        {
            if (state == null || state.Definition == null || !state.Definition.ShowInDetails) return false;
            if (holder == null || holder.IsDead) return false;

            var library = AudioManager.Library;
            return !state.IsPermanent || (library != null && library.StateSoundForPermanentStates);
        }

        private static void OnCharacterDied(CharacterDiedEvent e)
        {
            AudioManager.Play(e.Character is PlayerCharacter ? ESfx.PlayerDeath : ESfx.EnemyDeath);
        }

        private static void OnXpRewardDropped(XpRewardDroppedEvent e)
        {
            if (e.Packets != null && e.Packets.Count > 0)
                AudioManager.Play(ESfx.XpOrb);
        }

        private void OnPlayerXpChanged(PlayerXpChangedEvent e)
        {
            // The first event of a run (or a restored battle) only sets the baseline.
            var leveledUp = _battleActive && _lastLevel > 0 && e.Level > _lastLevel;
            _lastLevel = e.Level;
            if (leveledUp)
                AudioManager.Play(ESfx.LevelUp);
        }

        private void OnGridInitialized(GridInitializedEvent e)
        {
            _battleActive = true;

            // The run system asks for Boss before the boss battle starts; keep it.
            if (AudioManager.MusicContext != EMusicContext.Boss)
                AudioManager.SetMusicContext(EMusicContext.Battle);
        }

        private void OnBattleEnded(BattleEndedEvent e)
        {
            _battleActive = false;

            var library = AudioManager.Library;
            if (library == null) return;
            AudioManager.SetMusicContext(e.Victory ? library.MusicAfterVictory : library.MusicAfterDefeat);
        }
    }
}
