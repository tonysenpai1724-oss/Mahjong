using System;
using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds concentric direct-placement shells for simple silhouettes.
    /// </summary>
    internal static class NestedDirectShellLayoutBuilder
    {
        public static List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(
            VoxelGridSize maximumGridSize,
            VoxelGridSize minimumGridSize,
            int targetLayerCount,
            int minTileCount,
            int maxTileCount,
            ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics,
            float inPlaneGap,
            System.Random random,
            Func<VoxelGridSize, HashSet<Vector3Int>> occupancyFactory,
            Func<Vector3> resolveCellStep)
        {
            int safeLayerCount = Mathf.Max(1, targetLayerCount);
            int safeMinTileCount = Mathf.Max(2, minTileCount);
            int safeMaxTileCount = Mathf.Max(safeMinTileCount, maxTileCount);
            int targetPairCount = random == null
                ? safeMinTileCount / 2
                : random.Next(safeMinTileCount / 2, (safeMaxTileCount / 2) + 1);
            int bestDistance = int.MaxValue;
            int bestPrefixLayerCount = 0;
            List<List<ProceduralLevelBatchGenerator.TilePlacementData>> bestShells = null;

            int minWidth = Mathf.Max(1, minimumGridSize.Width);
            int minHeight = Mathf.Max(1, minimumGridSize.Height);
            int minDepth = Mathf.Max(1, minimumGridSize.Depth);
            int maxWidth = Mathf.Max(minWidth, maximumGridSize.Width);
            int maxHeight = Mathf.Max(minHeight, maximumGridSize.Height);
            int maxDepth = Mathf.Max(minDepth, maximumGridSize.Depth);

            for (int width = minWidth; width <= maxWidth; width++)
            {
                for (int height = minHeight; height <= maxHeight; height++)
                {
                    for (int depth = minDepth; depth <= maxDepth; depth++)
                    {
                        VoxelGridSize outerGridSize = new VoxelGridSize(width, height, depth);
                        List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells = BuildShells(
                            outerGridSize,
                            minimumGridSize,
                            safeLayerCount,
                            tileMetrics,
                            inPlaneGap,
                            occupancyFactory,
                            resolveCellStep);
                        int prefixLayerCount = ResolveBestPrefixLayerCount(shells, safeMinTileCount, safeMaxTileCount);
                        if (prefixLayerCount < safeLayerCount)
                        {
                            continue;
                        }

                        int capacity = 0;
                        for (int shellIndex = 0; shellIndex < prefixLayerCount; shellIndex++)
                        {
                            capacity += shells[shellIndex].Count;
                        }

                        int distance = Mathf.Abs((capacity / 2) - targetPairCount);
                        if (prefixLayerCount > bestPrefixLayerCount
                            || (prefixLayerCount == bestPrefixLayerCount && distance < bestDistance)
                            || (prefixLayerCount == bestPrefixLayerCount && distance == bestDistance && random != null && random.Next(0, 2) == 0))
                        {
                            bestPrefixLayerCount = prefixLayerCount;
                            bestDistance = distance;
                            bestShells = shells;
                        }
                    }
                }
            }

            return bestShells ?? new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>();
        }

        private static int ResolveBestPrefixLayerCount(List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells, int minTileCount, int maxTileCount)
        {
            if (shells == null || shells.Count == 0)
            {
                return 0;
            }

            int cumulativeCount = 0;
            int bestLayerCount = 0;
            for (int index = 0; index < shells.Count; index++)
            {
                List<ProceduralLevelBatchGenerator.TilePlacementData> shell = shells[index];
                if (shell == null || shell.Count == 0)
                {
                    return bestLayerCount;
                }

                cumulativeCount += shell.Count;
                if (cumulativeCount % 2 == 0 && cumulativeCount >= minTileCount && cumulativeCount <= maxTileCount)
                {
                    bestLayerCount = index + 1;
                }
            }

            return bestLayerCount;
        }

        private static List<List<ProceduralLevelBatchGenerator.TilePlacementData>> BuildShells(
            VoxelGridSize outerGridSize,
            VoxelGridSize minimumGridSize,
            int layerCount,
            ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics,
            float inPlaneGap,
            Func<VoxelGridSize, HashSet<Vector3Int>> occupancyFactory,
            Func<Vector3> resolveCellStep)
        {
            List<List<ProceduralLevelBatchGenerator.TilePlacementData>> innerToOuter = new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>(layerCount);
            Vector3 fixedCenter = new Vector3(
                (outerGridSize.Width - 1) * 0.5f,
                (outerGridSize.Height - 1) * 0.5f,
                (outerGridSize.Depth - 1) * 0.5f);
            Vector3 cellStep = resolveCellStep();

            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                VoxelGridSize layerGridSize = ResolveLayerGridSize(minimumGridSize, outerGridSize, layerIndex, layerCount);
                HashSet<Vector3Int> occupiedCells = occupancyFactory(layerGridSize);
                OffsetOccupancy(occupiedCells, outerGridSize, layerGridSize);
                List<ProceduralLevelBatchGenerator.TilePlacementData> shell = DirectShellLayoutBuilder.BuildSurfaceShell(
                    occupiedCells,
                    tileMetrics,
                    cellStep,
                    0f,
                    tileMetrics.Thickness + inPlaneGap,
                    tileMetrics.Thickness + inPlaneGap,
                    fixedCenter);
                if (shell == null || shell.Count == 0)
                {
                    return null;
                }

                innerToOuter.Add(shell);
            }

            innerToOuter.Reverse();
            return innerToOuter;
        }

        private static void OffsetOccupancy(HashSet<Vector3Int> occupiedCells, VoxelGridSize outerGridSize, VoxelGridSize layerGridSize)
        {
            if (occupiedCells == null || occupiedCells.Count == 0)
            {
                return;
            }

            int offsetX = Mathf.Max(0, (outerGridSize.Width - layerGridSize.Width) / 2);
            int offsetY = Mathf.Max(0, (outerGridSize.Height - layerGridSize.Height) / 2);
            int offsetZ = Mathf.Max(0, (outerGridSize.Depth - layerGridSize.Depth) / 2);
            if (offsetX == 0 && offsetY == 0 && offsetZ == 0)
            {
                return;
            }

            List<Vector3Int> coordinates = new List<Vector3Int>(occupiedCells);
            occupiedCells.Clear();
            for (int index = 0; index < coordinates.Count; index++)
            {
                occupiedCells.Add(coordinates[index] + new Vector3Int(offsetX, offsetY, offsetZ));
            }
        }

        private static VoxelGridSize ResolveLayerGridSize(VoxelGridSize minimumGridSize, VoxelGridSize outerGridSize, int layerIndex, int layerCount)
        {
            int width = ResolveLayerDimension(minimumGridSize.Width, outerGridSize.Width, layerIndex, layerCount);
            int height = ResolveLayerDimension(minimumGridSize.Height, outerGridSize.Height, layerIndex, layerCount);
            int depth = ResolveLayerDimension(minimumGridSize.Depth, outerGridSize.Depth, layerIndex, layerCount);
            return new VoxelGridSize(width, height, depth);
        }

        private static int ResolveLayerDimension(int minimum, int maximum, int layerIndex, int layerCount)
        {
            if (layerCount <= 1)
            {
                return Mathf.Max(minimum, maximum);
            }

            float progress = layerIndex / (float)(layerCount - 1);
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(minimum, maximum, progress)), minimum, maximum);
        }
    }
}
