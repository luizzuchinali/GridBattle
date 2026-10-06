using System.Collections.Generic;
using System.IO;
using System.Linq;
using GridBattle.Data;
using UnityEditor;
using UnityEngine;

namespace GridBattle.Editor
{
    /// <summary>
    /// Keeps the GameDatabase (every GameDefinition) and GameSettings (every
    /// IGameSettings asset) up to date: rebuilt automatically when relevant assets
    /// are imported, moved or deleted, and on demand from the menu.
    /// </summary>
    public sealed class GameDatabaseBuilder : AssetPostprocessor
    {
        private const string ResourcesFolder = "Assets/Application/Settings/Resources";
        private static bool _scheduled;

        [MenuItem("GridBattle/Rebuild Game Database")]
        public static void Rebuild()
        {
            var database = LoadOrCreate<GameDatabase>(GameDatabase.ResourcePath);
            var definitions = FindAssets<GameDefinition>();
            foreach (var definition in definitions)
                definition.EnsureId();

            var ordered = definitions.OrderBy(d => d.GetType().Name).ThenBy(d => d.name).ToList();
            if (!ordered.SequenceEqual(database.Definitions))
                database.SetDefinitions(ordered);

            var settingsRoot = LoadOrCreate<GameSettings>(GameSettings.ResourcePath);
            var settings = FindAssets<ScriptableObject>()
                .Where(asset => asset is IGameSettings)
                .OrderBy(asset => asset.GetType().Name)
                .ToList();
            if (!settings.SequenceEqual(settingsRoot.All))
                settingsRoot.SetSettings(settings);

            AssetDatabase.SaveAssets();
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved,
            string[] movedFrom)
        {
            if (_scheduled) return;
            if (!imported.Concat(deleted).Concat(moved).Any(path => path.EndsWith(".asset"))) return;

            _scheduled = true;
            EditorApplication.delayCall += () =>
            {
                _scheduled = false;
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    Rebuild();
            };
        }

        private static List<T> FindAssets<T>() where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith("Assets/"))
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .Distinct()
                .ToList();
        }

        private static T LoadOrCreate<T>(string resourceName) where T : ScriptableObject
        {
            var path = $"{ResourcesFolder}/{resourceName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            Directory.CreateDirectory(ResourcesFolder);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
