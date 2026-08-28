using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds a blocky L-shaped shell from rectangular prisms.
    /// </summary>
    internal sealed class LShellLayoutBuilder
    {
        private readonly ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics;
        private readonly float inPlaneGap;

        public LShellLayoutBuilder(ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics, float inPlaneGap)
        {
            this.tileMetrics = tileMetrics;
            this.inPlaneGap = Mathf.Max(0f, inPlaneGap);
        }

        public List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(VoxelGridSize gridSize)
        {
            int widthCount = Mathf.Max(3, gridSize.Width);
            int heightCount = Mathf.Max(3, gridSize.Height);
            int depthCount = Mathf.Max(2, gridSize.Depth);

            HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>();

            int barWidth = Mathf.Max(1, Mathf.CeilToInt(widthCount * 0.26f));
            int barHeight = Mathf.Max(1, Mathf.CeilToInt(heightCount * 0.26f));

            DirectShellLayoutBuilder.AddBox(occupiedCells, 0, barWidth - 1, 0, heightCount - 1, 0, depthCount - 1);
            DirectShellLayoutBuilder.AddBox(occupiedCells, 0, widthCount - 1, 0, barHeight - 1, 0, depthCount - 1);

            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = DirectShellLayoutBuilder.BuildSurfaceShell(occupiedCells, tileMetrics, ResolveCellStep());
            return new List<List<ProceduralLevelBatchGenerator.TilePlacementData>> { shell };
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
