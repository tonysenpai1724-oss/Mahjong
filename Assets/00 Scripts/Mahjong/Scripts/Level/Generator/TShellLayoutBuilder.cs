using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds concentric direct T-shaped shells.
    /// </summary>
    internal sealed class TShellLayoutBuilder
    {
        private readonly ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics;
        private readonly float inPlaneGap;

        public TShellLayoutBuilder(ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics, float inPlaneGap)
        {
            this.tileMetrics = tileMetrics;
            this.inPlaneGap = Mathf.Max(0f, inPlaneGap);
        }

        public List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(VoxelGridSize gridSize, int targetLayerCount, int minTileCount, int maxTileCount, System.Random random)
        {
            return NestedDirectShellLayoutBuilder.Build(
                gridSize,
                new VoxelGridSize(3, 3, 2),
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
            int widthCount = Mathf.Max(3, size.Width);
            int heightCount = Mathf.Max(3, size.Height);
            int depthCount = Mathf.Max(2, size.Depth);
            HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();
            int topBarHeight = Mathf.Max(1, Mathf.CeilToInt(heightCount * 0.28f));
            int stemWidth = Mathf.Max(1, Mathf.CeilToInt(widthCount * 0.30f));
            int stemStartX = Mathf.Clamp((widthCount - stemWidth) / 2, 0, Mathf.Max(0, widthCount - stemWidth));
            int topBarStartY = Mathf.Max(0, heightCount - topBarHeight);
            DirectShellLayoutBuilder.AddBox(occupiedCells, 0, widthCount - 1, topBarStartY, heightCount - 1, 0, depthCount - 1);
            DirectShellLayoutBuilder.AddBox(occupiedCells, stemStartX, stemStartX + stemWidth - 1, 0, heightCount - 1, 0, depthCount - 1);
            return occupiedCells;
        }

        private Vector3 ResolveCellStep()
        {
            return new Vector3(tileMetrics.FaceWidth + inPlaneGap, tileMetrics.FaceHeight + inPlaneGap, tileMetrics.FaceHeight + inPlaneGap);
        }
    }
}
