using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Extracts visible shell layers from filled voxel coordinate shapes.
    /// </summary>
    internal static class VoxelShapeShellLayoutBuilder
    {
        public static List<List<ProceduralLevelBatchGenerator.TilePlacementData>> BuildShells(List<Vector3Int> occupiedCoordinates)
        {
            List<List<ProceduralLevelBatchGenerator.TilePlacementData>> shells = new List<List<ProceduralLevelBatchGenerator.TilePlacementData>>();
            HashSet<Vector3Int> remaining = new HashSet<Vector3Int>(occupiedCoordinates);

            while (remaining.Count > 0)
            {
                List<ProceduralLevelBatchGenerator.TilePlacementData> shell = ExtractSurfaceShell(remaining);
                if (shell.Count == 0)
                {
                    break;
                }

                shells.Add(shell);
                for (int index = 0; index < shell.Count; index++)
                {
                    remaining.Remove(shell[index].Coordinate);
                }
            }

            return shells;
        }

        private static List<ProceduralLevelBatchGenerator.TilePlacementData> ExtractSurfaceShell(HashSet<Vector3Int> occupiedCoordinates)
        {
            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = new List<ProceduralLevelBatchGenerator.TilePlacementData>();
            foreach (Vector3Int coordinate in occupiedCoordinates)
            {
                for (int directionIndex = 0; directionIndex < NeighborDirections.Length; directionIndex++)
                {
                    Vector3Int neighbor = coordinate + NeighborDirections[directionIndex];
                    if (!occupiedCoordinates.Contains(neighbor))
                    {
                        VoxelGridDirection facingDirection = ToGridDirection(NeighborDirections[directionIndex]);
                        shell.Add(new ProceduralLevelBatchGenerator.TilePlacementData
                        {
                            Coordinate = coordinate,
                            FacingDirection = facingDirection,
                            SurfaceSlotIndex = -1,
                        });
                    }
                }
            }

            return shell;
        }

        private static VoxelGridDirection ToGridDirection(Vector3Int offset)
        {
            if (offset == Vector3Int.left)
            {
                return VoxelGridDirection.Left;
            }

            if (offset == Vector3Int.right)
            {
                return VoxelGridDirection.Right;
            }

            if (offset == Vector3Int.down)
            {
                return VoxelGridDirection.Down;
            }

            if (offset == Vector3Int.up)
            {
                return VoxelGridDirection.Up;
            }

            if (offset == new Vector3Int(0, 0, -1))
            {
                return VoxelGridDirection.Back;
            }

            return VoxelGridDirection.Forward;
        }

        private static readonly Vector3Int[] NeighborDirections =
        {
            Vector3Int.left,
            Vector3Int.right,
            Vector3Int.down,
            Vector3Int.up,
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 0, 1),
        };
    }
}
