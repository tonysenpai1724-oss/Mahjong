using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Stores spacing and pivot rules used to convert voxel coordinates into local positions.
    /// </summary>
    [CreateAssetMenu(menuName = "Mahjong Out 3D/Level/Voxel Grid Layout Settings", fileName = "VoxelGridLayoutSettings")]
    public sealed class VoxelGridLayoutSettings : ScriptableObject
    {
        [field: Header("Cell")]
        [field: SerializeField]
        public Vector3 CellSize { get; private set; } = Vector3.one;

        [field: SerializeField]
        public Vector3 CellSpacing { get; private set; } = new Vector3(0.05f, 0.05f, 0.05f);

        [field: Header("Pivot")]
        [field: SerializeField]
        public VoxelGridPivotMode PivotMode { get; private set; } = VoxelGridPivotMode.Center;

        [field: SerializeField]
        public Vector3 OriginOffset { get; private set; } = Vector3.zero;

        /// <summary>
        /// Gets the step distance between two adjacent cells.
        /// </summary>
        public Vector3 CellStep => CellSize + CellSpacing;

        /// <summary>
        /// Converts a grid coordinate into a local-space position.
        /// </summary>
        /// <param name="coordinate">Grid coordinate to convert.</param>
        /// <param name="gridSize">Grid size used to compute the pivot offset.</param>
        /// <returns>Local-space position for the specified coordinate.</returns>
        public Vector3 GetLocalPosition(Vector3Int coordinate, VoxelGridSize gridSize)
        {
            Vector3 step = CellStep;
            Vector3 position = Vector3.Scale((Vector3)coordinate, step);

            if (PivotMode == VoxelGridPivotMode.Center)
            {
                Vector3 centerOffset = new Vector3(
                    (gridSize.Width - 1) * step.x,
                    (gridSize.Height - 1) * step.y,
                    (gridSize.Depth - 1) * step.z) * 0.5f;

                position -= centerOffset;
            }

            return position + OriginOffset;
        }

        /// <summary>
        /// Calculates the local-space bounds occupied by the grid volume.
        /// </summary>
        /// <param name="gridSize">Grid size to evaluate.</param>
        /// <returns>Bounds enclosing the voxel grid in local space.</returns>
        public Bounds GetLocalBounds(VoxelGridSize gridSize)
        {
            Vector3 step = CellStep;
            Vector3 extentsFromCells = new Vector3(
                Mathf.Max(0f, (gridSize.Width - 1) * step.x),
                Mathf.Max(0f, (gridSize.Height - 1) * step.y),
                Mathf.Max(0f, (gridSize.Depth - 1) * step.z));

            Vector3 size = new Vector3(
                extentsFromCells.x + CellSize.x,
                extentsFromCells.y + CellSize.y,
                extentsFromCells.z + CellSize.z);

            Vector3 center = OriginOffset;
            if (PivotMode == VoxelGridPivotMode.MinCorner)
            {
                center += extentsFromCells * 0.5f;
            }

            return new Bounds(center, size);
        }
    }
}
