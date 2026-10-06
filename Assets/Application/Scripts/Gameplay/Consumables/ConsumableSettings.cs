using System.Collections.Generic;
using GridBattle.Data;
using UnityEngine;

namespace GridBattle.Gameplay.Consumables
{
    /// <summary>What happens when the player receives a consumable with every slot taken (consumiveis 2.8).</summary>
    public enum EFullInventoryPolicy
    {
        /// <summary>
        /// Nothing changes automatically: the UI asks the player to discard one of the
        /// carried items (<see cref="ConsumableInventory.Replace"/>) or to decline the new one.
        /// </summary>
        AskPlayer,

        /// <summary>The new item is lost.</summary>
        DiscardNew,

        /// <summary>The item carried the longest (first slot) is discarded to make room.</summary>
        ReplaceOldest
    }

    /// <summary>
    /// Global consumable rules (GDD 2.8 / consumiveis). Open questions of the
    /// design documents are fields here, with neutral defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "ConsumableSettings", menuName = "GridBattle/Settings/Consumable Settings",
        order = 0)]
    public sealed class ConsumableSettings : ScriptableObject, IGameSettings
    {
        /// <summary>Most slots the item bar can show (six 32 px buttons at the reference resolution).</summary>
        public const int MaxSlots = 6;

        [Header("Inventory")]
        [SerializeField]
        [Range(1, MaxSlots)]
        [Tooltip("Open question (consumiveis 2.8, number of slots): how many consumables the player carries. " +
                 "The item bar fits up to 6.")]
        private int slots = 3;

        [SerializeField]
        [Tooltip("Open question (consumiveis 2.8, full inventory): what happens when a consumable is received " +
                 "with every slot taken.")]
        private EFullInventoryPolicy fullPolicy = EFullInventoryPolicy.AskPlayer;

        [Header("Use rules (GDD 2.8)")]
        [SerializeField]
        [Min(1)]
        [Tooltip("Consumables the player can use in one turn. The GDD fixes it at 1. Using one does not consume " +
                 "the turn's action.")]
        private int maxUsesPerTurn = 1;

        [SerializeField]
        [Tooltip("Consumables can only be used in battle, never on the map (GDD 2.8).")]
        private bool onlyInBattle = true;

        [Header("Consumable node")]
        [SerializeField]
        [Tooltip("Items the consumable node can grant. A definition's Drop Weight is its relative chance.")]
        private List<ConsumableDefinition> pool = new();

        [SerializeField]
        [Min(1)]
        [Tooltip("Open question (consumiveis 2.8, how it is obtained): 1 = the node grants one random item " +
                 "(drawn from the run seed); more than 1 = the player chooses among that many options.")]
        private int choiceCount = 1;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Consumables a player starts with when a battle begins outside a run (e.g. from the editor), " +
                 "so the feature can be tested. Runs override it with their own inventory.")]
        private List<ConsumableDefinition> debugStartingConsumables = new();

        /// <summary>Number of slots, clamped to what the item bar can show.</summary>
        public int Slots => Mathf.Clamp(slots, 1, MaxSlots);

        public EFullInventoryPolicy FullPolicy => fullPolicy;
        public int MaxUsesPerTurn => Mathf.Max(1, maxUsesPerTurn);
        public bool OnlyInBattle => onlyInBattle;
        public IReadOnlyList<ConsumableDefinition> Pool => pool;
        public int ChoiceCount => Mathf.Max(1, choiceCount);
        public IReadOnlyList<ConsumableDefinition> DebugStartingConsumables => debugStartingConsumables;

        /// <summary>The settings asset registered in <see cref="GameSettings"/> (defaults if missing).</summary>
        public static ConsumableSettings Current => GameSettings.Get<ConsumableSettings>();
    }
}
