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
        [SerializeField] private bool replaceCatalogEntries = true;
        [SerializeField] private bool overwriteExistingAssets = true;
        [SerializeField] private DifficultyBatchDefinition normalSettings = DifficultyBatchDefinition.CreateNormalDefaults();
        [SerializeField] private DifficultyBatchDefinition hardSettings = DifficultyBatchDefinition.CreateHardDefaults();
        [SerializeField] private DifficultyBatchDefinition superHardSettings = DifficultyBatchDefinition.CreateSuperHardDefaults();

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
                if (allowedShapes != null && allowedShapes.Count > 0)
                {
                    return allowedShapes[random.Next(0, allowedShapes.Count)];
                }

                return shape;
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
                    shape = LevelShapeType.Heart,
                    allowedShapes = new List<LevelShapeType>
                    {
                        LevelShapeType.Heart,
                    },
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
                    maxLayerCount = 6,
                    minPairCount = 34,
                    maxPairCount = 40,
                    flippedTileChance = 0.55f,
                    shape = LevelShapeType.Castle,
                    allowedShapes = new List<LevelShapeType>
                    {
                        LevelShapeType.Castle,
                    },
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
                    minLayerCount = 6,
                    maxLayerCount = 7,
                    minPairCount = 48,
                    maxPairCount = 58,
                    flippedTileChance = 0.65f,
                    shape = LevelShapeType.Castle,
                    allowedShapes = new List<LevelShapeType>
                    {
                        LevelShapeType.Heart,
                        LevelShapeType.Castle,
                    },
                    difficulty = LevelDifficulty.Expert,
                };
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
        private sealed class TilePlacementData
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
        private readonly struct CubeTileMetrics
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
        public List<GeneratedLevelData> GenerateLevelData()
        {
            List<GeneratedLevelData> results = new List<GeneratedLevelData>();
            System.Random random = new System.Random(seed);
            int sequence = 0;

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
            if (targetCatalog == null)
            {
                throw new InvalidOperationException("ProceduralLevelBatchGenerator requires a target LevelCatalog.");
            }

            if (string.IsNullOrWhiteSpace(outputFolder) || !outputFolder.StartsWith("Assets", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Output folder must be a valid Unity project path starting with 'Assets'.");
            }

            EnsureFolderExists(outputFolder);
            List<GeneratedLevelData> generatedData = GenerateLevelData();
            List<LevelDefinition> generatedAssets = new List<LevelDefinition>(generatedData.Count);

            for (int index = 0; index < generatedData.Count; index++)
            {
                GeneratedLevelData data = generatedData[index];
                string safeLevelName = SanitizeFileName(data.LevelName);
                string assetPath = $"{outputFolder}/{safeLevelName}.asset";

                if (overwriteExistingAssets)
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
                else
                {
                    assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                }

                LevelDefinition asset = CreateInstance<LevelDefinition>();
                ApplyGeneratedData(asset, data);
                AssetDatabase.CreateAsset(asset, assetPath);
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
                int tileCount = GetTargetTileCount(settings, candidate.Shells, candidate.Shape, random);
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
        private static int GetTargetTileCount(DifficultyBatchDefinition settings, List<List<TilePlacementData>> shells, LevelShapeType shape, System.Random random)
        {
            if (shells == null || shells.Count == 0)
            {
                return 0;
            }

            int maxTileCount = Mathf.Max(2, settings.MaxPairCount * 2);
            int minTileCount = Mathf.Max(2, settings.MinPairCount * 2);

            if (shape == LevelShapeType.Cube)
            {
                return GetPreferredCubeTileCount(shells, settings.MinLayerCount, minTileCount, maxTileCount);
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

            if (shape == LevelShapeType.Heart || shape == LevelShapeType.Castle)
            {
                int targetFullLayerCount = Mathf.Min(shells.Count, shape == LevelShapeType.Castle ? 4 : 5);
                if (targetFullLayerCount >= 3)
                {
                    int layeredTileCount = 0;
                    for (int layerIndex = 0; layerIndex < targetFullLayerCount; layerIndex++)
                    {
                        layeredTileCount += shells[layerIndex].Count;
                    }

                    if (layeredTileCount >= 2)
                    {
                        return layeredTileCount % 2 == 0 ? layeredTileCount : layeredTileCount - 1;
                    }
                }

                if (preferredCounts.Count > 0)
                {
                    return preferredCounts[preferredCounts.Count - 1];
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

            return completeLayerCounts[completeLayerCounts.Count - 1];
        }

        /// <summary>
        /// Chooses a visually complete cube by keeping whole shells from the smallest cube outward.
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

                case LevelShapeType.Heart:
                case LevelShapeType.Castle:
                {
                    List<List<TilePlacementData>> nestedShells = BuildNestedSilhouetteShells(gridSize, shape, targetLayerCount);
                    if (GetShellTileCapacity(nestedShells) >= 2)
                    {
                        return nestedShells;
                    }

                    break;
                }
            }

            return BuildShells(BuildShapeCoordinates(gridSize, shape));
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
        /// Builds shells as nested complete silhouettes where each inner silhouette is smaller than the outer one.
        /// </summary>
        private static List<List<TilePlacementData>> BuildNestedSilhouetteShells(VoxelGridSize gridSize, LevelShapeType shape, int targetLayerCount)
        {
            HashSet<Vector3Int> outerVolume = new HashSet<Vector3Int>(BuildShapeCoordinates(gridSize, shape));
            if (outerVolume.Count < 2)
            {
                return BuildShells(BuildShapeCoordinates(gridSize, shape));
            }

            int desiredLayerCount = Mathf.Max(1, targetLayerCount);
            int scaleSampleCount = GetNestedLayerScaleSampleCount(gridSize, shape);
            float minimumScale = GetMinimumNestedScale(shape);

            List<HashSet<Vector3Int>> nestedVolumes = new List<HashSet<Vector3Int>>(desiredLayerCount)
            {
                outerVolume,
            };

            HashSet<Vector3Int> previousVolume = outerVolume;
            for (int sampleIndex = 1; sampleIndex <= scaleSampleCount && nestedVolumes.Count < desiredLayerCount; sampleIndex++)
            {
                float t = sampleIndex / (float)scaleSampleCount;
                float layerScale = Mathf.Lerp(0.96f, minimumScale, t);
                HashSet<Vector3Int> scaledVolume = BuildScaledShapeCoordinateSet(gridSize, shape, layerScale);
                scaledVolume.IntersectWith(previousVolume);

                if (scaledVolume.Count < 2 || scaledVolume.SetEquals(previousVolume) || scaledVolume.Count >= previousVolume.Count)
                {
                    continue;
                }

                nestedVolumes.Add(scaledVolume);
                previousVolume = scaledVolume;
            }

            if (nestedVolumes.Count <= 1)
            {
                return BuildShells(BuildShapeCoordinates(gridSize, shape));
            }

            List<List<TilePlacementData>> shells = new List<List<TilePlacementData>>(nestedVolumes.Count);
            for (int index = 0; index < nestedVolumes.Count; index++)
            {
                List<TilePlacementData> shell = ExtractSurfaceShell(nestedVolumes[index]);
                if (shell.Count >= 2)
                {
                    shells.Add(shell);
                }
            }

            return shells.Count > 0 ? shells : BuildShells(BuildShapeCoordinates(gridSize, shape));
        }

        /// <summary>
        /// Builds nested cube shells directly in world space so the resulting block stays cubic on all three axes.
        /// </summary>
        private List<List<TilePlacementData>> BuildNestedCubeShells(int targetLayerCount, DifficultyBatchDefinition settings)
        {
            CubeTileMetrics metrics = ResolveCubeTileMetrics();
            List<List<TilePlacementData>> shells = new List<List<TilePlacementData>>();
            int layerCount = Mathf.Max(2, targetLayerCount);
            int desiredVisibleLayerCount = settings != null ? settings.MinLayerCount : 1;
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
            float edgeInset = GetCubeFaceEdgeInset(metrics);
            float targetPanelSideLength = Mathf.Max(0.01f, cubeSideLength - (edgeInset * 2f));
            float widthAxisStep = GetSquarePanelStep(widthCount, metrics.FaceWidth, targetPanelSideLength);
            float heightAxisStep = GetSquarePanelStep(heightCount, metrics.FaceHeight, targetPanelSideLength);

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
            float inwardRecess = metrics.Thickness * 0.5f;
            return Mathf.Max(0f, centeredOffset - inwardRecess);
        }

        /// <summary>
        /// Resolves the two in-plane face dimensions and thickness.
        /// Layout settings take priority so swapping tile art does not reshape generated levels.
        /// </summary>
        private CubeTileMetrics ResolveCubeTileMetrics()
        {
            if (layoutOverride != null)
            {
                Vector3 layoutCellSize = layoutOverride.CellSize;
                return new CubeTileMetrics(layoutCellSize.x, layoutCellSize.z, layoutCellSize.y);
            }

            if (TryGetTilePrefabBounds(out Bounds prefabBounds))
            {
                return new CubeTileMetrics(prefabBounds.size.x, prefabBounds.size.z, prefabBounds.size.y);
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

            Collider[] colliders = tilePrefab.GetComponentsInChildren<Collider>(true);
            if (TryEncapsulateBounds(colliders, out prefabBounds))
            {
                return true;
            }

            Renderer[] renderers = tilePrefab.GetComponentsInChildren<Renderer>(true);
            return TryEncapsulateBounds(renderers, out prefabBounds);
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
        /// Builds the next larger cube shell by expanding the shorter face axis until the previous shell is fully covered.
        /// </summary>
        private static CubeShellPlan CreateCoveringCubeShellPlan(CubeShellPlan previousShell, CubeTileMetrics metrics)
        {
            float requiredSideLength = previousShell.SideLength + (metrics.Thickness * 2f);
            int columnCount = Mathf.Max(1, previousShell.ColumnCount);
            int rowCount = Mathf.Max(1, previousShell.RowCount);
            float edgeInset = GetCubeFaceEdgeInset(metrics);

            while (true)
            {
                float panelWidth = columnCount * metrics.FaceWidth;
                float panelHeight = rowCount * metrics.FaceHeight;
                float sideLength = Mathf.Max(panelWidth, panelHeight) + (edgeInset * 2f);
                if (sideLength + 0.0001f >= requiredSideLength)
                {
                    return new CubeShellPlan(columnCount, rowCount, sideLength);
                }

                if (panelWidth <= panelHeight)
                {
                    columnCount++;
                }
                else
                {
                    rowCount++;
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
        /// Resolves the center-to-center step that stretches one tile strip to fill a square panel without overlap.
        /// </summary>
        private static float GetSquarePanelStep(int tileCount, float tileSize, float targetPanelSideLength)
        {
            int safeTileCount = Mathf.Max(1, tileCount);
            float safeTileSize = Mathf.Max(0.01f, tileSize);
            if (safeTileCount <= 1)
            {
                return 0f;
            }

            float step = (Mathf.Max(safeTileSize, targetPanelSideLength) - safeTileSize) / (safeTileCount - 1);
            return Mathf.Max(safeTileSize, step);
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
        /// Returns how many intermediate scale samples should be tested to find unique nested layers.
        /// </summary>
        private static int GetNestedLayerScaleSampleCount(VoxelGridSize gridSize, LevelShapeType shape)
        {
            int minDimension = Mathf.Min(gridSize.Width, Mathf.Min(gridSize.Height, gridSize.Depth));
            return shape == LevelShapeType.Castle ? Mathf.Max(10, minDimension * 4) : Mathf.Max(12, minDimension * 5);
        }

        /// <summary>
        /// Returns the smallest scale allowed for the innermost nested silhouette.
        /// </summary>
        private static float GetMinimumNestedScale(LevelShapeType shape)
        {
            return shape == LevelShapeType.Castle ? 0.46f : 0.34f;
        }

        /// <summary>
        /// Builds one full silhouette volume at a given scale.
        /// </summary>
        private static HashSet<Vector3Int> BuildScaledShapeCoordinateSet(VoxelGridSize gridSize, LevelShapeType shape, float scale)
        {
            HashSet<Vector3Int> coordinates = new HashSet<Vector3Int>();
            for (int x = 0; x < gridSize.Width; x++)
            {
                for (int y = 0; y < gridSize.Height; y++)
                {
                    for (int z = 0; z < gridSize.Depth; z++)
                    {
                        Vector3Int coordinate = new Vector3Int(x, y, z);
                        if (IsCoordinateInsideScaledShape(coordinate, gridSize, shape, scale))
                        {
                            coordinates.Add(coordinate);
                        }
                    }
                }
            }

            return coordinates;
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

                case LevelShapeType.Heart:
                    width = Mathf.Max(width, 5);
                    height = Mathf.Max(height, 5);
                    depth = Mathf.Max(safeLayerCount, 4);
                    break;

                case LevelShapeType.Castle:
                    width = Mathf.Max(width + 1, 6);
                    height = Mathf.Max(height, 5);
                    depth = Mathf.Max(depth, 5);
                    break;
            }

            return new VoxelGridSize(width, height, depth);
        }

        /// <summary>
        /// Builds all voxel coordinates belonging to the requested silhouette.
        /// </summary>
        private static List<Vector3Int> BuildShapeCoordinates(VoxelGridSize gridSize, LevelShapeType shape)
        {
            List<Vector3Int> coordinates = new List<Vector3Int>(gridSize.Volume);
            for (int x = 0; x < gridSize.Width; x++)
            {
                for (int y = 0; y < gridSize.Height; y++)
                {
                    for (int z = 0; z < gridSize.Depth; z++)
                    {
                        Vector3Int coordinate = new Vector3Int(x, y, z);
                        if (IsCoordinateInsideShape(coordinate, gridSize, shape))
                        {
                            coordinates.Add(coordinate);
                        }
                    }
                }
            }

            if (coordinates.Count < 2)
            {
                return BuildShapeCoordinates(gridSize, LevelShapeType.Cube);
            }

            return coordinates;
        }

        /// <summary>
        /// Returns whether a voxel belongs to the scaled version of a silhouette.
        /// </summary>
        private static bool IsCoordinateInsideScaledShape(Vector3Int coordinate, VoxelGridSize gridSize, LevelShapeType shape, float scale)
        {
            switch (shape)
            {
                case LevelShapeType.Heart:
                    return IsInsideHeartShape(
                        ScaleCenteredSigned(NormalizeAxis(coordinate.x, gridSize.Width), scale),
                        ScaleCenteredHeight(GetHeight01(coordinate.y, gridSize.Height), scale),
                        ScaleCenteredSigned(NormalizeAxis(coordinate.z, gridSize.Depth), scale));

                case LevelShapeType.Castle:
                    return IsInsideCastleShape(
                        ScaleCenteredSigned(NormalizeAxis(coordinate.x, gridSize.Width), scale),
                        ScaleBottomAnchoredHeight(GetHeight01(coordinate.y, gridSize.Height), scale),
                        ScaleCenteredSigned(NormalizeAxis(coordinate.z, gridSize.Depth), scale));

                default:
                    return IsCoordinateInsideShape(coordinate, gridSize, shape);
            }
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
            bool flipTile = random.NextDouble() <= flippedTileChance;
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
        /// Resolves the tile thickness used when shell layers are compacted against each other.
        /// </summary>
        private float GetSurfaceShellThickness()
        {
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
        /// Returns whether a voxel belongs to the requested silhouette.
        /// </summary>
        private static bool IsCoordinateInsideShape(Vector3Int coordinate, VoxelGridSize gridSize, LevelShapeType shape)
        {
            switch (shape)
            {
                case LevelShapeType.Heart:
                    return IsInsideHeart(coordinate, gridSize);
                case LevelShapeType.Castle:
                    return IsInsideCastle(coordinate, gridSize);
                case LevelShapeType.Robot:
                    return IsInsideRobot(coordinate, gridSize);
                case LevelShapeType.Bonsai:
                    return IsInsideBonsai(coordinate, gridSize);
                case LevelShapeType.Statue:
                    return IsInsideStatue(coordinate, gridSize);
                case LevelShapeType.Cat:
                    return IsInsideCat(coordinate, gridSize);
                case LevelShapeType.Dragon:
                    return IsInsideDragon(coordinate, gridSize);
                case LevelShapeType.Custom:
                case LevelShapeType.Cube:
                default:
                    return true;
            }
        }

        /// <summary>
        /// Returns whether a voxel belongs to the heart silhouette.
        /// </summary>
        private static bool IsInsideHeart(Vector3Int coordinate, VoxelGridSize gridSize)
        {
            return IsInsideHeartShape(
                NormalizeAxis(coordinate.x, gridSize.Width),
                GetHeight01(coordinate.y, gridSize.Height),
                NormalizeAxis(coordinate.z, gridSize.Depth));
        }

        /// <summary>
        /// Returns whether a voxel belongs to the castle silhouette.
        /// </summary>
        private static bool IsInsideCastle(Vector3Int coordinate, VoxelGridSize gridSize)
        {
            return IsInsideCastleShape(
                NormalizeAxis(coordinate.x, gridSize.Width),
                GetHeight01(coordinate.y, gridSize.Height),
                NormalizeAxis(coordinate.z, gridSize.Depth));
        }

        /// <summary>
        /// Returns whether normalized coordinates belong to the heart silhouette.
        /// </summary>
        private static bool IsInsideHeartShape(float x, float y, float z)
        {
            bool leftLobe = DistanceSquared(x + 0.34f, y - 0.76f, 0f) <= 0.13f;
            bool rightLobe = DistanceSquared(x - 0.34f, y - 0.76f, 0f) <= 0.13f;

            float bodyHalfWidth = Mathf.Lerp(0.08f, 0.72f, Mathf.InverseLerp(0.08f, 0.62f, y));
            bool upperBody = y >= 0.30f && y <= 0.82f && Mathf.Abs(x) <= bodyHalfWidth;

            float lowerBlend = Mathf.InverseLerp(0.00f, 0.42f, y);
            float lowerHalfWidth = Mathf.Lerp(0.04f, 0.34f, lowerBlend);
            bool lowerPoint = y <= 0.38f && Mathf.Abs(x) <= lowerHalfWidth;

            bool heartFace = leftLobe || rightLobe || upperBody || lowerPoint;
            if (!heartFace)
            {
                return false;
            }

            float centerBias = 1f - Mathf.Clamp01(Mathf.Abs(x) * 0.85f);
            float verticalBias = 1f - Mathf.Abs(y - 0.55f);
            float depthAllowance = Mathf.Lerp(0.18f, 0.92f, Mathf.Clamp01((centerBias * 0.6f) + (verticalBias * 0.4f)));
            return Mathf.Abs(z) <= depthAllowance;
        }

        /// <summary>
        /// Returns whether normalized coordinates belong to the castle silhouette.
        /// </summary>
        private static bool IsInsideCastleShape(float signedX, float height, float signedZ)
        {
            float x = Mathf.Abs(signedX);
            float z = Mathf.Abs(signedZ);

            bool foundation = height <= 0.16f && x <= 0.94f && z <= 0.94f;
            bool outerWalls = height >= 0.12f && height <= 0.58f && x <= 0.90f && z <= 0.90f && (x >= 0.54f || z >= 0.54f);
            bool frontGateCut = height >= 0.12f && height <= 0.40f && signedZ >= 0.54f && Mathf.Abs(signedX) <= 0.22f;
            bool keepBase = height >= 0.18f && height <= 0.78f && x <= 0.28f && z <= 0.28f;
            bool keepTop = height >= 0.78f && height <= 0.94f && x <= 0.36f && z <= 0.36f;
            bool cornerTowerCore = height >= 0.12f && height <= 0.96f && x >= 0.58f && x <= 0.94f && z >= 0.58f && z <= 0.94f;
            bool cornerTowerCaps = height >= 0.86f && x >= 0.50f && x <= 0.98f && z >= 0.50f && z <= 0.98f;
            bool parapetNorth = height >= 0.56f && height <= 0.74f && signedZ >= 0.78f && x <= 0.88f;
            bool parapetSouth = height >= 0.56f && height <= 0.74f && signedZ <= -0.78f && x <= 0.88f;
            bool parapetEast = height >= 0.56f && height <= 0.74f && signedX >= 0.78f && z <= 0.88f;
            bool parapetWest = height >= 0.56f && height <= 0.74f && signedX <= -0.78f && z <= 0.88f;

            bool castle = foundation || outerWalls || keepBase || keepTop || cornerTowerCore || cornerTowerCaps || parapetNorth || parapetSouth || parapetEast || parapetWest;
            return castle && !frontGateCut;
        }

        /// <summary>
        /// Returns whether a voxel belongs to the robot silhouette.
        /// </summary>
        private static bool IsInsideRobot(Vector3Int coordinate, VoxelGridSize gridSize)
        {
            float x = NormalizeAxis(coordinate.x, gridSize.Width);
            float z = NormalizeAxis(coordinate.z, gridSize.Depth);
            float ax = Mathf.Abs(x);
            float az = Mathf.Abs(z);
            float height = GetHeight01(coordinate.y, gridSize.Height);

            bool head = height >= 0.72f && height <= 0.98f && ax <= 0.26f && az <= 0.26f;
            bool neck = height >= 0.66f && height <= 0.76f && ax <= 0.12f && az <= 0.12f;
            bool torso = height >= 0.28f && height <= 0.74f && ax <= 0.38f && az <= 0.24f;
            bool shoulders = height >= 0.50f && height <= 0.66f && ax <= 0.72f && az <= 0.24f;
            bool arms = height >= 0.28f && height <= 0.62f && ax >= 0.42f && ax <= 0.72f && az <= 0.18f;
            bool hips = height >= 0.20f && height <= 0.32f && ax <= 0.42f && az <= 0.24f;
            bool legs = height <= 0.32f && az <= 0.16f && ((x >= -0.38f && x <= -0.10f) || (x >= 0.10f && x <= 0.38f));
            bool feet = height <= 0.10f && az <= 0.28f && ((x >= -0.44f && x <= -0.02f) || (x >= 0.02f && x <= 0.44f));
            return head || neck || torso || shoulders || arms || hips || legs || feet;
        }

        /// <summary>
        /// Returns whether a voxel belongs to the bonsai silhouette.
        /// </summary>
        private static bool IsInsideBonsai(Vector3Int coordinate, VoxelGridSize gridSize)
        {
            float x = NormalizeAxis(coordinate.x, gridSize.Width);
            float z = NormalizeAxis(coordinate.z, gridSize.Depth);
            float ax = Mathf.Abs(x);
            float az = Mathf.Abs(z);
            float height = GetHeight01(coordinate.y, gridSize.Height);

            bool pot = height <= 0.18f && ax <= 0.60f && az <= 0.60f;
            bool trunk = height >= 0.12f && height <= 0.62f && Mathf.Abs(x - 0.08f + (height * 0.12f)) <= 0.14f && az <= 0.14f;
            bool canopyA = DistanceSquared(x, height - 0.72f, z) <= 0.30f;
            bool canopyB = DistanceSquared(x + 0.28f, height - 0.62f, z + 0.12f) <= 0.18f;
            bool canopyC = DistanceSquared(x - 0.22f, height - 0.68f, z - 0.16f) <= 0.17f;
            return pot || trunk || canopyA || canopyB || canopyC;
        }

        /// <summary>
        /// Returns whether a voxel belongs to the statue silhouette.
        /// </summary>
        private static bool IsInsideStatue(Vector3Int coordinate, VoxelGridSize gridSize)
        {
            float x = NormalizeAxis(coordinate.x, gridSize.Width);
            float z = NormalizeAxis(coordinate.z, gridSize.Depth);
            float ax = Mathf.Abs(x);
            float az = Mathf.Abs(z);
            float height = GetHeight01(coordinate.y, gridSize.Height);

            bool pedestal = height <= 0.18f && ax <= 0.64f && az <= 0.64f;
            bool lowerBody = height >= 0.18f && height <= 0.50f && ax <= 0.26f && az <= 0.22f;
            bool upperBody = height >= 0.46f && height <= 0.78f && ax <= 0.34f && az <= 0.24f;
            bool arms = height >= 0.52f && height <= 0.68f && ax <= 0.60f && az <= 0.14f;
            bool head = height >= 0.76f && height <= 0.98f && ax <= 0.18f && az <= 0.18f;
            return pedestal || lowerBody || upperBody || arms || head;
        }

        /// <summary>
        /// Returns whether a voxel belongs to the cat silhouette.
        /// </summary>
        private static bool IsInsideCat(Vector3Int coordinate, VoxelGridSize gridSize)
        {
            float x = NormalizeAxis(coordinate.x, gridSize.Width);
            float z = NormalizeAxis(coordinate.z, gridSize.Depth);
            float ax = Mathf.Abs(x);
            float az = Mathf.Abs(z);
            float height = GetHeight01(coordinate.y, gridSize.Height);

            bool body = height >= 0.18f && height <= 0.62f && ax <= 0.48f && az <= 0.28f;
            bool head = height >= 0.56f && height <= 0.90f && ax <= 0.24f && z >= 0.08f && z <= 0.50f;
            bool ears = height >= 0.80f && height <= 1.00f && z >= 0.12f && z <= 0.42f && ((x >= -0.30f && x <= -0.10f) || (x >= 0.10f && x <= 0.30f));
            bool paws = height <= 0.22f && az <= 0.20f && ((x >= -0.46f && x <= -0.20f) || (x >= -0.12f && x <= 0.12f) || (x >= 0.20f && x <= 0.46f));
            bool tail = height >= 0.34f && height <= 0.88f && x >= 0.32f && x <= 0.56f && z <= -0.08f && z >= -0.30f;
            return body || head || ears || paws || tail;
        }

        /// <summary>
        /// Returns whether a voxel belongs to the dragon silhouette.
        /// </summary>
        private static bool IsInsideDragon(Vector3Int coordinate, VoxelGridSize gridSize)
        {
            float x = NormalizeAxis(coordinate.x, gridSize.Width);
            float z = NormalizeAxis(coordinate.z, gridSize.Depth);
            float ax = Mathf.Abs(x);
            float height = GetHeight01(coordinate.y, gridSize.Height);
            float spineX = Mathf.Sin((z + 1f) * 2.1f) * 0.30f;

            bool body = height >= 0.24f && height <= 0.58f && Mathf.Abs(x - spineX) <= 0.24f && Mathf.Abs(z) <= 0.90f;
            bool neck = height >= 0.50f && height <= 0.86f && Mathf.Abs(x - 0.08f) <= 0.14f && z >= 0.06f && z <= 0.72f;
            bool head = height >= 0.74f && height <= 0.98f && x >= -0.14f && x <= 0.26f && z >= 0.42f && z <= 0.92f;
            bool wingLeft = height >= 0.38f && height <= 0.76f && x <= -0.14f && x >= -0.82f && Mathf.Abs(z) <= 0.52f && (Mathf.Abs(x + 0.46f) + Mathf.Abs(height - 0.58f) * 1.4f + Mathf.Abs(z) * 0.7f <= 0.78f);
            bool wingRight = height >= 0.38f && height <= 0.76f && x >= 0.14f && x <= 0.82f && Mathf.Abs(z) <= 0.52f && (Mathf.Abs(x - 0.46f) + Mathf.Abs(height - 0.58f) * 1.4f + Mathf.Abs(z) * 0.7f <= 0.78f);
            bool tail = height >= 0.18f && height <= 0.42f && Mathf.Abs(x - Mathf.Sin((z + 1f) * 2.7f) * 0.38f) <= 0.16f && z <= -0.32f;
            bool horns = height >= 0.88f && height <= 1.00f && z >= 0.62f && ((x >= -0.26f && x <= -0.10f) || (x >= 0.10f && x <= 0.26f));
            return body || neck || head || wingLeft || wingRight || tail || horns || (ax <= 0.10f && height <= 0.18f && z >= -0.16f && z <= 0.18f);
        }

        /// <summary>
        /// Converts a voxel index to a normalized range from -1 to 1.
        /// </summary>
        private static float NormalizeAxis(int index, int size)
        {
            if (size <= 1)
            {
                return 0f;
            }

            return ((index / (float)(size - 1)) * 2f) - 1f;
        }

        /// <summary>
        /// Scales a signed -1..1 coordinate around the center.
        /// </summary>
        private static float ScaleCenteredSigned(float value, float scale)
        {
            return scale <= Mathf.Epsilon ? value : value / scale;
        }

        /// <summary>
        /// Scales a 0..1 height coordinate around the vertical center.
        /// </summary>
        private static float ScaleCenteredHeight(float value, float scale)
        {
            if (scale <= Mathf.Epsilon)
            {
                return value;
            }

            return (((value - 0.5f) / scale) + 0.5f);
        }

        /// <summary>
        /// Scales a 0..1 height coordinate while keeping the silhouette anchored to the bottom.
        /// </summary>
        private static float ScaleBottomAnchoredHeight(float value, float scale)
        {
            return scale <= Mathf.Epsilon ? value : value / scale;
        }

        /// <summary>
        /// Converts a voxel height index to a normalized 0..1 range.
        /// </summary>
        private static float GetHeight01(int index, int size)
        {
            if (size <= 1)
            {
                return 0f;
            }

            return index / (float)(size - 1);
        }

        /// <summary>
        /// Returns a squared distance value for simple blob tests.
        /// </summary>
        private static float DistanceSquared(float x, float y, float z)
        {
            return (x * x) + (y * y) + (z * z);
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
            serializedObject.FindProperty("<Shape>k__BackingField").enumValueIndex = (int)data.Shape;
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

            if (replaceCatalogEntries)
            {
                levelsProperty.ClearArray();
            }

            int insertIndex = levelsProperty.arraySize;
            for (int index = 0; index < generatedAssets.Count; index++)
            {
                levelsProperty.InsertArrayElementAtIndex(insertIndex);
                levelsProperty.GetArrayElementAtIndex(insertIndex).objectReferenceValue = generatedAssets[index];
                insertIndex++;
            }

            catalogObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetCatalog);
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
