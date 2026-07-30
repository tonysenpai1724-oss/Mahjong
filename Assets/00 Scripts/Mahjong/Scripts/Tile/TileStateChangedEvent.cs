namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Published whenever a Mahjong tile changes runtime state.
    /// </summary>
    public readonly struct TileStateChangedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TileStateChangedEvent"/> struct.
        /// </summary>
        /// <param name="tile">Tile that changed state.</param>
        /// <param name="previousState">Previous tile state.</param>
        /// <param name="currentState">Current tile state.</param>
        public TileStateChangedEvent(MahjongTile tile, TileState previousState, TileState currentState)
        {
            Tile = tile;
            PreviousState = previousState;
            CurrentState = currentState;
        }

        /// <summary>
        /// Gets the tile that changed state.
        /// </summary>
        public MahjongTile Tile { get; }

        /// <summary>
        /// Gets the previous tile state.
        /// </summary>
        public TileState PreviousState { get; }

        /// <summary>
        /// Gets the current tile state.
        /// </summary>
        public TileState CurrentState { get; }
    }
}
