using System;
using System.Collections.Generic;
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
        [SerializeField] private string outputFolder = "Assets/00 Scripts/Mahjong/Generated Levels";
        [SerializeField] private string levelNamePrefix = "Generated";
        [SerializeField] private int seed = 20260730;
        [SerializeField] private bool replaceCatalogEntries = true;
        [SerializeField] private bool overwriteExistingAssets = true;
        [SerializeField] private DifficultyBatchDefinition normalSettings = DifficultyBatchDefinition.CreateNormalDefaults();
        [SerializeField] private DifficultyBatchDefinition hardSettings = DifficultyBatchDefinition.CreateHardDefaults();
        [SerializeField] private DifficultyBatchDefinition superHardSettings = DifficultyBatchDefinition.CreateSuperHardDefaults();

        /// <summary>
        /// Describes how many levels to generate for one difficulty tier and the grid sizes that tier may use.
        /// </summary>
        [Serializable]
        public sealed class DifficultyBatchDefinition
        {
            [SerializeField] private string label = "Normal";
            [SerializeField] private int levelCount = 5;
            [SerializeField] private Vector3Int minGridSize = new Vector3Int(4, 4, 3);
            [SerializeField] private Vector3Int maxGridSize = new Vector3Int(4, 4, 4);
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
            /// Gets the minimum voxel grid size allowed for this tier.
            /// </summary>
            public Vector3Int MinGridSize => new Vector3Int(
                Mathf.Max(2, minGridSize.x),
                Mathf.Max(2, minGridSize.y),
                Mathf.Max(2, minGridSize.z));

            /// <summary>
            /// Gets the maximum voxel grid size allowed for this tier.
            /// </summary>
            public Vector3Int MaxGridSize => new Vector3Int(
                Mathf.Max(MinGridSize.x, maxGridSize.x),
                Mathf.Max(MinGridSize.y, maxGridSize.y),
                Mathf.Max(MinGridSize.z, maxGridSize.z));

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
                    minGridSize = new Vector3Int(5, 5, 4),
                    maxGridSize = new Vector3Int(6, 6, 5),
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
                    minGridSize = new Vector3Int(6, 5, 5),
                    maxGridSize = new Vector3Int(7, 6, 5),
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
                    minGridSize = new Vector3Int(5, 5, 5),
                    maxGridSize = new Vector3Int(6, 5, 5),
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
            public List<List<Vector3Int>> Shells { get; set; } = new List<List<Vector3Int>>();

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
                List<Vector3Int> occupiedCoordinates = BuildOccupiedCoordinates(candidate.Shells, tileCount, random);
                List<LevelTileDefinition> tileDefinitions = BuildTileDefinitions(occupiedCoordinates, settings, random);

                results.Add(new GeneratedLevelData
                {
                    LevelName = $"{levelNamePrefix}_{settings.Label}_{sequence:000}",
                    GridSize = candidate.GridSize,
                    LayoutOverride = layoutOverride,
                    Shape = candidate.Shape,
                    Difficulty = settings.Difficulty,
                    Tiles = tileDefinitions,
                });
            }
        }

        /// <summary>
        /// Chooses a random grid size inside the bounds defined for the difficulty tier.
        /// </summary>
        private static VoxelGridSize GetRandomGridSize(DifficultyBatchDefinition settings, System.Random random)
        {
            Vector3Int min = settings.MinGridSize;
            Vector3Int max = settings.MaxGridSize;
            int width = random.Next(min.x, max.x + 1);
            int height = random.Next(min.y, max.y + 1);
            int depth = random.Next(min.z, max.z + 1);
            return new VoxelGridSize(width, height, depth);
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
        private static int GetTargetTileCount(DifficultyBatchDefinition settings, List<List<Vector3Int>> shells, LevelShapeType shape, System.Random random)
        {
            if (shells == null || shells.Count == 0)
            {
                return 0;
            }

            int maxTileCount = Mathf.Max(2, settings.MaxPairCount * 2);
            int minTileCount = Mathf.Max(2, settings.MinPairCount * 2);
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
        /// Generates a shape candidate with enough voxels to support the tier.
        /// </summary>
        private static ShapeCandidate CreateShapeCandidate(DifficultyBatchDefinition settings, System.Random random)
        {
            ShapeCandidate bestCandidate = null;
            int requestedTileCount = settings.MinPairCount * 2;

            for (int attempt = 0; attempt < 12; attempt++)
            {
                LevelShapeType selectedShape = settings.GetRandomShape(random);
                VoxelGridSize gridSize = AdjustGridSizeForShape(GetRandomGridSize(settings, random), selectedShape);
                List<List<Vector3Int>> shells = BuildShapeShells(gridSize, selectedShape);
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

            VoxelGridSize fallbackGridSize = AdjustGridSizeForShape(GetRandomGridSize(settings, random), LevelShapeType.Cube);
            return new ShapeCandidate
            {
                GridSize = fallbackGridSize,
                Shape = LevelShapeType.Cube,
                Shells = BuildShapeShells(fallbackGridSize, LevelShapeType.Cube),
            };
        }

        /// <summary>
        /// Builds the occupied coordinates by taking shell layers from outside to inside.
        /// </summary>
        private static List<Vector3Int> BuildOccupiedCoordinates(List<List<Vector3Int>> shells, int tileCount, System.Random random)
        {
            List<Vector3Int> orderedCoordinates = new List<Vector3Int>(tileCount);
            for (int shellIndex = 0; shellIndex < shells.Count && orderedCoordinates.Count < tileCount; shellIndex++)
            {
                List<Vector3Int> shellCoordinates = new List<Vector3Int>(shells[shellIndex]);
                Shuffle(shellCoordinates, random);

                for (int coordinateIndex = 0; coordinateIndex < shellCoordinates.Count && orderedCoordinates.Count < tileCount; coordinateIndex++)
                {
                    orderedCoordinates.Add(shellCoordinates[coordinateIndex]);
                }
            }

            if (orderedCoordinates.Count % 2 != 0)
            {
                orderedCoordinates.RemoveAt(orderedCoordinates.Count - 1);
            }

            return orderedCoordinates;
        }

        /// <summary>
        /// Builds the shell list for the requested shape.
        /// </summary>
        private static List<List<Vector3Int>> BuildShapeShells(VoxelGridSize gridSize, LevelShapeType shape)
        {
            switch (shape)
            {
                case LevelShapeType.Heart:
                case LevelShapeType.Castle:
                    return BuildNestedSilhouetteShells(gridSize, shape);
                default:
                    return BuildShells(BuildShapeCoordinates(gridSize, shape));
            }
        }

        /// <summary>
        /// Calculates the total tile capacity across all shell layers.
        /// </summary>
        private static int GetShellTileCapacity(List<List<Vector3Int>> shells)
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
        private static List<List<Vector3Int>> BuildNestedSilhouetteShells(VoxelGridSize gridSize, LevelShapeType shape)
        {
            int layerCount = GetNestedLayerCount(gridSize, shape);
            List<HashSet<Vector3Int>> nestedVolumes = new List<HashSet<Vector3Int>>(layerCount);

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                float scale = GetNestedLayerScale(layerIndex, layerCount, shape);
                HashSet<Vector3Int> layerVolume = BuildScaledShapeCoordinateSet(gridSize, shape, scale);
                if (nestedVolumes.Count > 0)
                {
                    layerVolume.IntersectWith(nestedVolumes[nestedVolumes.Count - 1]);
                }

                if (layerVolume.Count < 2)
                {
                    continue;
                }

                if (nestedVolumes.Count > 0 && layerVolume.Count >= nestedVolumes[nestedVolumes.Count - 1].Count)
                {
                    continue;
                }

                nestedVolumes.Add(layerVolume);
            }

            if (nestedVolumes.Count <= 1)
            {
                return BuildShells(BuildShapeCoordinates(gridSize, shape));
            }

            List<List<Vector3Int>> shells = new List<List<Vector3Int>>(nestedVolumes.Count);
            for (int volumeIndex = 0; volumeIndex < nestedVolumes.Count - 1; volumeIndex++)
            {
                List<Vector3Int> shell = new List<Vector3Int>();
                foreach (Vector3Int coordinate in nestedVolumes[volumeIndex])
                {
                    if (!nestedVolumes[volumeIndex + 1].Contains(coordinate))
                    {
                        shell.Add(coordinate);
                    }
                }

                if (shell.Count > 0)
                {
                    shells.Add(shell);
                }
            }

            shells.Add(new List<Vector3Int>(nestedVolumes[nestedVolumes.Count - 1]));
            return shells;
        }

        /// <summary>
        /// Returns how many nested silhouette layers should be generated.
        /// </summary>
        private static int GetNestedLayerCount(VoxelGridSize gridSize, LevelShapeType shape)
        {
            int minDimension = Mathf.Min(gridSize.Width, Mathf.Min(gridSize.Height, gridSize.Depth));
            int maxLayers = shape == LevelShapeType.Castle ? 4 : 5;
            return Mathf.Clamp(minDimension - 1, 2, maxLayers);
        }

        /// <summary>
        /// Returns the scale used by one nested silhouette layer.
        /// </summary>
        private static float GetNestedLayerScale(int layerIndex, int layerCount, LevelShapeType shape)
        {
            if (layerCount <= 1)
            {
                return 1f;
            }

            float minimumScale = shape == LevelShapeType.Castle ? 0.38f : 0.26f;
            float t = layerIndex / (float)(layerCount - 1);
            return Mathf.Lerp(1f, minimumScale, t);
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
        /// Ensures the selected silhouette has enough resolution to be visually recognizable.
        /// </summary>
        private static VoxelGridSize AdjustGridSizeForShape(VoxelGridSize gridSize, LevelShapeType shape)
        {
            int width = gridSize.Width;
            int height = gridSize.Height;
            int depth = gridSize.Depth;

            switch (shape)
            {
                case LevelShapeType.Heart:
                    width = Mathf.Max(width, 5);
                    height = Mathf.Max(height, 5);
                    depth = Mathf.Max(depth, 4);
                    break;
                case LevelShapeType.Castle:
                    width = Mathf.Max(width, 6);
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
        private static List<LevelTileDefinition> BuildTileDefinitions(List<Vector3Int> occupiedCoordinates, DifficultyBatchDefinition settings, System.Random random)
        {
            List<LevelTileDefinition> tileDefinitions = new List<LevelTileDefinition>(occupiedCoordinates.Count);
            int pairCount = occupiedCoordinates.Count / 2;

            for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
            {
                int firstIndex = pairIndex * 2;
                int secondIndex = firstIndex + 1;
                tileDefinitions.Add(CreateTileDefinition(pairIndex, occupiedCoordinates[firstIndex], settings.FlippedTileChance, random));
                tileDefinitions.Add(CreateTileDefinition(pairIndex, occupiedCoordinates[secondIndex], settings.FlippedTileChance, random));
            }

            return tileDefinitions;
        }

        /// <summary>
        /// Creates a single tile definition with an optional 180-degree Y flip.
        /// </summary>
        private static LevelTileDefinition CreateTileDefinition(int matchId, Vector3Int coordinate, float flippedTileChance, System.Random random)
        {
            bool flipTile = random.NextDouble() <= flippedTileChance;
            return new LevelTileDefinition
            {
                MatchId = matchId,
                GridCoordinate = coordinate,
                LocalEulerAngles = flipTile ? new Vector3(0f, 180f, 0f) : Vector3.zero,
            };
        }

        /// <summary>
        /// Extracts the shell layers from a filled voxel shape.
        /// </summary>
        private static List<List<Vector3Int>> BuildShells(List<Vector3Int> occupiedCoordinates)
        {
            List<List<Vector3Int>> shells = new List<List<Vector3Int>>();
            HashSet<Vector3Int> remaining = new HashSet<Vector3Int>(occupiedCoordinates);

            while (remaining.Count > 0)
            {
                List<Vector3Int> shell = ExtractSurfaceShell(remaining);
                if (shell.Count == 0)
                {
                    break;
                }

                shells.Add(shell);
                for (int index = 0; index < shell.Count; index++)
                {
                    remaining.Remove(shell[index]);
                }
            }

            return shells;
        }

        /// <summary>
        /// Finds the current surface voxels of the supplied volume.
        /// </summary>
        private static List<Vector3Int> ExtractSurfaceShell(HashSet<Vector3Int> occupiedCoordinates)
        {
            List<Vector3Int> shell = new List<Vector3Int>();
            foreach (Vector3Int coordinate in occupiedCoordinates)
            {
                for (int directionIndex = 0; directionIndex < NeighborDirections.Length; directionIndex++)
                {
                    Vector3Int neighbor = coordinate + NeighborDirections[directionIndex];
                    if (!occupiedCoordinates.Contains(neighbor))
                    {
                        shell.Add(coordinate);
                        break;
                    }
                }
            }

            return shell;
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
