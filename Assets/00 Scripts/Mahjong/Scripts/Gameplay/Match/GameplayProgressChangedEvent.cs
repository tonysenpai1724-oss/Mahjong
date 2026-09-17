namespace MahjongOut3D.Gameplay
{
    /// <summary>
    /// Published when gameplay progress changes because tiles were spawned, removed or restored.
    /// </summary>
    public readonly struct GameplayProgressChangedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameplayProgressChangedEvent"/> struct.
        /// </summary>
        public GameplayProgressChangedEvent(int remainingTiles, int totalTiles)
        {
            RemainingTiles = remainingTiles;
            TotalTiles = totalTiles;
        }

        /// <summary>
        /// Gets the number of remaining tiles.
        /// </summary>
        public int RemainingTiles { get; }

        /// <summary>
        /// Gets the total number of spawned tiles for the level.
        /// </summary>
        public int TotalTiles { get; }

        /// <summary>
        /// Gets the normalized completion ratio in the range [0, 1].
        /// </summary>
        public float CompletionRatio => TotalTiles <= 0 ? 0f : 1f - ((float)RemainingTiles / TotalTiles);
    }
}
