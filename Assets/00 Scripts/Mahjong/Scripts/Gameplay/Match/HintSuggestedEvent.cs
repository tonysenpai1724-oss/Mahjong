using MahjongOut3D.TileSystem;

namespace MahjongOut3D.Gameplay
{
    /// <summary>
    /// Published when the hint system suggests a valid exposed pair.
    /// </summary>
    public readonly struct HintSuggestedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HintSuggestedEvent"/> struct.
        /// </summary>
        public HintSuggestedEvent(MahjongTile firstTile, MahjongTile secondTile)
        {
            FirstTile = firstTile;
            SecondTile = secondTile;
        }

        /// <summary>
        /// Gets the first suggested tile.
        /// </summary>
        public MahjongTile FirstTile { get; }

        /// <summary>
        /// Gets the second suggested tile.
        /// </summary>
        public MahjongTile SecondTile { get; }
    }
}
