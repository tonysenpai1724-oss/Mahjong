using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds concentric direct rectangular shells.
    /// </summary>
    internal sealed class RectangleShellLayoutBuilder
    {
        private readonly ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics;
        private readonly float inPlaneGap;

        public RectangleShellLayoutBuilder(ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics, float inPlaneGap)
        {
            this.tileMetrics = tileMetrics;
            this.inPlaneGap = Mathf.Max(0f, inPlaneGap);
        }

        public List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(
            VoxelGridSize gridSize,
            int targetLayerCount,
            int minTileCount,
            int maxTileCount,
            System.Random random)
        {
            return NestedDirectShellLayoutBuilder.Build(
                gridSize,
                new VoxelGridSize(2, 2, 2),
                targetLayerCount,
                minTileCount,
                maxTileCount,
                tileMetrics,
                inPlaneGap,
                random,
                CreateOccupancy,
                ResolveCellStep);
        }

        private static HashSet<Vector3Int> CreateOccupancy(VoxelGridSize size)
        {
            HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>(size.Volume);
            DirectShellLayoutBuilder.AddBox(occupiedCells, 0, Mathf.Max(2, size.Width) - 1, 0, Mathf.Max(2, size.Height) - 1, 0, Mathf.Max(2, size.Depth) - 1);
            return occupiedCells;
        }

        private Vector3 ResolveCellStep()
        {
            return new Vector3(
                tileMetrics.FaceWidth + inPlaneGap,
                tileMetrics.FaceHeight + inPlaneGap,
                tileMetrics.FaceHeight + inPlaneGap);
        }
    }
}
