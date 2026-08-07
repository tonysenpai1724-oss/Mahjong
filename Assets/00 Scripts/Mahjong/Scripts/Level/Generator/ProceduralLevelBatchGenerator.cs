using System;
using System.Collections.Generic;
using MahjongOut3D.TileSystem;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Generates many voxel-backed Mahjong level assets with different difficulty tiers from a single asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Level/Procedural Batch Generator", fileName = "ProceduralLevelBatchGenerator")]
    public sealed class ProceduralLevelBatchGenerator : ScriptableObject
    {
        private static readonly Vector3Int[] NeighborDirections =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
        };

        [SerializeField] private LevelCatalog targetCatalog;
        [SerializeField] private VoxelGridLayoutSettings layoutOverride;
        [SerializeField] private MahjongTile tilePrefab;
        [SerializeField] private string outputFolder = "Assets/00 Scripts/Mahjong/Generated Levels";
        [SerializeField] private string levelNamePrefix = "Generated";
        [SerializeField] private int seed = 20260730;
        [SerializeField] private GenerationWriteMode generationWriteMode = GenerationWriteMode.GenerateNew;
        [SerializeField] private DifficultyBatchDefinition normalSettings = DifficultyBatchDefinition.CreateNormalDefaults();
        [SerializeField] private DifficultyBatchDefinition hardSettings = DifficultyBatchDefinition.CreateHardDefaults();
        [SerializeField] private DifficultyBatchDefinition superHardSettings = DifficultyBatchDefinition.CreateSuperHardDefaults();

        public enum GenerationWriteMode
        {
            GenerateNew = 0,
            OverwriteMatching = 1,
        }

        public GenerationWriteMode WriteMode => generationWriteMode;

        /// <summary>
        /// Describes how many levels to generate for one difficulty tier and the shell-layer counts that tier may use.
        /// </summary>
        [Serializable]
        public sealed class DifficultyBatchDefinition
        {
            [SerializeField] private string label = "Normal";
            [SerializeField] private int levelCount = 5;
            [SerializeField] private int minLayerCount = 4;
            [SerializeField] private int maxLayerCount = 5;
            [SerializeField] private int minPairCount = 22;
            [SerializeField] private int maxPairCount = 24;
            [SerializeField] private float flippedTileChance = 0.5f;
            [SerializeField] private LevelShapeType shape = LevelShapeType.Cube;
            [SerializeField] private List<LevelShapeType> allowedShapes = new List<LevelShapeType>();
            [SerializeField] private LevelDifficulty difficulty = LevelDifficulty.Normal;

            /// <summary>
            /// Gets the display label used in generated asset names.
            /// </summary>
            public string Label => string.IsNullOrWhiteSpace(label) ? difficulty.ToString() : label;

            /// <summary>
            /// Gets how many levels should be generated for this tier.
            /// </summary>
            public int LevelCount => Mathf.Max(0, levelCount);

            /// <summary>
            /// Gets the minimum shell-layer count allowed for this tier.
            /// </summary>
            public int MinLayerCount => Mathf.Max(1, minLayerCount);

            /// <summary>
            /// Gets the maximum shell-layer count allowed for this tier.
            /// </summary>
            public int MaxLayerCount => Mathf.Max(MinLayerCount, maxLayerCount);

            /// <summary>
            /// Gets the minimum pair count allowed for this tier.
            /// </summary>
            public int MinPairCount => Mathf.Max(1, minPairCount);

            /// <summary>
            /// Gets the maximum pair count allowed for this tier.
            /// </summary>
            public int MaxPairCount => Mathf.Max(MinPairCount, maxPairCount);

            /// <summary>
            /// Gets the chance that a tile will be rotated 180 degrees around Y.
            /// </summary>
            public float FlippedTileChance => Mathf.Clamp01(flippedTileChance);

            /// <summary>
            /// Gets the level shape label stored in generated assets.
            /// </summary>
            public LevelShapeType Shape => shape;

            /// <summary>
            /// Gets the runtime difficulty stored in generated assets.
            /// </summary>
            public LevelDifficulty Difficulty => difficulty;

            /// <summary>
            /// Chooses a random shape from the configured pool for this tier.
            /// </summary>
            public LevelShapeType GetRandomShape(System.Random random)
            {
                LevelShapeType selectedShape;
                if (allowedShapes != null && allowedShapes.Count > 0)
                {
                    selectedShape = allowedShapes[random.Next(0, allowedShapes.Count)];
                }
                else
                {
                    selectedShape = shape;
                }

                LevelShapeType normalizedSelectedShape = NormalizeSupportedShapeType(selectedShape);
                if (normalizedSelectedShape == selectedShape)
                {
                    return normalizedSelectedShape;
                }

                return NormalizeSupportedShapeType(shape);
            }

            /// <summary>
            /// Creates the default Normal tier settings.
            /// </summary>
            public static DifficultyBatchDefinition CreateNormalDefaults()
            {
                return new DifficultyBatchDefinition
                {
                    label = "Normal",
                    levelCount = 5,
                    minLayerCount = 4,
                    maxLayerCount = 5,
                    minPairCount = 22,
                    maxPairCount = 24,
                    flippedTileChance = 0.45f,
                    shape = LevelShapeType.Cube,
                    allowedShapes = new List<LevelShapeType>(),
                    difficulty = LevelDifficulty.Normal,
                };
            }

            /// <summary>
            /// Creates the default Hard tier settings.
            /// </summary>
            public static DifficultyBatchDefinition CreateHardDefaults()
            {
                return new DifficultyBatchDefinition
                {
                    label = "Hard",
                    levelCount = 5,
                    minLayerCount = 5,
                    maxLayerCount = 7,
                    minPairCount = 40,
                    maxPairCount = 60,
                    flippedTileChance = 0.55f,
                    shape = LevelShapeType.Pyramid,
                    allowedShapes = new List<LevelShapeType>(),
                    difficulty = LevelDifficulty.Hard,
                };
            }

            /// <summary>
            /// Creates the default Super Hard tier settings.
            /// </summary>
            public static DifficultyBatchDefinition CreateSuperHardDefaults()
            {
                return new DifficultyBatchDefinition
                {
                    label = "SuperHard",
                    levelCount = 5,
                    minLayerCount = 7,
                    maxLayerCount = 9,
                    minPairCount = 72,
                    maxPairCount = 120,
                    flippedTileChance = 0.65f,
                    shape = LevelShapeType.Pyramid,
                    allowedShapes = new List<LevelShapeType>(),
                    difficulty = LevelDifficulty.Expert,
                };
            }
        }

        private static LevelShapeType NormalizeSupportedShapeType(LevelShapeType shape)
        {
            switch (shape)
            {
                case LevelShapeType.Cube:
                case LevelShapeType.Pagoda:
                case LevelShapeType.Pyramid:
                case LevelShapeType.Custom:
                    return shape;
                default:
                    return LevelShapeType.Cube;
            }
        }

        /// <summary>
        /// Caches one generated shape candidate before tile definitions are created.
        /// </summary>
        private sealed class ShapeCandidate
        {
            /// <summary>
            /// Gets or sets the selected voxel grid size.
            /// </summary>
            public VoxelGridSize GridSize { get; set; }

            /// <summary>
            /// Gets or sets the selected silhouette type.
            /// </summary>
            public LevelShapeType Shape { get; set; }

            /// <summary>
            /// Gets or sets how many shell layers this candidate was authored to expose.
            /// </summary>
            public int TargetLayerCount { get; set; }

            /// <summary>
            /// Gets or sets the shell list from outside to inside.
            /// </summary>
            public List<List<TilePlacementData>> Shells { get; set; } = new List<List<TilePlacementData>>();

            /// <summary>
            /// Gets the total amount of voxels represented by the candidate.
            /// </summary>
            public int TileCapacity
            {
                get
                {
                    int total = 0;
                    for (int index = 0; index < Shells.Count; index++)
                    {
                        total += Shells[index].Count;
                    }

                    return total;
                }
            }
        }

        /// <summary>
        /// Stores a fully generated level payload before it is written to a ScriptableObject asset.
        /// </summary>
        public sealed class GeneratedLevelData
        {
            /// <summary>
            /// Gets or sets the generated level name.
            /// </summary>
            public string LevelName { get; set; }

            /// <summary>
            /// Gets or sets the generated grid size.
            /// </summary>
            public VoxelGridSize GridSize { get; set; }

            /// <summary>
            /// Gets or sets the generated layout override.
            /// </summary>
            public VoxelGridLayoutSettings LayoutOverride { get; set; }

            /// <summary>
            /// Gets or sets the generated shape tag.
            /// </summary>
            public LevelShapeType Shape { get; set; }

            /// <summary>
            /// Gets or sets the generated difficulty tag.
            /// </summary>
            public LevelDifficulty Difficulty { get; set; }

            /// <summary>
            /// Gets or sets the generated tile list.
            /// </summary>
            public List<LevelTileDefinition> Tiles { get; set; } = new List<LevelTileDefinition>();
        }

        /// <summary>
        /// Stores one generated tile placement including its outward-facing shell direction.
        /// </summary>
        internal sealed class TilePlacementData
        {
            /// <summary>
            /// Gets or sets the source voxel coordinate that owns this face slot.
            /// </summary>
            public Vector3Int Coordinate { get; set; }

            /// <summary>
            /// Gets or sets the direction the tile face should point toward.
            /// </summary>
            public VoxelGridDirection FacingDirection { get; set; }

            /// <summary>
            /// Gets or sets the nesting depth of this tile shell, where zero is the outermost shell.
            /// </summary>
            public int ShellIndex { get; set; }

            /// <summary>
            /// Gets or sets the split-slot index on a single exposed face. Negative means centered on the face.
            /// </summary>
            public int SurfaceSlotIndex { get; set; }

            /// <summary>
            /// Gets or sets a custom local position override used by direct shell generators.
            /// </summary>
            public Vector3 CustomLocalPosition { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the custom local position override should be used.
            /// </summary>
            public bool UseCustomLocalPosition { get; set; }
        }

        /// <summary>
        /// Stores the tile face and thickness dimensions used to build square cube shells.
        /// </summary>
        internal readonly struct CubeTileMetrics
        {
            public CubeTileMetrics(float faceWidth, float faceHeight, float thickness)
            {
                FaceWidth = Mathf.Max(0.01f, faceWidth);
                FaceHeight = Mathf.Max(0.01f, faceHeight);
                Thickness = Mathf.Max(0.01f, thickness);
            }

            public float FaceWidth { get; }

            public float FaceHeight { get; }

            public float Thickness { get; }
        }

        /// <summary>
        /// Stores the resolved tile grid used by one cube shell.
        /// </summary>
        private readonly struct CubeShellPlan
        {
            public CubeShellPlan(int columnCount, int rowCount, float sideLength)
            {
                ColumnCount = Mathf.Max(1, columnCount);
                RowCount = Mathf.Max(1, rowCount);
                SideLength = Mathf.Max(0.01f, sideLength);
            }

            public int ColumnCount { get; }

            public int RowCount { get; }

            public float SideLength { get; }
        }

        /// <summary>
        /// Creates a full batch of levels for all configured difficulty tiers.
        /// </summary>
        /// <returns>Generated level payloads.</returns>
        public List<GeneratedLevelData> GenerateLevelData(int startingSequence = 0)
        {
            List<GeneratedLevelData> results = new List<GeneratedLevelData>();
            System.Random random = new System.Random(seed);
            int sequence = Mathf.Max(0, startingSequence);

            AppendGeneratedTier(results, normalSettings, ref sequence, random);
            AppendGeneratedTier(results, hardSettings, ref sequence, random);
            AppendGeneratedTier(results, superHardSettings, ref sequence, random);
            return results;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Generates ScriptableObject level assets and writes them into the configured output folder.
        /// </summary>
        /// <returns>Created level asset references.</returns>
        public List<LevelDefinition> GenerateAssets()
        {
            return GenerateAssets(generationWriteMode);
        }

        /// <summary>
        /// Generates ScriptableObject level assets and writes them into the configured output folder.
        /// </summary>
        /// <returns>Created or updated level asset references.</returns>
        public List<LevelDefinition> GenerateAssets(GenerationWriteMode writeMode)
        {
            if (targetCatalog == null)
            {
                throw new InvalidOperationException("ProceduralLevelBatchGenerator requires a target LevelCatalog.");
            }

            if (string.IsNullOrWhiteSpace(outputFolder) || !outputFolder.StartsWith("Assets", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Output folder must be a valid Unity project path starting with 'Assets'.");
            }

            EnsureFolderExists(outputFolder);
            int startingSequence = writeMode == GenerationWriteMode.GenerateNew ? GetHighestExistingSequence() : 0;
            List<GeneratedLevelData> generatedData = GenerateLevelData(startingSequence);
            List<LevelDefinition> generatedAssets = new List<LevelDefinition>(generatedData.Count);

            for (int index = 0; index < generatedData.Count; index++)
            {
                GeneratedLevelData data = generatedData[index];
                string safeLevelName = SanitizeFileName(data.LevelName);
                string assetPath = $"{outputFolder}/{safeLevelName}.asset";

                if (writeMode == GenerationWriteMode.GenerateNew)
                {
                    assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                }

                LevelDefinition asset = writeMode == GenerationWriteMode.OverwriteMatching
                    ? AssetDatabase.LoadAssetAtPath<LevelDefinition>(assetPath)
                    : null;

                if (asset == null)
                {
                    asset = CreateInstance<LevelDefinition>();
                    ApplyGeneratedData(asset, data);
                    AssetDatabase.CreateAsset(asset, assetPath);
                }
                else
                {
                    ApplyGeneratedData(asset, data);
                }

                generatedAssets.Add(asset);
            }

            ApplyCatalogEntries(generatedAssets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(this);
            return generatedAssets;
        }
#endif

        /// <summary>
        /// Appends generated levels for a single difficulty tier.
        /// </summary>
        private void AppendGeneratedTier(List<GeneratedLevelData> results, DifficultyBatchDefinition settings, ref int sequence, System.Random random)
        {
            if (settings == null || settings.LevelCount <= 0)
            {
                return;
            }

            for (int index = 0; index < settings.LevelCount; index++)
            {
                sequence++;
                ShapeCandidate candidate = CreateShapeCandidate(settings, random);
                int tileCount = GetTargetTileCount(settings, candidate.Shells, candidate.Shape, candidate.TargetLayerCount, random);
                List<TilePlacementData> occupiedCoordinates = BuildOccupiedCoordinates(candidate.Shells, tileCount, random);
                VoxelGridSize logicalGridSize = BuildLogicalGridSize(occupiedCoordinates.Count);
                List<LevelTileDefinition> tileDefinitions = BuildTileDefinitions(occupiedCoordinates, candidate.GridSize, logicalGridSize, settings, random);

                results.Add(new GeneratedLevelData
                {
                    LevelName = $"{levelNamePrefix}_{settings.Label}_{sequence:000}",
                    GridSize = logicalGridSize,
                    LayoutOverride = layoutOverride,
                    Shape = candidate.Shape,
                    Difficulty = settings.Difficulty,
                    Tiles = tileDefinitions,
                });
            }
        }

        /// <summary>
        /// Chooses a random shell-layer count inside the bounds defined for the difficulty tier.
        /// </summary>
        private static int GetRandomLayerCount(DifficultyBatchDefinition settings, System.Random random)
        {
            return random.Next(settings.MinLayerCount, settings.MaxLayerCount + 1);
        }

        /// <summary>
        /// Chooses a pair count that fits inside the selected grid.
        /// </summary>
        private static int GetRandomPairCount(DifficultyBatchDefinition settings, int availableTileCount, System.Random random)
        {
            int maxPairsByShape = Mathf.Max(1, availableTileCount / 2);
            int minPairs = Mathf.Min(settings.MinPairCount, maxPairsByShape);
            int maxPairs = Mathf.Min(settings.MaxPairCount, maxPairsByShape);
            if (maxPairs < minPairs)
            {
                maxPairs = minPairs;
            }

            return random.Next(minPairs, maxPairs + 1);
        }

        /// <summary>
        /// Chooses a compact tile count that preserves full outer layers whenever possible.
        /// </summary>
        private static int GetTargetTileCount(DifficultyBatchDefinition settings, List<List<TilePlacementData>> shells, LevelShapeType shape, int targetLayerCount, System.Random random)
        {
            if (shells == null || shells.Count == 0)
            {
                return 0;
            }

            int maxTileCount = Mathf.Max(2, settings.MaxPairCount * 2);
            int minTileCount = Mathf.Max(2, settings.MinPairCount * 2);

            if (shape == LevelShapeType.Cube)
            {
                return GetPreferredCubeTileCount(shells, targetLayerCount, minTileCount, maxTileCount);
            }

            if (shape == LevelShapeType.Pagoda || shape == LevelShapeType.Pyramid)
            {
                return GetPreferredPagodaTileCount(shells, minTileCount, maxTileCount);
            }

            List<int> completeLayerCounts = new List<int>(shells.Count);
            int cumulativeCount = 0;

            for (int shellIndex = 0; shellIndex < shells.Count; shellIndex++)
            {
                cumulativeCount += shells[shellIndex].Count;
                if (cumulativeCount >= 2 && cumulativeCount % 2 == 0)
                {
                    completeLayerCounts.Add(cumulativeCount);
                }
            }

            if (completeLayerCounts.Count == 0)
            {
                int fallbackTileCount = Mathf.Min(GetShellTileCapacity(shells), maxTileCount);
                return fallbackTileCount % 2 == 0 ? fallbackTileCount : fallbackTileCount - 1;
            }

            List<int> preferredCounts = new List<int>();
            for (int index = 0; index < completeLayerCounts.Count; index++)
            {
                int count = completeLayerCounts[index];
                if (count >= minTileCount && count <= maxTileCount)
                {
                    preferredCounts.Add(count);
                }
            }

            if (preferredCounts.Count > 0)
            {
                return preferredCounts[random.Next(0, preferredCounts.Count)];
            }

            for (int index = completeLayerCounts.Count - 1; index >= 0; index--)
            {
                if (completeLayerCounts[index] <= maxTileCount)
                {
                    return completeLayerCounts[index];
                }
            }

            return completeLayerCounts[0];
        }

        private static int GetPreferredPagodaTileCount(List<List<TilePlacementData>> shells, int minTileCount, int maxTileCount)
        {
            int cumulativeCount = 0;
            int smallestValidCount = 0;
            int bestInRangeCount = 0;

            for (int index = 0; index < shells.Count; index++)
            {
                cumulativeCount += Mathf.Max(0, shells[index].Count);
                if (cumulativeCount < 2 || cumulativeCount % 2 != 0)
                {
                    continue;
                }

                if (smallestValidCount == 0)
                {
                    smallestValidCount = cumulativeCount;
                }

                if (cumulativeCount >= minTileCount && cumulativeCount <= maxTileCount)
                {
                    bestInRangeCount = cumulativeCount;
                }
            }

            if (bestInRangeCount >= 2)
            {
                return bestInRangeCount;
            }

            if (smallestValidCount >= 2)
            {
                return smallestValidCount;
            }

            int fallbackCount = GetShellTileCapacity(shells);
            return fallbackCount % 2 == 0 ? fallbackCount : fallbackCount - 1;
        }

        /// <summary>
        /// Chooses a visually complete cube by keeping whole shells from the outermost cube inward.
        /// </summary>
        private static int GetPreferredCubeTileCount(List<List<TilePlacementData>> shells, int minimumLayerCount, int minTileCount, int maxTileCount)
        {
            if (shells == null || shells.Count == 0)
            {
                return 0;
            }

            int cumulativeCount = 0;
            int bestCount = 0;
            int bestLayerCount = 0;
            for (int index = 0; index < shells.Count; index++)
            {
                cumulativeCount += Mathf.Max(0, shells[index].Count);
                if (cumulativeCount < 2 || cumulativeCount % 2 != 0)
                {
                    continue;
                }

                int layerCount = index + 1;
                if (layerCount >= Mathf.Max(1, minimumLayerCount) && cumulativeCount >= minTileCount && cumulativeCount <= maxTileCount)
                {
                    return cumulativeCount;
                }

                if (cumulativeCount <= maxTileCount && layerCount > bestLayerCount)
                {
                    bestCount = cumulativeCount;
                    bestLayerCount = layerCount;
                }
            }

            if (bestCount >= 2)
            {
                return bestCount;
            }

            int fallbackCount = 0;
            int targetLayerIndex = Mathf.Clamp(Mathf.Max(1, minimumLayerCount) - 1, 0, shells.Count - 1);
            for (int index = 0; index <= targetLayerIndex; index++)
            {
                fallbackCount += Mathf.Max(0, shells[index].Count);
            }

            return fallbackCount % 2 == 0 ? fallbackCount : fallbackCount - 1;
        }

        /// <summary>
        /// Generates a shape candidate with enough voxels to support the tier.
        /// </summary>
        private ShapeCandidate CreateShapeCandidate(DifficultyBatchDefinition settings, System.Random random)
        {
            ShapeCandidate bestCandidate = null;
            int requestedTileCount = settings.MinPairCount * 2;

            for (int attempt = 0; attempt < 12; attempt++)
            {
                LevelShapeType selectedShape = settings.GetRandomShape(random);
                int targetLayerCount = GetRandomLayerCount(settings, random);
                VoxelGridSize gridSize = BuildGridSizeForShape(targetLayerCount, selectedShape);
                List<List<TilePlacementData>> shells = BuildShapeShells(gridSize, selectedShape, targetLayerCount, settings);
                if (GetShellTileCapacity(shells) < 2)
                {
                    continue;
                }

                ShapeCandidate candidate = new ShapeCandidate
                {
                    GridSize = gridSize,
                    Shape = selectedShape,
                    TargetLayerCount = targetLayerCount,
                    Shells = shells,
                };

                if (bestCandidate == null || candidate.TileCapacity > bestCandidate.TileCapacity)
                {
                    bestCandidate = candidate;
                }

                if (candidate.TileCapacity >= requestedTileCount)
                {
                    return candidate;
                }
            }

            if (bestCandidate != null)
            {
                return bestCandidate;
            }

            int fallbackLayerCount = GetRandomLayerCount(settings, random);
            VoxelGridSize fallbackGridSize = BuildGridSizeForShape(fallbackLayerCount, LevelShapeType.Cube);
            return new ShapeCandidate
            {
                GridSize = fallbackGridSize,
                Shape = LevelShapeType.Cube,
                TargetLayerCount = fallbackLayerCount,
                Shells = BuildShapeShells(fallbackGridSize, LevelShapeType.Cube, fallbackLayerCount, settings),
            };
        }

        /// <summary>
        /// Builds the occupied coordinates by taking shell layers in the order supplied by the shell builder.
        /// </summary>
        private static List<TilePlacementData> BuildOccupiedCoordinates(List<List<TilePlacementData>> shells, int tileCount, System.Random random)
        {
            List<TilePlacementData> orderedCoordinates = new List<TilePlacementData>(tileCount);
            for (int shellIndex = 0; shellIndex < shells.Count && orderedCoordinates.Count < tileCount; shellIndex++)
            {
                List<TilePlacementData> shellCoordinates = new List<TilePlacementData>(shells[shellIndex]);
                Shuffle(shellCoordinates, random);

                for (int coordinateIndex = 0; coordinateIndex < shellCoordinates.Count && orderedCoordinates.Count < tileCount; coordinateIndex++)
                {
                    orderedCoordinates.Add(ClonePlacement(shellCoordinates[coordinateIndex], shellIndex));
                }
            }

            if (orderedCoordinates.Count % 2 != 0)
            {
                orderedCoordinates.RemoveAt(orderedCoordinates.Count - 1);
            }

            return orderedCoordinates;
        }

        /// <summary>
        /// Creates a copy of the placement data annotated with the shell depth it belongs to.
        /// </summary>
        private static TilePlacementData ClonePlacement(TilePlacementData placement, int shellIndex)
        {
            if (placement == null)
            {
                return null;
            }

            return new TilePlacementData
            {
                Coordinate = placement.Coordinate,
                FacingDirection = placement.FacingDirection,
                ShellIndex = Mathf.Max(0, shellIndex),
                SurfaceSlotIndex = placement.SurfaceSlotIndex,
                CustomLocalPosition = placement.CustomLocalPosition,
                UseCustomLocalPosition = placement.UseCustomLocalPosition,
            };
        }

        /// <summary>
        /// Builds the shell list for the requested shape.
        /// </summary>
        private List<List<TilePlacementData>> BuildShapeShells(VoxelGridSize gridSize, LevelShapeType shape, int targetLayerCount, DifficultyBatchDefinition settings)
        {
            switch (shape)
            {
                case LevelShapeType.Cube:
                    return BuildNestedCubeShells(targetLayerCount, settings);

                case LevelShapeType.Pagoda:
                    return PagodaLevelShapeGenerator.BuildShells(
                        targetLayerCount,
                        ResolveCubeTileMetrics(),
                        Mathf.Max(2, settings.MinPairCount * 2),
                        Mathf.Max(2, settings.MaxPairCount * 2));

                case LevelShapeType.Pyramid:
                    return PyramidLevelShapeGenerator.BuildShells(
                        targetLayerCount,
                        ResolveCubeTileMetrics(),
                        Mathf.Max(2, settings.MinPairCount * 2),
                        Mathf.Max(2, settings.MaxPairCount * 2));
            }

            return BuildShells(BuildShapeCoordinates(gridSize));
        }

        /// <summary>
        /// Calculates the total tile capacity across all shell layers.
        /// </summary>
        private static int GetShellTileCapacity(List<List<TilePlacementData>> shells)
        {
            int total = 0;
            for (int index = 0; index < shells.Count; index++)
            {
                total += shells[index].Count;
            }

            return total;
        }

        /// <summary>
        /// Builds nested cube shells directly in world space so the resulting block stays cubic on all three axes.
        /// </summary>
        private List<List<TilePlacementData>> BuildNestedCubeShells(int targetLayerCount, DifficultyBatchDefinition settings)
        {
            CubeTileMetrics metrics = ResolveCubeTileMetrics();
            List<List<TilePlacementData>> shells = new List<List<TilePlacementData>>();
            int layerCount = Mathf.Max(2, targetLayerCount);
            int desiredVisibleLayerCount = layerCount;
            int maxTileCount = settings != null ? Mathf.Max(2, settings.MaxPairCount * 2) : int.MaxValue;

            CubeShellPlan previousShell = CreateInitialCubeShellPlan(metrics, maxTileCount, desiredVisibleLayerCount);
            shells.Add(BuildCubeFacePanels(previousShell, metrics));

            for (int layerIndex = 1; layerIndex < layerCount; layerIndex++)
            {
                CubeShellPlan shellPlan = CreateCoveringCubeShellPlan(previousShell, metrics);
                List<TilePlacementData> shell = BuildCubeFacePanels(shellPlan, metrics);
                if (shell.Count >= 2)
                {
                    shells.Add(shell);
                }

                previousShell = shellPlan;
            }

            shells.Reverse();
            return shells;
        }

        /// <summary>
        /// Builds one cube shell using exact world-space tile positions derived from the tile prefab dimensions.
        /// </summary>
        private List<TilePlacementData> BuildCubeFacePanels(CubeShellPlan shellPlan, CubeTileMetrics metrics)
        {
            int widthCount = shellPlan.ColumnCount;
            int heightCount = shellPlan.RowCount;
            float cubeSideLength = shellPlan.SideLength;
            float normalOffset = GetRecessedCubeFaceNormalOffset(cubeSideLength, metrics);
            float widthAxisStep = Mathf.Max(0.01f, metrics.FaceWidth);
            float heightAxisStep = Mathf.Max(0.01f, metrics.FaceHeight);

            List<TilePlacementData> shell = new List<TilePlacementData>();

            for (int verticalIndex = 0; verticalIndex < widthCount; verticalIndex++)
            {
                float localY = GetCenteredPanelCoordinate(verticalIndex, widthCount, widthAxisStep);
                for (int depthIndex = 0; depthIndex < heightCount; depthIndex++)
                {
                    float localZ = GetCenteredPanelCoordinate(depthIndex, heightCount, heightAxisStep);
                    shell.Add(CreateCustomPlacement(new Vector3(-normalOffset, localY, localZ), VoxelGridDirection.Left));
                    shell.Add(CreateCustomPlacement(new Vector3(normalOffset, localY, localZ), VoxelGridDirection.Right));
                }
            }

            for (int depthIndex = 0; depthIndex < heightCount; depthIndex++)
            {
                float localZ = GetCenteredPanelCoordinate(depthIndex, heightCount, heightAxisStep);
                for (int widthIndex = 0; widthIndex < widthCount; widthIndex++)
                {
                    float localX = GetCenteredPanelCoordinate(widthIndex, widthCount, widthAxisStep);
                    shell.Add(CreateCustomPlacement(new Vector3(localX, -normalOffset, localZ), VoxelGridDirection.Down));
                    shell.Add(CreateCustomPlacement(new Vector3(localX, normalOffset, localZ), VoxelGridDirection.Up));
                }
            }

            for (int heightIndex = 0; heightIndex < heightCount; heightIndex++)
            {
                float localY = GetCenteredPanelCoordinate(heightIndex, heightCount, heightAxisStep);
                for (int widthIndex = 0; widthIndex < widthCount; widthIndex++)
                {
                    float localX = GetCenteredPanelCoordinate(widthIndex, widthCount, widthAxisStep);
                    shell.Add(CreateCustomPlacement(new Vector3(localX, localY, -normalOffset), VoxelGridDirection.Back));
                    shell.Add(CreateCustomPlacement(new Vector3(localX, localY, normalOffset), VoxelGridDirection.Forward));
                }
            }

            return shell;
        }

        /// <summary>
        /// Resolves the face-center offset after pushing the entire tile inward so the assembled block reads as a flatter square face.
        /// </summary>
        private static float GetRecessedCubeFaceNormalOffset(float cubeSideLength, CubeTileMetrics metrics)
        {
            float centeredOffset = (cubeSideLength - metrics.Thickness) * 0.55f;
            float inwardRecess = metrics.Thickness * 1f;
            return Mathf.Max(0f, centeredOffset - inwardRecess);
        }

        /// <summary>
        /// Resolves the two in-plane face dimensions and thickness.
        /// The assigned Mahjong tile prefab takes priority so generated cube shells match the actual tile footprint.
        /// </summary>
        private CubeTileMetrics ResolveCubeTileMetrics()
        {
            if (TryGetTilePrefabBounds(out Bounds prefabBounds))
            {
                return new CubeTileMetrics(prefabBounds.size.x, prefabBounds.size.z, prefabBounds.size.y);
            }

            if (layoutOverride != null)
            {
                Vector3 layoutCellSize = layoutOverride.CellSize;
                return new CubeTileMetrics(layoutCellSize.x, layoutCellSize.z, layoutCellSize.y);
            }

            Vector3 cellSize = layoutOverride != null ? layoutOverride.CellSize : new Vector3(0.95f, 0.45f, 0.7f);
            return new CubeTileMetrics(cellSize.x, cellSize.z, cellSize.y);
        }

        /// <summary>
        /// Tries to resolve the physical tile bounds directly from the assigned prefab.
        /// </summary>
        private bool TryGetTilePrefabBounds(out Bounds prefabBounds)
        {
            prefabBounds = default;
            if (tilePrefab == null)
            {
                return false;
            }

            if (TryGetTilePrefabPlacementBounds(out prefabBounds))
            {
                prefabBounds = ApplyTileOutlineScale(prefabBounds);
                return true;
            }

            Collider[] colliders = tilePrefab.GetComponentsInChildren<Collider>(true);
            if (TryEncapsulateBounds(colliders, out prefabBounds))
            {
                prefabBounds = ApplyTileOutlineScale(prefabBounds);
                return true;
            }

            Renderer[] renderers = tilePrefab.GetComponentsInChildren<Renderer>(true);
            if (TryEncapsulateBounds(renderers, out prefabBounds))
            {
                prefabBounds = ApplyTileOutlineScale(prefabBounds);
                return true;
            }

            return false;
        }

        private Bounds ApplyTileOutlineScale(Bounds bounds)
        {
            float outlineScale = tilePrefab != null && tilePrefab.Outline != null
                ? tilePrefab.Outline.OutlineScale
                : 1f;

            if (outlineScale <= 0f || Mathf.Approximately(outlineScale, 1f))
            {
                return bounds;
            }

            return new Bounds(bounds.center, bounds.size * outlineScale);
        }

        /// <summary>
        /// Tries to resolve the tile footprint from the Mahjong tile's placement mesh/collider in parent-space units.
        /// </summary>
        private bool TryGetTilePrefabPlacementBounds(out Bounds placementBounds)
        {
            placementBounds = default;
            if (tilePrefab == null)
            {
                return false;
            }

            if (tilePrefab.MeshRenderer != null)
            {
                MeshFilter meshFilter = tilePrefab.MeshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null &&
                    TryTransformBounds(tilePrefab.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix, meshFilter.sharedMesh.bounds, out Bounds rootLocalBounds) &&
                    TryTransformBounds(tilePrefab.transform.localToWorldMatrix, rootLocalBounds, out placementBounds))
                {
                    return true;
                }
            }

            if (tilePrefab.TileCollider is MeshCollider meshCollider && meshCollider.sharedMesh != null &&
                TryTransformBounds(tilePrefab.transform.worldToLocalMatrix * meshCollider.transform.localToWorldMatrix, meshCollider.sharedMesh.bounds, out Bounds meshColliderBounds) &&
                TryTransformBounds(tilePrefab.transform.localToWorldMatrix, meshColliderBounds, out placementBounds))
            {
                return true;
            }

            if (tilePrefab.TileCollider is BoxCollider boxCollider &&
                TryTransformBounds(tilePrefab.transform.worldToLocalMatrix * boxCollider.transform.localToWorldMatrix, new Bounds(boxCollider.center, boxCollider.size), out Bounds boxColliderBounds) &&
                TryTransformBounds(tilePrefab.transform.localToWorldMatrix, boxColliderBounds, out placementBounds))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Builds the most compact starting square face plan that still leaves room for the requested visible layer count.
        /// </summary>
        private static CubeShellPlan CreateInitialCubeShellPlan(CubeTileMetrics metrics, int maxTileCount, int desiredVisibleLayerCount)
        {
            float edgeInset = GetCubeFaceEdgeInset(metrics);
            int safeVisibleLayerCount = Mathf.Max(1, desiredVisibleLayerCount);
            int maxFaceTileCount = Mathf.Max(1, maxTileCount / (6 * safeVisibleLayerCount));
            GetSquareFaceTileGrid(metrics.FaceWidth, metrics.FaceHeight, maxFaceTileCount, out int baseColumnCount, out int baseRowCount);

            float panelWidth = baseColumnCount * metrics.FaceWidth;
            float panelHeight = baseRowCount * metrics.FaceHeight;
            float sideLength = Mathf.Max(panelWidth, panelHeight) + (edgeInset * 2f);
            return new CubeShellPlan(baseColumnCount, baseRowCount, sideLength);
        }

        /// <summary>
        /// Builds the next larger cube shell by choosing the smallest larger panel that still keeps the face as square as possible.
        /// This avoids growing only one axis on the next shell, which can leave an outer face visually under-filled.
        /// </summary>
        private static CubeShellPlan CreateCoveringCubeShellPlan(CubeShellPlan previousShell, CubeTileMetrics metrics)
        {
            float requiredSideLength = previousShell.SideLength + (metrics.Thickness * 2f);
            int minimumColumnCount = Mathf.Max(1, previousShell.ColumnCount);
            int minimumRowCount = Mathf.Max(1, previousShell.RowCount);
            float edgeInset = GetCubeFaceEdgeInset(metrics);
            const int SearchPaddingPerAxis = 12;

            float bestMismatch = float.MaxValue;
            float bestSideLength = float.MaxValue;
            int bestColumnCount = minimumColumnCount;
            int bestRowCount = minimumRowCount;
            int bestTileCount = int.MaxValue;
            bool foundCandidate = false;

            for (int columnCount = minimumColumnCount; columnCount <= minimumColumnCount + SearchPaddingPerAxis; columnCount++)
            {
                for (int rowCount = minimumRowCount; rowCount <= minimumRowCount + SearchPaddingPerAxis; rowCount++)
                {
                    float panelWidth = columnCount * metrics.FaceWidth;
                    float panelHeight = rowCount * metrics.FaceHeight;
                    float sideLength = Mathf.Max(panelWidth, panelHeight) + (edgeInset * 2f);
                    if (sideLength + 0.0001f < requiredSideLength)
                    {
                        continue;
                    }

                    float longestSide = Mathf.Max(panelWidth, panelHeight);
                    float mismatch = longestSide <= 0.01f ? 0f : Mathf.Abs(panelWidth - panelHeight) / longestSide;
                    int tileCount = columnCount * rowCount;

                    bool isBetterMismatch = mismatch + 0.0001f < bestMismatch;
                    bool isSameMismatchWithSmallerShell = Mathf.Abs(mismatch - bestMismatch) <= 0.0001f && sideLength + 0.0001f < bestSideLength;
                    bool isSameMismatchAndShellWithFewerTiles = Mathf.Abs(mismatch - bestMismatch) <= 0.0001f && Mathf.Abs(sideLength - bestSideLength) <= 0.0001f && tileCount < bestTileCount;
                    if (!isBetterMismatch && !isSameMismatchWithSmallerShell && !isSameMismatchAndShellWithFewerTiles)
                    {
                        continue;
                    }

                    bestMismatch = mismatch;
                    bestSideLength = sideLength;
                    bestColumnCount = columnCount;
                    bestRowCount = rowCount;
                    bestTileCount = tileCount;
                    foundCandidate = true;
                }
            }

            if (foundCandidate)
            {
                return new CubeShellPlan(bestColumnCount, bestRowCount, bestSideLength);
            }

            int columnCountFallback = minimumColumnCount;
            int rowCountFallback = minimumRowCount;

            while (true)
            {
                float panelWidth = columnCountFallback * metrics.FaceWidth;
                float panelHeight = rowCountFallback * metrics.FaceHeight;
                float sideLength = Mathf.Max(panelWidth, panelHeight) + (edgeInset * 2f);
                if (sideLength + 0.0001f >= requiredSideLength)
                {
                    return new CubeShellPlan(columnCountFallback, rowCountFallback, sideLength);
                }

                if (panelWidth <= panelHeight)
                {
                    columnCountFallback++;
                }
                else
                {
                    rowCountFallback++;
                }
            }
        }

        /// <summary>
        /// Resolves the empty border each face needs so perpendicular faces do not overlap at the edges.
        /// </summary>
        private static float GetCubeFaceEdgeInset(CubeTileMetrics metrics)
        {
            return Mathf.Max(0.01f, metrics.Thickness + 0.001f);
        }

        /// <summary>
        /// Resolves the smallest tile grid whose total X/Y span is as square as possible.
        /// </summary>
        private static void GetSquareFaceTileGrid(float tileFaceWidth, float tileFaceHeight, int maxFaceTileCount, out int columnCount, out int rowCount)
        {
            float safeWidth = Mathf.Max(0.01f, tileFaceWidth);
            float safeHeight = Mathf.Max(0.01f, tileFaceHeight);
            const int MaxTilesPerAxis = 12;
            int safeMaxFaceTileCount = Mathf.Max(1, maxFaceTileCount);

            float bestMismatch = float.MaxValue;
            int bestColumnCount = 1;
            int bestRowCount = 1;
            int bestTileCount = int.MaxValue;

            for (int candidateColumnCount = 1; candidateColumnCount <= MaxTilesPerAxis; candidateColumnCount++)
            {
                for (int candidateRowCount = 1; candidateRowCount <= MaxTilesPerAxis; candidateRowCount++)
                {
                    int tileCount = candidateColumnCount * candidateRowCount;
                    if (tileCount > safeMaxFaceTileCount)
                    {
                        continue;
                    }

                    float panelWidth = candidateColumnCount * safeWidth;
                    float panelHeight = candidateRowCount * safeHeight;
                    float longestSide = Mathf.Max(panelWidth, panelHeight);
                    float mismatch = longestSide <= 0.01f ? 0f : Mathf.Abs(panelWidth - panelHeight) / longestSide;

                    bool isBetterMismatch = mismatch + 0.0001f < bestMismatch;
                    bool isSameMismatchWithFewerTiles = Mathf.Abs(mismatch - bestMismatch) <= 0.0001f && tileCount < bestTileCount;
                    if (!isBetterMismatch && !isSameMismatchWithFewerTiles)
                    {
                        continue;
                    }

                    bestMismatch = mismatch;
                    bestColumnCount = candidateColumnCount;
                    bestRowCount = candidateRowCount;
                    bestTileCount = tileCount;
                }
            }

            columnCount = bestColumnCount;
            rowCount = bestRowCount;
        }

        /// <summary>
        /// Tries to combine collider or renderer bounds into a single prefab bound.
        /// </summary>
        private static bool TryEncapsulateBounds<TComponent>(TComponent[] components, out Bounds combinedBounds)
            where TComponent : Component
        {
            combinedBounds = default;
            bool hasBounds = false;

            if (components == null)
            {
                return false;
            }

            for (int index = 0; index < components.Length; index++)
            {
                TComponent component = components[index];
                if (component == null)
                {
                    continue;
                }

                Bounds componentBounds;
                switch (component)
                {
                    case Collider collider:
                        componentBounds = collider.bounds;
                        break;

                    case Renderer renderer:
                        componentBounds = renderer.bounds;
                        break;

                    default:
                        continue;
                }

                if (componentBounds.size.sqrMagnitude <= Mathf.Epsilon)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = componentBounds;
                    hasBounds = true;
                    continue;
                }

                combinedBounds.Encapsulate(componentBounds.min);
                combinedBounds.Encapsulate(componentBounds.max);
            }

            return hasBounds;
        }

        /// <summary>
        /// Transforms a bounds volume through the supplied matrix and returns the resulting axis-aligned bounds.
        /// </summary>
        private static bool TryTransformBounds(Matrix4x4 matrix, Bounds sourceBounds, out Bounds transformedBounds)
        {
            transformedBounds = default;
            if (sourceBounds.size.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            Vector3 sourceCenter = sourceBounds.center;
            Vector3 sourceExtents = sourceBounds.extents;
            Vector3[] corners = new Vector3[8];
            int cornerIndex = 0;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = sourceCenter + Vector3.Scale(sourceExtents, new Vector3(x, y, z));
                        corners[cornerIndex++] = matrix.MultiplyPoint3x4(corner);
                    }
                }
            }

            transformedBounds = new Bounds(corners[0], Vector3.zero);
            for (int index = 1; index < corners.Length; index++)
            {
                transformedBounds.Encapsulate(corners[index]);
            }

            return transformedBounds.size.sqrMagnitude > Mathf.Epsilon;
        }

        /// <summary>
        /// Creates one outward-facing placement at a custom local position.
        /// </summary>
        private static TilePlacementData CreateCustomPlacement(Vector3 localPosition, VoxelGridDirection facingDirection)
        {
            return new TilePlacementData
            {
                Coordinate = Vector3Int.zero,
                FacingDirection = facingDirection,
                SurfaceSlotIndex = -1,
                CustomLocalPosition = localPosition,
                UseCustomLocalPosition = true,
            };
        }

        /// <summary>
        /// Builds a visually reasonable voxel grid for the supplied shell-layer count and shape.
        /// </summary>
        private static VoxelGridSize BuildGridSizeForShape(int layerCount, LevelShapeType shape)
        {
            int safeLayerCount = Mathf.Max(1, layerCount);
            int width = safeLayerCount + 1;
            int height = safeLayerCount + 1;
            int depth = safeLayerCount + 1;

            switch (shape)
            {
                case LevelShapeType.Cube:
                {
                    int cubeSide = Mathf.Clamp((safeLayerCount / 2) + 1, 3, 4);
                    width = cubeSide;
                    height = cubeSide;
                    depth = cubeSide;
                    break;
                }

                case LevelShapeType.Pagoda:
                    return PagodaLevelShapeGenerator.BuildGridSize(layerCount);

                case LevelShapeType.Pyramid:
                    return PyramidLevelShapeGenerator.BuildGridSize(layerCount);
            }

            return new VoxelGridSize(width, height, depth);
        }

        /// <summary>
        /// Builds a solid occupied voxel block for non-custom-world-space shapes.
        /// </summary>
        private static List<Vector3Int> BuildShapeCoordinates(VoxelGridSize gridSize)
        {
            List<Vector3Int> coordinates = new List<Vector3Int>(gridSize.Volume);
            for (int x = 0; x < gridSize.Width; x++)
            {
                for (int y = 0; y < gridSize.Height; y++)
                {
                    for (int z = 0; z < gridSize.Depth; z++)
                    {
                        coordinates.Add(new Vector3Int(x, y, z));
                    }
                }
            }

            return coordinates;
        }

        /// <summary>
        /// Builds tile definitions and assigns match ids in pairs.
        /// </summary>
        private List<LevelTileDefinition> BuildTileDefinitions(List<TilePlacementData> occupiedCoordinates, VoxelGridSize shapeGridSize, VoxelGridSize logicalGridSize, DifficultyBatchDefinition settings, System.Random random)
        {
            List<LevelTileDefinition> tileDefinitions = new List<LevelTileDefinition>(occupiedCoordinates.Count);
            int pairCount = occupiedCoordinates.Count / 2;

            for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
            {
                int firstIndex = pairIndex * 2;
                int secondIndex = firstIndex + 1;
                tileDefinitions.Add(CreateTileDefinition(pairIndex, firstIndex, occupiedCoordinates[firstIndex], shapeGridSize, logicalGridSize, settings.FlippedTileChance, random));
                tileDefinitions.Add(CreateTileDefinition(pairIndex, secondIndex, occupiedCoordinates[secondIndex], shapeGridSize, logicalGridSize, settings.FlippedTileChance, random));
            }

            return tileDefinitions;
        }

        /// <summary>
        /// Creates a single tile definition with an optional 180-degree Y flip.
        /// </summary>
        private LevelTileDefinition CreateTileDefinition(int matchId, int tileIndex, TilePlacementData placement, VoxelGridSize shapeGridSize, VoxelGridSize logicalGridSize, float flippedTileChance, System.Random random)
        {
            bool flipTile = ShouldFlipGeneratedTile(placement, flippedTileChance, random);
            return new LevelTileDefinition
            {
                MatchId = matchId,
                GridCoordinate = GetLogicalGridCoordinate(tileIndex, logicalGridSize),
                SurfaceShellIndex = placement.ShellIndex,
                UseCustomLocalPosition = true,
                LocalPosition = GetCompactedSurfaceTileLocalPosition(placement, shapeGridSize),
                LocalEulerAngles = GetFacingRotationEuler(placement.FacingDirection, flipTile),
            };
        }

        private static bool ShouldFlipGeneratedTile(TilePlacementData placement, float flippedTileChance, System.Random random)
        {
            if (placement == null)
            {
                return false;
            }

            if (placement.UseCustomLocalPosition || placement.SurfaceSlotIndex >= -1)
            {
                return false;
            }

            return random != null && random.NextDouble() <= flippedTileChance;
        }

        /// <summary>
        /// Resolves the wrapped tile local position and pulls inner shells outward until they touch the previous shell.
        /// </summary>
        private Vector3 GetCompactedSurfaceTileLocalPosition(TilePlacementData placement, VoxelGridSize shapeGridSize)
        {
            if (placement == null)
            {
                return Vector3.zero;
            }

            if (placement.UseCustomLocalPosition)
            {
                return placement.CustomLocalPosition;
            }

            Vector3 localPosition = GetSurfaceTileLocalPosition(placement, shapeGridSize);

            int shellIndex = Mathf.Max(0, placement.ShellIndex);
            if (shellIndex == 0)
            {
                return localPosition;
            }

            Vector3 faceNormal = ((Vector3)VoxelGridDirections.GetOffset(placement.FacingDirection)).normalized;
            float faceStep = GetSurfaceFaceStep(placement.FacingDirection);
            float shellThickness = GetSurfaceShellThickness();
            float shellGap = Mathf.Max(0f, faceStep - shellThickness);
            if (shellGap <= Mathf.Epsilon)
            {
                return localPosition;
            }

            return localPosition + (faceNormal * (shellGap * shellIndex));
        }

        /// <summary>
        /// Extracts the shell layers from a filled voxel shape.
        /// </summary>
        private static List<List<TilePlacementData>> BuildShells(List<Vector3Int> occupiedCoordinates)
        {
            List<List<TilePlacementData>> shells = new List<List<TilePlacementData>>();
            HashSet<Vector3Int> remaining = new HashSet<Vector3Int>(occupiedCoordinates);

            while (remaining.Count > 0)
            {
                List<TilePlacementData> shell = ExtractSurfaceShell(remaining);
                if (shell.Count == 0)
                {
                    break;
                }

                shells.Add(shell);
                for (int index = 0; index < shell.Count; index++)
                {
                    remaining.Remove(shell[index].Coordinate);
                }
            }

            return shells;
        }

        /// <summary>
        /// Finds the current surface voxels of the supplied volume.
        /// </summary>
        private static List<TilePlacementData> ExtractSurfaceShell(HashSet<Vector3Int> occupiedCoordinates)
        {
            List<TilePlacementData> shell = new List<TilePlacementData>();
            foreach (Vector3Int coordinate in occupiedCoordinates)
            {
                for (int directionIndex = 0; directionIndex < NeighborDirections.Length; directionIndex++)
                {
                    Vector3Int neighbor = coordinate + NeighborDirections[directionIndex];
                    if (!occupiedCoordinates.Contains(neighbor))
                    {
                        VoxelGridDirection facingDirection = ToGridDirection(NeighborDirections[directionIndex]);
                        shell.Add(new TilePlacementData
                        {
                            Coordinate = coordinate,
                            FacingDirection = facingDirection,
                            SurfaceSlotIndex = -1,
                        });
                    }
                }
            }

            return shell;
        }

        /// <summary>
        /// Resolves the face direction a shell tile should point toward.
        /// </summary>
        private static VoxelGridDirection ResolveFacingDirection(Vector3Int coordinate, List<VoxelGridDirection> exposedDirections, Vector3 center)
        {
            if (exposedDirections == null || exposedDirections.Count == 0)
            {
                return VoxelGridDirection.Up;
            }

            if (exposedDirections.Count == 1)
            {
                return exposedDirections[0];
            }

            Vector3 outwardVector = ((Vector3)coordinate - center).normalized;
            float bestScore = float.NegativeInfinity;
            VoxelGridDirection bestDirection = exposedDirections[0];

            for (int index = 0; index < exposedDirections.Count; index++)
            {
                VoxelGridDirection direction = exposedDirections[index];
                float score = Vector3.Dot(outwardVector, ((Vector3)VoxelGridDirections.GetOffset(direction)).normalized);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = direction;
                }
            }

            return bestDirection;
        }

        /// <summary>
        /// Calculates the center of a coordinate set for outward-direction scoring.
        /// </summary>
        private static Vector3 CalculateCoordinateSetCenter(HashSet<Vector3Int> coordinates)
        {
            if (coordinates == null || coordinates.Count == 0)
            {
                return Vector3.zero;
            }

            Vector3 sum = Vector3.zero;
            foreach (Vector3Int coordinate in coordinates)
            {
                sum += coordinate;
            }

            return sum / coordinates.Count;
        }

        /// <summary>
        /// Converts a cardinal offset into the corresponding voxel-grid direction.
        /// </summary>
        private static VoxelGridDirection ToGridDirection(Vector3Int offset)
        {
            if (offset == Vector3Int.left)
            {
                return VoxelGridDirection.Left;
            }

            if (offset == Vector3Int.right)
            {
                return VoxelGridDirection.Right;
            }

            if (offset == Vector3Int.down)
            {
                return VoxelGridDirection.Down;
            }

            if (offset == Vector3Int.up)
            {
                return VoxelGridDirection.Up;
            }

            if (offset == new Vector3Int(0, 0, -1))
            {
                return VoxelGridDirection.Back;
            }

            return VoxelGridDirection.Forward;
        }

        /// <summary>
        /// Builds an outward-facing tile rotation, with optional spin around the face normal.
        /// </summary>
        private static Vector3 GetFacingRotationEuler(VoxelGridDirection facingDirection, bool flipTile)
        {
            Vector3 outwardNormal = VoxelGridDirections.GetOffset(facingDirection);
            Quaternion outwardRotation = Quaternion.FromToRotation(Vector3.up, outwardNormal);
            if (flipTile)
            {
                outwardRotation = Quaternion.AngleAxis(180f, outwardNormal) * outwardRotation;
            }

            return outwardRotation.eulerAngles;
        }

        /// <summary>
        /// Converts a sequential tile index into a compact logical runtime coordinate.
        /// </summary>
        private static Vector3Int GetLogicalGridCoordinate(int tileIndex, VoxelGridSize logicalGridSize)
        {
            int width = Mathf.Max(1, logicalGridSize.Width);
            int height = Mathf.Max(1, logicalGridSize.Height);
            int area = width * height;
            int x = tileIndex % width;
            int y = (tileIndex / width) % height;
            int z = tileIndex / area;
            return new Vector3Int(x, y, z);
        }

        /// <summary>
        /// Builds a compact cube-like logical grid large enough for every generated tile.
        /// </summary>
        private static VoxelGridSize BuildLogicalGridSize(int tileCount)
        {
            int safeTileCount = Mathf.Max(1, tileCount);
            int side = Mathf.Max(1, Mathf.CeilToInt(Mathf.Pow(safeTileCount, 1f / 3f)));

            while (side * side * side < safeTileCount)
            {
                side++;
            }

            return new VoxelGridSize(side, side, side);
        }

        /// <summary>
        /// Calculates the custom local-space position of a tile slot wrapped on an exposed face.
        /// </summary>
        private Vector3 GetSurfaceTileLocalPosition(TilePlacementData placement, VoxelGridSize shapeGridSize)
        {
            Vector3 cellSize = layoutOverride != null ? layoutOverride.CellSize : Vector3.one;
            Vector3 cellSpacing = layoutOverride != null ? layoutOverride.CellSpacing : Vector3.zero;
            Vector3 originOffset = layoutOverride != null ? layoutOverride.OriginOffset : Vector3.zero;
            VoxelGridPivotMode pivotMode = layoutOverride != null ? layoutOverride.PivotMode : VoxelGridPivotMode.Center;
            Vector3 step = cellSize + cellSpacing;
            Vector3 voxelCenter = GetStaticLocalPosition(placement.Coordinate, shapeGridSize, step, originOffset, pivotMode);

            Vector3 faceNormal = ((Vector3)VoxelGridDirections.GetOffset(placement.FacingDirection)).normalized;
            Vector3 halfFaceOffset = Vector3.Scale(faceNormal, cellSize) * 0.5f;
            Vector3 paddingOffset = faceNormal * 0.02f;

            if (placement.SurfaceSlotIndex < 0)
            {
                return voxelCenter + halfFaceOffset + paddingOffset;
            }

            Vector3 tangent = GetSurfaceTangent(placement.FacingDirection);
            Vector3 slotOffset = tangent * GetSurfaceSlotDistance(placement.FacingDirection, step) * (placement.SurfaceSlotIndex == 0 ? -1f : 1f);
            return voxelCenter + halfFaceOffset + paddingOffset + slotOffset;
        }

        /// <summary>
        /// Converts a panel index into a centered local coordinate that supports both odd and even shell sizes.
        /// </summary>
        private static float GetCenteredPanelCoordinate(int index, int count)
        {
            return index - ((count - 1) * 0.5f);
        }

        /// <summary>
        /// Converts a panel index into an exact centered world-space coordinate for the supplied step distance.
        /// </summary>
        private static float GetCenteredPanelCoordinate(int index, int count, float step)
        {
            return ((index + 0.5f) - (count * 0.5f)) * step;
        }

        /// <summary>
        /// Resolves the normal-axis spacing currently used by surface-wrapped placements on the supplied face.
        /// </summary>
        private float GetSurfaceFaceStep(VoxelGridDirection facingDirection)
        {
            Vector3 step = layoutOverride != null ? layoutOverride.CellStep : new Vector3(0.95f, 0.45f, 0.7f);
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
        /// Resolves the physical tile thickness used when shell layers are compacted against each other.
        /// Prefers the assigned tile prefab bounds so wrapped shell layers stay visually tight across every shape.
        /// </summary>
        private float GetSurfaceShellThickness()
        {
            if (TryGetTilePrefabPlacementBounds(out Bounds placementBounds))
            {
                return Mathf.Max(0.01f, placementBounds.size.y);
            }

            if (TryGetTilePrefabBounds(out Bounds prefabBounds))
            {
                return Mathf.Max(0.01f, prefabBounds.size.y);
            }

            Vector3 cellSize = layoutOverride != null ? layoutOverride.CellSize : new Vector3(0.95f, 0.45f, 0.7f);
            return Mathf.Max(0.01f, cellSize.y);
        }

        /// <summary>
        /// Recreates centered local-space grid math without allocating a runtime voxel grid.
        /// </summary>
        private static Vector3 GetStaticLocalPosition(Vector3Int coordinate, VoxelGridSize gridSize, Vector3 step, Vector3 originOffset, VoxelGridPivotMode pivotMode)
        {
            Vector3 position = Vector3.Scale((Vector3)coordinate, step);
            if (pivotMode == VoxelGridPivotMode.Center)
            {
                Vector3 centerOffset = new Vector3(
                    (gridSize.Width - 1) * step.x,
                    (gridSize.Height - 1) * step.y,
                    (gridSize.Depth - 1) * step.z) * 0.5f;

                position -= centerOffset;
            }

            return position + originOffset;
        }

        /// <summary>
        /// Resolves the tangent axis used to split one face into two side-by-side slots.
        /// </summary>
        private static Vector3 GetSurfaceTangent(VoxelGridDirection facingDirection)
        {
            switch (facingDirection)
            {
                case VoxelGridDirection.Left:
                case VoxelGridDirection.Right:
                    return Vector3.forward;

                case VoxelGridDirection.Down:
                case VoxelGridDirection.Up:
                case VoxelGridDirection.Back:
                case VoxelGridDirection.Forward:
                default:
                    return Vector3.right;
            }
        }

        /// <summary>
        /// Resolves the half-lane spacing used by the two surface slots on one face.
        /// </summary>
        private static float GetSurfaceSlotDistance(VoxelGridDirection facingDirection, Vector3 step)
        {
            switch (facingDirection)
            {
                case VoxelGridDirection.Left:
                case VoxelGridDirection.Right:
                    return Mathf.Max(0.01f, step.z * 0.25f);

                case VoxelGridDirection.Down:
                case VoxelGridDirection.Up:
                case VoxelGridDirection.Back:
                case VoxelGridDirection.Forward:
                default:
                    return Mathf.Max(0.01f, step.x * 0.25f);
            }
        }

        /// <summary>
        /// Shuffles the supplied list using Fisher-Yates.
        /// </summary>
        private static void Shuffle<TValue>(IList<TValue> values, System.Random random)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int swapIndex = random.Next(0, index + 1);
                TValue temp = values[index];
                values[index] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Writes a generated payload into a concrete level asset instance.
        /// </summary>
        private static void ApplyGeneratedData(LevelDefinition asset, GeneratedLevelData data)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("<LevelName>k__BackingField").stringValue = data.LevelName;

            SerializedProperty gridSizeProperty = serializedObject.FindProperty("<GridSize>k__BackingField");
            gridSizeProperty.FindPropertyRelative("width").intValue = data.GridSize.Width;
            gridSizeProperty.FindPropertyRelative("height").intValue = data.GridSize.Height;
            gridSizeProperty.FindPropertyRelative("depth").intValue = data.GridSize.Depth;

            serializedObject.FindProperty("<LayoutOverride>k__BackingField").objectReferenceValue = data.LayoutOverride;
            serializedObject.FindProperty("<Shape>k__BackingField").intValue = (int)data.Shape;
            serializedObject.FindProperty("<UseSurfaceTilePlacement>k__BackingField").boolValue = true;
            serializedObject.FindProperty("<Difficulty>k__BackingField").enumValueIndex = (int)data.Difficulty;

            SerializedProperty tilesProperty = serializedObject.FindProperty("<Tiles>k__BackingField");
            tilesProperty.ClearArray();
            for (int index = 0; index < data.Tiles.Count; index++)
            {
                LevelTileDefinition tile = data.Tiles[index];
                tilesProperty.InsertArrayElementAtIndex(index);
                SerializedProperty tileProperty = tilesProperty.GetArrayElementAtIndex(index);
                tileProperty.FindPropertyRelative("matchId").intValue = tile.MatchId;

                SerializedProperty coordinateProperty = tileProperty.FindPropertyRelative("gridCoordinate");
                coordinateProperty.FindPropertyRelative("x").intValue = tile.GridCoordinate.x;
                coordinateProperty.FindPropertyRelative("y").intValue = tile.GridCoordinate.y;
                coordinateProperty.FindPropertyRelative("z").intValue = tile.GridCoordinate.z;

                tileProperty.FindPropertyRelative("useCustomLocalPosition").boolValue = tile.UseCustomLocalPosition;

                SerializedProperty localPositionProperty = tileProperty.FindPropertyRelative("localPosition");
                localPositionProperty.vector3Value = tile.LocalPosition;

                SerializedProperty eulerProperty = tileProperty.FindPropertyRelative("localEulerAngles");
                eulerProperty.vector3Value = tile.LocalEulerAngles;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        /// <summary>
        /// Applies generated assets into the configured level catalog.
        /// </summary>
        private void ApplyCatalogEntries(List<LevelDefinition> generatedAssets)
        {
            SerializedObject catalogObject = new SerializedObject(targetCatalog);
            SerializedProperty levelsProperty = catalogObject.FindProperty("<Levels>k__BackingField");

            for (int index = 0; index < generatedAssets.Count; index++)
            {
                LevelDefinition generatedAsset = generatedAssets[index];
                if (generatedAsset == null)
                {
                    continue;
                }

                int existingIndex = FindCatalogIndex(levelsProperty, generatedAsset);
                if (existingIndex >= 0)
                {
                    levelsProperty.GetArrayElementAtIndex(existingIndex).objectReferenceValue = generatedAsset;
                    continue;
                }

                int insertIndex = levelsProperty.arraySize;
                levelsProperty.InsertArrayElementAtIndex(insertIndex);
                levelsProperty.GetArrayElementAtIndex(insertIndex).objectReferenceValue = generatedAsset;
            }

            catalogObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetCatalog);
        }

        private int GetHighestExistingSequence()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:LevelDefinition", new[] { outputFolder });
            int highestSequence = 0;

            for (int index = 0; index < assetGuids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[index]);
                string assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                int separatorIndex = assetName.LastIndexOf('_');
                if (separatorIndex < 0 || separatorIndex >= assetName.Length - 1)
                {
                    continue;
                }

                string suffix = assetName.Substring(separatorIndex + 1);
                if (!int.TryParse(suffix, out int sequence))
                {
                    continue;
                }

                if (sequence > highestSequence)
                {
                    highestSequence = sequence;
                }
            }

            return highestSequence;
        }

        private static int FindCatalogIndex(SerializedProperty levelsProperty, LevelDefinition targetLevel)
        {
            for (int index = 0; index < levelsProperty.arraySize; index++)
            {
                if (levelsProperty.GetArrayElementAtIndex(index).objectReferenceValue == targetLevel)
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Ensures every path segment in the configured output folder exists as a Unity asset folder.
        /// </summary>
        private static void EnsureFolderExists(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string currentPath = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string nextPath = $"{currentPath}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[index]);
                }

                currentPath = nextPath;
            }
        }

        /// <summary>
        /// Converts a level name into a safe asset file name.
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            string sanitized = fileName;
            char[] invalidCharacters = System.IO.Path.GetInvalidFileNameChars();
            for (int index = 0; index < invalidCharacters.Length; index++)
            {
                sanitized = sanitized.Replace(invalidCharacters[index], '_');
            }

            return sanitized.Replace(' ', '_');
        }
#endif
    }
}
