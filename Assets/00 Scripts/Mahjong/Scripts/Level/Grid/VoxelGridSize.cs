using System;
using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Stores the integer dimensions of a voxel grid volume.
    /// </summary>
    [Serializable]
    public struct VoxelGridSize : IEquatable<VoxelGridSize>
    {
        [SerializeField, Min(1)] private int width;
        [SerializeField, Min(1)] private int height;
        [SerializeField, Min(1)] private int depth;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelGridSize"/> struct.
        /// </summary>
        /// <param name="width">Grid width on the x-axis.</param>
        /// <param name="height">Grid height on the y-axis.</param>
        /// <param name="depth">Grid depth on the z-axis.</param>
        public VoxelGridSize(int width, int height, int depth)
        {
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);
            this.depth = Mathf.Max(1, depth);
        }

        /// <summary>
        /// Gets the width of the grid on the x-axis.
        /// </summary>
        public int Width => Mathf.Max(1, width);

        /// <summary>
        /// Gets the height of the grid on the y-axis.
        /// </summary>
        public int Height => Mathf.Max(1, height);

        /// <summary>
        /// Gets the depth of the grid on the z-axis.
        /// </summary>
        public int Depth => Mathf.Max(1, depth);

        /// <summary>
        /// Gets the total number of cells inside the grid volume.
        /// </summary>
        public int Volume => Width * Height * Depth;

        /// <summary>
        /// Gets a value indicating whether the dimensions are valid.
        /// </summary>
        public bool IsValid => Width > 0 && Height > 0 && Depth > 0;

        /// <summary>
        /// Gets the maximum valid coordinate inside the grid.
        /// </summary>
        public Vector3Int MaxCoordinate => new Vector3Int(Width - 1, Height - 1, Depth - 1);

        /// <summary>
        /// Checks whether the specified coordinate is inside the grid bounds.
        /// </summary>
        /// <param name="coordinate">Coordinate to test.</param>
        /// <returns>True when the coordinate is inside the grid; otherwise false.</returns>
        public bool Contains(Vector3Int coordinate)
        {
            return coordinate.x >= 0 && coordinate.x < Width
                && coordinate.y >= 0 && coordinate.y < Height
                && coordinate.z >= 0 && coordinate.z < Depth;
        }

        /// <summary>
        /// Converts the size to a Vector3Int.
        /// </summary>
        /// <returns>Vector3Int containing width, height and depth.</returns>
        public Vector3Int ToVector3Int()
        {
            return new Vector3Int(Width, Height, Depth);
        }

        /// <summary>
        /// Compares this size to another size.
        /// </summary>
        /// <param name="other">Other size to compare.</param>
        /// <returns>True when both sizes are equal; otherwise false.</returns>
        public bool Equals(VoxelGridSize other)
        {
            return Width == other.Width && Height == other.Height && Depth == other.Depth;
        }

        /// <summary>
        /// Compares this size to another object.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True when the object is an equal size; otherwise false.</returns>
        public override bool Equals(object obj)
        {
            return obj is VoxelGridSize other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for this size.
        /// </summary>
        /// <returns>Hash code for the current size.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height, Depth);
        }

        /// <summary>
        /// Compares two sizes for equality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True when both sizes are equal; otherwise false.</returns>
        public static bool operator ==(VoxelGridSize left, VoxelGridSize right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two sizes for inequality.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True when both sizes differ; otherwise false.</returns>
        public static bool operator !=(VoxelGridSize left, VoxelGridSize right)
        {
            return !left.Equals(right);
        }
    }
}
