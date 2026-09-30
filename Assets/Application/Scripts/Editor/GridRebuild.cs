using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using GridBattle.Gameplay;
using GridBattle.Gameplay.Entities;

namespace GridBattle.EditorTools
{
    public static class GridRebuild
    {
        public static List<string> Run()
        {
            var results = new List<string>();

            var grid = Object.FindAnyObjectByType<GridController>();
            if (grid == null)
            {
                results.Add("GridController not found!");
                return results;
            }

            var so = new SerializedObject(grid);
            var prefab = so.FindProperty("debugEntityPrefab").objectReferenceValue as PlayerCharacter;
            results.Add($"debugEntityPrefab = {(prefab != null ? prefab.name : "NULL")}");

            grid.InitializeGrid(prefab);

            var scene = grid.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            results.Add("Grid rebuilt and scene saved");
            return results;
        }
    }
}