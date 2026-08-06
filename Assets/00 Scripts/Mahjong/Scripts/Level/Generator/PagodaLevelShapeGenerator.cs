using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds a pagoda as a solid stepped volume, then peels it into nested shells.
    /// This keeps the visible layers wrapped around each other like the cube generator.
    /// </summary>
    internal static class PagodaLevelShapeGenerator
    {
        private static readonly Vector3Int[] NeighborDirections =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1),
        };

        private const int MaxTilesPerAxis = 10;
        private const float TargetTileRangeBias = 0.72f;
        private const float FootprintMismatchPenalty = 45f;
        private const float DistinctTierReward = 10f;
        private const float OversizePenalty = 0.1f;

        private readonly struct TierPlan
        {
            public TierPlan(int columnCount, int rowCount, int heightCount)
            {
                ColumnCount = Mathf.Max(1, columnCount);
                RowCount = Mathf.Max(1, rowCount);
                HeightCount = Mathf.Max(1, heightCount);
            }

            public int ColumnCount { get; }

            public int RowCount { get; }

            public int HeightCount { get; }
        }

        public static VoxelGridSize BuildGridSize(int layerCount)
        {
            int safeLayerCount = Mathf.Max(1, layerCount);
            int width = Mathf.Max(8, (safeLayerCount * 2) + 4);
            int height = Mathf.Max(6, (safeLayerCount * 2) + 2);
            int depth = Mathf.Max(8, (safeLayerCount * 2) + 4);
            return new VoxelGridSize(width, height, depth);
        }

        public static List<List<ProceduralLevelBatchGenerator.TilePlacementData>> BuildShells(
            int targetLayerCount,
            ProceduralLevelBatchGenerator.CubeTileMetrics metrics,
            int minTileCount,
            int maxTileCount)
        {
            VoxelGridSize gridSize = BuildGridSize(targetLayerCount);
            List<TierPlan> tierPlans = SelectTierPlans(targetLayerCount, metrics, minTileCount, maxTileCount, gridSize);
            if (tierPlans.Count == 0)
            {
                return new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>();
            }

            HashSet<Vector3Int> occupiedCoordinates = BuildOccupiedCoordinates(tierPlans, gridSize);
            return BuildShellsFromOccupied(occupiedCoordinates);
        }

        private static List<TierPlan> SelectTierPlans(
            int targetLayerCount,
            ProceduralLevelBatchGenerator.CubeTileMetrics metrics,
            int minTileCount,
            int maxTileCount,
            VoxelGridSize gridSize)
        {
            int tierCount = Mathf.Max(2, targetLayerCount);
            int desiredTileCount = Mathf.RoundToInt(Mathf.Lerp(Mathf.Max(2, minTileCount), Mathf.Max(minTileCount, maxTileCount), TargetTileRangeBias));

            float bestScore = float.MaxValue;
            int bestCapacity = 0;
            List<TierPlan> bestPlans = null;

            int minBaseWidth = Mathf.Max(4, targetLayerCount + 2);
            int maxBaseWidth = Mathf.Min(MaxTilesPerAxis, targetLayerCount + 5);
            int minBaseDepth = Mathf.Max(4, targetLayerCount + 2);
            int maxBaseDepth = Mathf.Min(MaxTilesPerAxis, targetLayerCount + 5);

            for (int baseColumnCount = minBaseWidth; baseColumnCount <= maxBaseWidth; baseColumnCount++)
            {
                for (int baseRowCount = minBaseDepth; baseRowCount <= maxBaseDepth; baseRowCount++)
                {
                    List<TierPlan> candidatePlans = BuildTierPlans(baseColumnCount, baseRowCount, tierCount, metrics);
                    if (candidatePlans.Count < 2)
                    {
                        continue;
                    }

                    HashSet<Vector3Int> occupiedCoordinates = BuildOccupiedCoordinates(candidatePlans, gridSize);
                    List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells = BuildShellsFromOccupied(occupiedCoordinates);
                    int capacity = GetShellTileCapacity(shells);
                    int availableLayers = shells.Count;
                    if (capacity < 2 || availableLayers < 2)
                    {
                        continue;
                    }

                    TierPlan basePlan = candidatePlans[0];
                    float mismatch = GetFootprintMismatch(basePlan, metrics);
                    float rangeDistance = GetDistanceToTileRange(capacity, minTileCount, maxTileCount, desiredTileCount);
                    float score = rangeDistance;
                    score += mismatch * FootprintMismatchPenalty;
                    score -= Mathf.Min(availableLayers, targetLayerCount) * DistinctTierReward;
                    score += Mathf.Max(0, capacity - maxTileCount) * OversizePenalty;

                    bool isBetter = score + 0.0001f < bestScore;
                    bool isTieWithCloserCapacity = Mathf.Abs(score - bestScore) <= 0.0001f && Mathf.Abs(capacity - desiredTileCount) < Mathf.Abs(bestCapacity - desiredTileCount);
                    if (!isBetter && !isTieWithCloserCapacity)
                    {
                        continue;
                    }

                    bestScore = score;
                    bestCapacity = capacity;
                    bestPlans = candidatePlans;
                }
            }

            if (bestPlans != null)
            {
                return bestPlans;
            }

            return BuildTierPlans(Mathf.Max(4, targetLayerCount + 2), Mathf.Max(4, targetLayerCount + 2), tierCount, metrics);
        }

        private static List<TierPlan> BuildTierPlans(int baseColumnCount, int baseRowCount, int tierCount, ProceduralLevelBatchGenerator.CubeTileMetrics metrics)
        {
            List<TierPlan> plans = new List<TierPlan>(tierCount);
            int currentColumnCount = Mathf.Max(1, baseColumnCount);
            int currentRowCount = Mathf.Max(1, baseRowCount);

            for (int tierIndex = 0; tierIndex < tierCount; tierIndex++)
            {
                plans.Add(new TierPlan(currentColumnCount, currentRowCount, 2));
                ShrinkTierFootprint(metrics, ref currentColumnCount, ref currentRowCount);
            }

            return plans;
        }

        private static void ShrinkTierFootprint(ProceduralLevelBatchGenerator.CubeTileMetrics metrics, ref int columnCount, ref int rowCount)
        {
            float widthSpan = columnCount * metrics.FaceWidth;
            float depthSpan = rowCount * metrics.FaceHeight;

            if (columnCount > 2)
            {
                columnCount--;
            }

            if (rowCount > 2)
            {
                rowCount--;
            }

            if (columnCount == rowCount)
            {
                return;
            }

            if (widthSpan > depthSpan && rowCount > 2)
            {
                rowCount--;
                return;
            }

            if (depthSpan > widthSpan && columnCount > 2)
            {
                columnCount--;
            }
        }

        private static HashSet<Vector3Int> BuildOccupiedCoordinates(List<TierPlan> tierPlans, VoxelGridSize gridSize)
        {
            HashSet<Vector3Int> occupiedCoordinates = new HashSet<Vector3Int>();
            int currentY = 0;

            for (int tierIndex = 0; tierIndex < tierPlans.Count; tierIndex++)
            {
                TierPlan plan = tierPlans[tierIndex];
                int minX = Mathf.Max(0, (gridSize.Width - plan.ColumnCount) / 2);
                int minZ = Mathf.Max(0, (gridSize.Depth - plan.RowCount) / 2);
                int maxX = Mathf.Min(gridSize.Width, minX + plan.ColumnCount);
                int maxY = Mathf.Min(gridSize.Height, currentY + plan.HeightCount);
                int maxZ = Mathf.Min(gridSize.Depth, minZ + plan.RowCount);

                for (int x = minX; x < maxX; x++)
                {
                    for (int y = currentY; y < maxY; y++)
                    {
                        for (int z = minZ; z < maxZ; z++)
                        {
                            occupiedCoordinates.Add(new Vector3Int(x, y, z));
                        }
                    }
                }

                currentY = maxY;
                if (currentY >= gridSize.Height)
                {
                    break;
                }
            }

            return occupiedCoordinates;
        }

        private static List<List<ProceduralLevelBatchGenerator.TilePlacementData>> BuildShellsFromOccupied(HashSet<Vector3Int> occupiedCoordinates)
        {
            List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells = new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>();
            HashSet<Vector3Int> remaining = new HashSet<Vector3Int>(occupiedCoordinates);

            while (remaining.Count > 0)
            {
                List<ProceduralLevelBatchGenerator.TilePlacementData> shell = ExtractSurfaceShell(remaining);
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

        private static List<ProceduralLevelBatchGenerator.TilePlacementData> ExtractSurfaceShell(HashSet<Vector3Int> occupiedCoordinates)
        {
            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = new List<ProceduralLevelBatchGenerator.TilePlacementData>();
            foreach (Vector3Int coordinate in occupiedCoordinates)
            {
                for (int directionIndex = 0; directionIndex < NeighborDirections.Length; directionIndex++)
                {
                    Vector3Int neighbor = coordinate + NeighborDirections[directionIndex];
                    if (occupiedCoordinates.Contains(neighbor))
                    {
                        continue;
                    }

                    shell.Add(new ProceduralLevelBatchGenerator.TilePlacementData
                    {
                        Coordinate = coordinate,
                        FacingDirection = ToGridDirection(NeighborDirections[directionIndex]),
                        SurfaceSlotIndex = -1,
                        UseCustomLocalPosition = false,
                    });
                }
            }

            return shell;
        }

        private static VoxelGridDirection ToGridDirection(Vector3Int offset)
        {
            if (offset == Vector3Int.right)
            {
                return VoxelGridDirection.Right;
            }

            if (offset == Vector3Int.left)
            {
                return VoxelGridDirection.Left;
            }

            if (offset == Vector3Int.up)
            {
                return VoxelGridDirection.Up;
            }

            if (offset == Vector3Int.down)
            {
                return VoxelGridDirection.Down;
            }

            if (offset.z > 0)
            {
                return VoxelGridDirection.Forward;
            }

            return VoxelGridDirection.Back;
        }

        private static int GetShellTileCapacity(List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells)
        {
            int total = 0;
            for (int index = 0; index < shells.Count; index++)
            {
                total += shells[index].Count;
            }

            return total;
        }

        private static float GetFootprintMismatch(TierPlan plan, ProceduralLevelBatchGenerator.CubeTileMetrics metrics)
        {
            float widthSpan = plan.ColumnCount * metrics.FaceWidth;
            float depthSpan = plan.RowCount * metrics.FaceHeight;
            float longestSide = Mathf.Max(widthSpan, depthSpan);
            return longestSide <= 0.01f ? 0f : Mathf.Abs(widthSpan - depthSpan) / longestSide;
        }

        private static float GetDistanceToTileRange(int tileCount, int minTileCount, int maxTileCount, int targetTileCount)
        {
            if (tileCount >= minTileCount && tileCount <= maxTileCount)
            {
                return Mathf.Abs(tileCount - targetTileCount);
            }

            if (tileCount < minTileCount)
            {
                return (minTileCount - tileCount) + 1000f;
            }

            return (tileCount - maxTileCount) + 1000f;
        }
    }
}
