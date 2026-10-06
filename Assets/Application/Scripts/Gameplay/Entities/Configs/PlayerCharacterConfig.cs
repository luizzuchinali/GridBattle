using System.Collections.Generic;
using GridBattle.Gameplay.Talents;
using UnityEngine;

namespace GridBattle.Gameplay.Entities
{
    [CreateAssetMenu(fileName = "PlayerCharacterConfig", menuName = "GridBattle/Characters/Player Character Config", order = 1)]
    public class PlayerCharacterConfig : CharacterConfig
    {
        [Header("Template")]
        [SerializeField]
        [Tooltip("Shared playable character prefab template.")]
        private PlayerCharacter prefab;

        [Header("Class")]
        [SerializeField]
        [Tooltip("Class chosen in the menu that resolves to this character.")]
        private ECharacter characterClass;

        [Header("Unlock (GDD 3.2)")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Battles the player must win (summed over all runs) to unlock this class. 0 = available from the start. Only battles count, not heal/talent/consumable nodes.")]
        private int battlesToUnlock;

        [Header("Talents (GDD 3.5)")]
        [SerializeField]
        [Tooltip("Talents this class can be offered, besides the shared pool of the Talent Offer Settings. The XP curve and the level cap are global (Progression Settings), not per class.")]
        private List<TalentDefinition> talentPool = new();

        public PlayerCharacter Prefab => prefab;
        public ECharacter CharacterClass => characterClass;

        /// <summary>Battles won (across runs) needed to unlock this class; 0 = unlocked from the start.</summary>
        public int BattlesToUnlock => battlesToUnlock;

        /// <summary>The class's own talent pool (the shared pool of the Talent Offer Settings is added on top).</summary>
        public IReadOnlyList<TalentDefinition> TalentPool => talentPool;
    }
}
