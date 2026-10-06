using System.Collections.Generic;
using GridBattle.Data;
using GridBattle.Gameplay.Entities;
using GridBattle.Gameplay.States;
using UnityEngine;

namespace GridBattle.Gameplay.Terrain
{
    /// <summary>
    /// A kind of terrain cell: obstacle, hazard or bonus. Pure data: what the cell
    /// blocks, when it triggers, what it does to the character standing on it and
    /// how it looks. The localized name and description explain the effect (the
    /// details window and the node preview show them). Player and enemies follow
    /// the same rules unless the affected sides are restricted here.
    /// </summary>
    [CreateAssetMenu(fileName = "Terrain", menuName = "GridBattle/Terrain/Terrain Definition", order = 0)]
    public class TerrainDefinition : DisplayableDefinition
    {
        [Header("Kind")]
        [SerializeField]
        private ETerrainKind kind = ETerrainKind.Hazard;

        [SerializeField]
        [Tooltip("Nobody can walk into, occupy, be spawned or teleported onto this cell. Always on for obstacles.")]
        private bool blocksMovement;

        [SerializeField]
        [Tooltip("Open question (grid_e_terreno.md): skill areas skip this cell, so no one on it is affected. Off by default.")]
        private bool blocksSkillArea;

        [Header("Effect")]
        [SerializeField]
        [Tooltip("Open question (grid_e_terreno.md): when the effect applies. Default: at the end of the standing character's own turn.")]
        private ETerrainTrigger trigger = ETerrainTrigger.OnTurnEnd;

        [SerializeField]
        [Min(0)]
        [Tooltip("Damage dealt to the character (kind Terrain: it ignores defense when CombatSettings says so). 0 = none.")]
        private int damage;

        [SerializeField]
        [Tooltip("States applied to the character (no source: they are not removed with any talent).")]
        private List<StateGrant> states = new();

        [SerializeField]
        [Tooltip("Whether the player is affected.")]
        private bool affectsPlayer = true;

        [SerializeField]
        [Tooltip("Whether enemies are affected.")]
        private bool affectsEnemies = true;

        [Header("Visuals")]
        [SerializeField]
        [Tooltip("Sprite drawn over the cell floor, under the characters. Keep it pixel-aligned (even size).")]
        private Sprite overlaySprite;

        [SerializeField]
        [Tooltip("Tint of the overlay sprite.")]
        private Color overlayColor = Color.white;

        [SerializeField]
        [Tooltip("Multiplied into the cell floor so the type reads before entering it. White = unchanged.")]
        private Color floorTint = Color.white;

        public ETerrainKind Kind => kind;

        /// <summary>Whether the cell cannot be walked into, occupied, spawned or teleported onto.</summary>
        public bool BlocksMovement => blocksMovement || kind == ETerrainKind.Obstacle;

        /// <summary>Whether skill areas skip the cell.</summary>
        public bool BlocksSkillArea => blocksSkillArea;

        public ETerrainTrigger Trigger => trigger;
        public int Damage => damage;
        public IReadOnlyList<StateGrant> States => states;
        public bool AffectsPlayer => affectsPlayer;
        public bool AffectsEnemies => affectsEnemies;
        public Sprite OverlaySprite => overlaySprite;
        public Color OverlayColor => overlayColor;
        public Color FloorTint => floorTint;

        /// <summary>Whether the cell does anything to the character standing on it.</summary>
        public bool HasEffect
        {
            get
            {
                if (damage > 0) return true;
                foreach (var grant in states)
                {
                    if (grant.IsValid) return true;
                }

                return false;
            }
        }

        /// <summary>Whether the terrain affects this character's side.</summary>
        public bool AffectsCharacter(Character character)
        {
            return character is PlayerCharacter ? affectsPlayer : affectsEnemies;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (kind == ETerrainKind.Obstacle)
                blocksMovement = true;
        }
#endif
    }
}
