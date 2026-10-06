using System;
using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Events;
using JetBrains.Annotations;

namespace GridBattle.Gameplay.Consumables
{
    /// <summary>Outcome of <see cref="ConsumableInventory.Receive(ConsumableDefinition, EFullInventoryPolicy)"/>.</summary>
    public enum EConsumableReceiveResult
    {
        /// <summary>The item went into a free slot.</summary>
        Added,

        /// <summary>Full inventory, policy DiscardNew (or an invalid item): the new item was lost.</summary>
        DiscardedNew,

        /// <summary>Full inventory, policy ReplaceOldest: the oldest item was discarded and the new one added.</summary>
        ReplacedOldest,

        /// <summary>
        /// Full inventory, policy AskPlayer: nothing changed. The UI lets the player
        /// pick a slot to discard (<see cref="ConsumableInventory.Replace"/>) or decline the item.
        /// </summary>
        NeedsPlayerDecision
    }

    /// <summary>
    /// The consumables the player carries: a fixed number of slots over a list of
    /// definition ids. The list is owned by the caller (the run keeps it in
    /// <c>RunState.Player.ConsumableIds</c>), so the inventory writes straight into
    /// saved data and can be rebuilt on top of it at any time. A slot is occupied
    /// when it holds an id that resolves to a <see cref="ConsumableDefinition"/>;
    /// null, empty or unknown ids count as free. Items are kept compact and in the
    /// order they were received (the first slot is the oldest). Every change
    /// raises <see cref="Changed"/> and <see cref="ConsumableInventoryChangedEvent"/>.
    /// </summary>
    public sealed class ConsumableInventory
    {
        private readonly List<string> _ids;
        private readonly Func<string, ConsumableDefinition> _resolve;

        /// <summary>
        /// Wraps <paramref name="ids"/> (null = a new empty list). <paramref name="slots"/> 0 or less
        /// uses <see cref="ConsumableSettings.Slots"/>; the inventory never has fewer slots than the
        /// list has occupied positions, so saved items are never dropped. <paramref name="resolve"/>
        /// turns an id into its definition (default: the <see cref="GameDatabase"/>); it is
        /// replaceable for tests.
        /// </summary>
        public ConsumableInventory([CanBeNull] List<string> ids = null, int slots = 0,
            [CanBeNull] Func<string, ConsumableDefinition> resolve = null)
        {
            _ids = ids ?? new List<string>();
            _resolve = resolve ?? ResolveFromDatabase;

            var wanted = slots > 0 ? slots : ConsumableSettings.Current.Slots;
            var lastUsed = -1;
            for (var i = 0; i < _ids.Count; i++)
            {
                if (!string.IsNullOrEmpty(_ids[i]))
                    lastUsed = i;
            }

            Slots = Math.Max(wanted, lastUsed + 1);

            // Fixed size, so slot positions are stable indexes into the list.
            while (_ids.Count < Slots)
                _ids.Add(null);
            if (_ids.Count > Slots)
                _ids.RemoveRange(Slots, _ids.Count - Slots);
        }

        /// <summary>Number of slots (the item bar shows this many buttons).</summary>
        public int Slots { get; }

        /// <summary>The backing ids, one per slot (null = free). Read-only view for saving.</summary>
        public IReadOnlyList<string> Ids => _ids;

        /// <summary>Occupied slots.</summary>
        public int Count
        {
            get
            {
                var count = 0;
                for (var i = 0; i < Slots; i++)
                {
                    if (!IsFree(i))
                        count++;
                }

                return count;
            }
        }

        public bool IsFull => Count >= Slots;
        public bool IsEmpty => Count == 0;

        /// <summary>Raised after any change of the carried items.</summary>
        public event Action Changed;

        /// <summary>The item in <paramref name="slot"/>, or null when the slot is free or out of range.</summary>
        [CanBeNull]
        public ConsumableDefinition Get(int slot)
        {
            if (slot < 0 || slot >= Slots) return null;

            var id = _ids[slot];
            return string.IsNullOrEmpty(id) ? null : _resolve(id);
        }

        /// <summary>Whether <paramref name="slot"/> holds no usable item (out-of-range slots are not free).</summary>
        public bool IsFree(int slot) => slot >= 0 && slot < Slots && Get(slot) == null;

        /// <summary>First free slot, or -1 when full.</summary>
        public int FirstFreeSlot()
        {
            for (var i = 0; i < Slots; i++)
            {
                if (IsFree(i))
                    return i;
            }

            return -1;
        }

        /// <summary>Slot of the first occurrence of <paramref name="definition"/>, or -1.</summary>
        public int IndexOf([CanBeNull] ConsumableDefinition definition)
        {
            if (!IsStorable(definition)) return -1;

            for (var i = 0; i < Slots; i++)
            {
                if (_ids[i] == definition.Id && Get(i) != null)
                    return i;
            }

            return -1;
        }

        /// <summary>Adds the item to the first free slot. Returns false when the inventory is full.</summary>
        public bool TryAdd([CanBeNull] ConsumableDefinition definition) => TryAdd(definition, out _);

        public bool TryAdd([CanBeNull] ConsumableDefinition definition, out int slot)
        {
            slot = -1;
            if (!IsStorable(definition)) return false;

            slot = FirstFreeSlot();
            if (slot < 0) return false;

            _ids[slot] = definition.Id;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Puts the item in <paramref name="slot"/>, discarding what was there (the "discard one of the
        /// carried items" answer to a full inventory). Returns false for an invalid slot or item.
        /// </summary>
        public bool Replace(int slot, [CanBeNull] ConsumableDefinition definition)
        {
            if (slot < 0 || slot >= Slots || !IsStorable(definition)) return false;

            _ids[slot] = definition.Id;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Takes the item out of <paramref name="slot"/> and closes the gap (later items move one slot
        /// toward the start). Returns the removed item, or null when the slot was free.
        /// </summary>
        [CanBeNull]
        public ConsumableDefinition RemoveAt(int slot)
        {
            var removed = Get(slot);
            if (removed == null) return null;

            _ids[slot] = null;
            Compact();
            NotifyChanged();
            return removed;
        }

        /// <summary>Removes every item.</summary>
        public void Clear()
        {
            var changed = false;
            for (var i = 0; i < Slots; i++)
            {
                if (string.IsNullOrEmpty(_ids[i])) continue;

                _ids[i] = null;
                changed = true;
            }

            if (changed)
                NotifyChanged();
        }

        /// <summary>
        /// Gives the player an item applying <paramref name="policy"/> when there is no free slot.
        /// See <see cref="EConsumableReceiveResult"/>.
        /// </summary>
        public EConsumableReceiveResult Receive([CanBeNull] ConsumableDefinition definition,
            EFullInventoryPolicy policy)
        {
            if (!IsStorable(definition)) return EConsumableReceiveResult.DiscardedNew;
            if (TryAdd(definition)) return EConsumableReceiveResult.Added;

            switch (policy)
            {
                case EFullInventoryPolicy.DiscardNew:
                    return EConsumableReceiveResult.DiscardedNew;

                case EFullInventoryPolicy.ReplaceOldest:
                    RemoveAt(0);
                    TryAdd(definition);
                    return EConsumableReceiveResult.ReplacedOldest;

                default:
                    return EConsumableReceiveResult.NeedsPlayerDecision;
            }
        }

        /// <summary>Gives the player an item with the policy of <see cref="ConsumableSettings"/>.</summary>
        public EConsumableReceiveResult Receive([CanBeNull] ConsumableDefinition definition) =>
            Receive(definition, ConsumableSettings.Current.FullPolicy);

        /// <summary>Raises the change notifications (also useful after editing the backing list externally).</summary>
        public void NotifyChanged()
        {
            Changed?.Invoke();
            EventBus.Raise(new ConsumableInventoryChangedEvent(this));
        }

        /// <summary>Moves occupied slots to the front, keeping their order (unknown ids are dropped).</summary>
        private void Compact()
        {
            var write = 0;
            for (var read = 0; read < Slots; read++)
            {
                if (Get(read) == null) continue;

                if (write != read)
                {
                    _ids[write] = _ids[read];
                    _ids[read] = null;
                }

                write++;
            }

            for (var i = write; i < Slots; i++)
                _ids[i] = null;
        }

        private static bool IsStorable([CanBeNull] ConsumableDefinition definition) =>
            definition != null && !string.IsNullOrEmpty(definition.Id);

        [CanBeNull]
        private static ConsumableDefinition ResolveFromDatabase(string id)
        {
            var database = GameDatabase.Instance;
            return database != null ? database.Get<ConsumableDefinition>(id) : null;
        }
    }
}

