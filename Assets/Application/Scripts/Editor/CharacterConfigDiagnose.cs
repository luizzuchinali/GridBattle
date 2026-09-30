using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using GridBattle.Gameplay.Entities;

namespace GridBattle.EditorTools
{
    public static class CharacterConfigDiagnose
    {
        public static List<string> Run()
        {
            var results = new List<string>();

            foreach (var path in new[]
                     {
                         "Assets/Application/Prefabs/Entities/Enemies/Goblin.prefab",
                         "Assets/Application/Prefabs/Entities/Enemies/Rat.prefab",
                         "Assets/Application/Prefabs/Entities/Enemies/Slime.prefab",
                         "Assets/Application/Prefabs/Entities/Enemies/FireSkull.prefab",
                         "Assets/Application/Prefabs/Entities/Enemies/EyeBat.prefab",
                         "Assets/Application/Prefabs/Entities/PlayerCharacters/Knight.prefab",
                     })
            {
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var c = contents.GetComponent<Character>();
                    if (c == null)
                    {
                        results.Add($"{path}: no Character");
                        continue;
                    }

                    var so = new SerializedObject(c);
                    var configRef = so.FindProperty("config").objectReferenceValue;
                    results.Add($"{path}: config={(configRef != null ? configRef.name : "NULL")} " +
                                $"(maxHp={c.MaxHp}, atk={c.BasicAttackDamage}, walk={c.WalkDistance})");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            results.Add("--- assets ---");
            foreach (var path in new[]
                     {
                         "Assets/Application/Settings/Characters/Enemies/Goblin.asset",
                         "Assets/Application/Settings/Characters/Players/Knight.asset",
                     })
            {
                var config = AssetDatabase.LoadAssetAtPath<CharacterConfig>(path);
                if (config == null)
                {
                    results.Add($"{path}: NULL");
                    continue;
                }

                var so = new SerializedObject(config);
                results.Add($"{path}: {config.GetType().Name} maxHp={so.FindProperty("maxHp").intValue} " +
                            $"atk={so.FindProperty("basicAttackDamage").intValue} " +
                            $"walk={so.FindProperty("walkDistance").intValue} " +
                            $"xp={so.FindProperty("xpReward")?.intValue ?? -1}");
            }

            results.Add("--- entidades na cena (edit mode) ---");
            foreach (var c in Object.FindObjectsByType<Character>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                results.Add($"{c.name}: config={(c.Config != null ? c.Config.name : "NULL")} " +
                            $"(maxHp={c.MaxHp}, atk={c.BasicAttackDamage}, walk={c.WalkDistance})");
            }

            return results;
        }
    }
}