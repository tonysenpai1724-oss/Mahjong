using System.Collections.Generic;
using MahjongOut3D.Core;
using MahjongOut3D.Managers;
using MahjongOut3D.TileSystem;
using MahjongOut3D.Utilities;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Spawns Mahjong tile instances from ScriptableObject, JSON or 3D array voxel sources.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VoxelLevelGenerator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private MahjongTile tilePrefab;
        [SerializeField] private Transform tileRoot;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart;
        [SerializeField] private bool clearExistingChildrenOnGenerate = true;

        private readonly List<MahjongTile> spawnedTiles = new List<MahjongTile>();
        private GameContext context;
        private LevelManager levelManager;
        private TileManager tileManager;
        private CameraManager cameraManager;
        private int nextTileId;

        /// <summary>
        /// Initializes the generator with the shared runtime context.
        /// </summary>
        /// <param name="gameContext">Shared runtime context.</param>
        public void Initialize(GameContext gameContext)
        {
            context = gameContext;
            levelManager = context.Services.Get<LevelManager>();
            tileManager = context.Services.Get<TileManager>();
            context.Services.TryGet(out cameraManager);

            if (tileRoot == null)
            {
                tileRoot = transform;
            }
        }

        /// <summary>
        /// Generates the configured level definition once the scene starts.
        /// </summary>
        private void Start()
        {
            if (!generateOnStart || context == null || levelDefinition == null)
            {
                return;
            }

            GenerateFromDefinition(levelDefinition);
        }

        /// <summary>
        /// Generates a level from a ScriptableObject definition.
        /// </summary>
        /// <param name="definition">Definition to generate from.</param>
        /// <returns>List of spawned tiles.</returns>
        public IReadOnlyList<MahjongTile> GenerateFromDefinition(LevelDefinition definition)
        {
            if (definition == null)
            {
                MahjongRuntimeLogger.LogWarning("VoxelLevelGenerator received a null LevelDefinition.");
                return spawnedTiles;
            }

            return Generate(definition.LevelName, definition.GridSize, definition.LayoutOverride, definition.Tiles);
        }

        /// <summary>
        /// Generates a level from a JSON payload.
        /// </summary>
        /// <param name="json">JSON payload to parse.</param>
        /// <returns>List of spawned tiles.</returns>
        public IReadOnlyList<MahjongTile> GenerateFromJson(string json)
        {
            LevelJsonData jsonData = LevelJsonSerializer.FromJson(json);
            if (jsonData == null)
            {
                MahjongRuntimeLogger.LogWarning("VoxelLevelGenerator could not parse the provided level JSON.");
                return spawnedTiles;
            }

            VoxelGridSize gridSize = new VoxelGridSize(jsonData.width, jsonData.height, jsonData.depth);
            return Generate(jsonData.levelName, gridSize, null, LevelJsonSerializer.ToTileDefinitions(jsonData));
        }

        /// <summary>
        /// Generates a level from a 3D array of match ids.
        /// </summary>
        /// <param name="matchIdGrid">3D array where -1 means empty and non-negative values are match ids.</param>
        /// <returns>List of spawned tiles.</returns>
        public IReadOnlyList<MahjongTile> GenerateFromMatchIdArray(int[,,] matchIdGrid)
        {
            if (matchIdGrid == null)
            {
                return spawnedTiles;
            }

            int width = matchIdGrid.GetLength(0);
            int height = matchIdGrid.GetLength(1);
            int depth = matchIdGrid.GetLength(2);

            List<LevelTileDefinition> tiles = new List<LevelTileDefinition>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        int matchId = matchIdGrid[x, y, z];
                        if (matchId < 0)
                        {
                            continue;
                        }

                        tiles.Add(new LevelTileDefinition
                        {
                            MatchId = matchId,
                            GridCoordinate = new Vector3Int(x, y, z),
                            LocalEulerAngles = Vector3.zero,
                        });
                    }
                }
            }

            return Generate("ArrayGeneratedLevel", new VoxelGridSize(width, height, depth), null, tiles);
        }

        /// <summary>
        /// Generates a level from a 3D boolean voxel mask and auto-builds match pairs.
        /// </summary>
        /// <param name="occupancyMask">3D mask where true means the cell contains a tile.</param>
        /// <returns>List of spawned tiles.</returns>
        public IReadOnlyList<MahjongTile> GenerateFromOccupancyMask(bool[,,] occupancyMask)
        {
            if (occupancyMask == null)
            {
                return spawnedTiles;
            }

            int width = occupancyMask.GetLength(0);
            int height = occupancyMask.GetLength(1);
            int depth = occupancyMask.GetLength(2);

            List<Vector3Int> occupiedCoordinates = new List<Vector3Int>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        if (occupancyMask[x, y, z])
                        {
                            occupiedCoordinates.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }

            if (occupiedCoordinates.Count % 2 != 0)
            {
                MahjongRuntimeLogger.LogWarning("Occupancy mask contains an odd number of tiles, so auto-pair generation is impossible.");
                return spawnedTiles;
            }

            List<LevelTileDefinition> tiles = new List<LevelTileDefinition>(occupiedCoordinates.Count);
            int matchId = 0;
            for (int index = 0; index < occupiedCoordinates.Count; index += 2)
            {
                tiles.Add(new LevelTileDefinition { MatchId = matchId, GridCoordinate = occupiedCoordinates[index], LocalEulerAngles = Vector3.zero });
                tiles.Add(new LevelTileDefinition { MatchId = matchId, GridCoordinate = occupiedCoordinates[index + 1], LocalEulerAngles = Vector3.zero });
                matchId++;
            }

            return Generate("MaskGeneratedLevel", new VoxelGridSize(width, height, depth), null, tiles);
        }

        /// <summary>
        /// Clears every currently spawned tile instance.
        /// </summary>
        public void ClearGeneratedLevel()
        {
            for (int index = 0; index < spawnedTiles.Count; index++)
            {
                MahjongTile tile = spawnedTiles[index];
                if (tile == null)
                {
                    continue;
                }

                tileManager?.UnregisterTile(tile);
                Destroy(tile.gameObject);
            }

            spawnedTiles.Clear();
            nextTileId = 0;
            levelManager?.ClearActiveGrid();

            if (clearExistingChildrenOnGenerate && tileRoot != null)
            {
                for (int index = tileRoot.childCount - 1; index >= 0; index--)
                {
                    Transform child = tileRoot.GetChild(index);
                    if (child != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Builds the runtime grid, spawns tiles and focuses the orbit camera.
        /// </summary>
        private IReadOnlyList<MahjongTile> Generate(string levelName, VoxelGridSize gridSize, VoxelGridLayoutSettings layoutOverride, IList<LevelTileDefinition> tileDefinitions)
        {
            if (context == null)
            {
                MahjongRuntimeLogger.LogWarning("VoxelLevelGenerator must be initialized before generating levels.");
                return spawnedTiles;
            }

            if (tilePrefab == null)
            {
                MahjongRuntimeLogger.LogWarning("VoxelLevelGenerator has no tile prefab assigned.");
                return spawnedTiles;
            }

            ClearGeneratedLevel();

            VoxelGridData grid = levelManager.CreateGrid(gridSize, layoutOverride);
            levelManager.SetActiveGrid(grid);

            if (tileDefinitions != null)
            {
                for (int index = 0; index < tileDefinitions.Count; index++)
                {
                    LevelTileDefinition definition = tileDefinitions[index];
                    if (definition == null || !grid.Contains(definition.GridCoordinate))
                    {
                        continue;
                    }

                    SpawnTile(grid, definition);
                }
            }

            tileManager.RefreshTileExposure();
            FocusCameraOnGrid(grid);
            context.EventBus.Publish(new LevelGeneratedEvent(levelName, spawnedTiles.Count, grid));
            return spawnedTiles;
        }

        /// <summary>
        /// Spawns a single tile instance and registers it with the runtime systems.
        /// </summary>
        private void SpawnTile(VoxelGridData grid, LevelTileDefinition definition)
        {
            MahjongTile tile = Instantiate(tilePrefab, tileRoot == null ? transform : tileRoot);
            TileRuntimeData runtimeData = new TileRuntimeData
            {
                TileId = nextTileId++,
                MatchId = definition.MatchId,
                GridCoordinate = definition.GridCoordinate,
                LocalPosition = grid.GetLocalPosition(definition.GridCoordinate),
                LocalEulerAngles = definition.LocalEulerAngles,
            };

            tile.ApplyRuntimeData(runtimeData);
            tile.ResetTile();
            grid.TryPlaceTile(tile.TileId, definition.GridCoordinate);
            tileManager.RegisterTile(tile);
            spawnedTiles.Add(tile);
        }

        /// <summary>
        /// Moves the orbit camera focus to the center of the generated grid.
        /// </summary>
        private void FocusCameraOnGrid(VoxelGridData grid)
        {
            if (cameraManager == null || grid == null)
            {
                return;
            }

            Bounds localBounds = grid.GetLocalBounds();
            Vector3 worldCenter = tileRoot != null ? tileRoot.TransformPoint(localBounds.center) : transform.TransformPoint(localBounds.center);
            cameraManager.SetFocusPoint(worldCenter);
        }
    }
}
