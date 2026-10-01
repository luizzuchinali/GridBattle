using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using UnityEngine;

namespace GridBattle.Gameplay
{
    /// <summary>
    /// Enemies of an encounter. List order is spawn order: each enemy takes the
    /// first free cell in the grid scan.
    /// </summary>
    [CreateAssetMenu(fileName = "Encounter", menuName = "GridBattle/Encounter Config", order = 10)]
    public class EncounterConfig : ScriptableObject
    {
        [SerializeField]
        private List<EnemyConfig> enemies = new();

        public IReadOnlyList<EnemyConfig> Enemies => enemies;
    }
}
