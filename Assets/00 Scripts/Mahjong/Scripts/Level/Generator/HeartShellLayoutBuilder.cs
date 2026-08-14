using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds concentric heart-shaped shell layouts with clearly separated nested hearts.
    /// </summary>
    internal sealed class HeartShellLayoutBuilder
    {
        private static readonly HeartBlueprint[] Blueprints =
        {
            new HeartBlueprint(17, 15, new[] { 0.75f, 0.88f, 1f, 0.88f, 0.75f }),
            new HeartBlueprint(15, 13, new[] { 0.75f, 0.88f, 1f, 0.88f, 0.75f }),
            new HeartBlueprint(13, 11, new[] { 0.82f, 0.93f, 1f, 0.93f, 0.82f }),
            new HeartBlueprint(11, 9, new[] { 0.82f, 0.93f, 1f, 0.93f, 0.82f }),
            new HeartBlueprint(9, 9, new[] { 0.82f, 0.93f, 1f, 0.93f, 0.82f }),
            new HeartBlueprint(9, 7, new[] { 0.8f, 1f, 0.8f }),
            new HeartBlueprint(7, 7, new[] { 0.8f, 1f, 0.8f }),
            new HeartBlueprint(7, 5, new[] { 0.8f, 1f, 0.8f }),
            new HeartBlueprint(5, 5, new[] { 1f }),
        };

        private readonly ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics;
        private readonly float inPlaneGap;
        private readonly Dictionary<int, int> blueprintCapacityByIndex = new Dictionary<int, int>();

        public HeartShellLayoutBuilder(ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics, float inPlaneGap)
        {
            this.tileMetrics = tileMetrics;
            this.inPlaneGap = Mathf.Max(0f, inPlaneGap);
        }

        /// <summary>
        /// Builds nested heart shells sized to closely match the requested tile budget.
        /// </summary>
        public List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(int targetLayerCount, int minTileCount, int maxTileCount, System.Random random)
        {
            int desiredShellCount = Mathf.Clamp(targetLayerCount, 1, Blueprints.Length);
            int safeMinTileCount = Mathf.Max(2, minTileCount);
            int safeMaxTileCount = Mathf.Max(safeMinTileCount, maxTileCount);
            List<int> selectedBlueprintIndexes = ResolveBlueprintIndexes(desiredShellCount, safeMinTileCount, safeMaxTileCount, random);
            List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells = new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>(selectedBlueprintIndexes.Count);

            for (int index = 0; index < selectedBlueprintIndexes.Count; index++)
            {
                shells.Add(BuildShell(Blueprints[selectedBlueprintIndexes[index]]));
            }

            return shells;
        }

        private List<int> ResolveBlueprintIndexes(int desiredShellCount, int minTileCount, int maxTileCount, System.Random random)
        {
            for (int shellCount = desiredShellCount; shellCount >= 1; shellCount--)
            {
                List<BlueprintSelection> selections = new List<BlueprintSelection>();
                List<int> workingIndexes = new List<int>(shellCount);

                SearchBlueprintCombos(0, shellCount, minTileCount, maxTileCount, workingIndexes, selections);

                if (selections.Count == 0)
                {
                    continue;
                }

                selections.Sort(CompareSelections);

                int topCount = Mathf.Min(4, selections.Count);
                if (random == null || topCount <= 1)
                {
                    return selections[0].Indexes;
                }

                int totalWeight = 0;
                for (int index = 0; index < topCount; index++)
                {
                    totalWeight += topCount - index;
                }

                int roll = random.Next(0, totalWeight);
                for (int index = 0; index < topCount; index++)
                {
                    int weight = topCount - index;
                    if (roll < weight)
                    {
                        return selections[index].Indexes;
                    }

                    roll -= weight;
                }

                return selections[0].Indexes;
            }

            List<int> fallbackIndexes = new List<int>(Mathf.Max(1, desiredShellCount));
            for (int index = 0; index < Mathf.Max(1, desiredShellCount); index++)
            {
                fallbackIndexes.Add(Mathf.Min(index, Blueprints.Length - 1));
            }

            return fallbackIndexes;
        }

        private void SearchBlueprintCombos(
            int startIndex,
            int remainingCount,
            int minTileCount,
            int maxTileCount,
            List<int> workingIndexes,
            List<BlueprintSelection> selections)
        {
            if (remainingCount == 0)
            {
                EvaluateBlueprintCombo(workingIndexes, minTileCount, maxTileCount, selections);
                return;
            }

            int maxStart = Blueprints.Length - remainingCount;
            for (int index = startIndex; index <= maxStart; index++)
            {
                if (workingIndexes.Count > 0 && !HasClearNestedGap(Blueprints[workingIndexes[workingIndexes.Count - 1]], Blueprints[index]))
                {
                    continue;
                }

                workingIndexes.Add(index);
                SearchBlueprintCombos(index + 1, remainingCount - 1, minTileCount, maxTileCount, workingIndexes, selections);
                workingIndexes.RemoveAt(workingIndexes.Count - 1);
            }
        }

        private void EvaluateBlueprintCombo(
            List<int> indexes,
            int minTileCount,
            int maxTileCount,
            List<BlueprintSelection> selections)
        {
            if (indexes == null || indexes.Count == 0)
            {
                return;
            }

            int totalCapacity = 0;
            int separationScore = 0;
            for (int index = 0; index < indexes.Count; index++)
            {
                totalCapacity += GetCapacity(indexes[index]);
                if (index <= 0)
                {
                    continue;
                }

                HeartBlueprint outer = Blueprints[indexes[index - 1]];
                HeartBlueprint inner = Blueprints[indexes[index]];
                separationScore += (outer.Width - inner.Width) + (outer.Height - inner.Height) + (outer.Depth - inner.Depth);
            }

            if (totalCapacity > maxTileCount)
            {
                return;
            }

            bool meetsTarget = totalCapacity >= minTileCount;
            int difference = Mathf.Abs(maxTileCount - totalCapacity);

            selections.Add(new BlueprintSelection(new List<int>(indexes), totalCapacity, difference, separationScore, meetsTarget));
        }

        private static int CompareSelections(BlueprintSelection left, BlueprintSelection right)
        {
            if (left.MeetsTarget != right.MeetsTarget)
            {
                return right.MeetsTarget.CompareTo(left.MeetsTarget);
            }

            int differenceComparison = left.Difference.CompareTo(right.Difference);
            if (differenceComparison != 0)
            {
                return differenceComparison;
            }

            int separationComparison = right.SeparationScore.CompareTo(left.SeparationScore);
            if (separationComparison != 0)
            {
                return separationComparison;
            }

            return left.MeetsTarget
                ? left.TotalCapacity.CompareTo(right.TotalCapacity)
                : right.TotalCapacity.CompareTo(left.TotalCapacity);
        }

        private int GetCapacity(int blueprintIndex)
        {
            if (blueprintCapacityByIndex.TryGetValue(blueprintIndex, out int cachedCapacity))
            {
                return cachedCapacity;
            }

            int capacity = BuildShell(Blueprints[blueprintIndex]).Count;
            blueprintCapacityByIndex[blueprintIndex] = capacity;
            return capacity;
        }

        private List<ProceduralLevelBatchGenerator.TilePlacementData> BuildShell(HeartBlueprint blueprint)
        {
            HashSet<Vector3Int> occupied = BuildOccupiedCoordinates(blueprint);
            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = new List<ProceduralLevelBatchGenerator.TilePlacementData>();

            foreach (Vector3Int coordinate in occupied)
            {
                Vector3 voxelCenter = GetVoxelCenter(coordinate, blueprint);
                for (int directionIndex = 0; directionIndex < NeighborDirections.Length; directionIndex++)
                {
                    NeighborDirection neighborDirection = NeighborDirections[directionIndex];
                    if (occupied.Contains(coordinate + neighborDirection.Offset))
                    {
                        continue;
                    }

                    Vector3 faceNormal = neighborDirection.Normal;
                    Vector3 halfFaceOffset = faceNormal * (tileMetrics.Thickness * 0.5f);

                    shell.Add(new ProceduralLevelBatchGenerator.TilePlacementData
                    {
                        Coordinate = Vector3Int.zero,
                        FacingDirection = neighborDirection.FacingDirection,
                        SurfaceSlotIndex = -1,
                        CustomLocalPosition = voxelCenter + halfFaceOffset + (faceNormal * 0.02f),
                        UseCustomLocalPosition = true,
                        ApplyShellCompaction = false,
                    });
                }
            }

            return shell;
        }

        private HashSet<Vector3Int> BuildOccupiedCoordinates(HeartBlueprint blueprint)
        {
            HashSet<Vector3Int> occupied = new HashSet<Vector3Int>();
            for (int depthIndex = 0; depthIndex < blueprint.Depth; depthIndex++)
            {
                float scale = blueprint.DepthScales[depthIndex];
                for (int y = 0; y < blueprint.Height; y++)
                {
                    for (int x = 0; x < blueprint.Width; x++)
                    {
                        if (!IsInsideHeart(x, y, blueprint.Width, blueprint.Height, scale))
                        {
                            continue;
                        }

                        occupied.Add(new Vector3Int(x, y, depthIndex));
                    }
                }
            }

            return occupied;
        }

        private Vector3 GetVoxelCenter(Vector3Int coordinate, HeartBlueprint blueprint)
        {
            float widthStep = tileMetrics.FaceWidth + inPlaneGap;
            float verticalStep = Mathf.Max(tileMetrics.FaceWidth, tileMetrics.FaceHeight) + inPlaneGap;
            float depthStep = tileMetrics.FaceHeight + inPlaneGap;
            return new Vector3(
                (coordinate.x - ((blueprint.Width - 1) * 0.5f)) * widthStep,
                (coordinate.y - ((blueprint.Height - 1) * 0.5f)) * verticalStep,
                (coordinate.z - ((blueprint.Depth - 1) * 0.5f)) * depthStep);
        }

        private static bool IsInsideHeart(int xIndex, int yIndex, int width, int height, float scale)
        {
            float safeScale = Mathf.Max(0.01f, scale);
            float xScale = Mathf.Max(0.01f, (width / 2.3f) * safeScale);
            float yScale = Mathf.Max(0.01f, (height / 2.3f) * safeScale);
            float x = (xIndex - ((width - 1) * 0.5f)) / xScale;
            float y = ((yIndex - ((height - 1) * 0.5f)) / yScale) + 0.15f;
            float value = Mathf.Pow((x * x) + (y * y) - 1f, 3f) - (x * x * y * y * y);
            return value <= 0f;
        }

        private static bool HasClearNestedGap(HeartBlueprint outer, HeartBlueprint inner)
        {
            int widthDelta = outer.Width - inner.Width;
            int heightDelta = outer.Height - inner.Height;
            return widthDelta >= 2 && heightDelta >= 2 && (widthDelta + heightDelta) >= 4;
        }

        private readonly struct HeartBlueprint
        {
            public HeartBlueprint(int width, int height, float[] depthScales)
            {
                Width = Mathf.Max(1, width);
                Height = Mathf.Max(1, height);
                DepthScales = depthScales ?? new[] { 1f };
            }

            public int Width { get; }

            public int Height { get; }

            public int Depth => DepthScales.Length;

            public float[] DepthScales { get; }
        }

        private readonly struct BlueprintSelection
        {
            public BlueprintSelection(List<int> indexes, int totalCapacity, int difference, int separationScore, bool meetsTarget)
            {
                Indexes = indexes;
                TotalCapacity = totalCapacity;
                Difference = difference;
                SeparationScore = separationScore;
                MeetsTarget = meetsTarget;
            }

            public List<int> Indexes { get; }

            public int TotalCapacity { get; }

            public int Difference { get; }

            public int SeparationScore { get; }

            public bool MeetsTarget { get; }
        }

        private readonly struct NeighborDirection
        {
            public NeighborDirection(Vector3Int offset, Vector3 normal, VoxelGridDirection facingDirection)
            {
                Offset = offset;
                Normal = normal;
                FacingDirection = facingDirection;
            }

            public Vector3Int Offset { get; }

            public Vector3 Normal { get; }

            public VoxelGridDirection FacingDirection { get; }
        }

        private static readonly NeighborDirection[] NeighborDirections =
        {
            new NeighborDirection(Vector3Int.left, Vector3.left, VoxelGridDirection.Left),
            new NeighborDirection(Vector3Int.right, Vector3.right, VoxelGridDirection.Right),
            new NeighborDirection(Vector3Int.down, Vector3.down, VoxelGridDirection.Down),
            new NeighborDirection(Vector3Int.up, Vector3.up, VoxelGridDirection.Up),
            new NeighborDirection(new Vector3Int(0, 0, -1), Vector3.back, VoxelGridDirection.Back),
            new NeighborDirection(new Vector3Int(0, 0, 1), Vector3.forward, VoxelGridDirection.Forward),
        };
    }
}
