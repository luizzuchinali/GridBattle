using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace GridBattle.Core
{
    /// <summary>
    /// Access to localized text (Unity Localization). Every text shown to the
    /// player comes from the string tables: <see cref="UiTable"/> for interface
    /// texts and <see cref="ContentTable"/> for content (names and descriptions of
    /// classes, enemies, skills, states, talents...). Missing entries fall back to
    /// the key so nothing breaks while a table is incomplete.
    /// </summary>
    public static class Loc
    {
        public const string UiTable = "UI";
        public const string ContentTable = "Content";

        /// <summary>Raised when the player changes the language.</summary>
        public static event Action LocaleChanged
        {
            add
            {
                _localeChanged += value;
                EnsureHooked();
            }
            remove => _localeChanged -= value;
        }

        private static Action _localeChanged;
        private static bool _hooked;

        /// <summary>Interface text from the UI table. Supports Smart String / string.Format arguments.</summary>
        public static string Ui(string key, params object[] args) => Get(UiTable, key, args);

        public static string Get(string table, string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            try
            {
                var entry = LocalizationSettings.StringDatabase.GetTableEntry(table, key);
                if (entry.Entry == null) return key;

                return args is { Length: > 0 }
                    ? entry.Entry.GetLocalizedString(args)
                    : entry.Entry.GetLocalizedString();
            }
            catch (Exception)
            {
                return key;
            }
        }

        /// <summary>Text of a LocalizedString field, or <paramref name="fallback"/> if it is not set.</summary>
        public static string Get(LocalizedString text, string fallback, params object[] args)
        {
            if (text == null || text.IsEmpty) return fallback;

            try
            {
                var value = args is { Length: > 0 } ? text.GetLocalizedString(args) : text.GetLocalizedString();
                return string.IsNullOrEmpty(value) ? fallback : value;
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private static void EnsureHooked()
        {
            if (_hooked) return;
            _hooked = true;
            LocalizationSettings.SelectedLocaleChanged += _ => _localeChanged?.Invoke();
        }
    }
}
