using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds a stepped ramp shell as one contiguous 3D block.
    /// </summary>
    internal sealed class RampShellLayoutBuilder
    {
        private readonly ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics;
        private readonly float inPlaneGap;

        public RampShellLayoutBuilder(ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics, float inPlaneGap)
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
            for (int x = 0; x < widthCount; x++)
            {
                float progress = widthCount <= 1 ? 1f : x / Mathf.Max(1f, widthCount - 1f);
                int columnHeight = Mathf.Clamp(Mathf.CeilToInt(Mathf.Lerp(1f, heightCount, progress)), 1, heightCount);
                DirectShellLayoutBuilder.AddBox(occupiedCells, x, x, 0, columnHeight - 1, 0, depthCount - 1);
            }

            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = DirectShellLayoutBuilder.BuildSurfaceShell(occupiedCells, tileMetrics, ResolveCellStep(), tileMetrics.Thickness + inPlaneGap, tileMetrics.Thickness + inPlaneGap, tileMetrics.Thickness + inPlaneGap);
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
