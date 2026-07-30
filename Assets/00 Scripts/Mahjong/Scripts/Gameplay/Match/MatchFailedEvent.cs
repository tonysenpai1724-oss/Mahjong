using MahjongOut3D.TileSystem;

namespace MahjongOut3D.Gameplay
{
    /// <summary>
    /// Published when two selected tiles do not form a valid pair.
    /// </summary>
    public readonly struct MatchFailedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MatchFailedEvent"/> struct.
        /// </summary>
        public MatchFailedEvent(MahjongTile firstTile, MahjongTile secondTile)
        {
            FirstTile = firstTile;
            SecondTile = secondTile;
        }

        /// <summary>
        /// Gets the first selected tile.
        /// </summary>
        public MahjongTile FirstTile { get; }

        /// <summary>
        /// Gets the second selected tile.
        /// </summary>
        public MahjongTile SecondTile { get; }
    }
}
