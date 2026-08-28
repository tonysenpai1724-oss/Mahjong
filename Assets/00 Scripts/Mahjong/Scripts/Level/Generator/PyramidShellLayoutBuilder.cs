using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds a solid stepped pyramid and emits one direct tile shell for the complete block.
    /// </summary>
    internal sealed class PyramidShellLayoutBuilder
    {
        private readonly ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics;
        private readonly float inPlaneGap;

        public PyramidShellLayoutBuilder(ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics, float inPlaneGap)
        {
            this.tileMetrics = tileMetrics;
            this.inPlaneGap = Mathf.Max(0f, inPlaneGap);
        }

        public List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(VoxelGridSize gridSize)
        {
            int widthCount = Mathf.Max(3, gridSize.Width);
            int heightCount = Mathf.Max(2, gridSize.Height);
            int depthCount = Mathf.Max(3, gridSize.Depth);
            HashSet<Vector3Int> occupiedCells = new HashSet<Vector3Int>(gridSize.Volume);

            for (int y = 0; y < heightCount; y++)
            {
                int insetX = Mathf.Min(y, Mathf.Max(0, (widthCount - 1) / 2));
                int insetZ = Mathf.Min(y, Mathf.Max(0, (depthCount - 1) / 2));
                DirectShellLayoutBuilder.AddBox(occupiedCells, insetX, widthCount - 1 - insetX, y, y, insetZ, depthCount - 1 - insetZ);
            }

            return new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>
            {
                DirectShellLayoutBuilder.BuildSurfaceShell(occupiedCells, tileMetrics, ResolveCellStep(), tileMetrics.Thickness + inPlaneGap, tileMetrics.Thickness + inPlaneGap, tileMetrics.Thickness + inPlaneGap)
            };
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
