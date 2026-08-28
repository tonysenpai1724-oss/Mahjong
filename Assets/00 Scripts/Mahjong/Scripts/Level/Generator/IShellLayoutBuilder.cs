using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds a tall 3D I-shaped block from rectangular segments.
    /// </summary>
    internal sealed class IShellLayoutBuilder
    {
        private readonly ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics;
        private readonly float inPlaneGap;

        public IShellLayoutBuilder(ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics, float inPlaneGap)
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
            int barWidth = Mathf.Max(1, Mathf.CeilToInt(widthCount * 0.33f));
            int barDepth = Mathf.Max(1, Mathf.CeilToInt(depthCount * 0.5f));
            int startX = Mathf.Clamp((widthCount - barWidth) / 2, 0, Mathf.Max(0, widthCount - barWidth));
            int startZ = Mathf.Clamp((depthCount - barDepth) / 2, 0, Mathf.Max(0, depthCount - barDepth));

            DirectShellLayoutBuilder.AddBox(occupiedCells, startX, startX + barWidth - 1, 0, heightCount - 1, startZ, startZ + barDepth - 1);

            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = DirectShellLayoutBuilder.BuildSurfaceShell(occupiedCells, tileMetrics, ResolveCellStep(), 0f, 0f, tileMetrics.Thickness + inPlaneGap);
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
