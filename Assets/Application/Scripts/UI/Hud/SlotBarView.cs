using System;
using System.Collections.Generic;
using GridBattle.Gameplay;
using GridBattle.Gameplay.Controllers;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.Events;
using GridBattle.Gameplay.Turns;
using GridBattle.Managers;
using GridBattle.Managers.Audio;
using UnityEngine.UIElements;

namespace GridBattle.UI.Hud
{
    /// <summary>
    /// Base of the skill bar and the item bar: a row of <see cref="HudSlot"/>s in a container, bound to
    /// the player and its input controller. Redraws whenever the game state it shows changes (events,
    /// no polling) and forwards taps to <see cref="PlayerCharacterController"/>, which owns the rules.
    /// </summary>
    public abstract class SlotBarView : IDisposable
    {
        private readonly VisualElement _container;
        private readonly bool _round;
        private readonly List<Action> _unsubscribers = new();
        private readonly List<HudSlot> _slots = new();

        protected SlotBarView(VisualElement container, bool round)
        {
            _container = container;
            _round = round;
            var turns = UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            IsPlayerTurn = turns == null || turns.CurrentTurn == ETurnOwner.Player;

            Watch<TurnChangedEvent>(e =>
            {
                IsPlayerTurn = e.IsPlayerTurn;
                Refresh();
            });
            Watch<GridInitializedEvent>(_ =>
            {
                FindPlayer();
                Refresh();
            });
            Watch<CharacterDiedEvent>(e =>
            {
                if (e.Character is not PlayerCharacter) return;

                Player = null;
                Controller = null;
                Refresh();
            });

            // Windows (details, talent choice, pause) hold the turn flow: the bar looks inactive meanwhile.
            Action<bool> onBlockedChanged = _ => Refresh();
            TurnBlockers.BlockedChanged += onBlockedChanged;
            _unsubscribers.Add(() => TurnBlockers.BlockedChanged -= onBlockedChanged);

            FindPlayer();
        }

        /// <summary>The slots, in order (rebuilt when the slot count changes).</summary>
        public IReadOnlyList<HudSlot> Slots => _slots;

        protected PlayerCharacter Player { get; set; }
        protected PlayerCharacterController Controller { get; private set; }

        /// <summary>Whether the turn belongs to the player (tracked from <see cref="TurnChangedEvent"/>).</summary>
        protected bool IsPlayerTurn { get; private set; }

        /// <summary>Whether the bar must look inactive: not the player's turn, or a window holds the game.</summary>
        protected bool IsInactive => !IsPlayerTurn || GameplayInput.IsBlocked;

        /// <summary>Number of slots the bar shows.</summary>
        protected abstract int GetSlotCount();

        /// <summary>Applies the state of slot <paramref name="index"/> (0-based).</summary>
        protected abstract void RefreshSlot(int index, HudSlot slot);

        /// <summary>The player tapped slot <paramref name="index"/>.</summary>
        protected abstract void OnSlotTapped(int index);

        /// <summary>Calls <paramref name="handler"/> on every <typeparamref name="TEvent"/> until disposed.</summary>
        protected void Watch<TEvent>(Action<TEvent> handler)
        {
            EventBus.Subscribe(handler);
            _unsubscribers.Add(() => EventBus.Unsubscribe(handler));
        }

        /// <summary>Takes the current player and its controller from the scene (null when there is none).</summary>
        protected void FindPlayer()
        {
            Player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
            Controller = Player != null ? Player.GetComponent<PlayerCharacterController>() : null;
        }

        /// <summary>Redraws every slot from the current game state.</summary>
        public void Refresh()
        {
            var count = GetSlotCount();
            if (_slots.Count != count)
                Rebuild(count);

            for (var i = 0; i < _slots.Count; i++)
                RefreshSlot(i, _slots[i]);
        }

        public void Dispose()
        {
            foreach (var unsubscribe in _unsubscribers)
                unsubscribe();
            _unsubscribers.Clear();
        }

        private void Rebuild(int count)
        {
            _container.Clear();
            _slots.Clear();
            for (var i = 0; i < count; i++)
            {
                var index = i;
                var slot = new HudSlot(_round);
                slot.Button.clicked += () =>
                {
                    AudioManager.Play(ESfx.ButtonTap);
                    OnSlotTapped(index);
                };
                _slots.Add(slot);
                _container.Add(slot.Button);
            }
        }
    }
}
