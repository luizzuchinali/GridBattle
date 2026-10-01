using System;
using System.Collections.Generic;
using GridBattle.Gameplay.Entities;
using GridBattle.Managers;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace GridBattle.Gameplay
{
    /// <summary>
    /// Owns the board: creates the cells, places the player and the encounter's
    /// enemies (via CharacterFactory) and centralizes occupancy queries and
    /// moves.
    /// </summary>
    [ExecuteAlways]
    public class GridController : MonoBehaviour
    {
        private static readonly Vector2Int PlayerSpawnPosition = new(2, 2);

        [Header("Grid Settings")]
        [SerializeField]
        private Vector2Int gridSize = new Vector2Int(6, 6);

        [SerializeField]
        private Cell cellPrefab;

        [Header("Spawn")]
        [SerializeField]
        [Tooltip("Enemies placed when the grid is initialized.")]
        private EncounterConfig encounter;

        [SerializeField]
        [CanBeNull]
        [Tooltip("Character used when the grid is initialized from the editor, without going through the menu.")]
        private PlayerCharacterConfig debugPlayerConfig;

        private Cell[,] _cells;

        public static float Ppu => GameConfigManager.Ppu;

        [CanBeNull]
        public PlayerCharacterConfig DebugPlayerConfig => debugPlayerConfig;

        private void Awake()
        {
            Assert.IsNotNull(cellPrefab, "Cell prefab is not set!");
#if UNITY_EDITOR
            InitializeGrid(debugPlayerConfig);
#endif
        }

        public void InitializeGrid([CanBeNull] PlayerCharacterConfig playerConfig)
        {
            BuildCells();
            SpawnPlayer(playerConfig);
            SpawnEncounter();
        }

        private void BuildCells()
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
                    instance.GridPosition = new Vector2Int(x, y);
                    _cells[x, y] = instance;
                }
            }
        }

        private void SpawnPlayer([CanBeNull] PlayerCharacterConfig playerConfig)
        {
            if (playerConfig == null) return;

            var player = CharacterFactory.Spawn(playerConfig);
            if (player == null) return;

            player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _cells[PlayerSpawnPosition.x, PlayerSpawnPosition.y].SetContent(player);
        }

        private void SpawnEncounter()
        {
            if (encounter == null) return;

            foreach (var enemyConfig in encounter.Enemies)
            {
                if (enemyConfig == null) continue;

                var cell = FindFirstFreeCell();
                if (cell == null) return;

                var enemy = CharacterFactory.Spawn(enemyConfig);
                if (enemy == null) continue;

                enemy.transform.SetParent(cell.transform, false);
                cell.SetContent(enemy);
            }
        }

        [CanBeNull]
        private Cell FindFirstFreeCell()
        {
            foreach (var cell in _cells)
            {
                if (!cell.HasContent)
                    return cell;
            }

            return null;
        }

        public void MoveEntity(GridEntity entity, Vector2Int targetPos)
        {
            Move(entity.CurrentGridPos, targetPos);
            entity.CurrentGridPos = targetPos;
        }

        public bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < gridSize.x &&
                   position.y >= 0 && position.y < gridSize.y;
        }

        public bool IsFreePosition(Vector2Int position)
        {
            return !_cells[position.x, position.y].HasContent;
        }

        public GridEntity GetContent(Vector2Int position)
        {
            return _cells[position.x, position.y].GetContent();
        }

        public void Move(Vector2Int currentPos, Vector2Int targetPos)
        {
            if (currentPos == targetPos)
                return;

            if (!IsFreePosition(targetPos))
                throw new InvalidOperationException("Target position is not free");

            var content = _cells[currentPos.x, currentPos.y].GetContent();
            _cells[currentPos.x, currentPos.y].RemoveContent();
            _cells[targetPos.x, targetPos.y].SetContent(content);
        }

        public void HighlightCells(Dictionary<Vector2Int, ECellHighlightType> highlightInfos)
        {
            foreach (var cell in _cells)
            {
                if (highlightInfos.TryGetValue(cell.GridPosition, out var highlightType))
                {
                    cell.Highlighted = true;
                    cell.HighlightType = highlightType;
                }
                else
                {
                    cell.Highlighted = false;
                }
            }
        }
    }
}
