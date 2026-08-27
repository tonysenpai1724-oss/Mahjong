using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds compact stepped pyramid shells with every outer face exposed.
    /// </summary>
    internal sealed class PyramidShellLayoutBuilder
    {
        public List<List<ProceduralLevelBatchGenerator.TilePlacementData>> Build(VoxelGridSize gridSize)
        {
            return VoxelShapeShellLayoutBuilder.BuildShells(BuildPyramidCoordinates(gridSize));
        }

        private static List<Vector3Int> BuildPyramidCoordinates(VoxelGridSize gridSize)
        {
            int width = Mathf.Max(1, gridSize.Width);
            int height = Mathf.Max(1, gridSize.Height);
            int depth = Mathf.Max(1, gridSize.Depth);
            List<Vector3Int> coordinates = new List<Vector3Int>(gridSize.Volume);

            for (int y = 0; y < height; y++)
            {
                int insetX = Mathf.Min(y, Mathf.Max(0, (width - 1) / 2));
                int insetZ = Mathf.Min(y, Mathf.Max(0, (depth - 1) / 2));
                int minX = insetX;
                int maxX = Mathf.Max(minX, width - 1 - insetX);
                int minZ = insetZ;
                int maxZ = Mathf.Max(minZ, depth - 1 - insetZ);

                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
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
