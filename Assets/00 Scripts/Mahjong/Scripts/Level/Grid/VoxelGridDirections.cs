using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Provides reusable lookup helpers for voxel grid directions and offsets.
    /// </summary>
    public static class VoxelGridDirections
    {
        private static readonly VoxelGridDirection[] AllDirections =
        {
            VoxelGridDirection.Left,
            VoxelGridDirection.Right,
            VoxelGridDirection.Down,
            VoxelGridDirection.Up,
            VoxelGridDirection.Back,
            VoxelGridDirection.Forward,
        };

        /// <summary>
        /// Gets the six cardinal directions used by the grid.
        /// </summary>
        public static VoxelGridDirection[] Cardinals => AllDirections;

        /// <summary>
        /// Gets the coordinate offset for the specified grid direction.
        /// </summary>
        /// <param name="direction">Direction to convert.</param>
        /// <returns>Coordinate offset for the direction.</returns>
        public static Vector3Int GetOffset(VoxelGridDirection direction)
        {
            switch (direction)
            {
                case VoxelGridDirection.Left:
                    return Vector3Int.left;
                case VoxelGridDirection.Right:
                    return Vector3Int.right;
                case VoxelGridDirection.Down:
                    return Vector3Int.down;
                case VoxelGridDirection.Up:
                    return Vector3Int.up;
                case VoxelGridDirection.Back:
                    return new Vector3Int(0, 0, -1);
                case VoxelGridDirection.Forward:
                    return new Vector3Int(0, 0, 1);
                default:
                    return Vector3Int.zero;
            }
        }

        /// <summary>
        /// Gets the opposite of the specified grid direction.
        /// </summary>
        /// <param name="direction">Direction to invert.</param>
        /// <returns>Opposite grid direction.</returns>
        public static VoxelGridDirection GetOpposite(VoxelGridDirection direction)
        {
            switch (direction)
            {
                case VoxelGridDirection.Left:
                    return VoxelGridDirection.Right;
                case VoxelGridDirection.Right:
                    return VoxelGridDirection.Left;
                case VoxelGridDirection.Down:
                    return VoxelGridDirection.Up;
                case VoxelGridDirection.Up:
                    return VoxelGridDirection.Down;
                case VoxelGridDirection.Back:
                    return VoxelGridDirection.Forward;
                case VoxelGridDirection.Forward:
                    return VoxelGridDirection.Back;
                default:
                    return direction;
            }
        }
    }
}
