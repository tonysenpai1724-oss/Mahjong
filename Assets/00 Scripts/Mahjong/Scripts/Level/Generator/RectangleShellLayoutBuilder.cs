using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds a direct rectangular block shell.
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

        public List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(VoxelGridSize gridSize)
        {
            int widthCount = Mathf.Max(2, gridSize.Width);
            int heightCount = Mathf.Max(2, gridSize.Height);
            int depthCount = Mathf.Max(2, gridSize.Depth);

            HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>(gridSize.Volume);
            DirectShellLayoutBuilder.AddBox(occupiedCells, 0, widthCount - 1, 0, heightCount - 1, 0, depthCount - 1);

            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = DirectShellLayoutBuilder.BuildSurfaceShell(occupiedCells, tileMetrics, ResolveCellStep(), 0f, tileMetrics.Thickness + inPlaneGap, tileMetrics.Thickness + inPlaneGap);
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
