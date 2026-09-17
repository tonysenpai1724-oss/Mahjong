namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Published whenever a tile enters or leaves the currently exposed set.
    /// </summary>
    public readonly struct TileExposureChangedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TileExposureChangedEvent"/> struct.
        /// </summary>
        /// <param name="tile">Tile whose exposure changed.</param>
        /// <param name="isExposed">True when the tile is currently exposed; otherwise false.</param>
        public TileExposureChangedEvent(MahjongTile tile, bool isExposed)
        {
            Tile = tile;
            IsExposed = isExposed;
        }

        /// <summary>
        /// Gets the tile whose exposure changed.
        /// </summary>
        public MahjongTile Tile { get; }

        /// <summary>
        /// Gets a value indicating whether the tile is currently exposed.
        /// </summary>
        public bool IsExposed { get; }
    }
}
