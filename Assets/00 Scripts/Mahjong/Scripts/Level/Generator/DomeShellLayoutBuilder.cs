using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds softly rounded dome shells by shrinking each upper layer toward the center.
    /// </summary>
    internal sealed class DomeShellLayoutBuilder
    {
        public List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(VoxelGridSize gridSize)
        {
            return VoxelShapeShellLayoutBuilder.BuildShells(BuildDomeCoordinates(gridSize));
        }

        private static List<Vector3Int> BuildDomeCoordinates(VoxelGridSize gridSize)
        {
            int width = Mathf.Max(1, gridSize.Width);
            int height = Mathf.Max(1, gridSize.Height);
            int depth = Mathf.Max(1, gridSize.Depth);
            List<Vector3Int> coordinates = new List<Vector3Int>(gridSize.Volume);
            Vector2 center = new Vector2((width - 1) * 0.5f, (depth - 1) * 0.5f);
            float radiusX = Mathf.Max(0.5f, (width - 1) * 0.5f);
            float radiusZ = Mathf.Max(0.5f, (depth - 1) * 0.5f);

            for (int y = 0; y < height; y++)
            {
                float t = height <= 1 ? 0f : y / Mathf.Max(1f, height - 1f);
                float layerScale = Mathf.Sqrt(Mathf.Clamp01(1f - (t * t * 0.92f)));
                float layerRadiusX = Mathf.Max(1f, radiusX * layerScale);
                float layerRadiusZ = Mathf.Max(1f, radiusZ * layerScale);
                int minX = Mathf.Clamp(Mathf.FloorToInt(center.x - layerRadiusX), 0, width - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(center.x + layerRadiusX), 0, width - 1);
                int minZ = Mathf.Clamp(Mathf.FloorToInt(center.y - layerRadiusZ), 0, depth - 1);
                int maxZ = Mathf.Clamp(Mathf.CeilToInt(center.y + layerRadiusZ), 0, depth - 1);

                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        float dx = (x - center.x) / layerRadiusX;
                        float dz = (z - center.y) / layerRadiusZ;
                        if ((dx * dx) + (dz * dz) <= 1f)
                        {
                            coordinates.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }

            EnsureEvenCoordinateCount(coordinates);
            return coordinates;
        }

        private static void EnsureEvenCoordinateCount(List<Vector3Int> coordinates)
        {
            if (coordinates == null || coordinates.Count % 2 == 0 || coordinates.Count == 0)
            {
                return;
            }

            coordinates.RemoveAt(coordinates.Count - 1);
        }
    }
}
