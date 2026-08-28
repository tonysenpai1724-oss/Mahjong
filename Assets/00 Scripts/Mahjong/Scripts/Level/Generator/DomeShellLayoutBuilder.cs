using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds a solid rounded dome and emits one direct tile shell for the complete block.
    /// </summary>
    internal sealed class DomeShellLayoutBuilder
    {
        private readonly ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics;
        private readonly float inPlaneGap;

        public DomeShellLayoutBuilder(ProceduralLevelBatchGenerator.CubeTileMetrics tileMetrics, float inPlaneGap)
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
            Vector2 center = new Vector2((widthCount - 1) * 0.5f, (depthCount - 1) * 0.5f);
            float radiusX = Mathf.Max(0.5f, (widthCount - 1) * 0.5f);
            float radiusZ = Mathf.Max(0.5f, (depthCount - 1) * 0.5f);

            for (int y = 0; y < heightCount; y++)
            {
                float progress = heightCount <= 1 ? 0f : y / Mathf.Max(1f, heightCount - 1f);
                float layerScale = Mathf.Sqrt(Mathf.Clamp01(1f - (progress * progress * 0.92f)));
                float layerRadiusX = Mathf.Max(0.5f, radiusX * layerScale);
                float layerRadiusZ = Mathf.Max(0.5f, radiusZ * layerScale);
                int minX = Mathf.Clamp(Mathf.FloorToInt(center.x - layerRadiusX), 0, widthCount - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(center.x + layerRadiusX), 0, widthCount - 1);
                int minZ = Mathf.Clamp(Mathf.FloorToInt(center.y - layerRadiusZ), 0, depthCount - 1);
                int maxZ = Mathf.Clamp(Mathf.CeilToInt(center.y + layerRadiusZ), 0, depthCount - 1);

                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        float normalizedX = (x - center.x) / layerRadiusX;
                        float normalizedZ = (z - center.y) / layerRadiusZ;
                        if ((normalizedX * normalizedX) + (normalizedZ * normalizedZ) <= 1f)
                        {
                            occupiedCells.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }

            return new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>
            {
                DirectShellLayoutBuilder.BuildSurfaceShell(occupiedCells, tileMetrics, ResolveCellStep(), tileMetrics.Thickness + inPlaneGap, tileMetrics.Thickness + inPlaneGap)
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
