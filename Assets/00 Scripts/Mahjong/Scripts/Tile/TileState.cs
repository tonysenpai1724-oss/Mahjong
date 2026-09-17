namespace MahjongOut3D.TileSystem
{
    /// <summary>
    /// Defines the runtime lifecycle state of a Mahjong tile.
    /// </summary>
    public enum TileState
    {
        Hidden = 0,
        Visible = 1,
        Selected = 2,
        Matched = 3,
        Removed = 4,
    }
}
