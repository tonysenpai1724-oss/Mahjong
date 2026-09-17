namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Published when the runtime switches from one active voxel grid to another.
    /// </summary>
    public readonly struct ActiveVoxelGridChangedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveVoxelGridChangedEvent"/> struct.
        /// </summary>
        /// <param name="previousGrid">Previously active grid, or null.</param>
        /// <param name="currentGrid">Currently active grid, or null.</param>
        public ActiveVoxelGridChangedEvent(VoxelGridData previousGrid, VoxelGridData currentGrid)
        {
            PreviousGrid = previousGrid;
            CurrentGrid = currentGrid;
        }

        /// <summary>
        /// Gets the previously active grid.
        /// </summary>
        public VoxelGridData PreviousGrid { get; }

        /// <summary>
        /// Gets the currently active grid.
        /// </summary>
        public VoxelGridData CurrentGrid { get; }
    }
}
