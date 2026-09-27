using GridBattle.Gameplay.Entities;
using GridBattle.Managers;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace GridBattle.Gameplay
{
    [ExecuteAlways]
    public class GridController : MonoBehaviour
    {
        [Header("Grid Settings")] [SerializeField]
        private Vector2Int gridSize = new Vector2Int(6, 6);

        [SerializeField] private Cell cellPrefab;

        [SerializeField] [CanBeNull] public PlayerCharacter debugEntityPrefab;

        [SerializeField] [CanBeNull] public Enemy[] debugEnemyEntityPrefabs;

        private Cell[,] _cells;
        public PlayerCharacter PlayerCharacter { get; private set; }

        public static float Ppu => GameConfigManager.Ppu;

        private void Awake()
        {
            Assert.IsNotNull(cellPrefab, "Cell prefab is not set!");
            InitializeGrid();
        }

        public void InitializeGrid()
        {
            var cells = FindObjectsByType<Cell>();
            foreach (var cell in cells)
            {
                if (cell != null)
                    DestroyImmediate(cell.gameObject);
            }

            _cells = new Cell[gridSize.x, gridSize.y];

            var pitchX = (Cell.Size.x + 2) / Ppu;
            var pitchY = (Cell.Size.y + 2) / Ppu;

            var originX = -(gridSize.x - 1) * pitchX / 2f;
            var originY = (gridSize.y - 1) * pitchY / 2f;

            var spriteBounds = cellPrefab.GetComponent<SpriteRenderer>().sprite.bounds;
            var pivotOffset = new Vector3(spriteBounds.center.x, spriteBounds.center.y, 0);

            for (var x = 0; x < gridSize.x; x++)
            {
                for (var y = 0; y < gridSize.y; y++)
                {
                    var pos = new Vector3(
                        originX + x * pitchX,
                        originY - y * pitchY,
                        0
                    ) - pivotOffset;

                    var instance = Instantiate(cellPrefab, pos, Quaternion.identity, new InstantiateParameters
                    {
                        parent = transform,
                        worldSpace = false
                    });
                    instance.gameObject.name = $"Cell_{x}_{y}";
                    _cells[x, y] = instance;
                }
            }

            if (debugEntityPrefab != null)
            {
                var cell = _cells[2, 2];
                var debugInstance = Instantiate(debugEntityPrefab, new Vector3(0, 0, 0), Quaternion.identity,
                    new InstantiateParameters
                    {
                        parent = cell.transform,
                        worldSpace = false
                    }
                );
                cell.SetContent(debugInstance);
                PlayerCharacter = debugInstance;
            }

            if (debugEnemyEntityPrefabs != null)
            {
                var length = debugEnemyEntityPrefabs.Length;
                for (int i = 0; i < length; i++)
                {
                    foreach (var cell in _cells)
                    {
                        if (cell.HasContent)
                            continue;

                        var debugEnemyEntityPrefab = debugEnemyEntityPrefabs[i];
                        var debugInstance = Instantiate(debugEnemyEntityPrefab, new Vector3(0, 0, 0),
                            Quaternion.identity,
                            new InstantiateParameters
                            {
                                parent = cell.transform,
                                worldSpace = false
                            }
                        );
                        cell.SetContent(debugInstance);
                        break;
                    }
                }
            }
        }
    }
}