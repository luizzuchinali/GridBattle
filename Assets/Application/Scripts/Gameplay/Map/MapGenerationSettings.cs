using System;
using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Run;
using JetBrains.Annotations;
using UnityEngine;

namespace GridBattle.Gameplay.Map
{
    /// <summary>
    /// Relative chance of each node type from <see cref="MinDepth"/> until the next band starts. A weight of 0
    /// removes the type from those depths.
    /// </summary>
    [Serializable]
    public sealed class MapNodeTypeBand
    {
        [SerializeField]
        [Min(1)]
        [Tooltip("First depth (map floor, from 1) this band applies to.")]
        private int minDepth = 1;

        [SerializeField]
        [Min(0f)]
        private float battleWeight = 55f;

        [SerializeField]
        [Min(0f)]
        private float healWeight = 15f;

        [SerializeField]
        [Min(0f)]
        private float talentWeight = 10f;

        [SerializeField]
        [Min(0f)]
        private float consumableWeight = 20f;

        public MapNodeTypeBand()
        {
        }

        public MapNodeTypeBand(int minDepth, float battle, float heal, float talent, float consumable)
        {
            this.minDepth = minDepth;
            battleWeight = battle;
            healWeight = heal;
            talentWeight = talent;
            consumableWeight = consumable;
        }

        public int MinDepth => minDepth;

        /// <summary>Weight of a node type (the boss node is placed by the generator, never drawn).</summary>
        public float GetWeight(EMapNodeType type) => type switch
        {
            EMapNodeType.Battle => battleWeight,
            EMapNodeType.Heal => healWeight,
            EMapNodeType.Talent => talentWeight,
            EMapNodeType.Consumable => consumableWeight,
            _ => 0f,
        };
    }

    /// <summary>
    /// Configuration of the map generator (GDD Mechanic 2, mapa_e_nos.md): shape of the map (floors, nodes per
    /// floor, lanes, edges), the distribution of node types by depth and the difficulty of battle nodes. The
    /// exact format and the distribution are open questions in the design documents; the defaults give about 30
    /// nodes, as in the GDD's example.
    /// </summary>
    [CreateAssetMenu(fileName = "MapGenerationSettings", menuName = "GridBattle/Map/Map Generation Settings",
        order = 0)]
    public sealed class MapGenerationSettings : ScriptableObject, IGameSettings
    {
        private static readonly EMapNodeType[] DrawnTypes =
            { EMapNodeType.Battle, EMapNodeType.Heal, EMapNodeType.Talent, EMapNodeType.Consumable };

        [Header("Shape")]
        [SerializeField]
        [Min(2)]
        [Tooltip("Open question (mapa_e_nos, map format): number of floors, counting the last one, which holds only the final boss. The depth of a node is its floor + 1. The balance follows this number (see Balance Floor Count).")]
        private int floorCount = 30;

        [SerializeField]
        [Min(2)]
        [Tooltip("Number of floors the depth values of the balance were written for: the enemy pool, threat budget, enemy scaling and XP of Battle Generation Settings, and the terrain bands of Terrain Generation Settings. " +
                 "On a map with another Floor Count those depths are stretched (the first floor and the boss keep the same strength) and the XP of each battle is scaled so a run grants about the same total XP. " +
                 "Keep it equal to Floor Count to read the balance assets floor by floor. The node type bands of this asset are not stretched.")]
        private int balanceFloorCount = 30;

        [SerializeField]
        [Range(1, 7)]
        [Tooltip("Columns of the map. A floor never has more nodes than lanes, and nodes are never drawn on the same lane twice in a floor.")]
        private int lanes = 4;

        [SerializeField]
        [Min(1)]
        [Tooltip("Fewest nodes of a regular floor (never more than the number of lanes).")]
        private int minNodesPerFloor = 2;

        [SerializeField]
        [Min(1)]
        [Tooltip("Most nodes of a regular floor (never more than the number of lanes).")]
        private int maxNodesPerFloor = 4;

        [SerializeField]
        [Range(1, 3)]
        [Tooltip("Most paths leaving one node. Edges never cross, every node is reachable from the first floor and leads to the boss.")]
        private int maxEdgesPerNode = 2;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Chance that two neighboring nodes lead to the same node of the next floor (paths merging) instead of each having its own. 0 = parallel paths, 1 = paths merge as much as the shape allows.")]
        private float mergeChance = 0.5f;

        [Header("Node types by depth")]
        [SerializeField]
        [Tooltip("Open question (mapa_e_nos, distribution): bands by depth; the band with the highest Min Depth not above the node's depth applies. Default: only battles at depth 1, then battle 55 / heal 15 / talent 10 / consumable 20.")]
        private List<MapNodeTypeBand> typeBands = new()
        {
            new MapNodeTypeBand(1, 1f, 0f, 0f, 0f),
            new MapNodeTypeBand(2, 55f, 15f, 10f, 20f),
        };

        [SerializeField]
        [Tooltip("A node never gets the same non-battle type (heal, talent, consumable) as one of the nodes that lead to it, so those nodes do not appear twice in a row on a path.")]
        private bool avoidSameNonBattleTypeInARow = true;

        [SerializeField]
        [Tooltip("Makes sure the floor before the boss has at least one heal node (turns a random node into one if the draw gave none).")]
        private bool guaranteeHealBeforeBoss;

        [SerializeField]
        [Min(1)]
        [Tooltip("Times a node's type is drawn again when it breaks the 'same type in a row' rule, before the last draw is accepted.")]
        private int typeRerollAttempts = 6;

        [Header("Battle difficulty")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("Open question (mapa_e_nos, difficulty in the same floor): relative chance of an Easy battle node.")]
        private float easyWeight = 35f;

        [SerializeField]
        [Min(0f)]
        private float normalWeight = 40f;

        [SerializeField]
        [Min(0f)]
        private float hardWeight = 25f;

        [SerializeField]
        [Tooltip("Risk and reward (GDD Mechanic 2): when a node leads to two or more battles (or the first floor " +
                 "offers several), they get different difficulties whenever possible, so the player really chooses " +
                 "between a safer and a more rewarding fight.")]
        private bool diversifyBattleChoices = true;

        public int FloorCount => Mathf.Max(2, floorCount);
        public int BalanceFloorCount => Mathf.Max(2, balanceFloorCount);
        public int Lanes => Mathf.Clamp(lanes, 1, 7);

        /// <summary>
        /// Depth of the balance assets that corresponds to <paramref name="depth"/> of this map: the first floor
        /// is 1 and the boss floor is <see cref="BalanceFloorCount"/>, stretched linearly in between. Equal to the
        /// depth when the map has <see cref="BalanceFloorCount"/> floors.
        /// </summary>
        public float GetBalanceDepth(int depth) =>
            1f + Mathf.Max(0, depth - 1) * (BalanceFloorCount - 1f) / (FloorCount - 1f);

        /// <summary>
        /// Factor of the XP of each battle that keeps the XP of a whole run about the same on a map longer or
        /// shorter than <see cref="BalanceFloorCount"/> (1 when they are equal).
        /// </summary>
        public float RunLengthXpFactor => (BalanceFloorCount - 1f) / (FloorCount - 1f);

        /// <summary>Fewest nodes of a regular floor, never above the lanes.</summary>
        public int MinNodesPerFloor => Mathf.Clamp(minNodesPerFloor, 1, Lanes);

        /// <summary>Most nodes of a regular floor, never above the lanes nor below the minimum.</summary>
        public int MaxNodesPerFloor => Mathf.Clamp(maxNodesPerFloor, MinNodesPerFloor, Lanes);

        public int MaxEdgesPerNode => Mathf.Clamp(maxEdgesPerNode, 1, 3);
        public float MergeChance => mergeChance;
        public bool AvoidSameNonBattleTypeInARow => avoidSameNonBattleTypeInARow;
        public bool GuaranteeHealBeforeBoss => guaranteeHealBeforeBoss;
        public bool DiversifyBattleChoices => diversifyBattleChoices;
        public int TypeRerollAttempts => Mathf.Max(1, typeRerollAttempts);
        public IReadOnlyList<MapNodeTypeBand> TypeBands => typeBands;

        /// <summary>Weight of a battle difficulty.</summary>
        public float GetDifficultyWeight(EBattleDifficulty difficulty) => difficulty switch
        {
            EBattleDifficulty.Easy => easyWeight,
            EBattleDifficulty.Normal => normalWeight,
            _ => hardWeight,
        };

        /// <summary>The type band that applies to <paramref name="depth"/>, or null if none does (battles only).</summary>
        [CanBeNull]
        public MapNodeTypeBand GetTypeBand(int depth)
        {
            MapNodeTypeBand best = null;
            foreach (var band in typeBands)
            {
                if (band == null || band.MinDepth > depth) continue;
                if (best == null || band.MinDepth >= best.MinDepth)
                    best = band;
            }

            return best;
        }

        /// <summary>Weights of the drawn node types (battle, heal, talent, consumable) at a depth.</summary>
        public float[] GetTypeWeights(int depth)
        {
            var band = GetTypeBand(depth);
            var weights = new float[DrawnTypes.Length];
            for (var i = 0; i < DrawnTypes.Length; i++)
                weights[i] = band != null ? band.GetWeight(DrawnTypes[i]) : (DrawnTypes[i] == EMapNodeType.Battle ? 1f : 0f);

            return weights;
        }

        /// <summary>The node type of an index of <see cref="GetTypeWeights"/>.</summary>
        public static EMapNodeType GetDrawnType(int index) => DrawnTypes[Mathf.Clamp(index, 0, DrawnTypes.Length - 1)];
    }
}
