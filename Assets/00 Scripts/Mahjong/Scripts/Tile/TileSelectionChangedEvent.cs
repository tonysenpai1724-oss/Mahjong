namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Published whenever a Mahjong tile enters or exits the selected state.
    /// </summary>
    public readonly struct TileSelectionChangedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TileSelectionChangedEvent"/> struct.
        /// </summary>
        /// <param name="tile">Tile whose selection changed.</param>
        /// <param name="isSelected">True when the tile is selected; otherwise false.</param>
        public TileSelectionChangedEvent(MahjongTile tile, bool isSelected)
        {
            Tile = tile;
            IsSelected = isSelected;
        }

        /// <summary>
        /// Gets the tile whose selection changed.
        /// </summary>
        public MahjongTile Tile { get; }

        /// <summary>
        /// Gets a value indicating whether the tile is selected.
        /// </summary>
        public bool IsSelected { get; }
    }
}
