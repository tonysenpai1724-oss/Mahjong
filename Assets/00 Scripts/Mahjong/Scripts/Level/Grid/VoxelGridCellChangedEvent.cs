using UnityEngine;

namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Published whenever a voxel grid cell changes occupancy.
    /// </summary>
    public readonly struct VoxelGridCellChangedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelGridCellChangedEvent"/> struct.
        /// </summary>
        /// <param name="grid">Grid that changed.</param>
        /// <param name="coordinate">Coordinate that changed.</param>
        /// <param name="previousTileId">Previous tile id, or -1 when empty.</param>
        /// <param name="currentTileId">Current tile id, or -1 when empty.</param>
        public VoxelGridCellChangedEvent(VoxelGridData grid, Vector3Int coordinate, int previousTileId, int currentTileId)
        {
            Grid = grid;
            Coordinate = coordinate;
            PreviousTileId = previousTileId;
            CurrentTileId = currentTileId;
        }

        /// <summary>
        /// Gets the grid that changed.
        /// </summary>
        public VoxelGridData Grid { get; }

        /// <summary>
        /// Gets the cell coordinate that changed.
        /// </summary>
        public Vector3Int Coordinate { get; }

        /// <summary>
        /// Gets the previous tile id, or -1 when the cell was empty.
        /// </summary>
        public int PreviousTileId { get; }

        /// <summary>
        /// Gets the current tile id, or -1 when the cell is empty.
        /// </summary>
        public int CurrentTileId { get; }
    }
}
