using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds direct shell placements from occupied block coordinates.
    /// </summary>
    internal static class DirectShellLayoutBuilder
    {
        private static readonly Vector3Int[] NeighborOffsets =
        {
            Vector3Int.left,
            Vector3Int.right,
            Vector3Int.down,
            Vector3Int.up,
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 0, 1),
        };

        public static void AddBox(HashSet<Vector3Int> occupiedCells, int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
        {
            if (occupiedCells == null)
            {
                return;
            }

            for (int x = Mathf.Min(minX, maxX); x <= Mathf.Max(minX, maxX); x++)
            {
                for (int y = Mathf.Min(minY, maxY); y <= Mathf.Max(minY, maxY); y++)
                {
                    for (int z = Mathf.Min(minZ, maxZ); z <= Mathf.Max(minZ, maxZ); z++)
                    {
                        occupiedCells.Add(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        public static List<ProceduralLevelBatchGenerator.TilePlacementData> BuildSurfaceShell(
            HashSet<Vector3Int> occupiedCells,
            ProceduralLevelBatchGenerator.CubeTileMetrics metrics,
            Vector3 cellStep)
        {
            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = new List<ProceduralLevelBatchGenerator.TilePlacementData>();
            if (occupiedCells == null || occupiedCells.Count == 0)
            {
                return shell;
            }

            GetBounds(occupiedCells, out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ);
            Vector3 safeStep = new Vector3(
                Mathf.Max(0.01f, cellStep.x),
                Mathf.Max(0.01f, cellStep.y),
                Mathf.Max(0.01f, cellStep.z));
            Vector3 centerOffset = new Vector3(
                (maxX - minX) * safeStep.x,
                (maxY - minY) * safeStep.y,
                (maxZ - minZ) * safeStep.z) * 0.5f;
            Vector3 normalPadding = new Vector3(0.02f, 0.02f, 0.02f);

            foreach (Vector3Int coordinate in occupiedCells)
            {
                Vector3 baseLocalPosition = new Vector3(
                    (coordinate.x - minX) * safeStep.x,
                    (coordinate.y - minY) * safeStep.y,
                    (coordinate.z - minZ) * safeStep.z) - centerOffset;

                for (int directionIndex = 0; directionIndex < NeighborOffsets.Length; directionIndex++)
                {
                    Vector3Int offset = NeighborOffsets[directionIndex];
                    if (occupiedCells.Contains(coordinate + offset))
                    {
                        continue;
                    }

                    VoxelGridDirection facingDirection = ToGridDirection(offset);
                    Vector3 outwardNormal = ((Vector3)offset).normalized;
                    float faceOffset = GetFaceCenterOffset(facingDirection, safeStep);
                    Vector3 localPosition = baseLocalPosition + (outwardNormal * (faceOffset + 0.02f));
                    localPosition += Vector3.Scale(outwardNormal, normalPadding);
                    shell.Add(CreatePlacement(coordinate, localPosition, facingDirection));
                }
            }

            return shell;
        }

        private static float GetFaceCenterOffset(VoxelGridDirection facingDirection, Vector3 cellStep)
        {
            switch (facingDirection)
            {
                case VoxelGridDirection.Left:
                case VoxelGridDirection.Right:
                    return Mathf.Max(0.01f, cellStep.x * 0.5f);

                case VoxelGridDirection.Down:
                case VoxelGridDirection.Up:
                    return Mathf.Max(0.01f, cellStep.y * 0.5f);

                case VoxelGridDirection.Back:
                case VoxelGridDirection.Forward:
                default:
                    return Mathf.Max(0.01f, cellStep.z * 0.5f);
            }
        }

        private static void GetBounds(HashSet<Vector3Int> occupiedCells, out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ)
        {
            minX = int.MaxValue;
            minY = int.MaxValue;
            minZ = int.MaxValue;
            maxX = int.MinValue;
            maxY = int.MinValue;
            maxZ = int.MinValue;

            foreach (Vector3Int coordinate in occupiedCells)
            {
                minX = Mathf.Min(minX, coordinate.x);
                maxX = Mathf.Max(maxX, coordinate.x);
                minY = Mathf.Min(minY, coordinate.y);
                maxY = Mathf.Max(maxY, coordinate.y);
                minZ = Mathf.Min(minZ, coordinate.z);
                maxZ = Mathf.Max(maxZ, coordinate.z);
            }
        }

        private static ProceduralLevelBatchGenerator.TilePlacementData CreatePlacement(Vector3Int coordinate, Vector3 localPosition, VoxelGridDirection facingDirection)
        {
            return new ProceduralLevelBatchGenerator.TilePlacementData
            {
                Coordinate = coordinate,
                FacingDirection = facingDirection,
                SurfaceSlotIndex = -1,
                CustomLocalPosition = localPosition,
                CustomLocalEulerAngles = GetFacingBaseEuler(facingDirection),
                UseCustomLocalPosition = true,
                UseCustomLocalEulerAngles = true,
                ApplyShellCompaction = false,
            };
        }

        private static Vector3 GetFacingBaseEuler(VoxelGridDirection facingDirection)
        {
            switch (facingDirection)
            {
                case VoxelGridDirection.Left:
                    return new Vector3(0f, 0f, 90f);

                case VoxelGridDirection.Right:
                    return new Vector3(0f, 0f, 270f);

                case VoxelGridDirection.Down:
                    return new Vector3(0f, 0f, 180f);

                case VoxelGridDirection.Up:
                    return Vector3.zero;

                case VoxelGridDirection.Back:
                    return new Vector3(270f, 0f, 0f);

                case VoxelGridDirection.Forward:
                default:
                    return new Vector3(90f, 0f, 0f);
            }
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
    }
}
