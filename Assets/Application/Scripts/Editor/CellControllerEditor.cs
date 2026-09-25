using GridBattle.Gameplay;
using UnityEditor;
using UnityEngine;

namespace GridBattle.Editor
{
    [CustomEditor(typeof(GridController))]
    public class CellControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var controller = (GridController)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Reset grid"))
            {
                controller.InitializeGrid();
            }
        }
    }
}