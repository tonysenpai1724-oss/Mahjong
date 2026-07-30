namespace MahjongOut3D.LevelSystem
{
    /// <summary>
    /// Published after a level generator finishes spawning a voxel level.
    /// </summary>
    public readonly struct LevelGeneratedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LevelGeneratedEvent"/> struct.
        /// </summary>
        /// <param name="levelName">Generated level name.</param>
        /// <param name="spawnedTileCount">Number of spawned tiles.</param>
        /// <param name="grid">Generated active voxel grid.</param>
        public LevelGeneratedEvent(string levelName, int spawnedTileCount, VoxelGridData grid)
        {
            LevelName = levelName;
            SpawnedTileCount = spawnedTileCount;
            Grid = grid;
        }

        /// <summary>
        /// Gets the generated level name.
        /// </summary>
        public string LevelName { get; }

        /// <summary>
        /// Gets the number of spawned tiles.
        /// </summary>
        public int SpawnedTileCount { get; }

        /// <summary>
        /// Gets the generated active voxel grid.
        /// </summary>
        public VoxelGridData Grid { get; }
    }
}
