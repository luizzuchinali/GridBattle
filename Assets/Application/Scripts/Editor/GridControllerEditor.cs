using GridBattle.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GridBattle.Editor
{
    [CustomEditor(typeof(GridController))]
    public class GridControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var controller = (GridController)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Reset grid"))
            {
                controller.InitializeGrid(controller.DebugPlayerConfig, true);

                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            }
        }
    }
}
