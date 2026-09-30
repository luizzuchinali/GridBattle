using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GridBattle.Gameplay.Entities;

namespace GridBattle.EditorTools
{
    public static class CharacterConfigSetup
    {
        private const string PlayersRoot = "Assets/Application/Settings/Characters/Players";
        private const string EnemiesRoot = "Assets/Application/Settings/Characters/Enemies";

        /// <summary>
        /// Valores de balanceamento inicial. Espelho do documento
        /// docs/design/sistema_combate.md — alterar sempre em pares.
        /// </summary>
        private static readonly Dictionary<string, (int maxHp, int attackDamage, int walkDistance)> PlayerStats =
            new()
            {
                { "Knight", (120, 15, 1) },
                { "Mage", (80, 15, 1) },
                { "Rogue", (90, 15, 1) },
            };

        private static readonly Dictionary<string, (int maxHp, int attackDamage, int walkDistance)> EnemyStats =
            new()
            {
                { "Goblin", (30, 5, 1) },
                { "Rat", (20, 5, 2) },
                { "Slime", (40, 5, 1) },
                { "FireSkull", (25, 7, 1) },
                { "EyeBat", (25, 5, 2) },
            };

        private static List<string> SetupPlayer(string prefabName, ECharacter characterClass)
        {
            var results = new List<string>();
            var assetPath = $"{PlayersRoot}/{prefabName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<PlayerCharacterConfig>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<PlayerCharacterConfig>();
                AssetDatabase.CreateAsset(asset, assetPath);
                results.Add($"{assetPath}: created");
            }

            var stats = PlayerStats[prefabName];
            var so = new SerializedObject(asset);
            so.FindProperty("characterClass").enumValueIndex = (int)characterClass;
            so.FindProperty("maxHp").intValue = stats.maxHp;
            so.FindProperty("basicAttackDamage").intValue = stats.attackDamage;
            so.FindProperty("walkDistance").intValue = stats.walkDistance;
            so.ApplyModifiedPropertiesWithoutUndo();
            results.Add(
                $"{assetPath}: class = {characterClass}, maxHp = {stats.maxHp}, basicAttackDamage = {stats.attackDamage}, walkDistance = {stats.walkDistance}");

            results.AddRange(AssignToPrefab($"Assets/Application/Prefabs/Entities/PlayerCharacters/{prefabName}.prefab", asset));
            return results;
        }

        private static List<string> SetupEnemy(string prefabName)
        {
            var results = new List<string>();
            var assetPath = $"{EnemiesRoot}/{prefabName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<CharacterConfig>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CharacterConfig>();
                AssetDatabase.CreateAsset(asset, assetPath);
                results.Add($"{assetPath}: created");
            }

            var stats = EnemyStats[prefabName];
            var so = new SerializedObject(asset);
            so.FindProperty("maxHp").intValue = stats.maxHp;
            so.FindProperty("basicAttackDamage").intValue = stats.attackDamage;
            so.FindProperty("walkDistance").intValue = stats.walkDistance;
            so.ApplyModifiedPropertiesWithoutUndo();
            results.Add(
                $"{assetPath}: maxHp = {stats.maxHp}, basicAttackDamage = {stats.attackDamage}, walkDistance = {stats.walkDistance}");

            results.AddRange(AssignToPrefab($"Assets/Application/Prefabs/Entities/Enemies/{prefabName}.prefab", asset));
            return results;
        }

        public static List<string> CreateAndAssign()
        {
            var results = new List<string>();

            EnsureFolders();
            results.AddRange(SetupPlayer("Knight", ECharacter.Warrior));
            results.AddRange(SetupPlayer("Mage", ECharacter.Mage));
            results.AddRange(SetupPlayer("Rogue", ECharacter.Rogue));

            foreach (var enemyName in new[] { "Goblin", "Rat", "Slime", "FireSkull", "EyeBat" })
                results.AddRange(SetupEnemy(enemyName));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return results;
        }

        private static List<string> AssignToPrefab(string prefabPath, CharacterConfig config)
        {
            var results = new List<string>();
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                var character = contents.GetComponent<Character>();
                if (character == null)
                {
                    results.Add($"{prefabPath}: no Character component!");
                    return results;
                }

                var so = new SerializedObject(character);
                so.FindProperty("config").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                results.Add($"{prefabPath}: config = {config.name}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return results;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Application/Settings");
            EnsureFolder("Assets/Application/Settings/Characters");
            EnsureFolder(PlayersRoot);
            EnsureFolder(EnemiesRoot);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = path.Substring(0, path.LastIndexOf('/'));
            var name = path.Substring(path.LastIndexOf('/') + 1);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}