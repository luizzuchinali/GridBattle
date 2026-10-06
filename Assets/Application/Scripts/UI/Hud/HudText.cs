using System;
using GridBattle.Core;

namespace GridBattle.UI.Hud
{
    /// <summary>
    /// Localized texts of the HUD (UI table). Formatting is positional ({0}, {1}), independent of whether
    /// the table entry is a Smart String.
    /// </summary>
    public static class HudText
    {
        public static string Get(string key) => Loc.Ui(key);

        public static string Format(string key, params object[] args)
        {
            var template = Loc.Ui(key);
            if (args == null || args.Length == 0) return template;

            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }
    }
}
