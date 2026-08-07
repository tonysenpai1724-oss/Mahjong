using System.Collections.Generic;
using MahjongOut3D.Core;
using MahjongOut3D.Data;
using MahjongOut3D.Managers;
using MahjongOut3D.TileSystem;
using MahjongOut3D.Utilities;
using MahjongOut3D;
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
        private const string DefaultFallbackVisualSourceName = "Bamboo_1";
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
        [SerializeField] private GameObject fallbackVisualSource;
        [SerializeField] private TileVisualSettings fallbackTileVisualSettings;
       // [SerializeField] private Material[] matchIndicatorMaterials;

        [Header("Tile Tuning")]
        [SerializeField] private bool applyTileBaseColor = true;
        [SerializeField] private Color tileBaseColor = Color.white;
        [SerializeField] private Vector3 tileSpacingOffset;
        [SerializeField, Range(0.5f, 1f)] private float surfaceShellSeparationScale = 0.8f;

        [Header("Generation")]
        [SerializeField] private bool generateOnStart;
        [SerializeField] private bool clearExistingChildrenOnGenerate = true;
        [SerializeField] private bool usePooling = true;
        [SerializeField, Min(1f)] private float cameraFramePaddingOnLoad = 1.7f;

        private readonly List<MahjongTile> spawnedTiles = new List<MahjongTile>();
        private readonly Dictionary<int, Texture2D> fillTexturesByMatchId = new Dictionary<int, Texture2D>();
        private readonly List<Texture2D> activeLevelFillTextures = new List<Texture2D>();
        private ComponentPool<MahjongTile> tilePool;
        private GameContext context;
        private LevelManager levelManager;
        private TileManager tileManager;
        private CameraManager cameraManager;
        private ZoomSlider zoomSlider;
        private int nextTileId;
        private MahjongTile runtimeFallbackTilePrefab;
        private Quaternion defaultTileRootLocalRotation = Quaternion.identity;
        public MahjongMaterialSO mahjongMaterialSO;
        private Material pieceBaseMaterial;
        private Texture2D pieceTexture;
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
            zoomSlider = FindFirstObjectByType<ZoomSlider>(FindObjectsInactive.Exclude);

            if (tileRoot == null)
            {
                tileRoot = transform;
            }

            defaultTileRootLocalRotation = tileRoot != null ? tileRoot.localRotation : Quaternion.identity;

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
            levelManager?.SetActiveLevelDefinition(definition, definition.UseSurfaceTilePlacement);
            ConfigureFillTexturePool(definition.FillCategoryNames);

            IList<LevelTileDefinition> runtimeTiles = BuildRuntimeTileDefinitions(
                definition.UseSurfaceTilePlacement,
                definition.Shape,
                definition.LayoutOverride,
                definition.Tiles);
            return Generate(definition.LevelName, definition.GetRuntimeGridSize(), definition.LayoutOverride, runtimeTiles);
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
            levelManager?.SetActiveLevelDefinition(null, jsonData.useSurfaceTilePlacement);
            ConfigureFillTexturePool(jsonData.fillCategoryNames);
            IList<LevelTileDefinition> runtimeTiles = BuildRuntimeTileDefinitions(
                jsonData.useSurfaceTilePlacement,
                jsonData.shape,
                null,
                LevelJsonSerializer.ToTileDefinitions(jsonData));
            return Generate(jsonData.levelName, gridSize, null, runtimeTiles);
        }
        /// <summary>
        /// Returns a random piece texture from the configured MahjongMaterialSO.
        /// </summary>
        public Texture2D RandomPieceTexture()
        {
            if (mahjongMaterialSO == null)
            {
                return null;
            }

            List<Texture2D> activePieceTextures = mahjongMaterialSO.GetActivePieceTextures();
            if (activePieceTextures == null || activePieceTextures.Count == 0)
            {
                return null;
            }

            int randomIndex = Random.Range(0, activePieceTextures.Count);
            return activePieceTextures[randomIndex];
        }

        private Texture2D GetFillTextureForMatch(int matchId)
        {
            if (activeLevelFillTextures.Count == 0)
            {
                return null;
            }

            if (fillTexturesByMatchId.TryGetValue(matchId, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            int resolvedIndex = Mathf.Abs(matchId) % activeLevelFillTextures.Count;
            Texture2D resolvedTexture = activeLevelFillTextures[resolvedIndex];
            fillTexturesByMatchId[matchId] = resolvedTexture;
            return resolvedTexture;
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

            levelManager?.SetActiveLevelDefinition(null, false);
            ConfigureFillTexturePool(null);
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

            levelManager?.SetActiveLevelDefinition(null, false);
            ConfigureFillTexturePool(null);
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
            fillTexturesByMatchId.Clear();
            activeLevelFillTextures.Clear();
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
            pieceBaseMaterial = mahjongMaterialSO != null ? mahjongMaterialSO.PieceBaseMaterial : null;
            pieceTexture = RandomPieceTexture();
            if (activeLevelFillTextures.Count == 0)
            {
                ConfigureFillTexturePool(levelDefinition != null ? levelDefinition.FillCategoryNames : null);
            }
          
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

        private void ConfigureFillTexturePool(IList<string> categoryNames)
        {
            activeLevelFillTextures.Clear();
            fillTexturesByMatchId.Clear();

            if (mahjongMaterialSO == null)
            {
                return;
            }

            List<Texture2D> resolvedTextures = mahjongMaterialSO.GetActiveFillTextures(categoryNames);
            if ((resolvedTextures == null || resolvedTextures.Count == 0) && categoryNames != null && categoryNames.Count > 0)
            {
                resolvedTextures = mahjongMaterialSO.GetActiveFillTextures();
            }

            if (resolvedTextures == null || resolvedTextures.Count == 0)
            {
                return;
            }

            activeLevelFillTextures.AddRange(resolvedTextures);
            Shuffle(activeLevelFillTextures);
        }

        private static void Shuffle<TValue>(IList<TValue> values)
        {
            if (values == null)
            {
                return;
            }

            for (int index = values.Count - 1; index > 0; index--)
            {
                int swapIndex = Random.Range(0, index + 1);
                TValue temporary = values[index];
                values[index] = values[swapIndex];
                values[swapIndex] = temporary;
            }
        }

        /// <summary>
        /// Returns the authored tile list without applying any runtime cube migration.
        /// </summary>
        private IList<LevelTileDefinition> BuildRuntimeTileDefinitions(bool useSurfaceTilePlacement, LevelShapeType shape, VoxelGridLayoutSettings layoutOverride, IList<LevelTileDefinition> sourceTiles)
        {
            if (sourceTiles == null)
            {
                return sourceTiles;
            }

            List<LevelTileDefinition> runtimeTiles = new List<LevelTileDefinition>(sourceTiles.Count);
            List<float> shellMagnitudes = useSurfaceTilePlacement ? new List<float>(sourceTiles.Count) : null;

            for (int index = 0; index < sourceTiles.Count; index++)
            {
                LevelTileDefinition clone = CloneTileDefinition(sourceTiles[index]);
                if (clone == null)
                {
                    continue;
                }

                runtimeTiles.Add(clone);
                if (!useSurfaceTilePlacement)
                {
                    continue;
                }

                VoxelGridDirection facingDirection = ResolveFacingDirection(clone.LocalEulerAngles);
                float shellMagnitude = GetCubeShellNormalMagnitude(clone.LocalPosition, facingDirection);
                shellMagnitudes.Add(shellMagnitude);
            }

            if (!useSurfaceTilePlacement)
            {
                return runtimeTiles;
            }

            List<float> uniqueMagnitudes = BuildUniqueDescendingMagnitudes(shellMagnitudes);
            for (int index = 0; index < runtimeTiles.Count; index++)
            {
                LevelTileDefinition tile = runtimeTiles[index];
                if (tile == null || tile.SurfaceShellIndex > 0 || !tile.UseCustomLocalPosition)
                {
                    continue;
                }

                VoxelGridDirection facingDirection = ResolveFacingDirection(tile.LocalEulerAngles);
                float shellMagnitude = GetCubeShellNormalMagnitude(tile.LocalPosition, facingDirection);
                tile.SurfaceShellIndex = ResolveShellIndex(shellMagnitude, uniqueMagnitudes);
            }

            CompactSurfaceShellLayers(runtimeTiles, layoutOverride);

            return runtimeTiles;
        }

        /// <summary>
        /// Pulls nested surface shells slightly closer together while preserving their order and cube-like layering.
        /// This runs at load time so existing generated level assets also benefit from tighter layer spacing.
        /// </summary>
        private void CompactSurfaceShellLayers(IList<LevelTileDefinition> runtimeTiles, VoxelGridLayoutSettings layoutOverride)
        {
            if (runtimeTiles == null || runtimeTiles.Count == 0 || surfaceShellSeparationScale >= 0.9999f)
            {
                return;
            }

            float shellThickness = GetSurfaceShellThickness(layoutOverride);
            if (shellThickness <= Mathf.Epsilon)
            {
                return;
            }

            for (int index = 0; index < runtimeTiles.Count; index++)
            {
                LevelTileDefinition tile = runtimeTiles[index];
                if (tile == null || tile.SurfaceShellIndex <= 0 || !tile.UseCustomLocalPosition)
                {
                    continue;
                }

                VoxelGridDirection facingDirection = ResolveFacingDirection(tile.LocalEulerAngles);
                float faceStep = GetSurfaceFaceStep(layoutOverride, facingDirection);
                float authoredGap = Mathf.Max(0f, faceStep - shellThickness);
                if (authoredGap <= Mathf.Epsilon)
                {
                    continue;
                }

                float targetGap = authoredGap * Mathf.Clamp(surfaceShellSeparationScale, 0f, 1f);
                float additionalOutwardOffset = (targetGap - authoredGap) * tile.SurfaceShellIndex;
                if (Mathf.Abs(additionalOutwardOffset) <= 0.0001f)
                {
                    continue;
                }

                Vector3 faceNormal = ((Vector3)VoxelGridDirections.GetOffset(facingDirection)).normalized;
                tile.LocalPosition += faceNormal * additionalOutwardOffset;
            }
        }

        /// <summary>
        /// Creates a detached tile-definition copy safe for runtime adjustments.
        /// </summary>
        private static LevelTileDefinition CloneTileDefinition(LevelTileDefinition source)
        {
            if (source == null)
            {
                return null;
            }

            return new LevelTileDefinition
            {
                MatchId = source.MatchId,
                GridCoordinate = source.GridCoordinate,
                SurfaceShellIndex = source.SurfaceShellIndex,
                UseCustomLocalPosition = source.UseCustomLocalPosition,
                LocalPosition = source.LocalPosition,
                LocalEulerAngles = source.LocalEulerAngles,
            };
        }

        /// <summary>
        /// Resolves which cardinal face a custom-placed cube tile is pointing toward.
        /// </summary>
        private static VoxelGridDirection ResolveFacingDirection(Vector3 localEulerAngles)
        {
            Vector3 normal = Quaternion.Euler(localEulerAngles) * Vector3.up;
            Vector3 absoluteNormal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

            if (absoluteNormal.x >= absoluteNormal.y && absoluteNormal.x >= absoluteNormal.z)
            {
                return normal.x >= 0f ? VoxelGridDirection.Right : VoxelGridDirection.Left;
            }

            if (absoluteNormal.y >= absoluteNormal.x && absoluteNormal.y >= absoluteNormal.z)
            {
                return normal.y >= 0f ? VoxelGridDirection.Up : VoxelGridDirection.Down;
            }

            return normal.z >= 0f ? VoxelGridDirection.Forward : VoxelGridDirection.Back;
        }

        /// <summary>
        /// Reads the magnitude on the axis normal to the resolved cube face.
        /// </summary>
        private static float GetCubeShellNormalMagnitude(Vector3 localPosition, VoxelGridDirection facingDirection)
        {
            switch (facingDirection)
            {
                case VoxelGridDirection.Left:
                case VoxelGridDirection.Right:
                    return Mathf.Abs(localPosition.x);

                case VoxelGridDirection.Down:
                case VoxelGridDirection.Up:
                    return Mathf.Abs(localPosition.y);

                case VoxelGridDirection.Back:
                case VoxelGridDirection.Forward:
                default:
                    return Mathf.Abs(localPosition.z);
            }
        }

        /// <summary>
        /// Applies a corrected magnitude on the axis normal to the resolved cube face.
        /// </summary>
        private static Vector3 SetCubeShellNormalMagnitude(Vector3 localPosition, VoxelGridDirection facingDirection, float correctedMagnitude)
        {
            switch (facingDirection)
            {
                case VoxelGridDirection.Left:
                case VoxelGridDirection.Right:
                    localPosition.x = Mathf.Sign(localPosition.x == 0f ? (facingDirection == VoxelGridDirection.Right ? 1f : -1f) : localPosition.x) * correctedMagnitude;
                    return localPosition;

                case VoxelGridDirection.Down:
                case VoxelGridDirection.Up:
                    localPosition.y = Mathf.Sign(localPosition.y == 0f ? (facingDirection == VoxelGridDirection.Up ? 1f : -1f) : localPosition.y) * correctedMagnitude;
                    return localPosition;

                case VoxelGridDirection.Back:
                case VoxelGridDirection.Forward:
                default:
                    localPosition.z = Mathf.Sign(localPosition.z == 0f ? (facingDirection == VoxelGridDirection.Forward ? 1f : -1f) : localPosition.z) * correctedMagnitude;
                    return localPosition;
            }
        }

        /// <summary>
        /// Collapses repeated shell magnitudes into one descending set used to infer shell depth.
        /// </summary>
        private static List<float> BuildUniqueDescendingMagnitudes(List<float> magnitudes)
        {
            List<float> uniqueMagnitudes = new List<float>();
            if (magnitudes == null || magnitudes.Count == 0)
            {
                return uniqueMagnitudes;
            }

            magnitudes.Sort((left, right) => right.CompareTo(left));
            for (int index = 0; index < magnitudes.Count; index++)
            {
                float magnitude = magnitudes[index];
                if (uniqueMagnitudes.Count == 0 || Mathf.Abs(uniqueMagnitudes[uniqueMagnitudes.Count - 1] - magnitude) > 0.0001f)
                {
                    uniqueMagnitudes.Add(magnitude);
                }
            }

            return uniqueMagnitudes;
        }

        /// <summary>
        /// Maps a shell magnitude back to its outer-to-inner shell index.
        /// </summary>
        private static int ResolveShellIndex(float magnitude, List<float> uniqueMagnitudes)
        {
            if (uniqueMagnitudes == null || uniqueMagnitudes.Count == 0)
            {
                return 0;
            }

            int bestIndex = 0;
            float bestDelta = Mathf.Abs(uniqueMagnitudes[0] - magnitude);
            for (int index = 1; index < uniqueMagnitudes.Count; index++)
            {
                float delta = Mathf.Abs(uniqueMagnitudes[index] - magnitude);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    bestIndex = index;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Resolves the corrected center offset used by the current cube-shell generator for the supplied face.
        /// </summary>
        private static float GetCorrectedCubeShellNormalMagnitude(int shellWidth, Vector3 cellSize, VoxelGridDirection facingDirection)
        {
            float shellCount = Mathf.Max(2, shellWidth);
            switch (facingDirection)
            {
                case VoxelGridDirection.Left:
                case VoxelGridDirection.Right:
                case VoxelGridDirection.Down:
                case VoxelGridDirection.Up:
                    return Mathf.Max(0.01f, (shellCount * cellSize.x) - cellSize.y) * 0.5f;

                case VoxelGridDirection.Back:
                case VoxelGridDirection.Forward:
                default:
                    return Mathf.Max(0.01f, (shellCount * cellSize.z) - cellSize.y) * 0.5f;
            }
        }

        /// <summary>
        /// Resolves the face-normal step distance used by wrapped surface tiles.
        /// </summary>
        private float GetSurfaceFaceStep(VoxelGridLayoutSettings layoutOverride, VoxelGridDirection facingDirection)
        {
            VoxelGridLayoutSettings resolvedLayout = layoutOverride != null ? layoutOverride : levelManager != null ? levelManager.DefaultGridLayout : null;
            Vector3 step = resolvedLayout != null ? resolvedLayout.CellStep : new Vector3(0.95f, 0.45f, 0.7f);

            switch (facingDirection)
            {
                case VoxelGridDirection.Left:
                case VoxelGridDirection.Right:
                    return Mathf.Max(0.01f, step.x);

                case VoxelGridDirection.Down:
                case VoxelGridDirection.Up:
                    return Mathf.Max(0.01f, step.y);

                case VoxelGridDirection.Back:
                case VoxelGridDirection.Forward:
                default:
                    return Mathf.Max(0.01f, step.z);
            }
        }

        /// <summary>
        /// Resolves the effective tile thickness used when tightening wrapped surface shells.
        /// </summary>
        private float GetSurfaceShellThickness(VoxelGridLayoutSettings layoutOverride)
        {
            MahjongTile template = tilePrefab != null ? tilePrefab : runtimeFallbackTilePrefab;
            if (template != null)
            {
                float placementThickness = template.GetPlacementSize().y;
                if (placementThickness > 0.01f)
                {
                    return placementThickness;
                }
            }

            VoxelGridLayoutSettings resolvedLayout = layoutOverride != null ? layoutOverride : levelManager != null ? levelManager.DefaultGridLayout : null;
            Vector3 cellSize = resolvedLayout != null ? resolvedLayout.CellSize : new Vector3(0.95f, 0.45f, 0.7f);
            return Mathf.Max(0.01f, cellSize.y);
        }

        /// <summary>
        /// Spawns a single tile instance and registers it with the runtime systems.
        /// </summary>
        private void SpawnTile(VoxelGridData grid, LevelTileDefinition definition)
        {
            Transform parent = tileRoot == null ? transform : tileRoot;
            MahjongTile template = tilePrefab != null ? tilePrefab : runtimeFallbackTilePrefab;
            MahjongTile tile = usePooling && tilePool != null ? tilePool.Get(parent) : Instantiate(template, parent);
            Quaternion spawnRotation = Quaternion.Euler(definition.LocalEulerAngles);
            Vector3 baseLocalPosition = ApplyTileSpacing(definition.UseCustomLocalPosition ? definition.LocalPosition : grid.GetLocalPosition(definition.GridCoordinate));
            Vector3 placementOffset = spawnRotation * tile.GetPlacementOffset();
            TileRuntimeData runtimeData = new TileRuntimeData
            {
                TileId = nextTileId++,
                MatchId = definition.MatchId,
                GridCoordinate = definition.GridCoordinate,
                LocalPosition = baseLocalPosition - placementOffset,
                LocalEulerAngles = definition.LocalEulerAngles,
                SurfaceShellIndex = definition.SurfaceShellIndex,
            };

            tile.ApplyRuntimeData(runtimeData);
            tile.Setup(pieceBaseMaterial, mahjongMaterialSO != null ? mahjongMaterialSO.FillBaseMaterial : null);
            tile.SetupPieceTexture(pieceTexture);
            tile.SetupFillTexture(GetFillTextureForMatch(definition.MatchId));
            if (applyTileBaseColor)
            {
                tile.SetDebugMatchColor(tileBaseColor);
            }
            else
            {
                tile.ClearDebugMatchColor();
            }

            //tile.SetMatchIndicatorMaterial(GetMatchIndicatorMaterial(definition.MatchId));
            tile.ResetTile();
            grid.TryPlaceTile(tile.TileId, definition.GridCoordinate);
            tileManager.RegisterTile(tile);
            spawnedTiles.Add(tile);
        }

        /// <summary>
        /// Resolves the optional test material used to identify a tile match group.
        /// </summary>
        /// <param name="matchId">Match identifier.</param>
        /// <returns>Indicator material for that match group, or null when none is configured.</returns>
        // private Material GetMatchIndicatorMaterial(int matchId)
        // {
        //     if (matchIndicatorMaterials == null || matchIndicatorMaterials.Length == 0)
        //     {
        //         return null;
        //     }

        //     int materialIndex = Mathf.Abs(matchId) % matchIndicatorMaterials.Length;
        //     return matchIndicatorMaterials[materialIndex];
        // }

        /// <summary>
        /// Applies an outward spacing offset so the distance between tiles can be tuned from the inspector.
        /// </summary>
        /// <param name="localPosition">Original tile local position.</param>
        /// <returns>Adjusted tile local position.</returns>
        private Vector3 ApplyTileSpacing(Vector3 localPosition)
        {
            if (tileSpacingOffset == Vector3.zero || localPosition.sqrMagnitude <= Mathf.Epsilon)
            {
                return localPosition;
            }

            Vector3 spacingScale = new Vector3(
                Mathf.Max(0f, 1f + tileSpacingOffset.x),
                Mathf.Max(0f, 1f + tileSpacingOffset.y),
                Mathf.Max(0f, 1f + tileSpacingOffset.z));

            return Vector3.Scale(localPosition, spacingScale);
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

            Transform rotationRoot = tileRoot != null ? tileRoot : transform;
            if (rotationRoot != null)
            {
                rotationRoot.localRotation = defaultTileRootLocalRotation;
            }

            cameraManager.SetRotationTarget(rotationRoot);

            Bounds worldBounds;
            if (!TryBuildSpawnedTileBounds(out worldBounds))
            {
                Bounds localBounds = grid.GetLocalBounds();
                worldBounds = TransformBounds(rotationRoot, localBounds);
            }

            cameraManager.FrameBounds(worldBounds, cameraFramePaddingOnLoad, true, true);
            zoomSlider?.SyncWithCamera();
        }

        /// <summary>
        /// Builds a world-space bounds volume that encloses every spawned tile.
        /// </summary>
        /// <param name="worldBounds">Combined world-space bounds.</param>
        /// <returns>True when at least one valid bounds source was found; otherwise false.</returns>
        private bool TryBuildSpawnedTileBounds(out Bounds worldBounds)
        {
            worldBounds = default;
            bool hasBounds = false;

            for (int index = 0; index < spawnedTiles.Count; index++)
            {
                MahjongTile tile = spawnedTiles[index];
                if (tile == null)
                {
                    continue;
                }

                if (tile.TileCollider != null)
                {
                    if (!hasBounds)
                    {
                        worldBounds = tile.TileCollider.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        worldBounds.Encapsulate(tile.TileCollider.bounds);
                    }

                    continue;
                }

                Renderer[] renderers = tile.GetComponentsInChildren<Renderer>(false);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer == null)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        worldBounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        worldBounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return hasBounds;
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

            CreatePrimitiveFallbackTile();
        }

        /// <summary>
        /// Creates the old primitive-based fallback when no imported Mahjong source is available.
        /// </summary>
        private void CreatePrimitiveFallbackTile()
        {

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

                MeshRenderer sourceRenderer = ResolveFallbackMaterialRenderer();
                if (sourceRenderer != null && sourceRenderer.sharedMaterials != null && sourceRenderer.sharedMaterials.Length > 0)
                {
                    renderer.sharedMaterial = sourceRenderer.sharedMaterials[0];
                }
                else
                {
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
        /// Resolves the imported Mahjong visual source used for runtime fallback tiles.
        /// </summary>
        /// <returns>Resolved source object when available; otherwise null.</returns>
        private GameObject ResolveFallbackVisualSource()
        {
            if (fallbackVisualSource != null)
            {
                return fallbackVisualSource;
            }

            GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < sceneObjects.Length; index++)
            {
                GameObject candidate = sceneObjects[index];
                if (candidate != null && candidate.name.Equals(DefaultFallbackVisualSourceName, System.StringComparison.OrdinalIgnoreCase))
                {
                    fallbackVisualSource = candidate;
                    return fallbackVisualSource;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves the renderer used only as a material donor for fallback runtime tiles.
        /// </summary>
        /// <returns>Renderer providing Mahjong materials when available; otherwise null.</returns>
        private MeshRenderer ResolveFallbackMaterialRenderer()
        {
            GameObject source = ResolveFallbackVisualSource();
            return source != null ? source.GetComponentInChildren<MeshRenderer>(true) : null;
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
