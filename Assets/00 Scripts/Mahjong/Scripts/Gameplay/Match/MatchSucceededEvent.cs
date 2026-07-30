using MahjongOut3D.TileSystem;

namespace MahjongOut3D.Gameplay
{
    /// <summary>
    /// Published after two tiles are matched and removed successfully.
    /// </summary>
    public readonly struct MatchSucceededEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MatchSucceededEvent"/> struct.
        /// </summary>
        public MatchSucceededEvent(MahjongTile firstTile, MahjongTile secondTile)
        {
            FirstTile = firstTile;
            SecondTile = secondTile;
        }

        /// <summary>
        /// Gets the first matched tile.
        /// </summary>
        public MahjongTile FirstTile { get; }

        /// <summary>
        /// Gets the second matched tile.
        /// </summary>
        public MahjongTile SecondTile { get; }
    }
}
