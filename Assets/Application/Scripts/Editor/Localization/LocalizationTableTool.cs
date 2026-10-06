using System;
using System.Reflection;
using GridBattle.Core;
using GridBattle.Data;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;

namespace GridBattle.Editor.Localization
{
    /// <summary>
    /// Editor helpers to write localization entries and link them to content
    /// (used by setup scripts; designers can also edit the tables in
    /// Window > Asset Management > Localization Tables).
    /// </summary>
    public static class LocalizationTableTool
    {
        /// <summary>
        /// Creates or updates <paramref name="key"/> in <paramref name="table"/> with
        /// the English, Spanish and Brazilian Portuguese texts.
        /// </summary>
        public static void SetEntry(string table, string key, string en, string es, string ptBr)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(table);
            if (collection == null)
                throw new ArgumentException($"String table collection '{table}' not found.");

            if (collection.SharedData.GetEntry(key) == null)
                collection.SharedData.AddKey(key);

            foreach (var stringTable in collection.StringTables)
            {
                var value = stringTable.LocaleIdentifier.Code switch
                {
                    "en" => en,
                    "es" => es,
                    "pt-BR" => ptBr,
                    _ => en,
                };
                stringTable.AddEntry(key, value);
                EditorUtility.SetDirty(stringTable);
            }

            EditorUtility.SetDirty(collection.SharedData);
        }

        public static LocalizedString Reference(string table, string key) => new(table, key);

        /// <summary>
        /// Writes name ("{key}.name") and description ("{key}.desc") entries in the
        /// Content table and links them to <paramref name="definition"/>.
        /// Pass null descriptions to only set the name.
        /// </summary>
        public static void SetDisplayTexts(DisplayableDefinition definition, string key,
            (string en, string es, string ptBr) displayName,
            (string en, string es, string ptBr)? description = null)
        {
            SetEntry(Loc.ContentTable, key + ".name", displayName.en, displayName.es, displayName.ptBr);
            SetField(definition, "displayName", Reference(Loc.ContentTable, key + ".name"));

            if (description is { } desc)
            {
                SetEntry(Loc.ContentTable, key + ".desc", desc.en, desc.es, desc.ptBr);
                SetField(definition, "description", Reference(Loc.ContentTable, key + ".desc"));
            }

            EditorUtility.SetDirty(definition);
        }

        /// <summary>Assigns a LocalizedString (or any value) to a private serialized field, walking base classes.</summary>
        public static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    if (target is UnityEngine.Object unityObject)
                        EditorUtility.SetDirty(unityObject);
                    return;
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Field '{fieldName}' not found on {target.GetType().Name}.");
        }

        [MenuItem("GridBattle/Localization/Save Tables")]
        public static void SaveTables()
        {
            AssetDatabase.SaveAssets();
            Debug.Log("Localization tables saved.");
        }
    }
}
