using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds simple slanted ramp shells that stay readable from the outside.
    /// </summary>
    internal sealed class RampShellLayoutBuilder
    {
        public List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(VoxelGridSize gridSize)
        {
            return VoxelShapeShellLayoutBuilder.BuildShells(BuildRampCoordinates(gridSize));
        }

        private static List<Vector3Int> BuildRampCoordinates(VoxelGridSize gridSize)
        {
            int width = Mathf.Max(1, gridSize.Width);
            int height = Mathf.Max(1, gridSize.Height);
            int depth = Mathf.Max(1, gridSize.Depth);
            List<Vector3Int> coordinates = new List<Vector3Int>(gridSize.Volume);

            for (int x = 0; x < width; x++)
            {
                float progress = width <= 1 ? 1f : x / Mathf.Max(1f, width - 1f);
                int allowedHeight = Mathf.Clamp(Mathf.CeilToInt(Mathf.Lerp(1f, height, progress)), 1, height);
                for (int y = 0; y < allowedHeight; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        coordinates.Add(new Vector3Int(x, y, z));
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
