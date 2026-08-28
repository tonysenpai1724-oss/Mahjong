using System.Collections.Generic;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Builds direct shell placements from occupied block coordinates.
    /// </summary>
    internal static class DirectShellLayoutBuilder
    {
        private const float MinimumSeparation = 0.001f;
        private const float FacePadding = 0.02f;

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
            Vector3 cellStep,
            float sideNormalClearance = 0f,
            float frontBackNormalClearance = 0f,
            float upDownNormalClearance = 0f)
        {
            List<ProceduralLevelBatchGenerator.TilePlacementData> shell = new List<ProceduralLevelBatchGenerator.TilePlacementData>();
            if (occupiedCells == null || occupiedCells.Count == 0)
            {
                return shell;
            }

            GetBounds(occupiedCells, out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ);
            float faceWidth = Mathf.Max(0.01f, metrics.FaceWidth);
            float faceHeight = Mathf.Max(0.01f, metrics.FaceHeight);
            float thickness = Mathf.Max(0.01f, metrics.Thickness);
            float xStep = Mathf.Max(faceWidth + MinimumSeparation, cellStep.x);
            float yStep = Mathf.Max(faceWidth + MinimumSeparation, cellStep.y);
            float zStep = Mathf.Max(faceHeight + MinimumSeparation, cellStep.z);
            float sideClearance = Mathf.Max(0f, sideNormalClearance);
            float frontBackClearance = Mathf.Max(0f, frontBackNormalClearance);
            float upDownClearance = Mathf.Max(0f, upDownNormalClearance);
            Vector3 center = new Vector3(
                (minX + maxX) * 0.5f,
                (minY + maxY) * 0.5f,
                (minZ + maxZ) * 0.5f);

            foreach (Vector3Int coordinate in occupiedCells)
            {
                for (int directionIndex = 0; directionIndex < NeighborOffsets.Length; directionIndex++)
                {
                    Vector3Int offset = NeighborOffsets[directionIndex];
                    if (occupiedCells.Contains(coordinate + offset))
                    {
                        continue;
                    }

                    VoxelGridDirection facingDirection = ToGridDirection(offset);
                    Vector3 localPosition = GetFacePosition(
                        coordinate,
                        center,
                        facingDirection,
                        xStep,
                        yStep,
                        zStep,
                        thickness,
                        sideClearance,
                        frontBackClearance,
                        upDownClearance);
                    shell.Add(CreatePlacement(coordinate, localPosition, facingDirection));
                }
            }

            return shell;
        }

        private static Vector3 GetFacePosition(
            Vector3Int coordinate,
            Vector3 center,
            VoxelGridDirection facingDirection,
            float xStep,
            float yStep,
            float zStep,
            float thickness,
            float sideClearance,
            float frontBackClearance,
            float upDownClearance)
        {
            float centeredX = (coordinate.x - center.x) * xStep;
            float centeredY = (coordinate.y - center.y) * yStep;
            float centeredZ = (coordinate.z - center.z) * zStep;
            float halfThickness = thickness * 0.5f;

            switch (facingDirection)
            {
                case VoxelGridDirection.Left:
                    return new Vector3(
                        centeredX - (xStep * 0.5f) - sideClearance + halfThickness - FacePadding,
                        centeredY,
                        centeredZ);

                case VoxelGridDirection.Right:
                    return new Vector3(
                        centeredX + (xStep * 0.5f) + sideClearance - halfThickness + FacePadding,
                        centeredY,
                        centeredZ);

                case VoxelGridDirection.Down:
                    return new Vector3(
                        centeredX,
                        centeredY - (yStep * 0.5f) - upDownClearance + halfThickness - FacePadding,
                        centeredZ);

                case VoxelGridDirection.Up:
                    return new Vector3(
                        centeredX,
                        centeredY + (yStep * 0.5f) + upDownClearance - halfThickness + FacePadding,
                        centeredZ);

                case VoxelGridDirection.Back:
                    return new Vector3(
                        centeredX,
                        centeredY,
                        centeredZ - (zStep * 0.5f) - frontBackClearance + halfThickness - FacePadding);

                case VoxelGridDirection.Forward:
                default:
                    return new Vector3(
                        centeredX,
                        centeredY,
                        centeredZ + (zStep * 0.5f) + frontBackClearance - halfThickness + FacePadding);
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
