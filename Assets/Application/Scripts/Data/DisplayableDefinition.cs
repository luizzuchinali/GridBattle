using GridBattle.Core;
using UnityEngine;
using UnityEngine.Localization;

namespace GridBattle.Data
{
    /// <summary>
    /// A definition shown to the player: localized name and description plus an
    /// icon. Texts come from the localization tables, never from code.
    /// </summary>
    public abstract class DisplayableDefinition : GameDefinition
    {
        [Header("Presentation")]
        [SerializeField]
        [Tooltip("Localized name shown to the player (usually in the Content table).")]
        private LocalizedString displayName = new();

        [SerializeField]
        [Tooltip("Localized description shown to the player (usually in the Content table).")]
        private LocalizedString description = new();

        [SerializeField]
        private Sprite icon;

        public LocalizedString DisplayName => displayName;
        public LocalizedString Description => description;
        public Sprite Icon => icon;

        /// <summary>Localized name, or the asset name if no entry is set.</summary>
        public string GetDisplayName() => Loc.Get(displayName, name);

        /// <summary>Localized description, or an empty string if no entry is set.</summary>
        public string GetDescription() => Loc.Get(description, string.Empty);
    }
}
