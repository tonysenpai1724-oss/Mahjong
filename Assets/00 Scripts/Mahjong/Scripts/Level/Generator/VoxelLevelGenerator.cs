using System.Collections.Generic;
using MahjongOut3D.Core;
using MahjongOut3D.Managers;
using MahjongOut3D.TileSystem;
using MahjongOut3D.Utilities;
using UnityEngine;
using UnityEngine.Rendering;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Spawns Mahjong tile instances from ScriptableObject, JSON or 3D array voxel sources.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VoxelLevelGenerator : MonoBehaviour
    {
        private static readonly Color[] DebugMatchPalette =
        {
            new Color(0.94f, 0.42f, 0.42f),
            new Color(0.31f, 0.76f, 0.93f),
            new Color(0.44f, 0.84f, 0.54f),
            new Color(0.97f, 0.73f, 0.32f),
            new Color(0.72f, 0.54f, 0.95f),
            new Color(0.96f, 0.47f, 0.75f),
            new Color(0.38f, 0.85f, 0.8f),
            new Color(0.84f, 0.84f, 0.36f),
        };

        [Header("References")]
        [SerializeField] private LevelDefinition levelDefinition;
        [SerializeField] private MahjongTile tilePrefab;
        [SerializeField] private Transform tileRoot;
        [SerializeField] private TileVisualSettings fallbackTileVisualSettings;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart;
        [SerializeField] private bool clearExistingChildrenOnGenerate = true;
        [SerializeField] private bool usePooling = true;

        private readonly List<MahjongTile> spawnedTiles = new List<MahjongTile>();
        private ComponentPool<MahjongTile> tilePool;
        private GameContext context;
        private LevelManager levelManager;
        private TileManager tileManager;
        private CameraManager cameraManager;
        private int nextTileId;
        private MahjongTile runtimeFallbackTilePrefab;

        /// <summary>
        /// Initializes the generator with the shared runtime context.
        /// </summary>
        /// <param name="gameContext">Shared runtime context.</param>
        public void Initialize(GameContext gameContext)
        {
            context = gameContext;
            context.Services.Register(typeof(VoxelLevelGenerator), this);
            levelManager = context.Services.Get<LevelManager>();
            tileManager = context.Services.Get<TileManager>();
            context.Services.TryGet(out cameraManager);

            if (tileRoot == null)
            {
                tileRoot = transform;
            }

            EnsureTileTemplate();

            if (tilePrefab != null && usePooling)
            {
                tilePool = new ComponentPool<MahjongTile>(tilePrefab);
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

            levelDefinition = definition;

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
                if (usePooling && tilePool != null)
                {
                    tilePool.Release(tile, tileRoot);
                }
                else
                {
                    Destroy(tile.gameObject);
                }
            }

            spawnedTiles.Clear();
            nextTileId = 0;
            levelManager?.ClearActiveGrid();

            if (clearExistingChildrenOnGenerate && tileRoot != null && (!usePooling || tilePool == null))
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

            EnsureTileTemplate();

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
            Transform parent = tileRoot == null ? transform : tileRoot;
            MahjongTile template = tilePrefab != null ? tilePrefab : runtimeFallbackTilePrefab;
            MahjongTile tile = usePooling && tilePool != null ? tilePool.Get(parent) : Instantiate(template, parent);
            TileRuntimeData runtimeData = new TileRuntimeData
            {
                TileId = nextTileId++,
                MatchId = definition.MatchId,
                GridCoordinate = definition.GridCoordinate,
                LocalPosition = grid.GetLocalPosition(definition.GridCoordinate),
                LocalEulerAngles = definition.LocalEulerAngles,
            };

            tile.ApplyRuntimeData(runtimeData);
            tile.SetDebugMatchColor(GetDebugMatchColor(definition.MatchId));
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
            Bounds worldBounds = TransformBounds(tileRoot != null ? tileRoot : transform, localBounds);
            cameraManager.FrameBounds(worldBounds, 1.35f);
        }

        /// <summary>
        /// Converts local-space bounds into world-space bounds using the specified transform.
        /// </summary>
        /// <param name="targetTransform">Transform providing the local-to-world conversion.</param>
        /// <param name="localBounds">Local-space bounds to convert.</param>
        /// <returns>World-space bounds.</returns>
        private static Bounds TransformBounds(Transform targetTransform, Bounds localBounds)
        {
            Vector3 center = targetTransform.TransformPoint(localBounds.center);
            Vector3 extents = localBounds.extents;

            Vector3 axisX = targetTransform.TransformVector(new Vector3(extents.x, 0f, 0f));
            Vector3 axisY = targetTransform.TransformVector(new Vector3(0f, extents.y, 0f));
            Vector3 axisZ = targetTransform.TransformVector(new Vector3(0f, 0f, extents.z));

            Vector3 worldExtents = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));

            return new Bounds(center, worldExtents * 2f);
        }

        /// <summary>
        /// Resolves a debug color for the supplied match identifier.
        /// </summary>
        /// <param name="matchId">Match identifier.</param>
        /// <returns>Consistent debug color for that pair group.</returns>
        private static Color GetDebugMatchColor(int matchId)
        {
            if (DebugMatchPalette.Length == 0)
            {
                return Color.white;
            }

            int colorIndex = Mathf.Abs(matchId) % DebugMatchPalette.Length;
            return DebugMatchPalette[colorIndex];
        }

        /// <summary>
        /// Ensures the generator has a valid tile template, creating a runtime cube fallback when needed.
        /// </summary>
        private void EnsureTileTemplate()
        {
            if (tilePrefab != null)
            {
                return;
            }

            if (runtimeFallbackTilePrefab != null)
            {
                tilePrefab = runtimeFallbackTilePrefab;
                return;
            }

            GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tileObject.name = "RuntimeFallbackMahjongTileTemplate";
            tileObject.hideFlags = HideFlags.HideAndDontSave;
            tileObject.SetActive(false);
            int defaultLayer = LayerMask.NameToLayer("Default");
            tileObject.layer = defaultLayer >= 0 ? defaultLayer : 0;
            tileObject.transform.SetParent(transform, false);
            tileObject.transform.localScale = new Vector3(0.95f, 0.45f, 0.7f);

            MeshRenderer renderer = tileObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;

                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    Material material = new Material(shader);
                    material.name = "RuntimeFallbackMahjongTileMaterial";
                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", new Color(0.96f, 0.94f, 0.9f, 1f));
                    }
                    else if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", new Color(0.96f, 0.94f, 0.9f, 1f));
                    }

                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", Color.black);
                    }

                    renderer.sharedMaterial = material;
                }
            }

            TileOutlinePresenter outlinePresenter = tileObject.GetComponent<TileOutlinePresenter>();
            if (outlinePresenter == null)
            {
                outlinePresenter = tileObject.AddComponent<TileOutlinePresenter>();
            }

            TileVisualController visualController = tileObject.GetComponent<TileVisualController>();
            if (visualController == null)
            {
                visualController = tileObject.AddComponent<TileVisualController>();
            }

            MahjongTile tile = tileObject.GetComponent<MahjongTile>();
            if (tile == null)
            {
                tile = tileObject.AddComponent<MahjongTile>();
            }

            if (fallbackTileVisualSettings != null)
            {
                SetPrivateField(visualController, "settings", fallbackTileVisualSettings);
            }

            runtimeFallbackTilePrefab = tile;
            tilePrefab = runtimeFallbackTilePrefab;

            if (usePooling)
            {
                tilePool = new ComponentPool<MahjongTile>(tilePrefab);
            }
        }

        /// <summary>
        /// Writes a serialized private field on a runtime-created component.
        /// </summary>
        /// <typeparam name="TTarget">Component type to modify.</typeparam>
        /// <typeparam name="TValue">Value type being assigned.</typeparam>
        /// <param name="target">Component instance receiving the value.</param>
        /// <param name="fieldName">Serialized field name.</param>
        /// <param name="value">Value to assign.</param>
        private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
            where TTarget : Component
        {
            if (target == null)
            {
                return;
            }

            System.Reflection.FieldInfo field = typeof(TTarget).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
